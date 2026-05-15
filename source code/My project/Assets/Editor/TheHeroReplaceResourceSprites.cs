using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Reflection;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class TheHeroReplaceResourceSprites
{
    private const string LogPrefix = "[TheHeroResourceSprites]";
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string ReportPath = "Assets/CodeAudit/Resource_Sprites_Replacement_Report.md";
    private const string CircleMenuPath = "Assets/ExternalAssets/MainAssets/Circle_menu.png";
    private const string WoodPath = "Assets/ExternalAssets/MainAssets/Wood Resource.png";
    private const string RockPath = "Assets/ExternalAssets/MainAssets/Rock1.png";

    private enum ResourceKind
    {
        Unknown,
        Gold,
        Wood,
        Stone,
        Mana
    }

    private sealed class SpriteSelection
    {
        public ResourceKind Kind;
        public string AssetPath;
        public string PreferredName;
        public string ReportedName;
        public Sprite Sprite;
        public string SelectionNote;
        public bool Missing;
        public bool WholeSheet;
    }

    private sealed class ResourceCandidate
    {
        public GameObject GameObject;
        public THMapObject MapObject;
        public THResource Resource;
        public string IdentityText;
        public ResourceKind Kind;
        public string SkipReason;
        public bool FromNameOnly;
    }

    private sealed class ReplacementRecord
    {
        public string ObjectPath;
        public ResourceKind Kind;
        public string SpritePath;
        public string SpriteName;
        public string RendererPath;
        public string Scale;
        public float MaxWorldSize;
        public bool CreatedRenderer;
        public bool WholeSheet;
    }

    private sealed class SkipRecord
    {
        public string ObjectPath;
        public string Reason;
    }

    private sealed class RunResult
    {
        public Dictionary<ResourceKind, SpriteSelection> Sprites = new Dictionary<ResourceKind, SpriteSelection>();
        public Dictionary<ResourceKind, int> Counts = new Dictionary<ResourceKind, int>();
        public List<ReplacementRecord> Replacements = new List<ReplacementRecord>();
        public List<SkipRecord> Skipped = new List<SkipRecord>();
        public List<string> Warnings = new List<string>();
        public List<string> ImportSettingsChanges = new List<string>();
        public List<string> Validation = new List<string>();
        public List<string> CriticalFailures = new List<string>();
        public bool MapSaved;
    }

    [MenuItem("The Hero/Map/Replace Resource Sprites")]
    public static void ReplaceResourceSprites()
    {
        var result = new RunResult();
        foreach (ResourceKind kind in new[] { ResourceKind.Gold, ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Mana })
        {
            result.Counts[kind] = 0;
        }

        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        result.Sprites = LoadSpriteSelections(result);

        LogSelectedSprite(result.Sprites[ResourceKind.Gold], "Gold");
        LogSelectedSprite(result.Sprites[ResourceKind.Mana], "Mana");
        LogSelectedSprite(result.Sprites[ResourceKind.Wood], "Wood");
        LogSelectedSprite(result.Sprites[ResourceKind.Stone], "Stone");

        var candidates = FindResourceCandidates(scene, result);
        var beforeGameplay = CaptureGameplaySnapshot(scene);
        var beforeResourcePositions = CaptureResourcePositions(candidates);

        foreach (ResourceCandidate candidate in candidates)
        {
            if (candidate.Kind == ResourceKind.Unknown)
            {
                result.Skipped.Add(new SkipRecord
                {
                    ObjectPath = GetPath(candidate.GameObject),
                    Reason = string.IsNullOrEmpty(candidate.SkipReason) ? "Resource type could not be determined." : candidate.SkipReason
                });
                continue;
            }

            if (!result.Sprites.TryGetValue(candidate.Kind, out SpriteSelection selection) || selection.Sprite == null)
            {
                result.Skipped.Add(new SkipRecord
                {
                    ObjectPath = GetPath(candidate.GameObject),
                    Reason = candidate.Kind + " sprite is missing; current map object was preserved."
                });
                continue;
            }

            ReplaceSprite(candidate, selection, result);
        }

        ValidateGameplay(scene, beforeGameplay, beforeResourcePositions, candidates, result);
        ValidateAssignedSprites(result);

        if (result.CriticalFailures.Count == 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            result.MapSaved = EditorSceneManager.SaveScene(scene, MapScenePath);
            if (result.MapSaved)
            {
                Debug.Log(LogPrefix + " Map saved");
            }
            else
            {
                result.CriticalFailures.Add("Map.unity was not saved by EditorSceneManager.SaveScene.");
                Debug.LogError(LogPrefix + " Map save failed");
            }
        }
        else
        {
            Debug.LogError(LogPrefix + " Map was not saved because validation failed.");
        }

        Debug.Log(LogPrefix + " Replaced Gold resources: " + result.Counts[ResourceKind.Gold]);
        Debug.Log(LogPrefix + " Replaced Wood resources: " + result.Counts[ResourceKind.Wood]);
        Debug.Log(LogPrefix + " Replaced Stone resources: " + result.Counts[ResourceKind.Stone]);
        Debug.Log(LogPrefix + " Replaced Mana resources: " + result.Counts[ResourceKind.Mana]);

        WriteReport(result);
        AssetDatabase.Refresh();

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(result.CriticalFailures.Count == 0 ? 0 : 1);
        }
    }

    private static Dictionary<ResourceKind, SpriteSelection> LoadSpriteSelections(RunResult result)
    {
        var selections = new Dictionary<ResourceKind, SpriteSelection>
        {
            [ResourceKind.Gold] = SelectCircleSprite(ResourceKind.Gold, "Circle_menu_44", new Color(1f, 0.75f, 0.12f), result),
            [ResourceKind.Mana] = SelectCircleSprite(ResourceKind.Mana, "Circle_menu_42", new Color(0.20f, 0.55f, 1f), result),
            [ResourceKind.Wood] = SelectExactSprite(ResourceKind.Wood, WoodPath, "Wood Resource_0", result),
            [ResourceKind.Stone] = SelectExactSprite(ResourceKind.Stone, RockPath, "Rock1_0", result)
        };

        return selections;
    }

    private static SpriteSelection SelectCircleSprite(ResourceKind kind, string preferredName, Color fallbackTarget, RunResult result)
    {
        var sprites = LoadSprites(CircleMenuPath, result);
        Sprite preferred = sprites.FirstOrDefault(s => s != null && s.name == preferredName);
        string note = "Preferred sub-sprite selected.";

        if (preferred == null)
        {
            preferred = PickClosestByColor(CircleMenuPath, sprites.Where(s => s != null && s.name.StartsWith("Circle_menu_", StringComparison.Ordinal)).ToList(), fallbackTarget, kind);
            note = preferred != null
                ? "Preferred sub-sprite missing; color fallback selected."
                : "Preferred sub-sprite missing; no safe Circle_menu_N fallback found.";
        }

        var selection = new SpriteSelection
        {
            Kind = kind,
            AssetPath = CircleMenuPath,
            PreferredName = preferredName,
            ReportedName = preferred != null ? preferred.name : preferredName,
            Sprite = preferred,
            SelectionNote = note,
            Missing = preferred == null,
            WholeSheet = IsWholePngSheet(preferred)
        };

        if (selection.Missing)
        {
            string message = "Missing " + kind + " sprite in " + CircleMenuPath + " (preferred " + preferredName + ").";
            result.Warnings.Add(message);
            Debug.LogError(LogPrefix + " " + message);
        }
        else if (!preferred.name.StartsWith("Circle_menu_", StringComparison.Ordinal))
        {
            string message = kind + " fallback is not a Circle_menu_N sub-sprite: " + preferred.name;
            result.Warnings.Add(message);
            Debug.LogError(LogPrefix + " " + message);
        }

        return selection;
    }

    private static SpriteSelection SelectExactSprite(ResourceKind kind, string assetPath, string spriteName, RunResult result)
    {
        var sprites = LoadSprites(assetPath, result);
        Sprite sprite = sprites.FirstOrDefault(s => s != null && s.name == spriteName);
        string note = "Exact requested sprite selected.";
        string reportedName = spriteName;

        if (sprite == null && sprites.Count == 1 && !IsWholePngSheet(sprites[0]))
        {
            sprite = sprites[0];
            note = "Unity exposed the single sprite as `" + sprite.name + "`; selected it from LoadAllAssetsAtPath as the requested " + spriteName + " asset sprite.";
            reportedName = spriteName + " (Unity object name: " + sprite.name + ")";
        }

        var selection = new SpriteSelection
        {
            Kind = kind,
            AssetPath = assetPath,
            PreferredName = spriteName,
            ReportedName = reportedName,
            Sprite = sprite,
            SelectionNote = sprite != null ? note : "Exact requested sprite was not found.",
            Missing = sprite == null,
            WholeSheet = IsWholePngSheet(sprite)
        };

        if (selection.Missing)
        {
            string message = "Missing " + kind + " sprite " + spriteName + " in " + assetPath + ".";
            result.Warnings.Add(message);
            Debug.LogError(LogPrefix + " " + message);
        }

        return selection;
    }

    private static List<Sprite> LoadSprites(string assetPath, RunResult result)
    {
        if (!File.Exists(ToFullPath(assetPath)))
        {
            string message = "Asset does not exist: " + assetPath;
            result.Warnings.Add(message);
            Debug.LogError(LogPrefix + " " + message);
            return new List<Sprite>();
        }

        EnsureSpriteImportSettings(assetPath, result);

        return AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .Where(s => s != null)
            .OrderBy(s => s.name, StringComparer.Ordinal)
            .ToList();
    }

    private static void EnsureSpriteImportSettings(string assetPath, RunResult result)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            string message = "TextureImporter missing for " + assetPath;
            result.Warnings.Add(message);
            Debug.LogError(LogPrefix + " " + message);
            return;
        }

        bool changed = false;
        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (HasMetaSlicing(assetPath) && importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            changed = true;
        }

        if (importer.filterMode != FilterMode.Point)
        {
            importer.filterMode = FilterMode.Point;
            changed = true;
        }

        if (importer.textureCompression != TextureImporterCompression.Uncompressed)
        {
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            changed = true;
        }

        if (!changed) return;

        importer.SaveAndReimport();
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        result.ImportSettingsChanges.Add(assetPath + " import settings normalized to expose sliced sprites.");
    }

    private static bool HasMetaSlicing(string assetPath)
    {
        string metaPath = ToFullPath(assetPath + ".meta");
        if (!File.Exists(metaPath)) return false;

        string text = File.ReadAllText(metaPath);
        return text.Contains("spriteSheet:") && text.Contains("nameFileIdTable:") && text.Contains("spriteID:");
    }

    private static Sprite PickClosestByColor(string assetPath, List<Sprite> sprites, Color target, ResourceKind kind)
    {
        if (sprites.Count == 0) return null;

        Texture2D readable = LoadReadablePng(assetPath);
        if (readable == null)
        {
            return sprites.OrderBy(s => s.name, StringComparer.Ordinal).FirstOrDefault();
        }

        return sprites
            .Select(s => new { Sprite = s, Score = ScoreSpriteColor(readable, s, target, kind) })
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Sprite.name, StringComparer.Ordinal)
            .Select(x => x.Sprite)
            .FirstOrDefault();
    }

    private static Texture2D LoadReadablePng(string assetPath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(ToFullPath(assetPath));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return texture.LoadImage(bytes) ? texture : null;
        }
        catch
        {
            return null;
        }
    }

    private static float ScoreSpriteColor(Texture2D texture, Sprite sprite, Color target, ResourceKind kind)
    {
        Rect rect = sprite.rect;
        int xMin = Mathf.Clamp(Mathf.FloorToInt(rect.xMin), 0, texture.width - 1);
        int yMin = Mathf.Clamp(Mathf.FloorToInt(rect.yMin), 0, texture.height - 1);
        int width = Mathf.Clamp(Mathf.RoundToInt(rect.width), 1, texture.width - xMin);
        int height = Mathf.Clamp(Mathf.RoundToInt(rect.height), 1, texture.height - yMin);

        Color[] pixels = texture.GetPixels(xMin, yMin, width, height);
        float r = 0f;
        float g = 0f;
        float b = 0f;
        int count = 0;

        foreach (Color pixel in pixels)
        {
            if (pixel.a < 0.2f) continue;
            r += pixel.r;
            g += pixel.g;
            b += pixel.b;
            count++;
        }

        if (count == 0) return -1000f;

        r /= count;
        g /= count;
        b /= count;

        float distance = Mathf.Abs(r - target.r) + Mathf.Abs(g - target.g) + Mathf.Abs(b - target.b);
        float saturation = Mathf.Max(r, g, b) - Mathf.Min(r, g, b);
        float roleBias = kind == ResourceKind.Gold ? (r + g - b) : (b + 0.4f * g - r);
        return roleBias + saturation - distance;
    }

    private static List<ResourceCandidate> FindResourceCandidates(Scene scene, RunResult result)
    {
        var candidates = new List<ResourceCandidate>();

        foreach (GameObject go in EnumerateSceneObjects(scene))
        {
            THMapObject mapObject = go.GetComponent<THMapObject>();
            THResource resource = go.GetComponent<THResource>();
            bool fromNameOnly = mapObject == null && resource == null;

            if (fromNameOnly && !LooksLikeStandaloneResourceObject(go)) continue;

            string identity = BuildIdentityText(go, mapObject, resource);
            var candidate = new ResourceCandidate
            {
                GameObject = go,
                MapObject = mapObject,
                Resource = resource,
                IdentityText = identity,
                FromNameOnly = fromNameOnly
            };

            candidate.Kind = DetermineKind(candidate, out string skipReason);
            candidate.SkipReason = skipReason;

            bool protectedTreasure = IsProtectedTreasureText(identity) ||
                                     (mapObject != null &&
                                      (mapObject.type == THMapObject.ObjectType.Treasure ||
                                       mapObject.type == THMapObject.ObjectType.ArtifactChest ||
                                       mapObject.type == THMapObject.ObjectType.Artifact));

            if (candidate.Kind != ResourceKind.Unknown || resource != null || IsResourceLike(identity) || protectedTreasure)
            {
                if (fromNameOnly && candidate.Kind == ResourceKind.Unknown) continue;
                candidates.Add(candidate);
            }
        }

        result.Validation.Add("Resource candidates found: " + candidates.Count);
        return candidates
            .OrderBy(c => GetPath(c.GameObject), StringComparer.Ordinal)
            .ToList();
    }

    private static IEnumerable<GameObject> EnumerateSceneObjects(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                yield return transform.gameObject;
            }
        }
    }

    private static bool LooksLikeStandaloneResourceObject(GameObject go)
    {
        if (go.GetComponentInParent<Canvas>() != null) return false;
        if (go.GetComponent<RectTransform>() != null) return false;
        if (go.GetComponentInParent<THMapObject>() != null) return false;
        if (go.GetComponent<SpriteRenderer>() == null && go.GetComponentInChildren<SpriteRenderer>(true) == null) return false;

        string text = BuildIdentityText(go, null, null);
        return IsResourceLike(text) && !IsProtectedTreasureText(text);
    }

    private static ResourceKind DetermineKind(ResourceCandidate candidate, out string skipReason)
    {
        skipReason = string.Empty;

        if (candidate.MapObject != null)
        {
            switch (candidate.MapObject.type)
            {
                case THMapObject.ObjectType.GoldResource:
                    return ResourceKind.Gold;
                case THMapObject.ObjectType.WoodResource:
                    return ResourceKind.Wood;
                case THMapObject.ObjectType.StoneResource:
                    return ResourceKind.Stone;
                case THMapObject.ObjectType.ManaResource:
                    return ResourceKind.Mana;
                case THMapObject.ObjectType.Treasure:
                case THMapObject.ObjectType.ArtifactChest:
                case THMapObject.ObjectType.Artifact:
                    skipReason = "Protected chest/artifact object.";
                    return ResourceKind.Unknown;
            }
        }

        string text = candidate.IdentityText;

        if (candidate.Resource != null)
        {
            ResourceKind byResource = KindFromText(candidate.Resource.resourceType);
            if (byResource != ResourceKind.Unknown) return byResource;
        }

        ResourceKind byIdentity = KindFromText(text);
        if (byIdentity != ResourceKind.Unknown) return byIdentity;

        if (IsProtectedTreasureText(text))
        {
            skipReason = "Protected chest/treasure/artifact object.";
            return ResourceKind.Unknown;
        }

        skipReason = "Resource-like object, but Gold/Wood/Stone/Mana type could not be determined.";
        return ResourceKind.Unknown;
    }

    private static ResourceKind KindFromText(string text)
    {
        if (string.IsNullOrEmpty(text)) return ResourceKind.Unknown;
        string lower = text.ToLowerInvariant();

        if (ContainsAny(lower, "goldresource", "res_gold", "gold", "coin", "золото")) return ResourceKind.Gold;
        if (ContainsAny(lower, "woodresource", "res_wood", "wood", "дерево", "древес")) return ResourceKind.Wood;
        if (ContainsAny(lower, "stoneresource", "res_stone", "stone", "rock", "камень", "камн")) return ResourceKind.Stone;
        if (ContainsAny(lower, "manaresource", "res_mana", "mana", "crystal", "gem", "мана", "кристалл")) return ResourceKind.Mana;

        return ResourceKind.Unknown;
    }

    private static bool IsResourceLike(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string lower = text.ToLowerInvariant();
        return ContainsAny(
            lower,
            "resource", "res_", "gold", "wood", "stone", "rock", "mana", "crystal",
            "золото", "дерево", "древес", "камень", "мана", "кристалл");
    }

    private static bool IsProtectedTreasureText(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        string lower = text.ToLowerInvariant();
        return ContainsAny(lower, "chest", "treasure", "artifact", "сундук", "артефакт");
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(n => text.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static string BuildIdentityText(GameObject go, THMapObject mapObject, THResource resource)
    {
        var parts = new List<string> { go.name };

        if (mapObject != null)
        {
            parts.Add(mapObject.type.ToString());
            parts.Add(mapObject.id);
            parts.Add(mapObject.displayName);
        }

        if (resource != null)
        {
            parts.Add(resource.resourceType);
        }

        foreach (MonoBehaviour behaviour in go.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null) continue;
            AppendFieldValue(parts, behaviour, "resourceType");
            AppendFieldValue(parts, behaviour, "rewardType");
            AppendFieldValue(parts, behaviour, "resourceId");
            AppendFieldValue(parts, behaviour, "displayName");
            AppendFieldValue(parts, behaviour, "objectId");
            AppendFieldValue(parts, behaviour, "id");
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static void AppendFieldValue(List<string> parts, object instance, string fieldName)
    {
        Type type = instance.GetType();
        while (type != null)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                object value = field.GetValue(instance);
                if (value != null) parts.Add(value.ToString());
                return;
            }

            type = type.BaseType;
        }
    }

    private static void ReplaceSprite(ResourceCandidate candidate, SpriteSelection selection, RunResult result)
    {
        SpriteRenderer renderer = FindOrCreateRenderer(candidate, out bool createdRenderer);
        if (renderer == null)
        {
            result.Skipped.Add(new SkipRecord
            {
                ObjectPath = GetPath(candidate.GameObject),
                Reason = "SpriteRenderer could not be found or created."
            });
            return;
        }

        renderer.sprite = selection.Sprite;
        renderer.sortingOrder = 80;
        NormalizeRendererScale(renderer, selection.Sprite);

        float maxWorldSize = MaxWorldSpriteSize(renderer, selection.Sprite);
        bool wholeSheet = IsWholePngSheet(selection.Sprite);

        result.Counts[candidate.Kind]++;
        result.Replacements.Add(new ReplacementRecord
        {
            ObjectPath = GetPath(candidate.GameObject),
            Kind = candidate.Kind,
            SpritePath = AssetDatabase.GetAssetPath(selection.Sprite),
            SpriteName = selection.Sprite.name,
            RendererPath = GetPath(renderer.gameObject),
            Scale = FormatVector(renderer.transform.localScale),
            MaxWorldSize = maxWorldSize,
            CreatedRenderer = createdRenderer,
            WholeSheet = wholeSheet
        });
    }

    private static SpriteRenderer FindOrCreateRenderer(ResourceCandidate candidate, out bool createdRenderer)
    {
        createdRenderer = false;
        GameObject go = candidate.GameObject;

        SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
        if (renderer != null) return renderer;

        renderer = go.GetComponentInChildren<SpriteRenderer>(true);
        if (renderer != null) return renderer;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = Vector3.one;
        createdRenderer = true;
        return visual.AddComponent<SpriteRenderer>();
    }

    private static void NormalizeRendererScale(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null || sprite == null) return;

        float spriteMax = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        if (spriteMax <= 0.0001f) return;

        Transform transform = renderer.transform;
        Vector3 scale = transform.localScale;
        if (!IsFinite(scale) || Mathf.Abs(scale.x) <= 0.0001f || Mathf.Abs(scale.y) <= 0.0001f)
        {
            transform.localScale = Vector3.one;
            return;
        }

        float maxWorld = MaxWorldSpriteSize(renderer, sprite);
        if (maxWorld > 1.2f)
        {
            float targetScale = Mathf.Clamp(0.85f / spriteMax, 0.7f, 1.0f);
            transform.localScale = new Vector3(targetScale, targetScale, scale.z == 0f ? 1f : scale.z);
        }
        else if (maxWorld < 0.30f)
        {
            float targetScale = Mathf.Clamp(0.70f / spriteMax, 1.0f, 1.2f);
            transform.localScale = new Vector3(targetScale, targetScale, scale.z == 0f ? 1f : scale.z);
        }
    }

    private static float MaxWorldSpriteSize(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null || sprite == null) return 0f;
        Vector3 scale = renderer.transform.lossyScale;
        return Mathf.Max(sprite.bounds.size.x * Mathf.Abs(scale.x), sprite.bounds.size.y * Mathf.Abs(scale.y));
    }

    private static bool IsFinite(Vector3 value)
    {
        return !(float.IsNaN(value.x) || float.IsNaN(value.y) || float.IsNaN(value.z) ||
                 float.IsInfinity(value.x) || float.IsInfinity(value.y) || float.IsInfinity(value.z));
    }

    private static Dictionary<string, string> CaptureGameplaySnapshot(Scene scene)
    {
        var snapshot = new Dictionary<string, string>();

        foreach (GameObject go in EnumerateSceneObjects(scene))
        {
            THMapObject mapObject = go.GetComponent<THMapObject>();
            THResource resource = go.GetComponent<THResource>();
            if (mapObject == null && resource == null) continue;

            string key = GetPath(go);
            var parts = new List<string>();
            if (mapObject != null)
            {
                parts.Add("mo.id=" + mapObject.id);
                parts.Add("mo.type=" + mapObject.type);
                parts.Add("mo.difficulty=" + mapObject.difficulty);
                parts.Add("mo.blocksMovement=" + mapObject.blocksMovement);
                parts.Add("mo.startsCombat=" + mapObject.startsCombat);
                parts.Add("mo.isFinalBoss=" + mapObject.isFinalBoss);
                parts.Add("mo.rewardGold=" + mapObject.rewardGold);
                parts.Add("mo.rewardWood=" + mapObject.rewardWood);
                parts.Add("mo.rewardStone=" + mapObject.rewardStone);
                parts.Add("mo.rewardMana=" + mapObject.rewardMana);
                parts.Add("mo.rewardExp=" + mapObject.rewardExp);
                parts.Add("mo.targetX=" + mapObject.targetX);
                parts.Add("mo.targetY=" + mapObject.targetY);
                parts.Add("mo.displayName=" + mapObject.displayName);
                parts.Add("mo.isDarkLord=" + mapObject.isDarkLord);
                parts.Add("mo.army=" + ArmyLine(mapObject));
            }

            if (resource != null)
            {
                parts.Add("res.resourceType=" + resource.resourceType);
                parts.Add("res.amount=" + resource.amount);
                parts.Add("res.collected=" + resource.collected);
            }

            Collider2D collider = go.GetComponent<Collider2D>();
            if (collider != null)
            {
                parts.Add("collider.enabled=" + collider.enabled);
                parts.Add("collider.isTrigger=" + collider.isTrigger);
                if (collider is BoxCollider2D box)
                {
                    parts.Add("box.offset=" + FormatVector(box.offset));
                    parts.Add("box.size=" + FormatVector(box.size));
                }
                else if (collider is CircleCollider2D circle)
                {
                    parts.Add("circle.offset=" + FormatVector(circle.offset));
                    parts.Add("circle.radius=" + F(circle.radius));
                }
            }

            snapshot[key] = string.Join("|", parts);
        }

        return snapshot;
    }

    private static string ArmyLine(THMapObject mapObject)
    {
        if (mapObject.enemyArmy == null || mapObject.enemyArmy.Count == 0) return string.Empty;
        return string.Join(";", mapObject.enemyArmy.Select(a => a.id + ":" + a.name + ":" + a.count));
    }

    private static Dictionary<string, Vector3> CaptureResourcePositions(IEnumerable<ResourceCandidate> candidates)
    {
        return candidates
            .Where(c => c.GameObject != null)
            .GroupBy(c => GetPath(c.GameObject))
            .ToDictionary(g => g.Key, g => g.First().GameObject.transform.position);
    }

    private static void ValidateGameplay(
        Scene scene,
        Dictionary<string, string> beforeGameplay,
        Dictionary<string, Vector3> beforeResourcePositions,
        List<ResourceCandidate> candidates,
        RunResult result)
    {
        Dictionary<string, string> afterGameplay = CaptureGameplaySnapshot(scene);

        foreach (var kv in beforeGameplay)
        {
            if (!afterGameplay.TryGetValue(kv.Key, out string after))
            {
                result.CriticalFailures.Add("Gameplay object disappeared: " + kv.Key);
                continue;
            }

            if (after != kv.Value)
            {
                result.CriticalFailures.Add("Gameplay fields changed: " + kv.Key);
            }
        }

        foreach (var kv in beforeResourcePositions)
        {
            ResourceCandidate candidate = candidates.FirstOrDefault(c => GetPath(c.GameObject) == kv.Key);
            if (candidate == null || candidate.GameObject == null) continue;
            if (candidate.GameObject.transform.position != kv.Value)
            {
                result.CriticalFailures.Add("Resource position changed: " + kv.Key);
            }
        }

        if (result.CriticalFailures.Count == 0)
        {
            result.Validation.Add("PASS: THMapObject/THResource gameplay fields, rewards, collected flags, colliders, and resource positions were unchanged.");
        }
    }

    private static void ValidateAssignedSprites(RunResult result)
    {
        foreach (SpriteSelection selection in result.Sprites.Values)
        {
            if (selection.Sprite == null) continue;
            if (selection.WholeSheet)
            {
                result.CriticalFailures.Add(selection.Kind + " selected sprite is a whole PNG sheet: " + selection.Sprite.name);
            }

            if ((selection.Kind == ResourceKind.Gold || selection.Kind == ResourceKind.Mana) &&
                !selection.Sprite.name.StartsWith("Circle_menu_", StringComparison.Ordinal))
            {
                result.CriticalFailures.Add(selection.Kind + " selected sprite is not a Circle_menu_N sub-sprite: " + selection.Sprite.name);
            }
        }

        foreach (ReplacementRecord record in result.Replacements)
        {
            if (record.WholeSheet)
            {
                result.CriticalFailures.Add(record.ObjectPath + " uses a whole PNG sheet.");
            }

            if (record.MaxWorldSize > 1.2f)
            {
                result.CriticalFailures.Add(record.ObjectPath + " resource sprite is too large: " + F(record.MaxWorldSize) + " world units.");
            }
        }

        bool allVisible = result.Replacements.All(r => r.MaxWorldSize > 0f);
        result.Validation.Add(allVisible ? "PASS: replaced resources have visible non-empty sprites." : "FAIL: at least one replaced resource has an empty sprite size.");
        result.Validation.Add(result.Replacements.Any(r => r.WholeSheet) ? "FAIL: whole PNG sheet detected in replacements." : "PASS: no replacement uses a whole PNG sheet.");
        result.Validation.Add(result.Replacements.Any(r => r.MaxWorldSize > 1.2f) ? "FAIL: oversized resource sprite detected." : "PASS: resource sprites are not oversized.");
    }

    private static void LogSelectedSprite(SpriteSelection selection, string label)
    {
        Debug.Log(LogPrefix + " " + label + " sprite: " + (selection != null && selection.Sprite != null ? selection.Sprite.name : "<missing>"));
    }

    private static bool IsWholePngSheet(Sprite sprite)
    {
        if (sprite == null) return false;
        string path = AssetDatabase.GetAssetPath(sprite);
        if (string.IsNullOrEmpty(path) || !path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return false;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null) return false;

        Rect rect = sprite.rect;
        return Mathf.RoundToInt(rect.width) >= texture.width && Mathf.RoundToInt(rect.height) >= texture.height;
    }

    private static void WriteReport(RunResult result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));

        var sb = new StringBuilder();
        sb.AppendLine("# Resource Sprites Replacement Report");
        sb.AppendLine();
        sb.AppendLine("Generated by `The Hero/Map/Replace Resource Sprites`.");
        sb.AppendLine();

        sb.AppendLine("## Sprite assets used");
        sb.AppendLine("| Resource | Asset path | Preferred | Selected sub-sprite | Note | Whole PNG sheet |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- |");
        foreach (ResourceKind kind in new[] { ResourceKind.Gold, ResourceKind.Wood, ResourceKind.Stone, ResourceKind.Mana })
        {
            SpriteSelection selection = result.Sprites[kind];
            sb.AppendLine("| " + kind + " | `" + selection.AssetPath + "` | `" + selection.PreferredName + "` | `" +
                          (selection.Sprite != null ? selection.ReportedName : "<missing>") + "` | " +
                          selection.SelectionNote + " | " + (selection.WholeSheet ? "YES" : "NO") + " |");
        }
        sb.AppendLine();

        sb.AppendLine("## Replacement counts");
        sb.AppendLine("- Gold resources replaced: " + result.Counts[ResourceKind.Gold]);
        sb.AppendLine("- Wood resources replaced: " + result.Counts[ResourceKind.Wood]);
        sb.AppendLine("- Stone resources replaced: " + result.Counts[ResourceKind.Stone]);
        sb.AppendLine("- Mana resources replaced: " + result.Counts[ResourceKind.Mana]);
        sb.AppendLine();

        sb.AppendLine("## Replaced objects");
        sb.AppendLine("| Object | Kind | Sprite | Renderer | Scale | Max world size | Created renderer |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
        foreach (ReplacementRecord record in result.Replacements.OrderBy(r => r.Kind).ThenBy(r => r.ObjectPath, StringComparer.Ordinal))
        {
            sb.AppendLine("| `" + record.ObjectPath + "` | " + record.Kind + " | `" + record.SpritePath + "` / `" + record.SpriteName +
                          "` | `" + record.RendererPath + "` | " + record.Scale + " | " +
                          F(record.MaxWorldSize) + " | " + (record.CreatedRenderer ? "YES" : "NO") + " |");
        }
        if (result.Replacements.Count == 0) sb.AppendLine("| - | - | - | - | - | - | - |");
        sb.AppendLine();

        sb.AppendLine("## Skipped objects");
        if (result.Skipped.Count == 0)
        {
            sb.AppendLine("- None.");
        }
        else
        {
            foreach (SkipRecord skip in result.Skipped.OrderBy(s => s.ObjectPath, StringComparer.Ordinal))
            {
                sb.AppendLine("- `" + skip.ObjectPath + "`: " + skip.Reason);
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Whole PNG sheet check");
        var wholeSheets = result.Replacements.Where(r => r.WholeSheet).ToList();
        if (wholeSheets.Count == 0 && result.Sprites.Values.All(s => !s.WholeSheet))
        {
            sb.AppendLine("- No whole PNG sheets were selected or assigned.");
        }
        else
        {
            foreach (SpriteSelection selection in result.Sprites.Values.Where(s => s.WholeSheet))
            {
                sb.AppendLine("- Selected " + selection.Kind + " sprite is whole sheet: `" + selection.ReportedName + "`.");
            }
            foreach (ReplacementRecord record in wholeSheets)
            {
                sb.AppendLine("- Assigned whole sheet on `" + record.ObjectPath + "`.");
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Validation");
        foreach (string line in result.Validation)
        {
            sb.AppendLine("- " + line);
        }
        if (result.ImportSettingsChanges.Count == 0)
        {
            sb.AppendLine("- Import settings changed: NO");
        }
        else
        {
            sb.AppendLine("- Import settings changed: YES, only to expose requested sliced sprites:");
            foreach (string change in result.ImportSettingsChanges.Distinct().OrderBy(c => c, StringComparer.Ordinal))
            {
                sb.AppendLine("  - " + change);
            }
        }
        sb.AppendLine("- Map.unity saved: " + (result.MapSaved ? "YES" : "NO"));
        sb.AppendLine("- Map was not rebuilt; this tool opened the existing scene and changed only resource SpriteRenderer sprite/sorting/scale.");
        sb.AppendLine("- Hero, enemies, castle, UI, tilemaps, rewards, collected flags, colliders, interaction scripts, positions, and parents were not intentionally modified.");
        sb.AppendLine();

        sb.AppendLine("## Warnings");
        if (result.Warnings.Count == 0)
        {
            sb.AppendLine("- None.");
        }
        else
        {
            foreach (string warning in result.Warnings.Distinct().OrderBy(w => w, StringComparer.Ordinal))
            {
                sb.AppendLine("- " + warning);
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Critical failures");
        if (result.CriticalFailures.Count == 0)
        {
            sb.AppendLine("- None.");
        }
        else
        {
            foreach (string failure in result.CriticalFailures.Distinct().OrderBy(f => f, StringComparer.Ordinal))
            {
                sb.AppendLine("- " + failure);
            }
        }

        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
    }

    private static string GetPath(GameObject go)
    {
        if (go == null) return "<null>";
        var parts = new Stack<string>();
        Transform current = go.transform;
        while (current != null)
        {
            parts.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", parts);
    }

    private static string FormatVector(Vector2 value)
    {
        return "(" + F(value.x) + ", " + F(value.y) + ")";
    }

    private static string FormatVector(Vector3 value)
    {
        return "(" + F(value.x) + ", " + F(value.y) + ", " + F(value.z) + ")";
    }

    private static string F(float value)
    {
        return value.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string ToFullPath(string assetPath)
    {
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
    }
}
