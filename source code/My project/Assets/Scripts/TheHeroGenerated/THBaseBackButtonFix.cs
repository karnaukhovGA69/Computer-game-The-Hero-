using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace TheHero.Generated
{
    public class THBaseBackButtonFix : MonoBehaviour
    {
        public Button backButton;

        private void Start()
        {
            if (backButton == null)
            {
                backButton = FindBackButton();
            }

            if (backButton != null)
            {
                SetupButton(backButton);
            }
        }

        private void SetupButton(Button btn)
        {
            var rt = btn.GetComponent<RectTransform>();
            // Resize to 120x40
            rt.sizeDelta = new Vector2(120, 40);
            
            // Positioning - Bottom Left (Option 2 from request)
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 0);
            rt.pivot = new Vector2(0, 0);
            rt.anchoredPosition = new Vector2(20, 20);

            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null)
            {
                txt.text = "На карту";
                txt.fontSize = 20;
            }

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(BackToMap);
        }

        private Button FindBackButton()
        {
            // Search in children first
            var buttons = GetComponentsInChildren<Button>(true);
            foreach (var b in buttons)
            {
                if (b.name.Contains("Back") || b.name.Contains("Exit") || b.name.Contains("Map"))
                    return b;
            }
            return null;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                BackToMap();
            }
        }

        public void BackToMap()
        {
            if (THManager.Instance != null) THManager.Instance.SaveGame();

            if (SceneManager.GetActiveScene().name == "Base")
            {
                SceneManager.LoadScene("Map");
            }
            else
            {
                // If it's a popup in Map scene
                gameObject.SetActive(false);
            }
        }
    }
}
