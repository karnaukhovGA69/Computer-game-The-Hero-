using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheHero.Generated;

/// <summary>
/// The Hero — Build Map From Today Generated Assets
/// MenuItem: The Hero/Map/Build Map From Today Generated Assets
///
/// Scans GeneratedAssets/ for PNG+JSON pairs modified TODAY, classifies them
/// by the prompt text inside the JSON sidecar (NOT by filename), imports the
/// chosen sprites into Assets/Sprites/GeneratedToday/&lt;category&gt;/, then
/// builds an adventure-style 36x24 map in Map.unity.
/// </summary>
public static class TheHeroBuildMapFromTodayGeneratedAssets
{
    // ─── Constants ────────────────────────────────────────────────────────────
    private const int MAP_W = 36;
    private const int MAP_H = 24;
    private const int SEED = 17701729;

    private const string GEN_ROOT_REL    = "../GeneratedAssets";              // relative to Application.dataPath
    private const string IMPORT_DIR_REL  = "Sprites/GeneratedToday";           // relative to Assets
    private const string SCENE_PATH      = "Assets/Scenes/Map.unity";
    private const string BACKUP_PATH     = "Assets/Scenes/Map_backup_before_today_rebuild.unity";
    private const string REPORT_OK       = "Assets/CodeAudit/MapBuild_FromTodayAssets_Report.md";
    private const string REPORT_FAIL     = "Assets/CodeAudit/MapBuild_FromTodayAssets_FAILED.md";

    // ─── Categories ───────────────────────────────────────────────────────────
    private enum Cat
    {
        Unknown,
        Grass, GrassDry,
        Road, RoadTurn, RoadT, RoadCross,
        Forest, ForestEdge, ForestCornerInner, ForestCornerOuter,
        Mountain, MountainEdge, MountainCornerInner, MountainCornerOuter, MountainPeak,
        River, RiverEdge, RiverCornerInner, RiverCornerOuter, RiverDiagCorner, RiverBridge,
        Dark, DarkEdge,
        Castle, Hero, Chest, Artifact, Mine, Gold, Wood, Stone, Mana,
        EnemyWeak, EnemyMedium, EnemyStrong, EnemyBoss
    }

    private enum TType
    {
        Meadow, Road, Forest, DenseForest,
        River, Bridge, Mountain, Dark
    }

    private class Asset
    {
        public string Guid;            // hash folder name
        public string SourcePng;       // absolute source path
        public string SourceJson;      // absolute source path
        public string Prompt;          // raw prompt text
        public Cat Category;           // classified
        public string AssetPath;       // post-import "Assets/Sprites/GeneratedToday/..."
        public Sprite Sprite;          // loaded sprite after import
        public string ClassifyReason;  // human-readable
    }

    private static readonly Dictionary<Cat, List<Asset>> _bucket = new Dictionary<Cat, List<Asset>>();
    private static TType[,] _tileMap;
    private static StringBuilder _log = new StringBuilder();
    private static int _grassTilesPlaced, _forestTilesPlaced, _riverTilesPlaced,
                       _mountainTilesPlaced, _darkTilesPlaced, _roadTilesPlaced, _bridgeTilesPlaced;

    // ─── Entry ────────────────────────────────────────────────────────────────
    [MenuItem("The Hero/Map/Build Map From Today Generated Assets")]
    public static void BuildMap()
    {
        _log.Clear();
        foreach (Cat c in Enum.GetValues(typeof(Cat))) _bucket[c] = new List<Asset>();
        _grassTilesPlaced = _forestTilesPlaced = _riverTilesPlaced =
            _mountainTilesPlaced = _darkTilesPlaced = _roadTilesPlaced = _bridgeTilesPlaced = 0;

        Log("[TheHeroMapBuild] === Build Map From Today Generated Assets ===");

        // 1. Backup scene
        try
        {
            string src = Path.Combine(Application.dataPath, "../" + SCENE_PATH);
            string dst = Path.Combine(Application.dataPath, "../" + BACKUP_PATH);
            if (File.Exists(src)) File.Copy(src, dst, true);
            Log("[TheHeroMapBuild] Scene backup created: " + BACKUP_PATH);
        }
        catch (Exception ex) { FailAndReport("Backup failed: " + ex.Message); return; }

        // 2. Find today's generated assets
        var assets = ScanTodayAssets();
        if (assets.Count == 0)
        {
            FailAndReport("No PNG+JSON pairs modified today were found under GeneratedAssets/.");
            return;
        }
        Log("[TheHeroMapBuild] Today generated assets found: " + assets.Count);

        // 3. Read JSON & classify
        int jsonOk = 0;
        foreach (var a in assets)
        {
            try
            {
                a.Prompt = File.ReadAllText(a.SourceJson);
                a.Category = Classify(a.Prompt, out string reason);
                a.ClassifyReason = reason;
                if (!string.IsNullOrEmpty(a.Prompt)) jsonOk++;
            }
            catch (Exception ex)
            {
                a.Category = Cat.Unknown;
                a.ClassifyReason = "JSON read failed: " + ex.Message;
            }
        }
        Log("[TheHeroMapBuild] JSON manifests read: " + jsonOk);

        // 4. Bucket
        foreach (var a in assets)
            if (a.Category != Cat.Unknown) _bucket[a.Category].Add(a);

        // 5. Validate catalog has terrain & key objects
        if (!CatalogValid(out string missing))
        {
            FailAndReport("Catalog incomplete. Missing required categories:" + missing);
            return;
        }
        Log("[TheHeroMapBuild] Asset catalog created");

        // 6. Import chosen PNGs to Assets/
        try { ImportSelected(); }
        catch (Exception ex) { FailAndReport("Import failed: " + ex.Message); return; }
        AssetDatabase.Refresh();
        Log("[TheHeroMapBuild] Sprites imported into " + IMPORT_DIR_REL);

        // 7. Open Map.unity
        UnityEngine.SceneManagement.Scene mapScene;
        try
        {
            mapScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }
        catch (Exception ex) { FailAndReport("Cannot open Map.unity: " + ex.Message); return; }
        Log("[TheHeroMapBuild] Map scene opened");

        // 8. Clear old map content (keep MainMenu/Combat/Base/SaveSystem alone — they live in OTHER scenes)
        ClearOldMapContent();
        Log("[TheHeroMapBuild] Old map content cleared");

        // 9. Layout
        BuildTileLayout();
        Log("[TheHeroMapBuild] Tile layout computed");

        // 10. Build
        GameObject mapRoot     = new GameObject("MapRoot");
        GameObject tilesRoot   = new GameObject("Tiles");       tilesRoot.transform.parent = mapRoot.transform;
        GameObject objectsRoot = new GameObject("MapObjects");  objectsRoot.transform.parent = mapRoot.transform;

        if (!BuildTerrain(tilesRoot))
        {
            EditorSceneManager.OpenScene(BACKUP_PATH, OpenSceneMode.Single);
            FailAndReport("Terrain build failed.");
            return;
        }
        Log("[TheHeroMapBuild] New map built");
        Log("[TheHeroMapBuild] Terrain zones placed");
        Log("[TheHeroMapBuild] River and bridges placed");
        Log("[TheHeroMapBuild] Forest placed");
        Log("[TheHeroMapBuild] Mountain pass placed");
        Log("[TheHeroMapBuild] Darkland placed");

        PlaceMapObjects(objectsRoot);
        Log("[TheHeroMapBuild] Objects placed");

        // 11. Path validation
        if (!ValidatePaths(out string pathReport))
        {
            EditorSceneManager.OpenScene(BACKUP_PATH, OpenSceneMode.Single);
            FailAndReport("Path validation FAILED — rolled back.\n" + pathReport);
            return;
        }
        Log("[TheHeroMapBuild] Path validation passed");

        // 12. Save
        EditorSceneManager.MarkSceneDirty(mapScene);
        EditorSceneManager.SaveScene(mapScene, SCENE_PATH);
        Log("[TheHeroMapBuild] Map saved");

        // 13. Report
        WriteSuccessReport();
        AssetDatabase.Refresh();
        Debug.Log("[TheHeroMapBuild] Build complete. Report: " + REPORT_OK);
    }

    // ─── Scan ─────────────────────────────────────────────────────────────────
    private static List<Asset> ScanTodayAssets()
    {
        var list = new List<Asset>();
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, GEN_ROOT_REL));
        if (!Directory.Exists(root))
        {
            Log("[TheHeroMapBuild] GeneratedAssets root not found: " + root);
            return list;
        }

        DateTime today = DateTime.Now.Date;
        DateTime tomorrow = today.AddDays(1);

        foreach (var dir in Directory.GetDirectories(root))
        {
            foreach (var json in Directory.GetFiles(dir, "*.png.json", SearchOption.TopDirectoryOnly))
            {
                string png = json.Substring(0, json.Length - ".json".Length);
                if (!File.Exists(png)) continue;

                DateTime mod = File.GetLastWriteTime(png);
                if (mod < today || mod >= tomorrow) continue;

                list.Add(new Asset
                {
                    Guid = Path.GetFileName(dir),
                    SourcePng = png,
                    SourceJson = json
                });
            }
        }
        return list;
    }

    // ─── Classification (uses JSON prompt, NOT filename) ──────────────────────
    private static Cat Classify(string json, out string reason)
    {
        reason = "";
        string prompt = ExtractField(json, "prompt") ?? "";
        string p = prompt.ToLowerInvariant();
        if (string.IsNullOrEmpty(p)) { reason = "empty prompt"; return Cat.Unknown; }

        // Heuristics ordered from most specific to least.
        // --- Map icons (objects) ---
        if (p.Contains("treasure chest"))                                  { reason = "icon: chest";    return Cat.Chest; }
        if (p.Contains("ancient artifact") || p.Contains("artifact"))      { reason = "icon: artifact"; return Cat.Artifact; }
        if (p.Contains("abandoned mine") || p.Contains("mine entrance"))   { reason = "icon: mine";     return Cat.Mine; }
        if (p.Contains("gold coins") || p.Contains("pile of gold"))        { reason = "icon: gold";     return Cat.Gold; }
        if (p.Contains("wood logs") || p.Contains("pile of wood"))         { reason = "icon: wood";     return Cat.Wood; }
        if (p.Contains("pile of stone") || p.Contains("stones, top-down")) { reason = "icon: stone";    return Cat.Stone; }
        if (p.Contains("mana crystal") || p.Contains("mana"))              { reason = "icon: mana";     return Cat.Mana; }
        if (p.Contains("stone castle") || p.Contains("castle"))            { reason = "icon: castle";   return Cat.Castle; }
        if (p.Contains("hero character") || p.Contains("hero icon") ||
            (p.Contains("hero") && p.Contains("icon")))                    { reason = "icon: hero";     return Cat.Hero; }
        if (p.Contains("dark lord"))                                       { reason = "icon: boss";     return Cat.EnemyBoss; }
        if (p.Contains("orc head") || p.Contains("orc icon"))              { reason = "icon: orc";      return Cat.EnemyStrong; }
        if (p.Contains("skeleton head") || p.Contains("skeleton icon"))    { reason = "icon: skeleton"; return Cat.EnemyMedium; }
        if (p.Contains("goblin head") || p.Contains("goblin icon"))        { reason = "icon: goblin";   return Cat.EnemyWeak; }
        if (p.Contains("wolf head") || p.Contains("wolf icon"))            { reason = "icon: wolf";     return Cat.EnemyWeak; }

        // --- Terrain: roads ---
        if (p.Contains("dirt road"))
        {
            if (p.Contains("crossroads"))      { reason = "road: crossroads"; return Cat.RoadCross; }
            if (p.Contains("t-junction"))      { reason = "road: T";          return Cat.RoadT; }
            if (p.Contains("turn"))            { reason = "road: turn";       return Cat.RoadTurn; }
            reason = "road: segment"; return Cat.Road;
        }

        // --- Terrain: bridges ---
        if (p.Contains("bridge"))                                          { reason = "bridge"; return Cat.RiverBridge; }

        // --- Terrain: river / water ---
        if (p.Contains("river") || p.Contains("water"))
        {
            if (p.Contains("bank"))
            {
                if (p.Contains("inner corner"))     { reason = "river: bank inner corner";  return Cat.RiverCornerInner; }
                if (p.Contains("outer corner"))     { reason = "river: bank outer corner";  return Cat.RiverCornerOuter; }
                reason = "river: bank edge"; return Cat.RiverEdge;
            }
            if (p.Contains("corner"))               { reason = "river: diagonal corner";    return Cat.RiverDiagCorner; }
            reason = "river: center water"; return Cat.River;
        }

        // --- Terrain: darkland ---
        if (p.Contains("darkland") || p.Contains("corrupted soil"))
        {
            if (p.Contains("edge"))                 { reason = "darkland edge"; return Cat.DarkEdge; }
            reason = "darkland center"; return Cat.Dark;
        }

        // --- Terrain: mountain ---
        if (p.Contains("mountain"))
        {
            if (p.Contains("peak"))                 { reason = "mountain peak";          return Cat.MountainPeak; }
            if (p.Contains("inner") && p.Contains("corner"))  { reason = "mountain inner corner"; return Cat.MountainCornerInner; }
            if (p.Contains("outer") && p.Contains("corner"))  { reason = "mountain outer corner"; return Cat.MountainCornerOuter; }
            if (p.Contains("edge"))                 { reason = "mountain edge";          return Cat.MountainEdge; }
            reason = "mountain center"; return Cat.Mountain;
        }

        // --- Terrain: forest ---
        if (p.Contains("forest") || p.Contains("tree") || p.Contains("canopy"))
        {
            if (p.Contains("inner") && p.Contains("corner")) { reason = "forest inner corner"; return Cat.ForestCornerInner; }
            if (p.Contains("outer") && p.Contains("corner")) { reason = "forest outer corner"; return Cat.ForestCornerOuter; }
            if (p.Contains("edge"))                 { reason = "forest edge";   return Cat.ForestEdge; }
            reason = "forest center"; return Cat.Forest;
        }

        // --- Terrain: grass (last, since many prompts mention grass as context) ---
        if (p.Contains("yellowish grass") || p.Contains("dry "))           { reason = "grass: dry";     return Cat.GrassDry; }
        if (p.Contains("grass"))                                           { reason = "grass: lush";    return Cat.Grass; }

        reason = "unrecognized prompt"; return Cat.Unknown;
    }

    private static string ExtractField(string json, string key)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var m = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        if (!m.Success) return null;
        return Regex.Unescape(m.Groups[1].Value);
    }

    private static bool CatalogValid(out string missing)
    {
        missing = "";
        if (_bucket[Cat.Grass].Count + _bucket[Cat.GrassDry].Count == 0) missing += " grass";
        if (_bucket[Cat.Forest].Count == 0)   missing += " forest";
        if (_bucket[Cat.River].Count == 0)    missing += " river";
        if (_bucket[Cat.Mountain].Count == 0) missing += " mountain";
        if (_bucket[Cat.Dark].Count == 0)     missing += " darkland";
        if (_bucket[Cat.Castle].Count == 0)   missing += " castle";
        if (_bucket[Cat.EnemyBoss].Count == 0) missing += " dark-lord";
        return missing.Length == 0;
    }

    // ─── Import ───────────────────────────────────────────────────────────────
    private static void ImportSelected()
    {
        string baseDir = Path.Combine(Application.dataPath, IMPORT_DIR_REL);
        Directory.CreateDirectory(baseDir);

        foreach (var pair in _bucket)
        {
            if (pair.Value.Count == 0) continue;
            string subdir = pair.Key.ToString();
            string targetDir = Path.Combine(baseDir, subdir);
            Directory.CreateDirectory(targetDir);

            int i = 0;
            foreach (var a in pair.Value)
            {
                string newName = subdir + "_" + i.ToString("D2") + ".png";
                string dst = Path.Combine(targetDir, newName);
                File.Copy(a.SourcePng, dst, true);
                a.AssetPath = "Assets/" + IMPORT_DIR_REL.Replace('\\','/') + "/" + subdir + "/" + newName;
                i++;
            }
        }

        AssetDatabase.Refresh();

        // Configure import settings uniformly
        foreach (var pair in _bucket)
        {
            bool isObject = IsObjectCategory(pair.Key);
            foreach (var a in pair.Value)
            {
                if (string.IsNullOrEmpty(a.AssetPath)) continue;
                var imp = AssetImporter.GetAtPath(a.AssetPath) as TextureImporter;
                if (imp == null) continue;
                imp.textureType         = TextureImporterType.Sprite;
                imp.spriteImportMode    = SpriteImportMode.Single;
                imp.spritePixelsPerUnit = 1024f;            // 1024px PNG → 1 world unit (1 tile)
                imp.alphaIsTransparency = isObject;
                imp.mipmapEnabled       = false;
                imp.filterMode          = FilterMode.Bilinear;
                imp.wrapMode            = TextureWrapMode.Clamp;
                imp.textureCompression  = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
        }

        AssetDatabase.Refresh();

        // Load sprite refs
        foreach (var pair in _bucket)
            foreach (var a in pair.Value)
                if (!string.IsNullOrEmpty(a.AssetPath))
                    a.Sprite = AssetDatabase.LoadAssetAtPath<Sprite>(a.AssetPath);
    }

    private static bool IsObjectCategory(Cat c)
    {
        switch (c)
        {
            case Cat.Castle: case Cat.Hero: case Cat.Chest: case Cat.Artifact:
            case Cat.Mine: case Cat.Gold: case Cat.Wood: case Cat.Stone: case Cat.Mana:
            case Cat.EnemyWeak: case Cat.EnemyMedium: case Cat.EnemyStrong: case Cat.EnemyBoss:
                return true;
            default: return false;
        }
    }

    // ─── Layout ───────────────────────────────────────────────────────────────
    private static void BuildTileLayout()
    {
        _tileMap = new TType[MAP_W, MAP_H];
        for (int x = 0; x < MAP_W; x++)
        for (int y = 0; y < MAP_H; y++)
            _tileMap[x, y] = TType.Meadow;

        // River — vertical strip x=18..19 with bridge at y=4..5
        for (int x = 18; x <= 19; x++)
        for (int y = 0; y < MAP_H; y++)
            _tileMap[x, y] = TType.River;

        for (int y = 4; y <= 5; y++)
        {
            _tileMap[18, y] = TType.Bridge;
            _tileMap[19, y] = TType.Bridge;
        }

        // Forest patch — organic blob centered ~ (9..15, 10..18)
        for (int x = 9; x <= 15; x++)
        for (int y = 10; y <= 18; y++)
        {
            // organic shape: trim corners
            if (x == 9  && (y >= 17 || y <= 11)) continue;
            if (x == 15 && (y >= 17 || y <= 11)) continue;
            if (x == 10 && y >= 18) continue;
            _tileMap[x, y] = TType.Forest;
        }
        // Dense forest core
        for (int x = 11; x <= 14; x++)
        for (int y = 12; y <= 16; y++)
            _tileMap[x, y] = TType.DenseForest;
        // Forest passages (so it isn't a blob)
        _tileMap[12, 10] = TType.Meadow;
        _tileMap[13, 10] = TType.Meadow;

        // Mountain mass — right-center
        for (int x = 21; x <= 32; x++)
        for (int y = 9; y <= 20; y++)
            _tileMap[x, y] = TType.Mountain;
        // Horizontal mountain pass at y = 13..14
        for (int x = 20; x <= 33; x++)
        {
            _tileMap[x, 13] = TType.Meadow;
            _tileMap[x, 14] = TType.Meadow;
        }
        // North-bound pass to dark zone at x=28..29
        for (int y = 15; y <= 17; y++)
        {
            _tileMap[28, y] = TType.Meadow;
            _tileMap[29, y] = TType.Meadow;
        }

        // Dark zone — top-right
        for (int x = 26; x <= 35; x++)
        for (int y = 18; y <= 23; y++)
            _tileMap[x, y] = TType.Dark;
        // Entrance corridor
        _tileMap[28, 18] = TType.Dark;
        _tileMap[29, 18] = TType.Dark;

        // Road network: from castle (2,3) east along y=3..4 to bridge, across bridge,
        // east along y=4..5 to x=21, north to y=13, east along pass, north into dark zone.
        for (int x = 1; x <= 17; x++)
            if (_tileMap[x, 4] == TType.Meadow) _tileMap[x, 4] = TType.Road;
        for (int x = 1; x <= 17; x++)
            if (_tileMap[x, 5] == TType.Meadow) _tileMap[x, 5] = TType.Road;

        for (int x = 20; x <= 33; x++)
            if (_tileMap[x, 4] == TType.Meadow) _tileMap[x, 4] = TType.Road;
        for (int x = 20; x <= 33; x++)
            if (_tileMap[x, 5] == TType.Meadow) _tileMap[x, 5] = TType.Road;

        for (int y = 6; y <= 13; y++)
            if (_tileMap[21, y] == TType.Meadow) _tileMap[21, y] = TType.Road;

        for (int x = 22; x <= 33; x++)
        {
            if (_tileMap[x, 13] == TType.Meadow) _tileMap[x, 13] = TType.Road;
        }
        for (int y = 15; y <= 17; y++)
        {
            _tileMap[28, y] = TType.Road;
            _tileMap[29, y] = TType.Road;
        }
    }

    // ─── Build terrain ────────────────────────────────────────────────────────
    private static bool BuildTerrain(GameObject parent)
    {
        try
        {
            for (int x = 0; x < MAP_W; x++)
            for (int y = 0; y < MAP_H; y++)
            {
                TType t = _tileMap[x, y];
                Sprite sp = PickSprite(t, x, y);
                if (sp == null)
                {
                    Debug.LogError("[TheHeroMapBuild] No sprite for (" + x + "," + y + ") type=" + t);
                    return false;
                }
                CreateTileGO(parent, x, y, t, sp);

                switch (t)
                {
                    case TType.Meadow:       _grassTilesPlaced++; break;
                    case TType.Road:         _roadTilesPlaced++;  break;
                    case TType.Forest:
                    case TType.DenseForest:  _forestTilesPlaced++; break;
                    case TType.River:        _riverTilesPlaced++;  break;
                    case TType.Bridge:       _bridgeTilesPlaced++; break;
                    case TType.Mountain:     _mountainTilesPlaced++; break;
                    case TType.Dark:         _darkTilesPlaced++;   break;
                }
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[TheHeroMapBuild] BuildTerrain error: " + ex.Message + "\n" + ex.StackTrace);
            return false;
        }
    }

    private static Sprite PickSprite(TType t, int x, int y)
    {
        switch (t)
        {
            case TType.Meadow:
                return Pick(_bucket[Cat.Grass].Count > 0 ? _bucket[Cat.Grass] : _bucket[Cat.GrassDry], x, y);

            case TType.Road:
                // Prefer straight segments; only use crossroads/T at junctions when available
                if (_bucket[Cat.Road].Count > 0)      return Pick(_bucket[Cat.Road], x, y);
                if (_bucket[Cat.RoadTurn].Count > 0)  return Pick(_bucket[Cat.RoadTurn], x, y);
                if (_bucket[Cat.RoadT].Count > 0)     return Pick(_bucket[Cat.RoadT], x, y);
                if (_bucket[Cat.RoadCross].Count > 0) return Pick(_bucket[Cat.RoadCross], x, y);
                return Pick(_bucket[Cat.Grass], x, y);

            case TType.Forest:
            {
                bool edge = IsBoundary(x, y, IsForestLike);
                if (edge && _bucket[Cat.ForestEdge].Count > 0) return Pick(_bucket[Cat.ForestEdge], x, y);
                return Pick(_bucket[Cat.Forest], x, y);
            }
            case TType.DenseForest:
                return Pick(_bucket[Cat.Forest], x, y);

            case TType.River:
            {
                bool edge = IsBoundary(x, y, t2 => t2 == TType.River || t2 == TType.Bridge);
                if (edge && _bucket[Cat.RiverEdge].Count > 0) return Pick(_bucket[Cat.RiverEdge], x, y);
                return Pick(_bucket[Cat.River], x, y);
            }
            case TType.Bridge:
                if (_bucket[Cat.RiverBridge].Count > 0) return Pick(_bucket[Cat.RiverBridge], x, y);
                return Pick(_bucket[Cat.Road].Count > 0 ? _bucket[Cat.Road] : _bucket[Cat.Grass], x, y);

            case TType.Mountain:
            {
                bool edge = IsBoundary(x, y, t2 => t2 == TType.Mountain);
                if (edge && _bucket[Cat.MountainEdge].Count > 0) return Pick(_bucket[Cat.MountainEdge], x, y);
                return Pick(_bucket[Cat.Mountain], x, y);
            }
            case TType.Dark:
            {
                bool edge = IsBoundary(x, y, t2 => t2 == TType.Dark);
                if (edge && _bucket[Cat.DarkEdge].Count > 0) return Pick(_bucket[Cat.DarkEdge], x, y);
                return Pick(_bucket[Cat.Dark], x, y);
            }
        }
        return Pick(_bucket[Cat.Grass], x, y);
    }

    private static Sprite Pick(List<Asset> pool, int x, int y)
    {
        if (pool == null || pool.Count == 0) return null;
        int idx = Mathf.Abs(x * 31 + y * 17 + SEED) % pool.Count;
        return pool[idx].Sprite;
    }

    private static bool IsForestLike(TType t) => t == TType.Forest || t == TType.DenseForest;

    private static bool IsBoundary(int x, int y, Func<TType, bool> belongs)
    {
        TType self = _tileMap[x, y];
        if (!belongs(self)) return false;
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i], ny = y + dy[i];
            if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) return true;
            if (!belongs(_tileMap[nx, ny])) return true;
        }
        return false;
    }

    private static void CreateTileGO(GameObject parent, int x, int y, TType t, Sprite sp)
    {
        var go = new GameObject("Tile_" + x + "_" + y);
        go.transform.parent = parent.transform;

        float wx = x - MAP_W / 2f + 0.5f;
        float wy = y - MAP_H / 2f + 0.5f;
        go.transform.position = new Vector3(wx, wy, 0f);
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 0;

        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;
        bc.isTrigger = false;

        var tile = go.AddComponent<THTile>();
        tile.Setup(x, y, TileTypeToString(t));
    }

    private static string TileTypeToString(TType t)
    {
        switch (t)
        {
            case TType.Meadow:      return "grass";
            case TType.Road:        return "road";
            case TType.Forest:      return "forest";
            case TType.DenseForest: return "forest_dense";
            case TType.River:       return "river";
            case TType.Bridge:      return "bridge";
            case TType.Mountain:    return "mountain";
            case TType.Dark:        return "darkland";
            default:                return "grass";
        }
    }

    // ─── Place objects ────────────────────────────────────────────────────────
    private struct ObjDef
    {
        public int X, Y;
        public string Kind;     // castle/hero/resource/enemy/boss/artifact/mine/chest
        public string Sub;      // gold/wood/stone/mana/weak/medium/strong
        public string Label;
    }

    private static readonly ObjDef[] OBJECTS =
    {
        // Start zone
        new ObjDef { X=2,  Y=3,  Kind="castle",   Sub="player",  Label="Castle_Player" },
        new ObjDef { X=4,  Y=3,  Kind="hero",     Sub="hero",    Label="Hero" },

        // Start resources
        new ObjDef { X=6,  Y=2,  Kind="resource", Sub="gold",    Label="Gold_Start" },
        new ObjDef { X=7,  Y=6,  Kind="resource", Sub="wood",    Label="Wood_Start" },
        new ObjDef { X=2,  Y=7,  Kind="resource", Sub="stone",   Label="Stone_Start" },

        // Weak start enemies
        new ObjDef { X=8,  Y=3,  Kind="enemy",    Sub="weak",    Label="Enemy_Wolf_Start" },
        new ObjDef { X=10, Y=8,  Kind="enemy",    Sub="weak",    Label="Enemy_Goblin_Start" },

        // Forest interior
        new ObjDef { X=12, Y=14, Kind="chest",    Sub="chest",   Label="Chest_Forest" },
        new ObjDef { X=10, Y=12, Kind="resource", Sub="wood",    Label="Wood_Forest" },
        new ObjDef { X=13, Y=11, Kind="enemy",    Sub="medium",  Label="Enemy_Skeleton_Forest1" },
        new ObjDef { X=14, Y=16, Kind="enemy",    Sub="medium",  Label="Enemy_Skeleton_Forest2" },
        new ObjDef { X=11, Y=17, Kind="enemy",    Sub="medium",  Label="Enemy_Skeleton_Forest3" },

        // Mountain pass guard + mine
        new ObjDef { X=25, Y=13, Kind="mine",     Sub="stone",   Label="Mine_Mountain" },
        new ObjDef { X=23, Y=14, Kind="resource", Sub="stone",   Label="Stone_Mountain" },
        new ObjDef { X=27, Y=14, Kind="enemy",    Sub="strong",  Label="Enemy_Orc_Mountain" },

        // Dark zone
        new ObjDef { X=29, Y=20, Kind="resource", Sub="mana",    Label="Mana_Dark" },
        new ObjDef { X=31, Y=21, Kind="enemy",    Sub="strong",  Label="Enemy_Orc_Dark1" },
        new ObjDef { X=33, Y=22, Kind="enemy",    Sub="strong",  Label="Enemy_Orc_Dark2" },
        new ObjDef { X=27, Y=22, Kind="artifact", Sub="artifact",Label="Artifact_DarkRelic" },
        new ObjDef { X=32, Y=20, Kind="boss",     Sub="darklord",Label="Enemy_DarkLord_Final" },
    };

    private static void PlaceMapObjects(GameObject root)
    {
        foreach (var def in OBJECTS)
        {
            int px = def.X, py = def.Y;

            // For walking objects ensure walkable cell (resources / objects can sit on walkable terrain too)
            if (!IsWalkable(_tileMap[px, py]))
            {
                if (!TryFindNearestWalkable(ref px, ref py, 4))
                {
                    Debug.LogWarning("[TheHeroMapBuild] No walkable cell near " + def.Label + " @(" + def.X + "," + def.Y + ")");
                    continue;
                }
            }

            float wx = px - MAP_W / 2f + 0.5f;
            float wy = py - MAP_H / 2f + 0.5f;

            var go = new GameObject(def.Label);
            go.transform.parent = root.transform;
            go.transform.position = new Vector3(wx, wy, -0.1f);
            go.transform.localScale = Vector3.one;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 5;
            sr.sprite = SpriteForObject(def);

            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one * 0.8f;
            bc.isTrigger = true;

            THMapObject mapObj = null;
            if (def.Kind != "hero")
            {
                mapObj = go.AddComponent<THMapObject>();
                mapObj.id = def.Label;
                mapObj.targetX = px;
                mapObj.targetY = py;
                mapObj.displayName = DisplayNameForObject(def);
                mapObj.blocksMovement = false;
                mapObj.startsCombat = false;
            }

            switch (def.Kind)
            {
                case "hero":
                {
                    var h = go.AddComponent<THHero>();
                    h.heroName = "Knight";
                    break;
                }
                case "castle":
                {
                    var c = go.AddComponent<THCastle>();
                    c.castleName = "Castle";
                    c.isPlayerCastle = true;
                    mapObj.type = THMapObject.ObjectType.Base;
                    mapObj.blocksMovement = false;
                    break;
                }
                case "resource":
                {
                    var r = go.AddComponent<THResource>();
                    r.resourceType = def.Sub;
                    r.amount = ResourceAmount(def.Sub);
                    ConfigureResourceMapObject(mapObj, def.Sub, r.amount);
                    break;
                }
                case "mine":
                {
                    var r = go.AddComponent<THResource>();
                    r.resourceType = "stone";
                    r.amount = THBalanceConfig.StonePileSmallReward * 4;
                    mapObj.type = THMapObject.ObjectType.Mine;
                    mapObj.rewardStone = r.amount;
                    mapObj.blocksMovement = true;
                    break;
                }
                case "chest":
                {
                    var r = go.AddComponent<THResource>();
                    r.resourceType = "chest";
                    r.amount = THBalanceConfig.ChestGoldReward;
                    mapObj.type = THMapObject.ObjectType.Treasure;
                    mapObj.rewardGold = THBalanceConfig.ChestGoldReward;
                    mapObj.rewardExp = THBalanceConfig.ChestExpReward;
                    mapObj.blocksMovement = true;
                    break;
                }
                case "enemy":
                {
                    var e = go.AddComponent<THEnemy>();
                    e.enemyType = def.Sub;
                    e.startsCombat = true;
                    e.blocksMovement = true;
                    e.isFinalBoss = false;
                    e.displayName = HumanEnemyName(def.Label, def.Sub);
                    mapObj.type = THMapObject.ObjectType.Enemy;
                    mapObj.difficulty = DifficultyForEnemy(def.Sub);
                    mapObj.startsCombat = true;
                    mapObj.blocksMovement = true;
                    mapObj.isFinalBoss = false;
                    mapObj.displayName = e.displayName;
                    break;
                }
                case "boss":
                {
                    var e = go.AddComponent<THEnemy>();
                    e.enemyType = "boss";
                    e.startsCombat = true;
                    e.blocksMovement = true;
                    e.isFinalBoss = true;
                    e.displayName = "Тёмный Лорд";
                    mapObj.type = THMapObject.ObjectType.Enemy;
                    mapObj.difficulty = THEnemyDifficulty.Deadly;
                    mapObj.startsCombat = true;
                    mapObj.blocksMovement = true;
                    mapObj.isFinalBoss = true;
                    mapObj.isDarkLord = true;
                    mapObj.displayName = e.displayName;
                    break;
                }
                case "artifact":
                {
                    var a = go.AddComponent<THArtifact>();
                    a.artifactName = "Ancient Artifact";
                    a.collected = false;
                    mapObj.type = THMapObject.ObjectType.Artifact;
                    mapObj.blocksMovement = true;
                    mapObj.displayName = a.artifactName;
                    break;
                }
            }
        }
    }

    private static string DisplayNameForObject(ObjDef def)
    {
        switch (def.Kind)
        {
            case "castle":   return "Castle";
            case "resource": return ResourceDisplayName(def.Sub);
            case "mine":     return "Stone Mine";
            case "chest":    return "Forest Chest";
            case "enemy":    return HumanEnemyName(def.Label, def.Sub);
            case "boss":     return "Тёмный Лорд";
            case "artifact": return "Ancient Artifact";
            default:         return def.Label;
        }
    }

    private static string ResourceDisplayName(string sub)
    {
        switch (sub)
        {
            case "gold":  return "Gold";
            case "wood":  return "Wood";
            case "stone": return "Stone";
            case "mana":  return "Mana Crystal";
            default:      return sub;
        }
    }

    private static void ConfigureResourceMapObject(THMapObject mapObj, string sub, int amount)
    {
        switch (sub)
        {
            case "gold":
                mapObj.type = THMapObject.ObjectType.GoldResource;
                mapObj.rewardGold = amount;
                break;
            case "wood":
                mapObj.type = THMapObject.ObjectType.WoodResource;
                mapObj.rewardWood = amount;
                break;
            case "stone":
                mapObj.type = THMapObject.ObjectType.StoneResource;
                mapObj.rewardStone = amount;
                break;
            case "mana":
                mapObj.type = THMapObject.ObjectType.ManaResource;
                mapObj.rewardMana = amount;
                break;
            default:
                mapObj.type = THMapObject.ObjectType.Treasure;
                mapObj.rewardGold = amount;
                break;
        }
    }

    private static THEnemyDifficulty DifficultyForEnemy(string sub)
    {
        switch (sub)
        {
            case "medium": return THEnemyDifficulty.Medium;
            case "strong": return THEnemyDifficulty.Strong;
            case "boss":   return THEnemyDifficulty.Deadly;
            default:       return THEnemyDifficulty.Weak;
        }
    }

    private static Sprite SpriteForObject(ObjDef def)
    {
        switch (def.Kind)
        {
            case "hero":     return PickFirst(Cat.Hero);
            case "castle":   return PickFirst(Cat.Castle);
            case "chest":    return PickFirst(Cat.Chest);
            case "artifact": return PickFirst(Cat.Artifact);
            case "mine":     return PickFirst(Cat.Mine, Cat.Stone);
            case "boss":     return PickFirst(Cat.EnemyBoss);
            case "resource":
                switch (def.Sub)
                {
                    case "gold":  return PickFirst(Cat.Gold);
                    case "wood":  return PickFirst(Cat.Wood);
                    case "stone": return PickFirst(Cat.Stone);
                    case "mana":  return PickFirst(Cat.Mana);
                }
                break;
            case "enemy":
                switch (def.Sub)
                {
                    case "weak":   return PickFirst(Cat.EnemyWeak,   Cat.EnemyMedium);
                    case "medium": return PickFirst(Cat.EnemyMedium, Cat.EnemyWeak);
                    case "strong": return PickFirst(Cat.EnemyStrong, Cat.EnemyMedium);
                }
                break;
        }
        return null;
    }

    private static Sprite PickFirst(params Cat[] cats)
    {
        foreach (var c in cats)
            if (_bucket[c].Count > 0) return _bucket[c][0].Sprite;
        return null;
    }

    private static string HumanEnemyName(string label, string diff)
    {
        if (label.Contains("Wolf"))      return "Дикий волк";
        if (label.Contains("Goblin"))    return "Гоблин-разбойник";
        if (label.Contains("Skeleton"))  return "Скелет";
        if (label.Contains("Orc"))       return "Орк-страж";
        return label;
    }

    private static int ResourceAmount(string sub)
    {
        switch (sub)
        {
            case "gold":  return THBalanceConfig.GoldPileSmallReward;
            case "wood":  return THBalanceConfig.WoodPileSmallReward;
            case "stone": return THBalanceConfig.StonePileSmallReward;
            case "mana":  return THBalanceConfig.ManaCrystalReward;
            case "chest": return THBalanceConfig.ChestGoldReward;
            default:      return 50;
        }
    }

    private static bool IsWalkable(TType t)
    {
        return t == TType.Meadow || t == TType.Road || t == TType.Forest
            || t == TType.DenseForest || t == TType.Bridge || t == TType.Dark;
    }

    private static bool TryFindNearestWalkable(ref int x, ref int y, int radius)
    {
        for (int r = 1; r <= radius; r++)
        {
            for (int dx = -r; dx <= r; dx++)
            for (int dy = -r; dy <= r; dy++)
            {
                int nx = x + dx, ny = y + dy;
                if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) continue;
                if (IsWalkable(_tileMap[nx, ny])) { x = nx; y = ny; return true; }
            }
        }
        return false;
    }

    // ─── Clear ────────────────────────────────────────────────────────────────
    private static void ClearOldMapContent()
    {
        var scene = SceneManager.GetActiveScene();
        var roots = scene.GetRootGameObjects();
        var del = new List<GameObject>();
        foreach (var go in roots)
        {
            string n = go.name;
            if (n == "Tiles" || n == "MapObjects" || n == "MapTiles" || n == "Map" ||
                n == "MapRoot" || n == "TileMap" || n == "WorldMap" || n.StartsWith("Tile_"))
                del.Add(go);
        }
        foreach (var go in del) UnityEngine.Object.DestroyImmediate(go);
    }

    // ─── BFS path validation ──────────────────────────────────────────────────
    private static bool ValidatePaths(out string report)
    {
        var sb = new StringBuilder();
        int heroX = 4, heroY = 3;
        var targets = new (int x, int y, string n)[]
        {
            (2, 3,  "Castle"),
            (6, 2,  "Gold_Start"),
            (8, 3,  "Enemy_Weak_Start"),
            (12,14, "Forest_Chest"),
            (18, 4, "Bridge"),
            (25,13, "Mountain_Pass"),
            (32,20, "DarkLord")
        };
        bool ok = true;
        foreach (var t in targets)
        {
            bool reachable = BFS(heroX, heroY, t.x, t.y);
            sb.AppendLine("- Hero → " + t.n + " @(" + t.x + "," + t.y + "): " + (reachable ? "OK" : "FAIL"));
            if (!reachable) ok = false;
        }
        report = sb.ToString();
        return ok;
    }

    private static bool BFS(int sx, int sy, int tx, int ty)
    {
        if (sx == tx && sy == ty) return true;
        var visited = new bool[MAP_W, MAP_H];
        var q = new Queue<(int x, int y)>();
        q.Enqueue((sx, sy));
        visited[sx, sy] = true;
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };
        while (q.Count > 0)
        {
            var (cx, cy) = q.Dequeue();
            if (cx == tx && cy == ty) return true;
            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i], ny = cy + dy[i];
                if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) continue;
                if (visited[nx, ny]) continue;
                if (!IsWalkable(_tileMap[nx, ny])) continue;
                visited[nx, ny] = true;
                q.Enqueue((nx, ny));
            }
        }
        return false;
    }

    // ─── Reports ──────────────────────────────────────────────────────────────
    private static void Log(string s) { _log.AppendLine(s); Debug.Log(s); }

    private static void FailAndReport(string reason)
    {
        Debug.LogError("[TheHeroMapBuild] BUILD FAILED: " + reason);
        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# Map Build From Today Generated Assets — FAILED");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## Reason");
        sb.AppendLine(reason);
        sb.AppendLine();
        sb.AppendLine("## Build Log");
        sb.AppendLine("```");
        sb.AppendLine(_log.ToString());
        sb.AppendLine("```");
        sb.AppendLine();
        sb.AppendLine("## Rollback");
        sb.AppendLine("- Backup at: " + BACKUP_PATH);
        sb.AppendLine("- Map.unity restored from backup if reached.");
        File.WriteAllText(Path.Combine(dir, "MapBuild_FromTodayAssets_FAILED.md"), sb.ToString());
        AssetDatabase.Refresh();
    }

    private static void WriteSuccessReport()
    {
        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# Map Build From Today Generated Assets — REPORT");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## 1. Date of search");
        sb.AppendLine("- " + DateTime.Now.Date.ToString("yyyy-MM-dd"));
        sb.AppendLine();
        sb.AppendLine("## 2. Source folder");
        sb.AppendLine("- " + Path.GetFullPath(Path.Combine(Application.dataPath, GEN_ROOT_REL)));
        sb.AppendLine();
        sb.AppendLine("## 3. JSON manifests");
        int totalAssets = _bucket.Sum(kv => kv.Value.Count);
        sb.AppendLine("- Total PNG+JSON pairs classified: " + totalAssets);
        sb.AppendLine();
        sb.AppendLine("## 4. Asset catalog (category → count)");
        sb.AppendLine("| Category | Count |");
        sb.AppendLine("|---|---|");
        foreach (var kv in _bucket.OrderBy(k => k.Key.ToString()))
            sb.AppendLine("| " + kv.Key + " | " + kv.Value.Count + " |");
        sb.AppendLine();
        sb.AppendLine("## 5. Selected assets per role");
        AppendUsed(sb, "Grass",     Cat.Grass);
        AppendUsed(sb, "Grass dry", Cat.GrassDry);
        AppendUsed(sb, "Road",      Cat.Road);
        AppendUsed(sb, "Bridge",    Cat.RiverBridge);
        AppendUsed(sb, "Forest",    Cat.Forest);
        AppendUsed(sb, "Forest edge", Cat.ForestEdge);
        AppendUsed(sb, "Mountain",  Cat.Mountain);
        AppendUsed(sb, "Mountain edge", Cat.MountainEdge);
        AppendUsed(sb, "River center", Cat.River);
        AppendUsed(sb, "River edge", Cat.RiverEdge);
        AppendUsed(sb, "Darkland",  Cat.Dark);
        AppendUsed(sb, "Darkland edge", Cat.DarkEdge);
        AppendUsed(sb, "Castle",    Cat.Castle);
        AppendUsed(sb, "Hero icon", Cat.Hero);
        AppendUsed(sb, "Chest",     Cat.Chest);
        AppendUsed(sb, "Artifact",  Cat.Artifact);
        AppendUsed(sb, "Mine",      Cat.Mine);
        AppendUsed(sb, "Gold",      Cat.Gold);
        AppendUsed(sb, "Wood",      Cat.Wood);
        AppendUsed(sb, "Stone",     Cat.Stone);
        AppendUsed(sb, "Mana",      Cat.Mana);
        AppendUsed(sb, "Enemy weak",   Cat.EnemyWeak);
        AppendUsed(sb, "Enemy medium", Cat.EnemyMedium);
        AppendUsed(sb, "Enemy strong", Cat.EnemyStrong);
        AppendUsed(sb, "Dark Lord (boss)", Cat.EnemyBoss);
        sb.AppendLine();
        sb.AppendLine("## 6. Key placements");
        sb.AppendLine("- Castle:   (2, 3)");
        sb.AppendLine("- Hero:     (4, 3)");
        sb.AppendLine("- Bridge:   (18, 4)..(19, 5)");
        sb.AppendLine("- Pass:     y=13..14, x=20..33");
        sb.AppendLine("- DarkLord: (32, 20)");
        sb.AppendLine();
        sb.AppendLine("## 7. Tile counts");
        sb.AppendLine("- Meadow/Grass: " + _grassTilesPlaced);
        sb.AppendLine("- Road:         " + _roadTilesPlaced);
        sb.AppendLine("- Forest:       " + _forestTilesPlaced);
        sb.AppendLine("- River:        " + _riverTilesPlaced);
        sb.AppendLine("- Bridge:       " + _bridgeTilesPlaced);
        sb.AppendLine("- Mountain:     " + _mountainTilesPlaced);
        sb.AppendLine("- Darkland:     " + _darkTilesPlaced);
        sb.AppendLine();
        sb.AppendLine("## 8. Objects placed");
        sb.AppendLine("- Total: " + OBJECTS.Length);
        sb.AppendLine("- Enemies: " + OBJECTS.Count(o => o.Kind == "enemy") + " + 1 final boss");
        sb.AppendLine("- Resources: " + OBJECTS.Count(o => o.Kind == "resource" || o.Kind == "chest" || o.Kind == "mine"));
        sb.AppendLine("- Artifact: 1 (placeholder, no buffs)");
        sb.AppendLine();
        sb.AppendLine("## 9. Path validation");
        sb.AppendLine("- BFS from Hero@(4,3) reaches Castle, start resources, bridge, mountain pass and DarkLord.");
        sb.AppendLine();
        sb.AppendLine("## 10. Skipped / Unknown");
        sb.AppendLine("- Assets classified as Unknown were ignored (no usable category).");
        sb.AppendLine();
        sb.AppendLine("## 11. Manual verification");
        sb.AppendLine("1. Open Map.unity, ensure no pink/missing sprites.");
        sb.AppendLine("2. Enter Play mode, walk hero from castle → bridge → pass → DarkLord.");
        sb.AppendLine("3. Confirm river is not walkable; bridge is.");
        sb.AppendLine("4. Confirm DarkLord starts combat (not gold).");
        sb.AppendLine();
        sb.AppendLine("## 12. Build log");
        sb.AppendLine("```");
        sb.AppendLine(_log.ToString());
        sb.AppendLine("```");
        File.WriteAllText(Path.Combine(dir, "MapBuild_FromTodayAssets_Report.md"), sb.ToString());
    }

    private static void AppendUsed(StringBuilder sb, string label, Cat c)
    {
        if (_bucket[c].Count == 0) { sb.AppendLine("- " + label + ": (none)"); return; }
        sb.AppendLine("- " + label + ":");
        foreach (var a in _bucket[c])
            sb.AppendLine("  - `" + a.AssetPath + "`  — *" + a.ClassifyReason + "*");
    }
}
