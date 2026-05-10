using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class THStartupSceneFixer
{
    static THStartupSceneFixer()
    {
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // When the user clicks "Play"
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            // Save the currently open scene so we can return to it later if we want
            // but for now, just ensure MainMenu is loaded as the first scene.
            
            // Check if Main Menu is already the first scene in build settings
            if (EditorBuildSettings.scenes.Length > 0)
            {
                string mainMenuPath = EditorBuildSettings.scenes[0].path;
                var sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(mainMenuPath);
                if (sceneAsset != null)
                {
                    EditorSceneManager.playModeStartScene = sceneAsset;
                    Debug.Log($"[THStartup] Set Play Mode start scene to: {mainMenuPath}");
                }
            }
        }
    }
}
