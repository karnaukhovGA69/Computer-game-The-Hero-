using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheHero.Domain;

namespace TheHero.UI
{
    public class MapUI : MonoBehaviour
    {
        [Header("Ресурсы")]
        [SerializeField] private TextMeshProUGUI _goldText;
        [SerializeField] private TextMeshProUGUI _woodText;
        [SerializeField] private TextMeshProUGUI _stoneText;
        [SerializeField] private TextMeshProUGUI _manaText;

        [Header("Время")]
        [SerializeField] private TextMeshProUGUI _dayWeekText;

        [Header("Информация о герое")]
        [SerializeField] private TextMeshProUGUI _heroNameText;
        [SerializeField] private TextMeshProUGUI _heroLevelText;
        [SerializeField] private TextMeshProUGUI _movementPointsText;
        [SerializeField] private TextMeshProUGUI _armyCountText;

        [Header("Кнопки")]
        [SerializeField] private Button _endTurnButton;

        public System.Action OnEndTurn;

        private void Awake() =>
            _endTurnButton.onClick.AddListener(() => OnEndTurn?.Invoke());

        public void Refresh(GameState state)
        {
            var w = state.Wallet;
            _goldText.text  = w.Get(ResourceType.Gold).ToString();
            _woodText.text  = w.Get(ResourceType.Wood).ToString();
            _stoneText.text = w.Get(ResourceType.Stone).ToString();
            _manaText.text  = w.Get(ResourceType.Mana).ToString();

            _dayWeekText.text = $"День {state.Day} / Неделя {state.Week}";

            var hero = state.Hero;
            _heroNameText.text        = hero.Name;
            _heroLevelText.text       = $"Уровень {hero.Level}";
            _movementPointsText.text  = $"Движение: {hero.MovementPoints}/{hero.MaxMovementPoints}";
            _armyCountText.text       = $"Отрядов: {hero.Army.SlotCount}/{Army.MaxSlots}";
        }

        public void SetEndTurnInteractable(bool value) =>
            _endTurnButton.interactable = value;
    }
}
