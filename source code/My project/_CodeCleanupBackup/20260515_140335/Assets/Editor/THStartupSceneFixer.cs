using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class THStartupSceneFixer
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static THStartupSceneFixer()
    {
        // Lightweight editor preference only: this does not rebuild scenes or run
        // any fixer. It makes Play Mode start from MainMenu even when Map.unity is
        // the currently opened scene in the editor.
        ApplyMainMenuStartScene();
    }

    [MenuItem("The Hero/Small Fixes/Set Play Mode Start Scene")]
    public static void SetPlayModeStartScene()
    {
        ApplyMainMenuStartScene();
    }

    private static void ApplyMainMenuStartScene()
    {
        var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);
        if (sceneAsset != null)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
            Debug.Log($"[THStartup] Set Play Mode start scene to: {MainMenuPath}");
        }
    }
}
