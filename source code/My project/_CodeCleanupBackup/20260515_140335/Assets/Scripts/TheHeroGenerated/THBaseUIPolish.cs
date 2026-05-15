using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public class THBaseUIPolish : MonoBehaviour
    {
        public Sprite backgroundSprite;
        public Sprite panelSprite;
        public Sprite buttonNormal;
        public Sprite buttonHover;
        public Sprite buttonPressed;

        private THBaseController _controller;
        private THBuildingData _selectedBuilding;

        [Header("UI References")]
        public Text buildingNameText;
        public Text buildingLevelText;
        public Text buildingRecruitsText;
        public Text buildingCostText;
        public Image buildingPortrait;

        private void Start()
        {
            _controller = GetComponent<THBaseController>();
            if (_controller == null)
            {
                Debug.Log("[THBaseUIPolish] Deprecated polish skipped: active Base scene uses THBaseRuntime.");
                enabled = false;
                return;
            }

            SetupUI();
            RefreshSelection();
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                BackToMap();
            }
        }

        [ContextMenu("Apply Polish")]
        public void SetupUI()
{
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) return;

            // 1. Background
            var bgGo = GameObject.Find("BaseBackground") ?? new GameObject("BaseBackground", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvas.transform);
            bgGo.transform.SetAsFirstSibling();
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = backgroundSprite;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // 2. Resource Panel (Top)
            var resObj = GameObject.Find("Res");
            RectTransform resPanel = null;
            if (resObj != null)
            {
                if (resObj.GetComponent<Text>() != null)
                {
                    // If Res is the text, create a panel and move the text inside
                    var panelGo = new GameObject("ResourcePanel", typeof(RectTransform), typeof(Image));
                    panelGo.transform.SetParent(canvas.transform, false);
                    resPanel = panelGo.GetComponent<RectTransform>();
                    resObj.transform.SetParent(panelGo.transform, false);
                    _controller.ResourcesText = resObj.GetComponent<Text>();
                }
                else
                {
                    resPanel = resObj.GetComponent<RectTransform>();
                }
            }

            if (resPanel != null)
            {
                resPanel.anchorMin = new Vector2(0, 1);
                resPanel.anchorMax = new Vector2(1, 1);
                resPanel.pivot = new Vector2(0.5f, 1);
                resPanel.anchoredPosition = new Vector2(0, -10);
                resPanel.sizeDelta = new Vector2(-40, 60);
                var img = resPanel.GetComponent<Image>() ?? resPanel.gameObject.AddComponent<Image>();
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(1, 1, 1, 0.8f);
                
                var txt = resPanel.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.alignment = TextAnchor.MiddleCenter;
                    var rt = txt.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.sizeDelta = Vector2.zero;
                    rt.anchoredPosition = Vector2.zero;
                    txt.fontSize = 24;
                }
            }

            // 3. Buildings List (Left)
            var listPanel = GameObject.Find("BuildingsPanel")?.GetComponent<RectTransform>();
            if (listPanel != null)
            {
                listPanel.anchorMin = new Vector2(0, 0.5f);
                listPanel.anchorMax = new Vector2(0, 0.5f);
                listPanel.pivot = new Vector2(0, 0.5f);
                listPanel.anchoredPosition = new Vector2(30, 0);
                listPanel.sizeDelta = new Vector2(300, 600);
                var img = listPanel.GetComponent<Image>() ?? listPanel.gameObject.AddComponent<Image>();
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                
                var layout = listPanel.GetComponent<VerticalLayoutGroup>() ?? listPanel.gameObject.AddComponent<VerticalLayoutGroup>();
                layout.spacing = 10;
                layout.padding = new RectOffset(10, 10, 10, 10);
                layout.childControlHeight = false;
                layout.childForceExpandHeight = false;
                
                // Re-setup Building Buttons
                foreach(Transform child in listPanel)
                {
                    var btn = child.GetComponent<Button>();
                    if (btn != null)
                    {
                        var rt = child.GetComponent<RectTransform>();
                        rt.sizeDelta = new Vector2(280, 80);
                        
                        // Extract building ID from name (e.g., Card_Barracks -> unit_swordsman)
                        string buildingId = child.name switch {
                            "Card_Barracks" => "unit_swordsman",
                            "Card_Range" => "unit_archer",
                            "Card_MageTower" => "unit_mage",
                            _ => ""
                        };
                        
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(() => SelectBuilding(buildingId));
                        
                        var label = child.Find("Label")?.GetComponent<Text>();
                        if (label != null)
                        {
                             label.alignment = TextAnchor.MiddleCenter;
                             label.color = Color.white;
                        }
                    }
                }
            }

            // 4. Building Info (Right)
            var infoPanel = GameObject.Find("Info")?.GetComponent<RectTransform>();
            if (infoPanel != null)
            {
                infoPanel.anchorMin = new Vector2(1, 0.5f);
                infoPanel.anchorMax = new Vector2(1, 0.5f);
                infoPanel.pivot = new Vector2(1, 0.5f);
                infoPanel.anchoredPosition = new Vector2(-30, 0);
                infoPanel.sizeDelta = new Vector2(500, 600);
                var img = infoPanel.GetComponent<Image>() ?? infoPanel.gameObject.AddComponent<Image>();
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                
                // Content of Info Panel
                SetupInfoPanel(infoPanel);
            }

            // 5. Army List (Bottom)
            var armyPanel = GameObject.Find("ArmyList")?.GetComponent<RectTransform>();
            if (armyPanel != null)
            {
                armyPanel.anchorMin = new Vector2(0.5f, 0);
                armyPanel.anchorMax = new Vector2(0.5f, 0);
                armyPanel.pivot = new Vector2(0.5f, 0);
                armyPanel.anchoredPosition = new Vector2(0, 20);
                armyPanel.sizeDelta = new Vector2(1000, 120);
                var img = armyPanel.GetComponent<Image>() ?? armyPanel.gameObject.AddComponent<Image>();
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                
                var txt = armyPanel.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.alignment = TextAnchor.MiddleLeft;
                    var trt = txt.GetComponent<RectTransform>();
                    trt.anchorMin = Vector2.zero;
                    trt.anchorMax = Vector2.one;
                    trt.sizeDelta = new Vector2(-40, -20);
                }
            }

            // 6. Back Button
            var backBtn = GameObject.Find("BackToMapButton") ?? GameObject.Find("Button_Back");
            if (backBtn != null)
            {
                var rt = backBtn.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.pivot = new Vector2(1, 1);
                rt.anchoredPosition = new Vector2(-20, -20);
                rt.sizeDelta = new Vector2(120, 40);
                
                var btn = backBtn.GetComponent<Button>();
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(BackToMap);
                
                var img = backBtn.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = buttonNormal;
                    btn.transition = Selectable.Transition.SpriteSwap;
                    var ss = btn.spriteState;
                    ss.highlightedSprite = buttonHover;
                    ss.pressedSprite = buttonPressed;
                    btn.spriteState = ss;
                }
                }

                // 7. Style ALL Buttons
                var allButtons = canvas.GetComponentsInChildren<Button>(true);
                foreach (var b in allButtons)
                {
                var bImg = b.GetComponent<Image>();
                if (bImg == null) bImg = b.gameObject.AddComponent<Image>();
                if (bImg.sprite == null || bImg.sprite.name == "UISprite")
                {
                    bImg.sprite = buttonNormal;
                    b.transition = Selectable.Transition.SpriteSwap;
                    var ss = b.spriteState;
                    ss.highlightedSprite = buttonHover;
                    ss.pressedSprite = buttonPressed;
                    b.spriteState = ss;
                }
                }
                }

            private void SetupInfoPanel(RectTransform panel)
            {
            // Clear or find components
            buildingNameText = EnsureText(panel, "Name", new Vector2(0, 250), 32, true);
            buildingLevelText = EnsureText(panel, "Level", new Vector2(0, 210), 24, false);
            
            var portGo = panel.Find("Portrait")?.gameObject ?? new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portGo.transform.SetParent(panel, false);
            buildingPortrait = portGo.GetComponent<Image>();
            var pRt = portGo.GetComponent<RectTransform>();
            pRt.anchoredPosition = new Vector2(0, 80);
            pRt.sizeDelta = new Vector2(200, 200);
            buildingPortrait.preserveAspect = true;

            buildingRecruitsText = EnsureText(panel, "Recruits", new Vector2(0, -50), 20, false);
            buildingCostText = EnsureText(panel, "Cost", new Vector2(0, -80), 18, false);

            // Action Buttons
            CreateActionBtn(panel, "Recruit1", "Нанять 1", new Vector2(-130, -180), new Vector2(110, 34), () => _controller.Recruit(_selectedBuilding?.id));
            CreateActionBtn(panel, "RecruitAll", "Нанять всех", new Vector2(0, -180), new Vector2(130, 34), () => _controller.RecruitAll(_selectedBuilding?.id));
            CreateActionBtn(panel, "Upgrade", "Улучшить", new Vector2(140, -180), new Vector2(120, 34), () => _controller.Upgrade(_selectedBuilding?.id));
            }

        private Text EnsureText(Transform parent, string name, Vector2 pos, int size, bool bold)
        {
            var go = parent.Find(name)?.gameObject ?? new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(400, 40);
            return t;
        }

        private void CreateActionBtn(Transform parent, string name, string label, Vector2 pos, Vector2 size, UnityEngine.Events.UnityAction action)
        {
            var go = parent.Find(name)?.gameObject ?? new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            
            var img = go.GetComponent<Image>();
            img.sprite = buttonNormal;
            
            var btn = go.GetComponent<Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(action);
            btn.onClick.AddListener(RefreshSelection);

            btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState;
            ss.highlightedSprite = buttonHover;
            ss.pressedSprite = buttonPressed;
            btn.spriteState = ss;

            var txtGo = go.transform.Find("Text")?.gameObject ?? new GameObject("Text", typeof(RectTransform), typeof(Text));
            txtGo.transform.SetParent(go.transform, false);
            var t = txtGo.GetComponent<Text>();
            t.text = label;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = 18;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.sizeDelta = Vector2.zero;
        }

        public void SelectBuilding(string id)
        {
            if (_controller == null || _controller.State == null) return;
            _selectedBuilding = _controller.State.buildings.FirstOrDefault(b => b.id == id);
            RefreshSelection();
        }

        public void RefreshSelection()
        {
            if (_selectedBuilding == null)
            {
                if (buildingNameText) buildingNameText.text = "Выберите здание";
                if (buildingLevelText) buildingLevelText.text = "";
                if (buildingRecruitsText) buildingRecruitsText.text = "";
                if (buildingCostText) buildingCostText.text = "";
                if (buildingPortrait) buildingPortrait.enabled = false;
                return;
            }

            buildingNameText.text = _selectedBuilding.name;
            buildingLevelText.text = $"Уровень: {_selectedBuilding.level}";
            buildingRecruitsText.text = $"Доступно для найма: {_selectedBuilding.recruitsAvailable}";
            buildingCostText.text = $"Цена: {_selectedBuilding.goldCost} Золота";
            
            buildingPortrait.enabled = true;
            buildingPortrait.sprite = Resources.Load<Sprite>($"Sprites/Units/{_selectedBuilding.id}_portrait");
            
            _controller.UpdateUI();
        }

        public void BackToMap() => SceneManager.LoadScene("Map");
    }
}
