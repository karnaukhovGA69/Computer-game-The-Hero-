using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheHero.Domain;

namespace TheHero.UI
{
    // Строка одного отряда в CombatUI — отдельный prefab-компонент
    public class SquadRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private TextMeshProUGUI _hpText;
        [SerializeField] private Button _selectButton;

        private Squad _squad;

        public void Setup(Squad squad, System.Action<Squad> onSelect)
        {
            _squad = squad;
            Refresh();

            if (onSelect != null)
                _selectButton.onClick.AddListener(() => onSelect(squad));
            else
                _selectButton.interactable = false;
        }

        public void Refresh()
        {
            if (_squad == null) return;
            _nameText.text  = _squad.Type.DisplayName;
            _countText.text = $"x{_squad.Count}";
            _hpText.text    = $"HP: {_squad.FirstUnitHP}/{_squad.Type.MaxHP}";

            _selectButton.interactable = _squad.IsAlive;
        }
    }
}
