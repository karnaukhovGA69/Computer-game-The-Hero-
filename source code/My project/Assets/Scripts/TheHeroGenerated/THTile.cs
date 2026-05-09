using UnityEngine;

namespace TheHero.Generated
{
    public class THTile : MonoBehaviour
    {
        public int x;
        public int y;
        public bool walkable = true;
        public int moveCost = 1;
        public string tileType;

        public void Setup(int x, int y, string type)
        {
            this.x = x;
            this.y = y;
            this.tileType = type.ToLower();
            
            switch (tileType)
            {
                case "grass":
                case "road":
                    walkable = true;
                    moveCost = 1;
                    break;
                case "forest":
                    walkable = true;
                    moveCost = 2;
                    break;
                case "water":
                case "mountain":
                    walkable = false;
                    moveCost = 999;
                    break;
                default:
                    walkable = true;
                    moveCost = 1;
                    break;
            }
        }
    }
}
