using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroKingsBountyStyleValidation
    {
        public static List<string> Results = new List<string>();
        public static bool AllPassed = true;

        [MenuItem("The Hero/Validation/Run KB-Style Quality Validation")]
        public static void RunValidation()
        {
            Results.Clear();
            AllPassed = true;

            Debug.Log("<b>[TheHeroValidation] Starting King's Bounty Style Quality Validation...</b>");

            ValidateProjectLevel();
            ValidateMainMenu();
            ValidateMap();
            ValidateCombat();
            ValidateBase();

            ReportResults();
        }

        private static void ValidateProjectLevel()
        {
            LogStep("Checking Build Settings...");
            string[] requiredScenes = { "MainMenu", "Map", "Combat", "Base" };
            var activeScenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path)).ToList();
            
            bool missing = false;
            foreach (var rs in requiredScenes)
            {
                if (!activeScenes.Contains(rs))
                {
                    LogFail($"Scene '{rs}' is missing or disabled in Build Settings.");
                    missing = true;
                }
            }
            if (!missing) LogPass("Build Settings are correct.");

            LogStep("Checking for Missing Scripts in project assets...");
            string[] allPrefabs = AssetDatabase.FindAssets("t:Prefab");
            int missingCount = 0;
            foreach (var guid in allPrefabs)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (go != null) missingCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
            }
            if (missingCount > 0) LogFail($"Found {missingCount} missing scripts in prefabs.");
            else LogPass("No missing scripts in project assets.");
        }

        private static void ValidateMainMenu()
        {
            LogStep("Validating MainMenu Scene...");
            if (!OpenScene("Assets/Scenes/MainMenu.unity")) return;

            var polish = Object.FindAnyObjectByType<THFantasyMainMenuPolish>();
            if (polish != null) polish.ApplyPolish();
            
            ForceStyleAllButtons();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            LogPass("Main Menu styled and saved.");

            CheckButtons();
        }

        private static void ValidateMap()
        {
            LogStep("Validating Map Scene...");
            if (!OpenScene("Assets/Scenes/Map.unity")) return;

            var polish = Object.FindAnyObjectByType<THMapUIPolish>();
            if (polish != null) polish.ApplyPolish();

            ForceStyleAllButtons();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            var tiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            if (tiles.Length < 280) LogFail($"Map too small: {tiles.Length} tiles found (expected 20x14 = 280).");
            else LogPass($"Map size is sufficient ({tiles.Length} tiles).");

            var hero = GameObject.Find("Hero");
            if (hero == null) LogFail("Hero object not found.");
            else
            {
                var sr = hero.GetComponent<SpriteRenderer>();
                if (sr == null || sr.sprite == null) LogFail("Hero has no sprite.");
                else LogPass("Hero is visible.");

                var mover = hero.GetComponent<THStrictGridHeroMovement>();
                if (mover == null) LogFail("Hero is missing THStrictGridHeroMovement.");
                else LogPass("Hero uses grid movement.");
            }

            var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            int resources = mapObjects.Count(o => o.type == THMapObject.ObjectType.GoldResource || o.type == THMapObject.ObjectType.WoodResource || o.type == THMapObject.ObjectType.StoneResource || o.type == THMapObject.ObjectType.ManaResource);
            int enemies = mapObjects.Count(o => o.type == THMapObject.ObjectType.Enemy);
            int castles = mapObjects.Count(o => o.type == THMapObject.ObjectType.Base);
            bool darkLord = mapObjects.Any(o => o.isDarkLord);

            if (resources == 0) LogFail("No resources found on map.");
            else LogPass($"{resources} resources found.");

            if (enemies == 0) LogFail("No enemies found on map.");
            else LogPass($"{enemies} enemies found.");

            if (castles == 0) LogFail("No Castle (Base) found on map.");
            else LogPass("Castle found.");

            if (!darkLord) LogFail("DarkLord Boss not found on map.");
            else LogPass("DarkLord found.");

            CheckButtons();
        }

        private static void ValidateCombat()
        {
            LogStep("Validating Combat Scene...");
            if (!OpenScene("Assets/Scenes/Combat.unity")) return;

            var polish = Object.FindAnyObjectByType<THCombatPolish>();
            if (polish != null) polish.ApplyPolish();

            ForceStyleAllButtons();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            var controller = Object.FindAnyObjectByType<THCombatController>();
            if (controller == null) LogFail("THCombatController missing.");
            else
            {
                if (controller.VictoryPanel == null || controller.DefeatPanel == null) LogFail("Victory/Defeat panels not assigned.");
                else LogPass("Combat logic is set up.");
            }

            CheckButtons();
        }

        private static void ValidateBase()
        {
            LogStep("Validating Base Scene...");
            if (!OpenScene("Assets/Scenes/Base.unity")) return;

            var polish = Object.FindAnyObjectByType<THBaseUIPolish>();
            if (polish != null) polish.SetupUI();

            ForceStyleAllButtons();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            LogPass("Base UI styled and saved.");

            CheckButtons();
        }

        private static void ForceStyleAllButtons()
        {
            var normal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/UI/button_fantasy_normal.png");
            var hover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/UI/button_fantasy_hover.png");
            var pressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/UI/button_fantasy_pressed.png");

            if (normal == null) return;

            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var b in buttons)
            {
                var img = b.GetComponent<UnityEngine.UI.Image>();
                if (img == null) img = b.gameObject.AddComponent<UnityEngine.UI.Image>();

                if (img.sprite == null || img.sprite.name == "UISprite")
                {
                    img.sprite = normal;
                    b.transition = Selectable.Transition.SpriteSwap;
                    var ss = b.spriteState;
                    ss.highlightedSprite = hover;
                    ss.pressedSprite = pressed;
                    b.spriteState = ss;
                }
            }
        }

        private static bool OpenScene(string path)
        {
            if (System.IO.File.Exists(path))
            {
                EditorSceneManager.OpenScene(path);
                return true;
            }
            LogFail($"Scene missing: {path}");
            return false;
        }

        private static void CheckButtons()
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            int whiteButtons = 0;
            foreach (var b in buttons)
            {
                var img = b.GetComponent<UnityEngine.UI.Image>();
                if (img != null && (img.sprite == null || img.sprite.name == "UISprite"))
                {
                    whiteButtons++;
                }
            }
            if (whiteButtons > 0) LogFail($"Found {whiteButtons} default white Unity buttons in current scene.");
            else LogPass("No white buttons found in current scene.");
        }

        private static void LogStep(string msg) => Results.Add($"\n<b>--- {msg} ---</b>");
        private static void LogPass(string msg) => Results.Add($"<color=green>[PASS]</color> {msg}");
        private static void LogFail(string msg) { Results.Add($"<color=red>[FAIL]</color> {msg}"); AllPassed = false; }

        private static void ReportResults()
        {
            Debug.Log(string.Join("\n", Results));
            if (AllPassed)
            {
                Debug.Log("<color=green><b>[TheHeroValidation] PROJECT READY FOR BUILD! King's Bounty style requirements met.</b></color>");
            }
            else
            {
                Debug.LogError("[TheHeroValidation] QUALITY VALIDATION FAILED! Check log for issues.");
            }
        }
}
}