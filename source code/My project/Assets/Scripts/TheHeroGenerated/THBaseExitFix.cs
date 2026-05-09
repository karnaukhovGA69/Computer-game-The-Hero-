using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

namespace TheHero.Generated
{
    public class THBaseExitFix : MonoBehaviour
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
                backButton.onClick.RemoveAllListeners();
                backButton.onClick.AddListener(ExitToBase);
            }
        }

        private Button FindBackButton()
        {
            // Try to find by name
            GameObject go = GameObject.Find("BackToMapButton");
            if (go == null) go = GameObject.Find("BackButton");
            if (go == null) go = GameObject.Find("CloseButton");
            
            if (go != null) return go.GetComponent<Button>();
            return null;
        }

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                ExitToBase();
            }
        }

        public void ExitToBase()
        {
            if (THManager.Instance != null)
            {
                THManager.Instance.SaveGame();
            }

            if (SceneManager.GetActiveScene().name == "Base")
            {
                SceneManager.LoadScene("Map");
            }
            else
            {
                // If it's a popup, try to disable the parent panel
                gameObject.SetActive(false);
            }
        }
    }
}
