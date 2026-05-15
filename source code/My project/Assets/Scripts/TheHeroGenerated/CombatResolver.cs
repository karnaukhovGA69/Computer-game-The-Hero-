using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public sealed class CombatResolver
    {
        public CombatState CreateState(IEnumerable<THCombatUnit> playerUnits, IEnumerable<THCombatUnit> enemyUnits)
        {
            var state = new CombatState();
            if (playerUnits != null) state.PlayerUnits = playerUnits.Where(u => u != null).ToList();
            if (enemyUnits != null) state.EnemyUnits = enemyUnits.Where(u => u != null).ToList();
            state.RebuildTurnQueue();
            return state;
        }

        public int Attack(THCombatUnit attacker, THCombatUnit defender)
        {
            return DamageCalculator.ApplyDamage(attacker, defender);
        }

        public bool HasWinner(CombatState state, out bool playerWon)
        {
            playerWon = state != null && state.PlayerWon;
            return state == null || state.IsOver;
        }
    }
}
