using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using TheHero.Generated;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class TheHeroReplaceEnemySprites
{
    private const string LogPrefix = "[TheHeroEnemySprites]";
    private const string MapScenePath = "Assets/Scenes/Map.unity";
    private const string MainAssetsRoot = "Assets/ExternalAssets/MainAssets";
    private const string ReportPath = "Assets/CodeAudit/Enemy_Sprites_Replacement_Report.md";

    private const int SortingOrder = 90;
    private const float NormalTargetCells = 1.0f;
    private const float NormalMaxCells = 1.2f;
    private const float BossTargetCells = 1.35f;
    private const float BossMaxCells = 1.5f;

    private enum EnemySpriteRole
    {
        WeakOrc,
        StrongOrc,
        EliteOrc,
        Skeleton,
        SkeletonMage,
        FlyingDark,
        Boss
    }

    private sealed class SourceAsset
    {
        public EnemySpriteRole Role;
        public string RequestedFile;
        public string AssetPath;
        public string SelectedSpriteName;
        public string SelectedRect;
        public bool Found;
        public bool HadMetaSlicing;
        public bool AutoSliced;
        public bool UsedFallback;
        public string Note;
        public Sprite Sprite;
    }

    private sealed class EnemyCandidate
    {
        public GameObject GameObject;
        public THMapObject MapObject;
        public THEnemy Enemy;
        public string Identity;
        public EnemySpriteRole Role;
        public bool UnknownFallback;
        public string RoleReason;
    }

    private sealed class ReplacementRecord
    {
        public string ObjectPath;
        public string Identity;
        public EnemySpriteRole Role;
        public string SpritePath;
        public string SpriteName;
        public string SpriteRect;
        public string RendererPath;
        public string VisualScale;
        public float MaxWorldSize;
        public bool CreatedRenderer;
        public bool UsedFallback;
        public string Note;
    }

    private sealed class SkipRecord
    {
        public string ObjectPath;
        public string Reason;
    }

    private sealed class RunResult
    {
        public readonly Dictionary<EnemySpriteRole, SourceAsset> Sources = new Dictionary<EnemySpriteRole, SourceAsset>();
        public readonly Dictionary<EnemySpriteRole, int> Counts = new Dictionary<EnemySpriteRole, int>();
        public readonly List<ReplacementRecord> Replacements = new List<ReplacementRecord>();
        public readonly List<SkipRecord> Skipped = new List<SkipRecord>();
        public readonly List<string> Warnings = new List<string>();
        public readonly List<string> ImportChanges = new List<string>();
        public readonly List<string> Validation = new List<string>();
        public readonly List<string> CriticalFailures = new List<string>();
        public bool MapSaved;
    }

    [MenuItem("The Hero/Map/Replace Enemy Sprites")]
    public static void ReplaceEnemySprites()
    {
        var result = new RunResult();
        foreach (EnemySpriteRole role in Enum.GetValues(typeof(EnemySpriteRole)))
        {
            result.Counts[role] = 0;
        }

        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        Scene scene = EditorSceneManager.OpenScene(MapScenePath, OpenSceneMode.Single);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            result.CriticalFailures.Add("Could not open " + MapScenePath);
            WriteReport(result);
            Debug.LogError(LogPrefix + " Could not open Map.unity.");
            ExitBatch(result);
            return;
        }

        LoadSourceAssets(result);
        var enemies = FindEnemyCandidates(scene, result);
        Dictionary<string, string> beforeGameplay = CaptureGameplaySnapshot(scene);
        Dictionary<string, Vector3> beforePositions = CaptureEnemyPositions(enemies);
        Dictionary<string, int> beforeTileCounts = CaptureTilemapTileCounts(scene);

        foreach (EnemyCandidate candidate in enemies)
        {
            if (!result.Sources.TryGetValue(candidate.Role, out SourceAsset source) || source.Sprite == null)
            {
                SourceAsset fallback = result.Sources.TryGetValue(EnemySpriteRole.WeakOrc, out SourceAsset weak) ? weak : null;
                if (fallback == null || fallback.Sprite == null)
                {
                    result.Skipped.Add(new SkipRecord
                    {
                        ObjectPath = GetPath(candidate.GameObject),
                        Reason = "No sprite available for " + candidate.Role + ", and weak-orc fallback is missing."
                    });
                    continue;
                }

                candidate.Role = EnemySpriteRole.WeakOrc;
                candidate.UnknownFallback = true;
                candidate.RoleReason += " Sprite for requested role was missing; weak-orc fallback used.";
                source = fallback;
            }

            ReplaceSprite(candidate, source, result);
        }

        ValidateScene(scene, enemies, beforeGameplay, beforePositions, beforeTileCounts, result);

        if (result.CriticalFailures.Count == 0)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            result.MapSaved = EditorSceneManager.SaveScene(scene, MapScenePath);
            AssetDatabase.SaveAssets();
            if (!result.MapSaved)
            {
                result.CriticalFailures.Add("EditorSceneManager.SaveScene returned false for " + MapScenePath);
            }
        }

        WriteReport(result);
        AssetDatabase.Refresh();

        if (result.CriticalFailures.Count == 0)
        {
            Debug.Log(LogPrefix + " Enemy sprite replacement finished. Map saved: " + result.MapSaved);
        }
        else
        {
            Debug.LogError(LogPrefix + " Enemy sprite replacement finished with critical failures. See report.");
        }

        ExitBatch(result);
    }

    private static void LoadSourceAssets(RunResult result)
    {
        result.Sources[EnemySpriteRole.WeakOrc] = LoadRequiredSprite(EnemySpriteRole.WeakOrc, "orc1_idle_full.png", result);
        result.Sources[EnemySpriteRole.StrongOrc] = LoadRequiredSprite(EnemySpriteRole.StrongOrc, "orc2_idle_full.png", result);
        result.Sources[EnemySpriteRole.EliteOrc] = LoadRequiredSprite(EnemySpriteRole.EliteOrc, "orc3_idle_full.png", result);
        result.Sources[EnemySpriteRole.Skeleton] = LoadRequiredSprite(EnemySpriteRole.Skeleton, "Skeleton Warrior.png", result);
        result.Sources[EnemySpriteRole.SkeletonMage] = LoadRequiredSprite(EnemySpriteRole.SkeletonMage, "Skeleton Mage.png", result);

        SourceAsset flying = LoadRequiredSprite(EnemySpriteRole.FlyingDark, "FR_122_ClockworkBat.png", result);
        if (!flying.Found || flying.Sprite == null)
        {
            flying = CloneFallback(EnemySpriteRole.FlyingDark, "FR_122_ClockworkBat.png", result.Sources[EnemySpriteRole.Skeleton], "ClockworkBat missing; Skeleton Warrior first sub-sprite used.");
            result.Warnings.Add("FR_122_ClockworkBat.png was missing or unusable; flying/dark role uses Skeleton Warrior fallback.");
        }
        result.Sources[EnemySpriteRole.FlyingDark] = flying;

        result.Sources[EnemySpriteRole.Boss] = LoadBossSprite(result);
    }

    private static SourceAsset LoadRequiredSprite(EnemySpriteRole role, string fileName, RunResult result)
    {
        string assetPath = MainAssetsRoot + "/" + fileName;
        var source = new SourceAsset
        {
            Role = role,
            RequestedFile = fileName,
            AssetPath = assetPath,
            Found = File.Exists(ToFullPath(assetPath))
        };

        if (!source.Found)
        {
            source.Note = "Missing file.";
            result.Warnings.Add("Missing required PNG: " + assetPath);
            return source;
        }

        source.Sprite = EnsureSlicedAndPickFirst(assetPath, fileName, result, source);
        if (source.Sprite == null)
        {
            source.Note = AppendNote(source.Note, "No usable sub-sprite was exposed after import.");
            result.Warnings.Add("No usable sub-sprite found in " + assetPath);
            return source;
        }

        source.SelectedSpriteName = source.Sprite.name;
        source.SelectedRect = FormatRect(source.Sprite.rect);
        return source;
    }

    private static SourceAsset LoadBossSprite(RunResult result)
    {
        string[] preferredFiles =
        {
            "UndeadExecutioner.png",
            "idle.png",
            "FR_130_UnderworldKing.png",
            "FR_127_DarkTroll.png"
        };

        foreach (string file in preferredFiles)
        {
            string path = MainAssetsRoot + "/" + file;
            if (!File.Exists(ToFullPath(path))) continue;

            SourceAsset boss = LoadRequiredSprite(EnemySpriteRole.Boss, file, result);
            if (boss.Sprite == null) continue;

            if (!file.Equals("UndeadExecutioner.png", StringComparison.OrdinalIgnoreCase))
            {
                boss.UsedFallback = true;
                boss.Note = AppendNote(boss.Note, "Dedicated/UndeadExecutioner.png was not available; selected MainAssets boss-compatible file " + file + ".");
                result.Warnings.Add("Boss source uses " + file + " because Assets/ExternalAssets/MainAssets/UndeadExecutioner.png was not found.");
            }

            return boss;
        }

        string discovered = FindBossLikePng();
        if (!string.IsNullOrEmpty(discovered))
        {
            SourceAsset boss = new SourceAsset
            {
                Role = EnemySpriteRole.Boss,
                RequestedFile = Path.GetFileName(discovered),
                AssetPath = discovered,
                Found = true,
                UsedFallback = true,
                Note = "Discovered boss-like PNG inside MainAssets."
            };
            boss.Sprite = EnsureSlicedAndPickFirst(discovered, Path.GetFileName(discovered), result, boss);
            if (boss.Sprite != null)
            {
                boss.SelectedSpriteName = boss.Sprite.name;
                boss.SelectedRect = FormatRect(boss.Sprite.rect);
                result.Warnings.Add("Boss source discovered by search: " + discovered);
                return boss;
            }
        }

        SourceAsset fallback = CloneFallback(EnemySpriteRole.Boss, "UndeadExecutioner.png", result.Sources[EnemySpriteRole.EliteOrc], "Boss source missing; elite-orc fallback used.");
        result.Warnings.Add("No boss/UndeadExecutioner source was found in MainAssets; boss uses elite-orc fallback.");
        return fallback;
    }

    private static string FindBossLikePng()
    {
        if (!AssetDatabase.IsValidFolder(MainAssetsRoot)) return string.Empty;

        string[] tokens = { "darklord", "dark_lord", "boss", "undead", "executioner", "underworld", "king" };
        return AssetDatabase.FindAssets("t:Texture2D", new[] { MainAssetsRoot })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .Where(p =>
            {
                string label = p.ToLowerInvariant();
                return tokens.Any(t => label.Contains(t));
            })
            .OrderBy(p => ScoreBossPath(p))
            .FirstOrDefault();
    }

    private static int ScoreBossPath(string path)
    {
        string label = path.ToLowerInvariant();
        if (label.Contains("undead") && label.Contains("executioner")) return 0;
        if (label.Contains("idle")) return 1;
        if (label.Contains("underworld") || label.Contains("king")) return 2;
        if (label.Contains("dark") || label.Contains("boss")) return 3;
        return 9;
    }

    private static SourceAsset CloneFallback(EnemySpriteRole targetRole, string requestedFile, SourceAsset fallback, string note)
    {
        if (fallback == null)
        {
            return new SourceAsset
            {
                Role = targetRole,
                RequestedFile = requestedFile,
                Found = false,
                UsedFallback = true,
                Note = note
            };
        }

        return new SourceAsset
        {
            Role = targetRole,
            RequestedFile = requestedFile,
            AssetPath = fallback.AssetPath,
            SelectedSpriteName = fallback.SelectedSpriteName,
            SelectedRect = fallback.SelectedRect,
            Found = fallback.Found,
            HadMetaSlicing = fallback.HadMetaSlicing,
            AutoSliced = fallback.AutoSliced,
            UsedFallback = true,
            Note = note,
            Sprite = fallback.Sprite
        };
    }

    private static Sprite EnsureSlicedAndPickFirst(string assetPath, string fileName, RunResult result, SourceAsset source)
    {
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            source.Note = AppendNote(source.Note, "TextureImporter missing.");
            result.Warnings.Add("TextureImporter missing for " + assetPath);
            return null;
        }

#pragma warning disable CS0618
        SpriteMetaData[] existingSheet = importer.spritesheet;
#pragma warning restore CS0618
        source.HadMetaSlicing = existingSheet != null && existingSheet.Length > 0;

        bool changed = NormalizeImporter(importer);
        if (!source.HadMetaSlicing)
        {
            Texture2D texture = LoadReadableTexture(assetPath);
            SpriteMetaData[] rects = BuildSpriteSheet(texture, fileName).ToArray();
            if (rects.Length > 0)
            {
#pragma warning disable CS0618
                importer.spritesheet = rects;
#pragma warning restore CS0618
                source.AutoSliced = true;
                changed = true;
                source.Note = AppendNote(source.Note, "Auto-sliced into " + rects.Length.ToString(CultureInfo.InvariantCulture) + " sub-sprites.");
            }
            else
            {
                source.Note = AppendNote(source.Note, "Could not infer slices; importer settings normalized only.");
            }
        }
        else
        {
            source.Note = AppendNote(source.Note, "Existing meta slicing reused.");
        }

        if (changed)
        {
            importer.SaveAndReimport();
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            result.ImportChanges.Add(assetPath + ": import settings normalized" + (source.AutoSliced ? " and spritesheet generated." : "."));
        }
        else
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        List<Sprite> sprites = AssetDatabase.LoadAllAssetsAtPath(assetPath)
            .OfType<Sprite>()
            .Where(s => s != null)
            .ToList();

        if (sprites.Count == 0) return null;

        List<Sprite> nonWhole = sprites
            .Where(s => !IsWholePngSheet(s, sprites.Count))
            .ToList();

        List<Sprite> pickFrom = nonWhole.Count > 0 ? nonWhole : sprites;
        Sprite selected = pickFrom
            .OrderBy(s => ExtractTrailingIndex(s.name))
            .ThenByDescending(s => s.rect.yMax)
            .ThenBy(s => s.rect.xMin)
            .ThenBy(s => s.name, StringComparer.Ordinal)
            .FirstOrDefault();

        if (selected != null && IsWholePngSheet(selected, sprites.Count))
        {
            source.Note = AppendNote(source.Note, "Selected sprite covers the full PNG because no smaller sub-sprite exists.");
        }

        return selected;
    }

    private static bool NormalizeImporter(TextureImporter importer)
    {
        bool changed = false;

        if (importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            changed = true;
        }

        if (importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            changed = true;
        }

        if (Mathf.Abs(importer.spritePixelsPerUnit - 64f) > 0.001f)
        {
            importer.spritePixelsPerUnit = 64f;
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

        if (importer.mipmapEnabled)
        {
            importer.mipmapEnabled = false;
            changed = true;
        }

        if (!importer.alphaIsTransparency)
        {
            importer.alphaIsTransparency = true;
            changed = true;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        if (settings.spriteMeshType != SpriteMeshType.FullRect)
        {
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            changed = true;
        }

        return changed;
    }

    private static List<SpriteMetaData> BuildSpriteSheet(Texture2D texture, string fileName)
    {
        var rects = new List<SpriteMetaData>();
        if (texture == null) return rects;

        string baseName = Path.GetFileNameWithoutExtension(fileName);
        string lower = fileName.ToLowerInvariant();

        if (lower.StartsWith("orc", StringComparison.Ordinal) &&
            lower.Contains("idle_full") &&
            texture.width % 64 == 0 &&
            texture.height % 64 == 0)
        {
            return BuildGrid(texture.width, texture.height, 64, 64, baseName);
        }

        if (texture.width > texture.height && texture.height > 0 && texture.width % texture.height == 0)
        {
            int count = texture.width / texture.height;
            if (count > 1 && count <= 24)
            {
                return BuildGrid(texture.width, texture.height, texture.height, texture.height, baseName);
            }
        }

        rects = BuildAlphaBandRects(texture, baseName);
        if (rects.Count > 0) return rects;

        Rect trimmed = FindAlphaBounds(texture);
        if (trimmed.width > 0f && trimmed.height > 0f)
        {
            rects.Add(Meta(baseName + "_0", trimmed));
        }

        return rects;
    }

    private static List<SpriteMetaData> BuildGrid(int textureWidth, int textureHeight, int frameWidth, int frameHeight, string baseName)
    {
        var rects = new List<SpriteMetaData>();
        int columns = textureWidth / frameWidth;
        int rows = textureHeight / frameHeight;
        int index = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                Rect rect = new Rect(col * frameWidth, textureHeight - ((row + 1) * frameHeight), frameWidth, frameHeight);
                rects.Add(Meta(baseName + "_" + index.ToString(CultureInfo.InvariantCulture), rect));
                index++;
            }
        }

        return rects;
    }

    private static List<SpriteMetaData> BuildAlphaBandRects(Texture2D texture, string baseName)
    {
        bool[] columns = new bool[texture.width];
        bool[] rows = new bool[texture.height];

        Color32[] pixels = texture.GetPixels32();
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                if (pixels[y * texture.width + x].a <= 16) continue;
                columns[x] = true;
                rows[y] = true;
            }
        }

        List<Vector2Int> columnBands = Bands(columns, 2);
        List<Vector2Int> rowBandsBottomUp = Bands(rows, 2);
        if (columnBands.Count <= 1 && rowBandsBottomUp.Count <= 1) return new List<SpriteMetaData>();
        if (columnBands.Count * rowBandsBottomUp.Count > 128) return new List<SpriteMetaData>();

        var rects = new List<SpriteMetaData>();
        int index = 0;
        foreach (Vector2Int row in rowBandsBottomUp.OrderByDescending(b => b.y))
        {
            foreach (Vector2Int column in columnBands.OrderBy(b => b.x))
            {
                Rect cell = new Rect(column.x, row.x, column.y - column.x + 1, row.y - row.x + 1);
                Rect content = FindAlphaBounds(texture, cell);
                if (content.width <= 0f || content.height <= 0f) continue;
                rects.Add(Meta(baseName + "_" + index.ToString(CultureInfo.InvariantCulture), content));
                index++;
            }
        }

        return rects;
    }

    private static List<Vector2Int> Bands(bool[] occupied, int minLength)
    {
        var bands = new List<Vector2Int>();
        int start = -1;
        for (int i = 0; i < occupied.Length; i++)
        {
            if (occupied[i])
            {
                if (start < 0) start = i;
                continue;
            }

            if (start >= 0)
            {
                if (i - start >= minLength) bands.Add(new Vector2Int(start, i - 1));
                start = -1;
            }
        }

        if (start >= 0 && occupied.Length - start >= minLength)
        {
            bands.Add(new Vector2Int(start, occupied.Length - 1));
        }

        return bands;
    }

    private static SpriteMetaData Meta(string name, Rect rect)
    {
        return new SpriteMetaData
        {
            name = name,
            rect = rect,
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };
    }

    private static Texture2D LoadReadableTexture(string assetPath)
    {
        try
        {
            byte[] bytes = File.ReadAllBytes(ToFullPath(assetPath));
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            return ImageConversion.LoadImage(texture, bytes) ? texture : null;
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static Rect FindAlphaBounds(Texture2D texture)
    {
        return FindAlphaBounds(texture, new Rect(0, 0, texture.width, texture.height));
    }

    private static Rect FindAlphaBounds(Texture2D texture, Rect area)
    {
        int xMin = Mathf.Clamp(Mathf.FloorToInt(area.xMin), 0, texture.width - 1);
        int yMin = Mathf.Clamp(Mathf.FloorToInt(area.yMin), 0, texture.height - 1);
        int xMax = Mathf.Clamp(Mathf.CeilToInt(area.xMax) - 1, 0, texture.width - 1);
        int yMax = Mathf.Clamp(Mathf.CeilToInt(area.yMax) - 1, 0, texture.height - 1);

        Color32[] pixels = texture.GetPixels32();
        int minX = texture.width;
        int minY = texture.height;
        int maxX = -1;
        int maxY = -1;

        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (pixels[y * texture.width + x].a <= 16) continue;
                if (x < minX) minX = x;
                if (x > maxX) maxX = x;
                if (y < minY) minY = y;
                if (y > maxY) maxY = y;
            }
        }

        if (maxX < minX || maxY < minY) return Rect.zero;
        return new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1);
    }

    private static List<EnemyCandidate> FindEnemyCandidates(Scene scene, RunResult result)
    {
        var candidates = new Dictionary<GameObject, EnemyCandidate>();

        foreach (GameObject go in EnumerateSceneObjects(scene))
        {
            if (go == null) continue;
            if (go.GetComponentInParent<Canvas>() != null || go.GetComponent<RectTransform>() != null) continue;

            THMapObject mapObject = go.GetComponent<THMapObject>();
            THEnemy enemy = go.GetComponent<THEnemy>();
            bool hasEnemyMapObject = mapObject != null && (mapObject.type == THMapObject.ObjectType.Enemy || mapObject.isDarkLord || mapObject.isFinalBoss);
            bool nameLooksEnemy = LooksEnemyLike(go.name, mapObject, enemy);

            if (!hasEnemyMapObject && enemy == null && !nameLooksEnemy) continue;
            if (go.GetComponentInParent<THMapObject>() != mapObject && mapObject == null) continue;

            var candidate = new EnemyCandidate
            {
                GameObject = go,
                MapObject = mapObject,
                Enemy = enemy,
                Identity = BuildIdentity(go, mapObject, enemy)
            };

            candidate.Role = DetermineRole(candidate, result, out bool unknownFallback, out string reason);
            candidate.UnknownFallback = unknownFallback;
            candidate.RoleReason = reason;
            candidates[go] = candidate;
        }

        result.Validation.Add("Enemy candidates found: " + candidates.Count.ToString(CultureInfo.InvariantCulture));
        return candidates.Values
            .OrderBy(c => GetPath(c.GameObject), StringComparer.Ordinal)
            .ToList();
    }

    private static EnemySpriteRole DetermineRole(EnemyCandidate candidate, RunResult result, out bool unknownFallback, out string reason)
    {
        unknownFallback = false;
        string text = NormalizeForSearch(candidate.Identity);

        if (candidate.MapObject != null && (candidate.MapObject.isDarkLord || candidate.MapObject.isFinalBoss))
        {
            reason = "THMapObject isDarkLord/isFinalBoss.";
            return EnemySpriteRole.Boss;
        }

        if (candidate.Enemy != null && candidate.Enemy.isFinalBoss)
        {
            reason = "THEnemy isFinalBoss.";
            return EnemySpriteRole.Boss;
        }

        if (ContainsAny(text, "darklord", "dark lord", "finallord", "final boss", "boss", "lord", "\u043b\u043e\u0440\u0434"))
        {
            reason = "Name/id/displayName indicates boss.";
            return EnemySpriteRole.Boss;
        }

        if (ContainsAny(text, "skeletonmage", "skeleton mage", "mage skeleton", "\u043c\u0430\u0433-\u0441\u043a\u0435\u043b\u0435\u0442", "\u043c\u0430\u0433 \u0441\u043a\u0435\u043b\u0435\u0442"))
        {
            reason = "Name/id/displayName indicates Skeleton Mage.";
            return EnemySpriteRole.SkeletonMage;
        }

        if (ContainsAny(text, "skeleton", "undead", "bone", "\u0441\u043a\u0435\u043b\u0435\u0442"))
        {
            reason = "Name/id/displayName indicates skeleton.";
            return EnemySpriteRole.Skeleton;
        }

        if (ContainsAny(text, "bat", "wolf", "darkmonster", "dark monster", "neutral monster", "gargoyle", "troll", "cursedwolf", "\u0432\u043e\u043b\u043a", "\u0433\u0430\u0440\u0433\u0443\u043b", "\u0442\u0440\u043e\u043b\u043b"))
        {
            reason = "Name/id/displayName indicates flying/dark fallback family.";
            return EnemySpriteRole.FlyingDark;
        }

        if (ContainsAny(text, "darkorc", "dark orc", "orc elite", "elite orc", "orc dark", "orc_dark", "elite"))
        {
            reason = "Name/id/displayName indicates elite or dark orc.";
            return EnemySpriteRole.EliteOrc;
        }

        if (ContainsAny(text, "orc guard", "guard", "raider", "orc_guard", "orc raider"))
        {
            reason = "Name/id/displayName indicates guard/raider.";
            return EnemySpriteRole.StrongOrc;
        }

        if (ContainsAny(text, "goblin", "enemy_goblin"))
        {
            reason = "Name/id/displayName indicates goblin/weak orc.";
            return EnemySpriteRole.WeakOrc;
        }

        if (ContainsAny(text, "orc"))
        {
            reason = "Name/id/displayName indicates generic orc.";
            return EnemySpriteRole.StrongOrc;
        }

        unknownFallback = true;
        reason = "Could not determine enemy role; weak-orc fallback selected.";
        result.Warnings.Add("Role fallback to weak orc: " + GetPath(candidate.GameObject) + " identity=[" + candidate.Identity + "]");
        return EnemySpriteRole.WeakOrc;
    }

    private static bool LooksEnemyLike(string name, THMapObject mapObject, THEnemy enemy)
    {
        string text = NormalizeForSearch(BuildIdentityTextOnly(name, mapObject, enemy));
        return ContainsAny(text, "enemy", "goblin", "orc", "skeleton", "wolf", "bat", "darkmonster", "dark lord", "darklord", "boss", "final");
    }

    private static void ReplaceSprite(EnemyCandidate candidate, SourceAsset source, RunResult result)
    {
        SpriteRenderer renderer = FindOrCreateVisualRenderer(candidate.GameObject, out bool createdRenderer);
        if (renderer == null)
        {
            result.Skipped.Add(new SkipRecord
            {
                ObjectPath = GetPath(candidate.GameObject),
                Reason = "SpriteRenderer could not be found or created."
            });
            return;
        }

        renderer.sprite = source.Sprite;
        renderer.enabled = true;
        renderer.sortingOrder = SortingOrder;
        renderer.sortingLayerID = 0;
        NormalizeVisualScale(renderer, source.Sprite, candidate.Role == EnemySpriteRole.Boss ? BossTargetCells : NormalTargetCells);

        float maxWorld = MaxWorldSpriteSize(renderer, source.Sprite);
        bool sizeFallback = false;
        if (candidate.Role == EnemySpriteRole.Boss && maxWorld > BossMaxCells)
        {
            NormalizeVisualScale(renderer, source.Sprite, BossMaxCells);
            maxWorld = MaxWorldSpriteSize(renderer, source.Sprite);
            sizeFallback = true;
        }
        else if (candidate.Role != EnemySpriteRole.Boss && maxWorld > NormalMaxCells)
        {
            NormalizeVisualScale(renderer, source.Sprite, NormalMaxCells);
            maxWorld = MaxWorldSpriteSize(renderer, source.Sprite);
            sizeFallback = true;
        }

        result.Counts[candidate.Role]++;
        result.Replacements.Add(new ReplacementRecord
        {
            ObjectPath = GetPath(candidate.GameObject),
            Identity = candidate.Identity,
            Role = candidate.Role,
            SpritePath = AssetDatabase.GetAssetPath(source.Sprite),
            SpriteName = source.Sprite.name,
            SpriteRect = FormatRect(source.Sprite.rect),
            RendererPath = GetPath(renderer.gameObject),
            VisualScale = FormatVector(renderer.transform.localScale),
            MaxWorldSize = maxWorld,
            CreatedRenderer = createdRenderer,
            UsedFallback = candidate.UnknownFallback || source.UsedFallback,
            Note = AppendNote(candidate.RoleReason, sizeFallback ? "Scale clamped to max allowed map size." : string.Empty)
        });
    }

    private static SpriteRenderer FindOrCreateVisualRenderer(GameObject enemy, out bool createdRenderer)
    {
        createdRenderer = false;
        if (enemy == null) return null;

        Transform visual = enemy.transform.Find("Visual");
        if (visual != null)
        {
            SpriteRenderer childRenderer = visual.GetComponent<SpriteRenderer>();
            if (childRenderer != null) return childRenderer;
        }

        SpriteRenderer existingChild = enemy.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(r => r != null && r.gameObject != enemy);
        if (existingChild != null) return existingChild;

        SpriteRenderer rootRenderer = enemy.GetComponent<SpriteRenderer>();
        if (rootRenderer != null && enemy.GetComponent<Collider2D>() == null && enemy.GetComponent<THMapObject>() == null && enemy.GetComponent<THEnemy>() == null)
        {
            return rootRenderer;
        }

        var visualGo = new GameObject("Visual");
        visualGo.transform.SetParent(enemy.transform, false);
        visualGo.transform.localPosition = Vector3.zero;
        visualGo.transform.localRotation = Quaternion.identity;
        visualGo.transform.localScale = Vector3.one;
        createdRenderer = true;

        return visualGo.AddComponent<SpriteRenderer>();
    }

    private static void NormalizeVisualScale(SpriteRenderer renderer, Sprite sprite, float targetCells)
    {
        if (renderer == null || sprite == null) return;
        float maxSpriteDim = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
        if (maxSpriteDim <= 0.0001f) return;

        float scale = Mathf.Clamp(targetCells / maxSpriteDim, 0.05f, 4f);
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }

    private static void ValidateScene(
        Scene scene,
        List<EnemyCandidate> enemies,
        Dictionary<string, string> beforeGameplay,
        Dictionary<string, Vector3> beforePositions,
        Dictionary<string, int> beforeTileCounts,
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

        foreach (var kv in beforePositions)
        {
            EnemyCandidate candidate = enemies.FirstOrDefault(c => GetPath(c.GameObject) == kv.Key);
            if (candidate == null || candidate.GameObject == null) continue;
            if (candidate.GameObject.transform.position != kv.Value)
            {
                result.CriticalFailures.Add("Enemy position changed: " + kv.Key);
            }
        }

        Dictionary<string, int> afterTileCounts = CaptureTilemapTileCounts(scene);
        foreach (var kv in beforeTileCounts)
        {
            if (!afterTileCounts.TryGetValue(kv.Key, out int after) || after != kv.Value)
            {
                result.CriticalFailures.Add("Tilemap tile count changed: " + kv.Key);
            }
        }

        foreach (EnemyCandidate enemy in enemies)
        {
            SpriteRenderer renderer = enemy.GameObject != null ? enemy.GameObject.GetComponentInChildren<SpriteRenderer>(true) : null;
            if (renderer == null || renderer.sprite == null)
            {
                result.CriticalFailures.Add("Enemy has no SpriteRenderer sprite: " + GetPath(enemy.GameObject));
                continue;
            }

            string spritePath = AssetDatabase.GetAssetPath(renderer.sprite);
            if (!spritePath.StartsWith(MainAssetsRoot + "/", StringComparison.Ordinal))
            {
                result.CriticalFailures.Add("Enemy sprite is outside MainAssets: " + GetPath(enemy.GameObject) + " -> " + spritePath);
            }

            List<Sprite> siblings = AssetDatabase.LoadAllAssetsAtPath(spritePath).OfType<Sprite>().Where(s => s != null).ToList();
            if (IsWholePngSheet(renderer.sprite, siblings.Count))
            {
                result.CriticalFailures.Add("Enemy uses whole PNG sheet: " + GetPath(enemy.GameObject) + " -> " + spritePath + "/" + renderer.sprite.name);
            }

            float maxWorld = MaxWorldSpriteSize(renderer, renderer.sprite);
            float allowed = IsBoss(enemy) ? BossMaxCells + 0.01f : NormalMaxCells + 0.01f;
            if (maxWorld > allowed)
            {
                result.CriticalFailures.Add("Enemy sprite is too large: " + GetPath(enemy.GameObject) + " size=" + maxWorld.ToString("0.###", CultureInfo.InvariantCulture));
            }
        }

        int bossCount = enemies.Count(IsBoss);
        if (bossCount == 0)
        {
            result.CriticalFailures.Add("Dark Lord / boss enemy was not found.");
        }
        else
        {
            result.Validation.Add("Boss candidates found: " + bossCount.ToString(CultureInfo.InvariantCulture));
        }

        bool bossVisuallyDistinct = result.Replacements.Any(r => r.Role == EnemySpriteRole.Boss) &&
                                    result.Replacements.Where(r => r.Role != EnemySpriteRole.Boss).All(r =>
                                        !string.Equals(r.SpritePath, result.Replacements.First(b => b.Role == EnemySpriteRole.Boss).SpritePath, StringComparison.Ordinal) ||
                                        !string.Equals(r.SpriteName, result.Replacements.First(b => b.Role == EnemySpriteRole.Boss).SpriteName, StringComparison.Ordinal));
        if (!bossVisuallyDistinct)
        {
            result.CriticalFailures.Add("Boss sprite is not visually distinct from regular enemies.");
        }

        if (result.CriticalFailures.Count == 0)
        {
            result.Validation.Add("PASS: enemy SpriteRenderers assigned, all sprites come from MainAssets, no whole sheets detected, sizes are bounded.");
            result.Validation.Add("PASS: THMapObject/THEnemy fields, colliders, enemy positions, and tilemap tile counts were unchanged.");
        }
    }

    private static bool IsBoss(EnemyCandidate candidate)
    {
        if (candidate == null) return false;
        if (candidate.Role == EnemySpriteRole.Boss) return true;
        if (candidate.MapObject != null && (candidate.MapObject.isDarkLord || candidate.MapObject.isFinalBoss)) return true;
        if (candidate.Enemy != null && candidate.Enemy.isFinalBoss) return true;
        return NormalizeForSearch(candidate.Identity).Contains("darklord") || NormalizeForSearch(candidate.Identity).Contains("\u043b\u043e\u0440\u0434");
    }

    private static bool IsWholePngSheet(Sprite sprite, int siblingSpriteCount)
    {
        if (sprite == null || sprite.texture == null) return true;

        Rect rect = sprite.rect;
        Texture2D texture = sprite.texture;
        bool fullRect = Mathf.Approximately(rect.width, texture.width) &&
                        Mathf.Approximately(rect.height, texture.height);
        if (!fullRect) return false;

        string path = AssetDatabase.GetAssetPath(texture);
        string file = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        bool knownSheet = file.Contains("idle_full") ||
                          file.Contains("skeleton") ||
                          file.Contains("idle") ||
                          file.Contains("animation") ||
                          file.Contains("warrior_idle");

        return siblingSpriteCount > 1 || knownSheet;
    }

    private static float MaxWorldSpriteSize(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null || sprite == null) return 0f;
        Vector3 scale = renderer.transform.lossyScale;
        return Mathf.Max(sprite.bounds.size.x * Mathf.Abs(scale.x), sprite.bounds.size.y * Mathf.Abs(scale.y));
    }

    private static Dictionary<string, string> CaptureGameplaySnapshot(Scene scene)
    {
        var snapshot = new Dictionary<string, string>();
        foreach (GameObject go in EnumerateSceneObjects(scene))
        {
            THMapObject mapObject = go.GetComponent<THMapObject>();
            THEnemy enemy = go.GetComponent<THEnemy>();
            Collider2D collider = go.GetComponent<Collider2D>();
            if (mapObject == null && enemy == null && collider == null) continue;

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
                parts.Add("mo.army=" + ArmyLine(mapObject.enemyArmy));
            }

            if (enemy != null)
            {
                parts.Add("enemy.enemyType=" + enemy.enemyType);
                parts.Add("enemy.startsCombat=" + enemy.startsCombat);
                parts.Add("enemy.blocksMovement=" + enemy.blocksMovement);
                parts.Add("enemy.isFinalBoss=" + enemy.isFinalBoss);
                parts.Add("enemy.defeated=" + enemy.defeated);
                parts.Add("enemy.displayName=" + enemy.displayName);
                parts.Add("enemy.difficulty=" + enemy.difficulty);
                parts.Add("enemy.rewardGold=" + enemy.rewardGold);
                parts.Add("enemy.rewardExp=" + enemy.rewardExp);
                parts.Add("enemy.army=" + ArmyLine(enemy.enemyArmy));
            }

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
                    parts.Add("circle.radius=" + circle.radius.ToString("0.###", CultureInfo.InvariantCulture));
                }
            }

            snapshot[GetPath(go)] = string.Join("|", parts);
        }

        return snapshot;
    }

    private static Dictionary<string, Vector3> CaptureEnemyPositions(IEnumerable<EnemyCandidate> enemies)
    {
        return enemies
            .Where(e => e.GameObject != null)
            .GroupBy(e => GetPath(e.GameObject))
            .ToDictionary(g => g.Key, g => g.First().GameObject.transform.position);
    }

    private static Dictionary<string, int> CaptureTilemapTileCounts(Scene scene)
    {
        var counts = new Dictionary<string, int>();
        foreach (GameObject go in EnumerateSceneObjects(scene))
        {
            var tilemap = go.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (tilemap == null) continue;
            counts[GetPath(go)] = CountTiles(tilemap);
        }

        return counts;
    }

    private static int CountTiles(UnityEngine.Tilemaps.Tilemap tilemap)
    {
        int count = 0;
        BoundsInt bounds = tilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos)) count++;
        }

        return count;
    }

    private static string ArmyLine(List<THArmyUnit> army)
    {
        if (army == null || army.Count == 0) return string.Empty;
        return string.Join(";", army.Select(a => a.id + ":" + a.name + ":" + a.count.ToString(CultureInfo.InvariantCulture)));
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

    private static string BuildIdentity(GameObject go, THMapObject mapObject, THEnemy enemy)
    {
        var parts = new List<string> { go != null ? go.name : string.Empty };
        if (mapObject != null)
        {
            parts.Add(mapObject.id);
            parts.Add(mapObject.displayName);
            parts.Add(mapObject.type.ToString());
            parts.Add(mapObject.difficulty.ToString());
            parts.Add(mapObject.isDarkLord ? "isDarkLord" : string.Empty);
            parts.Add(mapObject.isFinalBoss ? "isFinalBoss" : string.Empty);
            if (mapObject.enemyArmy != null) parts.AddRange(mapObject.enemyArmy.Select(a => a.id + " " + a.name));
        }

        if (enemy != null)
        {
            parts.Add(enemy.enemyType);
            parts.Add(enemy.displayName);
            parts.Add(enemy.difficulty.ToString());
            parts.Add(enemy.isFinalBoss ? "enemyFinalBoss" : string.Empty);
            if (enemy.enemyArmy != null) parts.AddRange(enemy.enemyArmy.Select(a => a.id + " " + a.name));
        }

        foreach (MonoBehaviour behaviour in go.GetComponents<MonoBehaviour>())
        {
            if (behaviour == null) continue;
            AppendFieldValue(parts, behaviour, "objectId");
            AppendFieldValue(parts, behaviour, "enemyId");
            AppendFieldValue(parts, behaviour, "enemyType");
            AppendFieldValue(parts, behaviour, "displayName");
            AppendFieldValue(parts, behaviour, "id");
        }

        return string.Join(" ", parts.Where(p => !string.IsNullOrWhiteSpace(p)));
    }

    private static string BuildIdentityTextOnly(string name, THMapObject mapObject, THEnemy enemy)
    {
        var parts = new List<string> { name };
        if (mapObject != null)
        {
            parts.Add(mapObject.id);
            parts.Add(mapObject.displayName);
            parts.Add(mapObject.type.ToString());
        }

        if (enemy != null)
        {
            parts.Add(enemy.enemyType);
            parts.Add(enemy.displayName);
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

    private static string NormalizeForSearch(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return text.Replace('_', ' ').Replace('-', ' ').ToLowerInvariant();
    }

    private static bool ContainsAny(string text, params string[] needles)
    {
        return needles.Any(n => text.IndexOf(n, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static int ExtractTrailingIndex(string name)
    {
        if (string.IsNullOrEmpty(name)) return int.MaxValue;
        int end = name.Length - 1;
        while (end >= 0 && char.IsDigit(name[end])) end--;
        if (end == name.Length - 1) return int.MaxValue;
        string digits = name.Substring(end + 1);
        return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : int.MaxValue;
    }

    private static string GetPath(GameObject go)
    {
        if (go == null) return "<null>";
        var names = new Stack<string>();
        Transform current = go.transform;
        while (current != null)
        {
            names.Push(current.name);
            current = current.parent;
        }

        return string.Join("/", names);
    }

    private static string FormatVector(Vector2 value)
    {
        return value.x.ToString("0.###", CultureInfo.InvariantCulture) + " x " + value.y.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatVector(Vector3 value)
    {
        return value.x.ToString("0.###", CultureInfo.InvariantCulture) + " x " +
               value.y.ToString("0.###", CultureInfo.InvariantCulture) + " x " +
               value.z.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string FormatRect(Rect rect)
    {
        return "x=" + rect.x.ToString("0.###", CultureInfo.InvariantCulture) +
               ", y=" + rect.y.ToString("0.###", CultureInfo.InvariantCulture) +
               ", w=" + rect.width.ToString("0.###", CultureInfo.InvariantCulture) +
               ", h=" + rect.height.ToString("0.###", CultureInfo.InvariantCulture);
    }

    private static string AppendNote(string current, string note)
    {
        if (string.IsNullOrWhiteSpace(note)) return current ?? string.Empty;
        if (string.IsNullOrWhiteSpace(current)) return note;
        return current + " " + note;
    }

    private static string ToFullPath(string assetPath)
    {
        string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath.Replace('/', Path.DirectorySeparatorChar)));
    }

    private static void WriteReport(RunResult result)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(ReportPath));
        var sb = new StringBuilder();

        sb.AppendLine("# Enemy Sprites Replacement Report");
        sb.AppendLine();
        sb.AppendLine("Generated by `The Hero/Map/Replace Enemy Sprites`.");
        sb.AppendLine();

        sb.AppendLine("## Source PNGs");
        sb.AppendLine("| Role | Requested PNG | Actual path | Found | Existing slicing | Auto sliced | Selected first sub-sprite | Rect | Notes |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (EnemySpriteRole role in Enum.GetValues(typeof(EnemySpriteRole)))
        {
            result.Sources.TryGetValue(role, out SourceAsset source);
            if (source == null)
            {
                sb.AppendLine("| " + RoleLabel(role) + " | - | - | No | No | No | - | - | Source was not evaluated. |");
                continue;
            }

            sb.AppendLine("| " + RoleLabel(role) +
                          " | `" + source.RequestedFile + "`" +
                          " | `" + (source.AssetPath ?? string.Empty) + "`" +
                          " | " + YesNo(source.Found) +
                          " | " + YesNo(source.HadMetaSlicing) +
                          " | " + YesNo(source.AutoSliced) +
                          " | `" + (source.SelectedSpriteName ?? string.Empty) + "`" +
                          " | " + (source.SelectedRect ?? string.Empty) +
                          " | " + (source.Note ?? string.Empty) + " |");
        }
        sb.AppendLine();

        sb.AppendLine("## Replacement Counts");
        foreach (EnemySpriteRole role in Enum.GetValues(typeof(EnemySpriteRole)))
        {
            result.Counts.TryGetValue(role, out int count);
            sb.AppendLine("- " + RoleLabel(role) + ": " + count.ToString(CultureInfo.InvariantCulture));
        }
        sb.AppendLine();

        sb.AppendLine("## Replaced Objects");
        sb.AppendLine("| Object | Identity | Role | Sprite path | Sprite | Rect | Renderer | Scale | Max world size | Notes |");
        sb.AppendLine("| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |");
        foreach (ReplacementRecord r in result.Replacements.OrderBy(r => r.Role).ThenBy(r => r.ObjectPath, StringComparer.Ordinal))
        {
            sb.AppendLine("| `" + r.ObjectPath + "` | `" + EscapeCell(r.Identity) + "` | " + RoleLabel(r.Role) +
                          " | `" + r.SpritePath + "` | `" + r.SpriteName + "` | " + r.SpriteRect +
                          " | `" + r.RendererPath + "` | " + r.VisualScale +
                          " | " + r.MaxWorldSize.ToString("0.###", CultureInfo.InvariantCulture) +
                          " | " + (r.UsedFallback ? "fallback; " : string.Empty) + EscapeCell(r.Note) + " |");
        }
        if (result.Replacements.Count == 0) sb.AppendLine("| - | - | - | - | - | - | - | - | - | No replacements. |");
        sb.AppendLine();

        sb.AppendLine("## Skipped Objects");
        if (result.Skipped.Count == 0)
        {
            sb.AppendLine("- None.");
        }
        else
        {
            foreach (SkipRecord skip in result.Skipped)
            {
                sb.AppendLine("- `" + skip.ObjectPath + "`: " + skip.Reason);
            }
        }
        sb.AppendLine();

        sb.AppendLine("## Fallbacks And Warnings");
        IEnumerable<string> fallbackLines = result.Replacements
            .Where(r => r.UsedFallback)
            .Select(r => "`" + r.ObjectPath + "` -> " + RoleLabel(r.Role) + ": " + r.Note);
        foreach (string line in fallbackLines) sb.AppendLine("- " + line);
        foreach (string warning in result.Warnings.Distinct()) sb.AppendLine("- " + warning);
        if (!fallbackLines.Any() && result.Warnings.Count == 0) sb.AppendLine("- None.");
        sb.AppendLine();

        sb.AppendLine("## Import Changes");
        if (result.ImportChanges.Count == 0)
        {
            sb.AppendLine("- None.");
        }
        else
        {
            foreach (string change in result.ImportChanges.Distinct()) sb.AppendLine("- " + change);
        }
        sb.AppendLine();

        sb.AppendLine("## Validation");
        foreach (string line in result.Validation) sb.AppendLine("- " + line);
        if (result.CriticalFailures.Count == 0)
        {
            sb.AppendLine("- PASS: Map.unity saved = " + YesNo(result.MapSaved) + ".");
            sb.AppendLine("- PASS: Map was not rebuilt; tile counts were preserved and only enemy Visual SpriteRenderers were edited/created.");
            sb.AppendLine("- PASS: Hero, UI, Resources, Castle, Tilemap, combat data, rewards, positions, and colliders were not intentionally modified.");
        }
        else
        {
            sb.AppendLine("- FAIL: critical validation failures were detected.");
            foreach (string failure in result.CriticalFailures) sb.AppendLine("- " + failure);
            sb.AppendLine("- Map.unity saved = " + YesNo(result.MapSaved) + ".");
        }

        File.WriteAllText(ReportPath, sb.ToString(), Encoding.UTF8);
    }

    private static string RoleLabel(EnemySpriteRole role)
    {
        switch (role)
        {
            case EnemySpriteRole.WeakOrc: return "weak orc";
            case EnemySpriteRole.StrongOrc: return "strong orc";
            case EnemySpriteRole.EliteOrc: return "elite orc";
            case EnemySpriteRole.Skeleton: return "skeleton";
            case EnemySpriteRole.SkeletonMage: return "skeleton mage";
            case EnemySpriteRole.FlyingDark: return "flying/dark";
            case EnemySpriteRole.Boss: return "boss";
            default: return role.ToString();
        }
    }

    private static string YesNo(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string EscapeCell(string value)
    {
        return (value ?? string.Empty).Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    }

    private static void ExitBatch(RunResult result)
    {
        if (Application.isBatchMode)
        {
            EditorApplication.Exit(result.CriticalFailures.Count == 0 ? 0 : 1);
        }
    }
}
