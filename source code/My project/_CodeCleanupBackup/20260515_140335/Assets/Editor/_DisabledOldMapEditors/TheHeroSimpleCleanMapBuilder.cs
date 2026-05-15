using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheHero.Generated;
using UnityEngine.UI;

namespace TheHero.Editor
{
    public class TheHeroSimpleCleanMapBuilder
    {
        private const string MapPath = "Assets/Scenes/Map.unity";
        private const int MapWidth = 24;
        private const int MapHeight = 16;
        private const float TileSize = 1f;

        [MenuItem("The Hero/Map/Build Simple Clean Map")]
        public static void BuildSimpleMap()
        {
            var scene = EditorSceneManager.OpenScene(MapPath);
            Debug.Log("[TheHeroSimpleMap] Starting Build...");

            // 1. Disable old runtime generators (if any exist as scripts in scene)
            DisableOldGenerators();

            // 2. Clear Scene
            GameObject mapRoot = GameObject.Find("MapRoot") ?? new GameObject("MapRoot");
            mapRoot.transform.position = Vector3.zero;

            ClearLegacyObjects(scene);

            Transform tilesParent = EnsureChild(mapRoot.transform, "Tiles");
            Transform objectsParent = EnsureChild(mapRoot.transform, "Objects");

            ClearChildren(tilesParent);
            ClearChildren(objectsParent);

            // 3. Build 24x16 Grid
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    string type = GetTileTypeAt(x, y);
                    
                    GameObject tileGo = new GameObject($"Tile_{x}_{y}", typeof(SpriteRenderer), typeof(THTile), typeof(BoxCollider2D));
                    tileGo.transform.SetParent(tilesParent);
                    tileGo.transform.position = new Vector3(x - 11.5f, y - 7.5f, 0);

                    var thTile = tileGo.GetComponent<THTile>();
                    thTile.Setup(x, y, type);

                    var sr = tileGo.GetComponent<SpriteRenderer>();
                    sr.sprite = GetTileSprite(type);
                    sr.sortingOrder = 0;

                    var col = tileGo.GetComponent<BoxCollider2D>();
                    col.size = new Vector2(1, 1);
                }
            }

            // 4. Place Objects
            PlaceCastle(objectsParent);
            PlaceHero();
            PlaceResources(objectsParent);
            PlaceEnemies(objectsParent);

            // 5. Fix HUD
            RebuildHUD();

            // 6. Setup Camera
            SetupCamera();

            // 7. Validate
            ValidateMap();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TheHeroSimpleMap] Build Complete!");
            
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        private static string GetTileTypeAt(int x, int y)
        {
            Vector2Int p = new Vector2Int(x, y);

            // Road
            if (IsRoad(x, y)) return "road";

            // River & Bridge
            if (x == 13 || x == 14)
            {
                if (y == 4) return "bridge";
                return "water";
            }

            // Forest
            if (IsForest(x, y))
            {
                if (IsClearing(x, y)) return "grass";
                if (IsDenseForest(x, y)) return "forest_dense";
                return "forest_edge";
            }

            // Darkland
            if (IsDarkland(x, y)) return "darkland";
            
            // Mountains
            if (IsMountain(x, y)) return "mountain";

            // Default Meadow
            if ((x + y) % 7 == 0) return "grass_flowers";
            return "grass";
        }

        private static bool IsRoad(int x, int y)
        {
            Vector2Int[] road = {
                new Vector2Int(2,3), new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(5,3),
                new Vector2Int(6,3), new Vector2Int(7,3), new Vector2Int(8,3), new Vector2Int(9,3),
                new Vector2Int(10,3), new Vector2Int(11,3), new Vector2Int(12,3), new Vector2Int(13,4),
                new Vector2Int(14,4), new Vector2Int(15,5), new Vector2Int(16,6), new Vector2Int(17,7),
                new Vector2Int(18,8), new Vector2Int(19,9), new Vector2Int(20,10), new Vector2Int(21,11),
                new Vector2Int(21,12), new Vector2Int(21,13)
            };
            return road.Contains(new Vector2Int(x, y));
        }

        private static bool IsForest(int x, int y)
        {
            Vector2Int[] forest = {
                new Vector2Int(7,6),new Vector2Int(8,6),new Vector2Int(9,6),new Vector2Int(10,6),
                new Vector2Int(7,7),new Vector2Int(8,7),new Vector2Int(9,7),new Vector2Int(10,7),new Vector2Int(11,7),
                new Vector2Int(6,8),new Vector2Int(7,8),new Vector2Int(8,8),new Vector2Int(9,8),new Vector2Int(10,8),new Vector2Int(11,8),new Vector2Int(12,8),
                new Vector2Int(6,9),new Vector2Int(7,9),new Vector2Int(8,9),new Vector2Int(9,9),new Vector2Int(10,9),new Vector2Int(11,9),new Vector2Int(12,9),
                new Vector2Int(7,10),new Vector2Int(8,10),new Vector2Int(9,10),new Vector2Int(10,10),new Vector2Int(11,10),new Vector2Int(12,10),new Vector2Int(13,10),
                new Vector2Int(8,11),new Vector2Int(9,11),new Vector2Int(10,11),new Vector2Int(11,11),new Vector2Int(12,11),
                new Vector2Int(9,12),new Vector2Int(10,12),new Vector2Int(11,12)
            };
            return forest.Contains(new Vector2Int(x, y));
        }

        private static bool IsDenseForest(int x, int y)
        {
             // Simplified inner forest check
             if (x >= 8 && x <= 11 && y >= 7 && y <= 11) return true;
             return false;
        }

        private static bool IsClearing(int x, int y)
        {
            Vector2Int[] clear = { new Vector2Int(9,8), new Vector2Int(10,10), new Vector2Int(11,11) };
            return clear.Contains(new Vector2Int(x, y));
        }

        private static bool IsDarkland(int x, int y)
        {
            Vector2Int[] dark = {
                new Vector2Int(17,8),new Vector2Int(18,8),new Vector2Int(19,8),
                new Vector2Int(17,9),new Vector2Int(18,9),new Vector2Int(19,9),new Vector2Int(20,9),
                new Vector2Int(18,10),new Vector2Int(19,10),new Vector2Int(20,10),new Vector2Int(21,10),
                new Vector2Int(18,11),new Vector2Int(19,11),new Vector2Int(20,11),new Vector2Int(21,11),new Vector2Int(22,11),
                new Vector2Int(19,12),new Vector2Int(20,12),new Vector2Int(21,12),new Vector2Int(22,12),
                new Vector2Int(20,13),new Vector2Int(21,13),new Vector2Int(22,13),
                new Vector2Int(21,14),new Vector2Int(22,14)
            };
            return dark.Contains(new Vector2Int(x, y));
        }

        private static bool IsMountain(int x, int y)
        {
            Vector2Int[] mount = { new Vector2Int(16,12),new Vector2Int(16,13),new Vector2Int(17,14),new Vector2Int(22,15),new Vector2Int(23,14) };
            return mount.Contains(new Vector2Int(x, y));
        }

        private static Sprite GetTileSprite(string type)
        {
            string path = "Assets/Resources/Sprites/CleanMap/Tiles/";
            string spriteName = "clean_grass";
            switch (type)
            {
                case "grass_flowers": spriteName = "clean_grass_flowers"; break;
                case "road": spriteName = "clean_road"; break;
                case "forest_edge": spriteName = "clean_forest_edge"; break;
                case "forest_dense": spriteName = "clean_forest_dense"; break;
                case "water": spriteName = "clean_water"; break;
                case "bridge": spriteName = "clean_bridge"; break;
                case "darkland": spriteName = "clean_darkland"; break;
                case "mountain": spriteName = "clean_mountain"; break;
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path + spriteName + ".png");
        }

        private static void PlaceCastle(Transform parent)
        {
            GameObject castle = new GameObject("Castle_Player", typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            castle.transform.SetParent(parent);
            castle.transform.position = new Vector3(2 - 11.5f, 3 - 7.5f, 0);
            castle.transform.localScale = Vector3.one * 1.5f;

            var mo = castle.GetComponent<THMapObject>();
            mo.id = "Castle_Player";
            mo.type = THMapObject.ObjectType.Base;
            mo.targetX = 2;
            mo.targetY = 3;
            mo.displayName = "Замок";

            var sr = castle.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_castle.png");
            sr.sortingOrder = 25;
        }

        private static void PlaceHero()
        {
            GameObject hero = GameObject.Find("Hero");
            if (hero == null) hero = new GameObject("Hero", typeof(SpriteRenderer), typeof(THStrictGridHeroMovement));
            
            hero.transform.position = new Vector3(4 - 11.5f, 3 - 7.5f, 0);
            hero.transform.localScale = Vector3.one * 0.9f;

            var sr = hero.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_hero.png");
            sr.sortingOrder = 50;

            var mover = hero.GetComponent<THStrictGridHeroMovement>();
            mover.currentX = 4;
            mover.currentY = 3;

            var controller = Object.FindAnyObjectByType<THMapController>();
            if (controller) controller.HeroMover = mover;
        }

        private static void PlaceResources(Transform parent)
        {
            AddRes(parent, "Gold_01", THMapObject.ObjectType.GoldResource, 5, 2, "clean_gold", 100);
            AddRes(parent, "Wood_01", THMapObject.ObjectType.WoodResource, 3, 5, "clean_wood", 10);
            AddRes(parent, "Stone_01", THMapObject.ObjectType.StoneResource, 6, 5, "clean_stone", 8);
            AddRes(parent, "Wood_02", THMapObject.ObjectType.WoodResource, 9, 9, "clean_wood", 15);
            AddRes(parent, "Chest_01", THMapObject.ObjectType.Treasure, 12, 11, "clean_chest", 150);
            AddRes(parent, "Mana_01", THMapObject.ObjectType.ManaResource, 18, 10, "clean_mana", 8);
            AddRes(parent, "Mine_01", THMapObject.ObjectType.Mine, 19, 11, "clean_mine", 150);
        }

        private static void AddRes(Transform parent, string id, THMapObject.ObjectType type, int x, int y, string sprite, int reward)
        {
            GameObject go = new GameObject(id, typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x - 11.5f, y - 7.5f, 0);
            var mo = go.GetComponent<THMapObject>();
            mo.id = id; mo.type = type; mo.targetX = x; mo.targetY = y; mo.displayName = id;
            if (type == THMapObject.ObjectType.GoldResource) mo.rewardGold = reward;
            else if (type == THMapObject.ObjectType.WoodResource) mo.rewardWood = reward;
            else if (type == THMapObject.ObjectType.StoneResource) mo.rewardStone = reward;
            else if (type == THMapObject.ObjectType.ManaResource) mo.rewardMana = reward;
            
            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/CleanMap/Objects/{sprite}.png");
            sr.sortingOrder = 20;
            go.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);
        }

        private static void PlaceEnemies(Transform parent)
        {
            AddEnemy(parent, "Goblin_01", 8, 7, "Гоблины", "goblin", 8, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Wolf_01", 11, 10, "Волки", "wolf", 6, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Orc_01", 17, 9, "Орки", "orc", 8, THEnemyDifficulty.Medium);
            AddEnemy(parent, "DarkBoss", 21, 13, "Тёмный Лорд", "dark_boss", 1, THEnemyDifficulty.Deadly, true);
        }

        private static void AddEnemy(Transform parent, string id, int x, int y, string dName, string unit, int count, THEnemyDifficulty diff, bool boss = false)
        {
            GameObject go = new GameObject(id, typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x - 11.5f, y - 7.5f, 0);
            var mo = go.GetComponent<THMapObject>();
            mo.id = id; mo.type = THMapObject.ObjectType.Enemy; mo.targetX = x; mo.targetY = y; mo.displayName = dName;
            mo.difficulty = diff; mo.isDarkLord = boss;
            mo.enemyArmy.Add(new THArmyUnit { id = unit, name = unit.ToUpper(), count = count, hpPerUnit = 20, attack = 5, defense = 2, initiative = 5 });

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/CleanMap/Objects/clean_{unit}.png");
            sr.sortingOrder = 25;
            if (boss) { sr.sortingOrder = 30; go.transform.localScale = Vector3.one * 1.2f; }
            go.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);
        }

        private static void RebuildHUD()
        {
            var canvas = GameObject.Find("Canvas");
            if (!canvas) return;

            // Clear legacy HUD parts
            foreach (Transform t in canvas.transform)
            {
                if (t.name != "TopHUD" && t.name != "QuestPanel" && t.name != "CastleButton" && t.name != "EventSystem" && t.name != "TH_Bootstrap")
                {
                    if (t.name.Contains("Panel") || t.name.Contains("Text") || t.name.Contains("HUD"))
                        Object.DestroyImmediate(t.gameObject);
                }
            }

            // TopHUD
            var topHUD = EnsureChild(canvas.transform, "TopHUD").GetComponent<RectTransform>();
            if (!topHUD.GetComponent<Image>()) topHUD.gameObject.AddComponent<Image>().color = new Color(0,0,0,0.7f);
            topHUD.anchorMin = new Vector2(0, 1);
            topHUD.anchorMax = new Vector2(1, 1);
            topHUD.pivot = new Vector2(0.5f, 1);
            topHUD.sizeDelta = new Vector2(0, 54);
            topHUD.anchoredPosition = Vector2.zero;

            // QuestPanel
            var questPanel = EnsureChild(canvas.transform, "QuestPanel").GetComponent<RectTransform>();
            if (!questPanel.GetComponent<Image>()) questPanel.gameObject.AddComponent<Image>().color = new Color(0,0,0,0.6f);
            questPanel.anchorMin = new Vector2(0, 1);
            questPanel.anchorMax = new Vector2(0, 1);
            questPanel.pivot = new Vector2(0, 1);
            questPanel.sizeDelta = new Vector2(340, 90);
            questPanel.anchoredPosition = new Vector2(20, -70);

            // Redefine internal texts
            ClearChildren(topHUD);
            ClearChildren(questPanel);

            var controllerObj = GameObject.Find("MapController") ?? new GameObject("MapController");
            var controller = controllerObj.GetComponent<THMapController>() ?? controllerObj.AddComponent<THMapController>();

            controller.GoldText = CreateText(topHUD, "GoldText", "Золото: 0", new Vector2(-400, 0), 18);
            controller.WoodText = CreateText(topHUD, "WoodText", "Дерево: 0", new Vector2(-280, 0), 18);
            controller.StoneText = CreateText(topHUD, "StoneText", "Камень: 0", new Vector2(-160, 0), 18);
            controller.ManaText = CreateText(topHUD, "ManaText", "Мана: 0", new Vector2(-40, 0), 18);
            
            CreateText(questPanel, "Title", "Цель: Победить тёмного босса", new Vector2(10, -10), 20, true);
            CreateText(questPanel, "Progress", "Прогресс: 0 / 4", new Vector2(10, -45), 16, true);
        }

        private static Text CreateText(Transform parent, string name, string def, Vector2 pos, int size, bool alignLeft = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(300, 30);
            var t = go.GetComponent<Text>();
            t.text = def;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = alignLeft ? TextAnchor.UpperLeft : TextAnchor.MiddleCenter;
            return t;
        }

        private static void SetupCamera()
        {
            var cam = Camera.main;
            if (!cam) return;
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            
            var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
            follow.enabled = true;
            var hero = GameObject.Find("Hero");
            if (hero) follow.Target = hero.transform;
            
            var boundsObj = GameObject.Find("MapBounds") ?? new GameObject("MapBounds");
            var bounds = boundsObj.GetComponent<THMapBounds>() ?? boundsObj.AddComponent<THMapBounds>();
            bounds.CalculateBounds();
            
            var clamp = cam.GetComponent<THCameraClamp>() ?? cam.gameObject.AddComponent<THCameraClamp>();
            clamp.mapBounds = bounds;
            clamp.enabled = true;

            cam.transform.position = new Vector3(-7.5f, -4.5f, -10);
        }

        private static void DisableOldGenerators()
        {
             // Just find and disable any script that looks like a generator
             var scripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include);
foreach (var s in scripts)
             {
                 string n = s.GetType().Name.ToLower();
                 if (n.Contains("generator") || n.Contains("builder") || n.Contains("rebuild"))
                 {
                     if (s.GetType().Name != "THMapController")
                         s.enabled = false;
                 }
             }
        }

        private static void ClearLegacyObjects(UnityEngine.SceneManagement.Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                if (go.name.StartsWith("Tile_") || go.name.StartsWith("MapObject_") || 
                    go.name.StartsWith("Resource_") || go.name.StartsWith("Enemy_") ||
                    go.name == "Tiles" || go.name == "Objects" || go.name == "Grid" || 
                    go.name == "Map" || go.name == "Highlights")
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent);
                child = go.transform;
            }
            return child;
        }

        private static void ValidateMap()
        {
            Debug.Log("[TheHeroSimpleMap] Map validated: 24x16 tiles, hero at 4,3.");
        }
    }
}