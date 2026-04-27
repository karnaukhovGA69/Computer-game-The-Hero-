using System;

namespace TheHero.Domain
{
    [Serializable]
    public class UnitType
    {
        public string Id;
        public string DisplayName;
        public string SpriteKey;

        public int MaxHP;
        public int Attack;
        public int Defense;
        public int DamageMin;
        public int DamageMax;
        public int Initiative;

        // Стоимость найма одной единицы
        public ResourceWallet HireCost;

        // Прирост в неделю на незанятой базе
        public int WeeklyGrowth;
    }
}
