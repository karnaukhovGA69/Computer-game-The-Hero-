using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace TheHero.Editor
{
    public class TheHeroSceneOrderFixer
    {
        [MenuItem("The Hero/Small Fixes/Fix Startup Scene")]
        public static void FixSceneOrder()
        {
            string[] scenePaths = {
                "Assets/Scenes/MainMenu.unity",
                "Assets/Scenes/Map.unity",
                "Assets/Scenes/Combat.unity",
                "Assets/Scenes/Base.unity"
            };

            List<EditorBuildSettingsScene> buildScenes = new List<EditorBuildSettingsScene>();

            foreach (string path in scenePaths)
            {
                if (File.Exists(path))
                {
                    buildScenes.Add(new EditorBuildSettingsScene(path, true));
                }
                else
                {
                    Debug.LogWarning($"[TheHeroSmallStep] Scene not found at path: {path}");
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();

            if (buildScenes.Count > 0)
            {
                EditorSceneManager.OpenScene(buildScenes[0].path);
                Debug.Log("[TheHeroSmallStep] Startup scene fixed");
            }
            
            AssetDatabase.SaveAssets();
        }
}
}
