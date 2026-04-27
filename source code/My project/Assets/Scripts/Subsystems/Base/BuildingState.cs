using System;

namespace TheHero.Subsystems.Base
{
    [Serializable]
    public class BuildingState
    {
        public string BuildingId;

        // 1 — базовый уровень, 2 — улучшенный
        public int Level;

        // Накопленные доступные к найму юниты (не забранные за прошлые недели суммируются)
        public int AccumulatedRecruits;

        public BuildingState() { }

        public BuildingState(string buildingId)
        {
            BuildingId = buildingId;
            Level = 1;
            AccumulatedRecruits = 0;
        }

        public bool IsUpgraded => Level >= 2;
    }
}
