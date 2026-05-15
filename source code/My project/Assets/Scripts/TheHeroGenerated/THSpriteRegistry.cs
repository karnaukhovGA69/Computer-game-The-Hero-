namespace TheHero.Generated
{
    public static class THSpriteRegistry
    {
        public const string Hero = "Assets/Tiny Swords/Units/Yellow Units/Warrior/Warrior_Idle.png";
        public const string Castle = "Assets/Tiny Swords/Buildings/Yellow Buildings/Castle.png";
        public const string Gold = "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png";
        public const string Wood = "Assets/Tiny Swords/Pawn and Resources/Wood/Wood Resource/Wood Resource.png";
        public const string Stone = "Assets/Tiny Swords/Terrain/Decorations/Rocks/Rock1.png";
        public const string Mana = "Assets/Resources/Sprites/CleanMap/Objects/clean_mana.png";
        public const string Chest = "Assets/Resources/Sprites/CleanMap/Objects/clean_chest.png";
        public const string Artifact = "Assets/Cainos/Pixel Art Top Down - Basic/Texture/TX Props.png";
        public const string Bridge = "Assets/ExternalAssets/Bridges/FreeTopDownBridges/PNG_n_Tiled/Bridges.png";
        public const string Goblin = "Assets/ExternalAssets/Orcs/FreeTopDownOrcCharacters/PNG/Orc1/Orc1_idle/orc1_idle_full.png";
        public const string Orc = "Assets/ExternalAssets/Orcs/FreeTopDownOrcCharacters/PNG/Orc2/Orc2_idle/orc2_idle_full.png";
        public const string Skeleton = "Assets/ExternalAssets/Skeletons/FantasySkeletonEnemies/Fantasy Skeleton Enemies/Skeleton Warrior.png";
        public const string DarkGuard = "Assets/ExternalAssets/Skeletons/FantasySkeletonEnemies/Fantasy Skeleton Enemies/Skeleton Mage.png";
        public const string DarkLord = "Assets/ExternalAssets/DarkLord/UndeadExecutioner/Undead executioner puppet/png/idle2.png";
        public const string Wolf = "Assets/ExternalAssets/Monsters_FR13/fr13/fr13_free_sample_pack/FR_121_CursedWolf.png";
        public const string Bandit = "Assets/Tiny Swords/Units/Black Units/Warrior/Warrior_Idle.png";

        public static string GetPath(string key)
        {
            switch (key)
            {
                case nameof(Hero): return Hero;
                case nameof(Castle): return Castle;
                case nameof(Gold): return Gold;
                case nameof(Wood): return Wood;
                case nameof(Stone): return Stone;
                case nameof(Mana): return Mana;
                case nameof(Chest): return Chest;
                case nameof(Artifact): return Artifact;
                case nameof(Bridge): return Bridge;
                case nameof(Goblin): return Goblin;
                case nameof(Orc): return Orc;
                case nameof(Skeleton): return Skeleton;
                case nameof(DarkGuard): return DarkGuard;
                case nameof(DarkLord): return DarkLord;
                case nameof(Wolf): return Wolf;
                case nameof(Bandit): return Bandit;
                default: return string.Empty;
            }
        }
    }
}
