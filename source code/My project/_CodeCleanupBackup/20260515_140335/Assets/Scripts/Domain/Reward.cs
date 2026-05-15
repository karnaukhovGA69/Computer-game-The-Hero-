using System;

namespace TheHero.Domain
{
    [Serializable]
    public class Reward
    {
        public ResourceWallet Resources = new ResourceWallet();
        public int Experience;

        // null — артефакта нет
        public Artifact Artifact;
    }

    [Serializable]
    public class Artifact
    {
        public string Id;
        public string DisplayName;
        public string SpriteKey;
        public string Description;
    }
}
