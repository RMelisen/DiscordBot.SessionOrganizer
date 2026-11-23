# Cosmetics rework — piece D: richer crafting and forage places

Piece D of the cosmetics rework (A: one cosmetics screen, shipped in 8.3.0; D: this; then B: what
a Plynling wears seen everywhere; C: accessories drawn on the sprite). Read
`2026-11-23-cosmetics-screen-design.md` for piece A, and `docs/agents/economy.md` (*Cosmetics*,
*Tiers and turn-ins*, the collection book) before building.

## Intent and why

The owner wants cosmetics, the wardrobe and crafting to be more user-friendly and more impactful.
For crafting the complaint is **"too thin"**, and the code shows why:

- 8 recipes, 2 per slot, each a handful of collectibles plus 200–300 cailloux.
- **No ingredient can be aimed for.** Forage, the daily gift, games, visits and packs all draw at
  random from ~70 collectibles (`ItemCatalog.DrawCollectible` / `DrawForage`). Only trading targets one.
- So most recipes are out of reach: Champignonnière ≈ 200 forages (two months of diligent foraging);
  Collier de coquillages needs 3 Coquillages, a summer-only rare at ≈ 1 forage in 250 — impossible
  in one summer; Plume d'or needs a légendaire.
- Nothing says where an item is found, in which season, or how rare it is.
- Meanwhile common duplicates pile up, worth 2 cailloux each when sold.

## What the owner decided

- **Two tiers** of recipes: easy ones fed by duplicates, plus trophies a player can aim for.
- **Aiming is by forage place**: `/plynling forage` takes an optional place from the village map
  (`docs/village-lore.md` § 5), each place leaning toward its own items. The owner likes that a
  future collection can be added by saying where its items are found, without heavier machinery.
- **The typed shortcut takes a place too** (`;forage <place>`, and `;pl forage <place>`).
- **No place named → a random place.** (How that random place is chosen is below: Claude's call.)
- **A recipe may ask for « any commons of a set »** (« 8 champignons communs, au choix »): the
  owner's answer to La Lisière's easy recipe (see *Recipes*).
- Accepted as played back: the places table, the tilt, the reply naming the place, ingredient hints,
  the 8 easy + 8 retuned + 2 new trophies, the check that measures every recipe, the two groups in
  the Fabrication tab, and the « il a tout pour fabriquer… » line.

## Goals

1. Every recipe is reachable by a player who aims for it, in a time the checks enforce.
2. A player always knows where to look for an ingredient (place, rarity, season).
3. Duplicates have a regular use: an easy recipe about once a week for an active forager.
4. Foraging gains a small choice and a sense of place in Humeval.
5. Adding a collection later means listing where its items are found — nothing else.

## Non-goals

- No market stall / item exchange (considered, set aside; may come later if duplicates still pile up).
- No pinned recipe or hidden weighting toward a recipe.
- No change to the gift, games, visits or packs: they stay random from everywhere.
- No change to prices of shop cosmetics, the forage cooldown (4 h), rarity odds, or the food share.
- No new place-specific side effects (mud, dirt by place, events by place).
- Le Bois Sourd is not a place: the lore keeps it for expeditions.
- No sprite art (that is piece C).

## Anti-goals (would make it a failure even if it works)

- A recipe that silently drifts out of reach again.
- Foraging without a place giving fewer mushrooms than today, shrinking the Champignons set's
  (1 000-caillou) economy by accident.
- Crafting removing items from the collection book. (It cannot: the book counts *discovered*
  items via `InventoryItem.DiscoveredAt`, and crafting only lowers `Quantity` — keep it that way.)
- `/plynling help` breaking its 6 000-character cap (page 0 is at 5 874 today).

## Approaches considered

1. **Forage places** — chosen. Luck stays, but the player steers it, and "where to find it" becomes
   something they do. Cost: one option, a place table, per-place lines.
2. **Market stall** (swap 5 commons for 1 chosen uncommon of the set, etc.) — no luck at all and it
   eats duplicates, but it trivialises the collection book and its 100–1 000-caillou set rewards;
   rates would need careful tuning. Set aside.
3. **Pinned recipe** (finds lean toward a pinned recipe's missing items) — no new command, but the
   player can't see it work, so it still feels like a lottery. Rejected.

Tiers vs only-easy vs only-trophies: the owner picked tiers (easy fixes "nobody crafts" at once;
aiming fixes the trophies).

## Design

### Places

Five places, each a stable string key (code) and a French name (display):

| Place | Leans toward |
|---|---|
| **La Sommeille** (the river, by the Pont-Vieux) | Cailloux set; Coquillage |
| **La Lisière** (the forest edge) | Champignons set; Châtaigne |
| **Les Mouilles** (the marsh) | Insectes set |
| **Le Communal** (pasture and common wood) | Nature set; Primevère, Coquelicot |
| **La place du Puits** (the square, its market) | Trésors set |

- **The place belongs to the item, not the set.** Each collectible's catalog entry names the place(s)
  it is found at (one or more). Seasonal items are spread where they fit; the table above is the
  intent, the builder places the remaining ones (Cerise, Citrouille, Flocon, Cristal de givre) and
  every one must have at least one place. A check fails on any collectible with no place.
- A future collection lists its items' places — an existing place or a new one — and nothing else
  changes.
- Places are not stored in the database (no migration), so their keys need not be append-only,
  but the typed aliases are a contract with players: don't rename them lightly.

### Foraging at a place

- **A tilt, not a guarantee.** When a place is chosen and the find is a collectible (the 20 % food
  chance is unchanged), about **3 in 4** collectible finds are drawn from that place's items, the
  rest from anywhere (the current draw). Inside the place, the rarity roll is today's
  (60 / 28 / 10 / 2 %), with today's fallback to commun when a rarity has nothing findable there
  now (out of season). The ¾ figure is a constant the builder may tune if the recipe-time check
  wants it.
- **No place named → exactly today's draw, then a place is named for the story.** The item is drawn
  by `DrawForage` as now, and the reply names one of that item's places (a random one if several;
  any place for food). The player sees a random place; the economy does not move — in particular
  the 60 % mushroom share of a placeless forage stays. (Claude's call: the owner asked for "random
  place"; this gives it without changing today's balance.)
- Same cooldown, refusals, dirt, `act.forage` metric and save as today
  (`PlynlingService.ForageOnceAsync`). The draw stays inside the unit of work.
- Economy check made while designing: aiming speeds up collection turn-ins only slightly, because
  each set needs its légendaire (≈ 1 in 80 aimed forages); order of 2–3 cailloux per forage, small
  against `/work`. The builder confirms with the checks (below).

### How players name a place

- **Slash**: `/plynling forage place:` — optional, a fixed choice list with the French names.
  Option name `place` (English, per the language rule). `/plynling` keeps 15 subcommands.
- **Typed**: `;forage <place>` and `;pl forage <place>` (`Helpers/Plynling/PlynlingShortcut`, pure,
  checked). Forgiving match: case- and accent-insensitive, the leading article optional, and a few
  aliases per place, at least: Sommeille → `sommeille`, `riviere`, `pont`; Lisière → `lisiere`,
  `foret`, `bois`; Mouilles → `mouilles`, `marais`, `etang`; Communal → `communal`, `pre`, `pature`;
  place du Puits → `puits`, `place`, `marche`, `village`. More may be added; no alias may match two
  places (a check enforces it).
- `;forage` alone forages with no place (as above).
- **An unknown word** gets a short refusal listing the places, sent like the shortcut's other
  refusals (a reply that deletes itself). It does not forage and does not start the cooldown.

### The forage reply

- Says where the Plynling went, with **new lines per place** (several per place, picked through
  `ResponsePicker`; food and collectible variants; gendered with `Agree`). Written to
  `docs/plynling-writing-style.md` and true to `docs/village-lore.md`. Example of the register only:
  « **Pépin** revient de la Sommeille, les pieds mouillés, avec 🐚 un Coquillage ! »
- The placeless forage uses the same lines (it has a place too).
- **The « il a tout » line**: when a find gives the owner the last missing ingredient of a recipe
  they don't own yet (items only; say nothing about cailloux), one more line under the find:
  « Il a tout pour fabriquer *un bocal à insectes* ! » pointing at `/inventory cosmetics`. At most
  one line (the first such recipe, or « …et N autres »). **Scope: forage only** in this piece
  (Claude's call: it is the aimed source; the gift/games/visits lines can adopt it later). It must
  not fire again for a recipe that was already ready before this find.

### Recipes

**A new kind of ingredient: « any commons of a set ».** Besides a named item (« 3 × Galet »), a
recipe ingredient may be **N commons of one set, any of them** (« 8 champignons communs, au
choix »). Owner's decision, from the builder check: La Lisière holds the 12 common mushrooms, so a
named mushroom comes up about once in 30 aimed forages and no named-mushroom Ouvrage fits its band;
« any common mushroom » comes up about 2 forages in 5 there (≈ 20 forages for 8), and it eats the
biggest duplicate pile.
- Only commons, only within one set; seasonal commons of that set count while held.
- **Crafting takes from the largest stacks first** (ties by catalog order), so the copies a
  turn-in needs (one of each, see *Tiers and turn-ins*) are the last to go. The book is unaffected
  (it counts discoveries).
- The recipe view shows it as one checklist line with its count (« 🍄 Champignons communs 5/8 ❌ —
  à la Lisière »); `CosmeticCards.Missing` names it (« 3 champignons communs »); the « il a tout »
  line and the ✅ count it like any ingredient.
- **La Lisière's Ouvrage(s) must use it.** Other Ouvrages may use it too, at the builder's choice,
  if they stay in the band. Trophies keep named items (they are aimed hunts).

**Easy tier — « Ouvrages » (8 new cosmetics, 2 per slot).**
- Each uses commons (at most one uncommon) whose places are **one place**, plus ~100 cailloux.
- Every place gets at least one.
- **Each is made once per person**, like every recipe today (`CosmeticService` refuses
  `AlreadyOwned`). Claude's call: a second copy would only be for giving, and repeatable crafts would
  rework the ownership check, the ✅ and the buttons. So the easy tier is a run of 8 to make
  (≈ two months for an active forager), refilled by each future collection's Ouvrages.
- Each is a new `CosmeticSource.Crafted` cosmetic with new append-only keys (`cos.<slot>.<name>`),
  so the catalog goes from 60 to 70 (with the 2 new trophies). Names, emoji, accents and exact
  recipes are the builder's, within the band below. Ideas from the conversation: a « Rivière »
  thème (Galets, Cailloux plats, a Quartz), a « Glaneur / Glaneuse » titre (Nature), « un bocal à
  insectes » (Mouilles), a « Lucioles » cadre. Avoid names already used by a shop cosmetic or a
  collectible (see the Plume d'or comment in `CosmeticCatalog`).

**Trophy tier — « Chefs-d'œuvre » (the 8 existing + 2 new).**
- The 8 existing recipes are **retuned** (recipes are not stored; their keys are, and stay). Each
  aims at one or two places.
- Collier de coquillages stays summer-only but needs fewer Coquillages, so one summer is enough.
- Plume d'or keeps its légendaire Plume dorée as the prize (≈ a month aiming at le Communal).
- 2 new trophies: one for Les Mouilles (it has none today), one for whichever slot ends up thinnest.
- The two mushroom trophies (Champignonnière, Cercle de fées) suffer the same dilution as the
  Lisière Ouvrage: retune them to fewer copies of each named mushroom (or uncommons/rares, whose
  pools are smaller) until they sit in the band.
- Trophies keep 200–300 cailloux.

**The bands, enforced by a check.** For each recipe, the checks compute the expected number of
forages to gather its items when aiming at the best place(s) for them, from a player holding
nothing, using the real draw code (simulated with a seeded `Random`, or computed exactly — the
builder's choice), in the season(s) where it can be made:
- Ouvrages: **≈ 10–25** forages.
- Chefs-d'œuvre: **≈ 60–150** forages.
A recipe outside its band fails the checks. A recipe whose items span two places is measured with
the player splitting forages sensibly (the builder defines it; e.g. alternating, or the best fixed
split).

Recipe ingredients are collectibles only (no foods, no cosmetics), named or « any commons of a set ».

### The Fabrication tab (piece A's screen, `CosmeticCards`)

- The recipe list shows two groups, « Ouvrages » then « Chefs-d'œuvre », a ✅ on each recipe the
  player can make now.
- The recipe select (≤ 25 options; 18 recipes) follows the same order.
- The recipe view's checklist gives each ingredient its **rarity, place(s) and season** when it has
  one, e.g. « 🐚 Coquillage 1/2 ❌ — rare, à la Sommeille, l'été ». `CosmeticCards.Missing` still
  names what is missing.
- Same caps as piece A: ≤ 40 components, ≤ 4 000 characters; the checks' section 59 must keep
  passing with 18 recipes and the longer lines.

### The collection book (`/inventory collection`)

Each set page gets one hint line: where its items are found mostly (« -# Se trouve surtout : la
Sommeille »), plus the places of any of its items found elsewhere if short. It must fit the page's
existing embed caps (a field ≤ 1 024; rarity-split pages for Champignons).

## Constraints (from the project)

- `CLAUDE.md` rules: French user text, English code; `ignoreGroupNames`; defer-first handlers;
  V2 caps; item and cosmetic keys append-only; `ResponsePicker` for lines; typed shortcuts parsed
  in pure helpers and covered by the checks.
- **`/plynling help` page 0 is at 5 874 / 6 000.** Adding the place option there must be paid for
  by trimming elsewhere on that page; measure it in the checks. Same for `/help` (5 823).
- Docs: `docs/agents/economy.md` (forage draw, places, recipes, bands), `docs/agents/plynling.md`
  (forage, typed shortcuts), `/help`, `/plynling help`, `README.md`.
- Version: **minor bump** (new option and behaviour): 8.3.0 → **8.4.0**, with a `CHANGELOG.md`
  entry. Check `git diff` first: one bump per batch.

## Left to the builder

- Exact names, emoji, accents and recipes of the 10 new cosmetics, and the retuned recipes, within
  the bands.
- Where the place data lives (a new field on `ItemInfo`, or a map beside the catalog) and the place
  type's shape, as long as adding a collection means only listing its places.
- The exact tilt constant (≈ ¾) if the bands need it.
- The extra aliases, the refusal wording, the number of lines per place.
- Whether places get an emoji in replies and lists.
