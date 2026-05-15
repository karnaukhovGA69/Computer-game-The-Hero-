using System.Collections.Generic;
using UnityEngine;

namespace TheHero.Generated
{
    /// <summary>
    /// Компонент врага на карте. Совместим с THMapObject по полям.
    /// </summary>
    public class THEnemy : MonoBehaviour
    {
        public string enemyType = "weak";      // weak, medium, strong, boss
        public bool startsCombat = true;
        public bool blocksMovement = true;
        public bool isFinalBoss = false;
        public bool defeated = false;

        public string displayName = "";
        public THEnemyDifficulty difficulty = THEnemyDifficulty.Weak;

        public int rewardGold = 0;
        public int rewardExp  = 0;

        public List<THArmyUnit> enemyArmy = new List<THArmyUnit>();

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(displayName))
                displayName = gameObject.name;

            // Синхронизировать difficulty из enemyType
            switch (enemyType)
            {
                case "weak":     difficulty = THEnemyDifficulty.Weak;   break;
                case "medium":   difficulty = THEnemyDifficulty.Medium; break;
                case "strong":   difficulty = THEnemyDifficulty.Strong; break;
                case "boss":     difficulty = THEnemyDifficulty.Deadly; break;
                default:         difficulty = THEnemyDifficulty.Weak;   break;
            }

            // Награды по умолчанию
            if (rewardGold == 0)
            {
                switch (enemyType)
                {
                    case "weak":   rewardGold = 50;  rewardExp = 20;  break;
                    case "medium": rewardGold = 150; rewardExp = 60;  break;
                    case "strong": rewardGold = 300; rewardExp = 120; break;
                    case "boss":   rewardGold = 999; rewardExp = 500; break;
                }
            }

            THBalanceConfig.ConfigureEnemyComponentBalance(this);
        }

        private void OnMouseEnter()
        {
            if (THSingleMapHoverLabel.Instance != null)
            {
                string diffStr = GetDifficultyString();
                THSingleMapHoverLabel.Instance.Show(
                    displayName + " [" + diffStr + "]",
                    transform.position);
            }
        }

        private void OnMouseExit()
        {
            if (THSingleMapHoverLabel.Instance != null)
                THSingleMapHoverLabel.Instance.Hide();
        }

        private string GetDifficultyString()
        {
            switch (difficulty)
            {
                case THEnemyDifficulty.Weak:   return "Слабый";
                case THEnemyDifficulty.Medium:  return "Средний";
                case THEnemyDifficulty.Strong:  return "Сильный";
                case THEnemyDifficulty.Deadly:  return "Смертельно опасный";
                default:                        return "Неизвестно";
            }
        }

        /// <summary>Пометить врага как побеждённого.</summary>
        public void SetDefeated(THGameState state)
        {
            if (defeated) return;
            defeated = true;

            if (state != null)
            {
                state.gold += rewardGold;
                state.heroExp += rewardExp;
                state.enemiesDefeated++;
                state.battlesWon++;

                if (isFinalBoss)
                    state.isDarkLordDefeated = true;

                string id = gameObject.name;
                if (!state.defeatedEnemyIds.Contains(id))
                    state.defeatedEnemyIds.Add(id);
            }

            gameObject.SetActive(false);
        }
    }
}
