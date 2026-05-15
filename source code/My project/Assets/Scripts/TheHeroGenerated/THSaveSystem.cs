using System;
using System.IO;
using UnityEngine;

namespace TheHero.Generated
{
    public static class THSaveSystem
    {
        private static string SavePath => Path.Combine(Application.persistentDataPath, "the_hero_save.json");
        private static string BackupPath => Path.Combine(Application.persistentDataPath, "the_hero_save_backup.json");

        public static void SaveGame(THGameState state)
        {
            if (state == null)
            {
                Debug.LogWarning("[TH] Save skipped: state is null.");
                return;
            }

            try
            {
                state.savesCount++;
                if (File.Exists(SavePath))
                {
                    File.Copy(SavePath, BackupPath, true);
                }
                string json = JsonUtility.ToJson(state, true);
                File.WriteAllText(SavePath, json);
                Debug.Log("[TH] Game saved to " + SavePath);
                if (THMessageSystem.Instance != null) THMessageSystem.Instance.ShowSuccess("Игра сохранена");
            }
            catch (Exception e)
            {
                Debug.LogError("[TH] Save failed: " + e.Message);
            }
        }

        public static THGameState LoadGame()
        {
            if (!File.Exists(SavePath))
            {
                return TryLoadBackup();
            }

            try
            {
                string json = File.ReadAllText(SavePath);
                var state = JsonUtility.FromJson<THGameState>(json);
                if (ValidateSave(state))
                {
                    THBalanceConfig.NormalizeLoadedState(state);
                    return state;
                }
                else return TryLoadBackup();
            }
            catch (Exception e)
            {
                Debug.LogError("[TH] Load failed: " + e.Message);
                return TryLoadBackup();
            }
        }

        public static THGameState TryLoadBackup()
        {
            if (!File.Exists(BackupPath)) return null;
            try
            {
                Debug.LogWarning("[TH] Attempting to load from backup.");
                string json = File.ReadAllText(BackupPath);
                var state = JsonUtility.FromJson<THGameState>(json);
                if (ValidateSave(state)) 
                {
                    THBalanceConfig.NormalizeLoadedState(state);
                    if (THMessageSystem.Instance != null) THMessageSystem.Instance.ShowWarning("Загружен бэкап");
                    return state;
                }
            }
            catch { }
            return null;
        }

        public static bool ValidateSave(THGameState state)
        {
            return state != null && !string.IsNullOrEmpty(state.gameVersion);
        }

        public static bool HasSave() => File.Exists(SavePath) || File.Exists(BackupPath);

        public static void DeleteSave()
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            Debug.Log("[TheHeroNewGame] Save files deleted");
        }

        public static void ClearAllSaveDataForNewGame()
        {
            DeleteSave();

            string[] keys = {
                "TheHero_Gold", "TheHero_Wood", "TheHero_Stone", "TheHero_Mana",
                "TheHero_Army_Swordsman", "TheHero_Army_Archer", "TheHero_Army_Mage",
                "TheHero_Building_Barracks_Level", "TheHero_Building_Archery_Level", "TheHero_Building_MageTower_Level",
                "TheHero_CollectedObjects", "TheHero_DefeatedEnemies", "TheHero_CapturedObjects", "TheHero_VisitedObjects",
                "TheHero_LastCombatVictory", "TheHero_LastDefeatedEnemyId", "TheHero_GameCompleted",
                "TheHero_HeroGridX", "TheHero_HeroGridY", "TheHero_MapSeed", "TheHero_CurrentScene",
                "TheHero_SaveData", "TH_Save", "TheHero_IsStartingNewGame", "Combat_DarkLord"
            };

            foreach (string key in keys)
            {
                if (PlayerPrefs.HasKey(key)) PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            Debug.Log("[TheHeroNewGame] PlayerPrefs save keys cleared");
        }

        public static THGameState NewGame()
        {
            Debug.Log("[TheHeroNewGame] START");
            ClearAllSaveDataForNewGame();

            var state = new THGameState();
            THBalanceConfig.ConfigureNewGameState(state);
            state.mapSeed = UnityEngine.Random.Range(1, 999999);

            state.tutorialShown = PlayerPrefs.GetInt("TheHero_TutorialShown", 0) == 1;
            PlayerPrefs.SetInt("TheHero_IsStartingNewGame", 1);
            PlayerPrefs.Save();

            Debug.Log($"[TheHeroNewGame] Resources: Gold={state.gold} Wood={state.wood} Stone={state.stone} Mana={state.mana}");
            Debug.Log($"[TheHeroNewGame] Army: Swordsman={THBalanceConfig.StartingSwordsman} Archer={THBalanceConfig.StartingArcher} Mage={THBalanceConfig.StartingMage}");
            Debug.Log("[TheHeroNewGame] CollectedObjects cleared");
            Debug.Log("[TheHeroNewGame] DefeatedEnemies cleared");
            Debug.Log("[TheHeroNewGame] CapturedObjects cleared");
            Debug.Log("[TheHeroNewGame] Default game state created");

            SaveGame(state);
            return state;
        }
}
}
