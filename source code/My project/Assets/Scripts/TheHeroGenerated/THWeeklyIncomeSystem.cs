using UnityEngine;

namespace TheHero.Generated
{
    public class THWeeklyIncomeSystem : MonoBehaviour
    {
        private static THWeeklyIncomeSystem _instance;
        public static THWeeklyIncomeSystem Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THWeeklyIncomeSystem");
                    _instance = go.AddComponent<THWeeklyIncomeSystem>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public void ApplyWeeklyIncome()
        {
            var state = THManager.Instance.Data;
            if (state == null) return;

            int gold = THBalanceConfig.BaseWeeklyGoldIncome;
            int wood = THBalanceConfig.BaseWeeklyWoodIncome;
            int stone = THBalanceConfig.BaseWeeklyStoneIncome;
            int mana = THBalanceConfig.BaseWeeklyManaIncome;

            foreach (var buildingId in state.capturedObjectIds)
            {
                string key = (buildingId ?? string.Empty).ToLower();
                if (key.Contains("goldmine") || key.Contains("gold_mine") || key.Contains("mine")) gold += THBalanceConfig.CapturedGoldMineWeeklyGold;
                else if (key.Contains("lumbermill") || key.Contains("lumber") || key.Contains("woodmill")) wood += THBalanceConfig.CapturedLumberMillWeeklyWood;
                else if (key.Contains("stonequarry") || key.Contains("quarry")) stone += THBalanceConfig.CapturedStoneQuarryWeeklyStone;
                else if (key.Contains("manaspring") || key.Contains("mana_source") || key.Contains("mana")) mana += THBalanceConfig.CapturedManaSourceWeeklyMana;
            }

            state.gold += gold;
            state.wood += wood;
            state.stone += stone;
            state.mana += mana;

            string msg = $"Новая неделя. Казна пополнена. +{gold} Gold, +{wood} Wood, +{stone} Stone, +{mana} Mana";
            if (THMessageSystem.Instance != null)
                THMessageSystem.Instance.ShowMessage(msg);

            Debug.Log($"[TheHeroIncome] {msg}");
        }
    }
}
