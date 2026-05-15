using System;
using System.Collections.Generic;

namespace TheHero.Subsystems.Base
{
    [Serializable]
    public class BaseState
    {
        public string BaseName;
        public bool IsOwned;

        public List<BuildingState> Buildings = new List<BuildingState>();

        public BuildingState GetBuilding(string buildingId) =>
            Buildings.Find(b => b.BuildingId == buildingId);

        public void AddBuilding(string buildingId)
        {
            if (GetBuilding(buildingId) == null)
                Buildings.Add(new BuildingState(buildingId));
        }
    }
}
