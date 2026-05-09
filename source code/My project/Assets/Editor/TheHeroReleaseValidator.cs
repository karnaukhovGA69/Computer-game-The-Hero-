using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using TheHero.Generated;

public class TheHeroReleaseValidator : EditorWindow
{
    [MenuItem("The Hero/Validation/Run Release Validation")]
    public static bool RunValidation()
    {
        bool allPassed = true;
        Debug.Log("<b>[TheHeroReleaseValidation] Starting validation...</b>");

        allPassed &= CheckScenes();
        allPassed &= CheckBuildSettings();
        allPassed &= CheckSystems();
        allPassed &= CheckDocumentation();

        if (allPassed)
            Debug.Log("<color=green>[TheHeroReleaseValidation] PASS: All checks successful!</color>");
        else
            Debug.LogError("[TheHeroReleaseValidation] FAIL: Some checks failed. See logs above.");

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
        if (scenes[0].path != "Assets/Scenes/MainMenu.unity")
        {
            Debug.LogError("[Validation] MainMenu must be the first scene in Build Settings.");
            return false;
        }
        return true;
    }

    private static bool CheckSystems()
    {
        bool pass = true;
        string[] required = { "THSaveSystem.cs", "THSceneLoader.cs", "THMessageSystem.cs", "THManager.cs", "THSystemInitializer.cs" };
        foreach (var r in required)
        {
            if (!File.Exists($"Assets/Scripts/TheHeroGenerated/{r}"))
            {
                Debug.LogError($"[Validation] System missing: {r}");
                pass = false;
            }
        }
        return pass;
    }

    private static bool CheckDocumentation()
    {
        string[] docs = { "THE_HERO_RELEASE_REPORT.txt", "THE_HERO_USER_GUIDE.txt", "THE_HERO_TESTING_CHECKLIST.txt", "THE_HERO_KNOWN_LIMITATIONS.txt" };
        bool pass = true;
        foreach (var d in docs)
        {
            if (!File.Exists($"Assets/{d}"))
            {
                Debug.LogWarning($"[Validation] Documentation missing: {d}");
                pass = false;
            }
        }
        return pass;
    }
}
