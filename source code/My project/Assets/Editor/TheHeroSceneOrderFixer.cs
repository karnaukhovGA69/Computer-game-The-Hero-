using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;

namespace TheHero.Editor
{
    public class TheHeroSceneOrderFixer
    {
        [MenuItem("The Hero/Fix/01 Fix Startup Scene Order")]
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
                    Debug.LogWarning($"[TheHeroFix] Scene not found at path: {path}");
                }
            }

            EditorBuildSettings.scenes = buildScenes.ToArray();

            if (buildScenes.Count > 0)
            {
                EditorSceneManager.OpenScene(buildScenes[0].path);
                Debug.Log($"[TheHeroFix] Startup scene fixed: {Path.GetFileName(buildScenes[0].path)} is build index 0");
            }
            
            AssetDatabase.SaveAssets();
        }
    }
}
