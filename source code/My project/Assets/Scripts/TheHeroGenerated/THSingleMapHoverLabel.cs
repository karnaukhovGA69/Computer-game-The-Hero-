using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheHero.Generated
{
    public class THSingleMapHoverLabel : MonoBehaviour
    {
        private static THSingleMapHoverLabel _instance;
        public static THSingleMapHoverLabel Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THSingleMapHoverLabel");
                    _instance = go.AddComponent<THSingleMapHoverLabel>();
                    DontDestroyOnLoad(go);
                    _instance.CreateUI();
                }
                return _instance;
            }
        }

        private GameObject _labelRoot;
        private Text _labelText;
        private Canvas _canvas;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void CreateUI()
        {
            _labelRoot = new GameObject("MapHoverLabelRoot");
            _labelRoot.transform.SetParent(transform);
            
            _canvas = _labelRoot.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.WorldSpace;
            _canvas.sortingOrder = 200;
            
            _labelRoot.GetComponent<RectTransform>().sizeDelta = new Vector2(4, 1);
            _labelRoot.transform.localScale = Vector3.one * 0.5f;

            var bgGo = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(_labelRoot.transform, false);
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.7f);
            bgGo.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 60);
            bgGo.transform.localScale = Vector3.one * 0.01f;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_labelRoot.transform, false);
            _labelText = textGo.GetComponent<Text>();
            _labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _labelText.fontSize = 28;
            _labelText.alignment = TextAnchor.MiddleCenter;
            _labelText.color = Color.white;
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 60);
            textGo.transform.localScale = Vector3.one * 0.01f;

            var outline = textGo.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(1, -1);

            _labelRoot.SetActive(false);
        }

        public void Show(string text, Vector3 worldPosition)
        {
            if (_labelRoot == null) CreateUI();
            
            _labelText.text = text;
            _labelRoot.transform.position = worldPosition + Vector3.up * 1.2f;
            _labelRoot.SetActive(true);
        }

        public void Hide()
        {
            if (_labelRoot != null)
            {
                _labelRoot.SetActive(false);
                _labelText.text = "";
            }
        }

        public void Clear() => Hide();

        public void ForceClearAllStaleLabels()
        {
            // Find and destroy any object named "Tooltip", "Label", etc.
            var allObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
            foreach (var go in allObjects)
            {
                if (go.name == "Tooltip" || go.name == "Tooltip(Clone)" || go.name == "FloatingText" || go.name.Contains("HoverLabel"))
                {
                    // Only destroy if it's not our instance
                    if (go.transform.root != transform)
                    {
                        Destroy(go);
                    }
                }
            }
        }
}
}
