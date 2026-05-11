using UnityEngine;
using TheHero.Generated;

namespace TheHero.Generated
{
    public class THManager : MonoBehaviour
    {
        private static THManager _instance;
        public static THManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("THManager");
                    _instance = go.AddComponent<THManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        public THGameState Data;

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            
            if (Data == null)
            {
                if (THSaveSystem.HasSave()) Data = THSaveSystem.LoadGame();
                if (Data == null) NewGame();
            }
        }

        public void NewGame()
        {
            Data = THSaveSystem.NewGame();
        }

        public void SaveGame()
        {
            THSaveSystem.SaveGame(Data);
        }
    }
}
