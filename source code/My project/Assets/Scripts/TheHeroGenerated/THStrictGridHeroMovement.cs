using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public class THStrictGridHeroMovement : MonoBehaviour
    {
        public int currentX;
        public int currentY;
        public int movementPoints = 20;
        public int maxMovementPoints = 20;
        public float moveSpeed = 6f;
        public bool isMoving;

        private Dictionary<Vector2Int, THTile> _tiles = new Dictionary<Vector2Int, THTile>();
        private Camera _mainCamera;
        private List<GameObject> _pathMarkers = new List<GameObject>();

        private void Start()
        {
            _mainCamera = Camera.main;
            InitializeGrid();
            SnapToNearestTile();
        }

        public void InitializeGrid()
        {
            _tiles.Clear();
            var foundTiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            foreach (var tile in foundTiles)
            {
                _tiles[new Vector2Int(tile.x, tile.y)] = tile;
            }
        }

        private void SnapToNearestTile()
        {
            if (THManager.Instance != null && THManager.Instance.Data != null)
            {
                currentX = (int)THManager.Instance.Data.heroX;
                currentY = (int)THManager.Instance.Data.heroY;
            }

            if (!_tiles.ContainsKey(new Vector2Int(currentX, currentY)))
            {
                // Fallback to closest walkable tile
                var nearest = _tiles.Values.Where(t => t.walkable).OrderBy(t => Vector2.Distance(t.transform.position, transform.position)).FirstOrDefault();
                if (nearest != null)
                {
                    currentX = nearest.x;
                    currentY = nearest.y;
                }
            }

            if (_tiles.TryGetValue(new Vector2Int(currentX, currentY), out THTile currentTile))
            {
                transform.position = new Vector3(currentTile.transform.position.x, currentTile.transform.position.y, 0);
            }
        }

        private void Update()
        {
            if (isMoving) return;

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                HandleClick();
            }
        }

        private void HandleClick()
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -_mainCamera.transform.position.z));
            
            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            THTile targetTile = null;

            if (hit.collider != null)
            {
                targetTile = hit.collider.GetComponent<THTile>();
                if (targetTile == null)
                {
                    // If clicked on an object, find tile at that position
                    Vector2Int pos = new Vector2Int(Mathf.RoundToInt(hit.collider.transform.position.x), Mathf.RoundToInt(hit.collider.transform.position.y));
                    _tiles.TryGetValue(pos, out targetTile);
                }
            }

            if (targetTile != null)
            {
                if (!targetTile.walkable)
                {
                    THMapController.Instance.Log("Эта клетка непроходима");
                    StartCoroutine(FlashTileColor(targetTile, Color.red));
                    return;
                }

                var path = FindPath(new Vector2Int(currentX, currentY), new Vector2Int(targetTile.x, targetTile.y));
                if (path == null)
                {
                    THMapController.Instance.Log("Путь невозможен");
                    StartCoroutine(FlashTileColor(targetTile, Color.red));
                }
                else
                {
                    int totalPoints = THManager.Instance.Data.movementPoints;
                    var reachablePath = GetReachablePath(path, totalPoints, out int actualCost);

                    if (reachablePath.Count == 0)
                    {
                        THMapController.Instance.Log("Недостаточно очков хода");
                        return;
                    }

                    bool partial = reachablePath.Count < path.Count;
                    StartCoroutine(MoveAlongPath(reachablePath, actualCost, partial));
                }
            }
            else
            {
                THMapController.Instance.Log("Кликните по клетке карты");
            }
        }

        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            if (!_tiles.ContainsKey(end) || !_tiles[end].walkable) return null;

            var openSet = new Queue<Vector2Int>();
            openSet.Enqueue(start);
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            cameFrom[start] = start;

            while (openSet.Count > 0)
            {
                var current = openSet.Dequeue();
                if (current == end)
                {
                    var path = new List<Vector2Int>();
                    while (current != start)
                    {
                        path.Add(current);
                        current = cameFrom[current];
                    }
                    path.Reverse();
                    return path;
                }

                foreach (var dir in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                {
                    var neighbor = current + dir;
                    if (_tiles.TryGetValue(neighbor, out THTile tile) && tile.walkable && !cameFrom.ContainsKey(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        openSet.Enqueue(neighbor);
                    }
                }
            }
            return null;
        }

        private List<Vector2Int> GetReachablePath(List<Vector2Int> fullPath, int points, out int cost)
        {
            var reachable = new List<Vector2Int>();
            cost = 0;
            foreach (var pos in fullPath)
            {
                int moveCost = _tiles[pos].moveCost;
                if (points >= cost + moveCost)
                {
                    cost += moveCost;
                    reachable.Add(pos);
                }
                else break;
            }
            return reachable;
        }

        private IEnumerator MoveAlongPath(List<Vector2Int> path, int cost, bool partial)
        {
            isMoving = true;
            HighlightPath(path, partial ? Color.yellow : Color.green);

            foreach (var pos in path)
            {
                Vector3 targetPos = _tiles[pos].transform.position;
                targetPos.z = transform.position.z;

                while (Vector3.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetPos;
                currentX = pos.x;
                currentY = pos.y;
                
                // Update persistent data every step for safety
                THManager.Instance.Data.heroX = currentX;
                THManager.Instance.Data.heroY = currentY;
            }

            THManager.Instance.Data.movementPoints -= cost;
            ClearPathHighlight();
            isMoving = false;

            THMapController.Instance.UpdateUI();
            THManager.Instance.SaveGame();

            // Check for interactions at the final tile
            CheckInteractions(new Vector2Int(currentX, currentY));
        }

        private void CheckInteractions(Vector2Int pos)
        {
            // Find map objects at this position
            var objects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            foreach (var obj in objects)
            {
                if (obj.targetX == pos.x && obj.targetY == pos.y)
                {
                    THMapController.Instance.HandleObjectInteraction(obj);
                    break;
                }
            }
        }

        private void HighlightPath(List<Vector2Int> path, Color color)
        {
            ClearPathHighlight();
            foreach (var pos in path)
            {
                var marker = new GameObject("PathMarker");
                marker.transform.position = _tiles[pos].transform.position + Vector3.back * 0.5f;
                var sr = marker.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/Knob.psd");
                sr.color = color;
                sr.sortingOrder = 40;
                marker.transform.localScale = Vector3.one * 0.3f;
                _pathMarkers.Add(marker);
            }
        }

        private void ClearPathHighlight()
        {
            foreach (var m in _pathMarkers) Destroy(m);
            _pathMarkers.Clear();
        }

        private IEnumerator FlashTileColor(THTile tile, Color color)
        {
            var sr = tile.GetComponent<SpriteRenderer>();
            if (sr == null) yield break;
            Color original = sr.color;
            sr.color = color;
            yield return new WaitForSeconds(0.3f);
            sr.color = original;
        }
    }
}
