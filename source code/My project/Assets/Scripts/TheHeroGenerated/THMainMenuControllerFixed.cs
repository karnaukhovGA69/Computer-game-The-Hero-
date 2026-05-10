using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace TheHero.Generated
{
    public class THMainMenuControllerFixed : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject MainMenuPanel;
        public GameObject SettingsPanel;
        public GameObject HelpPanel;

        [Header("Buttons")]
        public Button NewGameButton;
        public Button ContinueButton;
        public Button SettingsButton;
        public Button HelpButton;
        public Button ExitButton;

        private void Awake()
        {
            // Initial state
            if (SettingsPanel != null) SettingsPanel.SetActive(false);
            if (HelpPanel != null) HelpPanel.SetActive(false);
            if (MainMenuPanel != null) MainMenuPanel.SetActive(true);
        }

        private void Start()
        {
            // Setup Callbacks with protection against duplicates
            WireButton(NewGameButton, OnNewGameClick);
            WireButton(ContinueButton, OnContinueClick);
            WireButton(SettingsButton, OnSettingsClick);
            WireButton(HelpButton, OnHelpClick);
            WireButton(ExitButton, OnExitClick);

            // Check save existence for Continue button
            if (ContinueButton != null)
            {
                bool hasSave = PlayerPrefs.HasKey("TheHero_SaveData");
                // For demo purposes, we can also check if a save exists in THSaveSystem if available
                ContinueButton.interactable = hasSave;
            }
        }

        private void WireButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(action);
                
                // Add click sound if AudioManager exists
                btn.onClick.AddListener(() => {
                    // Try to find THAudioManager instance
                    var audioMan = Object.FindAnyObjectByType<THAudioManager>();
                    if (audioMan != null) audioMan.PlaySfx("button_click");
                });
            }
        }

        public void OnNewGameClick()
        {
            Debug.Log("[THMainMenu] Starting New Game...");
            // Clear current state
            PlayerPrefs.DeleteKey("TheHero_SaveData");
            SceneManager.LoadScene("Map");
        }

        public void OnContinueClick()
        {
            Debug.Log("[THMainMenu] Continuing Game...");
            SceneManager.LoadScene("Map");
        }

        public void OnSettingsClick()
        {
            if (SettingsPanel != null) SettingsPanel.SetActive(true);
        }

        public void OnHelpClick()
        {
            if (HelpPanel != null) HelpPanel.SetActive(true);
        }

        public void OnExitClick()
        {
            Debug.Log("[THMainMenu] Exit clicked.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
