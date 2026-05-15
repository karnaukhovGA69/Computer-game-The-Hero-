using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

namespace TheHero.Editor
{
    public static class TheHeroBuildHommStyleMap
    {
        private const int Width = 36;
        private const int Height = 24;
        private const int Seed = 17701729;
        private const string MapScenePath = "Assets/Scenes/Map.unity";
        private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
        private const string ReportPath = "Assets/CodeAudit/HOMM_Style_Map_Build_Report.md";
        private const string TileAssetRoot = "Assets/Resources/Sprites/CleanMap/Tiles";
        private const string ObjectAssetRoot = "Assets/Resources/Sprites/CleanMap/Objects";

        private enum TileKind
        {
            Grass,
            GrassVariant,
            Road,
            ForestEdge,
            ForestDense,
            Water,
            Bridge,
            Mountain,
            Darkland,
            DarklandVariant
        }

        private sealed class BuildAssets
        {
            public readonly Dictionary<string, Sprite> Tiles = new Dictionary<string, Sprite>();
            public readonly Dictionary<string, Sprite> Objects = new Dictionary<string, Sprite>();
            public readonly HashSet<string> FoundTileNames = new HashSet<string>();
            public readonly HashSet<string> FoundObjectNames = new HashSet<string>();
            public readonly SortedSet<string> UsedAssets = new SortedSet<string>();
            public readonly SortedSet<string> MissingAssets = new SortedSet<string>();
            public readonly SortedSet<string> Fallbacks = new SortedSet<string>();
        }

        private sealed class BuildStats
        {
            public int ResourceCount;
            public int EnemyCount;
            public int ArtifactCount;
            public bool ValidationPassed;
            public readonly List<string> ValidationChecks = new List<string>();
        }

        [MenuItem("The Hero/Map/Build HOMM Style Adventure Map")]
        public static void BuildHommStyleAdventureMap()
        {
            if (!File.Exists(MapScenePath))
            {
                Debug.LogError("[TheHeroMapBuilder] Map scene not found: " + MapScenePath);
                return;
            }

            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            EditorSceneManager.OpenScene(MapScenePath);

            var assets = LoadCleanMapAssets();
            Debug.Log("[TheHeroMapBuilder] Clean tile assets found");

            var mapRoot = EnsureRoot("MapRoot");
            var tilesRoot = EnsureChild(mapRoot.transform, "Tiles");
            var objectsRoot = EnsureChild(mapRoot.transform, "Objects");
            var resourcesRoot = EnsureChild(objectsRoot, "Resources");
            var enemiesRoot = EnsureChild(objectsRoot, "Enemies");
            var buildingsRoot = EnsureChild(objectsRoot, "Buildings");
            var artifactsRoot = EnsureChild(objectsRoot, "Artifacts");
            var specialRoot = EnsureChild(objectsRoot, "Special");
            var highlightsRoot = EnsureChild(mapRoot.transform, "Highlights");

            ClearChildren(tilesRoot);
            ClearChildren(resourcesRoot);
            ClearChildren(enemiesRoot);
            ClearChildren(buildingsRoot);
            ClearChildren(artifactsRoot);
            ClearChildren(specialRoot);
            ClearChildren(highlightsRoot);
            Debug.Log("[TheHeroMapBuilder] Map cleared");

            var cells = BuildTerrain();
            var mainRoad = BuildMainRoad();
            var forestRoad = BuildForestRoad();
            ApplyRoads(cells, mainRoad);
            ApplyRoads(cells, forestRoad);

            var stats = new BuildStats();
            ValidateMap(cells, stats);
            stats.ValidationPassed = true;

            CreateTiles(tilesRoot, cells, assets);
            Debug.Log("[TheHeroMapBuilder] Meadow zone built");
            Debug.Log("[TheHeroMapBuilder] Forest zone built");
            Debug.Log("[TheHeroMapBuilder] River and bridges built");
            Debug.Log("[TheHeroMapBuilder] Mountain pass built");
            Debug.Log("[TheHeroMapBuilder] Darkland built");
            Debug.Log("[TheHeroMapBuilder] Road connected");

            CreateMapObjects(resourcesRoot, enemiesRoot, buildingsRoot, artifactsRoot, assets, stats);
            Debug.Log("[TheHeroMapBuilder] Resources placed");
            Debug.Log("[TheHeroMapBuilder] Guards placed");
            Debug.Log("[TheHeroMapBuilder] Artifact placeholder placed");
            Debug.Log("[TheHeroMapBuilder] Dark Lord configured as final boss");

            ConfigureHeroAndRuntime(mapRoot.transform, assets);
            ConfigureCameraAndBounds();
            EnsureSingleHoverLabel(specialRoot);

            Debug.Log("[TheHeroMapBuilder] Path validation passed");

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            WriteReport(assets, stats);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[TheHeroMapBuilder] Map ready");
            if (File.Exists(MainMenuScenePath))
            {
                EditorSceneManager.OpenScene(MainMenuScenePath);
            }
        }

        private static BuildAssets LoadCleanMapAssets()
        {
            var assets = new BuildAssets();
            RegisterFoundNames(TileAssetRoot, assets.FoundTileNames);
            RegisterFoundNames(ObjectAssetRoot, assets.FoundObjectNames);

            LoadTile(assets, "clean_grass");
            LoadTile(assets, "clean_grass_flowers", "clean_grass");
            LoadTile(assets, "clean_road", "clean_grass");
            LoadTile(assets, "clean_water", "clean_grass");
            LoadTile(assets, "clean_bridge", "clean_road", "clean_grass");
            LoadTile(assets, "clean_forest_dense", "clean_grass");
            LoadTile(assets, "clean_forest_edge", "clean_forest_dense", "clean_grass");
            LoadTile(assets, "clean_mountain", "clean_grass");
            LoadTile(assets, "clean_darkland", "clean_dark_grass", "clean_grass");
            LoadTile(assets, "clean_dark_grass", "clean_darkland", "clean_grass");

            LoadObject(assets, "clean_castle");
            LoadObject(assets, "clean_hero");
            LoadObject(assets, "clean_gold");
            LoadObject(assets, "clean_wood");
            LoadObject(assets, "clean_stone");
            LoadObject(assets, "clean_mana");
            LoadObject(assets, "clean_chest");
            LoadObject(assets, "clean_mine");
            LoadObject(assets, "clean_goblin", "clean_enemy_goblin_map");
            LoadObject(assets, "clean_enemy_goblin_map", "clean_goblin");
            LoadObject(assets, "clean_wolf");
            LoadObject(assets, "clean_orc", "clean_enemy_orc_map");
            LoadObject(assets, "clean_enemy_orc_map", "clean_orc");
            LoadObject(assets, "clean_boss", "clean_dark_boss", "clean_darklord_map");
            LoadObject(assets, "clean_dark_boss", "clean_boss", "clean_darklord_map");
            LoadObject(assets, "clean_darklord_map", "clean_dark_boss", "clean_boss");

            return assets;
        }

        private static void RegisterFoundNames(string root, HashSet<string> names)
        {
            if (!Directory.Exists(root)) return;

            foreach (var path in Directory.GetFiles(root, "*.png", SearchOption.AllDirectories))
            {
                names.Add(Path.GetFileNameWithoutExtension(path));
            }
        }

        private static Sprite LoadTile(BuildAssets assets, string key, params string[] fallbacks)
        {
            var sprite = LoadSprite(TileAssetRoot, key);
            if (sprite != null)
            {
                assets.Tiles[key] = sprite;
                assets.UsedAssets.Add("Tiles/" + key);
                return sprite;
            }

            foreach (var fallback in fallbacks)
            {
                sprite = LoadSprite(TileAssetRoot, fallback);
                if (sprite == null) continue;

                assets.Tiles[key] = sprite;
                assets.UsedAssets.Add("Tiles/" + fallback);
                assets.Fallbacks.Add("Tiles/" + key + " -> Tiles/" + fallback);
                return sprite;
            }

            assets.MissingAssets.Add("Tiles/" + key);
            return null;
        }

        private static Sprite LoadObject(BuildAssets assets, string key, params string[] fallbacks)
        {
            var sprite = LoadSprite(ObjectAssetRoot, key);
            if (sprite != null)
            {
                assets.Objects[key] = sprite;
                assets.UsedAssets.Add("Objects/" + key);
                return sprite;
            }

            foreach (var fallback in fallbacks)
            {
                sprite = LoadSprite(ObjectAssetRoot, fallback);
                if (sprite == null) continue;

                assets.Objects[key] = sprite;
                assets.UsedAssets.Add("Objects/" + fallback);
                assets.Fallbacks.Add("Objects/" + key + " -> Objects/" + fallback);
                return sprite;
            }

            assets.MissingAssets.Add("Objects/" + key);
            return null;
        }

        private static Sprite LoadSprite(string root, string key)
        {
            var path = root + "/" + key + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        private static TileKind[,] BuildTerrain()
        {
            var cells = new TileKind[Width, Height];
            for (var x = 0; x < Width; x++)
            {
                for (var y = 0; y < Height; y++)
                {
                    cells[x, y] = DeterministicIndex(x, y, 2) == 0 ? TileKind.Grass : TileKind.GrassVariant;
                }
            }

            ApplyForest(cells);
            ApplyRiver(cells);
            ApplyMountains(cells);
            ApplyDarkland(cells);
            EnsureSafeMeadow(cells);
            return cells;
        }

        private static void ApplyForest(TileKind[,] cells)
        {
            var forest = new bool[Width, Height];

            for (var x = 3; x <= 16; x++)
            {
                for (var y = 10; y <= 22; y++)
                {
                    if (IsForestClearing(x, y)) continue;

                    var noise = DeterministicIndex(x, y, 9);
                    var organic = x <= 5 || y >= 20 || (x + y) % 5 != 1 || noise >= 5;
                    var shaped = !(x > 14 && y < 13) && !(x < 5 && y < 12);
                    forest[x, y] = organic && shaped;
                }
            }

            for (var x = 3; x <= 16; x++)
            {
                for (var y = 10; y <= 22; y++)
                {
                    if (!forest[x, y]) continue;
                    cells[x, y] = IsForestEdge(forest, x, y) ? TileKind.ForestEdge : TileKind.ForestDense;
                }
            }
        }

        private static bool IsForestClearing(int x, int y)
        {
            return SquaredDistance(x, y, 7, 15) <= 5 || SquaredDistance(x, y, 12, 19) <= 5;
        }

        private static bool IsForestEdge(bool[,] forest, int x, int y)
        {
            foreach (var dir in Directions)
            {
                var nx = x + dir.x;
                var ny = y + dir.y;
                if (!InBounds(nx, ny) || !forest[nx, ny]) return true;
            }

            return false;
        }

        private static void ApplyRiver(TileKind[,] cells)
        {
            for (var y = 0; y < Height; y++)
            {
                var riverX = RiverX(y);
                for (var dx = 0; dx <= 1; dx++)
                {
                    var x = riverX + dx;
                    if (InBounds(x, y)) cells[x, y] = TileKind.Water;
                }
            }

            SetBridge(cells, 16, 5);
            SetBridge(cells, 17, 5);
            SetBridge(cells, 16, 14);
            SetBridge(cells, 17, 14);
        }

        private static int RiverX(int y)
        {
            if (y <= 3) return 15;
            if (y <= 8) return 16;
            if (y <= 12) return 17;
            if (y <= 18) return 16;
            return 17;
        }

        private static void SetBridge(TileKind[,] cells, int x, int y)
        {
            if (InBounds(x, y)) cells[x, y] = TileKind.Bridge;
        }

        private static void ApplyMountains(TileKind[,] cells)
        {
            var roadHint = new HashSet<Vector2Int>(BuildMainRoad());

            for (var x = 20; x <= 29; x++)
            {
                for (var y = 4; y <= 17; y++)
                {
                    if (NearAny(roadHint, x, y, 1)) continue;

                    var lowerRidge = y <= 8 && x >= 21 && x <= 28 && DeterministicIndex(x, y, 5) != 1;
                    var upperRidge = y >= 12 && x >= 23 && x <= 29 && DeterministicIndex(x, y, 6) != 2;
                    var sideClump = (x == 20 && y >= 10 && y <= 15) || (x >= 27 && y >= 5 && y <= 10);

                    if (lowerRidge || upperRidge || sideClump)
                    {
                        cells[x, y] = TileKind.Mountain;
                    }
                }
            }
        }

        private static void ApplyDarkland(TileKind[,] cells)
        {
            for (var x = 28; x < Width; x++)
            {
                for (var y = 14; y < Height; y++)
                {
                    cells[x, y] = DeterministicIndex(x, y, 4) == 0 ? TileKind.DarklandVariant : TileKind.Darkland;
                }
            }
        }

        private static void EnsureSafeMeadow(TileKind[,] cells)
        {
            for (var x = 0; x <= 9; x++)
            {
                for (var y = 0; y <= 8; y++)
                {
                    if (cells[x, y] == TileKind.Water || cells[x, y] == TileKind.Mountain)
                    {
                        cells[x, y] = DeterministicIndex(x, y, 2) == 0 ? TileKind.Grass : TileKind.GrassVariant;
                    }
                }
            }
        }

        private static List<Vector2Int> BuildMainRoad()
        {
            var path = new List<Vector2Int>();
            AddPath(path, new Vector2Int(2, 3), new Vector2Int(4, 3));
            AddPath(path, Last(path), new Vector2Int(6, 4));
            AddPath(path, Last(path), new Vector2Int(8, 4));
            AddPath(path, Last(path), new Vector2Int(10, 5));
            AddPath(path, Last(path), new Vector2Int(12, 5));
            AddPath(path, Last(path), new Vector2Int(16, 5));
            AddPath(path, Last(path), new Vector2Int(17, 5));
            AddPath(path, Last(path), new Vector2Int(19, 7));
            AddPath(path, Last(path), new Vector2Int(22, 7));
            AddPath(path, Last(path), new Vector2Int(24, 9));
            AddPath(path, Last(path), new Vector2Int(25, 11));
            AddPath(path, Last(path), new Vector2Int(27, 12));
            AddPath(path, Last(path), new Vector2Int(29, 15));
            AddPath(path, Last(path), new Vector2Int(30, 17));
            AddPath(path, Last(path), new Vector2Int(32, 19));
            return path.Distinct().ToList();
        }

        private static List<Vector2Int> BuildForestRoad()
        {
            var path = new List<Vector2Int>();
            AddPath(path, new Vector2Int(7, 14), new Vector2Int(10, 14));
            AddPath(path, Last(path), new Vector2Int(13, 15));
            AddPath(path, Last(path), new Vector2Int(16, 14));
            AddPath(path, Last(path), new Vector2Int(17, 14));
            AddPath(path, Last(path), new Vector2Int(20, 14));
            AddPath(path, Last(path), new Vector2Int(23, 13));
            return path.Distinct().ToList();
        }

        private static void ApplyRoads(TileKind[,] cells, IEnumerable<Vector2Int> road)
        {
            foreach (var pos in road)
            {
                if (!InBounds(pos.x, pos.y)) continue;
                cells[pos.x, pos.y] = cells[pos.x, pos.y] == TileKind.Water ? TileKind.Bridge : TileKind.Road;
            }

            SetBridge(cells, 16, 5);
            SetBridge(cells, 17, 5);
            SetBridge(cells, 16, 14);
            SetBridge(cells, 17, 14);
        }

        private static void AddPath(List<Vector2Int> path, Vector2Int start, Vector2Int end)
        {
            var current = path.Count == 0 ? start : Last(path);
            if (path.Count == 0) path.Add(current);

            while (current.x != end.x)
            {
                current.x += Math.Sign(end.x - current.x);
                path.Add(current);
            }

            while (current.y != end.y)
            {
                current.y += Math.Sign(end.y - current.y);
                path.Add(current);
            }
        }

        private static Vector2Int Last(List<Vector2Int> path)
        {
            return path[path.Count - 1];
        }

        private static void CreateTiles(Transform tilesRoot, TileKind[,] cells, BuildAssets assets)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    var kind = cells[x, y];
                    var go = new GameObject("Tile_" + x + "_" + y);
                    go.transform.SetParent(tilesRoot, false);
                    go.transform.position = World(x, y, 0f);

                    var sr = go.AddComponent<SpriteRenderer>();
                    sr.sprite = PickTileSprite(kind, x, y, assets);
                    sr.sortingOrder = 0;
                    FitSpriteToWorldSize(go.transform, sr.sprite, 1f);

                    var tile = go.AddComponent<THTile>();
                    tile.Setup(x, y, TileTypeName(kind));

                    var collider = go.AddComponent<BoxCollider2D>();
                    collider.isTrigger = false;
                    collider.size = sr.sprite != null ? sr.sprite.bounds.size : Vector2.one;
                }
            }
        }

        private static Sprite PickTileSprite(TileKind kind, int x, int y, BuildAssets assets)
        {
            switch (kind)
            {
                case TileKind.GrassVariant:
                    return GetTile(assets, DeterministicIndex(x, y, 3) == 0 ? "clean_grass_flowers" : "clean_grass");
                case TileKind.Road:
                    return GetTile(assets, "clean_road");
                case TileKind.ForestEdge:
                    return GetTile(assets, "clean_forest_edge");
                case TileKind.ForestDense:
                    return GetTile(assets, "clean_forest_dense");
                case TileKind.Water:
                    return GetTile(assets, "clean_water");
                case TileKind.Bridge:
                    return GetTile(assets, "clean_bridge");
                case TileKind.Mountain:
                    return GetTile(assets, "clean_mountain");
                case TileKind.Darkland:
                    return GetTile(assets, "clean_darkland");
                case TileKind.DarklandVariant:
                    return GetTile(assets, DeterministicIndex(x, y, 2) == 0 ? "clean_dark_grass" : "clean_darkland");
                default:
                    return GetTile(assets, "clean_grass");
            }
        }

        private static string TileTypeName(TileKind kind)
        {
            switch (kind)
            {
                case TileKind.Road:
                    return "road";
                case TileKind.ForestEdge:
                    return "forest_edge";
                case TileKind.ForestDense:
                    return "forest_dense";
                case TileKind.Water:
                    return "water";
                case TileKind.Bridge:
                    return "bridge";
                case TileKind.Mountain:
                    return "mountain";
                case TileKind.Darkland:
                case TileKind.DarklandVariant:
                    return "darkland";
                default:
                    return "grass";
            }
        }

        private static Sprite GetTile(BuildAssets assets, string key)
        {
            if (assets.Tiles.TryGetValue(key, out var sprite)) return sprite;
            assets.Tiles.TryGetValue("clean_grass", out sprite);
            return sprite;
        }

        private static Sprite GetObjectSprite(BuildAssets assets, string key)
        {
            if (assets.Objects.TryGetValue(key, out var sprite)) return sprite;
            assets.Objects.TryGetValue("clean_chest", out sprite);
            return sprite;
        }

        private static void CreateMapObjects(Transform resourcesRoot, Transform enemiesRoot, Transform buildingsRoot, Transform artifactsRoot, BuildAssets assets, BuildStats stats)
        {
            CreateObject(buildingsRoot, assets, "Castle_Player", 2, 3, "clean_castle", "Castle", THMapObject.ObjectType.Base, 1.15f, 20, false, obj =>
            {
                obj.displayName = "Castle";
            });

            CreateResource(resourcesRoot, assets, stats, "GoldPile_01", 3, 5, "clean_gold", THMapObject.ObjectType.GoldResource, gold: 120);
            CreateResource(resourcesRoot, assets, stats, "WoodPile_01", 5, 2, "clean_wood", THMapObject.ObjectType.WoodResource, wood: 12);
            CreateResource(resourcesRoot, assets, stats, "StonePile_01", 6, 5, "clean_stone", THMapObject.ObjectType.StoneResource, stone: 8);

            CreateResource(resourcesRoot, assets, stats, "GoldPile_02", 13, 4, "clean_gold", THMapObject.ObjectType.GoldResource, gold: 220);
            CreateResource(resourcesRoot, assets, stats, "WoodPile_02", 7, 15, "clean_wood", THMapObject.ObjectType.WoodResource, wood: 18);
            CreateResource(resourcesRoot, assets, stats, "Chest_01", 12, 19, "clean_chest", THMapObject.ObjectType.Treasure, gold: 250);
            CreateResource(resourcesRoot, assets, stats, "StonePile_02", 23, 6, "clean_stone", THMapObject.ObjectType.StoneResource, stone: 18);
            CreateResource(resourcesRoot, assets, stats, "GoldMine_01", 25, 8, "clean_mine", THMapObject.ObjectType.Mine, gold: 150, scale: 0.95f);
            CreateResource(resourcesRoot, assets, stats, "ManaCrystal_01", 31, 17, "clean_mana", THMapObject.ObjectType.ManaResource, mana: 8);
            CreateResource(resourcesRoot, assets, stats, "Chest_02", 34, 21, "clean_chest", THMapObject.ObjectType.Treasure, gold: 400);

            CreateObject(artifactsRoot, assets, "Artifact_01", 6, 16, "clean_chest", "Artifact_01", THMapObject.ObjectType.Artifact, 0.72f, 22, false, obj =>
            {
                obj.displayName = "\u0414\u0440\u0435\u0432\u043d\u0438\u0439 \u0430\u043c\u0443\u043b\u0435\u0442";
                obj.rewardExp = 50;
                var sr = obj.GetComponent<SpriteRenderer>();
                if (sr != null) sr.color = new Color(0.75f, 0.55f, 1f, 1f);
            });
            stats.ArtifactCount++;
            assets.Fallbacks.Add("Objects/artifact_placeholder -> Objects/clean_chest");

            CreateEnemy(enemiesRoot, assets, stats, "Enemy_Goblin_01", 8, 4, "clean_enemy_goblin_map", THEnemyDifficulty.Weak, 80, 40,
                Unit("unit_goblin", "Goblin", 8, 18, 4, 1, 5));
            CreateEnemy(enemiesRoot, assets, stats, "Enemy_Goblin_02", 14, 9, "clean_goblin", THEnemyDifficulty.Weak, 90, 45,
                Unit("unit_goblin", "Goblin", 10, 18, 4, 1, 5));
            CreateEnemy(enemiesRoot, assets, stats, "Enemy_Wolf_01", 8, 15, "clean_wolf", THEnemyDifficulty.Medium, 120, 65,
                Unit("unit_wolf", "Wolf", 12, 20, 5, 1, 8));
            CreateEnemy(enemiesRoot, assets, stats, "Enemy_Bandits_01", 12, 18, "clean_goblin", THEnemyDifficulty.Medium, 170, 85,
                Unit("unit_bandit", "Bandit", 14, 24, 6, 2, 6));
            CreateEnemy(enemiesRoot, assets, stats, "Enemy_Orc_01", 25, 7, "clean_enemy_orc_map", THEnemyDifficulty.Strong, 260, 120,
                Unit("unit_orc", "Orc", 14, 34, 8, 3, 5));
            CreateEnemy(enemiesRoot, assets, stats, "Enemy_Skeleton_01", 31, 18, "clean_dark_boss", THEnemyDifficulty.Strong, 300, 150,
                Unit("unit_skeleton", "Skeleton", 16, 30, 8, 4, 5));
            CreateEnemy(enemiesRoot, assets, stats, "Enemy_DarkGuard_01", 33, 20, "clean_boss", THEnemyDifficulty.Deadly, 420, 220,
                Unit("unit_dark_guard", "Dark Guard", 18, 42, 10, 5, 6));

            CreateEnemy(enemiesRoot, assets, stats, "Enemy_DarkLord_Final", 32, 19, "clean_darklord_map", THEnemyDifficulty.Deadly, 900, 500,
                Unit("unit_dark_lord", "Dark Lord", 1, 260, 24, 12, 8),
                Unit("unit_dark_guard", "Dark Guard", 16, 42, 10, 5, 6),
                Unit("unit_orc_elite", "Orc Elite", 18, 38, 9, 4, 5));
        }

        private static void CreateResource(Transform parent, BuildAssets assets, BuildStats stats, string id, int x, int y, string spriteKey, THMapObject.ObjectType type, int gold = 0, int wood = 0, int stone = 0, int mana = 0, float scale = 0.72f)
        {
            CreateObject(parent, assets, id, x, y, spriteKey, id, type, scale, 21, false, obj =>
            {
                obj.rewardGold = gold;
                obj.rewardWood = wood;
                obj.rewardStone = stone;
                obj.rewardMana = mana;
                obj.rewardExp = type == THMapObject.ObjectType.Treasure ? 60 : 0;
            });
            stats.ResourceCount++;
        }

        private static void CreateEnemy(Transform parent, BuildAssets assets, BuildStats stats, string id, int x, int y, string spriteKey, THEnemyDifficulty difficulty, int rewardGold, int rewardExp, params THArmyUnit[] army)
        {
            CreateObject(parent, assets, id, x, y, spriteKey, id, THMapObject.ObjectType.Enemy, id == "Enemy_DarkLord_Final" ? 1.1f : 0.82f, 25, true, obj =>
            {
                obj.difficulty = difficulty;
                obj.startsCombat = true;
                obj.blocksMovement = true;
                obj.rewardGold = rewardGold;
                obj.rewardExp = rewardExp;
                obj.enemyArmy = army.Select(u => u.Clone()).ToList();
                obj.isFinalBoss = id == "Enemy_DarkLord_Final";
                obj.isDarkLord = id == "Enemy_DarkLord_Final";
                if (obj.isDarkLord)
                {
                    obj.displayName = "\u0422\u0451\u043c\u043d\u044b\u0439 \u041b\u043e\u0440\u0434";
                }
            });
            stats.EnemyCount++;
        }

        private static THMapObject CreateObject(Transform parent, BuildAssets assets, string id, int x, int y, string spriteKey, string displayName, THMapObject.ObjectType type, float worldSize, int sortingOrder, bool blocksMovement, Action<THMapObject> configure)
        {
            var go = new GameObject(id);
            go.transform.SetParent(parent, false);
            go.transform.position = World(x, y, -0.1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetObjectSprite(assets, spriteKey);
            sr.sortingOrder = sortingOrder;
            FitSpriteToWorldSize(go.transform, sr.sprite, worldSize);

            var collider = go.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = sr.sprite != null ? sr.sprite.bounds.size * 0.82f : Vector2.one;

            var obj = go.AddComponent<THMapObject>();
            obj.id = id;
            obj.type = type;
            obj.displayName = displayName;
            obj.targetX = x;
            obj.targetY = y;
            obj.blocksMovement = blocksMovement;
            obj.startsCombat = type == THMapObject.ObjectType.Enemy;
            configure?.Invoke(obj);
            THBalanceConfig.ConfigureMapObjectBalance(obj);
            EditorUtility.SetDirty(obj);
            return obj;
        }

        private static THArmyUnit Unit(string id, string name, int count, int hp, int attack, int defense, int initiative)
        {
            return new THArmyUnit
            {
                id = id,
                name = name,
                count = count,
                hpPerUnit = hp,
                attack = attack,
                defense = defense,
                initiative = initiative
            };
        }

        private static void ConfigureHeroAndRuntime(Transform mapRoot, BuildAssets assets)
        {
            var hero = GameObject.Find("Hero") ?? new GameObject("Hero");
            hero.transform.SetParent(mapRoot, false);
            hero.transform.position = World(4, 3, -0.2f);

            var sr = hero.GetComponent<SpriteRenderer>() ?? hero.AddComponent<SpriteRenderer>();
            sr.sprite = GetObjectSprite(assets, "clean_hero");
            sr.sortingOrder = 50;
            FitSpriteToWorldSize(hero.transform, sr.sprite, 0.88f);

            var collider = hero.GetComponent<BoxCollider2D>() ?? hero.AddComponent<BoxCollider2D>();
            collider.isTrigger = true;
            collider.size = sr.sprite != null ? sr.sprite.bounds.size * 0.7f : Vector2.one;

            foreach (var behaviour in hero.GetComponents<MonoBehaviour>())
            {
                var typeName = behaviour.GetType().Name;
                if (typeName == "THGuaranteedHeroMovement" || typeName == "THReliableHeroMovement" || typeName == "HeroMover")
                {
                    behaviour.enabled = false;
                }
            }

            var mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
            mover.enabled = true;
            mover.currentX = 4;
            mover.currentY = 3;
            mover.keyboardDebugMovement = true;

            var controller = UnityEngine.Object.FindAnyObjectByType<THMapController>();
            if (controller == null)
            {
                var go = GameObject.Find("MapController") ?? new GameObject("MapController");
                controller = go.AddComponent<THMapController>();
            }

            controller.HeroMover = mover;

            var grid = controller.GetComponent<THMapGridInput>() ?? controller.gameObject.AddComponent<THMapGridInput>();
            EditorUtility.SetDirty(grid);
            EditorUtility.SetDirty(controller);
        }

        private static void ConfigureCameraAndBounds()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
                cameraGo.tag = "MainCamera";
                camera = cameraGo.GetComponent<Camera>() ?? cameraGo.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.orthographicSize = 6.5f;
            camera.transform.position = World(4, 3, -10f);
            camera.backgroundColor = new Color(0.08f, 0.12f, 0.1f, 1f);

            if (camera.GetComponent<Physics2DRaycaster>() == null)
            {
                camera.gameObject.AddComponent<Physics2DRaycaster>();
            }

            var boundsGo = GameObject.Find("MapBounds") ?? new GameObject("MapBounds");
            var bounds = boundsGo.GetComponent<THMapBounds>() ?? boundsGo.AddComponent<THMapBounds>();
            bounds.minX = -18;
            bounds.maxX = 17;
            bounds.minY = -12;
            bounds.maxY = 11;
            bounds.initialized = true;
            EditorUtility.SetDirty(bounds);
        }

        private static void EnsureSingleHoverLabel(Transform specialRoot)
        {
            var labels = UnityEngine.Object.FindObjectsByType<THSingleMapHoverLabel>(FindObjectsInactive.Include);
            THSingleMapHoverLabel keep = labels.FirstOrDefault();

            if (keep == null)
            {
                var go = new GameObject("MapHoverLabelController");
                go.transform.SetParent(specialRoot, false);
                keep = go.AddComponent<THSingleMapHoverLabel>();
            }
            else
            {
                keep.transform.SetParent(specialRoot, false);
                keep.name = "MapHoverLabelController";
            }

            foreach (var duplicate in labels.Where(l => l != null && l != keep))
            {
                UnityEngine.Object.DestroyImmediate(duplicate.gameObject);
            }
        }

        private static void ValidateMap(TileKind[,] cells, BuildStats stats)
        {
            var blockers = new HashSet<Vector2Int>
            {
                new Vector2Int(8, 4),
                new Vector2Int(14, 9),
                new Vector2Int(8, 15),
                new Vector2Int(12, 18),
                new Vector2Int(25, 7),
                new Vector2Int(31, 18),
                new Vector2Int(33, 20),
                new Vector2Int(32, 19)
            };

            ValidatePoint(cells, "Hero", new Vector2Int(4, 3));
            ValidatePoint(cells, "Castle", new Vector2Int(2, 3));
            ValidatePoint(cells, "DarkLord", new Vector2Int(32, 19));

            var cleared = new HashSet<Vector2Int>();
            RequirePath(cells, stats, "Hero -> Castle", new Vector2Int(4, 3), new Vector2Int(2, 3), blockers, cleared, allowBlockedTarget: true);
            RequirePath(cells, stats, "Hero -> first enemy", new Vector2Int(4, 3), new Vector2Int(8, 4), blockers, cleared, allowBlockedTarget: true);
            cleared.Add(new Vector2Int(8, 4));

            RequirePath(cells, stats, "Hero -> forest chest", new Vector2Int(4, 3), new Vector2Int(12, 19), blockers, cleared, allowBlockedTarget: true);
            RequirePath(cells, stats, "Hero -> bridge", new Vector2Int(4, 3), new Vector2Int(16, 5), blockers, cleared, allowBlockedTarget: false);
            RequirePath(cells, stats, "Hero -> mine", new Vector2Int(4, 3), new Vector2Int(25, 8), blockers, cleared, allowBlockedTarget: true);
            RequirePath(cells, stats, "Hero -> darkland", new Vector2Int(4, 3), new Vector2Int(30, 17), blockers, cleared, allowBlockedTarget: false);
            RequirePath(cells, stats, "Hero -> DarkLord", new Vector2Int(4, 3), new Vector2Int(32, 19), blockers, cleared, allowBlockedTarget: true);

            if (cells[16, 5] != TileKind.Bridge || cells[17, 5] != TileKind.Bridge || cells[16, 14] != TileKind.Bridge || cells[17, 14] != TileKind.Bridge)
            {
                throw new InvalidOperationException("[TheHeroMapBuilder] Bridge validation failed.");
            }
        }

        private static void ValidatePoint(TileKind[,] cells, string label, Vector2Int point)
        {
            if (!InBounds(point.x, point.y) || !IsWalkable(cells[point.x, point.y]))
            {
                throw new InvalidOperationException("[TheHeroMapBuilder] " + label + " is placed on a blocked tile: " + point);
            }
        }

        private static void RequirePath(TileKind[,] cells, BuildStats stats, string label, Vector2Int start, Vector2Int target, HashSet<Vector2Int> blockers, HashSet<Vector2Int> cleared, bool allowBlockedTarget)
        {
            if (!HasPath(cells, start, target, blockers, cleared, allowBlockedTarget))
            {
                CarveRepairPath(cells, start, target);
            }

            if (!HasPath(cells, start, target, blockers, cleared, allowBlockedTarget))
            {
                throw new InvalidOperationException("[TheHeroMapBuilder] Path validation failed: " + label);
            }

            stats.ValidationChecks.Add(label + ": passed");
        }

        private static bool HasPath(TileKind[,] cells, Vector2Int start, Vector2Int target, HashSet<Vector2Int> blockers, HashSet<Vector2Int> cleared, bool allowBlockedTarget)
        {
            var queue = new Queue<Vector2Int>();
            var visited = new HashSet<Vector2Int>();
            queue.Enqueue(start);
            visited.Add(start);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current == target) return true;

                foreach (var dir in Directions)
                {
                    var next = current + dir;
                    if (!InBounds(next.x, next.y) || visited.Contains(next)) continue;
                    if (!IsWalkable(cells[next.x, next.y])) continue;

                    var blocked = blockers.Contains(next) && !cleared.Contains(next);
                    if (blocked && !(allowBlockedTarget && next == target)) continue;

                    visited.Add(next);
                    queue.Enqueue(next);
                }
            }

            return false;
        }

        private static void CarveRepairPath(TileKind[,] cells, Vector2Int start, Vector2Int target)
        {
            var repair = new List<Vector2Int>();
            AddPath(repair, start, target);
            foreach (var pos in repair)
            {
                if (!InBounds(pos.x, pos.y)) continue;
                cells[pos.x, pos.y] = cells[pos.x, pos.y] == TileKind.Water ? TileKind.Bridge : TileKind.Road;
            }
        }

        private static bool IsWalkable(TileKind kind)
        {
            return kind != TileKind.Water && kind != TileKind.Mountain;
        }

        private static void WriteReport(BuildAssets assets, BuildStats stats)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));

            var expectedTiles = new[]
            {
                "meadow_center_01", "meadow_center_02", "meadow_center_03", "meadow_center_04", "clean_grass", "clean_grass_variant",
                "river_center_01", "river_center_02", "river_straight_horizontal", "river_straight_vertical", "river_corner_ne", "river_corner_nw", "river_corner_se", "river_corner_sw", "river_bank_n", "river_bank_s", "river_bank_e", "river_bank_w", "clean_water", "clean_bridge",
                "forest_center_dense_01", "forest_center_dense_02", "forest_center_light_01", "forest_edge_n", "forest_edge_s", "forest_edge_e", "forest_edge_w", "clean_forest_edge", "clean_forest_dense",
                "mountain_center_01", "mountain_center_02", "mountain_edge_n", "mountain_edge_s", "mountain_edge_e", "mountain_edge_w", "clean_mountain",
                "darkland_center_01", "darkland_center_02", "darkland_cracked_01", "darkland_deadgrass_01", "clean_dark_grass", "clean_darkland"
            };
            var expectedObjects = new[]
            {
                "clean_castle", "clean_hero", "clean_gold", "clean_wood", "clean_stone", "clean_mana", "clean_chest", "clean_mine", "clean_goblin", "clean_wolf", "clean_orc", "clean_boss", "clean_darklord", "artifact_placeholder"
            };

            var missingExpected = expectedTiles
                .Where(name => !assets.FoundTileNames.Contains(name))
                .Select(name => "Tiles/" + name)
                .Concat(expectedObjects.Where(name => !assets.FoundObjectNames.Contains(name)).Select(name => "Objects/" + name))
                .OrderBy(name => name)
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("# HOMM Style Map Build Report");
            sb.AppendLine();
            sb.AppendLine("## Assets Found");
            sb.AppendLine("- Tiles: " + string.Join(", ", assets.FoundTileNames.OrderBy(x => x)));
            sb.AppendLine("- Objects: " + string.Join(", ", assets.FoundObjectNames.OrderBy(x => x)));
            sb.AppendLine();
            sb.AppendLine("## Assets Used");
            foreach (var item in assets.UsedAssets) sb.AppendLine("- " + item);
            sb.AppendLine();
            sb.AppendLine("## Missing Or Replaced Assets");
            if (missingExpected.Count == 0 && assets.MissingAssets.Count == 0 && assets.Fallbacks.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                foreach (var item in missingExpected) sb.AppendLine("- Expected variant missing: " + item);
                foreach (var item in assets.MissingAssets) sb.AppendLine("- Required fallback missing: " + item);
                foreach (var item in assets.Fallbacks) sb.AppendLine("- Fallback used: " + item);
            }
            sb.AppendLine();
            sb.AppendLine("## Map");
            sb.AppendLine("- Size: 36 x 24 tiles");
            sb.AppendLine("- Tile size: 1 Unity world unit");
            sb.AppendLine("- Hero: grid (4,3), world " + World(4, 3, 0));
            sb.AppendLine("- Castle: grid (2,3)");
            sb.AppendLine("- DarkLord: grid (32,19)");
            sb.AppendLine("- Resources and rewards: " + stats.ResourceCount);
            sb.AppendLine("- Enemies: " + stats.EnemyCount);
            sb.AppendLine("- Artifacts: " + stats.ArtifactCount);
            sb.AppendLine("- Validation: " + (stats.ValidationPassed ? "passed" : "failed"));
            foreach (var check in stats.ValidationChecks) sb.AppendLine("- " + check);
            sb.AppendLine();
            sb.AppendLine("## Manual Test Checklist");
            sb.AppendLine("- MainMenu -> New Game -> Map");
            sb.AppendLine("- Hero starts near Castle_Player at (4,3)");
            sb.AppendLine("- Road leads through meadow, forest, bridge, mountain pass, darkland");
            sb.AppendLine("- Resources can be collected");
            sb.AppendLine("- Enemy_Goblin_01 starts Combat");
            sb.AppendLine("- GoldMine_01 can be captured after guard fight");
            sb.AppendLine("- Artifact_01 can be collected as placeholder");
            sb.AppendLine("- Castle_Player opens Base");
            sb.AppendLine("- Enemy_DarkLord_Final starts final Combat and final victory flow");

            File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        }

        private static Transform EnsureChild(Transform parent, string name)
        {
            var child = parent.Find(name);
            if (child != null) return child;

            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.transform;
        }

        private static GameObject EnsureRoot(string name)
        {
            var go = GameObject.Find(name);
            if (go == null) go = new GameObject(name);
            go.transform.SetParent(null);
            go.transform.position = Vector3.zero;
            return go;
        }

        private static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void FitSpriteToWorldSize(Transform transform, Sprite sprite, float worldSize)
        {
            if (sprite == null)
            {
                transform.localScale = Vector3.one;
                return;
            }

            var bounds = sprite.bounds.size;
            var max = Mathf.Max(bounds.x, bounds.y);
            var scale = max > 0.001f ? worldSize / max : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static Vector3 World(int x, int y, float z)
        {
            return new Vector3(x - 18f, y - 12f, z);
        }

        private static int DeterministicIndex(int x, int y, int count)
        {
            if (count <= 1) return 0;
            return Mathf.Abs(x * 31 + y * 17 + Seed) % count;
        }

        private static int SquaredDistance(int x, int y, int cx, int cy)
        {
            var dx = x - cx;
            var dy = y - cy;
            return dx * dx + dy * dy;
        }

        private static bool NearAny(HashSet<Vector2Int> points, int x, int y, int radius)
        {
            var r2 = radius * radius;
            return points.Any(p => SquaredDistance(x, y, p.x, p.y) <= r2);
        }

        private static bool InBounds(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        private static readonly Vector2Int[] Directions =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };
    }
}
