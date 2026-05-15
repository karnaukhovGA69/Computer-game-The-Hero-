using System.Collections.Generic;
using TheHero.Domain;

namespace TheHero.Subsystems.Combat
{
    public class CombatResult
    {
        public bool PlayerWon;
        public List<Squad> SurvivedSquads;
        public Reward Reward;
    }
}
