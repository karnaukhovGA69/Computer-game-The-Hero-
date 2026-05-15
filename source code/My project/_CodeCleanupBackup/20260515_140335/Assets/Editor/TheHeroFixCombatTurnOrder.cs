using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TheHero.Generated;

public class TheHeroFixCombatTurnOrder : EditorWindow
{
    [MenuItem("The Hero/Combat/Fix Combat Turn Order")]
    public static void FixTurnOrder()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string combatScenePath = "Assets/Scenes/Combat.unity";
        var scene = EditorSceneManager.OpenScene(combatScenePath);

        var runtime = Object.FindAnyObjectByType<THCombatRuntime>();
        if (runtime == null)
        {
            Debug.LogError("THCombatRuntime not found in scene!");
            return;
        }

        var canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Canvas not found in scene!");
            return;
        }

        Transform root = canvas.transform;

        // 1. Create ActiveTurnPanel
        GameObject activePanel = root.Find("ActiveTurnPanel")?.gameObject;
        if (activePanel == null)
        {
            activePanel = new GameObject("ActiveTurnPanel", typeof(RectTransform), typeof(Image));
            activePanel.transform.SetParent(root, false);
        }
        
        var activeRt = activePanel.GetComponent<RectTransform>();
        activeRt.anchorMin = new Vector2(0.5f, 1f);
        activeRt.anchorMax = new Vector2(0.5f, 1f);
        activeRt.pivot = new Vector2(0.5f, 1f);
        activeRt.anchoredPosition = new Vector2(0, -64); // Below TopBar (height 64)
        activeRt.sizeDelta = new Vector2(520, 60);
        
        activePanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);
        
        GameObject activeTextGo = activePanel.transform.Find("ActiveUnitText")?.gameObject;
        if (activeTextGo == null)
        {
            activeTextGo = new GameObject("ActiveUnitText", typeof(RectTransform), typeof(Text));
            activeTextGo.transform.SetParent(activePanel.transform, false);
        }
        
        var activeTxt = activeTextGo.GetComponent<Text>();
        activeTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        activeTxt.fontSize = 24;
        activeTxt.alignment = TextAnchor.MiddleCenter;
        activeTxt.color = Color.white;
        activeTxt.text = "Ход: ...";
        Stretch(activeTextGo);
        
        runtime.activeUnitText = activeTxt;

        // 2. Create TurnOrderPanel
        GameObject orderPanel = root.Find("TurnOrderPanel")?.gameObject;
        if (orderPanel == null)
        {
            orderPanel = new GameObject("TurnOrderPanel", typeof(RectTransform), typeof(Image));
            orderPanel.transform.SetParent(root, false);
        }
        
        var orderRt = orderPanel.GetComponent<RectTransform>();
        orderRt.anchorMin = new Vector2(0.5f, 1f);
        orderRt.anchorMax = new Vector2(0.5f, 1f);
        orderRt.pivot = new Vector2(0.5f, 1f);
        orderRt.anchoredPosition = new Vector2(0, -124); // Below ActiveTurnPanel
        orderRt.sizeDelta = new Vector2(800, 40);
        
        orderPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);
        
        var hlg = orderPanel.GetComponent<HorizontalLayoutGroup>() ?? orderPanel.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;

        runtime.turnOrderContainer = orderPanel.transform;

        // 3. Connect Buttons under new logic
        runtime.ConnectButtons();

        EditorUtility.SetDirty(runtime);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[TheHeroCombatTurnOrder] Turn queue implemented and UI updated.");
        
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
