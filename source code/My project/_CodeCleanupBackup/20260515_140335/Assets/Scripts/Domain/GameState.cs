using System;
using TheHero.Subsystems.Base;

namespace TheHero.Domain
{
    [Serializable]
    public class GameState
    {
        public HeroState Hero = new HeroState();
        public ResourceWallet Wallet = new ResourceWallet();

        public MapState Map = new MapState();
        public BaseState Base = new BaseState();

        public int Day = 1;
        public int Week = 1;

        // Глобальный порядковый номер хода (1-based)
        public int TurnNumber = 1;

        public void AdvanceTurn()
        {
            TurnNumber++;
            Day++;
            if (Day > 7)
            {
                Day = 1;
                Week++;
            }
            Hero.RestoreMovement();
        }
    }

    // Заглушка карты — будет расширена в MapModule
    [Serializable]
    public class MapState
    {
        public int Seed;
        public int Width;
        public int Height;
    }
}
