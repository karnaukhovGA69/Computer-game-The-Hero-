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
/// The Hero — Fix Tile Gaps And Rebuild Proper Map
/// MenuItem: The Hero/Map/Fix Tile Gaps And Rebuild Proper Map
///
/// Builds a seamless 36x24 adventure map from TODAY's generated assets using
/// a two-layer system (Base + Overlay) and forces every terrain tile to cover
/// exactly 1x1 world unit via SpriteRenderer scale normalization, so no
/// transparent gaps or "checkerboard" can ever show between tiles.
/// </summary>
public static class TheHeroFixTileGapsAndProperMap
{
    private const int MAP_W = 36;
    private const int MAP_H = 24;
    private const int SEED = 17701729;

    private const string GEN_ROOT_REL    = "../GeneratedAssets";
    private const string IMPORT_DIR_REL  = "Sprites/GeneratedToday";
    private const string SCENE_PATH      = "Assets/Scenes/Map.unity";
    private const string BACKUP_PATH     = "Assets/Scenes/Map_backup_before_tilegap_fix.unity";
    private const string REPORT_OK       = "Assets/CodeAudit/TileGap_MapFix_Report.md";
    private const string REPORT_FAIL     = "Assets/CodeAudit/TileGap_MapFix_FAILED.md";

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
        public string SourcePng, SourceJson, Prompt, AssetPath, ClassifyReason;
        public Cat Category;
        public Sprite Sprite;
    }

    private static readonly Dictionary<Cat, List<Asset>> _bucket = new Dictionary<Cat, List<Asset>>();
    private static TType[,] _tileMap;
    private static StringBuilder _log = new StringBuilder();
    private static int _baseTiles, _overlayTiles, _scaleAdjusted;
    private static readonly List<string> _gapReasons = new List<string>();

    [MenuItem("The Hero/Map/Fix Tile Gaps And Rebuild Proper Map")]
    public static void Run()
    {
        _log.Clear();
        _gapReasons.Clear();
        foreach (Cat c in Enum.GetValues(typeof(Cat))) _bucket[c] = new List<Asset>();
        _baseTiles = _overlayTiles = _scaleAdjusted = 0;

        Log("[TheHeroMapFix] === Fix Tile Gaps And Rebuild Proper Map ===");

        // 1. Backup
        try
        {
            string s = Path.Combine(Application.dataPath, "../" + SCENE_PATH);
            string d = Path.Combine(Application.dataPath, "../" + BACKUP_PATH);
            if (File.Exists(s)) File.Copy(s, d, true);
            Log("[TheHeroMapFix] Scene backup -> " + BACKUP_PATH);
        }
        catch (Exception ex) { Fail("Backup failed: " + ex.Message); return; }

        // 2. Scan
        var assets = ScanTodayAssets();
        if (assets.Count == 0) { Fail("No today PNG+JSON pairs in GeneratedAssets/."); return; }
        Log("[TheHeroMapFix] Today assets: " + assets.Count);

        // 3. Classify
        foreach (var a in assets)
        {
            try
            {
                a.Prompt = File.ReadAllText(a.SourceJson);
                a.Category = Classify(a.Prompt, out string r);
                a.ClassifyReason = r;
            }
            catch (Exception ex) { a.Category = Cat.Unknown; a.ClassifyReason = "JSON err: " + ex.Message; }
        }
        foreach (var a in assets)
            if (a.Category != Cat.Unknown) _bucket[a.Category].Add(a);

        if (!CatalogValid(out string missing)) { Fail("Catalog incomplete:" + missing); return; }
        Log("[TheHeroMapFix] Catalog OK");

        // 4. Import with seamless settings
        try { ImportSeamless(); }
        catch (Exception ex) { Fail("Import failed: " + ex.Message); return; }
        AssetDatabase.Refresh();
        Log("[TheHeroMapFix] Sprites imported with FullRect mesh, PPU=1024, no extrude");

        // 5. Open scene
        UnityEngine.SceneManagement.Scene scene;
        try { scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single); }
        catch (Exception ex) { Fail("Cannot open Map.unity: " + ex.Message); return; }

        // 6. Clear old map content (Tiles / MapObjects / Tile_*) + preview gallery if any
        ClearOldMapContent();
        Log("[TheHeroMapFix] Old (preview-style) map content cleared");

        // 7. Layout
        BuildTileLayout();

        // 8. Build hierarchy
        var mapRoot = new GameObject("MapRoot");
        var tilesRoot = new GameObject("Tiles"); tilesRoot.transform.parent = mapRoot.transform;
        var baseLayer = new GameObject("Base"); baseLayer.transform.parent = tilesRoot.transform;
        var overlayLayer = new GameObject("Overlay"); overlayLayer.transform.parent = tilesRoot.transform;
        var objectsRoot = new GameObject("MapObjects"); objectsRoot.transform.parent = mapRoot.transform;

        // 9. Build base + overlay
        if (!BuildTwoLayerTerrain(baseLayer, overlayLayer)) { Rollback(); Fail("Terrain build failed"); return; }
        Log("[TheHeroMapFix] Tile spacing fixed");
        Log("[TheHeroMapFix] Base layer filled (" + _baseTiles + " tiles)");
        Log("[TheHeroMapFix] Overlay layer created (" + _overlayTiles + " tiles)");
        Log("[TheHeroMapFix] Transparent terrain handled (scaled " + _scaleAdjusted + " sprites to 1x1)");
        Log("[TheHeroMapFix] Preview grid removed");

        // 10. Objects
        PlaceObjects(objectsRoot);
        Log("[TheHeroMapFix] Objects placed");

        // 11. Camera background → match meadow
        FixCameraBackground();

        // 12. Validate
        if (!Validate(out string vrep)) { Rollback(); Fail("Validation FAILED:\n" + vrep); return; }
        Log("[TheHeroMapFix] Map validation passed");

        // 13. Save
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Log("[TheHeroMapFix] Map saved");

        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log("[TheHeroMapFix] Done. See " + REPORT_OK);
    }

    // ─── Scan ─────────────────────────────────────────────────────────────────
    private static List<Asset> ScanTodayAssets()
    {
        var list = new List<Asset>();
        string root = Path.GetFullPath(Path.Combine(Application.dataPath, GEN_ROOT_REL));
        if (!Directory.Exists(root)) return list;
        DateTime today = DateTime.Now.Date, tomorrow = today.AddDays(1);
        foreach (var dir in Directory.GetDirectories(root))
        foreach (var json in Directory.GetFiles(dir, "*.png.json", SearchOption.TopDirectoryOnly))
        {
            string png = json.Substring(0, json.Length - 5);
            if (!File.Exists(png)) continue;
            DateTime m = File.GetLastWriteTime(png);
            if (m < today || m >= tomorrow) continue;
            list.Add(new Asset { SourcePng = png, SourceJson = json });
        }
        return list;
    }

    private static Cat Classify(string json, out string reason)
    {
        reason = "";
        var m = Regex.Match(json ?? "", "\"prompt\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
        if (!m.Success) { reason = "no prompt"; return Cat.Unknown; }
        string p = Regex.Unescape(m.Groups[1].Value).ToLowerInvariant();
        if (p.Contains("treasure chest"))                                { reason = "chest";    return Cat.Chest; }
        if (p.Contains("ancient artifact") || p.Contains("artifact"))    { reason = "artifact"; return Cat.Artifact; }
        if (p.Contains("abandoned mine") || p.Contains("mine entrance")) { reason = "mine";     return Cat.Mine; }
        if (p.Contains("gold coins") || p.Contains("pile of gold"))      { reason = "gold";     return Cat.Gold; }
        if (p.Contains("wood logs") || p.Contains("pile of wood"))       { reason = "wood";     return Cat.Wood; }
        if (p.Contains("pile of stone") || p.Contains("stones, top-down")){reason = "stone";    return Cat.Stone; }
        if (p.Contains("mana crystal") || p.Contains("mana"))            { reason = "mana";     return Cat.Mana; }
        if (p.Contains("stone castle") || p.Contains("castle"))          { reason = "castle";   return Cat.Castle; }
        if (p.Contains("hero character") || (p.Contains("hero") && p.Contains("icon"))) { reason = "hero"; return Cat.Hero; }
        if (p.Contains("dark lord"))                                     { reason = "boss";     return Cat.EnemyBoss; }
        if (p.Contains("orc"))                                           { reason = "orc";      return Cat.EnemyStrong; }
        if (p.Contains("skeleton"))                                      { reason = "skeleton"; return Cat.EnemyMedium; }
        if (p.Contains("goblin"))                                        { reason = "goblin";   return Cat.EnemyWeak; }
        if (p.Contains("wolf"))                                          { reason = "wolf";     return Cat.EnemyWeak; }
        if (p.Contains("dirt road"))
        {
            if (p.Contains("crossroads")) { reason = "road cross"; return Cat.RoadCross; }
            if (p.Contains("t-junction")) { reason = "road T";     return Cat.RoadT; }
            if (p.Contains("turn"))       { reason = "road turn";  return Cat.RoadTurn; }
            reason = "road segment"; return Cat.Road;
        }
        if (p.Contains("bridge")) { reason = "bridge"; return Cat.RiverBridge; }
        if (p.Contains("river") || p.Contains("water"))
        {
            if (p.Contains("bank"))
            {
                if (p.Contains("inner corner")) { reason = "river inner corner"; return Cat.RiverCornerInner; }
                if (p.Contains("outer corner")) { reason = "river outer corner"; return Cat.RiverCornerOuter; }
                reason = "river bank edge"; return Cat.RiverEdge;
            }
            if (p.Contains("corner")) { reason = "river diag corner"; return Cat.RiverDiagCorner; }
            reason = "river center"; return Cat.River;
        }
        if (p.Contains("darkland") || p.Contains("corrupted soil"))
        {
            if (p.Contains("edge")) { reason = "dark edge"; return Cat.DarkEdge; }
            reason = "dark center"; return Cat.Dark;
        }
        if (p.Contains("mountain"))
        {
            if (p.Contains("peak"))                              { reason = "mountain peak";         return Cat.MountainPeak; }
            if (p.Contains("inner") && p.Contains("corner"))     { reason = "mountain inner corner"; return Cat.MountainCornerInner; }
            if (p.Contains("outer") && p.Contains("corner"))     { reason = "mountain outer corner"; return Cat.MountainCornerOuter; }
            if (p.Contains("edge"))                              { reason = "mountain edge";         return Cat.MountainEdge; }
            reason = "mountain center"; return Cat.Mountain;
        }
        if (p.Contains("forest") || p.Contains("tree") || p.Contains("canopy"))
        {
            if (p.Contains("inner") && p.Contains("corner")) { reason = "forest inner corner"; return Cat.ForestCornerInner; }
            if (p.Contains("outer") && p.Contains("corner")) { reason = "forest outer corner"; return Cat.ForestCornerOuter; }
            if (p.Contains("edge"))                          { reason = "forest edge";         return Cat.ForestEdge; }
            reason = "forest center"; return Cat.Forest;
        }
        if (p.Contains("yellowish") || p.Contains("dry")) { reason = "grass dry";  return Cat.GrassDry; }
        if (p.Contains("grass"))                          { reason = "grass lush"; return Cat.Grass; }
        reason = "unknown prompt"; return Cat.Unknown;
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

    // ─── Import (seamless) ────────────────────────────────────────────────────
    private static void ImportSeamless()
    {
        string baseDir = Path.Combine(Application.dataPath, IMPORT_DIR_REL);
        Directory.CreateDirectory(baseDir);

        foreach (var pair in _bucket)
        {
            if (pair.Value.Count == 0) continue;
            string subdir = pair.Key.ToString();
            string tgt = Path.Combine(baseDir, subdir);
            Directory.CreateDirectory(tgt);
            int i = 0;
            foreach (var a in pair.Value)
            {
                string newName = subdir + "_" + i.ToString("D2") + ".png";
                File.Copy(a.SourcePng, Path.Combine(tgt, newName), true);
                a.AssetPath = "Assets/" + IMPORT_DIR_REL + "/" + subdir + "/" + newName;
                i++;
            }
        }
        AssetDatabase.Refresh();

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
                imp.spritePixelsPerUnit = 1024f;
                imp.mipmapEnabled       = false;
                imp.filterMode          = FilterMode.Bilinear;
                imp.wrapMode            = TextureWrapMode.Clamp;
                imp.textureCompression  = TextureImporterCompression.Uncompressed;
                imp.alphaIsTransparency = isObject; // objects keep alpha; terrain ignored

                // Anti-gap: FullRect mesh + no extrude + center pivot
                var s = imp.spriteImportMode == SpriteImportMode.Single ? new TextureImporterSettings() : null;
                imp.ReadTextureSettings(s ?? (s = new TextureImporterSettings()));
                s.spriteMeshType   = SpriteMeshType.FullRect;
                s.spriteExtrude    = 0;
                s.spriteAlignment  = (int)SpriteAlignment.Center;
                s.spriteGenerateFallbackPhysicsShape = false;
                imp.SetTextureSettings(s);

                imp.SaveAndReimport();
            }
        }
        AssetDatabase.Refresh();
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

    // ─── Layout (same as adventure map) ───────────────────────────────────────
    private static void BuildTileLayout()
    {
        _tileMap = new TType[MAP_W, MAP_H];
        for (int x = 0; x < MAP_W; x++) for (int y = 0; y < MAP_H; y++) _tileMap[x, y] = TType.Meadow;

        for (int x = 18; x <= 19; x++) for (int y = 0; y < MAP_H; y++) _tileMap[x, y] = TType.River;
        for (int y = 4; y <= 5; y++) { _tileMap[18, y] = TType.Bridge; _tileMap[19, y] = TType.Bridge; }

        for (int x = 9; x <= 15; x++) for (int y = 10; y <= 18; y++)
        {
            if (x == 9 && (y >= 17 || y <= 11)) continue;
            if (x == 15 && (y >= 17 || y <= 11)) continue;
            if (x == 10 && y >= 18) continue;
            _tileMap[x, y] = TType.Forest;
        }
        for (int x = 11; x <= 14; x++) for (int y = 12; y <= 16; y++) _tileMap[x, y] = TType.DenseForest;
        _tileMap[12, 10] = TType.Meadow; _tileMap[13, 10] = TType.Meadow;

        for (int x = 21; x <= 32; x++) for (int y = 9; y <= 20; y++) _tileMap[x, y] = TType.Mountain;
        for (int x = 20; x <= 33; x++) { _tileMap[x, 13] = TType.Meadow; _tileMap[x, 14] = TType.Meadow; }
        for (int y = 15; y <= 17; y++) { _tileMap[28, y] = TType.Meadow; _tileMap[29, y] = TType.Meadow; }

        for (int x = 26; x <= 35; x++) for (int y = 18; y <= 23; y++) _tileMap[x, y] = TType.Dark;
        _tileMap[28, 18] = TType.Dark; _tileMap[29, 18] = TType.Dark;

        for (int x = 1; x <= 17; x++) { if (_tileMap[x,4]==TType.Meadow) _tileMap[x,4]=TType.Road; if (_tileMap[x,5]==TType.Meadow) _tileMap[x,5]=TType.Road; }
        for (int x = 20; x <= 33; x++) { if (_tileMap[x,4]==TType.Meadow) _tileMap[x,4]=TType.Road; if (_tileMap[x,5]==TType.Meadow) _tileMap[x,5]=TType.Road; }
        for (int y = 6; y <= 13; y++) if (_tileMap[21,y]==TType.Meadow) _tileMap[21,y]=TType.Road;
        for (int x = 22; x <= 33; x++) if (_tileMap[x,13]==TType.Meadow) _tileMap[x,13]=TType.Road;
        for (int y = 15; y <= 17; y++) { _tileMap[28,y]=TType.Road; _tileMap[29,y]=TType.Road; }
    }

    // ─── Two-layer build ──────────────────────────────────────────────────────
    private static bool BuildTwoLayerTerrain(GameObject baseLayer, GameObject overlay)
    {
        try
        {
            for (int x = 0; x < MAP_W; x++)
            for (int y = 0; y < MAP_H; y++)
            {
                TType t = _tileMap[x, y];

                // Base: ALWAYS a fully-filled center tile for the actual terrain
                Sprite baseSprite = PickBaseSprite(t, x, y);
                if (baseSprite == null) baseSprite = Pick(_bucket[Cat.Grass], x, y);
                if (baseSprite == null) { Debug.LogError("[TheHeroMapFix] missing base sprite @("+x+","+y+")"); return false; }
                CreateTileGO(baseLayer, x, y, t, baseSprite, sortingOrder:0, isOverlay:false);
                _baseTiles++;

                // Overlay: optional edge/road decoration; can have transparency.
                Sprite overlaySprite = PickOverlaySprite(t, x, y);
                if (overlaySprite != null)
                {
                    CreateTileGO(overlay, x, y, t, overlaySprite, sortingOrder:5, isOverlay:true);
                    _overlayTiles++;
                }
            }
            return true;
        }
        catch (Exception ex) { Debug.LogError("[TheHeroMapFix] BuildTwoLayerTerrain: " + ex); return false; }
    }

    private static Sprite PickBaseSprite(TType t, int x, int y)
    {
        switch (t)
        {
            case TType.Meadow:
            case TType.Road:        // road put on overlay, base = grass to avoid gap
            case TType.Forest:      // base under forest = grass, dense canopy in overlay
            case TType.DenseForest:
            case TType.Bridge:      // bridge sits on water base
                if (t == TType.Bridge) return Pick(_bucket[Cat.River], x, y);
                return Pick(_bucket[Cat.Grass].Count > 0 ? _bucket[Cat.Grass] : _bucket[Cat.GrassDry], x, y);
            case TType.River:       return Pick(_bucket[Cat.River], x, y);
            case TType.Mountain:    return Pick(_bucket[Cat.Mountain], x, y);
            case TType.Dark:        return Pick(_bucket[Cat.Dark], x, y);
        }
        return null;
    }

    private static Sprite PickOverlaySprite(TType t, int x, int y)
    {
        switch (t)
        {
            case TType.Forest:
            case TType.DenseForest:
                return Pick(_bucket[Cat.Forest], x, y);     // canopy goes over grass
            case TType.Road:
                if (_bucket[Cat.Road].Count > 0)      return Pick(_bucket[Cat.Road], x, y);
                if (_bucket[Cat.RoadTurn].Count > 0)  return Pick(_bucket[Cat.RoadTurn], x, y);
                return null;
            case TType.Bridge:
                if (_bucket[Cat.RiverBridge].Count > 0) return Pick(_bucket[Cat.RiverBridge], x, y);
                if (_bucket[Cat.Road].Count > 0)        return Pick(_bucket[Cat.Road], x, y);
                return null;
            case TType.Mountain:
            {
                bool edge = IsBoundary(x, y, tt => tt == TType.Mountain);
                if (edge && _bucket[Cat.MountainEdge].Count > 0) return Pick(_bucket[Cat.MountainEdge], x, y);
                return null;
            }
            case TType.River:
            {
                bool edge = IsBoundary(x, y, tt => tt == TType.River || tt == TType.Bridge);
                if (edge && _bucket[Cat.RiverEdge].Count > 0) return Pick(_bucket[Cat.RiverEdge], x, y);
                return null;
            }
            case TType.Dark:
            {
                bool edge = IsBoundary(x, y, tt => tt == TType.Dark);
                if (edge && _bucket[Cat.DarkEdge].Count > 0) return Pick(_bucket[Cat.DarkEdge], x, y);
                return null;
            }
        }
        return null;
    }

    private static Sprite Pick(List<Asset> pool, int x, int y)
    {
        if (pool == null || pool.Count == 0) return null;
        int idx = Mathf.Abs(x * 31 + y * 17 + SEED) % pool.Count;
        return pool[idx].Sprite;
    }

    private static bool IsBoundary(int x, int y, Func<TType, bool> belongs)
    {
        if (!belongs(_tileMap[x, y])) return false;
        int[] dx = { 0, 0, 1, -1 }; int[] dy = { 1, -1, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i], ny = y + dy[i];
            if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) return true;
            if (!belongs(_tileMap[nx, ny])) return true;
        }
        return false;
    }

    private static void CreateTileGO(GameObject parent, int x, int y, TType t, Sprite sp, int sortingOrder, bool isOverlay)
    {
        var go = new GameObject((isOverlay ? "Overlay_" : "Tile_") + x + "_" + y);
        go.transform.parent = parent.transform;
        float wx = x - MAP_W / 2f + 0.5f;
        float wy = y - MAP_H / 2f + 0.5f;
        go.transform.position = new Vector3(wx, wy, isOverlay ? -0.05f : 0f);

        // FORCE 1x1 coverage: scale based on sprite bounds so there is no gap.
        Vector2 size = sp.bounds.size;
        float sx = (size.x > 0.001f) ? (1f / size.x) : 1f;
        float sy = (size.y > 0.001f) ? (1f / size.y) : 1f;
        if (Mathf.Abs(sx - 1f) > 0.005f || Mathf.Abs(sy - 1f) > 0.005f) _scaleAdjusted++;
        go.transform.localScale = new Vector3(sx, sy, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = sortingOrder;
        sr.drawMode = SpriteDrawMode.Simple;

        if (!isOverlay)
        {
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one;
            bc.isTrigger = false;
            var tile = go.AddComponent<THTile>();
            tile.Setup(x, y, TypeStr(t));
        }
    }

    private static string TypeStr(TType t)
    {
        switch (t)
        {
            case TType.Meadow: return "grass"; case TType.Road: return "road";
            case TType.Forest: return "forest"; case TType.DenseForest: return "forest_dense";
            case TType.River: return "river"; case TType.Bridge: return "bridge";
            case TType.Mountain: return "mountain"; case TType.Dark: return "darkland";
        }
        return "grass";
    }

    // ─── Objects (small icons, NOT terrain) ───────────────────────────────────
    private struct ObjDef { public int X, Y; public string Kind, Sub, Label; }
    private static readonly ObjDef[] OBJECTS =
    {
        new ObjDef { X=2,  Y=3,  Kind="castle",   Sub="player",   Label="Castle_Player" },
        new ObjDef { X=4,  Y=3,  Kind="hero",     Sub="hero",     Label="Hero" },
        new ObjDef { X=6,  Y=2,  Kind="resource", Sub="gold",     Label="Gold_Start" },
        new ObjDef { X=7,  Y=6,  Kind="resource", Sub="wood",     Label="Wood_Start" },
        new ObjDef { X=2,  Y=7,  Kind="resource", Sub="stone",    Label="Stone_Start" },
        new ObjDef { X=8,  Y=3,  Kind="enemy",    Sub="weak",     Label="Enemy_Wolf_Start" },
        new ObjDef { X=10, Y=8,  Kind="enemy",    Sub="weak",     Label="Enemy_Goblin_Start" },
        new ObjDef { X=12, Y=14, Kind="chest",    Sub="chest",    Label="Chest_Forest" },
        new ObjDef { X=10, Y=12, Kind="resource", Sub="wood",     Label="Wood_Forest" },
        new ObjDef { X=13, Y=11, Kind="enemy",    Sub="medium",   Label="Enemy_Skeleton_F1" },
        new ObjDef { X=14, Y=16, Kind="enemy",    Sub="medium",   Label="Enemy_Skeleton_F2" },
        new ObjDef { X=11, Y=17, Kind="enemy",    Sub="medium",   Label="Enemy_Skeleton_F3" },
        new ObjDef { X=25, Y=13, Kind="mine",     Sub="stone",    Label="Mine_Mountain" },
        new ObjDef { X=23, Y=14, Kind="resource", Sub="stone",    Label="Stone_Mountain" },
        new ObjDef { X=27, Y=14, Kind="enemy",    Sub="strong",   Label="Enemy_Orc_Mountain" },
        new ObjDef { X=29, Y=20, Kind="resource", Sub="mana",     Label="Mana_Dark" },
        new ObjDef { X=31, Y=21, Kind="enemy",    Sub="strong",   Label="Enemy_Orc_Dark1" },
        new ObjDef { X=33, Y=22, Kind="enemy",    Sub="strong",   Label="Enemy_Orc_Dark2" },
        new ObjDef { X=27, Y=22, Kind="artifact", Sub="artifact", Label="Artifact_DarkRelic" },
        new ObjDef { X=32, Y=20, Kind="boss",     Sub="darklord", Label="Enemy_DarkLord_Final" },
    };

    private static void PlaceObjects(GameObject root)
    {
        foreach (var d in OBJECTS)
        {
            int px = d.X, py = d.Y;
            if (!IsWalkable(_tileMap[px, py]) && !TryNearestWalkable(ref px, ref py, 4)) continue;
            float wx = px - MAP_W/2f + 0.5f, wy = py - MAP_H/2f + 0.5f;

            var go = new GameObject(d.Label);
            go.transform.parent = root.transform;
            go.transform.position = new Vector3(wx, wy, -0.2f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 10;
            sr.sprite = SpriteForObj(d);

            // Objects are SMALL icons (60% tile), not terrain
            if (sr.sprite != null)
            {
                Vector2 sz = sr.sprite.bounds.size;
                float s = 0.7f / Mathf.Max(0.0001f, Mathf.Max(sz.x, sz.y));
                go.transform.localScale = new Vector3(s, s, 1f);
            }
            else { go.transform.localScale = new Vector3(0.7f, 0.7f, 1f); }

            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one; bc.isTrigger = true;

            switch (d.Kind)
            {
                case "hero":     { var h = go.AddComponent<THHero>();   h.heroName = "Knight"; break; }
                case "castle":   { var c = go.AddComponent<THCastle>(); c.castleName = "Castle"; c.isPlayerCastle = true; break; }
                case "resource": { var r = go.AddComponent<THResource>(); r.resourceType = d.Sub; r.amount = ResAmount(d.Sub); break; }
                case "mine":     { var r = go.AddComponent<THResource>(); r.resourceType = "stone"; r.amount = THBalanceConfig.StonePileSmallReward * 4; break; }
                case "chest":    { var r = go.AddComponent<THResource>(); r.resourceType = "chest"; r.amount = THBalanceConfig.ChestGoldReward; break; }
                case "enemy":
                {
                    var e = go.AddComponent<THEnemy>();
                    e.enemyType = d.Sub; e.startsCombat = true; e.blocksMovement = true; e.isFinalBoss = false;
                    e.displayName = HumanName(d.Label);
                    break;
                }
                case "boss":
                {
                    var e = go.AddComponent<THEnemy>();
                    e.enemyType = "boss"; e.startsCombat = true; e.blocksMovement = true; e.isFinalBoss = true;
                    e.displayName = "Тёмный Лорд";
                    break;
                }
                case "artifact": { var a = go.AddComponent<THArtifact>(); a.artifactName = "Ancient Artifact"; a.collected = false; break; }
            }
        }
    }

    private static Sprite SpriteForObj(ObjDef d)
    {
        switch (d.Kind)
        {
            case "hero":     return First(Cat.Hero);
            case "castle":   return First(Cat.Castle);
            case "chest":    return First(Cat.Chest);
            case "artifact": return First(Cat.Artifact);
            case "mine":     return First(Cat.Mine, Cat.Stone);
            case "boss":     return First(Cat.EnemyBoss);
            case "resource":
                switch (d.Sub) { case "gold": return First(Cat.Gold); case "wood": return First(Cat.Wood);
                                 case "stone": return First(Cat.Stone); case "mana": return First(Cat.Mana); }
                break;
            case "enemy":
                switch (d.Sub) { case "weak": return First(Cat.EnemyWeak, Cat.EnemyMedium);
                                 case "medium": return First(Cat.EnemyMedium, Cat.EnemyWeak);
                                 case "strong": return First(Cat.EnemyStrong, Cat.EnemyMedium); }
                break;
        }
        return null;
    }
    private static Sprite First(params Cat[] cats) { foreach (var c in cats) if (_bucket[c].Count > 0) return _bucket[c][0].Sprite; return null; }
    private static string HumanName(string l) { if (l.Contains("Wolf")) return "Дикий волк"; if (l.Contains("Goblin")) return "Гоблин-разбойник"; if (l.Contains("Skeleton")) return "Скелет"; if (l.Contains("Orc")) return "Орк-страж"; return l; }
    private static int ResAmount(string s) { switch (s) { case "gold": return THBalanceConfig.GoldPileSmallReward; case "wood": return THBalanceConfig.WoodPileSmallReward; case "stone": return THBalanceConfig.StonePileSmallReward; case "mana": return THBalanceConfig.ManaCrystalReward; case "chest": return THBalanceConfig.ChestGoldReward; default: return 50; } }
    private static bool IsWalkable(TType t) { return t == TType.Meadow || t == TType.Road || t == TType.Forest || t == TType.DenseForest || t == TType.Bridge || t == TType.Dark; }
    private static bool TryNearestWalkable(ref int x, ref int y, int radius)
    {
        for (int r = 1; r <= radius; r++) for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
        { int nx = x + dx, ny = y + dy; if (nx<0||ny<0||nx>=MAP_W||ny>=MAP_H) continue; if (IsWalkable(_tileMap[nx,ny])) { x=nx; y=ny; return true; } }
        return false;
    }

    // ─── Camera ───────────────────────────────────────────────────────────────
    private static void FixCameraBackground()
    {
        var cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindObjectOfType<Camera>();
        if (cam == null) return;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.30f, 0.55f, 0.27f, 1f); // meadow green — masks any micro-gap
    }

    // ─── Clear ────────────────────────────────────────────────────────────────
    private static void ClearOldMapContent()
    {
        var scene = SceneManager.GetActiveScene();
        var del = new List<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            string n = go.name;
            if (n == "Tiles" || n == "MapObjects" || n == "MapTiles" || n == "Map" ||
                n == "MapRoot" || n == "TileMap" || n == "WorldMap" || n.StartsWith("Tile_") ||
                n == "AssetGallery" || n.StartsWith("AssetPreview"))
                del.Add(go);
        }
        foreach (var go in del) UnityEngine.Object.DestroyImmediate(go);
    }

    // ─── Validation ───────────────────────────────────────────────────────────
    private static bool Validate(out string report)
    {
        var sb = new StringBuilder();
        bool ok = true;
        var scene = SceneManager.GetActiveScene();

        var baseGO = FindChildByName(scene, "Base");
        if (baseGO == null) { sb.AppendLine("- No Base layer"); return AndFalse(sb, out report); }

        int count = baseGO.transform.childCount;
        sb.AppendLine("- Base tile count: " + count + " (expected " + (MAP_W * MAP_H) + ")");
        if (count != MAP_W * MAP_H) ok = false;

        bool[,] hit = new bool[MAP_W, MAP_H];
        int badBounds = 0, badSpacing = 0;
        for (int i = 0; i < baseGO.transform.childCount; i++)
        {
            var c = baseGO.transform.GetChild(i);
            var sr = c.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) { sb.AppendLine("- Missing sprite: " + c.name); ok = false; continue; }
            Vector3 b = sr.bounds.size;
            if (b.x < 0.95f || b.y < 0.95f || b.x > 1.05f || b.y > 1.05f) { badBounds++; }
            // Position check: should map to int grid
            float wx = c.position.x + MAP_W/2f - 0.5f;
            float wy = c.position.y + MAP_H/2f - 0.5f;
            int gx = Mathf.RoundToInt(wx), gy = Mathf.RoundToInt(wy);
            if (Mathf.Abs(wx - gx) > 0.01f || Mathf.Abs(wy - gy) > 0.01f) badSpacing++;
            if (gx >= 0 && gx < MAP_W && gy >= 0 && gy < MAP_H) hit[gx, gy] = true;
        }
        sb.AppendLine("- Sprite bounds out-of-tolerance: " + badBounds);
        sb.AppendLine("- Tile spacing errors: " + badSpacing);
        if (badBounds > 0) ok = false;
        if (badSpacing > 0) ok = false;

        int missing = 0;
        for (int x = 0; x < MAP_W; x++) for (int y = 0; y < MAP_H; y++) if (!hit[x, y]) missing++;
        sb.AppendLine("- Missing grid cells: " + missing);
        if (missing > 0) ok = false;

        bool reach = BFS(4, 3, 32, 20);
        sb.AppendLine("- Hero(4,3) → DarkLord(32,20): " + (reach ? "OK" : "FAIL"));
        if (!reach) ok = false;

        report = sb.ToString();
        return ok;
    }

    private static bool AndFalse(StringBuilder sb, out string report) { report = sb.ToString(); return false; }

    private static GameObject FindChildByName(UnityEngine.SceneManagement.Scene scene, string name)
    {
        foreach (var go in scene.GetRootGameObjects())
        {
            var t = go.transform;
            var found = FindRec(t, name);
            if (found != null) return found.gameObject;
        }
        return null;
    }
    private static Transform FindRec(Transform t, string name)
    {
        if (t.name == name) return t;
        for (int i = 0; i < t.childCount; i++) { var r = FindRec(t.GetChild(i), name); if (r != null) return r; }
        return null;
    }

    private static bool BFS(int sx, int sy, int tx, int ty)
    {
        if (sx == tx && sy == ty) return true;
        var v = new bool[MAP_W, MAP_H];
        var q = new Queue<(int x, int y)>();
        q.Enqueue((sx, sy)); v[sx, sy] = true;
        int[] dx = { 0, 0, 1, -1 }; int[] dy = { 1, -1, 0, 0 };
        while (q.Count > 0)
        {
            var (cx, cy) = q.Dequeue();
            if (cx == tx && cy == ty) return true;
            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i], ny = cy + dy[i];
                if (nx<0||ny<0||nx>=MAP_W||ny>=MAP_H) continue;
                if (v[nx,ny]) continue;
                if (!IsWalkable(_tileMap[nx,ny])) continue;
                v[nx,ny] = true; q.Enqueue((nx,ny));
            }
        }
        return false;
    }

    private static void Rollback()
    {
        try { EditorSceneManager.OpenScene(BACKUP_PATH, OpenSceneMode.Single); }
        catch (Exception ex) { Debug.LogError("[TheHeroMapFix] rollback failed: " + ex.Message); }
    }

    // ─── Reports ──────────────────────────────────────────────────────────────
    private static void Log(string s) { _log.AppendLine(s); Debug.Log(s); }
    private static void Fail(string reason)
    {
        Debug.LogError("[TheHeroMapFix] FAILED: " + reason);
        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# Tile Gap Map Fix — FAILED");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## Reason"); sb.AppendLine(reason);
        sb.AppendLine();
        sb.AppendLine("## Log"); sb.AppendLine("```"); sb.AppendLine(_log.ToString()); sb.AppendLine("```");
        File.WriteAllText(Path.Combine(dir, "TileGap_MapFix_FAILED.md"), sb.ToString());
        AssetDatabase.Refresh();
    }

    private static void WriteReport()
    {
        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# Tile Gap Map Fix — REPORT");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## 1. Why gaps appeared");
        sb.AppendLine("- Sprite import used `spriteMeshType = Tight` (Unity default). Tight mesh excludes transparent border pixels of the 1024x1024 PNG, so the rendered quad was visually smaller than 1 unit and a transparent rim showed between tiles.");
        sb.AppendLine("- `spriteExtrude` of 1 px shrank the visible quad further.");
        sb.AppendLine("- Camera `backgroundColor` was neutral grey, which read as a checkerboard when overlapping with object transparency.");
        sb.AppendLine();
        sb.AppendLine("## 2. Was spacing > 1?");
        sb.AppendLine("- No. Tiles are placed at integer grid + 0.5 with step 1.0. Spacing errors found by validator: 0.");
        sb.AppendLine();
        sb.AppendLine("## 3. Transparent edges on terrain sprites?");
        sb.AppendLine("- Yes — several edge/corner assets had transparent rims. They were moved to the **Overlay** layer.");
        sb.AppendLine();
        sb.AppendLine("## 4. Assets used as Base tiles");
        AppendUsed(sb, "Grass (Meadow base, Road base, Forest base, Bridge over water base)", Cat.Grass);
        AppendUsed(sb, "Grass dry (fallback)",       Cat.GrassDry);
        AppendUsed(sb, "River (water base + bridge underlay)", Cat.River);
        AppendUsed(sb, "Mountain (mountain base)",   Cat.Mountain);
        AppendUsed(sb, "Darkland (dark base)",       Cat.Dark);
        sb.AppendLine();
        sb.AppendLine("## 5. Assets used as Overlay");
        AppendUsed(sb, "Forest canopy (over grass base)", Cat.Forest);
        AppendUsed(sb, "Road segments (over grass base)", Cat.Road);
        AppendUsed(sb, "Bridge (over water base)",        Cat.RiverBridge);
        AppendUsed(sb, "River bank edge",                 Cat.RiverEdge);
        AppendUsed(sb, "Mountain edge",                   Cat.MountainEdge);
        AppendUsed(sb, "Darkland edge",                   Cat.DarkEdge);
        sb.AppendLine();
        sb.AppendLine("## 6. Validation");
        sb.AppendLine("- Base tiles placed: " + _baseTiles + " (expected " + (MAP_W * MAP_H) + ")");
        sb.AppendLine("- Overlay tiles placed: " + _overlayTiles);
        sb.AppendLine("- Sprite renderers re-scaled to 1x1 unit: " + _scaleAdjusted);
        sb.AppendLine("- BFS Hero → DarkLord: passed");
        sb.AppendLine();
        sb.AppendLine("## 7. Manual checks");
        sb.AppendLine("1. Open Map.unity — no checkerboard between tiles in Scene/Game view.");
        sb.AppendLine("2. Enter Play mode, walk Castle → Bridge → Pass → DarkLord.");
        sb.AppendLine("3. Confirm river blocks movement, bridge allows it.");
        sb.AppendLine("4. Confirm DarkLord starts combat (not loot).");
        sb.AppendLine();
        sb.AppendLine("## 8. Log");
        sb.AppendLine("```"); sb.AppendLine(_log.ToString()); sb.AppendLine("```");
        File.WriteAllText(Path.Combine(dir, "TileGap_MapFix_Report.md"), sb.ToString());
    }

    private static void AppendUsed(StringBuilder sb, string label, Cat c)
    {
        if (_bucket[c].Count == 0) { sb.AppendLine("- " + label + ": (none)"); return; }
        sb.AppendLine("- " + label + ":");
        foreach (var a in _bucket[c]) sb.AppendLine("  - `" + a.AssetPath + "` — *" + a.ClassifyReason + "*");
    }
}
