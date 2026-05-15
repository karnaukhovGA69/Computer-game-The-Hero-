using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TheHero.Generated
{
    public class THMapUILayoutFix : MonoBehaviour
    {
        private void Start()
        {
            ApplyFix();
        }

        [ContextMenu("Apply UI Fix")]
        public void ApplyFix()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // Ensure CanvasScaler is correct
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            // 1. Setup TopHUD
            GameObject topHud = EnsureObject(canvas.transform, "TopHUD");
            RectTransform topRt = topHud.GetComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1);
            topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot = new Vector2(0.5f, 1);
            topRt.anchoredPosition = Vector2.zero;
            topRt.sizeDelta = new Vector2(0, 100);
            
            if (topHud.GetComponent<Image>() == null)
            {
                var img = topHud.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0.7f);
            }

            // 2. Move existing HUD texts to TopHUD
            string[] hudNames = { "GoldText", "WoodText", "StoneText", "ManaText", "DayText", "WeekText", "HeroText", "LevelText", "MoveText", "ArmyText" };
            float xOffset = 20;
            foreach (var name in hudNames)
            {
                var child = canvas.transform.Find(name);
                if (child != null)
                {
                    child.SetParent(topHud.transform);
                    var rt = child.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
                    rt.pivot = new Vector2(0, 0.5f);
                    rt.anchoredPosition = new Vector2(xOffset, 0);
                    xOffset += rt.sizeDelta.x + 10;
                }
            }

            // 3. Move/Create Buttons to TopHUD (Right side)
            GameObject buttonPanel = EnsureObject(topHud.transform, "ButtonPanel");
            RectTransform bpRt = buttonPanel.GetComponent<RectTransform>();
            bpRt.anchorMin = bpRt.anchorMax = new Vector2(1, 0.5f);
            bpRt.pivot = new Vector2(1, 0.5f);
            bpRt.anchoredPosition = new Vector2(-20, 0);
            bpRt.sizeDelta = new Vector2(800, 60);

            var layout = buttonPanel.GetComponent<HorizontalLayoutGroup>();
            if (layout == null) layout = buttonPanel.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10;
            layout.childAlignment = TextAnchor.MiddleRight;
            layout.childControlWidth = false;

            string[] btnNames = { "SaveButton", "LoadButton", "EndTurnButton", "MenuButton" };
            foreach (var name in btnNames)
            {
                var btn = canvas.transform.Find(name) ?? canvas.transform.Find("HUD_Buttons/" + name);
                if (btn != null)
                {
                    btn.SetParent(buttonPanel.transform);
                }
            }

            // 4. Create CastleButton (Bottom-Left)
            GameObject castleBtnGo = EnsureObject(canvas.transform, "CastleButton");
            RectTransform cRt = castleBtnGo.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(0, 0);
            cRt.pivot = new Vector2(0, 0);
            cRt.anchoredPosition = new Vector2(24, 24);
            cRt.sizeDelta = new Vector2(150, 50);

            if (castleBtnGo.GetComponent<Image>() == null)
            {
                var img = castleBtnGo.AddComponent<Image>();
                img.color = new Color(0.2f, 0.1f, 0, 0.9f);
            }

            Button castleBtn = castleBtnGo.GetComponent<Button>();
            if (castleBtn == null) castleBtn = castleBtnGo.AddComponent<Button>();
            
            castleBtn.onClick.RemoveAllListeners();
            castleBtn.onClick.AddListener(() => SceneManager.LoadScene("Base"));

            GameObject textGo = EnsureObject(castleBtnGo.transform, "Text");
            Text t = textGo.GetComponent<Text>();
            if (t == null)
            {
                t = textGo.AddComponent<Text>();
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontSize = 20;
                t.alignment = TextAnchor.MiddleCenter;
                t.color = Color.white;
                t.text = "Замок";
            }
            RectTransform tRt = textGo.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.sizeDelta = Vector2.zero;

            // Remove/Disable old Base buttons
            var oldBaseBtn = canvas.transform.Find("BaseButton");
            if (oldBaseBtn != null && oldBaseBtn.gameObject != castleBtnGo)
                oldBaseBtn.gameObject.SetActive(false);
                
            var hudButtons = canvas.transform.Find("HUD_Buttons");
            if (hudButtons != null && hudButtons.childCount == 0)
                hudButtons.gameObject.SetActive(false);

            Debug.Log("[THMapUILayoutFix] UI layout fixed.");
        }

        private GameObject EnsureObject(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child.gameObject;
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }
    }
}
