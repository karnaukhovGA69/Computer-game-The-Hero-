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

        public void LoadMainMenu() => StartCoroutine(LoadRoutine(0));
        public void LoadMap() => StartCoroutine(LoadRoutine(1));
        public void LoadCombat() => StartCoroutine(LoadRoutine(2));
        public void LoadBase() => StartCoroutine(LoadRoutine(3));
        public void ReloadCurrentScene() => StartCoroutine(LoadRoutine(SceneManager.GetActiveScene().buildIndex));

        private IEnumerator LoadRoutine(int index)
        {
            if (SceneManager.GetActiveScene().buildIndex != 0) // Don't save from main menu
            {
                THSaveSystem.SaveGame(THManager.Instance.Data);
            }

            _loadingPanel.SetActive(true);
            _progressBar.value = 0;

            AsyncOperation op = SceneManager.LoadSceneAsync(index);
            while (!op.isDone)
            {
                _progressBar.value = op.progress / 0.9f;
                yield return null;
            }

            yield return new WaitForSeconds(0.5f); // Smooth transition
            _loadingPanel.SetActive(false);
        }
    }
}
