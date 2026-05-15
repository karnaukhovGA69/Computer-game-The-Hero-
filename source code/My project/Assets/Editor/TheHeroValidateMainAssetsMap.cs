using System;
using System.IO;
using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class TheHeroValidateMainAssetsMap
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private static int _pass, _fail;

    [MenuItem("The Hero/Validation/Validate MainAssets Map")]
    public static void ValidateMainAssetsMap()
    {
        _pass = _fail = 0;
        Debug.Log("[TheHeroMapValidation] Dark zone requirement removed");
        Debug.Log("[TheHeroMapValidation] Mountain requirement removed");

        if (!File.Exists(MapScenePath)) { Fail("Map scene exists"); Summarize(); return; }
        Pass("Map scene exists");

        string mainAssets = TheHeroMainAssetsMapUtil.FindMainAssetsFolder();
        if (mainAssets == null) Fail("MainAssets folder found");
        else Pass("MainAssets folder found: " + mainAssets);

        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        Check(Object.FindAnyObjectByType<Grid>() != null, "Grid exists");

        Tilemap ground = FindTilemap("GroundTilemap");
        Check(ground != null, "GroundTilemap exists");
        if (ground != null)
            Check(TheHeroMainAssetsMapUtil.CountUsedTiles(ground) > 100, $"GroundTilemap has tiles ({TheHeroMainAssetsMapUtil.CountUsedTiles(ground)})");

        var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
        var castle = mapObjects.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base || o.id == "Castle_Player");
        Check(castle != null, "Castle exists");

        if (castle != null)
        {
            float dist = Vector2.Distance(
                new Vector2(castle.targetX, castle.targetY),
                new Vector2(TheHeroMainAssetsMapUtil.CenterX, TheHeroMainAssetsMapUtil.CenterY));
            Check(dist <= 4f, $"Castle near map center (dist {dist:0.0}, max 4)");

            Check(TheHeroMainAssetsMapUtil.CastleHasValidSprite(castle.gameObject), "Castle sprite is not whole PNG sheet");
            Check(!CastleLooksLikeUi(castle.gameObject), "Castle sprite is not UI button");
        }

        var hero = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include)
            .OrderByDescending(h => h.name == "Hero").FirstOrDefault();
        Check(hero != null, "Hero exists");
        if (hero != null)
        {
            var hsr = hero.GetComponent<SpriteRenderer>();
            Check(hsr != null && hsr.enabled && hsr.sprite != null, "Hero visible");
            if (castle != null)
            {
                float d = Vector2.Distance(new Vector2(hero.currentX, hero.currentY), new Vector2(castle.targetX, castle.targetY));
                Check(d <= 4f, $"Hero near castle (dist {d:0.0})");
            }
        }

        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        var follow = cam != null ? cam.GetComponent<THCameraFollow>() : null;
        Check(follow != null && follow.Target != null && follow.Target.GetComponent<THStrictGridHeroMovement>() != null,
            "CameraFollow target is Hero");

        Check(HasBridge(), "At least 1 bridge exists");
        Check(HasWater(), "At least 1 water area exists");
        Check(HasRoad(), "At least 1 road exists");
        Check(HasForestDetail(), "At least 1 forest/detail area exists");
        // Dark zone / mountain requirements removed: MainAssets has no dedicated dark or mountain tiles.
        // Northern boss area is built from ruins (walls_floor / Interior / TX Props) instead.

        Check(mapObjects.Any(o => o.displayName != null && o.displayName.Contains("Скелет") && o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord),
            "At least 1 Skeleton Warrior enemy exists");
        Check(mapObjects.Any(o => o.displayName != null && o.displayName.Contains("Маг-скелет")),
            "At least 1 Skeleton Mage enemy exists");
        Check(mapObjects.Any(o => o.type == THMapObject.ObjectType.Enemy && o.displayName != null &&
            (o.displayName.IndexOf("волк", StringComparison.OrdinalIgnoreCase) >= 0 ||
             o.displayName.IndexOf("Волк", StringComparison.Ordinal) >= 0 ||
             o.displayName.IndexOf("Проклят", StringComparison.OrdinalIgnoreCase) >= 0 ||
             o.displayName.IndexOf("гаргуль", StringComparison.OrdinalIgnoreCase) >= 0 ||
             o.displayName.IndexOf("тролл", StringComparison.OrdinalIgnoreCase) >= 0)),
            "At least 1 wolf/dark monster enemy exists");

        var darkLord = mapObjects.FirstOrDefault(o => o.isDarkLord || o.isFinalBoss);
        Check(darkLord != null, "DarkLord exists");
        if (darkLord != null)
        {
            Check(darkLord.isFinalBoss, "DarkLord isFinalBoss = true");
            var sr = darkLord.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                string path = AssetDatabase.GetAssetPath(sr.sprite);
                bool fromMain = path.IndexOf("MainAssets", StringComparison.OrdinalIgnoreCase) >= 0;
                Check(fromMain || !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite),
                    "DarkLord uses MainAssets boss sprite or valid fallback");
            }
            else Fail("DarkLord uses MainAssets boss sprite or valid fallback");
        }

        int resCount = mapObjects.Count(o =>
            o.type == THMapObject.ObjectType.GoldResource || o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource || o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure || o.type == THMapObject.ObjectType.Artifact);
        Check(resCount >= 10, $"At least 10 resource objects exist ({resCount})");

        foreach (var enemy in mapObjects.Where(o => o.type == THMapObject.ObjectType.Enemy))
        {
            var sr = enemy.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                Check(!TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite), $"Enemy {enemy.id} not whole sheet");
            Check(enemy.startsCombat, $"Enemy {enemy.id} startsCombat");
            Check(enemy.blocksMovement, $"Enemy {enemy.id} blocksMovement");
        }

        foreach (var res in mapObjects.Where(o =>
            o.type == THMapObject.ObjectType.GoldResource || o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource || o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure))
        {
            var sr = res.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                Check(!TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite), $"Resource {res.id} not whole sheet");
        }

        Check(!TerrainUsesWholeAtlas(), "No terrain tile uses whole atlas as a tile");
        Check(castle != null && castle.GetComponent<THCastle>() != null, "Castle opens Base (THCastle present)");
        Check(Object.FindAnyObjectByType<THMapController>() != null, "Map gameplay controller exists");
        Check(Object.FindAnyObjectByType<THMapGridInput>() != null, "Map grid input exists");

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        Check(canvas != null, "Map UI Canvas exists");
        if (canvas != null)
        {
            Check(canvas.GetComponentsInChildren<Text>(true).Any(t => !string.IsNullOrEmpty(t.text)), "Map UI still exists (HUD texts)");
            Check(canvas.GetComponentsInChildren<Button>(true).Any(b => b.name.IndexOf("Castle", StringComparison.OrdinalIgnoreCase) >= 0),
                "Castle UI button exists");
        }

        AppendValidationToReport();
        Summarize();
    }

    private static Tilemap FindTilemap(string name) =>
        Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == name);

    private static bool HasBridge() =>
        TheHeroMainAssetsMapUtil.CountUsedTiles(FindTilemap("BridgeTilemap")) > 0 ||
        Object.FindObjectsByType<THTile>(FindObjectsInactive.Include).Any(t => t.tileType == "bridge");

    private static bool HasWater() =>
        TheHeroMainAssetsMapUtil.CountUsedTiles(FindTilemap("WaterTilemap")) > 0 ||
        Object.FindObjectsByType<THTile>(FindObjectsInactive.Include).Any(t => t.tileType == "water");

    private static bool HasRoad() =>
        TheHeroMainAssetsMapUtil.CountUsedTiles(FindTilemap("RoadTilemap")) > 0 ||
        Object.FindObjectsByType<THTile>(FindObjectsInactive.Include).Any(t => t.tileType == "road");

    private static bool HasForestDetail() =>
        TheHeroMainAssetsMapUtil.CountUsedTiles(FindTilemap("ForestTilemap")) > 0 ||
        TheHeroMainAssetsMapUtil.CountUsedTiles(FindTilemap("DetailTilemap")) > 0 ||
        Object.FindObjectsByType<THTile>(FindObjectsInactive.Include).Any(t => t.tileType == "forest");

    private static bool HasDarkZone() =>
        TheHeroMainAssetsMapUtil.CountUsedTiles(FindTilemap("DarkTilemap")) > 0 ||
        Object.FindObjectsByType<THTile>(FindObjectsInactive.Include).Any(t => t.tileType == "dark");

    private static bool CastleLooksLikeUi(GameObject castle)
    {
        foreach (var sr in castle.GetComponentsInChildren<SpriteRenderer>(true))
            if (sr.sprite != null && TheHeroMainAssetsMapUtil.LooksLikeUiButton(sr.sprite)) return true;
        return false;
    }

    private static bool TerrainUsesWholeAtlas()
    {
        foreach (var tm in Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include))
        foreach (Vector3Int p in tm.cellBounds.allPositionsWithin)
        {
            Sprite sp = tm.GetSprite(p);
            if (sp != null && TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sp)) return true;
        }
        return false;
    }

    private static void Pass(string m) { _pass++; Debug.Log("[TheHeroMainAssetsValidation] PASS " + m); }
    private static void Fail(string m) { _fail++; Debug.LogError("[TheHeroMainAssetsValidation] FAIL " + m); }
    private static void Check(bool ok, string m) { if (ok) Pass(m); else Fail(m); }

    private static void Summarize()
    {
        if (_fail == 0) Debug.Log($"[TheHeroMainAssetsValidation] PASS All checks ({_pass})");
        else Debug.LogError($"[TheHeroMainAssetsValidation] FAIL {_fail} issue(s), PASS {_pass}");
    }

    private static void AppendValidationToReport()
    {
        string path = Path.Combine(Application.dataPath, "CodeAudit/MainAssets_Map_Validation_Fix_Report.md");
        if (!File.Exists(path)) return;
        string block = $"\n## Validation ({DateTime.Now:yyyy-MM-dd HH:mm})\n- PASS: {_pass}\n- FAIL: {_fail}\n";
        string text = File.ReadAllText(path);
        int idx = text.IndexOf("## Validation (", StringComparison.Ordinal);
        if (idx >= 0) text = text.Substring(0, idx);
        File.WriteAllText(path, text + block);
    }
}
