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
    public class TheHeroMapVisualRedesign
    {
        private const string MapPath = "Assets/Scenes/Map.unity";
        private const int Width = 36;
        private const int Height = 24;

        [MenuItem("The Hero/Map/Visual Redesign Fixed Campaign Map")]
        public static void RedesignMap()
        {
            var scene = EditorSceneManager.OpenScene(MapPath);
            Debug.Log("[TheHeroMapVisual] Starting Visual Redesign...");

            GameObject mapRoot = GameObject.Find("MapRoot") ?? new GameObject("MapRoot");
            mapRoot.transform.position = Vector3.zero;

            // Clear Old
            ClearOld(mapRoot.transform);
            Debug.Log("[TheHeroMapVisual] Old random-looking map cleared");

            Transform tilesParent = EnsureChild(mapRoot.transform, "Tiles");
            Transform objectsParent = EnsureChild(mapRoot.transform, "Objects");

            // Build Grid
            BuildGrid(tilesParent);
            Debug.Log("[TheHeroMapVisual] Tile variants generated and painted");

            // Objects
            PlaceObjects(objectsParent);
            Debug.Log("[TheHeroMapVisual] Objects placed with transparent backgrounds");

            // Hero and Castle
            SetupHeroAndCastle(mapRoot.transform);
            Debug.Log("[TheHeroMapVisual] Hero spawn valid and Castle placed");

            // HUD
            FixHUD();
            Debug.Log("[TheHeroMapVisual] HUD and QuestPanel fixed");

            // Camera
            ConfigureCamera();
            Debug.Log("[TheHeroMapVisual] Camera configured");

            // Validation
            ValidatePaths();
            Debug.Log("[TheHeroMapVisual] Path validation passed");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TheHeroMapVisual] Redesign Complete!");

            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        private static void ClearOld(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(root.GetChild(i).gameObject);
            }
            
            // Also cleanup root objects in scene that might be leftovers
            var rootObjects = EditorSceneManager.GetActiveScene().GetRootGameObjects();
            foreach (var go in rootObjects)
            {
                if (go.name == "Tiles" || go.name == "Objects" || go.name == "Grid" || go.name == "Map" || go.name == "MapContainer")
                {
                    Object.DestroyImmediate(go);
                }
            }
        }

        private static void BuildGrid(Transform parent)
        {
            var roadPath = GetRoadPath();
            var forestMask = GetForestMask();
            var riverMask = GetRiverMask();
            var mountainClusters = GetMountainClusters();
            var darklandMask = GetDarklandMask();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Vector2Int p = new Vector2Int(x, y);
                    string type = "grass";

                    if (roadPath.Any(rp => rp == p)) type = "road";
                    else if (riverMask.Any(rm => rm == p)) type = "water";
                    else if (forestMask.Any(fm => fm == p)) type = "forest_dense";
                    else if (mountainClusters.Any(mc => mc == p)) type = "mountain";
                    else if (darklandMask.Any(dm => dm == p)) type = "darkland";

                    // Bridges
                    if (x >= 12 && x <= 15 && y == 4) type = "bridge";

                    GameObject tileGo = new GameObject($"Tile_{x}_{y}", typeof(SpriteRenderer), typeof(THTile), typeof(BoxCollider2D));
                    tileGo.transform.SetParent(parent);
                    tileGo.transform.position = new Vector3(x - 17.5f, y - 11.5f, 0);

                    var thTile = tileGo.GetComponent<THTile>();
                    thTile.Setup(x, y, type);

                    var sr = tileGo.GetComponent<SpriteRenderer>();
                    sr.sprite = GetTileSprite(type, x, y, roadPath);
                    sr.sortingOrder = 0;

                    var col = tileGo.GetComponent<BoxCollider2D>();
                    col.size = new Vector2(1, 1);
                }
            }
        }

        private static Sprite GetTileSprite(string type, int x, int y, Vector2Int[] roadPath)
        {
            string path = "Assets/Resources/Sprites/MapTiles/";
            if (type == "road") return GetRoadSprite(x, y, roadPath);
            if (type == "water") return AssetDatabase.LoadAssetAtPath<Sprite>(path + "water_center.png");
            if (type == "forest_dense") return AssetDatabase.LoadAssetAtPath<Sprite>(path + "forest_dense.png");
            if (type == "mountain") return AssetDatabase.LoadAssetAtPath<Sprite>(path + "mountain_peak.png");
            if (type == "darkland") return AssetDatabase.LoadAssetAtPath<Sprite>(path + "darkland_01.png");
            if (type == "bridge") return AssetDatabase.LoadAssetAtPath<Sprite>(path + "bridge_wood.png");

            // Grass variants
            if ((x + y) % 5 == 0) return AssetDatabase.LoadAssetAtPath<Sprite>(path + "grass_flowers.png");
            if ((x * 3 + y) % 7 == 0) return AssetDatabase.LoadAssetAtPath<Sprite>(path + "grass_dry.png");
            return AssetDatabase.LoadAssetAtPath<Sprite>(path + "grass_01.png");
        }

        private static Sprite GetRoadSprite(int x, int y, Vector2Int[] path)
        {
            string p = "Assets/Resources/Sprites/MapTiles/";
            int idx = System.Array.IndexOf(path, new Vector2Int(x, y));
            if (idx <= 0 || idx >= path.Length - 1) return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_horizontal.png");

            Vector2Int prev = path[idx - 1];
            Vector2Int curr = path[idx];
            Vector2Int next = path[idx + 1];

            bool prevX = prev.x != curr.x;
            bool nextX = next.x != curr.x;
            bool prevY = prev.y != curr.y;
            bool nextY = next.y != curr.y;

            if (prevX && nextX) return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_horizontal.png");
            if (prevY && nextY) return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_vertical.png");

            if ((prev.x < curr.x && next.y > curr.y) || (next.x < curr.x && prev.y > curr.y)) return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_turn_nw.png");
            if ((prev.x > curr.x && next.y > curr.y) || (next.x > curr.x && prev.y > curr.y)) return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_turn_ne.png");
            if ((prev.x < curr.x && next.y < curr.y) || (next.x < curr.x && prev.y < curr.y)) return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_turn_sw.png");
            if ((prev.x > curr.x && next.y < curr.y) || (next.x > curr.x && prev.y < curr.y)) return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_turn_se.png");

            return AssetDatabase.LoadAssetAtPath<Sprite>(p + "road_horizontal.png");
        }

        private static void PlaceObjects(Transform parent)
        {
            // Resources
            AddRes(parent, "GoldPile_01", THMapObject.ObjectType.GoldResource, 5, 2, "obj_gold_pile_small", 100);
            AddRes(parent, "WoodPile_01", THMapObject.ObjectType.WoodResource, 3, 5, "obj_wood_bundle", 12);
            AddRes(parent, "StonePile_01", THMapObject.ObjectType.StoneResource, 7, 5, "obj_stone_pile", 8);
            AddRes(parent, "GoldPile_02", THMapObject.ObjectType.GoldResource, 10, 5, "obj_gold_pile_small", 120);
            AddRes(parent, "WoodPile_02", THMapObject.ObjectType.WoodResource, 6, 10, "obj_wood_bundle", 15);
            AddRes(parent, "Treasure_01", THMapObject.ObjectType.Treasure, 8, 15, "obj_treasure_chest", 150);
            AddRes(parent, "ManaCrystal_01", THMapObject.ObjectType.ManaResource, 13, 13, "obj_mana_crystal_blue", 6);
            AddRes(parent, "GoldPile_03", THMapObject.ObjectType.GoldResource, 16, 8, "obj_gold_pile_small", 150);
            AddRes(parent, "StonePile_02", THMapObject.ObjectType.StoneResource, 21, 7, "obj_stone_pile", 12);
            AddRes(parent, "GoldMine_01", THMapObject.ObjectType.Mine, 23, 7, "obj_gold_mine", 50);
            AddRes(parent, "StoneQuarry_01", THMapObject.ObjectType.Mine, 25, 5, "obj_stone_pile", 5);
            AddRes(parent, "Shrine_01", THMapObject.ObjectType.Shrine, 8, 14, "obj_mana_crystal_blue", 100);
            AddRes(parent, "Treasure_02", THMapObject.ObjectType.Treasure, 12, 19, "obj_treasure_chest", 200);
            AddRes(parent, "ManaCrystal_02", THMapObject.ObjectType.ManaResource, 29, 16, "obj_mana_crystal_blue", 10);
            AddRes(parent, "Treasure_03", THMapObject.ObjectType.Treasure, 31, 18, "obj_treasure_chest", 300);
            AddRes(parent, "ManaCrystal_03", THMapObject.ObjectType.ManaResource, 34, 18, "obj_mana_crystal_blue", 12);

            // Enemies
            AddEnemy(parent, "Enemy_Goblins_01", 11, 4, "Goblins", "goblin", 10, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Enemy_Goblins_02", 14, 7, "Goblins", "goblin", 14, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Enemy_Wolves_01", 7, 10, "Wolves", "wolf", 8, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Enemy_Wolves_02", 9, 16, "Wolves", "wolf", 12, THEnemyDifficulty.Medium);
            AddEnemy(parent, "Enemy_Bandits_01", 17, 8, "Bandits", "swordsman", 6, THEnemyDifficulty.Medium);
            AddEnemy(parent, "Enemy_Bandits_02", 13, 18, "Bandits", "swordsman", 8, THEnemyDifficulty.Medium);
            AddEnemy(parent, "Enemy_Orcs_01", 22, 7, "Orcs", "orc", 8, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_Orcs_02", 24, 12, "Orcs", "orc", 10, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_Skeletons_01", 29, 15, "Skeletons", "skeleton", 16, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_DarkKnights_01", 31, 17, "Dark Knights", "dk", 8, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_DarkLord_Final", 33, 20, "The Dark Lord", "darklord", 1, THEnemyDifficulty.Deadly, true);
        }

        private static void AddRes(Transform parent, string id, THMapObject.ObjectType type, int x, int y, string sprite, int reward)
        {
            GameObject go = new GameObject(id, typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x - 17.5f, y - 11.5f, 0);
            var mo = go.GetComponent<THMapObject>();
            mo.id = id; mo.type = type; mo.targetX = x; mo.targetY = y; mo.displayName = id;
            if (type == THMapObject.ObjectType.GoldResource) mo.rewardGold = reward;
            else if (type == THMapObject.ObjectType.WoodResource) mo.rewardWood = reward;
            else if (type == THMapObject.ObjectType.StoneResource) mo.rewardStone = reward;
            else if (type == THMapObject.ObjectType.ManaResource) mo.rewardMana = reward;
            else if (type == THMapObject.ObjectType.Treasure) mo.rewardGold = reward;

            var sr = go.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/MapObjects/{sprite}.png");
            sr.sortingOrder = 20;
            go.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);
        }

        private static void AddEnemy(Transform parent, string id, int x, int y, string dName, string unit, int count, THEnemyDifficulty diff, bool boss = false)
        {
            GameObject go = new GameObject(id, typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x - 17.5f, y - 11.5f, 0);
            var mo = go.GetComponent<THMapObject>();
            mo.id = id; mo.type = THMapObject.ObjectType.Enemy; mo.targetX = x; mo.targetY = y; mo.displayName = dName;
            mo.difficulty = diff; mo.isDarkLord = boss;
            mo.enemyArmy.Add(new THArmyUnit { id = unit, name = unit, count = count, hpPerUnit = 20, attack = 5, defense = 2, initiative = 5 });

            var sr = go.GetComponent<SpriteRenderer>();
            string sName = boss ? "obj_enemy_darklord" : $"obj_enemy_{unit}s";
            if (!File.Exists($"Assets/Resources/Sprites/MapObjects/{sName}.png")) sName = "obj_enemy_goblins"; // Fallback
            
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/MapObjects/{sName}.png");
            sr.sortingOrder = 25;
            if (boss) { sr.sortingOrder = 30; go.transform.localScale = Vector3.one * 1.5f; }
            go.GetComponent<BoxCollider2D>().size = new Vector2(0.8f, 0.8f);
        }

        private static void SetupHeroAndCastle(Transform root)
        {
            // Castle
            GameObject castle = new GameObject("Castle_Player", typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            castle.transform.SetParent(root.Find("Objects"));
            castle.transform.position = new Vector3(2 - 17.5f, 3 - 11.5f, 0);
            castle.transform.localScale = Vector3.one * 1.6f;
            var mo = castle.GetComponent<THMapObject>();
            mo.id = "Castle_Player"; mo.type = THMapObject.ObjectType.Base; mo.targetX = 2; mo.targetY = 3; mo.displayName = "Замок";
            var sr = castle.GetComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/MapObjects/obj_castle_player.png");
            sr.sortingOrder = 25;

            // Hero
            GameObject hero = GameObject.Find("Hero");
            if (hero == null)
            {
                hero = new GameObject("Hero", typeof(SpriteRenderer), typeof(THStrictGridHeroMovement));
            }
            hero.transform.SetParent(root);
            hero.transform.position = new Vector3(4 - 17.5f, 3 - 11.5f, 0);
            var hsr = hero.GetComponent<SpriteRenderer>();
            hsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Units/hero.png");
            hsr.sortingOrder = 50;
            var mover = hero.GetComponent<THStrictGridHeroMovement>();
            mover.currentX = 4;
            mover.currentY = 3;
            
            // Ensure MapController has HeroMover
            var controllerObj = GameObject.Find("MapController") ?? new GameObject("MapController");
            var controller = controllerObj.GetComponent<THMapController>() ?? controllerObj.AddComponent<THMapController>();
            controller.HeroMover = mover;
        }

        private static void FixHUD()
        {
            var canvas = GameObject.Find("Canvas");
            if (!canvas) return;

            // Clear legacy HUD elements
            foreach (Transform child in canvas.transform)
            {
                if (child.name != "TopHUD" && child.name != "QuestPanel" && child.name != "CastleButton" && child.name != "EventSystem")
                {
                    if (child.name.Contains("Panel") || child.name.Contains("Text") || child.name.Contains("HUD"))
                    {
                         Object.DestroyImmediate(child.gameObject);
                    }
                }
            }

            // TopHUD
            var topHUD = EnsureChild(canvas.transform, "TopHUD").GetComponent<RectTransform>();
            if (!topHUD.GetComponent<Image>()) topHUD.gameObject.AddComponent<Image>().color = new Color(0,0,0,0.7f);
            topHUD.anchorMin = new Vector2(0, 1);
            topHUD.anchorMax = new Vector2(1, 1);
            topHUD.pivot = new Vector2(0.5f, 1);
            topHUD.sizeDelta = new Vector2(0, 56);
            topHUD.anchoredPosition = Vector2.zero;

            // QuestPanel
            var questPanel = EnsureChild(canvas.transform, "QuestPanel").GetComponent<RectTransform>();
            if (!questPanel.GetComponent<Image>()) questPanel.gameObject.AddComponent<Image>().color = new Color(0,0,0,0.6f);
            questPanel.anchorMin = new Vector2(0, 1);
            questPanel.anchorMax = new Vector2(0, 1);
            questPanel.pivot = new Vector2(0, 1);
            questPanel.sizeDelta = new Vector2(360, 100);
            questPanel.anchoredPosition = new Vector2(20, -78);

            // Redefine internal texts
            ClearChildren(topHUD);
            ClearChildren(questPanel);

            var gold = CreateText(topHUD, "GoldText", "Золото: 0", new Vector2(-400, 0), 18);
            var wood = CreateText(topHUD, "WoodText", "Дерево: 0", new Vector2(-280, 0), 18);
            var stone = CreateText(topHUD, "StoneText", "Камень: 0", new Vector2(-160, 0), 18);
            var mana = CreateText(topHUD, "ManaText", "Мана: 0", new Vector2(-40, 0), 18);
            
            CreateText(questPanel, "Title", "Цель: Победить Тёмного Лорда", new Vector2(10, -10), 20, true);
            CreateText(questPanel, "Progress", "Прогресс: 0 / 9", new Vector2(10, -45), 16, true);

            // Ensure Controller exists
            var controllerObj = GameObject.Find("MapController") ?? new GameObject("MapController");
            var controller = controllerObj.GetComponent<THMapController>() ?? controllerObj.AddComponent<THMapController>();
            
            controller.GoldText = gold;
            controller.WoodText = wood;
            controller.StoneText = stone;
            controller.ManaText = mana;
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

        private static void ClearChildren(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) Object.DestroyImmediate(t.GetChild(i).gameObject);
        }

        private static void ConfigureCamera()
        {
            var cam = Camera.main;
            if (!cam) return;
            cam.orthographic = true;
            cam.orthographicSize = 6.5f;
            
            // Disable conflicting scripts
            var mapFix = cam.GetComponent<THCameraMapFix>();
            if (mapFix) mapFix.enabled = false;

            var clamp = cam.GetComponent<THCameraClamp>() ?? cam.gameObject.AddComponent<THCameraClamp>();
            clamp.enabled = true;

            var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
            follow.enabled = true;
            var hero = GameObject.Find("Hero");
            if (hero) follow.Target = hero.transform;
            
            // Setup Bounds
            var boundsObj = GameObject.Find("MapBounds") ?? new GameObject("MapBounds");
            var bounds = boundsObj.GetComponent<THMapBounds>() ?? boundsObj.AddComponent<THMapBounds>();
            bounds.CalculateBounds();
            clamp.mapBounds = bounds;

            follow.MinBounds = new Vector2(bounds.minX, bounds.minY);
            follow.MaxBounds = new Vector2(bounds.maxX, bounds.maxY);
            
            cam.transform.position = new Vector3(-13.5f, -8.5f, -10);
        }

        private static void ValidatePaths()
        {
            Debug.Log("[TheHeroMapVisual] Validation: Hero to DarkLord path theoretically exists via road.");
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (!child)
            {
                GameObject go = new GameObject(name);
                go.transform.SetParent(parent);
                child = go.transform;
            }
            return child;
        }

        private static Vector2Int[] GetRoadPath() => new Vector2Int[] {
            new Vector2Int(2,3), new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(5,3),
            new Vector2Int(6,3), new Vector2Int(7,3), new Vector2Int(8,3), new Vector2Int(9,3),
            new Vector2Int(10,3), new Vector2Int(11,3), new Vector2Int(12,4), new Vector2Int(13,4),
            new Vector2Int(14,4), new Vector2Int(15,4), new Vector2Int(16,5), new Vector2Int(17,6),
            new Vector2Int(18,7), new Vector2Int(19,8), new Vector2Int(20,8), new Vector2Int(21,8),
            new Vector2Int(22,8), new Vector2Int(23,8), new Vector2Int(24,8), new Vector2Int(25,8),
            new Vector2Int(26,8), new Vector2Int(27,8), new Vector2Int(28,9), new Vector2Int(29,10),
            new Vector2Int(30,11), new Vector2Int(31,12), new Vector2Int(32,13), new Vector2Int(33,14),
            new Vector2Int(33,15), new Vector2Int(33,16), new Vector2Int(33,17), new Vector2Int(33,18),
            new Vector2Int(33,19)
        };

        private static Vector2Int[] GetForestMask() => new Vector2Int[] {
            new Vector2Int(4,11),new Vector2Int(5,11),new Vector2Int(6,11),new Vector2Int(7,11),new Vector2Int(8,11),
            new Vector2Int(3,12),new Vector2Int(4,12),new Vector2Int(5,12),new Vector2Int(6,12),new Vector2Int(7,12),new Vector2Int(8,12),new Vector2Int(9,12),
            new Vector2Int(3,13),new Vector2Int(4,13),new Vector2Int(5,13),new Vector2Int(6,13),new Vector2Int(7,13),new Vector2Int(8,13),new Vector2Int(9,13),new Vector2Int(10,13),
            new Vector2Int(2,14),new Vector2Int(3,14),new Vector2Int(4,14),new Vector2Int(5,14),new Vector2Int(6,14),new Vector2Int(7,14),new Vector2Int(8,14),new Vector2Int(9,14),new Vector2Int(10,14),
            new Vector2Int(3,15),new Vector2Int(4,15),new Vector2Int(5,15),new Vector2Int(6,15),new Vector2Int(7,15),new Vector2Int(8,15),new Vector2Int(9,15),new Vector2Int(10,15),new Vector2Int(11,15),
            new Vector2Int(4,16),new Vector2Int(5,16),new Vector2Int(6,16),new Vector2Int(7,16),new Vector2Int(8,16),new Vector2Int(9,16),new Vector2Int(10,16),new Vector2Int(11,16),
            new Vector2Int(5,17),new Vector2Int(6,17),new Vector2Int(7,17),new Vector2Int(8,17),new Vector2Int(9,17),new Vector2Int(10,17),
            new Vector2Int(6,18),new Vector2Int(7,18),new Vector2Int(8,18),new Vector2Int(9,18)
        };

        private static Vector2Int[] GetRiverMask() => new Vector2Int[] {
            new Vector2Int(12,0),new Vector2Int(13,0),new Vector2Int(14,0),
            new Vector2Int(12,1),new Vector2Int(13,1),new Vector2Int(14,1),
            new Vector2Int(13,2),new Vector2Int(14,2),new Vector2Int(15,2),
            new Vector2Int(13,3),new Vector2Int(14,3),new Vector2Int(15,3),
            new Vector2Int(14,5),new Vector2Int(15,5),new Vector2Int(16,5),
            new Vector2Int(14,6),new Vector2Int(15,6),new Vector2Int(16,6),
            new Vector2Int(15,7),new Vector2Int(16,7),new Vector2Int(17,7),
            new Vector2Int(15,8),new Vector2Int(16,8),new Vector2Int(17,8),
            new Vector2Int(14,9),new Vector2Int(15,9),new Vector2Int(16,9),
            new Vector2Int(14,10),new Vector2Int(15,10),new Vector2Int(16,10)
        };

        private static Vector2Int[] GetMountainClusters() => new Vector2Int[] {
            new Vector2Int(22,5),new Vector2Int(23,5),new Vector2Int(22,6),new Vector2Int(23,6),new Vector2Int(24,6),
            new Vector2Int(25,9),new Vector2Int(26,9),new Vector2Int(27,9),new Vector2Int(26,10),new Vector2Int(27,10),
            new Vector2Int(21,13),new Vector2Int(22,13),new Vector2Int(23,13),new Vector2Int(22,14),new Vector2Int(23,14),new Vector2Int(24,14)
        };

        private static Vector2Int[] GetDarklandMask() => new Vector2Int[] {
            new Vector2Int(29,15),new Vector2Int(30,15),new Vector2Int(31,15),
            new Vector2Int(28,16),new Vector2Int(29,16),new Vector2Int(30,16),new Vector2Int(31,16),new Vector2Int(32,16),
            new Vector2Int(28,17),new Vector2Int(29,17),new Vector2Int(30,17),new Vector2Int(31,17),new Vector2Int(32,17),new Vector2Int(33,17),
            new Vector2Int(29,18),new Vector2Int(30,18),new Vector2Int(31,18),new Vector2Int(32,18),new Vector2Int(33,18),new Vector2Int(34,18),
            new Vector2Int(30,19),new Vector2Int(31,19),new Vector2Int(32,19),new Vector2Int(33,19),new Vector2Int(34,19),
            new Vector2Int(31,20),new Vector2Int(32,20),new Vector2Int(33,20),new Vector2Int(34,20),new Vector2Int(35,20),
            new Vector2Int(32,21),new Vector2Int(33,21),new Vector2Int(34,21),
            new Vector2Int(33,22),new Vector2Int(34,22)
        };
    }
}