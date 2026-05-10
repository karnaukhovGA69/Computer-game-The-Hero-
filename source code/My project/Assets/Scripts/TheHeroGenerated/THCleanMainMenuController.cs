using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

namespace TheHero.Generated
{
    public class THCleanMainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject SettingsPanel;
        public GameObject HelpPanel;

        [Header("Main Buttons")]
        public Button NewGameButton;
        public Button ContinueButton;
        public Button SettingsButton;
        public Button HelpButton;
        public Button ExitButton;

        [Header("Sub Buttons")]
        public Button CloseSettingsButton;
        public Button CloseHelpButton;

        private void Start()
        {
            // Connect Main Buttons
            if (NewGameButton)
            {
                NewGameButton.onClick.RemoveAllListeners();
                NewGameButton.onClick.AddListener(OnNewGameClick);
            }
            if (ContinueButton)
            {
                ContinueButton.onClick.RemoveAllListeners();
                ContinueButton.onClick.AddListener(OnContinueClick);
            }
            if (SettingsButton) SettingsButton.onClick.AddListener(() => SettingsPanel?.SetActive(true));
            if (HelpButton) HelpButton.onClick.AddListener(() => HelpPanel?.SetActive(true));
            if (ExitButton) ExitButton.onClick.AddListener(OnExitClick);

            // Connect Sub Buttons
            if (CloseSettingsButton) CloseSettingsButton.onClick.AddListener(() => SettingsPanel?.SetActive(false));
            if (CloseHelpButton) CloseHelpButton.onClick.AddListener(() => HelpPanel?.SetActive(false));

            // Initial State
            if (SettingsPanel) SettingsPanel.SetActive(false);
            if (HelpPanel) HelpPanel.SetActive(false);

            UpdateButtonStates();
            
            if (THAudioManager.Instance != null) THAudioManager.Instance.PlayMusic("MainMenu");
        }

        private void UpdateButtonStates()
        {
            if (ContinueButton)
            {
                ContinueButton.interactable = THSaveSystem.HasSave();
            }
        }

        private void OnNewGameClick()
        {
            Debug.Log("[THCleanMainMenu] New Game clicked.");
            
            // If we want a confirmation dialog, we can check for existing save here
            if (THSaveSystem.HasSave())
            {
                // For now, let's just use the direct start as requested to fix logic first
                StartNewGame();
            }
            else
            {
                StartNewGame();
            }
        }

        public void StartNewGame()
        {
            THManager.Instance.NewGame();
            THSceneLoader.Instance.LoadMap();
        }

        private void OnContinueClick()
        {
            Debug.Log("[THCleanMainMenu] Continue clicked.");
            if (THSaveSystem.HasSave())
            {
                var data = THSaveSystem.LoadGame();
                if (data != null)
                {
                    THManager.Instance.Data = data;
                    THSceneLoader.Instance.LoadMap();
                }
            }
        }

        private void OnExitClick()
        {
            Debug.Log("[THCleanMainMenu] Exit clicked.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
