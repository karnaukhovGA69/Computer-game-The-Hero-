using System.Collections.Generic;
using System.Linq;

namespace TheHero.Generated
{
    public sealed class TurnQueue
    {
        private readonly List<THCombatUnit> _units = new List<THCombatUnit>();
        private int _index;

        public IReadOnlyList<THCombatUnit> Units => _units;

        public void Rebuild(IEnumerable<THCombatUnit> playerUnits, IEnumerable<THCombatUnit> enemyUnits)
        {
            _units.Clear();
            if (playerUnits != null) _units.AddRange(playerUnits.Where(u => u != null && u.IsAlive));
            if (enemyUnits != null) _units.AddRange(enemyUnits.Where(u => u != null && u.IsAlive));

            _units.Sort((a, b) =>
            {
                int initiative = b.initiative.CompareTo(a.initiative);
                return initiative != 0 ? initiative : b.isPlayer.CompareTo(a.isPlayer);
            });

            for (int i = 0; i < _units.Count; i++)
                _units[i].turnOrderIndex = i;

            _index = 0;
        }

        public THCombatUnit GetNext()
        {
            if (_units.Count == 0)
                return null;

            int checkedCount = 0;
            while (checkedCount < _units.Count)
            {
                if (_index >= _units.Count)
                    _index = 0;

                THCombatUnit unit = _units[_index++];
                checkedCount++;
                if (unit != null && unit.IsAlive)
                    return unit;
            }

            return null;
        }
    }
}
