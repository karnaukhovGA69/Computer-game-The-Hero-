using System;
using System.IO;
using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

/// <summary>
/// Gameplay-critical validation for the MainAssets-only map. No mountain or dark-zone checks.
/// Menu: The Hero/Validation/Validate MainAssets Only Map
/// </summary>
public static class TheHeroValidateMainAssetsOnlyMap
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string MainAssetsRoot = "Assets/ExternalAssets/MainAssets";
    private static int _pass, _fail, _warn;

    [MenuItem("The Hero/Validation/Validate MainAssets Only Map")]
    public static void Run()
    {
        _pass = _fail = _warn = 0;
        Debug.Log("[TheHeroMapValidation] Dark zone requirement removed");
        Debug.Log("[TheHeroMapValidation] Mountain requirement removed");

        if (!File.Exists(MapScenePath)) { Fail("Map scene exists"); Summarize(); return; }
        Pass("Map scene exists");
        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        Check(Object.FindAnyObjectByType<Grid>() != null, "Grid exists");
        Tilemap ground = FindTilemap("GroundTilemap");
        Check(ground != null, "GroundTilemap exists");
        if (ground != null)
            Check(TheHeroMainAssetsMapUtil.CountUsedTiles(ground) > 100,
                $"GroundTilemap has tiles ({TheHeroMainAssetsMapUtil.CountUsedTiles(ground)})");

        Tilemap road = FindTilemap("RoadTilemap");
        Check(road != null && TheHeroMainAssetsMapUtil.CountUsedTiles(road) > 0, "RoadTilemap exists with tiles");

        var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
        var castle = mapObjects.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base || o.id == "Castle_Player");
        Check(castle != null, "Castle_Player exists");
        if (castle != null)
        {
            float dist = Vector2.Distance(new Vector2(castle.targetX, castle.targetY), new Vector2(24, 16));
            Check(dist <= 4f, $"Castle near map center (dist {dist:0.0})");
            Check(TheHeroMainAssetsMapUtil.CastleHasValidSprite(castle.gameObject), "Castle uses sub-sprite (not whole sheet)");
            Check(castle.GetComponent<THCastle>() != null, "Castle opens Base (THCastle)");
        }

        var hero = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include)
            .OrderByDescending(h => h.name == "Hero").FirstOrDefault();
        Check(hero != null, "Hero exists");
        if (hero != null && castle != null)
        {
            float d = Vector2.Distance(new Vector2(hero.currentX, hero.currentY),
                                       new Vector2(castle.targetX, castle.targetY));
            Check(d <= 5f, $"Hero near castle (dist {d:0.0})");
            var sr = hero.GetComponent<SpriteRenderer>() ?? hero.GetComponentInChildren<SpriteRenderer>(true);
            Check(sr != null && sr.sprite != null && !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite),
                "Hero sprite is sub-sprite (not whole sheet)");
        }

        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        var follow = cam != null ? cam.GetComponent<THCameraFollow>() : null;
        Check(follow != null && follow.Target != null && follow.Target.GetComponent<THStrictGridHeroMovement>() != null,
            "CameraFollow target is Hero");

        var enemies = mapObjects.Where(o => o.type == THMapObject.ObjectType.Enemy).ToList();
        Check(enemies.Count >= 5, $"At least 5 enemies ({enemies.Count})");

        bool skMageAssetExists = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "Skeleton Mage") != null;
        if (skMageAssetExists)
            Check(enemies.Any(e => e.displayName != null && e.displayName.IndexOf("Маг-скелет", StringComparison.Ordinal) >= 0),
                "Skeleton Mage enemy exists (asset available)");
        else Warn("Skeleton Mage asset not found — enemy skipped");

        var darkLord = enemies.FirstOrDefault(e => e.isFinalBoss || e.isDarkLord || e.id == "Enemy_DarkLord_Final");
        Check(darkLord != null, "Enemy_DarkLord_Final exists");
        if (darkLord != null) Check(darkLord.isFinalBoss, "DarkLord isFinalBoss = true");

        foreach (var e in enemies)
        {
            var sr = e.GetComponent<SpriteRenderer>() ?? e.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null)
                Check(!TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite), $"Enemy {e.id} sub-sprite");
            Check(e.startsCombat, $"Enemy {e.id} startsCombat");
            Check(e.blocksMovement, $"Enemy {e.id} blocksMovement");
        }

        var resources = mapObjects.Where(o =>
            o.type == THMapObject.ObjectType.GoldResource || o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource || o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure || o.type == THMapObject.ObjectType.Artifact).ToList();
        Check(resources.Count >= 8, $"At least 8 resource objects ({resources.Count})");
        foreach (var r in resources)
        {
            var sr = r.GetComponent<SpriteRenderer>() ?? r.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null)
                Check(!TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite), $"Resource {r.id} sub-sprite");
        }

        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        Check(canvas != null, "Map UI Canvas exists");
        if (canvas != null)
            Check(canvas.GetComponentsInChildren<Text>(true).Any(t => !string.IsNullOrEmpty(t.text)), "Map UI text present");

        Summarize();
    }

    private static Tilemap FindTilemap(string n) =>
        Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == n);

    private static void Pass(string m) { _pass++; Debug.Log("[TheHeroMainAssetsOnlyValidation] PASS " + m); }
    private static void Fail(string m) { _fail++; Debug.LogError("[TheHeroMainAssetsOnlyValidation] FAIL " + m); }
    private static void Warn(string m) { _warn++; Debug.LogWarning("[TheHeroMainAssetsOnlyValidation] WARN " + m); }
    private static void Check(bool ok, string m) { if (ok) Pass(m); else Fail(m); }

    private static void Summarize()
    {
        if (_fail == 0)
            Debug.Log($"[TheHeroMainAssetsOnlyValidation] PASS All gameplay-critical checks ({_pass}, warn {_warn})");
        else
            Debug.LogError($"[TheHeroMainAssetsOnlyValidation] FAIL {_fail}, PASS {_pass}, WARN {_warn}");
    }
}
