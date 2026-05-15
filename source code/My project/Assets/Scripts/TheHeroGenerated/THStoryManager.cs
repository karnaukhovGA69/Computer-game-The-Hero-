using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

namespace TheHero.Generated
{
    public class THStoryManager : MonoBehaviour
{
        private static THStoryManager _instance;
        public static THStoryManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THStoryManager");
                    _instance = go.AddComponent<THStoryManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("UI References")]
        public GameObject dialogPanel;
        public Image portraitImage;
        public Text titleText;
        public Text contentText;
        public Button continueButton;
        public Button mainMenuButton;

        public void ShowVictoryDialog(string title, string content, string portraitPath = "Sprites/Units/unit_swordsman_portrait")
        {
            StartCoroutine(DoShowVictoryDialog(title, content, portraitPath));
        }

        private IEnumerator DoShowVictoryDialog(string title, string content, string portraitPath)
        {
            yield return DoShowDialog(title, content, portraitPath);

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive(false);
            }
            
            if (mainMenuButton != null)
            {
                mainMenuButton.gameObject.SetActive(true);
                var rt = mainMenuButton.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0);
                    rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    rt.anchoredPosition = new Vector2(0, 30);
                    rt.sizeDelta = new Vector2(260, 60);
                }

                var label = mainMenuButton.GetComponentInChildren<Text>();
                if (label != null) label.text = "\u0412\u042b\u0419\u0422\u0418 \u0412 \u041c\u0415\u041d\u042e";
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(() => {
                    Time.timeScale = 1f;
                    THSceneLoader.Instance.LoadMainMenu();
                });
            }
        }

        public void ShowDialog(string dialogId, string title, string content, string portraitPath = "Sprites/Units/unit_swordsman_portrait")
        {
            var state = THManager.Instance.Data;
            if (state != null && state.shownDialogueIds.Contains(dialogId)) return;

            if (state != null) state.shownDialogueIds.Add(dialogId);

            StartCoroutine(DoShowDialogWrapper(title, content, portraitPath));
        }

        public void ShowNotification(string title, string content, string portraitPath = "Sprites/Units/unit_swordsman_portrait")
        {
            StartCoroutine(DoShowDialogWrapper(title, content, portraitPath));
        }

        private IEnumerator DoShowDialogWrapper(string title, string content, string portraitPath)
        {
            yield return DoShowDialog(title, content, portraitPath);
            if (continueButton != null) continueButton.gameObject.SetActive(true);
            if (mainMenuButton != null) mainMenuButton.gameObject.SetActive(false);
        }

        private IEnumerator DoShowDialog(string title, string content, string portraitPath)
        {
            yield return null;

            if (dialogPanel == null || titleText == null || contentText == null || continueButton == null)
            {
                CreateUI();
            }

            titleText.text = title;
            contentText.text = content;
            
            var sprite = Resources.Load<Sprite>(portraitPath);
            if (sprite != null) portraitImage.sprite = sprite;

            dialogPanel.SetActive(true);
            Time.timeScale = 0f; // Force pause
            
            continueButton.gameObject.SetActive(true);
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(CloseDialog);
            
            Debug.Log($"[StoryManager] Showing dialog: {title}. Game paused.");
        }

        public void CloseDialog()
        {
            Debug.Log("[StoryManager] Closing dialog button clicked.");
            if (dialogPanel != null) dialogPanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void CreateUI()
        {
            var canvasGo = new GameObject("StoryCanvas");
            canvasGo.transform.SetParent(transform);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10000;
            
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            
            canvasGo.AddComponent<GraphicRaycaster>();

            var blocker = new GameObject("StoryBlocker", typeof(RectTransform), typeof(Image));
            blocker.transform.SetParent(canvasGo.transform, false);
            var bRt = blocker.GetComponent<RectTransform>();
            bRt.anchorMin = Vector2.zero;
            bRt.anchorMax = Vector2.one;
            bRt.offsetMin = Vector2.zero;
            bRt.offsetMax = Vector2.zero;
            var blockerImg = blocker.GetComponent<Image>();
            blockerImg.color = new Color(0, 0, 0, 0.75f);
            blockerImg.raycastTarget = true;

            dialogPanel = new GameObject("StoryDialogPanel", typeof(RectTransform), typeof(Image));
            dialogPanel.transform.SetParent(blocker.transform, false);
            var dpRt = dialogPanel.GetComponent<RectTransform>();
            dpRt.anchorMin = new Vector2(0.5f, 0.5f);
            dpRt.anchorMax = new Vector2(0.5f, 0.5f);
            dpRt.sizeDelta = new Vector2(800, 450);
            
            var dpImg = dialogPanel.GetComponent<Image>();
            dpImg.color = new Color(0.1f, 0.1f, 0.1f, 1f); 
            dpImg.raycastTarget = true;
            
            var outline = dialogPanel.AddComponent<Outline>();
            outline.effectColor = new Color(1, 0.85f, 0.4f);
            outline.effectDistance = new Vector2(3, -3);

            var portGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portGo.transform.SetParent(dialogPanel.transform, false);
            portraitImage = portGo.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            var pRt = portGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0, 1);
            pRt.anchorMax = new Vector2(0, 1);
            pRt.pivot = new Vector2(0, 1);
            pRt.anchoredPosition = new Vector2(30, -30);
            pRt.sizeDelta = new Vector2(200, 200);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(dialogPanel.transform, false);
            titleText = titleGo.GetComponent<Text>();
            titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleText.fontSize = 42;
            titleText.fontStyle = FontStyle.Bold;
            titleText.color = new Color(1, 0.9f, 0.2f);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 1);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.pivot = new Vector2(0, 1);
            tRt.anchoredPosition = new Vector2(260, -30);
            tRt.sizeDelta = new Vector2(-290, 70);

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(Text));
            contentGo.transform.SetParent(dialogPanel.transform, false);
            contentText = contentGo.GetComponent<Text>();
            contentText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            contentText.fontSize = 28;
            contentText.color = Color.white;
            contentText.alignment = TextAnchor.UpperLeft;
            var cRt = contentGo.GetComponent<RectTransform>();
            cRt.anchorMin = Vector2.zero;
            cRt.anchorMax = Vector2.one;
            cRt.offsetMin = new Vector2(260, 120);
            cRt.offsetMax = new Vector2(-40, -110);

            // Continue Button
            var btnGo = new GameObject("ContinueButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(dialogPanel.transform, false);
            continueButton = btnGo.GetComponent<Button>();
            var bRtBtn = btnGo.GetComponent<RectTransform>();
            bRtBtn.anchorMin = new Vector2(1, 0);
            bRtBtn.anchorMax = new Vector2(1, 0);
            bRtBtn.pivot = new Vector2(1, 0);
            bRtBtn.anchoredPosition = new Vector2(-30, 30);
            bRtBtn.sizeDelta = new Vector2(220, 60);

            var bImg = btnGo.GetComponent<Image>();
            bImg.color = new Color(0.3f, 0.3f, 0.3f, 1f);
            bImg.raycastTarget = true;
            
            var btnOutline = btnGo.AddComponent<Outline>();
            btnOutline.effectColor = Color.white;
            btnOutline.effectDistance = new Vector2(2, -2);
            
            var bTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            bTextGo.transform.SetParent(btnGo.transform, false);
            var bText = bTextGo.GetComponent<Text>();
            bText.text = "Продолжить";
            bText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bText.fontSize = 24;
            bText.alignment = TextAnchor.MiddleCenter;
            bText.color = Color.white;
            var btRt = bTextGo.GetComponent<RectTransform>();
            btRt.anchorMin = Vector2.zero;
            btRt.anchorMax = Vector2.one;
            btRt.sizeDelta = Vector2.zero;

            // Main Menu Button (Victory only)
            var mmBtnGo = new GameObject("MainMenuButton", typeof(RectTransform), typeof(Image), typeof(Button));
            mmBtnGo.transform.SetParent(dialogPanel.transform, false);
            mainMenuButton = mmBtnGo.GetComponent<Button>();
            var mmRt = mmBtnGo.GetComponent<RectTransform>();
            mmRt.anchorMin = new Vector2(1, 0);
            mmRt.anchorMax = new Vector2(1, 0);
            mmRt.pivot = new Vector2(1, 0);
            mmRt.anchoredPosition = new Vector2(-260, 30);
            mmRt.sizeDelta = new Vector2(220, 60);

            var mmImg = mmBtnGo.GetComponent<Image>();
            mmImg.color = new Color(0.4f, 0.2f, 0.2f, 1f);
            mmImg.raycastTarget = true;
            
            var mmOutline = mmBtnGo.AddComponent<Outline>();
            mmOutline.effectColor = Color.white;
            mmOutline.effectDistance = new Vector2(2, -2);
            
            var mmTextGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            mmTextGo.transform.SetParent(mmBtnGo.transform, false);
            var mmText = mmTextGo.GetComponent<Text>();
            mmText.text = "Главное Меню";
mmText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            mmText.fontSize = 24;
            mmText.alignment = TextAnchor.MiddleCenter;
            mmText.color = Color.white;
            var mmtRt = mmTextGo.GetComponent<RectTransform>();
            mmtRt.anchorMin = Vector2.zero;
            mmtRt.anchorMax = Vector2.one;
            mmtRt.sizeDelta = Vector2.zero;

            mainMenuButton.gameObject.SetActive(false);

            continueButton.onClick.AddListener(CloseDialog);
            
            dialogPanel = blocker;
        }
}
}
