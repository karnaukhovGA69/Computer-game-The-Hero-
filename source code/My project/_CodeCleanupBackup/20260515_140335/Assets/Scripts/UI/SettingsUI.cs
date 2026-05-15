using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheHero.Infrastructure;

namespace TheHero.UI
{
    public class SettingsUI : MonoBehaviour
    {
        [Header("Звук")]
        [SerializeField] private Slider _volumeSlider;
        [SerializeField] private Toggle _soundToggle;

        [Header("Локализация и сложность")]
        [SerializeField] private TMP_Dropdown _languageDropdown;
        [SerializeField] private TMP_Dropdown _difficultyDropdown;

        [Header("Кнопки")]
        [SerializeField] private Button _closeButton;

        private AudioManager _audio;
        private LocalizationManager _localization;

        public System.Action OnClose;

        private static readonly string[] Languages   = { "RU", "EN" };
        private static readonly string[] Difficulties = { "Лёгкий", "Нормальный", "Сложный" };

        public void Init(AudioManager audio, LocalizationManager localization)
        {
            _audio        = audio;
            _localization = localization;

            // Заполнить dropdown'ы
            _languageDropdown.ClearOptions();
            _languageDropdown.AddOptions(new System.Collections.Generic.List<string>(Languages));

            _difficultyDropdown.ClearOptions();
            _difficultyDropdown.AddOptions(new System.Collections.Generic.List<string>(Difficulties));

            // Загрузить сохранённые настройки
            _volumeSlider.value    = PlayerPrefs.GetFloat("Volume", 1f);
            _soundToggle.isOn      = PlayerPrefs.GetInt("SoundEnabled", 1) == 1;
            _languageDropdown.value   = PlayerPrefs.GetInt("Language", 0);
            _difficultyDropdown.value = PlayerPrefs.GetInt("Difficulty", 1);

            _volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            _soundToggle.onValueChanged.AddListener(OnSoundToggled);
            _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);
            _difficultyDropdown.onValueChanged.AddListener(OnDifficultyChanged);
            _closeButton.onClick.AddListener(OnCloseClicked);
        }

        private void OnVolumeChanged(float value)
        {
            _audio.SetVolume(value);
            PlayerPrefs.SetFloat("Volume", value);
        }

        private void OnSoundToggled(bool enabled)
        {
            if (!enabled) _audio.StopMusic();
            PlayerPrefs.SetInt("SoundEnabled", enabled ? 1 : 0);
        }

        private void OnLanguageChanged(int index)
        {
            _localization.LoadLanguage(Languages[index]);
            PlayerPrefs.SetInt("Language", index);
        }

        private void OnDifficultyChanged(int index) =>
            PlayerPrefs.SetInt("Difficulty", index);

        private void OnCloseClicked()
        {
            PlayerPrefs.Save();
            OnClose?.Invoke();
        }
    }
}
