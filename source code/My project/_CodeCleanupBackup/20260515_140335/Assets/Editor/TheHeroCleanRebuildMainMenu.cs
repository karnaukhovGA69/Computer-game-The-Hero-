using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEditor.SceneManagement;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroCleanRebuildMainMenu
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string SpritePath = "Assets/Sprites/UI/";

        [MenuItem("The Hero/Fix/Clean Rebuild Main Menu")]
        public static void CleanRebuild()
        {
            Debug.Log("<b>[TheHeroCleanMenu] Starting full clean rebuild of Main Menu...</b>");

            if (!File.Exists(MainMenuPath))
            {
                Debug.LogError($"[TheHeroCleanMenu] Main Menu scene not found at {MainMenuPath}");
                return;
            }

            EditorSceneManager.OpenScene(MainMenuPath);

            // 1. Cleanup everything UI related
            ClearUI();

            // 2. Setup Build Settings
            FixBuildSettings();

            // 3. Setup Canvas
            GameObject canvasGo = EnsureCanvas();
            SetupCanvasScaler(canvasGo);

            // 4. Setup EventSystem
            EnsureEventSystem();

            // 5. Create Background
            CreateBackground(canvasGo.transform);

            // 6. Create Menu Panel
            GameObject menuPanelGo = CreateMenuPanel(canvasGo.transform);

            // 7. Create Title
            CreateTitle(menuPanelGo.transform);

            // 8. Create Buttons
            CreateButtons(menuPanelGo.transform);

            // 9. Create Settings/Help Panels
            GameObject settingsPanel = CreateSubPanel(canvasGo.transform, "SettingsPanel", "НАСТРОЙКИ");
            GameObject helpPanel = CreateSubPanel(canvasGo.transform, "HelpPanel", "ПОМОЩЬ");
            SetupHelpText(helpPanel);

            // 10. Add Controller
            SetupController(menuPanelGo, settingsPanel, helpPanel);

            // 11. Final Polish (Raycasts)
            SetRaycasts(canvasGo);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[TheHeroCleanMenu] MainMenu ready</color>");
        }

        private static void ClearUI()
        {
            var rootGos = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in rootGos)
            {
                if (go.name == "Main Camera" || go.name == "EventSystem") continue;
                
                // If it's a Canvas or any of the forbidden names, destroy it
                string n = go.name.ToLower();
                if (go.GetComponent<Canvas>() != null || 
                    n.Contains("frame") || n.Contains("panel") || n.Contains("root") || 
                    n.Contains("title") || n.Contains("background") || n.Contains("vignette") ||
                    n.Contains("purchase") || n.Contains("recruit") || n.Contains("base"))
                {
                    Object.DestroyImmediate(go);
                }
            }
            Debug.Log("[TheHeroCleanMenu] Old MainMenu UI cleared");
        }

        private static void FixBuildSettings()
        {
            string[] paths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Map.unity", "Assets/Scenes/Combat.unity", "Assets/Scenes/Base.unity" };
            var scenes = paths.Where(File.Exists).Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[TheHeroCleanMenu] Build Settings fixed");
        }

        private static GameObject EnsureCanvas()
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvasGo;
        }

        private static void SetupCanvasScaler(GameObject canvasGo)
        {
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
        }

        private static void CreateBackground(Transform parent)
        {
            var bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(parent, false);
            var rt = bgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var img = bgGo.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "mm_clean_dark_fantasy_bg.png");
            img.raycastTarget = false;
            Debug.Log("[TheHeroCleanMenu] Clean background created");
        }

        private static GameObject CreateMenuPanel(Transform parent)
        {
            var panelGo = new GameObject("MenuPanel", typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            var rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -40);
            rt.sizeDelta = new Vector2(520, 660);

            var img = panelGo.GetComponent<Image>();
            img.color = new Color32(20, 16, 28, 225); // #14101C alpha 0.88
            img.raycastTarget = false;

            var outline = panelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.6f, 0.2f); // Gold
            outline.effectDistance = new Vector2(2, -2);

            var shadow = panelGo.AddComponent<Shadow>();
            shadow.effectColor = Color.black;
            shadow.effectDistance = new Vector2(4, -4);

            Debug.Log("[TheHeroCleanMenu] Clean MenuPanel created");
            return panelGo;
        }

        private static void CreateTitle(Transform parent)
        {
            var titleGo = new GameObject("TitleText", typeof(RectTransform), typeof(Text), typeof(Shadow));
            titleGo.transform.SetParent(parent, false);
            var rt = titleGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(480, 95);
            rt.anchoredPosition = new Vector2(0, -45);

            var t = titleGo.GetComponent<Text>();
            t.text = "THE HERO";
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 72;
            t.color = new Color(1, 0.85f, 0.2f);
            t.alignment = TextAnchor.MiddleCenter;

            var shadow = titleGo.GetComponent<Shadow>();
            shadow.effectDistance = new Vector2(2, -2);

            var subGo = new GameObject("SubtitleText", typeof(RectTransform), typeof(Text));
            subGo.transform.SetParent(parent, false);
            var srt = subGo.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 1f);
            srt.pivot = new Vector2(0.5f, 1f);
            srt.sizeDelta = new Vector2(480, 45);
            srt.anchoredPosition = new Vector2(0, -120);

            var st = subGo.GetComponent<Text>();
            st.text = "Fantasy Turn-Based Strategy";
            st.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            st.fontSize = 26;
            st.color = new Color(0.8f, 0.8f, 0.8f);
            st.alignment = TextAnchor.MiddleCenter;

            Debug.Log("[TheHeroCleanMenu] Single title created");
        }

        private static void CreateButtons(Transform parent)
        {
            var containerGo = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            containerGo.transform.SetParent(parent, false);
            var rt = containerGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, -55);
            rt.sizeDelta = new Vector2(380, 390);

            var vlg = containerGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;
            vlg.childForceExpandWidth = vlg.childForceExpandHeight = false;

            string[] names = { "NewGameButton", "ContinueButton", "SettingsButton", "HelpButton", "ExitButton" };
            string[] labels = { "НОВАЯ ИГРА", "ПРОДОЛЖИТЬ", "НАСТРОЙКИ", "ПОМОЩЬ", "ВЫХОД" };

            for (int i = 0; i < names.Length; i++)
            {
                CreateMenuButton(containerGo.transform, names[i], labels[i]);
            }

            var verGo = new GameObject("VersionText", typeof(RectTransform), typeof(Text));
            verGo.transform.SetParent(parent, false);
            var vrt = verGo.GetComponent<RectTransform>();
            vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0f);
            vrt.pivot = new Vector2(0.5f, 0f);
            vrt.anchoredPosition = new Vector2(0, 24);
            vrt.sizeDelta = new Vector2(300, 30);
            var vt = verGo.GetComponent<Text>();
            vt.text = "v1.0.0-demo";
            vt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vt.fontSize = 16;
            vt.color = Color.gray;
            vt.alignment = TextAnchor.MiddleCenter;

            Debug.Log("[TheHeroCleanMenu] Buttons recreated");
        }

        private static void CreateMenuButton(Transform parent, string name, string label)
        {
            var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(parent, false);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(360, 58);

            var le = btnGo.GetComponent<LayoutElement>();
            le.preferredWidth = 360;
            le.preferredHeight = 58;

            var img = btnGo.GetComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "mm_button_normal.png");
            img.type = Image.Type.Simple;

            var btn = btnGo.GetComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "mm_button_hover.png");
            ss.pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "mm_button_pressed.png");
            ss.disabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "mm_button_disabled.png");
            btn.spriteState = ss;

            var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(btnGo.transform, false);
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;

            var t = txtGo.GetComponent<Text>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 24;
            t.color = new Color(0.95f, 0.95f, 0.9f);
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 18;
            t.resizeTextMaxSize = 26;

            btnGo.AddComponent<THFantasyButtonHover>();
        }

        private static GameObject CreateSubPanel(Transform parent, string name, string title)
        {
            var panelGo = new GameObject(name, typeof(RectTransform), typeof(Image));
            panelGo.transform.SetParent(parent, false);
            var rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = name.Contains("Settings") ? new Vector2(620, 420) : new Vector2(720, 460);
            rt.anchoredPosition = Vector2.zero;

            var img = panelGo.GetComponent<Image>();
            img.color = new Color32(15, 12, 24, 250);
            var outline = panelGo.AddComponent<Outline>();
            outline.effectColor = new Color(0.8f, 0.6f, 0.2f);
            outline.effectDistance = new Vector2(2, -2);

            var titleGo = new GameObject("Title", typeof(RectTransform), typeof(Text));
            titleGo.transform.SetParent(panelGo.transform, false);
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = trt.anchorMax = new Vector2(0.5f, 1f);
            trt.pivot = new Vector2(0.5f, 1f);
            trt.anchoredPosition = new Vector2(0, -30);
            trt.sizeDelta = new Vector2(400, 50);
            var t = titleGo.GetComponent<Text>();
            t.text = title;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 36;
            t.color = new Color(1, 0.84f, 0);
            t.alignment = TextAnchor.MiddleCenter;

            var closeBtnGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeBtnGo.transform.SetParent(panelGo.transform, false);
            var crt = closeBtnGo.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0f);
            crt.pivot = new Vector2(0.5f, 0f);
            crt.anchoredPosition = new Vector2(0, 30);
            crt.sizeDelta = new Vector2(180, 50);

            var cImg = closeBtnGo.GetComponent<Image>();
            cImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "mm_button_normal.png");
            
            var cTxtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
            cTxtGo.transform.SetParent(closeBtnGo.transform, false);
            var ctrt = cTxtGo.GetComponent<RectTransform>();
            ctrt.anchorMin = Vector2.zero;
            ctrt.anchorMax = Vector2.one;
            ctrt.sizeDelta = Vector2.zero;
            var ct = cTxtGo.GetComponent<Text>();
            ct.text = "ЗАКРЫТЬ";
            ct.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            ct.fontSize = 22;
            ct.color = Color.white;
            ct.alignment = TextAnchor.MiddleCenter;

            closeBtnGo.AddComponent<THFantasyButtonHover>();

            panelGo.SetActive(false);
            return panelGo;
        }

        private static void SetupHelpText(GameObject panel)
        {
            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(Text));
            contentGo.transform.SetParent(panel.transform, false);
            var rt = contentGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(40, 100);
            rt.offsetMax = new Vector2(-40, -100);

            var t = contentGo.GetComponent<Text>();
            t.text = "Цель игры — победить Тёмного Лорда.\n\nКликайте по карте, чтобы перемещать героя.\nСобирайте ресурсы.\nПобеждайте врагов.\nВ замке нанимайте юнитов и улучшайте здания.";
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 22;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperLeft;
        }

        private static void SetupController(GameObject menuPanel, GameObject settingsPanel, GameObject helpPanel)
        {
            var ctrl = menuPanel.AddComponent<THCleanMainMenuController>();
            ctrl.SettingsPanel = settingsPanel;
            ctrl.HelpPanel = helpPanel;

            var container = menuPanel.transform.Find("ButtonsContainer");
            ctrl.NewGameButton = container.Find("NewGameButton").GetComponent<Button>();
            ctrl.ContinueButton = container.Find("ContinueButton").GetComponent<Button>();
            ctrl.SettingsButton = container.Find("SettingsButton").GetComponent<Button>();
            ctrl.HelpButton = container.Find("HelpButton").GetComponent<Button>();
            ctrl.ExitButton = container.Find("ExitButton").GetComponent<Button>();

            ctrl.CloseSettingsButton = settingsPanel.transform.Find("CloseButton").GetComponent<Button>();
            ctrl.CloseHelpButton = helpPanel.transform.Find("CloseButton").GetComponent<Button>();

            Debug.Log("[TheHeroCleanMenu] Button callbacks connected");
        }

        private static void SetRaycasts(GameObject canvasGo)
        {
            foreach (var img in canvasGo.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.GetComponent<Button>() != null)
                {
                    img.raycastTarget = true;
                }
                else
                {
                    img.raycastTarget = false;
                }
            }
        }
    }
}
