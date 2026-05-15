using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using TheHero.Domain;
using TheHero.Subsystems.Base;
using TheHero.Subsystems.Save;

namespace TheHero
{
    // Deprecated architecture duplicate. Active scenes use TheHero.Generated.THManager.
    // Kept because subsystem prototypes still compile against this namespace.
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; }

        // Загруженные конфиги (доступны всем подсистемам)
        public List<UnitType> UnitTypes { get; private set; }
        public List<BuildingConfig> BuildingConfigs { get; private set; }

        public event Action OnGameStateChanged;
        public event Action<int> OnDayEnded;
        public event Action<int> OnWeekEnded;

        private SaveManager _saveManager;
        private RecruitmentService _recruitmentService;

        // Имена сцен должны совпадать с Build Settings
        private const string SceneMainMenu = "MainMenu";
        private const string SceneMap      = "Map";
        private const string SceneCombat   = "Combat";
        private const string SceneBase     = "Base";

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _saveManager = new SaveManager();
            LoadConfigs();
        }

        private void LoadConfigs()
        {
            UnitTypes       = LoadJson<List<UnitType>>("Config/units_config");
            BuildingConfigs = LoadJson<List<BuildingConfig>>("Config/buildings_config");

            if (UnitTypes == null || BuildingConfigs == null)
            {
                Debug.LogError("[GameManager] Не удалось загрузить конфиги из Resources/Config/");
                return;
            }

            var unitDict = new Dictionary<string, UnitType>();
            foreach (var u in UnitTypes) unitDict[u.Id] = u;

            // Связать HireCost юнитов из конфига построек
            foreach (var cfg in BuildingConfigs)
            {
                if (cfg.UpgradeCost == null) cfg.UpgradeCost = new ResourceWallet();
                if (cfg.UnitUpgradeCostPerUnit == null) cfg.UnitUpgradeCostPerUnit = new ResourceWallet();
            }

            _recruitmentService = new RecruitmentService(BuildingConfigs, UnitTypes);
            Debug.Log($"[GameManager] Загружено {UnitTypes.Count} юнитов, {BuildingConfigs.Count} построек");
        }

        public void StartNewGame()
        {
            State = new GameState
            {
                Hero = new HeroState { Name = "Герой", Level = 1, MaxMovementPoints = 10, MovementPoints = 10 },
                Day  = 1,
                Week = 1,
                TurnNumber = 1
            };

            // Инициализировать базу постройками из конфига
            foreach (var cfg in BuildingConfigs)
                State.Base.AddBuilding(cfg.Id);
            State.Base.IsOwned = true;

            State.Wallet.Add(ResourceType.Gold, 500);
            State.Wallet.Add(ResourceType.Wood,  20);
            State.Wallet.Add(ResourceType.Stone, 10);

            OnGameStateChanged?.Invoke();
            LoadScene(SceneMap);
        }

        public void LoadGame()
        {
            var loaded = _saveManager.Load();
            if (loaded == null)
            {
                Debug.LogWarning("[GameManager] Нет сохранения для загрузки");
                return;
            }
            State = loaded;
            OnGameStateChanged?.Invoke();
            LoadScene(SceneMap);
        }

        public void SaveGame() => _saveManager.Save(State);

        public void EndTurn()
        {
            if (State == null) return;

            int prevWeek = State.Week;
            State.AdvanceTurn();

            OnDayEnded?.Invoke(State.Day);

            // Смена недели: день перешёл с 7 на 1
            if (State.Week != prevWeek)
            {
                _recruitmentService?.AccumulateWeeklyRecruits(State.Base, 1);
                OnWeekEnded?.Invoke(State.Week);
                Debug.Log($"[GameManager] Началась неделя {State.Week}");
            }

            OnGameStateChanged?.Invoke();
            _saveManager.AutoSave(State);
        }

        // Переходы между сценами
        public void GoToMainMenu() => LoadScene(SceneMainMenu);
        public void GoToMap()      => LoadScene(SceneMap);
        public void GoToCombat()   => LoadScene(SceneCombat);
        public void GoToBase()     => LoadScene(SceneBase);

        private void LoadScene(string sceneName) =>
            SceneManager.LoadScene(sceneName);

        private static T LoadJson<T>(string resourcePath) where T : class
        {
            var asset = Resources.Load<TextAsset>(resourcePath);
            if (asset == null)
            {
                Debug.LogError($"[GameManager] Файл не найден: Resources/{resourcePath}");
                return null;
            }
            try
            {
                return JsonConvert.DeserializeObject<T>(asset.text);
            }
            catch (Exception e)
            {
                Debug.LogError($"[GameManager] Ошибка разбора {resourcePath}: {e.Message}");
                return null;
            }
        }
    }
}
