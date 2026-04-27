using System;
using System.Collections.Generic;
using UnityEngine;
using TheHero.Domain;

namespace TheHero.Subsystems.Map
{
    public class MapController : MonoBehaviour
    {
        [SerializeField] private HeroMover _heroMover;

        // Размер клетки в мировых координатах
        [SerializeField] private float _tileSize = 1f;

        private MapGrid _grid;
        private GameState _gameState;

        // Внешний обработчик взаимодействия с объектом карты
        public event Action<MapObject> OnObjectInteract;

        public void Initialize(MapGrid grid, GameState gameState)
        {
            _grid = grid;
            _gameState = gameState;
            _heroMover.OnDestinationReached += HandleDestinationReached;
        }

        // Вызвать при клике на клетку карты (например из MapClickHandler)
        public void HandleTileClick(Vector2Int tilePos)
        {
            if (_heroMover.IsMoving) return;

            var heroPos = new Vector2Int(_gameState.Hero.MapX, _gameState.Hero.MapY);
            var path = PathFinder.FindPath(_grid, heroPos, tilePos);

            if (path == null || path.Count == 0) return;

            _heroMover.StartMove(path, TileToWorld);
        }

        private void HandleDestinationReached(Vector2Int pos)
        {
            _gameState.Hero.MapX = pos.x;
            _gameState.Hero.MapY = pos.y;

            var tile = _grid.GetTile(pos);
            if (tile?.Object != null && tile.Object.IsInteractable)
                OnObjectInteract?.Invoke(tile.Object);
        }

        private Vector3 TileToWorld(Vector2Int pos) =>
            new Vector3(pos.x * _tileSize, pos.y * _tileSize, 0f);

        private void OnDestroy()
        {
            if (_heroMover != null)
                _heroMover.OnDestinationReached -= HandleDestinationReached;
        }
    }
}
