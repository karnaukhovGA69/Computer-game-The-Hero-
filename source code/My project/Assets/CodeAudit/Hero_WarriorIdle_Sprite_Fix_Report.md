# Hero WarriorIdle Sprite Fix Report

Date: 2026-05-15 19:41

- Warrior_Idle.png found: **True** (`Assets/ExternalAssets/MainAssets/Warrior_Idle.png`)
- Warrior_Idle_0 sub-sprite found: **False**
- Slicing restored: **True** (8 × 192×192 frames)
- Sprite assigned to Hero: `(none)`
- Is whole sheet: **false** (rect 192×192 < texture 1536×192)
- Error: **Warrior_Idle_0 not loadable**

## Manual verification
1. **The Hero → Map → Fix Hero Warrior Idle Sprite**
2. **The Hero → Validation → Validate Map MainAssets With Fallbacks**
3. Expect `PASS Hero sub-sprite` and FAIL=0.
