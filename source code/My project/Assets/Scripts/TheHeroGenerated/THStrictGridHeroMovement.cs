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
        public float moveSpeed = 6f;
        public bool isMoving;
        public bool keyboardDebugMovement = true;

        private Camera _mainCamera;
        private List<GameObject> _pathMarkers = new List<GameObject>();
        private GameObject _hoverMarker;

        public void InitializeGrid()
        {
            if (THMapGridInput.Instance != null)
                THMapGridInput.Instance.RefreshGrid();
        }

        public bool IsMoving => isMoving;

        public void SetPositionImmediate(int x, int y)
        {
            currentX = x;
            currentY = y;
            if (THMapGridInput.Instance != null)
            {
                var tile = THMapGridInput.Instance.GetTileAt(x, y);
                if (tile != null) transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, 0);
            }
            else
            {
                transform.position = new Vector3(x, y, 0);
            }
        }

        public void TryMoveTo(int x, int y, THMapObject interaction = null)
        {
            if (interaction != null && x == currentX && y == currentY)
            {
                if (THMapController.Instance != null) THMapController.Instance.HandleObjectInteraction(interaction);
                return;
            }

            if (THMapGridInput.Instance != null)
            {
                var targetTile = THMapGridInput.Instance.GetTileAt(x, y);
                if (targetTile != null)
                {
                    PerformMovementToTile(targetTile);
                }
            }
        }

        private void PerformMovementToTile(THTile targetTile)
        {
            if (targetTile.x == currentX && targetTile.y == currentY)
            {
                if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult("Already on tile");
                return;
            }

            if (!targetTile.walkable)
            {
                if (THMapController.Instance) THMapController.Instance.Log("Клетка непроходима");
                return;
            }

            var path = FindPath(new Vector2Int(currentX, currentY), new Vector2Int(targetTile.x, targetTile.y));
            if (path == null)
            {
                if (THMapController.Instance) THMapController.Instance.Log("Путь невозможен");
            }
            else
            {
                int totalPoints = THManager.Instance.Data.movementPoints;
                var reachablePath = GetReachablePath(path, totalPoints, out int actualCost);

                if (reachablePath.Count == 0)
                {
                    if (THMapController.Instance) THMapController.Instance.Log("Недостаточно очков хода");
                    return;
                }

                bool partial = reachablePath.Count < path.Count;
                StartCoroutine(MoveAlongPath(reachablePath, actualCost, partial));
            }
        }

        private void Start()
        {
            _mainCamera = Camera.main;
            SnapToNearestTile();
            EnsureVisibility();
            CreateHoverMarker();
        }

        private void EnsureVisibility()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.enabled = true;
            sr.sortingOrder = 50;
            transform.position = new Vector3(transform.position.x, transform.position.y, 0);
        }

        private void SnapToNearestTile()
        {
            if (THManager.Instance != null && THManager.Instance.Data != null)
            {
                currentX = (int)THManager.Instance.Data.heroX;
                currentY = (int)THManager.Instance.Data.heroY;
            }

            var input = THMapGridInput.Instance;
            if (input != null)
            {
                var tile = input.GetTileAt(currentX, currentY);
                if (tile == null || !tile.walkable)
                {
                    // Find safe spot near castle or any walkable tile
                    GameObject castle = GameObject.Find("Castle_Player") ?? GameObject.Find("Castle") ?? GameObject.Find("Base");
                    Vector3 referencePos = castle != null ? castle.transform.position : transform.position;

                    var safeTile = input.GetAllTiles()
                        .Where(t => t.walkable)
                        .OrderBy(t => Vector2.Distance(t.transform.position, referencePos))
                        .FirstOrDefault();
                    
                    if (safeTile != null)
                    {
                        currentX = safeTile.x;
                        currentY = safeTile.y;
                        Debug.LogWarning($"[TheHeroRecovery] Hero was in invalid position ({THManager.Instance.Data.heroX}, {THManager.Instance.Data.heroY}). Clamped to safe tile at {currentX}, {currentY}");
                        
                        THManager.Instance.Data.heroX = currentX;
                        THManager.Instance.Data.heroY = currentY;
                    }
                }

                tile = input.GetTileAt(currentX, currentY);
                if (tile != null)
                {
                    transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y, 0);
                }
            }
        }

        private void CreateHoverMarker()
        {
            _hoverMarker = new GameObject("HoverMarker");
            var sr = _hoverMarker.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Sprites/UI/white_pixel");
            sr.color = new Color(1, 1, 0, 0.3f);
            sr.sortingOrder = 40;
            _hoverMarker.transform.localScale = Vector3.one * 0.9f;
            _hoverMarker.SetActive(false);
        }

        private void Update()
        {
            UpdateHover();

            if (isMoving) return;
            if (THManager.Instance != null && THManager.Instance.Data != null && THManager.Instance.Data.gameCompleted) return;

            if (keyboardDebugMovement)
{
                HandleKeyboard();
            }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleMouseClick();
            }
        }

        private void UpdateHover()
        {
            if (THMapGridInput.Instance == null || _hoverMarker == null) return;

            if (THMapGridInput.Instance.TryGetTileFromMouse(out THTile tile, out _))
            {
                _hoverMarker.SetActive(true);
                _hoverMarker.transform.position = tile.transform.position + Vector3.back * 0.05f;
                var sr = _hoverMarker.GetComponent<SpriteRenderer>();
                sr.color = tile.walkable ? new Color(0, 1, 0, 0.3f) : new Color(1, 0, 0, 0.3f);
            }
            else
            {
                _hoverMarker.SetActive(false);
            }
        }

        private void HandleKeyboard()
        {
            if (Keyboard.current == null) return;
            if (THMapGridInput.Instance == null) return;

            Vector2Int dir = Vector2Int.zero;
            if (Keyboard.current.wKey.wasPressedThisFrame || Keyboard.current.upArrowKey.wasPressedThisFrame) dir = Vector2Int.up;
            else if (Keyboard.current.sKey.wasPressedThisFrame || Keyboard.current.downArrowKey.wasPressedThisFrame) dir = Vector2Int.down;
            else if (Keyboard.current.aKey.wasPressedThisFrame || Keyboard.current.leftArrowKey.wasPressedThisFrame) dir = Vector2Int.left;
            else if (Keyboard.current.dKey.wasPressedThisFrame || Keyboard.current.rightArrowKey.wasPressedThisFrame) dir = Vector2Int.right;

            if (dir != Vector2Int.zero)
            {
                THTile target = THMapGridInput.Instance.GetTileAt(currentX + dir.x, currentY + dir.y);
                if (target != null && target.walkable)
                {
                    if (THManager.Instance.Data.movementPoints >= target.moveCost)
                    {
                        StartCoroutine(MoveAlongPath(new List<Vector2Int> { new Vector2Int(target.x, target.y) }, target.moveCost, false));
                    }
                    else
                    {
                        Debug.Log("[TheHeroClickDebug] Keyboard move failed: Not enough movement points");
                    }
                }
            }
        }

        private void HandleMouseClick()
        {
            if (THMapGridInput.Instance == null)
            {
                Debug.LogWarning("[TheHeroClickDebug] Click failed: grid input is missing");
                return;
            }

            if (THMapGridInput.Instance.TryGetTileFromMouse(out THTile targetTile, out string reason))
            {
                string msg = $"Clicked tile: {targetTile.x}, {targetTile.y} ({reason})";
                Debug.Log($"[TheHeroClickDebug] {msg}");
                if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult(msg);

                if (targetTile.x == currentX && targetTile.y == currentY)
                {
                    msg = "Already on this tile";
                    Debug.Log($"[TheHeroClickDebug] {msg}");
                    if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult(msg);
                    return;
                }

                if (!targetTile.walkable)
                {
                    msg = "Target tile is not walkable: " + targetTile.tileType;
                    Debug.Log($"[TheHeroClickDebug] {msg}");
                    if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult(msg);
                    return;
                }

                var path = FindPath(new Vector2Int(currentX, currentY), new Vector2Int(targetTile.x, targetTile.y));
                if (path == null)
                {
                    msg = "Path not found";
                    Debug.Log($"[TheHeroClickDebug] {msg}");
                    if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult(msg);
                }
                else
                {
                    int totalPoints = THManager.Instance.Data.movementPoints;
                    var reachablePath = GetReachablePath(path, totalPoints, out int actualCost);

                    if (reachablePath.Count == 0)
                    {
                        msg = "Not enough movement points";
                        Debug.Log($"[TheHeroClickDebug] {msg}");
                        if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult(msg);
                        return;
                    }

                    bool partial = reachablePath.Count < path.Count;
                    msg = partial ? "Moving (Partial Path)" : "Moving (Full Path)";
                    if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult(msg);
                    StartCoroutine(MoveAlongPath(reachablePath, actualCost, partial));
                }
            }
            else
            {
                Debug.Log($"[TheHeroClickDebug] Click failed: {reason}");
                if (THClickDebugPanel.Instance != null) THClickDebugPanel.Instance.SetLastClickResult(reason);
            }
        }

        private List<Vector2Int> FindPath(Vector2Int start, Vector2Int end)
        {
            var input = THMapGridInput.Instance;
            if (input == null) return null;

            var queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
            cameFrom[start] = start;

            // Cache blockers for faster lookup
            var allMapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Exclude);
            var blockers = new HashSet<Vector2Int>();
            foreach (var obj in allMapObjects)
            {
                if (obj.blocksMovement) blockers.Add(new Vector2Int(obj.targetX, obj.targetY));
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
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
                    var tile = input.GetTileAt(neighbor.x, neighbor.y);
                    
                    // Allow endpoint to be a blocker (interaction logic will handle stopping)
                    bool isBlocker = blockers.Contains(neighbor) && neighbor != end;

                    if (tile != null && tile.walkable && !isBlocker && !cameFrom.ContainsKey(neighbor))
                    {
                        cameFrom[neighbor] = current;
                        queue.Enqueue(neighbor);
                    }
                }
            }
return null;
        }

        private List<Vector2Int> GetReachablePath(List<Vector2Int> fullPath, int points, out int cost)
        {
            var reachable = new List<Vector2Int>();
            cost = 0;
            var input = THMapGridInput.Instance;
            foreach (var pos in fullPath)
            {
                var tile = input.GetTileAt(pos.x, pos.y);
                if (tile == null) break;
                if (points >= cost + tile.moveCost)
                {
                    cost += tile.moveCost;
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

            var input = THMapGridInput.Instance;
            int spentCost = 0;
            foreach (var pos in path)
            {
                // Check if target tile has a blocker that should stop us early
                var objAtPos = GetObjectAt(pos);
                if (objAtPos != null && objAtPos.blocksMovement)
                {
                     if (THMapController.Instance) THMapController.Instance.HandleObjectInteraction(objAtPos);
                     break;
                }

                if (input == null) break;
                var tile = input.GetTileAt(pos.x, pos.y);
                if (tile == null) break;
Vector3 targetPos = tile.transform.position;
                targetPos.z = transform.position.z;

                while (Vector3.Distance(transform.position, targetPos) > 0.01f)
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
                    yield return null;
                }
                transform.position = targetPos;
                if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("hero_step");
                currentX = pos.x;
                currentY = pos.y;
                spentCost += tile.moveCost;

                THManager.Instance.Data.heroX = currentX;
                THManager.Instance.Data.heroY = currentY;
            }

            THManager.Instance.Data.movementPoints = Mathf.Max(0, THManager.Instance.Data.movementPoints - spentCost);
            ClearPathHighlight();
            isMoving = false;

            if (THMapController.Instance) THMapController.Instance.UpdateUI();
            // THManager.Instance.SaveGame();

            CheckInteractions(new Vector2Int(currentX, currentY));
        }

        private void CheckInteractions(Vector2Int pos)
        {
            var objects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            foreach (var obj in objects)
            {
                if (obj.targetX == pos.x && obj.targetY == pos.y)
                {
                    if (THMapController.Instance) THMapController.Instance.HandleObjectInteraction(obj);
                    break;
                }
            }
        }

        private THMapObject GetObjectAt(Vector2Int pos)
        {
            var objects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Exclude);
            return objects.FirstOrDefault(o => o.targetX == pos.x && o.targetY == pos.y);
        }

        private void HighlightPath(List<Vector2Int> path, Color color)
        {
            ClearPathHighlight();
            var input = THMapGridInput.Instance;
            foreach (var pos in path)
            {
                var tile = input.GetTileAt(pos.x, pos.y);
                if (tile == null) continue;
                var marker = new GameObject("PathMarker");
                marker.transform.position = tile.transform.position + Vector3.back * 0.1f;
                var sr = marker.AddComponent<SpriteRenderer>();
                sr.sprite = Resources.Load<Sprite>("Sprites/UI/white_pixel");
sr.color = color;
                sr.sortingOrder = 45;
                marker.transform.localScale = Vector3.one * 0.25f;
                _pathMarkers.Add(marker);
            }
        }

        private void ClearPathHighlight()
        {
            foreach (var m in _pathMarkers) Destroy(m);
            _pathMarkers.Clear();
        }
    }
}
