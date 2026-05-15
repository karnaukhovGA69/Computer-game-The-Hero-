using UnityEngine;

namespace TheHero.Generated
{
    /// <summary>
    /// Компонент ресурса на карте (золото, дерево, камень, мана, сундук).
    /// </summary>
    public class THResource : MonoBehaviour
    {
        public string resourceType = "gold"; // gold, wood, stone, mana, chest
        public int amount = 100;
        public bool collected = false;

        private void OnMouseEnter()
        {
            if (THSingleMapHoverLabel.Instance != null)
            {
                string label = GetLabel();
                THSingleMapHoverLabel.Instance.Show(label, transform.position);
            }
        }

        private void OnMouseExit()
        {
            if (THSingleMapHoverLabel.Instance != null)
                THSingleMapHoverLabel.Instance.Hide();
        }

        private string GetLabel()
        {
            switch (resourceType)
            {
                case "gold":  return "Золото (" + amount + ")";
                case "wood":  return "Дерево (" + amount + ")";
                case "stone": return "Камень (" + amount + ")";
                case "mana":  return "Мана (" + amount + ")";
                case "chest": return "Сундук (" + amount + " gold)";
                default:      return resourceType + " (" + amount + ")";
            }
        }

        /// <summary>Собрать ресурс и добавить в GameState.</summary>
        public void Collect(THGameState state)
        {
            if (collected || state == null) return;
            collected = true;

            switch (resourceType)
            {
                case "gold":  state.gold  += amount; break;
                case "wood":  state.wood  += amount; break;
                case "stone": state.stone += amount; break;
                case "mana":  state.mana  += amount; break;
                case "chest": state.gold  += amount; break;
            }

            state.resourcesCollected++;
            gameObject.SetActive(false);
        }
    }
}