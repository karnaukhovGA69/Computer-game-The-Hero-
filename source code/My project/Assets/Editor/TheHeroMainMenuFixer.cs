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
    public class TheHeroMainMenuFixer
    {
        [MenuItem("The Hero/Fix/Fix Main Menu Screen")]
        public static void FixMainMenuScreen()
        {
            Debug.Log("<b>[TheHeroMainMenuFix] Starting Main Menu Fix...</b>");

            FixBuildSettings();

            if (!File.Exists("Assets/Scenes/MainMenu.unity"))
            {
                Debug.LogError("[TheHeroMainMenuFix] MainMenu.unity not found!");
                return;
            }

            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");

            CleanupScene();
            SetupSystems();
            SetupBackground();
            SetupMainMenuPanel();
            SetupPanels();
            
            // Add Controller
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                var controller = canvas.GetComponent<THMainMenuControllerFixed>() ?? canvas.AddComponent<THMainMenuControllerFixed>();
                controller.MainMenuPanel = canvas.transform.Find("MainMenuPanel")?.gameObject;
                controller.SettingsPanel = canvas.transform.Find("SettingsPanel")?.gameObject;
                controller.HelpPanel = canvas.transform.Find("HelpPanel")?.gameObject;
                
                if (controller.MainMenuPanel != null)
                {
                    var container = controller.MainMenuPanel.transform.Find("ButtonsContainer");
                    if (container != null)
                    {
                        controller.NewGameButton = container.Find("NewGameButton")?.GetComponent<Button>();
                        controller.ContinueButton = container.Find("ContinueButton")?.GetComponent<Button>();
                        controller.SettingsButton = container.Find("SettingsButton")?.GetComponent<Button>();
                        controller.HelpButton = container.Find("HelpButton")?.GetComponent<Button>();
                        controller.ExitButton = container.Find("ExitButton")?.GetComponent<Button>();
                    }
                }
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[TheHeroMainMenuFix] MainMenu ready for testing</color>");
        }

        private static void FixBuildSettings()
        {
            string[] scenePaths = { 
                "Assets/Scenes/MainMenu.unity", 
                "Assets/Scenes/Map.unity", 
                "Assets/Scenes/Combat.unity", 
                "Assets/Scenes/Base.unity" 
            };

            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var path in scenePaths)
            {
                if (File.Exists(path))
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[TheHeroMainMenuFix] Build Settings fixed. MainMenu is build index 0.");
        }

        private static void CleanupScene()
        {
            GameObject canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                List<GameObject> toDestroy = new List<GameObject>();
                foreach (Transform child in canvas.transform)
                {
                    toDestroy.Add(child.gameObject);
                }
                foreach (var go in toDestroy) Object.DestroyImmediate(go);
            }

            // Remove other loose objects
            string[] baseKeywords = { "Base", "Recruit", "Building", "Purchase", "UnitShop", "Barracks", "Archery", "Mage", "UnitPurchase", "Bootstrap" };
            var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);

            foreach (var go in allGos)
            {
                if (go == null) continue;
                if (go.transform.parent != null) continue; // Only root objects here
                if (go.name == "Main Camera" || go.name == "Canvas" || go.name == "EventSystem") continue;

                if (baseKeywords.Any(k => go.name.Contains(k)))
                {
                    Object.DestroyImmediate(go);
                }
            }

            var oldController = Object.FindAnyObjectByType<THMainMenuController>();
            if (oldController != null) Object.DestroyImmediate(oldController.gameObject);

            Debug.Log("[TheHeroMainMenuFix] Removed old/conflicting UI elements from MainMenu scene.");
        }

        private static void SetupSystems()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }
        }

        private static GameObject CreateUIGo(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetupBackground()
        {
            GameObject canvasGo = EnsureCanvas();
            
            GameObject bgGo = CreateUIGo("Background", canvasGo.transform);
            bgGo.transform.SetAsFirstSibling();
            
            var img = bgGo.GetComponent<Image>() ?? bgGo.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/main_menu_fantasy_bg.png");
            img.raycastTarget = false;
            img.color = Color.white;
            
            var rt = bgGo.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            GameObject vigGo = CreateUIGo("VignetteOverlay", canvasGo.transform);
            vigGo.transform.SetSiblingIndex(1);
            
            var vigImg = vigGo.GetComponent<Image>() ?? vigGo.AddComponent<Image>();
            vigImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/vignette_overlay.png");
            vigImg.raycastTarget = false;
            vigImg.color = new Color(0, 0, 0, 0.7f);
            
            var vrt = vigGo.GetComponent<RectTransform>();
            vrt.anchorMin = Vector2.zero;
            vrt.anchorMax = Vector2.one;
            vrt.offsetMin = Vector2.zero;
            vrt.offsetMax = Vector2.zero;

            Debug.Log("[TheHeroMainMenuFix] Fantasy background installed.");
        }

        private static void SetupMainMenuPanel()
        {
            GameObject canvasGo = EnsureCanvas();
            GameObject panelGo = CreateUIGo("MainMenuPanel", canvasGo.transform);
            
            var rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(520, 680);

            var img = panelGo.GetComponent<Image>() ?? panelGo.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/panel_dark_fantasy.png");
            img.type = Image.Type.Sliced;
            img.raycastTarget = true;

            // Title
            GameObject titleGo = CreateUIGo("TitleText", panelGo.transform);
            var tText = titleGo.GetComponent<Text>() ?? titleGo.AddComponent<Text>();
            tText.text = "THE HERO";
            tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tText.fontSize = 64;
            tText.color = new Color(1, 0.85f, 0.2f);
            tText.alignment = TextAnchor.MiddleCenter;
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1);
            trt.anchorMax = new Vector2(1, 1);
            trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -40);
            trt.sizeDelta = new Vector2(0, 80);

            // Subtitle
            GameObject subGo = CreateUIGo("SubtitleText", panelGo.transform);
            var sText = subGo.GetComponent<Text>() ?? subGo.AddComponent<Text>();
            sText.text = "Fantasy Strategy";
            sText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sText.fontSize = 22;
            sText.color = new Color(0.8f, 0.8f, 0.8f);
            sText.alignment = TextAnchor.MiddleCenter;
            var srt = subGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0, 1);
            srt.anchorMax = new Vector2(1, 1);
            srt.pivot = new Vector2(0.5f, 1);
            srt.anchoredPosition = new Vector2(0, -110);
            srt.sizeDelta = new Vector2(0, 30);

            SetupButtons(panelGo);

            // Version
            GameObject verGo = CreateUIGo("VersionText", panelGo.transform);
            var vText = verGo.GetComponent<Text>() ?? verGo.AddComponent<Text>();
            vText.text = "v1.0.0-demo";
            vText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vText.fontSize = 16;
            vText.color = new Color(0.5f, 0.5f, 0.5f);
            vText.alignment = TextAnchor.MiddleCenter;
            var vrt = verGo.GetComponent<RectTransform>();
            vrt.anchorMin = new Vector2(0, 0);
            vrt.anchorMax = new Vector2(1, 0);
            vrt.pivot = new Vector2(0.5f, 0);
            vrt.anchoredPosition = new Vector2(0, 10);
            vrt.sizeDelta = new Vector2(0, 25);

            Debug.Log("[TheHeroMainMenuFix] MainMenuPanel centered and resized.");
        }

        private static void SetupButtons(GameObject panel)
        {
            GameObject container = CreateUIGo("ButtonsContainer", panel.transform);
            
            var crt = container.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.1f, 0.15f);
            crt.anchorMax = new Vector2(0.9f, 0.75f);
            crt.offsetMin = crt.offsetMax = Vector2.zero;

            var layout = container.GetComponent<VerticalLayoutGroup>() ?? container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            string[] names = { "NewGameButton", "ContinueButton", "SettingsButton", "HelpButton", "ExitButton" };
            string[] labels = { "НОВАЯ ИГРА", "ПРОДОЛЖИТЬ", "НАСТРОЙКИ", "ПОМОЩЬ", "ВЫХОД" };

            Sprite normal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/button_fantasy_normal.png");
            Sprite hover = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/button_fantasy_hover.png");
            Sprite pressed = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/button_fantasy_pressed.png");

            for (int i = 0; i < names.Length; i++)
            {
                GameObject btnGo = CreateUIGo(names[i], container.transform);
                var rt = btnGo.GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(360, 58);
                
                var le = btnGo.GetComponent<LayoutElement>() ?? btnGo.AddComponent<LayoutElement>();
                le.minHeight = 58; le.preferredHeight = 58;

                var img = btnGo.GetComponent<Image>() ?? btnGo.AddComponent<Image>();
                img.sprite = normal; img.type = Image.Type.Simple; img.raycastTarget = true;

                var btn = btnGo.GetComponent<Button>() ?? btnGo.AddComponent<Button>();
                btn.transition = Selectable.Transition.SpriteSwap;
                var ss = btn.spriteState;
                ss.highlightedSprite = hover;
                ss.pressedSprite = pressed;
                btn.spriteState = ss;

                GameObject txtGo = CreateUIGo("Text", btnGo.transform);
                var t = txtGo.GetComponent<Text>() ?? txtGo.AddComponent<Text>();
                t.text = labels[i];
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 28;
                t.fontStyle = FontStyle.Bold;
                t.color = new Color(0.95f, 0.9f, 0.7f);
                t.alignment = TextAnchor.MiddleCenter;
                
                var trt = txtGo.GetComponent<RectTransform>();
                trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one; trt.offsetMin = Vector2.zero; trt.offsetMax = new Vector2(0, 2);

                if (btnGo.GetComponent<THFantasyButtonHover>() == null)
                    btnGo.AddComponent<THFantasyButtonHover>();
            }
            Debug.Log("[TheHeroMainMenuFix] Buttons rebuilt.");
        }

        private static void SetupPanels()
        {
            GameObject canvasGo = EnsureCanvas();
            
            GameObject settingsGo = CreateUIGo("SettingsPanel", canvasGo.transform);
            SetupSubPanelBase(settingsGo, "НАСТРОЙКИ", new Vector2(600, 420));
            SetupSettingsControls(settingsGo);

            GameObject helpGo = CreateUIGo("HelpPanel", canvasGo.transform);
            SetupSubPanelBase(helpGo, "ПОМОЩЬ", new Vector2(700, 480));
            
            GameObject contGo = CreateUIGo("Content", helpGo.transform);
            var cText = contGo.GetComponent<Text>() ?? contGo.AddComponent<Text>();
            cText.text = "Цель игры — победить Тёмного Лорда.\n\n" +
                         "Кликайте по карте, чтобы перемещать героя.\n" +
                         "Собирайте ресурсы.\n" +
                         "Побеждайте врагов.\n" +
                         "В замке нанимайте юнитов и улучшайте здания.";
            cText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cText.fontSize = 24; cText.color = Color.white; cText.alignment = TextAnchor.UpperLeft;
            var crt = contGo.GetComponent<RectTransform>();
            crt.anchorMin = Vector2.zero; crt.anchorMax = Vector2.one;
            crt.offsetMin = new Vector2(40, 100); crt.offsetMax = new Vector2(-40, -100);

            Debug.Log("[TheHeroMainMenuFix] Settings and Help panels fixed.");
        }

        private static void SetupSubPanelBase(GameObject panelGo, string title, Vector2 size)
        {
            panelGo.SetActive(false);
            var rt = panelGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = size; rt.anchoredPosition = Vector2.zero;

            var img = panelGo.GetComponent<Image>() ?? panelGo.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/panel_dark_fantasy.png");
            img.type = Image.Type.Sliced; img.color = Color.white;

            GameObject titleGo = CreateUIGo("Title", panelGo.transform);
            var tText = titleGo.GetComponent<Text>() ?? titleGo.AddComponent<Text>();
            tText.text = title; tText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tText.fontSize = 36; tText.color = new Color(1, 0.84f, 0); tText.alignment = TextAnchor.MiddleCenter;
            var trt = titleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0, 1); trt.anchorMax = new Vector2(1, 1); trt.pivot = new Vector2(0.5f, 1);
            trt.anchoredPosition = new Vector2(0, -30); trt.sizeDelta = new Vector2(0, 50);

            GameObject closeGo = CreateUIGo("CloseButton", panelGo.transform);
            var clRt = closeGo.GetComponent<RectTransform>();
            clRt.anchorMin = new Vector2(0.5f, 0); clRt.anchorMax = new Vector2(0.5f, 0); clRt.pivot = new Vector2(0.5f, 0);
            clRt.anchoredPosition = new Vector2(0, 30); clRt.sizeDelta = new Vector2(200, 50);
            var clImg = closeGo.GetComponent<Image>() ?? closeGo.AddComponent<Image>();
            clImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/UI/button_fantasy_normal.png");
            var btn = closeGo.GetComponent<Button>() ?? closeGo.AddComponent<Button>();
            btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(() => panelGo.SetActive(false));
            GameObject clTxtGo = CreateUIGo("Text", closeGo.transform);
            var clt = clTxtGo.GetComponent<Text>() ?? clTxtGo.AddComponent<Text>();
            clt.text = "ЗАКРЫТЬ"; clt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            clt.fontSize = 24; clt.color = Color.white; clt.alignment = TextAnchor.MiddleCenter;
            var cltrt = clTxtGo.GetComponent<RectTransform>();
            cltrt.anchorMin = Vector2.zero; cltrt.anchorMax = Vector2.one; cltrt.offsetMin = cltrt.offsetMax = Vector2.zero;

            if (closeGo.GetComponent<THFantasyButtonHover>() == null)
                closeGo.AddComponent<THFantasyButtonHover>();
        }

        private static void SetupSettingsControls(GameObject panel)
        {
            GameObject volumeGo = CreateUIGo("VolumeRow", panel.transform);
            var vrt = volumeGo.GetComponent<RectTransform>();
            vrt.anchorMin = vrt.anchorMax = new Vector2(0.5f, 0.7f);
            vrt.sizeDelta = new Vector2(400, 40); vrt.anchoredPosition = Vector2.zero;

            GameObject volLabelGo = CreateUIGo("Label", volumeGo.transform);
            var vl = volLabelGo.GetComponent<Text>() ?? volLabelGo.AddComponent<Text>();
            vl.text = "ГРОМКОСТЬ"; vl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vl.fontSize = 20; vl.color = Color.white; vl.alignment = TextAnchor.MiddleLeft;
            var vlrt = volLabelGo.GetComponent<RectTransform>();
            vlrt.anchorMin = Vector2.zero; vlrt.anchorMax = new Vector2(0.4f, 1); vlrt.sizeDelta = Vector2.zero;

            GameObject sliderGo = CreateUIGo("Slider", volumeGo.transform);
            var slrt = sliderGo.GetComponent<RectTransform>();
            slrt.anchorMin = new Vector2(0.45f, 0.5f); slrt.anchorMax = new Vector2(1f, 0.5f);
            slrt.sizeDelta = new Vector2(0, 20);
            var slider = sliderGo.GetComponent<Slider>() ?? sliderGo.AddComponent<Slider>();
            GameObject bg = CreateUIGo("Background", sliderGo.transform);
            var bImg = bg.GetComponent<Image>() ?? bg.AddComponent<Image>();
            bImg.color = Color.gray; bg.GetComponent<RectTransform>().anchorMin = Vector2.zero; bg.GetComponent<RectTransform>().anchorMax = Vector2.one;
            GameObject fillArea = CreateUIGo("Fill Area", sliderGo.transform);
            fillArea.GetComponent<RectTransform>().anchorMin = Vector2.zero; fillArea.GetComponent<RectTransform>().anchorMax = Vector2.one;
            fillArea.GetComponent<RectTransform>().sizeDelta = new Vector2(-10, -10);
            GameObject fill = CreateUIGo("Fill", fillArea.transform);
            var fImg = fill.GetComponent<Image>() ?? fill.AddComponent<Image>();
            fImg.color = new Color(1, 0.84f, 0); fill.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
            slider.fillRect = fill.GetComponent<RectTransform>();

            GameObject soundGo = CreateUIGo("SoundRow", panel.transform);
            var srt = soundGo.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = new Vector2(0.5f, 0.55f);
            srt.sizeDelta = new Vector2(400, 40); srt.anchoredPosition = Vector2.zero;
            GameObject soundLabelGo = CreateUIGo("Label", soundGo.transform);
            var sl = soundLabelGo.GetComponent<Text>() ?? soundLabelGo.AddComponent<Text>();
            sl.text = "ЗВУК ВКЛ/ВЫКЛ"; sl.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sl.fontSize = 20; sl.color = Color.white;
            var slrt2 = soundLabelGo.GetComponent<RectTransform>();
            slrt2.anchorMin = Vector2.zero; slrt2.anchorMax = new Vector2(0.7f, 1); slrt2.sizeDelta = Vector2.zero;
            GameObject toggleGo = CreateUIGo("Toggle", soundGo.transform);
            var trt = toggleGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0.8f, 0.5f); trt.anchorMax = new Vector2(0.9f, 0.5f);
            trt.sizeDelta = new Vector2(30, 30);
            var toggle = toggleGo.GetComponent<Toggle>() ?? toggleGo.AddComponent<Toggle>();
            GameObject tbg = CreateUIGo("Background", toggleGo.transform);
            var tbgImg = tbg.GetComponent<Image>() ?? tbg.AddComponent<Image>();
            tbgImg.color = Color.gray; tbg.GetComponent<RectTransform>().sizeDelta = new Vector2(30, 30);
            GameObject check = CreateUIGo("Checkmark", tbg.transform);
            var cImg = check.GetComponent<Image>() ?? check.AddComponent<Image>();
            cImg.color = new Color(1, 0.84f, 0); check.GetComponent<RectTransform>().sizeDelta = new Vector2(20, 20);
            toggle.graphic = cImg;
        }

        private static GameObject EnsureCanvas()
        {
            GameObject canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas", typeof(RectTransform));
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                canvasGo.AddComponent<GraphicRaycaster>();
            }
            return canvasGo;
        }
    }
}