# The Hero Code Audit Report

Дата: 2026-05-11

## 1. Найденные системы

Активные runtime-скрипты в сценах:

- MainMenu: `THCleanMainMenuController`, `THFantasyButtonHover`.
- Map: `THMapController`, `THMapGridInput`, `THStrictGridHeroMovement`, `THMapObject`, `THTile`, `THSingleMapHoverLabel`, `THStoryManager`, camera helpers, `THBootstrap`.
- Combat: `THCombatRuntime`, `THBootstrap`.
- Base: `THBaseRuntime`, `THBaseUIPolish`, `THBootstrap`.

Основные runtime-группы:

- MainMenu: `THCleanMainMenuController`, legacy `THMainMenuController`, `THMainMenuControllerFixed`, UI `MainMenuUI`.
- Map / movement: `THMapController`, `THStrictGridHeroMovement`, `THMapGridInput`, `THTile`, legacy `THGuaranteedHeroMovement`, `THReliableHeroMovement`, `THHeroMover`, `Subsystems/Map/HeroMover`.
- Tile/Grid: `THTile`, `THMapTile`, `THMapGridInput`, `THMapBounds`, `Subsystems/Map/MapGrid`, `TileData`.
- Save/Load: active `THSaveSystem`, `THManager`, `THSavePolicy`; legacy `GameManager`, `SaveManager`, monolithic `TheHeroGameRuntime`.
- Combat: active `THCombatRuntime`; legacy `THCombatController`, `Subsystems/Combat/*`, monolithic `TheHeroCombatController`.
- Base/Castle: active `THBaseRuntime`; legacy `THBaseController`, `BaseController`, `THBaseBackButtonFix`, `THBaseExitFix`.
- Hover labels: active `THSingleMapHoverLabel`; old/duplicate paths in `THMapGridInput` and `THMapObjectVisuals`.
- Weekly income: `THWeeklyIncomeSystem`.
- Artifacts: `THArtifactManager`.
- Scene loading: `THSceneLoader`, `THSceneNavigator`, direct `SceneManager.LoadScene` calls.
- Map generation/editor: multiple manual builders/fixers in `Assets/Editor`, plus disabled old map editors.

Editor scripts found in `Assets/Editor`:

- Builders: `TheHeroAutoBuilder`, `TheHeroCompleteMVPBuilder`, `TheHeroReleaseBuilder`, `TheHeroPreReleaseBuilder`, `TheHeroFinalBuilder`.
- Scene/UI fixers: `TheHeroRebuildMainMenu`, `TheHeroCleanRebuildMainMenu`, `TheHeroRebuildActiveSceneCanvas`, `TheHeroRebuildMapFromScratchClean`, `TheHeroBuildWorkingBase`, `TheHeroBuildWorkingCombat`.
- Bug fixers: `TheHeroProblemFixer`, `TheHeroCriticalFixer`, `TheHeroCurrentBugsFixer`, `TheHeroFixNewGameReset`, `TheHeroFixMapCombatAndDarkLord`, `TheHeroFixMapAndBaseUI`, `TheHeroFixCombatTurnOrder`, `TheHeroFixBossCombatAndSavePolicy`, `TheHeroFixStartupAndCombatButtons`, `TheHeroFixOversizedMapSprites`, `TheHeroMovementAndBaseExitFixer`, `TheHeroStrictGridMovementFixer`, `TheHeroClickMovementDebugger`.
- Validators: `TheHeroValidationRunner`, `TheHeroDemoValidation`, `TheHeroPlaythroughValidator`, `TheHeroPreReleaseValidator`, `TheHeroReleaseValidator`, `TheHeroKingsBountyStyleValidation`.
- Startup/order helper: `THStartupSceneFixer`.
- Disabled old editors: `_DisabledOldMapEditors/*`.

## 2. Дубли и конфликты

- GameManager/state: `GameManager` + Subsystems and generated `THManager/THGameState`; active scenes use generated.
- SaveSystem: `THSaveSystem` and `SaveManager`; active scenes use `THSaveSystem`.
- Hero movement: active `THStrictGridHeroMovement`; deprecated duplicates `THGuaranteedHeroMovement`, `THReliableHeroMovement`, `THHeroMover`, `HeroMover`.
- Map controller: active `THMapController`; legacy `MapController`.
- Combat: active `THCombatRuntime`; deprecated `THCombatController` and subsystem combat controller.
- Base: active `THBaseRuntime`; deprecated `THBaseController` and subsystem base controller.
- MainMenu: active `THCleanMainMenuController`; deprecated `THMainMenuController`, `THMainMenuControllerFixed`.
- Hover labels: active `THSingleMapHoverLabel`; duplicate `MapCaption` path disabled.
- `Map.unity` has duplicate object id `Enemy_DarkLord_Final`; runtime now keeps one preferred object active and disables duplicate.

## 3. Что удалено

- Файлы runtime, сцены, ассеты и prefab не удалялись.
- Удалены только неиспользуемые marker constants / commented autoload paths for old `RUN_*.txt` editor workflows.

## 4. Что помечено deprecated

Комментариями помечены как неактивные/устаревшие:

- `THBaseController`
- `THCombatController`
- `THMainMenuController`
- `THMainMenuControllerFixed`
- `THGuaranteedHeroMovement`
- `THReliableHeroMovement`
- `THHeroMover`
- `THBaseBackButtonFix`
- `THBaseExitFix`
- `TheHeroGameRuntime`
- `GameManager`

## 5. Что исправлено

- `THBootstrap` больше не добавляет legacy controllers на Map/Combat/Base и не создаёт дубли runtime-систем.
- Editor autoload отключён: удалены активные `[InitializeOnLoad]` и `EditorApplication.delayCall`; операции оставлены как ручные `MenuItem`.
- Build Settings проверены: `MainMenu`, `Map`, `Combat`, `Base` идут в правильном порядке.
- `THSceneLoader` больше не autosave-ит при каждом переходе сцены.
- `THStoryManager` больше не сохраняет игру при показе диалога.
- `THMapController` больше не сохраняет игру при intro/base/combat transitions.
- `THBaseRuntime.BackToMap` и deprecated base exit helpers больше не сохраняют игру при выходе.
- New Game очищает старые save keys, сбрасывает enemy/resource/artifact/captured state и создаёт default army: Swordsman x12, Archer x8, Mage x0.
- `THManager` корректно создаёт clean New Game, если save exists but failed to load.
- `THMapGridInput` получил camera null-check и больше не создаёт второй hover label.
- `THMapObject` нормализует empty id/displayName и показывает hover через `THSingleMapHoverLabel`.
- `THStrictGridHeroMovement` получил guards для missing grid/camera/tile и не списывает movement ниже 0.
- Weekly recruits use correct ids and are capped by max values.
- `THCombatRuntime` применяет награду/defeated enemy/final victory только при победе, не при поражении.
- `THCombatRuntime` очищает combat context and `Combat_DarkLord` after applying result.
- `THBaseRuntime` показывает total recruit cost for all resources, не только gold.
- Editor deprecated API warnings fixed in manual fixer scripts.

## 6. Оставшиеся риски

- В `Map.unity` физически остаётся duplicate `Enemy_DarkLord_Final`; runtime disables duplicate safely. Удаление из YAML/сцены лучше делать через Unity Inspector после ручной проверки.
- В проекте всё ещё есть две архитектурные ветки: generated `TH*` и older `TheHero/Subsystems`. Older branch не активна в сценах, поэтому не удалялась.
- Часть текста в старых generated-файлах отображается mojibake из-за исторической кодировки, но это не менялось, чтобы не затронуть UI ассеты/сцены.
- Некоторые old editor builders всё ещё могут перестроить сцены вручную через меню; запуск автоматом отключён.
- `THCombatRuntime` пока использует default combat reward, а не полную reward model из map object.

## 7. Проверка компиляции

Пройдено:

- `dotnet build Assembly-CSharp.csproj` - 0 errors, 0 warnings.
- `dotnet build Assembly-CSharp-Editor.csproj` - 0 errors, 0 warnings.

## 8. Минимальный ручной тест

Пройти в Unity:

- MainMenu -> New Game -> Map.
- Continue -> Map.
- Hero movement по grid без диагоналей.
- Нельзя выйти за карту, пройти по water/mountain.
- Enemy blocker starts Combat.
- Defeated enemy исчезает/становится проходимым after return to Map.
- Resource collection даёт reward и скрывает объект.
- Hover label один, исчезает при уходе мыши, при Base/Combat transitions.
- Combat: turn queue visible, active player unit attacks selected target, enemy ходит одним активным unit, Skip skips only current player unit, AutoBattle ends through queue.
- ResultPanel shows reward, FinishBattleButton returns to Map.
- Defeat does not grant reward or mark enemy defeated.
- Base opens only from castle/button, resources and army visible.
- Recruit 1 / Recruit all / Upgrade обновляют UI и не уводят ресурсы в минус.
- BackToMap и Esc из Base возвращают на Map без autosave.
- End Turn до новой недели начисляет weekly income и autosave only on new week.
- New Game fully resets save state: enemies/resources return, defeated/collected/captured/artifacts empty, day=1, week=1.
- DarkLord starts final combat.
- DarkLord victory sets gameCompleted and shows final victory on Map.
