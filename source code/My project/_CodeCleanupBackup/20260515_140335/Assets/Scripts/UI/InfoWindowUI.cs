using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TheHero.UI
{
    // Универсальное информационное окно: заголовок + текст + кнопка закрыть
    public class InfoWindowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _titleText;
        [SerializeField] private TextMeshProUGUI _bodyText;
        [SerializeField] private Button _closeButton;

        public System.Action OnClosed;

        private void Awake() =>
            _closeButton.onClick.AddListener(Close);

        public void Show(string title, string body)
        {
            _titleText.text = title;
            _bodyText.text  = body;
            gameObject.SetActive(true);
        }

        public void Close()
        {
            gameObject.SetActive(false);
            OnClosed?.Invoke();
        }
    }
}
