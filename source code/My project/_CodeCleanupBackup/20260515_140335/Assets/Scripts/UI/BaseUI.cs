using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheHero.Subsystems.Base;

namespace TheHero.UI
{
    public class BaseUI : MonoBehaviour
    {
        [SerializeField] private Transform _buildingsContainer;
        [SerializeField] private GameObject _buildingRowPrefab;

        private BaseController _controller;
        private readonly List<BuildingRowUI> _rows = new List<BuildingRowUI>();

        public void Init(BaseController controller)
        {
            _controller = controller;
            _controller.OnStateChanged += Refresh;
            Refresh();
        }

        public void Refresh()
        {
            foreach (Transform child in _buildingsContainer) Destroy(child.gameObject);
            _rows.Clear();

            foreach (var building in _controller.GetAllBuildings())
            {
                var go  = Instantiate(_buildingRowPrefab, _buildingsContainer);
                var row = go.GetComponent<BuildingRowUI>();
                row.Init(building, _controller);
                _rows.Add(row);
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
                _controller.OnStateChanged -= Refresh;
        }
    }
}
