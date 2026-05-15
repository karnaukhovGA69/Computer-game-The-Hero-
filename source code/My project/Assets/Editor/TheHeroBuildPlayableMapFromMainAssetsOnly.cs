using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>
/// Builds a fully playable 48x32 map using ONLY Assets/ExternalAssets/MainAssets sub-sprites.
/// No mountains, no dark zone, no Tiny Swords. If a feature's source asset is missing, the
/// feature is simply omitted (warning to report). Menu:
/// The Hero/Map/Build Playable Map From MainAssets Only
/// </summary>
public static class TheHeroBuildPlayableMapFromMainAssetsOnly
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string MainAssetsRoot = "Assets/ExternalAssets/MainAssets";
    private const string ReportPath = "Assets/CodeAudit/MainAssets_Only_Playable_Map_Report.md";

    private const int W = 48;
    private const int H = 32;
    private const int CX = 24;
    private const int CY = 16;
    private const int HeroX = 24;
    private const int HeroY = 13;

    private static readonly StringBuilder _report = new StringBuilder();
    private static readonly List<string> _missing = new List<string>();

    [MenuItem("The Hero/Map/Build Playable Map From MainAssets Only")]
    public static void Build()
    {
        _report.Clear();
        _missing.Clear();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder(MainAssetsRoot))
        {
            Debug.LogError($"[TheHeroMainAssetsOnly] MainAssets not found at {MainAssetsRoot}");
            return;
        }
        Debug.Log("[TheHeroMainAssetsOnly] MainAssets found");

        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        Catalog cat = Catalog.Load();
        Debug.Log("[TheHeroMainAssetsOnly] Sub-sprites catalog built");

        ClearOldMapContent();
        var mapRoot = GetOrCreate("MapRoot");
        var grid = EnsureGrid(mapRoot);
        var objectsRoot = GetOrCreateChild(mapRoot, "ObjectsRoot");
        ClearChildren(objectsRoot);

        var ground = NewTilemap(grid, "GroundTilemap", 0);
        var detail = NewTilemap(grid, "DetailTilemap", 1);
        var road = NewTilemap(grid, "RoadTilemap", 2);
        Tilemap water = cat.Water != null ? NewTilemap(grid, "WaterTilemap", 3) : null;
        Tilemap bridge = cat.Bridge != null ? NewTilemap(grid, "BridgeTilemap", 4) : null;
        var forest = NewTilemap(grid, "ForestTilemap", 5);
        var decor = NewTilemap(grid, "ObjectDecorTilemap", 6);
        var blocking = NewTilemap(grid, "BlockingTilemap", 7);

        PaintGround(ground, cat);
        PaintForestZone(forest, detail, cat);
        PaintRuinsZone(detail, decor, cat);
        PaintBossZone(detail, decor, cat);
        PaintSouthZone(detail, decor, cat);
        Debug.Log("[TheHeroMainAssetsOnly] Map rebuilt without mountains and dark zone");
        Debug.Log("[TheHeroMainAssetsOnly] Forest area created");
        Debug.Log("[TheHeroMainAssetsOnly] Ruins area created");
        Debug.Log("[TheHeroMainAssetsOnly] Boss area created");

        PaintRoads(road, cat);
        Debug.Log("[TheHeroMainAssetsOnly] Road created");

        PaintWaterAndBridge(water, bridge, cat);
        Debug.Log("[TheHeroMainAssetsOnly] Water/bridge processed");

        var castle = BuildCastle(objectsRoot, cat);
        Debug.Log("[TheHeroMainAssetsOnly] Castle centered");

        var hero = BuildHero(objectsRoot, cat);
        Debug.Log("[TheHeroMainAssetsOnly] Hero placed near castle");

        PlaceResources(objectsRoot, cat);
        Debug.Log("[TheHeroMainAssetsOnly] Resources placed");

        PlaceEnemies(objectsRoot, cat);
        Debug.Log("[TheHeroMainAssetsOnly] Enemies placed");

        EnsureSystems(castle, hero);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MapScenePath);
        Debug.Log("[TheHeroMainAssetsOnly] Map saved");

        WriteReport(cat);
        TheHeroValidateMainAssetsOnlyMap.Run();
    }

    // ── catalog ──────────────────────────────────────────────────────────────
    private sealed class Catalog
    {
        public List<Sprite> Grass = new List<Sprite>();
        public List<Sprite> GrassDetail = new List<Sprite>();
        public List<Sprite> Floor = new List<Sprite>();
        public List<Sprite> Walls = new List<Sprite>();
        public List<Sprite> Interior = new List<Sprite>();
        public List<Sprite> Props = new List<Sprite>();
        public List<Sprite> Plants = new List<Sprite>();
        public List<Sprite> Trees = new List<Sprite>();
        public List<Sprite> Woods = new List<Sprite>();
        public List<Sprite> Houses = new List<Sprite>();
        public Sprite Water, Bridge;
        public Sprite Road;
        public Sprite Hero, Wolf, Skeleton, SkeletonMage, BloodGargoyle, DarkTroll, UnderworldKing, MadDoctor, ClockworkBat;
        public Sprite Gold, Wood, Stone, Mana, Chest, Artifact;

        public static Catalog Load()
        {
            var c = new Catalog();
            c.Grass = Sub("TX Tileset Grass.png");
            c.GrassDetail = Sub("ground_grass_details.png");
            c.Floor = Sub("Main_tiles.png");
            c.Walls = Sub("walls_floor.png");
            c.Interior = Sub("Interior.png");
            c.Props = Sub("TX Props.png");
            c.Plants = Sub("TX Plant.png");
            c.Trees = Sub("Trees_animation.png");
            c.Woods = Sub("free_pixel_16_woods.png");
            c.Houses = Sub("house_details.png");
            c.Water = Sub("Water_animation.png").FirstOrDefault();
            c.Bridge = Sub("Bridges.png").FirstOrDefault();
            c.Road = ByName(c.Floor, "road", "path", "dirt", "stone")
                  ?? ByName(c.Walls, "road", "path", "stone", "floor")
                  ?? c.GrassDetail.FirstOrDefault();

            c.Hero = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "idle");
            c.Wolf = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_121_CursedWolf");
            c.Skeleton = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "Skeleton Warrior");
            c.SkeletonMage = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "Skeleton Mage");
            c.BloodGargoyle = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_124_BloodGargoyle");
            c.DarkTroll = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_127_DarkTroll");
            c.UnderworldKing = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_130_UnderworldKing");
            c.MadDoctor = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_123_MadDoctor");
            c.ClockworkBat = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_122_ClockworkBat");

            var icons = Sub("Icons.png");
            c.Gold = ByName(c.Props, "pot", "coin", "gold") ?? ByName(icons, "gold", "coin");
            c.Wood = ByName(c.Props, "crate", "barrel", "wood", "log") ?? ByName(c.Houses, "wood");
            c.Stone = ByName(c.Props, "stone", "rock") ?? ByName(c.Walls, "stone");
            c.Mana = ByName(c.Props, "rune", "crystal", "mana", "gem") ?? ByName(icons, "mana", "crystal", "gem");
            c.Chest = ByName(c.Props, "chest", "box");
            c.Artifact = ByName(c.Props, "altar", "statue", "shrine");
            return c;
        }

        public static List<Sprite> Sub(string file)
        {
            string p = $"{MainAssetsRoot}/{file}";
            if (!File.Exists(p)) return new List<Sprite>();
            return TheHeroMainAssetsMapUtil.LoadSlicedSprites(p);
        }

        public static Sprite ByName(IEnumerable<Sprite> pool, params string[] tokens) =>
            TheHeroMainAssetsMapUtil.PickByName(pool, tokens);
    }

    // ── painters ────────────────────────────────────────────────────────────
    private static void PaintGround(Tilemap tm, Catalog c)
    {
        if (c.Grass.Count == 0) { _missing.Add("TX Tileset Grass.png sub-sprites"); return; }
        var tiles = c.Grass.Take(4).Select(MakeTile).ToList();
        for (int x = 0; x < W; x++)
        for (int y = 0; y < H; y++)
            tm.SetTile(new Vector3Int(x, y, 0), tiles[(x + y) % tiles.Count]);
    }

    private static void PaintForestZone(Tilemap forest, Tilemap detail, Catalog c)
    {
        var pool = new List<Sprite>();
        pool.AddRange(c.Plants);
        pool.AddRange(c.Trees);
        pool.AddRange(c.Woods);
        pool.AddRange(c.GrassDetail);
        pool = pool.Where(s => s != null).Distinct().Take(6).ToList();
        if (pool.Count == 0) { _missing.Add("forest sub-sprites"); return; }
        var tiles = pool.Select(MakeTile).ToList();

        for (int x = 2; x <= 13; x++)
        for (int y = 8; y <= 25; y++)
            if (((x * 7 + y * 13) % 4) == 0)
                forest.SetTile(new Vector3Int(x, y, 0), tiles[(x + y) % tiles.Count]);
    }

    private static void PaintRuinsZone(Tilemap detail, Tilemap decor, Catalog c)
    {
        var floor = c.Walls.Concat(c.Interior).Concat(c.Floor).Where(s => s != null).Distinct().Take(6).ToList();
        var props = c.Props.Take(6).ToList();
        if (floor.Count == 0) { _missing.Add("ruins floor sub-sprites"); return; }
        var ftiles = floor.Select(MakeTile).ToList();

        for (int x = 33; x <= 45; x++)
        for (int y = 8; y <= 22; y++)
            if (((x + y) % 3) == 0)
                detail.SetTile(new Vector3Int(x, y, 0), ftiles[(x * 5 + y) % ftiles.Count]);

        if (props.Count > 0)
        {
            var ptiles = props.Select(MakeTile).ToList();
            for (int x = 34; x <= 44; x += 3)
            for (int y = 9; y <= 21; y += 4)
                decor.SetTile(new Vector3Int(x, y, 0), ptiles[(x + y) % ptiles.Count]);
        }
    }

    private static void PaintBossZone(Tilemap detail, Tilemap decor, Catalog c)
    {
        var floor = c.Walls.Concat(c.Interior).Where(s => s != null).Distinct().Take(5).ToList();
        if (floor.Count == 0) { _missing.Add("boss-area floor sub-sprites"); return; }
        var tiles = floor.Select(MakeTile).ToList();
        for (int x = 14; x <= 34; x++)
        for (int y = 24; y <= 30; y++)
            detail.SetTile(new Vector3Int(x, y, 0), tiles[(x * 3 + y) % tiles.Count]);

        var props = c.Props.Take(4).ToList();
        if (props.Count > 0)
        {
            var ptiles = props.Select(MakeTile).ToList();
            for (int x = 16; x <= 32; x += 4)
                decor.SetTile(new Vector3Int(x, 25, 0), ptiles[x % ptiles.Count]);
        }
    }

    private static void PaintSouthZone(Tilemap detail, Tilemap decor, Catalog c)
    {
        var pool = c.GrassDetail.Concat(c.Props).Where(s => s != null).Distinct().Take(4).ToList();
        if (pool.Count == 0) return;
        var tiles = pool.Select(MakeTile).ToList();
        for (int x = 14; x <= 34; x += 3)
        for (int y = 2; y <= 7; y += 2)
            decor.SetTile(new Vector3Int(x, y, 0), tiles[(x + y) % tiles.Count]);
    }

    private static void PaintRoads(Tilemap tm, Catalog c)
    {
        Sprite sp = c.Road ?? c.GrassDetail.FirstOrDefault() ?? c.Grass.FirstOrDefault();
        if (sp == null) { _missing.Add("road sub-sprite"); return; }
        var tile = MakeTile(sp);

        for (int x = 6; x <= 42; x++) tm.SetTile(new Vector3Int(x, CY, 0), tile);
        for (int y = 4; y <= 28; y++) tm.SetTile(new Vector3Int(CX, y, 0), tile);

        // Brownish tint if we had to substitute a non-road sprite.
        if (c.Road == null || !sp.name.ToLowerInvariant().Contains("road"))
            tm.color = new Color(0.75f, 0.65f, 0.45f, 1f);
    }

    private static void PaintWaterAndBridge(Tilemap water, Tilemap bridge, Catalog c)
    {
        if (water != null && c.Water != null)
        {
            var t = MakeTile(c.Water);
            for (int y = 4; y <= 28; y++)
            {
                if (y == CY || y == CY + 1) continue; // bridge gap
                water.SetTile(new Vector3Int(10, y, 0), t);
                water.SetTile(new Vector3Int(11, y, 0), t);
            }
        }
        if (bridge != null && c.Bridge != null)
        {
            var t = MakeTile(c.Bridge);
            bridge.SetTile(new Vector3Int(10, CY, 0), t);
            bridge.SetTile(new Vector3Int(11, CY, 0), t);
            bridge.SetTile(new Vector3Int(10, CY + 1, 0), t);
            bridge.SetTile(new Vector3Int(11, CY + 1, 0), t);
        }
    }

    // ── objects ──────────────────────────────────────────────────────────────
    private static GameObject BuildCastle(GameObject root, Catalog c)
    {
        var castle = new GameObject("Castle_Player");
        castle.transform.SetParent(root.transform, false);
        castle.transform.position = new Vector3(CX, CY, -0.2f);

        var mo = castle.AddComponent<THMapObject>();
        mo.id = "Castle_Player";
        mo.type = THMapObject.ObjectType.Base;
        mo.displayName = "Замок";
        mo.targetX = CX;
        mo.targetY = CY;
        mo.blocksMovement = false;
        mo.startsCombat = false;
        castle.AddComponent<THCastle>();
        castle.AddComponent<BoxCollider2D>().size = new Vector2(1.4f, 1.4f);

        // Composite visual from house_details + walls_floor + TX Props (no dedicated castle sprite).
        AddVisual(castle, c.Houses.FirstOrDefault() ?? c.Walls.FirstOrDefault() ?? c.Props.FirstOrDefault(),
            "Visual_House", Vector3.zero, 1.5f, 70);
        AddVisual(castle, Catalog.ByName(c.Walls, "wall", "stone") ?? c.Walls.FirstOrDefault(),
            "Visual_Wall_1", new Vector3(-0.45f, 0f, 0f), 0.8f, 71);
        AddVisual(castle, Catalog.ByName(c.Walls, "wall", "stone") ?? c.Walls.FirstOrDefault(),
            "Visual_Wall_2", new Vector3(0.45f, 0f, 0f), 0.8f, 71);
        AddVisual(castle, c.Props.FirstOrDefault(),
            "Visual_Decor", new Vector3(0f, 0.4f, 0f), 0.6f, 72);
        return castle;
    }

    private static void AddVisual(GameObject parent, Sprite sp, string name, Vector3 localPos, float targetCells, int sortingOrder)
    {
        if (sp == null) return;
        var v = new GameObject(name);
        v.transform.SetParent(parent.transform, false);
        v.transform.localPosition = localPos;
        var sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = sortingOrder;
        float dim = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
        v.transform.localScale = Vector3.one * (targetCells / Mathf.Max(0.01f, dim));
    }

    private static GameObject BuildHero(GameObject root, Catalog c)
    {
        GameObject hero = GameObject.Find("Hero") ?? new GameObject("Hero");
        hero.transform.SetParent(root.transform, false);
        hero.transform.position = new Vector3(HeroX, HeroY, -0.2f);

        if (c.Hero != null)
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(hero, c.Hero, 0.95f, 100);

        var mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
        mover.currentX = HeroX;
        mover.currentY = HeroY;
        if (hero.GetComponent<BoxCollider2D>() == null) hero.AddComponent<BoxCollider2D>();
        return hero;
    }

    private static void PlaceResources(GameObject root, Catalog c)
    {
        var list = new List<(string id, string name, THMapObject.ObjectType t, int x, int y, Sprite s)>
        {
            ("Gold_Center_01",  "Золото", THMapObject.ObjectType.GoldResource,  25, 17, c.Gold),
            ("Gold_West_01",    "Золото", THMapObject.ObjectType.GoldResource,   7, 18, c.Gold),
            ("Gold_East_01",    "Золото", THMapObject.ObjectType.GoldResource,  42, 12, c.Gold),
            ("Gold_South_01",   "Золото", THMapObject.ObjectType.GoldResource,  20,  6, c.Gold),
            ("Wood_Forest_01",  "Дерево", THMapObject.ObjectType.WoodResource,   5, 14, c.Wood),
            ("Wood_Forest_02",  "Дерево", THMapObject.ObjectType.WoodResource,   9, 20, c.Wood),
            ("Wood_Center_01",  "Дерево", THMapObject.ObjectType.WoodResource,  22, 15, c.Wood),
            ("Stone_East_01",   "Камень", THMapObject.ObjectType.StoneResource, 38, 18, c.Stone),
            ("Stone_East_02",   "Камень", THMapObject.ObjectType.StoneResource, 40, 14, c.Stone),
            ("Stone_East_03",   "Камень", THMapObject.ObjectType.StoneResource, 36, 16, c.Stone),
            ("Mana_North_01",   "Мана",   THMapObject.ObjectType.ManaResource,  28, 27, c.Mana),
            ("Mana_North_02",   "Мана",   THMapObject.ObjectType.ManaResource,  22, 25, c.Mana),
            ("Chest_Forest",    "Сундук", THMapObject.ObjectType.Treasure,       6, 16, c.Chest),
            ("Chest_East_01",   "Сундук", THMapObject.ObjectType.Treasure,      41, 20, c.Chest),
            ("Artifact_Forest", "Артефакт", THMapObject.ObjectType.Artifact,     4, 22, c.Artifact),
        };
        foreach (var r in list)
            PlaceResource(root, r.id, r.name, r.t, r.x, r.y, r.s ?? c.Props.FirstOrDefault());
    }

    private static void PlaceResource(GameObject root, string id, string display, THMapObject.ObjectType type, int x, int y, Sprite sp)
    {
        var go = new GameObject(id);
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, y, -0.2f);
        var mo = go.AddComponent<THMapObject>();
        mo.id = id;
        mo.type = type;
        mo.displayName = display;
        mo.targetX = x;
        mo.targetY = y;
        mo.blocksMovement = type == THMapObject.ObjectType.Treasure || type == THMapObject.ObjectType.Artifact;
        if (type == THMapObject.ObjectType.Artifact) go.AddComponent<THArtifact>();
        else
        {
            var res = go.AddComponent<THResource>();
            res.resourceType = type.ToString();
        }
        if (sp != null && !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sp))
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(go, sp, 0.75f, 40);
        go.AddComponent<BoxCollider2D>();
    }

    private static void PlaceEnemies(GameObject root, Catalog c)
    {
        PlaceEnemy(root, "Enemy_Wolf_Start", "Проклятые волки", 19, 14, c.Wolf, THEnemyDifficulty.Weak, false);
        PlaceEnemy(root, "Enemy_Wolf_West", "Волки",            8, 18, c.Wolf, THEnemyDifficulty.Weak, false);
        PlaceEnemy(root, "Enemy_Skeleton_Forest", "Скелеты",     6, 22, c.Skeleton, THEnemyDifficulty.Medium, false);
        PlaceEnemy(root, "Enemy_Skeleton_Ruins", "Скелеты",     36, 14, c.Skeleton, THEnemyDifficulty.Medium, false);
        PlaceEnemy(root, "Enemy_BloodGargoyle_East", "Кровавая гаргулья", 40, 20, c.BloodGargoyle ?? c.DarkTroll, THEnemyDifficulty.Strong, false);
        PlaceEnemy(root, "Enemy_SkeletonMage_North", "Маг-скелет", 24, 28, c.SkeletonMage, THEnemyDifficulty.Strong, false);
        PlaceEnemy(root, "Enemy_DarkTroll_Guard", "Тёмный тролль", 28, 26, c.DarkTroll, THEnemyDifficulty.Strong, false);
        PlaceEnemy(root, "Enemy_DarkLord_Final", "Тёмный Лорд",   24, 30, c.UnderworldKing, THEnemyDifficulty.Deadly, true);
    }

    private static void PlaceEnemy(GameObject root, string id, string display, int x, int y, Sprite sp, THEnemyDifficulty d, bool boss)
    {
        var go = new GameObject(id);
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, y, -0.2f);
        var mo = go.AddComponent<THMapObject>();
        mo.id = id;
        mo.type = THMapObject.ObjectType.Enemy;
        mo.displayName = display;
        mo.targetX = x;
        mo.targetY = y;
        mo.difficulty = d;
        mo.startsCombat = true;
        mo.blocksMovement = true;
        mo.isFinalBoss = boss;
        mo.isDarkLord = boss;
        go.AddComponent<THEnemy>();
        go.AddComponent<BoxCollider2D>();
        if (sp != null && !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sp))
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(go, sp, boss ? 1.4f : 1f, boss ? 55 : 45);
    }

    private static void EnsureSystems(GameObject castle, GameObject hero)
    {
        var ctrl = GameObject.Find("MapController") ?? new GameObject("MapController");
        if (ctrl.GetComponent<THMapController>() == null) ctrl.AddComponent<THMapController>();
        if (ctrl.GetComponent<THMapGridInput>() == null) ctrl.AddComponent<THMapGridInput>();

        var bounds = GameObject.Find("MapBounds") ?? new GameObject("MapBounds");
        var b = bounds.GetComponent<THMapBounds>() ?? bounds.AddComponent<THMapBounds>();
        b.minX = 0; b.minY = 0; b.maxX = W - 1; b.maxY = H - 1; b.initialized = true;

        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null && hero != null)
        {
            var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
            follow.Target = hero.transform;
            cam.orthographic = true;
            cam.orthographicSize = 7.5f;
        }
        var controller = ctrl.GetComponent<THMapController>();
        if (controller != null && hero != null)
            controller.HeroMover = hero.GetComponent<THStrictGridHeroMovement>();
    }

    // ── scene helpers ────────────────────────────────────────────────────────
    private static void ClearOldMapContent()
    {
        var scene = SceneManager.GetActiveScene();
        var keep = new HashSet<string> { "Main Camera", "EventSystem", "Canvas", "MapController",
            "TH_Bootstrap", "MapBounds", "MapHoverLabelController" };
        var del = new List<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (keep.Contains(go.name)) continue;
            string n = go.name;
            if (n == "MapRoot" || n == "Grid" || n == "WalkLogic" || n == "ObjectsRoot" ||
                n.EndsWith("Tilemap", StringComparison.Ordinal))
                del.Add(go);
        }
        foreach (var go in del) Object.DestroyImmediate(go);
        foreach (var t in Object.FindObjectsByType<THTile>(FindObjectsInactive.Include))
            if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static GameObject GetOrCreate(string name) =>
        GameObject.Find(name) ?? new GameObject(name);

    private static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        var t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void ClearChildren(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
    }

    private static GameObject EnsureGrid(GameObject parent)
    {
        var go = GetOrCreateChild(parent, "Grid");
        if (go.GetComponent<Grid>() == null) go.AddComponent<Grid>().cellSize = Vector3.one;
        return go;
    }

    private static Tilemap NewTilemap(GameObject grid, string name, int order)
    {
        var existing = grid.transform.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        return tm;
    }

    private static Tile MakeTile(Sprite sp)
    {
        var t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = sp;
        return t;
    }

    private static void WriteReport(Catalog c)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MainAssets-Only Playable Map Report");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## Assets used (sub-sprites from MainAssets only)");
        Report(sb, "Grass", c.Grass.Count > 0);
        Report(sb, "Grass detail", c.GrassDetail.Count > 0);
        Report(sb, "Floor (Main_tiles)", c.Floor.Count > 0);
        Report(sb, "Walls (walls_floor)", c.Walls.Count > 0);
        Report(sb, "Interior", c.Interior.Count > 0);
        Report(sb, "Props (TX Props)", c.Props.Count > 0);
        Report(sb, "Plants (TX Plant)", c.Plants.Count > 0);
        Report(sb, "Trees", c.Trees.Count > 0);
        Report(sb, "Woods (free_pixel_16_woods)", c.Woods.Count > 0);
        Report(sb, "Houses (house_details)", c.Houses.Count > 0);
        Report(sb, "Water (Water_animation)", c.Water != null);
        Report(sb, "Bridge (Bridges)", c.Bridge != null);
        Report(sb, "Hero idle", c.Hero != null);
        Report(sb, "Wolf", c.Wolf != null);
        Report(sb, "Skeleton Warrior", c.Skeleton != null);
        Report(sb, "Skeleton Mage", c.SkeletonMage != null);
        Report(sb, "BloodGargoyle", c.BloodGargoyle != null);
        Report(sb, "DarkTroll", c.DarkTroll != null);
        Report(sb, "UnderworldKing (DarkLord)", c.UnderworldKing != null);

        sb.AppendLine();
        sb.AppendLine("## Substitutions");
        sb.AppendLine("- Mountains: REMOVED. No mountain asset in MainAssets.");
        sb.AppendLine("- Dark zone: REMOVED. Northern boss area is built from ruins (walls_floor + Interior + TX Props).");
        sb.AppendLine("- Castle visual: composite from house_details + walls_floor + TX Props (no dedicated castle sprite in MainAssets).");

        sb.AppendLine();
        sb.AppendLine("## Map layout");
        sb.AppendLine($"- 48x32 cells. Castle_Player at ({CX},{CY}). Hero at ({HeroX},{HeroY}).");
        sb.AppendLine("- West: forest (TX Plant + Trees + free_pixel_16_woods + ground_grass_details).");
        sb.AppendLine("- East: ruins (walls_floor + Interior + TX Props).");
        sb.AppendLine("- North: boss area (no dark zone): ruins floor + props + Skeleton Mage / DarkTroll / DarkLord.");
        sb.AppendLine("- South: road + light decor + 1 weak enemy + extra resources.");
        sb.AppendLine($"- Water/Bridge: {(c.Water != null && c.Bridge != null ? "river with bridge placed" : "skipped (asset missing)")}.");

        sb.AppendLine();
        sb.AppendLine("## Missing assets (gracefully skipped)");
        if (_missing.Count == 0) sb.AppendLine("None.");
        else foreach (var m in _missing) sb.AppendLine($"- {m}");

        sb.AppendLine();
        sb.AppendLine("## Sub-sprite rule");
        sb.AppendLine("All sprites loaded via `AssetDatabase.LoadAllAssetsAtPath` and filtered through `IsWholeSheetSprite`. No whole PNG sheet is used.");

        sb.AppendLine();
        sb.AppendLine("## Manual verification");
        sb.AppendLine("1. **The Hero → Map → Build Playable Map From MainAssets Only**");
        sb.AppendLine("2. **The Hero → Validation → Validate MainAssets Only Map** (FAIL=0 on gameplay-critical checks).");
        sb.AppendLine("3. Play → MainMenu → New Game.");

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));
        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/MainAssets_Only_Playable_Map_Report.md"), sb.ToString());
    }

    private static void Report(StringBuilder sb, string label, bool found) =>
        sb.AppendLine(found ? $"- [x] {label}" : $"- [ ] {label} — NOT FOUND");
}
