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
/// Fixes the residual 19 FAILs reported by TheHeroValidateMainAssetsMap without
/// rebuilding the whole scene. Menu: The Hero/Map/Fix Remaining MainAssets Validation Fails
/// </summary>
public static class TheHeroFixRemainingMainAssetsValidationFails
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string ReportPath = "Assets/CodeAudit/Remaining_MainAssets_Validation_Fix_Report.md";
    private const string MainAssetsRoot = "Assets/ExternalAssets/MainAssets";

    private static readonly StringBuilder _report = new StringBuilder();

    [MenuItem("The Hero/Map/Fix Remaining MainAssets Validation Fails")]
    public static void FixRemaining()
    {
        _report.Clear();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        Vector2Int center = FixGroundTilemap();
        FixCastleCenter(center);
        FixRoadTilemap(center);
        FixBridge();
        FixForestDetail();
        // Dark zone no longer required (MainAssets has no dedicated dark biome) — northern area is just ruins.
        FixSkeletonMage(center);
        FixEnemyWolfStart();
        int replaced = FixResourceWholeSheets();

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MapScenePath);
        Debug.Log("[TheHeroRemainingFix] Map saved");

        WriteReport(replaced, center);

        TheHeroValidateMainAssetsMap.ValidateMainAssetsMap();
    }

    // ── 1. GroundTilemap ──────────────────────────────────────────────────────
    private static Vector2Int FixGroundTilemap()
    {
        var grid = EnsureGrid();
        Tilemap ground = FindTilemap("GroundTilemap");
        if (ground == null)
        {
            // Try to repurpose largest existing tilemap as GroundTilemap.
            Tilemap largest = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
                .OrderByDescending(t => TheHeroMainAssetsMapUtil.CountUsedTiles(t)).FirstOrDefault();
            if (largest != null && TheHeroMainAssetsMapUtil.CountUsedTiles(largest) > 100)
            {
                largest.gameObject.name = "GroundTilemap";
                if (largest.transform.parent != grid.transform)
                    largest.transform.SetParent(grid.transform, true);
                ground = largest;
                _report.AppendLine("- Renamed largest tilemap → GroundTilemap.");
            }
        }

        if (ground == null)
        {
            ground = CreateTilemap(grid, "GroundTilemap", 0);
            var grass = LoadSubSprites("TX Tileset Grass.png");
            if (grass.Count > 0)
            {
                var tile = MakeTile(grass[0]);
                for (int x = 0; x < TheHeroMainAssetsMapUtil.MapW; x++)
                for (int y = 0; y < TheHeroMainAssetsMapUtil.MapH; y++)
                    ground.SetTile(new Vector3Int(x, y, 0), tile);
                _report.AppendLine("- Created GroundTilemap and filled with TX Tileset Grass sub-sprite.");
            }
        }

        Debug.Log("[TheHeroRemainingFix] GroundTilemap fixed");

        BoundsInt b = ground != null ? ground.cellBounds : new BoundsInt(0, 0, 0, TheHeroMainAssetsMapUtil.MapW, TheHeroMainAssetsMapUtil.MapH, 1);
        int cx = b.xMin + b.size.x / 2;
        int cy = b.yMin + b.size.y / 2;
        if (b.size.x <= 0 || b.size.y <= 0) { cx = TheHeroMainAssetsMapUtil.CenterX; cy = TheHeroMainAssetsMapUtil.CenterY; }
        return new Vector2Int(cx, cy);
    }

    // ── 2. Castle near map center ─────────────────────────────────────────────
    private static void FixCastleCenter(Vector2Int center)
    {
        var allBases = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .Where(o => o.type == THMapObject.ObjectType.Base ||
                        o.id == "Castle_Player" ||
                        o.gameObject.name == "Castle_Player")
            .ToList();

        // Pick canonical: prefer one named Castle_Player; otherwise the closest to center.
        THMapObject keep = allBases.FirstOrDefault(o => o.gameObject.name == "Castle_Player")
            ?? allBases.OrderBy(o => Vector2.Distance(new Vector2(o.targetX, o.targetY), center)).FirstOrDefault();

        // Destroy stale castle duplicates (BigCastle / OldCastle / extra Castle*).
        foreach (var o in allBases)
            if (o != keep) Object.DestroyImmediate(o.gameObject);

        // Also remove any non-MapObject "Castle" / "BigCastle" / "OldCastle" GameObjects (UI buttons untouched).
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (go == null) continue;
            if (keep != null && go == keep.gameObject) continue;
            string n = go.name;
            if (n == "BigCastle" || n == "OldCastle" || n == "Castle")
            {
                if (go.GetComponentInParent<Canvas>() != null) continue; // never touch UI
                if (go.GetComponent<THMapObject>() == null && go.GetComponent<THCastle>() == null) continue;
                Object.DestroyImmediate(go);
            }
        }

        if (keep == null)
        {
            // Create Castle_Player at center.
            var objectsRoot = GameObject.Find("ObjectsRoot");
            if (objectsRoot == null)
            {
                var mapRoot = GameObject.Find("MapRoot") ?? new GameObject("MapRoot");
                objectsRoot = new GameObject("ObjectsRoot");
                objectsRoot.transform.SetParent(mapRoot.transform, false);
            }
            var castleGo = new GameObject("Castle_Player");
            castleGo.transform.SetParent(objectsRoot.transform, false);
            castleGo.transform.position = new Vector3(center.x, center.y, -0.2f);
            keep = castleGo.AddComponent<THMapObject>();
            keep.id = "Castle_Player";
            keep.type = THMapObject.ObjectType.Base;
            keep.displayName = "Замок";
            castleGo.AddComponent<THCastle>();
            if (castleGo.GetComponent<BoxCollider2D>() == null)
                castleGo.AddComponent<BoxCollider2D>().size = new Vector2(1.2f, 1.2f);
        }
        else
        {
            keep.gameObject.name = "Castle_Player";
            keep.transform.position = new Vector3(center.x, center.y, -0.2f);
        }

        keep.targetX = center.x;
        keep.targetY = center.y;
        keep.type = THMapObject.ObjectType.Base;
        if (string.IsNullOrEmpty(keep.id)) keep.id = "Castle_Player";

        if (!TheHeroMainAssetsMapUtil.CastleHasValidSprite(keep.gameObject))
        {
            var prop = LoadSubSprites("TX Props.png");
            var fb = TheHeroMainAssetsMapUtil.PickByName(prop, "tower", "wall", "stone", "house", "pillar")
                  ?? prop.FirstOrDefault();
            if (fb != null) TheHeroMainAssetsMapUtil.ApplyObjectSprite(keep.gameObject, fb, 2.2f, 70);
        }

        _report.AppendLine($"- Castle_Player at ({center.x},{center.y}); {allBases.Count - 1} duplicate castle(s) removed.");
        Debug.Log("[TheHeroRemainingFix] Castle centered");
    }

    // ── 3. Road ───────────────────────────────────────────────────────────────
    private static void FixRoadTilemap(Vector2Int center)
    {
        var grid = EnsureGrid();
        Tilemap road = FindTilemap("RoadTilemap") ?? CreateTilemap(grid, "RoadTilemap", 1);

        var pool = LoadSubSprites("Main_tiles.png")
            .Concat(LoadSubSprites("walls_floor.png"))
            .Concat(LoadSubSprites("ground_grass_details.png"))
            .Where(s => s != null).ToList();

        Sprite roadSp = TheHeroMainAssetsMapUtil.PickByName(pool, "road", "path", "stone", "dirt")
                     ?? pool.FirstOrDefault();
        if (roadSp == null) { _report.AppendLine("- Road tile pool empty."); return; }

        var tile = MakeTile(roadSp);
        int placed = 0;
        for (int dx = -6; dx <= 6; dx++) { road.SetTile(new Vector3Int(center.x + dx, center.y, 0), tile); placed++; }
        for (int dy = -4; dy <= 4; dy++) { road.SetTile(new Vector3Int(center.x, center.y + dy, 0), tile); placed++; }

        _report.AppendLine($"- RoadTilemap: {placed} road tiles placed (sub-sprite '{roadSp.name}').");
        Debug.Log("[TheHeroRemainingFix] Road fixed");
    }

    // ── 4. Bridge ─────────────────────────────────────────────────────────────
    private static void FixBridge()
    {
        var grid = EnsureGrid();
        Tilemap bridge = FindTilemap("BridgeTilemap") ?? CreateTilemap(grid, "BridgeTilemap", 3);

        var bridges = LoadSubSprites("Bridges.png");
        Sprite sp = bridges.FirstOrDefault();
        if (sp == null) { _report.AppendLine("- Bridges.png produced no sub-sprites."); return; }

        var tile = MakeTile(sp);
        bridge.SetTile(new Vector3Int(10, 16, 0), tile);
        bridge.SetTile(new Vector3Int(11, 16, 0), tile);
        bridge.SetTile(new Vector3Int(10, 17, 0), tile);
        bridge.SetTile(new Vector3Int(11, 17, 0), tile);
        _report.AppendLine($"- BridgeTilemap: 4 tiles using sub-sprite '{sp.name}'.");
        Debug.Log("[TheHeroRemainingFix] Bridge fixed");
    }

    // ── 5. Forest / detail ────────────────────────────────────────────────────
    private static void FixForestDetail()
    {
        var grid = EnsureGrid();
        Tilemap forest = FindTilemap("ForestTilemap") ?? CreateTilemap(grid, "ForestTilemap", 4);

        var pool = LoadSubSprites("TX Plant.png")
            .Concat(LoadSubSprites("Trees_animation.png"))
            .Concat(LoadSubSprites("ground_grass_details.png"))
            .Where(s => s != null).Distinct().Take(6).ToList();
        if (pool.Count == 0) { _report.AppendLine("- Forest sprite pool empty."); return; }

        var tiles = pool.Select(MakeTile).ToList();
        int placed = 0;
        for (int x = 2; x <= 14; x++)
        for (int y = 8; y <= 24; y++)
        {
            if (((x * 7 + y * 13) % 5) != 0) continue;
            forest.SetTile(new Vector3Int(x, y, 0), tiles[(x + y) % tiles.Count]);
            placed++;
        }
        _report.AppendLine($"- ForestTilemap: {placed} plant/detail tiles in west zone.");
        Debug.Log("[TheHeroRemainingFix] Forest/detail fixed");
    }

    // ── 6. Dark zone ──────────────────────────────────────────────────────────
    private static void FixDarkZone(Vector2Int center)
    {
        var grid = EnsureGrid();
        Tilemap dark = FindTilemap("DarkTilemap") ?? CreateTilemap(grid, "DarkTilemap", 6);

        var pool = LoadSubSprites("walls_floor.png")
            .Concat(LoadSubSprites("Interior.png"))
            .Concat(LoadSubSprites("Main_tiles.png"))
            .Where(s => s != null).Take(8).ToList();
        if (pool.Count == 0) { _report.AppendLine("- Dark zone sprite pool empty."); return; }

        var tiles = pool.Select(MakeTile).ToList();
        int placed = 0;
        for (int x = 14; x <= 40; x++)
        for (int y = 24; y <= 30; y++)
        {
            dark.SetTile(new Vector3Int(x, y, 0), tiles[(x * 3 + y) % tiles.Count]);
            placed++;
        }
        _report.AppendLine($"- DarkTilemap: {placed} dark tiles in north band.");
        Debug.Log("[TheHeroRemainingFix] Dark zone fixed");
    }

    // ── 7. Skeleton Mage enemy ────────────────────────────────────────────────
    private static void FixSkeletonMage(Vector2Int center)
    {
        var existing = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.displayName != null && o.displayName.IndexOf("Маг-скелет", StringComparison.Ordinal) >= 0);
        Sprite sp = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "Skeleton Mage")
                  ?? LoadSubSprites("Skeleton Mage.png").FirstOrDefault();

        GameObject go;
        if (existing != null) go = existing.gameObject;
        else
        {
            var root = GameObject.Find("ObjectsRoot");
            if (root == null) { _report.AppendLine("- ObjectsRoot missing; skipped Skeleton Mage."); return; }

            go = new GameObject("Enemy_SkeletonMage_North");
            go.transform.SetParent(root.transform, false);
            go.transform.position = new Vector3(24, 28, -0.2f);
            var mo = go.AddComponent<THMapObject>();
            mo.id = "Enemy_SkeletonMage_North";
            mo.type = THMapObject.ObjectType.Enemy;
            mo.displayName = "Маг-скелет";
            mo.targetX = 24;
            mo.targetY = 28;
            mo.startsCombat = true;
            mo.blocksMovement = true;
            mo.isFinalBoss = false;
            mo.difficulty = THEnemyDifficulty.Strong;
            go.AddComponent<THEnemy>();
            go.AddComponent<BoxCollider2D>();
        }

        if (sp != null) TheHeroMainAssetsMapUtil.ApplyObjectSprite(go, sp, 1f, 90);
        _report.AppendLine($"- Skeleton Mage enemy '{go.name}' sprite='{sp?.name ?? "n/a"}'.");
        Debug.Log("[TheHeroRemainingFix] Skeleton Mage placed");
    }

    // ── 8. Enemy_Wolf_Start ───────────────────────────────────────────────────
    private static void FixEnemyWolfStart()
    {
        var wolf = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.gameObject.name == "Enemy_Wolf_Start" || o.id == "Enemy_Wolf_Start");
        if (wolf == null) { _report.AppendLine("- Enemy_Wolf_Start not found; skipped."); return; }

        Sprite sp = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_121_CursedWolf")
                  ?? LoadSubSprites("FR_121_CursedWolf.png").FirstOrDefault();
        if (sp == null) { _report.AppendLine("- Wolf sub-sprite not found."); return; }

        TheHeroMainAssetsMapUtil.ApplyObjectSprite(wolf.gameObject, sp, 1f, 45);
        _report.AppendLine($"- Enemy_Wolf_Start sprite='{sp.name}'.");
        Debug.Log("[TheHeroRemainingFix] Wolf sprite fixed");
    }

    // ── 9. Resources whole-sheet ──────────────────────────────────────────────
    private static int FixResourceWholeSheets()
    {
        var props = LoadSubSprites("TX Props.png");
        var icons = LoadSubSprites("Icons.png");
        var details = LoadSubSprites("ground_grass_details.png");
        Sprite gold = TheHeroMainAssetsMapUtil.PickByName(props, "pot", "coin", "gold")
                   ?? TheHeroMainAssetsMapUtil.PickByName(icons, "gold", "coin");
        Sprite wood = TheHeroMainAssetsMapUtil.PickByName(props, "crate", "barrel", "wood", "log");
        Sprite stone = TheHeroMainAssetsMapUtil.PickByName(props, "stone", "rock");
        Sprite mana = TheHeroMainAssetsMapUtil.PickByName(props, "rune", "crystal", "mana", "gem")
                   ?? TheHeroMainAssetsMapUtil.PickByName(icons, "mana", "crystal");
        Sprite chest = TheHeroMainAssetsMapUtil.PickByName(props, "chest", "box");
        Sprite generic = props.FirstOrDefault() ?? details.FirstOrDefault();

        int replaced = 0;
        foreach (var mo in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
        {
            if (mo.type == THMapObject.ObjectType.Base || mo.type == THMapObject.ObjectType.Enemy) continue;

            var sr = mo.GetComponent<SpriteRenderer>() ?? mo.GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || sr.sprite == null) continue;
            if (!TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite)) continue;

            Sprite pick = SpriteForResource(mo, gold, wood, stone, mana, chest, generic);
            if (pick == null) continue;
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(mo.gameObject, pick, 0.75f, 40);
            replaced++;
        }
        _report.AppendLine($"- Replaced whole-sheet resource sprites: {replaced}.");
        Debug.Log("[TheHeroRemainingFix] Resource sprites fixed");
        return replaced;
    }

    private static Sprite SpriteForResource(THMapObject mo, Sprite gold, Sprite wood, Sprite stone, Sprite mana, Sprite chest, Sprite generic)
    {
        string n = (mo.gameObject.name + " " + (mo.id ?? "") + " " + (mo.displayName ?? "")).ToLowerInvariant();
        if (n.Contains("chest") || mo.type == THMapObject.ObjectType.Treasure) return chest ?? generic;
        switch (mo.type)
        {
            case THMapObject.ObjectType.GoldResource: return gold ?? generic;
            case THMapObject.ObjectType.WoodResource: return wood ?? generic;
            case THMapObject.ObjectType.StoneResource: return stone ?? generic;
            case THMapObject.ObjectType.ManaResource: return mana ?? generic;
            case THMapObject.ObjectType.Artifact: return chest ?? generic;
        }
        if (n.Contains("gold")) return gold ?? generic;
        if (n.Contains("wood")) return wood ?? generic;
        if (n.Contains("stone") || n.Contains("rock")) return stone ?? generic;
        if (n.Contains("mana") || n.Contains("crystal")) return mana ?? generic;
        return generic;
    }

    // ── helpers ───────────────────────────────────────────────────────────────
    private static GameObject EnsureGrid()
    {
        var mapRoot = GameObject.Find("MapRoot") ?? new GameObject("MapRoot");
        Transform g = mapRoot.transform.Find("Grid");
        GameObject go = g != null ? g.gameObject : new GameObject("Grid");
        go.transform.SetParent(mapRoot.transform, false);
        if (go.GetComponent<Grid>() == null) go.AddComponent<Grid>().cellSize = Vector3.one;
        return go;
    }

    private static Tilemap CreateTilemap(GameObject grid, string name, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        return tm;
    }

    private static Tilemap FindTilemap(string name) =>
        Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == name);

    private static Tile MakeTile(Sprite sp)
    {
        var t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = sp;
        return t;
    }

    private static List<Sprite> LoadSubSprites(string fileName)
    {
        string path = $"{MainAssetsRoot}/{fileName}";
        return TheHeroMainAssetsMapUtil.LoadSlicedSprites(path);
    }

    private static void WriteReport(int replaced, Vector2Int center)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Remaining MainAssets Validation Fix Report");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"## Center used: ({center.x},{center.y})");
        sb.AppendLine();
        sb.AppendLine("## Fixes applied");
        sb.Append(_report);
        sb.AppendLine();
        sb.AppendLine("## Manual verification");
        sb.AppendLine("1. Recompile, then **The Hero → Map → Fix Remaining MainAssets Validation Fails**.");
        sb.AppendLine("2. **The Hero → Validation → Validate MainAssets Map** — expect FAIL=0 for the 19 listed items.");
        sb.AppendLine("3. Play → MainMenu → New Game.");
        sb.AppendLine($"4. Replaced whole-sheet resource sprites: {replaced}.");

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));
        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/Remaining_MainAssets_Validation_Fix_Report.md"), sb.ToString());
    }
}
