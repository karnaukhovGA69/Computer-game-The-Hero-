using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheHero.Generated
{
    public class THSettingsController : MonoBehaviour
    {
        private static THSettingsController _instance;
        public static THSettingsController Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THSettingsController");
                    _instance = go.AddComponent<THSettingsController>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private GameObject _panel;

        public GameObject SettingsPanel;

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

        public void Open()
        {
            if (SettingsPanel != null)
            {
                SettingsPanel.SetActive(true);
            }
            else
            {
                if (_panel == null) CreateUI();
                _panel.SetActive(true);
            }
        }

        public void Close()
        {
            if (SettingsPanel != null) SettingsPanel.SetActive(false);
            if (_panel != null) _panel.SetActive(false);
        }

        private void CreateUI()
        {
            var canvasGo = new GameObject("SettingsCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1200;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("SettingsPanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvasGo.transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.2f, 0.1f);
            rect.anchorMax = new Vector2(0.8f, 0.9f);
            rect.sizeDelta = Vector2.zero;
            var pImg = _panel.GetComponent<Image>();
            pImg.sprite = Resources.Load<Sprite>("Sprites/UI/panel_fantasy_dark");
            pImg.type = Image.Type.Sliced;
            pImg.color = new Color(1, 1, 1, 0.95f);

            CreateText(_panel.transform, "НАСТРОЙКИ", new Vector2(0, 0.9f), 48, new Color(1, 0.84f, 0));
            
            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(_panel.transform, false);
            var cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.1f, 0.2f);
            cRect.anchorMax = new Vector2(0.9f, 0.8f);
            cRect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 30;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            CreateVolumeSlider(content.transform, "Общая громкость", (v) => THAudioManager.Instance.SetMasterVolume(v), () => THAudioManager.Instance.MasterVolume);
            CreateVolumeSlider(content.transform, "Музыка", (v) => THAudioManager.Instance.SetMusicVolume(v), () => THAudioManager.Instance.MusicVolume);
            CreateVolumeSlider(content.transform, "Звуки", (v) => THAudioManager.Instance.SetSfxVolume(v), () => THAudioManager.Instance.SfxVolume);
            
            CreateToggle(content.transform, "Звук включен", (b) => THAudioManager.Instance.SetSoundOn(b), () => THAudioManager.Instance.SoundOn);

            var closeBtnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(_panel.transform, false);
            var bRt = closeBtnGo.GetComponent<RectTransform>();
            bRt.anchorMin = bRt.anchorMax = new Vector2(0.5f, 0.1f);
            bRt.sizeDelta = new Vector2(200, 50);
            var bImg = closeBtnGo.GetComponent<Image>();
            bImg.sprite = Resources.Load<Sprite>("Sprites/UI/button_fantasy_normal");
            
            var btn = closeBtnGo.GetComponent<Button>();
            btn.onClick.AddListener(Close);
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = Resources.Load<Sprite>("Sprites/UI/button_fantasy_hover");
            ss.pressedSprite = Resources.Load<Sprite>("Sprites/UI/button_fantasy_pressed");
            btn.spriteState = ss;
CreateText(closeBtnGo.transform, "ЗАКРЫТЬ", Vector2.zero, 24, Color.white);
        }

        private Text CreateText(Transform parent, string txt, Vector2 anchorY, int size, Color col)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.text = txt;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = col;
            var rt = go.GetComponent<RectTransform>();
            if (anchorY != Vector2.zero)
            {
                rt.anchorMin = new Vector2(0, anchorY.x);
                rt.anchorMax = new Vector2(1, anchorY.y);
            }
            else
            {
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
            }
            rt.sizeDelta = Vector2.zero;
            return t;
        }

        private void CreateVolumeSlider(Transform parent, string label, UnityEngine.Events.UnityAction<float> onValueChange, System.Func<float> getValue)
        {
            var row = new GameObject(label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);

            var lbl = CreateText(row.transform, label, new Vector2(0, 1), 20, Color.white);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.GetComponent<RectTransform>().anchorMax = new Vector2(0.4f, 1);

            var sliderGo = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            sliderGo.transform.SetParent(row.transform, false);
            var sRt = sliderGo.GetComponent<RectTransform>();
            sRt.anchorMin = new Vector2(0.5f, 0.5f);
            sRt.anchorMax = new Vector2(1f, 0.5f);
            sRt.sizeDelta = new Vector2(0, 20);

            var slider = sliderGo.GetComponent<Slider>();
            slider.minValue = 0;
            slider.maxValue = 1;
            slider.value = getValue();
            slider.onValueChanged.AddListener(onValueChange);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(sliderGo.transform, false);
            bg.GetComponent<Image>().color = Color.gray;
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 10);
            
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(sliderGo.transform, false);
            var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(fillArea.transform, false);
            fill.GetComponent<Image>().color = new Color(1, 0.84f, 0);
            
            slider.fillRect = fill.GetComponent<RectTransform>();
        }

        private void CreateToggle(Transform parent, string label, UnityEngine.Events.UnityAction<bool> onValueChange, System.Func<bool> getValue)
        {
            var row = new GameObject(label, typeof(RectTransform));
            row.transform.SetParent(parent, false);
            row.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 60);

            var lbl = CreateText(row.transform, label, new Vector2(0, 1), 20, Color.white);
            lbl.alignment = TextAnchor.MiddleLeft;
            lbl.GetComponent<RectTransform>().anchorMax = new Vector2(0.4f, 1);

            var toggleGo = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
            toggleGo.transform.SetParent(row.transform, false);
            var tRt = toggleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0.5f, 0.5f);
            tRt.anchorMax = new Vector2(0.5f, 0.5f);
            tRt.sizeDelta = new Vector2(30, 30);

            var toggle = toggleGo.GetComponent<Toggle>();
            toggle.isOn = getValue();
            toggle.onValueChanged.AddListener(onValueChange);

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(toggleGo.transform, false);
            bg.GetComponent<Image>().color = Color.gray;
            bg.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);

            var check = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            check.transform.SetParent(bg.transform, false);
            check.GetComponent<Image>().color = new Color(1, 0.84f, 0);
            check.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            toggle.graphic = check.GetComponent<Image>();
        }
    }
}
