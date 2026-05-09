using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TheHero.Generated
{
    public class THCombatController : MonoBehaviour
    {
        public THGameState State;
        public Text LogText;
        public Text RoundText;
        public GameObject BackButton;
        public GameObject VictoryPanel;
        public GameObject DefeatPanel;
        public Text VictoryStatsText;
        public GameObject CombatUIPanel;

        public int round = 1;
        private bool isCombatFinished = false;

        private void Start()
        {
            State = THSaveSystem.LoadGame();
            if (State == null) State = THSaveSystem.NewGame();
            
            LogText.text = "Бой начался! Ваши отряды готовы к бою.";
            if (RoundText) RoundText.text = $"Раунд {round}";
            if (BackButton) BackButton.SetActive(false);
            if (VictoryPanel) VictoryPanel.SetActive(false);
            if (DefeatPanel) DefeatPanel.SetActive(false);
            if (CombatUIPanel) CombatUIPanel.SetActive(true);
        }

        public void Attack()
        {
            if (isCombatFinished) return;
            ExecuteTurn();
        }

        public void AutoBattle()
        {
            if (isCombatFinished) return;
            int maxRounds = 100;
            int currentBattleRounds = 0;
            while (!isCombatFinished && currentBattleRounds < maxRounds)
            {
                ExecuteTurn();
                currentBattleRounds++;
            }
            if (currentBattleRounds >= maxRounds && !isCombatFinished)
            {
                LogText.text = "Бой затянулся. Ничья.";
                isCombatFinished = true;
                if (BackButton) BackButton.SetActive(true);
            }
        }

        public void SkipTurn()
        {
            if (isCombatFinished) return;
            LogText.text = "Вы пропустили ход.";
            ExecuteTurn();
        }

        private void ExecuteTurn()
        {
            if (State.army.Count == 0 || State.currentEnemyArmy.Count == 0) return;

            // Combat logic: Highest initiative moves first
            var playerSquad = State.army.OrderByDescending(u => u.initiative).FirstOrDefault(u => u.count > 0);
            var enemySquad = State.currentEnemyArmy.OrderByDescending(u => u.initiative).FirstOrDefault(u => u.count > 0);

            if (playerSquad == null || enemySquad == null) return;

            if (playerSquad.initiative >= enemySquad.initiative)
            {
                PerformAttack(playerSquad, enemySquad, true);
                if (enemySquad.count > 0) PerformAttack(enemySquad, playerSquad, false);
            }
            else
            {
                PerformAttack(enemySquad, playerSquad, false);
                if (playerSquad.count > 0) PerformAttack(playerSquad, enemySquad, true);
            }

            round++;
            if (RoundText) RoundText.text = $"Раунд {round}";
            CheckCombatEnd();
            THSaveSystem.SaveGame(State);
        }

        private void PerformAttack(THArmyUnit attacker, THArmyUnit defender, bool isPlayerAttacking)
        {
            int totalAttack = attacker.attack * attacker.count;
            int totalDefense = (defender.defense * defender.count) / 2;
            int damage = Mathf.Max(1, totalAttack - totalDefense);
            int killed = damage / defender.hpPerUnit;
            if (killed > defender.count) killed = defender.count;
            defender.count -= killed;

            string side = isPlayerAttacking ? "Игрок" : "Враг";
            LogText.text = $"{side}: {attacker.name} атакует {defender.name}. Урон: {damage}. Потери: {killed}.";
            
            if (defender.count <= 0)
                LogText.text += $"\nОтряд {defender.name} уничтожен!";
        }

        private void CheckCombatEnd()
        {
            if (State.currentEnemyArmy.All(u => u.count <= 0))
            {
                isCombatFinished = true;
                bool isDarkLord = PlayerPrefs.GetInt("Combat_DarkLord", 0) == 1;
                if (isDarkLord)
                {
                    State.isDarkLordDefeated = true;
                    State.gameCompleted = true;
                    ShowVictoryScreen(true);
                }
                else
                {
                    State.gold += 150;
                    State.heroExp += 100;
                    State.defeatedEnemyIds.Add(State.lastEnemyId);
                    if (BackButton) BackButton.SetActive(true);
                    if (CombatUIPanel) CombatUIPanel.SetActive(false);
                    LogText.text = "Победа! Враг разбит.";
                }
                THSaveSystem.SaveGame(State);
            }
            else if (State.army.All(u => u.count <= 0))
            {
                isCombatFinished = true;
                if (DefeatPanel) DefeatPanel.SetActive(true);
                if (CombatUIPanel) CombatUIPanel.SetActive(false);
                LogText.text = "Поражение. Армия героя уничтожена.";
                THSaveSystem.SaveGame(State);
            }
        }

        public void ShowVictoryScreen(bool darkLord)
        {
            if (VictoryPanel)
            {
                VictoryPanel.SetActive(true);
                if (CombatUIPanel) CombatUIPanel.SetActive(false);
                if (VictoryStatsText)
                {
                    int totalDays = State.day + (State.week - 1) * 7;
                    VictoryStatsText.text = $"Дней прошло: {totalDays}\n" +
                                            $"Уровень героя: {State.heroLevel}\n" +
                                            $"Золото: {State.gold}\n" +
                                            $"Размер армии: {State.army.Sum(u => u.count)}\n" +
                                            $"Врагов побеждено: {State.defeatedEnemyIds.Count + 1}\n" +
                                            $"Ресурсов собрано: {State.collectedObjectIds.Count}";
                }
            }
        }

        public void BackToMap() => SceneManager.LoadScene("Map");
        public void NewGame() { THSaveSystem.NewGame(); SceneManager.LoadScene("Map"); }
        public void MainMenu() => SceneManager.LoadScene("MainMenu");
        public void LoadLastSave() => SceneManager.LoadScene("Map");
    }
}