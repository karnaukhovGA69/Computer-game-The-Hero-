using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TheHero.Generated
{
    public class THCombatBattleVisuals : MonoBehaviour
    {
        public Image playerUnitImage;
        public Image enemyUnitImage;

        private void Start()
        {
            CreateVisuals();
            UpdateVisuals();
        }

        private void CreateVisuals()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            var container = new GameObject("BattlefieldVisuals", typeof(RectTransform));
            container.transform.SetParent(canvas.transform, false);
            container.transform.SetSiblingIndex(2); // Above background, below UI
            var rt = container.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(1000, 400);
            rt.anchoredPosition = new Vector2(0, 50);

            playerUnitImage = CreateUnitDisplay(container.transform, "PlayerDisplay", new Vector2(0.2f, 0.5f));
            enemyUnitImage = CreateUnitDisplay(container.transform, "EnemyDisplay", new Vector2(0.8f, 0.5f));
            enemyUnitImage.transform.localScale = new Vector3(-1, 1, 1); // Flip enemy
        }

        private Image CreateUnitDisplay(Transform parent, string name, Vector2 anchor)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(300, 300);
            return img;
        }

        public void UpdateVisuals()
        {
            var controller = Object.FindAnyObjectByType<THCombatController>();
            if (controller == null || controller.State == null) return;

            var playerSquad = controller.State.army.OrderByDescending(u => u.initiative).FirstOrDefault(u => u.count > 0);
            var enemySquad = controller.State.currentEnemyArmy.OrderByDescending(u => u.initiative).FirstOrDefault(u => u.count > 0);

            if (playerSquad != null)
                playerUnitImage.sprite = Resources.Load<Sprite>($"Sprites/Units/{playerSquad.id}_battle");
            
            if (enemySquad != null)
                enemyUnitImage.sprite = Resources.Load<Sprite>($"Sprites/Units/{enemySquad.id}_battle");
                
            playerUnitImage.gameObject.SetActive(playerSquad != null);
            enemyUnitImage.gameObject.SetActive(enemySquad != null);
        }
    }
}
