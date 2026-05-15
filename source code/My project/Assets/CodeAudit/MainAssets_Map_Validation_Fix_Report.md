# MainAssets Map Validation Fix Report

Date: 2026-05-15 18:07

## 1. GroundTilemap
GroundTilemap creation failed — check MainAssets grass tiles.

## 2. Castle position
- (24, 16) (center target 24,16)

## 3. Castle asset
- Assets/Resources/Sprites/CleanMap/Objects/clean_castle.png :: clean_castle
- Removed old Ground_Tilemap / orphan tiles / stale map roots.

## 4. Tile layers
- Road tiles: 0
- Bridge tiles: 8
- Forest/detail tiles: 0
- Dark tiles: 0

## 5–7. Key enemies
- Skeleton Mage: assigned via catalog
- Wolf: assigned via catalog
- DarkLord: assigned via catalog

## 8. Whole-sheet replacements
- Replaced on enemies/resources: 0

## 9. Re-validation
Run **The Hero → Validation → Validate MainAssets Map** and check Console for remaining FAIL.



## Validation (2026-05-15 18:47)
- PASS: 230
- FAIL: 17
