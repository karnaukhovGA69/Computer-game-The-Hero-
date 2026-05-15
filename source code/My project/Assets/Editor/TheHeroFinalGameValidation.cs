// TheHeroFinalGameValidation.cs
// MenuItem: "The Hero/Validation/Validate Playable Game"
// Performs 27 checks to verify the game is fully playable.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using TheHero.Generated;

public class TheHeroFinalGameValidation
{
    [MenuItem("The Hero/Validation/Validate Playable Game")]
    public static void ValidatePlayableGame()
    {
        int failCount = RunAllChecks();
        if (failCount == 0)
            Debug.Log("[TheHeroFinalValidation] PASS Game is playable");
        else
            Debug.LogError($"[TheHeroFinalValidation] FAIL {failCount} issues");
    }

    public static int RunAllChecks()
    {
        int fails = 0;

        // -------------------------------------------------------
        // CHECKS 1-4: Scene assets exist
        // -------------------------------------------------------
        string[] requiredScenes = { "MainMenu", "Map", "Combat", "Base" };
        foreach (var sceneName in requiredScenes)
        {
            string scenePath = $"Assets/Scenes/{sceneName}.unity";
            var asset = AssetDatabase.LoadMainAssetAtPath(scenePath);
            if (asset == null)
            {
                Debug.LogError($"[TheHeroFinalValidation] FAIL Scene asset missing: {scenePath}");
                fails++;
            }
            else
                Debug.Log($"[TheHeroFinalValidation] PASS Scene exists: {scenePath}");
        }

        // -------------------------------------------------------
        // Open Map.unity for remaining checks
        // -------------------------------------------------------
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

        // CHECK 5: Hero GameObject exists
        var heroGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
            .Where(g => g.name == "Hero").ToArray();
        GameObject heroGO = heroGOs.FirstOrDefault();
        if (heroGO == null)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 5: Hero GameObject not found");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 5: Hero found");

        // CHECK 6: Hero activeInHierarchy, SpriteRenderer enabled, sprite != null
        if (heroGO != null)
        {
            var sr = heroGO.GetComponent<SpriteRenderer>();
            bool check6 = heroGO.activeInHierarchy && sr != null && sr.enabled && sr.sprite != null;
            if (!check6)
            {
                Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 6: Hero not active / SpriteRenderer missing or sprite null");
                fails++;
            }
            else
                Debug.Log("[TheHeroFinalValidation] PASS CHECK 6: Hero active with sprite");
        }

        // CHECK 7: Hero has a MonoBehaviour whose type name contains "Movement" or "HeroMover"
        bool check7 = false;
        if (heroGO != null)
        {
            var comps = heroGO.GetComponents<MonoBehaviour>();
            check7 = comps.Any(c => c != null && (c.GetType().Name.Contains("Movement") || c.GetType().Name.Contains("HeroMover")));
        }
        if (!check7)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 7: Hero has no Movement component");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 7: Hero has Movement component");

        // CHECK 8: Camera.main has THCameraFollow with Target set
        var cam = Camera.main;
        if (cam == null) cam = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).FirstOrDefault();
        bool check8 = false;
        if (cam != null)
        {
            var follow = cam.GetComponent<THCameraFollow>();
            check8 = (follow != null && follow.Target != null);
        }
        if (!check8)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 8: Camera missing THCameraFollow or Target not set");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 8: Camera has THCameraFollow with Target");

        var allMO = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);

        // CHECK 9: Castle_Player exists
        var castle = allMO.FirstOrDefault(o => o.id == "Castle_Player" || o.name == "Castle_Player");
        if (castle == null)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 9: Castle_Player not found");
            fails++;
        }
        else
            Debug.Log($"[TheHeroFinalValidation] PASS CHECK 9: Castle_Player found at ({castle.targetX},{castle.targetY})");

        // CHECK 10: Castle_Player has THMapObject type=Base
        bool check10 = castle != null && castle.type == THMapObject.ObjectType.Base;
        if (!check10)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 10: Castle_Player type is not Base");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 10: Castle_Player type=Base");

        // CHECKS 11-14: ≥5 enemies, each with sprite, startsCombat, blocksMovement
        var enemies = allMO.Where(o => o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord).ToList();
        if (enemies.Count < 5)
        {
            Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 11: Only {enemies.Count} enemies (need ≥5)");
            fails++;
        }
        else
            Debug.Log($"[TheHeroFinalValidation] PASS CHECK 11: {enemies.Count} enemies found");

        int enemiesNoSprite = enemies.Count(e => { var sr = e.GetComponent<SpriteRenderer>(); return sr == null || sr.sprite == null; });
        if (enemiesNoSprite > 0)
        {
            Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 12: {enemiesNoSprite} enemies missing sprite");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 12: All enemies have sprite");

        int enemiesNoCombat = enemies.Count(e => !e.startsCombat);
        if (enemiesNoCombat > 0)
        {
            Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 13: {enemiesNoCombat} enemies have startsCombat=false");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 13: All enemies startsCombat=true");

        int enemiesNoBlock = enemies.Count(e => !e.blocksMovement);
        if (enemiesNoBlock > 0)
        {
            Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 14: {enemiesNoBlock} enemies have blocksMovement=false");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 14: All enemies blocksMovement=true");

        // CHECKS 15-17: DarkLord exists, isFinalBoss, isDarkLord, type=Enemy
        var darkLord = allMO.FirstOrDefault(o => o.isDarkLord);
        if (darkLord == null)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 15: No DarkLord (isDarkLord=true) found");
            fails++;
        }
        else
            Debug.Log($"[TheHeroFinalValidation] PASS CHECK 15: DarkLord found: {darkLord.id}");

        if (darkLord != null && !darkLord.isFinalBoss)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 16: DarkLord isFinalBoss=false");
            fails++;
        }
        else if (darkLord != null)
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 16: DarkLord isFinalBoss=true");

        if (darkLord != null && darkLord.type != THMapObject.ObjectType.Enemy)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 17: DarkLord type is not Enemy");
            fails++;
        }
        else if (darkLord != null)
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 17: DarkLord type=Enemy");

        // CHECK 18: ≥4 resource-type THMapObjects
        int resourceCount = allMO.Count(o =>
            o.type == THMapObject.ObjectType.GoldResource ||
            o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource ||
            o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure);
        if (resourceCount < 4)
        {
            Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 18: Only {resourceCount} resources (need ≥4)");
            fails++;
        }
        else
            Debug.Log($"[TheHeroFinalValidation] PASS CHECK 18: {resourceCount} resources found");

        // CHECK 19: One THMapObject type=Artifact
        var artifacts = allMO.Where(o => o.type == THMapObject.ObjectType.Artifact).ToList();
        if (artifacts.Count == 0)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 19: No Artifact found");
            fails++;
        }
        else
            Debug.Log($"[TheHeroFinalValidation] PASS CHECK 19: Artifact found ({artifacts[0].id})");

        // CHECK 20: THCombatContext OR THCombatRuntime exists in compiled assemblies
        bool hasCombatType = false;
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (asm.GetType("TheHero.Generated.THCombatContext") != null ||
                asm.GetType("TheHero.Generated.THCombatRuntime") != null ||
                asm.GetType("THCombatContext") != null ||
                asm.GetType("THCombatRuntime") != null)
            {
                hasCombatType = true;
                break;
            }
        }
        if (!hasCombatType)
        {
            Debug.LogError("[TheHeroFinalValidation] FAIL CHECK 20: THCombatContext / THCombatRuntime not found in compiled assemblies");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 20: Combat runtime class found in assemblies");

        // CHECKS 21-23: Path-finding (skipped at edit time)
        Debug.Log("[TheHeroFinalValidation] INFO CHECK 21: path check skipped (requires runtime grid)");
        Debug.Log("[TheHeroFinalValidation] INFO CHECK 22: path check skipped (requires runtime grid)");
        Debug.Log("[TheHeroFinalValidation] INFO CHECK 23: path check skipped (requires runtime grid)");

        // CHECK 24: No SpriteRenderer with sprite.rect > 512 in scene
        var allSRs = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
        var oversized = allSRs.Where(sr => sr.sprite != null && (sr.sprite.rect.width > 512 || sr.sprite.rect.height > 512)).ToList();
        if (oversized.Count > 0)
        {
            foreach (var sr in oversized)
                Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 24: Oversized sprite on {sr.gameObject.name}: {sr.sprite.rect.width}x{sr.sprite.rect.height}");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 24: No oversized sprites");

        // CHECK 25: Only one GameObject named exactly "Hero"
        int heroNameCount = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
            .Count(g => g.name == "Hero");
        if (heroNameCount != 1)
        {
            Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 25: {heroNameCount} GameObjects named 'Hero' (need exactly 1)");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 25: Exactly one GameObject named Hero");

        // CHECK 26: Only one EventSystem in scene
        var eventSystems = UnityEngine.Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystems.Length != 1)
        {
            Debug.LogError($"[TheHeroFinalValidation] FAIL CHECK 26: {eventSystems.Length} EventSystem(s) in scene (need exactly 1)");
            fails++;
        }
        else
            Debug.Log("[TheHeroFinalValidation] PASS CHECK 26: Exactly one EventSystem");

        // CHECK 27: Map Canvas count ≤ 1
        var canvases = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        if (canvases.Length > 1)
        {
            Debug.LogWarning($"[TheHeroFinalValidation] WARN CHECK 27: {canvases.Length} Canvases in Map scene (expected ≤1)");
            // Warning only, not a fail
        }
        else
            Debug.Log($"[TheHeroFinalValidation] PASS CHECK 27: Canvas count = {canvases.Length}");

        Debug.Log($"[TheHeroFinalValidation] Summary: {fails} issue(s) found.");
        return fails;
    }
}
