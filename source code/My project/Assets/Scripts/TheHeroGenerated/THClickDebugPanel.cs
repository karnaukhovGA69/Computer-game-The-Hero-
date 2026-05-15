using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Text;

namespace TheHero.Generated
{
    public class THClickDebugPanel : MonoBehaviour
    {
        public static THClickDebugPanel Instance { get; private set; }
        
        private Text _debugText;
        private RectTransform _panelRect;
        private string _lastClickResult = "None";

        private void Awake()
        {
            Instance = this;
            CreateUI();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void CreateUI()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var panelGo = new GameObject("DebugPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform, false);
            _panelRect = panelGo.GetComponent<RectTransform>();
            _panelRect.anchorMin = new Vector2(1, 0);
            _panelRect.anchorMax = new Vector2(1, 0);
            _panelRect.pivot = new Vector2(1, 0);
            _panelRect.anchoredPosition = new Vector2(-10, 10);
            _panelRect.sizeDelta = new Vector2(300, 200);
            panelGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            panelGo.GetComponent<Image>().raycastTarget = false;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(panelGo.transform, false);
            _debugText = textGo.GetComponent<Text>();
            _debugText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _debugText.fontSize = 14;
            _debugText.color = Color.white;
            _debugText.alignment = TextAnchor.UpperLeft;
            _debugText.raycastTarget = false;
            
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = new Vector2(-10, -10);
        }

        public void SetLastClickResult(string result)
        {
            _lastClickResult = result;
        }

        private void Update()
        {
            if (_debugText == null) return;

            StringBuilder sb = new StringBuilder();
            
            Vector3 mouseWorld = Vector3.zero;
            if (Camera.main != null && UnityEngine.InputSystem.Mouse.current != null)
            {
                Vector2 mousePos = UnityEngine.InputSystem.Mouse.current.position.ReadValue();
                mouseWorld = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, -Camera.main.transform.position.z));
            }

            sb.AppendLine($"Mouse World: {mouseWorld.x:F1}, {mouseWorld.y:F1}");
            
            THTile tile = null;
            if (THMapGridInput.Instance != null)
            {
                THMapGridInput.Instance.TryGetTileFromMouse(out tile, out string reason);
                sb.AppendLine($"Selected Tile: {(tile != null ? $"{tile.x}, {tile.y}" : "None")}");
                sb.AppendLine($"Detection: {reason}");
            }

            var hero = GameObject.Find("Hero");
            if (hero != null)
            {
                var mover = hero.GetComponent<THStrictGridHeroMovement>();
                if (mover != null)
                {
                    sb.AppendLine($"Hero Grid: {mover.currentX}, {mover.currentY}");
                    sb.AppendLine($"Is Moving: {mover.isMoving}");
                }
            }

            sb.AppendLine($"Over UI: {EventSystem.current?.IsPointerOverGameObject()}");
            sb.AppendLine($"Last Click: {_lastClickResult}");
            
            if (THManager.Instance?.Data != null)
            {
                sb.AppendLine($"Movement Points: {THManager.Instance.Data.movementPoints}");
            }

            _debugText.text = sb.ToString();
        }
    }
}
