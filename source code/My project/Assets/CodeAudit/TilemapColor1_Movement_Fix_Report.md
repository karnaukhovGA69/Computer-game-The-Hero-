# Tilemap Color1 Movement Fix Report

## Tilemap_color1.png
- Sliced grid: 64x64.
- Sprite import mode: Multiple.
- Base grass center: `Tilemap_color1_9`.
- Edge/corner grass tiles: `corners 0/3/24/27, sides 1/2/8/16/11/19/25/26`.
- Blocking cliff tiles: `Tilemap_color1_34-37 and 40-43`.

## Scene
- GroundTilemap tile count: 1536.
- BlockingTilemap tile count: 156.
- Removed whole-atlas SpriteRenderers: 0.

## Hero
- Sprite: `Warrior_Idle_0` from `Assets/ExternalAssets/MainAssets/Warrior_Idle.png`.

## Movement
- THMapGridInput.GroundTilemap -> GroundTilemap; THMapController.HeroMover -> Hero/THStrictGridHeroMovement.
