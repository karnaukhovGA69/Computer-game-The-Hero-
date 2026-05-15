using System.Linq;

namespace TheHero.Generated
{
    public static class RecruitmentService
    {
        public static bool CanRecruit(THGameState state, THBuildingData building, int count)
        {
            if (state == null || building == null || count <= 0)
                return false;

            return building.recruitsAvailable >= count &&
                   state.gold >= building.goldCost * count &&
                   state.wood >= building.woodCost * count &&
                   state.stone >= building.stoneCost * count &&
                   state.mana >= building.manaCost * count;
        }

        public static bool Recruit(THGameState state, THBuildingData building, int count)
        {
            if (!CanRecruit(state, building, count))
                return false;

            state.gold -= building.goldCost * count;
            state.wood -= building.woodCost * count;
            state.stone -= building.stoneCost * count;
            state.mana -= building.manaCost * count;
            building.recruitsAvailable -= count;

            var unit = state.army.FirstOrDefault(u => u.id == building.id);
            if (unit == null)
            {
                unit = new THArmyUnit { id = building.id, name = building.name, count = 0 };
                state.army.Add(unit);
            }

            unit.count += count;
            state.unitsRecruited += count;
            return true;
        }
    }
}
