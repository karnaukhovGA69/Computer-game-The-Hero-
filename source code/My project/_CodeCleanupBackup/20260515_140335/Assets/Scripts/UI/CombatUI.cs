using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TheHero.Domain;
using TheHero.Subsystems.Combat;

namespace TheHero.UI
{
    public class CombatUI : MonoBehaviour
    {
        [Header("Отряды игрока")]
        [SerializeField] private Transform _playerSquadsContainer;
        [SerializeField] private GameObject _squadRowPrefab;

        [Header("Отряды врага")]
        [SerializeField] private Transform _enemySquadsContainer;

        [Header("Кнопки")]
        [SerializeField] private Button _endRoundButton;

        [Header("Лог боя")]
        [SerializeField] private TextMeshProUGUI _combatLogText;
        [SerializeField] private ScrollRect _logScrollRect;

        private CombatController _combat;
        private readonly List<SquadRowUI> _playerRows = new List<SquadRowUI>();
        private readonly List<SquadRowUI> _enemyRows  = new List<SquadRowUI>();

        public void Init(CombatController combat)
        {
            _combat = combat;
            _combat.OnSquadAttacked += (attacker, target, killed) =>
            {
                AppendLog($"{attacker.Type.DisplayName} атакует {target.Type.DisplayName}, убито {killed}");
                RefreshSquadLists();
            };
            _combat.OnTurnStarted += squad =>
                AppendLog($"Ход: {squad.Type.DisplayName} (x{squad.Count})");
            _combat.OnCombatEnded += result =>
                AppendLog(result.PlayerWon ? "Победа!" : "Поражение...");

            _endRoundButton.onClick.AddListener(OnEndRoundClicked);
        }

        public void SetupSquadLists(List<Squad> playerSquads, List<Squad> enemySquads)
        {
            BuildRows(_playerSquadsContainer, playerSquads, _playerRows, isEnemy: false);
            BuildRows(_enemySquadsContainer, enemySquads, _enemyRows, isEnemy: true);
        }

        private void BuildRows(Transform container, List<Squad> squads, List<SquadRowUI> rows, bool isEnemy)
        {
            foreach (Transform child in container) Destroy(child.gameObject);
            rows.Clear();

            foreach (var squad in squads)
            {
                var go  = Instantiate(_squadRowPrefab, container);
                var row = go.GetComponent<SquadRowUI>();
                row.Setup(squad, isEnemy ? OnEnemyTargetSelected : (System.Action<Squad>)null);
                rows.Add(row);
            }
        }

        private void OnEnemyTargetSelected(Squad target) =>
            _combat.PlayerSelectTarget(target);

        private void OnEndRoundClicked()
        {
            // Если ход игрока — передать управление: выбор цели ожидается через кнопки отрядов врага
            // Кнопка "Конец раунда" зарезервирована для будущей механики пропуска хода
        }

        private void RefreshSquadLists()
        {
            foreach (var row in _playerRows) row.Refresh();
            foreach (var row in _enemyRows)  row.Refresh();
        }

        private void AppendLog(string entry)
        {
            _combatLogText.text += entry + "\n";
            Canvas.ForceUpdateCanvases();
            _logScrollRect.verticalNormalizedPosition = 0f;
        }

        public void ClearLog() => _combatLogText.text = string.Empty;
    }
}
