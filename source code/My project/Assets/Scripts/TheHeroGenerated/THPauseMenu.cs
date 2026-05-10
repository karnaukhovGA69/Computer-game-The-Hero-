using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TheHero.Generated
{
    public class THPauseMenu : MonoBehaviour
    {
        private static THPauseMenu _instance;
        public static THPauseMenu Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THPauseMenu");
                    _instance = go.AddComponent<THPauseMenu>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private GameObject _panel;
        private bool _isPaused = false;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateUI();
        }

        private void CreateUI()
        {
            var canvasGo = new GameObject("PauseCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1100;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasGo.AddComponent<GraphicRaycaster>();

            _panel = new GameObject("PausePanel", typeof(RectTransform), typeof(Image));
            _panel.transform.SetParent(canvasGo.transform, false);
            var rect = _panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.3f, 0.2f);
            rect.anchorMax = new Vector2(0.7f, 0.8f);
            rect.sizeDelta = Vector2.zero;
            var pImg = _panel.GetComponent<Image>();
            pImg.sprite = Resources.Load<Sprite>("Sprites/UI/panel_fantasy_dark");
            pImg.type = Image.Type.Sliced;
            pImg.color = new Color(1, 1, 1, 0.95f);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(_panel.transform, false);
            var t = titleGo.GetComponent<Text>();
            t.text = "ПАУЗА";
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 48;
            t.alignment = TextAnchor.UpperCenter;
            t.color = new Color(1f, 0.84f, 0f);
            var tRect = titleGo.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0, 0.8f);
            tRect.anchorMax = new Vector2(1, 1);
            tRect.sizeDelta = Vector2.zero;

            var content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            content.transform.SetParent(_panel.transform, false);
            var cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.1f, 0.1f);
            cRect.anchorMax = new Vector2(0.9f, 0.8f);
            cRect.sizeDelta = Vector2.zero;
            var layout = content.GetComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 20;
            layout.childControlHeight = false;
            layout.childControlWidth = true;

            CreateButton(content.transform, "Продолжить", Resume);
            CreateButton(content.transform, "Сохранить", Save);
            CreateButton(content.transform, "Загрузить", Load);
            CreateButton(content.transform, "Настройки", OpenSettings);
            CreateButton(content.transform, "Главное меню", MainMenu);
            CreateButton(content.transform, "Выход", Exit);

            _panel.SetActive(false);
        }

        private void CreateButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            var btnGo = new GameObject(text, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false);
            btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(250, 50);
            var img = btnGo.GetComponent<Image>();
            img.sprite = Resources.Load<Sprite>("Sprites/UI/button_fantasy_normal");
            var btn = btnGo.GetComponent<Button>();
            btn.onClick.AddListener(action);
            
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = Resources.Load<Sprite>("Sprites/UI/button_fantasy_hover");
            ss.pressedSprite = Resources.Load<Sprite>("Sprites/UI/button_fantasy_pressed");
            btn.spriteState = ss;

            var textGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(btnGo.transform, false);
            var t = textGo.GetComponent<Text>();
            t.text = text;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            var tRect = textGo.GetComponent<RectTransform>();
            tRect.anchorMin = Vector2.zero;
            tRect.anchorMax = Vector2.one;
            tRect.sizeDelta = Vector2.zero;
        }

        private void Update()
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                if (_isPaused) Resume();
                else Pause();
            }
        }

        public void Pause()
        {
            _isPaused = true;
            _panel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            _isPaused = false;
            _panel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void Save() => THSaveSystem.SaveGame(THManager.Instance.Data);
        private void OpenSettings()
        {
            if (THSettingsController.Instance != null) THSettingsController.Instance.Open();
        }
        private void Load()
{
            var data = THSaveSystem.LoadGame();
            if (data != null)
            {
                THManager.Instance.Data = data;
                Resume();
                THSceneLoader.Instance.ReloadCurrentScene();
            }
        }

        private void MainMenu()
        {
            Resume();
            THSceneLoader.Instance.LoadMainMenu();
        }

        private void Exit() => Application.Quit();
    }
}
