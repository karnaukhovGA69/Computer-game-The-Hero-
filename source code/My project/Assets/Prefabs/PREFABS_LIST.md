# Список префабов проекта The Hero

## Итого: 10 префабов

---

## UI Префабы (Assets/Prefabs/UI/)

### 1. SquadRow.prefab
**Компонент:** `SquadRowUI`
**Используется в:** CombatUI (_squadRowPrefab)
**Дочерние объекты:**
- `NameText` — TextMeshProUGUI → _nameText
- `CountText` — TextMeshProUGUI → _countText
- `HPText` — TextMeshProUGUI → _hpText
- `SelectButton` — Button → _selectButton

### 2. BuildingRow.prefab
**Компонент:** `BuildingRowUI`
**Используется в:** BaseUI (_buildingRowPrefab)
**Дочерние объекты:**
- `NameText` — TextMeshProUGUI → _nameText
- `LevelText` — TextMeshProUGUI → _levelText
- `RecruitsText` — TextMeshProUGUI → _recruitsText
- `RecruitCostText` — TextMeshProUGUI → _recruitCostText
- `UpgradeCostText` — TextMeshProUGUI → _upgradeCostText
- `RecruitButton` — Button → _recruitButton
- `UpgradeButton` — Button → _upgradeButton

### 3. InfoWindow.prefab
**Компонент:** `InfoWindowUI`
**Используется в:** любой сцене, где нужно информационное окно
**Дочерние объекты:**
- `TitleText` — TextMeshProUGUI → _titleText
- `BodyText` — TextMeshProUGUI → _bodyText
- `CloseButton` — Button → _closeButton

---

## Сцены (Assets/Scenes/)

### 4. MainMenu.unity
**Компоненты на Canvas:**
- `MainMenuUI` с кнопками:
  - `NewGameButton` → _newGameButton
  - `ContinueButton` → _continueButton
  - `SettingsButton` → _settingsButton
  - `ExitButton` → _exitButton

### 5. Map.unity
**Компоненты на Canvas:**
- `MapUI` с полями:
  - `GoldText`, `WoodText`, `StoneText`, `ManaText` — TextMeshProUGUI
  - `DayWeekText` — TextMeshProUGUI
  - `HeroNameText`, `HeroLevelText`, `MovementPointsText`, `ArmyCountText` — TextMeshProUGUI
  - `EndTurnButton` — Button
**На сцене (не Canvas):**
- `GameManager` (если первая сцена в Build Settings — перенести сюда)
- `HeroMover` — на GameObject героя

### 6. Combat.unity
**Компоненты на Canvas:**
- `CombatUI` с полями:
  - `PlayerSquadsContainer` — Transform
  - `EnemySquadsContainer` — Transform
  - `SquadRowPrefab` → SquadRow.prefab
  - `EndRoundButton` — Button
  - `CombatLogText` — TextMeshProUGUI
  - `LogScrollRect` — ScrollRect

### 7. Base.unity
**Компоненты на Canvas:**
- `BaseUI` с полями:
  - `BuildingsContainer` — Transform
  - `BuildingRowPrefab` → BuildingRow.prefab

---

## Игровые GameObject-префабы (Assets/Prefabs/Game/)

### 8. Hero.prefab
**Компоненты:**
- `HeroMover` — _stepDuration = 0.15f
- `SpriteRenderer` — спрайт героя

### 9. MapTileView.prefab
**Компоненты:**
- `SpriteRenderer` — спрайт тайла
**Нужен для отрисовки карты MapController-ом**

### 10. GameManager.prefab
**Компоненты:**
- `GameManager` (DontDestroyOnLoad — создать в сцене MainMenu)
- `AudioManager` с:
  - `MusicSource` — AudioSource → _musicSource
  - `SFXSource` — AudioSource → _sfxSource
- `LocalizationManager`

---

## Build Settings — порядок сцен

| Индекс | Сцена      |
|--------|------------|
| 0      | MainMenu   |
| 1      | Map        |
| 2      | Combat     |
| 3      | Base       |
