using UnityEngine;

namespace TheHero.Generated
{
    public static class DamageCalculator
    {
        public static int Calculate(THCombatUnit attacker, THCombatUnit defender)
        {
            if (attacker == null || defender == null || attacker.count <= 0)
                return 0;

            return Mathf.Max(1, THBalanceConfig.CalculateDamage(attacker, defender));
        }

        public static int ApplyDamage(THCombatUnit attacker, THCombatUnit defender)
        {
            if (defender == null || defender.count <= 0)
                return 0;

            int beforeCount = defender.count;
            int maxHp = defender.count * defender.hpPerUnit;
            if (defender.currentTotalHp <= 0 || defender.currentTotalHp > maxHp)
                defender.currentTotalHp = maxHp;

            int damage = Calculate(attacker, defender);
            defender.currentTotalHp = Mathf.Max(0, defender.currentTotalHp - damage);
            defender.count = defender.currentTotalHp <= 0 ? 0 : Mathf.CeilToInt(defender.currentTotalHp / (float)defender.hpPerUnit);
            return Mathf.Max(0, beforeCount - defender.count);
        }
    }
}
