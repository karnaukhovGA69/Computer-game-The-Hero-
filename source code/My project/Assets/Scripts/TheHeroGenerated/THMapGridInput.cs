using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public class THMapGridInput : MonoBehaviour
    {
        public static THMapGridInput Instance { get; private set; }

        private Dictionary<Vector2Int, THTile> _tiles = new Dictionary<Vector2Int, THTile>();
        private readonly Dictionary<Vector2Int, THTile> _tilemapRuntimeTiles = new Dictionary<Vector2Int, THTile>();
        private Camera _mainCamera;
        private float _tileSize = 1f;
        private GameObject _tilemapRuntimeRoot;

        [Header("Tilemap Source")]
        public Tilemap GroundTilemap;
        public Tilemap BlockingTilemap;
        public Tilemap WaterTilemap;
        public Tilemap BridgeTilemap;
        public Tilemap RoadTilemap;
        
        public float MinX { get; private set; }
        public float MaxX { get; private set; }
        public float MinY { get; private set; }
        public float MaxY { get; private set; }

        private Text _captionText;

        private void Awake()
        {
            Instance = this;
            _mainCamera = Camera.main;
            RefreshGrid();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            // Hover labels are handled by THSingleMapHoverLabel from THMapObject.
            // Do not create a second MapCaption label here.
        }

        public void RefreshGrid()
        {
            _tiles.Clear();

            ResolveTilemaps();
            bool loadedFromGroundTilemap = RegisterGroundTilemapTiles();

            if (!loadedFromGroundTilemap)
            {
                var foundTiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
                foreach (var tile in foundTiles)
                {
                    _tiles[new Vector2Int(tile.x, tile.y)] = tile;
                }
            }

            if (_tiles.Count > 0)
            {
                MinX = _tiles.Values.Min(t => t.transform.position.x);
                MaxX = _tiles.Values.Max(t => t.transform.position.x);
                MinY = _tiles.Values.Min(t => t.transform.position.y);
                MaxY = _tiles.Values.Max(t => t.transform.position.y);

                if (GroundTilemap != null && GroundTilemap.layoutGrid != null)
                {
                    Vector3 cellSize = GroundTilemap.layoutGrid.cellSize;
                    _tileSize = Mathf.Max(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
                    if (_tileSize < 0.1f) _tileSize = 1f;
                }
                else if (_tiles.Count > 1)
                {
                    var foundTiles = _tiles.Values.ToArray();
                    _tileSize = Vector2.Distance(foundTiles[0].transform.position, foundTiles[1].transform.position);
                    if (_tileSize > 2f || _tileSize < 0.1f) _tileSize = 1f;
                }
            }
        }

        private void ResolveTilemaps()
        {
            if (GroundTilemap == null) GroundTilemap = FindTilemap("GroundTilemap");
            if (BlockingTilemap == null) BlockingTilemap = FindTilemap("BlockingTilemap");
            if (WaterTilemap == null) WaterTilemap = FindTilemap("WaterTilemap");
            if (BridgeTilemap == null) BridgeTilemap = FindTilemap("BridgeTilemap");
            if (RoadTilemap == null) RoadTilemap = FindTilemap("RoadTilemap");
        }

        private static Tilemap FindTilemap(string tilemapName)
        {
            return Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
                .FirstOrDefault(t => t != null && t.name == tilemapName);
        }

        private bool RegisterGroundTilemapTiles()
        {
            if (GroundTilemap == null)
                return false;

            BoundsInt bounds = GroundTilemap.cellBounds;
            var activeCells = new HashSet<Vector2Int>();

            foreach (Vector3Int cell in bounds.allPositionsWithin)
            {
                if (!GroundTilemap.HasTile(cell))
                    continue;

                var key = new Vector2Int(cell.x, cell.y);
                activeCells.Add(key);

                THTile tile = GetOrCreateTilemapRuntimeTile(key);
                tile.x = cell.x;
                tile.y = cell.y;
                tile.transform.position = GroundTilemap.GetCellCenterWorld(cell);

                ConfigureTileFromTilemaps(tile, cell);
                _tiles[key] = tile;
            }

            RemoveStaleRuntimeTiles(activeCells);
            return activeCells.Count > 0;
        }

        private THTile GetOrCreateTilemapRuntimeTile(Vector2Int key)
        {
            if (_tilemapRuntimeTiles.TryGetValue(key, out THTile tile) && tile != null)
                return tile;

            if (_tilemapRuntimeRoot == null)
            {
                _tilemapRuntimeRoot = new GameObject("TilemapWalkGridRuntime");
                _tilemapRuntimeRoot.hideFlags = HideFlags.DontSave;
            }

            var go = new GameObject($"GroundTile_{key.x}_{key.y}");
            go.hideFlags = HideFlags.DontSave;
            go.transform.SetParent(_tilemapRuntimeRoot.transform, false);

            tile = go.AddComponent<THTile>();
            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = Vector2.one;

            _tilemapRuntimeTiles[key] = tile;
            return tile;
        }

        private void ConfigureTileFromTilemaps(THTile tile, Vector3Int cell)
        {
            bool hasBridge = BridgeTilemap != null && BridgeTilemap.HasTile(cell);
            bool hasRoad = RoadTilemap != null && RoadTilemap.HasTile(cell);
            bool hasWater = WaterTilemap != null && WaterTilemap.HasTile(cell);
            bool hasBlocker = BlockingTilemap != null && BlockingTilemap.HasTile(cell);

            if (hasBridge)
            {
                tile.tileType = "bridge";
            }
            else if (hasBlocker)
            {
                tile.tileType = "mountain";
            }
            else if (hasWater)
            {
                tile.tileType = "water";
            }
            else if (hasRoad)
            {
                tile.tileType = "road";
            }
            else
            {
                tile.tileType = "grass";
            }

            tile.ApplyMovementBalance();
        }

        private void RemoveStaleRuntimeTiles(HashSet<Vector2Int> activeCells)
        {
            var stale = _tilemapRuntimeTiles.Keys.Where(k => !activeCells.Contains(k)).ToList();
            foreach (Vector2Int key in stale)
            {
                THTile tile = _tilemapRuntimeTiles[key];
                if (tile != null && tile.gameObject != null)
                {
                    if (Application.isPlaying)
                        Destroy(tile.gameObject);
                    else
                        DestroyImmediate(tile.gameObject);
                }
                _tilemapRuntimeTiles.Remove(key);
            }
        }

        private void CreateCaptionUI()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var go = new GameObject("MapCaption", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(canvas.transform, false);
            _captionText = go.GetComponent<Text>();
            _captionText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _captionText.fontSize = 28;
            _captionText.color = new Color(1f, 0.9f, 0.5f);
            _captionText.alignment = TextAnchor.MiddleCenter;
            _captionText.raycastTarget = false;
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 50);
            rt.sizeDelta = new Vector2(800, 50);
            
            var outline = go.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
        }

        private void Update()
        {
            if (_captionText != null) _captionText.gameObject.SetActive(false);
        }

        public bool TryGetTileFromMouse(out THTile tile, out string reason)
        {
            tile = null;
            reason = "";

            if (UnityEngine.InputSystem.Mouse.current == null) return false;
            if (_mainCamera == null) _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                reason = "Main camera not found";
                return false;
            }

            Vector2 mousePosition = UnityEngine.InputSystem.Mouse.current.position.ReadValue();

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                var pointerEventData = new PointerEventData(EventSystem.current) { position = mousePosition };
                var results = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointerEventData, results);
                
                bool blockedByInteractive = results.Any(r => 
                    r.gameObject.GetComponent<UnityEngine.UI.Button>() != null || 
                    r.gameObject.GetComponent<UnityEngine.UI.Selectable>() != null);

                if (blockedByInteractive)
                {
                    reason = "Click blocked by interactive UI";
                    return false;
                }
            }

            Vector3 worldPoint = _mainCamera.ScreenToWorldPoint(new Vector3(mousePosition.x, mousePosition.y, -_mainCamera.transform.position.z));
            worldPoint.z = 0;

            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);
            if (hit.collider != null)
            {
                tile = hit.collider.GetComponent<THTile>();
                if (tile != null)
                {
                    reason = "Detected via Raycast";
                    return true;
                }
            }

            if (GroundTilemap != null)
            {
                Vector3Int cell = GroundTilemap.WorldToCell(worldPoint);
                if (_tiles.TryGetValue(new Vector2Int(cell.x, cell.y), out tile))
                {
                    reason = "Detected via GroundTilemap";
                    return true;
                }
            }

            Vector2Int gridPos = new Vector2Int(Mathf.RoundToInt(worldPoint.x), Mathf.RoundToInt(worldPoint.y));
            if (_tiles.TryGetValue(gridPos, out tile))
            {
                float dist = Vector2.Distance(worldPoint, tile.transform.position);
                if (dist < _tileSize * 0.75f)
                {
                    reason = "Detected via Coordinate Fallback";
                    return true;
                }
            }

            reason = "No tile under cursor";
            return false;
        }

        public THTile GetTileAt(int x, int y)
        {
            _tiles.TryGetValue(new Vector2Int(x, y), out THTile tile);
            return tile;
        }
        
        public IEnumerable<THTile> GetAllTiles() => _tiles.Values;
    }
}
