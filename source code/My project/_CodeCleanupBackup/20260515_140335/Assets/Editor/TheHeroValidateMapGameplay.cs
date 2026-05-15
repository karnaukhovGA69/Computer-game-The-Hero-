// TheHeroValidateMapGameplay.cs
// MenuItem: "The Hero/Validation/Validate Map Gameplay"
// Performs 20 validation checks on Map.unity gameplay objects.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;

public class TheHeroValidateMapGameplay
{
    [MenuItem("The Hero/Validation/Validate Map Gameplay")]
    public static void ValidateMapGameplay()
    {
        int failCount = RunValidation();
        if (failCount == 0)
            Debug.Log("[TheHeroMapValidation] PASS Map gameplay ready");
        else
            Debug.LogError($"[TheHeroMapValidation] FAIL {failCount} check(s) failed. See above for details.");
    }

    public static int RunValidation()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
        int fails = 0;

        // --- CHECK 1: At least one Tilemap exists ---
        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        if (tilemaps == null || tilemaps.Length == 0)
        {
            Debug.LogError("[TheHeroMapValidation] FAIL CHECK 1: No Tilemap found in Map.unity");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 1: Tilemap(s) found: " + tilemaps.Length);

        // --- CHECK 2: MapRoot hierarchy exists ---
        var mapRoot = GameObject.Find("MapRoot");
        if (mapRoot == null)
        {
            Debug.LogWarning("[TheHeroMapValidation] FAIL CHECK 2: MapRoot GameObject not found");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 2: MapRoot found");

        // --- CHECK 3: Hero exists with THStrictGridHeroMovement ---
        var heroMovers = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include);
        if (heroMovers.Length == 0)
        {
            Debug.LogError("[TheHeroMapValidation] FAIL CHECK 3: No Hero with THStrictGridHeroMovement found");
            fails++;
        }
        else
        {
            Debug.Log($"[TheHeroMapValidation] PASS CHECK 3: Hero found at cell ({heroMovers[0].currentX},{heroMovers[0].currentY})");
            if (heroMovers.Length > 1)
                Debug.LogWarning($"[TheHeroMapValidation] WARNING CHECK 3: Multiple Hero movers found ({heroMovers.Length})");
        }

        // --- CHECK 4: Hero has SpriteRenderer with non-null sprite ---
        var heroSR = heroMovers.Length > 0 ? heroMovers[0].GetComponent<SpriteRenderer>() : null;
        if (heroSR == null || heroSR.sprite == null)
        {
            Debug.LogWarning("[TheHeroMapValidation] FAIL CHECK 4: Hero SpriteRenderer missing or no sprite");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 4: Hero has SpriteRenderer with sprite");

        // --- CHECK 5: Main Camera is Orthographic ---
        var cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
        if (cam == null || !cam.orthographic)
        {
            Debug.LogWarning("[TheHeroMapValidation] FAIL CHECK 5: Main Camera missing or not Orthographic");
            fails++;
        }
        else
            Debug.Log($"[TheHeroMapValidation] PASS CHECK 5: Camera orthographic, size={cam.orthographicSize}");

        // --- CHECK 6: THCameraFollow attached to camera and has target ---
        var cameraFollow = cam != null ? cam.GetComponent<THCameraFollow>() : null;
        if (cameraFollow == null || cameraFollow.Target == null)
        {
            Debug.LogWarning("[TheHeroMapValidation] FAIL CHECK 6: THCameraFollow missing on camera or Target is null");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 6: THCameraFollow wired with target=" + cameraFollow.Target.name);

        var allMO = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);

        // --- CHECK 7: Castle_Player (Base) exists ---
        var castle = allMO.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base);
        if (castle == null)
        {
            Debug.LogError("[TheHeroMapValidation] FAIL CHECK 7: No Base/Castle THMapObject found");
            fails++;
        }
        else
            Debug.Log($"[TheHeroMapValidation] PASS CHECK 7: Castle found: {castle.id} at ({castle.targetX},{castle.targetY})");

        // --- CHECK 8: Castle has THCastle component ---
        var castleComp = castle != null ? castle.GetComponent<THCastle>() : null;
        if (castle != null && castleComp == null)
        {
            Debug.LogWarning("[TheHeroMapValidation] FAIL CHECK 8: Castle THMapObject missing THCastle component");
            fails++;
        }
        else if (castle != null)
            Debug.Log("[TheHeroMapValidation] PASS CHECK 8: THCastle component present on castle");

        // --- CHECK 9: At least 5 enemies ---
        var enemies = allMO.Where(o => o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord).ToList();
        if (enemies.Count < 5)
        {
            Debug.LogError($"[TheHeroMapValidation] FAIL CHECK 9: Only {enemies.Count} regular enemies found (need ≥5)");
            fails++;
        }
        else
            Debug.Log($"[TheHeroMapValidation] PASS CHECK 9: {enemies.Count} regular enemies found");

        // --- CHECK 10: All enemies have non-empty army ---
        int enemiesNoArmy = enemies.Count(e => e.enemyArmy == null || e.enemyArmy.Count == 0);
        if (enemiesNoArmy > 0)
        {
            Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 10: {enemiesNoArmy} enemies have empty army");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 10: All enemies have army units");

        // --- CHECK 11: DarkLord exists with isFinalBoss=true and isDarkLord=true ---
        var darkLord = allMO.FirstOrDefault(o => o.isDarkLord);
        if (darkLord == null)
        {
            Debug.LogError("[TheHeroMapValidation] FAIL CHECK 11: No DarkLord (isDarkLord=true) found");
            fails++;
        }
        else if (!darkLord.isFinalBoss)
        {
            Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 11: DarkLord {darkLord.id} has isFinalBoss=false");
            fails++;
        }
        else
            Debug.Log($"[TheHeroMapValidation] PASS CHECK 11: DarkLord found: {darkLord.id} at ({darkLord.targetX},{darkLord.targetY})");

        // --- CHECK 12: DarkLord has army with ≥2 unit types ---
        if (darkLord != null)
        {
            if (darkLord.enemyArmy == null || darkLord.enemyArmy.Count < 2)
            {
                Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 12: DarkLord has fewer than 2 army unit types");
                fails++;
            }
            else
                Debug.Log($"[TheHeroMapValidation] PASS CHECK 12: DarkLord has {darkLord.enemyArmy.Count} army unit types");
        }

        // --- CHECK 13: At least 5 resources ---
        var resources = allMO.Where(o =>
            o.type == THMapObject.ObjectType.GoldResource ||
            o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource ||
            o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure).ToList();
        if (resources.Count < 5)
        {
            Debug.LogError($"[TheHeroMapValidation] FAIL CHECK 13: Only {resources.Count} resources found (need ≥5)");
            fails++;
        }
        else
            Debug.Log($"[TheHeroMapValidation] PASS CHECK 13: {resources.Count} resources found");

        // --- CHECK 14: Resources have non-zero rewards ---
        int resNoReward = resources.Count(r => r.rewardGold == 0 && r.rewardWood == 0 && r.rewardStone == 0 && r.rewardMana == 0 && r.rewardExp == 0);
        if (resNoReward > 0)
        {
            Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 14: {resNoReward} resources have all-zero rewards");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 14: All resources have at least one non-zero reward");

        // --- CHECK 15: Artifact exists ---
        var artifact = allMO.FirstOrDefault(o => o.type == THMapObject.ObjectType.Artifact);
        if (artifact == null)
        {
            Debug.LogError("[TheHeroMapValidation] FAIL CHECK 15: No Artifact (type=Artifact) found");
            fails++;
        }
        else
            Debug.Log($"[TheHeroMapValidation] PASS CHECK 15: Artifact found: {artifact.id} ({artifact.displayName})");

        // --- CHECK 16: Exactly one THSingleMapHoverLabel ---
        var hoverLabels = Object.FindObjectsByType<THSingleMapHoverLabel>(FindObjectsInactive.Include);
        if (hoverLabels.Length == 0)
        {
            Debug.LogWarning("[TheHeroMapValidation] FAIL CHECK 16: No THSingleMapHoverLabel found");
            fails++;
        }
        else if (hoverLabels.Length > 1)
        {
            Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 16: Multiple THSingleMapHoverLabel found ({hoverLabels.Length})");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 16: Exactly one THSingleMapHoverLabel");

        // --- CHECK 17: All enemies have blocksMovement=true ---
        var enemiesNotBlocking = enemies.Where(e => !e.blocksMovement).ToList();
        if (enemiesNotBlocking.Count > 0)
        {
            Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 17: {enemiesNotBlocking.Count} enemies have blocksMovement=false");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 17: All enemies have blocksMovement=true");

        // --- CHECK 18: All enemies have startsCombat=true ---
        var enemiesNoCombat = enemies.Where(e => !e.startsCombat).ToList();
        if (enemiesNoCombat.Count > 0)
        {
            Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 18: {enemiesNoCombat.Count} enemies have startsCombat=false");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 18: All enemies have startsCombat=true");

        // --- CHECK 19: No duplicate object IDs ---
        var idGroups = allMO
            .Where(o => !string.IsNullOrWhiteSpace(o.id))
            .GroupBy(o => o.id)
            .Where(g => g.Count() > 1)
            .ToList();
        if (idGroups.Count > 0)
        {
            foreach (var g in idGroups)
                Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 19: Duplicate id '{g.Key}' found {g.Count()} times");
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 19: No duplicate object IDs");

        // --- CHECK 20: All GameObjects with SpriteRenderer on map objects are not missing sprite ---
        var mapObjectsMissingSprite = allMO
            .Where(o => {
                var sr = o.GetComponent<SpriteRenderer>();
                return sr == null || sr.sprite == null;
            }).ToList();
        if (mapObjectsMissingSprite.Count > 0)
        {
            Debug.LogWarning($"[TheHeroMapValidation] FAIL CHECK 20: {mapObjectsMissingSprite.Count} map objects missing SpriteRenderer or sprite: " +
                string.Join(", ", mapObjectsMissingSprite.Take(5).Select(o => o.id)));
            fails++;
        }
        else
            Debug.Log("[TheHeroMapValidation] PASS CHECK 20: All map objects have SpriteRenderer with sprite");

        Debug.Log($"[TheHeroMapValidation] Summary: {20 - fails}/20 checks passed, {fails} failed.");
        return fails;
    }
}
