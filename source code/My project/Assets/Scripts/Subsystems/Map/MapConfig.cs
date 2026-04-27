using System;
using System.Collections.Generic;

namespace TheHero.Subsystems.Map
{
    // Конфиг карты, загружается из Assets/Resources/Config/map_config.json
    [Serializable]
    public class MapConfig
    {
        public int Width = 20;
        public int Height = 20;
        public int Seed = 0;
        public List<TileConfig> Tiles = new List<TileConfig>();
        public List<ObjectConfig> Objects = new List<ObjectConfig>();
    }

    [Serializable]
    public class TileConfig
    {
        public int X;
        public int Y;
        public bool Walkable;
    }

    [Serializable]
    public class ObjectConfig
    {
        public int X;
        public int Y;
        public string Type;
        public string DataKey;
    }
}
