# Plynling Badge Art Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the 62 Plynling badges (11 paliers × 5 tiers + 7 moments) 22×22 rosette icons, uploaded as
application emojis and shown wherever `BadgeInfo.Icon` prints today.

**Architecture:**
- **Drawing:** the art is text grids in `tools/item-art/icons.py`. A new `rosette()` composes a fixed
  22×22 template in the tier's `SHIELD_TIERS` colours with a hand-drawn subject of at most 8×8, as
  `shield()` does for the member badges.
- **Bot wiring:** the bot learns the new icon family through one prefix in `ItemEmojis` and an `IconKey` on
  `BadgeInfo`. `Icon` prefers the uploaded emoji and falls back to today's medal + Unicode.

**Tech Stack:** Python 3 + Pillow (art tool), C# / .NET 10 (bot), the console checks in
`tests/ProjectSYNCS.Checks`.

**Spec:** `docs/superpowers/specs/2026-11-26-plynling-badge-art-design.md`

## Global Constraints

- **Files:** icon keys are `plbadge.<palier key>_<tier>` (tier ∈ `bronze silver gold platinum diamond`) and
  `plbadge.<moment key>`. The files are `ProjectSYNCS/Assets/Icons/<icon key>.png`, 22×22 exported ×4 (88 px).
- **Emoji names:** the prefix is `pb_`. Names are at most 32 characters, from `[A-Za-z0-9_]` only.
- **Subjects:** a fill of at most 8×8 (10×10 outlined), sitting only on the template's `f` and `M` cells.
- **Colours:** the frame reuses `SHIELD_TIERS` unchanged: bronze → diamond for tiers, `moment` for moments.
- **Badges unchanged:** keys, thresholds, rewards and names do not change. No migration.
- **Version:** `ProjectSYNCS/config.yaml` 8.26.0 → **8.26.1**, plus a `ProjectSYNCS/CHANGELOG.md` entry
  (`## 8.26.1 — 2026-11-26`, English, no member names).
- **No commits:** the owner commits by hand. Every "commit" step below is replaced by "leave the changes
  for the owner".
- **Shell:** on this machine, never `python -` or `python - <<EOF`, which leaves an orphan REPL. Run
  scripts by path.

## Review Focus

1. **Before upload** (a fresh app, or a failed upload), a badge shows today's medal + Unicode emoji, never
   an empty string. Task 2 tests it.
2. **The journal at its fullest**, with every icon uploaded under a 32-character name, keeps its one text
   display at or under 4000 characters. Task 2 tests it.
3. **A badge added later without art** makes the checks fail, which names the missing PNG, rather than
   silently showing the fallback forever. Task 2 tests it.
4. **A stray `plbadge.*.png`** that no catalog badge claims is skipped, not uploaded under a bogus name.
   Task 2 tests it.
5. **No clash:** a Plynling icon key never equals a member-badge or collection-set badge key. Task 2
   tests it.

---

### Task 1: The rosette and its 62 icons (art tool)

**Files:**
- Modify: `tools/item-art/icons.py`. After the member moments/secrets loop (the `# ---- Badges` section),
  add the `ROSETTE` template, `ROSETTE_GLINTS`, `rosette()`, `PL_BADGE_SUBJECTS` and the two loops.
- Modify: `tools/item-art/export.py:32-33` (`size_of`): add `"plbadge."` to the 22×22 prefixes.
- Modify: `tools/item-art/check.py:21-22`: add `"plbadge."` to the known kinds.
- Modify: `tools/item-art/README.md`: document the rosette in the Badges paragraph.
- Create: the 62 files `ProjectSYNCS/Assets/Icons/plbadge.*.png`, written by `export.py`.

**Interfaces:**
- Produces: the 62 PNGs, named exactly as in Global Constraints. Task 2's checks look them up by
  `BadgeInfo.IconKey`.

The template is the frame the owner approved (spike v1), as exact copy:

| Cell | Meaning |
|---|---|
| `L` | lit rim |
| `S` | shaded rim |
| `M` | inner line |
| `f` | field, shaded where `x + y > 20` |
| `#` | outline |
| `.` | air |

```python
ROSETTE = (
    "......................",
    "..........##..........",
    "......####LL####......",
    ".....#LLMMLLMMLL#.....",
    "....##MLMMMMMMLM##....",
    "...#LMMMffffffMSSM#...",
    "...#LLMffffffffMMM#...",
    "...#MMffffffffffMS#...",
    "...#MMffffffffffMS#...",
    "..#LLMffffffffffMMM#..",
    "..#LLMffffffffffMMM#..",
    "...#MMffffffffffMS#...",
    "...#MMffffffffffMS#...",
    "...#LLMffffffffMMM#...",
    "...#LMSMffffffMSSM#...",
    "....##SMMMMMMMMS##....",
    "....##MMSSMMSSMM##....",
    "....#M####MM####S#....",
    "...#MMMM#.##.#SSSS#...",
    "...#M##M#....#S##S#...",
    "....#..#......#..#....",
    "......................",
)
ROSETTE_GLINTS = ((7, 3), (4, 6), (11, 2), (3, 10))   # (x, y), in the order the tiers gain them
```

- [ ] **Step 1: Add the prefixes to the tool's two scripts**

  Add `"plbadge."` to `size_of`'s tuple in `export.py` and to `check.py`'s known-kind tuple.

- [ ] **Step 2: Add `rosette(tier, *subject)` → 22 strings of 22 characters**

  - **Colours:** read `SHIELD_TIERS[tier]` the way `shield()` does. Map `L` to lit, `S` to shade and `M` to
    inner. Map `f` to field where `x + y <= 20`, else to the shaded field.
  - **Glints:** set the first `count` of `ROSETTE_GLINTS`.
  - **Subject:** trim it like `shield()` does, then place its top left at `((22 - w) // 2, (20 - h) // 2)`.
  - **Assertions:** assert every non-`.` subject cell lands on an `f` or `M` template cell, and that the
    trimmed subject is at most 10×10.

- [ ] **Step 3: Add `PL_BADGE_SUBJECTS`**

  The dict holds one fill (at most 8×8, outlined at use) per key, in the subjects table's order.

  | Group | Key and subject |
  |---|---|
  | Paliers | `longevity` small tree · `player` die showing five · `champion` trophy cup · `traveller` signpost with two arrows · `host` cottage door with a heart · `gourmet` small mushroom cap · `cuddly` glue pot with its brush · `beloved` gift box · `training` dumbbell · `level` star · `athlete` flexed arm |
  | Moments | `saved` life ring · `resurrected` halo with a sparkle · `first_friend` handshake · `best_friend` yellow heart · `couple` two overlapping hearts, pink and red · `recovered` two-tone capsule · `dapper` top hat |

  - **The die is fixed copy** (approved):
    `"wwwwwwws","w#www#ss","wwwwwwss","www#wwss","wwwwwwss","w#www#ss","wwwwwsss","sssssssG"`.
  - **The yellow heart** starts from the spike's 9×8 heart, trimmed to 8 wide.
  - **Everything else** follows the README's style rules: one outline colour, inner `#` separations, shading
    along a hue ramp, and the silhouette judged at 22 px first.

- [ ] **Step 4: Register the icons**

  - **Names and Unicode:** take the sheet name from the catalog's masculine name, then ` · ` and the tier
    label (`BADGE_TIERS`). Take the Unicode from the catalog emoji.
  - **Paliers:** loop over `(key, name, emoji)` triples for the 11 paliers in catalog order (longevity 🌳,
    player 🎲, champion 🏆, traveller 🌍, host 🏠, gourmet 🍄, cuddly 🤲, beloved 🎁, training 🏋️, level ⭐,
    athlete 💪). For each tier in `BADGE_TIERS`, call
    `icon("plynling_badges_paliers", f"plbadge.{key}_{tier}", …, *rosette(tier, *outlined(*PL_BADGE_SUBJECTS[key])))`.
  - **Moments:** for each of the 7 moments (saved 😮‍💨, resurrected ✨, first_friend 🤝, best_friend 💛,
    couple 💞, recovered 💊, dapper 🎩), call `icon("plynling_badges_moments", f"plbadge.{key}", …, *rosette("moment", …))`.

- [ ] **Step 5: Run the art checks**

  Run: `python tools/item-art/check.py`

  Expected: the last line is `OK`. Every rosette passes the outline, palette and edge rules, and the
  `rosette()` assertions.

- [ ] **Step 6: Export the two groups**

  Run: `python tools/item-art/export.py plynling_badges_paliers plynling_badges_moments`

  Expected:
  - The output prints `plynling_badges_paliers: 55 icons` and `plynling_badges_moments: 7 icons`.
  - `ls ProjectSYNCS/Assets/Icons/plbadge.* | wc -l` prints `62`.

- [ ] **Step 7: Owner review (gate)**

  - Send the owner `tools/item-art/out/sheet_plynling_badges_paliers.html` and
    `tools/item-art/out/sheet_plynling_badges_moments.html`.
  - Redraw what they reject, repeating Steps 5–6 each time. Stop here until they approve.
  - If the handshake doesn't read, the fallback is two linked rings.

- [ ] **Step 8: Update `tools/item-art/README.md`**

  Write one paragraph after the shield's:
  - **The family:** Plynling badges (`plbadge.<key>`) use `rosette(tier, …)`, a prize rosette with tails
    in the same `SHIELD_TIERS` colours.
  - **Subjects:** at most 8×8 so the medallion shows, kept in `PL_BADGE_SUBJECTS`.
  - **Sheets:** `plynling_badges_paliers` and `plynling_badges_moments`.

  Then leave the changes for the owner.

### Task 2: Show the icons in the bot

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingBadges.cs`: add `BadgeInfo.IconKey` and
  `PlynlingBadges.IsIconKey`, and change `BadgeInfo.Icon`.
- Modify: `ProjectSYNCS/Helpers/Economy/ItemEmojis.cs`: add `("plbadge.", "pb_")` to `Prefixes`, and a
  `PlynlingBadges.IsIconKey(key)` clause in `IsIconKey`.
- Create: `tests/ProjectSYNCS.Checks/PlynlingBadgeArtChecks.cs`, section 75.
- Modify: `tests/ProjectSYNCS.Checks/Program.cs`: register section 75 after 74.
- Modify: `ProjectSYNCS/config.yaml` (8.26.1) and `ProjectSYNCS/CHANGELOG.md`.
- Modify: `docs/agents/plynling/badges.md`, last bullet, and `docs/agents/economy.md`, *Item icons*.

**Interfaces:**
- Consumes: the 62 PNGs from Task 1.
- Produces:
  - `public string BadgeInfo.IconKey { get; }`: `plbadge.{palier.Key}_{tier lowercased}` for a tier,
    `plbadge.{Key}` for a moment.
  - `public static bool PlynlingBadges.IsIconKey(string key)`.
  - `BadgeInfo.Icon` returns `ItemEmojis.For(IconKey) ?? <today's value>`.

- [ ] **Step 1: Write the failing checks**

  Create `PlynlingBadgeArtChecks.cs` with `public static void Run(Action<string> fail)`, in the style of
  `StatsChecks` (an `Expect(bool, string)` helper prefixed `"badge art: "`). Register it in `Program.cs`:

  ```csharp
  // 75. Plynling badge art: icon keys, emoji names, fallbacks, shipped files, journal length: PlynlingBadgeArtChecks.cs.
  PlynlingBadgeArtChecks.Run(Fail);
  ```

  The cases, with these exact values:

  - **Keys:**
    - `PlynlingBadges.All.Select(b => b.IconKey).Distinct().Count() == 62`.
    - For every badge, `PlynlingBadges.IsIconKey(b.IconKey)` holds.
    - `ItemEmojis.EmojiName(b.IconKey)` starts with `"pb_"`, has length ≤ 32 and is all
      `char.IsAsciiLetterOrDigit(c) || c == '_'`.
    - Neither `MemberBadges.IsIconKey(b.IconKey)` nor `CollectionTiers.IsBadgeKey(b.IconKey)` holds.
  - **Pinned:**
    - `PlynlingBadges.ByKey("week")!.IconKey == "plbadge.longevity_bronze"`.
    - `ByKey("games_100")!.IconKey == "plbadge.player_platinum"`.
    - `ByKey("two_years")!.IconKey == "plbadge.longevity_diamond"`.
    - `ByKey("couple")!.IconKey == "plbadge.couple"`.
    - `ItemEmojis.EmojiName("plbadge.beloved_platinum") == "pb_beloved_platinum"`.
  - **Fallback:** after `ItemEmojis.Clear()`, `ByKey("week")!.Icon == "🥉🌳"` and
    `ByKey("couple")!.Icon == "💞"`.
  - **Uploaded:** `ItemEmojis.Set("plbadge.couple", "<:pb_couple:1>")`, then
    `ByKey("couple")!.Icon == "<:pb_couple:1>"`. Then `ItemEmojis.Clear()`.
  - **Shipped:**
    - Find `ProjectSYNCS/Assets/Icons` the way `BadgeChecks` does.
    - With `shipped = ItemEmojis.Sources("", iconDir).ToDictionary(s => s.Key, s => s.EmojiName)`, every
      `b.IconKey` is in `shipped` with value `ItemEmojis.EmojiName(b.IconKey)`. The failure message names
      the missing `<key>.png`.
  - **Stray file:**
    - Copy any shipped `plbadge.*.png` to a temp dir as `plbadge.nope.png`.
    - `ItemEmojis.Sources("", tempDir).Single().EmojiName is null`.
  - **Length:**
    - Set every `b.IconKey` to `ItemEmojis.Markup("pb_" + new string('x', 29), ulong.MaxValue)`.
    - Build a `Plynling` named with 32 `*`, holding every badge (`new PlynlingBadge { Key = b.Key }` for
      all 62).
    - Give it `PlynlingJournalCards.MomentsPerPage` moments, each
      `new PlynlingJournalEntry { Kind = JournalKind.Badge, Detail = "two_years", At = now }`.
    - Pass `facts: new BadgeFacts(TimeSpan.FromDays(9999), BestBodyStat: 99)`.
    - Then `PlynlingJournalCards.JournalText(p, badges, moments, 0, now, null, facts).Length <= 4000`.
    - `ItemEmojis.Clear()` at the end.

- [ ] **Step 2: Run the checks to see them fail**

  Run: `dotnet run --project tests/ProjectSYNCS.Checks`

  Expected: a build error on `IconKey` / `IsIconKey`. This is the red step.

- [ ] **Step 3: Implement the wiring**

  - **`PlynlingBadges.cs`:**
    - Add `IconKey` (computed from `Palier`, `Tier`, `Key`).
    - Change `Icon` to `ItemEmojis.For(IconKey) ?? (Palier is null ? Emoji : CollectionTiers.Medal(Tier) + Emoji)`.
    - Add `IsIconKey` over a `HashSet<string>` of `All`'s icon keys, declared in the same file after `All`.
      A static initializer must not read a static field in another file.
  - **`ItemEmojis.cs`:**
    - Add the prefix pair and the `IsIconKey` clause.
    - Extend the `Prefixes` comment: `pb_` is the Plynling rosettes.

- [ ] **Step 4: Run the checks to see them pass**

  Run: `dotnet run --project tests/ProjectSYNCS.Checks`

  Expected: `ALL CHECKS PASSED`. If section 75's length check fails, report the measured length to the
  owner before trimming anything.

- [ ] **Step 5: Build the bot**

  Run: `cd ProjectSYNCS && dotnet build -warnaserror`

  Expected: the build succeeds with 0 errors. dotnet prints in French: « La génération a réussi ».

- [ ] **Step 6: Bump the version, add the changelog, update the docs**

  - **Version:** in `config.yaml`, `version: "8.26.1"`.
  - **Changelog:** in `CHANGELOG.md`, newest first:

    ```markdown
    ## 8.26.1 — 2026-11-26

    - Plynling badges get their own pictures: a prize rosette per tier, with a subject per palier and moment.
    ```
  - **`docs/agents/plynling/badges.md`:** replace « Paliers have no art yet… » with one bullet. It says
    each tier and moment has a rosette icon `plbadge.<palier>_<tier>` / `plbadge.<moment>` (emoji
    `pb_…`), that `BadgeInfo.Icon` falls back to medal + emoji until it is uploaded, and that a new badge
    needs its icon (checks section 75).
  - **`docs/agents/economy.md`, *Item icons*:** add `plbadge.` as a 22×22 family drawn as rosettes. Add
    `pb_` to both lists of emoji prefixes: the `ItemEmojis.EmojiName` list and the "don't upload a
    hand-made emoji starting with" list.

  Then leave everything for the owner to commit.

- [ ] **Step 7 (owner, optional): Check in Discord**

  - Run against the dev guild. On Ready, the log shows the 62 `pb_` uploads.
  - `/plynling journal` on a Plynling with badges shows the rosettes inline.
