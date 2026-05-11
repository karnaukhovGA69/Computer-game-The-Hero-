using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public class THMapGridInput : MonoBehaviour
    {
        public static THMapGridInput Instance { get; private set; }

        private Dictionary<Vector2Int, THTile> _tiles = new Dictionary<Vector2Int, THTile>();
        private Camera _mainCamera;
        private float _tileSize = 1f;
        
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

        private void Start()
        {
            // Hover labels are handled by THSingleMapHoverLabel from THMapObject.
            // Do not create a second MapCaption label here.
        }

        public void RefreshGrid()
        {
            _tiles.Clear();
            var foundTiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include);
            if (foundTiles.Length > 0)
            {
                foreach (var tile in foundTiles)
                {
                    _tiles[new Vector2Int(tile.x, tile.y)] = tile;
                }

                MinX = foundTiles.Min(t => t.transform.position.x);
                MaxX = foundTiles.Max(t => t.transform.position.x);
                MinY = foundTiles.Min(t => t.transform.position.y);
                MaxY = foundTiles.Max(t => t.transform.position.y);
                
                if (foundTiles.Length > 1)
                {
                    _tileSize = Vector2.Distance(foundTiles[0].transform.position, foundTiles[1].transform.position);
                    if (_tileSize > 2f || _tileSize < 0.1f) _tileSize = 1f;
                }
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
