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

        // Scene controllers are serialized in the scenes. Do not auto-add legacy
        // controllers here: Map/Combat/Base already use THMapController,
        // THCombatRuntime and THBaseRuntime. Adding old controllers at runtime
        // creates duplicate systems and conflicting save/combat/base logic.
        switch (type)
        {
            case SceneType.MainMenu:
                WarnIfMissing(Object.FindAnyObjectByType<THCleanMainMenuController>(), "THCleanMainMenuController");
                break;
            case SceneType.Map:
                WarnIfMissing(Object.FindAnyObjectByType<THMapController>(), "THMapController");
                break;
            case SceneType.Combat:
                WarnIfMissing(Object.FindAnyObjectByType<THCombatRuntime>(), "THCombatRuntime");
                break;
            case SceneType.Base:
                WarnIfMissing(Object.FindAnyObjectByType<THBaseRuntime>(), "THBaseRuntime");
                break;
        }
    }

    private static void WarnIfMissing(Object controller, string controllerName)
    {
        if (controller == null)
        {
            Debug.LogWarning($"[TH] {controllerName} is missing from the active scene.");
        }
    }
}
