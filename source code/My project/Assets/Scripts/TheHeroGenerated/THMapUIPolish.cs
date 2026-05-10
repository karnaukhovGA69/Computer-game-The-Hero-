using UnityEngine;
using UnityEngine.UI;

namespace TheHero.Generated
{
    public class THMapUIPolish : MonoBehaviour
    {
        public Sprite buttonNormal;
        public Sprite panelSprite;

        private void Start()
        {
            ApplyPolish();
        }

        [ContextMenu("Apply Polish")]
        public void ApplyPolish()
{
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            if (buttonNormal == null) buttonNormal = Resources.Load<Sprite>("Sprites/UI/button_fantasy_normal");
            if (panelSprite == null) panelSprite = Resources.Load<Sprite>("Sprites/UI/panel_fantasy_dark");

            var allButtons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var b in allButtons)
            {
                var img = b.GetComponent<Image>();
                if (img == null) img = b.gameObject.AddComponent<Image>();
                if (img.sprite == null || img.sprite.name == "UISprite")
                {
                    img.sprite = buttonNormal;
                    img.type = Image.Type.Simple;
                }
                
                var txt = b.GetComponentInChildren<Text>();
                if (txt != null)
                {
                    txt.color = Color.white;
                    txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            
            // Style Top HUD if exists
            var topHud = GameObject.Find("TopHUD");
            if (topHud != null)
            {
                var img = topHud.GetComponent<Image>() ?? topHud.AddComponent<Image>();
                img.sprite = panelSprite;
                img.type = Image.Type.Sliced;
                img.color = new Color(1, 1, 1, 0.8f);
            }
        }
    }
}
