using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Linq;

public static class TheHeroPlaythroughValidator
{
    [MenuItem("The Hero/Validation/Run Full Playthrough Validation")]
    public static bool RunValidation()
    {
        bool pass = true;
        Debug.Log("[TH Validation] Starting Full Playthrough Validation...");

        // 1. Scene existence
        string[] scenes = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Map.unity", "Assets/Scenes/Combat.unity", "Assets/Scenes/Base.unity" };
        foreach (var s in scenes)
        {
            if (!File.Exists(s)) { LogResult("Scene Check: " + s, false); pass = false; }
            else LogResult("Scene Check: " + s, true);
        }

        // 2. Build Settings
        if (EditorBuildSettings.scenes.Length < 4) { LogResult("Build Settings (4 scenes)", false); pass = false; }
        else LogResult("Build Settings (4 scenes)", true);

        // 3. Script Integrity (Map)
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");
        if (Object.FindAnyObjectByType<TheHero.Generated.THMapController>() == null) { LogResult("Map: Runtime Controller", false); pass = false; }
        else LogResult("Map: Runtime Controller", true);

        if (GameObject.Find("Hero") == null) { LogResult("Map: Hero Object", false); pass = false; }
        else LogResult("Map: Hero Object", true);

        int objCount = Object.FindObjectsByType<TheHero.Generated.THMapObject>(FindObjectsInactive.Include).Length;
        if (objCount < 5) { LogResult("Map: Map Objects (min 5, found " + objCount + ")", false); pass = false; }
        else LogResult("Map: Map Objects", true);

        // 4. Main Menu
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var bridge = Object.FindAnyObjectByType<TheHero.Generated.THMainMenuController>();
        if (bridge == null) { LogResult("MainMenu: Controller", false); pass = false; }
        else LogResult("MainMenu: Controller", true);

        // 5. Combat
        EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity");
        if (Object.FindAnyObjectByType<TheHero.Generated.THCombatController>() == null) { LogResult("Combat: Controller", false); pass = false; }
        else LogResult("Combat: Controller", true);

        // 6. Base
        EditorSceneManager.OpenScene("Assets/Scenes/Base.unity");
        if (Object.FindAnyObjectByType<TheHero.Generated.THBaseController>() == null) { LogResult("Base: Controller", false); pass = false; }
        else LogResult("Base: Controller", true);

        Debug.Log(pass ? "[TH Validation] SUCCESS: Project is ready." : "[TH Validation] FAILURE: See logs above.");
        return pass;
    }

    private static void LogResult(string label, bool success)
    {
        if (success) Debug.Log("[PASS] " + label);
        else Debug.LogError("[FAIL] " + label);
    }

    [MenuItem("The Hero/Validation/Fix Common Problems")]
    public static void FixProblems()
    {
        Debug.Log("[TH Fix] Running auto-fix...");
        TheHeroMapFixer.CreatePlayableMap();
        TheHeroMapFixer.UpdateMainMenu();
        Debug.Log("[TH Fix] Completed. Please run validation again.");
    }
}