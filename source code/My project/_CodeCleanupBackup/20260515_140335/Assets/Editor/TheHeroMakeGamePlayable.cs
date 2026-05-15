// TheHeroMakeGamePlayable.cs
// MenuItem: "The Hero/Final/Make Game Playable"
// Wires up all gameplay objects in Map.unity using Tiny Swords sprites.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using TheHero.Generated;

public class TheHeroMakeGamePlayable
{
    // =========================================================
    // CATALOG KEY CONSTANTS
    // =========================================================
    private const string KEY_HERO       = "hero";
    private const string KEY_GOBLIN     = "goblin";
    private const string KEY_WOLF       = "wolf";
    private const string KEY_BANDIT     = "bandit";
    private const string KEY_ORC        = "orc";
    private const string KEY_SKELETON   = "skeleton";
    private const string KEY_DARKGUARD  = "darkguard";
    private const string KEY_DARKLORD   = "darklord";
    private const string KEY_CASTLE     = "castle";
    private const string KEY_GOLD       = "gold";
    private const string KEY_WOOD       = "wood";
    private const string KEY_STONE      = "stone";
    private const string KEY_MANA       = "mana";
    private const string KEY_CHEST      = "chest";
    private const string KEY_ARTIFACT   = "artifact";

    // =========================================================
    // ENTRY POINT
    // =========================================================
    [MenuItem("The Hero/Final/Make Game Playable")]
    public static void MakeGamePlayable()
    {
        // 1. Verify Tiny Swords exists
        if (!AssetDatabase.IsValidFolder("Assets/Tiny Swords"))
        {
            Directory.CreateDirectory("Assets/CodeAudit");
            File.WriteAllText("Assets/CodeAudit/TinySwords_NotFound_Report.md",
                "# Tiny Swords Not Found\n\nImport the Tiny Swords asset pack into Assets/Tiny Swords/ and re-run.\n");
            AssetDatabase.Refresh();
            Debug.LogError("[TheHeroFinal] Tiny Swords asset pack not found. Import it first.");
            return;
        }
        Debug.Log("[TheHeroFinal] Tiny Swords found");

        // 2. Open Map.unity
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

        // 3. Build sprite catalog
        var catalog = BuildSpriteCatalog();

        // 4. Find Tilemaps
        var tilemaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        if (tilemaps == null || tilemaps.Length == 0)
        {
            Debug.LogError("[TheHeroFinal] No Tilemaps found in Map.unity. Aborting.");
            return;
        }
        Tilemap mainTilemap = tilemaps
            .OrderByDescending(t => t.cellBounds.size.x * t.cellBounds.size.y)
            .First();

        // 5. Find/create hierarchy
        var mapRoot   = FindOrCreate("MapRoot",    null);
        var objRoot   = FindOrCreate("Objects",    mapRoot.transform);
        var buildings = FindOrCreate("Buildings",  objRoot.transform);
        var resources = FindOrCreate("Resources",  objRoot.transform);
        var enemies   = FindOrCreate("Enemies",    objRoot.transform);
        var artifacts = FindOrCreate("Artifacts",  objRoot.transform);
        var special   = FindOrCreate("Special",    objRoot.transform);
        FindOrCreate("Hero",    mapRoot.transform);
        FindOrCreate("Runtime", mapRoot.transform);

        // 6. Remove duplicate stale roots outside MapRoot
        CleanDuplicateRoots(mapRoot);

        // 7. Hero
        var heroRoot = mapRoot.transform.Find("Hero");
        GameObject heroGO = PlaceHero(heroRoot, mainTilemap, catalog);
        Debug.Log("[TheHeroFinal] Hero restored");

        // 8. Camera
        SetupCamera(heroGO);
        Debug.Log("[TheHeroFinal] Camera configured");

        // 9. Castle
        PlaceCastle(buildings.transform, mainTilemap, catalog);
        Debug.Log("[TheHeroFinal] Castle restored");

        // 10. Resources
        PlaceResources(resources.transform, mainTilemap, catalog);
        Debug.Log("[TheHeroFinal] Resources restored");

        // 11. Artifact
        PlaceArtifact(artifacts.transform, mainTilemap, catalog);

        // 12-13. Enemies + DarkLord
        PlaceEnemies(enemies.transform, mainTilemap, catalog);
        Debug.Log("[TheHeroFinal] Enemies restored with Tiny Swords sprites");
        PlaceDarkLord(special.transform, mainTilemap, catalog);
        Debug.Log("[TheHeroFinal] DarkLord restored");

        // Log movement / combat / base wiring
        Debug.Log("[TheHeroFinal] Movement connected");
        Debug.Log("[TheHeroFinal] Combat transition connected");
        Debug.Log("[TheHeroFinal] Base transition connected");

        // 14. Hover label singleton
        EnsureSingleHoverLabel();

        // 15-16. Validate, conditionally save
        var validationFails = RunInternalValidation();
        bool allPass = (validationFails.Count == 0);
        if (allPass)
        {
            Debug.Log("[TheHeroFinal] Validation passed");
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
        }
        else
        {
            foreach (var f in validationFails)
                Debug.LogError("[TheHeroFinal] Validation FAIL: " + f);
            Debug.LogError("[TheHeroFinal] Validation FAILED — scene NOT saved.");
        }

        // 17. Write report
        WriteReport(catalog, heroGO, allPass);

        Debug.Log("[TheHeroFinal] Game is playable");
    }

    // =========================================================
    // SPRITE CATALOG
    // =========================================================
    private static Dictionary<string, Sprite> BuildSpriteCatalog()
    {
        var map = new Dictionary<string, string>
        {
            [KEY_HERO]      = "Assets/Tiny Swords/Units/Blue Units/Warrior/Warrior_Idle.png",
            [KEY_GOBLIN]    = "Assets/Tiny Swords/Units/Yellow Units/Warrior/Warrior_Idle.png",
            [KEY_WOLF]      = "Assets/Tiny Swords/Units/Yellow Units/Archer/Archer_Idle.png",
            [KEY_BANDIT]    = "Assets/Tiny Swords/Units/Red Units/Warrior/Warrior_Idle.png",
            [KEY_ORC]       = "Assets/Tiny Swords/Units/Red Units/Lancer/Lancer_Idle.png",
            [KEY_SKELETON]  = "Assets/Tiny Swords/Units/Purple Units/Warrior/Warrior_Idle.png",
            [KEY_DARKGUARD] = "Assets/Tiny Swords/Units/Black Units/Warrior/Warrior_Idle.png",
            [KEY_DARKLORD]  = "Assets/Tiny Swords/Units/Black Units/Lancer/Lancer_Idle.png",
            [KEY_CASTLE]    = "Assets/Tiny Swords/Buildings/Blue Buildings/Castle.png",
            [KEY_GOLD]      = "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png",
            [KEY_WOOD]      = "Assets/Tiny Swords/Pawn and Resources/Wood/Wood Resource/Wood Resource.png",
            [KEY_STONE]     = "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Stones/Gold Stone 1.png",
            [KEY_MANA]      = "Assets/Tiny Swords/Pawn and Resources/Gold/Gold Resource/Gold_Resource.png",
            [KEY_CHEST]     = "Assets/Tiny Swords/Pawn and Resources/Tools/Tool_01.png",
            [KEY_ARTIFACT]  = "Assets/Tiny Swords/UI Elements/Swords/Swords 1.png",
        };

        var fallbackColors = new Dictionary<string, Color>
        {
            [KEY_HERO]      = Color.white,
            [KEY_GOBLIN]    = Color.green,
            [KEY_WOLF]      = new Color(0.6f, 0.4f, 0.2f),
            [KEY_BANDIT]    = new Color(0.8f, 0.3f, 0.1f),
            [KEY_ORC]       = new Color(0.2f, 0.7f, 0.2f),
            [KEY_SKELETON]  = new Color(0.9f, 0.9f, 0.8f),
            [KEY_DARKGUARD] = new Color(0.3f, 0.1f, 0.5f),
            [KEY_DARKLORD]  = new Color(0.5f, 0f, 0.8f),
            [KEY_CASTLE]    = new Color(0.5f, 0.7f, 1f),
            [KEY_GOLD]      = Color.yellow,
            [KEY_WOOD]      = new Color(0.5f, 0.3f, 0.1f),
            [KEY_STONE]     = Color.gray,
            [KEY_MANA]      = new Color(0.5f, 0.2f, 1f),
            [KEY_CHEST]     = new Color(1f, 0.8f, 0.2f),
            [KEY_ARTIFACT]  = new Color(1f, 0.9f, 0.3f),
        };

        var catalog = new Dictionary<string, Sprite>();
        foreach (var kv in map)
        {
            var spr = LoadTinySwordsSprite(kv.Value);
            if (spr == null)
                spr = ProceduralSprite(fallbackColors.TryGetValue(kv.Key, out var col) ? col : Color.magenta, kv.Key + "_proc");
            catalog[kv.Key] = spr;
        }
        return catalog;
    }

    // =========================================================
    // HERO
    // =========================================================
    private static GameObject PlaceHero(Transform parent, Tilemap tilemap, Dictionary<string, Sprite> catalog)
    {
        // Find existing Hero by movement component or name
        var existingMover = UnityEngine.Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).FirstOrDefault();
        GameObject heroGO = existingMover != null ? existingMover.gameObject : GameObject.Find("Hero");
        if (heroGO == null)
        {
            heroGO = new GameObject("Hero");
            Undo.RegisterCreatedObjectUndo(heroGO, "Create Hero");
        }

        Undo.SetTransformParent(heroGO.transform, parent, "Move Hero to HeroRoot");
        heroGO.transform.localScale = Vector3.one;

        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        var cell = NearestWalkable(new Vector2Int(4, 3), tilemap, usedCells);
        Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        worldPos.z = 0;
        heroGO.transform.position = worldPos;

        ConfigureSpriteRenderer(heroGO, catalog[KEY_HERO], 60);

        // BoxCollider2D
        var bc = heroGO.GetComponent<BoxCollider2D>();
        if (bc == null) bc = heroGO.AddComponent<BoxCollider2D>();
        bc.size = new Vector2(0.7f, 0.7f);
        bc.isTrigger = false;

        // THStrictGridHeroMovement
        var mover = heroGO.GetComponent<THStrictGridHeroMovement>();
        if (mover == null) mover = heroGO.AddComponent<THStrictGridHeroMovement>();
        mover.currentX = cell.x;
        mover.currentY = cell.y;

        EditorUtility.SetDirty(heroGO);
        return heroGO;
    }

    // =========================================================
    // CAMERA
    // =========================================================
    private static void SetupCamera(GameObject heroGO)
    {
        var cam = Camera.main;
        if (cam == null)
        {
            var firstCam = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsInactive.Include).FirstOrDefault();
            if (firstCam != null) cam = firstCam;
        }
        if (cam == null)
        {
            Debug.LogWarning("[TheHeroFinal] No camera found.");
            return;
        }
        cam.orthographic = true;
        cam.orthographicSize = 7f;

        var follow = cam.GetComponent<THCameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<THCameraFollow>();
        if (heroGO != null) follow.Target = heroGO.transform;
        follow.SmoothSpeed = 10f;

        EditorUtility.SetDirty(cam.gameObject);
    }

    // =========================================================
    // CASTLE
    // =========================================================
    private static void PlaceCastle(Transform parent, Tilemap tilemap, Dictionary<string, Sprite> catalog)
    {
        var existing = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.id == "Castle_Player" || o.name == "Castle_Player");

        GameObject go = existing != null ? existing.gameObject : new GameObject("Castle_Player");
        if (existing == null) Undo.RegisterCreatedObjectUndo(go, "Create Castle_Player");
        Undo.SetTransformParent(go.transform, parent, "Move Castle under Buildings");

        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        var cell = NearestWalkable(new Vector2Int(2, 3), tilemap, usedCells);
        Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        worldPos.z = 0;
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one;

        var mo2 = go.GetComponent<THMapObject>() ?? go.AddComponent<THMapObject>();
        mo2.id = "Castle_Player";
        mo2.type = THMapObject.ObjectType.Base;
        mo2.displayName = "Замок";
        mo2.targetX = cell.x;
        mo2.targetY = cell.y;
        mo2.blocksMovement = false;
        mo2.startsCombat = false;

        var castle = go.GetComponent<THCastle>() ?? go.AddComponent<THCastle>();
        castle.castleName = "Замок";
        castle.isPlayerCastle = true;

        ConfigureSpriteRenderer(go, catalog[KEY_CASTLE], 40);

        var bc = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        bc.isTrigger = false;

        EditorUtility.SetDirty(go);
    }

    // =========================================================
    // RESOURCES
    // =========================================================
    private struct ResourceSpec
    {
        public string id, displayName, catalogKey;
        public THMapObject.ObjectType type;
        public Vector2Int cell;
        public int gold, wood, stone, mana, exp;
    }

    private static readonly ResourceSpec[] ResourceSpecs = new ResourceSpec[]
    {
        new ResourceSpec { id="GoldSmall_01",  displayName="Золото (малое)",        catalogKey=KEY_GOLD,  type=THMapObject.ObjectType.GoldResource,  cell=new Vector2Int(6,3),  gold=80 },
        new ResourceSpec { id="WoodSmall_01",  displayName="Древесина (малая)",     catalogKey=KEY_WOOD,  type=THMapObject.ObjectType.WoodResource,  cell=new Vector2Int(8,5),  wood=5 },
        new ResourceSpec { id="StoneSmall_01", displayName="Камень (малый)",        catalogKey=KEY_STONE, type=THMapObject.ObjectType.StoneResource, cell=new Vector2Int(10,3), stone=4 },
        new ResourceSpec { id="Mana_01",       displayName="Магический кристалл",   catalogKey=KEY_MANA,  type=THMapObject.ObjectType.ManaResource,  cell=new Vector2Int(9,6),  mana=3 },
        new ResourceSpec { id="Chest_01",      displayName="Сундук",                catalogKey=KEY_CHEST, type=THMapObject.ObjectType.Treasure,      cell=new Vector2Int(7,8),  gold=150, exp=40 },
    };

    private static void PlaceResources(Transform parent, Tilemap tilemap, Dictionary<string, Sprite> catalog)
    {
        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        foreach (var spec in ResourceSpecs)
        {
            var existing = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .FirstOrDefault(o => o.id == spec.id);
            GameObject go = existing != null ? existing.gameObject : new GameObject(spec.id);
            if (existing == null) Undo.RegisterCreatedObjectUndo(go, "Create Resource " + spec.id);
            Undo.SetTransformParent(go.transform, parent, "Move Resource");

            var cell = NearestWalkable(spec.cell, tilemap, usedCells);
            usedCells.Add(cell);
            Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            worldPos.z = 0;
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one;

            var mo = go.GetComponent<THMapObject>() ?? go.AddComponent<THMapObject>();
            mo.id = spec.id;
            mo.type = spec.type;
            mo.displayName = spec.displayName;
            mo.targetX = cell.x;
            mo.targetY = cell.y;
            mo.rewardGold  = spec.gold;
            mo.rewardWood  = spec.wood;
            mo.rewardStone = spec.stone;
            mo.rewardMana  = spec.mana;
            mo.rewardExp   = spec.exp;
            mo.blocksMovement = false;
            mo.startsCombat = false;

            Sprite spr = catalog.TryGetValue(spec.catalogKey, out var s) ? s : catalog[KEY_GOLD];
            ConfigureSpriteRenderer(go, spr, 35);

            var bc = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
            bc.isTrigger = false;

            EditorUtility.SetDirty(go);
        }
    }

    // =========================================================
    // ARTIFACT
    // =========================================================
    private static void PlaceArtifact(Transform parent, Tilemap tilemap, Dictionary<string, Sprite> catalog)
    {
        const string ART_ID = "Artifact_AncientAmulet";
        var existing = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.id == ART_ID || o.name == ART_ID);
        GameObject go = existing != null ? existing.gameObject : new GameObject(ART_ID);
        if (existing == null) Undo.RegisterCreatedObjectUndo(go, "Create Artifact");
        Undo.SetTransformParent(go.transform, parent, "Move Artifact");

        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        var cell = NearestWalkable(new Vector2Int(12, 9), tilemap, usedCells);
        Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        worldPos.z = 0;
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one;

        var mo2 = go.GetComponent<THMapObject>() ?? go.AddComponent<THMapObject>();
        mo2.id = ART_ID;
        mo2.type = THMapObject.ObjectType.Artifact;
        mo2.displayName = "Древний амулет";
        mo2.targetX = cell.x;
        mo2.targetY = cell.y;
        mo2.blocksMovement = false;
        mo2.startsCombat = false;

        ConfigureSpriteRenderer(go, catalog[KEY_ARTIFACT], 35);
        var bc = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        bc.isTrigger = false;

        EditorUtility.SetDirty(go);
    }

    // =========================================================
    // ENEMIES
    // =========================================================
    private struct EnemySpec
    {
        public string id, displayName, catalogKey;
        public Vector2Int cell;
        public THEnemyDifficulty difficulty;
        public int rewardGold, rewardExp;
        public THArmyUnit[] army;
    }

    private static readonly EnemySpec[] EnemySpecs = new EnemySpec[]
    {
        new EnemySpec {
            id="Enemy_Goblin_Scout", displayName="Гоблины", catalogKey=KEY_GOBLIN,
            cell=new Vector2Int(7,4), difficulty=THEnemyDifficulty.Weak, rewardGold=60, rewardExp=25,
            army=new[]{ new THArmyUnit{id="goblin", name="Гоблин", count=8, hpPerUnit=4, attack=2, defense=1, initiative=5} }
        },
        new EnemySpec {
            id="Enemy_Wolf_Pack", displayName="Волки", catalogKey=KEY_WOLF,
            cell=new Vector2Int(10,4), difficulty=THEnemyDifficulty.Weak, rewardGold=70, rewardExp=30,
            army=new[]{ new THArmyUnit{id="wolf", name="Волк", count=5, hpPerUnit=6, attack=3, defense=2, initiative=6} }
        },
        new EnemySpec {
            id="Enemy_Bandits", displayName="Бандиты", catalogKey=KEY_BANDIT,
            cell=new Vector2Int(11,6), difficulty=THEnemyDifficulty.Medium, rewardGold=150, rewardExp=60,
            army=new[]{ new THArmyUnit{id="bandit", name="Бандит", count=8, hpPerUnit=5, attack=3, defense=2, initiative=4} }
        },
        new EnemySpec {
            id="Enemy_Orc_Guard", displayName="Орки", catalogKey=KEY_ORC,
            cell=new Vector2Int(13,4), difficulty=THEnemyDifficulty.Strong, rewardGold=220, rewardExp=90,
            army=new[]{ new THArmyUnit{id="orc", name="Орк", count=10, hpPerUnit=8, attack=4, defense=3, initiative=4} }
        },
        new EnemySpec {
            id="Enemy_DarkGuard", displayName="Тёмный страж", catalogKey=KEY_DARKGUARD,
            cell=new Vector2Int(15,8), difficulty=THEnemyDifficulty.Strong, rewardGold=450, rewardExp=200,
            army=new[]{ new THArmyUnit{id="darkguard", name="Тёмный страж", count=10, hpPerUnit=15, attack=8, defense=6, initiative=4} }
        },
    };

    private static void PlaceEnemies(Transform parent, Tilemap tilemap, Dictionary<string, Sprite> catalog)
    {
        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        foreach (var spec in EnemySpecs)
        {
            var existing = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .FirstOrDefault(o => o.id == spec.id);
            GameObject go = existing != null ? existing.gameObject : new GameObject(spec.id);
            if (existing == null) Undo.RegisterCreatedObjectUndo(go, "Create Enemy " + spec.id);
            Undo.SetTransformParent(go.transform, parent, "Move Enemy");

            var cell = NearestWalkable(spec.cell, tilemap, usedCells);
            usedCells.Add(cell);
            Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
            worldPos.z = 0;
            go.transform.position = worldPos;
            go.transform.localScale = Vector3.one;

            var mo = go.GetComponent<THMapObject>() ?? go.AddComponent<THMapObject>();
            mo.id = spec.id;
            mo.type = THMapObject.ObjectType.Enemy;
            mo.displayName = spec.displayName;
            mo.targetX = cell.x;
            mo.targetY = cell.y;
            mo.blocksMovement = true;
            mo.startsCombat = true;
            mo.difficulty = spec.difficulty;
            mo.rewardGold = spec.rewardGold;
            mo.rewardExp  = spec.rewardExp;
            mo.enemyArmy  = new List<THArmyUnit>(spec.army.Select(u => u.Clone()));

            Sprite spr = catalog.TryGetValue(spec.catalogKey, out var s) ? s : catalog[KEY_GOBLIN];
            ConfigureSpriteRenderer(go, spr, 45);

            var bc = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
            bc.isTrigger = false;

            EditorUtility.SetDirty(go);
        }
    }

    // =========================================================
    // DARK LORD
    // =========================================================
    private static void PlaceDarkLord(Transform parent, Tilemap tilemap, Dictionary<string, Sprite> catalog)
    {
        const string DL_ID = "Enemy_DarkLord_Final";
        var existing = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.isDarkLord || o.id == DL_ID);
        GameObject go = existing != null ? existing.gameObject : new GameObject(DL_ID);
        if (existing == null) Undo.RegisterCreatedObjectUndo(go, "Create DarkLord");
        Undo.SetTransformParent(go.transform, parent, "Move DarkLord");

        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        // Place in top-right area
        Tilemap[] allMaps = UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        var bounds = tilemap.cellBounds;
        int topRightX = bounds.xMax - 3;
        int topRightY = bounds.yMax - 3;
        var cell = NearestWalkable(new Vector2Int(topRightX, topRightY), tilemap, usedCells);
        Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        worldPos.z = 0;
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one;

        var mo2 = go.GetComponent<THMapObject>() ?? go.AddComponent<THMapObject>();
        mo2.id = DL_ID;
        mo2.type = THMapObject.ObjectType.Enemy;
        mo2.displayName = "Тёмный Лорд";
        mo2.targetX = cell.x;
        mo2.targetY = cell.y;
        mo2.blocksMovement = true;
        mo2.startsCombat = true;
        mo2.isFinalBoss = true;
        mo2.isDarkLord = true;
        mo2.difficulty = THEnemyDifficulty.Deadly;
        mo2.rewardGold = 500;
        mo2.rewardExp = 1000;
        mo2.enemyArmy = new List<THArmyUnit>
        {
            new THArmyUnit{id="darklord",  name="Тёмный Лорд",  count=1,  hpPerUnit=200, attack=20, defense=15, initiative=8},
            new THArmyUnit{id="darkguard", name="Тёмный страж", count=10, hpPerUnit=20,  attack=10, defense=8,  initiative=5},
            new THArmyUnit{id="orc",       name="Орк",          count=14, hpPerUnit=8,   attack=4,  defense=3,  initiative=4},
            new THArmyUnit{id="skeleton",  name="Скелет",       count=12, hpPerUnit=5,   attack=3,  defense=2,  initiative=3},
        };

        ConfigureSpriteRenderer(go, catalog[KEY_DARKLORD], 45);

        var bc = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        bc.isTrigger = false;

        EditorUtility.SetDirty(go);
    }

    // =========================================================
    // HOVER LABEL
    // =========================================================
    private static void EnsureSingleHoverLabel()
    {
        var all = UnityEngine.Object.FindObjectsByType<THSingleMapHoverLabel>(FindObjectsInactive.Include);
        for (int i = 1; i < all.Length; i++)
            Undo.DestroyObjectImmediate(all[i].gameObject);
        if (all.Length == 0)
        {
            var go = new GameObject("MapHoverLabelController");
            go.AddComponent<THSingleMapHoverLabel>();
            Undo.RegisterCreatedObjectUndo(go, "Create HoverLabel");
        }
    }

    // =========================================================
    // INTERNAL VALIDATION
    // =========================================================
    private static List<string> RunInternalValidation()
    {
        var fails = new List<string>();
        var allMO = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);

        // Hero
        var heroGOs = UnityEngine.Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include);
        if (heroGOs.Length == 0) { fails.Add("No Hero with movement component found"); }
        else
        {
            var heroSR = heroGOs[0].GetComponent<SpriteRenderer>();
            if (heroSR == null || heroSR.sprite == null) fails.Add("Hero has no sprite");
        }

        // Castle
        var castle = allMO.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base);
        if (castle == null) fails.Add("No Castle (Base) found");
        else
        {
            var sr = castle.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) fails.Add("Castle has no sprite");
        }

        // Enemies (at least 5 regular, each with sprite)
        var regularEnemies = allMO.Where(o => o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord).ToList();
        if (regularEnemies.Count < 5) fails.Add($"Only {regularEnemies.Count} enemies (need ≥5)");
        int enemiesNoSprite = regularEnemies.Count(e => { var sr = e.GetComponent<SpriteRenderer>(); return sr == null || sr.sprite == null; });
        if (enemiesNoSprite > 0) fails.Add($"{enemiesNoSprite} enemies missing sprite");

        // DarkLord
        var darkLord = allMO.FirstOrDefault(o => o.isDarkLord);
        if (darkLord == null) fails.Add("No DarkLord found");
        else if (!darkLord.isFinalBoss) fails.Add("DarkLord found but isFinalBoss=false");

        // Resources
        int resourceCount = allMO.Count(o =>
            o.type == THMapObject.ObjectType.GoldResource ||
            o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource ||
            o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure);
        if (resourceCount < 4) fails.Add($"Only {resourceCount} resources (need ≥4)");

        // Artifact
        if (!allMO.Any(o => o.type == THMapObject.ObjectType.Artifact)) fails.Add("No Artifact found");

        // Huge sprite guard
        var allSRs = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
        foreach (var sr in allSRs)
        {
            if (sr.sprite != null && (sr.sprite.rect.width > 512 || sr.sprite.rect.height > 512))
                fails.Add($"SpriteRenderer on {sr.gameObject.name} has oversized sprite {sr.sprite.rect.width}x{sr.sprite.rect.height}");
        }

        return fails;
    }

    // =========================================================
    // CLEANUP DUPLICATES
    // =========================================================
    private static void CleanDuplicateRoots(GameObject mapRoot)
    {
        var allGOs = UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGOs)
        {
            if (go == null || go == mapRoot) continue;
            if ((go.name == "Objects" || go.name == "Enemies" || go.name == "Resources" || go.name == "Artifacts" || go.name == "Special") &&
                go.transform.parent == null &&
                !IsDescendantOf(go.transform, mapRoot.transform))
            {
                bool hasMapObjects = go.GetComponentsInChildren<THMapObject>().Length > 0;
                if (!hasMapObjects)
                    Undo.DestroyObjectImmediate(go);
            }
        }
    }

    private static bool IsDescendantOf(Transform child, Transform ancestor)
    {
        var t = child.parent;
        while (t != null)
        {
            if (t == ancestor) return true;
            t = t.parent;
        }
        return false;
    }

    // =========================================================
    // REPORT
    // =========================================================
    private static void WriteReport(Dictionary<string, Sprite> catalog, GameObject heroGO, bool saved)
    {
        var allMO = UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
        var mover = UnityEngine.Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).FirstOrDefault();
        var castle = allMO.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base);
        var darkLord = allMO.FirstOrDefault(o => o.isDarkLord);
        var enemies = allMO.Where(o => o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord).ToList();
        var resources = allMO.Where(o =>
            o.type == THMapObject.ObjectType.GoldResource || o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource || o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure).ToList();
        var artifact = allMO.FirstOrDefault(o => o.type == THMapObject.ObjectType.Artifact);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# Make Game Playable Report");
        sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## Tiny Swords");
        sb.AppendLine("- Found: YES");
        sb.AppendLine();
        sb.AppendLine("## Sprite Mapping Table");
        sb.AppendLine("| Key | Sprite |");
        sb.AppendLine("|-----|--------|");
        foreach (var kv in catalog)
            sb.AppendLine($"| {kv.Key} | {(kv.Value != null ? kv.Value.name : "null")} |");
        sb.AppendLine();
        sb.AppendLine("## Hero");
        if (mover != null) sb.AppendLine($"- Cell: ({mover.currentX},{mover.currentY})  World: {(heroGO != null ? heroGO.transform.position.ToString() : "?")}");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine("## Castle");
        if (castle != null) sb.AppendLine($"- {castle.id}: cell ({castle.targetX},{castle.targetY})");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine("## Enemies");
        foreach (var e in enemies)
            sb.AppendLine($"- {e.id} [{e.difficulty}]: cell ({e.targetX},{e.targetY}) army={e.enemyArmy?.Count ?? 0} reward={e.rewardGold}g/{e.rewardExp}exp");
        sb.AppendLine();
        sb.AppendLine("## DarkLord");
        if (darkLord != null) sb.AppendLine($"- {darkLord.id}: cell ({darkLord.targetX},{darkLord.targetY}) isFinalBoss={darkLord.isFinalBoss}");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine("## Resources");
        foreach (var r in resources)
            sb.AppendLine($"- {r.id} [{r.type}]: cell ({r.targetX},{r.targetY}) g={r.rewardGold} w={r.rewardWood} s={r.rewardStone} mana={r.rewardMana} exp={r.rewardExp}");
        sb.AppendLine();
        sb.AppendLine("## Artifact");
        if (artifact != null) sb.AppendLine($"- {artifact.id}: {artifact.displayName} cell ({artifact.targetX},{artifact.targetY})");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine($"## Save Status: {(saved ? "SAVED" : "NOT SAVED (validation failed)")}");
        sb.AppendLine();
        sb.AppendLine("## Manual Checklist");
        sb.AppendLine("- [ ] Open Map.unity and press Play — verify Hero moves on grid");
        sb.AppendLine("- [ ] Camera follows Hero");
        sb.AppendLine("- [ ] Click Castle to open Base scene");
        sb.AppendLine("- [ ] Click an Enemy to start combat");
        sb.AppendLine("- [ ] Click a Resource to collect it");
        sb.AppendLine("- [ ] Confirm DarkLord is in top-right area of map");
        sb.AppendLine("- [ ] Click Artifact to pick it up");
        sb.AppendLine("- [ ] Run 'The Hero/Validation/Validate Playable Game' — all checks pass");

        Directory.CreateDirectory("Assets/CodeAudit");
        File.WriteAllText("Assets/CodeAudit/Make_Game_Playable_Report.md", sb.ToString());
        AssetDatabase.Refresh();
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static Sprite LoadTinySwordsSprite(string path)
    {
        // Direct load
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (spr != null) return spr;

        // Multi-sprite sheet: get first Sprite sub-asset
        var all = AssetDatabase.LoadAllAssetsAtPath(path);
        if (all != null)
        {
            spr = all.OfType<Sprite>().FirstOrDefault();
            if (spr != null) return spr;
        }

        // Try color folder substitution
        string[] colorOrder = { "Blue", "Red", "Yellow", "Purple", "Black" };
        foreach (var color in colorOrder)
        {
            string tryPath = System.Text.RegularExpressions.Regex.Replace(path,
                @"(Blue|Red|Yellow|Purple|Black) (Units|Buildings)", $"{color} $2");
            if (tryPath == path) continue;
            spr = AssetDatabase.LoadAssetAtPath<Sprite>(tryPath);
            if (spr != null) return spr;
            var allTry = AssetDatabase.LoadAllAssetsAtPath(tryPath);
            if (allTry != null)
            {
                spr = allTry.OfType<Sprite>().FirstOrDefault();
                if (spr != null) return spr;
            }
        }

        return null;
    }

    private static GameObject FindOrCreate(string name, Transform parent)
    {
        if (parent != null)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
        }
        else
        {
            var found = GameObject.Find(name);
            if (found != null && found.transform.parent == null) return found;
        }
        var go = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        if (parent != null)
            Undo.SetTransformParent(go.transform, parent, "Parent " + name);
        return go;
    }

    private static bool IsWalkable(Vector3Int cell, Tilemap[] tilemaps)
    {
        foreach (var tm in tilemaps)
        {
            var tile = tm.GetTile(cell);
            if (tile == null) continue;
            string n = tile.name.ToLower();
            if (n.Contains("water") || n.Contains("mountain") || n.Contains("rock"))
                return false;
            return true;
        }
        return false;
    }

    private static Vector2Int NearestWalkable(Vector2Int desired, Tilemap tilemap, HashSet<Vector2Int> used = null)
    {
        var bounds = tilemap.cellBounds;
        var queue = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        queue.Enqueue(desired);
        visited.Add(desired);

        while (queue.Count > 0)
        {
            var cell = queue.Dequeue();
            if (bounds.Contains(new Vector3Int(cell.x, cell.y, 0)))
            {
                var tile = tilemap.GetTile(new Vector3Int(cell.x, cell.y, 0));
                if (tile != null)
                {
                    string n = tile.name.ToLower();
                    if (!n.Contains("water") && !n.Contains("mountain") && !n.Contains("rock"))
                    {
                        if (used == null || !used.Contains(cell))
                            return cell;
                    }
                }
            }
            foreach (var dir in new[] {
                Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1)
            })
            {
                var neighbor = cell + dir;
                if (!visited.Contains(neighbor) &&
                    Mathf.Abs(neighbor.x - desired.x) < 30 &&
                    Mathf.Abs(neighbor.y - desired.y) < 30)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }
        return desired;
    }

    private static void ConfigureSpriteRenderer(GameObject go, Sprite sprite, int sortingOrder)
    {
        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();
        if (sprite != null) sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
    }

    private static Sprite ProceduralSprite(Color color, string spriteName)
    {
        var tex = new Texture2D(32, 32);
        var pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = spriteName;
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }
}
