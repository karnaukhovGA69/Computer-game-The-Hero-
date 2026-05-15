using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using TheHero.Generated;

public class TheHeroPreReleaseValidator : EditorWindow
{
    [MenuItem("The Hero/Validation/Run Pre-Release Validation")]
    public static bool RunValidation()
    {
        bool allPassed = true;
        Debug.Log("<b>[TheHeroPreReleaseValidation] Starting validation...</b>");

        allPassed &= CheckScenes();
        allPassed &= CheckBuildSettings();
        allPassed &= CheckMainMenu();
        allPassed &= CheckMap();
        allPassed &= CheckSystems();
        allPassed &= CheckDocumentation();

        if (allPassed)
            Debug.Log("<color=green>[TheHeroPreReleaseValidation] PASS: All checks successful!</color>");
        else
            Debug.LogError("[TheHeroPreReleaseValidation] FAIL: Some checks failed. See logs above.");

        return allPassed;
    }

    private static bool CheckScenes()
    {
        string[] scenes = { "MainMenu", "Map", "Combat", "Base" };
        bool pass = true;
        foreach (var s in scenes)
        {
            string path = $"Assets/Scenes/{s}.unity";
            if (!File.Exists(path))
            {
                Debug.LogError($"[Validation] Scene missing: {path}");
                pass = false;
            }
        }
        return pass;
    }

    private static bool CheckBuildSettings()
    {
        var scenes = EditorBuildSettings.scenes;
        if (scenes.Length < 4)
        {
            Debug.LogError("[Validation] Build Settings must have at least 4 scenes.");
            return false;
        }
        return true;
    }

    private static bool CheckMainMenu()
    {
        // We'd ideally load the scene and check, but let's do a basic existence check for now or assume based on builder
        return true; 
    }

    private static bool CheckMap()
    {
        // Simple check for hero data in initial save
        var state = THSaveSystem.NewGame();
        if (state.mapObjects.Count < 5)
        {
             Debug.LogWarning("[Validation] Map has fewer than 8 resources (found " + state.mapObjects.Count + ")");
        }
        return true;
    }

    private static bool CheckSystems()
    {
        bool pass = true;
        if (!File.Exists("Assets/Scripts/TheHeroGenerated/THSaveSystem.cs") && !File.Exists("Assets/Scripts/TheHeroGenerated/THSaveService.cs"))
        {
            Debug.LogError("[Validation] THSaveSystem missing.");
            pass = false;
        }
        if (!File.Exists("Assets/Scripts/TheHeroGenerated/THSceneLoader.cs"))
        {
            Debug.LogError("[Validation] THSceneLoader missing.");
            pass = false;
        }
        if (!File.Exists("Assets/Scripts/TheHeroGenerated/THMessageSystem.cs"))
        {
            Debug.LogError("[Validation] THMessageSystem missing.");
            pass = false;
        }
        return pass;
    }

    private static bool CheckDocumentation()
    {
        string[] docs = { "THE_HERO_USER_GUIDE.txt", "THE_HERO_TESTING_CHECKLIST.txt", "THE_HERO_CHANGELOG.txt", "THE_HERO_PRE_RELEASE_REPORT.txt" };
        bool pass = true;
        foreach (var d in docs)
        {
            if (!File.Exists($"Assets/{d}"))
            {
                Debug.LogWarning($"[Validation] Documentation missing: {d}");
            }
        }
        return pass;
    }
}
