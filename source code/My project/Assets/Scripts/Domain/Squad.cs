using System;

namespace TheHero.Domain
{
    [Serializable]
    public class Squad
    {
        public UnitType Type;

        // Количество юнитов в отряде
        public int Count;

        // HP первого юнита в стеке (остальные считаются с полным HP)
        public int FirstUnitHP;

        public Squad() { }

        public Squad(UnitType type, int count)
        {
            Type = type;
            Count = count;
            FirstUnitHP = type.MaxHP;
        }

        public bool IsAlive => Count > 0;

        // Суммарный HP отряда
        public int TotalHP => Count > 0 ? FirstUnitHP + (Count - 1) * Type.MaxHP : 0;

        // Применить урон к отряду, вернуть количество погибших
        public int ApplyDamage(int damage)
        {
            int before = Count;
            int remaining = FirstUnitHP - damage;

            if (remaining <= 0)
            {
                // Сколько полных юнитов убито сверх первого
                int killed = 1 + (int)Math.Ceiling((-remaining) / (double)Type.MaxHP);
                killed = Math.Min(killed, Count);
                Count -= killed;

                if (Count > 0)
                    // HP нового первого юнита
                    FirstUnitHP = Type.MaxHP - (int)((-remaining) % Type.MaxHP == 0
                        ? 0
                        : Type.MaxHP - (-remaining) % Type.MaxHP);
                else
                    FirstUnitHP = 0;
            }
            else
            {
                FirstUnitHP = remaining;
            }

            return before - Count;
        }
    }
}
