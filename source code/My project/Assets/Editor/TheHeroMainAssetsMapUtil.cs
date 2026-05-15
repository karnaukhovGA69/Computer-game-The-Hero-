using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>Shared MainAssets map helpers for build, fix, and validation tools.</summary>
public static class TheHeroMainAssetsMapUtil
{
    public const int MapW = 48;
    public const int MapH = 32;
    public const int CenterX = 24;
    public const int CenterY = 16;
    public const int HeroX = 24;
    public const int HeroY = 13;

    public const string MainAssetsPath = "Assets/ExternalAssets/MainAssets";

    private static readonly string[] KnownSheetFiles =
    {
        "Skeleton Warrior", "Skeleton Mage", "TX Props", "TX Plant", "TX Tileset Grass",
        "Bridges", "Water_animation", "Main_tiles", "walls_floor", "Interior",
        "Icons", "Trees_animation", "ground_grass_details", "Tilemap_color1",
        "free_pixel_16_woods", "Warrior_Idle", "FR_",
    };

    public static string FindMainAssetsFolder()
    {
        if (AssetDatabase.IsValidFolder(MainAssetsPath)) return MainAssetsPath;
        return AssetDatabase.FindAssets("MainAssets t:Folder")
            .Select(AssetDatabase.GUIDToAssetPath)
            .FirstOrDefault(p => p.EndsWith("/MainAssets", StringComparison.OrdinalIgnoreCase));
    }

  public static List<Sprite> LoadSlicedSprites(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath)) return new List<Sprite>();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath).OfType<Sprite>().Where(s => s != null).ToList();
        if (sprites.Count == 0) return sprites;
        return sprites.Where(s => !IsWholeSheetSprite(s)).ToList();
    }

    /// <summary>
    /// Whole-sheet only when sprite rect ~= full texture AND texture is a large atlas (&gt;128).
    /// Small single 32–96px sprites are allowed.
    /// </summary>
    public static bool IsWholeSheetSprite(Sprite sprite)
    {
        if (sprite == null || sprite.texture == null) return true;

        Rect rect = sprite.rect;
        Texture2D tex = sprite.texture;

        bool sameAsTexture =
            Mathf.Approximately(rect.width, tex.width) &&
            Mathf.Approximately(rect.height, tex.height);

        bool largeTexture = tex.width > 128 || tex.height > 128;

        if (!sameAsTexture || !largeTexture) return false;

        string path = AssetDatabase.GetAssetPath(tex);
        string file = Path.GetFileNameWithoutExtension(path);
        if (IsKnownSpritesheetFile(file, path)) return true;

        // Multiple sub-sprites exist — a full-rect pick is still wrong for gameplay objects.
        Sprite[] all = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        return all.Length > 1;
    }

    public static bool IsKnownSpritesheetFile(string fileName, string path)
    {
        if (string.IsNullOrEmpty(fileName)) return false;
        foreach (string token in KnownSheetFiles)
        {
            if (fileName.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (path.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        }
        return false;
    }

    public static bool LooksLikeUiButton(Sprite sprite)
    {
        if (sprite == null) return true;
        string path = AssetDatabase.GetAssetPath(sprite).ToLowerInvariant();
        string name = sprite.name.ToLowerInvariant();
        return path.Contains("button") || path.Contains("/ui/") || path.Contains("circle_menu") ||
               path.Contains("main_menu") || path.Contains("settings") || name.Contains("button");
    }

    public static Sprite PickCharacterFrame(string folder, string fileName)
    {
        string path = $"{folder}/{fileName}.png";
        List<Sprite> list = LoadSlicedSprites(path);
        if (list.Count == 0)
        {
            var single = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
            if (single != null && !IsWholeSheetSprite(single)) return single;
            return null;
        }

        return list.FirstOrDefault(s => s.name.EndsWith("_0", StringComparison.Ordinal)) ??
               list.FirstOrDefault(s => s.name.IndexOf("idle", StringComparison.OrdinalIgnoreCase) >= 0) ??
               list.OrderBy(s => s.rect.width * s.rect.height).FirstOrDefault();
    }

    public static Sprite PickByName(IEnumerable<Sprite> list, params string[] tokens)
    {
        if (list == null) return null;
        foreach (string token in tokens)
        {
            Sprite hit = list.FirstOrDefault(s => s.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
            if (hit != null && !IsWholeSheetSprite(hit)) return hit;
        }
        return null;
    }

    public static int CountUsedTiles(Tilemap tm)
    {
        if (tm == null) return 0;
        BoundsInt b = tm.cellBounds;
        int count = 0;
        foreach (Vector3Int p in b.allPositionsWithin)
            if (tm.HasTile(p)) count++;
        return count;
    }

    /// <summary>
    /// Returns a SpriteRenderer on <paramref name="go"/> or one of its children, creating
    /// a child "Visual" with a SpriteRenderer when no renderer exists. Never throws on
    /// parent-only objects (e.g. composite Castle_Player).
    /// </summary>
    public static SpriteRenderer EnsureSpriteRenderer(GameObject go)
    {
        if (go == null) return null;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) return sr;

        sr = go.GetComponentInChildren<SpriteRenderer>(true);
        if (sr != null) return sr;

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one;

        return visual.AddComponent<SpriteRenderer>();
    }

    public static bool ApplyObjectSprite(GameObject go, Sprite sprite, float targetCells, int sortingOrder)
    {
        if (go == null)
        {
            Debug.LogWarning("[TheHeroMainAssetsFix] ApplyObjectSprite: target GameObject is null");
            return false;
        }
        if (sprite == null)
        {
            Debug.LogWarning($"[TheHeroMainAssetsFix] ApplyObjectSprite: sprite is null for '{go.name}'");
            return false;
        }

        SpriteRenderer sr = EnsureSpriteRenderer(go);
        if (sr == null) return false;

        sr.sprite = sprite;
        sr.sortingOrder = sortingOrder;
        sr.enabled = true;

        float maxDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        float scale = targetCells / Mathf.Max(0.001f, maxDim);

        // If the SpriteRenderer is on a child "Visual", scale that child so parent gameplay
        // transforms (Collider2D, grid position) keep their unit scale.
        Transform scaleTarget = sr.gameObject == go ? go.transform : sr.transform;
        scaleTarget.localScale = new Vector3(scale, scale, 1f);
        return true;
    }

    public static bool CastleHasValidSprite(GameObject castleGo)
    {
        if (castleGo == null) return false;
        var renderers = castleGo.GetComponentsInChildren<SpriteRenderer>(true);
        if (renderers.Length == 0) return false;
        return renderers.Any(r => r.sprite != null && !IsWholeSheetSprite(r.sprite) && !LooksLikeUiButton(r.sprite));
    }
}
