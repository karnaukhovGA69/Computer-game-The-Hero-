using System;
using System.Collections.Generic;
using UnityEngine;
using TheHero.Domain;

namespace TheHero.Subsystems.Base
{
    public class BaseController : MonoBehaviour
    {
        private RecruitmentService _service;
        private BaseState _baseState;
        private GameState _gameState;

        // Вызывается после успешного найма: buildingId, количество нанятых
        public event Action<string, int> OnRecruited;
        // Вызывается после улучшения постройки
        public event Action<string> OnBuildingUpgraded;
        // Вызывается после апгрейда юнитов
        public event Action<string, int> OnUnitsUpgraded;
        // Вызывается при любом изменении состояния для обновления UI
        public event Action OnStateChanged;

        public void Initialize(RecruitmentService service, GameState gameState)
        {
            _service = service;
            _gameState = gameState;
            _baseState = gameState.Base as BaseState
                ?? throw new InvalidOperationException("GameState.Base должен быть BaseState");
        }

        public bool Recruit(string buildingId, int count)
        {
            bool ok = _service.Recruit(buildingId, count, _gameState.Wallet, _baseState, _gameState.Hero.Army);
            if (ok)
            {
                OnRecruited?.Invoke(buildingId, count);
                OnStateChanged?.Invoke();
            }
            return ok;
        }

        public bool UpgradeBuilding(string buildingId)
        {
            bool ok = _service.UpgradeBuilding(buildingId, _gameState.Wallet, _baseState);
            if (ok)
            {
                OnBuildingUpgraded?.Invoke(buildingId);
                OnStateChanged?.Invoke();
            }
            return ok;
        }

        public bool UpgradeExistingUnits(string buildingId, int count)
        {
            bool ok = _service.UpgradeExistingUnits(buildingId, count, _gameState.Wallet, _baseState, _gameState.Hero.Army);
            if (ok)
            {
                OnUnitsUpgraded?.Invoke(buildingId, count);
                OnStateChanged?.Invoke();
            }
            return ok;
        }

        public bool CanRecruit(string buildingId, int count) =>
            _service.CanRecruit(buildingId, count, _gameState.Wallet, _baseState);

        public BuildingState GetBuilding(string buildingId) =>
            _baseState.GetBuilding(buildingId);

        public List<BuildingState> GetAllBuildings() => _baseState.Buildings;

        public UnitType GetCurrentUnitType(string buildingId) =>
            _service.GetCurrentUnitType(buildingId, _baseState);
    }
}
