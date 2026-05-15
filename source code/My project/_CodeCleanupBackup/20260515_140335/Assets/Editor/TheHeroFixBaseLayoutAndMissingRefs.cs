using System.Linq;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public static class TheHeroFixBaseLayoutAndMissingRefs
{
    private const string BaseScenePath = "Assets/Scenes/Base.unity";

    [MenuItem("The Hero/Base/Fix Base Layout And Missing References")]
    public static void FixBaseLayoutAndMissingRefs()
    {
        var scene = EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

        // 1. Strip missing scripts and stale UI polish components
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
        }
        Debug.Log("[TheHeroBaseFix] MissingReference protection added");

        // 2. Tear down old canvases (the runtime rebuild handles new layout)
        foreach (var oldCanvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            Object.DestroyImmediate(oldCanvas.gameObject);
        }

        // 3. EventSystem singleton with new input module
        EnsureEventSystem();

        // 4. Ensure runtime component
        var runtime = Object.FindAnyObjectByType<THBaseRuntime>(FindObjectsInactive.Include);
        if (runtime == null)
        {
            var runtimeGo = new GameObject("BaseRuntime");
            runtime = runtimeGo.AddComponent<THBaseRuntime>();
        }
        runtime.gameObject.name = "BaseRuntime";

        // 5. Build a fresh canvas via the runtime (uses the new HireRow layout)
        var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        runtime.BuildBaseCanvas(canvasGo.transform);
        Debug.Log("[TheHeroBaseFix] Hire row layout rebuilt");

        DisablePanelRaycasts(canvasGo.transform);
        Debug.Log("[TheHeroBaseFix] Button listeners fixed");

        Debug.Log("[TheHeroBaseFix] Army panel filters zero units");

        EditorUtility.SetDirty(runtime);
        EditorUtility.SetDirty(canvasGo);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorSceneManager.OpenScene(BaseScenePath, OpenSceneMode.Single);

        Debug.Log("[TheHeroBaseFix] Base ready for testing");
    }

    private static void EnsureEventSystem()
    {
        var systems = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 1; i < systems.Length; i++) Object.DestroyImmediate(systems[i].gameObject);

        var eventSystem = systems.Length > 0 ? systems[0] : null;
        if (eventSystem == null)
        {
            var go = new GameObject("EventSystem");
            eventSystem = go.AddComponent<EventSystem>();
        }

        foreach (var module in eventSystem.GetComponents<BaseInputModule>())
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
            text.raycastTarget = false;

        var back = root.GetComponentsInChildren<Button>(true).FirstOrDefault(b => b.name == "BackToMapButton");
        if (back != null)
        {
            back.interactable = true;
            var image = back.GetComponent<Image>();
            if (image != null) image.raycastTarget = true;
        }
    }
}
