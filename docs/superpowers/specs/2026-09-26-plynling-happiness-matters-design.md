# Plynlings: happiness matters — design

**Date:** 2026-09-26 · **Status:** approved

Happiness used to change only the face. Now it changes what meals are worth, a Plynling at 0 %
sulks, and a happy one sometimes brings back a gift.

## 1. Mood changes a meal's worth

Measured at the moment it eats, on **hunger only** (price and the food's happiness unchanged):

- **Happy** (happiness > 80 %): the meal fills **+15 %** — « Heureux, il mange de bon appétit (+15 %) ».
- **Sad** (happiness < 30 %): the meal fills **−25 %** — « Triste, il chipote (−25 %) ».

Whoever feeds it, owner or friend.

## 2. The sulk

At **0 %** happiness (below 0.5 %, what the card shows as 0 %) it refuses every meal, from anyone,
and nothing is charged: « Il boude : il veut qu'on joue avec lui ou qu'on le caresse. » One pet or
one game ends it. **Safety:** while it is **starving** (hunger < 25 %) it eats anyway — so the
sulk can never kill it, including at night when petting is refused.

## 3. The happy gift

The first time each day (Paris) the **owner** looks at their own card — `/plynling view`, or a
pet or a meal from the card — while it is **happy** (> 80 %), one draw: **50 %** chance of
**5–15 cailloux** to the owner, « 🪨 Pouf a trouvé un joli caillou pour toi ! +8 cailloux ». One
draw a day, win or lose, recorded in `Plynling.LastGiftDay` (migration `AddPlynlingGiftDay`), so a
restart cannot give a second. Not while it is dead, frozen or asleep; a look while it is not happy
does not use the day's draw. Not journaled.

## Checks

The meal factor at every threshold and the price unchanged; the sulk and its starving safety; the
gift's odds, amounts and one draw a day against an in-memory database; the lines in both genders.
Build `-warnaserror`; docs; version.
