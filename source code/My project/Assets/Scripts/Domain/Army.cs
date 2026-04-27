using System;
using System.Collections.Generic;

namespace TheHero.Domain
{
    [Serializable]
    public class Army
    {
        public const int MaxSlots = 6;

        public List<Squad> Slots = new List<Squad>(MaxSlots);

        // Добавить отряд. Если тип уже есть — объединить. Вернуть false если слоты заняты.
        public bool Add(Squad squad)
        {
            foreach (var existing in Slots)
            {
                if (existing.Type.Id == squad.Type.Id)
                {
                    existing.Count += squad.Count;
                    return true;
                }
            }

            if (Slots.Count >= MaxSlots)
                return false;

            Slots.Add(squad);
            return true;
        }

        // Удалить отряд из слота по индексу
        public void RemoveAt(int index)
        {
            if (index >= 0 && index < Slots.Count)
                Slots.RemoveAt(index);
        }

        // Убрать все мёртвые отряды
        public void ClearDead()
        {
            Slots.RemoveAll(s => !s.IsAlive);
        }

        public bool IsAlive => Slots.Exists(s => s.IsAlive);

        public int SlotCount => Slots.Count;
    }
}
