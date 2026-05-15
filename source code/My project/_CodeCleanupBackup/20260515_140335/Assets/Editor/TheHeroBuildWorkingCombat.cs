using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;

public class TheHeroBuildWorkingCombat : EditorWindow
{
    [MenuItem("The Hero/Combat/Build Working Combat MVP")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string scenePath = "Assets/Scenes/Combat.unity";
        var scene = EditorSceneManager.OpenScene(scenePath);

        // 1. Clear Old UI
        ClearOldCombatUI();

        // 2. Setup Systems
        GameObject esGo = GameObject.Find("EventSystem");
        if (esGo == null)
        {
            esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
        }

        // 3. Create Runtime Controller
        GameObject runtimeGo = new GameObject("CombatRuntime");
        var runtime = runtimeGo.AddComponent<THCombatRuntime>();

        // 4. Create Canvas
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // 5. Build Layout
        Transform root = canvasGo.transform;

        // TopBar
        var topBar = CreatePanel(root, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(0, 60));
        CreateText(topBar.transform, "Title", "БОЙ", 32, TextAnchor.MiddleCenter);
        runtime.roundText = CreateText(topBar.transform, "RoundText", "Раунд 1", 24, TextAnchor.MiddleRight);
        runtime.roundText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 0);
        var backBtn = CreateButton(topBar.transform, "BackButton", "НА КАРТУ", new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(140, 40));

        // Side Panels
        var pPanel = CreatePanel(root, "PlayerPanel", new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(215, 0), new Vector2(430, 800));
        CreateText(pPanel.transform, "Title", "АРМИЯ ГЕРОЯ", 24, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        var pScroll = CreateEmpty(pPanel.transform, "Scroll", true);
        pScroll.GetComponent<RectTransform>().offsetMin = new Vector2(10, 20);
        pScroll.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -60);
        var pVlg = pScroll.AddComponent<VerticalLayoutGroup>();
        runtime.playerUnitsContainer = pVlg.transform;
        pVlg.spacing = 10;
        pVlg.childForceExpandHeight = false;

        var ePanel = CreatePanel(root, "EnemyPanel", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-215, 0), new Vector2(430, 800));
        CreateText(ePanel.transform, "Title", "ВРАГИ", 24, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        var eScroll = CreateEmpty(ePanel.transform, "Scroll", true);
        eScroll.GetComponent<RectTransform>().offsetMin = new Vector2(10, 20);
        eScroll.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -60);
        var eVlg = eScroll.AddComponent<VerticalLayoutGroup>();
        runtime.enemyUnitsContainer = eVlg.transform;
        eVlg.spacing = 10;
        eVlg.childForceExpandHeight = false;

        // Center Panel
        var cPanel = CreatePanel(root, "CenterPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 220));
        runtime.infoText = CreateText(cPanel.transform, "InfoText", "Ваш ход", 20, TextAnchor.MiddleCenter);
        runtime.infoText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
        
        var actions = CreateEmpty(cPanel.transform, "ActionButtons", true);
        runtime.actionButtonsRoot = actions;
        actions.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        actions.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 60);
        var actionLayout = actions.AddComponent<HorizontalLayoutGroup>();
        actionLayout.spacing = 10;
        actionLayout.childControlWidth = true;
        actionLayout.childAlignment = TextAnchor.MiddleCenter;
        
        var atkBtn = CreateButton(actions.transform, "AttackButton", "АТАКА");
        var autoBtn = CreateButton(actions.transform, "AutoBattleButton", "АВТОБОЙ");
        var skipBtn = CreateButton(actions.transform, "SkipButton", "ПРОПУСТИТЬ");

        // Log Panel
        var lPanel = CreatePanel(root, "CombatLogPanel", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(0, 160));
        CreateText(lPanel.transform, "LogTitle", "ЖУРНАЛ БОЯ", 18, TextAnchor.UpperLeft).GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -10);
        runtime.logText = CreateText(lPanel.transform, "CombatLogText", "", 16, TextAnchor.UpperLeft);
        runtime.logText.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        runtime.logText.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -35);

        // Result Panel
        var resPanel = CreatePanel(root, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 450));
        runtime.resultPanel = resPanel.gameObject;
        runtime.resultTitleText = CreateText(resPanel.transform, "ResultTitleText", "РЕЗУЛЬТАТ", 42, TextAnchor.UpperCenter);
        runtime.resultTitleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -40);
        runtime.resultBodyText = CreateText(resPanel.transform, "ResultBodyText", "...", 24, TextAnchor.MiddleCenter);
        runtime.resultBodyText.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 200);
        
        var resBtns = CreateEmpty(resPanel.transform, "Buttons", true);
        resBtns.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -150);
        resBtns.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 50);
        resBtns.AddComponent<HorizontalLayoutGroup>().spacing = 20;
        var retBtn = CreateButton(resBtns.transform, "ReturnButton", "ВЕРНУТЬСЯ");
        var menuBtn = CreateButton(resBtns.transform, "MenuButton", "МЕНЮ");
        resPanel.gameObject.SetActive(false);

        // 6. Connect Buttons
        atkBtn.onClick.AddListener(runtime.ExecuteAttack);
        autoBtn.onClick.AddListener(runtime.AutoBattle);
        skipBtn.onClick.AddListener(runtime.SkipTurn);
        backBtn.onClick.AddListener(runtime.BackToMap);
        retBtn.onClick.AddListener(runtime.BackToMap);
        menuBtn.onClick.AddListener(runtime.MainMenu);

        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TheHeroCombat] Rebuild complete.");
        
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
    }

    private static void ClearOldCombatUI()
    {
        var allCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in allCanvas) Object.DestroyImmediate(c.gameObject);

        var stray = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in stray)
        {
            if (go == null) continue;
            if (go.name.Contains("Panel") || go.name.Contains("Unit") || go.name.Contains("Runtime") || go.name.Contains("Controller"))
            {
                if (go.name != "Main Camera" && go.name != "TH_Bootstrap" && go.name != "Directional Light")
                    Object.DestroyImmediate(go);
            }
        }
    }

    private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax; rt.pivot = pivot;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        var img = go.AddComponent<Image>();
        img.color = new Color(0, 0, 0, 0.85f);
        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(1, 0.85f, 0.4f, 0.5f);
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
        var btn = CreateButton(parent, name, label);
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return btn;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 40);
        go.AddComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 1f);
        var b = go.AddComponent<Button>();
        var t = CreateText(go.transform, "Text", label, 20, TextAnchor.MiddleCenter);
        return b;
    }

    private static GameObject CreateEmpty(Transform parent, string name, bool stretch)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (stretch)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        return go;
    }
}
