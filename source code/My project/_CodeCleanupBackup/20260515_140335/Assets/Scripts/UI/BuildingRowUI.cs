using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheHero.Subsystems.Base;

namespace TheHero.UI
{
    // Строка одной постройки в BaseUI
    public class BuildingRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _levelText;
        [SerializeField] private TextMeshProUGUI _recruitsText;
        [SerializeField] private TextMeshProUGUI _recruitCostText;
        [SerializeField] private Button _recruitButton;
        [SerializeField] private Button _upgradeButton;
        [SerializeField] private TextMeshProUGUI _upgradeCostText;

        private BuildingState _state;
        private BaseController _controller;

        public void Init(BuildingState state, BaseController controller)
        {
            _state      = state;
            _controller = controller;

            var unitType = controller.GetCurrentUnitType(state.BuildingId);
            _nameText.text    = unitType?.DisplayName ?? state.BuildingId;
            _levelText.text   = $"Ур. {state.Level}";
            _recruitsText.text = $"Доступно: {state.AccumulatedRecruits}";

            _upgradeButton.gameObject.SetActive(!state.IsUpgraded);
            _upgradeButton.onClick.AddListener(() => controller.UpgradeBuilding(state.BuildingId));
            _recruitButton.onClick.AddListener(OnRecruitAll);

            _recruitButton.interactable = state.AccumulatedRecruits > 0;
        }

        // Нанять всех доступных юнитов одним нажатием
        private void OnRecruitAll()
        {
            if (_state.AccumulatedRecruits > 0)
                _controller.Recruit(_state.BuildingId, _state.AccumulatedRecruits);
        }
    }
}
