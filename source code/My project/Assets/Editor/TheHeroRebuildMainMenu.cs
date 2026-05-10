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
    public class TheHeroRebuildMainMenu
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string SpritePath = "Assets/Sprites/UI/";

        [MenuItem("The Hero/Fix/Rebuild Main Menu Properly")]
        public static void RebuildMainMenu()
        {
            Debug.Log("<b>[TheHeroMainMenuRebuild] Starting full rebuild of Main Menu...</b>");

            if (!File.Exists(MainMenuPath))
            {
                Debug.LogError($"[TheHeroMainMenuRebuild] Main Menu scene not found at {MainMenuPath}");
                return;
            }

            EditorSceneManager.OpenScene(MainMenuPath);

            // 1. Cleanup Scene
            CleanupScene();

            // 2. Setup Build Settings
            FixBuildSettings();

            // 3. Ensure EventSystem
            EnsureEventSystem();

            // 4. Setup Canvas
            GameObject canvasGo = EnsureCanvas();
            Canvas canvas = canvasGo.GetComponent<Canvas>();

            // 5. Create Background & Vignette
            CreateBackground(canvasGo.transform);
            CreateVignette(canvasGo.transform);

            // 6. Create MainMenuRoot
            GameObject rootGo = CreateRectGo("MainMenuRoot", canvasGo.transform);
            RectTransform rootRt = rootGo.GetComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            // 7. Create Decorative Frame
            CreateDecorativeFrame(rootGo.transform);

            // 8. Create Title Block
            CreateTitleBlock(rootGo.transform);

            // 9. Create Menu Panel & Buttons
            GameObject menuPanelGo = CreateMenuPanel(rootGo.transform);
            CreateButtons(menuPanelGo.transform);

            // 10. Create Secondary Panels (Settings, Help)
            GameObject settingsPanel = CreateSecondaryPanel(rootGo.transform, "SettingsPanel", "НАСТРОЙКИ");
            SetupSettingsPanel(settingsPanel);
            
            GameObject helpPanel = CreateSecondaryPanel(rootGo.transform, "HelpPanel", "ПОМОЩЬ");
            SetupHelpPanel(helpPanel);

            // 11. Setup Controller
            SetupController(rootGo);

            // 12. Final Polish
            FixRaycasts(canvasGo);

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[TheHeroMainMenuRebuild] MainMenu ready for testing</color>");
        }

        private static void CleanupScene()
        {
            var rootObjects = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var obj in rootObjects)
            {
                if (obj.name == "Main Camera" || obj.name == "EventSystem") continue;
                if (obj.name == "Canvas")
                {
                    // Wipe canvas children
                    for (int i = obj.transform.childCount - 1; i >= 0; i--)
                    {
                        Object.DestroyImmediate(obj.transform.GetChild(i).gameObject);
                    }
                    continue;
                }
                Object.DestroyImmediate(obj);
            }
            Debug.Log("[TheHeroMainMenuRebuild] Scene cleaned up.");
        }

        private static void FixBuildSettings()
        {
            string[] paths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Map.unity", "Assets/Scenes/Combat.unity", "Assets/Scenes/Base.unity" };
            var scenes = new List<EditorBuildSettingsScene>();
            foreach (var p in paths)
            {
                if (File.Exists(p)) scenes.Add(new EditorBuildSettingsScene(p, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[TheHeroMainMenuRebuild] Build Settings verified.");
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
            }
        }

        private static GameObject EnsureCanvas()
        {
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            }

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return canvasGo;
        }

        private static void CreateBackground(Transform parent)
        {
            var bg = CreateRectGo("Background", parent);
            var img = bg.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "main_menu_fantasy_bg.png");
            if (img.sprite == null) img.color = new Color(0.1f, 0.08f, 0.15f);
            img.raycastTarget = false;
            
            var rt = bg.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        private static void CreateVignette(Transform parent)
        {
            var vig = CreateRectGo("VignetteOverlay", parent);
            var img = vig.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "vignette_overlay.png");
            img.color = new Color(0, 0, 0, 0.6f);
            img.raycastTarget = false;

            var rt = vig.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
        }

        private static void CreateDecorativeFrame(Transform parent)
        {
            var frame = CreateRectGo("DecorativeFrame", parent);
            var img = frame.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "panel_gold_frame.png");
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var rt = frame.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000, 800);
            rt.anchoredPosition = new Vector2(0, -50);
        }

        private static void CreateTitleBlock(Transform parent)
        {
            var titleBlock = CreateRectGo("TitleBlock", parent);
            var rt = titleBlock.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(800, 200);
            rt.anchoredPosition = new Vector2(0, -120);

            var mainTitle = CreateTextGo("GameTitleText", titleBlock.transform, "THE HERO", 82, new Color(1, 0.85f, 0.2f), true);
            var mainRt = mainTitle.GetComponent<RectTransform>();
            mainRt.anchorMin = new Vector2(0, 0.5f);
            mainRt.anchorMax = new Vector2(1, 1f);
            mainRt.sizeDelta = Vector2.zero;

            var subtitle = CreateTextGo("SubtitleText", titleBlock.transform, "Fantasy Turn-Based Strategy", 32, new Color(0.8f, 0.8f, 0.8f), false);
            var subRt = subtitle.GetComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0, 0);
            subRt.anchorMax = new Vector2(1, 0.4f);
            subRt.sizeDelta = Vector2.zero;
            
            Debug.Log("[TheHeroMainMenuRebuild] Title block rebuilt.");
        }

        private static GameObject CreateMenuPanel(Transform parent)
        {
            var panel = CreateRectGo("MenuPanel", parent);
            var img = panel.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "panel_fantasy_dark.png");
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;

            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(420, 520);
            rt.anchoredPosition = new Vector2(0, -60);

            return panel;
        }

        private static void CreateButtons(Transform parent)
        {
            var container = CreateRectGo("ButtonsContainer", parent);
            var rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.1f);
            rt.anchorMax = new Vector2(0.95f, 0.9f);
            rt.sizeDelta = Vector2.zero;

            var layout = container.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 14;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            CreateButton(container.transform, "NewGameButton", "НОВАЯ ИГРА");
            CreateButton(container.transform, "ContinueButton", "ПРОДОЛЖИТЬ");
            CreateButton(container.transform, "SettingsButton", "НАСТРОЙКИ");
            CreateButton(container.transform, "HelpButton", "ПОМОЩЬ");
            CreateButton(container.transform, "ExitButton", "ВЫХОД");

            var version = CreateTextGo("VersionText", parent, "v1.0.0-final", 18, new Color(0.5f, 0.5f, 0.5f), false);
            var vRt = version.GetComponent<RectTransform>();
            vRt.anchorMin = new Vector2(0, 0);
            vRt.anchorMax = new Vector2(1, 0.05f);
            vRt.sizeDelta = Vector2.zero;
            
            Debug.Log("[TheHeroMainMenuRebuild] Buttons rebuilt.");
        }

        private static void CreateButton(Transform parent, string name, string label)
        {
            var btnGo = CreateRectGo(name, parent);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 60);

            var img = btnGo.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "button_fantasy_normal.png");
            img.type = Image.Type.Simple;

            var btn = btnGo.AddComponent<Button>();
            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "button_fantasy_hover.png");
            ss.pressedSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "button_fantasy_pressed.png");
            ss.disabledSprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "button_fantasy_disabled.png");
            btn.spriteState = ss;

            var txtGo = CreateTextGo("Text", btnGo.transform, label, 26, new Color(0.95f, 0.95f, 0.85f), true);
            var tRt = txtGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            btnGo.AddComponent<THFantasyButtonHover>();
        }

        private static GameObject CreateSecondaryPanel(Transform parent, string name, string title)
        {
            var panel = CreateRectGo(name, parent);
            panel.SetActive(false);
            
            var img = panel.AddComponent<Image>();
            img.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "panel_fantasy_dark.png");
            img.type = Image.Type.Sliced;
            
            var rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(620, 420);
            rt.anchoredPosition = Vector2.zero;

            var titleGo = CreateTextGo("Title", panel.transform, title, 36, new Color(1, 0.84f, 0), true);
            var tRt = titleGo.GetComponent<RectTransform>();
            tRt.anchorMin = new Vector2(0, 0.85f);
            tRt.anchorMax = new Vector2(1, 1);
            tRt.sizeDelta = Vector2.zero;

            var closeBtn = CreateRectGo("CloseButton", panel.transform);
            var cbRt = closeBtn.GetComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.5f, 0);
            cbRt.anchorMax = new Vector2(0.5f, 0);
            cbRt.sizeDelta = new Vector2(180, 44);
            cbRt.anchoredPosition = new Vector2(0, 30);

            var cbImg = closeBtn.AddComponent<Image>();
            cbImg.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath + "button_fantasy_normal.png");
            
            var btn = closeBtn.AddComponent<Button>();
            btn.onClick.AddListener(() => panel.SetActive(false));

            var cbTxt = CreateTextGo("Text", closeBtn.transform, "ЗАКРЫТЬ", 22, Color.white, false);
            var cbtRt = cbTxt.GetComponent<RectTransform>();
            cbtRt.anchorMin = Vector2.zero;
            cbtRt.anchorMax = Vector2.one;
            cbtRt.sizeDelta = Vector2.zero;

            closeBtn.AddComponent<THFantasyButtonHover>();

            return panel;
        }

        private static void SetupSettingsPanel(GameObject panel)
        {
            // Placeholder for controls
            var content = CreateRectGo("Content", panel.transform);
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.25f);
            rt.anchorMax = new Vector2(0.9f, 0.8f);
            rt.sizeDelta = Vector2.zero;

            var txt = CreateTextGo("Placeholder", content.transform, "Настройки громкости и звука", 24, Color.white, false);
        }

        private static void SetupHelpPanel(GameObject panel)
        {
            var content = CreateRectGo("Content", panel.transform);
            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.1f, 0.25f);
            rt.anchorMax = new Vector2(0.9f, 0.8f);
            rt.sizeDelta = Vector2.zero;

            var txt = CreateTextGo("HelpText", content.transform, 
                "Цель игры — победить Тёмного Лорда.\n\n" +
                "Кликайте по карте, чтобы перемещать героя.\n" +
                "Собирайте ресурсы и побеждайте врагов.\n" +
                "В замке нанимайте юнитов и улучшайте здания.", 
                22, Color.white, false);
            txt.GetComponent<Text>().alignment = TextAnchor.UpperLeft;
        }

        private static void SetupController(GameObject root)
        {
            var canvasGo = GameObject.Find("Canvas");
            var ctrl = canvasGo.GetComponent<THMainMenuControllerFixed>() ?? canvasGo.AddComponent<THMainMenuControllerFixed>();
            
            ctrl.MainMenuPanel = root.transform.Find("MenuPanel")?.gameObject;
            ctrl.SettingsPanel = root.transform.Find("SettingsPanel")?.gameObject;
            ctrl.HelpPanel = root.transform.Find("HelpPanel")?.gameObject;

            if (ctrl.MainMenuPanel != null)
            {
                var cont = ctrl.MainMenuPanel.transform.Find("ButtonsContainer");
                if (cont != null)
                {
                    ctrl.NewGameButton = cont.Find("NewGameButton")?.GetComponent<Button>();
                    ctrl.ContinueButton = cont.Find("ContinueButton")?.GetComponent<Button>();
                    ctrl.SettingsButton = cont.Find("SettingsButton")?.GetComponent<Button>();
                    ctrl.HelpButton = cont.Find("HelpButton")?.GetComponent<Button>();
                    ctrl.ExitButton = cont.Find("ExitButton")?.GetComponent<Button>();
                }
            }
            Debug.Log("[TheHeroMainMenuRebuild] Controller configured.");
        }

        private static void FixRaycasts(GameObject canvas)
        {
            var images = canvas.GetComponentsInChildren<Image>(true);
            foreach (var img in images)
            {
                if (img.gameObject.GetComponent<Button>() != null)
                {
                    img.raycastTarget = true;
                }
                else
                {
                    // Only keep raycast for background of panels to block clicks through them if they are full screen or large
                    if (img.gameObject.name.Contains("Panel") || img.gameObject.name.Contains("Root"))
                        img.raycastTarget = true;
                    else
                        img.raycastTarget = false;
                }
            }
            Debug.Log("[TheHeroMainMenuRebuild] Raycasts fixed.");
        }

        private static GameObject CreateRectGo(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static GameObject CreateTextGo(string name, Transform parent, string content, int size, Color col, bool bold)
        {
            var go = CreateRectGo(name, parent);
            var txt = go.AddComponent<Text>();
            txt.text = content;
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            txt.fontSize = size;
            txt.color = col;
            txt.alignment = TextAnchor.MiddleCenter;
            if (bold) txt.fontStyle = FontStyle.Bold;
            
            if (bold)
            {
                var shadow = go.AddComponent<Shadow>();
                shadow.effectColor = new Color(0, 0, 0, 0.5f);
                shadow.effectDistance = new Vector2(2, -2);
            }

            return go;
        }
    }
}
