using UnityEditor;
using UnityEngine;
using System.IO;

public class ExternalAssetsImportPostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        // Only apply to ExternalAssets folder
        if (!assetPath.Contains("Assets/ExternalAssets"))
            return;

        TextureImporter importer = (TextureImporter)assetImporter;

        // Apply common settings for all sprites
        importer.textureType = TextureImporterType.Sprite;
        importer.spritePixelsPerUnit = 64;
        importer.filterMode = FilterMode.Point;

        // Compression settings
        TextureImporterPlatformSettings platformSettings = importer.GetDefaultPlatformTextureSettings();
        platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(platformSettings);

        // Set sprite mode based on filename patterns
        // Most files are Single mode by default
        importer.spriteImportMode = SpriteImportMode.Single;

        // Special cases: Spritesheets (Multiple mode)
        if (IsSpritesheetFile(assetPath))
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
        }
    }

    private bool IsSpritesheetFile(string path)
    {
        // List of known spritesheets
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

        foreach (string spritesheet in spritesheets)
        {
            if (path.EndsWith(spritesheet, System.StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
