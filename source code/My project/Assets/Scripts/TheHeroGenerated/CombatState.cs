using System;
using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    [Serializable]
    public sealed class CombatState
    {
        public List<THCombatUnit> PlayerUnits = new List<THCombatUnit>();
        public List<THCombatUnit> EnemyUnits = new List<THCombatUnit>();
        public int Round = 1;
        public THCombatUnit ActiveUnit;
        public readonly TurnQueue TurnQueue = new TurnQueue();

        public bool IsOver => !PlayerUnits.Any(u => u != null && u.IsAlive) ||
                              !EnemyUnits.Any(u => u != null && u.IsAlive);

        public bool PlayerWon => EnemyUnits.Count > 0 && !EnemyUnits.Any(u => u != null && u.IsAlive);

        public void RebuildTurnQueue()
        {
            TurnQueue.Rebuild(PlayerUnits, EnemyUnits);
            ActiveUnit = TurnQueue.GetNext();
        }
    }
}
