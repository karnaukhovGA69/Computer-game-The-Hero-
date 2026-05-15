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

        private void Awake()
        {
            ApplyMovementBalance();
        }

        public void Setup(int x, int y, string type)
        {
            this.x = x;
            this.y = y;
            this.tileType = type.ToLower();

            ApplyMovementBalance();
        }

        public void ApplyMovementBalance()
        {
            tileType = (tileType ?? "grass").ToLower();

            switch (tileType)
            {
                case "road":
                case "bridge":
                    walkable = true;
                    moveCost = 1;
                    break;
                case "grass":
                case "meadow":
                case "plain":
                case "plains":
                case "hill":
                    walkable = true;
                    moveCost = 2;
                    break;
                case "forest_edge":
                case "forest_sparse":
                case "darkland":
                case "dark":
                case "forest":
                case "forest_dense":
                case "dense_forest":
                case "swamp":
                    walkable = true;
                    moveCost = 3;
                    break;
                case "mountain":
                case "water":
                case "river":
                    walkable = false;
                    moveCost = 999;
                    break;
                default:
                    walkable = true;
                    moveCost = 2;
                    break;
            }
        }
    }
}
