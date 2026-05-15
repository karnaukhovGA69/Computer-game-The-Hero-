using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TheHero.Editor
{
    public static class TheHeroFixSceneBuildProfile
    {
        private static readonly string[] MainScenes =
        {
            "Assets/Scenes/MainMenu.unity",
            "Assets/Scenes/Map.unity",
            "Assets/Scenes/Combat.unity",
            "Assets/Scenes/Base.unity"
        };

        [MenuItem("The Hero/Validation/Fix Scene Build Profile")]
        public static void FixSceneBuildProfile()
        {
            EditorBuildSettings.scenes = MainScenes
                .Select(path => new EditorBuildSettingsScene(path, true))
                .ToArray();

            Debug.Log("[TheHeroCleanup] Scene build profile fixed: MainMenu, Map, Combat, Base");
        }
    }
}
