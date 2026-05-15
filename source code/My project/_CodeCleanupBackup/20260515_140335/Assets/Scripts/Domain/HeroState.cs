using System;

namespace TheHero.Domain
{
    [Serializable]
    public class HeroState
    {
        public string Name;
        public int Level;
        public int Experience;

        // Позиция на карте
        public int MapX;
        public int MapY;

        // Очки движения на текущий ход
        public int MovementPoints;
        public int MaxMovementPoints;

        public Army Army = new Army();

        // Опыт, необходимый для следующего уровня
        public int ExperienceToNextLevel => Level * 100;

        // Добавить опыт, вернуть true если произошёл левел-ап
        public bool AddExperience(int amount)
        {
            Experience += amount;
            if (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                return true;
            }
            return false;
        }

        public void RestoreMovement() => MovementPoints = MaxMovementPoints;
    }
}
