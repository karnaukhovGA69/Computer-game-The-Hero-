using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;

public class TheHeroBuildWorkingBase : EditorWindow
{
    private static Color PanelColor = new Color(0, 0, 0, 0.85f);
    private static Color GoldColor = new Color(1f, 0.85f, 0.4f);
    private static Color ButtonColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [MenuItem("The Hero/Base/Build Working Castle UI")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string scenePath = "Assets/Scenes/Base.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        // 1. Cleanup
        ClearOldBaseUI();

        // 2. Systems
        EnsureSystems();

        // 3. Canvas
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

        // 4. Runtime Controller
        GameObject runtimeGo = new GameObject("BaseRuntime");
        var runtime = runtimeGo.AddComponent<THBaseRuntime>();
        Undo.RegisterCreatedObjectUndo(runtimeGo, "Create BaseRuntime");

        // 5. Layout
        Transform root = canvasGo.transform;

        // TopBar
        var topBar = CreatePanel(root, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -35), new Vector2(0, 70));
        CreateText(topBar.transform, "Title", "ЗАМОК", 32, TextAnchor.MiddleCenter);
        runtime.resourcesText = CreateText(topBar.transform, "ResourcesText", "Gold: 0 | Wood: 0 | Stone: 0 | Mana: 0", 22, TextAnchor.MiddleLeft);
        runtime.resourcesText.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);
        runtime.resourcesText.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 40);
        
        runtime.backToMapButton = CreateButton(topBar.transform, "BackToMapButton", "НА КАРТУ", new Vector2(1, 0.5f), new Vector2(-100, 0), new Vector2(180, 46));

        // Main Area
        var mainPanel = CreateEmpty(root, "MainPanel", true);
        var mainRt = mainPanel.GetComponent<RectTransform>();
        mainRt.offsetMin = new Vector2(40, 40);
        mainRt.offsetMax = new Vector2(-40, -110);

        // Buildings Panel
        var bPanel = CreatePanel(mainPanel.transform, "BuildingsPanel", new Vector2(0, 0), new Vector2(0.68f, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateText(bPanel.transform, "Title", "СТРОЕНИЯ И НАЙМ", 26, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        
        var bScroll = CreateEmpty(bPanel.transform, "BuildingsContainer", true);
        bScroll.GetComponent<RectTransform>().offsetMin = new Vector2(20, 20);
        bScroll.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -70);
        runtime.buildingsContainer = bScroll.transform;
        var bLayout = bScroll.AddComponent<VerticalLayoutGroup>();
        bLayout.spacing = 15;
        bLayout.childForceExpandHeight = false;
        bLayout.childControlHeight = false;

        // Army Panel
        var aPanel = CreatePanel(mainPanel.transform, "ArmyPanel", new Vector2(0.7f, 0), new Vector2(1, 1), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        CreateText(aPanel.transform, "Title", "АРМИЯ", 26, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        
        var aScroll = CreateEmpty(aPanel.transform, "ArmyContainer", true);
        aScroll.GetComponent<RectTransform>().offsetMin = new Vector2(20, 60);
        aScroll.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -70);
        runtime.armyContainer = aScroll.transform;
        var aLayout = aScroll.AddComponent<VerticalLayoutGroup>();
        aLayout.spacing = 8;
        aLayout.childForceExpandHeight = false;
        
        runtime.armySummaryText = CreateText(aPanel.transform, "Summary", "Всего юнитов: 0", 22, TextAnchor.LowerCenter);
        runtime.armySummaryText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 20);

        // 6. Templates (Prefabs substitutes)
        runtime.buildingCardTemplate = CreateBuildingTemplate(root);
        runtime.armyRowTemplate = CreateArmyRowTemplate(root);

        // Message Panel
        var msgPanel = CreateEmpty(root, "MessagePanel", true);
        msgPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);
        msgPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 50);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TheHeroBase] Rebuild complete.");
        
        // Return to main menu as requested by protocol? No, usually I stay or go back. User said "Открыть MainMenu.unity" in command.
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }

    private static void ClearOldBaseUI()
    {
        var allCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in allCanvas) Object.DestroyImmediate(c.gameObject);

        var stray = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in stray)
        {
            if (go == null) continue;
            if (go.name.Contains("Panel") || go.name.Contains("Container") || go.name.Contains("Runtime") || go.name.Contains("Controller"))
            {
                if (go.name != "Main Camera" && go.name != "Directional Light")
                    Object.DestroyImmediate(go);
            }
        }
    }

    private static void EnsureSystems()
    {
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }
    }

    private static GameObject CreateBuildingTemplate(Transform root)
    {
        var go = CreatePanel(root, "BuildingTemplate", Vector2.zero, Vector2.one, Vector2.one, Vector2.zero, new Vector2(0, 150));
        go.gameObject.SetActive(false);
        var t = go.transform;

        CreateText(t, "NameText", "Building Name", 22, TextAnchor.UpperLeft).GetComponent<RectTransform>().anchoredPosition = new Vector2(20, -10);
        CreateText(t, "LevelText", "Level: 1/2", 18, TextAnchor.UpperLeft).GetComponent<RectTransform>().anchoredPosition = new Vector2(20, -45);
        CreateText(t, "UnitText", "Unit: Type", 18, TextAnchor.MiddleLeft).GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);
        CreateText(t, "AvailableText", "Available: 0", 18, TextAnchor.LowerLeft).GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 45);
        
        CreateText(t, "RecruitCostText", "Price: 0 gold", 16, TextAnchor.UpperRight).GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, -10);
        CreateText(t, "UpgradeCostText", "Upgrade: 0 wood", 16, TextAnchor.MiddleRight).GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, -45);

        var btnRow = CreateEmpty(t, "Buttons", true);
        btnRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(220, -40);
        btnRow.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 50);
        var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 10;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;

        CreateButton(btnRow.transform, "RecruitOneButton", "Нанять 1", Vector2.zero, Vector2.zero, new Vector2(140, 38));
        CreateButton(btnRow.transform, "RecruitAllButton", "Нанять всех", Vector2.zero, Vector2.zero, new Vector2(140, 38));
        CreateButton(btnRow.transform, "UpgradeButton", "Улучшить", Vector2.zero, Vector2.zero, new Vector2(140, 38));

        return go.gameObject;
    }

    private static GameObject CreateArmyRowTemplate(Transform root)
    {
        var go = CreateEmpty(root, "ArmyRowTemplate", false);
        go.SetActive(false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 30);
        CreateText(go.transform, "Text", "Unit x0", 20, TextAnchor.MiddleLeft).GetComponent<RectTransform>().sizeDelta = new Vector2(400, 30);
        return go;
    }

    private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = PanelColor;
        var outline = go.AddComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(2, -2);
        return img;
    }

    private static Text CreateText(Transform parent, string name, string content, int size, TextAnchor align)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.color = Color.white;
        t.alignment = align;
        t.raycastTarget = false;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
        return t;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(parent, false);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        
        btnGo.GetComponent<Image>().color = ButtonColor;
        var btn = btnGo.GetComponent<Button>();
        
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGo.transform.SetParent(btnGo.transform, false);
        var txt = txtGo.GetComponent<Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 18;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var txtRt = txtGo.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero; txtRt.anchorMax = Vector2.one; txtRt.offsetMin = Vector2.zero; txtRt.offsetMax = Vector2.zero;
        
        btnGo.AddComponent<Outline>().effectColor = GoldColor;
        
        return btn;
    }

    private static GameObject CreateEmpty(Transform parent, string name, bool stretch)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (stretch)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        return go;
    }
}
