using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TheHero.Generated
{
    public class THMessageSystem : MonoBehaviour
    {
        private static THMessageSystem _instance;
        public static THMessageSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THMessageSystem");
                    _instance = go.AddComponent<THMessageSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private Canvas _canvas;
        private VerticalLayoutGroup _layout;
        private List<GameObject> _activeMessages = new List<GameObject>();

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            EnsureCanvas();
        }

        private void EnsureCanvas()
        {
            if (_canvas == null)
            {
                var canvasGo = new GameObject("MessageCanvas");
                canvasGo.transform.SetParent(transform);
                _canvas = canvasGo.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 999;
                
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                
                var panelGo = new GameObject("MessagePanel");
                panelGo.transform.SetParent(canvasGo.transform, false);
                var rect = panelGo.AddComponent<RectTransform>();
                rect.anchorMin = new Vector2(0.5f, 0.8f);
                rect.anchorMax = new Vector2(0.5f, 0.95f);
                rect.pivot = new Vector2(0.5f, 1f);
                rect.sizeDelta = new Vector2(600, 200);
                
                _layout = panelGo.AddComponent<VerticalLayoutGroup>();
                _layout.childAlignment = TextAnchor.UpperCenter;
                _layout.childControlHeight = true;
                _layout.childControlWidth = true;
                _layout.spacing = 10;
            }
        }

        public void ShowMessage(string text, Color? color = null)
        {
            StartCoroutine(MessageRoutine(text, color ?? Color.white));
        }

        public void ShowWarning(string text) => ShowMessage(text, Color.yellow);
        public void ShowError(string text) => ShowMessage(text, Color.red);
        public void ShowSuccess(string text) => ShowMessage(text, Color.green);

        private IEnumerator MessageRoutine(string text, Color color)
        {
            EnsureCanvas();
            
            var msgGo = new GameObject("Message", typeof(RectTransform), typeof(Image));
            msgGo.transform.SetParent(_layout.transform, false);
            
            var img = msgGo.GetComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            var outline = msgGo.AddComponent<Outline>();
            outline.effectColor = new Color(1, 0.85f, 0.4f); // Gold outline
            outline.effectDistance = new Vector2(2, -2);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(msgGo.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            
            // Add padding for the text so it doesn't touch the outline
            var rt = textGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(10, 5);
            rt.offsetMax = new Vector2(-10, -5);
            
            var fitter = msgGo.AddComponent<ContentSizeFitter>();
fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            
            var layoutElement = msgGo.AddComponent<LayoutElement>();
            layoutElement.minHeight = 40;

            _activeMessages.Add(msgGo);

            yield return new WaitForSeconds(3f);

            _activeMessages.Remove(msgGo);
            Destroy(msgGo);
        }
    }
}
