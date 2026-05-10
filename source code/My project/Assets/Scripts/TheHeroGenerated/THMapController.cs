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
        public THStrictGridHeroMovement HeroMover;

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
            ApplyPersistence();
            
            bool isNewGame = State.daysPassed == 0 && State.collectedObjectIds.Count == 0 && State.defeatedEnemyIds.Count == 0;
            if (isNewGame)
            {
                Debug.Log("[TheHeroMap] New game map state applied");
                // Position should already be set in NewGame(), but just in case:
                if (State.heroX == 0 && State.heroY == 0) { State.heroX = 4; State.heroY = 10; }
            }

            // Stable hero position
            if (HeroMover != null)
            {
                HeroMover.SetPositionImmediate((int)State.heroX, (int)State.heroY);
            }
            
            // Camera setup
            Camera cam = Camera.main;
            if (cam != null && HeroMover != null)
            {
                var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
                follow.Target = HeroMover.transform;
                follow.MinBounds = new Vector2(-12, -8);
                follow.MaxBounds = new Vector2(12, 8);
                follow.SmoothSpeed = 10f;
                
                // Immediate focus on start
                cam.transform.position = new Vector3(HeroMover.transform.position.x, HeroMover.transform.position.y, cam.transform.position.z);
            }

            UpdateUI();
            Log("Добро пожаловать!");
            
            // Auto-wire buttons
            WireButton("SaveButton", SaveGame);
            WireButton("LoadButton", LoadGame);
            WireButton("EndTurnButton", EndTurn);
            WireButton("MenuButton", OpenPauseMenu);
            WireButton("BaseButton", GoToBase);
            WireButton("CastleButton", GoToBase);

            // Final Victory Check
            if (State.gameCompleted)
            {
                ShowFinalVictory();
            }
            // Trigger Start Dialog (only if not completed)
            else if (THStoryManager.Instance != null && !State.shownDialogueIds.Contains("intro"))
            {
                THStoryManager.Instance.ShowDialog("intro", "Начало пути", "Королевство пало во тьму. Соберите армию и победите Тёмного Лорда.", "Sprites/Units/unit_swordsman_portrait");
                State.shownDialogueIds.Add("intro");
                THSaveSystem.SaveGame(State);
            }
        }

        private void ShowFinalVictory()
        {
            if (THStoryManager.Instance != null)
            {
                THStoryManager.Instance.ShowVictoryDialog("ПОБЕДА!", 
                    "Тёмный Лорд повержен! Земли The Hero наконец освобождены от вечной тьмы.\n\nКоролевство спасено, а ваше имя навсегда войдёт в легенды.\n\nПоздравляем с завершением кампании!", 
                    "Sprites/Units/unit_swordsman_portrait");
            }
            Log("Кампания завершена! Победа!");
        }

        private void ApplyPersistence()
        {
            var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            int activeResources = 0;
            int activeEnemies = 0;

            foreach (var obj in mapObjects)
            {
                bool isCollected = State.collectedObjectIds.Contains(obj.id) || State.defeatedEnemyIds.Contains(obj.id);
                if (isCollected)
                {
                    obj.gameObject.SetActive(false);
                }
                else
                {
                    obj.gameObject.SetActive(true);
                    if (obj.type == THMapObject.ObjectType.GoldResource || obj.type == THMapObject.ObjectType.WoodResource || 
                        obj.type == THMapObject.ObjectType.StoneResource || obj.type == THMapObject.ObjectType.ManaResource) 
                        activeResources++;
                    if (obj.type == THMapObject.ObjectType.Enemy) 
                        activeEnemies++;

                    if (State.capturedObjectIds.Contains(obj.id))
                    {
                        obj.SetCaptured();
                    }
                    else if (State.visitedShrineIds.Contains(obj.id))
                    {
                        obj.SetVisited();
                    }
                    else
                    {
                        // Reset to neutral if not in captured/visited (for New Game reset)
                        // Note: Some objects might not have reset logic in their script yet.
                    }
                }
            }
            
            Debug.Log($"[TheHeroMap] Resources active: {activeResources}");
            Debug.Log($"[TheHeroMap] Enemies active: {activeEnemies}");
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

        public void SaveGame() => THSavePolicy.ManualSave();
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
            THSavePolicy.ManualSave();
            THSceneLoader.Instance.LoadBase();
        }

        private bool _outOfMovesShown = false;

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

            if (State.movementPoints <= 0 && !State.gameCompleted && !_isTransitioning && !_outOfMovesShown)
            {
                _outOfMovesShown = true;
                if (THStoryManager.Instance != null)
                    THStoryManager.Instance.ShowNotification("Усталость", "Ходы закончились. Нажмите 'Завершить ход', чтобы восстановить силы.", "Sprites/Units/unit_swordsman_portrait");
            }
            
            if (State.movementPoints > 0) _outOfMovesShown = false;

            if (State.gameCompleted)
            {
                Log("Тёмный Лорд повержен! Победа!");
            }

            if (THQuestSystem.Instance) THQuestSystem.Instance.UpdateUI();
        }

        public void EndTurn()
        {
            if (State.gameCompleted || _isTransitioning) return;

            int prevWeek = State.week;
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

                if (THWeeklyIncomeSystem.Instance != null)
                {
                    THWeeklyIncomeSystem.Instance.ApplyWeeklyIncome();
                }
                else
                {
                    THMessageSystem.Instance.ShowMessage("Новая неделя: найм пополнен");
                }
            }

            // Recalculate max movement in case artifacts were added
            int bonusMove = THArtifactManager.Instance.GetTotalMoveBonus(State.heroArtifactIds);
            State.maxMovementPoints = 20 + (State.heroLevel - 1) * 2 + bonusMove;
            State.movementPoints = State.maxMovementPoints;
            
            int artifactMana = THArtifactManager.Instance.GetTotalManaIncome(State.heroArtifactIds);
            if (artifactMana > 0)
            {
                State.mana += artifactMana;
                Log($"Artifact income: {artifactMana} mana.");
            }

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

            // ONLY SAVE IF WEEK CHANGED
            THSavePolicy.SaveOnNewWeek(prevWeek, State.week);

            if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("end_turn");
            UpdateUI();
        }

        public void Log(string msg)
        {
            if (InfoText) InfoText.text = msg;
            Debug.Log("[TH] " + msg);
        }

        public void ShowConfirmation(string message, UnityEngine.Events.UnityAction onConfirm)
        {
            var panel = GameObject.Find("ConfirmationPanel");
            if (panel != null)
            {
                panel.SetActive(true);
                var title = panel.transform.Find("Title")?.GetComponent<Text>();
                if (title != null) title.text = message;

                var yesBtn = panel.transform.Find("Да")?.GetComponent<Button>();
                if (yesBtn != null)
                {
                    yesBtn.onClick.RemoveAllListeners();
                    yesBtn.onClick.AddListener(() => {
                        panel.SetActive(false);
                        onConfirm?.Invoke();
                    });
                }

                var noBtn = panel.transform.Find("Нет")?.GetComponent<Button>();
                if (noBtn != null)
                {
                    noBtn.onClick.RemoveAllListeners();
                    noBtn.onClick.AddListener(() => panel.SetActive(false));
                }
            }
            else
            {
                Debug.LogWarning("ConfirmationPanel not found. Auto-confirming.");
                onConfirm?.Invoke();
            }
        }

        public void HandleObjectInteraction(THMapObject obj)
        {
            if (THSingleMapHoverLabel.Instance != null) THSingleMapHoverLabel.Instance.Hide();

            if (State.gameCompleted || _isTransitioning) return;
if (State.collectedObjectIds.Contains(obj.id) || State.defeatedEnemyIds.Contains(obj.id)) return;

            switch (obj.type)
            {
                case THMapObject.ObjectType.GoldResource:
                case THMapObject.ObjectType.WoodResource:
                case THMapObject.ObjectType.StoneResource:
                case THMapObject.ObjectType.ManaResource:
                case THMapObject.ObjectType.Treasure:
                    if (THStoryManager.Instance != null)
                        THStoryManager.Instance.ShowDialog("first_resource", "Ресурсы", "Ресурсы нужны для найма войск и развития замка.", "Sprites/Units/unit_swordsman_portrait");
                    break;
                case THMapObject.ObjectType.Enemy:
                    if (!obj.isDarkLord && THStoryManager.Instance != null && !State.shownDialogueIds.Contains("first_enemy"))
                    {
                        THStoryManager.Instance.ShowDialog("first_enemy", "Враги", "Враги охраняют дороги. Побеждайте их, чтобы получать опыт и награды.", "Sprites/Units/unit_darkknight_portrait");
                        return; // Stop here to let player read. They click again to enter combat.
                    }
                    
                    if (obj.isDarkLord && THStoryManager.Instance != null && !State.shownDialogueIds.Contains("pre_boss"))
                    {
                         THStoryManager.Instance.ShowDialog("pre_boss", "Финальная битва", "Это последняя битва. Подготовьте армию.", "Sprites/Units/unit_swordsman_portrait");
                         return;
                    }
                    break;
}

            switch (obj.type)
            {
                case THMapObject.ObjectType.GoldResource:
        int g = obj.rewardGold > 0 ? obj.rewardGold : 100;
                    State.gold += g;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    THMapObjectVisuals.SpawnRewardText(obj.transform.position, $"+{g} Gold", Color.yellow);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Золото собрано");
                    if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("resource_collect");
                    break;
        case THMapObject.ObjectType.WoodResource:
                    int w = obj.rewardWood > 0 ? obj.rewardWood : 10;
                    State.wood += w;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    THMapObjectVisuals.SpawnRewardText(obj.transform.position, $"+{w} Wood", Color.green);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Дерево собрано");
                    break;
                case THMapObject.ObjectType.StoneResource:
                    int s = obj.rewardStone > 0 ? obj.rewardStone : 8;
                    State.stone += s;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    THMapObjectVisuals.SpawnRewardText(obj.transform.position, $"+{s} Stone", Color.gray);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Камень собран");
                    break;
                case THMapObject.ObjectType.ManaResource:
                    int m = obj.rewardMana > 0 ? obj.rewardMana : 5;
                    State.mana += m;
                    State.resourcesCollected++;
                    State.collectedObjectIds.Add(obj.id);
                    THMapObjectVisuals.SpawnRewardText(obj.transform.position, $"+{m} Mana", Color.cyan);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Мана собрана");
                    break;
                case THMapObject.ObjectType.Treasure:
                    State.gold += 200;
                    GainExp(50);
                    State.collectedObjectIds.Add(obj.id);
                    THMapObjectVisuals.SpawnRewardText(obj.transform.position, "+200 Gold & 50 XP", Color.yellow);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess("Сокровище найдено!");
                    break;
                case THMapObject.ObjectType.ArtifactChest:
                    var art = THArtifactManager.Instance.GetRandomArtifact(State);
                    if (art != null)
                    {
                        State.heroArtifactIds.Add(art.id);
                        State.collectedObjectIds.Add(obj.id);
                        obj.gameObject.SetActive(false);
                        THMessageSystem.Instance.ShowSuccess($"Found Artifact: {art.name}");
                    }
                    break;
                case THMapObject.ObjectType.Shrine:
                    if (!State.visitedShrineIds.Contains(obj.id))
                    {
                        GainExp(100);
                        State.visitedShrineIds.Add(obj.id);
                        THMapObjectVisuals.SpawnRewardText(obj.transform.position, "+100 XP", Color.magenta);
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
                    THSavePolicy.ManualSave();
                    THSceneLoader.Instance.LoadBase();
                    break;
                case THMapObject.ObjectType.Artifact:
                    State.heroArtifactIds.Add(obj.id);
                    State.collectedObjectIds.Add(obj.id);
                    obj.gameObject.SetActive(false);
                    THMessageSystem.Instance.ShowSuccess($"Найден артефакт: {obj.displayName}");
                    Log($"[TheHeroArtifact] Collected artifact: {obj.displayName}");
                    break;
                case THMapObject.ObjectType.Enemy:
if (!State.defeatedEnemyIds.Contains(obj.id))
                    {
                        _isTransitioning = true;
                        State.lastEnemyId = obj.id;
                        State.currentEnemyArmy = obj.enemyArmy.Select(u => u.Clone()).ToList();
                        PlayerPrefs.SetInt("Combat_DarkLord", obj.isDarkLord ? 1 : 0);
                        THSavePolicy.ManualSave();
                        THSceneLoader.Instance.LoadCombat();
                    }
                    break;
            }
            UpdateUI();
        }

        public void GainExp(int amount)
        {
            if (State == null) return;
            float multiplier = THArtifactManager.Instance.GetTotalExpMultiplier(State.heroArtifactIds);
        State.heroExp += Mathf.RoundToInt(amount * multiplier);
            int nextLevelExp = 100 * State.heroLevel;
            if (State.heroExp >= nextLevelExp)
            {
                State.heroLevel++;
                State.heroExp -= nextLevelExp;
                int bonusMove = THArtifactManager.Instance.GetTotalMoveBonus(State.heroArtifactIds);
                State.maxMovementPoints = 20 + (State.heroLevel - 1) * 2 + bonusMove;
                State.movementPoints = State.maxMovementPoints;
                THMessageSystem.Instance.ShowSuccess($"Новый уровень: {State.heroLevel}");
            }
            UpdateUI();
        }
}
}
