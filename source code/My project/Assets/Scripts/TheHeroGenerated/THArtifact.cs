using UnityEngine;

namespace TheHero.Generated
{
    /// <summary>
    /// Компонент артефакта на карте.
    /// </summary>
    public class THArtifact : MonoBehaviour
    {
        public string artifactName = "Ancient Artifact";
        public string description  = "";
        public bool collected = false;

        public int attackBonus  = 0;
        public int defenseBonus = 0;
        public int moveBonus    = 0;
        public float expMultiplier = 1f;
        public int manaIncome   = 0;

        private void Awake()
        {
            if (string.IsNullOrWhiteSpace(artifactName))
                artifactName = gameObject.name;
        }

        private void OnMouseEnter()
        {
            if (THSingleMapHoverLabel.Instance != null)
                THSingleMapHoverLabel.Instance.Show(
                    "Артефакт: " + artifactName,
                    transform.position);
        }

        private void OnMouseExit()
        {
            if (THSingleMapHoverLabel.Instance != null)
                THSingleMapHoverLabel.Instance.Hide();
        }

        /// <summary>Подобрать артефакт и применить бонусы.</summary>
        public void Collect(THGameState state)
        {
            if (collected || state == null) return;
            collected = true;

            state.mana += manaIncome;

            string id = gameObject.name;
            if (!state.heroArtifactIds.Contains(id))
                state.heroArtifactIds.Add(id);

            gameObject.SetActive(false);
        }
    }
}