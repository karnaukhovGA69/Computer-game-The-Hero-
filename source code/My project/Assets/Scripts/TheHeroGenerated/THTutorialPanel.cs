using UnityEngine;
using UnityEngine.UI;

namespace TheHero.Generated
{
    public class THTutorialPanel : MonoBehaviour
    {
        public GameObject Panel;
        public Text TutorialText;

        private int step = 0;
        private string[] steps = new string[]
        {
            "Карта\nКликайте по клеткам, чтобы перемещать героя.",
            "Очки хода\nКаждый день герой может пройти ограниченное расстояние.",
            "Ресурсы\nСобирайте золото, дерево, камень и ману.",
            "Бой\nПобеждайте врагов, чтобы получать опыт и награды.",
            "База\nНанимайте юнитов и улучшайте здания.",
            "Цель\nПодготовьтесь и победите Тёмного Лорда."
        };

        private void Start()
        {
            if (PlayerPrefs.GetInt("TheHero_TutorialShown", 0) == 1)
            {
                if (Panel) Panel.SetActive(false);
            }
            else
            {
                ShowStep(0);
            }
        }

        public void Next()
        {
            step++;
            if (step < steps.Length) ShowStep(step);
            else Close();
        }

        public void Back()
        {
            if (step > 0) step--;
            ShowStep(step);
        }

        private void ShowStep(int i)
        {
            if (Panel) Panel.SetActive(true);
            if (TutorialText) TutorialText.text = steps[i];
        }

        public void Close()
        {
            if (Panel) Panel.SetActive(false);
        }

        public void DontShowAgain()
        {
            PlayerPrefs.SetInt("TheHero_TutorialShown", 1);
            Close();
        }
    }
}