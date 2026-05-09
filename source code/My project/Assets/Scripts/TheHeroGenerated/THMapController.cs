using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TheHero.Generated
{
    public class THMapController : MonoBehaviour
    {
        public static THMapController Instance { get; private set; }
        public THGameState State => THManager.Instance.Data;
        public THHeroMover HeroMover;

        [Header("UI References")]
        public Text GoldText;
        public Text WoodText;
        public Text StoneText;
        public Text ManaText;
        public Text DayText;
        public Text HeroText;
        public Text LevelText;
        public Text MoveText;
        public Text ArmyText;
        public Text InfoText;

        private bool _isTransitioning = false;

        private void Awake()
        {
            Instance = this;
            _isTransitioning = false;
        }

        private void Start()
        {
            if (HeroMover != null) HeroMover.SetPositionImmediate((int)State.heroX, (int)State.heroY);
            UpdateUI();
            Log("Добро пожаловать!");
            
            // Auto-wire buttons if they exist
            WireButton("SaveButton", SaveGame);
            WireButton("LoadButton", LoadGame);
            WireButton("EndTurnButton", EndTurn);
            WireButton("MenuButton", OpenPauseMenu);
            WireButton("BaseButton", GoToBase);
        }

        private void WireButton(string name, UnityEngine.Events.UnityAction action)
        {
            var btn = GameObject.Find(name)?.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
            }
        }

        public void SaveGame() => THSaveSystem.SaveGame(State);
        public void LoadGame()
        {
            var data = THSaveSystem.LoadGame();
            if (data != null)
            {
                THManager.Instance.Data = data;
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void OpenPauseMenu() => THPauseMenu.Instance.Pause();
        public void GoToBase() 
        {
            THSaveSystem.SaveGame(State);
            THSceneLoader.Instance.LoadBase();
        }

        public void UpdateUI()
        {
            if (GoldText) GoldText.text = $"Gold: {State.gold}";
            if (WoodText) WoodText.text = $"Wood: {State.wood}";
            if (StoneText) StoneText.text = $"Stone: {State.stone}";
            if (ManaText) ManaText.text = $"Mana: {State.mana}";
            if (DayText) DayText.text = $"Day: {State.day} / Week: {State.week}";
            if (HeroText) HeroText.text = $"Hero: {State.heroName}";
            if (LevelText) LevelText.text = $"Level: {State.heroLevel}";
            if (MoveText) MoveText.text = $"Moves: {State.movementPoints}";
            if (ArmyText) ArmyText.text = $"Army: {State.army.Sum(u => u.count)}";

            if (State.gameCompleted)
            {
                Log("Тёмный Лорд повержен! Победа!");
            }

            if (THQuestSystem.Instance) THQuestSystem.Instance.UpdateUI();
        }

        public void EndTurn()
        {
            if (State.gameCompleted || _isTransitioning) return;

            State.day++;
            State.daysPassed++;
            if (State.day > 7)
            {
                State.day = 1;
                State.week++;
                
                foreach(var b in State.buildings)
                {
                    b.recruitsAvailable += (b.id == "mage" ? 2 : (b.id == "range" ? 4 : 5));
                }
                THMessageSystem.Instance.ShowMessage("Новая неделя: найм пополнен");
            }

            State.movementPoints = State.maxMovementPoints;
            
            if (State.capturedObjectIds.Count > 0)
            {
                int income = State.capturedObjectIds.Count * 50;
                State.gold += income;
                Log($"Новый день. Доход: {income} золота.");
            }
            else
            {
                Log("Новый день настал.");
            }

            THSaveSystem.SaveGame(State);
            UpdateUI();
        }

        public void Log(string msg)
        {
            if (InfoText) InfoText.text = msg;
            Debug.Log("[TH] " + msg);
        }

        public void HandleObjectInteraction(THMapObject obj)
        {
            if (State.gameCompleted || _isTransitioning) return;
            if (State.collectedObjectIds.Contains(obj.id) || State.defeatedEnemyIds.Contains(obj.id)) return;

            switch (obj.type)
            {
                case THMapObject.ObjectType.GoldResource:
                    State.gold += obj.rewardGold > 0 ? obj.rewardGold : 100;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Золото собрано");
                    break;
                case THMapObject.ObjectType.WoodResource:
                    State.wood += obj.rewardWood > 0 ? obj.rewardWood : 10;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Дерево собрано");
                    break;
                case THMapObject.ObjectType.StoneResource:
                    State.stone += obj.rewardStone > 0 ? obj.rewardStone : 8;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Камень собран");
                    break;
                case THMapObject.ObjectType.ManaResource:
                    State.mana += obj.rewardMana > 0 ? obj.rewardMana : 5;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Мана собрана");
                    break;
                case THMapObject.ObjectType.Treasure:
                    State.gold += 200;
                    GainExp(50);
                    State.collectedObjectIds.Add(obj.id);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Сокровище найдено!");
                    break;
                case THMapObject.ObjectType.Shrine:
                    if (!State.visitedShrineIds.Contains(obj.id))
                    {
                        GainExp(100);
                        State.visitedShrineIds.Add(obj.id);
                        obj.SetVisited();
                        THMessageSystem.Instance.ShowSuccess("Святилище посещено");
                    }
                    break;
                case THMapObject.ObjectType.Mine:
                    if (!State.capturedObjectIds.Contains(obj.id))
                    {
                        State.gold += 150;
                        State.capturedObjectIds.Add(obj.id);
                        obj.SetCaptured();
                        THMessageSystem.Instance.ShowSuccess("Шахта захвачена!");
                    }
                    break;
                case THMapObject.ObjectType.Base:
                    _isTransitioning = true;
                    THSaveSystem.SaveGame(State);
                    THSceneLoader.Instance.LoadBase();
                    break;
                case THMapObject.ObjectType.Enemy:
                    if (!State.defeatedEnemyIds.Contains(obj.id))
                    {
                        _isTransitioning = true;
                        State.lastEnemyId = obj.id;
                        State.currentEnemyArmy = obj.enemyArmy.Select(u => u.Clone()).ToList();
                        PlayerPrefs.SetInt("Combat_DarkLord", obj.isDarkLord ? 1 : 0);
                        THSaveSystem.SaveGame(State);
                        THSceneLoader.Instance.LoadCombat();
                    }
                    break;
            }
            UpdateUI();
            THSaveSystem.SaveGame(State);
        }

        public void GainExp(int amount)
        {
            State.heroExp += amount;
            int nextLevelExp = 100 * State.heroLevel;
            if (State.heroExp >= nextLevelExp)
            {
                State.heroLevel++;
                State.heroExp -= nextLevelExp;
                State.maxMovementPoints += 2;
                State.movementPoints = State.maxMovementPoints;
                THMessageSystem.Instance.ShowSuccess($"Новый уровень: {State.heroLevel}");
            }
            UpdateUI();
            THSaveSystem.SaveGame(State);
        }
    }
}
