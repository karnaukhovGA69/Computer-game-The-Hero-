using System;
using System.Collections.Generic;
using TheHero.Domain;

namespace TheHero.Subsystems.Base
{
    public class RecruitmentService
    {
        private readonly Dictionary<string, BuildingConfig> _configs;
        private readonly Dictionary<string, UnitType> _unitTypes;

        public RecruitmentService(
            IEnumerable<BuildingConfig> buildingConfigs,
            IEnumerable<UnitType> unitTypes)
        {
            _configs = new Dictionary<string, BuildingConfig>();
            foreach (var c in buildingConfigs)
                _configs[c.Id] = c;

            _unitTypes = new Dictionary<string, UnitType>();
            foreach (var u in unitTypes)
                _unitTypes[u.Id] = u;
        }

        // Начислить накопленный найм за weeksElapsed недель для каждой постройки базы
        public void AccumulateWeeklyRecruits(BaseState baseState, int weeksElapsed)
        {
            if (weeksElapsed <= 0) return;
            foreach (var building in baseState.Buildings)
            {
                if (!_configs.TryGetValue(building.BuildingId, out var cfg)) continue;
                int perWeek = building.Level == 2 ? cfg.WeeklyRecruitLevel2 : cfg.WeeklyRecruitLevel1;
                building.AccumulatedRecruits += perWeek * weeksElapsed;
            }
        }

        // Проверить, можно ли нанять count юнитов из buildingId (ресурсы + доступный найм)
        public bool CanRecruit(string buildingId, int count, ResourceWallet wallet, BaseState baseState)
        {
            var building = baseState.GetBuilding(buildingId);
            if (building == null || count <= 0) return false;
            if (building.AccumulatedRecruits < count) return false;

            var cfg = GetConfig(buildingId);
            if (cfg == null) return false;

            var unitType = GetCurrentUnitType(building, cfg);
            if (unitType == null) return false;

            var totalCost = ScaleCost(unitType.HireCost, count);
            return wallet.CanAfford(totalCost);
        }

        // Нанять count юнитов: списать ресурсы, уменьшить накопленный найм, добавить в армию
        // Возвращает false если условия не выполнены
        public bool Recruit(string buildingId, int count, ResourceWallet wallet, BaseState baseState, Army army)
        {
            if (!CanRecruit(buildingId, count, wallet, baseState)) return false;

            var building = baseState.GetBuilding(buildingId);
            var cfg = GetConfig(buildingId);
            var unitType = GetCurrentUnitType(building, cfg);

            wallet.Spend(ScaleCost(unitType.HireCost, count));
            building.AccumulatedRecruits -= count;

            army.Add(new Squad(unitType, count));
            return true;
        }

        // Улучшить постройку до уровня 2. Возвращает false если недостаточно ресурсов или уже улучшена.
        public bool UpgradeBuilding(string buildingId, ResourceWallet wallet, BaseState baseState)
        {
            var building = baseState.GetBuilding(buildingId);
            if (building == null || building.IsUpgraded) return false;

            var cfg = GetConfig(buildingId);
            if (cfg == null || !wallet.CanAfford(cfg.UpgradeCost)) return false;

            wallet.Spend(cfg.UpgradeCost);
            building.Level = 2;
            return true;
        }

        // Апгрейд уже нанятых юнитов уровня 1 до юнитов уровня 2 в армии героя.
        // count — количество юнитов для апгрейда. Возвращает false если условия не выполнены.
        public bool UpgradeExistingUnits(string buildingId, int count, ResourceWallet wallet,
            BaseState baseState, Army army)
        {
            var building = baseState.GetBuilding(buildingId);
            if (building == null || !building.IsUpgraded) return false;

            var cfg = GetConfig(buildingId);
            if (cfg == null) return false;

            var unitL1 = GetUnitType(cfg.UnitTypeIdLevel1);
            var unitL2 = GetUnitType(cfg.UnitTypeIdLevel2);
            if (unitL1 == null || unitL2 == null) return false;

            // Найти отряд юнитов первого уровня в армии
            var squad = army.Slots.Find(s => s.Type.Id == unitL1.Id);
            if (squad == null || squad.Count < count) return false;

            var totalCost = ScaleCost(cfg.UnitUpgradeCostPerUnit, count);
            if (!wallet.CanAfford(totalCost)) return false;

            wallet.Spend(totalCost);
            squad.Count -= count;

            // Добавить апгрейднутых юнитов (или объединить с существующим отрядом L2)
            army.Add(new Squad(unitL2, count));

            // Удалить пустой отряд
            army.ClearDead();
            return true;
        }

        // Получить тип юнита, доступного для найма в постройке с учётом её уровня
        public UnitType GetCurrentUnitType(string buildingId, BaseState baseState)
        {
            var building = baseState.GetBuilding(buildingId);
            var cfg = GetConfig(buildingId);
            return building != null && cfg != null ? GetCurrentUnitType(building, cfg) : null;
        }

        private UnitType GetCurrentUnitType(BuildingState building, BuildingConfig cfg) =>
            GetUnitType(building.Level == 2 ? cfg.UnitTypeIdLevel2 : cfg.UnitTypeIdLevel1);

        private BuildingConfig GetConfig(string id) =>
            _configs.TryGetValue(id, out var c) ? c : null;

        private UnitType GetUnitType(string id) =>
            _unitTypes.TryGetValue(id, out var u) ? u : null;

        private static ResourceWallet ScaleCost(ResourceWallet baseCost, int multiplier)
        {
            var result = new ResourceWallet();
            foreach (var kv in baseCost.Values)
                result.Add(kv.Key, kv.Value * multiplier);
            return result;
        }
    }
}
