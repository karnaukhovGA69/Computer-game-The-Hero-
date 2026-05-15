using System.Collections.Generic;
using UnityEngine;

namespace TheHero.Subsystems.Map
{
    public class MapGrid
    {
        private readonly TileData[,] _tiles;

        public int Width { get; }
        public int Height { get; }

        public MapGrid(int width, int height)
        {
            Width = width;
            Height = height;
            _tiles = new TileData[width, height];
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _tiles[x, y] = new TileData(x, y);
        }

        public TileData GetTile(int x, int y)
        {
            if (!InBounds(x, y)) return null;
            return _tiles[x, y];
        }

        public TileData GetTile(Vector2Int pos) => GetTile(pos.x, pos.y);

        public void SetTile(TileData tile)
        {
            if (InBounds(tile.Position.x, tile.Position.y))
                _tiles[tile.Position.x, tile.Position.y] = tile;
        }

        public bool IsWalkable(int x, int y)
        {
            var tile = GetTile(x, y);
            return tile != null && tile.Walkable;
        }

        public bool IsWalkable(Vector2Int pos) => IsWalkable(pos.x, pos.y);

        // Возвращает 4 соседних клетки (без диагоналей)
        public List<TileData> GetNeighbors(Vector2Int pos)
        {
            var result = new List<TileData>(4);
            var offsets = new[] {
                new Vector2Int(0, 1), new Vector2Int(0, -1),
                new Vector2Int(1, 0), new Vector2Int(-1, 0)
            };
            foreach (var offset in offsets)
            {
                var tile = GetTile(pos + offset);
                if (tile != null) result.Add(tile);
            }
            return result;
        }

        private bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height;
    }
}
