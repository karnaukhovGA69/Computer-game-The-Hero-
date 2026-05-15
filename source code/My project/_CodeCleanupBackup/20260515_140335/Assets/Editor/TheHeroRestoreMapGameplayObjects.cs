// TheHeroRestoreMapGameplayObjects.cs
// MenuItem: "The Hero/Map/Restore Gameplay Objects On Tilemap"
// Restores all gameplay objects (Hero, Castle, Resources, Enemies, Artifact, DarkLord)
// on top of the existing Tilemap in Map.unity. Does NOT rebuild terrain.

using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TheHero.Generated;

public class TheHeroRestoreMapGameplayObjects
{
    [MenuItem("The Hero/Map/Restore Gameplay Objects On Tilemap")]
    public static void RestoreGameplayObjects()
    {
        // --- 1. Open Map scene ---
        EditorSceneManager.OpenScene("Assets/Scenes/Map.unity");

        // --- 2. Verify Tilemaps exist ---
        var tilemaps = Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include);
        if (tilemaps == null || tilemaps.Length == 0)
        {
            Debug.LogError("[TheHeroMapGameplay] No Tilemaps found in Map.unity. Aborting — DO NOT rebuild terrain.");
            return;
        }
        // Pick the largest tilemap (most cells) as the walkable reference
        Tilemap mainTilemap = tilemaps
            .OrderByDescending(t => t.cellBounds.size.x * t.cellBounds.size.y)
            .First();
        Debug.Log($"[TheHeroMapGameplay] Found {tilemaps.Length} tilemaps. Main: {mainTilemap.name}");

        // --- 3. Find or create hierarchy ---
        var mapRoot = FindOrCreate("MapRoot", null);
        var objRoot = FindOrCreate("Objects", mapRoot.transform);
        var buildings = FindOrCreate("Buildings", objRoot.transform);
        var resources = FindOrCreate("Resources", objRoot.transform);
        var enemies = FindOrCreate("Enemies", objRoot.transform);
        var artifacts = FindOrCreate("Artifacts", objRoot.transform);
        var special = FindOrCreate("Special", objRoot.transform);
        var heroRoot = FindOrCreate("Hero", mapRoot.transform);
        var runtimeRoot = FindOrCreate("Runtime", mapRoot.transform);

        // --- 4. Remove duplicate groups OUTSIDE MapRoot ---
        CleanDuplicateGroups(mapRoot);

        // --- 5. Hero ---
        GameObject heroGO = PlaceHero(heroRoot.transform, mainTilemap);

        // --- 6. Camera ---
        SetupCamera(heroGO);

        // --- 7. Castle_Player ---
        PlaceCastle(buildings.transform, mainTilemap);

        // --- 8. Resources ---
        PlaceResources(resources.transform, mainTilemap);

        // --- 9. Regular Enemies ---
        PlaceEnemies(enemies.transform, mainTilemap);

        // --- 10. DarkLord ---
        PlaceDarkLord(special.transform, mainTilemap);

        // --- 11. Artifact ---
        PlaceArtifact(artifacts.transform, mainTilemap);

        // --- 12. Hover label singleton ---
        EnsureSingleHoverLabel();

        // --- 13. Validate before saving ---
        if (!ValidateScene())
        {
            Debug.LogError("[TheHeroMapGameplay] Validation failed. Scene NOT saved. Fix errors above and re-run.");
            return;
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveOpenScenes();
        Debug.Log("[TheHeroMapGameplay] Map scene saved successfully.");

        // --- 14. Write report ---
        WriteReport();
    }

    // =========================================================
    // HERO
    // =========================================================
    private static GameObject PlaceHero(Transform parent, Tilemap tilemap)
    {
        // Try finding existing Hero
        var existingHero = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).FirstOrDefault();
        GameObject heroGO = existingHero != null ? existingHero.gameObject : null;

        if (heroGO == null)
        {
            // Try by name
            heroGO = GameObject.Find("Hero");
            if (heroGO == null)
            {
                heroGO = new GameObject("Hero");
                Undo.RegisterCreatedObjectUndo(heroGO, "Create Hero");
                Debug.Log("[TheHeroMapGameplay] Hero created.");
            }
        }

        Undo.SetTransformParent(heroGO.transform, parent, "Move Hero under Hero root");

        // Find walkable cell near (4,3)
        var desired = new Vector2Int(4, 3);
        var cell = GetWalkableCellNear(desired, tilemap);
        Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        worldPos.z = 0;
        heroGO.transform.position = worldPos;
        heroGO.transform.localScale = Vector3.one;

        // SpriteRenderer
        var sr = heroGO.GetComponent<SpriteRenderer>();
        if (sr == null) sr = heroGO.AddComponent<SpriteRenderer>();
        if (sr.sprite == null)
        {
            var heroSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_hero.png");
            sr.sprite = heroSprite != null ? heroSprite : MakePlaceholderSprite(Color.white, "HeroPlaceholder");
        }
        sr.sortingOrder = 50;

        // Collider
        var col = heroGO.GetComponent<CircleCollider2D>();
        if (col == null) col = heroGO.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        // THStrictGridHeroMovement
        var mover = heroGO.GetComponent<THStrictGridHeroMovement>();
        if (mover == null) mover = heroGO.AddComponent<THStrictGridHeroMovement>();
        mover.currentX = cell.x;
        mover.currentY = cell.y;

        EditorUtility.SetDirty(heroGO);
        Debug.Log($"[TheHeroMapGameplay] Hero restored at cell ({cell.x},{cell.y}) world {worldPos}");
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
            var camGO = GameObject.FindFirstObjectByType<Camera>()?.gameObject;
            if (camGO != null) cam = camGO.GetComponent<Camera>();
        }
        if (cam == null)
        {
            Debug.LogWarning("[TheHeroMapGameplay] No camera found for setup.");
            return;
        }

        cam.orthographic = true;
        if (cam.orthographicSize < 1f || cam.orthographicSize > 20f)
            cam.orthographicSize = 7f;

        var follow = cam.GetComponent<THCameraFollow>();
        if (follow == null) follow = cam.gameObject.AddComponent<THCameraFollow>();
        if (follow.Target == null && heroGO != null)
            follow.Target = heroGO.transform;
        follow.SmoothSpeed = 10f;

        EditorUtility.SetDirty(cam.gameObject);
    }

    // =========================================================
    // CASTLE
    // =========================================================
    private static void PlaceCastle(Transform parent, Tilemap tilemap)
    {
        // Check if Castle_Player already exists
        var existing = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.id == "Castle_Player" || o.name == "Castle_Player");

        GameObject go;
        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = new GameObject("Castle_Player");
            Undo.RegisterCreatedObjectUndo(go, "Create Castle_Player");
        }
        Undo.SetTransformParent(go.transform, parent, "Move Castle under Buildings");

        var cell = GetWalkableCellNear(new Vector2Int(2, 3), tilemap);
        Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        worldPos.z = 0;
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one;

        var mo = go.GetComponent<THMapObject>() ?? go.AddComponent<THMapObject>();
        mo.id = "Castle_Player";
        mo.type = THMapObject.ObjectType.Base;
        mo.displayName = "Замок";
        mo.targetX = cell.x;
        mo.targetY = cell.y;
        mo.blocksMovement = false;
        mo.startsCombat = false;

        var castle = go.GetComponent<THCastle>() ?? go.AddComponent<THCastle>();
        castle.castleName = "Замок";
        castle.isPlayerCastle = true;

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_castle.png");
        sr.sprite = spr != null ? spr : MakePlaceholderSprite(new Color(0.6f, 0.6f, 1f), "CastlePlaceholder");
        sr.sortingOrder = 20;

        var bc = go.GetComponent<BoxCollider2D>() ?? go.AddComponent<BoxCollider2D>();
        bc.isTrigger = false;

        EditorUtility.SetDirty(go);
        Debug.Log($"[TheHeroMapGameplay] Castle_Player placed at cell ({cell.x},{cell.y})");
    }

    // =========================================================
    // RESOURCES (8 total)
    // =========================================================
    private static readonly ResourceSpec[] ResourceSpecs = new ResourceSpec[]
    {
        new ResourceSpec("GoldSmall_01",   "Золото (малое)",      THMapObject.ObjectType.GoldResource,  new Vector2Int(6,3),  80,  0,  0, 0,  0),
        new ResourceSpec("WoodSmall_01",   "Древесина (малая)",   THMapObject.ObjectType.WoodResource,  new Vector2Int(8,5),   0,  5,  0, 0,  0),
        new ResourceSpec("StoneSmall_01",  "Камень (малый)",      THMapObject.ObjectType.StoneResource, new Vector2Int(10,3),  0,  0,  4, 0,  0),
        new ResourceSpec("GoldMedium_01",  "Золото (среднее)",    THMapObject.ObjectType.GoldResource,  new Vector2Int(5,7), 150,  0,  0, 0,  0),
        new ResourceSpec("WoodMedium_01",  "Древесина (средняя)", THMapObject.ObjectType.WoodResource,  new Vector2Int(12,5),  0, 10,  0, 0,  0),
        new ResourceSpec("Chest_01",       "Сундук",              THMapObject.ObjectType.Treasure,      new Vector2Int(7,8), 150,  0,  0, 0, 40),
        new ResourceSpec("Mana_01",        "Магический кристалл", THMapObject.ObjectType.ManaResource,  new Vector2Int(9,6),   0,  0,  0, 3,  0),
        new ResourceSpec("Chest_02",       "Большой сундук",      THMapObject.ObjectType.Treasure,      new Vector2Int(14,7),250,  0,  0, 0, 80),
    };

    private struct ResourceSpec
    {
        public string id; public string displayName; public THMapObject.ObjectType type;
        public Vector2Int desiredCell; public int gold, wood, stone, mana, exp;
        public ResourceSpec(string id, string dn, THMapObject.ObjectType t, Vector2Int cell, int g, int w, int s, int m, int e)
        { this.id=id; displayName=dn; type=t; desiredCell=cell; gold=g; wood=w; stone=s; mana=m; exp=e; }
    }

    private static void PlaceResources(Transform parent, Tilemap tilemap)
    {
        var usedCells = new HashSet<Vector2Int>();

        // Collect cells already used by other objects
        foreach (var mo in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        foreach (var spec in ResourceSpecs)
        {
            // Check if already exists
            var existing = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .FirstOrDefault(o => o.id == spec.id);

            GameObject go;
            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(spec.id);
                Undo.RegisterCreatedObjectUndo(go, "Create Resource " + spec.id);
            }
            Undo.SetTransformParent(go.transform, parent, "Move Resource under Resources");

            var cell = GetWalkableCellNear(spec.desiredCell, tilemap, usedCells);
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
            mo.rewardGold = spec.gold;
            mo.rewardWood = spec.wood;
            mo.rewardStone = spec.stone;
            mo.rewardMana = spec.mana;
            mo.rewardExp = spec.exp;
            mo.blocksMovement = false;
            mo.startsCombat = false;

            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            sr.sprite = GetResourceSprite(spec.type, spec.id);
            sr.sortingOrder = 15;

            var col = go.GetComponent<CircleCollider2D>() ?? go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;
            col.isTrigger = false;

            EditorUtility.SetDirty(go);
            Debug.Log($"[TheHeroMapGameplay] Resource {spec.id} placed at ({cell.x},{cell.y})");
        }
    }

    private static Sprite GetResourceSprite(THMapObject.ObjectType type, string id)
    {
        string path = type switch
        {
            THMapObject.ObjectType.GoldResource  => "Assets/Resources/Sprites/CleanMap/Objects/clean_gold.png",
            THMapObject.ObjectType.WoodResource  => "Assets/Resources/Sprites/CleanMap/Objects/clean_wood.png",
            THMapObject.ObjectType.StoneResource => "Assets/Resources/Sprites/CleanMap/Objects/clean_stone.png",
            THMapObject.ObjectType.ManaResource  => "Assets/Resources/Sprites/CleanMap/Objects/clean_mana.png",
            THMapObject.ObjectType.Treasure      => "Assets/Resources/Sprites/CleanMap/Objects/clean_chest.png",
            _                                    => null
        };
        if (path != null)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (s != null) return s;
        }
        return MakePlaceholderSprite(Color.yellow, id + "_placeholder");
    }

    // =========================================================
    // ENEMIES (5)
    // =========================================================
    private struct EnemySpec
    {
        public string id, displayName;
        public Vector2Int desiredCell;
        public THEnemyDifficulty difficulty;
        public int rewardGold, rewardExp;
        public THArmyUnit[] army;
        public EnemySpec(string id, string dn, Vector2Int cell, THEnemyDifficulty diff, int g, int exp, THArmyUnit[] army)
        { this.id=id; displayName=dn; desiredCell=cell; difficulty=diff; rewardGold=g; rewardExp=exp; this.army=army; }
    }

    private static readonly EnemySpec[] EnemySpecs = new EnemySpec[]
    {
        new EnemySpec("Enemy_Goblin_Scout", "Гоблин-разведчик",   new Vector2Int(7,4),  THEnemyDifficulty.Weak,   30,  20,
            new[]{ new THArmyUnit{id="goblin",  name="Гоблин",  count=3, hpPerUnit=4, attack=2, defense=1, initiative=5} }),

        new EnemySpec("Enemy_Wolf_Pack",    "Стая волков",         new Vector2Int(10,4), THEnemyDifficulty.Weak,   40,  30,
            new[]{ new THArmyUnit{id="wolf",    name="Волк",    count=3, hpPerUnit=6, attack=3, defense=2, initiative=6} }),

        new EnemySpec("Enemy_Bandits",      "Бандиты",             new Vector2Int(11,6), THEnemyDifficulty.Medium, 80,  50,
            new[]{ new THArmyUnit{id="goblin",  name="Гоблин",  count=4, hpPerUnit=4, attack=2, defense=1, initiative=5},
                   new THArmyUnit{id="orc",     name="Орк",     count=2, hpPerUnit=8, attack=4, defense=3, initiative=4} }),

        new EnemySpec("Enemy_Orc_Guard",    "Орки-стражи",         new Vector2Int(13,4), THEnemyDifficulty.Strong, 120, 80,
            new[]{ new THArmyUnit{id="orc",     name="Орк",     count=5, hpPerUnit=8, attack=4, defense=3, initiative=4},
                   new THArmyUnit{id="skeleton", name="Скелет", count=4, hpPerUnit=5, attack=3, defense=2, initiative=3} }),

        new EnemySpec("Enemy_DarkGuard",    "Тёмные стражи",       new Vector2Int(15,8), THEnemyDifficulty.Deadly, 200, 150,
            new[]{ new THArmyUnit{id="darkknight", name="Тёмный рыцарь", count=4, hpPerUnit=15, attack=8, defense=6, initiative=4},
                   new THArmyUnit{id="skeleton",   name="Скелет",         count=6, hpPerUnit=5,  attack=3, defense=2, initiative=3} }),
    };

    private static void PlaceEnemies(Transform parent, Tilemap tilemap)
    {
        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        foreach (var spec in EnemySpecs)
        {
            var existing = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
                .FirstOrDefault(o => o.id == spec.id);

            GameObject go;
            if (existing != null) go = existing.gameObject;
            else
            {
                go = new GameObject(spec.id);
                Undo.RegisterCreatedObjectUndo(go, "Create Enemy " + spec.id);
            }
            Undo.SetTransformParent(go.transform, parent, "Move Enemy under Enemies");

            var cell = GetWalkableCellNear(spec.desiredCell, tilemap, usedCells);
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
            mo.rewardExp = spec.rewardExp;
            mo.enemyArmy = new List<THArmyUnit>(spec.army.Select(u => u.Clone()));

            var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
            sr.sprite = GetEnemySprite(spec.id);
            sr.sortingOrder = 25;

            var col = go.GetComponent<CircleCollider2D>() ?? go.AddComponent<CircleCollider2D>();
            col.radius = 0.45f;

            EditorUtility.SetDirty(go);
            Debug.Log($"[TheHeroMapGameplay] Enemy {spec.id} placed at ({cell.x},{cell.y})");
        }
    }

    private static Sprite GetEnemySprite(string id)
    {
        string path;
        if (id.Contains("Goblin")) path = "Assets/Resources/Sprites/CleanMap/Objects/clean_goblin.png";
        else if (id.Contains("Wolf")) path = "Assets/Resources/Sprites/CleanMap/Objects/clean_wolf.png";
        else if (id.Contains("Orc") || id.Contains("Bandit")) path = "Assets/Resources/Sprites/CleanMap/Objects/clean_orc.png";
        else path = "Assets/Resources/Sprites/CleanMap/Objects/clean_enemy_orc_map.png";

        var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return s != null ? s : MakePlaceholderSprite(Color.red, id + "_placeholder");
    }

    // =========================================================
    // DARK LORD
    // =========================================================
    private static void PlaceDarkLord(Transform parent, Tilemap tilemap)
    {
        const string DARK_LORD_ID = "dark_lord_final";
        var existing = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.isDarkLord || o.id == DARK_LORD_ID);

        GameObject go;
        if (existing != null) go = existing.gameObject;
        else
        {
            go = new GameObject("Enemy_DarkLord_Final");
            Undo.RegisterCreatedObjectUndo(go, "Create DarkLord");
        }
        Undo.SetTransformParent(go.transform, parent, "Move DarkLord under Special");

        // Top-right area
        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        var cell = GetWalkableCellNear(new Vector2Int(18, 12), tilemap, usedCells);
        Vector3 worldPos = tilemap.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        worldPos.z = 0;
        go.transform.position = worldPos;
        go.transform.localScale = Vector3.one * 1.2f;

        var mo2 = go.GetComponent<THMapObject>() ?? go.AddComponent<THMapObject>();
        mo2.id = DARK_LORD_ID;
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
            new THArmyUnit{id="darklord",   name="Тёмный Лорд",      count=1,  hpPerUnit=200, attack=20, defense=15, initiative=8},
            new THArmyUnit{id="darkguard",  name="Тёмный страж",     count=10, hpPerUnit=20,  attack=10, defense=8,  initiative=5},
            new THArmyUnit{id="orc",        name="Орк",               count=14, hpPerUnit=8,   attack=4,  defense=3,  initiative=4},
            new THArmyUnit{id="skeleton",   name="Скелет",            count=12, hpPerUnit=5,   attack=3,  defense=2,  initiative=3},
        };

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_dark_boss.png");
        if (spr == null) spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_darklord_map.png");
        sr.sprite = spr != null ? spr : MakePlaceholderSprite(new Color(0.4f, 0f, 0.6f), "DarkLordPlaceholder");
        sr.sortingOrder = 30;

        var col = go.GetComponent<CircleCollider2D>() ?? go.AddComponent<CircleCollider2D>();
        col.radius = 0.5f;

        EditorUtility.SetDirty(go);
        Debug.Log($"[TheHeroMapGameplay] DarkLord placed at ({cell.x},{cell.y})");
    }

    // =========================================================
    // ARTIFACT
    // =========================================================
    private static void PlaceArtifact(Transform parent, Tilemap tilemap)
    {
        const string ART_ID = "AncientAmulet";
        var existing = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .FirstOrDefault(o => o.id == ART_ID || o.name == "Artifact_AncientAmulet");

        GameObject go;
        if (existing != null) go = existing.gameObject;
        else
        {
            go = new GameObject("Artifact_AncientAmulet");
            Undo.RegisterCreatedObjectUndo(go, "Create Artifact");
        }
        Undo.SetTransformParent(go.transform, parent, "Move Artifact under Artifacts");

        var usedCells = new HashSet<Vector2Int>();
        foreach (var mo in Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include))
            usedCells.Add(new Vector2Int(mo.targetX, mo.targetY));

        var cell = GetWalkableCellNear(new Vector2Int(12, 9), tilemap, usedCells);
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

        var sr = go.GetComponent<SpriteRenderer>() ?? go.AddComponent<SpriteRenderer>();
        var spr = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Resources/Sprites/CleanMap/Objects/clean_chest.png");
        sr.sprite = spr != null ? spr : MakePlaceholderSprite(new Color(1f, 0.8f, 0.2f), "ArtifactPlaceholder");
        sr.sortingOrder = 20;

        var col = go.GetComponent<CircleCollider2D>() ?? go.AddComponent<CircleCollider2D>();
        col.radius = 0.4f;

        EditorUtility.SetDirty(go);
        Debug.Log($"[TheHeroMapGameplay] Artifact_AncientAmulet placed at ({cell.x},{cell.y})");
    }

    // =========================================================
    // HOVER LABEL
    // =========================================================
    private static void EnsureSingleHoverLabel()
    {
        var all = Object.FindObjectsByType<THSingleMapHoverLabel>(FindObjectsInactive.Include);
        if (all.Length > 1)
        {
            for (int i = 1; i < all.Length; i++)
                Undo.DestroyObjectImmediate(all[i].gameObject);
        }
        if (all.Length == 0)
        {
            var go = new GameObject("MapHoverLabelController");
            go.AddComponent<THSingleMapHoverLabel>();
            Undo.RegisterCreatedObjectUndo(go, "Create Hover Label");
        }
    }

    // =========================================================
    // VALIDATION (internal)
    // =========================================================
    private static bool ValidateScene()
    {
        bool ok = true;
        var allMO = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);

        bool hasHero = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).Length > 0;
        if (!hasHero) { Debug.LogError("[TheHeroMapGameplay] FAIL: No Hero found."); ok = false; }

        bool hasCastle = allMO.Any(o => o.type == THMapObject.ObjectType.Base);
        if (!hasCastle) { Debug.LogError("[TheHeroMapGameplay] FAIL: No Castle/Base found."); ok = false; }

        int enemyCount = allMO.Count(o => o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord);
        if (enemyCount < 5) { Debug.LogError($"[TheHeroMapGameplay] FAIL: Only {enemyCount} enemies (need ≥5)."); ok = false; }

        bool hasDarkLord = allMO.Any(o => o.isDarkLord);
        if (!hasDarkLord) { Debug.LogError("[TheHeroMapGameplay] FAIL: No DarkLord found."); ok = false; }

        int resourceCount = allMO.Count(o =>
            o.type == THMapObject.ObjectType.GoldResource ||
            o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource ||
            o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure);
        if (resourceCount < 5) { Debug.LogError($"[TheHeroMapGameplay] FAIL: Only {resourceCount} resources (need ≥5)."); ok = false; }

        bool hasArtifact = allMO.Any(o => o.type == THMapObject.ObjectType.Artifact);
        if (!hasArtifact) { Debug.LogError("[TheHeroMapGameplay] FAIL: No Artifact found."); ok = false; }

        return ok;
    }

    // =========================================================
    // CLEANUP DUPLICATES
    // =========================================================
    private static void CleanDuplicateGroups(GameObject mapRoot)
    {
        // Find Objects groups NOT under MapRoot and remove them
        var allGOs = Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include);
        foreach (var go in allGOs)
        {
            if (go == null || go == mapRoot) continue;
            if ((go.name == "Objects" || go.name == "Enemies" || go.name == "Resources") &&
                (go.transform.parent == null || go.transform.parent.gameObject != mapRoot))
            {
                // Only remove if it's a true root-level orphan or is a duplicate outside MapRoot
                if (go.transform.parent == null || !IsDescendantOf(go.transform, mapRoot.transform))
                {
                    // Check if children are gameplay objects - if so, re-parent instead
                    bool hasMapObjects = go.GetComponentsInChildren<THMapObject>().Length > 0;
                    if (!hasMapObjects)
                    {
                        Undo.DestroyObjectImmediate(go);
                    }
                }
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
    private static void WriteReport()
    {
        var allMO = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);
        var hero = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).FirstOrDefault();
        var castle = allMO.FirstOrDefault(o => o.type == THMapObject.ObjectType.Base);
        var darkLord = allMO.FirstOrDefault(o => o.isDarkLord);
        var enemies = allMO.Where(o => o.type == THMapObject.ObjectType.Enemy && !o.isDarkLord).ToList();
        var resources = allMO.Where(o =>
            o.type == THMapObject.ObjectType.GoldResource || o.type == THMapObject.ObjectType.WoodResource ||
            o.type == THMapObject.ObjectType.StoneResource || o.type == THMapObject.ObjectType.ManaResource ||
            o.type == THMapObject.ObjectType.Treasure).ToList();
        var artifact = allMO.FirstOrDefault(o => o.type == THMapObject.ObjectType.Artifact);

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("# MapGameplay Restore Report");
        sb.AppendLine($"Generated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine();
        sb.AppendLine("## Hero");
        if (hero != null) sb.AppendLine($"- Position: cell ({hero.currentX},{hero.currentY}), world {hero.transform.position}");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine("## Castle");
        if (castle != null) sb.AppendLine($"- {castle.id}: cell ({castle.targetX},{castle.targetY}), world {castle.transform.position}");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine("## Enemies");
        foreach (var e in enemies)
            sb.AppendLine($"- {e.id} [{e.difficulty}]: cell ({e.targetX},{e.targetY}), army={e.enemyArmy.Count} units, reward={e.rewardGold}g/{e.rewardExp}exp");
        sb.AppendLine();
        sb.AppendLine("## DarkLord");
        if (darkLord != null) sb.AppendLine($"- {darkLord.id}: cell ({darkLord.targetX},{darkLord.targetY}), army={darkLord.enemyArmy.Count} units");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine("## Resources");
        foreach (var r in resources)
            sb.AppendLine($"- {r.id} [{r.type}]: cell ({r.targetX},{r.targetY}), gold={r.rewardGold} wood={r.rewardWood} stone={r.rewardStone} mana={r.rewardMana} exp={r.rewardExp}");
        sb.AppendLine();
        sb.AppendLine("## Artifact");
        if (artifact != null) sb.AppendLine($"- {artifact.id}: {artifact.displayName}, cell ({artifact.targetX},{artifact.targetY})");
        else sb.AppendLine("- NOT FOUND");
        sb.AppendLine();
        sb.AppendLine("## Wired Up");
        sb.AppendLine("- THStrictGridHeroMovement on Hero");
        sb.AppendLine("- THCameraFollow on Main Camera (Target = Hero)");
        sb.AppendLine("- THCastle on Castle_Player");
        sb.AppendLine("- THMapObject on all gameplay objects");
        sb.AppendLine("- THSingleMapHoverLabel singleton in scene");
        sb.AppendLine();
        sb.AppendLine("## Manual Checklist");
        sb.AppendLine("- [ ] Open Map.unity in Unity Editor and press Play to verify Hero moves");
        sb.AppendLine("- [ ] Confirm camera follows Hero");
        sb.AppendLine("- [ ] Click Castle to verify Base scene loads");
        sb.AppendLine("- [ ] Click an Enemy to verify combat launches");
        sb.AppendLine("- [ ] Click a Resource to verify it is collected");
        sb.AppendLine("- [ ] Confirm DarkLord in top-right area");
        sb.AppendLine("- [ ] Click Artifact to confirm it is picked up");
        sb.AppendLine("- [ ] Run 'The Hero/Validation/Validate Map Gameplay' to confirm all checks pass");
        sb.AppendLine();
        sb.AppendLine("## API Assumptions");
        sb.AppendLine("- THCastle has no opensScene field; Base scene loading handled by THMapObject.OnPointerDown -> THMapController.HandleObjectInteraction (ObjectType.Base -> THSceneLoader.Instance.LoadBase())");
        sb.AppendLine("- THEnemyDifficulty enum: Weak/Medium/Strong/Deadly (not Normal)");
        sb.AppendLine("- THArmyUnit.Clone() used to safely copy army lists");

        Directory.CreateDirectory("Assets/CodeAudit");
        File.WriteAllText("Assets/CodeAudit/MapGameplay_Restore_Report.md", sb.ToString());
        AssetDatabase.Refresh();
        Debug.Log("[TheHeroMapGameplay] Report written to Assets/CodeAudit/MapGameplay_Restore_Report.md");
    }

    // =========================================================
    // HELPERS
    // =========================================================
    private static GameObject FindOrCreate(string name, Transform parent)
    {
        // Search under parent
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

    private static Vector2Int GetWalkableCellNear(Vector2Int desired, Tilemap tilemap, HashSet<Vector2Int> usedCells = null)
    {
        // BFS from desired cell outward, skipping impassable tiles
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
                if (IsWalkableCell(cell, tilemap) && (usedCells == null || !usedCells.Contains(cell)))
                    return cell;
            }

            foreach (var dir in new[]{ Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
                new Vector2Int(1,1), new Vector2Int(-1,1), new Vector2Int(1,-1), new Vector2Int(-1,-1) })
            {
                var neighbor = cell + dir;
                if (!visited.Contains(neighbor) && Mathf.Abs(neighbor.x - desired.x) < 30 && Mathf.Abs(neighbor.y - desired.y) < 30)
                {
                    visited.Add(neighbor);
                    queue.Enqueue(neighbor);
                }
            }
        }

        // Fallback: return desired regardless
        return desired;
    }

    private static bool IsWalkableCell(Vector2Int cell, Tilemap tilemap)
    {
        var tileBase = tilemap.GetTile(new Vector3Int(cell.x, cell.y, 0));
        if (tileBase == null) return false;
        // Skip water/mountain/rock tiles by name
        string tileName = tileBase.name.ToLower();
        if (tileName.Contains("water") || tileName.Contains("mountain") || tileName.Contains("rock"))
            return false;
        return true;
    }

    private static Sprite MakePlaceholderSprite(Color color, string name)
    {
        var tex = new Texture2D(32, 32);
        var pixels = new Color[32 * 32];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
        tex.SetPixels(pixels);
        tex.Apply();
        tex.name = name;
        return Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f), 32f);
    }
}
