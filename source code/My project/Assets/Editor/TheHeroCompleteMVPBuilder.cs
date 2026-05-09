using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using TheHero.Generated;

namespace TheHero.Editor
{
[InitializeOnLoad]
    public static class TheHeroCompleteMVPBuilder
    {
        private const string MarkerPath = "Assets/Editor/RUN_THE_HERO_AUTOBUILD.txt";
        private const string ActionsPath = "Assets/InputSystem_Actions.inputactions";

        static TheHeroCompleteMVPBuilder()
        {
            EditorApplication.delayCall += () =>
            {
                if (File.Exists(MarkerPath))
                {
                    Debug.Log("[TheHero] Marker file found. Starting autobuild...");
                    CreateScenesAndBuild();
                    File.Delete(MarkerPath);
                    AssetDatabase.Refresh();
                }
            };
        }

        [MenuItem("The Hero/Complete MVP/Create Scenes")]
        public static void CreateScenes()
        {
            Directory.CreateDirectory("Assets/Scenes");
            CreateScene("Assets/Scenes/MainMenu.unity", THSceneNavigator.MainMenu);
            CreateScene("Assets/Scenes/Map.unity", THSceneNavigator.Map);
            CreateScene("Assets/Scenes/Combat.unity", THSceneNavigator.Combat);
            CreateScene("Assets/Scenes/Base.unity", THSceneNavigator.Base);

            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[TheHero] Scenes created and configured.");
        }

        private static void CreateScene(string path, int index)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            
            var bootGo = new GameObject("TH_Bootstrap");
            var boot = bootGo.AddComponent<THBootstrap>();
            boot.type = (THBootstrap.SceneType)index;

            var canvasGo = new GameObject("Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            switch ((THBootstrap.SceneType)index)
            {
                case THBootstrap.SceneType.MainMenu:
                    CreateUIButton(canvasGo.transform, "New Game", new Vector2(0, 50));
                    CreateUIButton(canvasGo.transform, "Continue", new Vector2(0, 0));
                    CreateUIButton(canvasGo.transform, "Exit", new Vector2(0, -50));
                    break;
                // Other scenes don't need buttons yet or will have them added by controllers
            }

            EditorSceneManager.SaveScene(scene, path);
        }

        private static void CreateUIButton(Transform parent, string label, Vector2 pos)
        {
            var btnGo = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(160, 40);
            
            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            var txt = txtGo.GetComponent<Text>();
            txt.text = label;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            var txtRt = txtGo.GetComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
        }

        [MenuItem("The Hero/Complete MVP/Build Windows EXE")]
        public static void BuildWindowsExe()
        {
            string buildPath = "Builds/TheHero/TheHero.exe";
            Directory.CreateDirectory("Builds/TheHero");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
            buildPlayerOptions.scenes = new[] { 
                "Assets/Scenes/MainMenu.unity", 
                "Assets/Scenes/Map.unity", 
                "Assets/Scenes/Combat.unity", 
                "Assets/Scenes/Base.unity" 
            };
            buildPlayerOptions.locationPathName = buildPath;
            buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
            buildPlayerOptions.options = BuildOptions.None;

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log("[TheHero] Build finished: " + buildPath);
            }
            else
            {
                Debug.LogError("[TheHero] Build failed: " + summary.result);
            }
        }

        [MenuItem("The Hero/Complete MVP/Create Scenes And Build EXE")]
        public static void CreateScenesAndBuild()
        {
            CreateScenes();
            BuildWindowsExe();
        }

        private static void ConfigureBuildSettings()
        {
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Map.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Combat.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Base.unity", true)
            };
        }
        }
        }
