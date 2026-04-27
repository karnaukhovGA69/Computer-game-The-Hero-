using System;

namespace TheHero.Subsystems.Map
{
    public enum MapObjectType
    {
        Resource,
        Enemy,
        StaticEnemy,
        Base,
        NeutralBuilding
    }

    public enum MapObjectState
    {
        Active,
        Collected,
        Defeated,
        Captured
    }

    [Serializable]
    public class MapObject
    {
        public string Id;
        public MapObjectType Type;
        public MapObjectState State;

        // Произвольные данные объекта (ключ конфига врага, тип ресурса и т.д.)
        public string DataKey;

        public bool IsInteractable => State == MapObjectState.Active;
    }
}
