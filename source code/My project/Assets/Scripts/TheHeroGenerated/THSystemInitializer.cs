using UnityEngine;

namespace TheHero.Generated
{
    public static class THSystemInitializer
    {
        public static void EnsureSystems()
        {
            // Getters for Instance will create them if missing
            if (THMessageSystem.Instance == null) { }
            if (THSceneLoader.Instance == null) { }
            if (THPauseMenu.Instance == null) { }
            if (THManager.Instance == null) { }
        }
    }
}
