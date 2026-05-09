using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TheHero.Generated
{
    public class THBaseCompactUILayout : MonoBehaviour
    {
        private void Start()
        {
            ApplyCompactLayout();
        }

        [ContextMenu("Apply Compact Layout")]
        public void ApplyCompactLayout()
        {
            // Find BuildingRow elements (they might have names like BuildingRow, Card_*, etc.)
            // Based on previous builder scripts, they might be in a BuildingsPanel
            var rows = GetComponentsInChildren<HorizontalLayoutGroup>(true)
                .Concat(GetComponentsInChildren<VerticalLayoutGroup>(true).SelectMany(v => v.GetComponentsInChildren<HorizontalLayoutGroup>(true)))
                .Distinct()
                .ToList();

            foreach (var row in rows)
            {
                var rt = row.GetComponent<RectTransform>();
                // height = 80
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 80);

                row.spacing = 10;
                row.childAlignment = TextAnchor.MiddleLeft;
                row.childControlHeight = true;
                row.childControlWidth = false;

                // Adjust buttons in the row
                var buttons = row.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    var btnRt = btn.GetComponent<RectTransform>();
                    var txt = btn.GetComponentInChildren<Text>();
                    
                    if (btn.name.Contains("1")) // Recruit 1
                    {
                        btnRt.sizeDelta = new Vector2(110, 34);
                        if (txt) { txt.text = "Нанять 1"; txt.fontSize = 18; }
                    }
                    else if (btn.name.Contains("всех") || btn.name.Contains("All")) // Recruit All
                    {
                        btnRt.sizeDelta = new Vector2(130, 34);
                        if (txt) { txt.text = "Нанять всех"; txt.fontSize = 18; }
                    }
                    else if (btn.name.Contains("Улучшить") || btn.name.Contains("Upgrade")) // Upgrade
                    {
                        btnRt.sizeDelta = new Vector2(120, 34);
                        if (txt) { txt.text = "Улучшить"; txt.fontSize = 18; }
                    }
                }
            }

            // Ensure VerticalLayoutGroup on parent
            var parentLayout = GetComponent<VerticalLayoutGroup>();
            if (parentLayout != null)
            {
                parentLayout.spacing = 8;
                parentLayout.padding = new RectOffset(10, 10, 10, 10);
                parentLayout.childControlHeight = false;
                parentLayout.childForceExpandHeight = false;
            }
        }
    }
}
