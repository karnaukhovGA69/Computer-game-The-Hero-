// TheHeroPlayableBatch.cs
// Batchmode entrypoints for headless Unity runs.
// Usage:
//   Unity.exe -batchmode -quit -projectPath "<proj>" -executeMethod TheHeroPlayableBatch.MakeAndValidate -logFile -
using UnityEditor;
using UnityEngine;
using System;
using System.IO;

public static class TheHeroPlayableBatch
{
    public static void MakeGamePlayable()
    {
        try
        {
            TheHeroMakeGamePlayable.MakeGamePlayable();
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogError("[TheHeroPlayableBatch] MakeGamePlayable threw: " + ex);
            EditorApplication.Exit(1);
        }
    }

    public static void ValidatePlayableGame()
    {
        try
        {
            int fails = TheHeroFinalGameValidation.RunAllChecks();
            Directory.CreateDirectory("Assets/CodeAudit");
            File.WriteAllText("Assets/CodeAudit/Make_Game_Playable_BatchExit.txt",
                $"fails={fails}\n{DateTime.UtcNow:O}\n");
            EditorApplication.Exit(fails == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Debug.LogError("[TheHeroPlayableBatch] Validate threw: " + ex);
            EditorApplication.Exit(1);
        }
    }

    public static void PatchAndValidate()
    {
        try
        {
            TheHeroPatchMovementAndCamera.Patch();
            int fails = TheHeroFinalGameValidation.RunAllChecks();
            Directory.CreateDirectory("Assets/CodeAudit");
            File.WriteAllText("Assets/CodeAudit/Make_Game_Playable_BatchExit.txt",
                $"fails={fails}\n{DateTime.UtcNow:O}\n");
            EditorApplication.Exit(fails == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Debug.LogError("[TheHeroPlayableBatch] PatchAndValidate threw: " + ex);
            EditorApplication.Exit(1);
        }
    }

    public static void MakeAndValidate()
    {
        try
        {
            TheHeroMakeGamePlayable.MakeGamePlayable();
            int fails = TheHeroFinalGameValidation.RunAllChecks();
            Directory.CreateDirectory("Assets/CodeAudit");
            File.WriteAllText("Assets/CodeAudit/Make_Game_Playable_BatchExit.txt",
                $"fails={fails}\n{DateTime.UtcNow:O}\n");
            EditorApplication.Exit(fails == 0 ? 0 : 2);
        }
        catch (Exception ex)
        {
            Debug.LogError("[TheHeroPlayableBatch] MakeAndValidate threw: " + ex);
            EditorApplication.Exit(1);
        }
    }
}
