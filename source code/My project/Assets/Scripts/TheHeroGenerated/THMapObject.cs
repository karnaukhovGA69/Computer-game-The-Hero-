using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;

namespace TheHero.Generated
{
    public class THMapObject : MonoBehaviour, IPointerDownHandler
    {
        public enum ObjectType { GoldResource, WoodResource, StoneResource, ManaResource, Enemy, Base, Mine, NeutralBuilding, Treasure, Shrine, ArtifactChest, Artifact }
public string id;
public ObjectType type;
public THEnemyDifficulty difficulty = THEnemyDifficulty.Weak;
public bool blocksMovement = true;
public bool startsCombat = true;
public bool isFinalBoss = false;
public int rewardGold;
public int rewardWood;
        public int rewardStone;
        public int rewardMana;
        public int rewardExp;
        public List<THArmyUnit> enemyArmy = new List<THArmyUnit>();
        public int targetX, targetY;
        public string displayName;
        public bool isDarkLord = false;

        public int GetArmyPower() => enemyArmy.Sum(u => u.GetPower());

        public void OnPointerDown(PointerEventData eventData)
        {
            if (THMapController.Instance == null || THMapController.Instance.HeroMover == null) return;
            if (THMapController.Instance.HeroMover.IsMoving) return;

            if (isDarkLord)
            {
                int playerPower = THMapController.Instance.State.army.Sum(u => u.GetPower());
                int enemyPower = GetArmyPower();
                
                if (playerPower < enemyPower * 0.8f)
                {
                    THMapController.Instance.ShowConfirmation("Враг очень силён. Вы уверены?", () => {
                        THMapController.Instance.HeroMover.TryMoveTo(targetX, targetY, this);
                    });
                    return;
                }
            }

            THMapController.Instance.HeroMover.TryMoveTo(targetX, targetY, this);
        }

        private void OnMouseEnter()
        {
            if (THMapController.Instance)
            {
                if (type == ObjectType.Enemy)
                {
                    string diffStr = difficulty switch
                    {
                        THEnemyDifficulty.Weak => "<color=green>Слабый</color>",
                        THEnemyDifficulty.Medium => "<color=yellow>Средний</color>",
                        THEnemyDifficulty.Strong => "<color=orange>Сильный</color>",
                        THEnemyDifficulty.Deadly => "<color=red>Смертельно опасный</color>",
                        _ => "Неизвестно"
                    };
                    
                    string info = $"<b>{displayName}</b>\nСила: {diffStr}";
                    if (rewardExp > 0 || rewardGold > 0)
                        info += $"\nНаграда: {(rewardExp > 0 ? $"{rewardExp} XP " : "")}{(rewardGold > 0 ? $"{rewardGold} Gold" : "")}";
                    
                    THMapController.Instance.Log(info);
                }
                else
                {
                    THMapController.Instance.Log(displayName);
                }
            }
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
