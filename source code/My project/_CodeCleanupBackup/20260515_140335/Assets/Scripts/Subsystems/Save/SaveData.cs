using System;
using TheHero.Domain;

namespace TheHero.Subsystems.Save
{
    [Serializable]
    public class SaveData
    {
        public int SaveVersion = 1;
        public string SaveDate;
        public GameState State;

        public static SaveData From(GameState state)
        {
            return new SaveData
            {
                SaveVersion = 1,
                SaveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                State = state
            };
        }
    }
}
