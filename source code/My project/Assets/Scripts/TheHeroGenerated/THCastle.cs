using UnityEngine;

namespace TheHero.Generated
{
    /// <summary>
    /// Компонент замка игрока на карте.
    /// </summary>
    public class THCastle : MonoBehaviour
    {
        public string castleName = "Castle";
        public bool isPlayerCastle = true;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(castleName)) castleName = "Castle";
        }

        private void OnMouseEnter()
        {
            if (THSingleMapHoverLabel.Instance != null)
                THSingleMapHoverLabel.Instance.Show(castleName, transform.position);
        }

        private void OnMouseExit()
        {
            if (THSingleMapHoverLabel.Instance != null)
                THSingleMapHoverLabel.Instance.Hide();
        }
    }
}