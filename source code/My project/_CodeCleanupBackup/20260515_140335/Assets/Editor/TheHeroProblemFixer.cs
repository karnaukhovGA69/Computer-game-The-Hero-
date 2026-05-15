using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TheHero.Generated;
using System.IO;

public class TheHeroProblemFixer : EditorWindow
{
    [MenuItem("The Hero/Validation/Fix Release Problems")]
    public static void FixProblems()
    {
        Debug.Log("[TheHeroFixer] Starting automatic fixes...");

        FixBuildSettings();
        FixRequiredFolders();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("[TheHeroFixer] Finished fixes. Running validation...");
        TheHeroReleaseValidator.RunValidation();
    }

    private static void FixBuildSettings()
    {
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Map.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Combat.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Base.unity", true)
        };
        Debug.Log("[TheHeroFixer] Build Settings fixed.");
    }

    private static void FixRequiredFolders()
    {
        string[] dirs = {
            "Assets/Scenes",
            "Assets/Scripts/TheHeroGenerated",
            "Assets/Prefabs/UI",
            "Assets/Prefabs/Game",
            "Assets/Resources/Sprites",
            "Assets/Resources/Audio",
            "Assets/Editor"
        };
        foreach (var dir in dirs)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                Debug.Log($"[TheHeroFixer] Created directory: {dir}");
            }
        }
    }
}