using System;
using System.Collections.Generic;
using UnityEngine;
using TheHero.Domain;

namespace TheHero.Subsystems.Combat
{
    public class CombatController : MonoBehaviour
    {
        private CombatResolver _resolver;
        private CombatState _state;
        private Reward _victoryReward;

        // squad — атакующий, target — цель, killed — погибших в цели
        public event Action<Squad, Squad, int> OnSquadAttacked;
        // squad — чей ход начался
        public event Action<Squad> OnTurnStarted;
        public event Action<CombatResult> OnCombatEnded;

        public void StartCombat(List<Squad> playerSquads, List<Squad> enemySquads, Reward reward)
        {
            _resolver = new CombatResolver();
            _victoryReward = reward;
            _state = _resolver.InitCombat(playerSquads, enemySquads);

            NotifyTurnStarted();
        }

        // Игрок выбрал цель для своего отряда
        public void PlayerSelectTarget(Squad target)
        {
            if (_state == null || !_state.IsPlayerTurn) return;

            var attacker = _state.PendingAttacker;
            int killed = _resolver.ProcessTurn(target);
            OnSquadAttacked?.Invoke(attacker, target, killed);

            if (_state.IsOver) { EndCombat(); return; }

            // Если следующий ход — вражеский, выполнить автоматически
            ProcessEnemyTurnsIfNeeded();
        }

        // Выполнять ходы врагов пока не наступит ход игрока или бой не завершится
        private void ProcessEnemyTurnsIfNeeded()
        {
            while (_state != null && !_state.IsOver && !_state.IsPlayerTurn)
            {
                NotifyTurnStarted();
                var (target, killed) = _resolver.ProcessEnemyTurn();
                if (target != null)
                    OnSquadAttacked?.Invoke(_state.PendingAttacker, target, killed);

                if (_state.IsOver) { EndCombat(); return; }
            }

            if (_state != null && !_state.IsOver)
                NotifyTurnStarted();
        }

        private void EndCombat()
        {
            var result = _resolver.CheckVictory(_victoryReward);
            OnCombatEnded?.Invoke(result);
            _state = null;
        }

        private void NotifyTurnStarted()
        {
            if (_state?.PendingAttacker != null)
                OnTurnStarted?.Invoke(_state.PendingAttacker);
        }

        public CombatState GetState() => _state;

        // Список живых целей для отображения в UI выбора
        public List<Squad> GetValidTargets() =>
            _state != null && _state.IsPlayerTurn
                ? _state.EnemySquads.FindAll(s => s.IsAlive)
                : new List<Squad>();
    }
}
