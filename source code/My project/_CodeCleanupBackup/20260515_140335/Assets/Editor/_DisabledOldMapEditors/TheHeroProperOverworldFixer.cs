using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroProperOverworldFixer
    {
        [MenuItem("The Hero/Fix/Build Proper Overworld Map")]
        public static void FixAll()
        {
            Debug.Log("<b>[TheHeroFix] Building Proper Overworld Map (Balanced)...</b>");

            if (!File.Exists("Assets/Scenes/Map.unity"))
            {
                Debug.LogError("[TheHeroFix] Map scene not found!");
                return;
            }

            EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

            // 1. Cleanup
            var mapRoot = GameObject.Find("MapRoot");
            if (mapRoot != null) Object.DestroyImmediate(mapRoot);
            
            var oldObjects = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include)
                .Where(go => go.name.StartsWith("Tile_") || go.name == "Hero" || go.name == "Tiles" || go.name == "Objects" || go.name == "MapController" || go.name == "Guard")
                .ToList();
            foreach (var go in oldObjects) if (go != null) Object.DestroyImmediate(go);

            mapRoot = new GameObject("MapRoot");
            var tilesContainer = new GameObject("Tiles");
            tilesContainer.transform.SetParent(mapRoot.transform);
            var objectsContainer = new GameObject("Objects");
            objectsContainer.transform.SetParent(mapRoot.transform);

            // 2. Build 20x14 Grid
            int width = 20;
            int height = 14;
            
            Sprite grass = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_grass.png");
            Sprite road = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_road.png");
            Sprite forest = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_forest.png");
            Sprite mountain = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_mountain.png");
            Sprite water = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Map/tile_water.png");

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var tileGo = new GameObject($"Tile_{x}_{y}");
                    tileGo.transform.SetParent(tilesContainer.transform);
                    tileGo.transform.position = new Vector3(x, y, 0);
                    var sr = tileGo.AddComponent<SpriteRenderer>();
                    var tile = tileGo.AddComponent<THTile>();
                    tileGo.AddComponent<BoxCollider2D>();
                    
                    string type = "grass";
                    if (y == 0) type = "water";
                    else if (x == 13 && y < 10 && y != 3 && y != 6) type = "mountain";
                    else if (x >= 7 && x < 13 && y >= 1 && y < 8) type = "forest";
                    else if (x > 13 && y < 9) type = "mountain";
                    
                    bool isRoad = (y == 3 && x < 14) || (x == 12 && y >= 3 && y <= 6) || (x >= 12 && y == 6 && x < 16) || (x == 15 && y >= 6 && y < 11);
                    if (isRoad) type = "road";

                    sr.sprite = type switch {
                        "road" => road,
                        "forest" => forest,
                        "mountain" => mountain,
                        "water" => water,
                        _ => grass
                    };
                    
                    if (x >= 14 && y >= 9) sr.color = new Color(0.4f, 0.4f, 0.5f);
                    else if (x < 10 && y >= 9) sr.color = new Color(0.7f, 0.7f, 1f);

                    tile.Setup(x, y, type);
                }
            }

            // 3. Place Objects
            CreateMapObject(objectsContainer.transform, "Castle_Player", THMapObject.ObjectType.Base, 1, 1, "Мой Замок", "obj_base");
            CreateMapObject(objectsContainer.transform, "GoldPile_01", THMapObject.ObjectType.GoldResource, 4, 3, "Золото (Малое)", "obj_gold");
            CreateMapObject(objectsContainer.transform, "WoodPile_01", THMapObject.ObjectType.WoodResource, 2, 4, "Дрова", "obj_wood");

            CreateMapObject(objectsContainer.transform, "Mine_Gold", THMapObject.ObjectType.Mine, 9, 2, "Золотая шахта", "obj_mine");
            CreateMapObject(objectsContainer.transform, "GoldPile_02", THMapObject.ObjectType.GoldResource, 11, 4, "Куча золота", "obj_gold");
            CreateMapObject(objectsContainer.transform, "WoodPile_02", THMapObject.ObjectType.WoodResource, 8, 6, "Запас древесины", "obj_wood");
            
            CreateMapObject(objectsContainer.transform, "StonePile_01", THMapObject.ObjectType.StoneResource, 16, 2, "Груда камней", "obj_stone");
            CreateMapObject(objectsContainer.transform, "StonePile_02", THMapObject.ObjectType.StoneResource, 18, 5, "Камнепад", "obj_stone");
            
            CreateMapObject(objectsContainer.transform, "ManaCrystal_01", THMapObject.ObjectType.ManaResource, 2, 11, "Кристалл Маны", "obj_mana");
            CreateMapObject(objectsContainer.transform, "ManaCrystal_02", THMapObject.ObjectType.ManaResource, 5, 10, "Древний Источник", "obj_mana");
            CreateMapObject(objectsContainer.transform, "Shrine_Experience", THMapObject.ObjectType.Shrine, 8, 12, "Алтарь Знаний", "obj_mana");
            
            CreateMapObject(objectsContainer.transform, "GoldPile_03", THMapObject.ObjectType.GoldResource, 13, 11, "Сокровища Лорда", "obj_gold");

            // Enemies
            CreateEnemy(objectsContainer.transform, "guard_weak", 10, 5, "Гоблины-стражи", 10, THEnemyDifficulty.Weak, 50, 50);
            CreateEnemy(objectsContainer.transform, "guard_strong", 16, 7, "Орда Орков", 15, THEnemyDifficulty.Strong, 150, 200);
            CreateEnemy(objectsContainer.transform, "guard_medium", 6, 9, "Скелеты-разведчики", 12, THEnemyDifficulty.Medium, 100, 100);

            var dlGo = CreateEnemy(objectsContainer.transform, "boss_final", 18, 12, "Тёмный Лорд", 40, THEnemyDifficulty.Deadly, 500, 1000);
            var dl = dlGo.GetComponent<THMapObject>();
            dl.isDarkLord = true;
            dl.displayName = "<color=red><b>ТЁМНЫЙ ЛОРД</b></color>";

            // 4. Hero
            var heroGo = new GameObject("Hero");
            heroGo.transform.SetParent(mapRoot.transform);
            var hsr = heroGo.AddComponent<SpriteRenderer>();
            hsr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/Units/hero.png");
            hsr.sortingOrder = 50;
            heroGo.transform.position = new Vector3(1, 1, 0);
            var mover = heroGo.AddComponent<THStrictGridHeroMovement>();
            mover.currentX = 1;
            mover.currentY = 1;

            // 5. Systems
            var controllerGo = new GameObject("MapController");
            controllerGo.transform.SetParent(mapRoot.transform);
            var controller = controllerGo.AddComponent<THMapController>();
            controllerGo.AddComponent<THMapGridInput>();
            controller.HeroMover = mover;
            
            var cam = Camera.main;
            if (cam != null) {
                cam.transform.position = new Vector3(width / 2f - 0.5f, height / 2f - 0.5f, -10);
                cam.orthographic = true;
                cam.orthographicSize = 8f;
                cam.backgroundColor = new Color(0.05f, 0.1f, 0.05f);
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            Debug.Log("<color=green>[TheHeroFix] Balanced Overworld Map built successfully!</color>");
        }

        private static GameObject CreateMapObject(Transform parent, string id, THMapObject.ObjectType type, int x, int y, string dName, string spriteName)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent);
            go.transform.position = new Vector3(x, y, 0);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Sprites/Map/{spriteName}.png");
            sr.sortingOrder = 10;
            var obj = go.AddComponent<THMapObject>();
            obj.id = id;
            obj.type = type;
            obj.targetX = x;
            obj.targetY = y;
            obj.displayName = dName;
            go.AddComponent<BoxCollider2D>();
            return go;
        }

        private static GameObject CreateEnemy(Transform parent, string id, int x, int y, string dName, int count, THEnemyDifficulty difficulty, int exp, int gold)
        {
            var go = CreateMapObject(parent, id, THMapObject.ObjectType.Enemy, x, y, dName, "obj_enemy");
            var obj = go.GetComponent<THMapObject>();
            obj.difficulty = difficulty;
            obj.rewardExp = exp;
            obj.rewardGold = gold;
            
            string unitId = difficulty switch {
                THEnemyDifficulty.Weak => "unit_goblin",
                THEnemyDifficulty.Medium => "unit_skeleton",
                THEnemyDifficulty.Strong => "unit_orc",
                THEnemyDifficulty.Deadly => "unit_darklord",
                _ => "unit_goblin"
            };

            obj.enemyArmy.Add(new THArmyUnit { id = unitId, name = dName, count = count, hpPerUnit = 25, attack = 6, defense = 3, initiative = 5 });
            return go;
        }
    }
}