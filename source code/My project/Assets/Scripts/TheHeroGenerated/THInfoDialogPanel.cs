using UnityEngine;
using UnityEngine.UI;
using System;

namespace TheHero.Generated
{
    public class THInfoDialogPanel : MonoBehaviour
    {
        public static THInfoDialogPanel Instance { get; private set; }

        public GameObject Panel;
        public Text TitleText;
        public Text ContentText;
        public Button ContinueButton;

        private Action onComplete;

        private void Awake()
        {
            Instance = this;
            if (Panel) Panel.SetActive(false);
            if (ContinueButton) ContinueButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(string title, string content, Action callback = null)
        {
            if (!Panel) return;
            Panel.SetActive(true);
            if (TitleText) TitleText.text = title;
            if (ContentText) ContentText.text = content;
            onComplete = callback;
            
            // Pause game or disable movement if needed
            if (THMapController.Instance && THMapController.Instance.HeroMover)
            {
                // Simple way to prevent movement: 
                // We can't easily "pause" but we can check if panel is active in HeroMover
            }
        }

        public void Close()
        {
            if (Panel) Panel.SetActive(false);
            onComplete?.Invoke();
            onComplete = null;
        }
    }
}