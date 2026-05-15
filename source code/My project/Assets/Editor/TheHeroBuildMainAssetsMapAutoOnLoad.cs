using UnityEditor;

/// <summary>
/// DISABLED: Auto-build on load was causing MissingComponentException on Castle_Player.
/// Use menu item to manually rebuild when needed.
/// </summary>
public static class TheHeroBuildMainAssetsMapAutoOnLoad
{
    private const string AutoBuildVersion = "MainAssetsFinalMap_v5_castle";
    private const string PrefKey = "TheHero_MainAssetsAutoBuildVersion";

    [MenuItem("The Hero/Map/Force Rebuild MainAssets Map (Auto)")]
    public static void ForceRebuild()
    {
        EditorPrefs.DeleteKey(PrefKey);
        TheHeroFixMainAssetsMapValidationFails.FixValidationFails();
    }
}
