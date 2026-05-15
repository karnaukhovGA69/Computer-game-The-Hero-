using UnityEngine;
using UnityEditor;
using System.IO;

public class TheHeroPreReleaseBuilder
{
    [MenuItem("The Hero/Build/Build Pre-Release Windows EXE")]
    public static void BuildWindows()
    {
        // 1. Save all scenes
        AssetDatabase.SaveAssets();
        
        // 2. Run validation
        if (!TheHeroPreReleaseValidator.RunValidation())
        {
            Debug.LogError("[TheHeroBuilder] Validation failed. Build cancelled.");
            return;
        }

        // 3. Build
        string outputFolder = "Builds/TheHeroPreRelease";
        string exePath = outputFolder + "/TheHero.exe";
        Directory.CreateDirectory(outputFolder);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = new[] { 
            "Assets/Scenes/MainMenu.unity", 
            "Assets/Scenes/Map.unity", 
            "Assets/Scenes/Combat.unity", 
            "Assets/Scenes/Base.unity" 
        };
        options.locationPathName = exePath;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[TheHeroBuilder] Build successful: " + exePath);
            CreateReadme(outputFolder);
        }
        else
        {
            Debug.LogError("[TheHeroBuilder] Build failed: " + report.summary.result);
        }
    }

    private static void CreateReadme(string folder)
    {
        string readme = @"Название игры: The Hero
Файл запуска: TheHero.exe
Управление: мышь, Esc для паузы
Цель: победить Тёмного Лорда
Как начать: New Game
Как продолжить: Continue
Где хранится сохранение: Application.persistentDataPath
Что делать при проблемах: открыть проект в Unity и запустить MainMenu.unity";
        
        File.WriteAllText(folder + "/README_RUN.txt", readme);
    }
}
