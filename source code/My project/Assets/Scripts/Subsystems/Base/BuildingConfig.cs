using System;
using TheHero.Domain;

namespace TheHero.Subsystems.Base
{
    // Конфиг постройки, загружается из JSON (Assets/Resources/Config/buildings.json)
    [Serializable]
    public class BuildingConfig
    {
        public string Id;
        public string DisplayName;

        // Юнит, нанимаемый на уровне 1 и 2
        public string UnitTypeIdLevel1;
        public string UnitTypeIdLevel2;

        // Сколько юнитов накапливается каждую неделю (на уровне 1)
        public int WeeklyRecruitLevel1;
        // На уровне 2 прирост больше
        public int WeeklyRecruitLevel2;

        // Стоимость улучшения постройки до уровня 2
        public ResourceWallet UpgradeCost;

        // Стоимость апгрейда одного уже нанятого юнита уровня 1 до уровня 2
        public ResourceWallet UnitUpgradeCostPerUnit;
    }
}
