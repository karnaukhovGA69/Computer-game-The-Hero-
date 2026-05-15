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
/// Rebuilds Map.unity ground from MainAssets/Tilemap_color1.png and wires movement
/// to the real GroundTilemap source.
/// </summary>
public static class TheHeroFixTilemapColor1AndMovement
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string MainAssetsRoot = "Assets/ExternalAssets/MainAssets";
    private const string TilemapTexturePath = MainAssetsRoot + "/Tilemap_color1.png";
    private const string WarriorTexturePath = MainAssetsRoot + "/Warrior_Idle.png";
    private const string GeneratedTilesFolder = "Assets/GeneratedTiles/TilemapColor1";
    private const string ReportPath = "Assets/CodeAudit/TilemapColor1_Movement_Fix_Report.md";

    private const int TileSize = 64;
    private const int MapWidth = 48;
    private const int MapHeight = 32;
    private const int HeroStartX = 24;
    private const int HeroStartY = 13;

    private static readonly Vector2Int[] OccupiedTilemapCells =
    {
        new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(3, 0),
        new Vector2Int(5, 0), new Vector2Int(6, 0), new Vector2Int(7, 0), new Vector2Int(8, 0),
        new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(2, 1), new Vector2Int(3, 1),
        new Vector2Int(5, 1), new Vector2Int(6, 1), new Vector2Int(7, 1), new Vector2Int(8, 1),
        new Vector2Int(0, 2), new Vector2Int(1, 2), new Vector2Int(2, 2), new Vector2Int(3, 2),
        new Vector2Int(5, 2), new Vector2Int(6, 2), new Vector2Int(7, 2), new Vector2Int(8, 2),
        new Vector2Int(0, 3), new Vector2Int(1, 3), new Vector2Int(2, 3), new Vector2Int(3, 3),
        new Vector2Int(5, 3), new Vector2Int(6, 3), new Vector2Int(7, 3), new Vector2Int(8, 3),
        new Vector2Int(0, 4), new Vector2Int(3, 4), new Vector2Int(5, 4), new Vector2Int(6, 4),
        new Vector2Int(7, 4), new Vector2Int(8, 4),
        new Vector2Int(0, 5), new Vector2Int(3, 5), new Vector2Int(5, 5), new Vector2Int(6, 5),
        new Vector2Int(7, 5), new Vector2Int(8, 5),
    };

    private sealed class FixReport
    {
        public bool TilemapSliced;
        public bool WarriorSliced;
        public int GroundTileCount;
        public int BlockingTileCount;
        public string GrassCenter;
        public string GrassEdges;
        public string CliffTiles;
        public string HeroSprite;
        public string MovementReferences;
        public readonly List<string> RemovedWholeAtlasSprites = new List<string>();
        public readonly List<string> Errors = new List<string>();
    }

    [MenuItem("The Hero/Map/Fix Tilemap Color1 And Movement")]
    public static void Fix()
    {
        var report = new FixReport();

        if (!AssetFileExists(TilemapTexturePath))
        {
            report.Errors.Add("Missing " + TilemapTexturePath);
            WriteReport(report);
            Debug.LogError("[TheHeroTilemapColor1Fix] Tilemap_color1.png not found.");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            report.Errors.Add("Could not open " + MapScenePath);
            WriteReport(report);
            Debug.LogError("[TheHeroTilemapColor1Fix] Could not open Map.unity.");
            return;
        }

        EnsureGeneratedTileFolders();
        report.TilemapSliced = SliceTilemapColor1();
        report.WarriorSliced = EnsureWarriorIdleSubSprites();

        AssetDatabase.Refresh();

        var sprites = AssetDatabase.LoadAllAssetsAtPath(TilemapTexturePath)
            .OfType<Sprite>()
            .Where(s => s != null)
            .ToDictionary(s => s.name, StringComparer.Ordinal);

        TileSet tiles = CreateTileAssets(sprites, report);
        if (!tiles.IsValid)
        {
            report.Errors.Add("Failed to create Tilemap_color1 tile assets.");
            WriteReport(report);
            Debug.LogError("[TheHeroTilemapColor1Fix] Failed to create tile assets.");
            return;
        }

        GameObject mapRoot = EnsureRoot("MapRoot");
        GameObject gridGo = EnsureChild(mapRoot, "Grid");
        Grid grid = gridGo.GetComponent<Grid>() ?? gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        grid.cellGap = Vector3.zero;
        grid.cellLayout = GridLayout.CellLayout.Rectangle;

        Tilemap ground = FindOrCreateTilemap(gridGo, "GroundTilemap", 0);
        Tilemap blocking = FindOrCreateTilemap(gridGo, "BlockingTilemap", 7);
        Tilemap road = FindTilemap("RoadTilemap");
        Tilemap water = FindTilemap("WaterTilemap");
        Tilemap bridge = FindTilemap("BridgeTilemap");

        ConfigureTilemap(ground);
        ConfigureTilemap(blocking);
        if (road != null) ConfigureTilemap(road);
        if (water != null) ConfigureTilemap(water);
        if (bridge != null) ConfigureTilemap(bridge);

        PaintGround(ground, tiles);
        PaintBlockingEdges(blocking, tiles);

        report.GroundTileCount = CountTiles(ground);
        report.BlockingTileCount = CountTiles(blocking);

        RemoveWholeTilemapColor1Sprites(report);

        THMapGridInput input = EnsureMapGridInput(ground, blocking, water, bridge, road);
        THStrictGridHeroMovement heroMover = FixHero(scene, ground, report);
        FixMapObjectPositions(ground);
        FixMovementReferences(input, heroMover, report);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MapScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        WriteReport(report);

        if (report.Errors.Count == 0)
        {
            Debug.Log("[TheHeroTilemapColor1Fix] Map saved.");
            Debug.Log($"[TheHeroTilemapColor1Fix] GroundTilemap tiles: {report.GroundTileCount}");
            Debug.Log($"[TheHeroTilemapColor1Fix] BlockingTilemap tiles: {report.BlockingTileCount}");
            Debug.Log($"[TheHeroTilemapColor1Fix] Hero sprite: {report.HeroSprite}");
        }
        else
        {
            Debug.LogError("[TheHeroTilemapColor1Fix] Finished with errors. See report.");
        }
    }

    public static void ValidateOnly()
    {
        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        Tilemap ground = FindTilemap("GroundTilemap");
        Tilemap blocking = FindTilemap("BlockingTilemap");
        THMapGridInput input = Object.FindAnyObjectByType<THMapGridInput>();
        THStrictGridHeroMovement mover = Object.FindAnyObjectByType<THStrictGridHeroMovement>();

        int fail = 0;
        fail += Require(scene.IsValid() && scene.isLoaded, "Map scene loaded");
        fail += Require(ground != null && CountTiles(ground) >= MapWidth * MapHeight, "GroundTilemap contains real tiles");
        fail += Require(ground != null && TilemapUsesTexture(ground, TilemapTexturePath), "GroundTilemap uses Tilemap_color1 sub-sprites");
        fail += Require(!SceneUsesWholeTilemapColor1Sprite(), "No whole Tilemap_color1 sprite renderer");
        fail += Require(blocking != null && CountTiles(blocking) > 0, "BlockingTilemap contains impassable cliff tiles");
        fail += Require(input != null && input.GroundTilemap == ground, "THMapGridInput reads GroundTilemap");
        fail += Require(mover != null && mover.currentX == HeroStartX && mover.currentY == HeroStartY, "Hero mover starts on expected grid cell");
        fail += Require(mover != null && HeroUsesWarriorIdle0(mover.gameObject), "Hero uses Warrior_Idle_0 sub-sprite");

        if (fail == 0) Debug.Log("[TheHeroTilemapColor1Fix] Validation PASS");
        else Debug.LogError($"[TheHeroTilemapColor1Fix] Validation FAIL: {fail}");
    }

    private static bool SliceTilemapColor1()
    {
        TextureImporter importer = AssetImporter.GetAtPath(TilemapTexturePath) as TextureImporter;
        if (importer == null) return false;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var rects = new SpriteMetaData[OccupiedTilemapCells.Length];
        for (int i = 0; i < OccupiedTilemapCells.Length; i++)
        {
            Vector2Int cell = OccupiedTilemapCells[i];
            rects[i] = new SpriteMetaData
            {
                name = "Tilemap_color1_" + i,
                rect = new Rect(cell.x * TileSize, (5 - cell.y) * TileSize, TileSize, TileSize),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }

#pragma warning disable CS0618
        importer.spritesheet = rects;
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return true;
    }

    private static bool EnsureWarriorIdleSubSprites()
    {
        if (!AssetFileExists(WarriorTexturePath))
            return false;

        Sprite existing = LoadWarriorIdle0();
        if (existing != null)
            return true;

        TextureImporter importer = AssetImporter.GetAtPath(WarriorTexturePath) as TextureImporter;
        if (importer == null) return false;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        var rects = new SpriteMetaData[8];
        for (int i = 0; i < rects.Length; i++)
        {
            rects[i] = new SpriteMetaData
            {
                name = "Warrior_Idle_" + i,
                rect = new Rect(i * 192, 0, 192, 192),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }

#pragma warning disable CS0618
        importer.spritesheet = rects;
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return LoadWarriorIdle0() != null;
    }

    private static TileSet CreateTileAssets(Dictionary<string, Sprite> sprites, FixReport report)
    {
        var set = new TileSet
        {
            GrassCenter = CreateTileAsset("Ground_Grass_Center", Pick(sprites, "Tilemap_color1_9"), Tile.ColliderType.None),
            GrassTopLeft = CreateTileAsset("Ground_Grass_TopLeft", Pick(sprites, "Tilemap_color1_0"), Tile.ColliderType.None),
            GrassTopA = CreateTileAsset("Ground_Grass_Top_A", Pick(sprites, "Tilemap_color1_1"), Tile.ColliderType.None),
            GrassTopB = CreateTileAsset("Ground_Grass_Top_B", Pick(sprites, "Tilemap_color1_2"), Tile.ColliderType.None),
            GrassTopRight = CreateTileAsset("Ground_Grass_TopRight", Pick(sprites, "Tilemap_color1_3"), Tile.ColliderType.None),
            GrassLeftA = CreateTileAsset("Ground_Grass_Left_A", Pick(sprites, "Tilemap_color1_8"), Tile.ColliderType.None),
            GrassLeftB = CreateTileAsset("Ground_Grass_Left_B", Pick(sprites, "Tilemap_color1_16"), Tile.ColliderType.None),
            GrassRightA = CreateTileAsset("Ground_Grass_Right_A", Pick(sprites, "Tilemap_color1_11"), Tile.ColliderType.None),
            GrassRightB = CreateTileAsset("Ground_Grass_Right_B", Pick(sprites, "Tilemap_color1_19"), Tile.ColliderType.None),
            GrassBottomLeft = CreateTileAsset("Ground_Grass_BottomLeft", Pick(sprites, "Tilemap_color1_24"), Tile.ColliderType.None),
            GrassBottomA = CreateTileAsset("Ground_Grass_Bottom_A", Pick(sprites, "Tilemap_color1_25"), Tile.ColliderType.None),
            GrassBottomB = CreateTileAsset("Ground_Grass_Bottom_B", Pick(sprites, "Tilemap_color1_26"), Tile.ColliderType.None),
            GrassBottomRight = CreateTileAsset("Ground_Grass_BottomRight", Pick(sprites, "Tilemap_color1_27"), Tile.ColliderType.None),
            CliffLeft = CreateTileAsset("Blocking_Cliff_Left", Pick(sprites, "Tilemap_color1_34"), Tile.ColliderType.Sprite),
            CliffA = CreateTileAsset("Blocking_Cliff_A", Pick(sprites, "Tilemap_color1_35"), Tile.ColliderType.Sprite),
            CliffB = CreateTileAsset("Blocking_Cliff_B", Pick(sprites, "Tilemap_color1_36"), Tile.ColliderType.Sprite),
            CliffRight = CreateTileAsset("Blocking_Cliff_Right", Pick(sprites, "Tilemap_color1_37"), Tile.ColliderType.Sprite),
            CliffBottomLeft = CreateTileAsset("Blocking_Cliff_BottomLeft", Pick(sprites, "Tilemap_color1_40"), Tile.ColliderType.Sprite),
            CliffBottomA = CreateTileAsset("Blocking_Cliff_Bottom_A", Pick(sprites, "Tilemap_color1_41"), Tile.ColliderType.Sprite),
            CliffBottomB = CreateTileAsset("Blocking_Cliff_Bottom_B", Pick(sprites, "Tilemap_color1_42"), Tile.ColliderType.Sprite),
            CliffBottomRight = CreateTileAsset("Blocking_Cliff_BottomRight", Pick(sprites, "Tilemap_color1_43"), Tile.ColliderType.Sprite),
        };

        report.GrassCenter = "Tilemap_color1_9";
        report.GrassEdges = "corners 0/3/24/27, sides 1/2/8/16/11/19/25/26";
        report.CliffTiles = "Tilemap_color1_34-37 and 40-43";
        return set;
    }

    private static Sprite Pick(Dictionary<string, Sprite> sprites, string name)
    {
        sprites.TryGetValue(name, out Sprite sprite);
        return sprite;
    }

    private static Tile CreateTileAsset(string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        if (sprite == null) return null;

        string path = GeneratedTilesFolder + "/" + name + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }

        tile.name = name;
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        tile.color = Color.white;
        tile.transform = Matrix4x4.identity;
        tile.flags = TileFlags.LockColor;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    private static void PaintGround(Tilemap ground, TileSet tiles)
    {
        ground.ClearAllTiles();
        ground.origin = Vector3Int.zero;
        ground.size = new Vector3Int(MapWidth, MapHeight, 1);

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                ground.SetTile(new Vector3Int(x, y, 0), PickGroundTile(x, y, tiles));
            }
        }

        ground.RefreshAllTiles();
        ground.CompressBounds();
        EditorUtility.SetDirty(ground);
    }

    private static TileBase PickGroundTile(int x, int y, TileSet t)
    {
        bool left = x == 0;
        bool right = x == MapWidth - 1;
        bool bottom = y == 0;
        bool top = y == MapHeight - 1;

        if (left && top) return t.GrassTopLeft;
        if (right && top) return t.GrassTopRight;
        if (left && bottom) return t.GrassBottomLeft;
        if (right && bottom) return t.GrassBottomRight;
        if (top) return x % 2 == 0 ? t.GrassTopA : t.GrassTopB;
        if (bottom) return x % 2 == 0 ? t.GrassBottomA : t.GrassBottomB;
        if (left) return y % 2 == 0 ? t.GrassLeftA : t.GrassLeftB;
        if (right) return y % 2 == 0 ? t.GrassRightA : t.GrassRightB;
        return t.GrassCenter;
    }

    private static void PaintBlockingEdges(Tilemap blocking, TileSet tiles)
    {
        blocking.ClearAllTiles();
        blocking.origin = Vector3Int.zero;
        blocking.size = new Vector3Int(MapWidth, MapHeight, 1);

        for (int x = 0; x < MapWidth; x++)
        {
            TileBase tile = x == 0 ? tiles.CliffLeft :
                x == MapWidth - 1 ? tiles.CliffRight :
                x % 2 == 0 ? tiles.CliffA : tiles.CliffB;
            blocking.SetTile(new Vector3Int(x, 0, 0), tile);
        }

        for (int x = 0; x < MapWidth; x++)
        {
            TileBase tile = x == 0 ? tiles.CliffBottomLeft :
                x == MapWidth - 1 ? tiles.CliffBottomRight :
                x % 2 == 0 ? tiles.CliffBottomA : tiles.CliffBottomB;
            blocking.SetTile(new Vector3Int(x, MapHeight - 1, 0), tile);
        }

        for (int y = 1; y < MapHeight - 1; y++)
        {
            blocking.SetTile(new Vector3Int(0, y, 0), y % 2 == 0 ? tiles.CliffLeft : tiles.CliffBottomLeft);
            blocking.SetTile(new Vector3Int(MapWidth - 1, y, 0), y % 2 == 0 ? tiles.CliffRight : tiles.CliffBottomRight);
        }

        blocking.RefreshAllTiles();
        blocking.CompressBounds();
        EditorUtility.SetDirty(blocking);
    }

    private static THMapGridInput EnsureMapGridInput(Tilemap ground, Tilemap blocking, Tilemap water, Tilemap bridge, Tilemap road)
    {
        GameObject controllerGo = GameObject.Find("MapController") ?? new GameObject("MapController");
        THMapGridInput input = controllerGo.GetComponent<THMapGridInput>() ?? controllerGo.AddComponent<THMapGridInput>();
        input.GroundTilemap = ground;
        input.BlockingTilemap = blocking;
        input.WaterTilemap = water;
        input.BridgeTilemap = bridge;
        input.RoadTilemap = road;
        EditorUtility.SetDirty(input);
        return input;
    }

    private static THStrictGridHeroMovement FixHero(Scene scene, Tilemap ground, FixReport report)
    {
        Sprite heroSprite = LoadWarriorIdle0();
        report.HeroSprite = heroSprite != null ? heroSprite.name : "(missing)";

        GameObject hero = FindOrCreateHero();
        hero.name = "Hero";
        hero.SetActive(true);
        hero.transform.position = CellCenter(ground, HeroStartX, HeroStartY);
        hero.transform.localScale = Vector3.one;

        THStrictGridHeroMovement mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
        mover.currentX = HeroStartX;
        mover.currentY = HeroStartY;
        mover.enabled = true;

        SpriteRenderer primary = GetOrCreatePrimaryHeroRenderer(hero);
        primary.sprite = heroSprite;
        primary.enabled = heroSprite != null;
        primary.sortingOrder = 100;

        if (heroSprite != null)
        {
            float dim = Mathf.Max(heroSprite.bounds.size.x, heroSprite.bounds.size.y);
            float scale = 0.95f / Mathf.Max(0.01f, dim);
            primary.transform.localPosition = Vector3.zero;
            primary.transform.localScale = new Vector3(scale, scale, 1f);
        }

        foreach (SpriteRenderer renderer in hero.GetComponentsInChildren<SpriteRenderer>(true))
        {
            if (renderer == primary) continue;
            renderer.enabled = false;
            EditorUtility.SetDirty(renderer);
        }

        BoxCollider2D collider = hero.GetComponent<BoxCollider2D>() ?? hero.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.7f, 0.7f);
        collider.offset = Vector2.zero;
        collider.isTrigger = false;

        EditorUtility.SetDirty(hero);
        EditorUtility.SetDirty(mover);
        EditorUtility.SetDirty(primary);
        EditorSceneManager.MarkSceneDirty(scene);
        return mover;
    }

    private static GameObject FindOrCreateHero()
    {
        THStrictGridHeroMovement mover = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include)
            .OrderByDescending(m => m.name == "Hero")
            .FirstOrDefault();

        if (mover != null) return mover.gameObject;

        GameObject hero = GameObject.Find("Hero");
        if (hero != null) return hero;

        GameObject mapRoot = EnsureRoot("MapRoot");
        GameObject objectsRoot = EnsureChild(mapRoot, "ObjectsRoot");
        hero = new GameObject("Hero");
        hero.transform.SetParent(objectsRoot.transform, false);
        return hero;
    }

    private static SpriteRenderer GetOrCreatePrimaryHeroRenderer(GameObject hero)
    {
        SpriteRenderer renderer = hero.GetComponentsInChildren<SpriteRenderer>(true)
            .OrderByDescending(r => r.gameObject.name == "Visual")
            .FirstOrDefault();
        if (renderer != null) return renderer;

        GameObject visual = new GameObject("Visual");
        visual.transform.SetParent(hero.transform, false);
        visual.transform.localPosition = Vector3.zero;
        return visual.AddComponent<SpriteRenderer>();
    }

    private static Sprite LoadWarriorIdle0()
    {
        return AssetDatabase.LoadAllAssetsAtPath(WarriorTexturePath)
            .OfType<Sprite>()
            .Where(s => s != null && s.texture != null)
            .Where(s => s.name == "Warrior_Idle_0" || s.name.StartsWith("Warrior_Idle_", StringComparison.Ordinal))
            .Where(s => s.rect.width < s.texture.width || s.rect.height < s.texture.height)
            .OrderBy(s => s.name == "Warrior_Idle_0" ? 0 : 1)
            .ThenBy(s => s.name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static void FixMapObjectPositions(Tilemap ground)
    {
        foreach (THMapObject obj in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
        {
            if (obj == null) continue;
            obj.transform.position = CellCenter(ground, obj.targetX, obj.targetY);
            EditorUtility.SetDirty(obj.transform);
        }
    }

    private static void FixMovementReferences(THMapGridInput input, THStrictGridHeroMovement heroMover, FixReport report)
    {
        foreach (THMapController controller in Object.FindObjectsByType<THMapController>(FindObjectsInactive.Include))
        {
            controller.HeroMover = heroMover;
            EditorUtility.SetDirty(controller);
        }

        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null && heroMover != null)
        {
            THCameraFollow follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
            follow.Target = heroMover.transform;
            follow.followSpeed = 8f;
            follow.z = -10f;
            cam.orthographic = true;
            cam.transform.position = new Vector3(heroMover.transform.position.x, heroMover.transform.position.y, -10f);
            EditorUtility.SetDirty(follow);
            EditorUtility.SetDirty(cam);
        }

        report.MovementReferences = input != null && heroMover != null
            ? "THMapGridInput.GroundTilemap -> GroundTilemap; THMapController.HeroMover -> Hero/THStrictGridHeroMovement."
            : "Movement references incomplete.";
    }

    private static void RemoveWholeTilemapColor1Sprites(FixReport report)
    {
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            Sprite sprite = renderer.sprite;
            if (sprite == null || sprite.texture == null) continue;
            if (AssetDatabase.GetAssetPath(sprite.texture) != TilemapTexturePath) continue;

            bool whole =
                Mathf.Approximately(sprite.rect.width, sprite.texture.width) &&
                Mathf.Approximately(sprite.rect.height, sprite.texture.height);
            if (!whole) continue;

            report.RemovedWholeAtlasSprites.Add(renderer.gameObject.name);
            renderer.enabled = false;
            renderer.sprite = null;
            EditorUtility.SetDirty(renderer);
        }
    }

    private static Tilemap FindOrCreateTilemap(GameObject gridGo, string name, int sortingOrder)
    {
        Tilemap tilemap = FindTilemap(name);
        GameObject go;
        if (tilemap == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(gridGo.transform, false);
            tilemap = go.AddComponent<Tilemap>();
        }
        else
        {
            go = tilemap.gameObject;
            if (go.transform.parent != gridGo.transform)
                go.transform.SetParent(gridGo.transform, false);
        }

        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>() ?? go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(renderer);
        return tilemap;
    }

    private static Tilemap FindTilemap(string name)
    {
        return Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
            .FirstOrDefault(t => t != null && t.name == name);
    }

    private static void ConfigureTilemap(Tilemap tilemap)
    {
        if (tilemap == null) return;
        tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
        tilemap.orientation = Tilemap.Orientation.XY;
        tilemap.color = Color.white;
        tilemap.transform.localPosition = Vector3.zero;
        tilemap.transform.localScale = Vector3.one;
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(tilemap.transform);
    }

    private static Vector3 CellCenter(Tilemap tilemap, int x, int y)
    {
        if (tilemap == null) return new Vector3(x + 0.5f, y + 0.5f, 0f);
        Vector3 center = tilemap.GetCellCenterWorld(new Vector3Int(x, y, 0));
        center.z = 0f;
        return center;
    }

    private static int CountTiles(Tilemap tilemap)
    {
        if (tilemap == null) return 0;
        int count = 0;
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(cell)) count++;
        }
        return count;
    }

    private static bool TilemapUsesTexture(Tilemap tilemap, string texturePath)
    {
        if (tilemap == null) return false;
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            TileBase tile = tilemap.GetTile(cell);
            if (tile is Tile unityTile && unityTile.sprite != null && unityTile.sprite.texture != null)
            {
                if (AssetDatabase.GetAssetPath(unityTile.sprite.texture) == texturePath)
                    return true;
            }
        }
        return false;
    }

    private static bool SceneUsesWholeTilemapColor1Sprite()
    {
        foreach (SpriteRenderer renderer in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include))
        {
            Sprite sprite = renderer.sprite;
            if (sprite == null || sprite.texture == null) continue;
            if (AssetDatabase.GetAssetPath(sprite.texture) != TilemapTexturePath) continue;
            if (Mathf.Approximately(sprite.rect.width, sprite.texture.width) &&
                Mathf.Approximately(sprite.rect.height, sprite.texture.height))
                return true;
        }
        return false;
    }

    private static bool HeroUsesWarriorIdle0(GameObject hero)
    {
        if (hero == null) return false;
        SpriteRenderer renderer = hero.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(r => r.enabled);
        if (renderer == null || renderer.sprite == null || renderer.sprite.texture == null) return false;
        return renderer.sprite.name == "Warrior_Idle_0" &&
               AssetDatabase.GetAssetPath(renderer.sprite.texture) == WarriorTexturePath;
    }

    private static int Require(bool condition, string label)
    {
        if (condition)
        {
            Debug.Log("[TheHeroTilemapColor1Fix] PASS " + label);
            return 0;
        }

        Debug.LogError("[TheHeroTilemapColor1Fix] FAIL " + label);
        return 1;
    }

    private static void EnsureGeneratedTileFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/GeneratedTiles"))
            AssetDatabase.CreateFolder("Assets", "GeneratedTiles");
        if (!AssetDatabase.IsValidFolder(GeneratedTilesFolder))
            AssetDatabase.CreateFolder("Assets/GeneratedTiles", "TilemapColor1");
    }

    private static GameObject EnsureRoot(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    private static GameObject EnsureChild(GameObject parent, string name)
    {
        Transform existing = parent.transform.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static bool AssetFileExists(string assetPath)
    {
        string fullPath = Path.Combine(Application.dataPath, "..", assetPath);
        return File.Exists(fullPath);
    }

    private static void WriteReport(FixReport report)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));

        var sb = new StringBuilder();
        sb.AppendLine("# Tilemap Color1 Movement Fix Report");
        sb.AppendLine();
        sb.AppendLine("## Tilemap_color1.png");
        sb.AppendLine("- Sliced grid: 64x64.");
        sb.AppendLine("- Sprite import mode: Multiple.");
        sb.AppendLine("- Base grass center: `" + (report.GrassCenter ?? "Tilemap_color1_9") + "`.");
        sb.AppendLine("- Edge/corner grass tiles: `" + (report.GrassEdges ?? "") + "`.");
        sb.AppendLine("- Blocking cliff tiles: `" + (report.CliffTiles ?? "") + "`.");
        sb.AppendLine();
        sb.AppendLine("## Scene");
        sb.AppendLine("- GroundTilemap tile count: " + report.GroundTileCount + ".");
        sb.AppendLine("- BlockingTilemap tile count: " + report.BlockingTileCount + ".");
        sb.AppendLine("- Removed whole-atlas SpriteRenderers: " + report.RemovedWholeAtlasSprites.Count + ".");
        foreach (string removed in report.RemovedWholeAtlasSprites)
            sb.AppendLine("  - " + removed);
        sb.AppendLine();
        sb.AppendLine("## Hero");
        sb.AppendLine("- Sprite: `" + (report.HeroSprite ?? "(none)") + "` from `" + WarriorTexturePath + "`.");
        sb.AppendLine();
        sb.AppendLine("## Movement");
        sb.AppendLine("- " + (report.MovementReferences ?? "Not updated."));
        if (report.Errors.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine("## Errors");
            foreach (string error in report.Errors)
                sb.AppendLine("- " + error);
        }

        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/TilemapColor1_Movement_Fix_Report.md"), sb.ToString());
    }

    private sealed class TileSet
    {
        public Tile GrassCenter;
        public Tile GrassTopLeft, GrassTopA, GrassTopB, GrassTopRight;
        public Tile GrassLeftA, GrassLeftB, GrassRightA, GrassRightB;
        public Tile GrassBottomLeft, GrassBottomA, GrassBottomB, GrassBottomRight;
        public Tile CliffLeft, CliffA, CliffB, CliffRight;
        public Tile CliffBottomLeft, CliffBottomA, CliffBottomB, CliffBottomRight;

        public bool IsValid =>
            GrassCenter != null &&
            GrassTopLeft != null && GrassTopA != null && GrassTopB != null && GrassTopRight != null &&
            GrassLeftA != null && GrassLeftB != null && GrassRightA != null && GrassRightB != null &&
            GrassBottomLeft != null && GrassBottomA != null && GrassBottomB != null && GrassBottomRight != null &&
            CliffLeft != null && CliffA != null && CliffB != null && CliffRight != null &&
            CliffBottomLeft != null && CliffBottomA != null && CliffBottomB != null && CliffBottomRight != null;
    }
}
