using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace TheHero.Generated
{
    public class THDemoCampaignController : MonoBehaviour
    {
        public static THDemoCampaignController Instance { get; private set; }

        private string[] stageGoals = new string[]
        {
            "Соберите первое золото",
            "Захватите шахту",
            "Победите первый отряд врагов",
            "Вернитесь на базу",
            "Наймите новых юнитов",
            "Улучшите любое здание",
            "Победите второй отряд врагов",
            "Подготовьтесь к финальному бою",
            "Победите Тёмного Лорда"
        };

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            CheckInitialStories();
        }

        public void UpdateProgress()
        {
            var state = THMapController.Instance.State;
            int current = state.campaignStageIndex;

            bool advanced = false;
            if (current == 0 && state.gold > 500) advanced = true; // Started with 500, collect first gold
            else if (current == 1 && state.capturedObjectIds.Count > 0) advanced = true;
            else if (current == 2 && state.defeatedEnemyIds.Count > 0) advanced = true;
            else if (current == 3 && IsAtBase(state)) advanced = true;
            else if (current == 4 && state.army.Sum(u => u.count) > 20) advanced = true; // Started with 20
            else if (current == 5 && state.buildings.Any(b => b.level > 1)) advanced = true;
            else if (current == 6 && state.defeatedEnemyIds.Count > 1) advanced = true;
            else if (current == 7 && state.army.Sum(u => u.count) > 40) advanced = true;
            else if (current == 8 && state.isDarkLordDefeated) advanced = true;

            if (advanced)
            {
                state.campaignStageIndex++;
                THMapController.Instance.Log($"Новая цель: {GetCurrentGoalText()}");
                CheckStoryDialogs();
                THSaveSystem.SaveGame(state);
            }
        }

        public string GetCurrentGoalText()
        {
            int index = THMapController.Instance.State.campaignStageIndex;
            if (index < stageGoals.Length) return stageGoals[index];
            return "Тёмный Лорд повержен!";
        }

        private bool IsAtBase(THGameState state)
        {
            return state.heroX == 1 && state.heroY == 1;
        }

        private void CheckInitialStories()
        {
            var state = THMapController.Instance.State;
            if (!state.shownDialogueIds.Contains("start"))
            {
                ShowStory("start", "Начало похода", "Королевство оказалось под угрозой. Герой отправляется собрать армию и победить Тёмного Лорда.");
            }
        }

        public void CheckStoryDialogs()
        {
            var state = THMapController.Instance.State;
            int stage = state.campaignStageIndex;

            if (stage == 1 && !state.shownDialogueIds.Contains("gold"))
                ShowStory("gold", "Ресурсы", "Ресурсы нужны для найма армии и улучшения построек.");
            else if (stage == 3 && !state.shownDialogueIds.Contains("enemy1"))
                ShowStory("enemy1", "Первый враг", "Вражеский отряд охраняет дорогу. Победите его, чтобы продолжить путь.");
            else if (stage == 4 && !state.shownDialogueIds.Contains("base"))
                ShowStory("base", "Замок", "На базе можно нанимать юнитов и улучшать здания.");
            else if (stage == 8 && !state.shownDialogueIds.Contains("final"))
                ShowStory("final", "Финальная битва", "Тёмный Лорд силён. Перед боем убедитесь, что армия достаточно большая.");
            else if (state.isDarkLordDefeated && !state.shownDialogueIds.Contains("victory"))
                ShowStory("victory", "Победа", "Тёмный Лорд побеждён. Королевство спасено.");
        }

        private void ShowStory(string id, string title, string content)
        {
            if (THInfoDialogPanel.Instance)
            {
                THInfoDialogPanel.Instance.Show(title, content, () => {
                    THMapController.Instance.State.shownDialogueIds.Add(id);
                    THSaveSystem.SaveGame(THMapController.Instance.State);
                });
            }
        }
    }
}