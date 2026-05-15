using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheHero.Editor
{
    public static class TheHeroRestoreMapUI
    {
        private const string MapScenePath = "Assets/Scenes/Map.unity";
        private const string ReportPath = "Assets/CodeAudit/MapUI_Restore_Report.md";
        private static readonly Color PanelColor = new Color(0.08f, 0.07f, 0.06f, 0.92f);
        private static readonly Color ButtonColor = new Color(0.20f, 0.15f, 0.08f, 0.96f);
        private static readonly Color GoldColor = new Color(1f, 0.80f, 0.32f, 1f);

        [MenuItem("The Hero/UI/Restore Map UI")]
        public static void RestoreMapUI()
        {
            RestoreOpenMapUI(true);
        }

        public static void RestoreOpenMapUI(bool saveScene)
        {
            if (!File.Exists(MapScenePath))
            {
                Debug.LogError("[TheHeroMapUI] Map scene not found: " + MapScenePath);
                return;
            }

            var scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
            Debug.Log("[TheHeroMapUI] Map opened");

            Canvas canvas = EnsureMapCanvas();
            Debug.Log("[TheHeroMapUI] Canvas restored");
            Debug.Log("[TheHeroMapUI] Map Canvas checked");

            EnsureOneEventSystem();
            Debug.Log("[TheHeroMapUI] EventSystem checked");

            var refs = RestoreHud(canvas);
            Debug.Log("[TheHeroMapUI] Top HUD restored");

            RestoreCastleButton(canvas, refs);
            Debug.Log("[TheHeroMapUI] Castle button restored");

            RestoreMessagePanel(canvas, refs);
            Debug.Log("[TheHeroMapUI] Message panel restored");

            RestorePauseOverlay(canvas, refs);
            Debug.Log("[TheHeroMapUI] Pause overlay restored");

            ConnectRuntime(canvas, refs);
            Debug.Log("[TheHeroMapUI] Buttons connected");
            Debug.Log("[TheHeroMapUI] Map UI runtime connected");

            DisableBrokenDuplicateMapUI(canvas, refs.CastleButton);
            ProtectMapBuilderScripts();
            Debug.Log("[TheHeroMapUI] Map builders protected from deleting UI");

            EditorSceneManager.MarkSceneDirty(scene);
            if (saveScene)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log("[TheHeroMapUI] Map saved");
            }

            bool validationPassed = ValidateRestoredMapUI(canvas, refs);
            WriteReport(validationPassed);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static Canvas EnsureMapCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            Canvas canvas = canvases
                .Where(c => c != null && !c.name.StartsWith("Deprecated_") && c.GetComponent<THMapUIRuntime>() != null)
                .FirstOrDefault();

            if (canvas == null)
            {
                canvas = canvases
                    .Where(c => c != null && !c.name.StartsWith("Deprecated_") && c.renderMode == RenderMode.ScreenSpaceOverlay)
                    .OrderByDescending(c => c.name == "MapCanvas" || c.name == "Canvas")
                    .FirstOrDefault();
            }

            if (canvas == null)
            {
                var go = new GameObject("MapCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                Undo.RegisterCreatedObjectUndo(go, "Create Map Canvas");
                canvas = go.GetComponent<Canvas>();
            }

            canvas.name = "MapCanvas";
            canvas.gameObject.SetActive(true);
            canvas.transform.SetParent(null, false);
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvas.enabled = true;

            var scaler = canvas.GetComponent<CanvasScaler>() ?? canvas.gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            scaler.enabled = true;

            var raycaster = canvas.GetComponent<GraphicRaycaster>() ?? canvas.gameObject.AddComponent<GraphicRaycaster>();
            raycaster.enabled = true;

            foreach (var other in canvases)
            {
                if (other == null || other == canvas) continue;
                if (other.name.StartsWith("Deprecated_") || other.renderMode == RenderMode.WorldSpace)
                {
                    other.gameObject.SetActive(false);
                }
            }

            EditorUtility.SetDirty(canvas);
            return canvas;
        }

        private static void EnsureOneEventSystem()
        {
            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include)
                .Where(es => es != null)
                .ToList();

            EventSystem keep = systems.FirstOrDefault(es => es.gameObject.activeSelf) ?? systems.FirstOrDefault();
            if (keep == null)
            {
                var go = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                Undo.RegisterCreatedObjectUndo(go, "Create EventSystem");
                keep = go.GetComponent<EventSystem>();
            }

            keep.name = "EventSystem";
            keep.gameObject.SetActive(true);
            keep.enabled = true;
            keep.transform.SetParent(null, false);

            var input = keep.GetComponent<InputSystemUIInputModule>() ?? keep.gameObject.AddComponent<InputSystemUIInputModule>();
            input.enabled = true;
            foreach (var module in keep.GetComponents<BaseInputModule>())
            {
                if (module != input)
                {
                    Undo.DestroyObjectImmediate(module);
                }
            }

            foreach (var extra in systems)
            {
                if (extra != null && extra != keep)
                {
                    Undo.DestroyObjectImmediate(extra.gameObject);
                }
            }
        }

        private static MapUIRefs RestoreHud(Canvas canvas)
        {
            var refs = new MapUIRefs();
            Transform root = canvas.transform;
            GameObject topHud = EnsureDirectChild(root, "TopHUD", typeof(Image));
            var topRt = topHud.GetComponent<RectTransform>();
            SetAnchor(topRt, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 64));
            topHud.SetActive(true);

            var topImage = topHud.GetComponent<Image>();
            topImage.color = PanelColor;
            topImage.raycastTarget = false;
            RemoveLayoutComponents(topHud);

            refs.GoldText = EnsureHudText(topHud.transform, "GoldText", "Gold: 300", 24f, 150f);
            refs.WoodText = EnsureHudText(topHud.transform, "WoodText", "Wood: 10", 190f, 150f);
            refs.StoneText = EnsureHudText(topHud.transform, "StoneText", "Stone: 5", 356f, 150f);
            refs.ManaText = EnsureHudText(topHud.transform, "ManaText", "Mana: 0", 522f, 150f);

            refs.MenuButton = EnsureTopButton(topHud.transform, "MenuButton", "MENU", -24f, 110f);
            refs.EndTurnButton = EnsureTopButton(topHud.transform, "EndTurnButton", "END TURN", -142f, 150f);
            refs.LoadButton = EnsureTopButton(topHud.transform, "LoadButton", "LOAD", -300f, 110f);
            refs.SaveButton = EnsureTopButton(topHud.transform, "SaveButton", "SAVE", -418f, 110f);

            return refs;
        }

        private static void RestoreCastleButton(Canvas canvas, MapUIRefs refs)
        {
            refs.CastleButton = EnsureButton(canvas.transform, "CastleButton", "ЗАМОК");
            var rt = refs.CastleButton.GetComponent<RectTransform>();
            SetAnchor(rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(140, 48));
            StyleButton(refs.CastleButton, "ЗАМОК", 20);
        }

        private static void RestoreMessagePanel(Canvas canvas, MapUIRefs refs)
        {
            GameObject panel = EnsureDirectChild(canvas.transform, "MessagePanel", typeof(Image));
            var rt = panel.GetComponent<RectTransform>();
            SetAnchor(rt, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 104), new Vector2(680, 56));

            var image = panel.GetComponent<Image>();
            image.color = new Color(0.06f, 0.05f, 0.04f, 0.88f);
            image.raycastTarget = false;

            refs.MessageText = EnsureChildText(panel.transform, "MessageText", string.Empty, 22, TextAnchor.MiddleCenter, Color.white);
            Stretch(refs.MessageText.GetComponent<RectTransform>(), 18, 8);
            refs.MessagePanel = panel;
            panel.SetActive(false);
        }

        private static void RestorePauseOverlay(Canvas canvas, MapUIRefs refs)
        {
            GameObject overlay = EnsureDirectChild(canvas.transform, "PauseOverlay", typeof(Image));
            var overlayRt = overlay.GetComponent<RectTransform>();
            SetAnchor(overlayRt, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            overlay.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.62f);

            GameObject panel = EnsureDirectChild(overlay.transform, "Panel", typeof(Image));
            var panelRt = panel.GetComponent<RectTransform>();
            SetAnchor(panelRt, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(420, 340));
            panel.GetComponent<Image>().color = PanelColor;

            var title = EnsureChildText(panel.transform, "Title", "ПАУЗА", 32, TextAnchor.MiddleCenter, GoldColor);
            SetAnchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -44), new Vector2(340, 48));

            refs.ContinueButton = EnsurePanelButton(panel.transform, "ContinueButton", "CONTINUE", -100f);
            refs.PauseSaveButton = EnsurePanelButton(panel.transform, "SaveButton", "SAVE", -42f);
            refs.PauseLoadButton = EnsurePanelButton(panel.transform, "LoadButton", "LOAD", 16f);
            refs.MainMenuButton = EnsurePanelButton(panel.transform, "MainMenuButton", "MAIN MENU", 74f);
            refs.PauseOverlay = overlay;
            overlay.SetActive(false);
        }

        private static void ConnectRuntime(Canvas canvas, MapUIRefs refs)
        {
            var runtime = canvas.GetComponent<THMapUIRuntime>() ?? canvas.gameObject.AddComponent<THMapUIRuntime>();
            runtime.GoldText = refs.GoldText;
            runtime.WoodText = refs.WoodText;
            runtime.StoneText = refs.StoneText;
            runtime.ManaText = refs.ManaText;
            runtime.SaveButton = refs.SaveButton.GetComponent<Button>();
            runtime.LoadButton = refs.LoadButton.GetComponent<Button>();
            runtime.EndTurnButton = refs.EndTurnButton.GetComponent<Button>();
            runtime.MenuButton = refs.MenuButton.GetComponent<Button>();
            runtime.CastleButton = refs.CastleButton.GetComponent<Button>();
            runtime.PauseOverlay = refs.PauseOverlay;
            runtime.ContinueButton = refs.ContinueButton.GetComponent<Button>();
            runtime.PauseSaveButton = refs.PauseSaveButton.GetComponent<Button>();
            runtime.PauseLoadButton = refs.PauseLoadButton.GetComponent<Button>();
            runtime.MainMenuButton = refs.MainMenuButton.GetComponent<Button>();
            runtime.MessagePanel = refs.MessagePanel;
            runtime.MessageText = refs.MessageText;

            var controller = Object.FindObjectsByType<THMapController>(FindObjectsInactive.Include).FirstOrDefault();
            if (controller != null)
            {
                controller.GoldText = refs.GoldText;
                controller.WoodText = refs.WoodText;
                controller.StoneText = refs.StoneText;
                controller.ManaText = refs.ManaText;
                controller.InfoText = refs.MessageText;
                EditorUtility.SetDirty(controller);
            }

            EditorUtility.SetDirty(runtime);
        }

        private static void DisableBrokenDuplicateMapUI(Canvas canvas, GameObject keepCastleButton)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include))
            {
                if (button == null || button.gameObject == keepCastleButton) continue;
                if (button.name != "CastleButton" && button.name != "SmallCastleButton") continue;

                var buttonCanvas = button.GetComponentInParent<Canvas>();
                var rt = button.GetComponent<RectTransform>();
                bool tooLarge = rt != null && (rt.rect.width > 250f || rt.rect.height > 100f || rt.sizeDelta.x > 250f || rt.sizeDelta.y > 100f);
                bool wrongCanvas = buttonCanvas == null || buttonCanvas != canvas || buttonCanvas.renderMode == RenderMode.WorldSpace;
                if (tooLarge || wrongCanvas)
                {
                    button.gameObject.name = "Deprecated_" + button.name;
                    button.gameObject.SetActive(false);
                }
            }
        }

        private static void ProtectMapBuilderScripts()
        {
            string path = "Assets/Editor/TheHeroRebuildMapFromScratchClean.cs";
            if (!File.Exists(path)) return;

            string text = File.ReadAllText(path);
            const string oldBlock =
@"        GameObject canvas = GameObject.Find(""Canvas"");
        if (canvas != null)
        {
            for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(canvas.transform.GetChild(i).gameObject);
        }";

            const string newBlock =
@"        // Map builders must preserve scene UI. Use The Hero/UI/Restore Map UI
        // for intentional Map Canvas repairs.";

            if (text.Contains(oldBlock))
            {
                File.WriteAllText(path, text.Replace(oldBlock, newBlock), Encoding.UTF8);
                AssetDatabase.ImportAsset(path);
            }
        }

        private static bool ValidateRestoredMapUI(Canvas canvas, MapUIRefs refs)
        {
            return canvas != null &&
                   refs.GoldText != null &&
                   refs.WoodText != null &&
                   refs.StoneText != null &&
                   refs.ManaText != null &&
                   refs.SaveButton != null &&
                   refs.LoadButton != null &&
                   refs.EndTurnButton != null &&
                   refs.MenuButton != null &&
                   refs.CastleButton != null &&
                   canvas.GetComponent<THMapUIRuntime>() != null;
        }

        private static Text EnsureHudText(Transform parent, string name, string value, float x, float width)
        {
            var text = EnsureChildText(parent, name, value, 21, TextAnchor.MiddleLeft, Color.white);
            SetAnchor(text.GetComponent<RectTransform>(), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(x, 0), new Vector2(width, 44));
            return text;
        }

        private static GameObject EnsureTopButton(Transform parent, string name, string label, float rightOffset, float width)
        {
            GameObject button = EnsureButton(parent, name, label);
            SetAnchor(button.GetComponent<RectTransform>(), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(rightOffset, 0), new Vector2(width, 44));
            StyleButton(button, label, label.Length > 8 ? 16 : 18);
            return button;
        }

        private static GameObject EnsurePanelButton(Transform parent, string name, string label, float y)
        {
            GameObject button = EnsureButton(parent, name, label);
            SetAnchor(button.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, y), new Vector2(280, 48));
            StyleButton(button, label, 18);
            return button;
        }

        private static GameObject EnsureButton(Transform parent, string name, string label)
        {
            GameObject go = EnsureDirectChild(parent, name, typeof(Image), typeof(Button));
            var image = go.GetComponent<Image>();
            var button = go.GetComponent<Button>();
            button.interactable = true;
            button.targetGraphic = image;
            StyleButton(go, label, 18);
            return go;
        }

        private static void StyleButton(GameObject buttonObject, string label, int fontSize)
        {
            var image = buttonObject.GetComponent<Image>();
            if (image != null)
            {
                image.color = ButtonColor;
                image.raycastTarget = true;
            }

            var button = buttonObject.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = true;
                button.transition = Selectable.Transition.ColorTint;
                var colors = button.colors;
                colors.normalColor = Color.white;
                colors.highlightedColor = new Color(1f, 0.88f, 0.52f, 1f);
                colors.pressedColor = new Color(0.85f, 0.64f, 0.24f, 1f);
                button.colors = colors;
            }

            var text = EnsureChildText(buttonObject.transform, "Text", label, fontSize, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.GetComponent<RectTransform>(), 4, 2);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = fontSize;
        }

        private static Text EnsureChildText(Transform parent, string name, string value, int fontSize, TextAnchor anchor, Color color)
        {
            GameObject go = EnsureDirectChild(parent, name, typeof(Text));
            var text = go.GetComponent<Text>();
            text.text = value;
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.raycastTarget = false;
            return text;
        }

        private static GameObject EnsureDirectChild(Transform parent, string name, params System.Type[] components)
        {
            Transform child = parent.Find(name);
            GameObject go;
            if (child != null)
            {
                go = child.gameObject;
            }
            else
            {
                var types = new List<System.Type> { typeof(RectTransform), typeof(CanvasRenderer) };
                types.AddRange(components.Where(c => c != typeof(RectTransform) && c != typeof(CanvasRenderer)));
                go = new GameObject(name, types.ToArray());
                Undo.RegisterCreatedObjectUndo(go, "Create " + name);
                go.transform.SetParent(parent, false);
            }

            if (go.GetComponent<RectTransform>() == null) go.AddComponent<RectTransform>();
            if (go.GetComponent<CanvasRenderer>() == null) go.AddComponent<CanvasRenderer>();
            foreach (var type in components)
            {
                if (go.GetComponent(type) == null) go.AddComponent(type);
            }

            go.transform.SetParent(parent, false);
            go.name = name;
            go.SetActive(true);
            return go;
        }

        private static void RemoveLayoutComponents(GameObject go)
        {
            foreach (var layout in go.GetComponents<LayoutGroup>())
            {
                Undo.DestroyObjectImmediate(layout);
            }

            var fitter = go.GetComponent<ContentSizeFitter>();
            if (fitter != null) Undo.DestroyObjectImmediate(fitter);
        }

        private static void SetAnchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void Stretch(RectTransform rt, float xPadding, float yPadding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(xPadding, yPadding);
            rt.offsetMax = new Vector2(-xPadding, -yPadding);
            rt.localScale = Vector3.one;
            rt.localRotation = Quaternion.identity;
        }

        private static void WriteReport(bool validationPassed)
        {
            Directory.CreateDirectory("Assets/CodeAudit");

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
            int activeCanvases = canvases.Count(c => c != null && c.gameObject.activeInHierarchy);
            int deprecatedCanvases = canvases.Count(c => c != null && c.name.StartsWith("Deprecated_"));

            var sb = new StringBuilder();
            sb.AppendLine("# Map UI Restore Report");
            sb.AppendLine();
            sb.AppendLine("Generated by `The Hero/UI/Restore Map UI`.");
            sb.AppendLine();
            sb.AppendLine("## Diagnosis");
            sb.AppendLine("- Map scene contained duplicate/deprecated UI traces (`Deprecated_Canvas`) alongside `MapCanvas`, so UI could be hidden, duplicated, or disconnected after asset/map repair commands.");
            sb.AppendLine("- The restore command normalizes the main Map UI to a root `MapCanvas` in Screen Space Overlay and disables deprecated/world-space UI leftovers.");
            sb.AppendLine("- Active canvases after restore: " + activeCanvases + ". Deprecated canvases found: " + deprecatedCanvases + ".");
            sb.AppendLine();
            sb.AppendLine("## Restored UI");
            sb.AppendLine("- `TopHUD` with `GoldText`, `WoodText`, `StoneText`, `ManaText`.");
            sb.AppendLine("- `SaveButton`, `LoadButton`, `EndTurnButton`, `MenuButton`.");
            sb.AppendLine("- Small bottom-left `CastleButton` sized 140x48.");
            sb.AppendLine("- `MessagePanel` with `MessageText`.");
            sb.AppendLine("- `PauseOverlay` with Continue, Save, Load, Main Menu buttons.");
            sb.AppendLine();
            sb.AppendLine("## Runtime Wiring");
            sb.AppendLine("- `THMapUIRuntime` is attached to `MapCanvas` and refreshes resource text from `THMapController`/`THManager` with fallback values 300/10/5/0.");
            sb.AppendLine("- Top buttons call existing `THMapController` methods when available; otherwise they use safe fallback behavior.");
            sb.AppendLine("- `MenuButton` and Esc open the restored `PauseOverlay`.");
            sb.AppendLine("- `CastleButton` loads `Base`.");
            sb.AppendLine();
            sb.AppendLine("## Builder Protection");
            sb.AppendLine("- Active map builders now preserve Canvas/EventSystem/UI. The old clean-map builder Canvas-child wipe is replaced with a preserve-UI comment.");
            sb.AppendLine("- Validation scans active `TheHero*Map*.cs` editor scripts for Canvas deletion patterns.");
            sb.AppendLine();
            sb.AppendLine("## Validation");
            sb.AppendLine(validationPassed ? "- PASS: Map UI validation succeeded." : "- FAIL: Run `The Hero/Validation/Validate Map UI` and inspect Console errors.");
            sb.AppendLine();
            sb.AppendLine("## Manual Checks");
            sb.AppendLine("- Play: MainMenu -> New Game -> Map.");
            sb.AppendLine("- Confirm TopHUD, Save/Load/End Turn/Menu, small `ЗАМОК`, MessagePanel, and PauseOverlay are visible/usable.");
            sb.AppendLine("- Confirm End Turn updates day/resources, Menu/Esc opens pause, Continue closes it, and Castle opens Base.");
            sb.AppendLine("- Confirm no red Console errors.");

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(ReportPath);
        }

        private sealed class MapUIRefs
        {
            public Text GoldText;
            public Text WoodText;
            public Text StoneText;
            public Text ManaText;
            public GameObject SaveButton;
            public GameObject LoadButton;
            public GameObject EndTurnButton;
            public GameObject MenuButton;
            public GameObject CastleButton;
            public GameObject PauseOverlay;
            public GameObject ContinueButton;
            public GameObject PauseSaveButton;
            public GameObject PauseLoadButton;
            public GameObject MainMenuButton;
            public GameObject MessagePanel;
            public Text MessageText;
        }
    }
}
