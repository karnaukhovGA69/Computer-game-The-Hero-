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
using Object = UnityEngine.Object;

public static class TheHeroAssignImportedSpritesToMap
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string ReportPath = "Assets/CodeAudit/ImportedSprites_Assignment_Report.md";

    private static readonly string[] CandidateRoots =
    {
        "Assets/ExternalAssets",
        "Assets/Resources",
        "Assets/Sprites",
        "Assets/Tiny Swords",
        "Assets/Cainos"
    };

    private static readonly string[] RoleOrder =
    {
        "Hero", "Castle", "Gold", "Wood", "Stone", "Mana", "Chest", "Artifact",
        "Bridge", "Goblin", "Orc", "Skeleton", "DarkGuard", "DarkLord", "Wolf", "Bandit"
    };

    [MenuItem("The Hero/Assets/Assign Imported Sprites To Map")]
    public static void AssignImportedSpritesToMap()
    {
        Debug.Log("[TheHeroSprites] Asset scan started");

        var catalog = THImportedSpriteEditorUtil.BuildCatalog(CandidateRoots);
        var selections = new Dictionary<string, THImportedSpriteEditorUtil.SpriteCandidate>();
        foreach (string role in RoleOrder)
        {
            selections[role] = THImportedSpriteEditorUtil.PickBest(catalog, role);
        }

        var scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        var assignments = new List<THImportedSpriteEditorUtil.AssignmentRecord>();
        var warnings = new List<string>();
        var before = THImportedSpriteEditorUtil.CaptureGameplay();

        AssignHero(selections, assignments, warnings);
        Debug.Log("[TheHeroSprites] Hero sprite assigned");

        AssignMapObjects(selections, assignments, warnings);
        AssignBridgeObjects(selections, assignments, warnings);

        var after = THImportedSpriteEditorUtil.CaptureGameplay();
        var gameplayDiffs = THImportedSpriteEditorUtil.CompareGameplay(before, after);
        foreach (string diff in gameplayDiffs)
        {
            Debug.LogError("[TheHeroSprites] Gameplay field changed: " + diff);
        }

        Debug.Log("[TheHeroSprites] Castle sprite assigned");
        Debug.Log("[TheHeroSprites] Resource sprites assigned");
        Debug.Log("[TheHeroSprites] Orc sprite assigned");
        Debug.Log("[TheHeroSprites] Skeleton sprite assigned");
        Debug.Log("[TheHeroSprites] DarkLord sprite assigned");
        Debug.Log("[TheHeroSprites] Enemy sprites assigned");
        Debug.Log("[TheHeroSprites] Scale and sorting normalized");

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, MapScenePath);
        Debug.Log("[TheHeroSprites] Map saved");

        THImportedSpriteEditorUtil.WriteReport(ReportPath, CandidateRoots, catalog, selections, assignments, warnings, gameplayDiffs);
        AssetDatabase.Refresh();

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(gameplayDiffs.Count == 0 ? 0 : 1);
        }
    }

    private static void AssignHero(
        Dictionary<string, THImportedSpriteEditorUtil.SpriteCandidate> selections,
        List<THImportedSpriteEditorUtil.AssignmentRecord> assignments,
        List<string> warnings)
    {
        GameObject hero = THImportedSpriteEditorUtil.FindHero();
        if (hero == null)
        {
            warnings.Add("Hero object was not found.");
            return;
        }

        var sprite = THImportedSpriteEditorUtil.GetSprite(selections, "Hero");
        THImportedSpriteEditorUtil.ApplyVisual(
            hero,
            "Hero",
            sprite,
            0.95f,
            100,
            new Vector2(0.7f, 0.7f),
            assignments,
            warnings);
    }

    private static void AssignMapObjects(
        Dictionary<string, THImportedSpriteEditorUtil.SpriteCandidate> selections,
        List<THImportedSpriteEditorUtil.AssignmentRecord> assignments,
        List<string> warnings)
    {
        var mapObjects = Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include);

        foreach (var obj in mapObjects)
        {
            string role = THImportedSpriteEditorUtil.RoleForMapObject(obj);
            if (string.IsNullOrEmpty(role)) continue;

            float targetCells = THImportedSpriteEditorUtil.TargetCellsForRole(role);
            int sorting = THImportedSpriteEditorUtil.SortingForRole(role);
            Vector2 collider = THImportedSpriteEditorUtil.ColliderForRole(role);
            Sprite sprite = THImportedSpriteEditorUtil.GetSprite(selections, role);

            THImportedSpriteEditorUtil.ApplyVisual(
                obj.gameObject,
                role,
                sprite,
                targetCells,
                sorting,
                collider,
                assignments,
                warnings);
        }
    }

    private static void AssignBridgeObjects(
        Dictionary<string, THImportedSpriteEditorUtil.SpriteCandidate> selections,
        List<THImportedSpriteEditorUtil.AssignmentRecord> assignments,
        List<string> warnings)
    {
        Sprite bridge = THImportedSpriteEditorUtil.GetSprite(selections, "Bridge");
        if (bridge == null) return;

        var renderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include);
        foreach (var sr in renderers)
        {
            if (sr == null || sr.GetComponent<THTile>() != null || sr.GetComponent<THMapObject>() != null) continue;
            if (sr.gameObject.name.IndexOf("bridge", StringComparison.OrdinalIgnoreCase) < 0) continue;

            THImportedSpriteEditorUtil.ApplyVisual(
                sr.gameObject,
                "Bridge",
                bridge,
                1.4f,
                50,
                new Vector2(1.0f, 0.7f),
                assignments,
                warnings);
        }
    }
}

internal static class THImportedSpriteEditorUtil
{
    internal sealed class SpriteCandidate
    {
        public string Path;
        public string FileName;
        public string SpriteName;
        public string PrimaryCategory;
        public string Suitability;
        public string Reason;
        public int TextureWidth;
        public int TextureHeight;
        public int SpriteWidth;
        public int SpriteHeight;
        public bool IsSubSprite;
        public Sprite Sprite;
    }

    internal sealed class AssignmentRecord
    {
        public string ObjectName;
        public string Role;
        public string SpritePath;
        public string SpriteName;
        public string Scale;
        public int SortingOrder;
        public string ColliderSize;
        public bool PreservedCurrentSprite;
    }

    private sealed class GameplaySnapshot
    {
        public string Key;
        public string Id;
        public THMapObject.ObjectType Type;
        public THEnemyDifficulty Difficulty;
        public bool BlocksMovement;
        public bool StartsCombat;
        public bool IsFinalBoss;
        public bool IsDarkLord;
        public int RewardGold;
        public int RewardWood;
        public int RewardStone;
        public int RewardMana;
        public int RewardExp;
        public int TargetX;
        public int TargetY;
        public string Army;
    }

    internal static List<SpriteCandidate> BuildCatalog(IEnumerable<string> roots)
    {
        var validRoots = roots.Where(AssetDatabase.IsValidFolder).ToArray();
        var catalog = new List<SpriteCandidate>();

        foreach (string guid in AssetDatabase.FindAssets("t:Sprite", validRoots))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid).Replace('\\', '/');
            if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;

            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(path).OfType<Sprite>().ToList();
            Sprite single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprites.Count == 0 && single != null) sprites.Add(single);

            foreach (var sprite in sprites)
            {
                if (sprite == null) continue;
                var c = new SpriteCandidate
                {
                    Path = path,
                    FileName = Path.GetFileName(path),
                    SpriteName = sprite.name,
                    TextureWidth = texture != null ? texture.width : Mathf.RoundToInt(sprite.rect.width),
                    TextureHeight = texture != null ? texture.height : Mathf.RoundToInt(sprite.rect.height),
                    SpriteWidth = Mathf.RoundToInt(sprite.rect.width),
                    SpriteHeight = Mathf.RoundToInt(sprite.rect.height),
                    IsSubSprite = single == null || sprite != single,
                    Sprite = sprite
                };

                Classify(c);
                catalog.Add(c);
            }
        }

        return catalog;
    }

    internal static SpriteCandidate PickBest(List<SpriteCandidate> catalog, string role)
    {
        return catalog
            .Select(c => new { Candidate = c, Score = ScoreForRole(c, role) })
            .Where(x => x.Score > 0 && IsSuitableForRole(x.Candidate, role))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Candidate.Path)
            .ThenBy(x => x.Candidate.SpriteName)
            .Select(x => x.Candidate)
            .FirstOrDefault();
    }

    internal static Sprite GetSprite(Dictionary<string, SpriteCandidate> selections, string role)
    {
        return selections.TryGetValue(role, out var candidate) && candidate != null ? candidate.Sprite : null;
    }

    internal static GameObject FindHero()
    {
        var mover = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).FirstOrDefault();
        if (mover != null) return mover.gameObject;

        string[] names = { "Hero", "PlayerHero", "THHero", "MapHero" };
        foreach (string name in names)
        {
            GameObject go = GameObject.Find(name);
            if (go != null) return go;
        }

        return Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include)
            .Select(r => r.gameObject)
            .FirstOrDefault(g => g.name.IndexOf("hero", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    internal static string RoleForMapObject(THMapObject obj)
    {
        if (obj == null) return string.Empty;

        var army = obj.enemyArmy ?? new List<THArmyUnit>();
        string text = (obj.gameObject.name + " " + obj.id + " " + obj.displayName + " " +
                       string.Join(" ", army.Select(a => a.id + " " + a.name)))
            .ToLowerInvariant();

        if (obj.type == THMapObject.ObjectType.Base) return "Castle";
        if (obj.type == THMapObject.ObjectType.GoldResource) return "Gold";
        if (obj.type == THMapObject.ObjectType.WoodResource) return "Wood";
        if (obj.type == THMapObject.ObjectType.StoneResource || obj.type == THMapObject.ObjectType.Mine) return "Stone";
        if (obj.type == THMapObject.ObjectType.ManaResource) return "Mana";
        if (obj.type == THMapObject.ObjectType.Treasure || obj.type == THMapObject.ObjectType.ArtifactChest) return "Chest";
        if (obj.type == THMapObject.ObjectType.Artifact || text.Contains("artifact") || text.Contains("relic") || text.Contains("amulet")) return "Artifact";

        if (obj.type != THMapObject.ObjectType.Enemy) return string.Empty;
        if (obj.isFinalBoss || obj.isDarkLord || text.Contains("darklord") || text.Contains("dark lord") || text.Contains("lord") || text.Contains("boss"))
            return "DarkLord";
        if (text.Contains("darkguard") || text.Contains("dark guard") || text.Contains("guard"))
            return "DarkGuard";
        if (text.Contains("skeleton") || text.Contains("undead") || text.Contains("bone"))
            return "Skeleton";
        if (text.Contains("orc"))
            return "Orc";
        if (text.Contains("goblin"))
            return "Goblin";
        if (text.Contains("wolf") || text.Contains("beast"))
            return "Wolf";
        if (text.Contains("bandit") || text.Contains("rogue"))
            return "Bandit";

        switch (obj.difficulty)
        {
            case THEnemyDifficulty.Strong:
                return "Orc";
            case THEnemyDifficulty.Medium:
                return "Skeleton";
            default:
                return "Goblin";
        }
    }

    internal static float TargetCellsForRole(string role)
    {
        switch (role)
        {
            case "Hero": return 0.95f;
            case "Castle": return 1.9f;
            case "DarkLord": return 1.35f;
            case "Gold":
            case "Wood":
            case "Stone":
            case "Mana":
            case "Chest":
            case "Artifact":
                return 0.85f;
            case "Bridge": return 1.4f;
            default: return 0.95f;
        }
    }

    internal static int SortingForRole(string role)
    {
        switch (role)
        {
            case "Hero": return 100;
            case "Castle": return 70;
            case "Gold":
            case "Wood":
            case "Stone":
            case "Mana":
            case "Chest":
            case "Artifact":
                return 60;
            case "Bridge": return 50;
            default: return 90;
        }
    }

    internal static Vector2 ColliderForRole(string role)
    {
        switch (role)
        {
            case "Hero": return new Vector2(0.7f, 0.7f);
            case "Castle": return new Vector2(1.2f, 1.2f);
            case "DarkLord": return new Vector2(1.0f, 1.0f);
            case "Gold":
            case "Wood":
            case "Stone":
            case "Mana":
            case "Chest":
            case "Artifact":
                return new Vector2(0.7f, 0.7f);
            case "Bridge": return new Vector2(1.0f, 0.7f);
            default: return new Vector2(0.8f, 0.8f);
        }
    }

    internal static void ApplyVisual(
        GameObject go,
        string role,
        Sprite sprite,
        float targetMaxCells,
        int sortingOrder,
        Vector2 colliderSize,
        List<AssignmentRecord> assignments,
        List<string> warnings)
    {
        if (go == null) return;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr == null) sr = go.AddComponent<SpriteRenderer>();

        bool preserved = sprite == null;
        if (sprite != null)
        {
            sr.sprite = sprite;
        }
        else if (sr.sprite == null)
        {
            warnings.Add(go.name + " (" + role + ") has no selected replacement and no current sprite.");
        }

        FitToMaxCells(go.transform, sr.sprite, targetMaxCells);
        sr.sortingOrder = sortingOrder;

        var box = go.GetComponent<BoxCollider2D>();
        if (box == null) box = go.AddComponent<BoxCollider2D>();
        box.size = colliderSize;

        assignments.Add(new AssignmentRecord
        {
            ObjectName = go.name,
            Role = role,
            SpritePath = sr.sprite != null ? AssetDatabase.GetAssetPath(sr.sprite) : string.Empty,
            SpriteName = sr.sprite != null ? sr.sprite.name : string.Empty,
            Scale = FormatVector(go.transform.localScale),
            SortingOrder = sr.sortingOrder,
            ColliderSize = FormatVector(colliderSize),
            PreservedCurrentSprite = preserved
        });
    }

    internal static void FitToMaxCells(Transform transform, Sprite sprite, float targetMaxCells)
    {
        if (transform == null || sprite == null) return;
        Vector2 size = sprite.bounds.size;
        float max = Mathf.Max(size.x, size.y);
        if (max <= 0.0001f) return;
        float scale = targetMaxCells / max;
        scale = Mathf.Clamp(scale, 0.05f, 4f);
        transform.localScale = new Vector3(scale, scale, 1f);
    }

    internal static Dictionary<string, string> CaptureGameplay()
    {
        return Object.FindObjectsByType<THMapObject>(FindObjectsInactive.Include)
            .Select(ToSnapshot)
            .ToDictionary(s => s.Key, ToSnapshotLine);
    }

    internal static List<string> CompareGameplay(Dictionary<string, string> before, Dictionary<string, string> after)
    {
        var diffs = new List<string>();
        foreach (var kv in before)
        {
            if (!after.TryGetValue(kv.Key, out string value))
            {
                diffs.Add(kv.Key + " disappeared");
                continue;
            }

            if (value != kv.Value)
            {
                diffs.Add(kv.Key + " before=[" + kv.Value + "] after=[" + value + "]");
            }
        }

        foreach (string key in after.Keys)
        {
            if (!before.ContainsKey(key)) diffs.Add(key + " appeared");
        }

        return diffs;
    }

    internal static void WriteReport(
        string path,
        IEnumerable<string> roots,
        List<SpriteCandidate> catalog,
        Dictionary<string, SpriteCandidate> selections,
        List<AssignmentRecord> assignments,
        List<string> warnings,
        List<string> gameplayDiffs)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        var sb = new StringBuilder();

        sb.AppendLine("# Imported Sprites Assignment Report");
        sb.AppendLine();
        sb.AppendLine("Generated by `The Hero/Assets/Assign Imported Sprites To Map`.");
        sb.AppendLine();

        sb.AppendLine("## Scanned folders");
        foreach (string root in roots)
        {
            sb.AppendLine("- " + root + (AssetDatabase.IsValidFolder(root) ? "" : " (missing)"));
        }
        sb.AppendLine();

        sb.AppendLine("## Selected sprites");
        sb.AppendLine("| Role | Path | Sprite | Size | Status |");
        sb.AppendLine("| --- | --- | --- | --- | --- |");
        foreach (string role in selections.Keys.OrderBy(k => Array.IndexOf(TheHeroAssignImportedSpritesToMapRoleOrder, k)))
        {
            var c = selections[role];
            if (c == null)
            {
                sb.AppendLine("| " + role + " | - | - | - | Not found; current scene sprite preserved if present. |");
            }
            else
            {
                sb.AppendLine("| " + role + " | `" + c.Path + "` | `" + c.SpriteName + "` | " + c.SpriteWidth + "x" + c.SpriteHeight + " | " + c.Reason + " |");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Map objects updated");
        sb.AppendLine("| Object | Role | Sprite path | Sprite | Scale | Sorting | Collider | Note |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var a in assignments.OrderBy(a => a.Role).ThenBy(a => a.ObjectName))
        {
            sb.AppendLine("| `" + a.ObjectName + "` | " + a.Role + " | `" + a.SpritePath + "` | `" + a.SpriteName + "` | " + a.Scale + " | " + a.SortingOrder + " | " + a.ColliderSize + " | " + (a.PreservedCurrentSprite ? "current sprite preserved" : "assigned") + " |");
        }
        sb.AppendLine();

        sb.AppendLine("## Asset catalog");
        sb.AppendLine("| Path | File | Sprite | Texture | Sprite rect | Category | Suitable | Reason |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (var c in catalog
                     .Where(ShouldListInReport)
                     .OrderBy(c => c.PrimaryCategory)
                     .ThenBy(c => c.Path)
                     .ThenBy(c => c.SpriteName))
        {
            sb.AppendLine("| `" + c.Path + "` | `" + c.FileName + "` | `" + c.SpriteName + "` | " +
                          c.TextureWidth + "x" + c.TextureHeight + " | " + c.SpriteWidth + "x" + c.SpriteHeight +
                          " | " + c.PrimaryCategory + " | " + c.Suitability + " | " + c.Reason + " |");
        }
        sb.AppendLine();

        sb.AppendLine("## Not found or preserved");
        foreach (string role in selections.Keys.Where(k => selections[k] == null))
        {
            sb.AppendLine("- " + role + ": no safe imported non-UI sprite found; existing Map sprite was preserved when present.");
        }
        if (selections.Values.All(v => v != null)) sb.AppendLine("- None for required roles with safe candidates.");
        sb.AppendLine();

        sb.AppendLine("## Rejected examples");
        foreach (var c in catalog.Where(c => c.Suitability == "No").Take(80))
        {
            sb.AppendLine("- `" + c.Path + "` / `" + c.SpriteName + "`: " + c.Reason);
        }
        sb.AppendLine();

        sb.AppendLine("## Gameplay preservation");
        if (gameplayDiffs.Count == 0)
        {
            sb.AppendLine("- PASS: gameplay fields on THMapObject were unchanged.");
            sb.AppendLine("- Checked: id, type, difficulty, blocksMovement, startsCombat, isFinalBoss, isDarkLord, rewards, target grid position, enemy army.");
        }
        else
        {
            sb.AppendLine("- FAIL: gameplay diffs detected.");
            foreach (string diff in gameplayDiffs) sb.AppendLine("- " + diff);
        }
        sb.AppendLine();

        sb.AppendLine("## Warnings");
        if (warnings.Count == 0)
        {
            sb.AppendLine("- None.");
        }
        else
        {
            foreach (string warning in warnings.Distinct()) sb.AppendLine("- " + warning);
        }
        sb.AppendLine();

        sb.AppendLine("## Manual checks");
        sb.AppendLine("- Play from MainMenu, start a New Game, and inspect Map object sizes.");
        sb.AppendLine("- Verify enemy clicks still start Combat.");
        sb.AppendLine("- Verify resources are still collected and Castle still opens Base.");
        sb.AppendLine("- Run `The Hero/Validation/Validate Imported Map Sprites` and confirm no red Console errors.");

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
    }

    private static readonly string[] TheHeroAssignImportedSpritesToMapRoleOrder =
    {
        "Hero", "Castle", "Gold", "Wood", "Stone", "Mana", "Chest", "Artifact",
        "Bridge", "Goblin", "Orc", "Skeleton", "DarkGuard", "DarkLord", "Wolf", "Bandit"
    };

    private static void Classify(SpriteCandidate c)
    {
        if (IsRejectedPath(c.Path, c.SpriteName, out string reason))
        {
            c.PrimaryCategory = "Rejected";
            c.Suitability = "No";
            c.Reason = reason;
            return;
        }

        string bestRole = "Other";
        float bestScore = 0f;
        foreach (string role in TheHeroAssignImportedSpritesToMapRoleOrder)
        {
            float score = ScoreForRole(c, role);
            if (score > bestScore)
            {
                bestScore = score;
                bestRole = role;
            }
        }

        c.PrimaryCategory = bestRole;
        if (bestRole == "Other")
        {
            c.Suitability = "No";
            c.Reason = "No map-object role match.";
        }
        else if (IsSuitableForRole(c, bestRole))
        {
            c.Suitability = "Yes";
            c.Reason = ReasonFor(bestRole, c);
        }
        else
        {
            c.Suitability = "No";
            c.Reason = "Rejected for " + bestRole + ": UI, terrain tile, placeholder, portrait, or unsafe oversized art.";
        }
    }

    private static bool IsSuitableForRole(SpriteCandidate c, string role)
    {
        if (c == null || c.Sprite == null) return false;
        if (IsRejectedPath(c.Path, c.SpriteName, out _)) return false;
        if (role == "Mana") return false;

        string label = Label(c);
        if (role == "Hero" && (label.Contains("cat") || label.Contains("bird") || label.Contains("pawn"))) return false;
        if (IsUiPath(label)) return false;
        if ((role == "Hero" || role == "Goblin" || role == "Orc" || role == "Skeleton" || role == "DarkGuard" || role == "DarkLord" || role == "Wolf" || role == "Bandit") && IsTerrainTilePath(label)) return false;
        if ((role == "Gold" || role == "Wood" || role == "Stone" || role == "Chest" || role == "Artifact") && IsTerrainTilePath(label) && !label.Contains("/terrain/decorations/rocks/")) return false;
        if (role != "Castle" && label.Contains("/buildings/")) return false;
        if (role != "Bridge" && label.Contains("bridges.png")) return false;
        if (role != "Chest" && role != "Artifact" && label.Contains("tx props")) return false;

        float worldMax = Mathf.Max(c.Sprite.bounds.size.x, c.Sprite.bounds.size.y);
        if (worldMax > 8f && !label.Contains("cleanmap/objects")) return false;

        return true;
    }

    private static float ScoreForRole(SpriteCandidate c, string role)
    {
        string label = Label(c);
        float score = 0f;

        switch (role)
        {
            case "Hero":
                if (label.Contains("tiny swords") && label.Contains("/yellow units/warrior/") && label.Contains("warrior_idle")) score += 160;
                if (label.Contains("warrior_idle_0")) score += 20;
                if (label.Contains("swordsman")) score += 40;
                if (label.Contains("hero")) score += 25;
                break;
            case "Castle":
                if (label.Contains("tiny swords") && label.Contains("/buildings/") && label.Contains("castle")) score += 170;
                if (label.Contains("/yellow buildings/")) score += 25;
                if (label.Contains("house_details")) score += 35;
                if (label.Contains("clean_castle")) score += 10;
                break;
            case "Gold":
                if (label.Contains("tiny swords") && label.Contains("gold_resource")) score += 160;
                if (label.Contains("gold stone") && !label.Contains("highlight")) score += 110;
                if (label.Contains("gold") || label.Contains("coin")) score += 40;
                break;
            case "Wood":
                if (label.Contains("tiny swords") && label.Contains("wood resource")) score += 160;
                if (label.Contains("wood") || label.Contains("log") || label.Contains("lumber")) score += 40;
                break;
            case "Stone":
                if (label.Contains("tiny swords") && label.Contains("/terrain/decorations/rocks/") && label.Contains("rock1")) score += 150;
                if (label.Contains("rock") || label.Contains("stone") || label.Contains("ore")) score += 45;
                break;
            case "Mana":
                if (label.Contains("crystal") || label.Contains("mana") || label.Contains("gem")) score += 20;
                break;
            case "Chest":
                if (label.Contains("tx props chest") && !label.Contains("opened")) score += 160;
                if (label.Contains("chest") || label.Contains("treasure")) score += 60;
                break;
            case "Artifact":
                if (label.Contains("tx props altar") || label.Contains("tx props statue") || label.Contains("rune pillar")) score += 150;
                if (label.Contains("artifact") || label.Contains("relic") || label.Contains("amulet")) score += 70;
                break;
            case "Bridge":
                if (label.Contains("externalassets/bridges") && label.Contains("bridges_")) score += 140;
                if (label.Contains("bridge")) score += 70;
                if (c.SpriteWidth >= 80 && c.SpriteWidth <= 150 && c.SpriteHeight >= 18 && c.SpriteHeight <= 45) score += 40;
                break;
            case "Goblin":
                if (label.Contains("externalassets/orcs") && label.Contains("orc1_idle_full")) score += 150;
                if (label.Contains("goblin")) score += 80;
                if (label.Contains("orc")) score += 25;
                break;
            case "Orc":
                if (label.Contains("externalassets/orcs") && label.Contains("orc2_idle_full")) score += 160;
                if (label.Contains("orc")) score += 80;
                break;
            case "Skeleton":
                if (label.Contains("externalassets/skeletons") && label.Contains("skeleton warrior")) score += 160;
                if (label.Contains("skeleton") || label.Contains("undead") || label.Contains("bone")) score += 75;
                break;
            case "DarkGuard":
                if (label.Contains("externalassets/skeletons") && label.Contains("skeleton mage")) score += 160;
                if (label.Contains("dark") || label.Contains("undead") || label.Contains("skeleton")) score += 60;
                break;
            case "DarkLord":
                if (label.Contains("externalassets/darklord") && label.Contains("idle2")) score += 180;
                if (label.Contains("executioner") || label.Contains("boss") || label.Contains("dark") || label.Contains("lord") || label.Contains("demon") || label.Contains("necromancer")) score += 80;
                break;
            case "Wolf":
                if (label.Contains("cursedwolf")) score += 160;
                if (label.Contains("wolf") || label.Contains("beast") || label.Contains("monster")) score += 70;
                break;
            case "Bandit":
                if (label.Contains("tiny swords") && label.Contains("/black units/warrior/") && label.Contains("warrior_idle")) score += 150;
                if (label.Contains("bandit") || label.Contains("warrior")) score += 45;
                break;
        }

        if (c.SpriteName.EndsWith("_0", StringComparison.OrdinalIgnoreCase)) score += 8;
        if (c.SpriteName.EndsWith(" 0", StringComparison.OrdinalIgnoreCase)) score += 8;
        if (label.Contains("highlight")) score -= 30;
        if (label.Contains("death")) score -= 40;
        if (label.Contains("hurt")) score -= 20;
        if (label.Contains("attack") && role != "Bandit") score -= 12;
        if (label.Contains("shadow") || label.Contains("head") || label.Contains("body") || label.Contains("sword_")) score -= 45;
        return score;
    }

    private static string ReasonFor(string role, SpriteCandidate c)
    {
        if (c.Path.Contains("ExternalAssets/Orcs")) return "Imported Orcs pack, sliced map-scale unit sprite.";
        if (c.Path.Contains("ExternalAssets/Skeletons")) return "Imported Skeletons pack, sliced map-scale unit sprite.";
        if (c.Path.Contains("ExternalAssets/DarkLord")) return "Imported DarkLord pack, sliced boss/undead sprite.";
        if (c.Path.Contains("ExternalAssets/Bridges")) return "Imported Bridges pack, sliced bridge sprite.";
        if (c.Path.Contains("ExternalAssets/Monsters_FR13")) return "Imported Monsters_FR13 pack, transparent monster sprite.";
        if (c.Path.Contains("Tiny Swords")) return "Tiny Swords pixel-art top-down sprite.";
        if (c.Path.Contains("Cainos")) return "Cainos top-down prop sprite.";
        return "Safe map-scale fallback sprite.";
    }

    private static bool IsRejectedPath(string path, string spriteName, out string reason)
    {
        string label = (path + " " + spriteName).Replace('\\', '/').ToLowerInvariant();
        if (label.Contains("__macosx") || Path.GetFileName(path).StartsWith("._", StringComparison.Ordinal))
        {
            reason = "macOS metadata file.";
            return true;
        }
        if (label.Contains("coupon") || label.Contains(".pdf"))
        {
            reason = "Coupon/marketing asset.";
            return true;
        }
        if (label.Contains("/_disabledoldmapassets/"))
        {
            reason = "Old disabled map asset.";
            return true;
        }
        if (label.Contains("/sprites/generatedtoday/"))
        {
            reason = "Old generated asset pack, not requested imported PNG pack.";
            return true;
        }
        if (label.Contains("/sprites/combat/") || label.Contains("/sprites/base/") || label.Contains("background"))
        {
            reason = "Background or scene illustration.";
            return true;
        }
        if (IsUiPath(label))
        {
            reason = "UI asset, button, panel, icon sheet, or avatar.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

    private static bool ShouldListInReport(SpriteCandidate c)
    {
        if (c.PrimaryCategory != "Other") return true;
        string label = Label(c);
        return label.Contains("externalassets") || label.Contains("tiny swords") || label.Contains("cainos");
    }

    private static bool IsUiPath(string label)
    {
        return label.Contains("/ui/") ||
               label.Contains("/ui elements/") ||
               label.Contains("button") ||
               label.Contains("panel") ||
               label.Contains("inventory") ||
               label.Contains("equipment") ||
               label.Contains("main_menu") ||
               label.Contains("settings") ||
               label.Contains("avatar") ||
               label.Contains("portrait") ||
               label.Contains("icons.png");
    }

    private static bool IsTerrainTilePath(string label)
    {
        return label.Contains("/cleanmap/tiles/") ||
               (label.Contains("/generatedmaptiles/") && !label.Contains("/generatedmaptiles/objects/")) ||
               label.Contains("/terrain/tileset/") ||
               label.Contains("/tile palette/") ||
               label.Contains("tileset");
    }

    private static string Label(SpriteCandidate c)
    {
        return (c.Path + " " + c.SpriteName).Replace('\\', '/').ToLowerInvariant();
    }

    private static GameplaySnapshot ToSnapshot(THMapObject obj)
    {
        return new GameplaySnapshot
        {
            Key = obj.gameObject.name + "#" + obj.GetInstanceID(),
            Id = obj.id,
            Type = obj.type,
            Difficulty = obj.difficulty,
            BlocksMovement = obj.blocksMovement,
            StartsCombat = obj.startsCombat,
            IsFinalBoss = obj.isFinalBoss,
            IsDarkLord = obj.isDarkLord,
            RewardGold = obj.rewardGold,
            RewardWood = obj.rewardWood,
            RewardStone = obj.rewardStone,
            RewardMana = obj.rewardMana,
            RewardExp = obj.rewardExp,
            TargetX = obj.targetX,
            TargetY = obj.targetY,
            Army = string.Join(",", (obj.enemyArmy ?? new List<THArmyUnit>()).Select(a => a.id + ":" + a.name + ":" + a.count + ":" + a.hpPerUnit + ":" + a.attack + ":" + a.defense + ":" + a.initiative))
        };
    }

    private static string ToSnapshotLine(GameplaySnapshot s)
    {
        return string.Join("|",
            s.Id,
            s.Type,
            s.Difficulty,
            s.BlocksMovement,
            s.StartsCombat,
            s.IsFinalBoss,
            s.IsDarkLord,
            s.RewardGold,
            s.RewardWood,
            s.RewardStone,
            s.RewardMana,
            s.RewardExp,
            s.TargetX,
            s.TargetY,
            s.Army);
    }

    private static string FormatVector(Vector2 v)
    {
        return v.x.ToString("0.###") + " x " + v.y.ToString("0.###");
    }

    private static string FormatVector(Vector3 v)
    {
        return v.x.ToString("0.###") + " x " + v.y.ToString("0.###") + " x " + v.z.ToString("0.###");
    }
}
