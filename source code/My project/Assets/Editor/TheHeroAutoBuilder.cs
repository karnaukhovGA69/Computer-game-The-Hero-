using System.IO;
using TheHeroGenerated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TheHeroGenerated.EditorTools
{
    public static class TheHeroAutoBuilder
    {
        private const string ScenesDir = "Assets/Scenes";
        private const string BuildDir = "Builds/TheHero";
        private static readonly string[] SceneNames = { "MainMenu", "Map", "Combat", "Base" };

        [MenuItem("The Hero/1 Setup MVP Scenes And Assets")]
        public static void SetupMvp()
        {
            CreateFolders();
            CreatePlaceholderSprites();
            CreateScenes();
            ConfigureBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
            Debug.Log("[TheHeroAutoBuilder] Setup complete. Open MainMenu and press Play, or run The Hero/2 Build Windows EXE.");
        }

        [MenuItem("The Hero/2 Build Windows EXE")]
        public static void BuildWindowsExe()
        {
            SetupMvp();
            Directory.CreateDirectory(BuildDir);

            var target = BuildTarget.StandaloneWindows64;
            if (EditorUserBuildSettings.activeBuildTarget != target)
            {
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, target);
            }

            var options = new BuildPlayerOptions
            {
                scenes = GetScenePaths(),
                locationPathName = Path.Combine(BuildDir, "TheHero.exe").Replace("\\", "/"),
                target = target,
                options = BuildOptions.None
            };

            var report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                Debug.Log($"[TheHeroAutoBuilder] Build succeeded: {options.locationPathName}. Size: {report.summary.totalSize} bytes");
                EditorUtility.RevealInFinder(options.locationPathName);
            }
            else
            {
                Debug.LogError("[TheHeroAutoBuilder] Build failed: " + report.summary.result);
            }
        }

        private static void CreateFolders()
        {
            string[] folders =
            {
                "Assets/Prefabs", "Assets/Prefabs/UI", "Assets/Prefabs/Game",
                "Assets/Scenes", "Assets/Sprites", "Assets/Sprites/Units", "Assets/Sprites/Map", "Assets/Sprites/UI",
                "Assets/Audio", "Assets/Audio/Music", "Assets/Audio/SFX",
                "Assets/Resources", "Assets/Resources/Config", "Assets/Resources/Sprites", "Assets/Resources/Sprites/Units",
                "Assets/Resources/Sprites/Map", "Assets/Resources/Audio", "Assets/Resources/Audio/Music", "Assets/Resources/Audio/SFX",
                "Assets/Editor", BuildDir
            };

            foreach (var folder in folders)
            {
                if (!AssetDatabase.IsValidFolder(folder))
                {
                    var parent = Path.GetDirectoryName(folder)?.Replace("\\", "/");
                    var name = Path.GetFileName(folder);
                    if (!string.IsNullOrEmpty(parent) && AssetDatabase.IsValidFolder(parent))
                        AssetDatabase.CreateFolder(parent, name);
                }
            }
            Debug.Log("[TheHeroAutoBuilder] Created folders.");
        }

        private static void CreateScenes()
        {
            CreateScene("MainMenu", SceneKind.MainMenu);
            CreateScene("Map", SceneKind.Map);
            CreateScene("Combat", SceneKind.Combat);
            CreateScene("Base", SceneKind.Base);
            Debug.Log("[TheHeroAutoBuilder] Created scenes.");
        }

        private static void CreateScene(string sceneName, SceneKind kind)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = sceneName;

            var cameraObject = new GameObject("Main Camera");
            var camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.03f, 0.04f, 0.07f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.tag = "MainCamera";

            var bootstrapObject = new GameObject("TheHeroSceneBootstrap");
            var bootstrap = bootstrapObject.AddComponent<TheHeroSceneBootstrap>();
            bootstrap.sceneKind = kind;

            EditorSceneManager.SaveScene(scene, $"{ScenesDir}/{sceneName}.unity");
        }

        private static void ConfigureBuildSettings()
        {
            var scenes = new EditorBuildSettingsScene[SceneNames.Length];
            for (var i = 0; i < SceneNames.Length; i++)
            {
                scenes[i] = new EditorBuildSettingsScene($"{ScenesDir}/{SceneNames[i]}.unity", true);
            }
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[TheHeroAutoBuilder] Added scenes to Build Settings: MainMenu, Map, Combat, Base.");
        }

        private static string[] GetScenePaths()
        {
            var paths = new string[SceneNames.Length];
            for (var i = 0; i < SceneNames.Length; i++)
                paths[i] = $"{ScenesDir}/{SceneNames[i]}.unity";
            return paths;
        }

        private static void CreatePlaceholderSprites()
        {
            CreateSprite("Assets/Resources/Sprites/Units/hero.png", new Color(0.1f, 0.35f, 0.95f), new Color(1f, 0.85f, 0.2f));
            CreateSprite("Assets/Resources/Sprites/Units/unit_swordsman.png", new Color(0.55f, 0.55f, 0.65f), new Color(0.85f, 0.1f, 0.1f));
            CreateSprite("Assets/Resources/Sprites/Units/unit_archer.png", new Color(0.1f, 0.55f, 0.18f), new Color(0.45f, 0.25f, 0.1f));
            CreateSprite("Assets/Resources/Sprites/Units/unit_orc.png", new Color(0.1f, 0.45f, 0.1f), new Color(0.05f, 0.1f, 0.05f));

            CreateSprite("Assets/Resources/Sprites/Map/tile_grass.png", new Color(0.1f, 0.45f, 0.15f), new Color(0.15f, 0.6f, 0.18f));
            CreateSprite("Assets/Resources/Sprites/Map/tile_forest.png", new Color(0.02f, 0.25f, 0.05f), new Color(0.08f, 0.5f, 0.1f));
            CreateSprite("Assets/Resources/Sprites/Map/tile_mountain.png", new Color(0.35f, 0.35f, 0.35f), new Color(0.65f, 0.65f, 0.65f));
            CreateSprite("Assets/Resources/Sprites/Map/tile_water.png", new Color(0.05f, 0.18f, 0.55f), new Color(0.15f, 0.55f, 0.95f));
            CreateSprite("Assets/Resources/Sprites/Map/tile_road.png", new Color(0.42f, 0.28f, 0.13f), new Color(0.62f, 0.45f, 0.2f));
            CreateSprite("Assets/Resources/Sprites/Map/obj_mine.png", new Color(0.28f, 0.28f, 0.28f), new Color(1f, 0.82f, 0.2f));
            CreateSprite("Assets/Resources/Sprites/Map/obj_base.png", new Color(0.25f, 0.15f, 0.08f), new Color(0.95f, 0.75f, 0.35f));
            CreateSprite("Assets/Resources/Sprites/Map/obj_enemy.png", new Color(0.45f, 0.05f, 0.04f), new Color(0.95f, 0.25f, 0.15f));

            CopyIfExists("Assets/Resources/Sprites/Units/hero.png", "Assets/Sprites/Units/hero.png");
            CopyIfExists("Assets/Resources/Sprites/Units/unit_swordsman.png", "Assets/Sprites/Units/unit_swordsman.png");
            CopyIfExists("Assets/Resources/Sprites/Units/unit_archer.png", "Assets/Sprites/Units/unit_archer.png");
            CopyIfExists("Assets/Resources/Sprites/Units/unit_orc.png", "Assets/Sprites/Units/unit_orc.png");
            CopyIfExists("Assets/Resources/Sprites/Map/tile_grass.png", "Assets/Sprites/Map/tile_grass.png");
            CopyIfExists("Assets/Resources/Sprites/Map/tile_forest.png", "Assets/Sprites/Map/tile_forest.png");
            CopyIfExists("Assets/Resources/Sprites/Map/tile_mountain.png", "Assets/Sprites/Map/tile_mountain.png");
            CopyIfExists("Assets/Resources/Sprites/Map/tile_water.png", "Assets/Sprites/Map/tile_water.png");
            CopyIfExists("Assets/Resources/Sprites/Map/tile_road.png", "Assets/Sprites/Map/tile_road.png");
            CopyIfExists("Assets/Resources/Sprites/Map/obj_mine.png", "Assets/Sprites/Map/obj_mine.png");
            CopyIfExists("Assets/Resources/Sprites/Map/obj_base.png", "Assets/Sprites/Map/obj_base.png");
            CopyIfExists("Assets/Resources/Sprites/Map/obj_enemy.png", "Assets/Sprites/Map/obj_enemy.png");

            AssetDatabase.Refresh();
            Debug.Log("[TheHeroAutoBuilder] Created placeholder sprites.");
        }

        private static void CreateSprite(string assetPath, Color main, Color accent)
        {
            if (File.Exists(assetPath)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var border = x < 3 || y < 3 || x > size - 4 || y > size - 4;
                    var diagonal = Mathf.Abs(x - y) < 4 || Mathf.Abs((size - x) - y) < 4;
                    tex.SetPixel(x, y, border || diagonal ? accent : main);
                }
            }
            tex.Apply();
            File.WriteAllBytes(assetPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(assetPath);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 16;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static void CopyIfExists(string source, string destination)
        {
            if (!File.Exists(source) || File.Exists(destination)) return;
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? "Assets");
            File.Copy(source, destination, false);
        }
    }
}
