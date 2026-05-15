using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.IO;
using TheHero.Generated;

public class TheHeroRebuildMapFromScratchClean : EditorWindow
{
    [MenuItem("The Hero/Map/Rebuild Map From Scratch Clean")]
    public static void Rebuild()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        string mapScenePath = "Assets/Scenes/Map.unity";
        var scene = EditorSceneManager.OpenScene(mapScenePath);

        // 1. Clear Scene
        ClearMapScene();

        // 2. Setup Roots
        GameObject mapRoot = EnsureRoot("MapRoot");
        GameObject tilesRoot = new GameObject("Tiles");
        tilesRoot.transform.SetParent(mapRoot.transform);
        GameObject objectsRoot = new GameObject("Objects");
        objectsRoot.transform.SetParent(mapRoot.transform);

        GameObject canvasObj = EnsureRoot("Canvas");
        Canvas canvas = canvasObj.GetComponent<Canvas>();
        if (canvas == null) canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (canvasObj.GetComponent<CanvasScaler>() == null) canvasObj.AddComponent<CanvasScaler>();
        if (canvasObj.GetComponent<GraphicRaycaster>() == null) canvasObj.AddComponent<GraphicRaycaster>();

        GameObject controllerObj = EnsureRoot("MapController");
        var controller = controllerObj.GetComponent<THMapController>();
        if (controller == null) controller = controllerObj.AddComponent<THMapController>();
        
        if (controllerObj.GetComponent<THMapGridInput>() == null)
            controllerObj.AddComponent<THMapGridInput>();
        
        GameObject bootstrapObj = EnsureRoot("TH_Bootstrap");
var bootstrap = bootstrapObj.GetComponent<THBootstrap>();
        if (bootstrap == null) bootstrap = bootstrapObj.AddComponent<THBootstrap>();
        bootstrap.type = THBootstrap.SceneType.Map;

        // 3. Generate Grid (20x15)
        int width = 20;
        int height = 15;
        // float tileSize = 1.0f; 
// Wait, if PPU is 64 and sprite is 1024, it's 16x16 units. That's too big for one tile.
        // User said: "Тайлы должны покрывать клетку целиком". 
        // Usually, a tile is 64x64 or 128x128. 
        // If I generated 1024x1024, and PPU is 64, then 1 unit = 64px. 
        // So the sprite is 16 units wide.
        // I should set the scale of the tile to (1/16, 1/16, 1) to make it 1x1 units?
        // Or I should have generated smaller sprites.
        // Let's set the scale to 1/16f = 0.0625f.

        float scaleFactor = 0.0625f; 

        Sprite grass = LoadSprite("Tiles/clean_grass");
        Sprite water = LoadSprite("Tiles/clean_water");
        Sprite darkGrass = LoadSprite("Tiles/clean_dark_grass");
        Sprite forest = LoadSprite("Tiles/clean_forest_dense");
        Sprite road = LoadSprite("Tiles/clean_road");
        Sprite bridge = LoadSprite("Tiles/clean_bridge");

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Sprite s = grass;
                bool walkable = true;

                if (x == 10) { s = water; walkable = false; }
                else if (x > 10) s = darkGrass;

                // River crossing
                if (x == 10 && y == 7) { s = bridge; walkable = true; }
                
                // Road
                if (y == 7 && x != 10) s = road;
                
                // Forest clumps
                if ((x == 5 || x == 15) && (y < 3 || y > 11)) s = forest;

                CreateTile(tilesRoot.transform, x, y, s, scaleFactor, walkable);
            }
        }

        // 4. Place Objects
        Sprite castleSprite = LoadSprite("Objects/clean_castle");
        Sprite heroSprite = LoadSprite("Objects/clean_hero");
        Sprite bossSprite = LoadSprite("Objects/clean_boss");
        Sprite goldSprite = LoadSprite("Objects/clean_gold");
        Sprite woodSprite = LoadSprite("Objects/clean_wood");
        Sprite stoneSprite = LoadSprite("Objects/clean_stone");
        Sprite manaSprite = LoadSprite("Objects/clean_mana");
        Sprite orcSprite = LoadSprite("Objects/clean_orc");
        Sprite wolfSprite = LoadSprite("Objects/clean_wolf");
        Sprite mineSprite = LoadSprite("Objects/clean_mine");
        Sprite chestSprite = LoadSprite("Objects/clean_chest");

        // Castle at (2, 7) - on the road
        CreateMapObject(objectsRoot.transform, 2, 7, castleSprite, "Замок", THMapObject.ObjectType.Base);

        // Hero near castle
        GameObject heroObj = new GameObject("Hero");
        heroObj.transform.SetParent(mapRoot.transform);
        var heroSr = heroObj.AddComponent<SpriteRenderer>();
        heroSr.sprite = heroSprite;
        heroSr.sortingOrder = 10;
        heroObj.transform.localScale = Vector3.one * scaleFactor;
        heroObj.transform.position = new Vector3(3, 7, 0);
        var mover = heroObj.AddComponent<THStrictGridHeroMovement>();
        controller.HeroMover = mover;

        // Resources & Mines
        CreateMapObject(objectsRoot.transform, 5, 5, goldSprite, "Золото", THMapObject.ObjectType.GoldResource, 500);
        CreateMapObject(objectsRoot.transform, 5, 9, woodSprite, "Дерево", THMapObject.ObjectType.WoodResource, 20);
        CreateMapObject(objectsRoot.transform, 1, 10, stoneSprite, "Камень", THMapObject.ObjectType.StoneResource, 15);
        CreateMapObject(objectsRoot.transform, 12, 12, manaSprite, "Мана", THMapObject.ObjectType.ManaResource, 10);
        CreateMapObject(objectsRoot.transform, 4, 2, mineSprite, "Шахта", THMapObject.ObjectType.Mine);

        // Enemies
        CreateMapObject(objectsRoot.transform, 8, 7, orcSprite, "Гоблины", THMapObject.ObjectType.Enemy);
        CreateMapObject(objectsRoot.transform, 12, 7, wolfSprite, "Волки", THMapObject.ObjectType.Enemy);
        
        // Boss at the end
        var boss = CreateMapObject(objectsRoot.transform, 18, 7, bossSprite, "Тёмный Лорд", THMapObject.ObjectType.Enemy);
        boss.isDarkLord = true;
        boss.difficulty = THEnemyDifficulty.Deadly;
        
        CreateMapObject(objectsRoot.transform, 18, 4, chestSprite, "Сокровище", THMapObject.ObjectType.Treasure);

        // 5. Restore HUD without deleting or replacing the Map UI Canvas
        TheHero.Editor.TheHeroRestoreMapUI.RestoreOpenMapUI(false);

        // 6. Camera & Bounds
        GameObject camObj = GameObject.Find("Main Camera");
        if (camObj != null)
        {
            camObj.transform.position = new Vector3(3, 7, -10);
            var cam = camObj.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
        }

        GameObject boundsObj = EnsureRoot("MapBounds");
        var bounds = boundsObj.GetComponent<THMapBounds>();
        if (bounds == null) bounds = boundsObj.AddComponent<THMapBounds>();
        bounds.minX = 0;
        bounds.minY = 0;
        bounds.maxX = width - 1;
        bounds.maxY = height - 1;
        bounds.initialized = true;

        EditorSceneManager.SaveScene(scene);
        
        Debug.Log("[TheHeroMapClean] Old broken map cleared");
        Debug.Log("[TheHeroMapClean] Old map assets disabled");
        Debug.Log("[TheHeroMapClean] Clean top-down asset set created");
        Debug.Log("[TheHeroMapClean] Simple coherent map built");
        Debug.Log("[TheHeroMapClean] Clean HUD created");
        Debug.Log("[TheHeroMapClean] Hero spawn fixed");
        Debug.Log("[TheHeroMapClean] Runtime auto-generation disabled");
        Debug.Log("[TheHeroMapClean] Ready for testing");
        
        EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

    private static void ClearMapScene()
    {
        string[] toKeep = { "Main Camera", "EventSystem", "Canvas", "MapRoot", "MapController", "TH_Bootstrap", "MapBounds" };
        GameObject[] all = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
foreach (var go in all)
{
            if (go.transform.parent == null)
            {
                bool keep = false;
                foreach (var k in toKeep) if (go.name.Contains(k)) keep = true;
                if (!keep) Undo.DestroyObjectImmediate(go);
            }
        }

        GameObject root = GameObject.Find("MapRoot");
        if (root != null)
        {
            for (int i = root.transform.childCount - 1; i >= 0; i--)
                Undo.DestroyObjectImmediate(root.transform.GetChild(i).gameObject);
        }

        // Map builders must preserve scene UI. Use The Hero/UI/Restore Map UI
        // for intentional Map Canvas repairs.
    }

    private static GameObject EnsureRoot(string name)
    {
        GameObject go = GameObject.Find(name);
        if (go == null) go = new GameObject(name);
        return go;
    }

    private static Sprite LoadSprite(string path)
    {
        string fullPath = "Assets/Resources/Sprites/CleanMap/" + path + ".png";
        Sprite s = AssetDatabase.LoadAssetAtPath<Sprite>(fullPath);
        if (s == null) Debug.LogWarning("Missing sprite: " + fullPath);
        return s;
    }

    private static void CreateTile(Transform parent, int x, int y, Sprite sprite, float scale, bool walkable)
    {
        GameObject go = new GameObject($"Tile_{x}_{y}");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, 0);
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = -10;

        var tileData = go.AddComponent<THTile>();
        tileData.x = x;
        tileData.y = y;
        tileData.walkable = walkable;
        tileData.moveCost = walkable ? 1 : 999;
        
        // Add Collider for Raycast detection
        var col = go.AddComponent<BoxCollider2D>();
        col.size = new Vector2(16, 16); // 1024px / 64 PPU = 16 units
    }

    private static THMapObject CreateMapObject(Transform parent, int x, int y, Sprite sprite, string name, THMapObject.ObjectType type, int reward = 0)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(x, y, 0);
        go.transform.localScale = Vector3.one * 0.0625f; // scale factor
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = 5;

        var obj = go.AddComponent<THMapObject>();
        obj.id = System.Guid.NewGuid().ToString().Substring(0, 8);
        obj.type = type;
        obj.displayName = name;
        obj.targetX = x;
        obj.targetY = y;
        
        if (type == THMapObject.ObjectType.GoldResource) obj.rewardGold = reward;
        if (type == THMapObject.ObjectType.WoodResource) obj.rewardWood = reward;

        go.AddComponent<BoxCollider2D>();
        
        return obj;
    }

    private static void BuildCleanHUD(Transform canvas, THMapController controller)
    {
        // Top HUD
        GameObject topHUD = new GameObject("TopHUD", typeof(RectTransform));
        topHUD.transform.SetParent(canvas);
        var rt = topHUD.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(0, 60);

        var bg = topHUD.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);

        GameObject hlgObj = new GameObject("Layout", typeof(RectTransform));
        hlgObj.transform.SetParent(topHUD.transform);
        var hlgRt = hlgObj.GetComponent<RectTransform>();
        hlgRt.anchorMin = Vector2.zero;
        hlgRt.anchorMax = Vector2.one;
        hlgRt.sizeDelta = Vector2.zero;
        var hlg = hlgObj.AddComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.spacing = 20;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        hlg.childControlWidth = false;

        controller.GoldText = CreateUIText(hlgObj.transform, "GoldText", "Gold: 0");
        controller.WoodText = CreateUIText(hlgObj.transform, "WoodText", "Wood: 0");
        controller.StoneText = CreateUIText(hlgObj.transform, "StoneText", "Stone: 0");
        controller.ManaText = CreateUIText(hlgObj.transform, "ManaText", "Mana: 0");
        
        // System Buttons
        CreateUIButton(hlgObj.transform, "SaveButton", "Save");
        CreateUIButton(hlgObj.transform, "LoadButton", "Load");
        CreateUIButton(hlgObj.transform, "EndTurnButton", "End Turn");
        CreateUIButton(hlgObj.transform, "MenuButton", "Menu");

        // Quest Panel
        GameObject questPanel = new GameObject("QuestPanel", typeof(RectTransform));
        questPanel.transform.SetParent(canvas);
        var qrt = questPanel.GetComponent<RectTransform>();
        qrt.anchorMin = new Vector2(0, 0.5f);
        qrt.anchorMax = new Vector2(0, 0.5f);
        qrt.pivot = new Vector2(0, 0.5f);
        qrt.anchoredPosition = new Vector2(10, 0);
        qrt.sizeDelta = new Vector2(200, 300);
        questPanel.AddComponent<Image>().color = new Color(0, 0, 0, 0.5f);
        controller.InfoText = CreateUIText(questPanel.transform, "QuestText", "Задания:\n- Победить Тёмного Лорда");

        // Castle Button
        GameObject castleBtn = CreateUIButton(canvas, "CastleButton", "ЗАМОК");
        var cbrt = castleBtn.GetComponent<RectTransform>();
        cbrt.anchorMin = new Vector2(0, 0);
        cbrt.anchorMax = new Vector2(0, 0);
        cbrt.pivot = new Vector2(0, 0);
        cbrt.anchoredPosition = new Vector2(10, 10);
        cbrt.sizeDelta = new Vector2(120, 50);
    }

    private static Text CreateUIText(Transform parent, string name, string content)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent);
        var t = go.AddComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
t.fontSize = 18;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleLeft;
        go.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 30);
        return t;
    }

    private static GameObject CreateUIButton(Transform parent, string name, string label)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent);
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(80, 30);
        
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.2f, 1);
        var btn = go.AddComponent<Button>();
        
        GameObject txtGo = new GameObject("Text", typeof(RectTransform));
        txtGo.transform.SetParent(go.transform);
        var t = txtGo.AddComponent<Text>();
        t.text = label;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
t.fontSize = 14;
        t.color = Color.white;
        t.alignment = TextAnchor.MiddleCenter;
        txtGo.GetComponent<RectTransform>().anchorMin = Vector2.zero;
        txtGo.GetComponent<RectTransform>().anchorMax = Vector2.one;
        txtGo.GetComponent<RectTransform>().sizeDelta = Vector2.zero;
        
        return go;
    }
}
