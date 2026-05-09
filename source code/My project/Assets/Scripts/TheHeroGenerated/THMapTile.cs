using UnityEngine;
using UnityEngine.EventSystems;

namespace TheHero.Generated
{
    public class THMapTile : MonoBehaviour, IPointerDownHandler
    {
        public int x, y;
        public int moveCost = 1;
        public bool isPassable = true;
        public string tileType;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (THMapController.Instance == null || THMapController.Instance.HeroMover == null) return;
            if (THMapController.Instance.HeroMover.IsMoving) return;
            THMapController.Instance.HeroMover.TryMoveTo(x, y);
        }
    }
}