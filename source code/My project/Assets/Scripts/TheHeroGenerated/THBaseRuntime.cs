using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public class THBaseRuntime : MonoBehaviour
    {
        public static THBaseRuntime Instance { get; private set; }

        [Header("Top Bar")]
        public Text resourcesText;
        public Button backToMapButton;

        [Header("Containers")]
        public Transform buildingsContainer;
        public Transform armyContainer;

        [Header("Prefabs/Templates")]
        public GameObject buildingCardTemplate;
        public GameObject armyRowTemplate;

        [Header("Status")]
        public Text armySummaryText;

        private THGameState State => THManager.Instance.Data;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            if (backToMapButton != null)
            {
                backToMapButton.onClick.RemoveAllListeners();
                backToMapButton.onClick.AddListener(BackToMap);
            }

            RefreshUI();
            THButtonLayoutFix.ApplyBase();
            
            if (THAudioManager.Instance != null) THAudioManager.Instance.PlayMusic("Base");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                BackToMap();
            }
        }

        public void RefreshUI()
        {
            if (State == null) return;

            // Resources
            if (resourcesText != null)
            {
                resourcesText.text = $"\u0417\u043e\u043b\u043e\u0442\u043e: {State.gold}   \u0414\u0435\u0440\u0435\u0432\u043e: {State.wood}   \u041a\u0430\u043c\u0435\u043d\u044c: {State.stone}   \u041c\u0430\u043d\u0430: {State.mana}";
            }

            // Buildings
            UpdateBuildingsUI();

            // Army
            UpdateArmyUI();

            var swordsman = State.army.FirstOrDefault(u => u.id == "unit_swordsman");
            int s = swordsman != null ? swordsman.count : 0;
            var archer = State.army.FirstOrDefault(u => u.id == "unit_archer");
            int a = archer != null ? archer.count : 0;
            var mage = State.army.FirstOrDefault(u => u.id == "unit_mage");
            int m = mage != null ? mage.count : 0;
            Debug.Log($"[TheHeroBase] army stats: Swordsman={s} Archer={a} Mage={m}");
            THButtonLayoutFix.ApplyBase();
            }

        private void UpdateBuildingsUI()
        {
            if (buildingsContainer == null) return;

            // Clear container (except template if it's there)
            foreach (Transform child in buildingsContainer)
            {
                if (child.gameObject != buildingCardTemplate)
                    Destroy(child.gameObject);
            }

            foreach (var b in State.buildings)
            {
                CreateBuildingCard(b);
            }
        }

        private void CreateBuildingCard(THBuildingData b)
        {
            if (buildingCardTemplate == null)
            {
                Debug.LogWarning("[TheHeroBase] Building card template is missing.");
                return;
            }

            GameObject card = Instantiate(buildingCardTemplate, buildingsContainer);
            card.SetActive(true);
            card.name = "BuildingCard_" + b.id;

            var texts = card.GetComponentsInChildren<Text>(true);
            var buttons = card.GetComponentsInChildren<Button>(true);

            int maxVal = GetMaxRecruits(b.id);
            int currentVal = b.recruitsAvailable;

            SetText(texts, "NameText", b.name);
            SetText(texts, "LevelText", $"Уровень: {b.level}/2");
            SetText(texts, "UnitText", GetUnitDisplayName(b.id));
            SetText(texts, "CountAvailableText", $"{currentVal}/{maxVal}");
            SetText(texts, "CostText", $"Цена за 1: {GetRecruitCostString(b, false)}");
            
            SetText(texts, "TotalCostText", $"Сумма: {GetRecruitCostString(b, true)}");

            string upgradeCost = b.level >= 2 ? "Улучшено" : $"Улучшить: {GetUpgradeCostString(b.id)}";
            SetText(texts, "UpgradeCostText", upgradeCost);

            var r1 = buttons.FirstOrDefault(x => x.name == "RecruitOneButton");
            if (r1 != null)
            {
                r1.onClick.RemoveAllListeners();
                r1.onClick.AddListener(() => RecruitOne(b.id));
                r1.interactable = currentVal > 0 && HasResources(b.goldCost, b.woodCost, b.stoneCost, b.manaCost);
            }

            var ra = buttons.FirstOrDefault(x => x.name == "RecruitAllButton");
            if (ra != null)
            {
                ra.onClick.RemoveAllListeners();
                ra.onClick.AddListener(() => RecruitAll(b.id));
                ra.interactable = currentVal > 0 && HasResources(b.goldCost, b.woodCost, b.stoneCost, b.manaCost);
            }

            var up = buttons.FirstOrDefault(x => x.name == "UpgradeButton");
            if (up != null)
            {
                up.onClick.RemoveAllListeners();
                up.onClick.AddListener(() => UpgradeBuilding(b.id));
                up.interactable = b.level < 2;
                if (b.level >= 2) up.gameObject.SetActive(false);
            }
        }

        private int GetMaxRecruits(string id)
        {
            if (id == "unit_swordsman") return 18;
            if (id == "unit_archer") return 12;
            if (id == "unit_mage") return 6;
            return 10;
        }

        private void UpdateArmyUI()
        {
            if (armyContainer == null) return;

            foreach (Transform child in armyContainer)
            {
                if (child.gameObject != armyRowTemplate)
                    Destroy(child.gameObject);
            }

            int totalCount = 0;
            foreach (var u in State.army)
            {
                if (u.count <= 0) continue;
                if (armyRowTemplate == null)
                {
                    Debug.LogWarning("[TheHeroBase] Army row template is missing.");
                    break;
                }
                
                GameObject row = Instantiate(armyRowTemplate, armyContainer);
                row.SetActive(true);
                var t = row.GetComponentInChildren<Text>();
                if (t != null) t.text = $"{u.name} x{u.count}";
                totalCount += u.count;
            }

            if (armySummaryText != null)
                armySummaryText.text = $"\u0412\u0441\u0435\u0433\u043e \u044e\u043d\u0438\u0442\u043e\u0432: {totalCount}";
        }

        public void RecruitOne(string buildingId)
        {
            var b = State.buildings.Find(x => x.id == buildingId);
            if (b == null) return;

            if (b.recruitsAvailable <= 0)
            {
                ShowMessage("Нет доступного найма");
                return;
            }

            if (HasResources(b.goldCost, b.woodCost, b.stoneCost, b.manaCost))
            {
                SpendResources(b.goldCost, b.woodCost, b.stoneCost, b.manaCost);
                AddUnit(buildingId, 1);
                b.recruitsAvailable--;
                ShowMessage($"Нанят: {GetUnitDisplayName(buildingId)} x1");
                RefreshUI();
                SaveGameIfPossible();
            }
            else
            {
                ShowMessage("Недостаточно ресурсов");
            }
        }

        public void RecruitAll(string buildingId)
        {
            var b = State.buildings.Find(x => x.id == buildingId);
            if (b == null) return;

            int maxAfford = CalculateMaxAffordable(b);
            int toBuy = Mathf.Min(b.recruitsAvailable, maxAfford);

            if (toBuy <= 0)
            {
                ShowMessage("Недостаточно ресурсов или нет доступного найма");
                return;
            }

            SpendResources(b.goldCost * toBuy, b.woodCost * toBuy, b.stoneCost * toBuy, b.manaCost * toBuy);
            AddUnit(buildingId, toBuy);
            b.recruitsAvailable -= toBuy;
            ShowMessage($"Нанято: {GetUnitDisplayName(buildingId)} x{toBuy}");
            RefreshUI();
            SaveGameIfPossible();
        }

        public void UpgradeBuilding(string buildingId)
        {
            var b = State.buildings.Find(x => x.id == buildingId);
            if (b == null) return;

            if (b.level >= 2)
            {
                ShowMessage("Максимальный уровень");
                return;
            }

            int g = 0, w = 0, s = 0, m = 0;
            GetUpgradeCosts(buildingId, out g, out w, out s, out m);

            if (HasResources(g, w, s, m))
            {
                SpendResources(g, w, s, m);
                b.level++;
                ShowMessage($"Здание улучшено: {b.name}");
                RefreshUI();
                SaveGameIfPossible();
                if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("upgrade");
            }
            else
            {
                ShowMessage("Недостаточно ресурсов");
            }
        }

        private bool HasResources(int g, int w, int s, int m)
        {
            return State.gold >= g && State.wood >= w && State.stone >= s && State.mana >= m;
        }

        private void SpendResources(int g, int w, int s, int m)
        {
            State.gold -= g;
            State.wood -= w;
            State.stone -= s;
            State.mana -= m;
        }

        private int CalculateMaxAffordable(THBuildingData b)
        {
            int maxG = b.goldCost > 0 ? State.gold / b.goldCost : 999;
            int maxW = b.woodCost > 0 ? State.wood / b.woodCost : 999;
            int maxS = b.stoneCost > 0 ? State.stone / b.stoneCost : 999;
            int maxM = b.manaCost > 0 ? State.mana / b.manaCost : 999;

            return Mathf.Min(maxG, Mathf.Min(maxW, Mathf.Min(maxS, maxM)));
        }

        private void AddUnit(string id, int count)
        {
            var u = State.army.Find(x => x.id == id);
            if (u != null)
            {
                u.count += count;
            }
            else
            {
                u = CreateNewUnit(id);
                u.count = count;
                State.army.Add(u);
            }
        }

        private THArmyUnit CreateNewUnit(string id)
        {
            if (id == "unit_swordsman") return new THArmyUnit { id = id, name = "Swordsman", hpPerUnit = 30, attack = 5, defense = 2, initiative = 5 };
            if (id == "unit_archer") return new THArmyUnit { id = id, name = "Archer", hpPerUnit = 20, attack = 7, defense = 1, initiative = 7 };
            if (id == "unit_mage") return new THArmyUnit { id = id, name = "Mage", hpPerUnit = 25, attack = 10, defense = 2, initiative = 8 };
            return new THArmyUnit { id = id, name = id, count = 0 };
        }

        private string GetUnitDisplayName(string id)
        {
            if (id == "unit_swordsman") return "Swordsman";
            if (id == "unit_archer") return "Archer";
            if (id == "unit_mage") return "Mage";
            return id;
        }

        private string GetRecruitCostString(THBuildingData b, bool total)
        {
            int mult = total ? b.recruitsAvailable : 1;
            string s = $"{b.goldCost * mult} gold";
            if (b.woodCost > 0) s += $", {b.woodCost * mult} wood";
            if (b.stoneCost > 0) s += $", {b.stoneCost * mult} stone";
            if (b.manaCost > 0) s += $", {b.manaCost * mult} mana";
            return s;
        }

        private string GetUpgradeCostString(string id)
        {
            int g, w, s, m;
            GetUpgradeCosts(id, out g, out w, out s, out m);
            string str = $"{g} gold";
            if (w > 0) str += $", {w} wood";
            if (s > 0) str += $", {s} stone";
            if (m > 0) str += $", {m} mana";
            return str;
        }

        private void GetUpgradeCosts(string id, out int g, out int w, out int s, out int m)
        {
            g = w = s = m = 0;
            if (id == "unit_swordsman") { g = 300; w = 10; }
            else if (id == "unit_archer") { g = 350; w = 15; }
            else if (id == "unit_mage") { g = 500; s = 20; m = 10; }
        }

        private void SetText(Text[] array, string name, string val)
        {
            var t = array.FirstOrDefault(x => x.name == name);
            if (t != null) t.text = val;
        }

        private void ShowMessage(string msg)
        {
            if (THMessageSystem.Instance != null)
                THMessageSystem.Instance.ShowMessage(msg);
            Debug.Log("[Base] " + msg);
        }

        private void SaveGameIfPossible()
        {
            THSavePolicy.SaveOnBaseAction();
        }

        public void BackToMap()
        {
            Debug.Log("[TheHeroBase] Back to Map clicked");
            SceneManager.LoadScene("Map");
        }
    }
}
