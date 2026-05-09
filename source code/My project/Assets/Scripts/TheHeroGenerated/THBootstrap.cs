using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TheHero.Generated;

public class THBootstrap : MonoBehaviour
{
public enum SceneType { MainMenu, Map, Combat, Base }
    public SceneType type;

    void Awake()
    {
        Debug.Log("[TH] Bootstrapping " + type);
        
        TheHero.Generated.THSystemInitializer.EnsureSystems();

        // Ensure EventSystem
        if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            var module = es.AddComponent<InputSystemUIInputModule>();
            var actions = Resources.Load<UnityEngine.InputSystem.InputActionAsset>("InputSystem_Actions");
            if (actions != null) module.actionsAsset = actions;
        }

        // Ensure Manager
        if (THManager.Instance == null)
        {
            var mGo = new GameObject("THManager");
            mGo.AddComponent<THManager>();
        }

        // Add Controller
        switch (type)
        {
            case SceneType.MainMenu:
                if (gameObject.GetComponent<THMainMenuController>() == null)
                    gameObject.AddComponent<THMainMenuController>();
                break;
            case SceneType.Map:
                if (gameObject.GetComponent<THMapController>() == null)
                    gameObject.AddComponent<THMapController>();
                break;
            case SceneType.Combat:
                if (gameObject.GetComponent<THCombatController>() == null)
                    gameObject.AddComponent<THCombatController>();
                break;
            case SceneType.Base:
                if (gameObject.GetComponent<THBaseController>() == null)
                    gameObject.AddComponent<THBaseController>();
                break;
        }
    }
}
