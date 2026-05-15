using System;
using System.Collections.Generic;
using UnityEngine;

namespace TheHero.Generated
{
    public enum THEnemyDifficulty { Weak, Medium, Strong, Deadly }

    [Serializable]
    public class THArmyUnit
    {
        public string id;
        public string name;
        public int count;
        public int hpPerUnit;
        public int attack;
        public int defense;
        public int initiative;
        public THArmyUnit Clone() => new THArmyUnit { id = id, name = name, count = count, hpPerUnit = hpPerUnit, attack = attack, defense = defense, initiative = initiative };
        
        public int GetPower() => THBalanceConfig.CalculateUnitPower(count, hpPerUnit, attack, defense, initiative);
    }

    [Serializable]
    public class THBuildingData
    {
        public string id;
        public string name;
        public int level = 1;
        public int recruitsAvailable;
        public int goldCost;
        public int woodCost;
        public int stoneCost;
        public int manaCost;
    }

    [Serializable]
    public class THMapObjectData
    {
        public string id;
        public string type; // Resource, Mine, Base, Enemy
        public float x, y;
        public bool collected;
        public List<THArmyUnit> enemyArmy = new List<THArmyUnit>();
    }

    [Serializable]
    public class THArtifactData
    {
        public string id;
        public string name;
        public string description;
        public string iconPath;
        public int attackBonus;
        public int defenseBonus;
        public int moveBonus;
        public float expMultiplier;
        public int manaIncome;
    }

    [Serializable]
    public class THGameState
    {
        public string gameVersion = "1.0.0-release";
        public int gold = THBalanceConfig.StartingGold;
        public int wood = THBalanceConfig.StartingWood;
        public int stone = THBalanceConfig.StartingStone;
        public int mana = THBalanceConfig.StartingMana;
        public int day = 1;
        public int week = 1;
        public string heroName = "Knight";
        public int heroLevel = 1;
        public int heroExp = 0;
        public float heroX = 0;
        public float heroY = 0;
        public int movementPoints = THBalanceConfig.HeroMaxMovementPoints;
        public int maxMovementPoints = THBalanceConfig.HeroMaxMovementPoints;
        
        public List<THArmyUnit> army = new List<THArmyUnit>();
        public List<THBuildingData> buildings = new List<THBuildingData>();
        public List<THMapObjectData> mapObjects = new List<THMapObjectData>();
        
        public List<string> collectedObjectIds = new List<string>();
        public List<string> defeatedEnemyIds = new List<string>();
        public List<string> capturedObjectIds = new List<string>();
        public List<string> visitedShrineIds = new List<string>();
        public List<string> shownDialogueIds = new List<string>();
        public List<string> heroArtifactIds = new List<string>();

        public int campaignStageIndex = 0;
        public int questProgress = 0;
        public bool isDarkLordDefeated = false;
        public bool tutorialShown = false;
        public bool gameCompleted = false;
        public int difficulty = 1; // 0: Easy, 1: Normal, 2: Hard
        public int mapSeed = 0;

        public string lastEnemyId;
        public List<THArmyUnit> currentEnemyArmy = new List<THArmyUnit>();
        public string lastCombatRewardId;
        public int currentCombatRewardGold;
        public int currentCombatRewardWood;
        public int currentCombatRewardStone;
        public int currentCombatRewardMana;
        public int currentCombatRewardExp;
        public bool currentCombatIsFinal;

        // Statistics
        public int daysPassed = 0;
        public int battlesWon = 0;
        public int battlesLost = 0;
        public int resourcesCollected = 0;
        public int enemiesDefeated = 0;
        public int unitsRecruited = 0;
        public int buildingsUpgraded = 0;
        public int savesCount = 0;
    }
}
