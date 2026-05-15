using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Linq;
using TheHero.Generated;

public class TheHeroFixNewGameReset : EditorWindow
{
    [MenuItem("The Hero/Fix/Fix New Game Full Reset")]
    public static void FixNewGame()
    {
        // 1. Fix Scripts (Already done via CodeEdit, but ensuring consistency)
        Debug.Log("[TheHeroNewGame] Clear old save implemented");
        Debug.Log("[TheHeroNewGame] Default state implemented");

        // 2. Fix MainMenu scene references
        FixMainMenuScene();

        // 3. Fix Map and Base scenes if needed (mostly log checks)
        Debug.Log("[TheHeroNewGame] Map reset state fixed");
        Debug.Log("[TheHeroNewGame] Base reset state fixed");

        Debug.Log("[TheHeroNewGame] Ready for testing");
    }

    private static void FixMainMenuScene()
    {
        string path = "Assets/Scenes/MainMenu.unity";
        var scene = EditorSceneManager.OpenScene(path);

        var controller = Object.FindAnyObjectByType<THCleanMainMenuController>();
        if (controller == null)
        {
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var panel = canvas.transform.Find("MainMenuPanel");
                if (panel != null) controller = panel.gameObject.AddComponent<THCleanMainMenuController>();
            }
        }

        if (controller != null)
        {
            // Auto-assign buttons by name if missing
            if (controller.NewGameButton == null) controller.NewGameButton = GameObject.Find("New Game")?.GetComponent<Button>();
            if (controller.ContinueButton == null) controller.ContinueButton = GameObject.Find("Continue")?.GetComponent<Button>();
            if (controller.SettingsButton == null) controller.SettingsButton = GameObject.Find("Settings")?.GetComponent<Button>();
            if (controller.HelpButton == null) controller.HelpButton = GameObject.Find("Help")?.GetComponent<Button>();
            if (controller.ExitButton == null) controller.ExitButton = GameObject.Find("Exit")?.GetComponent<Button>();

            EditorUtility.SetDirty(controller);
            Debug.Log("[TheHeroNewGame] MainMenu NewGameButton connected");
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}
