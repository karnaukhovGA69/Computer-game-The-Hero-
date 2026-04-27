using System.Collections.Generic;
using System.Linq;
using TheHero.Domain;

namespace TheHero.Subsystems.Combat
{
    // Очередь ходов: все живые отряды отсортированы по убыванию инициативы
    public class TurnQueue
    {
        private readonly List<Squad> _all = new List<Squad>();
        private int _index;

        public TurnQueue(IEnumerable<Squad> playerSquads, IEnumerable<Squad> enemySquads)
        {
            _all.AddRange(playerSquads);
            _all.AddRange(enemySquads);
            Sort();
        }

        // Вернуть следующий живой отряд, пропустить мёртвых
        public Squad GetNext()
        {
            int checked_ = 0;
            while (checked_ < _all.Count)
            {
                if (_index >= _all.Count)
                    _index = 0;

                var squad = _all[_index];
                _index++;
                checked_++;

                if (squad.IsAlive)
                    return squad;
            }
            return null;
        }

        // Пересортировать после изменения состава (гибель отряда не удаляет его — GetNext пропустит)
        public void Sort() =>
            _all.Sort((a, b) => b.Type.Initiative.CompareTo(a.Type.Initiative));
    }
}
