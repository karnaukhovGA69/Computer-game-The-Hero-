using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;

public static class TheHeroFinalBuilder
{
    [MenuItem("The Hero/Build/Build Final Windows EXE")]
    public static void BuildFinal()
    {
        EditorSceneManager.SaveOpenScenes();

        if (!TheHeroPlaythroughValidator.RunValidation())
        {
            EditorUtility.DisplayDialog("Build Error", "Validation failed. Check the console for details.", "OK");
            return;
        }

        string buildPath = "Builds/TheHeroFinal/TheHero.exe";
        string dir = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
        options.locationPathName = buildPath;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[TH Build] Succeeded: " + buildPath);
            CreateReadme(dir);
            EditorUtility.RevealInFinder(buildPath);
        }
        else
        {
            Debug.LogError("[TH Build] Failed. Check the Build Report.");
        }
    }

    private static void CreateReadme(string dir)
    {
        string path = Path.Combine(dir, "README_RUN.txt");
        string content = @"Название: The Hero

Как запустить:
1. Открыть TheHero.exe.
2. В меню нажать 'Новая игра'.
3. Играть мышью.

Цель:
Победить Тёмного Лорда.

Управление:
- Клик по клетке — движение героя.
- Клик по ресурсу — собрать ресурс.
- Клик по врагу — начать бой.
- Клик по базе — открыть базу.
- Завершить ход — восстановить очки хода на следующий день.

Если build не запускается:
- открыть проект в Unity
- открыть Assets/Scenes/MainMenu.unity
- нажать Play";
        File.WriteAllText(path, content);
    }
}