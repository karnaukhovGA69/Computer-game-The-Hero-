using System;
using System.Collections.Generic;

namespace TheHero.Domain
{
    public enum ResourceType { Gold, Wood, Stone, Mana }

    [Serializable]
    public class ResourceWallet
    {
        // Словарь хранится как параллельные списки для совместимости с Newtonsoft.Json
        public Dictionary<ResourceType, int> Values = new Dictionary<ResourceType, int>
        {
            { ResourceType.Gold,  0 },
            { ResourceType.Wood,  0 },
            { ResourceType.Stone, 0 },
            { ResourceType.Mana,  0 },
        };

        public int Get(ResourceType type) =>
            Values.TryGetValue(type, out var v) ? v : 0;

        public void Add(ResourceType type, int amount) =>
            Values[type] = Get(type) + amount;

        public void Add(ResourceWallet other)
        {
            foreach (var kv in other.Values)
                Add(kv.Key, kv.Value);
        }

        public bool CanAfford(ResourceWallet cost)
        {
            foreach (var kv in cost.Values)
                if (Get(kv.Key) < kv.Value) return false;
            return true;
        }

        // Вернуть false если недостаточно ресурсов
        public bool Spend(ResourceWallet cost)
        {
            if (!CanAfford(cost)) return false;
            foreach (var kv in cost.Values)
                Values[kv.Key] = Get(kv.Key) - kv.Value;
            return true;
        }
    }
}
