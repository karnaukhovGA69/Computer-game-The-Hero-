using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;

public static class TheHeroDemoValidation
{
    [MenuItem("The Hero/Validation/Run Demo Validation")]
    public static void RunValidationMenu()
    {
        bool result = RunValidation();
        if (result) Debug.Log("[TheHeroDemoValidation] SUCCESS: Project is ready for demo build.");
        else Debug.LogError("[TheHeroDemoValidation] FAILED: Project has issues.");
    }

    public static bool RunValidation()
    {
        bool pass = true;
        
        string[] requiredScenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Map.unity", "Assets/Scenes/Combat.unity", "Assets/Scenes/Base.unity" };
        foreach (var s in requiredScenes)
        {
            if (!File.Exists(s)) { Debug.LogError("[TheHeroDemoValidation] Missing Scene: " + s); pass = false; }
            else Debug.Log("[TheHeroDemoValidation] Found Scene: " + s);
        }

        if (EditorBuildSettings.scenes.Length < 4) { Debug.LogError("[TheHeroDemoValidation] Build Settings missing scenes."); pass = false; }
        
        // Scripts check
        string[] reqClasses = { "THDemoCampaignController", "THInfoDialogPanel", "THMiniMap", "THGameState", "THSaveSystem" };
        foreach (var c in reqClasses)
        {
            var guid = AssetDatabase.FindAssets(c + " t:MonoScript");
            if (guid.Length == 0) { Debug.LogError("[TheHeroDemoValidation] Missing Class: " + c); pass = false; }
            else Debug.Log("[TheHeroDemoValidation] Found Class: " + c);
        }

        return pass;
    }
}