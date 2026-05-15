using System;
using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class TheHeroValidateImportedSprites
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private static int _failures;

    [MenuItem("The Hero/Validation/Validate Imported Map Sprites")]
    public static void ValidateImportedMapSprites()
    {
        _failures = 0;
        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        GameObject hero = THImportedSpriteEditorUtil.FindHero();
        var heroSr = hero != null ? hero.GetComponent<SpriteRenderer>() : null;
        Check(heroSr != null && heroSr.sprite != null, "Hero sprite");
        Check(heroSr != null && heroSr.sprite != null && MaxRendererSize(heroSr) <= 2f, "Hero sprite is not huge");

        var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
        var enemies = mapObjects.Where(o => o.type == THMapObject.ObjectType.Enemy).ToArray();
        var castle = mapObjects.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base || HasText(o, "castle"));
        var darkLord = enemies.FirstOrDefault(o => o.isFinalBoss || o.isDarkLord || HasText(o, "darklord") || HasText(o, "dark lord") || HasText(o, "lord"));

        var castleSr = castle != null ? castle.GetComponent<SpriteRenderer>() : null;
        Check(castleSr != null && castleSr.sprite != null, "Castle sprite");
        Check(castleSr != null && castleSr.sprite != null && !IsUiPath(SpritePath(castleSr.sprite)), "Castle is not UI button");

        bool resourcesOk =
            HasResourceSprite(mapObjects, THMapObject.ObjectType.GoldResource) &&
            HasResourceSprite(mapObjects, THMapObject.ObjectType.WoodResource) &&
            HasResourceSprite(mapObjects, THMapObject.ObjectType.StoneResource) &&
            HasResourceSprite(mapObjects, THMapObject.ObjectType.ManaResource);
        Check(resourcesOk, "Resource sprites");

        bool orcOk = enemies.Any(e => THImportedSpriteEditorUtil.RoleForMapObject(e) == "Orc" &&
                                      SpritePath(e).Contains("Assets/ExternalAssets/Orcs"));
        Check(orcOk, "Orc sprite");

        bool skeletonOk = enemies.Any(e => THImportedSpriteEditorUtil.RoleForMapObject(e) == "Skeleton" &&
                                           SpritePath(e).Contains("Assets/ExternalAssets/Skeletons"));
        Check(skeletonOk, "Skeleton sprite");

        bool darkLordOk = darkLord != null && SpritePath(darkLord).Contains("Assets/ExternalAssets/DarkLord");
        Check(darkLordOk, "DarkLord sprite");

        Check(enemies.All(e => !IsUiPath(SpritePath(e))), "No enemy uses UI sprite");
        Check(enemies.All(e => !IsTerrainPath(SpritePath(e))), "No enemy uses terrain tile");
        Check(enemies.All(e =>
        {
            var sr = e.GetComponent<SpriteRenderer>();
            return sr != null && sr.sprite != null && MaxRendererSize(sr) <= 2f;
        }), "No enemy sprite bounds > 2 cells");

        Check(darkLord != null && darkLord.type == THMapObject.ObjectType.Enemy, "DarkLord is Enemy, not Resource");
        Check(darkLord != null && darkLord.isFinalBoss, "DarkLord isFinalBoss = true");

        bool gameplayComponentsOk =
            hero != null &&
            hero.GetComponent<THStrictGridHeroMovement>() != null &&
            castle != null &&
            enemies.Length > 0 &&
            enemies.All(e => e.startsCombat) &&
            mapObjects.Any(o => o.type == THMapObject.ObjectType.GoldResource) &&
            mapObjects.Any(o => o.type == THMapObject.ObjectType.WoodResource) &&
            mapObjects.Any(o => o.type == THMapObject.ObjectType.StoneResource) &&
            mapObjects.Any(o => o.type == THMapObject.ObjectType.ManaResource);
        Check(gameplayComponentsOk, "Gameplay components still exist");

        bool noMissing = true;
        if (heroSr == null || heroSr.sprite == null) noMissing = false;
        foreach (var obj in mapObjects)
        {
            var sr = obj.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) noMissing = false;
        }
        Check(noMissing, "No missing SpriteRenderer sprites");

        if (_failures == 0)
        {
            Debug.Log("[TheHeroSpriteValidation] PASS Imported map sprite validation complete");
        }
        else
        {
            Debug.LogError("[TheHeroSpriteValidation] FAIL Imported map sprite validation failed: " + _failures + " issue(s)");
        }

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(_failures == 0 ? 0 : 1);
        }
    }

    private static void Check(bool condition, string name)
    {
        if (condition)
        {
            Debug.Log("[TheHeroSpriteValidation] PASS " + name);
        }
        else
        {
            _failures++;
            Debug.LogError("[TheHeroSpriteValidation] FAIL " + name);
        }
    }

    private static bool HasResourceSprite(THMapObject[] objects, THMapObject.ObjectType type)
    {
        return objects.Any(o =>
        {
            if (o.type != type) return false;
            var sr = o.GetComponent<SpriteRenderer>();
            return sr != null && sr.sprite != null;
        });
    }

    private static string SpritePath(THMapObject obj)
    {
        if (obj == null) return string.Empty;
        var sr = obj.GetComponent<SpriteRenderer>();
        return sr != null && sr.sprite != null ? SpritePath(sr.sprite) : string.Empty;
    }

    private static string SpritePath(Sprite sprite)
    {
        return sprite == null ? string.Empty : AssetDatabase.GetAssetPath(sprite).Replace('\\', '/');
    }

    private static bool HasText(THMapObject obj, string value)
    {
        string text = (obj.gameObject.name + " " + obj.id + " " + obj.displayName).ToLowerInvariant();
        return text.Contains(value);
    }

    private static float MaxRendererSize(SpriteRenderer sr)
    {
        return Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
    }

    private static bool IsUiPath(string path)
    {
        string p = path.ToLowerInvariant();
        return p.Contains("/ui/") ||
               p.Contains("/ui elements/") ||
               p.Contains("button") ||
               p.Contains("panel") ||
               p.Contains("inventory") ||
               p.Contains("equipment") ||
               p.Contains("main_menu") ||
               p.Contains("avatar") ||
               p.Contains("portrait") ||
               p.Contains("icons.png");
    }

    private static bool IsTerrainPath(string path)
    {
        string p = path.ToLowerInvariant();
        return p.Contains("/cleanmap/tiles/") ||
               (p.Contains("/generatedmaptiles/") && !p.Contains("/generatedmaptiles/objects/")) ||
               p.Contains("/terrain/tileset/") ||
               p.Contains("/tile palette/") ||
               p.Contains("tileset");
    }
}
