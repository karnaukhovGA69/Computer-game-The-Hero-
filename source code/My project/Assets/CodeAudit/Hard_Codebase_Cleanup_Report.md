# Hard Codebase Cleanup Report

Date: 2026-05-15  
Branch: `cleanup/hard-codebase-cleanup`  
Backup: `_CodeCleanupBackup/20260515_140335/`

## 1. Removed / Moved Folders

- Removed from `Assets/Scripts`: `Domain`, `Infrastructure`, `Subsystems`, `UI`.
- Removed root legacy runtime files: `Assets/Scripts/GameManager.cs`, `Assets/Scripts/TheHeroGameRuntime.cs`.
- Backup copies were written before removal.

## 2. Removed TheHeroGenerated Duplicates

- Movement: `THReliableHeroMovement.cs`, `THGuaranteedHeroMovement.cs`, `THHeroMover.cs`.
- Main menu: `THMainMenuController.cs`, `THMainMenuControllerFixed.cs`.
- Old controllers: `THBaseController.cs`, `THCombatController.cs`.
- Old scene/camera helpers: `THSceneNavigator.cs`, `THCameraClamp.cs`, `THCameraMapFix.cs`, `THCenterMapAndCamera.cs`.
- Inactive old UI/debug helpers removed: `THBaseUIPolish.cs`, `THCombatPolish.cs`, `THCombatBattleVisuals.cs`, `THLoadingPanel.cs`, `THBaseExitFix.cs`, `THBaseBackButtonFix.cs`, `THMapUILayoutFix.cs`, `THDebugMenu.cs`.

## 3. Editor Tools Removed / Disabled

Active editor tools left in `Assets/Editor`:

- `ExternalAssetsImporter.cs`
- `ExternalAssetsImportPostprocessor.cs`
- `TheHeroAssignImportedSpritesToMap.cs`
- `TheHeroFinalGameValidation.cs`
- `TheHeroFixCameraAndHeroFollow.cs`
- `TheHeroFixSceneBuildProfile.cs`
- `TheHeroMakeGamePlayable.cs`
- `TheHeroRestoreMapUI.cs`
- `TheHeroValidateProjectStability.cs`

Removed old builders/fixers, including `TheHeroBuildMapAutoTrigger.cs`, old map builders, old rebuild/fix scripts, old release builders, old validators, and `Assets/Editor/_DisabledOldMapEditors`.

## 4. Auto-Run Disabled

- Removed `THStartupSceneFixer.cs`.
- Removed `TheHeroBuildMapAutoTrigger.cs`.
- Verified no active `InitializeOnLoad`, `InitializeOnLoadMethod`, `EditorApplication.delayCall`, or editor update hooks remain in `Assets/Editor`.

## 5. Backup Scenes Moved Out Of Assets

Moved out of `Assets/Scenes` / `Assets` to `_CodeCleanupBackup/Scenes/`:

- `Map_backup_before_cainos.unity`
- `Map_backup_before_tilegap_fix.unity`
- `Map_backup_before_today_rebuild.unity`
- `SampleScene.unity`
- `ggdsg.unity`

Remaining active scenes:

- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/Map.unity`
- `Assets/Scenes/Combat.unity`
- `Assets/Scenes/Base.unity`

## 6. Active Systems Kept

- Runtime architecture: `Assets/Scripts/TheHeroGenerated/`.
- Core: `THSceneLoader`, `THSaveSystem`, `THSavePolicy`, `THDataModels`, `THManager`, `THBootstrap`, `THSystemInitializer`.
- Runtime: `THMapController`, `THMapGridInput`, `THStrictGridHeroMovement`, `THCameraFollow`, `THCombatRuntime`, `THBaseRuntime`, `THCleanMainMenuController`.
- UI: `THSingleMapHoverLabel`, `THInfoDialogPanel`, `THPauseMenu`, `THSettingsController`, `THMapUIRuntime`.
- Support files added/kept in the active namespace: `CombatResolver`, `CombatState`, `TurnQueue`, `DamageCalculator`, `RecruitmentService`, `BuildingConfig`, `BuildingState`, `BaseState`.

## 7. Rewired

- `Map.unity`: removed `THCameraMapFix` and `THCameraClamp` from Main Camera; kept one `THCameraFollow` targeting Hero.
- `Base.unity`: removed inactive old `THBaseUIPolish` component.
- Scene transitions in active runtime now go through `THSceneLoader`.
- `ProjectSettings.templateDefaultScene` changed from removed `SampleScene.unity` to `MainMenu.unity`.
- `TheHeroFixCameraAndHeroFollow` and `TheHeroRestoreMapUI` no longer depend on deleted validation/deprecated classes.

## 8. Compile Issues And Fixes

- Initial local `dotnet build` failed because Unity-generated `.csproj` files still referenced removed scripts.
- Updated generated `.csproj` compile lists for local verification; Unity will regenerate them on import.
- Fixed editor warnings from deprecated Unity APIs in active kept tools.
- Final local builds:
  - `Assembly-CSharp.csproj`: PASS, 0 errors, 0 warnings.
  - `Assembly-CSharp-Editor.csproj`: PASS, 0 errors, 0 warnings.

## 9. Stability Validation

- Added menu item: `The Hero/Validation/Validate Project Stability`.
- Unity batch execution was blocked because the project is already open in another Unity Editor instance.
- External stability validation over scenes, GUID references, Build Settings, Build Profile, code folders, and editor auto-run patterns: PASS.

## 10. Manual Checks

Run in Unity after import:

- Play starts at MainMenu.
- New Game opens Map.
- Map UI is visible.
- Hero is visible and moves on the grid.
- Camera follows Hero.
- Castle opens Base.
- Base returns to Map.
- Enemy starts Combat.
- Combat finish returns to Map.
- Resources can be collected.
- Save and Load buttons do not throw console errors.
