using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace TheHero.Generated
{
    public class THMapUIRuntime : MonoBehaviour
    {
        public static THMapUIRuntime Instance { get; private set; }

        [Header("HUD")]
        public Text GoldText;
        public Text WoodText;
        public Text StoneText;
        public Text ManaText;

        [Header("Top Buttons")]
        public Button SaveButton;
        public Button LoadButton;
        public Button EndTurnButton;
        public Button MenuButton;
        public Button CastleButton;

        [Header("Pause Overlay")]
        public GameObject PauseOverlay;
        public Button ContinueButton;
        public Button PauseSaveButton;
        public Button PauseLoadButton;
        public Button MainMenuButton;

        [Header("Messages")]
        public GameObject MessagePanel;
        public Text MessageText;

        private int _lastGold = int.MinValue;
        private int _lastWood = int.MinValue;
        private int _lastStone = int.MinValue;
        private int _lastMana = int.MinValue;
        private float _hideMessageAt = -1f;

        private void Awake()
        {
            Instance = this;
            ResolveReferences();
            SetPauseVisible(false);
        }

        private void Start()
        {
            ResolveReferences();
            ConnectButtons();
            UpdateHud(true);
            Debug.Log("[TheHeroMapUI] Runtime connected");
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ToggleMenu();
            }

            UpdateHud(false);

            if (_hideMessageAt > 0f && Time.unscaledTime >= _hideMessageAt)
            {
                _hideMessageAt = -1f;
                if (MessageText != null) MessageText.text = string.Empty;
                if (MessagePanel != null) MessagePanel.SetActive(false);
            }
        }

        public void SaveGame()
        {
            var controller = GetController();
            if (controller != null)
            {
                controller.SaveGame();
                ShowMessage("Игра сохранена.", 2f);
                return;
            }

            if (THManager.Instance != null && THManager.Instance.Data != null)
            {
                THSaveSystem.SaveGame(THManager.Instance.Data);
                ShowMessage("Игра сохранена.", 2f);
                return;
            }

            Debug.Log("[TheHeroMapUI] Save clicked");
        }

        public void LoadGame()
        {
            var controller = GetController();
            if (controller != null)
            {
                controller.LoadGame();
                return;
            }

            var data = THSaveSystem.LoadGame();
            if (data != null)
            {
                THManager.Instance.Data = data;
                UpdateHud(true);
                ShowMessage("Игра загружена.", 2f);
                return;
            }

            Debug.Log("[TheHeroMapUI] Load clicked");
        }

        public void EndTurn()
        {
            var controller = GetController();
            if (controller != null)
            {
                controller.EndTurn();
                UpdateHud(true);
                return;
            }

            var state = GetState();
            if (state == null)
            {
                Debug.Log("[TheHeroMapUI] End Turn clicked");
                return;
            }

            state.day++;
            state.daysPassed++;
            if (state.day > 7)
            {
                state.day = 1;
                state.week++;
                state.gold += THBalanceConfig.BaseWeeklyGoldIncome;
                state.wood += THBalanceConfig.BaseWeeklyWoodIncome;
                state.stone += THBalanceConfig.BaseWeeklyStoneIncome;
                state.mana += THBalanceConfig.BaseWeeklyManaIncome;
                ShowMessage("Новая неделя. Казна пополнена.", 3f);
            }

            state.maxMovementPoints = THBalanceConfig.HeroMaxMovementPoints;
            state.movementPoints = state.maxMovementPoints;
            UpdateHud(true);
        }

        public void OpenPauseMenu()
        {
            SetPauseVisible(true);
        }

        public void ShowPause()
        {
            SetPauseVisible(true);
        }

        public void HidePause()
        {
            SetPauseVisible(false);
        }

        public void ToggleMenu()
        {
            bool visible = PauseOverlay != null && PauseOverlay.activeSelf;
            SetPauseVisible(!visible);
        }

        public void LoadMainMenu()
        {
            Time.timeScale = 1f;
            THSceneLoader.Instance.LoadMainMenu();
        }

        public void OpenCastle()
        {
            Time.timeScale = 1f;
            THSceneLoader.Instance.LoadBase();
        }

        public void ShowMessage(string text)
        {
            ShowMessage(text, 3f);
        }

        public void ShowMessage(string text, float duration)
        {
            if (MessageText != null) MessageText.text = text ?? string.Empty;
            if (MessagePanel != null) MessagePanel.SetActive(!string.IsNullOrEmpty(text));
            _hideMessageAt = duration > 0f ? Time.unscaledTime + duration : -1f;
        }

        public void UpdateHud()
        {
            UpdateHud(true);
        }

        private void UpdateHud(bool force)
        {
            var state = GetState();
            int gold = state != null ? state.gold : 300;
            int wood = state != null ? state.wood : 10;
            int stone = state != null ? state.stone : 5;
            int mana = state != null ? state.mana : 0;

            if (!force && gold == _lastGold && wood == _lastWood && stone == _lastStone && mana == _lastMana)
                return;

            _lastGold = gold;
            _lastWood = wood;
            _lastStone = stone;
            _lastMana = mana;

            if (GoldText != null) GoldText.text = "Gold: " + gold;
            if (WoodText != null) WoodText.text = "Wood: " + wood;
            if (StoneText != null) StoneText.text = "Stone: " + stone;
            if (ManaText != null) ManaText.text = "Mana: " + mana;
        }

        private void SetPauseVisible(bool visible)
        {
            if (PauseOverlay != null) PauseOverlay.SetActive(visible);
            Time.timeScale = visible ? 0f : 1f;
        }

        private void ConnectButtons()
        {
            WireIfNeeded(SaveButton, SaveGame);
            WireIfNeeded(LoadButton, LoadGame);
            WireIfNeeded(EndTurnButton, EndTurn);
            WireIfNeeded(MenuButton, OpenPauseMenu);
            WireIfNeeded(CastleButton, OpenCastle);
            WireIfNeeded(ContinueButton, HidePause);
            WireIfNeeded(PauseSaveButton, SaveGame);
            WireIfNeeded(PauseLoadButton, LoadGame);
            WireIfNeeded(MainMenuButton, LoadMainMenu);
            Debug.Log("[TheHeroMapUI] Buttons connected");
        }

        private static void WireIfNeeded(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null || action == null) return;
            if (button.onClick.GetPersistentEventCount() > 0) return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void ResolveReferences()
        {
            GoldText = GoldText != null ? GoldText : FindText("GoldText");
            WoodText = WoodText != null ? WoodText : FindText("WoodText");
            StoneText = StoneText != null ? StoneText : FindText("StoneText");
            ManaText = ManaText != null ? ManaText : FindText("ManaText");

            SaveButton = SaveButton != null ? SaveButton : FindButton("SaveButton", transform);
            LoadButton = LoadButton != null ? LoadButton : FindButton("LoadButton", transform);
            EndTurnButton = EndTurnButton != null ? EndTurnButton : FindButton("EndTurnButton", transform);
            MenuButton = MenuButton != null ? MenuButton : FindButton("MenuButton", transform);
            CastleButton = CastleButton != null ? CastleButton : FindButton("CastleButton", transform);

            PauseOverlay = PauseOverlay != null ? PauseOverlay : FindObject("PauseOverlay");
            MessagePanel = MessagePanel != null ? MessagePanel : FindObject("MessagePanel");
            MessageText = MessageText != null ? MessageText : FindText("MessageText");

            if (PauseOverlay != null)
            {
                ContinueButton = ContinueButton != null ? ContinueButton : FindButton("ContinueButton", PauseOverlay.transform);
                PauseSaveButton = PauseSaveButton != null ? PauseSaveButton : FindButton("SaveButton", PauseOverlay.transform);
                PauseLoadButton = PauseLoadButton != null ? PauseLoadButton : FindButton("LoadButton", PauseOverlay.transform);
                MainMenuButton = MainMenuButton != null ? MainMenuButton : FindButton("MainMenuButton", PauseOverlay.transform);
            }
        }

        private THGameState GetState()
        {
            var controller = GetController();
            if (controller != null && controller.State != null) return controller.State;
            if (THManager.Instance != null) return THManager.Instance.Data;
            return null;
        }

        private static THMapController GetController()
        {
            if (THMapController.Instance != null) return THMapController.Instance;
            return Object.FindAnyObjectByType<THMapController>();
        }

        private Text FindText(string objectName)
        {
            foreach (var text in GetComponentsInChildren<Text>(true))
            {
                if (text.name == objectName) return text;
            }
            var go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<Text>() : null;
        }

        private GameObject FindObject(string objectName)
        {
            foreach (var rect in GetComponentsInChildren<RectTransform>(true))
            {
                if (rect.name == objectName) return rect.gameObject;
            }
            return GameObject.Find(objectName);
        }

        private static Button FindButton(string objectName, Transform root)
        {
            if (root != null)
            {
                foreach (var button in root.GetComponentsInChildren<Button>(true))
                {
                    if (button.name == objectName) return button;
                }
            }

            var go = GameObject.Find(objectName);
            return go != null ? go.GetComponent<Button>() : null;
        }
    }
}
