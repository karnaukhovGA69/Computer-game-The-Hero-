using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace TheHero.Generated
{
    public class THQuestSystem : MonoBehaviour
    {
        public static THQuestSystem Instance { get; private set; }

        public Text GoalTitle;
        public Text GoalText;
        public Text ProgressText;

        private void Awake()
        {
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void UpdateUI()
        {
            if (THDemoCampaignController.Instance)
            {
                THDemoCampaignController.Instance.UpdateProgress();
                
                var state = THMapController.Instance.State;
                if (GoalTitle) GoalTitle.text = "Цель";
                if (GoalText) GoalText.text = THDemoCampaignController.Instance.GetCurrentGoalText();
                if (ProgressText) ProgressText.text = $"{state.campaignStageIndex} / 9";
            }
        }
    }
}