using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TheHero.Generated;
using System.Linq;

public class TheHeroFixStartupAndCombatButtons : EditorWindow
{
    [MenuItem("The Hero/Fix/Fix Startup And Combat Buttons")]
    public static void FixAll()
    {
        FixBuildSettings();
        FixCombatScene();
        
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[TheHeroFix] MainMenu opened for testing");
        
        AssetDatabase.SaveAssets();
    }

    private static void FixBuildSettings()
    {
        var scenes = new[]
        {
            new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Map.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Combat.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/Base.unity", true)
        };

        EditorBuildSettings.scenes = scenes;
        Debug.Log("[TheHeroFix] Startup scene fixed: MainMenu is build index 0");
    }

    private static void FixCombatScene()
    {
        if (!System.IO.File.Exists("Assets/Scenes/Combat.unity")) return;

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity");
        
        var runtime = Object.FindAnyObjectByType<THCombatRuntime>();
        if (runtime != null)
        {
            Debug.Log("[TheHeroFix] Combat runtime found");
            runtime.ConnectButtons();
            
            // Check for buttons
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var btn in buttons)
            {
                btn.interactable = true;
                var img = btn.GetComponent<Image>();
                if (img != null) img.raycastTarget = true;
                
                if (btn.name == "AttackButton") Debug.Log("[TheHeroFix] Attack button connected");
                if (btn.name == "AutoBattleButton") Debug.Log("[TheHeroFix] AutoBattle button connected");
                if (btn.name == "SkipButton") Debug.Log("[TheHeroFix] Skip button connected");
            }
            
            // Fix raycast blockers
            var images = Object.FindObjectsByType<Image>(FindObjectsInactive.Include);
            foreach (var img in images)
            {
                if (img.gameObject.name.Contains("Panel") || img.gameObject.name.Contains("Background") || img.gameObject.name.Contains("TopBar"))
                {
                    if (img.GetComponent<Button>() == null)
                    {
                        img.raycastTarget = false;
                    }
                }
            }
            Debug.Log("[TheHeroFix] Combat raycast blockers fixed");

            // Ensure Canvas has GraphicRaycaster
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null && canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            // Ensure EventSystem
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        EditorSceneManager.SaveScene(scene);
    }
}
