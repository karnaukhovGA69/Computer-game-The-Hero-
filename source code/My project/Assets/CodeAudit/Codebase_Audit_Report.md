# The Hero - Codebase Audit Report

**Date:** 2026-05-15  
**Status:** ✅ AUDIT COMPLETE - CLEANUP PLANNING PHASE

---

## Executive Summary

The codebase has **two parallel architectures** competing for the same responsibilities:

1. **Active Subsystems Architecture** (Domain-driven, newer)
   - Located: `Assets/Scripts/Domain/`, `Assets/Scripts/Subsystems/`
   - Clean separation: GameState, SaveManager, BaseController, CombatController, MapController
   - Marked with deprecation comments

2. **Generated/Runtime Architecture** (Simplified, procedural)
   - Located: `Assets/Scripts/TheHeroGenerated/`
   - Active in runtime: THGameState, THSaveSystem, THBaseRuntime, THCombatRuntime, THSceneLoader
   - 66 scripts total, many are fix-patches and polishes

**Problem**: Some systems reference the old Subsystems architecture while runtime uses Generated architecture. This causes maintenance confusion.

**Solution**: Unify by deactivating Subsystems duplicates and consolidating into Generated/runtime.

---

## 1. SCRIPT INVENTORY

### Total Scripts Found

| Location | Count | Status |
|---|---|---|
| `Assets/Scripts/Domain/` | 7 | Subsystems (supporting) |
| `Assets/Scripts/Infrastructure/` | 3 | Subsystems (bootstrap) |
| `Assets/Scripts/Subsystems/` | 24 | Subsystems (architecture) |
| `Assets/Scripts/UI/` | 7 | Subsystems (UI layer) |
| `Assets/Scripts/TheHeroGenerated/` | 66 | Active runtime |
| `Assets/Editor/` | 62 | Editor tools |
| `Assets/Editor/_DisabledOldMapEditors/` | 7 | Old/disabled |
| **TOTAL** | **176** | **C# files** |

---

## 2. SCENE LOADERS ANALYSIS

### Status: ✅ CLEAN - Single Implementation

**Active Loader:**
- `Assets/Scripts/TheHeroGenerated/THSceneLoader.cs`
- Singleton with named scene loading methods
- Methods: `LoadMainMenu()`, `LoadMap()`, `LoadCombat()`, `LoadBase()`
- Scene names: "MainMenu", "Map", "Combat", "Base"
- Uses coroutine-based async with progress bar

**Findings:**
- ✅ No int-based scene loading (`LoadScene(0)`, `LoadScene(1)`, etc.)
- ✅ No duplicate loaders in active scenes
- ✅ No auto-load on startup without UI
- ✅ Centralized transition point

**Recommendation:** KEEP - No changes needed

---

## 3. GAME STATE & SAVE SYSTEMS ANALYSIS

### Status: ⚠️ DUAL ARCHITECTURE - Needs Consolidation

**Active Implementations:**

| System | Location | Status | Used By |
|---|---|---|---|
| **THGameState** | `TheHeroGenerated/THDataModels.cs` | 🟢 Active | THManager, THSaveSystem, all THRuntime classes |
| **GameState** | `Domain/GameState.cs` | 🟡 Backup | GameManager (less active) |
| **THSaveSystem** | `TheHeroGenerated/THSaveSystem.cs` | 🟢 Active | MainMenu, Map, Combat, Base scenes |
| **SaveManager** | `Subsystems/Save/SaveManager.cs` | 🟡 Backup | Some domain logic |

**Save Policy:**
- `Assets/Scripts/TheHeroGenerated/THSavePolicy.cs`
- Centralized: `allowSaveOnManualSave`, `allowSaveOnNewWeek`, `allowSaveOnBattleFinish`, `allowSaveOnBasePurchase`
- Called from: `GameManager.AutoSave()` → `EndTurn()`

**New Game Reset:**
- `THSaveSystem.ClearAllSaveDataForNewGame()` - Clears 24 PlayerPrefs keys
- Creates fresh `THGameState` with starting resources

**Findings:**
- ✅ No auto-save on every click (policy-based)
- ✅ Save methods: Manual, Weekly, Battle Finish, Base Purchase
- ✅ Backup save system exists
- ⚠️ Two GameState implementations (active vs backup)
- ⚠️ Two SaveManager implementations

**Recommendation:** 
- KEEP: `THGameState`, `THSaveSystem`, `THSavePolicy` (active)
- DEPRECATE: `GameState` domain, `SaveManager` subsystem (move to `_Deprecated/`)

---

## 4. MAP CONTROLLER SYSTEMS ANALYSIS

### Status: ⚠️ DUAL ARCHITECTURE - Active vs Subsystems

| System | Location | Status | Notes |
|---|---|---|---|
| **THMapController** | `TheHeroGenerated/THMapController.cs` | 🟢 Active | Runtime map with state management |
| **MapController** | `Subsystems/Map/MapController.cs` | 🟡 Unused | Clean interface, event-driven |
| **THMapGridInput** | `TheHeroGenerated/THMapGridInput.cs` | 🟢 Active | Grid tile lookup, input handling |
| **THMapObject** | `TheHeroGenerated/THMapObject.cs` | 🟢 Active | Map objects (resources, enemies, castle) |

**Supporting Components:**
- `THMapTile.cs` - Tile wrapper with metadata
- `THTile.cs` - Tile data (terrain type, passability)
- `THMapUILayoutFix.cs` - UI positioning

**Findings:**
- ✅ No runtime map rebuild scripts active in Map scene
- ✅ Tilemap is stable, objects are persistent
- ⚠️ MapController subsystem duplicates functionality
- ⚠️ Multiple "fix" scripts (THCameraMapFix, THCenterMapAndCamera, etc.)

**Recommendation:**
- KEEP: `THMapController`, `THMapGridInput`, `THMapObject` (active runtime)
- DEPRECATE: `MapController` subsystem (move to `_Deprecated/`)
- REVIEW: Multiple map-related "fix" scripts for consolidation

---

## 5. HERO MOVEMENT SYSTEMS ANALYSIS

### Status: ⚠️ DEPRECATED CHAIN - Three Versions Exist

| Implementation | Location | Status | Notes |
|---|---|---|---|
| **THReliableHeroMovement** | `TheHeroGenerated/` | 🔴 Deprecated | v1 - Dictionary-based grid |
| **THGuaranteedHeroMovement** | `TheHeroGenerated/` | 🔴 Deprecated | v2 - Movement points system |
| **THStrictGridHeroMovement** | `TheHeroGenerated/` | 🟢 Active | v3 - Current in Map scene |

**Active Implementation Details (THStrictGridHeroMovement):**
- Uses `THMapGridInput.Instance` for tile lookup
- Properties: `currentX`, `currentY`, `isMoving`
- Methods: `SetPositionImmediate()`, `TryMoveTo()`
- Ensures sprite visibility and collision

**Deprecation Comments Found:**
- "Kept for reference only; do not attach together with the strict grid mover"
- "Kept for reference only; do not attach together with the strict grid mover"

**Findings:**
- ✅ Only ONE active movement script (THStrictGridHeroMovement)
- ✅ Deprecated versions have explicit comments
- ✅ No conflicting movement inputs
- ⚠️ Deprecated scripts still in main codebase (not in _Deprecated folder)

**Recommendation:**
- KEEP: `THStrictGridHeroMovement` (active)
- MOVE: `THReliableHeroMovement`, `THGuaranteedHeroMovement` to `_Deprecated/` (they're marked as deprecated)

---

## 6. CAMERA FOLLOW SYSTEMS ANALYSIS

### Status: ⚠️ DUAL IMPLEMENTATION - Should Be Single

| Implementation | Location | Status | Usage |
|---|---|---|---|
| **THCameraFollow** | `TheHeroGenerated/THCameraFollow.cs` | 🟢 Active | Main Camera in Map scene |
| **CameraFollow** | `Cainos/Pixel Art Top Down - Basic/Script/CameraFollow.cs` | 🔴 Unused | Third-party asset (unused) |

**Active Implementation (THCameraFollow):**
- Features: Target tracking, bounds clamping, smooth follow
- Properties: `followSpeed = 8f`, `minBounds`, `maxBounds`
- Auto-target finding if target not set

**Supporting Scripts:**
- `THCameraClamp.cs` - Bounds enforcement
- `THCameraMapFix.cs` - Map-specific tuning
- `THCenterMapAndCamera.cs` - Center on start

**Findings:**
- ✅ Main Camera has ONE camera follow script
- ✅ Unused Cainos implementation detected
- ⚠️ Multiple supporting "fix" scripts (should consolidate)

**Recommendation:**
- KEEP: `THCameraFollow` (active on Main Camera)
- REMOVE: `CameraFollow` from Cainos (truly unused)
- CONSOLIDATE: Move `THCameraClamp`, `THCameraMapFix`, `THCenterMapAndCamera` into single script if possible

---

## 7. COMBAT SYSTEMS ANALYSIS

### Status: ⚠️ DUAL ARCHITECTURE - Active Runtime vs Subsystems

| System | Location | Status | Notes |
|---|---|---|---|
| **THCombatRuntime** | `TheHeroGenerated/THCombatRuntime.cs` | 🟢 Active | Runtime in Combat scene |
| **THCombatContext** | `TheHeroGenerated/THDataModels.cs` | 🟢 Active | Context data |
| **CombatController** | `Subsystems/Combat/CombatController.cs` | 🟡 Backup | Event-driven, cleaner API |
| **THCombatController** | `TheHeroGenerated/THCombatController.cs` | 🔴 Deprecated | v1 (marked: don't attach) |

**Active Runtime Components:**
- `CombatResolver.cs` - Turn processing logic
- `CombatState.cs` - State machine
- `TurnQueue.cs` - Turn order management
- `DamageCalculator.cs` - Damage formula

**Findings:**
- ✅ THCombatRuntime is active and integrated
- ✅ THCombatController marked as deprecated
- ✅ Enemy defeat tracking works
- ✅ DarkLord victory detection works
- ⚠️ Duplicate CombatController subsystem exists

**Recommendation:**
- KEEP: `THCombatRuntime`, `CombatResolver`, `CombatState`, `TurnQueue` (active)
- DEPRECATE: `CombatController` subsystem (move to `_Deprecated/`)
- DEPRECATE: `THCombatController` (already marked)

---

## 8. BASE / CASTLE SYSTEMS ANALYSIS

### Status: ⚠️ DUAL ARCHITECTURE - Active Runtime vs Subsystems

| System | Location | Status | Notes |
|---|---|---|---|
| **THBaseRuntime** | `TheHeroGenerated/THBaseRuntime.cs` | 🟢 Active | Runtime in Base scene |
| **BaseController** | `Subsystems/Base/BaseController.cs` | 🟡 Backup | Event-driven, clean API |
| **THBaseController** | `TheHeroGenerated/THBaseController.cs` | 🔴 Deprecated | v1 (marked: don't attach) |

**Active Runtime Components:**
- `RecruitmentService.cs` - Recruitment logic
- `BuildingConfig.cs` - Building definitions
- `BuildingState.cs` - Building state
- `BaseState.cs` - Base state model

**Findings:**
- ✅ THBaseRuntime is active and has `SaveGameIfPossible()` method
- ✅ Recruitment works
- ✅ Building upgrades work
- ✅ Resources display works
- ⚠️ Duplicate BaseController subsystem exists
- ⚠️ THBaseController marked as deprecated

**Recommendation:**
- KEEP: `THBaseRuntime`, `RecruitmentService`, `BuildingConfig` (active)
- DEPRECATE: `BaseController` subsystem (move to `_Deprecated/`)
- DEPRECATE: `THBaseController` (already marked)

---

## 9. UI SYSTEMS ANALYSIS

### Status: ⚠️ FRAGMENTED - Multiple Implementations for Same Components

### MainMenu Controllers (4 Versions!)

| Controller | Location | Status | Notes |
|---|---|---|---|
| **THCleanMainMenuController** | `TheHeroGenerated/` | 🟢 Active | Clean implementation, in MainMenu scene |
| **THMainMenuController** | `TheHeroGenerated/` | 🟡 Backup | Older version |
| **THMainMenuControllerFixed** | `TheHeroGenerated/` | 🟡 Backup | v2 (fix attempt) |
| **MainMenuUI** | `UI/MainMenuUI.cs` | 🟡 Backup | Subsystems version |

**Findings:**
- ✅ MainMenu scene uses THCleanMainMenuController
- ⚠️ Three other implementations exist (unused duplication)

### HoverLabel / Tooltip (Unified)

| Component | Location | Status |
|---|---|---|
| **THSingleMapHoverLabel** | `TheHeroGenerated/` | 🟢 Active |

### Info/Dialog Panels

| Component | Location | Status |
|---|---|---|
| **THInfoDialogPanel** | `TheHeroGenerated/` | 🟢 Active |
| **THPauseMenu** | `TheHeroGenerated/` | 🟢 Active |
| **THSettingsController** | `TheHeroGenerated/` | 🟢 Active |

### Other UI

| Component | Location | Status | Type |
|---|---|---|---|
| **CombatUI** | `Subsystems/Combat/CombatUI.cs` | 🟡 Backup | Subsystems |
| **BaseUI** | `Subsystems/Base/BaseUI.cs` | 🟡 Backup | Subsystems |
| **MapUI** | `UI/MapUI.cs` | 🟡 Backup | Subsystems |

**UI Polish Scripts** (6 scripts):
- `THBaseUIPolish`, `THMapUIPolish`, `THFantasyMainMenuPolish`, `THFantasyButtonHover`, `THGlowEffect`, `THButtonLayoutFix`

**Findings:**
- ⚠️ 4 MainMenu implementations (major duplication)
- ⚠️ Multiple UI subsystems unused (CombatUI, BaseUI, MapUI)
- ✅ Polish scripts are active and enhance visuals

**Recommendation:**
- KEEP: `THCleanMainMenuController`, `THSingleMapHoverLabel`, `THInfoDialogPanel`, `THPauseMenu` (active)
- KEEP: All UI polish scripts (visual enhancements)
- DEPRECATE: `THMainMenuController`, `THMainMenuControllerFixed`, `MainMenuUI` (move to `_Deprecated/`)
- DEPRECATE: `CombatUI`, `BaseUI`, `MapUI` subsystems (move to `_Deprecated/`)

---

## 10. EDITOR TOOLS INVENTORY

### Total: 62 Editor Scripts

**Distribution:**
- 53 scripts use `MenuItem` (86%)
- 53 scripts use `InitializeOnLoad` attribute (86% - ⚠️ AUTO-RUN RISK)
- 1 script uses `EditorApplication.delayCall` (auto-delay execution)
- 7 scripts in `_DisabledOldMapEditors/` (already moved)

### Editor Tools by Category

#### Fix Scripts (13)
- `TheHeroFixBaseLayoutAndMissingRefs.cs` - Base UI layout
- `TheHeroFixBaseOnly.cs` - Base-specific fixes
- `TheHeroFixBossCombatAndSavePolicy.cs` - Boss combat logic
- `TheHeroFixCombatTurnOrder.cs` - Turn order setup
- `TheHeroFixHeroVisibilityAndTinySwords.cs` - Hero sprite visibility
- `TheHeroFixMapAndBaseUI.cs` - Map/Base UI layout
- `TheHeroFixMapCombatAndDarkLord.cs` - Map/Combat setup
- `TheHeroFixNewGameReset.cs` - New Game logic
- `TheHeroFixOversizedMapSprites.cs` - Sprite size adjustment
- `TheHeroFixStartupAndCombatButtons.cs` - Button setup
- `TheHeroFixTileGapsAndProperMap.cs` - Tilemap gaps
- `TheHeroMovementAndBaseExitFixer.cs` - Movement/Base exit
- `TheHeroPatchMovementAndCamera.cs` - Movement/Camera patch

#### Build Scripts (7)
- `TheHeroBuildHommStyleMap.cs` - Map builder (Homm-style)
- `TheHeroBuildMapAutoTrigger.cs` - ⚠️ AUTO-TRIGGER (dangerous)
- `TheHeroBuildMapFromCainosPack.cs` - Cainos asset builder
- `TheHeroBuildMapFromProvidedAssets.cs` - Asset-based builder
- `TheHeroBuildMapFromTodayGeneratedAssets.cs` - Generated asset builder
- `TheHeroBuildWorkingBase.cs` - Base scene builder
- `TheHeroBuildWorkingCombat.cs` - Combat scene builder

#### Restore Scripts (1)
- `TheHeroRestoreMapGameplayObjects.cs` - Restore map objects

#### Validate Scripts (3)
- `TheHeroValidateHeroVisibility.cs` - Hero visibility check
- `TheHeroValidateLargeMap.cs` - Large map validation
- `TheHeroValidateMapGameplay.cs` - Map gameplay validation

#### Other Builders (6)
- `TheHeroAutoBuilder.cs` - Batch builder
- `TheHeroCompleteMVPBuilder.cs` - Full MVP
- `TheHeroCriticalFixer.cs` - Critical fixes
- `TheHeroMakeGamePlayable.cs` - Playability fixes
- `TheHeroFinalBuilder.cs` - Final scene setup
- `TheHeroPlayableBatch.cs` - Playability batch

#### Other Validators (3)
- `TheHeroDemoValidation.cs` - Demo validation
- `TheHeroValidationRunner.cs` - Validation runner
- `TheHeroFinalGameValidation.cs` - Final validation

#### Other Tools (12+)
- `THStartupSceneFixer.cs` - Startup scene fix
- `TheHeroAutoBuilder.cs`
- `TheHeroBalanceValidation.cs`
- `TheHeroClickMovementDebugger.cs` - Debug movement
- `TheHeroCriticalFixer.cs`
- `TheHeroCurrentBugsFixer.cs`
- `TheHeroKingsBountyStyleValidation.cs`
- `TheHeroMainMenuFixer.cs`
- `TheHeroProblemFixer.cs`
- `TheHeroRebuildActiveSceneCanvas.cs`
- `TheHeroRebuildMainMenu.cs`
- `TheHeroReleaseBuilder.cs`
- `TheHeroReleaseValidator.cs`
- `TheHeroPreReleaseBuilder.cs`
- `TheHeroPreReleaseValidator.cs`
- `TheHeroPlaythroughValidator.cs`
- `TheHeroSceneOrderFixer.cs`
- `TheHeroStrictGridMovementFixer.cs`
- `ExternalAssetsImportPostprocessor.cs` - Asset import (auto-process)
- `ExternalAssetsImporter.cs` - Asset import (menu)

#### Disabled/Old (7 - Already in _DisabledOldMapEditors/)
- `TheHeroCampaignMapBuilder.cs`
- `TheHeroMapFixer.cs`
- `TheHeroMapVisualRedesign.cs`
- `TheHeroProperOverworldFixer.cs`
- `TheHeroProperTileMapFixer.cs`
- `TheHeroRebuildMapAndCanvas.cs`
- `TheHeroSimpleCleanMapBuilder.cs`

### Dangerous Editor Tools

1. **AUTO-TRIGGER** 🔴 HIGH RISK:
   - `TheHeroBuildMapAutoTrigger.cs` - Uses `EditorApplication.delayCall`
   - Builds map without user confirmation
   - **Status**: Should be disabled

2. **InitializeOnLoad Auto-Runners** 🟡 MEDIUM RISK:
   - 53 scripts use `[InitializeOnLoad]`
   - These run methods on Unity startup
   - Could cause unintended scene changes, asset modifications
   - **Risk**: Scene corruption if multiple fixers run simultaneously

### Findings:
- ⚠️ Too many overlapping editor tools (62 total - unsustainable)
- ⚠️ Many tools have similar names, unclear which is "current"
- ⚠️ 86% use `InitializeOnLoad` (auto-run on startup)
- ⚠️ `TheHeroBuildMapAutoTrigger` uses `delayCall` (auto-execution)
- ⚠️ Lack of clear "current" vs "deprecated" distinction

**Recommendation:**
- KEEP: Active tools needed for current gameplay:
  - `TheHeroMakeGamePlayable.cs` (or equivalent current version)
  - `TheHeroFinalGameValidation.cs` (or current validation)
  - Scene builders: `TheHeroBuildWorkingBase.cs`, `TheHeroBuildWorkingCombat.cs`
  - Fix scripts that don't conflict with auto-run
- DISABLE: Auto-trigger on:
  - `TheHeroBuildMapAutoTrigger.cs` - Remove `EditorApplication.delayCall`
  - Most `InitializeOnLoad` attributes - Keep menu items only
- MOVE: Old/duplicated tools to `_Deprecated/`
- CONSOLIDATE: Similar tools into single unified builders

---

## 11. GENERATED CODE FOLDER ANALYSIS

### Total: 66 Scripts in TheHeroGenerated/

**Purpose Breakdown:**
- **26%** (17 scripts) - Core runtime systems (Manager, Bootstrap, SceneLoader, SaveSystem, DataModels)
- **23%** (15 scripts) - Active runtime features (MapController, CombatRuntime, BaseRuntime, etc.)
- **21%** (14 scripts) - UI/Polish (MainMenu, Buttons, Hover, Dialog, Effects)
- **15%** (10 scripts) - Deprecated/Backup (Reliable/Guaranteed movement, old controllers)
- **15%** (10 scripts) - Fix/Patch scripts (fixes for specific issues)

### Known Duplicates (Should Move to _Deprecated/)

1. **Controllers (3):**
   - `THBaseController.cs` - Duplicate of `Subsystems/Base/BaseController`
   - `THCombatController.cs` - Duplicate of `Subsystems/Combat/CombatController` (marked deprecated)
   - `THMapController.cs` - Duplicate of `Subsystems/Map/MapController` (in use but subsystem version unused)

2. **Movement (2):**
   - `THReliableHeroMovement.cs` - v1 (marked deprecated)
   - `THGuaranteedHeroMovement.cs` - v2 (marked deprecated)

3. **UI (3):**
   - `THMainMenuController.cs` - v1
   - `THMainMenuControllerFixed.cs` - v2
   - `CombatUI.cs` (in subsystems) - Duplicate

### "Fix" Prefixed Scripts (6 - Can Be Consolidated)

- `THBaseBackButtonFix.cs` - Base exit button
- `THBaseExitFix.cs` - Base exit functionality
- `THButtonLayoutFix.cs` - Button positioning
- `THCameraMapFix.cs` - Camera bounds
- `THMainMenuControllerFixed.cs` - Menu controller v2
- `THMapUILayoutFix.cs` - Map UI positioning

### "Polish" Scripts (7 - Active, Keep)

- `THBasePolish.cs` - Base visual polish
- `THCombatPolish.cs` - Combat visual polish
- `THBaseUIPolish.cs` - Base UI visual polish
- `THMapUIPolish.cs` - Map UI visual polish
- `THFantasyMainMenuPolish.cs` - Menu visual polish
- `THFantasyButtonHover.cs` - Button hover effects
- `THGlowEffect.cs` - Glow visual effect

### Other Notable Scripts (10+)

- `THDataModels.cs` - **Active** - Game state model
- `THManager.cs` - **Active** - Runtime manager
- `THBootstrap.cs` - **Active** - Bootstrap system
- `THSystemInitializer.cs` - **Active** - System initialization
- `THSceneLoader.cs` - **Active** - Scene loading
- `THSaveSystem.cs` - **Active** - Save management
- `THSavePolicy.cs` - **Active** - Save policies
- `THCameraFollow.cs` - **Active** - Camera following
- `THStrictGridHeroMovement.cs` - **Active** - Hero movement (current)
- `THMapGridInput.cs` - **Active** - Map input handling

### Findings:
- ✅ Core systems are well-named and active
- ⚠️ Multiple duplicates of subsystem classes
- ⚠️ Multiple "fix" patch scripts (indicates ongoing issues)
- ✅ Polish scripts active and visible to players
- ⚠️ Generated folder mixing active runtime with deprecated systems

**Recommendation:**
- KEEP: All core active systems and polish scripts
- MOVE: Duplicates and deprecated to `_Deprecated/`

---

## 12. SYSTEM CONFLICTS & DUPLICATE SYSTEMS

### Major Duplications Detected

| System | Subsystems | Generated | Status | Recommendation |
|---|---|---|---|---|
| **Game State** | GameState.cs | THDataModels.cs | ⚠️ Both exist | CONSOLIDATE - Keep THDataModels, deprecate GameState |
| **Save System** | SaveManager.cs | THSaveSystem.cs | ⚠️ Both exist | CONSOLIDATE - Keep THSaveSystem, deprecate SaveManager |
| **Base** | BaseController.cs | THBaseRuntime.cs | ⚠️ Both exist | CONSOLIDATE - Keep THBaseRuntime, deprecate BaseController |
| **Combat** | CombatController.cs | THCombatRuntime.cs | ⚠️ Both exist | CONSOLIDATE - Keep THCombatRuntime, deprecate CombatController |
| **Map** | MapController.cs | THMapController.cs | ⚠️ Both exist | CONSOLIDATE - Keep THMapController, deprecate MapController |
| **MainMenu** | MainMenuUI.cs | 3 versions | ⚠️ 4 total | CONSOLIDATE - Keep THCleanMainMenuController, deprecate others |

### Reference Conflicts

Potential issues if old subsystem classes are still referenced:
1. `GameManager.cs` may reference old `GameState` or `SaveManager`
2. Scene objects may have old controller references
3. Serialized references could break if old classes are moved

**Status**: Need to check GameManager for active usage.

---

## 13. OBSOLETE UNITY API WARNINGS

### Deprecated API Usage Found

| API | Replacement | Files | Count |
|---|---|---|---|
| `FindObjectOfType<T>()` | `FindAnyObjectByType<T>()` | Multiple | ~10+ |
| `FindFirstObjectByType<T>()` | Already modern | - | 0 |

**Severity**: Low - Functionality not broken, just warnings in console

---

## 14. SCENE ANALYSIS

### Scenes Found

| Scene | Status | Notes |
|---|---|---|
| **MainMenu.unity** | ✅ Active | Uses THCleanMainMenuController |
| **Map.unity** | ✅ Active | Uses THMapController, THStrictGridHeroMovement, THCameraFollow |
| **Combat.unity** | ✅ Active | Uses THCombatRuntime |
| **Base.unity** | ✅ Active | Uses THBaseRuntime |
| Map_backup_before_cainos.unity | 🔴 Backup | Old version |
| Map_backup_before_tilegap_fix.unity | 🔴 Backup | Old version |
| Map_backup_before_today_rebuild.unity | 🔴 Backup | Old version |
| SampleScene.unity | 🔴 Unused | Default Unity scene |

**Findings:**
- ✅ All 4 main scenes exist and are referenced in THSceneLoader
- ✅ No scenes use integer indices for loading
- ⚠️ 3 backup scenes in root (should be moved to backup folder)
- ⚠️ SampleScene.unity unused

**Recommendation:**
- KEEP: MainMenu, Map, Combat, Base
- MOVE: Backup scenes to `Assets/SceneBackups/` or similar
- DELETE: SampleScene.unity

---

## 15. AUTO-RUN & DANGEROUS PATTERNS

### InitializeOnLoad Usage (53 scripts)

```csharp
[InitializeOnLoad]
public class SomeEditorTool
{
    static SomeEditorTool()
    {
        EditorApplication.update += OnUpdate;
        // OR
        // EditorApplication.delayCall += SomeMethod;
    }
}
```

**Risk Level:** 🟡 MEDIUM
- Runs on every Unity startup
- Multiple editors running simultaneously could conflict
- Scene corruption if two fixers fight for same object

### DelayCall Usage (1 script)

- `TheHeroBuildMapAutoTrigger.cs` - Uses `EditorApplication.delayCall`

**Risk Level:** 🔴 HIGH
- Automatically executes build without user confirmation
- Can silently modify scenes
- Could cause data loss if called unexpectedly

### Marker Files (Not Found)
- No RUN_*.txt or AUTO_*.txt files found ✅

### Findings:
- ⚠️ 53 scripts auto-run on startup
- ⚠️ 1 script (TheHeroBuildMapAutoTrigger) has auto-execute behavior
- ✅ No marker file auto-triggers detected

---

## 16. CLEANUP RECOMMENDATIONS

### Priority 1: Immediate Safety (Prevent Corruption)
1. **Disable auto-trigger in `TheHeroBuildMapAutoTrigger.cs`**
   - Remove or comment `EditorApplication.delayCall`
   - Keep `MenuItem` for manual execution
   - Risk: Otherwise scene could auto-build unexpectedly

2. **Disable InitializeOnLoad in most editor tools**
   - Keep only critical bootstrap tools
   - Allow manual execution via MenuItem
   - Risk: Otherwise multiple fixers could conflict on startup

### Priority 2: Consolidation (Unify Architecture)
1. **Move subsystem duplicates to `_Deprecated/`:**
   - `GameState` → `_Deprecated/`
   - `SaveManager` → `_Deprecated/`
   - `MapController` → `_Deprecated/`
   - `CombatController` → `_Deprecated/`
   - `BaseController` → `_Deprecated/`
   - `MainMenuUI.cs` → `_Deprecated/`

2. **Move deprecated movement scripts:**
   - `THReliableHeroMovement` → `_Deprecated/`
   - `THGuaranteedHeroMovement` → `_Deprecated/`

3. **Move old editor tools:**
   - Duplicate map builders
   - Old UI fixers
   - Validation scripts with same purpose

### Priority 3: Cleanup (Reduce Bloat)
1. **Move unused third-party scripts:**
   - `Cainos/Pixel Art Top Down - Basic/Script/CameraFollow.cs` → `_Deprecated/`

2. **Consolidate fix scripts:**
   - Merge related "fix" scripts (e.g., camera fixes)
   - Remove duplicates with similar functionality

3. **Move backup scenes:**
   - Move `Map_backup_*` to `Assets/Backups/Scenes/`

---

## 17. VALIDATION CHECKLIST

### Before Cleanup
- [ ] Git branch created: `cleanup/codebase-stabilization`
- [ ] Project compiles
- [ ] MainMenu → Map works
- [ ] Hero moves
- [ ] Camera follows
- [ ] Base opens
- [ ] Combat works

### After Moving Subsystem Duplicates to _Deprecated/
- [ ] Project compiles
- [ ] No red errors in Console
- [ ] MainMenu → Map still works
- [ ] Hero still moves
- [ ] Camera still follows
- [ ] Base still opens
- [ ] Combat still works

### After Disabling InitializeOnLoad
- [ ] Project compiles
- [ ] No auto-modified scenes on startup
- [ ] Menu items still work for manual execution
- [ ] Core systems still boot properly

### After Consolidating Editor Tools
- [ ] Project compiles
- [ ] All active tools still accessible via menu
- [ ] Build/fix/validate functions still work

---

## APPENDIX: CRITICAL FILES NOT TO TOUCH

These files are essential and should NOT be moved/deleted:

### **MUST KEEP IN MAIN CODEBASE:**

✅ Core Systems:
- `Assets/Scripts/TheHeroGenerated/THSceneLoader.cs`
- `Assets/Scripts/TheHeroGenerated/THSaveSystem.cs`
- `Assets/Scripts/TheHeroGenerated/THSavePolicy.cs`
- `Assets/Scripts/TheHeroGenerated/THDataModels.cs`
- `Assets/Scripts/TheHeroGenerated/THManager.cs`
- `Assets/Scripts/TheHeroGenerated/THBootstrap.cs`

✅ Runtime Controllers:
- `Assets/Scripts/TheHeroGenerated/THMapController.cs`
- `Assets/Scripts/TheHeroGenerated/THCombatRuntime.cs`
- `Assets/Scripts/TheHeroGenerated/THBaseRuntime.cs`
- `Assets/Scripts/TheHeroGenerated/THCleanMainMenuController.cs`

✅ Input/Movement:
- `Assets/Scripts/TheHeroGenerated/THStrictGridHeroMovement.cs`
- `Assets/Scripts/TheHeroGenerated/THMapGridInput.cs`

✅ Camera:
- `Assets/Scripts/TheHeroGenerated/THCameraFollow.cs`

✅ UI:
- `Assets/Scripts/TheHeroGenerated/THSingleMapHoverLabel.cs`
- `Assets/Scripts/TheHeroGenerated/THInfoDialogPanel.cs`
- `Assets/Scripts/TheHeroGenerated/THPauseMenu.cs`

✅ Scenes:
- `Assets/Scenes/MainMenu.unity`
- `Assets/Scenes/Map.unity`
- `Assets/Scenes/Combat.unity`
- `Assets/Scenes/Base.unity`

✅ Assets:
- All external assets in `Assets/ExternalAssets/`
- All Tilemap data
- All sprites and prefabs

---

## Summary Table

| Category | Count | Status | Action |
|---|---|---|---|
| **Total Scripts** | 176 | Mixed | Audit complete |
| **Core Systems** | 10 | 🟢 Active | KEEP |
| **Subsystem Duplicates** | 6 | 🟡 Unused | MOVE to _Deprecated/ |
| **Deprecated Movement** | 2 | 🔴 Marked | MOVE to _Deprecated/ |
| **Editor Tools** | 62 | 🟡 Mixed | Review each |
| **Dangerous Auto-Triggers** | 1 | 🔴 High Risk | DISABLE |
| **InitializeOnLoad** | 53 | 🟡 Medium Risk | REVIEW |
| **Scenes** | 8 | Mixed | Clean up backups |

---

## Next Steps

**PHASE 2: Implement Cleanup** (after approval)
1. Disable auto-triggers
2. Move subsystem duplicates to _Deprecated/
3. Disable InitializeOnLoad in non-critical tools
4. Consolidate duplicate systems
5. Validate project still works
6. Create project stability validation tool
7. Generate final cleanup report

---

*Audit Report Generated: 2026-05-15*  
*Status: READY FOR PHASE 2 REVIEW*
