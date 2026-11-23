# Piece D — richer crafting and forage places — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Players aim their forage at a village place, every recipe becomes reachable in a measured
time, and crafting gains an easy tier fed by duplicates.

**Architecture:** A pure `ForagePlaces` helper owns the places, which items each place holds, the
aimed draw and the typed-name matching; `PlynlingService.ForageAsync` takes an optional place. A
pure `Recipes` helper owns ingredients (named item or « any commons of a set »), what a person
lacks, and which stacks a craft takes. The catalog gains 10 crafted cosmetics and a trophy flag; the
cosmetics screen and the collection book read places and recipes from those helpers.

**Tech Stack:** .NET 10, Discord.Net 3.20 (Components V2), EF Core + SQLite, the console checks
project `tests/ProjectSYNCS.Checks`.

**Spec:** `docs/superpowers/specs/2026-11-23-crafting-and-forage-places-design.md` (read it first),
built on piece A: `docs/superpowers/specs/2026-11-23-cosmetics-screen-design.md`.

## Global Constraints

- Read `CLAUDE.md`, `docs/agents/economy.md` and `docs/agents/plynling.md` before coding; their rules bind.
- Command and option names English (`place`); every user-facing string French; code, comments, logs English.
- Item and cosmetic keys are append-only: never rename or reorder an existing `col.*` / `cos.*` key.
  New cosmetic keys follow `cos.<slot>.<name>`.
- Places are **not stored** (no migration, no DB column).
- No `DateTime.Now`; seasons via `ItemCatalog.SeasonAt(now)`.
- Lines are picked with `ResponsePicker.Pick`, gendered with `GenderedLines` / `Agree`, written to
  `docs/plynling-writing-style.md` and true to `docs/village-lore.md` (§ 5 places).
- Components V2: ≤ 40 components, ≤ 4 000 characters, no duplicate custom-id, selects ≤ 25 options.
- `/plynling help` and `/help` pages each ≤ 6 000 characters (page 0 of `/plynling help` is at 5 874 now).
- Tilt constant ≈ 0.75; bands: Ouvrage **10–25** forages, Chef-d'œuvre **60–150** forages.
- Ouvrages ≈ 100 cailloux, trophies 200–300 cailloux; each recipe made once per person (`AlreadyOwned` stays).
- Version: **8.4.0** in `ProjectSYNCS/config.yaml` (check `git diff` first) + `ProjectSYNCS/CHANGELOG.md` entry.
- Do not commit: the owner commits by hand unless they ask. Every task ends with build + checks green.
- Build: `cd ProjectSYNCS && dotnet build -warnaserror` → 0 warnings, 0 errors.
  Checks: `dotnet run --project tests/ProjectSYNCS.Checks` → last line `ALL CHECKS PASSED`.

## Review Focus

1. Typed places as people write them — `;forage La Lisière`, `;forage la place du puits`,
   `;FORAGE riviere`, `;pl forage marais` — all match; a word matching nothing refuses without
   foraging and without starting the 4 h wait. (Task 3 test.)
2. Aiming at a place out of season: a rarity with nothing findable there falls back to its commons,
   and every place has at least one all-year common, so an aimed forage never finds nothing. (Task 1 test.)
3. Placeless forage must equal today's draw exactly (mushroom share untouched), food included, and
   still names a place. (Task 1 test.)
4. « Il a tout » fires once: not for a recipe already ready before the find, not for one already
   owned, never for cailloux alone. (Task 6 test.)
5. A craft with « any commons » takes the biggest stacks first and never more than asked, under the
   existing concurrency retry. (Task 4 test.)

Note on aliases: the spec lists `pre` for le Communal, but the lore's *pré de la Sommeille* is by the
river, so `pre` is ambiguous — **leave `pre` out** (builder's call allowed by the spec: "more may be added").

---

### Task 1: Places and the aimed draw

**Files:**
- Create: `ProjectSYNCS/Helpers/Economy/ForagePlaces.cs`
- Modify: `ProjectSYNCS/Helpers/Economy/ItemCatalog.cs` (expose the inner draw for reuse; no behaviour change)
- Create: `tests/ProjectSYNCS.Checks/ForagePlaceChecks.cs` (section 60); register in `tests/ProjectSYNCS.Checks/Program.cs` after section 59

**Interfaces:**
- Produces:
  - `public sealed record ForagePlace(string Key, string Name, string Emoji, IReadOnlyList<string> Aliases)` — `Name` with its article (« la Sommeille »), for « à la Sommeille » use `ForagePlaces.At(place)`.
  - `public static class ForagePlaces`:
    - `const double Tilt = 0.75;`
    - `IReadOnlyList<ForagePlace> All` — keys `sommeille`, `lisiere`, `mouilles`, `communal`, `puits`.
    - `ForagePlace? ByKey(string? key)`
    - `IReadOnlyList<ForagePlace> Of(ItemInfo item)` — the item's places (empty for non-collectibles).
    - `IEnumerable<ItemInfo> ItemsAt(ForagePlace place)`
    - `string At(ForagePlace place)` — « à la Sommeille », « au Communal », « aux Mouilles », « à la Lisière », « sur la place du Puits ».
    - `ForagePlace? Match(string text)` — forgiving typed match.
    - `(ItemInfo Item, ForagePlace Place) Draw(Random rng, DateTimeOffset now, ForagePlace? aimed)`
  - `ItemCatalog.DrawFrom` becomes `internal static` (or a public `DrawCollectibleFrom(Random, DateTimeOffset, IReadOnlyList<ItemInfo> pool)` the place draw shares) — rarity roll 60/28/10/2 and commun fallback live in one place only.

Data shape (so a new collection only lists places): a set → default places map, plus a per-item
override map; `Of(item)` = override ?? set default.
- Set defaults: `cailloux` → sommeille; `champignons` → lisiere; `insectes` → mouilles; `nature` → communal; `tresors` → puits.
- Saisons overrides: `coquillage` → sommeille; `chataigne` → lisiere; `primevere`, `coquelicot` → communal; `cerise`, `citrouille`, `flocon`, `cristal_givre` → builder places each where it fits (e.g. cerise/citrouille → communal or puits market; flocon/cristal → sommeille or lisiere).
- Aliases (normalized form): sommeille: `sommeille`, `riviere`, `pont`; lisiere: `lisiere`, `foret`, `bois`; mouilles: `mouilles`, `marais`, `etang`; communal: `communal`, `pature`; puits: `puits`, `place`, `marche`, `village`.
- `Match`: lowercase, strip diacritics (`string.Normalize(FormD)` + drop `NonSpacingMark`), trim, strip a leading `la `, `le `, `les `, `l'`, `au `, `aux `, then accept the whole text equal to an alias, or to the normalized place name without its article, or (for « place du puits ») its last word being an alias.

`Draw` algorithm:
- `aimed is null`: `item = ItemCatalog.DrawForage(rng, now)` (unchanged, consumes rng exactly as today), then `place = Of(item)` non-empty ? `Of(item)[rng.Next(count)]` : `All[rng.Next(All.Count)]` (food).
- `aimed` set: food first, exactly as `DrawForage` (same `ForageFoodChance` roll and food roll); otherwise with probability `Tilt` draw from `ItemsAt(aimed)` filtered `Findable(i, now)` with the shared rarity roll + commun fallback, else `ItemCatalog.DrawCollectible(rng, now)`; place = `aimed`.

- [ ] **Step 1: Write the failing checks (`ForagePlaceChecks.Run(Action<string> fail)`)**
  - Every `ItemCatalog.Collectibles` item has `Of(i).Count >= 1`.
  - Every place, in each of the four seasons (instants months 4, 7, 10, 1, day 15, 12:00 UTC), has at least one findable `Common` item.
  - No normalized alias appears in two places; `Match` returns the right key for: `"Sommeille"`, `"la Sommeille"`, `"RIVIÈRE"`, `"la Lisière"`, `"lisiere"`, `"marais"`, `"les Mouilles"`, `"le Communal"`, `"la place du Puits"`, `"place du puits"`, `"puits"`; returns null for `"pre"`, `"blabla"`, `""`.
  - Placeless equals today: for seeds 0..999, `Draw(new Random(s), now, null).Item.Key == ItemCatalog.DrawForage(new Random(s), now).Key`, and its `Place` is in `Of(item)` (or any place for food).
  - Tilt works: 20 000 aimed draws per place (seed 60), among collectible finds the share whose `Of` contains the place is ≥ 0.70.
- [ ] **Step 2: Run checks** — Expected: build fails (`ForagePlaces` not defined).
- [ ] **Step 3: Implement `ForagePlaces` and expose the shared draw in `ItemCatalog`.**
- [ ] **Step 4: Run build + checks** — Expected: `ALL CHECKS PASSED`.

---

### Task 2: Forage at a place — service, slash option, reply lines

**Files:**
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingService.cs:23` (`ForageResult`), `ProjectSYNCS/Services/Plynling/PlynlingService.Life.cs:205-225`
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.cs:384-410` (`ForageAsync`, `ForageReply`)
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingShortcutService.cs:96-100` (pass `null` place and `_picker` for now)
- Create: `ProjectSYNCS/Services/Personality/BotResponses/BotResponses.Plynlings.Forage.cs`
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingText.cs:213-215` (`Foraged` retires or becomes the food suffix helper)
- Test: `tests/ProjectSYNCS.Checks/ForagePlaceChecks.cs`

**Interfaces:**
- Consumes: `ForagePlaces.Draw`, `ForagePlace`.
- Produces:
  - `public sealed record ForageResult(CareOutcome Outcome, Plynling? Plynling, ItemFind? Find = null, DateTimeOffset? ReadyAt = null, ForagePlace? Place = null, IReadOnlyList<CosmeticInfo>? NowReady = null);` (`NowReady` filled in Task 6; null until then)
  - `PlynlingService.ForageAsync(ulong guildId, ulong ownerId, DateTimeOffset now, Random rng, ForagePlace? place = null)`
  - `BotResponses.PlynlingForaged : IReadOnlyDictionary<string, GenderedLines>` keyed by place key; each line has `{0}` = bold-ready safe name, `{1}` = item label (`PlynlingText.ItemLabel`). At least 4 M and 4 F lines per place.
  - `PlynlingModule.ForageReply(ForageResult result, ulong userId, ResponsePicker picker)` — the found line is `string.Format(picker.Pick(PlynlingForaged[place.Key].For(gender)), name, label)`, plus for food the fixed suffix « , rangé dans ton garde-manger » placed as the pool's grammar allows (builder: either a separate food pool per place or a suffix sentence « Rangé dans ton garde-manger ! »).
  - Slash: `ForageAsync([Summary("place", "Où l'envoyer fouiller (au hasard si vide)")][Choice("La Sommeille (rivière)", "sommeille")]…[Choice("La place du Puits", "puits")] string? place = null)` — resolve with `ForagePlaces.ByKey`.

- [ ] **Step 1: Write the failing check** — every place key in `ForagePlaces.All` has an entry in `BotResponses.PlynlingForaged` with ≥ 4 M and ≥ 4 F lines; every line contains `{0}` and `{1}` and formats without throwing with `("Pépin", "🐚 un Coquillage")`; no line contains a brace other than `{0}`/`{1}`.
- [ ] **Step 2: Run checks** — Expected: build fails (`PlynlingForaged` missing).
- [ ] **Step 3: Write the lines, change `ForageResult`, `ForageOnceAsync` (draw via `ForagePlaces.Draw(rng, now, place)`, grant `draw.Item`, return `Place = draw.Place`; refusals and cooldown unchanged), `ForageReply`, the slash option; pass `null` + `_picker` from the shortcut service.**
- [ ] **Step 4: Run build + checks** — Expected: `ALL CHECKS PASSED`; `/plynling` still has 15 subcommands.

---

### Task 3: Typed place — `;forage <place>` and `;pl forage <place>`

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingShortcut.cs` (record, `Parse`, `ParsePl`, `Usage`)
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingShortcutService.cs:96-100`
- Test: `tests/ProjectSYNCS.Checks/Program.cs` next to the existing shortcut checks (~line 4509)

**Interfaces:**
- Consumes: `ForagePlaces.Match`, `ForagePlaces.All`, `ForageAsync(..., place)`, `ForageReply(..., picker)`.
- Produces:
  - `public readonly record struct Shortcut(ShortcutKind Kind, ulong? UserId = null, string? Place = null, bool BadPlace = false);` — `Place` is a place key.
  - `PlynlingShortcut.Where` — the refusal text: « Où ça ? » + the five places with one alias each, e.g. `;forage riviere`.
  - Service: `BadPlace` → `RefuseAsync(message, PlynlingShortcut.Where)` without calling `ForageAsync`; otherwise `ForageAsync(..., ForagePlaces.ByKey(shortcut.Place))`.

- [ ] **Step 1: Write the failing checks**
  - `Parse(";forage") == new Shortcut(ShortcutKind.Forage)`; `Parse(";pl forage") == new Shortcut(ShortcutKind.Forage)`.
  - `Parse(";forage riviere") == new Shortcut(ShortcutKind.Forage, Place: "sommeille")`; same for `";FORAGE La Lisière"` → `lisiere`, `";forage la place du puits"` → `puits`, `";pl forage marais"` → `mouilles`.
  - `Parse(";forage blabla") == new Shortcut(ShortcutKind.Forage, BadPlace: true)`; `Parse(";foragex") is null`.
  - Existing `;work x` / `;daily x` → `Unknown` stays.
- [ ] **Step 2: Run checks** — Expected: FAIL on the new cases.
- [ ] **Step 3: Implement** (the rest words joined by a space go to `ForagePlaces.Match`), update `Usage` to mention `;forage [lieu]`.
- [ ] **Step 4: Run build + checks** — Expected: `ALL CHECKS PASSED`.

---

### Task 4: « Any commons of a set » ingredients and crafting

**Files:**
- Create: `ProjectSYNCS/Helpers/Economy/Recipes.cs`
- Modify: `ProjectSYNCS/Services/Economy/CosmeticService.cs:55-78` (`CraftOnceAsync`)
- Modify: `ProjectSYNCS/Commands/Economy/CosmeticCards.cs:234-252` (`Missing`, `RecipeStatus` use `Recipes`)
- Create: `tests/ProjectSYNCS.Checks/CraftingChecks.cs` (section 61); register after section 60. Reuse the in-memory `Db` pattern of `EconomySinkChecks` (copy the small class; it is private there).

**Interfaces:**
- Produces (`public static class Recipes`):
  - `string AnyCommon(string setKey)` → `"any:" + setKey`; `bool IsAnyCommon(string key, out string setKey)`.
  - `int Have(string key, IReadOnlyDictionary<string, int> held)` — named: its count; any-common: sum of held counts of the set's `Common` items.
  - `IReadOnlyList<(string ItemKey, int Count)> Take(string key, int count, IReadOnlyDictionary<string, int> held)` — named: `[(key, count)]`; any-common: one unit at a time from whichever held stack is currently largest (ties: earliest in catalog order), summing exactly `count` — stacks level down, so the last single copies (which a turn-in needs) go last.
  - `string Name(string key, int count)` — named: `ItemCatalog.ClearName`; any-common: « champignons communs » style (`$"{set.Name.ToLower()} communs"` adjusted by the builder for each set's French: « champignons communs », « cailloux communs », « insectes communs »…).
  - `string Emoji(string key)` — named: the item's; any-common: the set's.
  - `IEnumerable<ItemInfo> Items(string key)` — named: the item; any-common: the set's commons (for places, Task 6/7).
  - `List<string> Lacking(CosmeticInfo recipe, IReadOnlyDictionary<string, int> held)` — « 2 × Améthyste », « 3 champignons communs » (items only; cailloux stay in `CosmeticCards.Missing`).
  - `bool ItemsReady(CosmeticInfo recipe, IReadOnlyDictionary<string, int> held)`.
- `CosmeticInfo.Recipe` keeps its type `IReadOnlyList<(string ItemKey, int Count)>`; an `ItemKey` may now be an any-common key. `CosmeticCards.Missing(recipe, data)` = `Recipes.Lacking(recipe, data.Held)` + the cailloux line.
- `CraftOnceAsync`: build `held` for the recipe's items (`InventoryService.CountAsync` per item of `Recipes.Items(key)`), refuse `MissingIngredients` unless `ItemsReady`, then for each part `TakeAsync` every `(itemKey, n)` of `Recipes.Take(...)`.

- [ ] **Step 1: Write the failing checks**
  - Pure: with `held = { col.champignon_paris: 5, col.enoki: 2, col.girolle (uncommon): 9 }`, `Have(AnyCommon("champignons"), held) == 7`; `Take(AnyCommon("champignons"), 6, held)` == `[("col.champignon_paris", 5), ("col.enoki", 1)]`; `Lacking` for a recipe `[(AnyCommon("champignons"), 8)]` == `["1 champignon commun"]`, and for 3 short `["3 champignons communs"]` (`Name(key, count)` owns singular/plural per set).
  - DB (named ingredients still work): a person with enough cailloux and every item of an existing recipe crafts it → `Done`, ingredients gone, cosmetic held = 1; again → `AlreadyOwned`; with one ingredient short → `MissingIngredients`, nothing taken.
  - The any-common DB case is written in Task 5 (it needs the real Lisière Ouvrage).
- [ ] **Step 2: Run checks** — Expected: build fails (`Recipes` missing).
- [ ] **Step 3: Implement `Recipes`, rewire `CraftOnceAsync`, `Missing`, `RecipeStatus`.**
- [ ] **Step 4: Run build + checks** — Expected: `ALL CHECKS PASSED` (section 59 included).

---

### Task 5: The recipe catalog — 8 Ouvrages, 2 new trophies, retuned trophies, the band check

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs` (record field, builders, new entries, retuned recipes)
- Test: `tests/ProjectSYNCS.Checks/CraftingChecks.cs`
- Icons: none required (Unicode fallback); note new keys in `docs/agents/economy.md` *Item icons* if that file lists keys.

**Interfaces:**
- Consumes: `Recipes`, `ForagePlaces.Draw`, `ForagePlaces.Of`.
- Produces:
  - `CosmeticInfo` gains a last positional parameter `bool Trophy = false`. The 8 existing crafted cosmetics and the 2 new trophies set `Trophy: true`; the 8 Ouvrages leave it false.
  - The `Theme/Title/Accessory/Grave` local builders take `bool trophy = false`.
  - 10 new keys, append-only, under existing slots (2 Ouvrages per slot; 1 trophy for les Mouilles; 1 trophy for the thinnest slot). La Lisière's Ouvrage(s) use `Recipes.AnyCommon("champignons")`. Every place appears in at least one Ouvrage. Names collide with no existing cosmetic or collectible name.
  - `CraftingChecks.ExpectedForages(CosmeticInfo recipe, int runs = 300, int seed = 61) : double` — mean forages over `runs` simulations: start holding nothing; at each forage, aim at the place whose items cover the most still-missing units (ties: `ForagePlaces.All` order); add the find; stop when `Recipes.ItemsReady`. Instant: day 15 of the middle month of the season every seasonal ingredient shares (12:00 UTC); no seasonal ingredient → 15 April.

- [ ] **Step 1: Write the failing checks**
  - Counts: 18 crafted cosmetics, 8 with `Trophy == false`, 10 with `true`; 2 Ouvrages per slot.
  - Every place has an Ouvrage whose items are all at that place; every Ouvrage's items share one place; every recipe ingredient is a collectible or any-common key; an Ouvrage's named items are `Common` except at most one `Uncommon`.
  - Every Lisière Ouvrage contains an any-common-mushroom ingredient.
  - Bands: `ExpectedForages` within 10–25 for Ouvrages, 60–150 for trophies; on failure print the recipe key and the number.
  - Prices: Ouvrages 80–120, trophies 200–300.
  - DB, any-common: a person holding `{ col.champignon_paris: 5, col.enoki: 4, col.shimeji: 1 }` and enough cailloux crafts the Lisière Ouvrage (8 any-common mushrooms, plus whatever else its recipe needs, given) → `Done`; left: `champignon_paris` 0, `enoki` 1, `shimeji` 1 (the `Take` rule below). With 7 commons → `MissingIngredients`, nothing taken.
- [ ] **Step 2: Run checks** — Expected: FAIL (counts, bands for today's 8 recipes).
- [ ] **Step 3: Add the 10 cosmetics and retune the 8 recipes until the bands pass** (Collier de coquillages stays summer-only; Plume d'or keeps its Plume dorée).
- [ ] **Step 4: Run build + checks** — Expected: `ALL CHECKS PASSED`; section 59 still passes (the craft select now has 18 options).

---

### Task 6: « Il a tout pour fabriquer… » after a forage

**Files:**
- Modify: `ProjectSYNCS/Helpers/Economy/Recipes.cs`
- Modify: `ProjectSYNCS/Services/Plynling/PlynlingService.Life.cs` (`ForageOnceAsync`)
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.cs` (`ForageReply`)
- Test: `tests/ProjectSYNCS.Checks/CraftingChecks.cs`

**Interfaces:**
- Produces: `Recipes.NowReady(IReadOnlyDictionary<string, int> before, string foundKey) : IReadOnlyList<CosmeticInfo>` — crafted cosmetics not owned in `before` (`before[cos.key] == 0`), not `ItemsReady` with `before`, `ItemsReady` with `before` + 1 × `foundKey`; catalog order.
- `ForageOnceAsync` reads the owner's held items before the grant (one query over `InventoryItems` for that guild+user, `Quantity > 0`), sets `ForageResult.NowReady`.
- `ForageReply` appends one line when `NowReady` is non-empty: « {Il|Elle} a tout pour fabriquer *{Label}* ! » + « (et N autres) » when more, then « -# `/inventory cosmetics` → Fabrication ».

- [ ] **Step 1: Write the failing checks** — holding all but one unit of an Ouvrage, finding it → that recipe; finding it again when already ready → empty; when the cosmetic is owned → empty; finding an unrelated item → empty; an any-common recipe one mushroom short + a common mushroom found → that recipe; + an uncommon mushroom → empty.
- [ ] **Step 2: Run checks** — Expected: build fails (`NowReady` missing).
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run build + checks** — Expected: `ALL CHECKS PASSED`.

---

### Task 7: Where to find it — the Fabrication tab and the collection book

**Files:**
- Modify: `ProjectSYNCS/Commands/Economy/CosmeticCards.cs:191-232` (`CraftBody`)
- Modify: `ProjectSYNCS/Commands/Economy/InventoryModule.cs:276-310` (`BuildSetPage`)
- Test: `tests/ProjectSYNCS.Checks/CosmeticScreenChecks.cs`, `tests/ProjectSYNCS.Checks/CraftingChecks.cs`

**Interfaces:**
- Consumes: `Recipes.Have/Name/Emoji/Items/Lacking`, `ForagePlaces.Of/At`, `CosmeticInfo.Trophy`, `ItemCatalog.RarityLabel/SeasonLabel`.
- Produces:
  - `public static string Whereabouts(ItemInfo item)` in `ForagePlaces` — « rare, à la Sommeille, l'été » (rarity, places joined « ou », season when not `None`); for an any-common key `Recipes.Where(key)` « à la Lisière ».
  - Craft list text: « **Ouvrages** » then its recipes, « **Chefs-d'œuvre** » then its recipes, each line ending with `RecipeStatus` (✅ when ready). Select options in the same order.
  - Recipe view checklist line: `{✅|❌} {emoji} {name} {min(have,need)}/{need} — {whereabouts}`.
  - Book: `BuildSetPage` description gains « -# Se trouve surtout {At(place)} » for the set's most common place, plus « ; {items} {At(other)} » for items placed elsewhere, kept short.

- [ ] **Step 1: Write the failing checks** — `Whereabouts(coquillage)` == « rare, à la Sommeille, l'été » (builder may adjust the season wording; the check pins rarity + place + season presence); section 59 `Caps` still within limits with 18 recipes for every recipe picked (it already iterates picks); the book page for each set builds within embed caps (description ≤ 4 096, fields ≤ 1 024) with every item held.
- [ ] **Step 2: Run checks** — Expected: FAIL / build error.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run build + checks** — Expected: `ALL CHECKS PASSED`; note the worst view's component count from the checks' output.

---

### Task 8: Docs, help, version

**Files:**
- Modify: `ProjectSYNCS/Commands/Plynling/PlynlingModule/PlynlingModule.Help.cs` (forage line: places; trim elsewhere on page 0 to stay ≤ 6 000)
- Modify: `ProjectSYNCS/Commands/Core/HelpModule.cs` (forage / crafting lines)
- Modify: `README.md`, `docs/agents/economy.md` (*Foraging places*, recipes, tiers, bands, any-common, `NowReady`), `docs/agents/plynling.md` (forage, typed shortcuts)
- Modify: `ProjectSYNCS/config.yaml` → `8.4.0`; `ProjectSYNCS/CHANGELOG.md` new `## 8.4.0 — <date>` entry (English bullets: forage places, typed place, 10 new crafted cosmetics and retuned recipes, any-common ingredients, where-to-find hints, « il a tout » line)

- [ ] **Step 1: Edit help texts; run checks** — Expected: help page measurements printed by the checks ≤ 6 000 for every page; `ALL CHECKS PASSED`.
- [ ] **Step 2: Edit docs, version, changelog.**
- [ ] **Step 3: Final build + checks** — Expected: 0 warnings, `ALL CHECKS PASSED`.
- [ ] **Step 4: Dev-guild walk-through (owner or session):** `/plynling forage place:` each place; `;forage riviere`; `;forage blabla` (refusal, no cooldown); `;forage` (a place named); `/inventory cosmetics` → Fabrication: two groups, ✅, whereabouts lines; craft the Lisière Ouvrage; `/inventory collection` set page hint.
