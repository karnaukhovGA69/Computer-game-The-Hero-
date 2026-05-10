using UnityEngine;
using System.Linq;

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

            // Base Income
            int gold = 300;
            int wood = 10;
            int stone = 6;
            int mana = 2;

            // Add building income
            foreach (var buildingId in state.capturedObjectIds)
            {
                // This would ideally check the object type in a real database,
                // for now we'll use naming conventions or check actual map objects
                if (buildingId.ToLower().Contains("goldmine")) gold += 250;
                else if (buildingId.ToLower().Contains("lumbermill")) wood += 20;
                else if (buildingId.ToLower().Contains("stonequarry")) stone += 15;
                else if (buildingId.ToLower().Contains("manaspring")) mana += 8;
                else if (buildingId.ToLower().Contains("mine")) gold += 150; // Generic fallback
            }

            state.gold += gold;
            state.wood += wood;
            state.stone += stone;
            state.mana += mana;

            string msg = $"Новая неделя! Доход: +{gold} Gold, +{wood} Wood, +{stone} Stone, +{mana} Mana";
            if (THMessageSystem.Instance != null)
                THMessageSystem.Instance.ShowMessage(msg);
            
            Debug.Log($"[TheHeroIncome] {msg}");
        }
    }
}
