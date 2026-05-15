using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>
/// Builds a playable 48x32 Map.unity using a 3-tier asset policy:
///   1. Assets/ExternalAssets/MainAssets   (primary)
///   2. Assets/Cainos**                    (fallback for terrain / props / buildings)
///   3. Assets/Tiny Swords / TinySwords**  (fallback for hero / enemies / castle)
/// Only sub-sprites are used; whole PNG sheets are filtered out via
/// <see cref="TheHeroMainAssetsMapUtil.IsWholeSheetSprite"/>. No mountain or dark zone is required.
/// Menu: The Hero/Map/Build Map MainAssets With Cainos Tiny Fallbacks
/// </summary>
public static class TheHeroBuildMapMainAssetsWithFallbacks
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string MainAssetsRoot = "Assets/ExternalAssets/MainAssets";
    private const string ReportPath = "Assets/CodeAudit/Map_MainAssets_Cainos_TinyFallback_Report.md";

    private const int W = 48, H = 32, CX = 24, CY = 16, HeroX = 24, HeroY = 13;

    private static readonly StringBuilder _report = new StringBuilder();
    private static readonly List<string> _missing = new List<string>();
    private static readonly Dictionary<string, string> _provenance = new Dictionary<string, string>();

    [MenuItem("The Hero/Map/Build Map MainAssets With Cainos Tiny Fallbacks")]
    public static void Build()
    {
        _report.Clear(); _missing.Clear(); _provenance.Clear();
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Debug.Log("[TheHeroAssetPolicy] MainAssets first");
        Debug.Log("[TheHeroAssetPolicy] Cainos fallback enabled");
        Debug.Log("[TheHeroAssetPolicy] Tiny Swords fallback enabled");

        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        var sources = AssetSources.Discover();
        Debug.Log("[TheHeroMapFallback] MainAssets scanned");
        Debug.Log("[TheHeroMapFallback] Cainos fallback scanned");
        Debug.Log("[TheHeroMapFallback] Tiny Swords fallback scanned");

        var cat = Catalog.Build(sources);

        ClearOldMapContent();
        var mapRoot = GetOrCreate("MapRoot");
        var grid = EnsureGrid(mapRoot);
        var objectsRoot = GetOrCreateChild(mapRoot, "ObjectsRoot");
        ClearChildren(objectsRoot);

        var ground = NewTilemap(grid, "GroundTilemap", 0);
        var detail = NewTilemap(grid, "DetailTilemap", 1);
        var road = NewTilemap(grid, "RoadTilemap", 2);
        Tilemap water = cat.Water != null ? NewTilemap(grid, "WaterTilemap", 3) : null;
        Tilemap bridge = cat.Bridge != null ? NewTilemap(grid, "BridgeTilemap", 4) : null;
        var forest = NewTilemap(grid, "ForestTilemap", 5);
        var ruins = NewTilemap(grid, "RuinsTilemap", 6);
        var blocking = NewTilemap(grid, "BlockingTilemap", 7);

        PaintGround(ground, cat);
        PaintForestZone(forest, detail, cat);
        PaintRuinsZone(ruins, detail, cat);
        PaintBossZone(ruins, detail, cat);
        PaintRoads(road, cat);
        PaintWaterAndBridge(water, bridge, cat);
        Debug.Log("[TheHeroMapFallback] Map rebuilt");

        var castle = BuildCastle(objectsRoot, cat);
        Debug.Log("[TheHeroMapFallback] Castle centered");

        var hero = BuildHero(objectsRoot, cat);
        Debug.Log("[TheHeroMapFallback] Hero placed near castle");

        PlaceResources(objectsRoot, cat);
        Debug.Log("[TheHeroMapFallback] Resources placed");

        PlaceEnemies(objectsRoot, cat);
        Debug.Log("[TheHeroMapFallback] Enemies placed");
        Debug.Log("[TheHeroMapFallback] Boss placed");

        EnsureSystems(castle, hero);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MapScenePath);
        Debug.Log("[TheHeroMapFallback] Map saved");

        WriteReport(cat);
        TheHeroValidateMapMainAssetsWithFallbacks.Run();
    }

    // ── 3-tier asset sources ────────────────────────────────────────────────
    private sealed class AssetSources
    {
        public string MainRoot;
        public List<string> CainosRoots = new List<string>();
        public List<string> TinyRoots = new List<string>();

        public static AssetSources Discover()
        {
            var s = new AssetSources();
            if (AssetDatabase.IsValidFolder(MainAssetsRoot)) s.MainRoot = MainAssetsRoot;

            foreach (string guid in AssetDatabase.FindAssets("t:Folder"))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                string lower = p.ToLowerInvariant();
                if (lower.Contains("cainos") || lower.Contains("pixel art top down")) s.CainosRoots.Add(p);
                if (lower.Contains("tiny sword") || lower.Contains("tinyswords") || lower.Contains("pixel frog")) s.TinyRoots.Add(p);
            }
            s.CainosRoots = s.CainosRoots.Distinct().ToList();
            s.TinyRoots = s.TinyRoots.Distinct().ToList();
            return s;
        }
    }

    // ── catalog with provenance ─────────────────────────────────────────────
    private sealed class Catalog
    {
        public List<Sprite> Grass = new List<Sprite>();
        public List<Sprite> GrassDetail = new List<Sprite>();
        public List<Sprite> Floor = new List<Sprite>();
        public List<Sprite> Walls = new List<Sprite>();
        public List<Sprite> Interior = new List<Sprite>();
        public List<Sprite> Props = new List<Sprite>();
        public List<Sprite> Plants = new List<Sprite>();
        public List<Sprite> Trees = new List<Sprite>();
        public List<Sprite> Houses = new List<Sprite>();
        public List<Sprite> CainosTerrain = new List<Sprite>();
        public List<Sprite> CainosProps = new List<Sprite>();
        public List<Sprite> CainosBuildings = new List<Sprite>();
        public List<Sprite> TinyAll = new List<Sprite>();
        public Sprite Water, Bridge, Road;
        public Sprite Hero;
        public Sprite Wolf, Skeleton, SkeletonMage, BloodGargoyle, DarkTroll, UnderworldKing;
        public Sprite Gold, Wood, Stone, Mana, Chest, Artifact;
        public List<Sprite> CastleParts = new List<Sprite>();

        public static Catalog Build(AssetSources s)
        {
            var c = new Catalog();

            // Tier 1: MainAssets
            c.Grass = MainSub("TX Tileset Grass.png");
            c.GrassDetail = MainSub("ground_grass_details.png");
            c.Floor = MainSub("Main_tiles.png");
            c.Walls = MainSub("walls_floor.png");
            c.Interior = MainSub("Interior.png");
            c.Props = MainSub("TX Props.png");
            c.Plants = MainSub("TX Plant.png");
            c.Trees = MainSub("Trees_animation.png").Concat(MainSub("free_pixel_16_woods.png")).ToList();
            c.Houses = MainSub("house_details.png");
            c.Water = MainSub("Water_animation.png").FirstOrDefault();
            c.Bridge = MainSub("Bridges.png").FirstOrDefault();

            c.Hero = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "idle");
            c.Wolf = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_121_CursedWolf");
            c.Skeleton = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "Skeleton Warrior");
            c.SkeletonMage = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "Skeleton Mage");
            c.BloodGargoyle = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_124_BloodGargoyle");
            c.DarkTroll = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_127_DarkTroll");
            c.UnderworldKing = TheHeroMainAssetsMapUtil.PickCharacterFrame(MainAssetsRoot, "FR_130_UnderworldKing");

            var icons = MainSub("Icons.png");
            c.Gold = TheHeroMainAssetsMapUtil.PickByName(c.Props, "pot", "coin", "gold")
                  ?? TheHeroMainAssetsMapUtil.PickByName(icons, "gold", "coin");
            c.Wood = TheHeroMainAssetsMapUtil.PickByName(c.Props, "crate", "barrel", "wood", "log");
            c.Stone = TheHeroMainAssetsMapUtil.PickByName(c.Props, "stone", "rock");
            c.Mana = TheHeroMainAssetsMapUtil.PickByName(c.Props, "rune", "crystal", "mana", "gem")
                  ?? TheHeroMainAssetsMapUtil.PickByName(icons, "mana", "crystal");
            c.Chest = TheHeroMainAssetsMapUtil.PickByName(c.Props, "chest", "box");
            c.Artifact = TheHeroMainAssetsMapUtil.PickByName(c.Props, "altar", "statue", "shrine");
            c.Road = TheHeroMainAssetsMapUtil.PickByName(c.Floor, "road", "path", "dirt", "stone")
                  ?? TheHeroMainAssetsMapUtil.PickByName(c.Walls, "road", "path", "stone", "floor");

            // Tier 2: Cainos
            foreach (var root in s.CainosRoots)
            {
                foreach (string p in AllSpriteAssetsUnder(root))
                {
                    foreach (Sprite sp in TheHeroMainAssetsMapUtil.LoadSlicedSprites(p))
                    {
                        if (sp == null) continue;
                        string lp = p.ToLowerInvariant();
                        string ln = sp.name.ToLowerInvariant();
                        if (lp.Contains("terrain") || ln.Contains("grass") || ln.Contains("dirt") || ln.Contains("floor"))
                            c.CainosTerrain.Add(sp);
                        if (lp.Contains("prop") || ln.Contains("prop") || ln.Contains("rock") || ln.Contains("tree") || ln.Contains("plant"))
                            c.CainosProps.Add(sp);
                        if (ln.Contains("castle") || ln.Contains("tower") || ln.Contains("house") || ln.Contains("building") || ln.Contains("fort") || ln.Contains("wall"))
                            c.CainosBuildings.Add(sp);
                    }
                }
            }

            // Tier 3: Tiny Swords
            foreach (var root in s.TinyRoots)
            {
                foreach (string p in AllSpriteAssetsUnder(root))
                {
                    foreach (Sprite sp in TheHeroMainAssetsMapUtil.LoadSlicedSprites(p))
                    {
                        if (sp == null) continue;
                        c.TinyAll.Add(sp);
                    }
                }
            }

            // Promote fallbacks where MainAssets came up empty
            if (c.Hero == null)
                c.Hero = PickFromTiny(c.TinyAll, "knight", "warrior", "hero", "blue", "soldier", "swordman");
            if (c.SkeletonMage == null)
                c.SkeletonMage = PickFromTiny(c.TinyAll, "mage", "wizard", "shaman", "necro", "caster")
                              ?? PickFromTiny(c.TinyAll, "skeleton");

            // Castle parts: try Tiny/Cainos building first if MainAssets house empty
            var buildings = c.CainosBuildings.Concat(
                                c.TinyAll.Where(sp => {
                                    var n = sp.name.ToLowerInvariant();
                                    return n.Contains("castle") || n.Contains("tower") || n.Contains("house") ||
                                           n.Contains("base") || n.Contains("camp") || n.Contains("barrack");
                                }))
                            .Where(sp => !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sp))
                            .ToList();
            if (buildings.Count > 0)
            {
                c.CastleParts.AddRange(buildings.Take(4));
            }
            else
            {
                if (c.Houses.Count > 0) c.CastleParts.Add(c.Houses[0]);
                if (c.Walls.Count > 0) c.CastleParts.Add(TheHeroMainAssetsMapUtil.PickByName(c.Walls, "wall", "stone") ?? c.Walls[0]);
                if (c.Props.Count > 0) c.CastleParts.Add(c.Props[0]);
            }

            return c;
        }

        private static List<Sprite> MainSub(string file)
        {
            string p = $"{MainAssetsRoot}/{file}";
            if (!File.Exists(p)) return new List<Sprite>();
            return TheHeroMainAssetsMapUtil.LoadSlicedSprites(p);
        }

        private static IEnumerable<string> AllSpriteAssetsUnder(string root)
        {
            if (!AssetDatabase.IsValidFolder(root)) yield break;
            foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { root }))
                yield return AssetDatabase.GUIDToAssetPath(guid);
        }

        private static Sprite PickFromTiny(List<Sprite> pool, params string[] tokens) =>
            TheHeroMainAssetsMapUtil.PickByName(pool, tokens);
    }

    // ── painters ────────────────────────────────────────────────────────────
    private static void PaintGround(Tilemap tm, Catalog c)
    {
        var pool = c.Grass.Count > 0 ? c.Grass : c.CainosTerrain.Where(s => s.name.ToLowerInvariant().Contains("grass")).ToList();
        if (pool.Count == 0 && c.CainosTerrain.Count > 0) pool = c.CainosTerrain;
        if (pool.Count == 0) { _missing.Add("ground grass sub-sprites"); return; }
        Track("Ground", pool[0]);
        var tiles = pool.Take(4).Select(MakeTile).ToList();
        for (int x = 0; x < W; x++) for (int y = 0; y < H; y++)
            tm.SetTile(new Vector3Int(x, y, 0), tiles[(x + y) % tiles.Count]);
    }

    private static void PaintForestZone(Tilemap forest, Tilemap detail, Catalog c)
    {
        var pool = c.Trees.Concat(c.Plants).Concat(c.GrassDetail).Where(s => s != null).Distinct().Take(6).ToList();
        if (pool.Count == 0)
            pool = c.CainosProps.Where(s => { var n = s.name.ToLowerInvariant(); return n.Contains("tree") || n.Contains("plant") || n.Contains("bush"); }).Take(6).ToList();
        if (pool.Count == 0) { _missing.Add("forest sub-sprites"); return; }
        Track("Forest", pool[0]);
        var tiles = pool.Select(MakeTile).ToList();
        for (int x = 2; x <= 13; x++) for (int y = 8; y <= 25; y++)
            if (((x * 7 + y * 13) % 4) == 0) forest.SetTile(new Vector3Int(x, y, 0), tiles[(x + y) % tiles.Count]);
    }

    private static void PaintRuinsZone(Tilemap ruins, Tilemap detail, Catalog c)
    {
        var floor = c.Walls.Concat(c.Interior).Concat(c.Floor).Where(s => s != null).Distinct().Take(6).ToList();
        if (floor.Count == 0) floor = c.CainosTerrain.Where(s => { var n = s.name.ToLowerInvariant(); return n.Contains("stone") || n.Contains("floor") || n.Contains("dirt"); }).Take(6).ToList();
        if (floor.Count == 0) { _missing.Add("ruins floor sub-sprites"); return; }
        Track("Ruins", floor[0]);
        var ftiles = floor.Select(MakeTile).ToList();
        for (int x = 33; x <= 45; x++) for (int y = 8; y <= 22; y++)
            if (((x + y) % 3) == 0) ruins.SetTile(new Vector3Int(x, y, 0), ftiles[(x * 5 + y) % ftiles.Count]);
    }

    private static void PaintBossZone(Tilemap ruins, Tilemap detail, Catalog c)
    {
        var floor = c.Walls.Concat(c.Interior).Where(s => s != null).Distinct().Take(5).ToList();
        if (floor.Count == 0) floor = c.CainosTerrain.Take(5).ToList();
        if (floor.Count == 0) return;
        var tiles = floor.Select(MakeTile).ToList();
        for (int x = 14; x <= 34; x++) for (int y = 24; y <= 30; y++)
            ruins.SetTile(new Vector3Int(x, y, 0), tiles[(x * 3 + y) % tiles.Count]);
    }

    private static void PaintRoads(Tilemap tm, Catalog c)
    {
        Sprite sp = c.Road
                 ?? TheHeroMainAssetsMapUtil.PickByName(c.CainosTerrain, "road", "path", "dirt", "stone")
                 ?? c.GrassDetail.FirstOrDefault()
                 ?? c.Grass.FirstOrDefault();
        if (sp == null) { _missing.Add("road sub-sprite"); return; }
        Track("Road", sp);
        var tile = MakeTile(sp);
        for (int x = 6; x <= 42; x++) tm.SetTile(new Vector3Int(x, CY, 0), tile);
        for (int y = 4; y <= 28; y++) tm.SetTile(new Vector3Int(CX, y, 0), tile);
        if (sp.name.ToLowerInvariant().IndexOf("road", StringComparison.Ordinal) < 0)
            tm.color = new Color(0.75f, 0.65f, 0.45f, 1f);
    }

    private static void PaintWaterAndBridge(Tilemap water, Tilemap bridge, Catalog c)
    {
        if (water != null && c.Water != null)
        {
            Track("Water", c.Water);
            var t = MakeTile(c.Water);
            for (int y = 4; y <= 28; y++)
            {
                if (y == CY || y == CY + 1) continue;
                water.SetTile(new Vector3Int(10, y, 0), t);
                water.SetTile(new Vector3Int(11, y, 0), t);
            }
        }
        if (bridge != null && c.Bridge != null)
        {
            Track("Bridge", c.Bridge);
            var t = MakeTile(c.Bridge);
            bridge.SetTile(new Vector3Int(10, CY, 0), t);
            bridge.SetTile(new Vector3Int(11, CY, 0), t);
            bridge.SetTile(new Vector3Int(10, CY + 1, 0), t);
            bridge.SetTile(new Vector3Int(11, CY + 1, 0), t);
        }
    }

    // ── objects ─────────────────────────────────────────────────────────────
    private static GameObject BuildCastle(GameObject root, Catalog c)
    {
        var castle = new GameObject("Castle_Player");
        castle.transform.SetParent(root.transform, false);
        castle.transform.position = new Vector3(CX, CY, -0.2f);

        var mo = castle.AddComponent<THMapObject>();
        mo.id = "Castle_Player"; mo.type = THMapObject.ObjectType.Base;
        mo.displayName = "Замок"; mo.targetX = CX; mo.targetY = CY;
        mo.blocksMovement = false; mo.startsCombat = false;
        castle.AddComponent<THCastle>();
        castle.AddComponent<BoxCollider2D>().size = new Vector2(1.4f, 1.4f);

        var parts = c.CastleParts.Where(p => p != null).Take(4).ToList();
        if (parts.Count > 0)
        {
            AddVisual(castle, parts[0], "Visual_House", Vector3.zero, 1.6f, 70);
            if (parts.Count > 1) AddVisual(castle, parts[1], "Visual_Wall_1", new Vector3(-0.45f, 0f, 0f), 0.8f, 71);
            if (parts.Count > 2) AddVisual(castle, parts[2], "Visual_Wall_2", new Vector3(0.45f, 0f, 0f), 0.8f, 71);
            if (parts.Count > 3) AddVisual(castle, parts[3], "Visual_Decor", new Vector3(0f, 0.4f, 0f), 0.6f, 72);
            Track("Castle", parts[0]);
        }
        else
        {
            _missing.Add("castle building sprites");
        }
        return castle;
    }

    private static void AddVisual(GameObject parent, Sprite sp, string name, Vector3 localPos, float targetCells, int sortingOrder)
    {
        if (sp == null) return;
        var v = new GameObject(name);
        v.transform.SetParent(parent.transform, false);
        v.transform.localPosition = localPos;
        var sr = v.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = sortingOrder;
        float dim = Mathf.Max(sp.bounds.size.x, sp.bounds.size.y);
        v.transform.localScale = Vector3.one * (targetCells / Mathf.Max(0.01f, dim));
    }

    private static GameObject BuildHero(GameObject root, Catalog c)
    {
        GameObject hero = GameObject.Find("Hero") ?? new GameObject("Hero");
        hero.transform.SetParent(root.transform, false);
        hero.transform.position = new Vector3(HeroX, HeroY, -0.2f);
        if (c.Hero != null)
        {
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(hero, c.Hero, 0.95f, 100);
            Track("Hero", c.Hero);
        }
        var mover = hero.GetComponent<THStrictGridHeroMovement>() ?? hero.AddComponent<THStrictGridHeroMovement>();
        mover.currentX = HeroX; mover.currentY = HeroY;
        if (hero.GetComponent<BoxCollider2D>() == null) hero.AddComponent<BoxCollider2D>();
        return hero;
    }

    private static void PlaceResources(GameObject root, Catalog c)
    {
        Sprite generic = c.Props.FirstOrDefault() ?? c.CainosProps.FirstOrDefault();
        var list = new List<(string id, string n, THMapObject.ObjectType t, int x, int y, Sprite s)>
        {
            ("Gold_Center_01","Золото",THMapObject.ObjectType.GoldResource,25,17,c.Gold ?? generic),
            ("Gold_West_01","Золото",THMapObject.ObjectType.GoldResource,7,18,c.Gold ?? generic),
            ("Gold_East_01","Золото",THMapObject.ObjectType.GoldResource,42,12,c.Gold ?? generic),
            ("Gold_South_01","Золото",THMapObject.ObjectType.GoldResource,20,6,c.Gold ?? generic),
            ("Wood_Forest_01","Дерево",THMapObject.ObjectType.WoodResource,5,14,c.Wood ?? generic),
            ("Wood_Forest_02","Дерево",THMapObject.ObjectType.WoodResource,9,20,c.Wood ?? generic),
            ("Wood_Center_01","Дерево",THMapObject.ObjectType.WoodResource,22,15,c.Wood ?? generic),
            ("Stone_East_01","Камень",THMapObject.ObjectType.StoneResource,38,18,c.Stone ?? generic),
            ("Stone_East_02","Камень",THMapObject.ObjectType.StoneResource,40,14,c.Stone ?? generic),
            ("Stone_East_03","Камень",THMapObject.ObjectType.StoneResource,36,16,c.Stone ?? generic),
            ("Mana_North_01","Мана",THMapObject.ObjectType.ManaResource,28,27,c.Mana ?? generic),
            ("Mana_North_02","Мана",THMapObject.ObjectType.ManaResource,22,25,c.Mana ?? generic),
            ("Chest_Forest","Сундук",THMapObject.ObjectType.Treasure,6,16,c.Chest ?? generic),
            ("Chest_East_01","Сундук",THMapObject.ObjectType.Treasure,41,20,c.Chest ?? generic),
            ("Artifact_Forest","Артефакт",THMapObject.ObjectType.Artifact,4,22,c.Artifact ?? generic),
        };
        foreach (var r in list) PlaceResource(root, r.id, r.n, r.t, r.x, r.y, r.s);
    }

    private static void PlaceResource(GameObject root, string id, string display, THMapObject.ObjectType type, int x, int y, Sprite sp)
    {
        var go = new GameObject(id);
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, y, -0.2f);
        var mo = go.AddComponent<THMapObject>();
        mo.id = id; mo.type = type; mo.displayName = display; mo.targetX = x; mo.targetY = y;
        mo.blocksMovement = type == THMapObject.ObjectType.Treasure || type == THMapObject.ObjectType.Artifact;
        if (type == THMapObject.ObjectType.Artifact) go.AddComponent<THArtifact>();
        else { var res = go.AddComponent<THResource>(); res.resourceType = type.ToString(); }
        if (sp != null && !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sp))
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(go, sp, 0.75f, 40);
        go.AddComponent<BoxCollider2D>();
    }

    private static void PlaceEnemies(GameObject root, Catalog c)
    {
        Sprite tinyMonster = TheHeroMainAssetsMapUtil.PickByName(c.TinyAll, "goblin", "torch", "tnt", "barbarian");
        Sprite tinyBoss = TheHeroMainAssetsMapUtil.PickByName(c.TinyAll, "boss", "king", "demon", "shaman");

        PlaceEnemy(root, "Enemy_Wolf_Start", "Проклятые волки", 19, 14, c.Wolf ?? tinyMonster, THEnemyDifficulty.Weak, false);
        PlaceEnemy(root, "Enemy_Wolf_West", "Волки", 8, 18, c.Wolf ?? tinyMonster, THEnemyDifficulty.Weak, false);
        PlaceEnemy(root, "Enemy_Skeleton_Forest", "Скелеты", 6, 22, c.Skeleton ?? tinyMonster, THEnemyDifficulty.Medium, false);
        PlaceEnemy(root, "Enemy_Skeleton_Ruins", "Скелеты", 36, 14, c.Skeleton ?? tinyMonster, THEnemyDifficulty.Medium, false);
        PlaceEnemy(root, "Enemy_BloodGargoyle_East", "Кровавая гаргулья", 40, 20, c.BloodGargoyle ?? c.DarkTroll ?? tinyMonster, THEnemyDifficulty.Strong, false);
        PlaceEnemy(root, "Enemy_SkeletonMage_North", "Маг-скелет", 24, 28, c.SkeletonMage ?? tinyMonster, THEnemyDifficulty.Strong, false);
        PlaceEnemy(root, "Enemy_DarkTroll_Guard", "Тёмный тролль", 28, 26, c.DarkTroll ?? tinyMonster, THEnemyDifficulty.Strong, false);
        PlaceEnemy(root, "Enemy_DarkLord_Final", "Тёмный Лорд", 24, 30, c.UnderworldKing ?? tinyBoss ?? tinyMonster, THEnemyDifficulty.Deadly, true);
    }

    private static void PlaceEnemy(GameObject root, string id, string display, int x, int y, Sprite sp, THEnemyDifficulty d, bool boss)
    {
        var go = new GameObject(id);
        go.transform.SetParent(root.transform, false);
        go.transform.position = new Vector3(x, y, -0.2f);
        var mo = go.AddComponent<THMapObject>();
        mo.id = id; mo.type = THMapObject.ObjectType.Enemy; mo.displayName = display;
        mo.targetX = x; mo.targetY = y; mo.difficulty = d;
        mo.startsCombat = true; mo.blocksMovement = true;
        mo.isFinalBoss = boss; mo.isDarkLord = boss;
        go.AddComponent<THEnemy>();
        go.AddComponent<BoxCollider2D>();
        if (sp != null && !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(sp))
            TheHeroMainAssetsMapUtil.ApplyObjectSprite(go, sp, boss ? 1.4f : 1f, boss ? 55 : 45);
    }

    private static void EnsureSystems(GameObject castle, GameObject hero)
    {
        var ctrl = GameObject.Find("MapController") ?? new GameObject("MapController");
        if (ctrl.GetComponent<THMapController>() == null) ctrl.AddComponent<THMapController>();
        if (ctrl.GetComponent<THMapGridInput>() == null) ctrl.AddComponent<THMapGridInput>();

        var bounds = GameObject.Find("MapBounds") ?? new GameObject("MapBounds");
        var b = bounds.GetComponent<THMapBounds>() ?? bounds.AddComponent<THMapBounds>();
        b.minX = 0; b.minY = 0; b.maxX = W - 1; b.maxY = H - 1; b.initialized = true;

        Camera cam = Camera.main ?? Object.FindAnyObjectByType<Camera>();
        if (cam != null && hero != null)
        {
            var follow = cam.GetComponent<THCameraFollow>() ?? cam.gameObject.AddComponent<THCameraFollow>();
            follow.Target = hero.transform;
            cam.orthographic = true; cam.orthographicSize = 7.5f;
        }
        var controller = ctrl.GetComponent<THMapController>();
        if (controller != null && hero != null)
            controller.HeroMover = hero.GetComponent<THStrictGridHeroMovement>();
    }

    // ── helpers ─────────────────────────────────────────────────────────────
    private static void Track(string label, Sprite sp)
    {
        if (sp == null) return;
        string p = AssetDatabase.GetAssetPath(sp);
        string source = "Other";
        if (p.IndexOf("MainAssets", StringComparison.OrdinalIgnoreCase) >= 0) source = "MainAssets";
        else if (p.IndexOf("Cainos", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("Pixel Art Top Down", StringComparison.OrdinalIgnoreCase) >= 0) source = "Cainos";
        else if (p.IndexOf("Tiny Sword", StringComparison.OrdinalIgnoreCase) >= 0 || p.IndexOf("TinySwords", StringComparison.OrdinalIgnoreCase) >= 0) source = "TinySwords";
        _provenance[label] = $"{source} :: {sp.name} ({Path.GetFileName(p)})";
    }

    private static void ClearOldMapContent()
    {
        var scene = SceneManager.GetActiveScene();
        var keep = new HashSet<string> { "Main Camera", "EventSystem", "Canvas", "MapController",
            "TH_Bootstrap", "MapBounds", "MapHoverLabelController" };
        var del = new List<GameObject>();
        foreach (var go in scene.GetRootGameObjects())
        {
            if (keep.Contains(go.name)) continue;
            string n = go.name;
            if (n == "MapRoot" || n == "Grid" || n == "WalkLogic" || n == "ObjectsRoot" ||
                n.EndsWith("Tilemap", StringComparison.Ordinal))
                del.Add(go);
        }
        foreach (var go in del) Object.DestroyImmediate(go);
        foreach (var t in Object.FindObjectsByType<THTile>(FindObjectsInactive.Include))
            if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static GameObject GetOrCreate(string n) => GameObject.Find(n) ?? new GameObject(n);

    private static GameObject GetOrCreateChild(GameObject parent, string n)
    {
        var t = parent.transform.Find(n);
        if (t != null) return t.gameObject;
        var go = new GameObject(n);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    private static void ClearChildren(GameObject go)
    {
        for (int i = go.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(go.transform.GetChild(i).gameObject);
    }

    private static GameObject EnsureGrid(GameObject parent)
    {
        var go = GetOrCreateChild(parent, "Grid");
        if (go.GetComponent<Grid>() == null) go.AddComponent<Grid>().cellSize = Vector3.one;
        return go;
    }

    private static Tilemap NewTilemap(GameObject grid, string name, int order)
    {
        var existing = grid.transform.Find(name);
        if (existing != null) Object.DestroyImmediate(existing.gameObject);
        var go = new GameObject(name);
        go.transform.SetParent(grid.transform, false);
        var tm = go.AddComponent<Tilemap>();
        var r = go.AddComponent<TilemapRenderer>();
        r.sortingOrder = order;
        return tm;
    }

    private static Tile MakeTile(Sprite sp)
    {
        var t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = sp;
        return t;
    }

    private static void WriteReport(Catalog c)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Map MainAssets + Cainos + TinySwords Fallback Report");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## Asset provenance (key picks)");
        foreach (var kv in _provenance) sb.AppendLine($"- **{kv.Key}**: {kv.Value}");
        sb.AppendLine();
        sb.AppendLine("## MainAssets sub-sprite pools");
        sb.AppendLine($"- Grass: {c.Grass.Count}, Floor: {c.Floor.Count}, Walls: {c.Walls.Count}, Interior: {c.Interior.Count}, Props: {c.Props.Count}, Plants: {c.Plants.Count}, Trees: {c.Trees.Count}, Houses: {c.Houses.Count}");
        sb.AppendLine($"- Water: {(c.Water != null ? "yes" : "no")}, Bridge: {(c.Bridge != null ? "yes" : "no")}");
        sb.AppendLine();
        sb.AppendLine("## Cainos fallback pool");
        sb.AppendLine($"- Terrain: {c.CainosTerrain.Count}, Props: {c.CainosProps.Count}, Buildings: {c.CainosBuildings.Count}");
        sb.AppendLine();
        sb.AppendLine("## Tiny Swords fallback pool");
        sb.AppendLine($"- Total sub-sprites scanned: {c.TinyAll.Count}");
        sb.AppendLine();
        sb.AppendLine("## Castle");
        sb.AppendLine($"- Castle_Player at ({CX},{CY}), composite of {c.CastleParts.Count} sub-sprite parts (Visual_House + walls + decor).");
        sb.AppendLine("## Hero");
        sb.AppendLine($"- Hero at ({HeroX},{HeroY}); CameraFollow.Target = Hero.");
        sb.AppendLine("## Mountains / dark zone");
        sb.AppendLine("- Not required. Northern boss area uses ruins floor + props.");
        sb.AppendLine();
        sb.AppendLine("## Missing (gracefully skipped)");
        if (_missing.Count == 0) sb.AppendLine("None.");
        else foreach (var m in _missing) sb.AppendLine($"- {m}");
        sb.AppendLine();
        sb.AppendLine("## Whole-sheet check");
        sb.AppendLine("All sprites loaded via `LoadSlicedSprites` / `IsWholeSheetSprite` filter. No whole PNG sheet is assigned to any object.");
        sb.AppendLine();
        sb.AppendLine("## Manual verification");
        sb.AppendLine("1. **The Hero → Map → Build Map MainAssets With Cainos Tiny Fallbacks**");
        sb.AppendLine("2. **The Hero → Validation → Validate Map MainAssets With Fallbacks**");
        sb.AppendLine("3. Play → MainMenu → New Game.");

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));
        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/Map_MainAssets_Cainos_TinyFallback_Report.md"), sb.ToString());
    }
}
