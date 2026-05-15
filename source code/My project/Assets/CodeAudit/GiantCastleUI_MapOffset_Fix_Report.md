# Giant Castle UI & Map Offset Fix Report

1. **Giant Castle UI:** 
   - Found object: `Deprecated_Canvas` (which contained a large "ЗАМОК" button).
   - Action: Disabled and renamed to `Deprecated_`.
2. **Small Castle Button:** 
   - Created a new `CastleButton` in a Screen Space Overlay Canvas (`MapCanvas`).
   - Position: Bottom Left (24, 24).
   - Size: 140x48.
   - Functionality: Linked to `THMapController.GoToBase` (SceneManager.LoadScene("Base")).
3. **Map Shift:** 
   - MapRoot (containing Grid/Tiles and MapObjects) is at (0,0).
   - Hero is at (6,5).
   - Shift not needed as the map is correctly positioned relative to the game logic and camera.
4. **Camera:** 
   - Configured `Main Camera` with `THCameraFollow`.
   - Orthographic Size: 7.5.
   - Follow Target: `Hero`.
   - Bounds: Recalculated based on `Tilemap` and `THTile` objects, excluding UI.
5. **Validation:** 
   - Status: **PASS**.
   - All critical checks for UI size, button location, and camera bounds passed.

## Objects Disabled
- Deprecated_Canvas
- Deprecated_Text

## Manual Verification Steps
1. Open `Assets/Scenes/MainMenu.unity`.
2. Enter Play Mode.
3. Click "New Game".
4. Ensure no giant "ЗАМОК" covers the screen.
5. Verify the small "ЗАМОК" button is in the bottom-left corner.
6. Verify the camera follows the Hero.
7. Click the "ЗАМОК" button and ensure it loads the Base scene.
8. Check that enemies and resources are still present on the map.
