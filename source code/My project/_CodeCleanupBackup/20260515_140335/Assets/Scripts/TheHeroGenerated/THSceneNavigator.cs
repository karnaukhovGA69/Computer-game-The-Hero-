using UnityEngine.SceneManagement;

namespace TheHero.Generated
{
    public static class THSceneNavigator
    {
        public const int MainMenu = 0;
        public const int Map = 1;
        public const int Combat = 2;
        public const int Base = 3;

        public static void Load(int index) => SceneManager.LoadScene(index);
    }
}
