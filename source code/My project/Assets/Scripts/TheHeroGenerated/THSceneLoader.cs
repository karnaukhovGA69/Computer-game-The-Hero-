using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheHero.Generated
{
    public class THSceneLoader : MonoBehaviour
    {
        private static THSceneLoader _instance;
        public static THSceneLoader Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THSceneLoader");
                    _instance = go.AddComponent<THSceneLoader>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        private GameObject _loadingPanel;
        private Slider _progressBar;
        private Text _loadingText;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            CreateLoadingPanel();
        }

        private void CreateLoadingPanel()
        {
            var canvasGo = new GameObject("LoadingCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

            _loadingPanel = new GameObject("LoadingPanel", typeof(RectTransform), typeof(Image));
            _loadingPanel.transform.SetParent(canvasGo.transform, false);
            var rect = _loadingPanel.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            _loadingPanel.GetComponent<Image>().color = Color.black;

            var textGo = new GameObject("LoadingText", typeof(RectTransform), typeof(Text));
            textGo.transform.SetParent(_loadingPanel.transform, false);
            _loadingText = textGo.GetComponent<Text>();
            _loadingText.text = "Загрузка...";
            _loadingText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _loadingText.fontSize = 40;
            _loadingText.alignment = TextAnchor.MiddleCenter;
            _loadingText.color = Color.white;

            var progressGo = new GameObject("ProgressBar", typeof(RectTransform), typeof(Image));
            progressGo.transform.SetParent(_loadingPanel.transform, false);
            var pRect = progressGo.GetComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.2f, 0.2f);
            pRect.anchorMax = new Vector2(0.8f, 0.25f);
            pRect.sizeDelta = Vector2.zero;
            progressGo.GetComponent<Image>().color = Color.gray;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(progressGo.transform, false);
            var fRect = fillGo.GetComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero;
            fRect.anchorMax = new Vector2(0, 1);
            fRect.sizeDelta = Vector2.zero;
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.color = new Color(1f, 0.84f, 0f); // Gold

            _progressBar = progressGo.AddComponent<Slider>();
            _progressBar.interactable = false;
            _progressBar.targetGraphic = fillImg;
            _progressBar.fillRect = fRect;
            _progressBar.minValue = 0;
            _progressBar.maxValue = 1;

            _loadingPanel.SetActive(false);
        }

        public const string MainMenuSceneName = "MainMenu";
        public const string MapSceneName      = "Map";
        public const string CombatSceneName   = "Combat";
        public const string BaseSceneName     = "Base";

        public void LoadMainMenu() => StartCoroutine(LoadRoutine(MainMenuSceneName));
        public void LoadMap()      => StartCoroutine(LoadRoutine(MapSceneName));
        public void LoadCombat()   => StartCoroutine(LoadRoutine(CombatSceneName));
        public void LoadBase()     => StartCoroutine(LoadRoutine(BaseSceneName));
        public void ReloadCurrentScene() => StartCoroutine(LoadRoutine(SceneManager.GetActiveScene().name));

        private IEnumerator LoadRoutine(string sceneName)
        {
            // Scene transitions must not autosave. Save policy is centralized in
            // THSavePolicy: manual Save, new week, battle finish, and base actions.
            var hoverLabel = Object.FindAnyObjectByType<THSingleMapHoverLabel>();
            if (hoverLabel != null) hoverLabel.Hide();

            if (string.IsNullOrEmpty(sceneName))
            {
                Debug.LogError("[TheHeroSceneLoader] Empty scene name; aborting load.");
                yield break;
            }

            if (_loadingPanel != null) _loadingPanel.SetActive(true);
            if (_progressBar != null) _progressBar.value = 0;

            AsyncOperation op = null;
            try { op = SceneManager.LoadSceneAsync(sceneName); }
            catch (System.Exception ex)
            {
                Debug.LogError("[TheHeroSceneLoader] LoadSceneAsync threw for '" + sceneName + "': " + ex.Message);
            }

            if (op == null)
            {
                Debug.LogError("[TheHeroSceneLoader] Scene not available in Build Settings / active Build Profile: '" + sceneName + "'. Open File → Build Profiles and add Assets/Scenes/" + sceneName + ".unity.");
                if (_loadingPanel != null) _loadingPanel.SetActive(false);
                yield break;
            }

            while (!op.isDone)
            {
                if (_progressBar != null) _progressBar.value = op.progress / 0.9f;
                yield return null;
            }

            yield return new WaitForSeconds(0.5f); // Smooth transition
            if (_loadingPanel != null) _loadingPanel.SetActive(false);
        }
    }
}
