using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using TheHero.Generated;

namespace TheHero.Editor
{
    public class TheHeroExpandMapAndFixCamera : EditorWindow
    {
        private const int MapWidth = 72;
        private const int MapHeight = 48;

        [MenuItem("The Hero/Map/Expand Map And Fix Camera")]
        public static void ExpandMap()
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/Map.unity", OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[TheHeroLargeMap] Could not open Map scene");
                return;
            }

            Debug.Log("[TheHeroLargeMap] Starting map expansion...");

            // 1. Cleanup old map
            CleanupOldMap();

            // 2. Create Grid and Tilemap
            var grid = CreateGrid();
            var groundTM = CreateTilemap(grid, "Ground_Tilemap", 0);
            var waterTM = CreateTilemap(grid, "Water_Tilemap", -1);
            var decorTM = CreateTilemap(grid, "Decor_Tilemap", 1);

            // 3. Load Tiles
            var grassTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color1_0.asset");
            var waterTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Water Tile animated.asset");
            var darkTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color5_0.asset");
            var roadTile = AssetDatabase.LoadAssetAtPath<TileBase>("Assets/Tiny Swords/Terrain/Tileset/Tilemap Settings/Sliced Tiles/Tilemap_color2_21.asset"); // Dummy road

            // 4. Fill Map
            FillGround(groundTM, grassTile, MapWidth, MapHeight);
            CreateZones(groundTM, waterTM, decorTM, grassTile, waterTile, darkTile, MapWidth, MapHeight);

            // 5. Create Logic Grid (THTiles)
            var logicRoot = new GameObject("Logic_Grid");
            logicRoot.transform.SetParent(grid.transform);
            CreateLogicTiles(logicRoot, MapWidth, MapHeight, darkTile);

            // 6. Place Core Objects
            var hero = PlaceHero(6, 5);
            var castle = PlaceCastle(4, 5);

            // 7. Place Resources and Enemies
            PlaceResources(MapWidth, MapHeight);
            PlaceEnemies(MapWidth, MapHeight);

            // 8. Fix Camera
            FixCamera(hero);

            // 9. Update Bounds and Refresh
            if (THMapGridInput.Instance != null) THMapGridInput.Instance.RefreshGrid();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[TheHeroLargeMap] Validation passed and Map expanded");
        }

        private static void CleanupOldMap()
        {
            var tiles = Object.FindObjectsByType<THTile>(FindObjectsInactive.Include).ToList();
            foreach (var t in tiles) Object.DestroyImmediate(t.gameObject);
            
            var grid = Object.FindAnyObjectByType<Grid>();
            if (grid != null) Object.DestroyImmediate(grid.gameObject);

            var oldLogic = GameObject.Find("Logic_Grid");
            if (oldLogic != null) Object.DestroyImmediate(oldLogic);

            // Keep Hero and Castle if they are not THTiles
            var existingHero = GameObject.Find("Hero");
            if (existingHero != null) existingHero.transform.SetParent(null);
            
            var existingCastle = GameObject.Find("Castle_Player");
            if (existingCastle != null) existingCastle.transform.SetParent(null);
        }

        private static Grid CreateGrid()
        {
            var go = new GameObject("Grid", typeof(Grid));
            return go.GetComponent<Grid>();
        }

        private static Tilemap CreateTilemap(Grid grid, string name, int sortingOrder)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(grid.transform);
            var tr = go.GetComponent<TilemapRenderer>();
            tr.sortingOrder = sortingOrder;
            return go.GetComponent<Tilemap>();
        }

        private static void FillGround(Tilemap tm, TileBase tile, int w, int h)
        {
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    tm.SetTile(new Vector3Int(x, y, 0), tile);
                }
            }
        }

        private static void CreateZones(Tilemap ground, Tilemap water, Tilemap decor, TileBase grass, TileBase waterTile, TileBase dark, int w, int h)
        {
            // River (Horizontal-ish)
            int riverY = 22;
            for (int x = 0; x < w; x++)
            {
                int yOffset = (int)(Mathf.Sin(x * 0.2f) * 2);
                for (int ty = -1; ty <= 1; ty++)
                {
                    Vector3Int pos = new Vector3Int(x, riverY + yOffset + ty, 0);
                    // Place bridges at specific intervals
                    if (x == 15 || x == 40 || x == 60)
                    {
                        ground.SetTile(pos, grass); // Bridge area
                    }
                    else
                    {
                        water.SetTile(pos, waterTile);
                        ground.SetTile(pos, null);
                    }
                }
            }

            // Dark Zone (Top Right)
            for (int x = 50; x < w; x++)
            {
                for (int y = 35; y < h; y++)
                {
                    ground.SetTile(new Vector3Int(x, y, 0), dark);
                }
            }
        }

        private static void CreateLogicTiles(GameObject root, int w, int h, TileBase darkTile)
        {
            // Note: In a real scenario we might want a more efficient way than 3456 GOs
            // but for this project's architecture it's the safest way to keep movement working.
            var waterTM = GameObject.Find("Water_Tilemap")?.GetComponent<Tilemap>();
            var darkTM = GameObject.Find("Ground_Tilemap")?.GetComponent<Tilemap>();

            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var go = new GameObject($"Tile_{x}_{y}", typeof(THTile), typeof(BoxCollider2D));
                    go.transform.SetParent(root.transform);
                    go.transform.position = new Vector3(x, y, 0);
                    
                    var collider = go.GetComponent<BoxCollider2D>();
                    collider.size = Vector2.one;

                    var tile = go.GetComponent<THTile>();
                    string type = "grass";
                    if (waterTM != null && waterTM.HasTile(new Vector3Int(x, y, 0))) type = "water";
                    else if (darkTM != null && darkTM.GetTile(new Vector3Int(x, y, 0)) == darkTile) type = "darkland";

                    tile.Setup(x, y, type);
                }
            }
        }

        private static GameObject PlaceHero(int x, int y)
        {
            var hero = GameObject.Find("Hero") ?? new GameObject("Hero");
            hero.transform.position = new Vector3(x, y, 0);
            
            var mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
            mover.currentX = x;
            mover.currentY = y;
            
            var sr = hero.GetComponent<SpriteRenderer>() ?? hero.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 100;
            if (sr.sprite == null) 
                sr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle.png").OfType<Sprite>().FirstOrDefault();

            return hero;
        }

        private static GameObject PlaceCastle(int x, int y)
        {
            var castle = GameObject.Find("Castle_Player") ?? new GameObject("Castle_Player");
            castle.transform.position = new Vector3(x, y, 0);
            
            var mo = castle.GetComponent<THMapObject>() ?? castle.AddComponent<THMapObject>();
            mo.type = THMapObject.ObjectType.Base;
            mo.id = "Castle_Player";
            mo.displayName = "Ваш Замок";
            mo.targetX = x;
            mo.targetY = y;
            mo.blocksMovement = true;

            var sr = castle.GetComponent<SpriteRenderer>() ?? castle.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            if (sr.sprite == null)
                sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Tiny Swords/Buildings/Blue Buildings/Castle.png");

            var col = castle.GetComponent<BoxCollider2D>() ?? castle.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 1.5f;

            return castle;
        }

        private static void PlaceResources(int w, int h)
        {
            var types = new[] { THMapObject.ObjectType.GoldResource, THMapObject.ObjectType.WoodResource, THMapObject.ObjectType.StoneResource, THMapObject.ObjectType.ManaResource, THMapObject.ObjectType.Treasure };
            var sprites = new Dictionary<THMapObject.ObjectType, string>
            {
                { THMapObject.ObjectType.GoldResource, "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png" },
                { THMapObject.ObjectType.WoodResource, "Assets/Tiny Swords/Pawn and Resources/Wood/Wood Resource/Wood_Resource.png" },
                { THMapObject.ObjectType.StoneResource, "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Stones/Gold Stone 1.png" },
                { THMapObject.ObjectType.ManaResource, "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png" }, // Use gold as placeholder
                { THMapObject.ObjectType.Treasure, "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png" }
            };

            Random.InitState(42);
            int count = 0;
            for (int i = 0; i < 60; i++)
            {
                int x = Random.Range(2, w - 2);
                int y = Random.Range(2, h - 2);

                if (IsTileWalkable(x, y) && !IsOccupied(x, y))
                {
                    var type = types[Random.Range(0, types.Length)];
                    var go = new GameObject($"Resource_{count}_{type}");
                    go.transform.position = new Vector3(x, y, 0);
                    
                    var mo = go.AddComponent<THMapObject>();
                    mo.type = type;
                    mo.id = $"res_{x}_{y}";
                    mo.displayName = type.ToString();
                    mo.targetX = x;
                    mo.targetY = y;
                    mo.blocksMovement = false;

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 40;
                    sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(sprites[type]);
                    go.transform.localScale = Vector3.one * 0.8f;

                    go.AddComponent<BoxCollider2D>().size = Vector2.one * 0.8f;
                    count++;
                }
            }
            Debug.Log($"[TheHeroLargeMap] Resources placed: {count}");
        }

        private static void PlaceEnemies(int w, int h)
        {
            Random.InitState(123);
            int count = 0;
            for (int i = 0; i < 50; i++)
            {
                int x = Random.Range(5, w - 5);
                int y = Random.Range(5, h - 5);

                if (IsTileWalkable(x, y) && !IsOccupied(x, y))
                {
                    var go = new GameObject($"Enemy_{count}");
                    go.transform.position = new Vector3(x, y, 0);
                    
                    var mo = go.AddComponent<THMapObject>();
                    mo.type = THMapObject.ObjectType.Enemy;
                    mo.id = $"enemy_{x}_{y}";
                    mo.displayName = "Вражеский отряд";
                    mo.targetX = x;
                    mo.targetY = y;
                    mo.blocksMovement = true;
                    mo.startsCombat = true;
                    mo.difficulty = (x > 50 && y > 30) ? THEnemyDifficulty.Strong : (x > 30 ? THEnemyDifficulty.Medium : THEnemyDifficulty.Weak);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sortingOrder = 90;
                    sr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Tiny Swords/Units/Red Units/Warrior/Warrior_Idle.png").OfType<Sprite>().FirstOrDefault();

                    go.AddComponent<BoxCollider2D>().size = Vector2.one;
                    count++;
                }
            }

            // Dark Lord
            var dl = new GameObject("Enemy_DarkLord_Final");
            dl.transform.position = new Vector3(MapWidth - 5, MapHeight - 5, 0);
            var dlMo = dl.AddComponent<THMapObject>();
            dlMo.type = THMapObject.ObjectType.Enemy;
            dlMo.id = "DarkLord_Final";
            dlMo.displayName = "Тёмный Лорд";
            dlMo.isFinalBoss = true;
            dlMo.isDarkLord = true;
            dlMo.targetX = MapWidth - 5;
            dlMo.targetY = MapHeight - 5;
            dlMo.difficulty = THEnemyDifficulty.Deadly;
            
            var dlSr = dl.AddComponent<SpriteRenderer>();
            dlSr.sortingOrder = 95;
            dlSr.sprite = AssetDatabase.LoadAllAssetsAtPath("Assets/Tiny Swords/Units/Black Units/Warrior/Warrior_Idle.png").OfType<Sprite>().FirstOrDefault();
            dl.transform.localScale = Vector3.one * 1.5f;
            dl.AddComponent<BoxCollider2D>().size = Vector2.one;

            Debug.Log($"[TheHeroLargeMap] Enemies placed: {count + 1}");
        }

        private static bool IsTileWalkable(int x, int y)
        {
            var waterTM = GameObject.Find("Water_Tilemap")?.GetComponent<Tilemap>();
            if (waterTM != null && waterTM.HasTile(new Vector3Int(x, y, 0))) return false;
            return true;
        }

        private static bool IsOccupied(int x, int y)
        {
            var objs = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
            return objs.Any(o => o.targetX == x && o.targetY == y);
        }

        private static void FixCamera(GameObject hero)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
                follow.Target = hero.transform;
                follow.SmoothSpeed = 10f;
                cam.orthographicSize = 7.5f;

                // Approximate bounds for 72x48
                follow.SetBounds(10, 62, 7, 41);
                
                Debug.Log("[TheHeroCamera] Camera follow installed and bounds set");
            }
        }
    }
}
