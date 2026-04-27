using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TheHero.Subsystems.Map
{
    public class HeroMover : MonoBehaviour
    {
        public const int MovementPointsPerTurn = 10;

        // Секунд на одну клетку
        [SerializeField] private float _stepDuration = 0.15f;

        public int MovementPoints { get; private set; } = MovementPointsPerTurn;

        public bool IsMoving { get; private set; }

        // Вызывается при достижении конечной точки; аргумент — целевая позиция
        public event Action<Vector2Int> OnDestinationReached;

        public void RestoreMovement() => MovementPoints = MovementPointsPerTurn;

        // Начать движение по пути. Каждая клетка стоит 1 очко движения.
        // Движение обрывается если очки заканчиваются раньше конца пути.
        public void StartMove(List<Vector2Int> path, Func<Vector2Int, Vector3> tileToWorld)
        {
            if (IsMoving || path == null || path.Count == 0) return;
            StartCoroutine(MoveAlongPath(path, tileToWorld));
        }

        private IEnumerator MoveAlongPath(List<Vector2Int> path, Func<Vector2Int, Vector3> tileToWorld)
        {
            IsMoving = true;
            Vector2Int lastPos = path[0];
            bool moved = false;

            foreach (var tile in path)
            {
                if (MovementPoints <= 0) break;

                Vector3 target = tileToWorld(tile);
                float elapsed = 0f;
                Vector3 start = transform.position;

                while (elapsed < _stepDuration)
                {
                    transform.position = Vector3.Lerp(start, target, elapsed / _stepDuration);
                    elapsed += Time.deltaTime;
                    yield return null;
                }

                transform.position = target;
                MovementPoints--;
                lastPos = tile;
                moved = true;
            }

            IsMoving = false;
            if (moved) OnDestinationReached?.Invoke(lastPos);
        }
    }
}
