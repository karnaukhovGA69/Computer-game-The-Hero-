using System;

namespace TheHero.Generated
{
    [Serializable]
    public sealed class BuildingState
    {
        public string id;
        public int level = 1;
        public int recruitsAvailable;

        public bool IsUpgraded => level > 1;
    }
}
