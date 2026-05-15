# Tile Gap Map Fix — REPORT
Generated: 2026-05-14 22:17:21

## 1. Why gaps appeared
- Sprite import used `spriteMeshType = Tight` (Unity default). Tight mesh excludes transparent border pixels of the 1024x1024 PNG, so the rendered quad was visually smaller than 1 unit and a transparent rim showed between tiles.
- `spriteExtrude` of 1 px shrank the visible quad further.
- Camera `backgroundColor` was neutral grey, which read as a checkerboard when overlapping with object transparency.

## 2. Was spacing > 1?
- No. Tiles are placed at integer grid + 0.5 with step 1.0. Spacing errors found by validator: 0.

## 3. Transparent edges on terrain sprites?
- Yes — several edge/corner assets had transparent rims. They were moved to the **Overlay** layer.

## 4. Assets used as Base tiles
- Grass (Meadow base, Road base, Forest base, Bridge over water base):
  - `Assets/Sprites/GeneratedToday/Grass/Grass_00.png` — *grass lush*
  - `Assets/Sprites/GeneratedToday/Grass/Grass_01.png` — *grass lush*
  - `Assets/Sprites/GeneratedToday/Grass/Grass_02.png` — *grass lush*
  - `Assets/Sprites/GeneratedToday/Grass/Grass_03.png` — *grass lush*
  - `Assets/Sprites/GeneratedToday/Grass/Grass_04.png` — *grass lush*
  - `Assets/Sprites/GeneratedToday/Grass/Grass_05.png` — *grass lush*
- Grass dry (fallback):
  - `Assets/Sprites/GeneratedToday/GrassDry/GrassDry_00.png` — *grass dry*
  - `Assets/Sprites/GeneratedToday/GrassDry/GrassDry_01.png` — *grass dry*
- River (water base + bridge underlay):
  - `Assets/Sprites/GeneratedToday/River/River_00.png` — *river center*
  - `Assets/Sprites/GeneratedToday/River/River_01.png` — *river center*
  - `Assets/Sprites/GeneratedToday/River/River_02.png` — *river center*
- Mountain (mountain base):
  - `Assets/Sprites/GeneratedToday/Mountain/Mountain_00.png` — *mountain center*
  - `Assets/Sprites/GeneratedToday/Mountain/Mountain_01.png` — *mountain center*
- Darkland (dark base):
  - `Assets/Sprites/GeneratedToday/Dark/Dark_00.png` — *dark center*
  - `Assets/Sprites/GeneratedToday/Dark/Dark_01.png` — *dark center*
  - `Assets/Sprites/GeneratedToday/Dark/Dark_02.png` — *dark center*
  - `Assets/Sprites/GeneratedToday/Dark/Dark_03.png` — *dark center*
  - `Assets/Sprites/GeneratedToday/Dark/Dark_04.png` — *dark center*
  - `Assets/Sprites/GeneratedToday/Dark/Dark_05.png` — *dark center*
  - `Assets/Sprites/GeneratedToday/Dark/Dark_06.png` — *dark center*

## 5. Assets used as Overlay
- Forest canopy (over grass base):
  - `Assets/Sprites/GeneratedToday/Forest/Forest_00.png` — *forest center*
  - `Assets/Sprites/GeneratedToday/Forest/Forest_01.png` — *forest center*
  - `Assets/Sprites/GeneratedToday/Forest/Forest_02.png` — *forest center*
  - `Assets/Sprites/GeneratedToday/Forest/Forest_03.png` — *forest center*
  - `Assets/Sprites/GeneratedToday/Forest/Forest_04.png` — *forest center*
  - `Assets/Sprites/GeneratedToday/Forest/Forest_05.png` — *forest center*
  - `Assets/Sprites/GeneratedToday/Forest/Forest_06.png` — *forest center*
- Road segments (over grass base):
  - `Assets/Sprites/GeneratedToday/Road/Road_00.png` — *road segment*
  - `Assets/Sprites/GeneratedToday/Road/Road_01.png` — *road segment*
- Bridge (over water base): (none)
- River bank edge:
  - `Assets/Sprites/GeneratedToday/RiverEdge/RiverEdge_00.png` — *river bank edge*
  - `Assets/Sprites/GeneratedToday/RiverEdge/RiverEdge_01.png` — *river bank edge*
  - `Assets/Sprites/GeneratedToday/RiverEdge/RiverEdge_02.png` — *river bank edge*
  - `Assets/Sprites/GeneratedToday/RiverEdge/RiverEdge_03.png` — *river bank edge*
  - `Assets/Sprites/GeneratedToday/RiverEdge/RiverEdge_04.png` — *river bank edge*
- Mountain edge:
  - `Assets/Sprites/GeneratedToday/MountainEdge/MountainEdge_00.png` — *mountain edge*
  - `Assets/Sprites/GeneratedToday/MountainEdge/MountainEdge_01.png` — *mountain edge*
  - `Assets/Sprites/GeneratedToday/MountainEdge/MountainEdge_02.png` — *mountain edge*
  - `Assets/Sprites/GeneratedToday/MountainEdge/MountainEdge_03.png` — *mountain edge*
- Darkland edge:
  - `Assets/Sprites/GeneratedToday/DarkEdge/DarkEdge_00.png` — *dark edge*
  - `Assets/Sprites/GeneratedToday/DarkEdge/DarkEdge_01.png` — *dark edge*
  - `Assets/Sprites/GeneratedToday/DarkEdge/DarkEdge_02.png` — *dark edge*
  - `Assets/Sprites/GeneratedToday/DarkEdge/DarkEdge_03.png` — *dark edge*

## 6. Validation
- Base tiles placed: 864 (expected 864)
- Overlay tiles placed: 269
- Sprite renderers re-scaled to 1x1 unit: 0
- BFS Hero → DarkLord: passed

## 7. Manual checks
1. Open Map.unity — no checkerboard between tiles in Scene/Game view.
2. Enter Play mode, walk Castle → Bridge → Pass → DarkLord.
3. Confirm river blocks movement, bridge allows it.
4. Confirm DarkLord starts combat (not loot).

## 8. Log
```
[TheHeroMapFix] === Fix Tile Gaps And Rebuild Proper Map ===
[TheHeroMapFix] Scene backup -> Assets/Scenes/Map_backup_before_tilegap_fix.unity
[TheHeroMapFix] Today assets: 104
[TheHeroMapFix] Catalog OK
[TheHeroMapFix] Sprites imported with FullRect mesh, PPU=1024, no extrude
[TheHeroMapFix] Old (preview-style) map content cleared
[TheHeroMapFix] Tile spacing fixed
[TheHeroMapFix] Base layer filled (864 tiles)
[TheHeroMapFix] Overlay layer created (269 tiles)
[TheHeroMapFix] Transparent terrain handled (scaled 0 sprites to 1x1)
[TheHeroMapFix] Preview grid removed
[TheHeroMapFix] Objects placed
[TheHeroMapFix] Map validation passed
[TheHeroMapFix] Map saved

```
