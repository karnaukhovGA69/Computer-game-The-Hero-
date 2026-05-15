using System;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    [Serializable]
    public sealed class BaseState
    {
        public List<BuildingState> buildings = new List<BuildingState>();

        public BuildingState GetBuilding(string id)
        {
            return buildings.FirstOrDefault(b => b != null && b.id == id);
        }
    }
}
