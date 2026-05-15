using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEngine;

public static class TheHeroBalanceValidation
{
    [MenuItem("The Hero/Validation/Run Balance Validation")]
    public static void RunBalanceValidation()
    {
        int failed = 0;

        var startingArmy = THBalanceConfig.CreateStartingArmy();
        var tier1 = THBalanceConfig.CreateTier1GoblinArmy(8);
        var tier2 = THBalanceConfig.CreateTier2BanditArmy();
        var tier3 = THBalanceConfig.CreateTier3OrcArmy();
        var boss = THBalanceConfig.CreateFinalBossArmy();

        bool startBeatsTier1 = Simulate(startingArmy, tier1, out var startTier1Survivors, out _);
        Check(startBeatsTier1, "Starting army beats a Tier 1 guard", ref failed);

        bool startBeatsTier3 = Simulate(startingArmy, tier3, out var startTier3Survivors, out _);
        int startingPower = THBalanceConfig.CalculateArmyPower(startingArmy);
        int tier3SurvivorPower = THBalanceConfig.CalculateArmyPower(startTier3Survivors);
        Check(!startBeatsTier3 || tier3SurvivorPower <= Mathf.RoundToInt(startingPower * 0.45f),
            "Starting army loses to Tier 3 or survives only with heavy losses", ref failed);

        bool startBeatsBoss = Simulate(startingArmy, boss, out _, out _);
        int bossPower = THBalanceConfig.CalculateArmyPower(boss);
        Check(!startBeatsBoss && startingPower < bossPower * 0.5f,
            $"Starting army cannot beat DarkLord (start power {startingPower}, boss power {bossPower})", ref failed);

        var twoWeekArmy = CreateFullyRecruitedArmy(2);
        bool twoWeekBeatsTier2 = Simulate(twoWeekArmy, tier2, out _, out _);
        Check(twoWeekBeatsTier2, "Army after 2 recruitment weeks beats a Tier 2 guard", ref failed);

        var preparedArmy = CreateFullyRecruitedArmy(4);
        int preparedPower = THBalanceConfig.CalculateArmyPower(preparedArmy);
        Check(preparedPower >= Mathf.RoundToInt(bossPower * 0.7f),
            $"Army after 3-4 weeks has a credible DarkLord power window ({preparedPower}/{bossPower})", ref failed);

        var reset = new THGameState();
        THBalanceConfig.ConfigureNewGameState(reset);
        bool resetOk =
            reset.gold == THBalanceConfig.StartingGold &&
            reset.wood == THBalanceConfig.StartingWood &&
            reset.stone == THBalanceConfig.StartingStone &&
            reset.mana == THBalanceConfig.StartingMana &&
            reset.day == 1 &&
            reset.week == 1 &&
            reset.movementPoints == THBalanceConfig.HeroMaxMovementPoints &&
            reset.army.FirstOrDefault(u => u.id == THBalanceConfig.SwordsmanId)?.count == THBalanceConfig.StartingSwordsman &&
            reset.army.FirstOrDefault(u => u.id == THBalanceConfig.ArcherId)?.count == THBalanceConfig.StartingArcher &&
            reset.army.FirstOrDefault(u => u.id == THBalanceConfig.MageId)?.count == THBalanceConfig.StartingMage &&
            reset.defeatedEnemyIds.Count == 0 &&
            reset.collectedObjectIds.Count == 0 &&
            reset.capturedObjectIds.Count == 0 &&
            reset.heroArtifactIds.Count == 0 &&
            !reset.gameCompleted;
        Check(resetOk, "New Game reset uses the balanced clean state", ref failed);

        if (failed == 0)
            Debug.Log("[TheHeroBalance] PASS all balance validation checks");
        else
            Debug.LogError($"[TheHeroBalance] FAIL {failed} balance validation check(s)");
    }

    private static List<THArmyUnit> CreateFullyRecruitedArmy(int recruitmentWeeks)
    {
        var army = THBalanceConfig.CreateStartingArmy();
        Add(army, THBalanceConfig.SwordsmanId, THBalanceConfig.SwordsmanWeeklyGrowth * recruitmentWeeks);
        Add(army, THBalanceConfig.ArcherId, THBalanceConfig.ArcherWeeklyGrowth * recruitmentWeeks);
        Add(army, THBalanceConfig.MageId, THBalanceConfig.MageWeeklyGrowth * Mathf.Max(0, recruitmentWeeks - 1));
        return army;
    }

    private static void Add(List<THArmyUnit> army, string unitId, int count)
    {
        var unit = army.FirstOrDefault(u => u.id == unitId);
        if (unit == null)
        {
            army.Add(THBalanceConfig.CreateUnit(unitId, count));
            return;
        }

        unit.count += count;
    }

    private static void Check(bool condition, string message, ref int failed)
    {
        if (condition)
        {
            Debug.Log("[TheHeroBalance] PASS " + message);
            return;
        }

        failed++;
        Debug.LogError("[TheHeroBalance] FAIL " + message);
    }

    private static bool Simulate(List<THArmyUnit> playerArmy, List<THArmyUnit> enemyArmy, out List<THArmyUnit> playerSurvivors, out List<THArmyUnit> enemySurvivors)
    {
        var player = playerArmy.Select(u => new SimStack(u, true)).ToList();
        var enemy = enemyArmy.Select(u => new SimStack(u, false)).ToList();

        for (int round = 0; round < 100; round++)
        {
            var queue = player.Concat(enemy)
                .Where(u => u.IsAlive)
                .OrderByDescending(u => u.Initiative)
                .ThenByDescending(u => u.IsPlayer)
                .ToList();

            foreach (var attacker in queue)
            {
                if (!attacker.IsAlive) continue;

                var targets = attacker.IsPlayer ? enemy : player;
                var target = targets.FirstOrDefault(u => u.IsAlive);
                if (target == null)
                {
                    playerSurvivors = player.Select(u => u.ToArmyUnit()).ToList();
                    enemySurvivors = enemy.Select(u => u.ToArmyUnit()).ToList();
                    return attacker.IsPlayer;
                }

                int damage = CalculateDamage(attacker, target);
                target.TotalHp = Mathf.Max(0, target.TotalHp - damage);
                target.Count = target.TotalHp <= 0 ? 0 : Mathf.CeilToInt(target.TotalHp / (float)target.HpPerUnit);

                if (!enemy.Any(u => u.IsAlive))
                {
                    playerSurvivors = player.Select(u => u.ToArmyUnit()).ToList();
                    enemySurvivors = enemy.Select(u => u.ToArmyUnit()).ToList();
                    return true;
                }

                if (!player.Any(u => u.IsAlive))
                {
                    playerSurvivors = player.Select(u => u.ToArmyUnit()).ToList();
                    enemySurvivors = enemy.Select(u => u.ToArmyUnit()).ToList();
                    return false;
                }
            }
        }

        playerSurvivors = player.Select(u => u.ToArmyUnit()).ToList();
        enemySurvivors = enemy.Select(u => u.ToArmyUnit()).ToList();
        return false;
    }

    private static int CalculateDamage(SimStack attacker, SimStack defender)
    {
        float baseDamage = attacker.Attack * attacker.Count;
        float defenseReduction = Mathf.Clamp(defender.Defense * 0.04f, 0f, 0.60f);
        return Mathf.Max(1, Mathf.RoundToInt(baseDamage * (1f - defenseReduction)));
    }

    private class SimStack
    {
        public string Id;
        public string Name;
        public int Count;
        public int HpPerUnit;
        public int Attack;
        public int Defense;
        public int Initiative;
        public int TotalHp;
        public bool IsPlayer;
        public bool IsAlive => Count > 0;

        public SimStack(THArmyUnit unit, bool isPlayer)
        {
            Id = unit.id;
            Name = unit.name;
            Count = unit.count;
            HpPerUnit = unit.hpPerUnit;
            Attack = unit.attack;
            Defense = unit.defense;
            Initiative = unit.initiative;
            TotalHp = Mathf.Max(0, unit.count * unit.hpPerUnit);
            IsPlayer = isPlayer;
        }

        public THArmyUnit ToArmyUnit()
        {
            return new THArmyUnit
            {
                id = Id,
                name = Name,
                count = Count,
                hpPerUnit = HpPerUnit,
                attack = Attack,
                defense = Defense,
                initiative = Initiative
            };
        }
    }
}
