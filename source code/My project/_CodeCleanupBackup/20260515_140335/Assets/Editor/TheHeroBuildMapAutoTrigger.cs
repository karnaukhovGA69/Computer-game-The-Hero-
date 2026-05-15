using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// [CLEANUP] Disabled auto-run to prevent scene corruption.
/// Previously auto-ran TheHeroBuildMapFromCainosPack.Run() on domain reload
/// when the sentinel file Assets/Editor/.run_cainos_build existed.
///
/// To use this manually, call: TheHeroBuildMapAutoTrigger.TryRunManual()
/// from the Tools menu, or use TheHeroBuildMapFromCainosPack.Run() directly.
/// </summary>
//[InitializeOnLoad]  // DISABLED: Auto-run prevented to avoid scene corruption
public static class TheHeroBuildMapAutoTrigger
{
    private const string SENTINEL = "Assets/Editor/.run_cainos_build";
    private const string DONE     = "Assets/CodeAudit/Cainos_AutoTrigger_Done.txt";

    // Auto-trigger disabled: use manual execution instead
    // static TheHeroBuildMapAutoTrigger()
    // {
    //     EditorApplication.delayCall += TryRun;
    // }

    [MenuItem("The Hero/Editor Tools/Build Map From Cainos (Manual)")]
    public static void TryRunManual()
    {
        try
        {
            File.Delete(SENTINEL);
            string meta = SENTINEL + ".meta";
            if (File.Exists(meta)) File.Delete(meta);
        }
        catch { /* best effort */ }

        Debug.Log("[TheHeroCainosMap] Manual trigger: invoking Build Map From Cainos Pack");
        try
        {
            TheHeroBuildMapFromCainosPack.Run();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[TheHeroCainosMap] Manual build threw: " + ex);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(DONE));
            File.WriteAllText(DONE, System.DateTime.Now.ToString("O"));
        }
        catch { /* best effort */ }

        AssetDatabase.Refresh();
    }

    private static void TryRun()
    {
        if (!File.Exists(SENTINEL)) return;
        TryRunManual();
    }
}
