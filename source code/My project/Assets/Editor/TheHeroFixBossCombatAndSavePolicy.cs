using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;

public class TheHeroFixBossCombatAndSavePolicy : EditorWindow
{
    private static Color PanelColor = new Color(0, 0, 0, 0.85f);
    private static Color GoldColor = new Color(1f, 0.85f, 0.4f);
    private static Color ButtonColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [MenuItem("The Hero/Fix/Fix Boss Combat Canvas And Save Policy")]
    public static void FixAll()
    {
        FixCombatScene();
        EnforceSavePolicy();
        
        // Final Save for project assets
        AssetDatabase.SaveAssets();
        
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        Debug.Log("[TheHeroFix] Ready for testing");
    }

    private static void EnforceSavePolicy()
    {
        string[] generatedScripts = {
            "Assets/Scripts/TheHeroGenerated/THStrictGridHeroMovement.cs",
            "Assets/Scripts/TheHeroGenerated/THGuaranteedHeroMovement.cs",
            "Assets/Scripts/TheHeroGenerated/THReliableHeroMovement.cs"
        };

        foreach (var path in generatedScripts)
        {
            if (System.IO.File.Exists(path))
            {
                string content = System.IO.File.ReadAllText(path);
                if (content.Contains("THManager.Instance.SaveGame();") && !content.Contains("// THManager.Instance.SaveGame();"))
                {
                    content = content.Replace("THManager.Instance.SaveGame();", "// THManager.Instance.SaveGame(); // Policy enforced");
                    System.IO.File.WriteAllText(path, content);
                    Debug.Log($"[TheHeroFix] Save disabled in {path}");
                }
            }
        }
        AssetDatabase.Refresh();
    }

    private static void FixCombatScene()
    {
        if (!System.IO.File.Exists("Assets/Scenes/Combat.unity")) return;

        var scene = EditorSceneManager.OpenScene("Assets/Scenes/Combat.unity");
        
        // 1. Remove old/broken UI
        ClearOldCombatUI();

        // 2. Setup Systems
        EnsureSystems();

        // 3. Create Canvas
        GameObject canvasGo = new GameObject("CombatCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 0;
        
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGo.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Combat Canvas");

        // 4. Runtime Controller (find or create)
        var runtime = Object.FindAnyObjectByType<THCombatRuntime>();
        if (runtime == null)
        {
            GameObject rtGo = new GameObject("CombatRuntime");
            runtime = rtGo.AddComponent<THCombatRuntime>();
        }

        // 5. Build Layout
        Transform root = canvasGo.transform;

        // TopBar
        var topBar = CreatePanel(root, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(0, 64));
        runtime.battleTitleText = CreateText(topBar.transform, "BattleTitleText", "БОЙ", 32, TextAnchor.MiddleCenter);
        Stretch(runtime.battleTitleText.gameObject);

        runtime.roundText = CreateText(topBar.transform, "RoundText", "Раунд 1", 24, TextAnchor.MiddleRight);
        runtime.roundText.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 0);
        
        var backBtn = CreateButton(topBar.transform, "BackToMapButton", "НА КАРТУ", new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(150, 42));

        // Player Panel
        var pPanel = CreatePanel(root, "PlayerPanel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(210, 0), new Vector2(420, -260));
        pPanel.rectTransform.offsetMin = new Vector2(0, 180);
        pPanel.rectTransform.offsetMax = new Vector2(420, -80);
        CreateText(pPanel.transform, "Title", "АРМИЯ ГЕРОЯ", 24, TextAnchor.UpperCenter).rectTransform.anchoredPosition = new Vector2(0, -20);
        
        var pUnits = CreateEmpty(pPanel.transform, "PlayerUnitsContainer", true);
        pUnits.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        pUnits.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -60);
        runtime.playerUnitsContainer = pUnits.transform;
        var pLayout = pUnits.AddComponent<VerticalLayoutGroup>();
        pLayout.spacing = 10;
        pLayout.childForceExpandHeight = false;

        // Enemy Panel
        var ePanel = CreatePanel(root, "EnemyPanel", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-210, 0), new Vector2(420, -260));
        ePanel.rectTransform.offsetMin = new Vector2(-420, 180);
        ePanel.rectTransform.offsetMax = new Vector2(0, -80);
        CreateText(ePanel.transform, "Title", "ВРАГИ", 24, TextAnchor.UpperCenter).rectTransform.anchoredPosition = new Vector2(0, -20);
        
        var eUnits = CreateEmpty(ePanel.transform, "EnemyUnitsContainer", true);
        eUnits.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        eUnits.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -60);
        runtime.enemyUnitsContainer = eUnits.transform;
        var eLayout = eUnits.AddComponent<VerticalLayoutGroup>();
        eLayout.spacing = 10;
        eLayout.childForceExpandHeight = false;

        // Center Panel
        var cPanel = CreatePanel(root, "CenterPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 220));
        runtime.infoText = CreateText(cPanel.transform, "BattleMessageText", "Выберите свой отряд и цель.", 20, TextAnchor.MiddleCenter);
        runtime.infoText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
        
        var actions = CreateEmpty(cPanel.transform, "ActionButtons", true);
        runtime.actionButtonsRoot = actions;
        actions.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        actions.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 60);
        var aLayout = actions.AddComponent<HorizontalLayoutGroup>();
        aLayout.spacing = 10;
        aLayout.childControlWidth = true;
        aLayout.childAlignment = TextAnchor.MiddleCenter;
        
        var atkBtn = CreateButton(actions.transform, "AttackButton", "АТАКА");
        var autoBtn = CreateButton(actions.transform, "AutoBattleButton", "АВТОБОЙ");
        var skipBtn = CreateButton(actions.transform, "SkipButton", "ПРОПУСТИТЬ");

        // Log Panel
        var lPanel = CreatePanel(root, "CombatLogPanel", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 75), new Vector2(0, 150));
        CreateText(lPanel.transform, "LogTitle", "ЖУРНАЛ БОЯ", 18, TextAnchor.UpperLeft).rectTransform.anchoredPosition = new Vector2(15, -5);
        runtime.logText = CreateText(lPanel.transform, "CombatLogText", "", 16, TextAnchor.UpperLeft);
        runtime.logText.GetComponent<RectTransform>().offsetMin = new Vector2(20, 10);
        runtime.logText.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -30);

        // Result Panel
        var resPanel = CreatePanel(root, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(540, 340));
        runtime.resultPanel = resPanel.gameObject;
        runtime.resultTitleText = CreateText(resPanel.transform, "ResultTitleText", "РЕЗУЛЬТАТ", 38, TextAnchor.UpperCenter);
        runtime.resultTitleText.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        runtime.resultBodyText = CreateText(resPanel.transform, "ResultBodyText", "...", 22, TextAnchor.MiddleCenter);
        runtime.resultBodyText.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 150);
        
        var resBtns = CreateEmpty(resPanel.transform, "Buttons", true);
        resBtns.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
        resBtns.GetComponent<RectTransform>().sizeDelta = new Vector2(460, 50);
        resBtns.AddComponent<HorizontalLayoutGroup>().spacing = 20;
        runtime.finishBattleButton = CreateButton(resBtns.transform, "FinishBattleButton", "ЗАВЕРШИТЬ БОЙ");
        runtime.mainMenuButton = CreateButton(resBtns.transform, "MainMenuButton", "ГЛАВНОЕ МЕНЮ");
        resPanel.gameObject.SetActive(false);

        // 6. Connect
        runtime.ConnectButtons();
        runtime.ConnectResultButtons();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[TheHeroFix] Combat scene UI rebuilt.");
    }

    private static void ClearOldCombatUI()
    {
        var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in allCanvases) Undo.DestroyObjectImmediate(c.gameObject);

        string[] tags = { "Story", "Dialog", "Frame", "Panel", "Unit", "Button", "Text", "Window", "Fantasy" };
        var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGos)
        {
            if (go == null) continue;
            if (go.GetComponent<RectTransform>() != null && go.transform.parent == null)
            {
                Undo.DestroyObjectImmediate(go);
                continue;
            }
            if (tags.Any(t => go.name.Contains(t)) && go.transform.parent == null)
            {
                 Undo.DestroyObjectImmediate(go);
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
        var btn = CreateButton(parent, name, label);
        if (btn == null) return null;
        var rt = btn.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos; rt.sizeDelta = size;
        return btn;
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 40);
        
        var img = go.GetComponent<Image>();
        img.color = ButtonColor;
        img.raycastTarget = true;
        
        var b = go.GetComponent<Button>();
        var t = CreateText(go.transform, "Text", label, 18, TextAnchor.MiddleCenter);
        if (t != null) Stretch(t.gameObject);
        return b;
    }

    private static GameObject CreateEmpty(Transform parent, string name, bool stretch)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        if (stretch) Stretch(go);
        return go;
    }

    private static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }
}
