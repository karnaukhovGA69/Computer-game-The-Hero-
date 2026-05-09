using UnityEngine;
using UnityEditor;
using System.IO;

public class TheHeroReleaseBuilder
{
    [MenuItem("The Hero/Build/Build Release Windows EXE")]
    public static void BuildWindows()
    {
        // 1. Save all scenes
        AssetDatabase.SaveAssets();
        
        // 2. Run validation
        if (!TheHeroReleaseValidator.RunValidation())
        {
            Debug.LogError("[TheHeroBuilder] Validation failed. Build cancelled.");
            return;
        }

        // 3. Build
        string outputFolder = "Builds/TheHeroRelease";
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
        options.options = BuildOptions.None; // Final release, no debug

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("<color=green>[TheHeroBuilder] Build successful: " + exePath + "</color>");
            CreateReadme(outputFolder);
        }
        else
        {
            Debug.LogError("[TheHeroBuilder] Build failed: " + report.summary.result);
        }
    }

    private static void CreateReadme(string folder)
    {
        string readme = @"Название: The Hero

Файл запуска:
TheHero.exe

Как играть:
1. Нажмите New Game.
2. Перемещайте героя кликом по карте.
3. Собирайте ресурсы.
4. Побеждайте врагов.
5. На базе нанимайте юнитов и улучшайте здания.
6. Победите Тёмного Лорда.

Управление:
- ЛКМ — выбор клетки, объекта, кнопки.
- Esc — пауза.
- End Turn — закончить день.

Сохранение:
Игра сохраняется вручную и автоматически после важных событий.

Если игра не запускается:
Откройте проект в Unity и запустите Assets/Scenes/MainMenu.unity.
";
        
        File.WriteAllText(folder + "/README_RUN.txt", readme);
    }
}
