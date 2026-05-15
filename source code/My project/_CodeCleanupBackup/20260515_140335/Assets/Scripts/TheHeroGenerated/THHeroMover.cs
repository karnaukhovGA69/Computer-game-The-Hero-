using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TheHero.Generated
{
    // Deprecated movement duplicate. Current Map scene uses THStrictGridHeroMovement.
    // Kept for reference only; do not attach together with the strict grid mover.
    public class THHeroMover : MonoBehaviour
    {
        public bool IsMoving { get; private set; }
        public float speed = 5f;
        private THMapObject pendingInteraction;

        public void SetPositionImmediate(int x, int y)
        {
            transform.position = new Vector3(x, y, transform.position.z);
        }

        private void Start()
        {
            IsMoving = false;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = new Color(0.6f, 0.2f, 0.8f); // Purple
        }

        public void TryMoveTo(int targetX, int targetY, THMapObject interaction = null)
        {
            if (IsMoving) return;
            if (THMapController.Instance == null || THMapController.Instance.State == null) return;
            var state = THMapController.Instance.State;
            int startX = (int)state.heroX;
            int startY = (int)state.heroY;

            if (startX == targetX && startY == targetY)
            {
                if (interaction != null) THMapController.Instance.HandleObjectInteraction(interaction);
                return;
            }

            var path = FindPath(new Vector2Int(startX, startY), new Vector2Int(targetX, targetY));
            if (path == null || path.Count <= 1)
            {
                THMapController.Instance.Log("Путь невозможен.");
                return;
            }

            int totalCost = 0;
            List<Vector2Int> actualPath = new List<Vector2Int> { new Vector2Int(startX, startY) };

            for (int i = 1; i < path.Count; i++)
            {
                int cost = GetTileCost(path[i].x, path[i].y);
                if (state.movementPoints >= totalCost + cost)
                {
                    totalCost += cost;
                    actualPath.Add(path[i]);
                }
                else
                {
                    break;
                }
            }

            if (actualPath.Count > 1)
            {
                state.movementPoints -= totalCost;
                // If we didn't reach the target, clear interaction
                pendingInteraction = (actualPath.Last() == new Vector2Int(targetX, targetY)) ? interaction : null;
                StartCoroutine(MoveRoutine(actualPath));
            }
            else
            {
                THMapController.Instance.Log("Недостаточно очков хода!");
            }
        }

        private int GetTileCost(int x, int y)
        {
            var tileGo = GameObject.Find($"Tile_{x}_{y}");
            return tileGo ? tileGo.GetComponent<THMapTile>().moveCost : 1;
        }

        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            Queue<List<Vector2Int>> queue = new Queue<List<Vector2Int>>();
            queue.Enqueue(new List<Vector2Int> { start });
            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { start };

            while (queue.Count > 0)
            {
                var path = queue.Dequeue();
                var current = path.Last();
                if (current == end) return path;

                foreach (var next in GetNeighbors(current))
                {
                    if (!visited.Contains(next) && IsPassable(next.x, next.y))
                    {
                        visited.Add(next);
                        var newPath = new List<Vector2Int>(path) { next };
                        queue.Enqueue(newPath);
                    }
                }
            }
            return null;
        }

        private bool IsPassable(int x, int y)
        {
            var tileGo = GameObject.Find($"Tile_{x}_{y}");
            return tileGo != null && tileGo.GetComponent<THMapTile>().isPassable;
        }

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int p)
        {
            yield return new Vector2Int(p.x + 1, p.y);
            yield return new Vector2Int(p.x - 1, p.y);
            yield return new Vector2Int(p.x, p.y + 1);
            yield return new Vector2Int(p.x, p.y - 1);
        }

        private IEnumerator MoveRoutine(List<Vector2Int> path)
        {
            IsMoving = true;
            for (int i = 1; i < path.Count; i++)
            {
                Vector3 startPos = transform.position;
                Vector3 endPos = new Vector3(path[i].x, path[i].y, transform.position.z);
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * speed;
                    transform.position = Vector3.Lerp(startPos, endPos, t);
                    yield return null;
                }
                transform.position = endPos;
                THMapController.Instance.State.heroX = path[i].x;
                THMapController.Instance.State.heroY = path[i].y;
            }
            IsMoving = false;
            THMapController.Instance.UpdateUI();
            if (pendingInteraction != null) THMapController.Instance.HandleObjectInteraction(pendingInteraction);
        }
    }
}
