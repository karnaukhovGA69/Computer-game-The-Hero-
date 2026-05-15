using UnityEngine;

namespace TheHero.Generated
{
    /// <summary>
    /// Компонент героя на карте. Размещается на GameObject героя.
    /// </summary>
    public class THHero : MonoBehaviour
    {
        public string heroName = "Knight";
        public int level = 1;
        public int movementPoints = THBalanceConfig.HeroMaxMovementPoints;
        public int maxMovementPoints = THBalanceConfig.HeroMaxMovementPoints;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(heroName)) heroName = "Knight";
        }

        /// <summary>Синхронизировать позицию из GameState</summary>
        public void SyncFromState(THGameState state)
        {
            if (state == null) return;
            heroName        = state.heroName;
            level           = state.heroLevel;
            movementPoints  = state.movementPoints;
            maxMovementPoints = state.maxMovementPoints;
        }

        /// <summary>Сохранить позицию в GameState</summary>
        public void WriteToState(THGameState state)
        {
            if (state == null) return;
            state.heroX = transform.position.x;
            state.heroY = transform.position.y;
        }
    }
}
