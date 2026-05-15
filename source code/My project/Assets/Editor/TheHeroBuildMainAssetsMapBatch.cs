/// <summary>
/// Entry point for Unity -batchmode -executeMethod
/// </summary>
public static class TheHeroBuildMainAssetsMapBatch
{
    public static void Run()
    {
        TheHeroBuildFinalMapFromMainAssets.BuildFinalMap();
        TheHeroValidateMainAssetsMap.ValidateMainAssetsMap();
        UnityEditor.EditorApplication.Exit(0);
    }
}
