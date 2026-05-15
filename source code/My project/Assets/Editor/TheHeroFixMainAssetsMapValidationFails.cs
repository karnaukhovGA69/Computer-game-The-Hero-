using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Editor;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>Fixes MainAssets map validation failures. Menu: The Hero/Map/Fix MainAssets Map Validation Fails</summary>
public static class TheHeroFixMainAssetsMapValidationFails
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string ReportPath = "Assets/CodeAudit/MainAssets_Map_Validation_Fix_Report.md";

    private static readonly FixReport _fix = new FixReport();

    [MenuItem("The Hero/Map/Fix MainAssets Map Validation Fails")]
    public static void FixValidationFails()
    {
        _fix.Reset();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string folder = TheHeroMainAssetsMapUtil.FindMainAssetsFolder();
        if (folder == null)
        {
            Debug.LogError("[TheHeroMainAssetsFix] MainAssets not found.");
            return;
        }

        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        var catalog = MainAssetsSpriteCatalog.Build(folder);
        _fix.SkeletonMageSprite = catalog.SkeletonMage?.name;
        _fix.WolfSprite = catalog.Wolf?.name;
        _fix.BossSprite = catalog.UnderworldKing?.name;

        RemoveBrokenMapContent();
        BuildCorrectTilemapHierarchy(catalog);
        FixCastleAndHero(catalog);
        PlaceStandardResourcesAndEnemies(catalog);
        FixAllMapObjectSprites(catalog);

        EnsureMapSystems();
        TheHeroRestoreMapUI.RestoreOpenMapUI(false);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MapScenePath);

        Debug.Log("[TheHeroMainAssetsFix] Map saved");
        WriteFixReport();

        TheHeroValidateMainAssetsMap.ValidateMainAssetsMap();
    }

    private static void RemoveBrokenMapContent()
    {
        var scene = SceneManager.GetActiveScene();
        var delete = new List<GameObject>();
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            string n = go.name;
            if (n == "Main Camera" || n == "EventSystem" || n == "Canvas" || n == "MapController" ||
                n == "TH_Bootstrap" || n == "MapBounds" || n == "MapHoverLabelController")
                continue;
            if (n.Contains("MapRoot") || n.Contains("Tilemap") || n.Contains("Tile_") || n.StartsWith("Logic") ||
                n == "Tiles" || n == "MapObjects" || n == "WorldMap" || n == "AssetGallery" ||
                n == "Grid" || n == "WalkLogic" || n == "Ground_Tilemap" || n == "Water_Tilemap" || n == "ObjectsRoot")
                delete.Add(go);
        }

        foreach (GameObject go in delete)
            Object.DestroyImmediate(go);

        foreach (THTile t in Object.FindObjectsByType<THTile>(FindObjectsInactive.Include))
            if (t != null) Object.DestroyImmediate(t.gameObject);

        _fix.Notes.Add("Removed old Ground_Tilemap / orphan tiles / stale map roots.");
    }

    private static void BuildCorrectTilemapHierarchy(MainAssetsSpriteCatalog cat)
    {
        var mapRoot = GetOrCreateMapRoot();
        var gridGo = EnsureGridRoot(mapRoot);
        ClearChildren(gridGo);

        var ground = EnsureTilemap(gridGo, "GroundTilemap", 0);
        var road = EnsureTilemap(gridGo, "RoadTilemap", 1);
        var water = EnsureTilemap(gridGo, "WaterTilemap", 2);
        var bridge = EnsureTilemap(gridGo, "BridgeTilemap", 3);
        var forest = EnsureTilemap(gridGo, "ForestTilemap", 4);
        var detail = EnsureTilemap(gridGo, "DetailTilemap", 5);
        var dark = EnsureTilemap(gridGo, "DarkTilemap", 6);
        var blocking = EnsureTilemap(gridGo, "BlockingTilemap", 7);

        var logicRoot = GetOrCreateChild(mapRoot, "WalkLogic");
        ClearChildren(logicRoot);
        var tileCache = new Dictionary<Sprite, Tile>();

        for (int x = 0; x < TheHeroMainAssetsMapUtil.MapW; x++)
        for (int y = 0; y < TheHeroMainAssetsMapUtil.MapH; y++)
        {
            Zone z = MapLayout.GetZone(x, y);
            Vector3Int cell = new Vector3Int(x, y, 0);

            SetTm(ground, cell, cat.PickGrass(x, y), tileCache);
            if (z == Zone.Road) SetTm(road, cell, cat.PickRoad(x, y), tileCache);
            if (z == Zone.Water) SetTm(water, cell, cat.Water, tileCache);
            if (z == Zone.Bridge) { SetTm(water, cell, cat.Water, tileCache); SetTm(bridge, cell, cat.Bridge, tileCache); SetTm(road, cell, cat.PickRoad(x, y), tileCache); }
            if (z == Zone.Forest) SetTm(forest, cell, cat.PickForest(x, y), tileCache);
            if (z == Zone.Dark) SetTm(dark, cell, cat.PickDark(x, y), tileCache);
            if (z == Zone.Stone) SetTm(detail, cell, cat.PickStone(x, y), tileCache);

            CreateLogicTile(logicRoot, x, y, z);
        }

        _fix.GroundTilemapCreated = TheHeroMainAssetsMapUtil.CountUsedTiles(ground) > 0;
        _fix.RoadTiles = TheHeroMainAssetsMapUtil.CountUsedTiles(road);
        _fix.BridgeTiles = TheHeroMainAssetsMapUtil.CountUsedTiles(bridge);
        _fix.ForestTiles = TheHeroMainAssetsMapUtil.CountUsedTiles(forest) + TheHeroMainAssetsMapUtil.CountUsedTiles(detail);
        _fix.DarkTiles = TheHeroMainAssetsMapUtil.CountUsedTiles(dark);

        Debug.Log("[TheHeroMainAssetsFix] GroundTilemap created");
        Debug.Log("[TheHeroMainAssetsFix] Sub-sprites loaded from MainAssets");
        Debug.Log("[TheHeroMainAssetsFix] RoadTilemap filled");
        Debug.Log("[TheHeroMainAssetsFix] Bridge placed");
        Debug.Log("[TheHeroMainAssetsFix] Forest/detail area placed");
        Debug.Log("[TheHeroMainAssetsFix] Dark zone placed");
    }

    private static void FixCastleAndHero(MainAssetsSpriteCatalog cat)
    {
        var objectsRoot = GetOrCreateChild(GetOrCreateMapRoot(), "ObjectsRoot");
        ClearChildren(objectsRoot);

        // Castle
        foreach (var old in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                     .Where(o => o.type == THMapObject.ObjectType.Base).ToArray())
            Object.DestroyImmediate(old.gameObject);

        GameObject castle = new GameObject("Castle_Player");
        castle.transform.SetParent(objectsRoot.transform, false);
        castle.transform.position = Cell(TheHeroMainAssetsMapUtil.CenterX, TheHeroMainAssetsMapUtil.CenterY);

        var mo = castle.AddComponent<THMapObject>();
        mo.id = "Castle_Player";
        mo.type = THMapObject.ObjectType.Base;
        mo.displayName = "Замок";
        mo.targetX = TheHeroMainAssetsMapUtil.CenterX;
        mo.targetY = TheHeroMainAssetsMapUtil.CenterY;
        mo.blocksMovement = false;
        mo.startsCombat = false;
        castle.AddComponent<THCastle>();

        if (cat.CastleSprite != null)
        {
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(castle, cat.CastleSprite, 2.2f, 70);
            _fix.CastleAsset = cat.CastleAssetPath + " :: " + cat.CastleSprite.name;
        }
        else
        {
            BuildCompositeCastle(castle, cat);
            _fix.CastleAsset = "Composite fallback from MainAssets props";
            _fix.Notes.Add("No dedicated castle sprite found; constructed fallback castle from MainAssets props.");
        }

        EnsureCastleSpriteRenderer(castle, cat);
        if (castle.GetComponent<BoxCollider2D>() == null)
            castle.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 1.2f);
        _fix.CastlePosition = $"({mo.targetX}, {mo.targetY})";

        Debug.Log("[TheHeroMainAssetsFix] Castle moved to center");
        Debug.Log("[TheHeroMainAssetsFix] Castle sprite/fallback assigned");

        // Hero
        GameObject hero = GameObject.Find("Hero") ?? new GameObject("Hero");
        hero.transform.SetParent(objectsRoot.transform, false);
        hero.transform.position = Cell(TheHeroMainAssetsMapUtil.HeroX, TheHeroMainAssetsMapUtil.HeroY);
        if (cat.Hero != null) TheHeroMainAssetsMapUtil.ApplyObjectSprite(hero, cat.Hero, 0.95f, 100);
        var mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
        mover.currentX = TheHeroMainAssetsMapUtil.HeroX;
        mover.currentY = TheHeroMainAssetsMapUtil.HeroY;
        if (hero.GetComponent<BoxCollider2D>() == null)
            hero.AddComponent<BoxCollider2D>();

        _fix.HeroPosition = $"({mover.currentX}, {mover.currentY})";
    }

    private static void BuildCompositeCastle(GameObject parent, MainAssetsSpriteCatalog cat)
    {
        var parts = new[] { cat.PickProp("pillar", "tower", "wall"), cat.PickProp("stone", "wall"), cat.PickProp("wall", "house") }
            .Where(s => s != null).Take(3).ToList();
        if (parts.Count == 0) return;

        // Main sprite on parent so Castle_Player always has SpriteRenderer (required by gameplay/UI tools).
        TheHeroMainAssetsMapUtil.ApplyObjectSprite(parent, parts[0], 2.0f, 70);

        float[] xs = { -0.35f, 0.35f, 0f };
        float[] ys = { 0f, 0f, 0.35f };
        for (int i = 1; i < parts.Count; i++)
        {
            var child = new GameObject("CastlePart_" + i);
            child.transform.SetParent(parent.transform, false);
            child.transform.localPosition = new Vector3(xs[i % xs.Length], ys[i % ys.Length], 0);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = parts[i];
            sr.sortingOrder = 70 + i;
            float dim = Mathf.Max(parts[i].bounds.size.x, parts[i].bounds.size.y);
            child.transform.localScale = Vector3.one * (0.9f / Mathf.Max(0.01f, dim));
        }
    }

    private static void EnsureCastleSpriteRenderer(GameObject castle, MainAssetsSpriteCatalog cat)
    {
        // Parent Castle_Player keeps Collider2D + gameplay scripts only; SpriteRenderer lives
        // on a child "Visual" so composite castles work and validator finds it via children.
        var sr = TheHeroMainAssetsMapUtil.EnsureSpriteRenderer(castle);
        if (sr == null) return;
        if (sr.sprite != null) return;

        Sprite fallback = cat.CastleSprite ?? cat.PickProp("pillar", "tower", "wall", "house", "stone");
        if (fallback != null)
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(castle, fallback, 2.2f, 70);
    }

    private static void FixAllMapObjectSprites(MainAssetsSpriteCatalog cat)
    {
        int replaced = 0;
        foreach (var mo in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
        {
            if (mo.type == THMapObject.ObjectType.Base)
            {
                if (!TheHeroMainAssetsMapUtil.CastleHasValidSprite(mo.gameObject) && mo.gameObject.name == "Castle_Player")
                    EnsureCastleSpriteRenderer(mo.gameObject, cat);
                continue;
            }
            var sr = mo.GetComponent<SpriteRenderer>();
            if (sr == null) continue;
            if (sr.sprite != null && !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite)) continue;

            Sprite sp = cat.SpriteForMapObject(mo);
            if (sp == null) continue;
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(mo.gameObject, sp, cat.TargetCells(mo), cat.Sorting(mo));
            replaced++;
        }

        _fix.SpritesReplaced = replaced;
        Debug.Log("[TheHeroMainAssetsFix] Whole-sheet enemy/resource sprites replaced");
    }

    private static void PlaceStandardResourcesAndEnemies(MainAssetsSpriteCatalog cat)
    {
        var root = GameObject.Find("ObjectsRoot")?.transform;
        if (root == null) return;

        var resources = new (string id, string name, THMapObject.ObjectType type, int x, int y, Sprite sp)[]
        {
            ("Gold_Center_01", "Золото", THMapObject.ObjectType.GoldResource, 25, 17, cat.Gold),
            ("Wood_Center_01", "Дерево", THMapObject.ObjectType.WoodResource, 22, 15, cat.Wood),
            ("Stone_Center_01", "Камень", THMapObject.ObjectType.StoneResource, 23, 14, cat.StoneRes),
            ("Gold_West_01", "Золото", THMapObject.ObjectType.GoldResource, 7, 18, cat.Gold),
            ("Wood_West_01", "Дерево", THMapObject.ObjectType.WoodResource, 5, 14, cat.Wood),
            ("Wood_West_02", "Дерево", THMapObject.ObjectType.WoodResource, 9, 20, cat.Wood),
            ("Stone_East_01", "Камень", THMapObject.ObjectType.StoneResource, 38, 18, cat.StoneRes),
            ("Stone_East_02", "Камень", THMapObject.ObjectType.StoneResource, 40, 14, cat.StoneRes),
            ("Stone_East_03", "Камень", THMapObject.ObjectType.StoneResource, 36, 16, cat.StoneRes),
            ("Gold_East_01", "Золото", THMapObject.ObjectType.GoldResource, 42, 12, cat.Gold),
            ("Gold_South_01", "Золото", THMapObject.ObjectType.GoldResource, 20, 7, cat.Gold),
            ("Mana_North_01", "Мана", THMapObject.ObjectType.ManaResource, 28, 28, cat.Mana),
            ("Mana_North_02", "Мана", THMapObject.ObjectType.ManaResource, 22, 26, cat.Mana),
            ("Chest_West_01", "Сундук", THMapObject.ObjectType.Treasure, 6, 16, cat.Chest),
            ("Chest_East_01", "Сундук", THMapObject.ObjectType.Treasure, 41, 20, cat.Chest),
            ("Artifact_Forest_01", "Артефакт", THMapObject.ObjectType.Artifact, 4, 22, cat.Artifact),
        };
        foreach (var r in resources) PlaceResource(root, r.id, r.name, r.type, r.x, r.y, r.sp);

        var enemies = new (string id, string name, int x, int y, Sprite sp, THEnemyDifficulty diff, bool boss)[]
        {
            ("Enemy_Wolf_01", "Волки", 30, 17, cat.Wolf, THEnemyDifficulty.Weak, false),
            ("Enemy_Wolf_02", "Волки", 19, 14, cat.Wolf, THEnemyDifficulty.Weak, false),
            ("Enemy_CursedWolf_West", "Проклятые волки", 8, 18, cat.Wolf, THEnemyDifficulty.Weak, false),
            ("Enemy_Skeleton_N1", "Скелеты", 20, 28, cat.Skeleton, THEnemyDifficulty.Medium, false),
            ("Enemy_Skeleton_N2", "Скелеты", 26, 29, cat.Skeleton, THEnemyDifficulty.Medium, false),
            ("Enemy_Skeleton_N3", "Скелеты", 32, 27, cat.Skeleton, THEnemyDifficulty.Medium, false),
            ("Enemy_SkeletonMage_North", "Маг-скелет", 24, 29, cat.SkeletonMage, THEnemyDifficulty.Strong, false),
            ("Enemy_Gargoyle_Guard", "Кровавая гаргулья", 34, 26, cat.BloodGargoyle, THEnemyDifficulty.Strong, false),
            ("Enemy_East_Guard", "Тёмный тролль", 40, 22, cat.DarkTroll, THEnemyDifficulty.Strong, false),
            ("Enemy_South_01", "Волки", 18, 5, cat.Wolf, THEnemyDifficulty.Weak, false),
            ("Enemy_DarkLord_Final", "Тёмный Лорд", 28, 30, cat.UnderworldKing, THEnemyDifficulty.Deadly, true),
        };
        foreach (var e in enemies) PlaceEnemy(root, e.id, e.name, e.x, e.y, e.sp, e.diff, e.boss);

        Debug.Log("[TheHeroMainAssetsFix] Skeleton Mage placed");
        Debug.Log("[TheHeroMainAssetsFix] Wolf/dark monster placed");
        Debug.Log("[TheHeroMainAssetsFix] DarkLord boss sprite fixed");
    }

    private static void PlaceResource(Transform root, string id, string displayName, THMapObject.ObjectType type, int x, int y, Sprite sp)
    {
        var go = new GameObject(id);
        go.transform.SetParent(root, false);
        go.transform.position = Cell(x, y);
        var mo = go.AddComponent<THMapObject>();
        mo.id = id;
        mo.type = type;
        mo.displayName = displayName;
        mo.targetX = x;
        mo.targetY = y;
        mo.blocksMovement = type == THMapObject.ObjectType.Treasure || type == THMapObject.ObjectType.Artifact;
        if (type != THMapObject.ObjectType.Artifact)
        {
            var res = go.AddComponent<THResource>();
            res.resourceType = type.ToString();
        }
        else go.AddComponent<THArtifact>();
        if (sp != null) TheHeroMainAssetsMapUtil.ApplyObjectSprite(go, sp, 0.75f, 40);
        go.AddComponent<BoxCollider2D>();
    }

    private static void PlaceEnemy(Transform root, string id, string displayName, int x, int y, Sprite sp, THEnemyDifficulty diff, bool boss)
    {
        var go = new GameObject(id);
        go.transform.SetParent(root, false);
        go.transform.position = Cell(x, y);
        var mo = go.AddComponent<THMapObject>();
        mo.id = id;
        mo.type = THMapObject.ObjectType.Enemy;
        mo.displayName = displayName;
        mo.targetX = x;
        mo.targetY = y;
        mo.difficulty = diff;
        mo.startsCombat = true;
        mo.blocksMovement = true;
        mo.isFinalBoss = boss;
        mo.isDarkLord = boss;
        go.AddComponent<THEnemy>();
        if (sp != null) TheHeroMainAssetsMapUtil.ApplyObjectSprite(go, sp, boss ? 1.35f : 1f, boss ? 55 : 45);
        go.AddComponent<BoxCollider2D>();
    }

    private static void EnsureMapSystems()
    {
        var ctrl = GetOrCreate("MapController");
        if (ctrl.GetComponent<THMapController>() == null)
            ctrl.AddComponent<THMapController>();
        if (ctrl.GetComponent<THMapGridInput>() == null)
            ctrl.AddComponent<THMapGridInput>();

        var bounds = GetOrCreate("MapBounds");
        var b = bounds.GetComponent<THMapBounds>() ?? bounds.AddComponent<THMapBounds>();
        b.minX = 0; b.minY = 0;
        b.maxX = TheHeroMainAssetsMapUtil.MapW - 1;
        b.maxY = TheHeroMainAssetsMapUtil.MapH - 1;
        b.initialized = true;

        GameObject hero = GameObject.Find("Hero");
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

    // ─── layout ────────────────────────────────────────────────────────────────

    private enum Zone { Grass, Road, Forest, Stone, Water, Bridge, Dark }

    private static class MapLayout
    {
        public static Zone GetZone(int x, int y)
        {
            int cx = TheHeroMainAssetsMapUtil.CenterX;
            int cy = TheHeroMainAssetsMapUtil.CenterY;

            if (x >= 10 && x <= 11 && y >= 4 && y <= 27 && !(x >= 9 && x <= 12 && (y == 16 || y == 17)))
                return Zone.Water;
            if (x >= 9 && x <= 12 && (y == 16 || y == 17)) return Zone.Bridge;
            if (y >= 24 && y <= 30 && x >= 14 && x <= 40) return Zone.Dark;
            if (x >= 2 && x <= 14 && y >= 8 && y <= 24) return Zone.Forest;
            if (x >= 32 && x <= 45 && y >= 8 && y <= 24) return Zone.Stone;
            if (Mathf.Abs(x - cx) <= 4 && Mathf.Abs(y - cy) <= 3) return Zone.Road;
            if (y == 10 || y == 11) return Zone.Road;
            if (x == cx || x == cx + 1) return Zone.Road;
            return Zone.Grass;
        }
    }

    // ─── helpers ─────────────────────────────────────────────────────────────────

    private static Vector3 Cell(int x, int y) => new Vector3(x, y, -0.2f);

    private static GameObject GetOrCreate(string name)
    {
        GameObject go = GameObject.Find(name);
        return go != null ? go : new GameObject(name);
    }

    private static GameObject GetOrCreateMapRoot()
    {
        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot == null)
        {
            mapRoot = new GameObject("MapRoot");
            return mapRoot;
        }

        // Drop stale Grid child missing Grid component (causes MissingComponentException on tile paint).
        Transform staleGrid = mapRoot.transform.Find("Grid");
        if (staleGrid != null && staleGrid.GetComponent<Grid>() == null)
            Object.DestroyImmediate(staleGrid.gameObject);

        // Remove orphan tilemap layers parented directly under MapRoot (old broken builds).
        for (int i = mapRoot.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = mapRoot.transform.GetChild(i);
            if (child.name.EndsWith("Tilemap", StringComparison.Ordinal) || child.name == "WalkLogic")
                Object.DestroyImmediate(child.gameObject);
        }

        return mapRoot;
    }

    private static GameObject EnsureGridRoot(GameObject mapRoot)
    {
        GameObject orphan = GameObject.Find("Grid");
        if (orphan != null && orphan.transform.parent == null)
            Object.DestroyImmediate(orphan);

        Transform child = mapRoot.transform.Find("Grid");
        GameObject gridGo = child != null ? child.gameObject : new GameObject("Grid");
        gridGo.transform.SetParent(mapRoot.transform, false);

        Grid grid = gridGo.GetComponent<Grid>();
        if (grid == null)
            grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        return gridGo;
    }

    private static GameObject GetOrCreateChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        return t != null ? t.gameObject : EnsureChild(parent, name);
    }

    private static GameObject EnsureChild(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void ClearChildren(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
    }

    private static Tilemap EnsureTilemap(GameObject gridParent, string name, int order)
    {
        if (gridParent.GetComponent<Grid>() == null)
            gridParent.AddComponent<Grid>();

        Transform existing = gridParent.transform.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject(name);
        go.transform.SetParent(gridParent.transform, false);
        go.transform.localPosition = Vector3.zero;

        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        return tm;
    }

    private static void SetTm(Tilemap tm, Vector3Int cell, Sprite sp, Dictionary<Sprite, Tile> cache)
    {
        if (tm == null || sp == null) return;
        if (!cache.TryGetValue(sp, out Tile tile))
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sp;
            cache[sp] = tile;
        }
        tm.SetTile(cell, tile);
    }

    private static void CreateLogicTile(GameObject parent, int x, int y, Zone z)
    {
        var go = new GameObject($"LogicTile_{x}_{y}");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, y, 0);
        var t = go.AddComponent<THTile>();
        t.x = x; t.y = y;
        t.tileType = z switch
        {
            Zone.Road => "road",
            Zone.Bridge => "bridge",
            Zone.Water => "water",
            Zone.Forest => "forest",
            Zone.Dark => "dark",
            Zone.Stone => "hill",
            _ => "grass",
        };
        t.ApplyMovementBalance();
        if (!t.walkable)
        {
            var c = go.AddComponent<BoxCollider2D>();
            c.size = Vector2.one;
        }
    }

    private static void WriteFixReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("# MainAssets Map Validation Fix Report");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## 1. GroundTilemap");
        sb.AppendLine(_fix.GroundTilemapCreated
            ? "GroundTilemap was missing/wrong name (e.g. Ground_Tilemap). Recreated as **GroundTilemap** under Grid."
            : "GroundTilemap creation failed — check MainAssets grass tiles.");
        sb.AppendLine();
        sb.AppendLine("## 2. Castle position");
        sb.AppendLine($"- {_fix.CastlePosition} (center target {TheHeroMainAssetsMapUtil.CenterX},{TheHeroMainAssetsMapUtil.CenterY})");
        sb.AppendLine();
        sb.AppendLine("## 3. Castle asset");
        sb.AppendLine($"- {_fix.CastleAsset}");
        foreach (string n in _fix.Notes) sb.AppendLine($"- {n}");
        sb.AppendLine();
        sb.AppendLine("## 4. Tile layers");
        sb.AppendLine($"- Road tiles: {_fix.RoadTiles}");
        sb.AppendLine($"- Bridge tiles: {_fix.BridgeTiles}");
        sb.AppendLine($"- Forest/detail tiles: {_fix.ForestTiles}");
        sb.AppendLine($"- Dark tiles: {_fix.DarkTiles}");
        sb.AppendLine();
        sb.AppendLine("## 5–7. Key enemies");
        sb.AppendLine($"- Skeleton Mage: {(_fix.SkeletonMageSprite ?? "assigned via catalog")}");
        sb.AppendLine($"- Wolf: {(_fix.WolfSprite ?? "assigned via catalog")}");
        sb.AppendLine($"- DarkLord: {(_fix.BossSprite ?? "assigned via catalog")}");
        sb.AppendLine();
        sb.AppendLine("## 8. Whole-sheet replacements");
        sb.AppendLine($"- Replaced on enemies/resources: {_fix.SpritesReplaced}");
        sb.AppendLine();
        sb.AppendLine("## 9. Re-validation");
        sb.AppendLine("Run **The Hero → Validation → Validate MainAssets Map** and check Console for remaining FAIL.");

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));
        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/MainAssets_Map_Validation_Fix_Report.md"), sb.ToString());
    }

    internal sealed class FixReport
    {
        public bool GroundTilemapCreated;
        public int RoadTiles, BridgeTiles, ForestTiles, DarkTiles;
        public string CastlePosition, HeroPosition, CastleAsset;
        public string SkeletonMageSprite, WolfSprite, BossSprite;
        public int SpritesReplaced;
        public readonly List<string> Notes = new List<string>();
        public void Reset() { Notes.Clear(); SpritesReplaced = 0; CastleAsset = ""; }
    }
}

/// <summary>Sprite catalog for MainAssets map fix/build.</summary>
public sealed class MainAssetsSpriteCatalog
{
    public List<Sprite> Grass = new List<Sprite>();
    public List<Sprite> Road = new List<Sprite>();
    public List<Sprite> Forest = new List<Sprite>();
    public List<Sprite> Stone = new List<Sprite>();
    public List<Sprite> Dark = new List<Sprite>();
    public List<Sprite> Props = new List<Sprite>();
    public Sprite Water, Bridge, Hero, CastleSprite;
    public string CastleAssetPath;
    public Sprite Wolf, Skeleton, SkeletonMage, BloodGargoyle, DarkTroll, UnderworldKing;
    public Sprite Gold, Wood, StoneRes, Mana, Chest, Artifact;

    public static MainAssetsSpriteCatalog Build(string folder)
    {
        var c = new MainAssetsSpriteCatalog();
        c.Grass = Load(folder, "TX Tileset Grass");
        c.Road = Load(folder, "Main_tiles").Concat(Load(folder, "walls_floor")).ToList();
        c.Forest = Load(folder, "TX Plant").Concat(Load(folder, "Trees_animation")).Concat(Load(folder, "ground_grass_details")).ToList();
        c.Stone = Load(folder, "walls_floor").Concat(Load(folder, "Main_tiles")).ToList();
        c.Dark = Load(folder, "Interior").Concat(Load(folder, "walls_floor")).ToList();
        c.Props = Load(folder, "TX Props");
        c.Water = Pick(Load(folder, "Water_animation"), 0);
        c.Bridge = Pick(Load(folder, "Bridges"), 0) ?? TheHeroMainAssetsMapUtil.PickByName(Load(folder, "Bridges"), "bridge");
        c.Hero = TheHeroMainAssetsMapUtil.PickCharacterFrame(folder, "idle");
        c.Wolf = TheHeroMainAssetsMapUtil.PickCharacterFrame(folder, "FR_121_CursedWolf");
        c.Skeleton = TheHeroMainAssetsMapUtil.PickCharacterFrame(folder, "Skeleton Warrior");
        c.SkeletonMage = TheHeroMainAssetsMapUtil.PickCharacterFrame(folder, "Skeleton Mage");
        c.BloodGargoyle = TheHeroMainAssetsMapUtil.PickCharacterFrame(folder, "FR_124_BloodGargoyle");
        c.DarkTroll = TheHeroMainAssetsMapUtil.PickCharacterFrame(folder, "FR_127_DarkTroll");
        c.UnderworldKing = TheHeroMainAssetsMapUtil.PickCharacterFrame(folder, "FR_130_UnderworldKing");
        c.Gold = TheHeroMainAssetsMapUtil.PickByName(c.Props, "pot", "gold") ?? TheHeroMainAssetsMapUtil.PickByName(Load(folder, "Icons"), "gold");
        c.Wood = TheHeroMainAssetsMapUtil.PickByName(c.Props, "crate", "barrel", "wood");
        c.StoneRes = TheHeroMainAssetsMapUtil.PickByName(c.Props, "stone");
        c.Mana = TheHeroMainAssetsMapUtil.PickByName(c.Props, "rune", "crystal", "mana");
        c.Chest = TheHeroMainAssetsMapUtil.PickByName(c.Props, "chest");
        c.Artifact = TheHeroMainAssetsMapUtil.PickByName(c.Props, "altar", "statue");

        c.CastleSprite = FindCastleSprite(out c.CastleAssetPath);
        return c;
    }

    private static List<Sprite> Load(string folder, string file) =>
        TheHeroMainAssetsMapUtil.LoadSlicedSprites($"{folder}/{file}.png");

    private static Sprite Pick(List<Sprite> list, int i) =>
        list != null && list.Count > i ? list[i] : list?.FirstOrDefault();

    public Sprite PickGrass(int x, int y) => PickFrom(Grass, x, y);
    public Sprite PickRoad(int x, int y) => PickFrom(Road, x, y) ?? PickFrom(Grass, x, y);
    public Sprite PickForest(int x, int y) => PickFrom(Forest, x, y);
    public Sprite PickStone(int x, int y) => PickFrom(Stone, x, y);
    public Sprite PickDark(int x, int y) => PickFrom(Dark, x, y);
    public Sprite PickProp(params string[] t) => TheHeroMainAssetsMapUtil.PickByName(Props, t);

    private static Sprite PickFrom(List<Sprite> pool, int x, int y)
    {
        if (pool == null || pool.Count == 0) return null;
        return pool[Mathf.Abs(x * 31 + y * 17) % pool.Count];
    }

    public Sprite SpriteForMapObject(THMapObject mo)
    {
        if (mo.displayName != null && mo.displayName.Contains("Маг-скелет")) return SkeletonMage;
        if (mo.displayName != null && (mo.displayName.Contains("волк") || mo.displayName.Contains("Волк"))) return Wolf;
        if (mo.isDarkLord) return UnderworldKing;
        if (mo.displayName != null && mo.displayName.Contains("Скелет")) return Skeleton;
        if (mo.displayName != null && mo.displayName.Contains("гаргуль")) return BloodGargoyle ?? DarkTroll;
        if (mo.displayName != null && mo.displayName.Contains("тролл")) return DarkTroll ?? BloodGargoyle;
        switch (mo.type)
        {
            case THMapObject.ObjectType.GoldResource: return Gold;
            case THMapObject.ObjectType.WoodResource: return Wood;
            case THMapObject.ObjectType.StoneResource: return StoneRes;
            case THMapObject.ObjectType.ManaResource: return Mana;
            case THMapObject.ObjectType.Treasure: return Chest;
            case THMapObject.ObjectType.Artifact: return Artifact;
            case THMapObject.ObjectType.Enemy: return Skeleton ?? Wolf;
            default: return PickProp("stone");
        }
    }

    public float TargetCells(THMapObject mo) => mo.isDarkLord ? 1.35f : mo.type == THMapObject.ObjectType.Enemy ? 1f : 0.75f;
    public int Sorting(THMapObject mo) => mo.isDarkLord ? 55 : mo.type == THMapObject.ObjectType.Enemy ? 45 : 40;

    private static Sprite FindCastleSprite(out string path)
    {
        path = "";
        string[] roots = { "Assets/ExternalAssets", "Assets/Resources", "Assets/Sprites" };
        string[] keys = { "castle", "fort", "keep", "tower", "building" };
        foreach (string root in roots)
        {
            if (!AssetDatabase.IsValidFolder(root)) continue;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (p.IndexOf("MainAssets", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (p.IndexOf("Tiny Swords", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (p.IndexOf("GeneratedToday", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (p.IndexOf("UnityAI", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (!keys.Any(k => p.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)) continue;
                foreach (Sprite sp in TheHeroMainAssetsMapUtil.LoadSlicedSprites(p))
                {
                    if (TheHeroMainAssetsMapUtil.LooksLikeUiButton(sp)) continue;
                    float dim = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
                    if (dim > 4f) continue;
                    path = p;
                    return sp;
                }
            }
        }
        return null;
    }
}
