using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TheHero.Generated
{
    public static class THBalanceConfig
    {
        public const int StartingGold = 300;
        public const int StartingWood = 10;
        public const int StartingStone = 5;
        public const int StartingMana = 0;

        public const int StartingSwordsman = 8;
        public const int StartingArcher = 4;
        public const int StartingMage = 0;

        public const int HeroMaxMovementPoints = 12;

        public const int BaseWeeklyGoldIncome = 250;
        public const int BaseWeeklyWoodIncome = 8;
        public const int BaseWeeklyStoneIncome = 5;
        public const int BaseWeeklyManaIncome = 1;

        public const int CapturedGoldMineWeeklyGold = 250;
        public const int CapturedLumberMillWeeklyWood = 15;
        public const int CapturedStoneQuarryWeeklyStone = 12;
        public const int CapturedManaSourceWeeklyMana = 6;

        public const int SwordsmanWeeklyGrowth = 6;
        public const int ArcherWeeklyGrowth = 4;
        public const int MageWeeklyGrowth = 2;

        public const int SwordsmanCostGold = 80;
        public const int ArcherCostGold = 110;
        public const int ArcherCostWood = 1;
        public const int MageCostGold = 180;
        public const int MageCostMana = 2;

        public const int GoldPileSmallReward = 80;
        public const int WoodPileSmallReward = 5;
        public const int StonePileSmallReward = 4;
        public const int ManaCrystalReward = 3;
        public const int ChestGoldReward = 200;
        public const int ChestExpReward = 50;
        public const int GuardedChestGoldReward = 300;

        public const int WeakPowerMax = 400;
        public const int MediumPowerMax = 900;
        public const int StrongPowerMax = 1600;

        public const string SwordsmanId = "unit_swordsman";
        public const string ArcherId = "unit_archer";
        public const string MageId = "unit_mage";

        public static readonly string[] CoreUnitIds = { SwordsmanId, ArcherId, MageId };

        public static THArmyUnit CreateUnit(string id, int count)
        {
            id = NormalizeUnitId(id);

            switch (id)
            {
                case SwordsmanId:
                    return new THArmyUnit { id = SwordsmanId, name = "Swordsman", count = count, hpPerUnit = 30, attack = 5, defense = 3, initiative = 4 };
                case ArcherId:
                    return new THArmyUnit { id = ArcherId, name = "Archer", count = count, hpPerUnit = 18, attack = 7, defense = 1, initiative = 6 };
                case MageId:
                    return new THArmyUnit { id = MageId, name = "Mage", count = count, hpPerUnit = 14, attack = 10, defense = 1, initiative = 5 };
                case "unit_goblin":
                    return new THArmyUnit { id = "unit_goblin", name = "Goblin", count = count, hpPerUnit = 15, attack = 4, defense = 1, initiative = 5 };
                case "unit_wolf":
                    return new THArmyUnit { id = "unit_wolf", name = "Wolf", count = count, hpPerUnit = 20, attack = 6, defense = 1, initiative = 7 };
                case "unit_bandit":
                    return new THArmyUnit { id = "unit_bandit", name = "Bandit", count = count, hpPerUnit = 25, attack = 7, defense = 2, initiative = 5 };
                case "unit_orc":
                    return new THArmyUnit { id = "unit_orc", name = "Orc", count = count, hpPerUnit = 35, attack = 8, defense = 3, initiative = 3 };
                case "unit_skeleton":
                    return new THArmyUnit { id = "unit_skeleton", name = "Skeleton", count = count, hpPerUnit = 28, attack = 7, defense = 4, initiative = 3 };
                case "unit_dark_guard":
                    return new THArmyUnit { id = "unit_dark_guard", name = "Dark Guard", count = count, hpPerUnit = 45, attack = 10, defense = 5, initiative = 4 };
                case "unit_dark_lord":
                    return new THArmyUnit { id = "unit_dark_lord", name = "Dark Lord", count = count, hpPerUnit = 180, attack = 22, defense = 10, initiative = 6 };
                default:
                    return new THArmyUnit { id = id, name = id, count = count, hpPerUnit = 20, attack = 5, defense = 1, initiative = 4 };
            }
        }

        public static THCombatUnit CreateCombatUnit(string id, int count, bool isPlayer)
        {
            var unit = CreateUnit(id, count);
            return new THCombatUnit
            {
                id = unit.id,
                displayName = unit.name,
                count = unit.count,
                maxCount = unit.count,
                currentTotalHp = Mathf.Max(0, unit.count * unit.hpPerUnit),
                hpPerUnit = unit.hpPerUnit,
                attack = unit.attack,
                defense = unit.defense,
                initiative = unit.initiative,
                isPlayer = isPlayer
            };
        }

        public static List<THArmyUnit> CreateStartingArmy()
        {
            return new List<THArmyUnit>
            {
                CreateUnit(SwordsmanId, StartingSwordsman),
                CreateUnit(ArcherId, StartingArcher),
                CreateUnit(MageId, StartingMage)
            };
        }

        public static List<THArmyUnit> CreateFinalBossArmy()
        {
            return new List<THArmyUnit>
            {
                CreateUnit("unit_dark_lord", 1),
                CreateUnit("unit_dark_guard", 14),
                CreateUnit("unit_orc", 18),
                CreateUnit("unit_skeleton", 16)
            };
        }

        public static List<THArmyUnit> CreateTier1GoblinArmy(int count = 8) => Army(CreateUnit("unit_goblin", count));
        public static List<THArmyUnit> CreateTier1WolfArmy() => Army(CreateUnit("unit_wolf", 5));
        public static List<THArmyUnit> CreateTier2WolfArmy() => Army(CreateUnit("unit_wolf", 10));
        public static List<THArmyUnit> CreateTier2BanditArmy() => Army(CreateUnit("unit_bandit", 8));
        public static List<THArmyUnit> CreateTier2GoblinArmy() => Army(CreateUnit("unit_goblin", 16));
        public static List<THArmyUnit> CreateTier3OrcArmy() => Army(CreateUnit("unit_orc", 10));
        public static List<THArmyUnit> CreateTier3BanditSupportArmy() => Army(CreateUnit("unit_bandit", 12), CreateUnit("unit_goblin", 12));
        public static List<THArmyUnit> CreateTier3SkeletonArmy() => Army(CreateUnit("unit_skeleton", 14));
        public static List<THArmyUnit> CreateTier4OrcArmy() => Army(CreateUnit("unit_orc", 16));
        public static List<THArmyUnit> CreateTier4SkeletonArmy() => Army(CreateUnit("unit_skeleton", 20));
        public static List<THArmyUnit> CreateTier4DarkGuardArmy() => Army(CreateUnit("unit_dark_guard", 10));

        public static List<THArmyUnit> Army(params THArmyUnit[] units)
        {
            return units.Select(u => u.Clone()).ToList();
        }

        public static THBuildingData CreateBuilding(string unitId, int week = 1)
        {
            unitId = NormalizeUnitId(unitId);
            switch (unitId)
            {
                case SwordsmanId:
                    return new THBuildingData { id = SwordsmanId, name = "Barracks", level = 1, recruitsAvailable = SwordsmanWeeklyGrowth, goldCost = SwordsmanCostGold };
                case ArcherId:
                    return new THBuildingData { id = ArcherId, name = "Archery Range", level = 1, recruitsAvailable = ArcherWeeklyGrowth, goldCost = ArcherCostGold, woodCost = ArcherCostWood };
                case MageId:
                    return new THBuildingData { id = MageId, name = "Mage Tower", level = 1, recruitsAvailable = week >= 2 ? MageWeeklyGrowth : 0, goldCost = MageCostGold, manaCost = MageCostMana };
                default:
                    return new THBuildingData { id = unitId, name = unitId, level = 1, recruitsAvailable = 0, goldCost = 100 };
            }
        }

        public static int GetRecruitWeeklyGrowth(string unitId)
        {
            unitId = NormalizeUnitId(unitId);
            if (unitId == SwordsmanId) return SwordsmanWeeklyGrowth;
            if (unitId == ArcherId) return ArcherWeeklyGrowth;
            if (unitId == MageId) return MageWeeklyGrowth;
            return 0;
        }

        public static void ConfigureNewGameState(THGameState state)
        {
            if (state == null) return;

            state.gameVersion = "1.0.0-release";
            state.gold = StartingGold;
            state.wood = StartingWood;
            state.stone = StartingStone;
            state.mana = StartingMana;
            state.day = 1;
            state.week = 1;
            state.heroName = "Knight";
            state.heroLevel = 1;
            state.heroExp = 0;
            state.heroX = 4;
            state.heroY = 3;
            state.maxMovementPoints = HeroMaxMovementPoints;
            state.movementPoints = HeroMaxMovementPoints;
            state.gameCompleted = false;
            state.isDarkLordDefeated = false;
            state.daysPassed = 0;
            state.battlesWon = 0;
            state.battlesLost = 0;
            state.resourcesCollected = 0;
            state.enemiesDefeated = 0;
            state.unitsRecruited = 0;
            state.buildingsUpgraded = 0;
            state.lastEnemyId = string.Empty;
            state.lastCombatRewardId = string.Empty;
            ClearCombatReward(state);

            state.army = CreateStartingArmy();
            state.buildings = new List<THBuildingData>
            {
                CreateBuilding(SwordsmanId),
                CreateBuilding(ArcherId),
                CreateBuilding(MageId)
            };

            state.collectedObjectIds.Clear();
            state.defeatedEnemyIds.Clear();
            state.capturedObjectIds.Clear();
            state.visitedShrineIds.Clear();
            state.shownDialogueIds.Clear();
            state.heroArtifactIds.Clear();
            state.currentEnemyArmy.Clear();
            state.mapObjects.Clear();
        }

        public static void NormalizeLoadedState(THGameState state)
        {
            if (state == null) return;

            if (state.army == null) state.army = new List<THArmyUnit>();
            if (state.buildings == null) state.buildings = new List<THBuildingData>();
            if (state.mapObjects == null) state.mapObjects = new List<THMapObjectData>();
            if (state.collectedObjectIds == null) state.collectedObjectIds = new List<string>();
            if (state.defeatedEnemyIds == null) state.defeatedEnemyIds = new List<string>();
            if (state.capturedObjectIds == null) state.capturedObjectIds = new List<string>();
            if (state.visitedShrineIds == null) state.visitedShrineIds = new List<string>();
            if (state.shownDialogueIds == null) state.shownDialogueIds = new List<string>();
            if (state.heroArtifactIds == null) state.heroArtifactIds = new List<string>();
            if (state.currentEnemyArmy == null) state.currentEnemyArmy = new List<THArmyUnit>();

            if (state.army.Count == 0)
            {
                state.army = CreateStartingArmy();
            }
            else
            {
                EnsureCoreArmySlots(state.army);
                NormalizeArmyStats(state.army);
            }

            NormalizeBuildings(state, false);

            state.maxMovementPoints = HeroMaxMovementPoints;
            state.movementPoints = Mathf.Clamp(state.movementPoints, 0, state.maxMovementPoints);
        }

        public static void EnsureCoreArmySlots(List<THArmyUnit> army)
        {
            foreach (var unitId in CoreUnitIds)
            {
                if (!army.Any(u => NormalizeUnitId(u.id, u.name) == unitId))
                    army.Add(CreateUnit(unitId, 0));
            }
        }

        public static void NormalizeArmyStats(List<THArmyUnit> units)
        {
            if (units == null) return;
            foreach (var unit in units)
            {
                if (unit == null) continue;
                int count = Mathf.Max(0, unit.count);
                var balanced = CreateUnit(NormalizeUnitId(unit.id, unit.name), count);
                unit.id = balanced.id;
                unit.name = balanced.name;
                unit.count = balanced.count;
                unit.hpPerUnit = balanced.hpPerUnit;
                unit.attack = balanced.attack;
                unit.defense = balanced.defense;
                unit.initiative = balanced.initiative;
            }
        }

        public static void NormalizeBuildings(THGameState state, bool resetAvailability)
        {
            if (state == null) return;
            if (state.buildings == null) state.buildings = new List<THBuildingData>();

            foreach (var unitId in CoreUnitIds)
            {
                var existing = state.buildings.FirstOrDefault(b => NormalizeUnitId(b.id) == unitId);
                var balanced = CreateBuilding(unitId, state.week);

                if (existing == null)
                {
                    state.buildings.Add(balanced);
                    continue;
                }

                existing.id = balanced.id;
                existing.name = balanced.name;
                existing.goldCost = balanced.goldCost;
                existing.woodCost = balanced.woodCost;
                existing.stoneCost = balanced.stoneCost;
                existing.manaCost = balanced.manaCost;
                existing.level = Mathf.Max(1, existing.level);
                if (resetAvailability) existing.recruitsAvailable = balanced.recruitsAvailable;
                if (existing.id == MageId && state.week <= 1 && existing.level <= 1 && resetAvailability)
                    existing.recruitsAvailable = 0;
            }
        }

        public static void AddWeeklyRecruitGrowth(THGameState state)
        {
            if (state == null) return;
            NormalizeBuildings(state, false);

            foreach (var building in state.buildings)
            {
                string id = NormalizeUnitId(building.id);
                if (id == MageId && state.week < 2 && building.level <= 1) continue;
                building.recruitsAvailable += GetRecruitWeeklyGrowth(id);
            }
        }

        public static string NormalizeUnitId(string id, string name = "")
        {
            string key = ((id ?? string.Empty) + " " + (name ?? string.Empty)).ToLowerInvariant();

            if (key.Contains("swordsman") || key.Contains("sword") || key.Contains("barracks")) return SwordsmanId;
            if (key.Contains("archer") || key.Contains("range")) return ArcherId;
            if (key.Contains("mage") || key.Contains("wizard")) return MageId;
            if (key.Contains("dark_lord") || key.Contains("darklord") || key.Contains("dark lord") || key.Contains("lord")) return "unit_dark_lord";
            if (key.Contains("dark_guard") || key.Contains("darkguard") || key.Contains("dark guard") || key.Contains("darkknight") || key.Contains("dark knight") || key.Contains(" dk")) return "unit_dark_guard";
            if (key.Contains("skeleton")) return "unit_skeleton";
            if (key.Contains("bandit")) return "unit_bandit";
            if (key.Contains("goblin")) return "unit_goblin";
            if (key.Contains("wolf") || key.Contains("wolves")) return "unit_wolf";
            if (key.Contains("orc")) return "unit_orc";

            return string.IsNullOrWhiteSpace(id) ? SwordsmanId : id;
        }

        public static int CalculateUnitPower(int count, int hpPerUnit, int attack, int defense, int initiative)
        {
            if (count <= 0) return 0;
            float perUnit = hpPerUnit * 0.4f + attack * 3f + defense * 2f + initiative;
            return Mathf.RoundToInt(count * perUnit);
        }

        public static int CalculateArmyPower(IEnumerable<THArmyUnit> units)
        {
            if (units == null) return 0;
            int power = 0;
            foreach (var unit in units)
            {
                if (unit == null) continue;
                power += CalculateUnitPower(unit.count, unit.hpPerUnit, unit.attack, unit.defense, unit.initiative);
            }
            return power;
        }

        public static int CalculateArmyPower(IEnumerable<THCombatUnit> units)
        {
            if (units == null) return 0;
            int power = 0;
            foreach (var unit in units)
            {
                if (unit == null) continue;
                power += CalculateUnitPower(unit.count, unit.hpPerUnit, unit.attack, unit.defense, unit.initiative);
            }
            return power;
        }

        public static THEnemyDifficulty GetDifficultyForPower(int power)
        {
            if (power < WeakPowerMax) return THEnemyDifficulty.Weak;
            if (power < MediumPowerMax) return THEnemyDifficulty.Medium;
            if (power < StrongPowerMax) return THEnemyDifficulty.Strong;
            return THEnemyDifficulty.Deadly;
        }

        public static string GetPowerTierLabel(int power)
        {
            var tier = GetDifficultyForPower(power);
            if (tier == THEnemyDifficulty.Weak) return "Weak";
            if (tier == THEnemyDifficulty.Medium) return "Medium";
            if (tier == THEnemyDifficulty.Strong) return "Strong";
            return "Deadly";
        }

        public static int CalculateDamage(THCombatUnit attacker, THCombatUnit defender)
        {
            if (attacker == null || defender == null || attacker.count <= 0) return 0;
            float baseDamage = attacker.attack * attacker.count;
            float defenseReduction = Mathf.Clamp(defender.defense * 0.04f, 0f, 0.60f);
            float damageAfterDefense = baseDamage * (1f - defenseReduction);
            return Mathf.Max(1, Mathf.RoundToInt(damageAfterDefense));
        }

        public static void ConfigureMapObjectBalance(THMapObject obj)
        {
            if (obj == null) return;

            if (obj.type == THMapObject.ObjectType.Enemy)
            {
                ConfigureEnemyMapObject(obj);
                return;
            }

            switch (obj.type)
            {
                case THMapObject.ObjectType.GoldResource:
                    obj.rewardGold = IsMidOrLateObject(obj.id) ? 150 : GoldPileSmallReward;
                    break;
                case THMapObject.ObjectType.WoodResource:
                    obj.rewardWood = IsMidOrLateObject(obj.id) ? 8 : WoodPileSmallReward;
                    break;
                case THMapObject.ObjectType.StoneResource:
                    obj.rewardStone = IsMidOrLateObject(obj.id) ? 8 : StonePileSmallReward;
                    break;
                case THMapObject.ObjectType.ManaResource:
                    obj.rewardMana = ManaCrystalReward;
                    break;
                case THMapObject.ObjectType.Treasure:
                    obj.rewardGold = IsGuardedOrLateObject(obj.id) ? GuardedChestGoldReward : ChestGoldReward;
                    obj.rewardExp = ChestExpReward;
                    break;
                case THMapObject.ObjectType.Mine:
                    obj.rewardGold = 0;
                    break;
            }
        }

        public static void ConfigureEnemyComponentBalance(THEnemy enemy)
        {
            if (enemy == null) return;

            var temp = new THEnemyBalanceTemplate(enemy.gameObject.name, enemy.enemyType, enemy.isFinalBoss, enemy.difficulty);
            ApplyEnemyTemplate(temp);
            enemy.difficulty = temp.Difficulty;
            enemy.rewardGold = temp.RewardGold;
            enemy.rewardExp = temp.RewardExp;
            enemy.enemyArmy = temp.Army.Select(u => u.Clone()).ToList();
            enemy.isFinalBoss = temp.IsFinalBoss;
        }

        private static void ConfigureEnemyMapObject(THMapObject obj)
        {
            var template = new THEnemyBalanceTemplate(obj.id, obj.displayName, obj.isFinalBoss || obj.isDarkLord, obj.difficulty);
            ApplyEnemyTemplate(template);

            obj.difficulty = template.Difficulty;
            obj.rewardGold = template.RewardGold;
            obj.rewardWood = 0;
            obj.rewardStone = 0;
            obj.rewardMana = 0;
            obj.rewardExp = template.RewardExp;
            obj.enemyArmy = template.Army.Select(u => u.Clone()).ToList();
            obj.isFinalBoss = template.IsFinalBoss;
            obj.isDarkLord = template.IsFinalBoss;

            if (template.IsFinalBoss)
                obj.displayName = "Темный Лорд";
        }

        private static void ApplyEnemyTemplate(THEnemyBalanceTemplate template)
        {
            string key = (template.Id + " " + template.Name).ToLowerInvariant();

            if (template.IsFinalBoss || key.Contains("darklord") || key.Contains("dark lord") || key.Contains("final"))
            {
                template.IsFinalBoss = true;
                template.Difficulty = THEnemyDifficulty.Deadly;
                template.RewardGold = 500;
                template.RewardExp = 250;
                template.Army = CreateFinalBossArmy();
                return;
            }

            if (key.Contains("dark_guard") || key.Contains("darkguard") || key.Contains("dark_guard_") || key.Contains("dark "))
            {
                template.Difficulty = THEnemyDifficulty.Deadly;
                template.RewardGold = 420;
                template.RewardExp = 220;
                template.Army = CreateTier4DarkGuardArmy();
                return;
            }

            if (key.Contains("mineguard") || key.Contains("mine_guard") || key.Contains("mountain"))
            {
                template.Difficulty = THEnemyDifficulty.Strong;
                template.RewardGold = 260;
                template.RewardExp = 130;
                template.Army = CreateTier3OrcArmy();
                return;
            }

            if (key.Contains("skeleton"))
            {
                template.Difficulty = THEnemyDifficulty.Strong;
                template.RewardGold = 300;
                template.RewardExp = 150;
                template.Army = CreateTier3SkeletonArmy();
                return;
            }

            if (key.Contains("orc"))
            {
                template.Difficulty = THEnemyDifficulty.Strong;
                template.RewardGold = 260;
                template.RewardExp = 120;
                template.Army = CreateTier3OrcArmy();
                return;
            }

            if (key.Contains("bandit") || key.Contains("forest_2"))
            {
                template.Difficulty = THEnemyDifficulty.Medium;
                template.RewardGold = 170;
                template.RewardExp = 80;
                template.Army = CreateTier2BanditArmy();
                return;
            }

            if (key.Contains("wolf") || key.Contains("forest_1"))
            {
                template.Difficulty = template.Difficulty == THEnemyDifficulty.Weak ? THEnemyDifficulty.Weak : THEnemyDifficulty.Medium;
                template.RewardGold = template.Difficulty == THEnemyDifficulty.Weak ? 70 : 140;
                template.RewardExp = template.Difficulty == THEnemyDifficulty.Weak ? 30 : 65;
                template.Army = template.Difficulty == THEnemyDifficulty.Weak ? CreateTier1WolfArmy() : CreateTier2WolfArmy();
                return;
            }

            if (key.Contains("goblin_02") || key.Contains("weak_2"))
            {
                template.Difficulty = THEnemyDifficulty.Weak;
                template.RewardGold = 90;
                template.RewardExp = 35;
                template.Army = CreateTier1GoblinArmy(10);
                return;
            }

            if (key.Contains("forest_3"))
            {
                template.Difficulty = THEnemyDifficulty.Medium;
                template.RewardGold = 160;
                template.RewardExp = 75;
                template.Army = CreateTier2GoblinArmy();
                return;
            }

            if (template.Difficulty == THEnemyDifficulty.Medium)
            {
                template.RewardGold = 140;
                template.RewardExp = 65;
                template.Army = CreateTier2WolfArmy();
                return;
            }

            if (template.Difficulty == THEnemyDifficulty.Strong)
            {
                template.RewardGold = 260;
                template.RewardExp = 120;
                template.Army = CreateTier3OrcArmy();
                return;
            }

            if (template.Difficulty == THEnemyDifficulty.Deadly)
            {
                template.RewardGold = 420;
                template.RewardExp = 220;
                template.Army = CreateTier4DarkGuardArmy();
                return;
            }

            template.Difficulty = THEnemyDifficulty.Weak;
            template.RewardGold = 80;
            template.RewardExp = 30;
            template.Army = CreateTier1GoblinArmy();
        }

        public static void ClearCombatReward(THGameState state)
        {
            if (state == null) return;
            state.currentCombatRewardGold = 0;
            state.currentCombatRewardWood = 0;
            state.currentCombatRewardStone = 0;
            state.currentCombatRewardMana = 0;
            state.currentCombatRewardExp = 0;
            state.currentCombatIsFinal = false;
        }

        private static bool IsMidOrLateObject(string id)
        {
            string key = (id ?? string.Empty).ToLowerInvariant();
            return key.Contains("_02") || key.Contains("forest") || key.Contains("mountain") || key.Contains("dark");
        }

        private static bool IsGuardedOrLateObject(string id)
        {
            string key = (id ?? string.Empty).ToLowerInvariant();
            return key.Contains("_02") || key.Contains("guard") || key.Contains("dark");
        }

        private class THEnemyBalanceTemplate
        {
            public string Id;
            public string Name;
            public bool IsFinalBoss;
            public THEnemyDifficulty Difficulty;
            public int RewardGold;
            public int RewardExp;
            public List<THArmyUnit> Army = new List<THArmyUnit>();

            public THEnemyBalanceTemplate(string id, string name, bool isFinalBoss, THEnemyDifficulty difficulty)
            {
                Id = id ?? string.Empty;
                Name = name ?? string.Empty;
                IsFinalBoss = isFinalBoss;
                Difficulty = difficulty;
            }
        }
    }
}
