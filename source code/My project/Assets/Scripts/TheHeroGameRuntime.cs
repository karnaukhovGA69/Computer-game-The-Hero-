using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace TheHeroGenerated
{
    public enum SceneKind
    {
        MainMenu,
        Map,
        Combat,
        Base
    }

    [Serializable]
    public class UnitStack
    {
        public string id;
        public string name;
        public int count;
        public int hp;
        public int attack;
        public int defense;
        public int initiative;

        public UnitStack Clone()
        {
            return new UnitStack
            {
                id = id,
                name = name,
                count = count,
                hp = hp,
                attack = attack,
                defense = defense,
                initiative = initiative
            };
        }
    }

    [Serializable]
    public class BuildingState
    {
        public string id;
        public string name;
        public string unitId;
        public int level;
        public int available;
        public int weeklyGrowth;
        public int recruitGoldCost;
        public int upgradeGoldCost;
        public int upgradeWoodCost;
    }

    [Serializable]
    public class TheHeroState
    {
        public int day = 1;
        public int week = 1;
        public int gold = 500;
        public int wood = 20;
        public int stone = 10;
        public int mana = 5;
        public int heroLevel = 1;
        public int heroExp = 0;
        public int movement = 20;
        public List<string> visitedObjects = new List<string>();
        public List<UnitStack> army = new List<UnitStack>();
        public List<BuildingState> buildings = new List<BuildingState>();
    }

    public sealed class TheHeroGame : MonoBehaviour
    {
        public static TheHeroGame Instance { get; private set; }
        public TheHeroState State { get; private set; }
        public string PendingEnemyId { get; private set; } = "enemy_orcs";
        public string LastMessage { get; set; } = "Добро пожаловать в The Hero.";

        private string SavePath => Path.Combine(Application.persistentDataPath, "thehero_save.json");

        public static TheHeroGame Ensure()
        {
            if (Instance != null) return Instance;
            var obj = new GameObject("TheHeroGame");
            DontDestroyOnLoad(obj);
            Instance = obj.AddComponent<TheHeroGame>();
            Instance.NewGame(false);
            return Instance;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (State == null) NewGame(false);
        }

        public void NewGame(bool saveAfterCreate = true)
        {
            State = new TheHeroState();
            State.army = new List<UnitStack>
            {
                new UnitStack { id = "unit_swordsman", name = "Мечники", count = 12, hp = 30, attack = 7, defense = 4, initiative = 8 },
                new UnitStack { id = "unit_archer", name = "Лучники", count = 8, hp = 18, attack = 5, defense = 2, initiative = 10 }
            };
            State.buildings = new List<BuildingState>
            {
                new BuildingState { id = "barracks", name = "Казармы", unitId = "unit_swordsman", level = 1, available = 6, weeklyGrowth = 6, recruitGoldCost = 60, upgradeGoldCost = 500, upgradeWoodCost = 10 },
                new BuildingState { id = "range", name = "Стрельбище", unitId = "unit_archer", level = 1, available = 4, weeklyGrowth = 4, recruitGoldCost = 75, upgradeGoldCost = 600, upgradeWoodCost = 15 }
            };
            PendingEnemyId = "enemy_orcs";
            LastMessage = "Новая игра начата. Исследуйте карту, соберите ресурсы и победите врагов.";
            if (saveAfterCreate) SaveGame();
        }

        public bool HasSave()
        {
            return File.Exists(SavePath);
        }

        public bool LoadGame()
        {
            try
            {
                if (!File.Exists(SavePath))
                {
                    LastMessage = "Сохранение не найдено. Начата новая игра.";
                    NewGame(false);
                    return false;
                }

                var json = File.ReadAllText(SavePath);
                var loaded = JsonUtility.FromJson<TheHeroState>(json);
                if (loaded == null || loaded.army == null || loaded.buildings == null)
                {
                    LastMessage = "Файл сохранения повреждён. Начата новая игра.";
                    NewGame(false);
                    return false;
                }

                State = loaded;
                LastMessage = "Сохранение загружено.";
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TheHeroGame] Load failed: " + ex.Message);
                LastMessage = "Ошибка загрузки сохранения. Начата новая игра.";
                NewGame(false);
                return false;
            }
        }

        public void SaveGame()
        {
            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);
                var json = JsonUtility.ToJson(State, true);
                if (File.Exists(SavePath)) File.Copy(SavePath, SavePath + ".bak", true);
                File.WriteAllText(SavePath, json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[TheHeroGame] Save failed: " + ex.Message);
            }
        }

        public bool IsVisited(string id)
        {
            return State.visitedObjects.Contains(id);
        }

        public void MarkVisited(string id)
        {
            if (!State.visitedObjects.Contains(id)) State.visitedObjects.Add(id);
        }

        public void CollectObject(string id)
        {
            if (IsVisited(id))
            {
                LastMessage = "Объект уже посещён.";
                return;
            }

            switch (id)
            {
                case "mine_gold":
                    State.gold += 300;
                    State.stone += 10;
                    LastMessage = "Вы захватили шахту: +300 золота, +10 камня.";
                    break;
                case "forest_cache":
                    State.wood += 25;
                    State.gold += 100;
                    LastMessage = "Найден лесной тайник: +25 дерева, +100 золота.";
                    break;
                case "mana_spring":
                    State.mana += 15;
                    State.heroExp += 60;
                    CheckLevelUp();
                    LastMessage = "Источник маны: +15 маны, +60 опыта.";
                    break;
                default:
                    LastMessage = "Объект исследован.";
                    break;
            }

            MarkVisited(id);
            State.movement = Mathf.Max(0, State.movement - 4);
            SaveGame();
        }

        public void EnterCombat(string enemyId)
        {
            PendingEnemyId = enemyId;
            State.movement = Mathf.Max(0, State.movement - 5);
            SaveGame();
            SceneManager.LoadScene("Combat");
        }

        public List<UnitStack> CreateEnemyArmy(string enemyId)
        {
            if (enemyId == "enemy_bandits")
            {
                return new List<UnitStack>
                {
                    new UnitStack { id = "bandit", name = "Разбойники", count = 10, hp = 20, attack = 5, defense = 2, initiative = 9 },
                    new UnitStack { id = "wolf", name = "Волки", count = 6, hp = 16, attack = 6, defense = 1, initiative = 12 }
                };
            }

            if (enemyId == "enemy_orc_boss")
            {
                return new List<UnitStack>
                {
                    new UnitStack { id = "unit_orc", name = "Орки", count = 18, hp = 35, attack = 8, defense = 5, initiative = 7 },
                    new UnitStack { id = "ogre", name = "Огр", count = 3, hp = 90, attack = 15, defense = 8, initiative = 4 }
                };
            }

            return new List<UnitStack>
            {
                new UnitStack { id = "unit_orc", name = "Орки", count = 12, hp = 35, attack = 7, defense = 4, initiative = 7 }
            };
        }

        public void WinCombat(string enemyId)
        {
            if (!IsVisited(enemyId))
            {
                MarkVisited(enemyId);
                State.gold += enemyId == "enemy_orc_boss" ? 700 : 350;
                State.wood += enemyId == "enemy_bandits" ? 20 : 5;
                State.stone += enemyId == "enemy_orc_boss" ? 20 : 5;
                State.heroExp += enemyId == "enemy_orc_boss" ? 220 : 100;
                CheckLevelUp();
            }

            LastMessage = "Победа! Получена награда, карта обновлена.";
            SaveGame();
        }

        public void LoseCombat()
        {
            State.army.Clear();
            LastMessage = "Поражение. Герой вернулся на базу без армии. Можно загрузить сохранение или нанять войска.";
            SaveGame();
        }

        private void CheckLevelUp()
        {
            var needed = State.heroLevel * 120;
            while (State.heroExp >= needed)
            {
                State.heroExp -= needed;
                State.heroLevel++;
                State.mana += 5;
                needed = State.heroLevel * 120;
            }
        }

        public void EndTurn()
        {
            State.day++;
            State.movement = 20 + State.heroLevel;
            if (State.day > 1 && (State.day - 1) % 7 == 0)
            {
                State.week++;
                foreach (var b in State.buildings)
                {
                    b.available += b.weeklyGrowth + Mathf.Max(0, b.level - 1) * 2;
                }
                LastMessage = "Новая неделя: доступный найм пополнен.";
            }
            else
            {
                LastMessage = "Ход завершён. День " + State.day + ".";
            }
            SaveGame();
        }

        public bool Recruit(BuildingState building)
        {
            if (building == null || building.available <= 0)
            {
                LastMessage = "Нет доступных юнитов для найма.";
                return false;
            }

            var totalCost = building.available * building.recruitGoldCost;
            if (State.gold < totalCost)
            {
                LastMessage = "Недостаточно золота. Нужно: " + totalCost;
                return false;
            }

            State.gold -= totalCost;
            AddUnits(building.unitId, building.available, building.level);
            LastMessage = $"Нанято: {building.available} юнитов из постройки {building.name}.";
            building.available = 0;
            SaveGame();
            return true;
        }

        public bool Upgrade(BuildingState building)
        {
            if (building == null) return false;
            if (building.level >= 2)
            {
                LastMessage = "Постройка уже улучшена до максимального уровня MVP.";
                return false;
            }
            if (State.gold < building.upgradeGoldCost || State.wood < building.upgradeWoodCost)
            {
                LastMessage = "Недостаточно ресурсов для улучшения.";
                return false;
            }

            State.gold -= building.upgradeGoldCost;
            State.wood -= building.upgradeWoodCost;
            building.level++;
            building.weeklyGrowth += 2;
            foreach (var stack in State.army.Where(a => a.id == building.unitId))
            {
                stack.hp += 5;
                stack.attack += 2;
                stack.defense += 1;
            }
            LastMessage = "Постройка улучшена: " + building.name + ". Старые юниты усилены.";
            SaveGame();
            return true;
        }

        private void AddUnits(string unitId, int count, int level)
        {
            var stack = State.army.FirstOrDefault(a => a.id == unitId);
            if (stack == null)
            {
                stack = CreateUnit(unitId, 0, level);
                State.army.Add(stack);
            }
            stack.count += count;
        }

        private UnitStack CreateUnit(string unitId, int count, int level)
        {
            if (unitId == "unit_archer")
            {
                return new UnitStack { id = unitId, name = level > 1 ? "Снайперы" : "Лучники", count = count, hp = 18 + level * 4, attack = 5 + level * 2, defense = 2 + level, initiative = 10 };
            }

            return new UnitStack { id = "unit_swordsman", name = level > 1 ? "Рыцари" : "Мечники", count = count, hp = 30 + level * 5, attack = 7 + level * 2, defense = 4 + level, initiative = 8 };
        }
    }

    public sealed class TheHeroSceneBootstrap : MonoBehaviour
    {
        public SceneKind sceneKind;
        private Font _font;
        private Canvas _canvas;

        private void Start()
        {
            TheHeroGame.Ensure();
            _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_font == null) _font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            EnsureEventSystem();

            switch (sceneKind)
            {
                case SceneKind.MainMenu:
                    BuildMainMenu();
                    break;
                case SceneKind.Map:
                    BuildMap();
                    break;
                case SceneKind.Combat:
                    BuildCombat();
                    break;
                case SceneKind.Base:
                    BuildBase();
                    break;
            }
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() != null) return;
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        private Canvas CreateCanvas(string name)
        {
            var go = new GameObject(name, typeof(RectTransform));
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return _canvas;
        }

        private GameObject Panel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = go.AddComponent<Image>();
            image.color = color;
            return go;
        }

        private Text Label(Transform parent, string name, string text, int size, TextAnchor anchor, Color color, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var label = go.AddComponent<Text>();
            label.font = _font;
            label.text = text;
            label.fontSize = size;
            label.alignment = anchor;
            label.color = color;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            return label;
        }

        private Button UiButton(Transform parent, string name, string caption, Vector2 anchorMin, Vector2 anchorMax, Action onClick)
        {
            var go = Panel(parent, name, anchorMin, anchorMax, new Color(0.16f, 0.18f, 0.24f, 0.95f));
            var btn = go.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = new Color(0.16f, 0.18f, 0.24f, 0.95f);
            colors.highlightedColor = new Color(0.32f, 0.28f, 0.14f, 1f);
            colors.pressedColor = new Color(0.45f, 0.32f, 0.1f, 1f);
            btn.colors = colors;
            Label(go.transform, "Text", caption, 26, TextAnchor.MiddleCenter, new Color(1f, 0.88f, 0.55f), Vector2.zero, Vector2.one);
            btn.onClick.AddListener(() => onClick?.Invoke());
            return btn;
        }

        private void BuildMainMenu()
        {
            CreateCanvas("MainMenuCanvas");
            Panel(_canvas.transform, "Background", Vector2.zero, Vector2.one, new Color(0.03f, 0.04f, 0.08f));
            Label(_canvas.transform, "Title", "THE HERO", 72, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f), new Vector2(0.2f, 0.75f), new Vector2(0.8f, 0.92f));
            Label(_canvas.transform, "Subtitle", "Пошаговая стратегия / тактика", 28, TextAnchor.MiddleCenter, Color.white, new Vector2(0.25f, 0.68f), new Vector2(0.75f, 0.75f));

            UiButton(_canvas.transform, "NewGameButton", "Новая игра", new Vector2(0.38f, 0.55f), new Vector2(0.62f, 0.62f), () =>
            {
                TheHeroGame.Instance.NewGame();
                SceneManager.LoadScene("Map");
            });
            UiButton(_canvas.transform, "ContinueButton", "Продолжить", new Vector2(0.38f, 0.45f), new Vector2(0.62f, 0.52f), () =>
            {
                TheHeroGame.Instance.LoadGame();
                SceneManager.LoadScene("Map");
            });
            UiButton(_canvas.transform, "SettingsButton", "Настройки: звук / язык", new Vector2(0.38f, 0.35f), new Vector2(0.62f, 0.42f), () =>
            {
                TheHeroGame.Instance.LastMessage = "MVP: звук и локализация подготовлены. Громкость можно доработать через AudioMixer.";
                BuildInfoPopup("Настройки", TheHeroGame.Instance.LastMessage);
            });
            UiButton(_canvas.transform, "ExitButton", "Выход", new Vector2(0.38f, 0.25f), new Vector2(0.62f, 0.32f), Application.Quit);

            Label(_canvas.transform, "Hint", "Сохранение: " + Application.persistentDataPath, 16, TextAnchor.LowerCenter, new Color(0.7f, 0.75f, 0.82f), new Vector2(0.05f, 0.02f), new Vector2(0.95f, 0.08f));
        }

        private void BuildInfoPopup(string title, string body)
        {
            var popup = Panel(_canvas.transform, "InfoWindow", new Vector2(0.28f, 0.32f), new Vector2(0.72f, 0.68f), new Color(0.02f, 0.02f, 0.03f, 0.98f));
            Label(popup.transform, "PopupTitle", title, 34, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.35f), new Vector2(0.05f, 0.72f), new Vector2(0.95f, 0.95f));
            Label(popup.transform, "PopupBody", body, 22, TextAnchor.MiddleCenter, Color.white, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.72f));
            UiButton(popup.transform, "CloseButton", "OK", new Vector2(0.36f, 0.08f), new Vector2(0.64f, 0.22f), () => Destroy(popup));
        }

        private void BuildMap()
        {
            var game = TheHeroGame.Instance;
            CreateCanvas("MapCanvas");
            Panel(_canvas.transform, "MapBackground", Vector2.zero, Vector2.one, new Color(0.05f, 0.10f, 0.07f));
            BuildGrid(_canvas.transform);

            var hud = Panel(_canvas.transform, "HUD", new Vector2(0f, 0.91f), new Vector2(1f, 1f), new Color(0.03f, 0.04f, 0.06f, 0.88f));
            Label(hud.transform, "Resources", ResourceLine(), 26, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one);

            var heroPanel = Panel(_canvas.transform, "HeroInfo", new Vector2(0.76f, 0.18f), new Vector2(0.98f, 0.88f), new Color(0.02f, 0.03f, 0.05f, 0.86f));
            Label(heroPanel.transform, "HeroText", HeroInfoText(), 22, TextAnchor.UpperLeft, Color.white, new Vector2(0.06f, 0.06f), new Vector2(0.94f, 0.94f));

            Label(_canvas.transform, "Message", game.LastMessage, 22, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.65f), new Vector2(0.17f, 0.04f), new Vector2(0.74f, 0.13f));

            MapObjectButton("MineButton", game.IsVisited("mine_gold") ? "Шахта очищена" : "Шахта\n+ золото/камень", 0.18f, 0.58f, () => { game.CollectObject("mine_gold"); SceneManager.LoadScene("Map"); }, game.IsVisited("mine_gold"));
            MapObjectButton("ForestCacheButton", game.IsVisited("forest_cache") ? "Тайник пуст" : "Лесной тайник\n+ дерево", 0.42f, 0.70f, () => { game.CollectObject("forest_cache"); SceneManager.LoadScene("Map"); }, game.IsVisited("forest_cache"));
            MapObjectButton("ManaSpringButton", game.IsVisited("mana_spring") ? "Источник пуст" : "Источник маны\n+ опыт", 0.56f, 0.43f, () => { game.CollectObject("mana_spring"); SceneManager.LoadScene("Map"); }, game.IsVisited("mana_spring"));
            MapObjectButton("EnemyOrcsButton", game.IsVisited("enemy_orcs") ? "Орки побеждены" : "Орки\nБОЙ", 0.33f, 0.34f, () => game.EnterCombat("enemy_orcs"), game.IsVisited("enemy_orcs"));
            MapObjectButton("EnemyBanditsButton", game.IsVisited("enemy_bandits") ? "Разбойники побеждены" : "Разбойники\nБОЙ", 0.62f, 0.64f, () => game.EnterCombat("enemy_bandits"), game.IsVisited("enemy_bandits"));
            MapObjectButton("BossButton", game.IsVisited("enemy_orc_boss") ? "Босс побежден" : "Вождь орков\nБОЙ", 0.68f, 0.28f, () => game.EnterCombat("enemy_orc_boss"), game.IsVisited("enemy_orc_boss"));

            UiButton(_canvas.transform, "BaseButton", "База / найм", new Vector2(0.02f, 0.80f), new Vector2(0.16f, 0.88f), () => SceneManager.LoadScene("Base"));
            UiButton(_canvas.transform, "SaveButton", "Сохранить", new Vector2(0.02f, 0.70f), new Vector2(0.16f, 0.78f), () => { game.SaveGame(); game.LastMessage = "Игра сохранена."; SceneManager.LoadScene("Map"); });
            UiButton(_canvas.transform, "EndTurnButton", "Завершить ход", new Vector2(0.78f, 0.04f), new Vector2(0.97f, 0.12f), () => { game.EndTurn(); SceneManager.LoadScene("Map"); });
        }

        private string ResourceLine()
        {
            var s = TheHeroGame.Instance.State;
            return $"💰 {s.gold}     🪵 {s.wood}     🪨 {s.stone}     ✨ {s.mana}     День {s.day} / Неделя {s.week}";
        }

        private string HeroInfoText()
        {
            var s = TheHeroGame.Instance.State;
            var army = s.army.Count == 0 ? "нет армии" : string.Join("\n", s.army.Select(a => $"• {a.name}: x{a.count}  HP:{a.hp} ATK:{a.attack}"));
            return $"Герой: Артур\nУровень: {s.heroLevel}\nОпыт: {s.heroExp}/{s.heroLevel * 120}\nОчки движения: {s.movement}\n\nАрмия:\n{army}";
        }

        private void BuildGrid(Transform parent)
        {
            var area = Panel(parent, "GridArea", new Vector2(0.17f, 0.15f), new Vector2(0.74f, 0.88f), new Color(0.09f, 0.16f, 0.10f, 0.60f));
            for (var y = 0; y < 8; y++)
            {
                for (var x = 0; x < 12; x++)
                {
                    var c = ((x + y) % 2 == 0) ? new Color(0.10f, 0.22f, 0.12f, 0.55f) : new Color(0.08f, 0.17f, 0.10f, 0.55f);
                    if (y == 3 || x == 5) c = new Color(0.28f, 0.20f, 0.10f, 0.55f);
                    Panel(area.transform, "Tile_" + x + "_" + y, new Vector2(x / 12f, y / 8f), new Vector2((x + 1) / 12f, (y + 1) / 8f), c);
                }
            }
            Label(area.transform, "HeroMarker", "♞", 48, TextAnchor.MiddleCenter, new Color(0.3f, 0.7f, 1f), new Vector2(0.47f, 0.42f), new Vector2(0.54f, 0.53f));
        }

        private void MapObjectButton(string name, string caption, float x, float y, Action action, bool disabled)
        {
            var button = UiButton(_canvas.transform, name, caption, new Vector2(x, y), new Vector2(x + 0.12f, y + 0.09f), action);
            if (disabled)
            {
                button.interactable = false;
                button.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.18f, 0.8f);
            }
        }

        private void BuildCombat()
        {
            var game = TheHeroGame.Instance;
            var enemyArmy = game.CreateEnemyArmy(game.PendingEnemyId);
            var combat = new GameObject("CombatController").AddComponent<TheHeroCombatController>();
            combat.Init(enemyArmy, RebuildCombatScreen);
            RebuildCombatScreen(combat);
        }

        private void RebuildCombatScreen(TheHeroCombatController combat)
        {
            if (_canvas != null) Destroy(_canvas.gameObject);
            CreateCanvas("CombatCanvas");
            Panel(_canvas.transform, "Background", Vector2.zero, Vector2.one, new Color(0.08f, 0.04f, 0.04f));
            Label(_canvas.transform, "Title", "БОЙ", 52, TextAnchor.MiddleCenter, new Color(1f, 0.78f, 0.35f), new Vector2(0.35f, 0.88f), new Vector2(0.65f, 0.98f));

            var player = Panel(_canvas.transform, "PlayerSide", new Vector2(0.04f, 0.20f), new Vector2(0.43f, 0.84f), new Color(0.02f, 0.04f, 0.08f, 0.9f));
            Label(player.transform, "PlayerLabel", "Твои отряды", 30, TextAnchor.UpperCenter, new Color(0.55f, 0.8f, 1f), new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.98f));
            var y = 0.66f;
            foreach (var unit in TheHeroGame.Instance.State.army)
            {
                Label(player.transform, unit.id, $"{unit.name}  x{unit.count}\nHP:{unit.hp} ATK:{unit.attack} DEF:{unit.defense}", 22, TextAnchor.MiddleLeft, Color.white, new Vector2(0.08f, y), new Vector2(0.92f, y + 0.14f));
                y -= 0.16f;
            }

            var enemy = Panel(_canvas.transform, "EnemySide", new Vector2(0.57f, 0.20f), new Vector2(0.96f, 0.84f), new Color(0.08f, 0.02f, 0.02f, 0.9f));
            Label(enemy.transform, "EnemyLabel", "Враги", 30, TextAnchor.UpperCenter, new Color(1f, 0.45f, 0.38f), new Vector2(0.02f, 0.82f), new Vector2(0.98f, 0.98f));
            y = 0.66f;
            for (var i = 0; i < combat.Enemies.Count; i++)
            {
                var index = i;
                var e = combat.Enemies[i];
                var b = UiButton(enemy.transform, "Enemy_" + i, $"{e.name} x{e.count}\nАтаковать", new Vector2(0.08f, y), new Vector2(0.92f, y + 0.14f), () => combat.PlayerAttack(index));
                b.interactable = e.count > 0 && !combat.IsFinished;
                y -= 0.16f;
            }

            var log = Panel(_canvas.transform, "LogPanel", new Vector2(0.25f, 0.04f), new Vector2(0.75f, 0.17f), new Color(0.02f, 0.02f, 0.02f, 0.9f));
            Label(log.transform, "CombatLogText", combat.LogText, 20, TextAnchor.MiddleCenter, Color.white, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.98f));

            if (combat.IsFinished)
            {
                if (combat.PlayerWon)
                {
                    UiButton(_canvas.transform, "RewardButton", "Получить награду и вернуться на карту", new Vector2(0.34f, 0.42f), new Vector2(0.66f, 0.50f), () =>
                    {
                        TheHeroGame.Instance.WinCombat(TheHeroGame.Instance.PendingEnemyId);
                        SceneManager.LoadScene("Map");
                    });
                }
                else
                {
                    UiButton(_canvas.transform, "LoseLoadButton", "Загрузить сохранение", new Vector2(0.36f, 0.48f), new Vector2(0.64f, 0.56f), () => { TheHeroGame.Instance.LoadGame(); SceneManager.LoadScene("Map"); });
                    UiButton(_canvas.transform, "LoseBaseButton", "Воскреснуть на базе", new Vector2(0.36f, 0.38f), new Vector2(0.64f, 0.46f), () => { TheHeroGame.Instance.LoseCombat(); SceneManager.LoadScene("Base"); });
                }
            }
            else
            {
                UiButton(_canvas.transform, "SkipButton", "Пропустить ход", new Vector2(0.42f, 0.20f), new Vector2(0.58f, 0.28f), combat.EnemyRound);
                UiButton(_canvas.transform, "RunButton", "Отступить на карту", new Vector2(0.42f, 0.30f), new Vector2(0.58f, 0.38f), () => SceneManager.LoadScene("Map"));
            }
        }

        private void BuildBase()
        {
            var game = TheHeroGame.Instance;
            CreateCanvas("BaseCanvas");
            Panel(_canvas.transform, "Background", Vector2.zero, Vector2.one, new Color(0.06f, 0.05f, 0.08f));
            Label(_canvas.transform, "Title", "ЗАМОК", 56, TextAnchor.MiddleCenter, new Color(1f, 0.82f, 0.35f), new Vector2(0.33f, 0.87f), new Vector2(0.67f, 0.98f));
            Label(_canvas.transform, "Resources", ResourceLine(), 24, TextAnchor.MiddleCenter, Color.white, new Vector2(0.1f, 0.78f), new Vector2(0.9f, 0.86f));
            Label(_canvas.transform, "Message", game.LastMessage, 22, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.65f), new Vector2(0.12f, 0.08f), new Vector2(0.88f, 0.16f));

            var y = 0.58f;
            foreach (var b in game.State.buildings)
            {
                var panel = Panel(_canvas.transform, "Building_" + b.id, new Vector2(0.18f, y), new Vector2(0.82f, y + 0.16f), new Color(0.02f, 0.03f, 0.05f, 0.9f));
                Label(panel.transform, "Info", $"{b.name}\nУровень: {b.level}   Доступно: {b.available}\nНайм: {b.recruitGoldCost} золота за юнита   Улучшение: {b.upgradeGoldCost} золота / {b.upgradeWoodCost} дерева", 22, TextAnchor.MiddleLeft, Color.white, new Vector2(0.03f, 0.08f), new Vector2(0.62f, 0.92f));
                var captured = b;
                UiButton(panel.transform, "RecruitButton", "Нанять всех", new Vector2(0.66f, 0.55f), new Vector2(0.96f, 0.88f), () => { game.Recruit(captured); SceneManager.LoadScene("Base"); });
                UiButton(panel.transform, "UpgradeButton", "Улучшить", new Vector2(0.66f, 0.12f), new Vector2(0.96f, 0.45f), () => { game.Upgrade(captured); SceneManager.LoadScene("Base"); });
                y -= 0.20f;
            }

            UiButton(_canvas.transform, "ToMapButton", "На карту", new Vector2(0.42f, 0.20f), new Vector2(0.58f, 0.28f), () => SceneManager.LoadScene("Map"));
        }
    }

    public sealed class TheHeroCombatController : MonoBehaviour
    {
        public List<UnitStack> Enemies { get; private set; }
        public string LogText { get; private set; }
        public bool IsFinished { get; private set; }
        public bool PlayerWon { get; private set; }
        private int _activePlayerIndex;
        private Action<TheHeroCombatController> _redraw;

        public void Init(List<UnitStack> enemies, Action<TheHeroCombatController> redraw)
        {
            Enemies = enemies.Select(e => e.Clone()).ToList();
            _redraw = redraw;
            _activePlayerIndex = 0;
            LogText = "Выберите цель для атаки. Ходит первый живой отряд игрока.";
            NormalizeArmy();
        }

        public void PlayerAttack(int enemyIndex)
        {
            if (IsFinished) return;
            NormalizeArmy();
            var army = TheHeroGame.Instance.State.army;
            if (army.Count == 0)
            {
                Finish(false);
                return;
            }
            if (_activePlayerIndex >= army.Count) _activePlayerIndex = 0;
            if (enemyIndex < 0 || enemyIndex >= Enemies.Count || Enemies[enemyIndex].count <= 0) return;

            var attacker = army[_activePlayerIndex];
            var target = Enemies[enemyIndex];
            var damage = CalculateDamage(attacker, target);
            ApplyDamage(target, damage);
            LogText = $"{attacker.name} атакуют {target.name}: урон {damage}.";
            NormalizeEnemies();

            if (Enemies.Count == 0)
            {
                Finish(true);
                _redraw?.Invoke(this);
                return;
            }

            _activePlayerIndex++;
            if (_activePlayerIndex >= army.Count)
            {
                _activePlayerIndex = 0;
                EnemyRound();
                return;
            }
            _redraw?.Invoke(this);
        }

        public void EnemyRound()
        {
            if (IsFinished) return;
            NormalizeArmy();
            NormalizeEnemies();
            var army = TheHeroGame.Instance.State.army;
            if (army.Count == 0)
            {
                Finish(false);
                _redraw?.Invoke(this);
                return;
            }
            if (Enemies.Count == 0)
            {
                Finish(true);
                _redraw?.Invoke(this);
                return;
            }

            var events = new List<string>();
            foreach (var enemy in Enemies.ToList())
            {
                if (army.Count == 0) break;
                var target = army[Random.Range(0, army.Count)];
                var damage = CalculateDamage(enemy, target);
                ApplyDamage(target, damage);
                events.Add($"{enemy.name} бьют {target.name}: {damage}");
                NormalizeArmy();
            }

            LogText = string.Join(" | ", events);
            if (army.Count == 0) Finish(false);
            _redraw?.Invoke(this);
        }

        private int CalculateDamage(UnitStack attacker, UnitStack target)
        {
            return Mathf.Max(1, attacker.attack + attacker.count / 2 - target.defense / 2) * Mathf.Max(1, attacker.count / 3);
        }

        private void ApplyDamage(UnitStack target, int damage)
        {
            var killed = Mathf.Max(1, damage / Mathf.Max(1, target.hp));
            target.count = Mathf.Max(0, target.count - killed);
        }

        private void NormalizeArmy()
        {
            TheHeroGame.Instance.State.army = TheHeroGame.Instance.State.army.Where(a => a.count > 0).ToList();
        }

        private void NormalizeEnemies()
        {
            Enemies = Enemies.Where(e => e.count > 0).ToList();
        }

        private void Finish(bool playerWon)
        {
            IsFinished = true;
            PlayerWon = playerWon;
            LogText = playerWon ? "Победа. Нажмите кнопку награды." : "Поражение. Выберите дальнейшее действие.";
        }
    }
}
