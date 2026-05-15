using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace TheHero.Editor
{
    public static class TheHeroValidateProjectStability
    {
        private static readonly string[] MainScenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Map.unity",
            "Assets/Scenes/Combat.unity",
            "Assets/Scenes/Base.unity"
        };

        private static readonly HashSet<string> DeprecatedMovementTypes = new HashSet<string>
        {
            "THReliableHeroMovement",
            "THGuaranteedHeroMovement",
            "THHeroMover",
            "HeroMovement",
            "PlayerMovement"
        };

        private static int _failures;

        [MenuItem("The Hero/Validation/Validate Project Stability")]
        public static void ValidateProjectStability()
        {
            int failures = RunValidation();
            if (Application.isBatchMode)
                EditorApplication.Exit(failures == 0 ? 0 : 1);
        }

        public static int RunValidation()
        {
            _failures = 0;

            ValidateScenesAndBuildSettings();
            ValidateSceneLoadingCode();
            ValidateMainMenu();
            ValidateMap();
            ValidateCombat();
            ValidateBase();
            ValidateCodeCleanup();

            if (_failures == 0)
                Debug.Log("[TheHeroStability] PASS Project stability validation");
            else
                Debug.LogError($"[TheHeroStability] FAIL Project stability validation ({_failures} issue(s))");

            return _failures;
        }

        private static void ValidateScenesAndBuildSettings()
        {
            foreach (string scenePath in MainScenes)
                Check(File.Exists(scenePath), $"{scenePath} exists");

            var enabledScenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path.Replace('\\', '/'))
                .ToArray();
            Check(enabledScenes.SequenceEqual(MainScenes), "Build Settings contain only the four main scenes in order");

            foreach (string profilePath in Directory.Exists("Assets/Settings/Build Profiles")
                         ? Directory.GetFiles("Assets/Settings/Build Profiles", "*.asset")
                         : new string[0])
            {
                string text = File.ReadAllText(profilePath);
                var paths = Regex.Matches(text, @"m_path:\s*(Assets/Scenes/[^\r\n]+\.unity)")
                    .Cast<Match>()
                    .Select(m => m.Groups[1].Value.Replace('\\', '/'))
                    .ToArray();
                if (paths.Length > 0)
                    Check(paths.SequenceEqual(MainScenes), $"{profilePath} contains only the four main scenes in order");
            }
        }

        private static void ValidateSceneLoadingCode()
        {
            Check(File.Exists("Assets/Scripts/TheHeroGenerated/THSceneLoader.cs"), "THSceneLoader exists");
            Check(!AnyCsMatch(@"SceneManager\.LoadScene(?:Async)?\s*\(\s*\d+"), "No LoadScene(int) or LoadSceneAsync(int)");
        }

        private static void ValidateMainMenu()
        {
            if (!OpenMainScene("MainMenu")) return;

            var controller = Object.FindAnyObjectByType<THCleanMainMenuController>();
            Check(controller != null, "THCleanMainMenuController exists in MainMenu");
            Check(controller != null && controller.NewGameButton != null || FindButton("NewGame") != null || FindButton("New Game") != null,
                "New Game button exists");
        }

        private static void ValidateMap()
        {
            if (!OpenMainScene("Map")) return;

            GameObject hero = FindGameObject("Hero");
            Check(hero != null, "Hero exists");
            Check(hero != null && hero.GetComponent<THStrictGridHeroMovement>() != null, "Hero has THStrictGridHeroMovement");
            Check(hero != null && !hero.GetComponents<MonoBehaviour>().Any(c => c != null && DeprecatedMovementTypes.Contains(c.GetType().Name)),
                "Hero does not have deprecated movement scripts");

            Camera camera = Camera.main ?? Object.FindAnyObjectByType<Camera>();
            var follow = camera != null ? camera.GetComponent<THCameraFollow>() : null;
            Check(camera != null && follow != null, "Main Camera has THCameraFollow");
            Check(camera != null && camera.orthographic, "Main Camera is orthographic");
            Check(hero != null && follow != null && follow.Target == hero.transform, "Camera target is Hero");

            Check(Object.FindAnyObjectByType<Grid>() != null || Object.FindAnyObjectByType<Tilemap>() != null, "Map has Tilemap/Grid");
            Check(Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
                    .Any(c => c.GetComponent<THMapUIRuntime>() != null || c.name.Contains("Map")),
                "Map has Map UI Canvas");

            var objects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            Check(objects.Any(o => o.type == THMapObject.ObjectType.Base || o.name.Contains("Castle")), "Castle exists");
            Check(objects.Any(o => o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord), "Enemies exist");
            Check(objects.Any(IsResourceObject), "Resources exist");
            Check(objects.Any(o => o.isDarkLord && o.type == THMapObject.ObjectType.Enemy), "DarkLord exists and is Enemy");
        }

        private static void ValidateCombat()
        {
            if (!OpenMainScene("Combat")) return;

            Check(Object.FindAnyObjectByType<THCombatRuntime>() != null, "THCombatRuntime exists");
            Check(Object.FindAnyObjectByType<Canvas>() != null, "Combat scene has Combat UI root");
        }

        private static void ValidateBase()
        {
            if (!OpenMainScene("Base")) return;

            Check(Object.FindAnyObjectByType<THBaseRuntime>() != null, "THBaseRuntime exists");
            Check(FindButton("BackToMap") != null || FindButton("Back") != null, "BackToMap button exists");
        }

        private static void ValidateCodeCleanup()
        {
            Check(!AssetDatabase.IsValidFolder("Assets/Scripts/Domain"), "No Domain folder in Assets/Scripts");
            Check(!AssetDatabase.IsValidFolder("Assets/Scripts/Subsystems"), "No Subsystems folder in Assets/Scripts");
            Check(!AssetDatabase.IsValidFolder("Assets/Scripts/UI"), "No old UI folder in Assets/Scripts");

            Check(!File.Exists("Assets/Scripts/TheHeroGenerated/THMainMenuController.cs") &&
                  !File.Exists("Assets/Scripts/TheHeroGenerated/THMainMenuControllerFixed.cs"),
                "No old MainMenuController duplicates");

            Check(!File.Exists("Assets/Scripts/TheHeroGenerated/THReliableHeroMovement.cs") &&
                  !File.Exists("Assets/Scripts/TheHeroGenerated/THGuaranteedHeroMovement.cs") &&
                  !File.Exists("Assets/Scripts/TheHeroGenerated/THHeroMover.cs"),
                "No deprecated movement scripts in main codebase");

            Check(!AnyEditorCsMatch(@"\[" + "Initialize" + @"OnLoad(?:Method)?\]"), "No editor startup auto-fixers");
            Check(!AnyEditorCsMatch(@"EditorApplication\." + "delay" + "Call"), "No editor delayed map builders");
        }

        private static bool OpenMainScene(string sceneName)
        {
            string path = $"Assets/Scenes/{sceneName}.unity";
            if (!File.Exists(path))
            {
                Check(false, $"{sceneName} scene can be opened");
                return false;
            }

            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            return true;
        }

        private static bool IsResourceObject(THMapObject obj)
        {
            return obj.type == THMapObject.ObjectType.GoldResource ||
                   obj.type == THMapObject.ObjectType.WoodResource ||
                   obj.type == THMapObject.ObjectType.StoneResource ||
                   obj.type == THMapObject.ObjectType.ManaResource ||
                   obj.type == THMapObject.ObjectType.Treasure ||
                   obj.type == THMapObject.ObjectType.Artifact;
        }

        private static GameObject FindGameObject(string exactName)
        {
            return Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
                .FirstOrDefault(go => go.name == exactName);
        }

        private static Button FindButton(string namePart)
        {
            return Object.FindObjectsByType<Button>(FindObjectsInactive.Include)
                .FirstOrDefault(b => b.name.Contains(namePart));
        }

        private static bool AnyCsMatch(string pattern)
        {
            return Directory.GetFiles("Assets", "*.cs", SearchOption.AllDirectories)
                .Any(path => Regex.IsMatch(File.ReadAllText(path), pattern));
        }

        private static bool AnyEditorCsMatch(string pattern)
        {
            return Directory.Exists("Assets/Editor") &&
                   Directory.GetFiles("Assets/Editor", "*.cs", SearchOption.AllDirectories)
                       .Any(path => Regex.IsMatch(File.ReadAllText(path), pattern));
        }

        private static void Check(bool condition, string message)
        {
            if (condition)
            {
                Debug.Log("[TheHeroStability] PASS " + message);
                return;
            }

            _failures++;
            Debug.LogError("[TheHeroStability] FAIL " + message);
        }
    }
}
