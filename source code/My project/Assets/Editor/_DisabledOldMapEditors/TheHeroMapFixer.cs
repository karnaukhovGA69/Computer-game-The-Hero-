using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[InitializeOnLoad]
public class TheHeroMapFixer
{
    private static string MarkerPath => "Assets/Editor/RUN_THE_HERO_MAP_FIX.txt";

    static TheHeroMapFixer()
    {
        /*
        if (File.Exists(MarkerPath))
        {
            EditorApplication.delayCall += () => {
                CreatePlayableMap();
                UpdateMainMenu();
                File.Delete(MarkerPath);
                AssetDatabase.Refresh();
            };
        }
        */
    }

    [MenuItem("The Hero/Fix Map/Create Playable Map")]
    public static void CreatePlayableMap()
    {
        Debug.Log("[TH] Starting Map Fixer (Demo Version)...");
        EnsureFolders();
        GenerateSprites();
        SetupScenes();
        FixBuildSettings();
        Debug.Log("[TH] Map Fixer Completed.");
    }

    [MenuItem("The Hero/Build/Build Demo Windows EXE")]
    public static void BuildDemoWindows()
    {
        // Validation check
        bool valid = TheHeroDemoValidation.RunValidation();
        if (!valid)
        {
            Debug.LogError("[TH Build] Demo Validation FAILED. See log for details.");
            return;
        }

        string buildPath = "Builds/TheHeroDemo/TheHero.exe";
        string dir = Path.GetDirectoryName(buildPath);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        BuildPlayerOptions options = new BuildPlayerOptions();
        options.scenes = EditorBuildSettings.scenes.Select(s => s.path).ToArray();
        options.locationPathName = buildPath;
        options.target = BuildTarget.StandaloneWindows64;
        options.options = BuildOptions.None;

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
        {
            Debug.Log("[TH Build] Succeeded: " + buildPath);
            File.WriteAllText(dir + "/README_RUN.txt", "Запускать TheHero.exe\nУправление мышью\nЦель - победить Тёмного Лорда\nSave создаётся автоматически.");
        }
        else Debug.LogError("[TH Build] Failed.");
    }

    [MenuItem("The Hero/Fix Map/Update Main Menu")]
    public static void UpdateMainMenu()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        var canvas = GameObject.Find("Canvas");
        if (canvas == null) return;
        
        var bridge = UnityEngine.Object.FindAnyObjectByType<TheHero.Generated.THMainMenuController>();
        if (bridge == null)
        {
            var bootstrap = GameObject.Find("TH_Bootstrap");
            if (bootstrap == null) bootstrap = new GameObject("TH_Bootstrap");
            bridge = bootstrap.AddComponent<TheHero.Generated.THMainMenuController>();
        }

        SetupMainMenuUI(canvas.transform, bridge);
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void SetupMainMenuUI(Transform canvas, TheHero.Generated.THMainMenuController bridge)
        {
        if (canvas.Find("CreditsPanel") == null)
        {
            var p = CreatePanel(canvas, "CreditsPanel", new Vector2(800, 600), new Color(0, 0, 0, 0.95f));
            THUIFactory_Fixer.CreateHUDText(p.transform, "Title", "Авторы", new Vector2(0, 250));
            THUIFactory_Fixer.CreateHUDText(p.transform, "Text", "Проект: The Hero\nРазработчик: Студент\nТехнологии: Unity, C#, JSON", Vector2.zero);
            THUIFactory_Fixer.CreateButton(p.transform, "Закрыть", new Vector2(0, -250), () => p.SetActive(false));
            bridge.CreditsPanel = p; p.SetActive(false);
        }
        if (canvas.Find("HelpPanel") == null)
        {
            var p = CreatePanel(canvas, "HelpPanel", new Vector2(800, 600), new Color(0, 0, 0, 0.95f));
            THUIFactory_Fixer.CreateHUDText(p.transform, "Title", "Помощь", new Vector2(0, 250));
            string helpStr = "- Кликайте по клеткам для движения\n- Собирайте ресурсы и захватывайте шахты\n- На базе нанимайте армию\n- Цель: победить Тёмного Лорда";
            THUIFactory_Fixer.CreateHUDText(p.transform, "Text", helpStr, Vector2.zero);
            THUIFactory_Fixer.CreateButton(p.transform, "Закрыть", new Vector2(0, -250), () => p.SetActive(false));
            bridge.HelpPanel = p; p.SetActive(false);
        }
        if (canvas.Find("ConfirmationPanel") == null)
        {
            var p = CreatePanel(canvas, "ConfirmationPanel", new Vector2(400, 250), new Color(0.2f, 0, 0, 0.95f));
            THUIFactory_Fixer.CreateHUDText(p.transform, "Text", "Начать новую игру?\nСохранение будет удалено.", new Vector2(0, 50));
            THUIFactory_Fixer.CreateButton(p.transform, "Да", new Vector2(-100, -50), () => bridge.StartNewGame());
            THUIFactory_Fixer.CreateButton(p.transform, "Нет", new Vector2(100, -50), () => p.SetActive(false));
            bridge.ConfirmationPanel = p; p.SetActive(false);
        }
        if (canvas.Find("SettingsPanel") == null)
        {
            var p = CreatePanel(canvas, "SettingsPanel", new Vector2(600, 400), new Color(0, 0, 0, 0.95f));
            THUIFactory_Fixer.CreateHUDText(p.transform, "Title", "Настройки", new Vector2(0, 150));
            var ctrl = p.AddComponent<TheHero.Generated.THSettingsController>();
            ctrl.SettingsPanel = p;
            THUIFactory_Fixer.CreateButton(p.transform, "Закрыть", new Vector2(0, -150), () => ctrl.Close());
            p.SetActive(false);
        }
        EnsureButton(canvas, "New Game", new Vector2(0, 100));
        EnsureButton(canvas, "Continue", new Vector2(0, 40));
        EnsureButton(canvas, "Settings", new Vector2(0, -20));
        EnsureButton(canvas, "Help", new Vector2(0, -80));
        EnsureButton(canvas, "Credits", new Vector2(0, -140));
        EnsureButton(canvas, "Exit", new Vector2(0, -200));
    }

    private static void EnsureButton(Transform canvas, string name, Vector2 pos)
    {
        var btn = canvas.Find("Button_" + name);
        if (btn == null) THUIFactory_Fixer.CreateButton(canvas, name, pos, () => {});
    }

    private static GameObject CreatePanel(Transform parent, string name, Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>(); rt.sizeDelta = size;
        go.GetComponent<Image>().color = color; return go;
    }

    private static void EnsureFolders()
    {
        string[] paths = { "Assets/Resources/Sprites/Map", "Assets/Resources/Sprites/Units", "Assets/Scenes" };
        foreach (var p in paths) if (!AssetDatabase.IsValidFolder(p)) Directory.CreateDirectory(p);
        AssetDatabase.Refresh();
    }

    private static void GenerateSprites()
    {
        CreateSprite("Assets/Resources/Sprites/Map/tile_grass.png", Color.green);
        CreateSprite("Assets/Resources/Sprites/Map/tile_forest.png", new Color(0, 0.5f, 0));
        CreateSprite("Assets/Resources/Sprites/Map/tile_mountain.png", Color.gray);
        CreateSprite("Assets/Resources/Sprites/Map/tile_water.png", Color.blue);
        CreateSprite("Assets/Resources/Sprites/Map/tile_road.png", new Color(0.6f, 0.4f, 0.2f));
        CreateSprite("Assets/Resources/Sprites/Map/obj_gold.png", Color.yellow);
        CreateSprite("Assets/Resources/Sprites/Map/obj_wood.png", new Color(0.5f, 0.25f, 0));
        CreateSprite("Assets/Resources/Sprites/Map/obj_stone.png", new Color(0.7f, 0.7f, 0.7f));
        CreateSprite("Assets/Resources/Sprites/Map/obj_mana.png", Color.cyan);
        CreateSprite("Assets/Resources/Sprites/Map/obj_mine.png", Color.black);
        CreateSprite("Assets/Resources/Sprites/Map/obj_base.png", Color.white);
        CreateSprite("Assets/Resources/Sprites/Map/obj_enemy.png", Color.red);
        CreateSprite("Assets/Resources/Sprites/Units/hero.png", Color.blue);
        CreateSprite("Assets/Resources/Sprites/Units/unit_swordsman.png", Color.gray);
        CreateSprite("Assets/Resources/Sprites/Units/unit_archer.png", Color.green);
        CreateSprite("Assets/Resources/Sprites/Units/unit_orc.png", Color.red);
        AssetDatabase.Refresh();
    }

    private static void CreateSprite(string path, Color color)
    {
        if (File.Exists(path)) return;
        Texture2D tex = new Texture2D(16, 16);
        Color[] pixels = new Color[16 * 16];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels); tex.Apply();
        byte[] bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        AssetDatabase.ImportAsset(path);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null) { importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 16; importer.filterMode = FilterMode.Point; importer.SaveAndReimport(); }
    }

    private static void SetupScenes() { SetupScene_Map(); SetupScene_Combat(); SetupScene_Base(); }

    private static void SetupScene_Map()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var cam = Camera.main;
        cam.orthographic = true; cam.orthographicSize = 7; cam.transform.position = new Vector3(7.5f, 4.5f, -10); cam.backgroundColor = new Color(0.1f, 0.1f, 0.1f);
        var root = new GameObject("MapRoot");
        var tilesRoot = new GameObject("Tiles").transform; tilesRoot.SetParent(root.transform);
        var objectsRoot = new GameObject("Objects").transform; objectsRoot.SetParent(root.transform);
        var controllerGo = new GameObject("MapController");
        var controller = controllerGo.AddComponent<TheHero.Generated.THMapController>();
        controllerGo.AddComponent<TheHero.Generated.THDemoCampaignController>();
        controllerGo.AddComponent<TheHero.Generated.THDebugMenu>();
        
        var canvasGo = THUIFactory_Fixer.CreateCanvas();
        var canvas = canvasGo.transform;
        
        controller.GoldText = THUIFactory_Fixer.CreateHUDText(canvas, "GoldText", "Gold: 500", new Vector2(-400, 500));
        controller.WoodText = THUIFactory_Fixer.CreateHUDText(canvas, "WoodText", "Wood: 20", new Vector2(-250, 500));
        controller.StoneText = THUIFactory_Fixer.CreateHUDText(canvas, "StoneText", "Stone: 10", new Vector2(-100, 500));
        controller.ManaText = THUIFactory_Fixer.CreateHUDText(canvas, "ManaText", "Mana: 5", new Vector2(50, 500));
        controller.DayText = THUIFactory_Fixer.CreateHUDText(canvas, "DayText", "Day: 1 / Week: 1", new Vector2(800, 450));
        controller.HeroText = THUIFactory_Fixer.CreateHUDText(canvas, "HeroText", "Hero: Knight", new Vector2(800, 400));
        controller.LevelText = THUIFactory_Fixer.CreateHUDText(canvas, "LevelText", "Level: 1", new Vector2(800, 350));
        controller.MoveText = THUIFactory_Fixer.CreateHUDText(canvas, "MoveText", "Moves: 20", new Vector2(800, 300));
        controller.ArmyText = THUIFactory_Fixer.CreateHUDText(canvas, "ArmyText", "Army: 20", new Vector2(800, 250));
        controller.InfoText = THUIFactory_Fixer.CreateHUDText(canvas, "InfoText", "Кликайте для движения", new Vector2(0, -450));
        
        var qPanel = CreatePanel(canvas, "QuestPanel", new Vector2(300, 200), new Color(0, 0, 0, 0.6f));
        qPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(-800, 400);
        var quest = qPanel.AddComponent<TheHero.Generated.THQuestSystem>();
        quest.GoalTitle = THUIFactory_Fixer.CreateHUDText(qPanel.transform, "QuestTitle", "Цель", new Vector2(0, 70));
        quest.GoalText = THUIFactory_Fixer.CreateHUDText(qPanel.transform, "QuestGoal", "...", new Vector2(0, 0));
        quest.ProgressText = THUIFactory_Fixer.CreateHUDText(qPanel.transform, "QuestProgress", "0 / 9", new Vector2(0, -70));

        var idPanel = CreatePanel(canvas, "InfoDialogPanel", new Vector2(800, 400), new Color(0, 0, 0, 0.9f));
        var idCtrl = idPanel.AddComponent<TheHero.Generated.THInfoDialogPanel>();
        idCtrl.Panel = idPanel;
        idCtrl.TitleText = THUIFactory_Fixer.CreateHUDText(idPanel.transform, "Title", "Событие", new Vector2(0, 150));
        idCtrl.ContentText = THUIFactory_Fixer.CreateHUDText(idPanel.transform, "Content", "...", Vector2.zero);
        idCtrl.ContinueButton = THUIFactory_Fixer.CreateButton(idPanel.transform, "Продолжить", new Vector2(0, -150), () => {}).GetComponent<Button>();
        idPanel.SetActive(false);

        var mmPanel = CreatePanel(canvas, "MiniMapPanel", new Vector2(320, 200), new Color(0, 0, 0, 0.8f));
        mmPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(750, -400);
        var miniMap = mmPanel.AddComponent<TheHero.Generated.THMiniMap>();
        var container = new GameObject("Container", typeof(RectTransform)).GetComponent<RectTransform>();
        container.SetParent(mmPanel.transform, false); container.sizeDelta = new Vector2(300, 180);
        miniMap.Container = container;
        miniMap.PixelPrefab = new GameObject("Pixel", typeof(RectTransform), typeof(Image));
        miniMap.PixelPrefab.GetComponent<Image>().color = Color.white;
        miniMap.PixelPrefab.transform.SetParent(root.transform); miniMap.PixelPrefab.SetActive(false);

        var grass = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_grass.png");
        var road = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_road.png");
        var forest = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_forest.png");
        var mountain = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_mountain.png");
        var water = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_water.png");

        for (int y = 0; y < 10; y++) { for (int x = 0; x < 16; x++) {
            var tileGo = new GameObject("Tile_" + x + "_" + y); tileGo.transform.SetParent(tilesRoot); tileGo.transform.position = new Vector3(x, y, 0);
            var sr = tileGo.AddComponent<SpriteRenderer>(); var tile = tileGo.AddComponent<TheHero.Generated.THMapTile>(); tile.x = x; tile.y = y;
            bool isRoad = (x < 4 && y == 1) || (x == 3 && y > 0 && y < 6) || (x > 2 && x < 10 && y == 5);
            bool isWater = (y == 3 && x > 6 && x < 12);
            bool isMountain = (x == 12 && y < 6);
            bool isForest = (x > 13 && y > 6);
            if (isRoad) { sr.sprite = road; tile.moveCost = 1; }
            else if (isForest) { sr.sprite = forest; tile.moveCost = 2; }
            else if (isMountain) { sr.sprite = mountain; tile.isPassable = false; }
            else if (isWater) { sr.sprite = water; tile.isPassable = false; }
            else { sr.sprite = grass; tile.moveCost = 1; }
            sr.sortingOrder = 0; tileGo.AddComponent<BoxCollider2D>();
        } }

        var heroGo = new GameObject("Hero"); heroGo.transform.position = Vector3.zero; var hsr = heroGo.AddComponent<SpriteRenderer>();
        hsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Units/hero.png"); hsr.sortingOrder = 10;
        var hmover = heroGo.AddComponent<TheHero.Generated.THStrictGridHeroMovement>();
        controller.HeroMover = hmover;
var camFollow = cam.gameObject.AddComponent<TheHero.Generated.THCameraFollow>();
        camFollow.Target = heroGo.transform; camFollow.MinBounds = new Vector2(0, 0); camFollow.MaxBounds = new Vector2(15, 9);

        CreateMapObject(objectsRoot, "Base", TheHero.Generated.THMapObject.ObjectType.Base, 1, 1, "Assets/Resources/Sprites/Map/obj_base.png", "Замок");
        CreateMapObject(objectsRoot, "Gold_Pile", TheHero.Generated.THMapObject.ObjectType.GoldResource, 4, 2, "Assets/Resources/Sprites/Map/obj_gold.png", "Куча золота");
        CreateMapObject(objectsRoot, "Mine", TheHero.Generated.THMapObject.ObjectType.Mine, 11, 2, "Assets/Resources/Sprites/Map/obj_mine.png", "Золотая шахта");
        CreateMapObject(objectsRoot, "Treasure", TheHero.Generated.THMapObject.ObjectType.Treasure, 14, 1, "Assets/Resources/Sprites/Map/obj_stone.png", "Древний клад");
        CreateEnemy(objectsRoot, "Goblins", 5, 5, "Гоблины", "goblin", 10);
        CreateEnemy(objectsRoot, "Orcs", 9, 3, "Орки", "orc", 8);
        var dlGo = CreateMapObject(objectsRoot, "DarkLord", TheHero.Generated.THMapObject.ObjectType.Enemy, 15, 9, "Assets/Resources/Sprites/Map/obj_enemy.png", "Тёмный Лорд");
        var dl = dlGo.GetComponent<TheHero.Generated.THMapObject>();
        dl.isDarkLord = true; dl.enemyArmy.Clear();
        dl.enemyArmy.Add(new TheHero.Generated.THArmyUnit { id="dk", name="Dark Knight", count=10, hpPerUnit=50, attack=15, defense=10, initiative=6 });

        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Map.unity");
    }

    private static GameObject CreateEnemy(Transform parent, string id, int x, int y, string dName, string unitId, int count)
    {
        var go = CreateMapObject(parent, id, TheHero.Generated.THMapObject.ObjectType.Enemy, x, y, "Assets/Resources/Sprites/Map/obj_enemy.png", dName);
        var obj = go.GetComponent<TheHero.Generated.THMapObject>();
        obj.enemyArmy.Add(new TheHero.Generated.THArmyUnit { id = unitId, name = dName, count = count, hpPerUnit = 20, attack = 5, defense = 2, initiative = 5 });
        return go;
    }

    private static GameObject CreateMapObject(Transform parent, string id, TheHero.Generated.THMapObject.ObjectType type, int x, int y, string spritePath, string dName)
    {
        var go = new GameObject(id); go.transform.SetParent(parent); go.transform.position = new Vector3(x, y, 0);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath); sr.sortingOrder = 5;
        var obj = go.AddComponent<TheHero.Generated.THMapObject>(); obj.id = id; obj.type = type; obj.targetX = x; obj.targetY = y; obj.displayName = dName;
        go.AddComponent<BoxCollider2D>(); return go;
    }

    private static void SetupScene_Combat()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvasGo = THUIFactory_Fixer.CreateCanvas(); var canvas = canvasGo.transform;
        var controllerGo = new GameObject("CombatController");
        var controller = controllerGo.AddComponent<TheHero.Generated.THCombatController>();
        controller.LogText = THUIFactory_Fixer.CreateHUDText(canvas, "Log", "Battle!", Vector2.zero);
        controller.RoundText = THUIFactory_Fixer.CreateHUDText(canvas, "Round", "Раунд 1", new Vector2(0, 450));
        controller.BackButton = THUIFactory_Fixer.CreateButton(canvas, "Back to Map", new Vector2(0, -200), () => controller.BackToMap());
        var combatUIPanel = CreatePanel(canvas, "CombatUIPanel", new Vector2(400, 100), new Color(0, 0, 0, 0));
        combatUIPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -100);
        THUIFactory_Fixer.CreateButton(combatUIPanel.transform, "Атаковать", new Vector2(-100, 0), () => controller.Attack());
        THUIFactory_Fixer.CreateButton(combatUIPanel.transform, "Автобой", new Vector2(100, 0), () => controller.AutoBattle());
        controller.CombatUIPanel = combatUIPanel;
        var vPanel = CreatePanel(canvas, "VictoryPanel", new Vector2(800, 600), new Color(0, 0.5f, 0, 0.95f));
        THUIFactory_Fixer.CreateHUDText(vPanel.transform, "Title", "ПОБЕДА!", new Vector2(0, 200));
        controller.VictoryStatsText = THUIFactory_Fixer.CreateHUDText(vPanel.transform, "Stats", "...", Vector2.zero);
        THUIFactory_Fixer.CreateButton(vPanel.transform, "В меню", new Vector2(0, -200), () => controller.MainMenu());
        controller.VictoryPanel = vPanel; vPanel.SetActive(false);
        var dPanel = CreatePanel(canvas, "DefeatPanel", new Vector2(800, 600), new Color(0.5f, 0, 0, 0.95f));
        THUIFactory_Fixer.CreateHUDText(dPanel.transform, "Title", "ПОРАЖЕНИЕ", new Vector2(0, 200));
        THUIFactory_Fixer.CreateButton(dPanel.transform, "Загрузить", new Vector2(0, 0), () => controller.LoadLastSave());
        THUIFactory_Fixer.CreateButton(dPanel.transform, "В меню", new Vector2(0, -100), () => controller.MainMenu());
        controller.DefeatPanel = dPanel; dPanel.SetActive(false);
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Combat.unity");
    }

    private static void SetupScene_Base()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
        var canvasGo = THUIFactory_Fixer.CreateCanvas(); var canvas = canvasGo.transform;
        var controllerGo = new GameObject("BaseController");
        var controller = controllerGo.AddComponent<TheHero.Generated.THBaseController>();
        controller.ResourcesText = THUIFactory_Fixer.CreateHUDText(canvas, "Res", "Resources", new Vector2(0, 500));
        controller.DayWeekText = THUIFactory_Fixer.CreateHUDText(canvas, "DayWeek", "День: 1 / Неделя: 1", new Vector2(0, 450));
        controller.InfoText = THUIFactory_Fixer.CreateHUDText(canvas, "Info", "Welcome", new Vector2(0, -450));
        controller.ArmyListText = THUIFactory_Fixer.CreateHUDText(canvas, "ArmyList", "Army", new Vector2(-700, 0));
        var bPanel = CreatePanel(canvas, "BuildingsPanel", new Vector2(1000, 400), new Color(0, 0, 0, 0.6f));
        SetupBuildingCard(bPanel.transform, "Barracks", "Мечники", "barracks", new Vector2(-300, 0), controller);
        SetupBuildingCard(bPanel.transform, "Range", "Лучники", "range", new Vector2(0, 0), controller);
        SetupBuildingCard(bPanel.transform, "MageTower", "Маги", "mage", new Vector2(300, 0), controller);
        THUIFactory_Fixer.CreateButton(canvas, "Back", new Vector2(0, -350), () => controller.BackToMap());
        EditorSceneManager.SaveScene(scene, "Assets/Scenes/Base.unity");
    }

    private static void SetupBuildingCard(Transform parent, string name, string label, string id, Vector2 pos, TheHero.Generated.THBaseController ctrl)
    {
        var card = CreatePanel(parent, "Card_" + name, new Vector2(250, 350), new Color(0.1f, 0.1f, 0.1f, 0.9f));
        card.GetComponent<RectTransform>().anchoredPosition = pos;
        THUIFactory_Fixer.CreateHUDText(card.transform, "Label", label, new Vector2(0, 140));
        THUIFactory_Fixer.CreateButton(card.transform, "Нанять 1", new Vector2(0, 40), () => ctrl.Recruit(id));
        THUIFactory_Fixer.CreateButton(card.transform, "Нанять всех", new Vector2(0, -20), () => ctrl.RecruitAll(id));
        THUIFactory_Fixer.CreateButton(card.transform, "Улучшить", new Vector2(0, -100), () => ctrl.Upgrade(id));
    }

    private static void FixBuildSettings() { string[] paths = { "Assets/Scenes/MainMenu.unity", "Assets/Scenes/Map.unity", "Assets/Scenes/Combat.unity", "Assets/Scenes/Base.unity" }; var scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray(); EditorBuildSettings.scenes = scenes; }
}

public static class THUIFactory_Fixer
{
    public static GameObject CreateCanvas() { var go = new GameObject("Canvas"); var canvas = go.AddComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; go.AddComponent<GraphicRaycaster>(); return go; }
    public static Text CreateHUDText(Transform parent, string name, string content, Vector2 pos) { var go = new GameObject(name, typeof(RectTransform), typeof(Text)); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(600, 50); var t = go.GetComponent<Text>(); t.text = content; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); t.fontSize = 24; t.alignment = TextAnchor.MiddleCenter; t.color = new Color(1f, 0.85f, 0.5f); go.AddComponent<Outline>().effectColor = Color.black; return t; }
    public static GameObject CreateButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick) { var go = new GameObject("Button_" + label, typeof(RectTransform), typeof(Image), typeof(Button)); go.transform.SetParent(parent, false); var rt = go.GetComponent<RectTransform>(); rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(180, 45); go.GetComponent<Image>().color = new Color(0.15f, 0.15f, 0.15f, 0.95f); var txtGo = new GameObject("Text", typeof(RectTransform), typeof(Text)); txtGo.transform.SetParent(go.transform, false); var txt = txtGo.GetComponent<Text>(); txt.text = label; txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); txt.alignment = TextAnchor.MiddleCenter; txt.color = Color.white; txtGo.GetComponent<RectTransform>().sizeDelta = rt.sizeDelta; go.GetComponent<Button>().onClick.AddListener(onClick); return go; }
}
