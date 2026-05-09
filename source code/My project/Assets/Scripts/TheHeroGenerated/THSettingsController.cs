using UnityEngine;
using UnityEngine.UI;

namespace TheHero.Generated
{
    public class THSettingsController : MonoBehaviour
    {
        public static THSettingsController Instance { get; private set; }

        public GameObject SettingsPanel;
        public Dropdown DifficultyDropdown;
        public Slider MasterVolumeSlider;

        private void Awake()
        {
            Instance = this;
            if (SettingsPanel) SettingsPanel.SetActive(false);
            LoadSettings();
        }

        public void Open()
        {
            if (SettingsPanel) SettingsPanel.SetActive(true);
        }

        public void Close()
        {
            if (SettingsPanel) SettingsPanel.SetActive(false);
            SaveSettings();
        }

        public void SaveSettings()
        {
            if (DifficultyDropdown) PlayerPrefs.SetInt("TheHero_Difficulty", DifficultyDropdown.value);
            if (MasterVolumeSlider) PlayerPrefs.SetFloat("TheHero_MasterVolume", MasterVolumeSlider.value);
            PlayerPrefs.Save();
        }

        public void LoadSettings()
        {
            if (DifficultyDropdown) DifficultyDropdown.value = PlayerPrefs.GetInt("TheHero_Difficulty", 1);
            if (MasterVolumeSlider) MasterVolumeSlider.value = PlayerPrefs.GetFloat("TheHero_MasterVolume", 1f);
        }
    }
}