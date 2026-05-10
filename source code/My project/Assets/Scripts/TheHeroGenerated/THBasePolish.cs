using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TheHero.Generated
{
    public class THBasePolish : MonoBehaviour
    {
        private void Start()
        {
            ApplyPolish();
        }

        public void ApplyPolish()
        {
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Update Building Cards
            string[,] mapping = {
                { "Card_Barracks", "unit_swordsman" },
                { "Card_Range", "unit_archer" },
                { "Card_MageTower", "unit_mage" }
            };

            for (int i = 0; i < mapping.GetLength(0); i++)
            {
                var cardName = mapping[i, 0];
                var unitId = mapping[i, 1];
                var cardGo = GameObject.Find(cardName);
                if (cardGo != null)
                {
                    var portraitGo = cardGo.transform.Find("Portrait")?.gameObject;
                    if (portraitGo == null)
                    {
                        portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(Image));
                        portraitGo.transform.SetParent(cardGo.transform, false);
                        var rt = portraitGo.GetComponent<RectTransform>();
                        rt.anchorMin = new Vector2(0, 0.5f);
                        rt.anchorMax = new Vector2(0, 0.5f);
                        rt.pivot = new Vector2(0, 0.5f);
                        rt.sizeDelta = new Vector2(60, 60);
                        rt.anchoredPosition = new Vector2(10, 0);
                    }
                    var img = portraitGo.GetComponent<Image>();
                    img.sprite = Resources.Load<Sprite>($"Sprites/Units/{unitId}_portrait");
                }
            }
        }
    }
}
