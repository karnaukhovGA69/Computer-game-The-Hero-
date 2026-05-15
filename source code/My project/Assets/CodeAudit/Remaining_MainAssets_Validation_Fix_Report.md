# Remaining MainAssets Validation Fix Report

Date: 2026-05-15 18:47

## Center used: (36,24)

## Fixes applied
- Castle_Player at (36,24); 0 duplicate castle(s) removed.
- Road tile pool empty.
- BridgeTilemap: 4 tiles using sub-sprite 'Bridges_108'.
- Forest sprite pool empty.
- ObjectsRoot missing; skipped Skeleton Mage.
- Wolf sub-sprite not found.
- Replaced whole-sheet resource sprites: 0.

## Manual verification
1. Recompile, then **The Hero → Map → Fix Remaining MainAssets Validation Fails**.
2. **The Hero → Validation → Validate MainAssets Map** — expect FAIL=0 for the 19 listed items.
3. Play → MainMenu → New Game.
4. Replaced whole-sheet resource sprites: 0.
