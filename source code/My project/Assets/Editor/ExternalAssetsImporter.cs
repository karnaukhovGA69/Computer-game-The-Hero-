using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class ExternalAssetsImporter
{
    [MenuItem("Tools/Import External Assets/Apply Settings to All PNG Files")]
    public static void ApplySettingsToAllPNG()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/ExternalAssets" });
        int successCount = 0;

        EditorUtility.DisplayProgressBar("Applying Settings", "Processing textures...", 0);

        for (int i = 0; i < guids.Length; i++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

            if (!assetPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                continue;

            EditorUtility.DisplayProgressBar("Applying Settings", $"Processing: {assetPath}", (float)i / guids.Length);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
            if (importer == null)
                continue;

            // Apply sprite settings
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;

            // Compression settings
            TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SetPlatformTextureSettings(platformSettings);

            // Default to Single mode (can be manually changed to Multiple for spritesheets)
            importer.spriteImportMode = SpriteImportMode.Single;

            // Save changes
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();

            successCount++;
        }

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Success", $"Applied settings to {successCount} PNG files in ExternalAssets!", "OK");

        Debug.Log($"[ExternalAssets] Import settings applied to {successCount} texture files.");
    }

    [MenuItem("Tools/Import External Assets/Mark Spritesheets as Multiple Mode")]
    public static void MarkSpritesheets()
    {
        string[] spritesheets = new[]
        {
            "Buttons.png",
            "Icons.png",
            "Equipment.png",
            "Inventory.png",
            "Bridges.png",
            "Action_panel.png",
            "Circle_menu.png",
            "Craft.png",
            "Decorative_cracks.png",
            "character_panel.png"
        };

        int markedCount = 0;

        foreach (string spritesheet in spritesheets)
        {
            string[] guids = AssetDatabase.FindAssets(Path.GetFileNameWithoutExtension(spritesheet),
                new[] { "Assets/ExternalAssets" });

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);

                if (!assetPath.EndsWith(spritesheet, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
                if (importer == null)
                    continue;

                importer.spriteImportMode = SpriteImportMode.Multiple;
                EditorUtility.SetDirty(importer);
                importer.SaveAndReimport();

                markedCount++;
                Debug.Log($"[ExternalAssets] Marked as Multiple: {assetPath}");
            }
        }

        EditorUtility.DisplayDialog("Success", $"Marked {markedCount} spritesheets as Multiple Mode!", "OK");
    }

    [MenuItem("Tools/Import External Assets/Open ExternalAssets Folder")]
    public static void OpenExternalAssetsFolder()
    {
        string folderPath = Application.dataPath + "/ExternalAssets";
        EditorUtility.RevealInFinder(folderPath);
    }

    [MenuItem("Tools/Import External Assets/View Import Report")]
    public static void ViewImportReport()
    {
        string reportPath = Application.dataPath + "/CodeAudit/ExternalAssets_Import_Report.md";
        if (System.IO.File.Exists(reportPath))
        {
            System.Diagnostics.Process.Start(reportPath);
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Report file not found at: " + reportPath, "OK");
        }
    }
}
