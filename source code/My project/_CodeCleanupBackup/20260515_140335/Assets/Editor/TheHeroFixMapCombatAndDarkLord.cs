using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;

public class TheHeroFixMapCombatAndDarkLord : EditorWindow
{
    [MenuItem("The Hero/Fix/Fix Labels Guards Blockers Combat Result And DarkLord")]
    public static void FixAll()
    {
        FixMapScene();
        FixCombatScene();
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[TheHeroFix] All fixes applied.");
    }

    private static void FixMapScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
        
        // 1. Hover Labels Cleanup
        var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allObjects)
        {
            if (go.name == "Tooltip" || go.name == "Tooltip(Clone)" || go.name.Contains("HoverLabel") || go.name == "MapHoverLabel")
            {
                if (go.GetComponent<THSingleMapHoverLabel>() == null)
                    Undo.DestroyObjectImmediate(go);
            }
        }
        
        // Ensure Single Hover Label instance exists in scene
        var existing = Object.FindAnyObjectByType<THSingleMapHoverLabel>();
        if (existing == null)
        {
            var go = new GameObject("MapHoverLabelController", typeof(THSingleMapHoverLabel));
            Undo.RegisterCreatedObjectUndo(go, "Create Hover Label Controller");
        }

        // 2. Resource Guards & Mine Fix
        var objectsRoot = GameObject.Find("MapRoot/Objects") ?? GameObject.Find("Objects");
        if (objectsRoot == null) objectsRoot = new GameObject("Objects");

        var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include).ToList();
        
        // Mine Fix: Remove broken and add one good
        var mines = mapObjects.Where(o => o != null && o.type == THMapObject.ObjectType.Mine).ToList();
        foreach (var mine in mines)
        {
            if (mine == null) continue;

            // Remove guards near mine without sprites
            var brokenGuards = mapObjects.Where(o => o != null && o.type == THMapObject.ObjectType.Enemy && 
                Vector2.Distance(o.transform.position, mine.transform.position) < 1.5f &&
                (o.GetComponent<SpriteRenderer>() == null || o.GetComponent<SpriteRenderer>().sprite == null)).ToList();
            
            foreach (var bg in brokenGuards) Undo.DestroyObjectImmediate(bg.gameObject);
            
            // Re-fetch current objects for precise check
            var currentObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            bool hasGuard = currentObjects.Any(o => o != null && o.type == THMapObject.ObjectType.Enemy && 
                Vector2.Distance(o.transform.position, mine.transform.position) < 1.5f && 
                o.GetComponent<SpriteRenderer>()?.sprite != null);
            
            if (!hasGuard)
            {
                AddGuard(mine, "Orc Mine Guard", "Enemy_OrcMineGuard", "Assets/Resources/Sprites/CleanMap/Objects/clean_orc.png", 150, 10, 80);
            }
        }

        // Add guards for other resources
        var resources = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .Where(o => o != null && o.type != THMapObject.ObjectType.Enemy && o.type != THMapObject.ObjectType.Base).ToList();
            
        foreach (var res in resources)
        {
            if (res == null) continue;

            var currentObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            bool hasGuard = currentObjects.Any(o => o != null && o.type == THMapObject.ObjectType.Enemy && Vector2.Distance(o.transform.position, res.transform.position) < 1.5f);
            
            if (!hasGuard)
            {
                string guardName = "Guardian";
                string spritePath = "Assets/Resources/Sprites/CleanMap/Objects/clean_orc.png";
                if (res.type == THMapObject.ObjectType.GoldResource) guardName = "Goblin Guard";
                else if (res.type == THMapObject.ObjectType.WoodResource) guardName = "Wolf Guard";
                else if (res.type == THMapObject.ObjectType.StoneResource) guardName = "Orc Guard";
                else if (res.type == THMapObject.ObjectType.ManaResource) guardName = "Skeleton Guard";
                else if (res.type == THMapObject.ObjectType.Treasure) guardName = "Bandit Guard";

                AddGuard(res, guardName, "Guard_" + res.id, spritePath, 50, 0, 30);
            }
        }

        // 3. Dark Lord Sprite & Logic
        var darkLord = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.isDarkLord || o.displayName.Contains("Лорд"));
            
        if (darkLord != null)
        {
            darkLord.type = THMapObject.ObjectType.Enemy;
            darkLord.blocksMovement = true;
            darkLord.startsCombat = true;
            darkLord.isFinalBoss = true;
            darkLord.id = "dark_lord_final";
            darkLord.displayName = "Тёмный Лорд";
            
            var sr = darkLord.GetComponent<SpriteRenderer>() ?? darkLord.gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_dark_boss.png");
            sr.sortingOrder = 30;
            darkLord.transform.localScale = Vector3.one * 1.2f;
            
            THBalanceConfig.ConfigureMapObjectBalance(darkLord);
        }

        // Enable blockers on all enemies
        var allEnemies = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .Where(o => o.type == THMapObject.ObjectType.Enemy).ToList();
        foreach (var e in allEnemies)
        {
            e.blocksMovement = true;
            e.startsCombat = true;
            EditorUtility.SetDirty(e);
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void AddGuard(THMapObject res, string name, string id, string spritePath, int g, int s, int exp)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(res.transform.parent);
        
        // Find adjacent spot (simple)
        go.transform.position = res.transform.position + Vector3.right;
        
        var mo = go.AddComponent<THMapObject>();
        mo.id = id;
        mo.type = THMapObject.ObjectType.Enemy;
        mo.displayName = name;
        mo.targetX = (int)go.transform.position.x;
        mo.targetY = (int)go.transform.position.y;
        mo.rewardGold = g;
        mo.rewardStone = s;
        mo.rewardExp = exp;
        mo.blocksMovement = true;
        mo.startsCombat = true;
        
        THBalanceConfig.ConfigureMapObjectBalance(mo);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        sr.sortingOrder = 25;
        
        Undo.RegisterCreatedObjectUndo(go, "Add Resource Guard");
        }

    private static void FixCombatScene()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity");
        
        var panel = GameObject.Find("ResultPanel");
        if (panel == null)
        {
             var canvas = Object.FindAnyObjectByType<Canvas>();
             if (canvas != null)
             {
                 var t = canvas.transform.Find("ResultPanel");
                 if (t != null) panel = t.gameObject;
             }
        }

        if (panel != null)
        {
            Undo.RecordObject(panel.transform, "Fix Result Panel Layout");
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(540, 360);

            var finishBtn = panel.transform.Find("FinishBattleButton");
            if (finishBtn == null)
            {
                 // Search in whole scene and move it in
                 var allButtons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
                 foreach (var b in allButtons)
                 {
                     if (b.name == "FinishBattleButton" || b.name == "FinishButton" || b.GetComponentInChildren<Text>()?.text.Contains("ЗАВЕРШИТЬ") == true)
                     {
                         finishBtn = b.transform;
                         Undo.SetTransformParent(finishBtn, panel.transform, "Move Finish Button");
                         break;
                     }
                 }
            }

            if (finishBtn != null)
            {
                var brt = finishBtn.GetComponent<RectTransform>();
                brt.anchorMin = new Vector2(0.5f, 0);
                brt.anchorMax = new Vector2(0.5f, 0);
                brt.pivot = new Vector2(1.05f, 0);
                brt.anchoredPosition = new Vector2(0, 30);
                brt.sizeDelta = new Vector2(220, 50);
            }

            var menuBtn = panel.transform.Find("MainMenuButton");
            if (menuBtn != null)
            {
                var mrt = menuBtn.GetComponent<RectTransform>();
                mrt.anchorMin = new Vector2(0.5f, 0);
                mrt.anchorMax = new Vector2(0.5f, 0);
                mrt.pivot = new Vector2(-0.05f, 0);
                mrt.anchoredPosition = new Vector2(0, 30);
                mrt.sizeDelta = new Vector2(220, 50);
            }
            
            // Fix text positioning
            var body = panel.transform.Find("ResultBodyText");
            if (body != null)
            {
                var rrt = body.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0, 0);
                rrt.anchorMax = new Vector2(1, 1);
                rrt.offsetMin = new Vector2(20, 100);
                rrt.offsetMax = new Vector2(-20, -80);
            }
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }
}
