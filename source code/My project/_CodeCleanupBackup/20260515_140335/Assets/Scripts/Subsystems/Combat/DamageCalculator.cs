using System;
using TheHero.Domain;
using UnityEngine;

namespace TheHero.Subsystems.Combat
{
    public static class DamageCalculator
    {
        // урон = (атака / (атака + защита)) * средний_урон * количество_атакующих
        // Минимум 1 урона за удар
        public static int Calculate(Squad attacker, Squad defender)
        {
            if (attacker.Count <= 0) return 0;

            float atk = attacker.Type.Attack;
            float def = defender.Type.Defense;
            float avgDamage = (attacker.Type.DamageMin + attacker.Type.DamageMax) * 0.5f;
            float ratio = atk / (atk + def);

            int total = Mathf.Max(1, Mathf.RoundToInt(ratio * avgDamage * attacker.Count));
            return total;
        }
    }
}
