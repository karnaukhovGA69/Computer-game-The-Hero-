using System;
using UnityEngine;

namespace TheHero.Subsystems.Map
{
    [Serializable]
    public class TileData
    {
        public Vector2Int Position;
        public bool Walkable;

        // null — клетка пустая
        public MapObject Object;

        public TileData(int x, int y, bool walkable = true)
        {
            Position = new Vector2Int(x, y);
            Walkable = walkable;
        }
    }
}
