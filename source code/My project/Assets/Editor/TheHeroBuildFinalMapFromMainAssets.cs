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

/// <summary>
/// Builds Map.unity from sliced MainAssets sprites + project-wide castle search.
/// Menu: The Hero/Map/Build Final Map From MainAssets
/// </summary>
public static class TheHeroBuildFinalMapFromMainAssets
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string ReportPath = "Assets/CodeAudit/MainAssets_Final_Map_Report.md";

    private const int MapW = TheHeroMainAssetsMapUtil.MapW;
    private const int MapH = TheHeroMainAssetsMapUtil.MapH;
    private const int CastleX = TheHeroMainAssetsMapUtil.CenterX;
    private const int CastleY = TheHeroMainAssetsMapUtil.CenterY;
    private const int HeroX = TheHeroMainAssetsMapUtil.HeroX;
    private const int HeroY = TheHeroMainAssetsMapUtil.HeroY;

    private static readonly string[] MainAssetsFolderHints =
    {
        "Assets/ExternalAssets/MainAssets",
        "Assets/ExternalAssets/MainAssets/",
    };

    private static readonly string[] CastleSearchRoots =
    {
        "Assets/ExternalAssets",
        "Assets/Resources",
        "Assets/Sprites",
        "Assets",
    };

    private static readonly string[] CastleKeywords =
    {
        "castle", "fort", "fortress", "tower", "base", "building", "house", "hall", "keep", "gate",
        "citadel", "stronghold", "замок", "крепость", "башня", "город", "дом", "постройка",
    };

    private static readonly string[] KeepSceneRoots =
    {
        "Main Camera", "EventSystem", "Canvas", "MapController", "TH_Bootstrap", "MapBounds", "MapHoverLabelController",
    };

    private enum ZoneType
    {
        Grass,
        Road,
        Forest,
        Stone,
        Water,
        Bridge,
        Dark,
    }

    private static ZoneType[,] _zones;
    private static MainAssetsCatalog _catalog;
    private static readonly BuildReportData _report = new BuildReportData();

    [MenuItem("The Hero/Map/Build Final Map From MainAssets")]
    public static void BuildFinalMap()
    {
        _report.Reset();

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string mainAssetsPath = FindMainAssetsFolder();
        if (mainAssetsPath == null)
        {
            Debug.LogError("[TheHeroMainAssetsMap] MainAssets folder not found.");
            return;
        }

        _report.MainAssetsPath = mainAssetsPath;
        Debug.Log("[TheHeroMainAssetsMap] MainAssets found: " + mainAssetsPath);

        PrepareMainAssetsImportSettings(mainAssetsPath);
        _catalog = MainAssetsCatalog.Build(mainAssetsPath, _report);
        if (_catalog == null || !_catalog.IsValid)
        {
            Debug.LogError("[TheHeroMainAssetsMap] Failed to build sprite catalog.");
            return;
        }

        Debug.Log("[TheHeroMainAssetsMap] Sliced sprites loaded");
        Debug.Log("[TheHeroMainAssetsMap] Whole PNG usage blocked");

        SearchCastleAsset();
        Debug.Log("[TheHeroMainAssetsMap] Castle asset search completed");

        var scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        ClearOldMapContent();
        BuildTileLayout();

        var mapRoot = EnsureRoot("MapRoot");
        var gridGo = EnsureChild(mapRoot, "Grid");
        Grid gridComp = gridGo.GetComponent<Grid>();
        if (gridComp == null)
            gridComp = gridGo.AddComponent<Grid>();
        gridComp.cellSize = new Vector3(1f, 1f, 0f);

        // Remove orphan root-level Grid from older map builds.
        GameObject orphanGrid = GameObject.Find("Grid");
        if (orphanGrid != null && orphanGrid.transform.parent == null && orphanGrid != gridGo)
            Object.DestroyImmediate(orphanGrid);

        var groundTm = CreateTilemapLayer(gridGo, "GroundTilemap", 0);
        var roadTm = CreateTilemapLayer(gridGo, "RoadTilemap", 1);
        var waterTm = CreateTilemapLayer(gridGo, "WaterTilemap", 2);
        var bridgeTm = CreateTilemapLayer(gridGo, "BridgeTilemap", 3);
        var forestTm = CreateTilemapLayer(gridGo, "ForestTilemap", 4);
        var detailTm = CreateTilemapLayer(gridGo, "DetailTilemap", 5);
        var darkTm = CreateTilemapLayer(gridGo, "DarkTilemap", 6);
        var blockingTm = CreateTilemapLayer(gridGo, "BlockingTilemap", 7);

        var logicRoot = EnsureChild(mapRoot, "WalkLogic");
        BuildTerrainAndLogic(groundTm, detailTm, roadTm, waterTm, bridgeTm, forestTm, darkTm, blockingTm, logicRoot);

        var objectsRoot = EnsureChild(mapRoot, "ObjectsRoot");
        PlaceGameplayObjects(objectsRoot);

        EnsureMapController();
        EnsureMapBounds();
        FixCameraAndHero();
        TheHeroRestoreMapUI.RestoreOpenMapUI(false);
        EnsureHoverLabel();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MapScenePath);

        Debug.Log("[TheHeroMainAssetsMap] Map rebuilt");
        Debug.Log("[TheHeroMainAssetsMap] Castle placed at center");
        Debug.Log("[TheHeroMainAssetsMap] Hero placed near castle");
        Debug.Log("[TheHeroMainAssetsMap] Resources placed");
        Debug.Log("[TheHeroMainAssetsMap] Enemies placed");
        Debug.Log("[TheHeroMainAssetsMap] DarkLord placed");
        Debug.Log("[TheHeroMainAssetsMap] Map saved");

        WriteReport();
        AssetDatabase.Refresh();
    }

    // ─── MainAssets discovery / import ───────────────────────────────────────

    private static string FindMainAssetsFolder()
    {
        foreach (string hint in MainAssetsFolderHints)
        {
            string norm = hint.TrimEnd('/');
            if (AssetDatabase.IsValidFolder(norm))
                return norm;
        }

        string[] guids = AssetDatabase.FindAssets("MainAssets t:Folder");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.EndsWith("/MainAssets", StringComparison.OrdinalIgnoreCase) ||
                path.Equals("Assets/ExternalAssets/MainAssets", StringComparison.OrdinalIgnoreCase))
                return path;
        }

        return null;
    }

    private static void PrepareMainAssetsImportSettings(string folder)
    {
        string[] pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
        foreach (string guid in pngGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Multiple &&
                HasMetaSlicing(path))
            {
                importer.spriteImportMode = SpriteImportMode.Multiple;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                changed = true;
            }

            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                changed = true;
            }

            // Mesh Type (Full Rect) is already set in each .meta; spriteMeshType API varies by Unity version.

            if (changed)
            {
                importer.SaveAndReimport();
                _report.ReslicedOrReimported.Add(path);
            }
        }
    }

    private static bool HasMetaSlicing(string assetPath)
    {
        string metaPath = assetPath + ".meta";
        if (!File.Exists(metaPath)) return false;
        string text = File.ReadAllText(metaPath);
        return text.Contains("second:") && text.Contains("spriteID:");
    }

    // ─── Castle search (project-wide, not MainAssets) ────────────────────────

    private static void SearchCastleAsset()
    {
        var candidates = new List<CastleCandidate>();

        foreach (string root in CastleSearchRoots)
        {
            if (!AssetDatabase.IsValidFolder(root)) continue;
            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { root });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (IsDisallowedCastlePath(path)) continue;
                if (!CastleKeywords.Any(k => path.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)) continue;

                foreach (Sprite sp in LoadSubSpritesOnly(path))
                {
                    if (sp == null || IsUiOrTerrainTile(sp, path)) continue;
                    float cells = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
                    if (cells > 4f) continue;

                    candidates.Add(new CastleCandidate
                    {
                        Path = path,
                        Sprite = sp,
                        SpriteName = sp.name,
                        Score = ScoreCastle(path, sp),
                    });
                }
            }
        }

        CastleCandidate best = candidates.OrderByDescending(c => c.Score).FirstOrDefault();
        if (best != null && best.Sprite != null)
        {
            _catalog.CastleSprite = best.Sprite;
            _report.CastleAssetPath = best.Path;
            _report.CastleSpriteName = best.SpriteName;
            _report.CastleFound = true;
            _report.CastleFallbackNote = "";
            return;
        }

        _report.CastleFound = false;
        _report.CastleFallbackNote = "полноценный castle asset не найден";
        _catalog.CastleSprite = _catalog.PickProp("pillar", "house", "wall", "tower", "stone")
                                  ?? _catalog.PickGrassCenter();
        _report.CastleAssetPath = _catalog.CastleSprite != null ? "(MainAssets props fallback)" : "none";
        _report.CastleSpriteName = _catalog.CastleSprite != null ? _catalog.CastleSprite.name : "none";
    }

    private static bool IsDisallowedCastlePath(string path)
    {
        string p = path.Replace('\\', '/').ToLowerInvariant();
        if (p.Contains("/mainassets/")) return true;
        if (p.Contains("/tiny swords/")) return true;
        if (p.Contains("generatedtoday") && p.Contains("tile")) return true;
        if (p.Contains("/ui ") || p.Contains("/ui/") || p.Contains("button")) return true;
        if (p.Contains("tileset") || p.Contains("tile_")) return true;
        if (p.Contains("portrait")) return true;
        return false;
    }

    private static bool IsUiOrTerrainTile(Sprite sp, string path)
    {
        string n = (sp.name + " " + path).ToLowerInvariant();
        return n.Contains("button") || n.Contains("icon") || n.Contains("menu") ||
               n.Contains("tileset") || n.Contains("numbers") || n.Contains("equipment");
    }

    private static int ScoreCastle(string path, Sprite sp)
    {
        string p = path.ToLowerInvariant();
        string n = sp.name.ToLowerInvariant();
        int score = 0;
        if (p.Contains("clean_castle")) score += 120;
        if (p.Contains("castle_00") || n.Contains("castle_00")) score += 100;
        if (p.Contains("generatedtoday") || p.Contains("unityai")) score -= 40;
        if (p.Contains("/castle/")) score += 150;
        if (p.Contains("castle")) score += 80;
        if (p.Contains("fort") || p.Contains("keep")) score += 60;
        if (p.Contains("building")) score += 40;
        float dim = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
        if (dim >= 1.2f && dim <= 3.5f) score += 30;
        if (dim < 0.5f || dim > 5f) score -= 50;
        return score;
    }

    // ─── Layout ──────────────────────────────────────────────────────────────

    private static void BuildTileLayout()
    {
        _zones = new ZoneType[MapW, MapH];
        for (int x = 0; x < MapW; x++)
        for (int y = 0; y < MapH; y++)
            _zones[x, y] = ZoneType.Grass;

        int cx = CastleX;
        int cy = CastleY;

        // Central start ring + roads
        for (int x = cx - 4; x <= cx + 4; x++)
        for (int y = cy - 3; y <= cy + 3; y++)
            if (InMap(x, y)) _zones[x, y] = ZoneType.Road;

        // Main cross roads
        for (int x = 2; x < MapW - 2; x++)
        {
            if (InMap(x, 10)) _zones[x, 10] = ZoneType.Road;
            if (InMap(x, 11)) _zones[x, 11] = ZoneType.Road;
        }

        for (int y = 2; y < MapH - 2; y++)
        {
            if (InMap(cx, y)) _zones[cx, y] = ZoneType.Road;
            if (InMap(cx + 1, y)) _zones[cx + 1, y] = ZoneType.Road;
        }

        // West forest
        for (int x = 2; x <= 14; x++)
        for (int y = 8; y <= 24; y++)
            if (InMap(x, y) && _zones[x, y] == ZoneType.Grass)
                _zones[x, y] = ZoneType.Forest;

        // East stone zone
        for (int x = 32; x <= 45; x++)
        for (int y = 8; y <= 24; y++)
            if (InMap(x, y) && _zones[x, y] != ZoneType.Road)
                _zones[x, y] = ZoneType.Stone;

        // North dark boss zone
        for (int x = 16; x <= 42; x++)
        for (int y = 24; y <= 30; y++)
            if (InMap(x, y))
                _zones[x, y] = ZoneType.Dark;

        // River + bridges
        for (int y = 3; y <= 28; y++)
        {
            if (InMap(10, y)) _zones[10, y] = ZoneType.Water;
            if (InMap(11, y)) _zones[11, y] = ZoneType.Water;
        }

        for (int x = 9; x <= 12; x++)
        {
            if (InMap(x, 16)) _zones[x, 16] = ZoneType.Bridge;
            if (InMap(x, 17)) _zones[x, 17] = ZoneType.Bridge;
        }

        // Keep castle cell walkable road
        if (InMap(cx, cy)) _zones[cx, cy] = ZoneType.Road;
        if (InMap(HeroX, HeroY)) _zones[HeroX, HeroY] = ZoneType.Road;
    }

    private static bool InMap(int x, int y) => x >= 0 && y >= 0 && x < MapW && y < MapH;

    // ─── Scene build ─────────────────────────────────────────────────────────

    private static void ClearOldMapContent()
    {
        var scene = SceneManager.GetActiveScene();
        var deleteRoots = new List<GameObject>();
        foreach (GameObject go in scene.GetRootGameObjects())
        {
            string n = go.name;
            if (KeepSceneRoots.Any(k => n.Contains(k))) continue;
            if (n == "MapRoot" || n == "Tiles" || n == "MapObjects" || n == "MapTiles" ||
                n == "TileMap" || n == "WorldMap" || n.StartsWith("Tile_") ||
                n == "AssetGallery" || n.StartsWith("AssetPreview"))
                deleteRoots.Add(go);
        }

        foreach (GameObject go in deleteRoots)
            Undo.DestroyObjectImmediate(go);

        GameObject mapRoot = GameObject.Find("MapRoot");
        if (mapRoot != null)
        {
            for (int i = mapRoot.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(mapRoot.transform.GetChild(i).gameObject);
        }

        var orphanTiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
        foreach (var t in orphanTiles)
        {
            if (t != null && t.gameObject != null)
                Undo.DestroyObjectImmediate(t.gameObject);
        }
    }

    private static Tilemap CreateTilemapLayer(GameObject gridGo, string name, int order)
    {
        Transform existing = gridGo.transform.Find(name);
        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        var go = new GameObject(name);
        go.transform.SetParent(gridGo.transform, false);
        go.transform.localPosition = Vector3.zero;

        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        return tm;
    }

    private static void BuildTerrainAndLogic(
        Tilemap ground, Tilemap detail, Tilemap road, Tilemap water, Tilemap bridge, Tilemap forest,
        Tilemap dark, Tilemap blocking, GameObject logicRoot)
    {
        var tileCache = new Dictionary<Sprite, Tile>();

        for (int x = 0; x < MapW; x++)
        for (int y = 0; y < MapH; y++)
        {
            ZoneType z = _zones[x, y];
            Vector3Int cell = new Vector3Int(x, y, 0);

            Sprite baseSp = _catalog.PickZoneBase(z, x, y);
            Sprite overlaySp = _catalog.PickZoneOverlay(z, x, y);

            SetTile(ground, cell, baseSp, tileCache);
            if (z == ZoneType.Road)
                SetTile(road, cell, overlaySp ?? baseSp, tileCache);
            else if (z == ZoneType.Water)
                SetTile(water, cell, _catalog.Water ?? baseSp, tileCache);
            else if (z == ZoneType.Bridge)
            {
                SetTile(water, cell, _catalog.Water ?? baseSp, tileCache);
                SetTile(bridge, cell, _catalog.Bridge ?? overlaySp, tileCache);
                SetTile(road, cell, overlaySp ?? baseSp, tileCache);
            }
            else if (z == ZoneType.Forest)
                SetTile(forest, cell, overlaySp ?? baseSp, tileCache);
            else if (z == ZoneType.Dark)
                SetTile(dark, cell, overlaySp ?? baseSp, tileCache);
            else if (z == ZoneType.Stone && overlaySp != null)
                SetTile(detail, cell, overlaySp, tileCache);

            CreateLogicTile(logicRoot, x, y, z);
        }

        ground.CompressBounds();
    }

    private static void SetTile(Tilemap tm, Vector3Int cell, Sprite sp, Dictionary<Sprite, Tile> cache)
    {
        if (tm == null || sp == null) return;
        if (!cache.TryGetValue(sp, out Tile tile) || tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = sp;
            cache[sp] = tile;
        }

        tm.SetTile(cell, tile);
    }

    private static void CreateLogicTile(GameObject parent, int x, int y, ZoneType z)
    {
        var go = new GameObject($"LogicTile_{x}_{y}");
        go.transform.SetParent(parent.transform, false);
        go.transform.position = new Vector3(x, y, 0f);

        var tile = go.AddComponent<THTile>();
        tile.x = x;
        tile.y = y;
        tile.tileType = ZoneToTileType(z);
        tile.ApplyMovementBalance();

        if (!tile.walkable)
        {
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;
            col.isTrigger = false;
        }
    }

    private static string ZoneToTileType(ZoneType z)
    {
        switch (z)
        {
            case ZoneType.Road: return "road";
            case ZoneType.Bridge: return "bridge";
            case ZoneType.Water: return "water";
            case ZoneType.Forest: return "forest";
            case ZoneType.Stone: return "hill";
            case ZoneType.Dark: return "dark";
            default: return "grass";
        }
    }

    // ─── Gameplay objects ──────────────────────────────────────────────────────

    private static void PlaceGameplayObjects(GameObject objectsRoot)
    {
        ClearChildren(objectsRoot);

        PlaceCastle(objectsRoot.transform);
        PlaceHero(objectsRoot.transform);
        PlaceAllResources(objectsRoot.transform);
        PlaceAllEnemies(objectsRoot.transform);
    }

    private static void PlaceCastle(Transform parent)
    {
        var go = new GameObject("Castle_Player");
        go.transform.SetParent(parent, false);
        go.transform.position = GridPos(CastleX, CastleY);

        var mo = go.AddComponent<THMapObject>();
        mo.id = "Castle_Player";
        mo.type = THMapObject.ObjectType.Base;
        mo.displayName = "Замок";
        mo.targetX = CastleX;
        mo.targetY = CastleY;
        mo.blocksMovement = false;
        mo.startsCombat = false;

        var castle = go.AddComponent<THCastle>();
        castle.castleName = "Замок";
        castle.isPlayerCastle = true;

        Sprite castleSp = _catalog.CastleSprite ?? _catalog.PickProp("pillar", "tower", "wall", "house", "stone");
        if (castleSp != null)
            ApplyObjectSprite(go, castleSp, 2.2f, 70);
        else
        {
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 70;
        }

        if (go.GetComponent<BoxCollider2D>() == null)
            go.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 1.2f);

        _report.CastlePosition = $"({CastleX}, {CastleY})";
    }

    private static void PlaceHero(Transform parent)
    {
        GameObject go = GameObject.Find("Hero");
        if (go == null)
        {
            go = new GameObject("Hero");
            go.transform.SetParent(parent, false);
        }
        else
        {
            go.transform.SetParent(parent, false);
        }

        go.transform.position = GridPos(HeroX, HeroY);
        if (_catalog.Hero != null)
            ApplyObjectSprite(go, _catalog.Hero, 0.95f, 100);
        else
        {
            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            if (sr.sprite == null)
                _report.Rejected.Add("Hero: idle.png sub-sprite missing; kept existing hero sprite if any");
            sr.sortingOrder = 100;
        }

        var mover = go.GetComponent<THStrictGridHeroMovement>() ?? go.AddComponent<THStrictGridHeroMovement>();
        mover.currentX = HeroX;
        mover.currentY = HeroY;

        var col = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(0.7f, 0.7f);
        col.isTrigger = false;

        _report.HeroPosition = $"({HeroX}, {HeroY})";
    }

    private struct ResSpec
    {
        public string Id;
        public string Name;
        public THMapObject.ObjectType Type;
        public int X, Y;
        public int Gold, Wood, Stone, Mana, Exp;
        public Func<Sprite> Sprite;
    }

    private static void PlaceAllResources(Transform parent)
    {
        var specs = new[]
        {
            new ResSpec { Id = "Gold_Center_01", Name = "Золото", Type = THMapObject.ObjectType.GoldResource, X = 25, Y = 17, Gold = THBalanceConfig.GoldPileSmallReward, Sprite = () => _catalog.Gold },
            new ResSpec { Id = "Wood_Center_01", Name = "Дерево", Type = THMapObject.ObjectType.WoodResource, X = 22, Y = 15, Wood = THBalanceConfig.WoodPileSmallReward, Sprite = () => _catalog.Wood },
            new ResSpec { Id = "Stone_Center_01", Name = "Камень", Type = THMapObject.ObjectType.StoneResource, X = 23, Y = 14, Stone = THBalanceConfig.StonePileSmallReward, Sprite = () => _catalog.Stone },
            new ResSpec { Id = "Gold_West_01", Name = "Золото", Type = THMapObject.ObjectType.GoldResource, X = 7, Y = 18, Gold = THBalanceConfig.GoldPileSmallReward, Sprite = () => _catalog.Gold },
            new ResSpec { Id = "Wood_West_01", Name = "Дерево", Type = THMapObject.ObjectType.WoodResource, X = 5, Y = 14, Wood = THBalanceConfig.WoodPileSmallReward, Sprite = () => _catalog.Wood },
            new ResSpec { Id = "Wood_West_02", Name = "Дерево", Type = THMapObject.ObjectType.WoodResource, X = 9, Y = 20, Wood = THBalanceConfig.WoodPileSmallReward, Sprite = () => _catalog.Wood },
            new ResSpec { Id = "Stone_East_01", Name = "Камень", Type = THMapObject.ObjectType.StoneResource, X = 38, Y = 18, Stone = THBalanceConfig.StonePileSmallReward, Sprite = () => _catalog.Stone },
            new ResSpec { Id = "Stone_East_02", Name = "Камень", Type = THMapObject.ObjectType.StoneResource, X = 40, Y = 14, Stone = THBalanceConfig.StonePileSmallReward, Sprite = () => _catalog.Stone },
            new ResSpec { Id = "Stone_East_03", Name = "Камень", Type = THMapObject.ObjectType.StoneResource, X = 36, Y = 16, Stone = THBalanceConfig.StonePileSmallReward, Sprite = () => _catalog.Stone },
            new ResSpec { Id = "Gold_East_01", Name = "Золото", Type = THMapObject.ObjectType.GoldResource, X = 42, Y = 12, Gold = THBalanceConfig.GoldPileSmallReward, Sprite = () => _catalog.Gold },
            new ResSpec { Id = "Gold_South_01", Name = "Золото", Type = THMapObject.ObjectType.GoldResource, X = 20, Y = 7, Gold = THBalanceConfig.GoldPileSmallReward, Sprite = () => _catalog.Gold },
            new ResSpec { Id = "Mana_North_01", Name = "Мана", Type = THMapObject.ObjectType.ManaResource, X = 28, Y = 28, Mana = THBalanceConfig.ManaCrystalReward, Sprite = () => _catalog.Mana },
            new ResSpec { Id = "Mana_North_02", Name = "Мана", Type = THMapObject.ObjectType.ManaResource, X = 22, Y = 26, Mana = THBalanceConfig.ManaCrystalReward, Sprite = () => _catalog.Mana },
            new ResSpec { Id = "Chest_West_01", Name = "Сундук", Type = THMapObject.ObjectType.Treasure, X = 6, Y = 16, Gold = THBalanceConfig.ChestGoldReward, Exp = THBalanceConfig.ChestExpReward, Sprite = () => _catalog.Chest },
            new ResSpec { Id = "Chest_East_01", Name = "Сундук", Type = THMapObject.ObjectType.Treasure, X = 41, Y = 20, Gold = THBalanceConfig.ChestGoldReward, Exp = THBalanceConfig.ChestExpReward, Sprite = () => _catalog.Chest },
            new ResSpec { Id = "Artifact_Forest_01", Name = "Артефакт", Type = THMapObject.ObjectType.Artifact, X = 4, Y = 22, Sprite = () => _catalog.Artifact },
        };

        foreach (ResSpec s in specs)
            PlaceResource(parent, s);
    }

    private static void PlaceResource(Transform parent, ResSpec spec)
    {
        int x = spec.X, y = spec.Y;
        SnapWalkable(ref x, ref y);

        var go = new GameObject(spec.Id);
        go.transform.SetParent(parent, false);
        go.transform.position = GridPos(x, y);

        var mo = go.AddComponent<THMapObject>();
        mo.id = spec.Id;
        mo.type = spec.Type;
        mo.displayName = spec.Name;
        mo.targetX = x;
        mo.targetY = y;
        mo.rewardGold = spec.Gold;
        mo.rewardWood = spec.Wood;
        mo.rewardStone = spec.Stone;
        mo.rewardMana = spec.Mana;
        mo.rewardExp = spec.Exp;
        mo.blocksMovement = spec.Type == THMapObject.ObjectType.Treasure || spec.Type == THMapObject.ObjectType.Artifact;
        mo.startsCombat = false;

        if (spec.Type != THMapObject.ObjectType.Artifact)
        {
            var res = go.AddComponent<THResource>();
            res.resourceType = spec.Type.ToString();
            res.amount = Math.Max(spec.Gold + spec.Wood + spec.Stone + spec.Mana, 1);
        }
        else
        {
            var art = go.AddComponent<THArtifact>();
            art.artifactName = spec.Name;
            art.collected = false;
        }

        ApplyObjectSprite(go, spec.Sprite(), 0.75f, 40);
        go.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 0.7f);
        _report.Resources.Add($"{spec.Id} @ ({x},{y})");
    }

    private struct EnemySpec
    {
        public string Id;
        public string DisplayName;
        public int X, Y;
        public THEnemyDifficulty Difficulty;
        public bool IsBoss;
        public bool IsDarkLord;
        public Func<Sprite> Sprite;
        public THArmyUnit[] Army;
    }

    private static void PlaceAllEnemies(Transform parent)
    {
        var specs = new[]
        {
            new EnemySpec { Id = "Enemy_Wolf_01", DisplayName = "Волки", X = 30, Y = 17, Difficulty = THEnemyDifficulty.Weak,
                Sprite = () => _catalog.Wolf, Army = Army("wolf", "Волк", 5, 6, 3, 2, 6) },
            new EnemySpec { Id = "Enemy_Wolf_02", DisplayName = "Волки", X = 19, Y = 14, Difficulty = THEnemyDifficulty.Weak,
                Sprite = () => _catalog.Wolf, Army = Army("wolf", "Волк", 4, 6, 3, 2, 5) },
            new EnemySpec { Id = "Enemy_Skeleton_N1", DisplayName = "Скелеты", X = 20, Y = 28, Difficulty = THEnemyDifficulty.Medium,
                Sprite = () => _catalog.Skeleton, Army = Army("skeleton", "Скелет", 8, 5, 3, 2, 4) },
            new EnemySpec { Id = "Enemy_Skeleton_N2", DisplayName = "Скелеты", X = 26, Y = 29, Difficulty = THEnemyDifficulty.Medium,
                Sprite = () => _catalog.Skeleton, Army = Army("skeleton", "Скелет", 10, 5, 3, 2, 4) },
            new EnemySpec { Id = "Enemy_Skeleton_N3", DisplayName = "Скелеты", X = 32, Y = 27, Difficulty = THEnemyDifficulty.Medium,
                Sprite = () => _catalog.Skeleton, Army = Army("skeleton", "Скелет", 12, 5, 3, 2, 3) },
            new EnemySpec { Id = "Enemy_SkeletonMage_N1", DisplayName = "Маг-скелет", X = 24, Y = 30, Difficulty = THEnemyDifficulty.Strong,
                Sprite = () => _catalog.SkeletonMage, Army = Army("skeleton_mage", "Маг-скелет", 1, 40, 8, 4, 7) },
            new EnemySpec { Id = "Enemy_Gargoyle_Guard", DisplayName = "Кровавая гаргулья", X = 34, Y = 26, Difficulty = THEnemyDifficulty.Strong,
                Sprite = () => _catalog.BloodGargoyle ?? _catalog.DarkTroll, Army = Army("gargoyle", "Гаргулья", 1, 80, 12, 8, 5) },
            new EnemySpec { Id = "Enemy_East_01", DisplayName = "Волки", X = 39, Y = 15, Difficulty = THEnemyDifficulty.Medium,
                Sprite = () => _catalog.Wolf, Army = Army("wolf", "Волк", 6, 6, 4, 2, 5) },
            new EnemySpec { Id = "Enemy_East_02", DisplayName = "Скелеты", X = 43, Y = 17, Difficulty = THEnemyDifficulty.Medium,
                Sprite = () => _catalog.Skeleton, Army = Army("skeleton", "Скелет", 8, 5, 3, 2, 4) },
            new EnemySpec { Id = "Enemy_East_Guard", DisplayName = "Тёмный тролль", X = 40, Y = 22, Difficulty = THEnemyDifficulty.Strong,
                Sprite = () => _catalog.DarkTroll ?? _catalog.BloodGargoyle, Army = Army("troll", "Тролль", 1, 100, 10, 7, 4) },
            new EnemySpec { Id = "Enemy_South_01", DisplayName = "Волки", X = 18, Y = 5, Difficulty = THEnemyDifficulty.Weak,
                Sprite = () => _catalog.Wolf, Army = Army("wolf", "Волк", 3, 6, 3, 2, 6) },
            new EnemySpec { Id = "Enemy_South_02", DisplayName = "Скелеты", X = 32, Y = 6, Difficulty = THEnemyDifficulty.Weak,
                Sprite = () => _catalog.Skeleton, Army = Army("skeleton", "Скелет", 6, 5, 3, 2, 4) },
            new EnemySpec { Id = "Enemy_DarkLord_Final", DisplayName = "Тёмный Лорд", X = 28, Y = 30, Difficulty = THEnemyDifficulty.Deadly,
                IsBoss = true, IsDarkLord = true, Sprite = () => _catalog.UnderworldKing,
                Army = new[]
                {
                    Army("darklord", "Тёмный Лорд", 1, 200, 20, 15, 8)[0],
                    Army("skeleton", "Скелет", 12, 5, 3, 2, 3)[0],
                    Army("troll", "Тролль", 1, 80, 10, 7, 4)[0],
                }},
        };

        foreach (EnemySpec s in specs)
            PlaceEnemy(parent, s);
    }

    private static THArmyUnit[] Army(string id, string name, int count, int hp, int atk, int def, int ini) =>
        new[] { new THArmyUnit { id = id, name = name, count = count, hpPerUnit = hp, attack = atk, defense = def, initiative = ini } };

    private static void PlaceEnemy(Transform parent, EnemySpec spec)
    {
        int x = spec.X, y = spec.Y;
        SnapWalkable(ref x, ref y);

        var go = new GameObject(spec.Id);
        go.transform.SetParent(parent, false);
        go.transform.position = GridPos(x, y);

        var mo = go.AddComponent<THMapObject>();
        mo.id = spec.Id;
        mo.type = THMapObject.ObjectType.Enemy;
        mo.displayName = spec.DisplayName;
        mo.targetX = x;
        mo.targetY = y;
        mo.difficulty = spec.Difficulty;
        mo.startsCombat = true;
        mo.blocksMovement = true;
        mo.isFinalBoss = spec.IsBoss;
        mo.isDarkLord = spec.IsDarkLord;
        mo.enemyArmy = spec.Army.Select(u => u.Clone()).ToList();

        var enemy = go.AddComponent<THEnemy>();
        enemy.enemyType = spec.IsBoss ? "boss" : spec.Difficulty == THEnemyDifficulty.Weak ? "weak" : spec.Difficulty == THEnemyDifficulty.Medium ? "medium" : "strong";
        enemy.startsCombat = true;
        enemy.blocksMovement = true;
        enemy.isFinalBoss = spec.IsBoss;
        enemy.displayName = spec.DisplayName;

        ApplyObjectSprite(go, spec.Sprite(), spec.IsBoss ? 1.35f : 1.0f, spec.IsBoss ? 55 : 45);
        go.AddComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);
        _report.Enemies.Add($"{spec.DisplayName} ({spec.Id}) @ ({x},{y})");
    }

    // ─── Scene wiring ──────────────────────────────────────────────────────────

    private static void EnsureMapController()
    {
        var controllerGo = EnsureRoot("MapController");
        if (controllerGo.GetComponent<THMapController>() == null)
            controllerGo.AddComponent<THMapController>();
        if (controllerGo.GetComponent<THMapGridInput>() == null)
            controllerGo.AddComponent<THMapGridInput>();

        var bootstrapGo = EnsureRoot("TH_Bootstrap");
        var bootstrap = bootstrapGo.GetComponent<THBootstrap>() ?? bootstrapGo.AddComponent<THBootstrap>();
        bootstrap.type = THBootstrap.SceneType.Map;
    }

    private static void EnsureMapBounds()
    {
        var boundsGo = EnsureRoot("MapBounds");
        var bounds = boundsGo.GetComponent<THMapBounds>() ?? boundsGo.AddComponent<THMapBounds>();
        bounds.minX = 0;
        bounds.minY = 0;
        bounds.maxX = MapW - 1;
        bounds.maxY = MapH - 1;
        bounds.initialized = true;
    }

    private static void FixCameraAndHero()
    {
        GameObject hero = GameObject.Find("Hero");
        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam == null || hero == null) return;

        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        cam.transform.position = new Vector3(hero.transform.position.x, hero.transform.position.y, -10f);

        var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
        follow.Target = hero.transform;
        follow.followSpeed = 8f;
        follow.z = -10f;

        var mover = hero.GetComponent<THStrictGridHeroMovement>();
        var controller = Object.FindAnyObjectByType<THMapController>();
        if (controller != null && mover != null)
            controller.HeroMover = mover;
    }

    private static void EnsureHoverLabel()
    {
        if (Object.FindAnyObjectByType<THSingleMapHoverLabel>() != null) return;
        var go = new GameObject("MapHoverLabelController");
        go.AddComponent<THSingleMapHoverLabel>();
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static Vector3 GridPos(int x, int y) => new Vector3(x, y, -0.2f);

    private static void SnapWalkable(ref int x, ref int y)
    {
        if (InMap(x, y) && _zones[x, y] != ZoneType.Water) return;
        for (int r = 1; r <= 4; r++)
        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r; dy <= r; dy++)
        {
            int nx = x + dx, ny = y + dy;
            if (!InMap(nx, ny)) continue;
            if (_zones[nx, ny] != ZoneType.Water) { x = nx; y = ny; return; }
        }
    }

    private static void ApplyObjectSprite(GameObject go, Sprite sprite, float targetCells, int sortingOrder)
    {
        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.enabled = sprite != null;
        if (sprite == null) return;

        float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float scale = targetCells / Mathf.Max(0.001f, maxDim);
        go.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static GameObject EnsureRoot(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    private static GameObject EnsureChild(GameObject parent, string name)
    {
        Transform t = parent.transform.Find(name);
        if (t != null) return t.gameObject;
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void ClearChildren(GameObject parent)
    {
        for (int i = parent.transform.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(parent.transform.GetChild(i).gameObject);
    }

    internal static Sprite[] LoadSubSpritesOnly(string assetPath) =>
        TheHeroMainAssetsMapUtil.LoadSlicedSprites(assetPath).ToArray();

    internal static bool IsWholeSheetSprite(Sprite sprite, Sprite[] allSprites, bool multipleSubs) =>
        TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sprite);

    private static void WriteReport()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));
        var sb = new StringBuilder();
        sb.AppendLine("# MainAssets Final Map Report");
        sb.AppendLine();
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## 1. MainAssets folder");
        sb.AppendLine($"- Path: `{_report.MainAssetsPath}`");
        sb.AppendLine();
        sb.AppendLine("## 2. PNG used");
        foreach (string p in _report.UsedPngs.Distinct().OrderBy(p => p))
            sb.AppendLine($"- `{p}`");
        sb.AppendLine();
        sb.AppendLine("## 3. Sub-sprites used");
        foreach (string s in _report.UsedSprites.Distinct().OrderBy(s => s))
            sb.AppendLine($"- `{s}`");
        sb.AppendLine();
        sb.AppendLine("## 4. Meta slicing preserved");
        foreach (string p in _report.MetaSliced.Distinct().OrderBy(p => p))
            sb.AppendLine($"- `{p}`");
        sb.AppendLine();
        sb.AppendLine("## 5. Re-import / settings touched");
        if (_report.ReslicedOrReimported.Count == 0) sb.AppendLine("- None");
        else foreach (string p in _report.ReslicedOrReimported) sb.AppendLine($"- `{p}`");
        sb.AppendLine();
        sb.AppendLine("## 6. Castle asset");
        sb.AppendLine($"- Found: {_report.CastleFound}");
        sb.AppendLine($"- Path: `{_report.CastleAssetPath}`");
        sb.AppendLine($"- Sprite: `{_report.CastleSpriteName}`");
        sb.AppendLine($"- Fallback note: {_report.CastleFallbackNote}");
        sb.AppendLine();
        sb.AppendLine("## 7–9. Positions");
        sb.AppendLine($"- Castle: {_report.CastlePosition}");
        sb.AppendLine($"- Hero: {_report.HeroPosition}");
        sb.AppendLine();
        sb.AppendLine("## 10. Enemies");
        foreach (string e in _report.Enemies) sb.AppendLine($"- {e}");
        sb.AppendLine();
        sb.AppendLine("## 11. Resources");
        foreach (string r in _report.Resources) sb.AppendLine($"- {r}");
        sb.AppendLine();
        sb.AppendLine("## 12. Rejected assets");
        foreach (string r in _report.Rejected) sb.AppendLine($"- {r}");
        sb.AppendLine();
        sb.AppendLine("## 13. Validation");
        sb.AppendLine("Run **The Hero → Validation → Validate MainAssets Map** in Unity.");
        sb.AppendLine();
        sb.AppendLine("## 14. Manual checks");
        sb.AppendLine("1. Play → MainMenu → New Game");
        sb.AppendLine("2. Castle centered, hero beside castle, camera follows");
        sb.AppendLine("3. River/bridge visible, resources/enemies/boss present");
        sb.AppendLine("4. No whole-sheet sprites on units");
        sb.AppendLine("5. Castle opens Base, enemies start combat, resources collect");

        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/MainAssets_Final_Map_Report.md"), sb.ToString());
    }

    // ─── Catalog ───────────────────────────────────────────────────────────────

    private sealed class CastleCandidate
    {
        public string Path;
        public string SpriteName;
        public Sprite Sprite;
        public int Score;
    }

    private sealed class BuildReportData
    {
        public string MainAssetsPath;
        public bool CastleFound;
        public string CastleAssetPath;
        public string CastleSpriteName;
        public string CastleFallbackNote = "";
        public string CastlePosition;
        public string HeroPosition;
        public readonly List<string> UsedPngs = new List<string>();
        public readonly List<string> UsedSprites = new List<string>();
        public readonly List<string> MetaSliced = new List<string>();
        public readonly List<string> ReslicedOrReimported = new List<string>();
        public readonly List<string> Rejected = new List<string>();
        public readonly List<string> Enemies = new List<string>();
        public readonly List<string> Resources = new List<string>();

        public void Reset()
        {
            MainAssetsPath = CastleAssetPath = CastleSpriteName = CastleFallbackNote = CastlePosition = HeroPosition = "";
            CastleFound = false;
            UsedPngs.Clear();
            UsedSprites.Clear();
            MetaSliced.Clear();
            ReslicedOrReimported.Clear();
            Rejected.Clear();
            Enemies.Clear();
            Resources.Clear();
        }
    }

    private sealed class MainAssetsCatalog
    {
        public bool IsValid;
        public List<Sprite> Grass = new List<Sprite>();
        public List<Sprite> Road = new List<Sprite>();
        public List<Sprite> Forest = new List<Sprite>();
        public List<Sprite> StoneTiles = new List<Sprite>();
        public List<Sprite> DarkTiles = new List<Sprite>();
        public Sprite Water;
        public Sprite Bridge;
        public Sprite Hero;
        public Sprite CastleSprite;
        public Sprite Gold, Wood, Stone, Mana, Chest, Artifact;
        public Sprite Wolf, Skeleton, SkeletonMage, BloodGargoyle, DarkTroll, UnderworldKing;
        public List<Sprite> Props = new List<Sprite>();

        public static MainAssetsCatalog Build(string folder, BuildReportData report)
        {
            var c = new MainAssetsCatalog();
            c.Grass = LoadSheet(folder, "TX Tileset Grass", report, prefer: s => s.name.Contains("Grass") && !s.name.Contains("Flower") && !s.name.Contains("Pavement"));
            c.Road = LoadSheet(folder, "Main_tiles", report).Concat(LoadSheet(folder, "walls_floor", report)).ToList();
            if (c.Road.Count == 0)
                c.Road = c.Grass.Where(s => s.name.Contains("Pavement")).ToList();
            c.Forest = LoadSheet(folder, "TX Plant", report, prefer: s => s.name.Contains("Tree") || s.name.Contains("Bush") || s.name.Contains("Plant"));
            c.Forest.AddRange(LoadSheet(folder, "ground_grass_details", report));
            c.Forest.AddRange(LoadSheet(folder, "Trees_animation", report));
            c.StoneTiles = LoadSheet(folder, "walls_floor", report).Concat(LoadSheet(folder, "Main_tiles", report)).ToList();
            c.DarkTiles = LoadSheet(folder, "Interior", report).Concat(LoadSheet(folder, "walls_floor", report)).ToList();
            c.Water = PickFrame(LoadSheet(folder, "Water_animation", report), 0);
            c.Bridge = PickFrame(LoadSheet(folder, "Bridges", report), 0) ?? PickByName(LoadSheet(folder, "Bridges", report), "bridge");

            c.Hero = PickFrame(LoadSheet(folder, "idle", report), 0);
            c.Wolf = PickCharacterFrame(folder, "FR_121_CursedWolf", report);
            c.Skeleton = PickCharacterFrame(folder, "Skeleton Warrior", report);
            c.SkeletonMage = PickCharacterFrame(folder, "Skeleton Mage", report);
            c.BloodGargoyle = PickCharacterFrame(folder, "FR_124_BloodGargoyle", report);
            c.DarkTroll = PickCharacterFrame(folder, "FR_127_DarkTroll", report);
            c.UnderworldKing = PickCharacterFrame(folder, "FR_130_UnderworldKing", report);

            c.Props = LoadSheet(folder, "TX Props", report);
            var icons = LoadSheet(folder, "Icons", report);
            var props = c.Props;
            c.Gold = PickByName(props, "pot", "gold", "coin") ?? PickByName(icons, "gold");
            c.Wood = PickByName(props, "crate", "barrel", "wood", "log") ?? PickByName(icons, "wood");
            c.Stone = PickByName(props, "stone") ?? PickByName(icons, "stone");
            c.Mana = PickByName(props, "rune", "crystal", "mana") ?? PickByName(icons, "mana", "magic");
            c.Chest = PickByName(props, "chest");
            c.Artifact = PickByName(props, "altar", "statue", "rune");

            c.IsValid = c.Grass.Count > 0 && c.Wolf != null && c.Skeleton != null;
            return c;
        }

        private static List<Sprite> LoadSheet(string folder, string fileName, BuildReportData report, Func<Sprite, bool> prefer = null)
        {
            string path = $"{folder}/{fileName}.png";
            if (!File.Exists(Path.Combine(Application.dataPath, "..", path)))
            {
                report.Rejected.Add($"Missing PNG: {path}");
                return new List<Sprite>();
            }

            if (HasMetaSlicing(path)) report.MetaSliced.Add(path);
            report.UsedPngs.Add(path);

            Sprite[] subs = LoadSubSpritesOnly(path);
            if (subs.Length == 0)
            {
                report.Rejected.Add($"No sub-sprites: {path}");
                return new List<Sprite>();
            }

            IEnumerable<Sprite> chosen = prefer != null ? subs.Where(prefer) : subs.AsEnumerable();
            var list = chosen.ToList();
            if (list.Count == 0) list = subs.ToList();
            foreach (Sprite s in list.Take(32))
                report.UsedSprites.Add($"{path} :: {s.name}");
            return list;
        }

        private static Sprite PickCharacterFrame(string folder, string fileName, BuildReportData report)
        {
            string path = $"{folder}/{fileName}.png";
            var list = LoadSheet(folder, fileName, report);
            Sprite pick = list.FirstOrDefault(s => s.name.EndsWith("_0", StringComparison.Ordinal)) ??
                          list.FirstOrDefault(s => s.name.Contains("idle", StringComparison.OrdinalIgnoreCase)) ??
                          list.OrderBy(s => s.rect.width * s.rect.height).FirstOrDefault();
            if (pick != null) report.UsedSprites.Add($"character::{path}::{pick.name}");
            return pick;
        }

        private static Sprite PickFrame(List<Sprite> list, int index) =>
            list != null && list.Count > index ? list[index] : list?.FirstOrDefault();

        private static Sprite PickByName(IEnumerable<Sprite> list, params string[] tokens)
        {
            if (list == null) return null;
            foreach (string token in tokens)
            {
                Sprite hit = list.FirstOrDefault(s => s.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
                if (hit != null) return hit;
            }
            return null;
        }

        public Sprite PickGrassCenter()
        {
            return Grass.FirstOrDefault(s => s.name.Contains(" 5") || s.name.Contains(" 6")) ?? Grass.FirstOrDefault();
        }

        public Sprite PickProp(params string[] tokens) => PickByName(Props, tokens);

        public Sprite PickZoneBase(ZoneType z, int x, int y)
        {
            switch (z)
            {
                case ZoneType.Stone:
                    return PickFrom(StoneTiles, x, y) ?? PickGrassCenter();
                case ZoneType.Dark:
                    return PickFrom(DarkTiles, x, y) ?? PickGrassCenter();
                case ZoneType.Water:
                    return Water ?? PickGrassCenter();
                default:
                    return PickFrom(Grass, x, y) ?? PickGrassCenter();
            }
        }

        public Sprite PickZoneOverlay(ZoneType z, int x, int y)
        {
            switch (z)
            {
                case ZoneType.Forest:
                    return PickFrom(Forest, x, y);
                case ZoneType.Road:
                    return PickFrom(Road, x, y) ?? PickFrom(Grass.Where(s => s.name.Contains("Pavement")).ToList(), x, y);
                case ZoneType.Dark:
                    return PickFrom(DarkTiles, x, y);
                case ZoneType.Stone:
                    return PickFrom(StoneTiles, x, y);
                default:
                    return null;
            }
        }

        private static Sprite PickFrom(List<Sprite> pool, int x, int y)
        {
            if (pool == null || pool.Count == 0) return null;
            int i = Mathf.Abs(x * 31 + y * 17) % pool.Count;
            return pool[i];
        }
    }
}
