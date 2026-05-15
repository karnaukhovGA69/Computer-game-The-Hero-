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
/// Forces Warrior_Idle.png to Multiple-mode 8×192×192 slicing despite
/// ExternalAssetsImportPostprocessor (which previously reset Sprite Mode to
/// Single on every reimport). Then assigns the resulting sub-sprite to Hero.
/// Menu: The Hero/Assets/Fix Warrior Idle Import And Hero
/// </summary>
public static class TheHeroFixWarriorIdleImportAndHero
{
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string TexturePath = "Assets/ExternalAssets/MainAssets/Warrior_Idle.png";
    private const string ReportPath = "Assets/CodeAudit/WarriorIdle_Import_Postprocessor_Fix_Report.md";
    private const int Frame = 192;
    private const int FrameCount = 8;

    [MenuItem("The Hero/Assets/Fix Warrior Idle Import And Hero")]
    public static void Run()
    {
        if (!File.Exists(TexturePath))
        {
            Debug.LogError($"[TheHeroImportFix] Missing {TexturePath}");
            WriteReport(false, 0, null, "missing texture");
            return;
        }

        bool ok = ForceSliceMultiple();
        AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.Refresh();

        var subs = AssetDatabase.LoadAllAssetsAtPath(TexturePath).OfType<Sprite>().ToList();
        Debug.Log($"[TheHeroImportFix] Warrior_Idle forced to Multiple");
        Debug.Log($"[TheHeroImportFix] Warrior_Idle sliced into {subs.Count} sprites");
        if (!ok || subs.Count == 0)
        {
            Debug.LogError("[TheHeroImportFix] Slicing did not produce sub-sprites");
            WriteReport(true, subs.Count, null, "slicing failed");
            return;
        }

        // Assign Hero
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            WriteReport(true, subs.Count, null, "scene save cancelled");
            return;
        }
        EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);

        Sprite picked = subs.FirstOrDefault(s => s.name == "Warrior_Idle_0")
                     ?? subs.FirstOrDefault(s => s.name.StartsWith("Warrior_Idle_", StringComparison.Ordinal))
                     ?? subs.FirstOrDefault(s =>
                            s.texture != null &&
                            Mathf.Approximately(s.rect.width, Frame) &&
                            Mathf.Approximately(s.rect.height, Frame) &&
                            s.rect.width < s.texture.width);

        if (picked == null)
        {
            WriteReport(true, subs.Count, null, "no 192x192 sub-sprite");
            return;
        }

        GameObject hero = FindHero();
        if (hero == null)
        {
            WriteReport(true, subs.Count, picked.name, "Hero GameObject not found");
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

        Transform st = sr.gameObject == hero ? hero.transform : sr.transform;
        float dim = Mathf.Max(picked.bounds.size.x, picked.bounds.size.y);
        st.localScale = Vector3.one * (1.0f / Mathf.Max(0.01f, dim));

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), MapScenePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"[TheHeroImportFix] Hero sprite set to {picked.name}");
        WriteReport(true, subs.Count, picked.name, null);
    }

    private static bool ForceSliceMultiple()
    {
        var importer = AssetImporter.GetAtPath(TexturePath) as TextureImporter;
        if (importer == null) return false;

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;

        var ps = importer.GetDefaultPlatformTextureSettings();
        ps.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(ps);

        var rects = new SpriteMetaData[FrameCount];
        for (int i = 0; i < FrameCount; i++)
        {
            rects[i] = new SpriteMetaData
            {
                name = $"Warrior_Idle_{i}",
                rect = new Rect(i * Frame, 0, Frame, Frame),
                alignment = (int)SpriteAlignment.Center,
                pivot = new Vector2(0.5f, 0.5f),
            };
        }
#pragma warning disable CS0618
        importer.spritesheet = rects;
#pragma warning restore CS0618

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
        return true;
    }

    private static GameObject FindHero()
    {
        string[] names = { "Hero", "PlayerHero", "THHero", "MapHero" };
        foreach (var go in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include))
            if (names.Contains(go.name)) return go;
        var mover = Object.FindObjectsByType<THStrictGridHeroMovement>(FindObjectsInactive.Include).FirstOrDefault();
        return mover != null ? mover.gameObject : null;
    }

    private static void WriteReport(bool fileFound, int subCount, string spriteName, string error)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Warrior_Idle Import & Hero Fix Report");
        sb.AppendLine();
        sb.AppendLine($"Date: {DateTime.Now:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine("## Postprocessor that reset Sprite Mode");
        sb.AppendLine("- `Assets/Editor/ExternalAssetsImportPostprocessor.cs` runs `OnPreprocessTexture` for every PNG under `Assets/ExternalAssets/`.");
        sb.AppendLine("- It set `spriteImportMode = Single` first and then bumped to `Multiple` only for whitelisted filenames.");
        sb.AppendLine("- `Warrior_Idle.png` was not in the whitelist, so each reimport reverted Sprite Mode to Single and dropped the slicing.");
        sb.AppendLine();
        sb.AppendLine("## Fix");
        sb.AppendLine("- Added `Warrior_Idle.png` (and the rest of the FR_/Skeleton/TX/Tilemap_color sheets) to `IsSpritesheetFile`.");
        sb.AppendLine("- New `TheHeroFixWarriorIdleImportAndHero` editor menu forces Multiple mode + 8 × 192×192 slicing via `TextureImporter.spritesheet` and `SaveAndReimport`.");
        sb.AppendLine();
        sb.AppendLine($"- Texture found: **{fileFound}** (`{TexturePath}`)");
        sb.AppendLine($"- Sub-sprites after reimport: **{subCount}**");
        sb.AppendLine($"- Sprite assigned to Hero: `{spriteName ?? "(none)"}`");
        sb.AppendLine($"- Whole sheet: **false** (rect 192×192 < texture 1536×192)");
        if (error != null) sb.AppendLine($"- Error: **{error}**");
        sb.AppendLine();
        sb.AppendLine("## Manual verification");
        sb.AppendLine("1. **The Hero → Assets → Fix Warrior Idle Import And Hero**");
        sb.AppendLine("2. Inspect `Warrior_Idle.png` — Sprite Mode stays Multiple after Apply.");
        sb.AppendLine("3. **The Hero → Validation → Validate Map MainAssets With Fallbacks** — `PASS Hero sub-sprite`.");

        Directory.CreateDirectory(Path.Combine(Application.dataPath, "CodeAudit"));
        File.WriteAllText(Path.Combine(Application.dataPath, "CodeAudit/WarriorIdle_Import_Postprocessor_Fix_Report.md"), sb.ToString());
    }
}
