using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace TheHero.Generated
{
    public class THMapObject : MonoBehaviour, IPointerDownHandler
    {
        public enum ObjectType { GoldResource, WoodResource, StoneResource, ManaResource, Enemy, Base, Mine, NeutralBuilding, Treasure, Shrine }
        public string id;
        public ObjectType type;
        public int rewardGold;
        public int rewardWood;
        public int rewardStone;
        public int rewardMana;
        public int rewardExp;
        public List<THArmyUnit> enemyArmy = new List<THArmyUnit>();
        public int targetX, targetY;
        public string displayName;
        public bool isDarkLord = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (THMapController.Instance == null || THMapController.Instance.HeroMover == null) return;
            if (THMapController.Instance.HeroMover.IsMoving) return;
            THMapController.Instance.HeroMover.TryMoveTo(targetX, targetY, this);
        }

        private void OnMouseEnter()
        {
            if (THMapController.Instance)
                THMapController.Instance.Log(displayName);
        }

        public void SetCaptured()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr) sr.color = Color.yellow;
            displayName += " (Захвачено)";
        }

        public void SetVisited()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr) sr.color = Color.gray;
            displayName += " (Посещено)";
        }
    }
}
