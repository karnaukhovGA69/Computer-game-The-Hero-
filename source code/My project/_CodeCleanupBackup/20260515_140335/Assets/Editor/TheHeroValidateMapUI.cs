using System.IO;
using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TheHero.Editor
{
    public static class TheHeroValidateMapUI
    {
        private const string MapScenePath = "Assets/Scenes/Map.unity";

        [MenuItem("The Hero/Validation/Validate Map UI")]
        public static void ValidateMapUI()
        {
            if (!File.Exists(MapScenePath))
            {
                Debug.LogError("[TheHeroMapUIValidation] FAIL Map.unity missing");
                return;
            }

            EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
            ValidateOpenMapUI(true);
        }

        public static bool ValidateOpenMapUI(bool logFinal)
        {
            bool ok = true;

            ok &= Check(File.Exists(MapScenePath), "Map.unity exists");

            var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(es => es != null && es.gameObject.activeInHierarchy)
                .ToArray();
            ok &= Check(eventSystems.Length == 1, "Exactly one active EventSystem exists");

            Canvas canvas = FindMainMapCanvas();
            ok &= Check(canvas != null, "At least one active Canvas exists");
            ok &= Check(canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay, "Main Map Canvas is Screen Space Overlay");
            ok &= Check(canvas != null && canvas.GetComponent<GraphicRaycaster>() != null && canvas.GetComponent<GraphicRaycaster>().enabled, "Canvas has GraphicRaycaster");

            if (canvas != null)
            {
                Debug.Log("[TheHeroMapUIValidation] PASS Canvas");
            }

            var topHud = FindRect(canvas, "TopHUD");
            ok &= Check(topHud != null && topHud.gameObject.activeInHierarchy, "TopHUD exists");
            ok &= Check(FindText(topHud, "GoldText") != null, "GoldText exists");
            ok &= Check(FindText(topHud, "WoodText") != null, "WoodText exists");
            ok &= Check(FindText(topHud, "StoneText") != null, "StoneText exists");
            ok &= Check(FindText(topHud, "ManaText") != null, "ManaText exists");
            if (topHud != null)
            {
                Debug.Log("[TheHeroMapUIValidation] PASS TopHUD");
            }

            var save = FindButton(topHud, "SaveButton");
            var load = FindButton(topHud, "LoadButton");
            var endTurn = FindButton(topHud, "EndTurnButton");
            var menu = FindButton(topHud, "MenuButton");
            ok &= Check(IsClickable(save), "SaveButton exists and is clickable");
            ok &= Check(IsClickable(load), "LoadButton exists and is clickable");
            ok &= Check(IsClickable(endTurn), "EndTurnButton exists and is clickable");
            ok &= Check(IsClickable(menu), "MenuButton exists and is clickable");
            if (IsClickable(save) && IsClickable(load) && IsClickable(endTurn) && IsClickable(menu))
            {
                Debug.Log("[TheHeroMapUIValidation] PASS Buttons");
            }

            var castle = FindButton(canvas != null ? canvas.transform : null, "CastleButton");
            ok &= Check(IsClickable(castle), "CastleButton exists");
            ok &= Check(castle != null && ButtonSizeWithin(castle, 250f, 100f), "CastleButton size <= 250x100");
            if (IsClickable(castle) && ButtonSizeWithin(castle, 250f, 100f))
            {
                Debug.Log("[TheHeroMapUIValidation] PASS CastleButton");
            }

            var pauseOverlay = FindRect(canvas, "PauseOverlay");
            ok &= Check(pauseOverlay != null && !pauseOverlay.gameObject.activeSelf, "PauseOverlay exists and inactive by default");
            if (pauseOverlay != null && !pauseOverlay.gameObject.activeSelf)
            {
                Debug.Log("[TheHeroMapUIValidation] PASS PauseOverlay");
            }

            var messagePanel = FindRect(canvas, "MessagePanel");
            ok &= Check(messagePanel != null, "MessagePanel exists");
            if (messagePanel != null)
            {
                Debug.Log("[TheHeroMapUIValidation] PASS MessagePanel");
            }

            ok &= Check(canvas != null && canvas.GetComponent<THMapUIRuntime>() != null, "THMapUIRuntime exists");
            ok &= Check(!HasGiantCastleUI(canvas), "No giant Castle UI exists");
            ok &= Check(!MapBuilderDeletesCanvas(), "No map builder script deletes Canvas");

            if (logFinal)
            {
                if (ok) Debug.Log("[TheHeroMapUIValidation] PASS Map UI validation");
                else Debug.LogError("[TheHeroMapUIValidation] FAIL Map UI validation");
            }

            return ok;
        }

        private static bool Check(bool condition, string label)
        {
            if (condition)
            {
                Debug.Log("[TheHeroMapUIValidation] PASS " + label);
                return true;
            }

            Debug.LogError("[TheHeroMapUIValidation] FAIL " + label);
            return false;
        }

        private static Canvas FindMainMapCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(c => c != null && c.gameObject.activeInHierarchy && !c.name.StartsWith("Deprecated_"))
                .ToList();

            return canvases
                .OrderByDescending(c => c.GetComponent<THMapUIRuntime>() != null)
                .ThenByDescending(c => c.name == "MapCanvas")
                .ThenByDescending(c => c.renderMode == RenderMode.ScreenSpaceOverlay)
                .FirstOrDefault();
        }

        private static RectTransform FindRect(Canvas canvas, string name)
        {
            return canvas == null ? null : FindRect(canvas.transform, name);
        }

        private static RectTransform FindRect(Transform root, string name)
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rt => rt.name == name);
        }

        private static Text FindText(RectTransform root, string name)
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == name);
        }

        private static Button FindButton(RectTransform root, string name)
        {
            return root == null ? null : FindButton(root.transform, name);
        }

        private static Button FindButton(Transform root, string name)
        {
            if (root == null) return null;
            return root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
        }

        private static bool IsClickable(Button button)
        {
            if (button == null) return false;
            if (!button.gameObject.activeInHierarchy) return false;
            if (!button.interactable) return false;
            var graphic = button.targetGraphic != null ? button.targetGraphic : button.GetComponent<Graphic>();
            if (graphic == null || !graphic.raycastTarget) return false;
            var rt = button.GetComponent<RectTransform>();
            return rt != null && rt.rect.width > 1f && rt.rect.height > 1f;
        }

        private static bool ButtonSizeWithin(Button button, float maxWidth, float maxHeight)
        {
            if (button == null) return false;
            var rt = button.GetComponent<RectTransform>();
            if (rt == null) return false;
            float width = Mathf.Max(Mathf.Abs(rt.sizeDelta.x), rt.rect.width);
            float height = Mathf.Max(Mathf.Abs(rt.sizeDelta.y), rt.rect.height);
            return width <= maxWidth && height <= maxHeight;
        }

        private static bool HasGiantCastleUI(Canvas mainCanvas)
        {
            foreach (var button in Object.FindObjectsByType<Button>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (button == null || !button.gameObject.activeInHierarchy) continue;
                if (button.name != "CastleButton" && button.name != "SmallCastleButton") continue;

                var canvas = button.GetComponentInParent<Canvas>();
                if (canvas == null || canvas.renderMode == RenderMode.WorldSpace) return true;
                if (button != FindButton(mainCanvas != null ? mainCanvas.transform : null, "CastleButton") &&
                    ButtonSizeWithin(button, 250f, 100f))
                    continue;
                if (!ButtonSizeWithin(button, 250f, 100f)) return true;
            }

            foreach (var text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (text == null || !text.gameObject.activeInHierarchy) continue;
                if (string.IsNullOrEmpty(text.text) || !text.text.Contains("ЗАМОК")) continue;
                var rt = text.GetComponent<RectTransform>();
                if (rt != null && (Mathf.Abs(rt.sizeDelta.x) > 250f || Mathf.Abs(rt.sizeDelta.y) > 100f))
                    return true;
            }

            return false;
        }

        private static bool MapBuilderDeletesCanvas()
        {
            if (!Directory.Exists("Assets/Editor")) return false;

            foreach (string path in Directory.GetFiles("Assets/Editor", "TheHero*.cs", SearchOption.AllDirectories))
            {
                string normalized = path.Replace('\\', '/');
                if (normalized.Contains("/_DisabledOldMapEditors/")) continue;
                string file = Path.GetFileName(normalized);
                if (!IsActiveMapBuilder(file)) continue;

                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (!line.Contains("Destroy")) continue;

                    string window = string.Join("\n", lines.Skip(Mathf.Max(0, i - 4)).Take(6));
                    bool touchesCanvas = window.Contains("Canvas") || window.Contains("canvas");
                    bool deletesGameObject = line.Contains("DestroyImmediate") || line.Contains("DestroyObjectImmediate");
                    if (touchesCanvas && deletesGameObject)
                    {
                        Debug.LogError("[TheHeroMapUIValidation] Builder may delete Canvas: " + normalized + ":" + (i + 1));
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsActiveMapBuilder(string file)
        {
            if (!file.StartsWith("TheHero")) return false;
            if (file.Contains("Validate")) return false;
            if (file == "TheHeroRestoreMapUI.cs") return false;
            if (file == "TheHeroFixMapAndBaseUI.cs") return false;
            if (file == "TheHeroFixGiantCastleUIAndMapOffset.cs") return true;
            if (file == "TheHeroMakeGamePlayable.cs") return true;
            if (file == "TheHeroRestoreMapGameplayObjects.cs") return true;
            if (file == "TheHeroExpandMapAndFixCamera.cs") return true;
            return file.Contains("BuildMap") || file.Contains("MapFrom") || file.Contains("HommStyleMap") || file.Contains("RebuildMap");
        }
    }
}
