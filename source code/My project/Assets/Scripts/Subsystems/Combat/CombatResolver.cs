using System.Collections.Generic;
using System.Linq;
using TheHero.Domain;

namespace TheHero.Subsystems.Combat
{
    public class CombatResolver
    {
        private CombatState _state;

        // Инициализировать бой, вернуть начальное состояние
        public CombatState InitCombat(List<Squad> playerSquads, List<Squad> enemySquads)
        {
            _state = new CombatState();
            _state.PlayerSquads = playerSquads;
            _state.EnemySquads = enemySquads;
            _state.TurnQueue = new TurnQueue(playerSquads, enemySquads);
            _state.CurrentTurn = 1;

            // Определить первый ход
            _state.PendingAttacker = _state.TurnQueue.GetNext();
            return _state;
        }

        // Выполнить ход: attacker атакует target. Вызывать когда PendingAttacker определён.
        // Возвращает количество погибших юнитов в отряде защитника.
        public int ProcessTurn(Squad target)
        {
            var attacker = _state.PendingAttacker;
            int killed = ApplyDamage(attacker, target);

            _state.CurrentTurn++;

            // Определить следующий атакующий
            if (!_state.IsOver)
                _state.PendingAttacker = _state.TurnQueue.GetNext();
            else
                _state.PendingAttacker = null;

            return killed;
        }

        // Нанести урон, вернуть количество погибших
        public int ApplyDamage(Squad attacker, Squad defender)
        {
            int damage = DamageCalculator.Calculate(attacker, defender);
            return defender.ApplyDamage(damage);
        }

        // Автоматический ход ИИ: атакует случайный живой отряд игрока
        // Возвращает (цель, убитых)
        public (Squad target, int killed) ProcessEnemyTurn()
        {
            var alive = _state.PlayerSquads.FindAll(s => s.IsAlive);
            if (alive.Count == 0) return (null, 0);

            // ИИ атакует отряд с наибольшим кол-вом юнитов
            var target = alive.OrderByDescending(s => s.Count).First();
            int killed = ProcessTurn(target);
            return (target, killed);
        }

        public CombatResult CheckVictory(Reward reward)
        {
            bool playerWon = _state.EnemySquads.All(s => !s.IsAlive);
            var survivors = playerWon
                ? _state.PlayerSquads.FindAll(s => s.IsAlive)
                : new List<Squad>();

            return new CombatResult
            {
                PlayerWon = playerWon,
                SurvivedSquads = survivors,
                Reward = playerWon ? reward : null
            };
        }
    }
}
