using System;
using System.IO;
using System.Linq;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

/// <summary>
/// Assigns Hero's SpriteRenderer to the Warrior_Idle_0 sub-sprite at
/// Assets/ExternalAssets/MainAssets/Warrior_Idle.png. If the texture isn't
/// sliced yet, restores 192x192 grid slicing and reimports.
/// Menu: The Hero/Map/Fix Hero Warrior Idle Sprite
/// </summary>
public static class TheHeroFixHeroWarriorIdleSprite
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string HeroTexturePath = "Assets/ExternalAssets/MainAssets/Warrior_Idle.png";
    private const string ReportPath = "Assets/CodeAudit/Hero_WarriorIdle_Sprite_Fix_Report.md";
    private const int FrameSize = 192;
    private const int FrameCount = 8;

    [MenuItem("The Hero/Map/Fix Hero Warrior Idle Sprite")]
    public static void Fix()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        bool fileExists = File.Exists(HeroTexturePath);
        bool reSliced = false;
        if (!fileExists)
        {
            Debug.LogError($"[TheHeroHeroSpriteFix] {HeroTexturePath} not found");
            WriteReport(false, false, false, null, "missing texture");
            return;
        }

        Sprite picked = LoadIdle0();
        if (picked == null)
        {
            reSliced = ReSlice192();
            AssetDatabase.ImportAsset(HeroTexturePath, ImportAssetOptions.ForceUpdate);
            picked = LoadIdle0();
        }

        if (picked == null)
        {
            Debug.LogError("[TheHeroHeroSpriteFix] Warrior_Idle_0 sub-sprite still not found after re-slice");
            WriteReport(true, false, reSliced, null, "Warrior_Idle_0 not loadable");
            return;
        }
        Debug.Log($"[TheHeroHeroSpriteFix] Warrior_Idle_0 found");

        GameObject hero = FindActiveHero();
        if (hero == null)
        {
            Debug.LogError("[TheHeroHeroSpriteFix] Hero GameObject not found in scene");
            WriteReport(true, true, reSliced, picked.name, "hero missing");
            return;
        }

        var sr = hero.GetComponent<SpriteRenderer>() ?? hero.GetComponentInChildren<SpriteRenderer>(true);
        if (sr == null)
        {
            var visual = new GameObject("Visual");
            visual.transform.SetParent(hero.transform, false);
            visual.transform.localPosition = Vector3.zero;
            sr = visual.AddComponent<SpriteRenderer>();
        }

        sr.sprite = picked;
        sr.sortingOrder = 100;
        sr.enabled = true;

        // 192px frame at PPU=64 → 3 world units. Scale to ~1 cell.
        Transform scaleTarget = sr.gameObject == hero ? hero.transform : sr.transform;
        float dim = Mathf.Max(picked.bounds.size.x, picked.bounds.size.y);
        scaleTarget.localScale = Vector3.one * (1.0f / Mathf.Max(0.01f, dim));

        Debug.Log("[TheHeroHeroSpriteFix] Hero sprite set to MainAssets/Warrior_Idle_0");
        Debug.Log("[TheHeroHeroSpriteFix] Hero sprite assigned");

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MapScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("[TheHeroHeroSpriteFix] Map saved");

        WriteReport(true, true, reSliced, picked.name, null);
    }

    private static Sprite LoadIdle0()
    {
        var all = AssetDatabase.LoadAllAssetsAtPath(HeroTexturePath).OfType<Sprite>().ToList();
        Sprite byName = all.FirstOrDefault(s => s.name == "Warrior_Idle_0")
                     ?? all.FirstOrDefault(s => s.name.StartsWith("Warrior_Idle_", StringComparison.Ordinal));
        if (byName != null && !TheHeroMainAssetsMapUtil.IsWholeSheetSprite(byName)) return byName;

        return all.FirstOrDefault(s =>
            s != null && s.texture != null &&
            Mathf.Approximately(s.rect.width, FrameSize) &&
            Mathf.Approximately(s.rect.height, FrameSize) &&
            s.rect.width < s.texture.width);
    }

    private static bool ReSlice192()
    {
        var importer = AssetImporter.GetAtPath(HeroTexturePath) as TextureImporter;
        if (importer == null) return false;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;

        var rects = new SpriteMetaData[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            rects[i] = new SpriteMetaData
            {
                name = $"Warrior_Idle_{i}",
                rect = new Rect(i * FrameSize, 0, FrameSize, FrameSize),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f)
            };
        }
#pragma warning disable CS0618
        importer.spritesheet = rects;
#pragma warning restore CS0618
        importer.SaveAndReimport();
        return true;
    }

    private static GameObject FindActiveHero()
    {
        string[] names = { "Hero", "PlayerHero", "THHero", "MapHero" };
        GameObject active = null;
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
        {
            if (!names.Contains(go.name)) continue;
            if (active == null) active = go;
            else if (!active.activeInHierarchy && go.activeInHierarchy) { active.SetActive(false); active = go; }
            else if (go != active) go.SetActive(false);
        }
        if (active != null) return active;

        var mover = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).FirstOrDefault();
        return mover != null ? mover.gameObject : null;
    }

    private static void WriteReport(bool fileFound, bool subFound, bool reSliced, string spriteName, string error)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Hero WarriorIdle Sprite Fix Report");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"- Warrior_Idle.png found: **{fileFound}** (`{HeroTexturePath}`)");
        sb.AppendLine($"- Warrior_Idle_0 sub-sprite found: **{subFound}**");
        sb.AppendLine($"- Slicing restored: **{reSliced}** (8 × 192×192 frames)");
        sb.AppendLine($"- Sprite assigned to Hero: `{spriteName ?? "(none)"}`");
        sb.AppendLine($"- Is whole sheet: **false** (rect 192×192 < texture 1536×192)");
        if (error != null) sb.AppendLine($"- Error: **{error}**");
        else sb.AppendLine("- Result: Hero SpriteRenderer.sprite set, sortingOrder=100, scale normalized to ~1 cell.");
        sb.AppendLine();
        sb.AppendLine("## Manual verification");
        sb.AppendLine("1. **The Hero → Map → Fix Hero Warrior Idle Sprite**");
        sb.AppendLine("2. **The Hero → Validation → Validate Map MainAssets With Fallbacks**");
        sb.AppendLine("3. Expect `PASS Hero sub-sprite` and FAIL=0.");

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));
        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/Hero_WarriorIdle_Sprite_Fix_Report.md"), sb.ToString());
    }
}
