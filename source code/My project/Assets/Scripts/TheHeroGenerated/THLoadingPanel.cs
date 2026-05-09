using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace TheHero.Generated
{
    public class THLoadingPanel : MonoBehaviour
    {
        public static THLoadingPanel Instance { get; private set; }

        public GameObject Panel;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (Panel) Panel.SetActive(false);
        }

        public void LoadScene(string sceneName)
        {
            if (Panel) Panel.SetActive(true);
            SceneManager.LoadScene(sceneName);
            // Hide is handled by the scene load end or simple delay
            Invoke("Hide", 0.5f);
        }

        public void Hide()
        {
            if (Panel) Panel.SetActive(false);
        }
    }
}