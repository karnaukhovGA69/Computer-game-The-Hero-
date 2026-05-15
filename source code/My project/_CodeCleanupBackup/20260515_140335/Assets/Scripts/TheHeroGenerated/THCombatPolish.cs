using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public class THCombatUnitCard : MonoBehaviour
    {
        public Image background;
        public Image portrait;
        public Text nameText;
        public Text countText;
        public Text statsText; // HP, Atk, Def, Init

        public Sprite normalSprite;
        public Sprite selectedSprite;

        public void Setup(THArmyUnit unit, Sprite portraitSprite, Sprite normal, Sprite selected)
        {
            normalSprite = normal;
            selectedSprite = selected;
            background.sprite = normal;
            
            if (portraitSprite) portrait.sprite = portraitSprite;
            nameText.text = unit.name;
            countText.text = $"x{unit.count}";
            statsText.text = $"HP: {unit.hpPerUnit} | A: {unit.attack} | D: {unit.defense} | I: {unit.initiative}";
            
            gameObject.SetActive(unit.count > 0);
        }

        public void SetSelected(bool isSelected)
        {
            background.sprite = isSelected ? selectedSprite : normalSprite;
            transform.localScale = isSelected ? Vector3.one * 1.05f : Vector3.one;
        }
    }

    public class THCombatPolish : MonoBehaviour
    {
        public Sprite combatPanelSprite;
        public Sprite unitCardNormal;
        public Sprite unitCardSelected;
        public Sprite logPanelSprite;
        public Sprite bgGrassland;
        public Sprite bgDarkland;

        private GameObject _playerPanel;
        private GameObject _enemyPanel;
        private Image _bgImage;
        
        private List<THCombatUnitCard> _playerCards = new List<THCombatUnitCard>();
        private List<THCombatUnitCard> _enemyCards = new List<THCombatUnitCard>();

        private void Start()
        {
            ApplyPolish();
            UpdateSquads();
        }

        [ContextMenu("Apply Polish")]
        public void ApplyPolish()
{
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // 1. Background
            var bgGo = GameObject.Find("CombatBackground") ?? new GameObject("CombatBackground", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvas.transform);
            bgGo.transform.SetAsFirstSibling();
            _bgImage = bgGo.GetComponent<Image>();
            _bgImage.sprite = PlayerPrefs.GetInt("Combat_DarkLord", 0) == 1 ? bgDarkland : bgGrassland;
            var bgRt = bgGo.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero;

            // 2. Main Panels
            _playerPanel = CreateArmyPanel(canvas.transform, "PlayerArmy", new Vector2(0, 0.5f), new Vector2(50, 0));
            _enemyPanel = CreateArmyPanel(canvas.transform, "EnemyArmy", new Vector2(1, 0.5f), new Vector2(-50, 0));

            // 3. Log Panel
            var logText = GameObject.Find("Log")?.GetComponent<Text>();
            if (logText != null)
            {
                var logPanel = logText.transform.parent.gameObject;
                if (logPanel.name != "LogPanel")
                {
                    var lpGo = new GameObject("LogPanel", typeof(RectTransform), typeof(Image));
                    lpGo.transform.SetParent(canvas.transform);
                    var lpImg = lpGo.GetComponent<Image>();
                    lpImg.sprite = logPanelSprite;
                    lpImg.type = Image.Type.Sliced;
                    var lpRt = lpGo.GetComponent<RectTransform>();
                    lpRt.anchorMin = new Vector2(0.5f, 0);
                    lpRt.anchorMax = new Vector2(0.5f, 0);
                    lpRt.pivot = new Vector2(0.5f, 0);
                    lpRt.sizeDelta = new Vector2(1000, 150);
                    lpRt.anchoredPosition = new Vector2(0, 20);
                    
                    logText.transform.SetParent(lpGo.transform);
                    var ltRt = logText.GetComponent<RectTransform>();
                    ltRt.anchorMin = Vector2.zero;
                    ltRt.anchorMax = Vector2.one;
                    ltRt.sizeDelta = new Vector2(-40, -20);
                    logText.alignment = TextAnchor.UpperLeft;
                    logText.color = new Color(0.1f, 0.05f, 0); // Ink color
                }
            }

            // 4. Round Text
            var roundText = GameObject.Find("Round")?.GetComponent<Text>();
            if (roundText != null)
            {
                roundText.fontSize = 36;
                roundText.color = new Color(1, 0.9f, 0.5f);
                roundText.fontStyle = FontStyle.Bold;
                var rRt = roundText.GetComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0.5f, 1);
                rRt.anchorMax = new Vector2(0.5f, 1);
                rRt.pivot = new Vector2(0.5f, 1);
                rRt.anchoredPosition = new Vector2(0, -20);
                }

                // 5. Style All Buttons
                var allButtons = canvas.GetComponentsInChildren<Button>(true);
                foreach (var btn in allButtons)
                {
                var img = btn.GetComponent<Image>();
                if (img == null) img = btn.gameObject.AddComponent<Image>();
                img.sprite = combatPanelSprite; // Use panel sprite as base for buttons if no specific one
                img.type = Image.Type.Simple;
                
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.color = Color.white;
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
                }
                }

        private GameObject CreateArmyPanel(Transform parent, string name, Vector2 anchor, Vector2 pos)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent);
            var img = go.GetComponent<Image>();
            img.sprite = combatPanelSprite;
            img.type = Image.Type.Sliced;
            img.color = new Color(1, 1, 1, 0.8f);
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(anchor.x, 0.5f);
            rt.sizeDelta = new Vector2(350, 600);
            rt.anchoredPosition = pos;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10;
            layout.padding = new RectOffset(10, 10, 10, 10);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = false;
            layout.childForceExpandHeight = false;

            return go;
        }

        public void UpdateSquads()
        {
            var controller = Object.FindAnyObjectByType<THCombatController>();
            if (controller == null || controller.State == null) return;

            UpdateArmyList(_playerPanel.transform, controller.State.army, _playerCards);
            UpdateArmyList(_enemyPanel.transform, controller.State.currentEnemyArmy, _enemyCards);
            
            // Highlight current acting squads
            var playerSquad = controller.State.army.OrderByDescending(u => u.initiative).FirstOrDefault(u => u.count > 0);
            var enemySquad = controller.State.currentEnemyArmy.OrderByDescending(u => u.initiative).FirstOrDefault(u => u.count > 0);

            for (int i = 0; i < _playerCards.Count; i++)
                _playerCards[i].SetSelected(playerSquad != null && controller.State.army[i] == playerSquad);
            
            for (int i = 0; i < _enemyCards.Count; i++)
                _enemyCards[i].SetSelected(enemySquad != null && controller.State.currentEnemyArmy[i] == enemySquad);
        }

        private void UpdateArmyList(Transform container, List<THArmyUnit> army, List<THCombatUnitCard> cards)
        {
            // Simple sync
            while (cards.Count < army.Count)
            {
                var card = CreateCard(container);
                cards.Add(card);
            }

            for (int i = 0; i < army.Count; i++)
            {
                var portrait = Resources.Load<Sprite>($"Sprites/Units/{army[i].id}_portrait");
                if (!portrait) portrait = Resources.Load<Sprite>("Sprites/Units/unit_swordsman_portrait");
                cards[i].Setup(army[i], portrait, unitCardNormal, unitCardSelected);
            }
}

        private THCombatUnitCard CreateCard(Transform parent)
        {
            var go = new GameObject("UnitCard", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var card = go.AddComponent<THCombatUnitCard>();
            card.background = go.GetComponent<Image>();
            card.background.type = Image.Type.Sliced;
            
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(320, 100);

            // Portrait
            var portGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
            portGo.transform.SetParent(go.transform, false);
            card.portrait = portGo.GetComponent<Image>();
            var pRt = portGo.GetComponent<RectTransform>();
            pRt.anchorMin = pRt.anchorMax = new Vector2(0, 0.5f);
            pRt.pivot = new Vector2(0, 0.5f);
            pRt.sizeDelta = new Vector2(80, 80);
            pRt.anchoredPosition = new Vector2(10, 0);

            // Name
            var nameGo = new GameObject("Name", typeof(RectTransform), typeof(Text));
            nameGo.transform.SetParent(go.transform, false);
            card.nameText = nameGo.GetComponent<Text>();
            card.nameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            card.nameText.fontSize = 20;
            card.nameText.fontStyle = FontStyle.Bold;
            card.nameText.color = Color.white;
            var nRt = nameGo.GetComponent<RectTransform>();
            nRt.anchorMin = nRt.anchorMax = new Vector2(0, 1);
            nRt.pivot = new Vector2(0, 1);
            nRt.sizeDelta = new Vector2(200, 30);
            nRt.anchoredPosition = new Vector2(100, -10);

            // Count
            var countGo = new GameObject("Count", typeof(RectTransform), typeof(Text));
            countGo.transform.SetParent(go.transform, false);
            card.countText = countGo.GetComponent<Text>();
            card.countText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            card.countText.fontSize = 24;
            card.countText.color = new Color(1, 0.8f, 0);
            card.countText.alignment = TextAnchor.UpperRight;
            var cRt = countGo.GetComponent<RectTransform>();
            cRt.anchorMin = cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(1, 1);
            cRt.sizeDelta = new Vector2(80, 30);
            cRt.anchoredPosition = new Vector2(-10, -10);

            // Stats
            var statsGo = new GameObject("Stats", typeof(RectTransform), typeof(Text));
            statsGo.transform.SetParent(go.transform, false);
            card.statsText = statsGo.GetComponent<Text>();
            card.statsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            card.statsText.fontSize = 14;
            card.statsText.color = new Color(0.8f, 0.8f, 0.8f);
            var sRt = statsGo.GetComponent<RectTransform>();
            sRt.anchorMin = sRt.anchorMax = new Vector2(0, 0);
            sRt.pivot = new Vector2(0, 0);
            sRt.sizeDelta = new Vector2(210, 40);
            sRt.anchoredPosition = new Vector2(100, 10);

            return card;
        }
    }
}
