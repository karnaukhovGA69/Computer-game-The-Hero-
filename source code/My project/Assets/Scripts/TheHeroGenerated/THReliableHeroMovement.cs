using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Linq;

namespace TheHero.Generated
{
    public class THReliableHeroMovement : MonoBehaviour
    {
        public Transform heroTransform;
        public Camera mainCamera;
        public LayerMask tileLayer;
        public float moveSpeed = 4f;
        
        private bool isMoving = false;
        private Dictionary<Vector2Int, THTile> grid = new Dictionary<Vector2Int, THTile>();

        private void Start()
        {
            if (heroTransform == null) heroTransform = transform;
            if (mainCamera == null) mainCamera = Camera.main;
            InitializeGrid();
        }

        public void InitializeGrid()
        {
            grid.Clear();
            var tiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            foreach (var tile in tiles)
            {
                grid[new Vector2Int(tile.x, tile.y)] = tile;
            }
            Debug.Log($"[THReliableHeroMovement] Grid initialized with {grid.Count} tiles.");
        }

        private void Update()
        {
            if (isMoving) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

                HandleClick();
            }
        }

        private void HandleClick()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Ray ray = mainCamera.ScreenPointToRay(mousePos);
            RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction, 100f);

            if (hit.collider != null)
            {
                THTile tile = hit.collider.GetComponent<THTile>();
                if (tile == null)
                {
                    // Check if it's an object on a tile
                    Vector2Int pos = new Vector2Int(Mathf.RoundToInt(hit.point.x), Mathf.RoundToInt(hit.point.y));
                    if (grid.ContainsKey(pos)) tile = grid[pos];
                }

                if (tile != null)
                {
                    TryMoveTo(tile.x, tile.y);
                }
            }
        }

        public void TryMoveTo(int targetX, int targetY)
        {
            if (THManager.Instance == null || THManager.Instance.Data == null) return;
            var data = THManager.Instance.Data;

            Vector2Int start = new Vector2Int((int)data.heroX, (int)data.heroY);
            Vector2Int end = new Vector2Int(targetX, targetY);

            if (start == end) return;

            var path = FindPath(start, end);
            if (path == null || path.Count == 0)
            {
                StartCoroutine(FlashError(new Vector2Int(targetX, targetY)));
                if (THMessageSystem.Instance != null) THMessageSystem.Instance.ShowWarning("Путь невозможен");
                else Debug.LogWarning("Путь невозможен");
                return;
            }

            // Highlight target cell yellow
            HighlightCell(new Vector2Int(targetX, targetY), Color.yellow);

            // Calculate reachable path based on movement points
            List<Vector2Int> reachablePath = new List<Vector2Int>();
            int currentPoints = data.movementPoints;
            
            foreach (var step in path)
            {
                if (grid.TryGetValue(step, out THTile tile))
                {
                    if (currentPoints >= tile.moveCost)
                    {
                        currentPoints -= tile.moveCost;
                        reachablePath.Add(step);
                    }
                    else
                    {
                        break;
                    }
                }
            }

            if (reachablePath.Count > 0)
            {
                StartCoroutine(MoveAlongPath(reachablePath, data.movementPoints - currentPoints));
            }
            else
            {
                if (THMessageSystem.Instance != null) THMessageSystem.Instance.ShowWarning("Недостаточно очков хода");
            }
        }

        private void HighlightCell(Vector2Int pos, Color color)
        {
            var marker = new GameObject("CellHighlight");
            marker.transform.position = new Vector3(pos.x, pos.y, -0.5f);
            var sr = marker.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/UI/white_pixel");
            sr.color = color;
            sr.sortingOrder = 18;
            marker.transform.localScale = Vector3.one * 0.8f;
            Destroy(marker, 0.5f);
        }

        private IEnumerator FlashError(Vector2Int pos)
        {
            for (int i = 0; i < 3; i++)
            {
                HighlightCell(pos, Color.red);
                yield return new WaitForSeconds(0.2f);
            }
        }

        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (!grid.ContainsKey(end) || !grid[end].walkable) return null;

            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);

            Dictionary<Vector2Int, Vector2Int> cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            cameFrom[start] = start;

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();
                if (current == end) break;

                foreach (var next in GetNeighbors(current))
                {
                    if (grid.TryGetValue(next, out THTile tile) && tile.walkable && !cameFrom.ContainsKey(next))
                    {
                        cameFrom[next] = current;
                        queue.Enqueue(next);
                    }
                }
            }

            if (!cameFrom.ContainsKey(end)) return null;

            List<Vector2Int> path = new List<Vector2Int>();
            Vector2Int curr = end;
            while (curr != start)
            {
                path.Add(curr);
                curr = cameFrom[curr];
            }
            path.Reverse();
            return path;
        }

        private IEnumerable<Vector2Int> GetNeighbors(Vector2Int p)
        {
            yield return new Vector2Int(p.x + 1, p.y);
            yield return new Vector2Int(p.x - 1, p.y);
            yield return new Vector2Int(p.x, p.y + 1);
            yield return new Vector2Int(p.x, p.y - 1);
        }

        private List<GameObject> pathMarkers = new List<GameObject>();

        private IEnumerator MoveAlongPath(List<Vector2Int> path, int cost)
        {
            isMoving = true;
            HighlightPath(path);
            
            foreach (var step in path)
            {
                Vector3 targetPos = new Vector3(step.x, step.y, heroTransform.position.z);
                while (Vector3.Distance(heroTransform.position, targetPos) > 0.01f)
                {
                    heroTransform.position = Vector3.MoveTowards(heroTransform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                heroTransform.position = targetPos;
                
                THManager.Instance.Data.heroX = step.x;
                THManager.Instance.Data.heroY = step.y;
            }

            THManager.Instance.Data.movementPoints -= cost;
            isMoving = false;
            
            ClearPathHighlight();

            if (THMapController.Instance != null) THMapController.Instance.UpdateUI();
            // THManager.Instance.SaveGame();
            }

        private void HighlightPath(List<Vector2Int> path)
        {
            ClearPathHighlight();
            foreach (var step in path)
            {
                var marker = new GameObject("PathMarker");
                marker.transform.position = new Vector3(step.x, step.y, -1);
                var sr = marker.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Sprites/UI/white_pixel");
sr.color = new Color(0, 1, 0, 0.5f);
                sr.sortingOrder = 15;
                marker.transform.localScale = Vector3.one * 0.3f;
                pathMarkers.Add(marker);
            }
        }

        private void ClearPathHighlight()
        {
            foreach (var m in pathMarkers) Destroy(m);
            pathMarkers.Clear();
        }
}
}
