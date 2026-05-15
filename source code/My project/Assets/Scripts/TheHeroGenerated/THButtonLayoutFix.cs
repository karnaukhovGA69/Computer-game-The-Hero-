using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace TheHero.Generated
{
    public static class THButtonLayoutFix
    {
        private const float Gap = 10f;

        public static void ApplyMainMenu()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            EnsureCanvasScaler(canvas);

            var container = FindRect(canvas, "ButtonsContainer");
            if (container != null)
            {
                Anchor(container, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -45), new Vector2(390, 380));
                var layout = EnsureVerticalLayout(container.gameObject);
                layout.spacing = 14;
                layout.padding = new RectOffset(0, 0, 0, 0);
                layout.childAlignment = TextAnchor.UpperCenter;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            foreach (var name in new[] { "NewGameButton", "ContinueButton", "SettingsButton", "HelpButton", "ExitButton" })
            {
                var button = FindButton(canvas, name);
                if (button == null) continue;
                SetButtonSize(button, 360, 58);
            }

            PositionCloseButton(canvas, "CloseButton");
        }

        public static void ApplyMap()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            EnsureCanvasScaler(canvas);

            if (canvas.GetComponent<THMapUIRuntime>() != null && FindRect(canvas, "TopHUD") != null)
            {
                ApplyRestoredMapHud(canvas);
                return;
            }

            var row = EnsureRect(canvas.transform, "MapButtonRow");
            Anchor(row, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -18), new Vector2(600, 48));

            var layout = EnsureHorizontalLayout(row.gameObject);
            layout.spacing = Gap;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            MoveButtonTo(row.transform, canvas, "SaveButton", 110, 38);
            MoveButtonTo(row.transform, canvas, "LoadButton", 110, 38);
            MoveButtonTo(row.transform, canvas, "EndTurnButton", 170, 38);
            MoveButtonTo(row.transform, canvas, "MenuButton", 110, 38);

            var castle = FindButton(canvas, "CastleButton");
            if (castle != null)
            {
                var rt = castle.GetComponent<RectTransform>();
                rt.SetParent(canvas.transform, false);
                Anchor(rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(150, 50));
            }
        }

        private static void ApplyRestoredMapHud(Canvas canvas)
        {
            var topHud = FindRect(canvas, "TopHUD");
            if (topHud != null)
            {
                Anchor(topHud, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 64));
            }

            SetButtonSizeIfFound(canvas, "SaveButton", 110, 44, topHud);
            SetButtonSizeIfFound(canvas, "LoadButton", 110, 44, topHud);
            SetButtonSizeIfFound(canvas, "EndTurnButton", 150, 44, topHud);
            SetButtonSizeIfFound(canvas, "MenuButton", 110, 44, topHud);

            var castle = FindButton(canvas, "CastleButton");
            if (castle != null)
            {
                var rt = castle.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.SetParent(canvas.transform, false);
                    Anchor(rt, new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0), new Vector2(24, 24), new Vector2(140, 48));
                }
            }
        }

        private static void SetButtonSizeIfFound(Canvas canvas, string name, float width, float height, RectTransform requiredParent)
        {
            var button = FindButton(canvas, name);
            if (button == null) return;
            if (requiredParent != null && button.transform.parent != requiredParent)
                button.transform.SetParent(requiredParent, false);
            SetButtonSize(button, width, height);
        }

        public static void ApplyCombat()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            EnsureCanvasScaler(canvas);

            var actions = FindRect(canvas, "ActionButtons");
            if (actions != null)
            {
                Anchor(actions, new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 64), new Vector2(560, 52));
                var layout = EnsureHorizontalLayout(actions.gameObject);
                layout.spacing = Gap;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                MoveButtonTo(actions.transform, canvas, "AttackButton", 160, 42);
                MoveButtonTo(actions.transform, canvas, "AutoBattleButton", 160, 42);
                MoveButtonTo(actions.transform, canvas, "SkipButton", 160, 42);
            }

            var back = FindButton(canvas, "BackToMapButton");
            if (back != null)
            {
                var rt = back.GetComponent<RectTransform>();
                Anchor(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -18), new Vector2(150, 42));
            }

            var result = FindRect(canvas, "ResultPanel");
            if (result != null)
            {
                var finish = FindButton(canvas, "FinishBattleButton");
                if (finish != null && finish.transform.IsChildOf(result))
                {
                    Anchor(finish.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-125, 34), new Vector2(230, 50));
                }

                var menu = FindButton(canvas, "MainMenuButton");
                if (menu != null && menu.transform.IsChildOf(result))
                {
                    var finishHidden = finish == null || !finish.gameObject.activeInHierarchy;
                    Anchor(menu.GetComponent<RectTransform>(), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), finishHidden ? new Vector2(0, 34) : new Vector2(125, 34), finishHidden ? new Vector2(240, 50) : new Vector2(190, 50));
                }
            }
        }

        public static void ApplyBase()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;
            EnsureCanvasScaler(canvas);

            var topBar = FindRect(canvas, "TopBar");
            if (topBar != null)
            {
                Anchor(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(0, 56));

                var title = topBar.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == "Title");
                if (title != null)
                {
                    Anchor(title.GetComponent<RectTransform>(), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(180, 36));
                    title.fontSize = 24;
                    title.alignment = TextAnchor.MiddleCenter;
                    title.horizontalOverflow = HorizontalWrapMode.Wrap;
                    title.verticalOverflow = VerticalWrapMode.Truncate;
                    title.resizeTextForBestFit = false;
                }
            }

            var resources = FindText(canvas, "ResourcesText");
            if (resources != null)
            {
                var rt = resources.GetComponent<RectTransform>();
                if (topBar != null && resources.transform.IsChildOf(topBar))
                {
                    Anchor(rt, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(760, 38));
                }
                else
                {
                    Anchor(rt, new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(24, -12), new Vector2(760, 38));
                }

                resources.fontSize = 19;
                resources.alignment = TextAnchor.MiddleLeft;
                resources.horizontalOverflow = HorizontalWrapMode.Overflow;
                resources.verticalOverflow = VerticalWrapMode.Truncate;
                resources.resizeTextForBestFit = true;
                resources.resizeTextMinSize = 14;
                resources.resizeTextMaxSize = 19;
            }

            var back = FindButton(canvas, "BackToMapButton");
            if (back != null)
            {
                var rt = back.GetComponent<RectTransform>();
                if (topBar != null && back.transform.IsChildOf(topBar))
                {
                    Anchor(rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-22, 0), new Vector2(150, 38));
                }
                else
                {
                    Anchor(rt, new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-24, -12), new Vector2(150, 38));
                }

                SetButtonSize(back, 150, 38);
            }

            StyleBaseArmy(canvas);

            foreach (var buttonsRoot in canvas.GetComponentsInChildren<Transform>(true).Where(t => t.name == "Buttons"))
            {
                var rt = buttonsRoot.GetComponent<RectTransform>();
                if (rt == null) continue;

                Anchor(rt, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, -18), new Vector2(440, 46));
                var layout = EnsureHorizontalLayout(buttonsRoot.gameObject);
                layout.spacing = 8;
                layout.childAlignment = TextAnchor.MiddleRight;
                layout.childControlWidth = false;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                foreach (var button in buttonsRoot.GetComponentsInChildren<Button>(true))
                {
                    SetButtonSize(button, 132, 38);
                }
            }
        }

        private static void StyleBaseArmy(Canvas canvas)
        {
            var armyPanel = FindRect(canvas, "ArmyPanel");
            if (armyPanel != null)
            {
                Anchor(armyPanel, new Vector2(0.7f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

                var title = armyPanel.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == "Title");
                if (title != null)
                {
                    Anchor(title.GetComponent<RectTransform>(), new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -18), new Vector2(-36, 38));
                    title.fontSize = 24;
                    title.alignment = TextAnchor.MiddleCenter;
                    title.horizontalOverflow = HorizontalWrapMode.Wrap;
                    title.verticalOverflow = VerticalWrapMode.Truncate;
                    title.resizeTextForBestFit = false;
                }
            }

            var armyList = FindRect(canvas, "ArmyListContainer");
            if (armyList != null)
            {
                if (armyPanel != null && !armyList.transform.IsChildOf(armyPanel))
                {
                    armyList.SetParent(armyPanel, false);
                }

                Anchor(armyList, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(0, -18), new Vector2(-44, -150));

                var layout = EnsureVerticalLayout(armyList.gameObject);
                layout.padding = new RectOffset(24, 24, 58, 48);
                layout.spacing = 8;
                layout.childAlignment = TextAnchor.UpperLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = false;
            }

            var summary = FindText(canvas, "Summary");
            if (summary != null)
            {
                if (armyPanel != null && !summary.transform.IsChildOf(armyPanel))
                {
                    summary.transform.SetParent(armyPanel, false);
                }

                Anchor(summary.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 22), new Vector2(-44, 34));
                summary.fontSize = 20;
                summary.alignment = TextAnchor.MiddleLeft;
                summary.horizontalOverflow = HorizontalWrapMode.Overflow;
                summary.verticalOverflow = VerticalWrapMode.Truncate;
                summary.resizeTextForBestFit = false;
            }

            if (armyList == null) return;

            foreach (Transform row in armyList)
            {
                var rowRt = row.GetComponent<RectTransform>();
                if (rowRt != null)
                {
                    rowRt.localScale = Vector3.one;
                    rowRt.anchorMin = new Vector2(0, 0.5f);
                    rowRt.anchorMax = new Vector2(1, 0.5f);
                    rowRt.pivot = new Vector2(0.5f, 0.5f);
                    rowRt.sizeDelta = new Vector2(0, 32);
                    rowRt.anchoredPosition = Vector2.zero;
                }

                var rowLayout = row.GetComponent<LayoutElement>();
                if (rowLayout == null) rowLayout = row.gameObject.AddComponent<LayoutElement>();
                rowLayout.minHeight = 30;
                rowLayout.preferredHeight = 32;
                rowLayout.flexibleWidth = 1;
                rowLayout.flexibleHeight = 0;

                foreach (var text in row.GetComponentsInChildren<Text>(true))
                {
                    Anchor(text.GetComponent<RectTransform>(), new Vector2(0, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
                    text.fontSize = 20;
                    text.alignment = TextAnchor.MiddleLeft;
                    text.horizontalOverflow = HorizontalWrapMode.Overflow;
                    text.verticalOverflow = VerticalWrapMode.Truncate;
                    text.resizeTextForBestFit = false;
                }
            }
        }

        private static void MoveButtonTo(Transform parent, Canvas canvas, string name, float width, float height)
        {
            var button = FindButton(canvas, name);
            if (button == null) return;
            button.transform.SetParent(parent, false);
            SetButtonSize(button, width, height);
        }

        private static Button FindButton(Canvas canvas, string name)
        {
            return canvas.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == name);
        }

        private static Text FindText(Canvas canvas, string name)
        {
            return canvas.GetComponentsInChildren<Text>(true).FirstOrDefault(t => t.name == name);
        }

        private static RectTransform FindRect(Canvas canvas, string name)
        {
            return canvas.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rt => rt.name == name);
        }

        private static RectTransform EnsureRect(Transform parent, string name)
        {
            var found = parent.GetComponentsInChildren<RectTransform>(true).FirstOrDefault(rt => rt.name == name);
            if (found != null) return found;

            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RectTransform>();
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.pivot = pivot;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            rt.localScale = Vector3.one;
        }

        private static void SetButtonSize(Button button, float width, float height)
        {
            var rt = button.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);

            var layout = button.GetComponent<LayoutElement>();
            if (layout == null) layout = button.gameObject.AddComponent<LayoutElement>();
            layout.preferredWidth = width;
            layout.preferredHeight = height;
            layout.minWidth = width;
            layout.minHeight = height;
            layout.flexibleWidth = 0;
            layout.flexibleHeight = 0;
        }

        private static HorizontalLayoutGroup EnsureHorizontalLayout(GameObject go)
        {
            var vertical = go.GetComponent<VerticalLayoutGroup>();
            if (vertical != null) Object.Destroy(vertical);

            var layout = go.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = go.AddComponent<HorizontalLayoutGroup>();
            return layout;
        }

        private static VerticalLayoutGroup EnsureVerticalLayout(GameObject go)
        {
            var horizontal = go.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null) Object.Destroy(horizontal);

            var layout = go.GetComponent<VerticalLayoutGroup>();
            if (layout == null) layout = go.AddComponent<VerticalLayoutGroup>();
            return layout;
        }

        private static void PositionCloseButton(Canvas canvas, string name)
        {
            foreach (var button in canvas.GetComponentsInChildren<Button>(true).Where(b => b.name == name))
            {
                Anchor(button.GetComponent<RectTransform>(), new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-24, 24), new Vector2(180, 50));
            }
        }

        private static void EnsureCanvasScaler(Canvas canvas)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) return;

            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
        }
    }
}
