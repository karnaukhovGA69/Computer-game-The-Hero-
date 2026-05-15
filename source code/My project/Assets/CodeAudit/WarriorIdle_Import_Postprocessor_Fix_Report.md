# Warrior_Idle Import & Hero Fix Report

Date: 2026-05-15 19:57

## Postprocessor that reset Sprite Mode
- `Assets/Editor/ExternalAssetsImportPostprocessor.cs` runs `OnPreprocessTexture` for every PNG under `Assets/ExternalAssets/`.
- It set `spriteImportMode = Single` first and then bumped to `Multiple` only for whitelisted filenames.
- `Warrior_Idle.png` was not in the whitelist, so each reimport reverted Sprite Mode to Single and dropped the slicing.

## Fix
- Added `Warrior_Idle.png` (and the rest of the FR_/Skeleton/TX/Tilemap_color sheets) to `IsSpritesheetFile`.
- New `TheHeroFixWarriorIdleImportAndHero` editor menu forces Multiple mode + 8 × 192×192 slicing via `TextureImporter.spritesheet` and `SaveAndReimport`.

- Texture found: **True** (`Assets/ExternalAssets/MainAssets/Warrior_Idle.png`)
- Sub-sprites after reimport: **8**
- Sprite assigned to Hero: `Warrior_Idle_0`
- Whole sheet: **false** (rect 192×192 < texture 1536×192)

## Manual verification
1. **The Hero → Assets → Fix Warrior Idle Import And Hero**
2. Inspect `Warrior_Idle.png` — Sprite Mode stays Multiple after Apply.
3. **The Hero → Validation → Validate Map MainAssets With Fallbacks** — `PASS Hero sub-sprite`.
