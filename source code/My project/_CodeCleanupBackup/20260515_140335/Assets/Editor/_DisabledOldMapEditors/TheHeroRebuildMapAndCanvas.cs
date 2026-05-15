using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroRebuildMapAndCanvas
    {
        private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
        private const string MapPath = "Assets/Scenes/Map.unity";
        private const string SpritePath = "Assets/Resources/Sprites/";

        [MenuItem("The Hero/Fix/Rebuild Map Canvas And Expand World")]
        public static void RebuildEverything()
        {
            RebuildMainMenu();
            RebuildMap();
            
            EditorSceneManager.OpenScene(MainMenuPath);
            AssetDatabase.SaveAssets();
            Debug.Log("<color=green>[TheHeroWorldFix] Full Rebuild Complete!</color>");
        }

        private static void RebuildMainMenu()
        {
            Debug.Log("[TheHeroWorldFix] Rebuilding MainMenu...");
            EditorSceneManager.OpenScene(MainMenuPath);
            Canvas canvas = EnsureOneCanvas();
            ClearCanvas(canvas);
            SetupCanvasScaler(canvas);
            
            GameObject bgGo = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bgGo.transform.SetParent(canvas.transform, false);
            SetFullStretch(bgGo.GetComponent<RectTransform>());
            var bgImg = bgGo.GetComponent<Image>();
            bgImg.sprite = Resources.Load<Sprite>("Sprites/UI/mm_clean_dark_fantasy_bg");
            bgImg.raycastTarget = false;

            GameObject overlayGo = new GameObject("DarkOverlay", typeof(RectTransform), typeof(Image));
            overlayGo.transform.SetParent(canvas.transform, false);
            SetFullStretch(overlayGo.GetComponent<RectTransform>());
            overlayGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.4f);
            overlayGo.GetComponent<Image>().raycastTarget = false;

            GameObject panelGo = new GameObject("MainMenuPanel", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Shadow));
            panelGo.transform.SetParent(canvas.transform, false);
            var pRt = panelGo.GetComponent<RectTransform>();
            pRt.sizeDelta = new Vector2(520, 680);
            pRt.anchoredPosition = new Vector2(0, -40);
            panelGo.GetComponent<Image>().color = new Color32(20, 16, 28, 225);
            panelGo.GetComponent<Image>().raycastTarget = false;
            panelGo.GetComponent<Outline>().effectColor = new Color(0.8f, 0.6f, 0.2f);

            GameObject titleGo = CreateTextGo("TitleText", panelGo.transform, "THE HERO", 72, new Color(1, 0.85f, 0.2f), TextAnchor.MiddleCenter);
            titleGo.GetComponent<RectTransform>().anchorMin = titleGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
            titleGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -45);
            titleGo.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 95);
            titleGo.AddComponent<Shadow>().effectDistance = new Vector2(2, -2);

            GameObject subGo = CreateTextGo("SubtitleText", panelGo.transform, "Fantasy Turn-Based Strategy", 26, new Color(0.8f, 0.8f, 0.8f), TextAnchor.MiddleCenter);
            subGo.GetComponent<RectTransform>().anchorMin = subGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 1f);
            subGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -120);
            subGo.GetComponent<RectTransform>().sizeDelta = new Vector2(480, 45);

            GameObject containerGo = new GameObject("ButtonsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup));
            containerGo.transform.SetParent(panelGo.transform, false);
            containerGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -55);
            containerGo.GetComponent<RectTransform>().sizeDelta = new Vector2(380, 390);
            var vlg = containerGo.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = vlg.childControlHeight = true;

            string[] names = { "NewGameButton", "ContinueButton", "SettingsButton", "HelpButton", "ExitButton" };
            string[] labels = { "НОВАЯ ИГРА", "ПРОДОЛЖИТЬ", "НАСТРОЙКИ", "ПОМОЩЬ", "ВЫХОД" };
            for (int i = 0; i < names.Length; i++) CreateMenuButton(containerGo.transform, names[i], labels[i]);

            GameObject verGo = CreateTextGo("VersionText", panelGo.transform, "v1.0.0-demo", 16, Color.gray, TextAnchor.MiddleCenter);
            verGo.GetComponent<RectTransform>().anchorMin = verGo.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            verGo.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 24);
            verGo.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);

            GameObject settingsPanel = CreateSubPanel(canvas.transform, "SettingsPanel", "НАСТРОЙКИ");
            GameObject helpPanel = CreateSubPanel(canvas.transform, "HelpPanel", "ПОМОЩЬ");
            SetupHelpText(helpPanel);

            var ctrl = panelGo.AddComponent<THCleanMainMenuController>();
            ctrl.SettingsPanel = settingsPanel; ctrl.HelpPanel = helpPanel;
            ctrl.NewGameButton = containerGo.transform.Find("NewGameButton").GetComponent<Button>();
            ctrl.ContinueButton = containerGo.transform.Find("ContinueButton").GetComponent<Button>();
            ctrl.SettingsButton = containerGo.transform.Find("SettingsButton").GetComponent<Button>();
            ctrl.HelpButton = containerGo.transform.Find("HelpButton").GetComponent<Button>();
            ctrl.ExitButton = containerGo.transform.Find("ExitButton").GetComponent<Button>();
            ctrl.CloseSettingsButton = settingsPanel.transform.Find("CloseButton").GetComponent<Button>();
            ctrl.CloseHelpButton = helpPanel.transform.Find("CloseButton").GetComponent<Button>();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void RebuildMap()
        {
            Debug.Log("[TheHeroWorldFix] Rebuilding Map...");
            EditorSceneManager.OpenScene(MapPath);
            Canvas canvas = EnsureOneCanvas();
            ClearCanvas(canvas);
            SetupCanvasScaler(canvas);
            
            // Remove MainMenu artifacts
            foreach(var go in GameObject.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
if (go.name.Contains("MainMenuPanel") || go.name.Contains("DecorativeFrame") || go.name.Contains("TitleText")) 
                    Object.DestroyImmediate(go);

            SetupMapHUD(canvas);
            RebuildMapGrid(32, 20);
            
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }

        private static void SetupMapHUD(Canvas canvas)
        {
            GameObject topHUD = new GameObject("TopHUD", typeof(RectTransform), typeof(Image));
            topHUD.transform.SetParent(canvas.transform, false);
            var rt = topHUD.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1); rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1); rt.sizeDelta = new Vector2(0, 58);
            topHUD.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            GameObject resGroup = new GameObject("ResourcesGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            resGroup.transform.SetParent(topHUD.transform, false);
            var rRt = resGroup.GetComponent<RectTransform>();
            rRt.anchorMin = Vector2.zero; rRt.anchorMax = new Vector2(0.6f, 1); rRt.offsetMin = new Vector2(20, 0);
            var hlg = resGroup.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 20; hlg.childAlignment = TextAnchor.MiddleLeft; hlg.childControlWidth = false;

            CreateHUDText(resGroup.transform, "GoldText", "Золото: 0");
            CreateHUDText(resGroup.transform, "WoodText", "Дерево: 0");
            CreateHUDText(resGroup.transform, "StoneText", "Камень: 0");
            CreateHUDText(resGroup.transform, "ManaText", "Мана: 0");

            GameObject btnGroup = new GameObject("ButtonsGroup", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            btnGroup.transform.SetParent(topHUD.transform, false);
            var bRt = btnGroup.GetComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.6f, 0); bRt.anchorMax = Vector2.one; bRt.offsetMax = new Vector2(-20, 0);
            var bhlg = btnGroup.GetComponent<HorizontalLayoutGroup>();
            bhlg.spacing = 10; bhlg.childAlignment = TextAnchor.MiddleRight; bhlg.childControlWidth = false;

            CreateHUDButton(btnGroup.transform, "SaveButton", "Save");
            CreateHUDButton(btnGroup.transform, "LoadButton", "Load");
            CreateHUDButton(btnGroup.transform, "EndTurnButton", "End Turn");
            CreateHUDButton(btnGroup.transform, "MenuButton", "Menu");

            GameObject questPanel = new GameObject("QuestPanel", typeof(RectTransform), typeof(Image));
            questPanel.transform.SetParent(canvas.transform, false);
            var qRt = questPanel.GetComponent<RectTransform>();
            qRt.anchorMin = qRt.anchorMax = new Vector2(0, 1); qRt.pivot = new Vector2(0, 1);
            qRt.anchoredPosition = new Vector2(20, -78); qRt.sizeDelta = new Vector2(420, 110);
            questPanel.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            CreateTextGo("QuestTitleText", questPanel.transform, "Цель: Победить Тёмного Лорда", 22, Color.yellow, TextAnchor.UpperLeft)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -10);
            CreateTextGo("QuestBodyText", questPanel.transform, "Прогресс: 0/10 врагов повержено", 18, Color.white, TextAnchor.UpperLeft)
                .GetComponent<RectTransform>().anchoredPosition = new Vector2(10, -45);

            GameObject castleBtn = CreateHUDButton(canvas.transform, "CastleButton", "ЗАМОК");
            castleBtn.GetComponent<RectTransform>().anchorMin = castleBtn.GetComponent<RectTransform>().anchorMax = Vector2.zero;
            castleBtn.GetComponent<RectTransform>().pivot = Vector2.zero;
            castleBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(20, 20);
            castleBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(140, 48);
        }

        private static void RebuildMapGrid(int width, int height)
        {
            GameObject tilesGo = GameObject.Find("Tiles") ?? new GameObject("Tiles");
            tilesGo.transform.position = Vector3.zero;
            for (int i = tilesGo.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(tilesGo.transform.GetChild(i).gameObject);

            GameObject objectsGo = GameObject.Find("Objects") ?? new GameObject("Objects");
            for (int i = objectsGo.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(objectsGo.transform.GetChild(i).gameObject);

            Vector3 offset = new Vector3(-width / 2f, -height / 2f, 0);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    string type = "grass";
                    if (x > width * 0.8f) type = "darkland";
                    else if (x > width * 0.6f && y < height * 0.4f) type = "mountain";
                    else if (x > width * 0.3f && y > height * 0.6f) type = "forest";
                    else if (Random.value < 0.03f) type = "water";
                    if (y == 10 && x < width * 0.8f) type = "road";

                    GameObject tileGo = new GameObject($"Tile_{x}_{y}", typeof(SpriteRenderer), typeof(THTile), typeof(BoxCollider2D));
                    tileGo.transform.SetParent(tilesGo.transform);
                    tileGo.transform.position = offset + new Vector3(x, y, 0);
                    tileGo.GetComponent<THTile>().Setup(x, y, type);
                    var sr = tileGo.GetComponent<SpriteRenderer>();
                    int var = Random.Range(1, 5);
                    sr.sprite = Resources.Load<Sprite>($"Sprites/Map/tile_{type}_0{var}") ?? Resources.Load<Sprite>($"Sprites/Map/tile_{type}_01") ?? Resources.Load<Sprite>($"Sprites/Map/{type}");
                    sr.sortingOrder = 0;
                }
            }

            // Castle
            SpawnMapObject(objectsGo.transform, "Castle_Player", THMapObject.ObjectType.Base, 4, 10, "Замок", "base", 2.0f);
            
            // Resources: Gold x8
            int[] goldX = { 6, 8, 12, 18, 22, 25, 29, 31 };
            int[] goldY = { 11, 9, 5, 12, 16, 4, 8, 11 };
            for(int i=0; i<8; i++) SpawnMapObject(objectsGo.transform, "Gold_"+i, THMapObject.ObjectType.GoldResource, goldX[i], goldY[i], "Золото", "gold_pile", 1.0f);

            // Wood x6
            int[] woodX = { 10, 15, 20, 24, 28, 5 };
            int[] woodY = { 13, 17, 18, 12, 19, 15 };
            for(int i=0; i<6; i++) SpawnMapObject(objectsGo.transform, "Wood_"+i, THMapObject.ObjectType.WoodResource, woodX[i], woodY[i], "Дерево", "forest", 1.0f);

            // Stone x5
            int[] stoneX = { 18, 22, 25, 14, 27 };
            int[] stoneY = { 4, 3, 2, 7, 5 };
            for(int i=0; i<5; i++) SpawnMapObject(objectsGo.transform, "Stone_"+i, THMapObject.ObjectType.StoneResource, stoneX[i], stoneY[i], "Камень", "mountain", 1.0f);

            // Mana x5
            int[] manaX = { 28, 29, 31, 26, 30 };
            int[] manaY = { 14, 18, 16, 17, 19 };
            for(int i=0; i<5; i++) SpawnMapObject(objectsGo.transform, "Mana_"+i, THMapObject.ObjectType.ManaResource, manaX[i], manaY[i], "Кристалл Маны", "mana_crystal", 1.0f);

            // Chests x4
            int[] chestX = { 10, 20, 25, 30 };
            int[] chestY = { 5, 15, 8, 3 };
            for(int i=0; i<4; i++) SpawnMapObject(objectsGo.transform, "Chest_"+i, THMapObject.ObjectType.Treasure, chestX[i], chestY[i], "Сундук Сокровищ", "gold_pile", 1.1f);

            // Mines x2
            SpawnMapObject(objectsGo.transform, "Mine_Gold_1", THMapObject.ObjectType.Mine, 15, 10, "Золотая Шахта", "mine", 1.4f);
            SpawnMapObject(objectsGo.transform, "Mine_Gold_2", THMapObject.ObjectType.Mine, 25, 6, "Золотая Шахта", "mine", 1.4f);

            // Shrines x2
            SpawnMapObject(objectsGo.transform, "Shrine_1", THMapObject.ObjectType.Shrine, 20, 12, "Святилище Опыта", "base", 1.3f);
            SpawnMapObject(objectsGo.transform, "Shrine_2", THMapObject.ObjectType.Shrine, 5, 5, "Святилище Опыта", "base", 1.3f);

            // Enemies x10
            string[] enemyNames = { "Goblins_1", "Wolves_1", "Bandits_1", "Goblins_2", "Orcs_1", "Skeletons_1", "Bandits_2", "Knights_1", "Knights_2", "DarkLord" };
            int[] enemyX = { 10, 8, 15, 18, 22, 26, 20, 28, 29, 31 };
            int[] enemyY = { 10, 12, 11, 15, 8, 13, 5, 15, 5, 10 };
            for(int i=0; i<10; i++) SpawnMapObject(objectsGo.transform, "Enemy_"+enemyNames[i], THMapObject.ObjectType.Enemy, enemyX[i], enemyY[i], enemyNames[i], "enemy", 1.2f, i==9);

            GameObject hero = GameObject.Find("Hero");
            if (hero) {
                hero.transform.position = offset + new Vector3(4, 10, 0);
                var mover = hero.GetComponent<THStrictGridHeroMovement>();
                if (mover) mover.SetPositionImmediate(4, 10);
                
                Camera cam = Camera.main;
                if (cam) {
                    cam.transform.position = new Vector3(hero.transform.position.x, hero.transform.position.y, -10);
                    var follow = cam.gameObject.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
                    follow.Target = hero.transform; follow.MinBounds = new Vector2(-16, -10); follow.MaxBounds = new Vector2(16, 10);
                }
            }
        }

        private static void SpawnMapObject(Transform parent, string name, THMapObject.ObjectType type, int x, int y, string disp, string sprite, float scale, bool isBoss = false)
        {
            GameObject go = new GameObject(name, typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(-16 + x, -10 + y, 0);
            go.transform.localScale = Vector3.one * scale;
            var mo = go.GetComponent<THMapObject>();
            mo.id = name; mo.type = type; mo.targetX = x; mo.targetY = y; mo.displayName = disp; mo.isDarkLord = isBoss;
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>($"Sprites/Map/{sprite}"); sr.sortingOrder = 25;
        }

        private static Canvas EnsureOneCanvas()
        {
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
foreach (var c in canvases.Skip(1)) Object.DestroyImmediate(c.gameObject);
            return canvases.FirstOrDefault() ?? new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)).GetComponent<Canvas>();
        }

        private static void ClearCanvas(Canvas canvas) { for (int i = canvas.transform.childCount - 1; i >= 0; i--) Object.DestroyImmediate(canvas.transform.GetChild(i).gameObject); }
        private static void SetFullStretch(RectTransform rt) { rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero; }

        private static void CreateMenuButton(Transform parent, string name, string label)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
            btnGo.transform.SetParent(parent, false);
            btnGo.GetComponent<LayoutElement>().preferredWidth = 360; btnGo.GetComponent<LayoutElement>().preferredHeight = 58;
            btnGo.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/UI/mm_button_normal");
            var btn = btnGo.GetComponent<Button>(); btn.transition = Selectable.Transition.SpriteSwap;
            var ss = btn.spriteState; 
            ss.highlightedSprite = Resources.Load<Sprite>("Sprites/UI/mm_button_hover");
            ss.pressedSprite = Resources.Load<Sprite>("Sprites/UI/mm_button_pressed");
            btn.spriteState = ss;
            var txtGo = CreateTextGo("Text", btnGo.transform, label, 24, new Color(0.95f, 0.95f, 0.9f), TextAnchor.MiddleCenter);
            SetFullStretch(txtGo.GetComponent<RectTransform>());
            btnGo.AddComponent<THFantasyButtonHover>();
        }

        private static GameObject CreateSubPanel(Transform parent, string name, string title)
        {
            GameObject panelGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
            panelGo.transform.SetParent(parent, false);
            panelGo.GetComponent<RectTransform>().sizeDelta = new Vector2(620, 420);
            panelGo.GetComponent<Image>().color = new Color32(15, 12, 24, 250);
            panelGo.GetComponent<Outline>().effectColor = new Color(0.8f, 0.6f, 0.2f);
            CreateTextGo("Title", panelGo.transform, title, 36, new Color(1, 0.84f, 0), TextAnchor.UpperCenter).GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -30);
            var closeBtn = CreateHUDButton(panelGo.transform, "CloseButton", "ЗАКРЫТЬ");
            closeBtn.GetComponent<RectTransform>().anchorMin = closeBtn.GetComponent<RectTransform>().anchorMax = new Vector2(0.5f, 0);
            closeBtn.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 30);
            closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(180, 50);
            panelGo.SetActive(false); return panelGo;
        }

        private static void SetupHelpText(GameObject panel)
        {
            GameObject contentGo = CreateTextGo("Content", panel.transform, "Цель игры — победить Тёмного Лорда.\n\nКликайте по карте, чтобы перемещать героя.\nСобирайте ресурсы и побеждайте врагов.\nВ замке нанимайте юнитов и улучшайте здания.", 22, Color.white, TextAnchor.UpperLeft);
            var rt = contentGo.GetComponent<RectTransform>(); SetFullStretch(rt); rt.offsetMin = new Vector2(40, 100); rt.offsetMax = new Vector2(-40, -100);
        }

        private static GameObject CreateTextGo(string name, Transform parent, string content, int size, Color col, TextAnchor align)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>(); t.text = content; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size; t.color = col; t.alignment = align; return go;
        }

        private static void CreateHUDText(Transform parent, string name, string content) { var go = CreateTextGo(name, parent, content, 20, Color.white, TextAnchor.MiddleLeft); go.AddComponent<LayoutElement>().preferredWidth = 120; }
        private static GameObject CreateHUDButton(Transform parent, string name, string label)
        {
            GameObject btnGo = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(parent, false); btnGo.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 40);
            btnGo.GetComponent<Image>().sprite = Resources.Load<Sprite>("Sprites/UI/mm_button_normal");
            var txt = CreateTextGo("Text", btnGo.transform, label, 18, Color.white, TextAnchor.MiddleCenter); SetFullStretch(txt.GetComponent<RectTransform>());
            return btnGo;
        }

        private static void SetupCanvasScaler(Canvas canvas) { var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f; }
        private static void EnsureEventSystem() { if (Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null) new GameObject("EventSystem", typeof(UnityEngine.EventSystems.EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule)); }
    }
}
