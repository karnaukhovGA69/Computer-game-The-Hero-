using UnityEngine;

namespace TheHero.Generated
{
    public static class THSavePolicy
    {
        public static bool allowSaveOnManualSave = true;
        public static bool allowSaveOnNewWeek = true;
        public static bool allowSaveOnBattleFinish = true;
        public static bool allowSaveOnBasePurchase = true;

        public static void ManualSave()
        {
            if (!allowSaveOnManualSave) return;
            ForceSave();
        }

        public static void SaveOnNewWeek(int previousWeek, int currentWeek)
        {
            if (!allowSaveOnNewWeek) return;
            if (currentWeek != previousWeek)
            {
                ForceSave();
                if (THMessageSystem.Instance != null)
                    THMessageSystem.Instance.ShowSuccess("Новая неделя. Игра сохранена.");
            }
        }

        public static void SaveOnBattleFinish()
        {
            if (!allowSaveOnBattleFinish) return;
            ForceSave();
        }

        public static void SaveOnBaseAction()
        {
            if (!allowSaveOnBasePurchase) return;
            ForceSave();
        }

        private static void ForceSave()
        {
            var state = THManager.Instance.Data;
            if (state != null)
            {
                THSaveSystem.SaveGame(state);
                Debug.Log("[THSavePolicy] Game saved according to policy.");
            }
        }
    }
}
