# Map Visual And Startup Bugs Fix Report

## Backup
- Backup scene: `Assets/Scenes/Map_before_visual_startup_fix.unity (existing backup preserved)`
- Opened scene: `Assets/Scenes/Map.unity`

## Bad Cross
- Tilemap/object: `MapRoot/Grid/BlockingTilemap`
- Tiles before: 156
- Bounds before: `origin=(0,0,0), size=(48,32,1)`
- Action: BlockingTilemap tiles were preserved for movement logic; only its visual renderer was disabled to remove the bad cross.
- Tiles after: 156
- Bounds after: `origin=(0,0,0), size=(48,32,1)`
- GroundTilemap: not modified.

## Castle
- Castle object: `MapRoot/ObjectsRoot/Castle_Player`
- Visual object: `MapRoot/ObjectsRoot/Castle_Player/Visual_House`
- Texture path: `Assets/ExternalAssets/MainAssets/Castle.png`
- Sprite assigned: `Assets/ExternalAssets/MainAssets/Castle.png` / `Castle`
- Sorting order: 80
- Local scale: (0.6, 0.6, 1)
- Action: Existing Castle_Player gameplay object kept; SpriteRenderer visual restored with Castle.png.

## Startup
- Play Mode start scene: `Assets/Scenes/MainMenu.unity`
- Editor enforcer: `TheHeroMainMenuPlayModeStartEnforcer` now reapplies MainMenu after script reload and immediately before entering Play Mode.
- Build scene order:
  - `0: Assets/Scenes/MainMenu.unity`
  - `1: Assets/Scenes/Map.unity`
  - `2: Assets/Scenes/Combat.unity`
  - `3: Assets/Scenes/Base.unity`

## Auto-load Map Script Search
- No `Awake`/`Start` auto-load of `Map` was found.
- Valid `LoadMap()` flows are still present for MainMenu New Game/Continue and returns from Base/Combat.

## Manual Checks
- Press Play and confirm MainMenu appears first, not Map.
- Click New Game and confirm Map opens.
- Confirm the bad cross is gone and RoadTilemap stays empty.
- Confirm Castle_Player is visible in the center and clicking it can enter Base.
- Confirm Hero movement still works and orcs/chests/resources are still present.
- Confirm Console has no new red errors.
