# The Hero Balance Rework Report

## 1. Starting resources

- Gold: 300
- Wood: 10
- Stone: 5
- Mana: 0

## 2. Starting army

- Swordsman x8
- Archer x4
- Mage x0

## 3. Recruitment prices

- Swordsman: 80 gold
- Archer: 110 gold, 1 wood
- Mage: 180 gold, 2 mana

## 4. Weekly growth

- Swordsman: +6 available recruits
- Archer: +4 available recruits
- Mage: +2 available recruits from week 2 onward

Week 1 starts with Swordsman 6, Archer 4, Mage 0.

## 5. Weekly income

- Base income: +250 gold, +8 wood, +5 stone, +1 mana
- Gold mine: +250 gold per week
- Lumber mill: +15 wood per week
- Stone quarry: +12 stone per week
- Mana source: +6 mana per week

Income is applied on day 7 -> day 1 of the next week and triggers the normal new-week save policy.

## 6. Damage formula

Damage now scales from stack size and target defense:

```text
baseDamage = attacker.attack * attacker.count
defenseReduction = clamp(defender.defense * 0.04, 0, 0.60)
finalDamage = max(1, round(baseDamage * (1 - defenseReduction)))
```

Combat stacks now track total current HP during battle. Damage reduces total HP, and stack count is recalculated with `ceil(currentTotalHp / hpPerUnit)`, so wounded stacks lose damage output as units die.

## 7. Final boss army

- Dark Lord x1: HP 180, ATK 22, DEF 10, INIT 6
- Dark Guard x14: HP 45, ATK 10, DEF 5, INIT 4
- Orc x18: HP 35, ATK 8, DEF 3, INIT 3
- Skeleton x16: HP 28, ATK 7, DEF 4, INIT 3

## 8. Why the starting army cannot beat the boss

Starting army power is far below the final army power. The boss side has several high-HP stacks, strong defense, and enough first-round damage to destroy the starter Swordsman/Archer army before it can meaningfully reduce the guard stacks.

The starter can still clear Tier 1 enemies, but Tier 3+ and DarkLord require weekly growth, resource collection, and recruitment.

## 9. Manual tests

1. MainMenu -> New Game.
2. Confirm resources: 300 gold, 10 wood, 5 stone, 0 mana.
3. Confirm army: Swordsman x8, Archer x4, Mage x0.
4. Defeat a weak guard.
5. Try a strong guard and confirm heavy losses or defeat.
6. Try DarkLord immediately and confirm defeat.
7. End turns until a new week starts.
8. Confirm weekly income and recruit availability increased.
9. Open Base and recruit available units.
10. Progress for several weeks, collect resources, capture income sources, and confirm stronger enemies become beatable.
11. Fight DarkLord only with a prepared army.
12. Confirm the Console has no red errors.
