using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheHero.Generated;

/// <summary>
/// The Hero — Build Adventure Map From Provided Assets
/// MenuItem: The Hero/Map/Build Map From Provided Assets
/// </summary>
public static class TheHeroBuildMapFromProvidedAssets
{
    // ─── Constants ────────────────────────────────────────────────────────────
    private const int MAP_W = 36;
    private const int MAP_H = 24;
    private const int SEED = 17701729;
    private const float TILE_SIZE = 1f;

    private const string SCENE_PATH = "Assets/Scenes/Map.unity";
    private const string REPORT_OK = "Assets/CodeAudit/MapBuild_FromProvidedAssets_Report.md";
    private const string REPORT_FAIL = "Assets/CodeAudit/MapBuild_Failed_Report.md";
    private const string BACKUP_PATH = "Assets/Scenes/Map_backup_before_rebuild.unity";

    // Sprite folder paths (relative to Assets)
    private static readonly string[] ASSET_FOLDERS = {
        "Assets/Sprites/Map/casual",
        "Assets/Sprites/Map/Casual2",
        "Assets/Sprites/Map/Forrest",
        "Assets/Sprites/Map/Mountain",
        "Assets/Sprites/Map/River",
        "Assets/Sprites/Map/Dark"
    };

    // ─── Tile Types ───────────────────────────────────────────────────────────
    private enum TType
    {
        Meadow, Road, Forest, DenseForest,
        River, Bridge, Mountain, Dark
    }

    private static bool IsWalkable(TType t)
    {
        return t == TType.Meadow || t == TType.Road || t == TType.Forest
            || t == TType.DenseForest || t == TType.Bridge || t == TType.Dark;
    }

    private static int MoveCost(TType t)
    {
        switch (t)
        {
            case TType.Meadow:      return 2;
            case TType.Road:        return 1;
            case TType.Bridge:      return 1;
            case TType.Forest:      return 3;
            case TType.DenseForest: return 3;
            case TType.Dark:        return 3;
            case TType.River:       return 999;
            case TType.Mountain:    return 999;
            default:                return 999;
        }
    }

    // ─── Asset Mapping ────────────────────────────────────────────────────────
    private class SpriteEntry
    {
        public string Name;
        public Sprite Sprite;
        public string Role; // center, edge, corner, transition, bridge, road, etc.
        public string Zone; // casual, casual2, forest, mountain, river, dark
    }

    private class AssetMapping
    {
        // Meadow / casual ground
        public List<SpriteEntry> MeadowCenter = new List<SpriteEntry>();
        public List<SpriteEntry> MeadowEdge   = new List<SpriteEntry>();
        public List<SpriteEntry> Road          = new List<SpriteEntry>();

        // Forest
        public List<SpriteEntry> ForestCenter  = new List<SpriteEntry>();
        public List<SpriteEntry> ForestEdge    = new List<SpriteEntry>();
        public List<SpriteEntry> ForestCorner  = new List<SpriteEntry>();

        // River
        public List<SpriteEntry> RiverCenter   = new List<SpriteEntry>();
        public List<SpriteEntry> RiverEdge     = new List<SpriteEntry>();
        public List<SpriteEntry> Bridge        = new List<SpriteEntry>();

        // Mountain
        public List<SpriteEntry> MountainCenter = new List<SpriteEntry>();
        public List<SpriteEntry> MountainEdge   = new List<SpriteEntry>();

        // Dark
        public List<SpriteEntry> DarkCenter    = new List<SpriteEntry>();
        public List<SpriteEntry> DarkEdge      = new List<SpriteEntry>();

        public bool IsValid()
        {
            return MeadowCenter.Count > 0
                && ForestCenter.Count > 0
                && RiverCenter.Count > 0
                && MountainCenter.Count > 0
                && DarkCenter.Count > 0;
        }
    }

    // ─── Zone Map Layout ──────────────────────────────────────────────────────
    // Encoded as TType[y][x], built deterministically
    private static TType[,] _tileMap;   // [x, y]
    private static string _buildLog = "";

    // ─── Entry Point ──────────────────────────────────────────────────────────
    [MenuItem("The Hero/Map/Build Map From Provided Assets")]
    public static void BuildMap()
    {
        _buildLog = "";
        Log("[TheHeroMapAssets] === Build Map From Provided Assets ===");

        // 1. Backup existing scene
        if (!BackupScene())
        {
            FailAndReport("Could not backup Map.unity before rebuild.");
            return;
        }

        // 2. Scan JSON and build asset mapping
        AssetMapping mapping = ScanAndBuildMapping();
        if (mapping == null)
        {
            FailAndReport("Asset mapping failed — see console for details.");
            return;
        }
        Log("[TheHeroMapAssets] Asset mapping complete");

        // 3. Open Map scene
        UnityEngine.SceneManagement.Scene mapScene;
        try
        {
            mapScene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single);
        }
        catch (Exception ex)
        {
            FailAndReport("Cannot open Map.unity: " + ex.Message);
            return;
        }

        // 4. Build tile layout
        BuildTileLayout();
        Log("[TheHeroMapAssets] Tile layout computed");

        // 5. Clear old tiles / map objects
        ClearOldMapContent();
        Log("[TheHeroMapAssets] Map cleared");

        // 6. Create parent containers
        GameObject tilesRoot = new GameObject("Tiles");
        GameObject objectsRoot = new GameObject("MapObjects");

        // 7. Build terrain tiles
        bool terrainOk = BuildTerrain(tilesRoot, mapping);
        if (!terrainOk)
        {
            // Rollback
            EditorSceneManager.OpenScene(BACKUP_PATH, OpenSceneMode.Single);
            FailAndReport("Terrain build failed.");
            return;
        }
        Log("[TheHeroMapAssets] New adventure map built");
        Log("[TheHeroMapAssets] Zones created");
        Log("[TheHeroMapAssets] River and bridge created");
        Log("[TheHeroMapAssets] Forest created");
        Log("[TheHeroMapAssets] Mountain pass created");
        Log("[TheHeroMapAssets] Dark zone created");

        // 8. Place map objects
        PlaceMapObjects(objectsRoot, mapping);
        Log("[TheHeroMapAssets] Objects placed");

        // 9. Path validation
        bool pathOk = ValidatePaths();
        if (!pathOk)
        {
            EditorSceneManager.OpenScene(BACKUP_PATH, OpenSceneMode.Single);
            FailAndReport("Path validation FAILED — map is not traversable. Rolled back.");
            return;
        }
        Log("[TheHeroMapAssets] Path validation passed");

        // 10. Save
        EditorSceneManager.MarkSceneDirty(mapScene);
        EditorSceneManager.SaveScene(mapScene, SCENE_PATH);
        Log("[TheHeroMapAssets] Map saved");

        // 11. Write success report
        WriteSuccessReport(mapping);
        AssetDatabase.Refresh();

        Debug.Log("[TheHeroMapAssets] Build complete! See Assets/CodeAudit/MapBuild_FromProvidedAssets_Report.md");
    }

    // ─── Backup ───────────────────────────────────────────────────────────────
    private static bool BackupScene()
    {
        try
        {
            string src = Path.Combine(Application.dataPath, "../" + SCENE_PATH);
            string dst = Path.Combine(Application.dataPath, "../" + BACKUP_PATH);
            if (File.Exists(src))
            {
                File.Copy(src, dst, true);
                Log("[TheHeroMapAssets] Backup created: " + BACKUP_PATH);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[TheHeroMapAssets] Backup failed: " + ex.Message);
            return false;
        }
    }

    // ─── JSON Scanning ────────────────────────────────────────────────────────
    private static AssetMapping ScanAndBuildMapping()
    {
        Log("[TheHeroMapAssets] JSON scanned");
        var mapping = new AssetMapping();

        foreach (var folderPath in ASSET_FOLDERS)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                Debug.LogWarning("[TheHeroMapAssets] Folder not found: " + folderPath);
                continue;
            }

            string zone = DetectZone(folderPath);

            // Find JSON in folder
            string[] jsonGuids = AssetDatabase.FindAssets("t:TextAsset", new[] { folderPath });
            SimpleJsonData jsonData = null;

            foreach (var guid in jsonGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)) continue;
                TextAsset ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (ta == null) continue;
                jsonData = ParseJson(ta.text, zone);
                Log("[TheHeroMapAssets] Read JSON: " + path + " (" + (jsonData?.entries.Count ?? 0) + " entries)");
                break;
            }

            // Load all sprites from the folder
            string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
            var folderSprites = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);

            foreach (var guid in spriteGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // LoadAllAssetsAtPath handles sprite sheets
                var objs = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objs)
                {
                    if (obj is Sprite sp)
                        folderSprites[sp.name] = sp;
                }
            }

            // Also try LoadAssetAtPath for single sprites
            string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folderPath });
            foreach (var guid in pngGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var objs = AssetDatabase.LoadAllAssetsAtPath(path);
                foreach (var obj in objs)
                {
                    if (obj is Sprite sp && !folderSprites.ContainsKey(sp.name))
                        folderSprites[sp.name] = sp;
                }
            }

            Log("[TheHeroMapAssets] Zone '" + zone + "': found " + folderSprites.Count + " sprites");

            // Map sprites to roles using JSON data
            PopulateMapping(mapping, zone, folderSprites, jsonData);
        }

        if (!mapping.IsValid())
        {
            string missing = "";
            if (mapping.MeadowCenter.Count == 0)  missing += " MeadowCenter";
            if (mapping.ForestCenter.Count == 0)   missing += " ForestCenter";
            if (mapping.RiverCenter.Count == 0)    missing += " RiverCenter";
            if (mapping.MountainCenter.Count == 0) missing += " MountainCenter";
            if (mapping.DarkCenter.Count == 0)     missing += " DarkCenter";
            Debug.LogError("[TheHeroMapAssets] Mapping incomplete. Missing:" + missing);
            return null;
        }

        return mapping;
    }

    private static string DetectZone(string folderPath)
    {
        string name = Path.GetFileName(folderPath).ToLowerInvariant();
        if (name == "casual")   return "casual";
        if (name == "casual2")  return "casual2";
        if (name == "forrest")  return "forest";
        if (name == "mountain") return "mountain";
        if (name == "river")    return "river";
        if (name == "dark")     return "dark";
        return name;
    }

    // ─── Minimal JSON Parser ──────────────────────────────────────────────────
    private class JsonEntry
    {
        public string name;
        public string file;
        public string type;   // center, edge, corner, transition, bridge, road, water, bank
        public string role;   // additional role hints
    }

    private class SimpleJsonData
    {
        public List<JsonEntry> entries = new List<JsonEntry>();
    }

    private static SimpleJsonData ParseJson(string json, string zone)
    {
        var data = new SimpleJsonData();
        if (string.IsNullOrEmpty(json)) return data;

        // We parse manually since Unity's JsonUtility needs a wrapper class
        // Find all objects between { }
        // The JSON looks like:
        // { "tiles": [ { "name":"...", "file":"...", "type":"..." }, ... ] }
        // or a flat array
        // We handle both cases with a simple string scan

        // Extract all "key":"value" pairs per object
        int i = 0;
        while (i < json.Length)
        {
            int objStart = json.IndexOf('{', i);
            if (objStart < 0) break;
            int objEnd = FindMatchingBrace(json, objStart);
            if (objEnd < 0) break;

            string obj = json.Substring(objStart, objEnd - objStart + 1);
            var entry = ParseJsonObject(obj);
            if (entry != null && (!string.IsNullOrEmpty(entry.name) || !string.IsNullOrEmpty(entry.file)))
                data.entries.Add(entry);

            i = objEnd + 1;
        }

        return data;
    }

    private static int FindMatchingBrace(string s, int open)
    {
        int depth = 0;
        for (int i = open; i < s.Length; i++)
        {
            if (s[i] == '{') depth++;
            else if (s[i] == '}') { depth--; if (depth == 0) return i; }
        }
        return -1;
    }

    private static JsonEntry ParseJsonObject(string obj)
    {
        var e = new JsonEntry();
        e.name = ExtractJsonString(obj, "name");
        e.file = ExtractJsonString(obj, "file");
        e.type = ExtractJsonString(obj, "type");
        e.role = ExtractJsonString(obj, "role");
        // Also check "category", "kind", "tag"
        if (string.IsNullOrEmpty(e.type)) e.type = ExtractJsonString(obj, "category");
        if (string.IsNullOrEmpty(e.type)) e.type = ExtractJsonString(obj, "kind");
        if (string.IsNullOrEmpty(e.type)) e.type = ExtractJsonString(obj, "tag");
        return e;
    }

    private static string ExtractJsonString(string json, string key)
    {
        string pattern = "\"" + key + "\"";
        int idx = json.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        idx += pattern.Length;
        // Skip whitespace and colon
        while (idx < json.Length && (json[idx] == ' ' || json[idx] == '\t' || json[idx] == ':')) idx++;
        if (idx >= json.Length) return null;
        if (json[idx] == '"')
        {
            idx++;
            int end = json.IndexOf('"', idx);
            if (end < 0) return null;
            return json.Substring(idx, end - idx);
        }
        return null;
    }

    // ─── Populate Mapping ─────────────────────────────────────────────────────
    private static void PopulateMapping(AssetMapping mapping, string zone,
        Dictionary<string, Sprite> sprites, SimpleJsonData jsonData)
    {
        // Build role lookup from JSON
        var roleByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var roleByFile = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (jsonData != null)
        {
            foreach (var e in jsonData.entries)
            {
                string role = (e.type ?? e.role ?? "center").ToLowerInvariant();
                if (!string.IsNullOrEmpty(e.name))
                    roleByName[e.name] = role;
                if (!string.IsNullOrEmpty(e.file))
                {
                    string baseName = Path.GetFileNameWithoutExtension(e.file);
                    roleByFile[baseName] = role;
                    roleByFile[e.file] = role;
                }
            }
        }

        foreach (var kv in sprites)
        {
            string spriteName = kv.Key;
            Sprite sp = kv.Value;

            // Determine role
            string role = "center";
            if (roleByName.ContainsKey(spriteName))
                role = roleByName[spriteName];
            else if (roleByFile.ContainsKey(spriteName))
                role = roleByFile[spriteName];
            else
                role = InferRoleFromName(spriteName, zone);

            var entry = new SpriteEntry { Name = spriteName, Sprite = sp, Role = role, Zone = zone };
            AssignToMapping(mapping, entry, zone);
        }
    }

    private static string InferRoleFromName(string name, string zone)
    {
        string n = name.ToLowerInvariant();

        // Bridge
        if (n.Contains("bridge")) return "bridge";
        // Road
        if (n.Contains("road") || n.Contains("path") || n.Contains("dirt")) return "road";
        // Water
        if (n.Contains("water") || n.Contains("river") || n.Contains("sea")) return "water";
        // Bank / shore
        if (n.Contains("bank") || n.Contains("shore") || n.Contains("coast")) return "bank";
        // Edge
        if (n.Contains("edge") || n.Contains("border") || n.Contains("side")) return "edge";
        // Corner
        if (n.Contains("corner")) return "corner";
        // Transition
        if (n.Contains("trans") || n.Contains("blend")) return "transition";
        // Center / default
        if (n.Contains("center") || n.Contains("mid") || n.Contains("fill")) return "center";

        // Zone-specific defaults
        switch (zone)
        {
            case "river":   return n.Contains("grass") || n.Contains("land") ? "bank" : "water";
            default:        return "center";
        }
    }

    private static void AssignToMapping(AssetMapping m, SpriteEntry e, string zone)
    {
        string r = e.Role.ToLowerInvariant();

        switch (zone)
        {
            case "casual":
            case "casual2":
                if (r == "road" || r == "path") { m.Road.Add(e); break; }
                if (r == "edge" || r == "border") { m.MeadowEdge.Add(e); break; }
                // Everything else in casual = meadow center
                m.MeadowCenter.Add(e);
                break;

            case "forest":
                if (r == "center" || r == "fill" || r == "dense") { m.ForestCenter.Add(e); break; }
                if (r == "edge" || r == "border" || r == "side")  { m.ForestEdge.Add(e); break; }
                if (r == "corner")                                  { m.ForestCorner.Add(e); break; }
                m.ForestCenter.Add(e);
                break;

            case "river":
                if (r == "bridge") { m.Bridge.Add(e); break; }
                if (r == "bank" || r == "edge" || r == "shore" || r == "coast" || r == "transition")
                    { m.RiverEdge.Add(e); break; }
                if (r == "water" || r == "center" || r == "fill")
                    { m.RiverCenter.Add(e); break; }
                m.RiverCenter.Add(e);
                break;

            case "mountain":
                if (r == "edge" || r == "border") { m.MountainEdge.Add(e); break; }
                m.MountainCenter.Add(e);
                break;

            case "dark":
                if (r == "edge" || r == "border") { m.DarkEdge.Add(e); break; }
                m.DarkCenter.Add(e);
                break;
        }
    }

    // ─── Tile Layout ──────────────────────────────────────────────────────────
    // Zone definitions: which cells belong to which zone
    // Map: 36x24 (x=0..35, y=0..23)
    // y=0 is bottom, y=23 is top
    //
    // Layout plan:
    //  Bottom-left (x:0-12, y:0-9)   = Meadow (start zone)
    //  Center      (x:10-22, y:7-17) = Forest
    //  River       (x:18-21, y:0-23) = River (vertical strip, with bridge at y:5-6)
    //  Right-center (x:22-32, y:8-17) = Mountain
    //  Top-right   (x:24-35, y:17-23) = Dark zone
    //  Roads       = narrow corridor from start through forest to bridge to dark

    private static void BuildTileLayout()
    {
        _tileMap = new TType[MAP_W, MAP_H];

        // Fill everything with meadow first
        for (int x = 0; x < MAP_W; x++)
        for (int y = 0; y < MAP_H; y++)
            _tileMap[x, y] = TType.Meadow;

        // 1. River (vertical strip x=19..20)
        for (int x = 19; x <= 20; x++)
        for (int y = 0; y < MAP_H; y++)
            _tileMap[x, y] = TType.River;

        // 2. Bridge at y=4..5 (walkable crossing)
        for (int y = 4; y <= 5; y++)
        {
            _tileMap[19, y] = TType.Bridge;
            _tileMap[20, y] = TType.Bridge;
        }

        // 3. Forest (irregular shape, center ~x:11-18, y:10-19)
        for (int x = 11; x <= 18; x++)
        for (int y = 10; y <= 19; y++)
        {
            // Organic shape — skip corners
            if (x == 11 && y >= 18) continue;
            if (x == 18 && y <= 11) continue;
            if (x <= 12 && y >= 19) continue;
            if (x >= 17 && y <= 10) continue;
            _tileMap[x, y] = TType.Forest;
        }

        // Dense forest inner core
        for (int x = 13; x <= 16; x++)
        for (int y = 12; y <= 17; y++)
            _tileMap[x, y] = TType.DenseForest;

        // Forest passages (keep meadow lanes through forest)
        // Passage 1: x=12, y=14-15 (south passage into forest)
        _tileMap[12, 14] = TType.Meadow;
        _tileMap[12, 15] = TType.Meadow;
        // Passage 2: x=17, y=14-15 (east passage to river bridge)
        _tileMap[17, 14] = TType.Meadow;
        _tileMap[18, 14] = TType.Meadow;
        _tileMap[17, 15] = TType.Meadow;
        _tileMap[18, 15] = TType.Meadow;

        // 4. Mountains (x:21-32, y:10-20 with pass at y:14-15)
        for (int x = 21; x <= 32; x++)
        for (int y = 10; y <= 20; y++)
        {
            if (x >= 22) _tileMap[x, y] = TType.Mountain;
        }
        // Mountain pass at y=14-15 (horizontal corridor)
        for (int x = 21; x <= 32; x++)
        {
            _tileMap[x, 14] = TType.Meadow;
            _tileMap[x, 15] = TType.Meadow;
        }

        // 5. Dark zone (x:27-35, y:18-23)
        for (int x = 27; x <= 35; x++)
        for (int y = 18; y <= 23; y++)
            _tileMap[x, y] = TType.Dark;

        // Narrow pass from mountains to dark zone
        for (int y = 16; y <= 17; y++)
        {
            _tileMap[28, y] = TType.Meadow;
            _tileMap[29, y] = TType.Meadow;
        }

        // 6. Road from start zone through forest passage to bridge to mountain pass to dark
        // Road in start zone going east: y=4-5 from x=0 to x=18 (to bridge)
        for (int x = 0; x <= 18; x++)
        {
            if (_tileMap[x, 4] == TType.Meadow) _tileMap[x, 4] = TType.Road;
            if (_tileMap[x, 5] == TType.Meadow) _tileMap[x, 5] = TType.Road;
        }
        // Road after bridge going east to mountain pass
        for (int x = 21; x <= 35; x++)
        {
            if (_tileMap[x, 4] == TType.Meadow) _tileMap[x, 4] = TType.Road;
            if (_tileMap[x, 5] == TType.Meadow) _tileMap[x, 5] = TType.Road;
        }
        // Road north from bridge to mountain pass level
        for (int y = 6; y <= 14; y++)
        {
            if (_tileMap[21, y] == TType.Meadow) _tileMap[21, y] = TType.Road;
        }
        // Mountain pass road
        for (int x = 22; x <= 35; x++)
        {
            if (_tileMap[x, 14] == TType.Meadow || _tileMap[x, 14] == TType.Road)
                _tileMap[x, 14] = TType.Road;
        }
        // Road north to dark zone
        for (int y = 15; y <= 17; y++)
        {
            _tileMap[28, y] = TType.Road;
            _tileMap[29, y] = TType.Road;
        }
        // Road into dark zone
        for (int x = 27; x <= 35; x++)
            if (_tileMap[x, 18] == TType.Dark) {} // keep dark
        _tileMap[28, 18] = TType.Dark;
        _tileMap[29, 18] = TType.Dark;
    }

    // ─── Build Terrain ────────────────────────────────────────────────────────
    private static bool BuildTerrain(GameObject tilesRoot, AssetMapping mapping)
    {
        try
        {
            for (int x = 0; x < MAP_W; x++)
            for (int y = 0; y < MAP_H; y++)
            {
                TType tt = _tileMap[x, y];
                Sprite sp = PickSprite(mapping, tt, x, y);
                if (sp == null)
                {
                    // Fallback to any available meadow sprite
                    sp = mapping.MeadowCenter.Count > 0 ? mapping.MeadowCenter[0].Sprite : null;
                    if (sp == null)
                    {
                        Debug.LogError("[TheHeroMapAssets] No sprite for tile (" + x + "," + y + ") type=" + tt);
                        return false;
                    }
                }

                CreateTileGO(tilesRoot, x, y, tt, sp);
            }
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError("[TheHeroMapAssets] BuildTerrain error: " + ex.Message + "\n" + ex.StackTrace);
            return false;
        }
    }

    private static Sprite PickSprite(AssetMapping m, TType tt, int x, int y)
    {
        List<SpriteEntry> pool = null;

        switch (tt)
        {
            case TType.Road:        pool = m.Road.Count > 0 ? m.Road : m.MeadowCenter; break;
            case TType.Meadow:      pool = m.MeadowCenter; break;
            case TType.Forest:      pool = m.ForestEdge.Count > 0 ? PickForestPool(m, x, y) : m.ForestCenter; break;
            case TType.DenseForest: pool = m.ForestCenter; break;
            case TType.River:       pool = m.RiverCenter; break;
            case TType.Bridge:      pool = m.Bridge.Count > 0 ? m.Bridge : m.RiverEdge.Count > 0 ? m.RiverEdge : m.RiverCenter; break;
            case TType.Mountain:    pool = m.MountainCenter; break;
            case TType.Dark:        pool = m.DarkCenter; break;
            default:                pool = m.MeadowCenter; break;
        }

        if (pool == null || pool.Count == 0) return null;
        int idx = Mathf.Abs(x * 31 + y * 17 + SEED) % pool.Count;
        return pool[idx].Sprite;
    }

    private static List<SpriteEntry> PickForestPool(AssetMapping m, int x, int y)
    {
        // Check if on forest edge (adjacent to non-forest)
        bool isEdge = IsEdgeOf(x, y, TType.Forest) || IsEdgeOf(x, y, TType.DenseForest);
        bool isCorner = isEdge && IsCornerOf(x, y, TType.Forest);

        if (isCorner && m.ForestCorner.Count > 0) return m.ForestCorner;
        if (isEdge && m.ForestEdge.Count > 0)     return m.ForestEdge;
        return m.ForestCenter;
    }

    private static bool IsEdgeOf(int x, int y, TType t)
    {
        TType self = _tileMap[x, y];
        bool isType = self == TType.Forest || self == TType.DenseForest;
        if (!isType) return false;
        // Check 4 neighbours
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i], ny = y + dy[i];
            if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) return true;
            TType nt = _tileMap[nx, ny];
            if (nt != TType.Forest && nt != TType.DenseForest) return true;
        }
        return false;
    }

    private static bool IsCornerOf(int x, int y, TType t)
    {
        int[] dx = { 1, -1, 1, -1 };
        int[] dy = { 1, -1, -1, 1 };
        int nonForest = 0;
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i], ny = y + dy[i];
            if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) { nonForest++; continue; }
            TType nt = _tileMap[nx, ny];
            if (nt != TType.Forest && nt != TType.DenseForest) nonForest++;
        }
        return nonForest >= 2;
    }

    private static void CreateTileGO(GameObject parent, int x, int y, TType tt, Sprite sp)
    {
        var go = new GameObject("Tile_" + x + "_" + y);
        go.transform.parent = parent.transform;

        // World position
        float wx = x - MAP_W / 2f + 0.5f;
        float wy = y - MAP_H / 2f + 0.5f;
        go.transform.position = new Vector3(wx, wy, 0f);
        go.transform.localScale = Vector3.one;

        // SpriteRenderer
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingLayerName = "Default";
        sr.sortingOrder = 0;

        // BoxCollider2D
        var bc = go.AddComponent<BoxCollider2D>();
        bc.size = Vector2.one;
        bc.isTrigger = false;

        // THTile component
        var tile = go.AddComponent<TheHero.Generated.THTile>();
        tile.x = x;
        tile.y = y;
        tile.tileType = TileTypeToString(tt);
        tile.walkable = IsWalkable(tt);
        tile.moveCost = MoveCost(tt);
    }

    private static string TileTypeToString(TType tt)
    {
        switch (tt)
        {
            case TType.Meadow:      return "grass";
            case TType.Road:        return "road";
            case TType.Forest:      return "forest";
            case TType.DenseForest: return "dense_forest";
            case TType.River:       return "water";
            case TType.Bridge:      return "bridge";
            case TType.Mountain:    return "mountain";
            case TType.Dark:        return "dark";
            default:                return "grass";
        }
    }

    // ─── Place Map Objects ────────────────────────────────────────────────────
    // Object placement data: (x, y, type, name, extra)
    private struct MapObjDef
    {
        public int X, Y;
        public string ObjType; // "castle", "hero", "resource", "enemy", "boss", "artifact"
        public string SubType; // "gold","wood","stone","mana","chest" / "weak","medium","strong","boss"
        public string Label;
        public bool IsFinalBoss;
    }

    private static readonly MapObjDef[] MAP_OBJECTS = {
        // Castle
        new MapObjDef { X=2,  Y=2,  ObjType="castle",   SubType="player",  Label="Castle_Player" },
        // Hero — right of castle
        new MapObjDef { X=4,  Y=2,  ObjType="hero",     SubType="hero",    Label="Hero" },

        // Start zone resources (safe)
        new MapObjDef { X=6,  Y=1,  ObjType="resource", SubType="gold",    Label="Gold_Start" },
        new MapObjDef { X=8,  Y=3,  ObjType="resource", SubType="wood",    Label="Wood_Start" },
        new MapObjDef { X=1,  Y=6,  ObjType="resource", SubType="stone",   Label="Stone_Start" },

        // Weak enemy near start resources
        new MapObjDef { X=7,  Y=2,  ObjType="enemy",    SubType="weak",    Label="Enemy_Weak_1" },
        new MapObjDef { X=9,  Y=5,  ObjType="enemy",    SubType="weak",    Label="Enemy_Weak_2" },

        // Forest resource & medium enemies
        new MapObjDef { X=14, Y=14, ObjType="resource", SubType="chest",   Label="Chest_Forest" },
        new MapObjDef { X=13, Y=12, ObjType="enemy",    SubType="medium",  Label="Enemy_Forest_1" },
        new MapObjDef { X=15, Y=16, ObjType="enemy",    SubType="medium",  Label="Enemy_Forest_2" },
        new MapObjDef { X=12, Y=16, ObjType="enemy",    SubType="medium",  Label="Enemy_Forest_3" },

        // Mountain resources & strong guard
        new MapObjDef { X=26, Y=13, ObjType="resource", SubType="stone",   Label="Stone_Mine" },
        new MapObjDef { X=24, Y=12, ObjType="resource", SubType="gold",    Label="Gold_Mountain" },
        new MapObjDef { X=25, Y=14, ObjType="enemy",    SubType="strong",  Label="Enemy_Mountain_Guard" },

        // Dark zone
        new MapObjDef { X=30, Y=19, ObjType="resource", SubType="mana",    Label="Mana_Dark" },
        new MapObjDef { X=32, Y=20, ObjType="enemy",    SubType="strong",  Label="Enemy_Dark_Guard_1" },
        new MapObjDef { X=33, Y=21, ObjType="enemy",    SubType="strong",  Label="Enemy_Dark_Guard_2" },
        new MapObjDef { X=31, Y=22, ObjType="artifact", SubType="artifact",Label="Artifact_Placeholder" },
        new MapObjDef { X=31, Y=20, ObjType="boss",     SubType="finalboss",Label="DarkLord", IsFinalBoss=true },
    };

    private static void PlaceMapObjects(GameObject root, AssetMapping mapping)
    {
        foreach (var def in MAP_OBJECTS)
        {
            // Clamp to map bounds
            int px = Mathf.Clamp(def.X, 0, MAP_W - 1);
            int py = Mathf.Clamp(def.Y, 0, MAP_H - 1);

            // Validate placement on walkable tile
            TType tt = _tileMap[px, py];
            if (!IsWalkable(tt) && def.ObjType != "resource")
            {
                // Find nearest walkable
                bool found = false;
                for (int r = 1; r <= 3 && !found; r++)
                {
                    for (int dx = -r; dx <= r && !found; dx++)
                    for (int dy = -r; dy <= r && !found; dy++)
                    {
                        int nx = px + dx, ny = py + dy;
                        if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) continue;
                        if (IsWalkable(_tileMap[nx, ny]))
                        {
                            px = nx; py = ny; found = true;
                        }
                    }
                }
            }

            float wx = px - MAP_W / 2f + 0.5f;
            float wy = py - MAP_H / 2f + 0.5f;

            var go = new GameObject(def.Label);
            go.transform.parent = root.transform;
            go.transform.position = new Vector3(wx, wy, -0.1f);
            go.transform.localScale = Vector3.one;

            // SpriteRenderer placeholder (colored square)
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = 5;
            // No sprite assigned — objects use their own prefab sprites in play

            // BoxCollider2D (trigger for interaction)
            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one * 0.8f;
            bc.isTrigger = true;

            // Add appropriate component
            switch (def.ObjType)
            {
                case "hero":
                    var hero = go.AddComponent<TheHero.Generated.THHero>();
                    hero.heroName = "Knight";
                    break;

                case "castle":
                    var castle = go.AddComponent<TheHero.Generated.THCastle>();
                    castle.castleName = "Castle";
                    castle.isPlayerCastle = true;
                    break;

                case "resource":
                    var res = go.AddComponent<TheHero.Generated.THResource>();
                    res.resourceType = def.SubType;
                    res.amount = GetResourceAmount(def.SubType);
                    break;

                case "enemy":
                    var enemy = go.AddComponent<TheHero.Generated.THEnemy>();
                    enemy.enemyType = def.SubType;
                    enemy.startsCombat = true;
                    enemy.blocksMovement = true;
                    enemy.isFinalBoss = false;
                    break;

                case "boss":
                    var boss = go.AddComponent<TheHero.Generated.THEnemy>();
                    boss.enemyType = "boss";
                    boss.startsCombat = true;
                    boss.blocksMovement = true;
                    boss.isFinalBoss = true;
                    break;

                case "artifact":
                    var art = go.AddComponent<TheHero.Generated.THArtifact>();
                    art.artifactName = "Ancient Artifact";
                    art.collected = false;
                    break;
            }
        }
    }

    private static int GetResourceAmount(string type)
    {
        switch (type)
        {
            case "gold":  return THBalanceConfig.GoldPileSmallReward;
            case "wood":  return THBalanceConfig.WoodPileSmallReward;
            case "stone": return THBalanceConfig.StonePileSmallReward;
            case "mana":  return THBalanceConfig.ManaCrystalReward;
            case "chest": return THBalanceConfig.ChestGoldReward;
            default:      return 100;
        }
    }

    // ─── Clear Old Content ────────────────────────────────────────────────────
    private static void ClearOldMapContent()
    {
        var scene = SceneManager.GetActiveScene();
        var rootObjects = scene.GetRootGameObjects();

        var toDestroy = new List<GameObject>();
        foreach (var go in rootObjects)
        {
            string n = go.name;
            if (n == "Tiles" || n == "MapObjects" || n == "MapTiles" ||
                n.StartsWith("Tile_") || n == "Map" || n == "MapRoot" ||
                n == "TileMap" || n == "WorldMap")
            {
                toDestroy.Add(go);
            }
        }

        foreach (var go in toDestroy)
            UnityEngine.Object.DestroyImmediate(go);
    }

    // ─── Path Validation ──────────────────────────────────────────────────────
    // Simple BFS to validate key paths exist

    private static bool ValidatePaths()
    {
        // Find hero and key targets
        int heroX = 4, heroY = 2; // default from placement

        // Key destinations we must reach
        var targets = new List<(int x, int y, string name)>
        {
            (2,  2,  "Castle"),
            (6,  1,  "Gold_Start"),
            (7,  2,  "Enemy_Weak_1"),
            (14, 14, "Forest_Center"),
            (19, 4,  "Bridge"),
            (25, 14, "Mountain_Pass"),
            (31, 20, "DarkLord"),
        };

        bool allOk = true;
        foreach (var (tx, ty, name) in targets)
        {
            bool reachable = BFSReachable(heroX, heroY, tx, ty);
            if (!reachable)
            {
                Debug.LogError("[TheHeroMapAssets] Path validation FAILED: Hero(" + heroX + "," + heroY + ") -> " + name + "(" + tx + "," + ty + ") is NOT reachable");
                allOk = false;
            }
            else
            {
                Log("[TheHeroMapAssets] Path OK: Hero -> " + name);
            }
        }

        return allOk;
    }

    private static bool BFSReachable(int sx, int sy, int tx, int ty)
    {
        if (sx == tx && sy == ty) return true;

        bool[,] visited = new bool[MAP_W, MAP_H];
        var queue = new Queue<(int x, int y)>();
        queue.Enqueue((sx, sy));
        visited[sx, sy] = true;

        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            if (cx == tx && cy == ty) return true;

            for (int i = 0; i < 4; i++)
            {
                int nx = cx + dx[i], ny = cy + dy[i];
                if (nx < 0 || ny < 0 || nx >= MAP_W || ny >= MAP_H) continue;
                if (visited[nx, ny]) continue;
                if (!IsWalkable(_tileMap[nx, ny])) continue;
                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }
        return false;
    }

    // ─── Reports ──────────────────────────────────────────────────────────────
    private static void Log(string msg)
    {
        _buildLog += msg + "\n";
        Debug.Log(msg);
    }

    private static void FailAndReport(string reason)
    {
        Debug.LogError("[TheHeroMapAssets] BUILD FAILED: " + reason);

        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine("# Map Build Failed Report");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## Reason");
        sb.AppendLine(reason);
        sb.AppendLine();
        sb.AppendLine("## Build Log");
        sb.AppendLine(_buildLog);
        sb.AppendLine();
        sb.AppendLine("## What Was Attempted");
        sb.AppendLine("- Scanned asset folders: Dark, casual, Forrest, Mountain, River, Casual2");
        sb.AppendLine("- Attempted to build 36x24 adventure map");
        sb.AppendLine("- Placed zones: Meadow, Forest, River+Bridge, Mountain, Dark");
        sb.AppendLine();
        sb.AppendLine("## Files Checked");
        sb.AppendLine("- Assets/Sprites/Map/casual/");
        sb.AppendLine("- Assets/Sprites/Map/Casual2/");
        sb.AppendLine("- Assets/Sprites/Map/Forrest/");
        sb.AppendLine("- Assets/Sprites/Map/Mountain/");
        sb.AppendLine("- Assets/Sprites/Map/River/");
        sb.AppendLine("- Assets/Sprites/Map/Dark/");
        sb.AppendLine();
        sb.AppendLine("## Rollback");
        sb.AppendLine("- Map.unity was NOT saved with broken state");
        sb.AppendLine("- Backup available at: " + BACKUP_PATH);
        sb.AppendLine("- All in-memory changes discarded");
        sb.AppendLine();
        sb.AppendLine("## Next Steps");
        sb.AppendLine("1. Check that all 6 asset folders exist under Assets/Sprites/Map/");
        sb.AppendLine("2. Verify that each folder contains at least one PNG sprite loadable by Unity");
        sb.AppendLine("3. Verify that THTile, THEnemy, THResource, THCastle, THHero scripts compile without errors");
        sb.AppendLine("4. Re-run: The Hero / Map / Build Map From Provided Assets");

        File.WriteAllText(Path.Combine(dir, "MapBuild_Failed_Report.md"), sb.ToString());
        AssetDatabase.Refresh();
    }

    private static void WriteSuccessReport(AssetMapping m)
    {
        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);

        var sb = new StringBuilder();
        sb.AppendLine("# Map Build Report — From Provided Assets");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## 1. Asset Folders Found");
        sb.AppendLine("- Assets/Sprites/Map/casual     → Meadow/Road tiles");
        sb.AppendLine("- Assets/Sprites/Map/Casual2    → Meadow variants");
        sb.AppendLine("- Assets/Sprites/Map/Forrest    → Forest tiles");
        sb.AppendLine("- Assets/Sprites/Map/Mountain   → Mountain tiles");
        sb.AppendLine("- Assets/Sprites/Map/River      → River/Bridge tiles");
        sb.AppendLine("- Assets/Sprites/Map/Dark       → Dark zone tiles");
        sb.AppendLine();
        sb.AppendLine("## 2. Asset Counts");
        sb.AppendLine("| Category        | Sprites |");
        sb.AppendLine("|-----------------|--------|");
        sb.AppendLine("| Meadow Center   | " + m.MeadowCenter.Count + " |");
        sb.AppendLine("| Meadow Edge     | " + m.MeadowEdge.Count + " |");
        sb.AppendLine("| Road            | " + m.Road.Count + " |");
        sb.AppendLine("| Forest Center   | " + m.ForestCenter.Count + " |");
        sb.AppendLine("| Forest Edge     | " + m.ForestEdge.Count + " |");
        sb.AppendLine("| Forest Corner   | " + m.ForestCorner.Count + " |");
        sb.AppendLine("| River Center    | " + m.RiverCenter.Count + " |");
        sb.AppendLine("| River Edge      | " + m.RiverEdge.Count + " |");
        sb.AppendLine("| Bridge          | " + m.Bridge.Count + " |");
        sb.AppendLine("| Mountain Center | " + m.MountainCenter.Count + " |");
        sb.AppendLine("| Mountain Edge   | " + m.MountainEdge.Count + " |");
        sb.AppendLine("| Dark Center     | " + m.DarkCenter.Count + " |");
        sb.AppendLine("| Dark Edge       | " + m.DarkEdge.Count + " |");
        sb.AppendLine();
        sb.AppendLine("## 3. Map Size");
        sb.AppendLine("36 x 24 tiles");
        sb.AppendLine();
        sb.AppendLine("## 4. Zone Layout");
        sb.AppendLine("- Meadow/Start: bottom-left (x:0-10, y:0-9)");
        sb.AppendLine("- Forest: center (x:11-18, y:10-19)");
        sb.AppendLine("- River: vertical strip (x:19-20, y:0-23)");
        sb.AppendLine("- Bridge: at (x:19-20, y:4-5)");
        sb.AppendLine("- Mountain: right-center (x:22-32, y:10-20) with pass at y=14-15");
        sb.AppendLine("- Dark Zone: top-right (x:27-35, y:18-23)");
        sb.AppendLine();
        sb.AppendLine("## 5. Path Validation");
        sb.AppendLine("All paths validated via BFS:");
        sb.AppendLine("- Hero to Castle: OK");
        sb.AppendLine("- Hero to Bridge: OK");
        sb.AppendLine("- Hero to DarkLord: OK");
        sb.AppendLine();
        sb.AppendLine("## 6. Manual Verification Needed");
        sb.AppendLine("1. Open Map.unity in Unity Editor and enter Play mode");
        sb.AppendLine("2. Verify sprites display correctly (not pink/missing)");
        sb.AppendLine("3. Check that hero can move from start to bridge");
        sb.AppendLine("4. Check that water (river) is not walkable");
        sb.AppendLine("5. Check that DarkLord triggers combat on approach");

        File.WriteAllText(Path.Combine(dir, "MapBuild_FromProvidedAssets_Report.md"), sb.ToString());
    }
}
