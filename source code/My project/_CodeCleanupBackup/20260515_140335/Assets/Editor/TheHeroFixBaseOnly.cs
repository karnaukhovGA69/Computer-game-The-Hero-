using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class TheHeroFixBaseOnly
{
    private const string BaseScenePath = "Assets/Scenes/Base.unity";

    [MenuItem("The Hero/Base/Fix Base Scene Only")]
    public static void FixBaseSceneOnly()
    {
        var scene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);
        Debug.Log("[TheHeroBaseFix] Base scene opened");

        RemoveBrokenReferences();
        Debug.Log("[TheHeroBaseFix] Broken references removed");

        RebuildBaseCanvas();
        Debug.Log("[TheHeroBaseFix] Base Canvas rebuilt");
        Debug.Log("[TheHeroBaseFix] Hire UI rebuilt");
        Debug.Log("[TheHeroBaseFix] Army UI rebuilt");
        Debug.Log("[TheHeroBaseFix] Recruit buttons connected");
        Debug.Log("[TheHeroBaseFix] BackToMap connected");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

        Debug.Log("[TheHeroBaseFix] Base ready for testing");
    }

    private static void RemoveBrokenReferences()
    {
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }

        foreach (var polish in Object.FindObjectsByType<THBaseUIPolish>(FindObjectsInactive.Include))
        {
            Object.DestroyImmediate(polish);
        }

        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 1; i < eventSystems.Length; i++)
        {
            Object.DestroyImmediate(eventSystems[i].gameObject);
        }
    }

    private static void RebuildBaseCanvas()
    {
        foreach (var oldCanvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(oldCanvas.gameObject);
        }

        EnsureEventSystem();

        var runtime = Object.FindAnyObjectByType<THBaseRuntime>(FindObjectsInactive.Include);
        if (runtime == null)
        {
            var runtimeGo = new GameObject("BaseRuntime");
            runtime = runtimeGo.AddComponent<THBaseRuntime>();
        }
        runtime.gameObject.name = "BaseRuntime";

        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        runtime.BuildBaseCanvas(canvasGo.transform);
        DisablePanelRaycasts(canvasGo.transform);

        EditorUtility.SetDirty(runtime);
        EditorUtility.SetDirty(canvasGo);
    }

    private static void EnsureEventSystem()
    {
        var eventSystem = Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include);
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
        }

        var oldModules = eventSystem.GetComponents<BaseInputModule>();
        foreach (var module in oldModules)
        {
            if (module is InputSystemUIInputModule) continue;
            Object.DestroyImmediate(module);
        }

        if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    private static void DisablePanelRaycasts(Transform root)
    {
        foreach (var image in root.GetComponentsInChildren<Image>(true))
        {
            bool isButton = image.GetComponent<Button>() != null;
            image.raycastTarget = isButton;
        }

        foreach (var text in root.GetComponentsInChildren<Text>(true))
        {
            text.raycastTarget = false;
        }

        var back = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "BackToMapButton");
        if (back != null)
        {
            back.interactable = true;
            var image = back.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
        }
    }
}
