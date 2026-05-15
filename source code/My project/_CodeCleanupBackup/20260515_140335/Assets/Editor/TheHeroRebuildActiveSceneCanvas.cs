using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using TheHero.Generated;

public class TheHeroRebuildActiveSceneCanvas : EditorWindow
{
    private static Color PanelColor = new Color(0, 0, 0, 0.8f);
    private static Color GoldColor = new Color(1f, 0.85f, 0.4f);
    private static Color ButtonColor = new Color(0.15f, 0.15f, 0.15f, 1f);

    [MenuItem("The Hero/Fix/Rebuild Active Scene Canvas From Scratch")]
    public static void Rebuild()
    {
        var scene = EditorSceneManager.GetActiveScene();
        string sceneName = scene.name;

        // 1. Identify Scene Type
        string type = "Other";
        if (sceneName.Contains("Combat") || Object.FindAnyObjectByType<THCombatController>() != null) type = "Combat";
        else if (sceneName.Contains("Map") || Object.FindAnyObjectByType<THMapController>() != null) type = "Map";
        else if (sceneName.Contains("Base") || Object.FindAnyObjectByType<THBaseController>() != null) type = "Base";

        Debug.Log($"[TheHeroCanvasRebuild] Active scene detected: {sceneName} (Type: {type})");

        if (type == "Map")
        {
            Debug.Log("[TheHeroCanvasRebuild] Map Canvas is protected; restoring Map UI without deleting map UI.");
            TheHero.Editor.TheHeroRestoreMapUI.RestoreOpenMapUI(false);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            return;
        }

        // 2. Remove Old Canvas and UI
        var allCanvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in allCanvas) Undo.DestroyObjectImmediate(c.gameObject);

        // Remove stray UI objects
        string[] uiTags = { "Panel", "Frame", "HUD", "QuestPanel", "StoryDialogPanel", "PlayerPanel", "EnemyPanel", "ArmyPanel", "BuildingsPanel", "SettingsPanel", "HelpPanel", "MenuPanel", "ButtonsContainer" };
        var allGos = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGos)
{
            if (go == null) continue;
            if (uiTags.Any(t => go.name.Contains(t))) Undo.DestroyObjectImmediate(go);
            else if (go.GetComponent<RectTransform>() != null && go.transform.parent == null) Undo.DestroyObjectImmediate(go);
        }

        Debug.Log("[TheHeroCanvasRebuild] Old Canvas removed");

        // 3. Create EventSystem
        if (Object.FindAnyObjectByType<EventSystem>() == null)
        {
            GameObject es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(es, "Create EventSystem");
        }

        // 4. Create New Canvas
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGo.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasGo.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasGo.GetComponent<CanvasScaler>().matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        Undo.RegisterCreatedObjectUndo(canvasGo, "Create Canvas");

        // 5. Scene Specific Rebuild
        switch (type)
        {
            case "Combat": BuildCombatUI(canvasGo.transform); break;
            case "Map": BuildMapUI(canvasGo.transform); break;
            case "Base": BuildBaseUI(canvasGo.transform); break;
            default: Debug.LogWarning("[TheHeroCanvasRebuild] Unknown scene type, creating empty canvas."); break;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[TheHeroCanvasRebuild] Done");
    }

    private static void BuildCombatUI(Transform root)
    {
        var controller = Object.FindAnyObjectByType<THCombatController>();

        // TopBar
        var topBar = CreatePanel(root, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(0, 64));
        var title = CreateText(topBar.transform, "TitleText", "БОЙ", 32, TextAnchor.MiddleCenter);
        Stretch(title.gameObject);
        var round = CreateText(topBar.transform, "RoundText", "Раунд 1", 24, TextAnchor.MiddleRight);
        round.GetComponent<RectTransform>().anchoredPosition = new Vector2(-20, 0);
        var backBtn = CreateButton(topBar.transform, "BackToMapButton", "НА КАРТУ", new Vector2(1, 0.5f), new Vector2(-150, 0), new Vector2(140, 40));

        // PlayerPanel
        var pPanel = CreatePanel(root, "PlayerPanel", new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0.5f), new Vector2(210, 0), new Vector2(420, -260));
        pPanel.rectTransform.offsetMin = new Vector2(0, 180);
        pPanel.rectTransform.offsetMax = new Vector2(420, -80);
        CreateText(pPanel.transform, "PlayerTitle", "АРМИЯ ГЕРОЯ", 24, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        var pUnits = CreateEmpty(pPanel.transform, "PlayerUnitsContainer", true);
        pUnits.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        pUnits.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -60);
        var pLayout = pUnits.AddComponent<VerticalLayoutGroup>();
        pLayout.spacing = 10;
        pLayout.childForceExpandHeight = false;

        // EnemyPanel
        var ePanel = CreatePanel(root, "EnemyPanel", new Vector2(1, 0), new Vector2(1, 1), new Vector2(1, 0.5f), new Vector2(-210, 0), new Vector2(420, -260));
        ePanel.rectTransform.offsetMin = new Vector2(-420, 180);
        ePanel.rectTransform.offsetMax = new Vector2(0, -80);
        CreateText(ePanel.transform, "EnemyTitle", "ВРАГИ", 24, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        var eUnits = CreateEmpty(ePanel.transform, "EnemyUnitsContainer", true);
        eUnits.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        eUnits.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -60);
        var eLayout = eUnits.AddComponent<VerticalLayoutGroup>();
        eLayout.spacing = 10;
        eLayout.childForceExpandHeight = false;

        // CenterPanel
        var cPanel = CreatePanel(root, "CenterPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 220));
        var info = CreateText(cPanel.transform, "CombatInfoText", "Ваш ход", 20, TextAnchor.MiddleCenter);
        info.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 40);
        var actions = CreateEmpty(cPanel.transform, "ActionButtons", true);
        actions.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -50);
        actions.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 50);
        var aLayout = actions.AddComponent<HorizontalLayoutGroup>();
        aLayout.spacing = 10;
        aLayout.childControlWidth = true;
        aLayout.childAlignment = TextAnchor.MiddleCenter;
        var atkBtn = CreateButton(actions.transform, "AttackButton", "АТАКА");
        var autoBtn = CreateButton(actions.transform, "AutoBattleButton", "АВТОБОЙ");
        var skipBtn = CreateButton(actions.transform, "SkipButton", "ПРОПУСТИТЬ");

        // LogPanel
        var lPanel = CreatePanel(root, "CombatLogPanel", new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 75), new Vector2(0, 150));
        var logTxt = CreateText(lPanel.transform, "CombatLogText", "Бой начался...", 18, TextAnchor.UpperLeft);
        logTxt.GetComponent<RectTransform>().offsetMin = new Vector2(20, 10);
        logTxt.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -10);

        // ResultPanel (Inactive)
        var resPanel = CreatePanel(root, "ResultPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 400));
        CreateText(resPanel.transform, "ResultTitleText", "РЕЗУЛЬТАТ", 32, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
        var resBody = CreateText(resPanel.transform, "ResultBodyText", "Статистика боя...", 20, TextAnchor.MiddleCenter);
        resBody.GetComponent<RectTransform>().sizeDelta = new Vector2(500, 200);
        var resBtns = CreateEmpty(resPanel.transform, "ResultButtons", true);
        resBtns.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -140);
        resBtns.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 50);
        resBtns.AddComponent<HorizontalLayoutGroup>().spacing = 20;
        var retBtn = CreateButton(resBtns.transform, "ReturnToMapButton", "ВЕРНУТЬСЯ");
        var mainBtn = CreateButton(resBtns.transform, "MainMenuButton", "МЕНЮ");
        resPanel.gameObject.SetActive(false);

        // Wiring
        if (controller != null)
        {
            controller.LogText = logTxt;
            controller.RoundText = round;
            controller.BackButton = backBtn.gameObject;
            controller.VictoryPanel = resPanel.gameObject;
            controller.DefeatPanel = resPanel.gameObject; // Use same for now or separate
            controller.VictoryStatsText = resBody;
            controller.CombatUIPanel = cPanel.gameObject;

            atkBtn.onClick.AddListener(controller.Attack);
            autoBtn.onClick.AddListener(controller.AutoBattle);
            skipBtn.onClick.AddListener(controller.SkipTurn);
            backBtn.onClick.AddListener(controller.BackToMap);
            retBtn.onClick.AddListener(controller.BackToMap);
            mainBtn.onClick.AddListener(controller.MainMenu);
        }
    }

    private static void BuildMapUI(Transform root)
    {
        var controller = Object.FindAnyObjectByType<THMapController>();

        // TopHUD
        var topHUD = CreatePanel(root, "TopHUD", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -28), new Vector2(0, 56));
        var resGroup = CreateEmpty(topHUD.transform, "ResourcesGroup", true);
        resGroup.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        resGroup.GetComponent<RectTransform>().anchorMax = new Vector2(0.7f, 1);
        resGroup.GetComponent<RectTransform>().offsetMin = new Vector2(20, 0);
        var resLayout = resGroup.AddComponent<HorizontalLayoutGroup>();
        resLayout.spacing = 30;
        resLayout.childAlignment = TextAnchor.MiddleLeft;
        resLayout.childControlWidth = false;

        var gold = CreateText(resGroup.transform, "GoldText", "Gold: 0", 18);
        var wood = CreateText(resGroup.transform, "WoodText", "Wood: 0", 18);
        var stone = CreateText(resGroup.transform, "StoneText", "Stone: 0", 18);
        var mana = CreateText(resGroup.transform, "ManaText", "Mana: 0", 18);

        var btnGroup = CreateEmpty(topHUD.transform, "ButtonsGroup", true);
        btnGroup.GetComponent<RectTransform>().anchorMin = new Vector2(0.7f, 0);
        btnGroup.GetComponent<RectTransform>().anchorMax = Vector2.one;
        btnGroup.GetComponent<RectTransform>().offsetMax = new Vector2(-10, 0);
        var btnLayout = btnGroup.AddComponent<HorizontalLayoutGroup>();
        btnLayout.spacing = 10;
        btnLayout.childAlignment = TextAnchor.MiddleRight;
        btnLayout.childControlWidth = false;

        var saveBtn = CreateButton(btnGroup.transform, "SaveButton", "SAVE", Vector2.zero, Vector2.zero, new Vector2(80, 32));
        var loadBtn = CreateButton(btnGroup.transform, "LoadButton", "LOAD", Vector2.zero, Vector2.zero, new Vector2(80, 32));
        var endBtn = CreateButton(btnGroup.transform, "EndTurnButton", "END TURN", Vector2.zero, Vector2.zero, new Vector2(100, 32));
        var menuBtn = CreateButton(btnGroup.transform, "MenuButton", "MENU", Vector2.zero, Vector2.zero, new Vector2(80, 32));

        // QuestPanel
        var qPanel = CreatePanel(root, "QuestPanel", new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(190, -116), new Vector2(360, 100));
        var qText = CreateText(qPanel.transform, "QuestText", "Задания:\n- Победить Тёмного Лорда", 16, TextAnchor.UpperLeft);
        qText.GetComponent<RectTransform>().offsetMin = new Vector2(10, 10);
        qText.GetComponent<RectTransform>().offsetMax = new Vector2(-10, -10);

        // HeroPanel
        var hPanel = CreatePanel(root, "HeroPanel", new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-150, -126), new Vector2(280, 120));
        var hText = CreateText(hPanel.transform, "HeroText", "Hero: Knight\nLevel: 1\nMoves: 20", 18, TextAnchor.UpperLeft);
        hText.GetComponent<RectTransform>().offsetMin = new Vector2(15, 10);
        hText.GetComponent<RectTransform>().offsetMax = new Vector2(-15, -10);

        // CastleButton
        var castleBtn = CreateButton(root, "CastleButton", "ЗАМОК", new Vector2(0, 0), new Vector2(80, 34), new Vector2(140, 48));

        // MessagePanel
        var msgPanel = CreateEmpty(root, "MessagePanel", true);
        msgPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 200);
        msgPanel.GetComponent<RectTransform>().sizeDelta = new Vector2(600, 50);

        // Wiring
        if (controller != null)
        {
            controller.GoldText = gold;
            controller.WoodText = wood;
            controller.StoneText = stone;
            controller.ManaText = mana;
            controller.InfoText = qText;
            controller.HeroText = hText;

            saveBtn.onClick.AddListener(controller.SaveGame);
            loadBtn.onClick.AddListener(controller.LoadGame);
            endBtn.onClick.AddListener(controller.EndTurn);
            menuBtn.onClick.AddListener(controller.OpenPauseMenu);
            castleBtn.onClick.AddListener(controller.GoToBase);
        }
    }

    private static void BuildBaseUI(Transform root)
    {
        var controller = Object.FindAnyObjectByType<THBaseController>();

        // TopBar
        var topBar = CreatePanel(root, "TopBar", new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -32), new Vector2(0, 64));
        CreateText(topBar.transform, "Title", "ЗАМОК", 32, TextAnchor.MiddleCenter);
        var resTxt = CreateText(topBar.transform, "ResourcesText", "Gold: 0 | Wood: 0 | Stone: 0", 20, TextAnchor.MiddleLeft);
        resTxt.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 0);
        resTxt.GetComponent<RectTransform>().sizeDelta = new Vector2(800, 40);
        var backBtn = CreateButton(topBar.transform, "BackToMapButton", "НА КАРТУ", new Vector2(1, 0.5f), new Vector2(-120, 0), new Vector2(180, 44));

        // BuildingsPanel
        var bPanel = CreatePanel(root, "BuildingsPanel", new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 0.5f), new Vector2(400, 0), new Vector2(760, 800));
        CreateText(bPanel.transform, "BTitle", "СТРОЕНИЯ", 24, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        var bCont = CreateEmpty(bPanel.transform, "BuildingsContainer", true);
        bCont.GetComponent<RectTransform>().offsetMin = new Vector2(20, 20);
        bCont.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -60);
        var bLayout = bCont.AddComponent<VerticalLayoutGroup>();
        bLayout.spacing = 15;
        bLayout.childForceExpandHeight = false;

        // ArmyPanel
        var aPanel = CreatePanel(root, "ArmyPanel", new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-220, 0), new Vector2(400, 800));
        CreateText(aPanel.transform, "ATitle", "АРМИЯ", 24, TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -20);
        var armyTxt = CreateText(aPanel.transform, "ArmyListText", "Ваша армия...", 18, TextAnchor.UpperLeft);
        armyTxt.GetComponent<RectTransform>().offsetMin = new Vector2(20, 20);
        armyTxt.GetComponent<RectTransform>().offsetMax = new Vector2(-20, -60);

        // Simple Building UI Builder helper inside BaseUI
        string[] bIds = { "unit_swordsman", "unit_archer", "unit_mage" };
        foreach (var id in bIds)
        {
            var row = CreatePanel(bCont.transform, "Building_" + id, Vector2.zero, Vector2.one, Vector2.one, Vector2.zero, new Vector2(0, 100));
            row.GetComponent<Image>().color = new Color(1, 1, 1, 0.1f);
            var name = CreateText(row.transform, "Name", id.Replace("unit_", "").ToUpper(), 20, TextAnchor.MiddleLeft);
            name.rectTransform.anchoredPosition = new Vector2(100, 20);
            
            var btnRow = CreateEmpty(row.transform, "Btns", true);
            btnRow.GetComponent<RectTransform>().anchoredPosition = new Vector2(100, -20);
            btnRow.GetComponent<RectTransform>().sizeDelta = new Vector2(400, 40);
btnRow.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            
            var r1 = CreateButton(btnRow.transform, "Recruit1", "Нанять 1", Vector2.zero, Vector2.zero, new Vector2(100, 30));
            var ra = CreateButton(btnRow.transform, "RecruitAll", "Всех", Vector2.zero, Vector2.zero, new Vector2(100, 30));
            var up = CreateButton(btnRow.transform, "Upgrade", "Улучшить", Vector2.zero, Vector2.zero, new Vector2(120, 30));

            if (controller != null)
            {
                r1.onClick.AddListener(() => controller.Recruit(id));
                ra.onClick.AddListener(() => controller.RecruitAll(id));
                up.onClick.AddListener(() => controller.Upgrade(id));
            }
        }

        // Wiring
        if (controller != null)
        {
            controller.ResourcesText = resTxt;
            controller.ArmyListText = armyTxt;
            backBtn.onClick.AddListener(controller.BackToMap);
        }
    }

    // UTILS
    private static Image CreatePanel(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.AddComponent<Image>();
        img.color = PanelColor;
        
        var outline = go.AddComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(2, -2);
        
        return img;
    }

    private static Text CreateText(Transform parent, string name, string content, int size, TextAnchor align = TextAnchor.MiddleLeft)
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
        return t;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var btnGo = CreateButton(parent, name, label);
        var rt = btnGo.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return btnGo.GetComponent<Button>();
    }

    private static Button CreateButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(160, 40);
        
        var img = go.AddComponent<Image>();
        img.color = ButtonColor;
        var btn = go.AddComponent<Button>();
        
        var outline = go.AddComponent<Outline>();
        outline.effectColor = GoldColor;
        outline.effectDistance = new Vector2(1, -1);

        var txt = CreateText(go.transform, "Text", label, 18, TextAnchor.MiddleCenter);
        Stretch(txt.gameObject);
        
        return btn;
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
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }
}
