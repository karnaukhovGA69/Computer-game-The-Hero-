using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Linq;

namespace TheHero.Generated
{
    public class THMainMenuController : MonoBehaviour
    {
        public GameObject ConfirmationPanel;
        public GameObject HelpPanel;
        public GameObject SettingsPanel;
        public GameObject CreditsPanel;

        private void Start()
        {
            Debug.Log("[TH] MainMenuController initializing...");
            
            if (THAudioManager.Instance != null) THAudioManager.Instance.PlayMusic("MainMenu");

            Wire("New Game", OnNewGameClick);
Wire("Continue", OnContinueClick);
            Wire("Settings", OnSettingsClick);
            Wire("Help", OnHelpClick);
            Wire("Exit", Exit);

            // Confirmation buttons
            Wire("Да", StartNewGame);
            Wire("Нет", () => ConfirmationPanel.SetActive(false));

            // Close buttons for other panels
            Wire("Закрыть", CloseAllPanels);
            
            if (ConfirmationPanel) ConfirmationPanel.SetActive(false);
            if (HelpPanel) HelpPanel.SetActive(false);
            if (SettingsPanel) SettingsPanel.SetActive(false);
            if (CreditsPanel) CreditsPanel.SetActive(false);
            
            UpdateButtonState();
        }

        private void CloseAllPanels()
        {
            if (ConfirmationPanel) ConfirmationPanel.SetActive(false);
            if (HelpPanel) HelpPanel.SetActive(false);
            if (SettingsPanel) SettingsPanel.SetActive(false);
            if (CreditsPanel) CreditsPanel.SetActive(false);
        }

        private void Wire(string name, UnityEngine.Events.UnityAction action)
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            var matchingButtons = buttons.Where(b => b.name == name).ToList();
            
            if (matchingButtons.Count > 0)
            {
                foreach (var btn in matchingButtons)
                {
                    btn.onClick.RemoveAllListeners(); 
                    btn.onClick.AddListener(action); 
                    btn.onClick.AddListener(() => {
                        if (THAudioManager.Instance != null) THAudioManager.Instance.PlaySfx("button_click");
                    });
                    Debug.Log($"[TH] Wired button: {name} on {btn.gameObject.name}");
                }
}
            else
            {
                // Only log warning for main buttons to avoid noise from sub-panel buttons if they aren't loaded yet
                string[] mainButtons = { "New Game", "Continue", "Settings", "Help", "Exit" };
                if (mainButtons.Contains(name))
                    Debug.LogWarning($"[TH] Button not found: {name}");
            }
        }

        private void UpdateButtonState()
        {
            var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
            var continueBtn = buttons.FirstOrDefault(b => b.name == "Continue");
            if (continueBtn) continueBtn.interactable = THSaveSystem.HasSave();
        }

        public void OnNewGameClick()
        {
            if (THSaveSystem.HasSave())
            {
                if (ConfirmationPanel) ConfirmationPanel.SetActive(true);
                else StartNewGame();
            }
            else StartNewGame();
        }

        public void StartNewGame()
        {
            THManager.Instance.NewGame();
            THSceneLoader.Instance.LoadMap();
        }

        public void OnContinueClick()
        {
            if (THSaveSystem.HasSave())
            {
                var data = THSaveSystem.LoadGame();
                if (data != null)
                {
                    THManager.Instance.Data = data;
                    THSceneLoader.Instance.LoadMap();
                }
                else
                {
                    THMessageSystem.Instance.ShowError("Сохранение повреждено!");
                }
            }
        }

        public void OnSettingsClick()
        {
            if (SettingsPanel) SettingsPanel.SetActive(true);
            else if (THSettingsController.Instance) THSettingsController.Instance.Open();
        }

        public void OnHelpClick()
        {
            if (HelpPanel) HelpPanel.SetActive(true);
        }

        public void Exit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
