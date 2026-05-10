using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace TheHero.Generated
{
    public class THMapObjectVisuals : MonoBehaviour
    {
        private SpriteRenderer _sr;
        private Material _mat;
        private THMapObject _mapObj;
        private GameObject _tooltip;
        private static GameObject _tooltipPrefab;

        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _mapObj = GetComponent<THMapObject>();
            if (_sr != null && _sr.sharedMaterial != null)
            {
                _mat = new Material(_sr.sharedMaterial);
                _sr.material = _mat;
            }
        }

        private void OnMouseEnter()
        {
            if (_mat != null) _mat.SetFloat("_IsActive", 1f);
            if (THSingleMapHoverLabel.Instance != null && _mapObj != null)
            {
                THSingleMapHoverLabel.Instance.Show(_mapObj.displayName, transform.position);
            }
        }

        private void OnMouseExit()
        {
            if (_mat != null) _mat.SetFloat("_IsActive", 0f);
            if (THSingleMapHoverLabel.Instance != null)
            {
                THSingleMapHoverLabel.Instance.Hide();
            }
        }

        private void ShowTooltip()
        {
            // Replaced by THSingleMapHoverLabel
        }

        private void HideTooltip()
        {
            if (THSingleMapHoverLabel.Instance != null)
            {
                THSingleMapHoverLabel.Instance.Hide();
            }
        }

        private void CreateTooltipPrefab()
        {
            _tooltipPrefab = new GameObject("Tooltip", typeof(RectTransform), typeof(Canvas));
            _tooltipPrefab.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
            _tooltipPrefab.GetComponent<RectTransform>().sizeDelta = new Vector2(2, 0.5f);
            _tooltipPrefab.transform.localScale = Vector3.one * 0.01f;

            var bg = new GameObject("BG", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(_tooltipPrefab.transform, false);
            bg.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_tooltipPrefab.transform, false);
            var t = textGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 20;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 50);

            _tooltipPrefab.SetActive(false);
        }
        
        public static void SpawnRewardText(Vector3 position, string text, Color color)
        {
            GameObject go = new GameObject("FloatingText", typeof(RectTransform), typeof(Canvas));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            go.transform.position = position + Vector3.up * 0.5f;
            go.transform.localScale = Vector3.one * 0.01f;
            
            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(go.transform, false);
            var t = textGo.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 30;
            t.fontStyle = FontStyle.Bold;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = color;
            t.text = text;
            textGo.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 100);

            go.AddComponent<THFloatingText>();
        }
    }

    public class THFloatingText : MonoBehaviour
    {
        private float _duration = 1.5f;
        private float _speed = 1f;
        private float _timer = 0;
        private Text _text;

        private void Start()
        {
            _text = GetComponentInChildren<Text>();
        }

        private void Update()
        {
            _timer += Time.deltaTime;
            transform.position += Vector3.up * _speed * Time.deltaTime;
            
            if (_text != null)
            {
                Color c = _text.color;
                c.a = 1f - (_timer / _duration);
                _text.color = c;
            }

            if (_timer >= _duration) Destroy(gameObject);
        }
    }
}
