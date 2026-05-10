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
                case "bridge":
                    walkable = true;
                    moveCost = 1;
                    break;
                case "forest_edge":
                case "forest_sparse":
                case "hill":
                case "darkland":
                    walkable = true;
                    moveCost = 2;
                    break;
                case "forest_dense":
                case "swamp":
                    walkable = true;
                    moveCost = 3;
                    break;
                case "mountain":
                case "water":
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
