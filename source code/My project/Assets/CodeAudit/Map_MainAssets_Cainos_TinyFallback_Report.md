# Map MainAssets + Cainos + TinySwords Fallback Report

Date: 2026-05-15 20:10

## Asset provenance (key picks)
- **Forest**: MainAssets :: free_pixel_16_woods (free_pixel_16_woods.png)
- **Bridge**: MainAssets :: Bridges_108 (Bridges.png)
- **Castle**: Cainos :: TX Shadow Tileset Wall 3 (TX Shadow.png)
- **Hero**: MainAssets :: idle (idle.png)

## MainAssets sub-sprite pools
- Grass: 0, Floor: 0, Walls: 0, Interior: 0, Props: 0, Plants: 0, Trees: 1, Houses: 1
- Water: no, Bridge: yes

## Cainos fallback pool
- Terrain: 0, Props: 9, Buildings: 195

## Tiny Swords fallback pool
- Total sub-sprites scanned: 7427

## Castle
- Castle_Player at (24,16), composite of 4 sub-sprite parts (Visual_House + walls + decor).
## Hero
- Hero at (24,13); CameraFollow.Target = Hero.
## Mountains / dark zone
- Not required. Northern boss area uses ruins floor + props.

## Missing (gracefully skipped)
- ground grass sub-sprites
- ruins floor sub-sprites
- road sub-sprite

## Whole-sheet check
All sprites loaded via `LoadSlicedSprites` / `IsWholeSheetSprite` filter. No whole PNG sheet is assigned to any object.

## Manual verification
1. **The Hero → Map → Build Map MainAssets With Cainos Tiny Fallbacks**
2. **The Hero → Validation → Validate Map MainAssets With Fallbacks**
3. Play → MainMenu → New Game.
