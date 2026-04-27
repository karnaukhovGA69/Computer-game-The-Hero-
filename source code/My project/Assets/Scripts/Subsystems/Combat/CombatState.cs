using System;
using System.Collections.Generic;
using TheHero.Domain;

namespace TheHero.Subsystems.Combat
{
    [Serializable]
    public class CombatState
    {
        public List<Squad> PlayerSquads = new List<Squad>();
        public List<Squad> EnemySquads = new List<Squad>();

        public TurnQueue TurnQueue;
        public int CurrentTurn;

        // Отряд, ожидающий выбора цели игроком (null — ход врага или очередь пуста)
        public Squad PendingAttacker;
        public bool IsPlayerTurn => PendingAttacker != null && IsPlayerSquad(PendingAttacker);

        public bool IsPlayerSquad(Squad squad) => PlayerSquads.Contains(squad);

        public bool IsOver =>
            !PlayerSquads.Exists(s => s.IsAlive) ||
            !EnemySquads.Exists(s => s.IsAlive);
    }
}
