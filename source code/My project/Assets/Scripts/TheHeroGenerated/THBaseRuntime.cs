using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace TheHero.Generated
{
    public class THBaseRuntime : MonoBehaviour
    {
        public static THBaseRuntime Instance { get; private set; }

        private const string SwordsmanId = "unit_swordsman";
        private const string ArcherId = "unit_archer";
        private const string MageId = "unit_mage";

        private const int DefaultGold = 300;
        private const int DefaultWood = 10;
        private const int DefaultStone = 5;
        private const int DefaultMana = 0;

        private const int DefaultSwordsman = 8;
        private const int DefaultArcher = 4;
        private const int DefaultMage = 0;

        private const int SwordsmanAvailable = 6;
        private const int ArcherAvailable = 4;
        private const int MageAvailable = 0;

        private const int SwordsmanGoldCost = 60;
        private const int ArcherGoldCost = 90;
        private const int ArcherWoodCost = 1;
        private const int MageGoldCost = 150;
        private const int MageManaCost = 2;

        [Header("Top Bar")]
        public Text resourcesText;
        public Button backToMapButton;

        [Header("Containers")]
        public Transform buildingsContainer;
        public Transform armyContainer;

        [Header("Prefabs/Templates (legacy, no longer required)")]
        public GameObject buildingCardTemplate;
        public GameObject armyRowTemplate;

        [Header("Status")]
        public Text armySummaryText;
        public Text messageText;

        private THGameState _state;
        private readonly Dictionary<string, int> _weeklyCaps = new Dictionary<string, int>();

        private THGameState State
        {
            get
            {
                if (_state == null) LoadState();
                return _state;
            }
        }

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            LoadState();
            EnsureEventSystem();
            EnsureUi();
            RefreshUI();

            if (THAudioManager.Instance != null) THAudioManager.Instance.PlayMusic("Base");
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                BackToMap();
            }
        }

        private void LoadState()
        {
            try
            {
                _state = THManager.Instance.Data;
            }
            catch
            {
                _state = null;
            }

            if (_state == null)
            {
                _state = THSaveSystem.LoadGame();
            }

            if (_state == null)
            {
                _state = CreateFallbackStateFromPlayerPrefs();
            }

            EnsureBaseDefaults(_state);
        }

        private THGameState CreateFallbackStateFromPlayerPrefs()
        {
            var state = new THGameState();
            state.gold = PlayerPrefs.GetInt("TheHero_Gold", DefaultGold);
            state.wood = PlayerPrefs.GetInt("TheHero_Wood", DefaultWood);
            state.stone = PlayerPrefs.GetInt("TheHero_Stone", DefaultStone);
            state.mana = PlayerPrefs.GetInt("TheHero_Mana", DefaultMana);
            state.day = Mathf.Max(1, state.day);
            state.week = Mathf.Max(1, state.week);

            state.army = new List<THArmyUnit>
            {
                CreateUnit(SwordsmanId, PlayerPrefs.GetInt("TheHero_Army_Swordsman", DefaultSwordsman)),
                CreateUnit(ArcherId, PlayerPrefs.GetInt("TheHero_Army_Archer", DefaultArcher)),
                CreateUnit(MageId, PlayerPrefs.GetInt("TheHero_Army_Mage", DefaultMage))
            };

            state.buildings = new List<THBuildingData>
            {
                CreateBuilding(SwordsmanId, PlayerPrefs.GetInt("TheHero_Recruit_Swordsman_Available", SwordsmanAvailable)),
                CreateBuilding(ArcherId, PlayerPrefs.GetInt("TheHero_Recruit_Archer_Available", ArcherAvailable)),
                CreateBuilding(MageId, PlayerPrefs.GetInt("TheHero_Recruit_Mage_Available", MageAvailable))
            };

            return state;
        }

        private void EnsureBaseDefaults(THGameState state)
        {
            if (state == null) return;

            if (state.army == null) state.army = new List<THArmyUnit>();
            if (state.buildings == null) state.buildings = new List<THBuildingData>();

            if (state.army.Count == 0)
            {
                state.gold = PlayerPrefs.GetInt("TheHero_Gold", DefaultGold);
                state.wood = PlayerPrefs.GetInt("TheHero_Wood", DefaultWood);
                state.stone = PlayerPrefs.GetInt("TheHero_Stone", DefaultStone);
                state.mana = PlayerPrefs.GetInt("TheHero_Mana", DefaultMana);
                state.army.Add(CreateUnit(SwordsmanId, PlayerPrefs.GetInt("TheHero_Army_Swordsman", DefaultSwordsman)));
                state.army.Add(CreateUnit(ArcherId, PlayerPrefs.GetInt("TheHero_Army_Archer", DefaultArcher)));
                state.army.Add(CreateUnit(MageId, PlayerPrefs.GetInt("TheHero_Army_Mage", DefaultMage)));
            }

            EnsureArmySlot(state, SwordsmanId, DefaultSwordsman);
            EnsureArmySlot(state, ArcherId, DefaultArcher);
            EnsureArmySlot(state, MageId, DefaultMage);

            NormalizeArmyUnit(state.army.First(u => u.id == SwordsmanId), "Мечник", 30, 5, 3, 4);
            NormalizeArmyUnit(state.army.First(u => u.id == ArcherId), "Лучник", 18, 7, 1, 6);
            NormalizeArmyUnit(state.army.First(u => u.id == MageId), "Маг", 14, 10, 1, 5);

            EnsureBuilding(state, SwordsmanId, PlayerPrefs.GetInt("TheHero_Recruit_Swordsman_Available", SwordsmanAvailable));
            EnsureBuilding(state, ArcherId, PlayerPrefs.GetInt("TheHero_Recruit_Archer_Available", ArcherAvailable));
            EnsureBuilding(state, MageId, PlayerPrefs.GetInt("TheHero_Recruit_Mage_Available", MageAvailable));

            _weeklyCaps[SwordsmanId] = SwordsmanAvailable;
            _weeklyCaps[ArcherId] = ArcherAvailable;
            _weeklyCaps[MageId] = state.week >= 2 ? 2 : 0;
        }

        private void EnsureArmySlot(THGameState state, string unitId, int fallbackCount)
        {
            if (state.army.Any(u => u.id == unitId)) return;
            state.army.Add(CreateUnit(unitId, PlayerPrefs.GetInt(GetArmyPrefKey(unitId), fallbackCount)));
        }

        private void EnsureBuilding(THGameState state, string unitId, int fallbackAvailable)
        {
            var building = state.buildings.FirstOrDefault(b => b.id == unitId);
            if (building == null)
            {
                state.buildings.Add(CreateBuilding(unitId, fallbackAvailable));
                return;
            }

            var configured = CreateBuilding(unitId, building.recruitsAvailable);
            building.name = configured.name;
            building.level = Mathf.Max(1, building.level);
            building.goldCost = configured.goldCost;
            building.woodCost = configured.woodCost;
            building.stoneCost = configured.stoneCost;
            building.manaCost = configured.manaCost;
            building.recruitsAvailable = Mathf.Max(0, building.recruitsAvailable);
        }

        private THArmyUnit CreateUnit(string unitId, int count)
        {
            if (unitId == SwordsmanId) return new THArmyUnit { id = SwordsmanId, name = "Мечник", count = Mathf.Max(0, count), hpPerUnit = 30, attack = 5, defense = 3, initiative = 4 };
            if (unitId == ArcherId) return new THArmyUnit { id = ArcherId, name = "Лучник", count = Mathf.Max(0, count), hpPerUnit = 18, attack = 7, defense = 1, initiative = 6 };
            return new THArmyUnit { id = MageId, name = "Маг", count = Mathf.Max(0, count), hpPerUnit = 14, attack = 10, defense = 1, initiative = 5 };
        }

        private THBuildingData CreateBuilding(string unitId, int available)
        {
            if (unitId == SwordsmanId)
                return new THBuildingData { id = SwordsmanId, name = "Казармы", level = 1, recruitsAvailable = Mathf.Max(0, available), goldCost = SwordsmanGoldCost };
            if (unitId == ArcherId)
                return new THBuildingData { id = ArcherId, name = "Стрельбище", level = 1, recruitsAvailable = Mathf.Max(0, available), goldCost = ArcherGoldCost, woodCost = ArcherWoodCost };
            return new THBuildingData { id = MageId, name = "Башня магов", level = 1, recruitsAvailable = Mathf.Max(0, available), goldCost = MageGoldCost, manaCost = MageManaCost };
        }

        private void NormalizeArmyUnit(THArmyUnit unit, string name, int hp, int attack, int defense, int initiative)
        {
            unit.name = name;
            unit.count = Mathf.Max(0, unit.count);
            unit.hpPerUnit = hp;
            unit.attack = attack;
            unit.defense = defense;
            unit.initiative = initiative;
        }

        private void EnsureUi()
        {
            if (TryBindExistingUi()) return;
            RebuildRuntimeCanvas();
        }

        private bool TryBindExistingUi()
        {
            var canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include)
                .FirstOrDefault(c => c != null && c.transform.Find("TopBar") != null && c.transform.Find("MainPanel") != null);
            if (canvas == null) return false;

            resourcesText = FindText(canvas.transform, "ResourcesText");
            backToMapButton = FindButton(canvas.transform, "BackToMapButton");
            buildingsContainer = FindTransform(canvas.transform, "HireListContainer");
            armyContainer = FindTransform(canvas.transform, "ArmyListContainer");
            messageText = FindText(canvas.transform, "MessageText");

            if (resourcesText == null || backToMapButton == null || buildingsContainer == null || armyContainer == null || messageText == null)
                return false;

            WireBackButton();
            return true;
        }

        private void RebuildRuntimeCanvas()
        {
            foreach (var oldCanvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
            {
                if (oldCanvas != null) Destroy(oldCanvas.gameObject);
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            BuildBaseCanvas(canvasGo.transform);
            Debug.Log("[TheHeroBase] Runtime Base Canvas rebuilt.");
        }

        public void BuildBaseCanvas(Transform canvasRoot)
        {
            var topBar = CreatePanel(canvasRoot, "TopBar", new Color(0.09f, 0.07f, 0.05f, 0.96f));
            SetRect(topBar, new Vector2(0, 1), new Vector2(1, 1), new Vector2(0, -35), new Vector2(0, 70));

            var title = CreateText(topBar, "TitleText", "ЗАМОК", 32, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.36f));
            SetRect(title.transform, new Vector2(0, 0), new Vector2(0, 1), new Vector2(150, 0), new Vector2(260, 0));

            resourcesText = CreateText(topBar, "ResourcesText", "", 22, TextAnchor.MiddleLeft, Color.white);
            SetRect(resourcesText.transform, new Vector2(0, 0), new Vector2(1, 1), new Vector2(620, 0), new Vector2(-760, 0));

            backToMapButton = CreateButton(topBar, "BackToMapButton", "НА КАРТУ", 22);
            SetRect(backToMapButton.transform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-140, 0), new Vector2(220, 48));
            WireBackButton();

            var mainPanel = CreatePanel(canvasRoot, "MainPanel", new Color(0.035f, 0.035f, 0.04f, 0.96f));
            SetRect(mainPanel, new Vector2(0, 0), new Vector2(1, 1), new Vector2(0, -30), new Vector2(-80, -160));

            var hirePanel = CreatePanel(mainPanel, "HirePanel", new Color(0.11f, 0.10f, 0.09f, 0.92f));
            SetRect(hirePanel, new Vector2(0, 0), new Vector2(0.66f, 1), new Vector2(35, -10), new Vector2(-55, -30));

            var hireTitle = CreateText(hirePanel, "HireTitle", "НАЙМ ВОЙСК", 28, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.36f));
            SetRect(hireTitle.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(28, -35), new Vector2(-56, 58));

            var hireListGo = new GameObject("HireListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            hireListGo.transform.SetParent(hirePanel, false);
            buildingsContainer = hireListGo.transform;
            SetRect(buildingsContainer, new Vector2(0, 0), new Vector2(1, 1), new Vector2(26, -80), new Vector2(-52, -110));
            var hireLayout = hireListGo.GetComponent<VerticalLayoutGroup>();
            hireLayout.spacing = 14;
            hireLayout.padding = new RectOffset(0, 0, 0, 0);
            hireLayout.childControlHeight = false;
            hireLayout.childForceExpandHeight = false;
            hireLayout.childControlWidth = true;
            hireLayout.childForceExpandWidth = true;

            var armyPanel = CreatePanel(mainPanel, "ArmyPanel", new Color(0.09f, 0.10f, 0.12f, 0.92f));
            SetRect(armyPanel, new Vector2(0.68f, 0), new Vector2(1, 1), new Vector2(10, -10), new Vector2(-35, -30));

            var armyTitle = CreateText(armyPanel, "ArmyTitle", "АРМИЯ", 28, TextAnchor.MiddleLeft, new Color(1f, 0.82f, 0.36f));
            SetRect(armyTitle.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(26, -35), new Vector2(-52, 58));

            var armyListGo = new GameObject("ArmyListContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            armyListGo.transform.SetParent(armyPanel, false);
            armyContainer = armyListGo.transform;
            SetRect(armyContainer, new Vector2(0, 0), new Vector2(1, 1), new Vector2(24, -100), new Vector2(-48, -110));
            var armyLayout = armyListGo.GetComponent<VerticalLayoutGroup>();
            armyLayout.spacing = 10;
            armyLayout.childControlHeight = false;
            armyLayout.childForceExpandHeight = false;
            armyLayout.childControlWidth = true;
            armyLayout.childForceExpandWidth = true;

            armySummaryText = CreateText(armyPanel, "ArmySummaryText", "", 18, TextAnchor.MiddleLeft, Color.white);
            SetRect(armySummaryText.transform, new Vector2(0, 0), new Vector2(1, 0), new Vector2(24, 35), new Vector2(-48, 50));

            var messagePanel = CreatePanel(canvasRoot, "MessagePanel", new Color(0.08f, 0.07f, 0.06f, 0.96f));
            SetRect(messagePanel, new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 40), new Vector2(0, 80));
            messageText = CreateText(messagePanel, "MessageText", "Добро пожаловать в замок", 22, TextAnchor.MiddleCenter, Color.white);
            SetRect(messageText.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        public void RefreshUI()
        {
            EnsureBaseDefaults(State);
            EnsureUi();

            if (this == null) return;
            if (resourcesText != null)
                resourcesText.text = $"Золото: {State.gold} | Дерево: {State.wood} | Камень: {State.stone} | Мана: {State.mana}";

            if (buildingsContainer != null)
            {
                ClearChildren(buildingsContainer);
                CreateHireRow(GetBuilding(SwordsmanId));
                CreateHireRow(GetBuilding(ArcherId));
                CreateHireRow(GetBuilding(MageId));
            }

            if (armyContainer != null)
            {
                ClearChildren(armyContainer);
                int totalCount = 0;
                foreach (var unit in State.army.Where(u => u != null && u.count > 0))
                {
                    CreateArmyRow(unit);
                    totalCount += unit.count;
                }

                if (armySummaryText != null)
                    armySummaryText.text = $"Всего юнитов: {totalCount}";
            }
        }

        private THBuildingData GetBuilding(string unitId)
        {
            return State.buildings.First(b => b.id == unitId);
        }

        private void CreateHireRow(THBuildingData building)
        {
            if (building == null) return;
            if (buildingsContainer == null) return;

            string capturedUnitId = building.id;
            int available = Mathf.Max(0, building.recruitsAvailable);
            int cap = GetWeeklyCapForDisplay(capturedUnitId, available);

            var row = CreatePanel(buildingsContainer, "HireRow_" + capturedUnitId, new Color(0.16f, 0.14f, 0.12f, 0.96f));
            var rowLayout = row.gameObject.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 110;
            rowLayout.minHeight = 110;
            rowLayout.flexibleWidth = 1f;

            var hg = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            hg.childAlignment = TextAnchor.MiddleLeft;
            hg.spacing = 20;
            hg.padding = new RectOffset(20, 20, 6, 6);
            hg.childControlWidth = false;
            hg.childControlHeight = false;
            hg.childForceExpandWidth = false;
            hg.childForceExpandHeight = false;

            // Icon
            var icon = CreatePanel(row, "UnitIcon", GetUnitColor(capturedUnitId));
            AddSize(icon, 64, 64);

            // Info block
            var infoBlock = CreatePanel(row, "UnitInfoBlock", new Color(0, 0, 0, 0));
            AddSize(infoBlock, 260, 96);
            var infoLayout = infoBlock.gameObject.AddComponent<VerticalLayoutGroup>();
            infoLayout.childAlignment = TextAnchor.MiddleLeft;
            infoLayout.spacing = 6;
            infoLayout.padding = new RectOffset(0, 0, 0, 0);
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = true;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;

            AddRowText(infoBlock, "UnitNameText", GetUnitDisplayName(capturedUnitId), 22, FontStyle.Bold, Color.white);
            AddRowText(infoBlock, "AvailableText", $"Доступно: {available}/{cap}", 18, FontStyle.Normal, Color.white);
            var unit = State.army.FirstOrDefault(u => u.id == capturedUnitId);
            string statsLine = unit != null
                ? $"HP {unit.hpPerUnit} | ATK {unit.attack} | DEF {unit.defense}"
                : string.Empty;
            AddRowText(infoBlock, "StatsText", statsLine, 15, FontStyle.Normal, new Color(0.78f, 0.78f, 0.78f));

            // Cost block
            var costBlock = CreatePanel(row, "CostBlock", new Color(0, 0, 0, 0));
            AddSize(costBlock, 360, 96);
            var costLayout = costBlock.gameObject.AddComponent<VerticalLayoutGroup>();
            costLayout.childAlignment = TextAnchor.MiddleLeft;
            costLayout.spacing = 6;
            costLayout.padding = new RectOffset(0, 0, 0, 0);
            costLayout.childControlWidth = true;
            costLayout.childControlHeight = true;
            costLayout.childForceExpandWidth = true;
            costLayout.childForceExpandHeight = false;

            AddRowText(costBlock, "CostOneText", "Цена за 1: " + GetCostString(building, 1), 17, FontStyle.Normal, new Color(0.93f, 0.86f, 0.68f));
            string totalCostStr = available > 0 ? ("Сумма: " + GetCostString(building, available)) : "Сумма: 0";
            AddRowText(costBlock, "TotalCostText", totalCostStr, 17, FontStyle.Normal, new Color(0.93f, 0.86f, 0.68f));

            // Buttons block
            var buttonsBlock = CreatePanel(row, "ButtonsBlock", new Color(0, 0, 0, 0));
            AddSize(buttonsBlock, 260, 96);
            var btnLayout = buttonsBlock.gameObject.AddComponent<HorizontalLayoutGroup>();
            btnLayout.childAlignment = TextAnchor.MiddleCenter;
            btnLayout.spacing = 12;
            btnLayout.padding = new RectOffset(0, 0, 0, 0);
            btnLayout.childControlWidth = false;
            btnLayout.childControlHeight = false;
            btnLayout.childForceExpandWidth = false;
            btnLayout.childForceExpandHeight = false;

            var recruitOne = CreateButton(buttonsBlock, "RecruitOneButton", "Нанять 1", 18);
            AddSize(recruitOne.transform, 120, 40);
            recruitOne.interactable = available > 0 && HasResources(building, 1);
            recruitOne.onClick.RemoveAllListeners();
            recruitOne.onClick.AddListener(() => RecruitOne(capturedUnitId));

            var recruitAll = CreateButton(buttonsBlock, "RecruitAllButton", "Всех", 18);
            AddSize(recruitAll.transform, 120, 40);
            recruitAll.interactable = available > 0 && CalculateMaxAffordable(building) > 0;
            recruitAll.onClick.RemoveAllListeners();
            recruitAll.onClick.AddListener(() => RecruitAll(capturedUnitId));
        }

        private static void AddSize(Transform t, float w, float h)
        {
            var le = t.gameObject.AddComponent<LayoutElement>();
            le.preferredWidth = w;
            le.preferredHeight = h;
            le.minWidth = w;
            le.minHeight = h;
            var rect = t.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(w, h);
        }

        private static Text AddRowText(Transform parent, string name, string text, int size, FontStyle style, Color color)
        {
            var label = CreateText(parent, name, text, size, TextAnchor.MiddleLeft, color);
            label.fontStyle = style;
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            var le = label.gameObject.AddComponent<LayoutElement>();
            le.minHeight = size + 6;
            le.preferredHeight = size + 8;
            return label;
        }

        private void CreateArmyRow(THArmyUnit unit)
        {
            var row = CreatePanel(armyContainer, "ArmyRow_" + unit.id, new Color(0.14f, 0.15f, 0.17f, 0.96f));
            var layout = row.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 76;
            layout.minHeight = 76;

            var unitName = CreateText(row, "UnitNameText", GetUnitDisplayName(unit.id), 21, TextAnchor.MiddleLeft, Color.white);
            SetRect(unitName.transform, new Vector2(0, 0.5f), new Vector2(1, 1), new Vector2(22, -18), new Vector2(-150, 36));

            var count = CreateText(row, "CountText", "x" + unit.count, 24, TextAnchor.MiddleRight, new Color(1f, 0.82f, 0.36f));
            SetRect(count.transform, new Vector2(1, 0.5f), new Vector2(1, 1), new Vector2(-115, -18), new Vector2(90, 36));

            var stats = CreateText(row, "StatsText", $"HP {unit.hpPerUnit} | ATK {unit.attack} | DEF {unit.defense}", 15, TextAnchor.MiddleLeft, new Color(0.72f, 0.72f, 0.72f));
            SetRect(stats.transform, new Vector2(0, 0), new Vector2(1, 0.5f), new Vector2(22, 12), new Vector2(-44, 30));
        }

        public void RecruitOne(string unitId)
        {
            var building = GetBuilding(unitId);
            if (building.recruitsAvailable <= 0)
            {
                ShowMessage("Нет доступного найма");
                return;
            }

            if (!HasResources(building, 1))
            {
                ShowMessage("Недостаточно ресурсов");
                return;
            }

            SpendResources(building, 1);
            AddUnit(unitId, 1);
            building.recruitsAvailable--;
            State.unitsRecruited++;

            RefreshUI();
            SaveGameIfPossible();
            ShowMessage($"Нанят: {GetUnitDisplayName(unitId)} x1");
        }

        public void RecruitAll(string unitId)
        {
            var building = GetBuilding(unitId);
            int toBuy = Mathf.Min(building.recruitsAvailable, CalculateMaxAffordable(building));

            if (toBuy <= 0)
            {
                ShowMessage("Недостаточно ресурсов или нет доступного найма");
                return;
            }

            SpendResources(building, toBuy);
            AddUnit(unitId, toBuy);
            building.recruitsAvailable -= toBuy;
            State.unitsRecruited += toBuy;

            RefreshUI();
            SaveGameIfPossible();
            ShowMessage($"Нанято: {GetUnitDisplayName(unitId)} x{toBuy}");
        }

        public void SaveGameIfPossible()
        {
            SavePlayerPrefs();

            try
            {
                if (THManager.Instance != null)
                {
                    THManager.Instance.Data = State;
                    THSavePolicy.SaveOnBaseAction();
                    return;
                }
            }
            catch
            {
                // Fall back to direct save below.
            }

            try
            {
                THSaveSystem.SaveGame(State);
            }
            catch
            {
                Debug.LogWarning("[TheHeroBase] SaveGame fallback skipped; state was kept in PlayerPrefs.");
            }
        }

        public void BackToMap()
        {
            Debug.Log("[TheHeroBase] Back to Map clicked");
            SaveGameIfPossible();
            THSceneLoader.Instance.LoadMap();
        }

        private void AddUnit(string unitId, int count)
        {
            var unit = State.army.FirstOrDefault(u => u.id == unitId);
            if (unit == null)
            {
                unit = CreateUnit(unitId, 0);
                State.army.Add(unit);
            }

            unit.count += count;
        }

        private bool HasResources(THBuildingData building, int count)
        {
            return State.gold >= building.goldCost * count &&
                   State.wood >= building.woodCost * count &&
                   State.stone >= building.stoneCost * count &&
                   State.mana >= building.manaCost * count;
        }

        private void SpendResources(THBuildingData building, int count)
        {
            State.gold -= building.goldCost * count;
            State.wood -= building.woodCost * count;
            State.stone -= building.stoneCost * count;
            State.mana -= building.manaCost * count;
        }

        private int CalculateMaxAffordable(THBuildingData building)
        {
            int gold = building.goldCost > 0 ? State.gold / building.goldCost : 9999;
            int wood = building.woodCost > 0 ? State.wood / building.woodCost : 9999;
            int stone = building.stoneCost > 0 ? State.stone / building.stoneCost : 9999;
            int mana = building.manaCost > 0 ? State.mana / building.manaCost : 9999;
            return Mathf.Min(gold, Mathf.Min(wood, Mathf.Min(stone, mana)));
        }

        private string GetCostString(THBuildingData building, int count)
        {
            if (count <= 0) return "0";

            var parts = new List<string>();
            if (building.goldCost > 0) parts.Add($"{building.goldCost * count} золота");
            if (building.woodCost > 0) parts.Add($"{building.woodCost * count} дерева");
            if (building.stoneCost > 0) parts.Add($"{building.stoneCost * count} камня");
            if (building.manaCost > 0) parts.Add($"{building.manaCost * count} маны");
            return string.Join(", ", parts);
        }

        private int GetWeeklyCapForDisplay(string unitId, int available)
        {
            if (_weeklyCaps.TryGetValue(unitId, out int cap)) return Mathf.Max(cap, available);
            return available;
        }

        private string GetUnitDisplayName(string unitId)
        {
            if (unitId == SwordsmanId) return "Мечник";
            if (unitId == ArcherId) return "Лучник";
            if (unitId == MageId) return "Маг";
            return unitId;
        }

        private Color GetUnitColor(string unitId)
        {
            if (unitId == SwordsmanId) return new Color(0.58f, 0.58f, 0.62f, 1f);
            if (unitId == ArcherId) return new Color(0.18f, 0.46f, 0.24f, 1f);
            return new Color(0.35f, 0.25f, 0.58f, 1f);
        }

        private string GetArmyPrefKey(string unitId)
        {
            if (unitId == SwordsmanId) return "TheHero_Army_Swordsman";
            if (unitId == ArcherId) return "TheHero_Army_Archer";
            return "TheHero_Army_Mage";
        }

        private void SavePlayerPrefs()
        {
            PlayerPrefs.SetInt("TheHero_Gold", State.gold);
            PlayerPrefs.SetInt("TheHero_Wood", State.wood);
            PlayerPrefs.SetInt("TheHero_Stone", State.stone);
            PlayerPrefs.SetInt("TheHero_Mana", State.mana);
            PlayerPrefs.SetInt("TheHero_Army_Swordsman", State.army.First(u => u.id == SwordsmanId).count);
            PlayerPrefs.SetInt("TheHero_Army_Archer", State.army.First(u => u.id == ArcherId).count);
            PlayerPrefs.SetInt("TheHero_Army_Mage", State.army.First(u => u.id == MageId).count);
            PlayerPrefs.SetInt("TheHero_Recruit_Swordsman_Available", GetBuilding(SwordsmanId).recruitsAvailable);
            PlayerPrefs.SetInt("TheHero_Recruit_Archer_Available", GetBuilding(ArcherId).recruitsAvailable);
            PlayerPrefs.SetInt("TheHero_Recruit_Mage_Available", GetBuilding(MageId).recruitsAvailable);
            PlayerPrefs.Save();
        }

        private void ShowMessage(string msg)
        {
            if (messageText != null) messageText.text = msg;
            Debug.Log("[TheHeroBase] " + msg);
        }

        private void WireBackButton()
        {
            if (backToMapButton == null) return;
            backToMapButton.onClick.RemoveAllListeners();
            backToMapButton.onClick.AddListener(BackToMap);
            backToMapButton.interactable = true;

            var image = backToMapButton.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
        }

        private void EnsureEventSystem()
        {
            var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
            for (int i = 1; i < systems.Length; i++)
                Destroy(systems[i].gameObject);

            if (systems.Length > 0) return;

            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
        }

        private static Transform CreatePanel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go.transform;
        }

        private static Text CreateText(Transform parent, string name, string text, int size, TextAnchor anchor, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
            go.transform.SetParent(parent, false);
            var label = go.GetComponent<Text>();
            label.text = text;
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = size;
            label.alignment = anchor;
            label.color = color;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            return label;
        }

        private static Button CreateButton(Transform parent, string name, string text, int size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = new Color(0.42f, 0.28f, 0.12f, 1f);
            image.raycastTarget = true;

            var button = go.GetComponent<Button>();
            var colors = button.colors;
            colors.highlightedColor = new Color(0.62f, 0.42f, 0.18f, 1f);
            colors.pressedColor = new Color(0.28f, 0.18f, 0.08f, 1f);
            colors.disabledColor = new Color(0.22f, 0.22f, 0.22f, 0.7f);
            button.colors = colors;

            var label = CreateText(go.transform, "Text", text, size, TextAnchor.MiddleCenter, Color.white);
            SetRect(label.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            return button;
        }

        private static void SetRect(Transform transform, Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            var rect = transform.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static Text FindText(Transform root, string name)
        {
            return FindTransform(root, name)?.GetComponent<Text>();
        }

        private static Button FindButton(Transform root, string name)
        {
            return FindTransform(root, name)?.GetComponent<Button>();
        }

        private static Transform FindTransform(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name) return t;
            }

            return null;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child == null) continue;
                var go = child.gameObject;
                if (go == null) continue;
                if (Application.isPlaying)
                    Object.Destroy(go);
                else
                    Object.DestroyImmediate(go);
            }
        }
    }
}
