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
/// Gameplay-critical validation for the MainAssets+Cainos+TinySwords map. WARN (not FAIL)
/// for missing mountains / dark zone / dedicated castle / water / bridge.
/// Menu: The Hero/Validation/Validate Map MainAssets With Fallbacks
/// </summary>
public static class TheHeroValidateMapMainAssetsWithFallbacks
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private static int _pass, _fail, _warn;

    [MenuItem("The Hero/Validation/Validate Map MainAssets With Fallbacks")]
    public static void Run()
    {
        _pass = _fail = _warn = 0;
        if (!File.Exists(MapScenePath)) { Fail("Map scene exists"); Summarize(); return; }
        Pass("Map scene exists");
        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        Check(Object.FindAnyObjectByType<Grid>() != null, "Grid exists");
        Tilemap ground = FindTm("GroundTilemap");
        Check(ground != null, "GroundTilemap exists");
        if (ground != null)
        {
            int groundCount = TheHeroMainAssetsMapUtil.CountUsedTiles(ground);
            Check(groundCount > 100, $"GroundTilemap tiles ({groundCount})");
        }

        Tilemap road = FindTm("RoadTilemap");
        int roadCount = road != null ? TheHeroMainAssetsMapUtil.CountUsedTiles(road) : 0;
        Check(road != null && roadCount > 0, "RoadTilemap exists with tiles");

        bool forestOk = HasTiles("ForestTilemap") || HasTiles("DetailTilemap");
        Check(forestOk, "Forest/Detail area exists");

        if (FindTm("WaterTilemap") == null) Warn("No water (asset missing)");
        if (FindTm("BridgeTilemap") == null) Warn("No bridge (asset missing)");

        var mos = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
        var castle = mos.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base || o.id == "Castle_Player");
        Check(castle != null, "Castle_Player exists");
        if (castle != null)
        {
            float dist = Vector2.Distance(new Vector2(castle.targetX, castle.targetY), new Vector2(24, 16));
            Check(dist <= 4f, $"Castle near center (dist {dist:0.0})");
            Check(TheHeroMainAssetsMapUtil.CastleHasValidSprite(castle.gameObject), "Castle uses sub-sprite");
            Check(castle.GetComponent<THCastle>() != null, "Castle opens Base (THCastle)");
        }

        var hero = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include)
            .OrderByDescending(h => h.name == "Hero").FirstOrDefault();
        Check(hero != null, "Hero exists");
        if (hero != null && castle != null)
        {
            float d = Vector2.Distance(new Vector2(hero.currentX, hero.currentY), new Vector2(castle.targetX, castle.targetY));
            Check(d <= 5f, $"Hero near castle (dist {d:0.0})");
            var heroRenderers = hero.GetComponentsInChildren<SpriteRenderer>(true);
            Check(heroRenderers.Any(r => r != null && HeroUsesValidWarriorIdleFrame(r.sprite)), "Hero sub-sprite");
        }

        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        var follow = cam != null ? cam.GetComponent<THCameraFollow>() : null;
        Check(follow != null && follow.Target != null && follow.Target.GetComponent<THStrictGridHeroMovement>() != null, "CameraFollow target = Hero");

        var enemies = mos.Where(o => o.type == THMapObject.ObjectType.Enemy).ToList();
        Check(enemies.Count >= 5, $"At least 5 enemies ({enemies.Count})");
        var darkLord = enemies.FirstOrDefault(e => e.isFinalBoss || e.isDarkLord || e.id == "Enemy_DarkLord_Final");
        Check(darkLord != null, "Final boss exists");
        if (darkLord != null) Check(darkLord.isFinalBoss, "DarkLord isFinalBoss=true");

        foreach (var e in enemies)
        {
            var sr = e.GetComponent<SpriteRenderer>() ?? e.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && sr.sprite != null)
                Check(!TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sr.sprite), $"Enemy {e.id} sub-sprite");
            Check(e.startsCombat, $"Enemy {e.id} startsCombat");
            Check(e.blocksMovement, $"Enemy {e.id} blocksMovement");
        }

        var resources = mos.Where(o =>
            o.type == THMapObject.ObjectType.GoldResource || o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource || o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure || o.type == THMapObject.ObjectType.Artifact).ToList();
        Check(resources.Count >= 8, $"At least 8 resources ({resources.Count})");
        Check(resources.Count(r => r.type == THMapObject.ObjectType.Treasure) >= 2, "At least 2 chests/treasures");
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

    private static Tilemap FindTm(string n) =>
        Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include).FirstOrDefault(t => t.name == n);

    private static bool HasTiles(string n)
    {
        var t = FindTm(n);
        return t != null && TheHeroMainAssetsMapUtil.CountUsedTiles(t) > 0;
    }

    private static bool HeroUsesValidWarriorIdleFrame(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null)
            return false;

        Texture2D texture = sprite.texture;
        string spritePath = AssetDatabase.GetAssetPath(sprite);
        string texturePath = AssetDatabase.GetAssetPath(texture);
        bool isWarriorIdle =
            texture.name.IndexOf("Warrior_Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
            spritePath.IndexOf("Warrior_Idle", StringComparison.OrdinalIgnoreCase) >= 0 ||
            texturePath.IndexOf("Warrior_Idle", StringComparison.OrdinalIgnoreCase) >= 0;

        bool frameSized = sprite.rect.width <= 192f && sprite.rect.height <= 192f;
        bool whole1536Sheet =
            Mathf.Approximately(sprite.rect.width, 1536f) &&
            Mathf.Approximately(sprite.rect.height, 192f) &&
            texture.width == 1536 &&
            texture.height == 192;

        return isWarriorIdle && frameSized && !whole1536Sheet;
    }

    private static void Pass(string m) { _pass++; Debug.Log("[TheHeroFallbackValidation] PASS " + m); }
    private static void Fail(string m) { _fail++; Debug.LogError("[TheHeroFallbackValidation] FAIL " + m); }
    private static void Warn(string m) { _warn++; Debug.LogWarning("[TheHeroFallbackValidation] WARN " + m); }
    private static void Check(bool ok, string m) { if (ok) Pass(m); else Fail(m); }

    private static void Summarize()
    {
        if (_fail == 0) Debug.Log($"[TheHeroFallbackValidation] PASS All gameplay-critical ({_pass}, warn {_warn})");
        else Debug.LogError($"[TheHeroFallbackValidation] FAIL {_fail}, PASS {_pass}, WARN {_warn}");
    }
}
