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
/// Fixes only the final three fallback validation failures in Assets/Scenes/Map.unity:
/// GroundTilemap tile count, RoadTilemap tile count, and Hero Warrior_Idle sub-sprite.
/// </summary>
public static class TheHeroFixFinalThreeMapValidationFails
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string TileFolder = "Assets/GeneratedTiles";
    private const string ReportPath = "Assets/CodeAudit/Final_3_Map_Validation_Fix_Report.md";
    private const string GroundTilePath = "Assets/GeneratedTiles/Ground_Grass.asset";
    private const string RoadTilePath = "Assets/GeneratedTiles/Road_Path.asset";
    private const string MainRoot = "Assets/ExternalAssets/MainAssets";

    private const int MapWidth = 48;
    private const int MapHeight = 32;
    private const int CenterX = 24;
    private const int CenterY = 16;
    private const int HeroFrameSize = 192;

    [MenuItem("The Hero/Map/Fix Final 3 Map Validation Fails")]
    public static void Fix()
    {
        if (!File.Exists(MapScenePath))
        {
            Debug.LogError($"[TheHeroFinal3Fix] Map scene not found: {MapScenePath}");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError($"[TheHeroFinal3Fix] Failed to open {MapScenePath}");
            return;
        }

        Debug.Log("[TheHeroFinal3Fix] Opened Map.unity");

        EnsureGeneratedTileFolder();

        int groundCount = FillGroundTilemap(scene);
        SaveCheckpoint(scene, "GroundTilemap");

        int roadCount = FillRoadTilemap(scene);
        SaveCheckpoint(scene, "RoadTilemap");

        HeroFixResult heroResult = FixHeroSprite(scene);
        SaveCheckpoint(scene, "Hero sprite");

        bool finalOk = groundCount > 100 && roadCount > 0 && heroResult.Pass;
        if (groundCount == 0)
            Debug.LogError("[TheHeroFinal3Fix] GroundTilemap still has 0 tiles after fill");
        if (roadCount == 0)
            Debug.LogError("[TheHeroFinal3Fix] RoadTilemap still has 0 tiles after fill");
        if (!heroResult.Pass)
            Debug.LogError($"[TheHeroFinal3Fix] Hero sub-sprite still invalid: {heroResult.Error}");

        SaveCheckpoint(scene, "Final");
        Debug.Log("[TheHeroFinal3Fix] Map saved");

        WriteReport(groundCount, roadCount, heroResult, finalOk);
    }

    private static int FillGroundTilemap(Scene scene)
    {
        Tilemap ground = FindOrCreateTilemap("GroundTilemap", 0);
        if (ground == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] Could not find or create GroundTilemap");
            return 0;
        }

        Sprite grassSprite =
            LoadFirstStrictSubSprite($"{MainRoot}/Tilemap_color1.png") ??
            LoadFirstStrictSubSprite($"{MainRoot}/TX Tileset Grass.png") ??
            LoadFirstStrictSubSprite($"{MainRoot}/free_pixel_16_woods.png") ??
            LoadCainosStrictSubSprite("grass", "ground", "tile");

        if (grassSprite == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] No valid grass sub-sprite found for GroundTilemap");
            return 0;
        }

        Tile groundTile = CreateOrUpdateTile(GroundTilePath, "Ground_Grass", grassSprite);
        if (groundTile == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] Failed to create/update Ground_Grass.asset");
            return 0;
        }

        ground.ClearAllTiles();
        ground.origin = new Vector3Int(0, 0, 0);
        ground.size = new Vector3Int(MapWidth, MapHeight, 1);

        for (int x = 0; x < MapWidth; x++)
        {
            for (int y = 0; y < MapHeight; y++)
            {
                ground.SetTile(new Vector3Int(x, y, 0), groundTile);
            }
        }

        ground.RefreshAllTiles();
        ground.CompressBounds();
        EditorUtility.SetDirty(ground);
        EditorSceneManager.MarkSceneDirty(scene);

        int count = CountTilesInBounds(ground);
        Debug.Log($"[TheHeroFinal3Fix] GroundTilemap tile count after fill: {count}");
        return count;
    }

    private static int FillRoadTilemap(Scene scene)
    {
        Tilemap road = FindOrCreateTilemap("RoadTilemap", 2);
        if (road == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] Could not find or create RoadTilemap");
            return 0;
        }

        bool colorFallback = false;
        Sprite roadSprite =
            LoadNamedStrictSubSprite($"{MainRoot}/Tilemap_color1.png", "road", "path", "dirt", "stone", "floor") ??
            LoadNamedStrictSubSprite($"{MainRoot}/Main_tiles.png", "road", "path", "dirt", "stone", "floor") ??
            LoadNamedStrictSubSprite($"{MainRoot}/walls_floor.png", "road", "path", "dirt", "stone", "floor") ??
            LoadNamedStrictSubSprite($"{MainRoot}/ground_grass_details.png", "road", "path", "dirt", "stone", "floor") ??
            LoadCainosStrictSubSprite("road", "path", "dirt", "stone", "floor");

        if (roadSprite == null)
        {
            roadSprite =
                LoadFirstStrictSubSprite($"{MainRoot}/Tilemap_color1.png") ??
                LoadFirstStrictSubSprite($"{MainRoot}/TX Tileset Grass.png") ??
                LoadFirstStrictSubSprite($"{MainRoot}/Main_tiles.png") ??
                LoadCainosStrictSubSprite("grass", "ground", "tile");
            colorFallback = true;
        }

        if (roadSprite == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] No valid road/floor sub-sprite found for RoadTilemap");
            return 0;
        }

        Tile roadTile = CreateOrUpdateTile(RoadTilePath, "Road_Path", roadSprite);
        if (roadTile == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] Failed to create/update Road_Path.asset");
            return 0;
        }

        road.ClearAllTiles();
        road.origin = new Vector3Int(0, 0, 0);
        road.size = new Vector3Int(MapWidth, MapHeight, 1);
        road.color = colorFallback ? new Color(0.75f, 0.65f, 0.45f, 1f) : Color.white;

        for (int x = 12; x <= 36; x++)
            road.SetTile(new Vector3Int(x, CenterY, 0), roadTile);

        for (int y = 8; y <= 24; y++)
            road.SetTile(new Vector3Int(CenterX, y, 0), roadTile);

        road.RefreshAllTiles();
        road.CompressBounds();
        EditorUtility.SetDirty(road);
        EditorSceneManager.MarkSceneDirty(scene);

        int count = CountTilesInBounds(road);
        Debug.Log($"[TheHeroFinal3Fix] RoadTilemap tile count after fill: {count}");
        return count;
    }

    private static HeroFixResult FixHeroSprite(Scene scene)
    {
        string warriorPath = FindWarriorIdlePath();
        if (string.IsNullOrEmpty(warriorPath))
        {
            Debug.LogError("[TheHeroFinal3Fix] Warrior_Idle.png not found through AssetDatabase.FindAssets");
            return HeroFixResult.Fail("Warrior_Idle.png not found");
        }

        EnsureSpriteImporterSettings(warriorPath);

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(warriorPath);
        List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(warriorPath)
            .OfType<Sprite>()
            .Where(s => s != null)
            .ToList();

        Debug.Log($"[TheHeroFinal3Fix] Warrior_Idle sprites found: {sprites.Count}");
        foreach (Sprite sprite in sprites)
        {
            Texture2D spriteTexture = sprite.texture;
            string textureSize = spriteTexture != null ? $"{spriteTexture.width}x{spriteTexture.height}" : "0x0";
            Debug.Log($"[TheHeroFinal3Fix] Warrior_Idle sprite {sprite.name} rect={sprite.rect.width:0}x{sprite.rect.height:0} texture={textureSize}");
        }

        Sprite picked = PickWarriorIdleFrame(sprites);
        bool usedFallback = false;

        if (picked == null && sprites.Count == 1 && texture != null)
        {
            Sprite only = sprites[0];
            bool wholeWarriorSheet =
                Mathf.Approximately(only.rect.width, texture.width) &&
                Mathf.Approximately(only.rect.height, texture.height) &&
                texture.width == 1536 &&
                texture.height == 192;

            if (wholeWarriorSheet)
            {
                picked = Sprite.Create(
                    texture,
                    new Rect(0, 0, HeroFrameSize, HeroFrameSize),
                    new Vector2(0.5f, 0.5f),
                    64f,
                    0,
                    SpriteMeshType.FullRect);
                picked.name = "Warrior_Idle_RuntimeFrame0";
                usedFallback = true;
            }
        }

        if (picked == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] Could not select a valid Warrior_Idle sub-sprite or fallback frame");
            return HeroFixResult.Fail("No valid Warrior_Idle frame");
        }

        GameObject hero = FindActiveHero();
        if (hero == null)
        {
            Debug.LogError("[TheHeroFinal3Fix] Active Hero with THStrictGridHeroMovement not found");
            return HeroFixResult.Fail("Hero not found");
        }

        List<SpriteRenderer> renderers = hero.GetComponentsInChildren<SpriteRenderer>(true)
            .Where(r => r != null)
            .Distinct()
            .ToList();

        if (renderers.Count == 0)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(hero.transform, false);
            visual.transform.localPosition = Vector3.zero;
            renderers.Add(visual.AddComponent<SpriteRenderer>());
        }

        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.sprite = picked;
            renderer.enabled = true;
            renderer.sortingOrder = 100;
            renderer.transform.localScale = new Vector3(0.33f, 0.33f, 1f);
            EditorUtility.SetDirty(renderer);
            EditorUtility.SetDirty(renderer.transform);
            EditorUtility.SetDirty(renderer.gameObject);
        }

        EditorSceneManager.MarkSceneDirty(scene);

        string mode = usedFallback ? "Sprite.Create fallback" : "imported sub-sprite";
        Debug.Log($"[TheHeroFinal3Fix] Hero sprite assigned from Warrior_Idle frame 0 ({mode})");

        bool pass = IsValidWarriorIdleHeroSprite(picked, out string validationDetail);
        return new HeroFixResult
        {
            Pass = pass,
            SpriteName = picked.name,
            TexturePath = warriorPath,
            Rect = picked.rect,
            TextureSize = texture != null ? new Vector2(texture.width, texture.height) : Vector2.zero,
            UsedFallback = usedFallback,
            Error = pass ? null : validationDetail
        };
    }

    private static string FindWarriorIdlePath()
    {
        string preferred = $"{MainRoot}/Warrior_Idle.png";
        if (File.Exists(preferred))
            return preferred;

        return AssetDatabase.FindAssets("Warrior_Idle t:Texture2D")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith("Warrior_Idle.png", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Replace("\\", "/").Equals(preferred, StringComparison.OrdinalIgnoreCase))
            .ThenByDescending(p => p.Replace("\\", "/").Contains("/ExternalAssets/MainAssets/"))
            .FirstOrDefault();
    }

    private static void EnsureSpriteImporterSettings(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError($"[TheHeroFinal3Fix] TextureImporter missing for {path}");
            return;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }

        if (importer.spritePixelsPerUnit <= 0)
        {
            importer.spritePixelsPerUnit = 64;
            changed = true;
        }

        if (changed)
        {
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        }
    }

    private static Sprite PickWarriorIdleFrame(List<Sprite> sprites)
    {
        if (sprites == null || sprites.Count == 0)
            return null;

        return sprites
            .Where(IsSubSprite)
            .OrderByDescending(s => Mathf.Approximately(s.rect.width, HeroFrameSize) && Mathf.Approximately(s.rect.height, HeroFrameSize))
            .ThenByDescending(s => s.name.EndsWith("_0", StringComparison.Ordinal) || s.name.EndsWith(" 0", StringComparison.Ordinal))
            .ThenBy(s => s.name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static bool IsSubSprite(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return false;

        return sprite.rect.width < sprite.texture.width || sprite.rect.height < sprite.texture.height;
    }

    private static bool IsValidWarriorIdleHeroSprite(Sprite sprite, out string detail)
    {
        detail = string.Empty;
        if (sprite == null)
        {
            detail = "sprite is null";
            return false;
        }

        Texture2D texture = sprite.texture;
        if (texture == null)
        {
            detail = "texture is null";
            return false;
        }

        string spritePath = AssetDatabase.GetAssetPath(sprite);
        string texturePath = AssetDatabase.GetAssetPath(texture);
        bool warriorTexture =
            texture.name.IndexOf("Warrior_Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
            spritePath.IndexOf("Warrior_Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
            texturePath.IndexOf("Warrior_Idle", StringComparison.OrdinalIgnoreCase) >= 0;

        bool frameSized = sprite.rect.width <= HeroFrameSize && sprite.rect.height <= HeroFrameSize;
        bool whole1536Sheet =
            Mathf.Approximately(sprite.rect.width, 1536f) &&
            Mathf.Approximately(sprite.rect.height, 192f) &&
            texture.width == 1536 &&
            texture.height == 192;

        if (!warriorTexture)
            detail = $"texture/path is not Warrior_Idle ({texture.name}, {texturePath})";
        else if (!frameSized)
            detail = $"sprite rect too large ({sprite.rect.width}x{sprite.rect.height})";
        else if (whole1536Sheet)
            detail = "sprite is the whole 1536x192 Warrior_Idle sheet";

        return warriorTexture && frameSized && !whole1536Sheet;
    }

    private static Tilemap FindOrCreateTilemap(string name, int sortingOrder)
    {
        GameObject grid = EnsureGrid();
        Transform child = grid.transform.Find(name);
        Tilemap tilemap = child != null ? child.GetComponent<Tilemap>() : null;

        if (tilemap == null)
        {
            tilemap = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
                .FirstOrDefault(t => t.name == name);
        }

        GameObject go;
        if (tilemap == null)
        {
            go = new GameObject(name);
            go.transform.SetParent(grid.transform, false);
            tilemap = go.AddComponent<Tilemap>();
        }
        else
        {
            go = tilemap.gameObject;
            if (go.transform.parent != grid.transform)
                go.transform.SetParent(grid.transform, false);
        }

        TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
        if (renderer == null)
            renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;

        EditorUtility.SetDirty(go);
        EditorUtility.SetDirty(tilemap);
        EditorUtility.SetDirty(renderer);
        return tilemap;
    }

    private static GameObject EnsureGrid()
    {
        Grid grid = Object.FindObjectsByType<Grid>(FindObjectsInactive.Include)
            .OrderBy(g => g.name == "Grid" ? 0 : 1)
            .FirstOrDefault();

        if (grid != null)
            return grid.gameObject;

        GameObject gridGo = new GameObject("Grid");
        grid = gridGo.AddComponent<Grid>();
        grid.cellSize = Vector3.one;
        EditorUtility.SetDirty(gridGo);
        return gridGo;
    }

    private static Tile CreateOrUpdateTile(string path, string name, Sprite sprite)
    {
        if (sprite == null)
            return null;

        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            tile.name = name;
            tile.sprite = sprite;
            AssetDatabase.CreateAsset(tile, path);
        }
        else
        {
            tile.name = name;
            tile.sprite = sprite;
            EditorUtility.SetDirty(tile);
        }

        tile.colliderType = Tile.ColliderType.Sprite;
        AssetDatabase.SaveAssets();
        return tile;
    }

    private static Sprite LoadFirstStrictSubSprite(string path)
    {
        return LoadStrictSubSprites(path).FirstOrDefault();
    }

    private static Sprite LoadNamedStrictSubSprite(string path, params string[] tokens)
    {
        List<Sprite> sprites = LoadStrictSubSprites(path);
        foreach (string token in tokens)
        {
            Sprite hit = sprites.FirstOrDefault(s =>
                s.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0 ||
                path.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
            if (hit != null)
                return hit;
        }

        return sprites.FirstOrDefault();
    }

    private static List<Sprite> LoadStrictSubSprites(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return new List<Sprite>();

        return AssetDatabase.LoadAllAssetsAtPath(path)
            .OfType<Sprite>()
            .Where(s => s != null && s.texture != null)
            .Where(s => s.rect.width < s.texture.width || s.rect.height < s.texture.height)
            .OrderBy(s => s.name, StringComparer.Ordinal)
            .ToList();
    }

    private static Sprite LoadCainosStrictSubSprite(params string[] tokens)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Cainos" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Sprite picked = LoadNamedStrictSubSprite(path, tokens);
            if (picked != null)
                return picked;
        }

        return null;
    }

    private static GameObject FindActiveHero()
    {
        THStrictGridHeroMovement mover = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include)
            .OrderByDescending(h => h.name == "Hero")
            .FirstOrDefault();

        if (mover != null)
            return mover.gameObject;

        return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
            .FirstOrDefault(g => g.name == "Hero" || g.name == "PlayerHero" || g.name == "THHero");
    }

    private static int CountTilesInBounds(Tilemap tilemap)
    {
        if (tilemap == null)
            return 0;

        BoundsInt bounds = tilemap.cellBounds;
        int count = 0;
        foreach (Vector3Int position in bounds.allPositionsWithin)
        {
            if (tilemap.GetTile(position) != null)
                count++;
        }

        return count;
    }

    private static void EnsureGeneratedTileFolder()
    {
        if (!AssetDatabase.IsValidFolder(TileFolder))
            AssetDatabase.CreateFolder("Assets", "GeneratedTiles");
    }

    private static void SaveCheckpoint(Scene scene, string label)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
            Debug.LogError($"[TheHeroFinal3Fix] Failed to save Map.unity after {label}");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    private static void WriteReport(int groundCount, int roadCount, HeroFixResult hero, bool validationPass)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath) ?? "Assets/CodeAudit");

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("# Final 3 Map Validation Fix Report");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## GroundTilemap");
        sb.AppendLine($"- Real tile count: **{groundCount}**.");
        sb.AppendLine($"- Expected minimum: >100; target fill: {MapWidth * MapHeight}.");
        sb.AppendLine($"- Tile asset: `{GroundTilePath}`.");
        sb.AppendLine();
        sb.AppendLine("## RoadTilemap");
        sb.AppendLine($"- Real tile count: **{roadCount}**.");
        sb.AppendLine("- Expected minimum: >0; cross road target: 41.");
        sb.AppendLine($"- Tile asset: `{RoadTilePath}`.");
        sb.AppendLine();
        sb.AppendLine("## Hero Sprite");
        sb.AppendLine($"- Sprite: `{hero.SpriteName ?? "(none)"}`.");
        sb.AppendLine($"- Texture/path: `{hero.TexturePath ?? "(none)"}`.");
        sb.AppendLine($"- Rect: {hero.Rect.width:0}x{hero.Rect.height:0}.");
        sb.AppendLine($"- Texture size: {hero.TextureSize.x:0}x{hero.TextureSize.y:0}.");
        sb.AppendLine($"- Assignment mode: {(hero.UsedFallback ? "Sprite.Create fallback" : "imported sub-sprite")}.");
        if (hero.UsedFallback)
            sb.AppendLine("- Warrior_Idle.png was not available as imported sub-sprite, runtime Sprite.Create fallback used.");
        if (!string.IsNullOrEmpty(hero.Error))
            sb.AppendLine($"- Error: {hero.Error}.");
        sb.AppendLine();
        sb.AppendLine("## Validation Result");
        sb.AppendLine(validationPass
            ? "- Internal final-three validation: **PASS**."
            : "- Internal final-three validation: **FAIL**. See Console errors with `[TheHeroFinal3Fix]`.");
        sb.AppendLine();
        sb.AppendLine("## Manual Check");
        sb.AppendLine("- Run `The Hero/Validation/Validate Map MainAssets With Fallbacks` and confirm these are PASS: GroundTilemap tiles, RoadTilemap exists with tiles, Hero sub-sprite.");

        File.WriteAllText(ReportPath, sb.ToString());
        AssetDatabase.ImportAsset(ReportPath, ImportAssetOptions.ForceUpdate);
    }

    private sealed class HeroFixResult
    {
        public bool Pass;
        public string SpriteName;
        public string TexturePath;
        public Rect Rect;
        public Vector2 TextureSize;
        public bool UsedFallback;
        public string Error;

        public static HeroFixResult Fail(string error)
        {
            return new HeroFixResult
            {
                Pass = false,
                Error = error
            };
        }
    }
}
