# Camera Hero Follow Fix Report

1. Hero selected: Hero at grid 6,5, world 6,5.
2. Duplicate heroes: none.
3. Hero Transform movement: THStrictGridHeroMovement updates Hero.transform in SetPositionImmediate and MoveAlongPath.
4. Movement script: THStrictGridHeroMovement. THMapController.HeroMover and THCameraFollow target point to the active Hero.
5. Main Camera: Main Camera: Orthographic, size 7.5, z -10, THCameraFollow target = Hero.
6. Camera bounds: min=(-1,-1) max=(73,48)
7. Castle UI cleanup: no giant Castle UI objects were active; CastleButton was normalized to 150x50.
8. Validation: PASS (0 issue(s)).
9. Manual checks: Play, MainMenu -> New Game, verify Hero is visible, camera follows after clicks, and only the small Castle button remains in the lower-left corner.
