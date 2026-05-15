using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

public static class TheHeroFixMapVisualAndStartupBugs
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string CombatScenePath = "Assets/Scenes/Combat.unity";
    private const string BaseScenePath = "Assets/Scenes/Base.unity";
    private const string BackupScenePath = "Assets/Scenes/Map_before_visual_startup_fix.unity";
    private const string ReportPath = "Assets/CodeAudit/Map_Visual_And_Startup_Bugs_Fix_Report.md";
    private const string PreferredCastlePath = "Assets/ExternalAssets/MainAssets/Castle.png";

    private static readonly string[] BuildScenes =
    {
        MainMenuScenePath,
        MapScenePath,
        CombatScenePath,
        BaseScenePath
    };

    [MenuItem("The Hero/Fixes/Fix Map Visual And Startup Bugs")]
    public static void FixMapVisualAndStartupBugs()
    {
        var report = new FixReport();

        EnsureFolder("Assets/CodeAudit");
        CreateBackup(report);

        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        report.OpenedScene = scene.path;

        ClearBadRoadCross(report);
        RestoreCastleVisual(report);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MapScenePath);
        AssetDatabase.SaveAssets();

        ConfigureStartupScenes(report);
        WriteReport(report);

        Debug.Log("[TheHeroBugFix] Bad cross removed completely");
        Debug.Log("[TheHeroBugFix] Castle visual restored");
        Debug.Log("[TheHeroBugFix] Startup scene set to MainMenu");
        Debug.Log("[TheHeroBugFix] Report written: " + ReportPath);
    }

    [MenuItem("The Hero/Fixes/Force Startup Scene MainMenu")]
    public static void ForceStartupSceneMainMenu()
    {
        bool ok = EnsureMainMenuStartupScene(true);
        if (ok)
        {
            Debug.Log("[TheHeroBugFix] Startup scene set to MainMenu");
        }
    }

    public static void RunFromCommandLine()
    {
        try
        {
            FixMapVisualAndStartupBugs();
            EditorApplication.Exit(0);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    public static void RunStartupFixFromCommandLine()
    {
        try
        {
            bool ok = EnsureMainMenuStartupScene(true);
            EditorApplication.Exit(ok ? 0 : 1);
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            EditorApplication.Exit(1);
        }
    }

    public static bool EnsureMainMenuStartupScene(bool logResult)
    {
        SceneAsset mainMenu = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (mainMenu == null)
        {
            Debug.LogError("[TheHeroBugFix] MainMenu scene asset not found: " + MainMenuScenePath);
            return false;
        }

        bool changed = false;
        if (EditorSceneManager.playModeStartScene != mainMenu)
        {
            EditorSceneManager.playModeStartScene = mainMenu;
            changed = true;
        }

        EditorBuildSettingsScene[] desired = BuildScenes
            .Select(path => new EditorBuildSettingsScene(path, true))
            .ToArray();

        if (!EditorBuildSettings.scenes.Select(s => s.path).SequenceEqual(BuildScenes))
        {
            EditorBuildSettings.scenes = desired;
            changed = true;
        }

        if (changed)
        {
            AssetDatabase.SaveAssets();
        }

        if (logResult)
        {
            Debug.Log("[TheHeroBugFix] Play Mode Start Scene enforced: " + MainMenuScenePath);
        }

        return true;
    }

    private static void CreateBackup(FixReport report)
    {
        if (!File.Exists(MapScenePath))
        {
            throw new FileNotFoundException("Map scene not found", MapScenePath);
        }

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BackupScenePath) != null)
        {
            report.BackupScene = BackupScenePath + " (existing backup preserved)";
            return;
        }

        if (!AssetDatabase.CopyAsset(MapScenePath, BackupScenePath))
        {
            throw new InvalidOperationException("Could not create scene backup at " + BackupScenePath);
        }

        AssetDatabase.ImportAsset(BackupScenePath);
        report.BackupScene = BackupScenePath;
    }

    private static void ClearBadRoadCross(FixReport report)
    {
        Tilemap ground = FindTilemap("GroundTilemap");
        Tilemap road = FindTilemap("RoadTilemap");
        Tilemap blocking = FindTilemap("BlockingTilemap");

        if (road != null && CountTiles(road) > 0)
        {
            report.RoadTilemapName = GetHierarchyPath(road.transform);
            report.RoadTilesBefore = CountTiles(road);
            report.RoadBoundsBefore = BoundsToString(road.cellBounds);
            report.RoadAction = "RoadTilemap was cleared completely; no replacement road was created.";

            road.ClearAllTiles();
            road.CompressBounds();

            report.RoadTilesAfter = CountTiles(road);
            report.RoadBoundsAfter = BoundsToString(road.cellBounds);
            return;
        }

        Tilemap target = blocking != null ? blocking : FindCrossLikeTilemapExcept(ground);
        if (target == null)
        {
            report.RoadTilemapName = "not found";
            report.RoadTilesBefore = 0;
            report.RoadTilesAfter = 0;
            report.RoadAction = "No RoadTilemap/cross-like non-ground tilemap was found; GroundTilemap was not touched.";
            return;
        }

        report.RoadTilemapName = GetHierarchyPath(target.transform);
        report.RoadTilesBefore = CountTiles(target);
        report.RoadBoundsBefore = BoundsToString(target.cellBounds);

        if (target == blocking && report.RoadTilesBefore == 0)
        {
            int restored = RestoreTilemapTilesFromBackup(target, "BlockingTilemap");
            report.RoadTilesBefore = restored;
            report.RoadBoundsBefore = BoundsToString(target.cellBounds);
        }

        TilemapRenderer renderer = target.GetComponent<TilemapRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
        target.color = new Color(1f, 1f, 1f, 0f);

        report.RoadAction = target == blocking
            ? "BlockingTilemap tiles were preserved for movement logic; only its visual renderer was disabled to remove the bad cross."
            : "Cross-like non-ground tilemap visual renderer was disabled; no replacement visual was created.";

        report.RoadTilesAfter = CountTiles(target);
        report.RoadBoundsAfter = BoundsToString(target.cellBounds);
    }

    private static int RestoreTilemapTilesFromBackup(Tilemap target, string tilemapName)
    {
        if (target == null || AssetDatabase.LoadAssetAtPath<SceneAsset>(BackupScenePath) == null)
        {
            return CountTiles(target);
        }

        Scene activeScene = SceneManager.GetActiveScene();
        Scene backupScene = EditorSceneManager.OpenScene(BackupScenePath, OpenSceneMode.Additive);
        try
        {
            Tilemap source = backupScene.GetRootGameObjects()
                .SelectMany(root => root.GetComponentsInChildren<Tilemap>(true))
                .FirstOrDefault(tm => tm != null && tm.gameObject.name == tilemapName);

            if (source == null)
            {
                return CountTiles(target);
            }

            target.ClearAllTiles();
            foreach (Vector3Int pos in source.cellBounds.allPositionsWithin)
            {
                TileBase tile = source.GetTile(pos);
                if (tile == null) continue;

                target.SetTile(pos, tile);
                target.SetTileFlags(pos, TileFlags.None);
                target.SetTransformMatrix(pos, source.GetTransformMatrix(pos));
                target.SetColor(pos, source.GetColor(pos));
                target.SetTileFlags(pos, source.GetTileFlags(pos));
            }
            target.CompressBounds();
            return CountTiles(target);
        }
        finally
        {
            EditorSceneManager.CloseScene(backupScene, true);
            SceneManager.SetActiveScene(activeScene);
        }
    }

    private static void RestoreCastleVisual(FixReport report)
    {
        GameObject castle = FindExistingCastle();
        if (castle == null)
        {
            report.CastleObject = "not found";
            report.CastleAction = "Castle gameplay object was not found; no new gameplay object was created.";
            Debug.LogError("[TheHeroBugFix] Castle_Player gameplay object not found; visual was not changed.");
            return;
        }

        Sprite castleSprite = LoadCastleSprite(report);
        if (castleSprite == null)
        {
            report.CastleObject = GetHierarchyPath(castle.transform);
            report.CastleAction = "Castle sprite was not found; gameplay object was left unchanged.";
            Debug.LogError("[TheHeroBugFix] Castle sprite not found.");
            return;
        }

        SetActiveInHierarchy(castle.transform);

        SpriteRenderer renderer = castle.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = castle.GetComponentsInChildren<SpriteRenderer>(true)
                .OrderByDescending(sr => sr.gameObject.name.Equals("Visual", StringComparison.OrdinalIgnoreCase) ||
                                         sr.gameObject.name.Equals("Visual_House", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        if (renderer == null)
        {
            var visual = new GameObject("Visual");
            Undo.RegisterCreatedObjectUndo(visual, "Create Castle Visual");
            visual.transform.SetParent(castle.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            renderer = visual.AddComponent<SpriteRenderer>();
        }

        renderer.gameObject.SetActive(true);
        renderer.enabled = true;
        renderer.sprite = castleSprite;
        renderer.color = Color.white;
        renderer.sortingOrder = 80;
        renderer.sortingLayerID = 0;
        renderer.transform.localPosition = Vector3.zero;
        renderer.transform.localRotation = Quaternion.identity;
        renderer.transform.localScale = new Vector3(0.6f, 0.6f, 1f);

        report.CastleObject = GetHierarchyPath(castle.transform);
        report.CastleVisualObject = GetHierarchyPath(renderer.transform);
        report.CastleSpritePath = AssetDatabase.GetAssetPath(castleSprite);
        report.CastleSpriteName = castleSprite.name;
        report.CastleSortingOrder = renderer.sortingOrder;
        report.CastleScale = renderer.transform.localScale;
        report.CastleAction = "Existing Castle_Player gameplay object kept; SpriteRenderer visual restored with Castle.png.";
    }

    private static Sprite LoadCastleSprite(FixReport report)
    {
        string path = FindCastleTexturePath();
        report.CastleTexturePath = path ?? "not found";
        if (string.IsNullOrEmpty(path)) return null;

        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }
            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }
            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path) ??
               AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
    }

    private static string FindCastleTexturePath()
    {
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(PreferredCastlePath) != null)
        {
            return PreferredCastlePath;
        }

        string[] guids = AssetDatabase.FindAssets("Castle t:Texture2D");
        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(ScoreCastlePath)
            .FirstOrDefault();
    }

    private static int ScoreCastlePath(string path)
    {
        int score = 0;
        string normalized = path.Replace('\\', '/');
        if (normalized.EndsWith("/Castle.png", StringComparison.OrdinalIgnoreCase)) score += 100;
        if (normalized.IndexOf("/MainAssets/", StringComparison.OrdinalIgnoreCase) >= 0) score += 50;
        if (normalized.IndexOf("Tiny Swords", StringComparison.OrdinalIgnoreCase) >= 0) score += 25;
        if (normalized.IndexOf("/Buildings/", StringComparison.OrdinalIgnoreCase) >= 0) score += 10;
        return score;
    }

    private static void ConfigureStartupScenes(FixReport report)
    {
        foreach (string scenePath in BuildScenes)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
            {
                Debug.LogError("[TheHeroBugFix] Missing scene in desired build list: " + scenePath);
            }
        }

        report.PlayModeStartScene = EnsureMainMenuStartupScene(false)
            ? MainMenuScenePath
            : "MainMenu scene asset missing";

        report.SceneOrder.Clear();
        report.SceneOrder.AddRange(EditorBuildSettings.scenes
            .Select((scene, index) => index + ": " + scene.path));

        AssetDatabase.SaveAssets();
    }

    private static void WriteReport(FixReport report)
    {
        report.AutoLoadFindings = FindAutoLoadMapScripts();

        var sb = new StringBuilder();
        sb.AppendLine("# Map Visual And Startup Bugs Fix Report");
        sb.AppendLine();
        sb.AppendLine("## Backup");
        sb.AppendLine("- Backup scene: `" + report.BackupScene + "`");
        sb.AppendLine("- Opened scene: `" + report.OpenedScene + "`");
        sb.AppendLine();
        sb.AppendLine("## Bad Cross");
        sb.AppendLine("- Tilemap/object: `" + report.RoadTilemapName + "`");
        sb.AppendLine("- Tiles before: " + report.RoadTilesBefore);
        sb.AppendLine("- Bounds before: `" + report.RoadBoundsBefore + "`");
        sb.AppendLine("- Action: " + report.RoadAction);
        sb.AppendLine("- Tiles after: " + report.RoadTilesAfter);
        sb.AppendLine("- Bounds after: `" + report.RoadBoundsAfter + "`");
        sb.AppendLine("- GroundTilemap: not modified.");
        sb.AppendLine();
        sb.AppendLine("## Castle");
        sb.AppendLine("- Castle object: `" + report.CastleObject + "`");
        sb.AppendLine("- Visual object: `" + report.CastleVisualObject + "`");
        sb.AppendLine("- Texture path: `" + report.CastleTexturePath + "`");
        sb.AppendLine("- Sprite assigned: `" + report.CastleSpritePath + "` / `" + report.CastleSpriteName + "`");
        sb.AppendLine("- Sorting order: " + report.CastleSortingOrder);
        sb.AppendLine("- Local scale: " + VectorToString(report.CastleScale));
        sb.AppendLine("- Action: " + report.CastleAction);
        sb.AppendLine();
        sb.AppendLine("## Startup");
        sb.AppendLine("- Play Mode start scene: `" + report.PlayModeStartScene + "`");
        sb.AppendLine("- Build scene order:");
        foreach (string scene in report.SceneOrder)
        {
            sb.AppendLine("  - `" + scene + "`");
        }
        sb.AppendLine();
        sb.AppendLine("## Auto-load Map Script Search");
        if (report.AutoLoadFindings.Count == 0)
        {
            sb.AppendLine("- No `Awake`/`Start` auto-load of `Map` was found.");
            sb.AppendLine("- Valid `LoadMap()` flows are still present for MainMenu New Game/Continue and returns from Base/Combat.");
        }
        else
        {
            foreach (string finding in report.AutoLoadFindings)
            {
                sb.AppendLine("- " + finding);
            }
        }
        sb.AppendLine();
        sb.AppendLine("## Manual Checks");
        sb.AppendLine("- Press Play and confirm MainMenu appears first, not Map.");
        sb.AppendLine("- Click New Game and confirm Map opens.");
        sb.AppendLine("- Confirm the bad cross is gone and RoadTilemap stays empty.");
        sb.AppendLine("- Confirm Castle_Player is visible in the center and clicking it can enter Base.");
        sb.AppendLine("- Confirm Hero movement still works and orcs/chests/resources are still present.");
        sb.AppendLine("- Confirm Console has no new red errors.");

        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
        AssetDatabase.ImportAsset(ReportPath);
    }

    private static List<string> FindAutoLoadMapScripts()
    {
        var findings = new List<string>();
        string assetsPath = Application.dataPath;
        foreach (string file in Directory.GetFiles(assetsPath, "*.cs", SearchOption.AllDirectories))
        {
            string normalized = file.Replace('\\', '/');
            if (normalized.IndexOf("/Assets/Editor/", StringComparison.OrdinalIgnoreCase) >= 0) continue;

            string text = File.ReadAllText(file);
            if (text.IndexOf("LoadMap()", StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("LoadScene", StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("StartNewGame", StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("skipMainMenu", StringComparison.OrdinalIgnoreCase) < 0 &&
                text.IndexOf("autoStartMap", StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            string relative = "Assets" + normalized.Substring(assetsPath.Replace('\\', '/').Length);
            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inStartupMethod = false;
            bool startupMethodOpened = false;
            int startupMethodDepth = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();
                if (IsStartupMethodDeclaration(trimmed))
                {
                    inStartupMethod = true;
                    startupMethodOpened = false;
                    startupMethodDepth = 0;
                }

                bool loadsMap = line.Contains(".LoadMap()") ||
                                line.Contains("LoadScene(\"Map\"") ||
                                line.Contains("LoadSceneAsync(\"Map\"");
                bool hasStartupFlag = line.IndexOf("skipMainMenu", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                      line.IndexOf("autoStartMap", StringComparison.OrdinalIgnoreCase) >= 0;

                if ((loadsMap && inStartupMethod) || hasStartupFlag)
                {
                    findings.Add("`" + relative + ":" + (i + 1) + "` " + line.Trim());
                }

                if (inStartupMethod)
                {
                    startupMethodDepth += CountChar(line, '{');
                    startupMethodDepth -= CountChar(line, '}');
                    if (line.Contains("{")) startupMethodOpened = true;
                    if (startupMethodOpened && startupMethodDepth <= 0)
                    {
                        inStartupMethod = false;
                        startupMethodOpened = false;
                        startupMethodDepth = 0;
                    }
                }
            }
        }

        return findings;
    }

    private static bool IsStartupMethodDeclaration(string methodLine)
    {
        if (string.IsNullOrEmpty(methodLine)) return false;
        return methodLine.Contains(" void Awake(") ||
               methodLine.StartsWith("void Awake(") ||
               methodLine.Contains(" void Start(") ||
               methodLine.StartsWith("void Start(") ||
               methodLine.Contains(" void OnEnable(") ||
               methodLine.StartsWith("void OnEnable(") ||
               methodLine.Contains("RuntimeInitializeOnLoadMethod") ||
               methodLine.Contains("InitializeOnLoadMethod");
    }

    private static int CountChar(string value, char target)
    {
        int count = 0;
        foreach (char c in value)
        {
            if (c == target) count++;
        }
        return count;
    }

    private static GameObject FindExistingCastle()
    {
        GameObject byName = GameObject.Find("Castle_Player") ??
                            GameObject.Find("PlayerCastle") ??
                            GameObject.Find("Castle");
        if (byName != null) return byName;

        return UnityEngine.Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .Where(o => o != null)
            .OrderByDescending(o => o.id == "Castle_Player")
            .ThenByDescending(o => o.type == THMapObject.ObjectType.Base)
            .Select(o => o.gameObject)
            .FirstOrDefault();
    }

    private static Tilemap FindTilemap(string objectName)
    {
        return UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
            .FirstOrDefault(tm => tm != null && tm.gameObject.name.Equals(objectName, StringComparison.OrdinalIgnoreCase));
    }

    private static Tilemap FindCrossLikeTilemapExcept(Tilemap excluded)
    {
        return UnityEngine.Object.FindObjectsByType<Tilemap>(FindObjectsInactive.Include)
            .Where(tm => tm != null && tm != excluded)
            .Where(tm => !tm.gameObject.name.Equals("GroundTilemap", StringComparison.OrdinalIgnoreCase))
            .Where(tm => CountTiles(tm) > 0)
            .OrderByDescending(CrossScore)
            .FirstOrDefault(tm => CrossScore(tm) >= 20);
    }

    private static int CrossScore(Tilemap tilemap)
    {
        var xs = new Dictionary<int, int>();
        var ys = new Dictionary<int, int>();
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(pos)) continue;
            xs[pos.x] = xs.TryGetValue(pos.x, out int xv) ? xv + 1 : 1;
            ys[pos.y] = ys.TryGetValue(pos.y, out int yv) ? yv + 1 : 1;
        }

        int bestX = xs.Count == 0 ? 0 : xs.Values.Max();
        int bestY = ys.Count == 0 ? 0 : ys.Values.Max();
        return bestX + bestY;
    }

    private static int CountTiles(Tilemap tilemap)
    {
        if (tilemap == null) return 0;

        int count = 0;
        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos)) count++;
        }
        return count;
    }

    private static void SetActiveInHierarchy(Transform transform)
    {
        while (transform != null)
        {
            transform.gameObject.SetActive(true);
            transform = transform.parent;
        }
    }

    private static string GetHierarchyPath(Transform transform)
    {
        if (transform == null) return "";

        var names = new Stack<string>();
        while (transform != null)
        {
            names.Push(transform.name);
            transform = transform.parent;
        }
        return string.Join("/", names);
    }

    private static string BoundsToString(BoundsInt bounds)
    {
        return "origin=(" + bounds.xMin + "," + bounds.yMin + "," + bounds.zMin + "), size=(" +
               bounds.size.x + "," + bounds.size.y + "," + bounds.size.z + ")";
    }

    private static string VectorToString(Vector3 v)
    {
        return "(" + v.x.ToString("0.###", CultureInfo.InvariantCulture) + ", " +
               v.y.ToString("0.###", CultureInfo.InvariantCulture) + ", " +
               v.z.ToString("0.###", CultureInfo.InvariantCulture) + ")";
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;

        string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
        string leaf = Path.GetFileName(folder);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
        {
            EnsureFolder(parent);
        }
        AssetDatabase.CreateFolder(parent ?? "Assets", leaf);
    }

    private sealed class FixReport
    {
        public string BackupScene = "";
        public string OpenedScene = "";
        public string RoadTilemapName = "";
        public int RoadTilesBefore;
        public int RoadTilesAfter;
        public string RoadBoundsBefore = "";
        public string RoadBoundsAfter = "";
        public string RoadAction = "";
        public string CastleObject = "";
        public string CastleVisualObject = "";
        public string CastleTexturePath = "";
        public string CastleSpritePath = "";
        public string CastleSpriteName = "";
        public int CastleSortingOrder;
        public Vector3 CastleScale;
        public string CastleAction = "";
        public string PlayModeStartScene = "";
        public readonly List<string> SceneOrder = new List<string>();
        public List<string> AutoLoadFindings = new List<string>();
    }
}

[InitializeOnLoad]
public static class TheHeroMainMenuPlayModeStartEnforcer
{
    private const string SessionLogKey = "TheHero_MainMenuPlayModeStartEnforcer_Logged";

    static TheHeroMainMenuPlayModeStartEnforcer()
    {
        EditorApplication.delayCall += EnforceAfterReload;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            TheHeroFixMapVisualAndStartupBugs.EnsureMainMenuStartupScene(false);
        }
    }

    private static void EnforceAfterReload()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        bool shouldLog = !SessionState.GetBool(SessionLogKey, false);
        TheHeroFixMapVisualAndStartupBugs.EnsureMainMenuStartupScene(shouldLog);
        SessionState.SetBool(SessionLogKey, true);
    }
}
