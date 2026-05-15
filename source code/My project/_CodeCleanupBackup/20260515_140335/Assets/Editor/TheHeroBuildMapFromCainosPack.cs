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
/// The Hero — Build Map From Cainos Pack
/// MenuItem: The Hero/Map/Build Map From Cainos Pack
///
/// Uses the "Pixel Art Top Down - Basic" pack by Cainos (already imported under
/// Assets/Cainos/). Builds a clean 36x24 adventure map with seamless 1x1
/// terrain tiles. Pack has no water/darkland tiles, so we emulate them via
/// SpriteRenderer.color tinting on grass / stone-ground sprites (no new
/// assets created).
/// </summary>
public static class TheHeroBuildMapFromCainosPack
{
    private const int MAP_W = 36;
    private const int MAP_H = 24;
    private const int SEED = 17701729;

    private const string SCENE_PATH  = "Assets/Scenes/Map.unity";
    private const string BACKUP_PATH = "Assets/Scenes/Map_backup_before_cainos.unity";
    private const string REPORT_OK   = "Assets/CodeAudit/Cainos_MapBuild_Report.md";
    private const string REPORT_FAIL = "Assets/CodeAudit/Cainos_MapBuild_FAILED.md";

    private const string PACK_ROOT_HINT_A = "Assets/Cainos";
    private const string PACK_ROOT_HINT_B = "Pixel Art Top Down - Basic";

    private enum TType { Meadow, Road, Forest, DenseForest, River, Bridge, Mountain, Dark }

    private static readonly Color RIVER_TINT  = new Color(0.25f, 0.50f, 0.85f, 1f);
    private static readonly Color BRIDGE_TINT = new Color(0.70f, 0.55f, 0.35f, 1f);
    private static readonly Color DARK_TINT   = new Color(0.45f, 0.30f, 0.55f, 1f);
    private static readonly Color SHADOW_FOREST = new Color(0.70f, 1.00f, 0.70f, 1f);

    // ─── Sprite cache ─────────────────────────────────────────────────────────
    private class Cat
    {
        public List<Sprite> Grass        = new List<Sprite>();
        public List<Sprite> GrassFlower  = new List<Sprite>();
        public List<Sprite> Pavement     = new List<Sprite>();
        public List<Sprite> StoneGround  = new List<Sprite>();
        public List<Sprite> Wall         = new List<Sprite>();
        public List<Sprite> Tree         = new List<Sprite>();
        public List<Sprite> Bush         = new List<Sprite>();
        public List<Sprite> PlantGrass   = new List<Sprite>();
        public Sprite Chest, ChestOpen, Statue, Altar, Pillar, Coffin, Gravestone;
        public Sprite Stone1, Stone2, Stone3, Pot, RuneBroken;
        public Sprite Crate, Barrel;
    }
    private static Cat _cat;
    private static TType[,] _tileMap;
    private static StringBuilder _log = new StringBuilder();
    private static int _baseTiles, _overlayTiles;

    [MenuItem("The Hero/Map/Build Map From Cainos Pack")]
    public static void Run()
    {
        _log.Clear();
        _baseTiles = _overlayTiles = 0;
        Log("[TheHeroCainosMap] === Build Map From Cainos Pack ===");

        // 1. Backup
        try
        {
            string s = Path.Combine(Application.dataPath, "../" + SCENE_PATH);
            string d = Path.Combine(Application.dataPath, "../" + BACKUP_PATH);
            if (File.Exists(s)) File.Copy(s, d, true);
            Log("[TheHeroCainosMap] Scene backup -> " + BACKUP_PATH);
        }
        catch (Exception ex) { Fail("Backup failed: " + ex.Message); return; }

        // 2. Locate pack
        string packRoot = FindPackRoot();
        if (packRoot == null) { Fail("Cainos / Pixel Art Top Down - Basic pack not found under Assets/. Please import it first."); return; }
        Log("[TheHeroCainosMap] Cainos pack root: " + packRoot);

        // 3. Build catalog
        _cat = BuildCatalog(packRoot, out string catErr);
        if (_cat == null) { Fail("Catalog build failed: " + catErr); return; }
        Log("[TheHeroCainosMap] Cainos assets found");
        Log("[TheHeroCainosMap] Tile catalog created (grass=" + _cat.Grass.Count
            + ", pavement=" + _cat.Pavement.Count
            + ", stone=" + _cat.StoneGround.Count
            + ", wall=" + _cat.Wall.Count
            + ", trees=" + _cat.Tree.Count
            + ", bushes=" + _cat.Bush.Count + ")");

        // 4. Open scene
        UnityEngine.SceneManagement.Scene scene;
        try { scene = EditorSceneManager.OpenScene(SCENE_PATH, OpenSceneMode.Single); }
        catch (Exception ex) { Fail("Cannot open Map.unity: " + ex.Message); return; }

        // 5. Clear old map content
        ClearOldMapContent();

        // 6. Layout
        BuildTileLayout();

        // 7. Build
        var mapRoot = new GameObject("MapRoot");
        var tilesRoot = new GameObject("Tiles");   tilesRoot.transform.parent = mapRoot.transform;
        var baseLayer = new GameObject("Base");    baseLayer.transform.parent = tilesRoot.transform;
        var overlayLayer = new GameObject("Overlay"); overlayLayer.transform.parent = tilesRoot.transform;
        var objectsRoot = new GameObject("MapObjects"); objectsRoot.transform.parent = mapRoot.transform;

        if (!BuildTwoLayer(baseLayer, overlayLayer)) { Rollback(); Fail("Terrain build failed"); return; }
        Log("[TheHeroCainosMap] Map built (base=" + _baseTiles + ", overlay=" + _overlayTiles + ")");

        PlaceObjects(objectsRoot);
        Log("[TheHeroCainosMap] Objects placed");

        FixCamera();

        if (!Validate(out string vrep)) { Rollback(); Fail("Validation FAILED:\n" + vrep); return; }
        Log("[TheHeroCainosMap] Path validation passed");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, SCENE_PATH);
        Log("[TheHeroCainosMap] Map saved");

        WriteReport();
        AssetDatabase.Refresh();
        Debug.Log("[TheHeroCainosMap] Done. See " + REPORT_OK);
    }

    // ─── Pack discovery ───────────────────────────────────────────────────────
    private static string FindPackRoot()
    {
        // Search for any directory whose path contains both hints
        string assets = Application.dataPath;
        foreach (var dir in Directory.GetDirectories(assets, "*", SearchOption.AllDirectories))
        {
            string norm = dir.Replace('\\', '/');
            if (norm.IndexOf(PACK_ROOT_HINT_B, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                string rel = "Assets" + norm.Substring(assets.Length).Replace('\\', '/');
                return rel;
            }
        }
        // Fallback: top-level Cainos
        string fallback = Path.Combine(assets, "Cainos");
        if (Directory.Exists(fallback)) return "Assets/Cainos";
        return null;
    }

    private static Cat BuildCatalog(string packRoot, out string err)
    {
        err = "";
        var c = new Cat();

        Sprite[] grassSheet = LoadSheet(packRoot, "TX Tileset Grass");
        Sprite[] stoneSheet = LoadSheet(packRoot, "TX Tileset Stone Ground");
        Sprite[] wallSheet  = LoadSheet(packRoot, "TX Tileset Wall");
        Sprite[] plantSheet = LoadSheet(packRoot, "TX Plant");
        Sprite[] propsSheet = LoadSheet(packRoot, "TX Props");

        if (grassSheet == null || grassSheet.Length == 0) { err = "TX Tileset Grass not found"; return null; }
        if (stoneSheet == null || stoneSheet.Length == 0) { err = "TX Tileset Stone Ground not found"; return null; }
        if (wallSheet  == null || wallSheet.Length  == 0) { err = "TX Tileset Wall not found"; return null; }
        if (plantSheet == null || plantSheet.Length == 0) { err = "TX Plant not found"; return null; }
        if (propsSheet == null || propsSheet.Length == 0) { err = "TX Props not found"; return null; }

        // Cainos autotile layout: 4x4 grid where index 5 is the "center" cell.
        // Use the 16 plain centers (TX Tileset Grass 0..15) but prefer middle ones.
        var preferredGrassCenters = new[] { 5, 6, 9, 10 };
        var preferredStoneCenters = new[] { 5, 6, 9, 10 };
        var preferredWall         = new[] { 5, 6, 9, 10 };

        foreach (var sp in grassSheet)
        {
            string n = sp.name;
            if (n.StartsWith("TX Tileset Grass Pavement")) c.Pavement.Add(sp);
            else if (n.StartsWith("TX Tileset Grass Flower")) c.GrassFlower.Add(sp);
            else if (n.StartsWith("TX Tileset Grass "))
            {
                int idx;
                string tail = n.Substring("TX Tileset Grass ".Length);
                if (int.TryParse(tail, out idx) && Array.IndexOf(preferredGrassCenters, idx) >= 0)
                    c.Grass.Add(sp);
            }
        }
        if (c.Grass.Count == 0)
            foreach (var sp in grassSheet)
                if (sp.name.StartsWith("TX Tileset Grass ") && !sp.name.Contains("Flower") && !sp.name.Contains("Pavement"))
                    c.Grass.Add(sp);

        foreach (var sp in stoneSheet)
        {
            string n = sp.name;
            int idx;
            string tail = n.Replace("TX Tileset Stone Ground_", "");
            if (int.TryParse(tail, out idx) && Array.IndexOf(preferredStoneCenters, idx) >= 0)
                c.StoneGround.Add(sp);
        }
        if (c.StoneGround.Count == 0)
            foreach (var sp in stoneSheet) if (sp.name.StartsWith("TX Tileset Stone Ground")) c.StoneGround.Add(sp);

        foreach (var sp in wallSheet)
        {
            string n = sp.name;
            int idx;
            string tail = n.Replace("TX Tileset Wall_", "");
            if (int.TryParse(tail, out idx) && Array.IndexOf(preferredWall, idx) >= 0)
                c.Wall.Add(sp);
        }
        if (c.Wall.Count == 0)
            foreach (var sp in wallSheet) if (sp.name.StartsWith("TX Tileset Wall_")) c.Wall.Add(sp);

        foreach (var sp in plantSheet)
        {
            string n = sp.name;
            if (n.StartsWith("TX Tree") && n.EndsWith(" Upper")) c.Tree.Add(sp);
            else if (n.StartsWith("TX Bush")) c.Bush.Add(sp);
            else if (n.StartsWith("TX Plant - Grass")) c.PlantGrass.Add(sp);
        }
        if (c.Tree.Count == 0) c.Tree.AddRange(plantSheet.Where(s => s.name.StartsWith("TX Tree")));

        foreach (var sp in propsSheet)
        {
            switch (sp.name)
            {
                case "TX Props Chest":         c.Chest = sp; break;
                case "TX Props Chest Opened":  c.ChestOpen = sp; break;
                case "TX Props Statue":        c.Statue = sp; break;
                case "TX Props Altar":         c.Altar = sp; break;
                case "TX Props Pillar":        c.Pillar = sp; break;
                case "TX Props Stone Coffin V":c.Coffin = sp; break;
                case "TX Props Gravestone A":  c.Gravestone = sp; break;
                case "TX Props - Stone 01":    c.Stone1 = sp; break;
                case "TX Props - Stone 02":    c.Stone2 = sp; break;
                case "TX Props - Stone 03":    c.Stone3 = sp; break;
                case "TX Props Pot A":         c.Pot = sp; break;
                case "TX Props Rune Pillar Broken": c.RuneBroken = sp; break;
                case "TX Props Crate":         c.Crate = sp; break;
                case "TX Props Barrel":        c.Barrel = sp; break;
            }
        }

        return c;
    }

    private static Sprite[] LoadSheet(string packRoot, string sheetName)
    {
        // Texture path inside pack
        string p = packRoot + "/Texture/" + sheetName + ".png";
        if (!File.Exists(Path.Combine(Application.dataPath, "..", p))) return null;
        return AssetDatabase.LoadAllAssetsAtPath(p).OfType<Sprite>().ToArray();
    }

    // ─── Layout ───────────────────────────────────────────────────────────────
    private static void BuildTileLayout()
    {
        _tileMap = new TType[MAP_W, MAP_H];
        for (int x = 0; x < MAP_W; x++) for (int y = 0; y < MAP_H; y++) _tileMap[x, y] = TType.Meadow;

        // River strip
        for (int x = 18; x <= 19; x++) for (int y = 0; y < MAP_H; y++) _tileMap[x, y] = TType.River;
        for (int y = 4; y <= 5; y++) { _tileMap[18, y] = TType.Bridge; _tileMap[19, y] = TType.Bridge; }

        // Forest patch
        for (int x = 9; x <= 15; x++) for (int y = 10; y <= 18; y++)
        {
            if (x == 9 && (y >= 17 || y <= 11)) continue;
            if (x == 15 && (y >= 17 || y <= 11)) continue;
            if (x == 10 && y >= 18) continue;
            _tileMap[x, y] = TType.Forest;
        }
        for (int x = 11; x <= 14; x++) for (int y = 12; y <= 16; y++) _tileMap[x, y] = TType.DenseForest;
        _tileMap[12, 10] = TType.Meadow; _tileMap[13, 10] = TType.Meadow;

        // Mountain mass + pass
        for (int x = 21; x <= 32; x++) for (int y = 9; y <= 20; y++) _tileMap[x, y] = TType.Mountain;
        for (int x = 20; x <= 33; x++) { _tileMap[x, 13] = TType.Meadow; _tileMap[x, 14] = TType.Meadow; }
        for (int y = 15; y <= 17; y++) { _tileMap[28, y] = TType.Meadow; _tileMap[29, y] = TType.Meadow; }

        // Dark zone
        for (int x = 26; x <= 35; x++) for (int y = 18; y <= 23; y++) _tileMap[x, y] = TType.Dark;
        _tileMap[28, 18] = TType.Dark; _tileMap[29, 18] = TType.Dark;

        // Road
        for (int x = 1; x <= 17; x++) { if (_tileMap[x,4]==TType.Meadow) _tileMap[x,4]=TType.Road; if (_tileMap[x,5]==TType.Meadow) _tileMap[x,5]=TType.Road; }
        for (int x = 20; x <= 33; x++) { if (_tileMap[x,4]==TType.Meadow) _tileMap[x,4]=TType.Road; if (_tileMap[x,5]==TType.Meadow) _tileMap[x,5]=TType.Road; }
        for (int y = 6; y <= 13; y++) if (_tileMap[21,y]==TType.Meadow) _tileMap[21,y]=TType.Road;
        for (int x = 22; x <= 33; x++) if (_tileMap[x,13]==TType.Meadow) _tileMap[x,13]=TType.Road;
        for (int y = 15; y <= 17; y++) { _tileMap[28,y]=TType.Road; _tileMap[29,y]=TType.Road; }
    }

    // ─── Build ────────────────────────────────────────────────────────────────
    private static bool BuildTwoLayer(GameObject baseLayer, GameObject overlay)
    {
        try
        {
            for (int x = 0; x < MAP_W; x++) for (int y = 0; y < MAP_H; y++)
            {
                TType t = _tileMap[x, y];

                (Sprite baseSp, Color baseColor) = PickBase(t, x, y);
                if (baseSp == null) { Debug.LogError("[TheHeroCainosMap] no base @("+x+","+y+")"); return false; }
                CreateTileGO(baseLayer, x, y, t, baseSp, baseColor, sortingOrder:0, isOverlay:false);
                _baseTiles++;

                (Sprite ovSp, Color ovColor) = PickOverlay(t, x, y);
                if (ovSp != null)
                {
                    CreateTileGO(overlay, x, y, t, ovSp, ovColor, sortingOrder:5, isOverlay:true);
                    _overlayTiles++;
                }
            }
            return true;
        }
        catch (Exception ex) { Debug.LogError("[TheHeroCainosMap] BuildTwoLayer: " + ex); return false; }
    }

    private static (Sprite, Color) PickBase(TType t, int x, int y)
    {
        switch (t)
        {
            case TType.Meadow:
            case TType.Road:
            case TType.Forest:
            case TType.DenseForest:
                return (PickFrom(_cat.Grass, x, y), Color.white);
            case TType.River:
                return (PickFrom(_cat.Grass, x, y), RIVER_TINT);
            case TType.Bridge:
                return (PickFrom(_cat.Grass, x, y), RIVER_TINT);
            case TType.Mountain:
                return (PickFrom(_cat.Wall, x, y), Color.white);
            case TType.Dark:
                return (PickFrom(_cat.StoneGround, x, y), DARK_TINT);
        }
        return (null, Color.white);
    }

    private static (Sprite, Color) PickOverlay(TType t, int x, int y)
    {
        switch (t)
        {
            case TType.Forest:
            case TType.DenseForest:
                if (_cat.Tree.Count == 0) return (null, Color.white);
                return (PickFrom(_cat.Tree, x, y), Color.white);
            case TType.Road:
                if (_cat.Pavement.Count == 0) return (null, Color.white);
                return (PickFrom(_cat.Pavement, x, y), Color.white);
            case TType.Bridge:
                if (_cat.Pavement.Count == 0) return (null, Color.white);
                return (PickFrom(_cat.Pavement, x, y), BRIDGE_TINT);
            case TType.Meadow:
                // sparse flower variants every 5 tiles for life
                if (_cat.GrassFlower.Count > 0 && ((x * 7 + y * 11 + SEED) % 9 == 0))
                    return (PickFrom(_cat.GrassFlower, x, y), Color.white);
                return (null, Color.white);
        }
        return (null, Color.white);
    }

    private static Sprite PickFrom(List<Sprite> pool, int x, int y)
    {
        if (pool == null || pool.Count == 0) return null;
        int idx = Mathf.Abs(x * 31 + y * 17 + SEED) % pool.Count;
        return pool[idx];
    }

    private static void CreateTileGO(GameObject parent, int x, int y, TType t, Sprite sp, Color col, int sortingOrder, bool isOverlay)
    {
        var go = new GameObject((isOverlay ? "Overlay_" : "Tile_") + x + "_" + y);
        go.transform.parent = parent.transform;
        float wx = x - MAP_W / 2f + 0.5f;
        float wy = y - MAP_H / 2f + 0.5f;
        go.transform.position = new Vector3(wx, wy, isOverlay ? -0.05f : 0f);

        // Force 1x1 quad — Cainos sprites are 32px @ PPU=32 so bounds should already be 1
        Vector2 size = sp.bounds.size;
        float sx = (size.x > 0.001f) ? (1f / size.x) : 1f;
        float sy = (size.y > 0.001f) ? (1f / size.y) : 1f;
        go.transform.localScale = new Vector3(sx, sy, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.color = col;
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

    // ─── Objects ──────────────────────────────────────────────────────────────
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

            (Sprite sp, Color tint, float scale) = SpriteForObj(d);
            sr.sprite = sp;
            sr.color = tint;
            if (sp != null)
            {
                Vector2 sz = sp.bounds.size;
                float s = scale / Mathf.Max(0.0001f, Mathf.Max(sz.x, sz.y));
                go.transform.localScale = new Vector3(s, s, 1f);
            }
            else { go.transform.localScale = new Vector3(scale, scale, 1f); }

            var bc = go.AddComponent<BoxCollider2D>();
            bc.size = Vector2.one;
            bc.isTrigger = true;

            // THMapObject is required for click→Combat/Collect/Base routing.
            THMapObject mapObj = (d.Kind == "hero") ? null : go.AddComponent<THMapObject>();
            if (mapObj != null)
            {
                mapObj.id = d.Label;
                mapObj.targetX = px;
                mapObj.targetY = py;
                mapObj.displayName = HumanName(d.Label);
                mapObj.blocksMovement = false;
                mapObj.startsCombat  = false;
            }

            switch (d.Kind)
            {
                case "hero":
                {
                    var h = go.AddComponent<THHero>(); h.heroName = "Knight";
                    break;
                }
                case "castle":
                {
                    var c = go.AddComponent<THCastle>(); c.castleName = "Castle"; c.isPlayerCastle = true;
                    mapObj.type = THMapObject.ObjectType.Base;
                    mapObj.blocksMovement = false;
                    mapObj.displayName = "Замок";
                    break;
                }
                case "resource":
                {
                    var r = go.AddComponent<THResource>();
                    r.resourceType = d.Sub; r.amount = ResAmount(d.Sub);
                    switch (d.Sub)
                    {
                        case "gold":  mapObj.type = THMapObject.ObjectType.GoldResource;  mapObj.rewardGold  = r.amount; mapObj.displayName = "Золото"; break;
                        case "wood":  mapObj.type = THMapObject.ObjectType.WoodResource;  mapObj.rewardWood  = r.amount; mapObj.displayName = "Дерево"; break;
                        case "stone": mapObj.type = THMapObject.ObjectType.StoneResource; mapObj.rewardStone = r.amount; mapObj.displayName = "Камень"; break;
                        case "mana":  mapObj.type = THMapObject.ObjectType.ManaResource;  mapObj.rewardMana  = r.amount; mapObj.displayName = "Мана";   break;
                    }
                    mapObj.blocksMovement = false;
                    break;
                }
                case "mine":
                {
                    mapObj.type = THMapObject.ObjectType.Mine;
                    mapObj.rewardStone = THBalanceConfig.StonePileSmallReward * 4;
                    mapObj.blocksMovement = true;
                    mapObj.displayName = "Каменоломня";
                    break;
                }
                case "chest":
                {
                    mapObj.type = THMapObject.ObjectType.Treasure;
                    mapObj.rewardGold = THBalanceConfig.ChestGoldReward;
                    mapObj.rewardExp  = THBalanceConfig.ChestExpReward;
                    mapObj.blocksMovement = true;
                    mapObj.displayName = "Сундук";
                    break;
                }
                case "enemy":
                {
                    var e = go.AddComponent<THEnemy>();
                    e.enemyType = d.Sub; e.startsCombat = true; e.blocksMovement = true; e.isFinalBoss = false;
                    e.displayName = HumanName(d.Label);
                    mapObj.type = THMapObject.ObjectType.Enemy;
                    mapObj.difficulty = d.Sub == "weak" ? THEnemyDifficulty.Weak
                                       : d.Sub == "medium" ? THEnemyDifficulty.Medium
                                       : THEnemyDifficulty.Strong;
                    mapObj.startsCombat = true;
                    mapObj.blocksMovement = true;
                    mapObj.isFinalBoss = false;
                    mapObj.displayName = e.displayName;
                    break;
                }
                case "boss":
                {
                    var e = go.AddComponent<THEnemy>();
                    e.enemyType = "boss"; e.startsCombat = true; e.blocksMovement = true; e.isFinalBoss = true;
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
                    var a = go.AddComponent<THArtifact>(); a.artifactName = "Ancient Artifact"; a.collected = false;
                    mapObj.type = THMapObject.ObjectType.Artifact;
                    mapObj.blocksMovement = true;
                    mapObj.displayName = a.artifactName;
                    break;
                }
            }
        }
    }

    private static (Sprite, Color, float) SpriteForObj(ObjDef d)
    {
        // Each tuple: sprite, color tint, target size in world units
        switch (d.Kind)
        {
            case "hero":
                return (_cat.Statue ?? _cat.Pillar, new Color(0.4f, 0.7f, 1f, 1f), 0.9f);
            case "castle":
                return (_cat.Pillar ?? _cat.Statue, new Color(0.9f, 0.85f, 0.6f, 1f), 1.0f);
            case "chest":
                return (_cat.Chest, Color.white, 0.7f);
            case "artifact":
                return (_cat.Altar ?? _cat.RuneBroken, new Color(1f, 0.85f, 0.4f, 1f), 0.8f);
            case "mine":
                return (_cat.RuneBroken ?? _cat.Stone3, new Color(0.7f, 0.7f, 0.7f, 1f), 0.9f);
            case "boss":
                return (_cat.Coffin ?? _cat.Statue, new Color(0.6f, 0.2f, 0.8f, 1f), 1.0f);
            case "resource":
                switch (d.Sub)
                {
                    case "gold":  return (_cat.Pot ?? _cat.Crate,        new Color(1f, 0.85f, 0.2f, 1f), 0.7f);
                    case "wood":  return (_cat.Crate ?? _cat.Barrel,     new Color(0.6f, 0.4f, 0.2f, 1f), 0.7f);
                    case "stone": return (_cat.Stone1 ?? _cat.Stone2,    Color.white, 0.7f);
                    case "mana":  return (_cat.Pot ?? _cat.RuneBroken,   new Color(0.3f, 0.6f, 1f, 1f), 0.7f);
                }
                break;
            case "enemy":
                switch (d.Sub)
                {
                    case "weak":   return (_cat.Stone1 ?? _cat.Stone2,        new Color(0.7f, 1f, 0.7f, 1f), 0.7f);
                    case "medium": return (_cat.Gravestone ?? _cat.Stone2,    new Color(1f, 1f, 0.6f, 1f), 0.8f);
                    case "strong": return (_cat.Pillar ?? _cat.RuneBroken,    new Color(1f, 0.6f, 0.3f, 1f), 0.9f);
                }
                break;
        }
        return (_cat.Pot, Color.white, 0.6f);
    }

    private static string HumanName(string l)
    {
        if (l.Contains("Wolf")) return "Дикий волк";
        if (l.Contains("Goblin")) return "Гоблин-разбойник";
        if (l.Contains("Skeleton")) return "Скелет";
        if (l.Contains("Orc")) return "Орк-страж";
        return l;
    }

    private static int ResAmount(string s)
    {
        switch (s)
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

    private static bool TryNearestWalkable(ref int x, ref int y, int radius)
    {
        for (int r = 1; r <= radius; r++) for (int dx = -r; dx <= r; dx++) for (int dy = -r; dy <= r; dy++)
        { int nx = x + dx, ny = y + dy; if (nx<0||ny<0||nx>=MAP_W||ny>=MAP_H) continue; if (IsWalkable(_tileMap[nx,ny])) { x=nx; y=ny; return true; } }
        return false;
    }

    // ─── Camera ───────────────────────────────────────────────────────────────
    private static void FixCamera()
    {
        var cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindObjectOfType<Camera>();
        if (cam == null) return;
        cam.orthographic = true;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.30f, 0.55f, 0.27f, 1f);
    }

    // ─── Clear / Validate / Rollback ──────────────────────────────────────────
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

    private static bool Validate(out string report)
    {
        var sb = new StringBuilder(); bool ok = true;
        var baseGO = FindChild("Base");
        if (baseGO == null) { sb.AppendLine("- No Base layer"); report = sb.ToString(); return false; }
        int count = baseGO.transform.childCount;
        sb.AppendLine("- Base tile count: " + count + " (expected " + (MAP_W * MAP_H) + ")");
        if (count != MAP_W * MAP_H) ok = false;

        bool[,] hit = new bool[MAP_W, MAP_H];
        int badBounds = 0, badSpacing = 0, missingSprite = 0;
        for (int i = 0; i < baseGO.transform.childCount; i++)
        {
            var c = baseGO.transform.GetChild(i);
            var sr = c.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) { missingSprite++; continue; }
            Vector3 b = sr.bounds.size;
            if (b.x < 0.95f || b.y < 0.95f || b.x > 1.05f || b.y > 1.05f) badBounds++;
            float wx = c.position.x + MAP_W/2f - 0.5f;
            float wy = c.position.y + MAP_H/2f - 0.5f;
            int gx = Mathf.RoundToInt(wx), gy = Mathf.RoundToInt(wy);
            if (Mathf.Abs(wx - gx) > 0.01f || Mathf.Abs(wy - gy) > 0.01f) badSpacing++;
            if (gx>=0&&gx<MAP_W&&gy>=0&&gy<MAP_H) hit[gx,gy] = true;
        }
        sb.AppendLine("- Missing sprite tiles: " + missingSprite);
        sb.AppendLine("- Sprite bounds out-of-tolerance: " + badBounds);
        sb.AppendLine("- Spacing errors: " + badSpacing);
        if (missingSprite > 0 || badBounds > 0 || badSpacing > 0) ok = false;

        int missing = 0;
        for (int x = 0; x < MAP_W; x++) for (int y = 0; y < MAP_H; y++) if (!hit[x,y]) missing++;
        sb.AppendLine("- Missing grid cells: " + missing);
        if (missing > 0) ok = false;

        bool reach = BFS(4, 3, 32, 20);
        sb.AppendLine("- Hero(4,3) → DarkLord(32,20): " + (reach ? "OK" : "FAIL"));
        if (!reach) ok = false;

        report = sb.ToString();
        return ok;
    }

    private static GameObject FindChild(string name)
    {
        var scene = SceneManager.GetActiveScene();
        foreach (var go in scene.GetRootGameObjects())
        {
            var r = FindRec(go.transform, name);
            if (r != null) return r.gameObject;
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
        catch (Exception ex) { Debug.LogError("[TheHeroCainosMap] rollback failed: " + ex.Message); }
    }

    // ─── Reports ──────────────────────────────────────────────────────────────
    private static void Log(string s) { _log.AppendLine(s); Debug.Log(s); }

    private static void Fail(string reason)
    {
        Debug.LogError("[TheHeroCainosMap] FAILED: " + reason);
        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# Cainos Map Build — FAILED");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## Reason"); sb.AppendLine(reason);
        sb.AppendLine();
        sb.AppendLine("## Log"); sb.AppendLine("```"); sb.AppendLine(_log.ToString()); sb.AppendLine("```");
        File.WriteAllText(Path.Combine(dir, "Cainos_MapBuild_FAILED.md"), sb.ToString());
        AssetDatabase.Refresh();
    }

    private static void WriteReport()
    {
        string dir = Path.Combine(Application.dataPath, "CodeAudit");
        Directory.CreateDirectory(dir);
        var sb = new StringBuilder();
        sb.AppendLine("# Cainos Map Build — REPORT");
        sb.AppendLine("Generated: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
        sb.AppendLine();
        sb.AppendLine("## 1. Pack");
        sb.AppendLine("- Pixel Art Top Down - Basic (Cainos), found under Assets/Cainos/.");
        sb.AppendLine("- All terrain uses ONLY this pack. No AI-generated tiles used.");
        sb.AppendLine();
        sb.AppendLine("## 2. Tile catalog");
        sb.AppendLine("- Grass centers: " + _cat.Grass.Count);
        sb.AppendLine("- Grass flowers: " + _cat.GrassFlower.Count);
        sb.AppendLine("- Pavement (road): " + _cat.Pavement.Count);
        sb.AppendLine("- Stone Ground centers: " + _cat.StoneGround.Count);
        sb.AppendLine("- Wall (mountain): " + _cat.Wall.Count);
        sb.AppendLine("- Trees: " + _cat.Tree.Count);
        sb.AppendLine("- Bushes: " + _cat.Bush.Count);
        sb.AppendLine();
        sb.AppendLine("## 3. Notes on river / dark zone");
        sb.AppendLine("- Cainos Basic has no water or dark-corruption terrain.");
        sb.AppendLine("- River = grass center tinted blue (RGB 0.25/0.50/0.85), unwalkable.");
        sb.AppendLine("- Bridge = pavement tinted brown, walkable.");
        sb.AppendLine("- Dark zone = Stone Ground tinted purple (RGB 0.45/0.30/0.55), walkable cost 3.");
        sb.AppendLine("- Mountain = Wall sprites (full block), unwalkable.");
        sb.AppendLine();
        sb.AppendLine("## 4. Validation");
        sb.AppendLine("- Base tiles placed: " + _baseTiles + " / " + (MAP_W * MAP_H));
        sb.AppendLine("- Overlay tiles placed: " + _overlayTiles);
        sb.AppendLine("- BFS Hero → DarkLord: passed");
        sb.AppendLine();
        sb.AppendLine("## 5. Manual checks");
        sb.AppendLine("1. Open Map.unity — no checkerboard, tiles flush 1×1.");
        sb.AppendLine("2. Press Play, walk Castle → Bridge → Pass → DarkLord.");
        sb.AppendLine("3. River blocks movement, bridge allows it.");
        sb.AppendLine("4. DarkLord starts combat (not loot).");
        sb.AppendLine();
        sb.AppendLine("## 6. Log");
        sb.AppendLine("```"); sb.AppendLine(_log.ToString()); sb.AppendLine("```");
        File.WriteAllText(Path.Combine(dir, "Cainos_MapBuild_Report.md"), sb.ToString());
    }
}
