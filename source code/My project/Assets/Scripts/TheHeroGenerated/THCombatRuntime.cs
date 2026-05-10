using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    [System.Serializable]
    public class THCombatUnit
    {
        public string id;
        public string displayName;
        public int count;
        public int maxCount;
        public int hpPerUnit;
        public int attack;
        public int defense;
        public int initiative;
        public bool isPlayer;

        public bool hasActedThisRound;
        public int turnOrderIndex;
        public bool IsAlive => count > 0;
        
        [System.NonSerialized] public GameObject uiCard;
    }

    [System.Serializable]
    public class THCombatReward
    {
        public int gold;
        public int wood;
        public int stone;
        public int mana;
        public int exp;
        public bool isFinalVictory;
    }

    public class THCombatRuntime : MonoBehaviour
    {
        public static THCombatRuntime Instance { get; private set; }

        [Header("UI Containers")]
        public Transform playerUnitsContainer;
        public Transform enemyUnitsContainer;
        
        [Header("UI Labels")]
        public Text infoText;
        public Text roundText;
        public Text logText;
        public Text battleTitleText;

        [Header("Panels")]
        public GameObject resultPanel;
        public Text resultTitleText;
        public Text resultBodyText;
        public Button finishBattleButton;
        public Button mainMenuButton;
        public GameObject actionButtonsRoot;

        [Header("State")]
        public List<THCombatUnit> playerUnits = new List<THCombatUnit>();
        public List<THCombatUnit> enemyUnits = new List<THCombatUnit>();
        public THCombatUnit activeUnit;
        public THCombatUnit selectedEnemyUnit;
        public List<THCombatUnit> turnQueue = new List<THCombatUnit>();
        public int currentTurnIndex = 0;

        private bool combatFinished = false;
        private bool rewardApplied = false;
        private bool isLeavingCombat = false;
        private bool enemyTurnInProgress = false;
        public int round = 1;
        public string enemyId;
        public THCombatReward reward = new THCombatReward();

        [Header("Turn Order UI")]
        public Text activeUnitText;
        public Transform turnOrderContainer;
        public GameObject turnOrderTagTemplate;

        private void Awake()
        {
            Instance = this;
            ConnectButtons();
            ConnectResultButtons();
        }

        private void OnEnable()
        {
            ConnectButtons();
            ConnectResultButtons();
        }

        public void ConnectResultButtons()
        {
            if (resultPanel == null)
            {
                var canvas = Object.FindAnyObjectByType<Canvas>();
                if (canvas != null)
                {
                    var found = canvas.transform.Find("ResultPanel");
                    if (found) resultPanel = found.gameObject;
                }
            }

            if (resultPanel != null)
            {
                if (finishBattleButton == null) finishBattleButton = resultPanel.transform.Find("FinishBattleButton")?.GetComponent<Button>();
                if (mainMenuButton == null) mainMenuButton = resultPanel.transform.Find("MainMenuButton")?.GetComponent<Button>();
            }

            if (finishBattleButton != null)
            {
                finishBattleButton.onClick.RemoveAllListeners();
                finishBattleButton.onClick.AddListener(FinishBattleAndReturnToMap);
                finishBattleButton.interactable = true;
                Debug.Log("[TheHeroCombat] FinishBattleButton connected");
            }

            if (mainMenuButton != null)
            {
                mainMenuButton.onClick.RemoveAllListeners();
                mainMenuButton.onClick.AddListener(ReturnToMainMenu);
                mainMenuButton.interactable = true;
                Debug.Log("[TheHeroCombat] MainMenuButton connected");
            }
        }

        public void FinishBattleAndReturnToMap()
        {
            Debug.Log("[TheHeroCombat] Finish battle clicked");
            if (isLeavingCombat) return;
            isLeavingCombat = true;
            ApplyCombatResultIfNeeded();
            SceneManager.LoadScene("Map");
        }

        public void ReturnToMainMenu()
        {
            Debug.Log("[TheHeroCombat] Returning to MainMenu");
            SceneManager.LoadScene("MainMenu");
        }

        public void OnAutoBattleClicked()
        {
            Debug.Log("[TheHeroCombat] AutoBattle clicked");
            AutoBattle();
        }

        public void OnSkipClicked()
        {
            Debug.Log("[TheHeroCombat] Skip clicked");
            SkipTurn();
        }

        public void ConnectButtons()
{
            if (actionButtonsRoot != null)
            {
                var buttons = actionButtonsRoot.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn.name == "AttackButton") { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(OnAttackClicked); }
                    if (btn.name == "AutoBattleButton") { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(OnAutoBattleClicked); }
                    if (btn.name == "SkipButton") { btn.onClick.RemoveAllListeners(); btn.onClick.AddListener(OnSkipClicked); }
                }
            }
            
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas != null)
            {
                var allButtons = canvas.GetComponentsInChildren<Button>(true);
                foreach (var btn in allButtons)
                {
                    if (btn.name == "BackButton" || btn.name == "ReturnButton" || btn.name == "ReturnToMapButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(FinishBattleAndReturnToMap);
                    }
                    if (btn.name == "MainMenuButton")
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(ReturnToMainMenu);
                    }
                }
            }
        }

        private void Start()
        {
            LoadCombatData();
            round = 1;
            combatFinished = false;
            
            BuildTurnQueue();
            currentTurnIndex = 0;
            activeUnit = turnQueue.Count > 0 ? turnQueue[0] : null;

            UpdateRoundText();
            RefreshUI();
            RefreshTurnOrderUI();
            
            if (logText) logText.text = "Бой начался!";
            Log($"Раунд {round}");

            if (resultPanel) resultPanel.SetActive(false);

            CheckEnemyTurn();
        }

        public void BuildTurnQueue()
        {
            turnQueue.Clear();
            var allUnits = playerUnits.Concat(enemyUnits).Where(u => u.IsAlive).ToList();
            
            turnQueue = allUnits.OrderByDescending(u => u.initiative)
                                .ThenByDescending(u => u.isPlayer)
                                .ToList();

            for (int i = 0; i < turnQueue.Count; i++)
            {
                turnQueue[i].turnOrderIndex = i;
                turnQueue[i].hasActedThisRound = false;
            }
        }

        public void AdvanceTurn()
        {
            if (combatFinished) return;

            currentTurnIndex++;

            if (currentTurnIndex >= turnQueue.Count)
            {
                round++;
                UpdateRoundText();
                BuildTurnQueue();
                currentTurnIndex = 0;
                Log($"Раунд {round}");
            }

            activeUnit = turnQueue.Count > 0 ? turnQueue[currentTurnIndex] : null;

            if (activeUnit != null && !activeUnit.IsAlive)
            {
                AdvanceTurn();
                return;
            }

            RefreshUI();
            RefreshTurnOrderUI();
            CheckEnemyTurn();
        }

        private void CheckEnemyTurn()
        {
            if (activeUnit != null && !activeUnit.isPlayer && !combatFinished)
            {
                StartCoroutine(EnemyTurnRoutine());
            }
        }

        private System.Collections.IEnumerator EnemyTurnRoutine()
        {
            if (enemyTurnInProgress) yield break;
            enemyTurnInProgress = true;

            yield return new WaitForSeconds(1.0f);

            if (!combatFinished && activeUnit != null && !activeUnit.isPlayer)
            {
                EnemyTurn();
            }

            enemyTurnInProgress = false;
        }

        public void OnAttackClicked()
        {
            if (combatFinished || enemyTurnInProgress) return;

            if (activeUnit == null) { Log("Нет активного отряда"); return; }
            if (!activeUnit.isPlayer) { Log("Сейчас ход врага!"); return; }
            if (selectedEnemyUnit == null) { Log("Выберите цель!"); return; }
            if (!selectedEnemyUnit.IsAlive) { Log("Цель уже уничтожена!"); return; }

            PerformAttack(activeUnit, selectedEnemyUnit);
            
            if (CheckVictoryOrDefeat()) return;

            AdvanceTurn();
        }

        public void ExecuteAttack() => OnAttackClicked();

        private bool CheckVictoryOrDefeat()
        {
            if (enemyUnits.All(u => !u.IsAlive))
            {
                FinishCombat(true);
                return true;
            }

            if (playerUnits.All(u => !u.IsAlive))
            {
                FinishCombat(false);
                return true;
            }

            return false;
        }

        private void EnemyTurn()
        {
            if (activeUnit == null || activeUnit.isPlayer) return;

            var target = playerUnits.FirstOrDefault(u => u.IsAlive);

            if (target != null)
            {
                PerformAttack(activeUnit, target);
            }

            if (CheckVictoryOrDefeat()) return;

            AdvanceTurn();
        }

        private void PerformAttack(THCombatUnit attacker, THCombatUnit defender)
        {
            int damage = Mathf.Max(1, attacker.attack * attacker.count - (defender.defense * defender.count / 3));
            int killed = damage / defender.hpPerUnit;
            if (killed < 1 && damage > 0) killed = 1;
            if (killed > defender.count) killed = defender.count;

            defender.count -= killed;
            
            string side = attacker.isPlayer ? "Игрок" : "Враг";
            string msg = $"{attacker.displayName} атакует {defender.displayName}. Урон: {damage}. Потери: {killed}.";
            Log(msg);
        }

        public void AutoBattle()
        {
            if (combatFinished) return;
            int safetyLimit = 200;
            while (!combatFinished && safetyLimit > 0)
            {
                if (activeUnit == null) break;

                if (activeUnit.isPlayer)
                {
                    var target = enemyUnits.FirstOrDefault(u => u.IsAlive);
                    if (target == null) break;
                    PerformAttack(activeUnit, target);
                }
                else
                {
                    var target = playerUnits.FirstOrDefault(u => u.IsAlive);
                    if (target == null) break;
                    PerformAttack(activeUnit, target);
                }

                if (CheckVictoryOrDefeat()) break;
                
                currentTurnIndex++;
                if (currentTurnIndex >= turnQueue.Count)
                {
                    round++;
                    BuildTurnQueue();
                    currentTurnIndex = 0;
                }
                activeUnit = turnQueue.Count > 0 ? turnQueue[currentTurnIndex] : null;
                while (activeUnit != null && !activeUnit.IsAlive)
                {
                     currentTurnIndex++;
                     if (currentTurnIndex >= turnQueue.Count)
                     {
                         round++;
                         BuildTurnQueue();
                         currentTurnIndex = 0;
                     }
                     activeUnit = turnQueue.Count > 0 ? turnQueue[currentTurnIndex] : null;
                }

                safetyLimit--;
            }
            
            UpdateRoundText();
            RefreshUI();
            RefreshTurnOrderUI();
            
            if (safetyLimit <= 0) Log("Бой слишком затянулся...");
        }

        public void SkipTurn()
        {
            if (combatFinished || enemyTurnInProgress) return;
            if (activeUnit == null || !activeUnit.isPlayer) return;

            Log($"{activeUnit.displayName} пропускает ход.");
            AdvanceTurn();
        }

        public void RefreshTurnOrderUI()
        {
            if (activeUnitText != null)
            {
                if (activeUnit != null)
                {
                    activeUnitText.text = (activeUnit.isPlayer ? "Ход: " : "Ход врага: ") + activeUnit.displayName + " x" + activeUnit.count;
                    activeUnitText.color = activeUnit.isPlayer ? new Color(0.4f, 1f, 0.4f) : new Color(1f, 0.4f, 0.4f);
                    
                    if (infoText)
                    {
                        infoText.text = activeUnit.isPlayer ? "Выберите цель и нажмите АТАКА." : "Ход врага...";
                    }
                }
                else
                {
                    activeUnitText.text = "Ожидание...";
                }
            }

            if (turnOrderContainer != null)
            {
                ClearContainer(turnOrderContainer);
                for (int i = 0; i < turnQueue.Count; i++)
                {
                    int index = (currentTurnIndex + i) % turnQueue.Count;
                    var u = turnQueue[index];
                    if (!u.IsAlive) continue;

                    GameObject tag = turnOrderTagTemplate != null ? Instantiate(turnOrderTagTemplate, turnOrderContainer) : null;
                    if (tag == null)
                    {
                        tag = new GameObject("TurnTag_" + u.displayName, typeof(RectTransform));
                        tag.transform.SetParent(turnOrderContainer, false);
                        var t = tag.AddComponent<Text>();
                        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                        t.fontSize = 14;
                        t.text = u.displayName;
                        t.alignment = TextAnchor.MiddleCenter;
                    }
                    tag.SetActive(true);
                    var txt = tag.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.text = u.displayName;
                        txt.color = u.isPlayer ? Color.cyan : Color.red;
                        if (i == 0) txt.fontStyle = FontStyle.Bold;
                    }
                }
            }
        }

        private void LoadCombatData()
        {
            var state = THManager.Instance.Data;
            enemyId = state?.lastEnemyId ?? "test_enemy";

            if (state != null && state.army != null && state.army.Count > 0)
            {
                playerUnits = state.army.Select(u => ConvertToCombatUnit(u, true)).ToList();
            }
            else
            {
                Debug.Log("[TheHeroCombat] No player army found. Using test army.");
                playerUnits.Add(new THCombatUnit { id="swordsman", displayName="Swordsman", count=12, maxCount=12, hpPerUnit=30, attack=5, defense=2, initiative=5, isPlayer=true });
                playerUnits.Add(new THCombatUnit { id="archer", displayName="Archer", count=8, maxCount=8, hpPerUnit=20, attack=7, defense=1, initiative=7, isPlayer=true });
            }

            if (state != null && state.currentEnemyArmy != null && state.currentEnemyArmy.Count > 0)
            {
                enemyUnits = state.currentEnemyArmy.Select(u => ConvertToCombatUnit(u, false)).ToList();
            }
            else
            {
                Debug.Log("[TheHeroCombat] No enemy army context found. Using test enemy.");
                enemyUnits.Add(new THCombatUnit { id="goblin", displayName="Goblin", count=10, maxCount=10, hpPerUnit=15, attack=4, defense=1, initiative=6, isPlayer=false });
            }

            bool isBoss = enemyId.Contains("DarkLord") || enemyId.Contains("Boss") || enemyId.Contains("DarkBoss") || enemyId.Contains("Final") || PlayerPrefs.GetInt("Combat_DarkLord", 0) == 1;

            if (isBoss)
            {
                enemyUnits.Clear();
                enemyUnits.Add(new THCombatUnit { id="dark_lord", displayName="Dark Lord", count=1, maxCount=1, hpPerUnit=120, attack=18, defense=8, initiative=7, isPlayer=false });
                enemyUnits.Add(new THCombatUnit { id="orc_guard", displayName="Orc Guard", count=8, maxCount=8, hpPerUnit=35, attack=6, defense=2, initiative=4, isPlayer=false });
                reward.isFinalVictory = true;
                
                if (infoText) infoText.text = "Финальная битва. Победите Тёмного Лорда.";
                if (battleTitleText) battleTitleText.text = "ФИНАЛЬНАЯ БИТВА";
            }
            else
            {
                if (battleTitleText) battleTitleText.text = "БОЙ";
            }

            reward.gold = 100;
            reward.exp = 50;
        }

        private THCombatUnit ConvertToCombatUnit(THArmyUnit u, bool isPlayer)
        {
            return new THCombatUnit
            {
                id = u.id,
                displayName = u.name,
                count = u.count,
                maxCount = u.count,
                hpPerUnit = u.hpPerUnit,
                attack = u.attack,
                defense = u.defense,
                initiative = u.initiative,
                isPlayer = isPlayer
            };
        }

        public void RefreshUI()
        {
            ClearContainer(playerUnitsContainer);
            ClearContainer(enemyUnitsContainer);

            foreach (var u in playerUnits) CreateUnitCard(u, playerUnitsContainer);
            foreach (var u in enemyUnits) CreateUnitCard(u, enemyUnitsContainer);
            
            UpdateCardHighlights();
        }

        private void ClearContainer(Transform container)
        {
            if (!container) return;
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }
        }

        private void CreateUnitCard(THCombatUnit unit, Transform container)
        {
            GameObject card = new GameObject("UnitCard_" + unit.displayName, typeof(RectTransform));
            card.transform.SetParent(container, false);
            var rt = card.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(400, 80);
            
            var img = card.AddComponent<Image>();
            img.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            
            var outline = card.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);

            var nameTxt = CreateLabel(card.transform, "Name", unit.displayName, 20, TextAnchor.MiddleLeft);
            nameTxt.rectTransform.anchoredPosition = new Vector2(15, 15);
            
            var countTxt = CreateLabel(card.transform, "Count", "x" + unit.count, 22, TextAnchor.MiddleRight);
            countTxt.rectTransform.anchoredPosition = new Vector2(-15, 0);
            countTxt.color = new Color(1, 0.9f, 0.5f);

            var statsTxt = CreateLabel(card.transform, "Stats", $"HP:{unit.hpPerUnit} ATK:{unit.attack} DEF:{unit.defense}", 14, TextAnchor.MiddleLeft);
            statsTxt.rectTransform.anchoredPosition = new Vector2(15, -15);
            statsTxt.color = Color.gray;

            if (!unit.IsAlive)
            {
                img.color = new Color(0.2f, 0.05f, 0.05f, 0.8f);
                nameTxt.text += " (Уничтожен)";
                nameTxt.color = Color.darkGray;
            }
            else
            {
                var btn = card.AddComponent<Button>();
                btn.onClick.AddListener(() => OnUnitSelected(unit));
            }

            unit.uiCard = card;
        }

        private Text CreateLabel(Transform parent, string name, string content, int size, TextAnchor align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.text = content;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.alignment = align;
            t.raycastTarget = false;
            
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            
            return t;
        }

        private void OnUnitSelected(THCombatUnit unit)
        {
            if (combatFinished || enemyTurnInProgress) return;
            if (!unit.IsAlive) return;

            if (unit.isPlayer)
            {
                if (infoText) infoText.text = "Инфо: " + unit.displayName;
            }
            else
            {
                selectedEnemyUnit = unit;
                if (infoText) infoText.text = "Цель: " + unit.displayName;
            }
            UpdateCardHighlights();
        }

        private void UpdateCardHighlights()
        {
            foreach (var u in playerUnits) 
                SetCardHighlight(u, activeUnit == u ? new Color(0, 1f, 0.5f) : Color.black);
            
            foreach (var u in enemyUnits) 
            {
                Color c = Color.black;
                if (activeUnit == u) c = new Color(1f, 0.5f, 0);
                else if (selectedEnemyUnit == u) c = Color.red;
                SetCardHighlight(u, c);
            }
        }

        private void SetCardHighlight(THCombatUnit u, Color color)
        {
            if (u.uiCard == null) return;
            var outline = u.uiCard.GetComponent<Outline>();
            if (outline) outline.effectColor = color;
        }

        private void Log(string msg)
        {
            if (logText) logText.text += "\n" + msg;
            Debug.Log("[Combat] " + msg);
        }

        private void UpdateRoundText()
        {
            if (roundText) roundText.text = "Раунд " + round;
        }

        private void ApplyCombatResultIfNeeded()
        {
            if (rewardApplied) return;
            rewardApplied = true;
            SaveCombatResult();
        }

        private void FinishCombat(bool victory)
        {
            combatFinished = true;
            if (actionButtonsRoot) actionButtonsRoot.SetActive(false);
            if (resultPanel)
            {
                resultPanel.SetActive(true);
                if (resultTitleText) resultTitleText.text = victory ? "ПОБЕДА" : "ПОРАЖЕНИЕ";
                
                if (victory)
                {
                    string body = $"Получено: {reward.gold} золота, {reward.exp} опыта";
                    if (reward.isFinalVictory) body += "\n\nВЫ ПОБЕДИЛИ ТЁМНОГО ЛОРДА!";
                    if (resultBodyText) resultBodyText.text = body;
                }
                else
                {
                    if (resultBodyText) resultBodyText.text = "Армия героя уничтожена.";
                }

                if (finishBattleButton)
                {
                    finishBattleButton.gameObject.SetActive(true);
                    var btnText = finishBattleButton.GetComponentInChildren<Text>();
                    if (btnText) btnText.text = victory ? "ЗАВЕРШИТЬ БОЙ" : "НА КАРТУ";
                }
                if (mainMenuButton) mainMenuButton.gameObject.SetActive(true);
            }
            
            ConnectResultButtons();
        }

        private void SaveCombatResult()
        {
            var state = THManager.Instance.Data;
            if (state != null)
            {
                state.gold += reward.gold;
                state.heroExp += reward.exp;
                if (!string.IsNullOrEmpty(enemyId))
                {
                    if (!state.defeatedEnemyIds.Contains(enemyId))
                        state.defeatedEnemyIds.Add(enemyId);
                    
                    if (reward.isFinalVictory)
                    {
                        state.gameCompleted = true;
                        Debug.Log("[TheHeroVictory] Final boss defeated. Game marked as completed.");
                    }
                }

                foreach (var pu in playerUnits)
                {
                    var original = state.army.Find(u => u.id == pu.id);
                    if (original != null) original.count = pu.count;
                }
                
                THSavePolicy.SaveOnBattleFinish();
            }
            else
            {
                PlayerPrefs.SetInt("TheHero_LastCombatVictory", 1);
                PlayerPrefs.SetString("TheHero_LastDefeatedEnemyId", enemyId);
            }
        }

        public void BackToMap() => FinishBattleAndReturnToMap();
        public void MainMenu() => ReturnToMainMenu();
    }
}
