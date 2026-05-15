using UnityEngine;
using UnityEditor;
using System.IO;

public static class TheHeroValidationRunner
{
    [MenuItem("The Hero/Validation/Run Project Validation")]
    public static void RunValidationMenu()
    {
        bool result = RunValidation();
        if (result) Debug.Log("[TheHeroValidation] Final check: SUCCESS");
        else Debug.LogError("[TheHeroValidation] Final check: FAILED");
    }

    public static bool RunValidation()
    {
        bool pass = true;
        
        // Scenes
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Map.unity", "Assets/Scenes/Combat.unity", "Assets/Scenes/Base.unity" };
        foreach(var s in scenes)
        {
            if(!File.Exists(s)) { Debug.LogError("[TheHeroValidation] FAIL: Missing scene " + s); pass = false; }
            else Debug.Log("[TheHeroValidation] PASS: Found scene " + s);
        }

        // Build Settings
        if (EditorBuildSettings.scenes.Length < 4) { Debug.LogError("[TheHeroValidation] FAIL: Build Settings must have 4 scenes."); pass = false; }
        else Debug.Log("[TheHeroValidation] PASS: Build Settings has required scenes.");

        // Important scripts check
        string[] requiredScripts = { "THGameState", "THSaveSystem", "THMapController", "THQuestSystem" };
        foreach(var rs in requiredScripts)
        {
            var assets = AssetDatabase.FindAssets(rs + " t:MonoScript");
            if (assets.Length == 0) { Debug.LogError("[TheHeroValidation] FAIL: Script not found: " + rs); pass = false; }
            else Debug.Log("[TheHeroValidation] PASS: Found script " + rs);
        }

        return pass;
    }
}
