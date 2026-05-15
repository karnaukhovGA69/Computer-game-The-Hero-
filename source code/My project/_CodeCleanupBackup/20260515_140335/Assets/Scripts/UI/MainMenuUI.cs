using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheHero.Subsystems.Save;

namespace TheHero.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [SerializeField] private Button _newGameButton;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _quitButton;

        private SaveManager _saveManager;

        public System.Action OnNewGame;
        public System.Action OnContinue;
        public System.Action OnSettings;

        public void Init(SaveManager saveManager)
        {
            _saveManager = saveManager;
            _continueButton.interactable = _saveManager.SaveExists();

            _newGameButton.onClick.AddListener(() => OnNewGame?.Invoke());
            _continueButton.onClick.AddListener(() => OnContinue?.Invoke());
            _settingsButton.onClick.AddListener(() => OnSettings?.Invoke());
            _quitButton.onClick.AddListener(() => Application.Quit());
        }

        // Обновить доступность кнопки "Продолжить" после загрузки сцены
        public void RefreshContinueButton() =>
            _continueButton.interactable = _saveManager != null && _saveManager.SaveExists();
    }
}
