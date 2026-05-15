using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace TheHero.Generated
{
    public class THMiniMap : MonoBehaviour
    {
        public static THMiniMap Instance { get; private set; }

        public RectTransform Container;
        public GameObject PixelPrefab;

        private Image[,] pixels = new Image[16, 10];

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Initialize()
        {
            if (!Container || !PixelPrefab) return;

            foreach (Transform child in Container) Destroy(child.gameObject);

            float w = Container.rect.width / 16f;
            float h = Container.rect.height / 10f;

            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    var go = Instantiate(PixelPrefab, Container);
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchoredPosition = new Vector2(x * w, y * h);
                    rt.sizeDelta = new Vector2(w, h);
                    pixels[x, y] = go.GetComponent<Image>();
                    pixels[x, y].color = Color.black;
                }
            }
            Refresh();
        }

        public void Refresh()
        {
            if (pixels[0, 0] == null) return;

            var state = THMapController.Instance.State;

            // Reset colors (base colors from tiles)
            for (int y = 0; y < 10; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    pixels[x, y].color = new Color(0.2f, 0.2f, 0.2f);
                }
            }

            // In a real scenario, we'd check tiles. Here we just set some markers.
            // Hero
            SetPixel((int)state.heroX, (int)state.heroY, Color.blue);
            
            // Base (1,1)
            SetPixel(1, 1, Color.yellow);

            // Mine (11,2)
            if (!state.capturedObjectIds.Contains("Mine")) SetPixel(11, 2, Color.black);
            else SetPixel(11, 2, Color.grey);

            // DarkLord (15,9)
            if (!state.defeatedEnemyIds.Contains("DarkLord")) SetPixel(15, 9, Color.red);
        }

        private void SetPixel(int x, int y, Color c)
        {
            if (x >= 0 && x < 16 && y >= 0 && y < 10)
                pixels[x, y].color = c;
        }
    }
}