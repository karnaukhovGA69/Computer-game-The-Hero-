# THE HERO - Visual Art Direction

**Style:** Dark Fantasy 90s Strategy (Retro Pixel Art)
**Inspiration:** 90s overworld strategies (early King's Bounty, HOMM1/2).
**Goal:** Original atmosphere, not a direct copy. Gritty but readable.

## 1. Color Palette
- **Primary Background (Map):** Deep Greens (#1B3022, #0C1A11)
- **Water:** Midnight Blue (#0A1A2E)
- **UI Base (Parchment):** Light Tan / Beige (#D7CCC8)
- **UI Borders (Gold):** Rich Gold (#FFD700) with bronze shadows (#8D6E63)
- **UI Interactivity (Stone):** Dark Gray (#424242)
- **Enemy Accents:** Crimson Red (#B71C1C)
- **Hero Highlights:** Azure Blue (#1976D2) and Gold (#FFD700)

## 2. Tile Style
- **Resolution:** 32x32 pixels.
- **Perspective:** Top-down / 3/4 view (slight tilt).
- **Details:** Thick, dark outlines (not necessarily black, can be dark version of the base color).
- **Transition:** Hard edges between tiles to emphasize the grid-based nature of the strategy.
- **Variation:** 2-3 variations for Grass and Forest to avoid tiling patterns.

## 3. Hero Style
- **Character:** Clearly defined silhouette.
- **Visibility:** Use bright armor (blue/gold) to stand out against the dark map.
- **Animation:** Simple 2-4 frame idle/move cycles (breathing, bobbing).
- **Scale:** 0.8x to 1.1x of a tile size.

## 4. Enemy Style
- **Visual Cues:** Menacing shapes, glowing red eyes or red banners/shields.
- **Contrast:** Darker than the hero, usage of purples or obsidian blacks in shadows.

## 5. UI Style
- **Panels:** Parchment texture (noise + light beige gradient).
- **Frames:** Thick gilded (gold) frames with ornamental corners.
- **Fonts:** Serif, classic, high-contrast (Black on Parchment, Gold on Stone).

## 6. Buttons & Elements
- **Normal State:** Chiseled stone appearance.
- **Hover State:** Glow effect (Golden outline).
- **Pressed State:** Darkened/Sunken stone.

## 7. Resource Icons
- **Gold:** Bright coins, slightly oversized to be recognizable.
- **Wood/Stone:** Earthy, clear silhouettes.
- **Mana:** Glowing crystal (Cyan/Purple).

## 8. Asset Sizes & Technical Rules
- **Base Tile:** 32x32.
- **Unit Sprite:** 32x32 or 48x48 (if larger).
- **Filter Mode:** MUST be **Point (no filter)** for all textures.
- **Compression:** None or High Quality (avoid artifacts).
- **Pixel Perfect:** Always use Pixel Perfect Camera setup.

## 9. Sorting Order
- **Floor Tiles:** 0 - 5
- **Roads:** 6 - 9
- **World Objects (Trees, Mines):** 10 - 15
- **Units/Hero:** 50
- **UI Panels:** 100
- **UI Text/Icons:** 110

## 10. Prohibited (Do NOT do)
- No Anti-Aliasing (AA blurs the retro feel).
- No modern "Clean" vector UI.
- No photorealistic textures.
- No direct copying of licensed characters or logos.
- No neon colors (except for magic/mana crystals).
