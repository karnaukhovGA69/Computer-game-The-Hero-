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
                if (ValidateSave(state)) return state;
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
        }

        public static THGameState NewGame()
        {
            var state = new THGameState();
            state.gameVersion = "1.0.0-rc1";
            state.gold = 500;
            state.wood = 20;
            state.stone = 10;
            state.mana = 5;
            state.heroLevel = 1;
            state.heroX = 1; // Start near base
            state.heroY = 1;
            state.movementPoints = 20;
            state.maxMovementPoints = 20;
            
            // Clean statistics
            state.daysPassed = 0;
            state.battlesWon = 0;
            state.resourcesCollected = 0;

            state.army.Add(new THArmyUnit { id = "barracks", name = "Swordsman", count = 12, hpPerUnit = 30, attack = 5, defense = 2, initiative = 5 });
            state.army.Add(new THArmyUnit { id = "range", name = "Archer", count = 8, hpPerUnit = 20, attack = 7, defense = 1, initiative = 7 });

            state.buildings.Add(new THBuildingData { id = "barracks", name = "Barracks", recruitsAvailable = 5, goldCost = 60 });
            state.buildings.Add(new THBuildingData { id = "range", name = "Archery Range", recruitsAvailable = 4, goldCost = 80, woodCost = 1 });
            state.buildings.Add(new THBuildingData { id = "mage", name = "Mage Tower", recruitsAvailable = 2, goldCost = 120, manaCost = 2 });

            state.tutorialShown = PlayerPrefs.GetInt("TheHero_TutorialShown", 0) == 1;

            SaveGame(state);
            return state;
        }
    }
}
