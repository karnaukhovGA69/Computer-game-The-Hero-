using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheHero.Subsystems.Map
{
    public static class PathFinder
    {
        // Возвращает путь от start до goal (включая goal, без start) или null если недостижимо
        public static List<Vector2Int> FindPath(MapGrid grid, Vector2Int start, Vector2Int goal)
        {
            if (!grid.IsWalkable(goal)) return null;

            var open = new List<Node>();
            var closed = new HashSet<Vector2Int>();
            var nodeMap = new Dictionary<Vector2Int, Node>();

            var startNode = new Node(start, 0, Heuristic(start, goal), null);
            open.Add(startNode);
            nodeMap[start] = startNode;

            while (open.Count > 0)
            {
                var current = PopLowestF(open);

                if (current.Position == goal)
                    return ReconstructPath(current);

                closed.Add(current.Position);

                foreach (var neighbor in grid.GetNeighbors(current.Position))
                {
                    if (!neighbor.Walkable || closed.Contains(neighbor.Position))
                        continue;

                    float g = current.G + 1;

                    if (nodeMap.TryGetValue(neighbor.Position, out var existing))
                    {
                        if (g < existing.G)
                        {
                            existing.G = g;
                            existing.Parent = current;
                        }
                    }
                    else
                    {
                        var node = new Node(neighbor.Position, g, Heuristic(neighbor.Position, goal), current);
                        open.Add(node);
                        nodeMap[neighbor.Position] = node;
                    }
                }
            }

            return null;
        }

        private static float Heuristic(Vector2Int a, Vector2Int b) =>
            Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        private static Node PopLowestF(List<Node> open)
        {
            int best = 0;
            for (int i = 1; i < open.Count; i++)
                if (open[i].F < open[best].F) best = i;
            var node = open[best];
            open.RemoveAt(best);
            return node;
        }

        private static List<Vector2Int> ReconstructPath(Node goal)
        {
            var path = new List<Vector2Int>();
            var current = goal;
            while (current.Parent != null)
            {
                path.Add(current.Position);
                current = current.Parent;
            }
            path.Reverse();
            return path;
        }

        private class Node
        {
            public Vector2Int Position;
            public float G;
            public float H;
            public Node Parent;
            public float F => G + H;

            public Node(Vector2Int pos, float g, float h, Node parent)
            {
                Position = pos; G = g; H = h; Parent = parent;
            }
        }
    }
}
