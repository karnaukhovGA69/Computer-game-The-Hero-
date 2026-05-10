using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Linq;

namespace TheHero.Generated
{
    public class THFantasyMainMenuPolish : MonoBehaviour
    {
        [Header("Sprites")]
        public Sprite backgroundSprite;
        public Sprite buttonNormal;
        public Sprite buttonHover;
        public Sprite buttonPressed;
        public Sprite panelSprite;
        public Sprite goldDivider;
        public Sprite vignetteSprite;

        [Header("Settings")]
        public Vector2 buttonSize = new Vector2(300, 60);
        public float fadeDuration = 1.0f;
        public AudioClip clickSound;

        private void Start()
        {
            ApplyPolish();
            StartCoroutine(FadeInSequence());
        }

        [ContextMenu("Apply Polish")]
        public void ApplyPolish()
{
            Canvas canvas = GetComponent<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Background
            var bgGo = GameObject.Find("MenuBackground") ?? new GameObject("MenuBackground", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvas.transform);
            bgGo.transform.SetAsFirstSibling();
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = backgroundSprite;
            bgImg.color = Color.white;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;
            bgRt.anchoredPosition = Vector2.zero;

            // Vignette
            var vigGo = GameObject.Find("Vignette") ?? new GameObject("Vignette", typeof(RectTransform), typeof(Image));
            vigGo.transform.SetParent(canvas.transform);
            vigGo.transform.SetSiblingIndex(1);
            var vigImg = vigGo.GetComponent<Image>();
            vigImg.sprite = vignetteSprite;
            vigImg.color = new Color(0, 0, 0, 0.6f);
            vigImg.raycastTarget = false;
            var vigRt = vigGo.GetComponent<RectTransform>();
            vigRt.anchorMin = Vector2.zero;
            vigRt.anchorMax = Vector2.one;
            vigRt.sizeDelta = Vector2.zero;
            vigRt.anchoredPosition = Vector2.zero;

            // 2. Logo & Subtitle
            var logoGo = GameObject.Find("Logo") ?? new GameObject("Logo", typeof(RectTransform), typeof(Text));
            logoGo.transform.SetParent(canvas.transform);
            var logoTxt = logoGo.GetComponent<Text>();
            logoTxt.text = "THE HERO";
            logoTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            logoTxt.fontSize = 72;
            logoTxt.color = new Color(1f, 0.85f, 0.2f);
            logoTxt.alignment = TextAnchor.MiddleCenter;
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 0.85f);
            logoRt.anchorMax = new Vector2(0.5f, 0.85f);
            logoRt.sizeDelta = new Vector2(600, 100);
            logoRt.anchoredPosition = Vector2.zero;
            
            var subGo = GameObject.Find("Subtitle") ?? new GameObject("Subtitle", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(canvas.transform);
            var subTxt = subGo.GetComponent<Text>();
            subTxt.text = "Fantasy Turn-Based Strategy";
            subTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            subTxt.fontSize = 24;
            subTxt.fontStyle = FontStyle.Italic;
            subTxt.color = new Color(0.8f, 0.8f, 0.8f);
            subTxt.alignment = TextAnchor.MiddleCenter;
            var subRt = subGo.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0.5f, 0.78f);
            subRt.anchorMax = new Vector2(0.5f, 0.78f);
            subRt.sizeDelta = new Vector2(600, 50);
            subRt.anchoredPosition = Vector2.zero;

            // 3. Central Panel
            var panelGo = GameObject.Find("CentralPanel") ?? new GameObject("CentralPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(canvas.transform);
            var pImg = panelGo.GetComponent<Image>();
            pImg.sprite = panelSprite;
            pImg.type = Image.Type.Sliced;
            pImg.color = new Color(1, 1, 1, 0.9f);
            var pRt = panelGo.GetComponent<RectTransform>();
            pRt.anchorMin = new Vector2(0.5f, 0.45f);
            pRt.anchorMax = new Vector2(0.5f, 0.45f);
            pRt.sizeDelta = new Vector2(400, 500);
            pRt.anchoredPosition = Vector2.zero;

            // 4. Buttons
            bool hasSave = THSaveSystem.HasSave();
            var allButtons = canvas.GetComponentsInChildren<Button>(true);
foreach (var btn in allButtons)
            {
                var btnImg = btn.GetComponent<Image>();
                if (btnImg == null) btnImg = btn.gameObject.AddComponent<Image>();
                
                btnImg.sprite = buttonNormal;
                btnImg.type = Image.Type.Simple;
                
                btn.transition = Selectable.Transition.SpriteSwap;
                var ss = btn.spriteState;
                ss.highlightedSprite = buttonHover;
                ss.pressedSprite = buttonPressed;
                ss.disabledSprite = buttonNormal;
                btn.spriteState = ss;

                if (btn.name == "Continue") btn.interactable = hasSave;

                var rt = btn.GetComponent<RectTransform>();
                if (btn.name == "New Game" || btn.name == "Continue" || btn.name == "Settings" || btn.name == "Help" || btn.name == "Credits" || btn.name == "Exit")
                {
                    btn.transform.SetParent(panelGo.transform);
                }

                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.color = Color.white;
                    txt.fontStyle = FontStyle.Bold;
                }

                // Hover scale effect
                var effector = btn.GetComponent<THMenuButtonEffector>() ?? btn.gameObject.AddComponent<THMenuButtonEffector>();
                effector.clickSound = clickSound;
            }

            // Specific layout for main menu buttons
            string[] mainBtnNames = { "New Game", "Continue", "Settings", "Help", "Credits", "Exit" };
            float yPos = 180;
            foreach (var name in mainBtnNames)
            {
                var go = GameObject.Find(name);
                if (go != null)
                {
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = buttonSize;
                    rt.anchoredPosition = new Vector2(0, yPos);
                    yPos -= 70;
                }
            }

            // Hide other panels
            string[] panels = { "ConfirmationPanel", "HelpPanel", "SettingsPanel", "CreditsPanel" };
            foreach(var p in panels)
            {
                var go = GameObject.Find(p);
                if (go != null) go.SetActive(false);
            }
        }

        private IEnumerator FadeInSequence()
        {
            CanvasGroup group = GetComponent<CanvasGroup>();
            if (group == null) group = gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0;
            float t = 0;
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                group.alpha = t / fadeDuration;
                yield return null;
            }
            group.alpha = 1;
        }
    }

    public class THMenuButtonEffector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public AudioClip clickSound;
        private Vector3 originalScale;

        private void Start() => originalScale = transform.localScale;

        public void OnPointerEnter(PointerEventData eventData) => transform.localScale = originalScale * 1.1f;
        public void OnPointerExit(PointerEventData eventData) => transform.localScale = originalScale;
        public void OnPointerClick(PointerEventData eventData)
        {
            if (clickSound != null) AudioSource.PlayClipAtPoint(clickSound, Camera.main.transform.position);
            else if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("button_click");
        }
}
}
