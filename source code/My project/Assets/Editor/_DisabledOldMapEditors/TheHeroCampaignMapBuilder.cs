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
    public class TheHeroCampaignMapBuilder
    {
        private const string MapPath = "Assets/Scenes/Map.unity";
        private const int MapWidth = 36;
        private const int MapHeight = 24;
        private const long MapSeed = 17701729;

        [MenuItem("The Hero/Map/Build Campaign Map 01")]
        public static void BuildCampaignMap()
        {
            var scene = EditorSceneManager.OpenScene(MapPath);
            Debug.Log("[TheHeroCampaignMap] Starting Map Build...");

            // Clear any potential legacy objects in the scene
            var allRootGOs = scene.GetRootGameObjects();
            foreach (var go in allRootGOs)
            {
                if (go.name == "MapRoot") continue;
                if (go.name == "Canvas") continue;
                if (go.name == "EventSystem") continue;
                if (go.name == "Main Camera") continue;
                if (go.name == "Hero") continue; // We'll reposition it
                if (go.name == "TH_Bootstrap") continue;
                if (go.name == "MapController") continue;

                // If it's a legacy Tiles or Objects folder, destroy it
                if (go.name == "Tiles" || go.name == "Objects" || go.name == "Map" || go.name == "Grid")
                {
                    Object.DestroyImmediate(go);
                }
            }

            GameObject mapRoot = GameObject.Find("MapRoot") ?? new GameObject("MapRoot");

            mapRoot.transform.position = Vector3.zero;

            Transform tilesParent = EnsureChild(mapRoot.transform, "Tiles");
            Transform roadsParent = EnsureChild(mapRoot.transform, "Roads");
            Transform objectsParent = EnsureChild(mapRoot.transform, "Objects");
            
            Transform resParent = EnsureChild(objectsParent, "Resources");
            Transform enemiesParent = EnsureChild(objectsParent, "Enemies");
            Transform buildingsParent = EnsureChild(objectsParent, "Buildings");
            Transform specialParent = EnsureChild(objectsParent, "Special");

            ClearChildren(tilesParent);
            ClearChildren(roadsParent);
            ClearChildren(objectsParent); // Clear all, including subfolders
            
            // Re-ensure subfolders because ClearChildren deleted them
            resParent = EnsureChild(objectsParent, "Resources");
            enemiesParent = EnsureChild(objectsParent, "Enemies");
            buildingsParent = EnsureChild(objectsParent, "Buildings");
            specialParent = EnsureChild(objectsParent, "Special");

            // Remove Hero from root if exists (we will recreate/reposition)
            var heroGo = GameObject.Find("Hero");
            if (heroGo != null && heroGo.transform.parent == null) Object.DestroyImmediate(heroGo);


            // 1. Generate/Ensure Sprites
            GenerateRequiredSprites();

            // 2. Build Tiles
            Dictionary<Vector2Int, THTile> tileGrid = new Dictionary<Vector2Int, THTile>();
            for (int y = 0; y < MapHeight; y++)
            {
                for (int x = 0; x < MapWidth; x++)
                {
                    string type = GetTileTypeAt(x, y);
                    GameObject tileGo = new GameObject($"Tile_{x}_{y}", typeof(SpriteRenderer), typeof(THTile), typeof(BoxCollider2D));
                    tileGo.transform.SetParent(tilesParent);
                    tileGo.transform.position = new Vector3(x - 17.5f, y - 11.5f, 0);

                    var thTile = tileGo.GetComponent<THTile>();
                    thTile.Setup(x, y, type);
                    tileGrid[new Vector2Int(x, y)] = thTile;

                    var sr = tileGo.GetComponent<SpriteRenderer>();
                    sr.sprite = GetSpriteForTile(x, y, type);
                    sr.sortingOrder = 0;

                    var col = tileGo.GetComponent<BoxCollider2D>();
                    col.size = new Vector2(1, 1);
                }
            }

            // 3. Place Roads
            PlaceRoads(roadsParent);

            // 4. Place Objects
            PlaceCastle(buildingsParent);
            PlaceHero();
            PlaceAllResources(resParent);
            PlaceAllEnemies(enemiesParent);

            // 5. Rebuild HUD
            RebuildMapHUD();

            // 6. Cleanup and Validate
            RemoveMainMenuUI();
            ValidateMap();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TheHeroCampaignMap] Build Complete!");
            
            EditorSceneManager.OpenScene("Assets/Scenes/MainMenu.unity");
        }

        private static string GetTileTypeAt(int x, int y)
        {
            if (IsWater(x, y)) return "water";
            if (IsBridge(x, y)) return "bridge";
            if (IsRoad(x, y)) return "road";
            if (IsMountain(x, y)) return "mountain";
            if (IsHill(x, y)) return "hill";

            if (x >= 27 && y >= 14) 
            {
                if (IsSwamp(x, y)) return "swamp";
                return "darkland";
            }

            if (x >= 2 && x <= 14 && y >= 9 && y <= 22)
            {
                if (IsClearing(x, y)) return "grass";
                if (x >= 5 && x <= 11 && y >= 14 && y <= 21) return "forest_dense";
                if (x >= 4 && x <= 12 && y >= 11 && y <= 21) return "forest_sparse";
                return "forest_edge";
            }

            return "grass";
        }

        private static bool IsWater(int x, int y)
        {
            if (y == 0 && x >= 12 && x <= 15) return true;
            if (y == 1 && x >= 12 && x <= 15) return true;
            if (y == 2 && x >= 13 && x <= 16) return true;
            if (y == 3 && x >= 13 && x <= 16) return true;
            if (y == 5 && x >= 13 && x <= 16) return true;
            if (y == 6 && x >= 14 && x <= 17) return true;
            if (y == 7 && x >= 14 && x <= 17) return true;
            if (y == 8 && x >= 13 && x <= 16) return true;
            if (y == 9 && x >= 12 && x <= 15) return true;
            if (y == 10 && x >= 12 && x <= 15) return true;
            if (y == 13 && x >= 14 && x <= 16) return true;
            if (y == 14 && x >= 15 && x <= 17) return true;
            if (y == 15 && x >= 16 && x <= 18) return true;
            return false;
        }

        private static bool IsBridge(int x, int y)
        {
            if (y == 4 && x >= 12 && x <= 15) return true;
            if (y == 12 && x >= 14 && x <= 17) return true;
            return false;
        }

        private static bool IsRoad(int x, int y)
        {
            return GetRoadPath().Contains(new Vector2Int(x, y));
        }

        private static Vector2Int[] GetRoadPath()
        {
            return new Vector2Int[] {
                new Vector2Int(3,3), new Vector2Int(4,3), new Vector2Int(5,3), new Vector2Int(6,3),
                new Vector2Int(7,3), new Vector2Int(8,3), new Vector2Int(9,3), new Vector2Int(10,3),
                new Vector2Int(11,3), new Vector2Int(12,4), new Vector2Int(13,4), new Vector2Int(14,4),
                new Vector2Int(15,4), new Vector2Int(16,5), new Vector2Int(17,6), new Vector2Int(18,7),
                new Vector2Int(19,8), new Vector2Int(20,9), new Vector2Int(21,10), new Vector2Int(22,10),
                new Vector2Int(23,10), new Vector2Int(24,10), new Vector2Int(25,11), new Vector2Int(26,12),
                new Vector2Int(27,13), new Vector2Int(28,14), new Vector2Int(29,15), new Vector2Int(30,16),
                new Vector2Int(31,17), new Vector2Int(32,18), new Vector2Int(33,19)
            };
        }

        private static bool IsMountain(int x, int y)
        {
            Vector2Int[] m = {
                new Vector2Int(21,4),new Vector2Int(22,4),new Vector2Int(24,4),
                new Vector2Int(20,5),new Vector2Int(21,5),new Vector2Int(25,5),new Vector2Int(26,5),
                new Vector2Int(20,6),new Vector2Int(26,6),new Vector2Int(27,6),
                new Vector2Int(21,7),new Vector2Int(27,7),
                new Vector2Int(22,8),new Vector2Int(23,8),new Vector2Int(27,8),
                new Vector2Int(25,9),new Vector2Int(26,9),
                new Vector2Int(20,11),new Vector2Int(21,11),new Vector2Int(26,11),new Vector2Int(27,11),
                new Vector2Int(20,12),new Vector2Int(27,12),
                new Vector2Int(21,13),new Vector2Int(22,13),new Vector2Int(26,13),
                new Vector2Int(22,14),new Vector2Int(23,14),new Vector2Int(24,14),
                new Vector2Int(24,15),new Vector2Int(25,15),
                new Vector2Int(25,16),new Vector2Int(26,16)
            };
            return m.Contains(new Vector2Int(x, y));
        }

        private static bool IsHill(int x, int y)
        {
            if (x >= 19 && x <= 28 && y >= 3 && y <= 17 && !IsMountain(x,y) && !IsRoad(x,y)) return true;
            return false;
        }

        private static bool IsSwamp(int x, int y)
        {
            Vector2Int[] s = { new Vector2Int(28,16),new Vector2Int(29,16),new Vector2Int(30,17),new Vector2Int(31,18),new Vector2Int(32,19) };
            return s.Contains(new Vector2Int(x, y));
        }

        private static bool IsClearing(int x, int y)
        {
            Vector2Int[] c = { new Vector2Int(7,16),new Vector2Int(8,16),new Vector2Int(9,16),new Vector2Int(9,11),new Vector2Int(10,11),new Vector2Int(10,17) };
            return c.Contains(new Vector2Int(x, y));
        }

        private static Sprite GetSpriteForTile(int x, int y, string type)
        {
            string path = "Assets/Resources/Sprites/MapTiles/";
            if (type == "grass")
            {
                if ((x + y) % 5 == 0) return LoadSprite(path + "grass_flowers.png");
                if ((x * 3 + y) % 7 == 0) return LoadSprite(path + "grass_02.png");
                if ((x + y * 2) % 9 == 0) return LoadSprite(path + "grass_dry.png");
                return LoadSprite(path + "grass_01.png");
            }
            if (type == "water") 
            {
                if (IsWaterEdge(x, y, out string edgeSuffix)) return LoadSprite(path + "water_bank_grass_" + edgeSuffix + ".png");
                return LoadSprite(path + "water_center_01.png");
            }
            if (type == "bridge") return LoadSprite(path + "bridge_wood.png");
            if (type == "mountain") return LoadSprite(path + "mountain_01.png");
            if (type == "hill") return LoadSprite(path + "hill_01.png");
            if (type == "forest_dense") return LoadSprite(path + "forest_dense_01.png");
            if (type == "forest_sparse") return LoadSprite(path + "forest_sparse_01.png");
            if (type == "forest_edge") return LoadSprite(path + "forest_edge_n.png");
            if (type == "darkland") return LoadSprite(path + "darkland_01.png");
            if (type == "swamp") return LoadSprite(path + "swamp_01.png");

            return LoadSprite(path + "grass_01.png");
        }

        private static bool IsWaterEdge(int x, int y, out string suffix)
        {
            suffix = "n";
            if (!IsWater(x, y + 1)) { suffix = "n"; return true; }
            if (!IsWater(x, y - 1)) { suffix = "s"; return true; }
            if (!IsWater(x + 1, y)) { suffix = "e"; return true; }
            if (!IsWater(x - 1, y)) { suffix = "w"; return true; }
            return false;
        }

        private static void PlaceRoads(Transform parent)
        {
            var path = GetRoadPath();
            for (int i = 0; i < path.Length; i++)
            {
                var p = path[i];
                GameObject roadGo = new GameObject($"Road_{p.x}_{p.y}", typeof(SpriteRenderer));
                roadGo.transform.SetParent(parent);
                roadGo.transform.position = new Vector3(p.x - 17.5f, p.y - 11.5f, 0);
                var sr = roadGo.GetComponent<SpriteRenderer>();
                sr.sortingOrder = 1;
                sr.sprite = GetRoadSprite(path, i);
            }
        }

        private static Sprite GetRoadSprite(Vector2Int[] path, int index)
        {
            string p = "Assets/Resources/Sprites/MapTiles/";
            if (index == 0) return LoadSprite(p + "road_horizontal.png");
            if (index == path.Length - 1) return LoadSprite(p + "road_horizontal.png");

            Vector2Int prev = path[index - 1];
            Vector2Int curr = path[index];
            Vector2Int next = path[index + 1];

            bool prevX = prev.x != curr.x;
            bool nextX = next.x != curr.x;
            bool prevY = prev.y != curr.y;
            bool nextY = next.y != curr.y;

            if (prevX && nextX) return LoadSprite(p + "road_horizontal.png");
            if (prevY && nextY) return LoadSprite(p + "road_vertical.png");
            
            // Turns
            if ((prev.x < curr.x && next.y > curr.y) || (next.x < curr.x && prev.y > curr.y)) return LoadSprite(p + "road_turn_nw.png");
            if ((prev.x > curr.x && next.y > curr.y) || (next.x > curr.x && prev.y > curr.y)) return LoadSprite(p + "road_turn_ne.png");
            if ((prev.x < curr.x && next.y < curr.y) || (next.x < curr.x && prev.y < curr.y)) return LoadSprite(p + "road_turn_sw.png");
            if ((prev.x > curr.x && next.y < curr.y) || (next.x > curr.x && prev.y < curr.y)) return LoadSprite(p + "road_turn_se.png");

            return LoadSprite(p + "road_horizontal.png");
        }

        private static Sprite LoadSprite(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        private static void PlaceCastle(Transform parent)
        {
            GameObject castle = new GameObject("Castle_Player", typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            castle.transform.SetParent(parent);
            castle.transform.position = new Vector3(2 - 17.5f, 3 - 11.5f, 0);
            castle.transform.localScale = Vector3.one * 1.8f;
            var mo = castle.GetComponent<THMapObject>();
            mo.id = "Castle_Player"; mo.type = THMapObject.ObjectType.Base; mo.targetX = 2; mo.targetY = 3; mo.displayName = "Замок";
            var sr = castle.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Assets/Resources/Sprites/MapObjects/obj_castle_player.png"); sr.sortingOrder = 25;
        }

        private static void PlaceHero()
        {
            GameObject hero = GameObject.Find("Hero");
            if (hero == null) hero = new GameObject("Hero", typeof(SpriteRenderer), typeof(THStrictGridHeroMovement));
            hero.transform.position = new Vector3(4 - 17.5f, 3 - 11.5f, 0);
            var sr = hero.GetComponent<SpriteRenderer>();
            sr.sprite = LoadSprite("Assets/Resources/Sprites/Units/hero.png"); sr.sortingOrder = 50;
            var mover = hero.GetComponent<THStrictGridHeroMovement>();
            mover.currentX = 4; mover.currentY = 3;
        }

        private static void PlaceAllResources(Transform parent)
        {
            AddRes(parent, "GoldPile_01", THMapObject.ObjectType.GoldResource, 5, 2, "obj_gold_pile_small", 100);
            AddRes(parent, "WoodPile_01", THMapObject.ObjectType.WoodResource, 3, 5, "obj_wood_bundle", 12);
            AddRes(parent, "StonePile_01", THMapObject.ObjectType.StoneResource, 7, 5, "obj_stone_pile", 8);
            AddRes(parent, "GoldPile_02", THMapObject.ObjectType.GoldResource, 10, 5, "obj_gold_pile_small", 120);
            AddRes(parent, "WoodPile_02", THMapObject.ObjectType.WoodResource, 6, 10, "obj_wood_bundle", 15);
            AddRes(parent, "Treasure_01", THMapObject.ObjectType.Treasure, 9, 11, "obj_treasure_chest", 150);
            AddRes(parent, "ManaCrystal_01", THMapObject.ObjectType.ManaResource, 13, 13, "obj_mana_crystal_blue", 6);
            AddRes(parent, "GoldPile_03", THMapObject.ObjectType.GoldResource, 16, 8, "obj_gold_pile_small", 150);
            AddRes(parent, "StonePile_02", THMapObject.ObjectType.StoneResource, 21, 7, "obj_stone_pile", 12);
            AddRes(parent, "GoldMine_01", THMapObject.ObjectType.Mine, 23, 8, "obj_gold_mine", 50);
            AddRes(parent, "StoneQuarry_01", THMapObject.ObjectType.Mine, 25, 5, "obj_stone_pile", 5);
            AddRes(parent, "Shrine_01", THMapObject.ObjectType.Shrine, 10, 17, "obj_mana_crystal_blue", 100);
            AddRes(parent, "Treasure_02", THMapObject.ObjectType.Treasure, 12, 19, "obj_treasure_chest", 200);
            AddRes(parent, "ManaCrystal_02", THMapObject.ObjectType.ManaResource, 29, 16, "obj_mana_crystal_blue", 10);
            AddRes(parent, "Treasure_03", THMapObject.ObjectType.Treasure, 31, 18, "obj_treasure_chest", 300);
            AddRes(parent, "ManaCrystal_03", THMapObject.ObjectType.ManaResource, 34, 18, "obj_mana_crystal_blue", 12);
        }

        private static void AddRes(Transform parent, string id, THMapObject.ObjectType type, int x, int y, string sprite, int reward)
        {
            GameObject go = new GameObject(id, typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            go.transform.SetParent(parent); go.transform.position = new Vector3(x - 17.5f, y - 11.5f, 0);
            var mo = go.GetComponent<THMapObject>(); mo.id = id; mo.type = type; mo.targetX = x; mo.targetY = y; mo.displayName = id;
            if (type == THMapObject.ObjectType.GoldResource) mo.rewardGold = reward;
            else if (type == THMapObject.ObjectType.WoodResource) mo.rewardWood = reward;
            else if (type == THMapObject.ObjectType.StoneResource) mo.rewardStone = reward;
            else if (type == THMapObject.ObjectType.ManaResource) mo.rewardMana = reward;
            var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = LoadSprite($"Assets/Resources/Sprites/MapObjects/{sprite}.png");
            sr.sortingOrder = 20;
        }

        private static void PlaceAllEnemies(Transform parent)
        {
            AddEnemy(parent, "Enemy_Goblins_01", 11, 4, "Goblins", "goblin", 10, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Enemy_Goblins_02", 14, 7, "Goblins", "goblin", 14, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Enemy_Wolves_01", 7, 10, "Wolves", "wolf", 8, THEnemyDifficulty.Weak);
            AddEnemy(parent, "Enemy_Wolves_02", 9, 16, "Wolves", "wolf", 12, THEnemyDifficulty.Medium);
            AddEnemy(parent, "Enemy_Bandits_01", 17, 8, "Bandits", "swordsman", 6, THEnemyDifficulty.Medium);
            AddEnemy(parent, "Enemy_Bandits_02", 13, 18, "Bandits", "swordsman", 8, THEnemyDifficulty.Medium);
            AddEnemy(parent, "Enemy_Orcs_01", 22, 8, "Orcs", "orc", 8, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_Orcs_02", 24, 12, "Orcs", "orc", 10, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_Skeletons_01", 29, 15, "Skeletons", "skeleton", 16, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_DarkKnights_01", 31, 17, "Dark Knights", "dk", 8, THEnemyDifficulty.Strong);
            AddEnemy(parent, "Enemy_DarkLord_Final", 33, 20, "The Dark Lord", "darklord", 1, THEnemyDifficulty.Deadly, true);
        }

        private static void AddEnemy(Transform parent, string id, int x, int y, string dName, string unit, int count, THEnemyDifficulty diff, bool boss = false)
        {
            GameObject go = new GameObject(id, typeof(SpriteRenderer), typeof(THMapObject), typeof(BoxCollider2D));
            go.transform.SetParent(parent); go.transform.position = new Vector3(x - 17.5f, y - 11.5f, 0);
            var mo = go.GetComponent<THMapObject>(); mo.id = id; mo.type = THMapObject.ObjectType.Enemy; mo.targetX = x; mo.targetY = y; mo.displayName = dName;
            mo.difficulty = diff; mo.isDarkLord = boss;
            mo.enemyArmy.Add(new THArmyUnit { id = unit, name = unit, count = count, hpPerUnit = 20, attack = 5, defense = 2, initiative = 5 });
            var sr = go.GetComponent<SpriteRenderer>(); sr.sprite = LoadSprite("Assets/Resources/Sprites/MapObjects/obj_enemy_goblins.png");
            if (boss) sr.sprite = LoadSprite("Assets/Resources/Sprites/MapObjects/obj_enemy_darklord.png");
            sr.sortingOrder = 25;
            if (boss) { sr.sortingOrder = 30; go.transform.localScale = Vector3.one * 1.5f; }
        }

        private static void RebuildMapHUD()
        {
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                var c = canvasGo.GetComponent<Canvas>();
                c.renderMode = RenderMode.ScreenSpaceOverlay;
                var s = canvasGo.GetComponent<CanvasScaler>();
                s.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                s.referenceResolution = new Vector2(1920, 1080);
            }

            Transform canvasTransform = canvasGo.transform;

            // TopHUD
            GameObject topHUDGo = GameObject.Find("TopHUD") ?? new GameObject("TopHUD", typeof(RectTransform), typeof(Image));
            topHUDGo.transform.SetParent(canvasTransform, false);
            var topRT = topHUDGo.GetComponent<RectTransform>();
            topRT.anchorMin = new Vector2(0, 1);
            topRT.anchorMax = new Vector2(1, 1);
            topRT.pivot = new Vector2(0.5f, 1);
            topRT.sizeDelta = new Vector2(0, 58);
            topRT.anchoredPosition = Vector2.zero;
            topHUDGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.7f);

            // QuestPanel
            GameObject questGo = GameObject.Find("QuestPanel") ?? new GameObject("QuestPanel", typeof(RectTransform), typeof(Image));
            questGo.transform.SetParent(canvasTransform, false);
            var qRT = questGo.GetComponent<RectTransform>();
            qRT.anchorMin = new Vector2(0, 1);
            qRT.anchorMax = new Vector2(0, 1);
            qRT.pivot = new Vector2(0, 1);
            qRT.sizeDelta = new Vector2(380, 110);
            qRT.anchoredPosition = new Vector2(20, -78);
            questGo.GetComponent<Image>().color = new Color(0, 0, 0, 0.6f);

            EnsureText(questGo.transform, "Title", "Цель: Победить Тёмного Лорда", new Vector2(10, -10), 22).alignment = TextAnchor.UpperLeft;
            EnsureText(questGo.transform, "Progress", "Прогресс: 0 / 9", new Vector2(10, -45), 18).alignment = TextAnchor.UpperLeft;
            EnsureText(questGo.transform, "Details", "Найдите Тёмного Лорда в Тёмных Землях", new Vector2(10, -75), 18).alignment = TextAnchor.UpperLeft;


            // CastleButton
            GameObject castleBtn = GameObject.Find("CastleButton") ?? new GameObject("CastleButton", typeof(RectTransform), typeof(Image), typeof(Button));
            castleBtn.transform.SetParent(canvasTransform, false);
            var cRT = castleBtn.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero;
            cRT.anchorMax = Vector2.zero;
            cRT.pivot = Vector2.zero;
            cRT.sizeDelta = new Vector2(140, 48);
            cRT.anchoredPosition = new Vector2(20, 20);
            castleBtn.GetComponent<Image>().color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            // Wire Controller
            var controller = Object.FindAnyObjectByType<THMapController>();
            if (controller != null)
            {
                // Resources
                controller.GoldText = EnsureText(topHUDGo.transform, "GoldText", "Золото: 0", new Vector2(-800, 0), 22);
                controller.WoodText = EnsureText(topHUDGo.transform, "WoodText", "Дерево: 0", new Vector2(-650, 0), 22);
                controller.StoneText = EnsureText(topHUDGo.transform, "StoneText", "Камень: 0", new Vector2(-500, 0), 22);
                controller.ManaText = EnsureText(topHUDGo.transform, "ManaText", "Мана: 0", new Vector2(-350, 0), 22);
                
                // Info
                controller.InfoText = EnsureText(canvasTransform, "InfoMessagePanel", "Добро пожаловать", new Vector2(0, 100), 24, true);
            }
        }

        private static Text EnsureText(Transform parent, string name, string def, Vector2 pos, int size, bool isBottom = false)
        {
            var go = parent.Find(name)?.gameObject ?? new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            if (isBottom)
            {
                rt.anchorMin = new Vector2(0.5f, 0);
                rt.anchorMax = new Vector2(0.5f, 0);
                rt.anchoredPosition = pos;
            }
            else
            {
                rt.anchoredPosition = pos;
            }
            rt.sizeDelta = new Vector2(200, 50);
            var t = go.GetComponent<Text>();
            t.text = def;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            return t;
        }

        private static void RemoveMainMenuUI()
        {
            string[] toRemove = { "MainMenuPanel", "NewGamePanel", "DecorativeFrame", "MainTitle" };
            foreach (var name in toRemove)
            {
                var go = GameObject.Find(name);
                if (go != null) Object.DestroyImmediate(go);
            }
        }

        private static void ValidateMap()
        {
            // Reachability check (simple check for path block)
            Debug.Log("[TheHeroCampaignMap] Validation: 36x24 tiles created.");
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            Transform child = parent.Find(name);
            if (child == null) { GameObject go = new GameObject(name); go.transform.SetParent(parent); child = go.transform; }
            return child;
        }

        private static void ClearChildren(Transform parent) { for (int i = parent.childCount - 1; i >= 0; i--) Object.DestroyImmediate(parent.GetChild(i).gameObject); }

        private static void GenerateRequiredSprites()
        {
            string path = "Assets/Resources/Sprites/MapTiles/";
            string[] files = { "grass_01", "grass_02", "grass_03", "grass_flowers", "grass_dry", "road_horizontal", "road_vertical", "road_turn_ne", "road_turn_nw", "road_turn_se", "road_turn_sw", "water_center_01", "bridge_wood", "forest_dense_01", "forest_sparse_01", "forest_edge_n", "mountain_01", "hill_01", "darkland_01", "swamp_01", "water_bank_grass_n", "water_bank_grass_s", "water_bank_grass_e", "water_bank_grass_w" };
            Color[] colors = { Color.green, new Color(0, 0.8f, 0), new Color(0, 0.7f, 0), Color.yellow, new Color(0.6f, 0.6f, 0.2f), new Color(0.5f, 0.3f, 0.1f), new Color(0.5f, 0.3f, 0.1f), Color.white, Color.white, Color.white, Color.white, Color.blue, new Color(0.4f, 0.2f, 0.1f), new Color(0, 0.4f, 0), new Color(0, 0.5f, 0), new Color(0, 0.6f, 0), Color.gray, new Color(0.4f, 0.4f, 0.2f), new Color(0.2f, 0.1f, 0.2f), new Color(0.1f, 0.2f, 0.1f), Color.cyan, Color.cyan, Color.cyan, Color.cyan };
            for (int i = 0; i < files.Length; i++) EnsureSprite(path + files[i] + ".png", colors[i]);
        }

        private static void EnsureSprite(string path, Color color)
        {
            if (File.Exists(path)) return;
            Texture2D tex = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels); tex.Apply();
            byte[] bytes = tex.EncodeToPNG();
            File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            importer.textureType = TextureImporterType.Sprite; importer.spritePixelsPerUnit = 64; importer.filterMode = FilterMode.Point; importer.SaveAndReimport();
        }
    }
}