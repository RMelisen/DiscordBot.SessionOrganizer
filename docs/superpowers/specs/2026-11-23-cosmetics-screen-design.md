# Cosmetics rework — piece A: one cosmetics screen with a live try-on

## Context

The owner wants cosmetics, the wardrobe and crafting to be **more user-friendly and more impactful**.
Why (owner's answers): players have cailloux but **nobody uses cosmetics**; the screens are **clunky**
(three commands in two groups, no preview, nothing offers to wear what you just bought, recipes say
only « Il manque quelque chose »); what a Plynling wears is **nearly invisible**; crafting is **thin**.

The rework is four pieces, designed and shipped one at a time, in the owner's order:

- **A — one cosmetics screen** (this plan)
- **D — richer crafting** (more recipes, where to find ingredients)
- **B — what it wears seen everywhere** (visits, events, play, journal, her lines)
- **C — accessories drawn on the sprite** (art approach still to discuss with the owner, in plain words)

Owner decisions so far: prices stay (players have cailloux; the payoff is the problem);
`/inventory cosmetics` becomes the one screen, `/plynling wardrobe` opens it on the wardrobe tab,
`/inventory craft` is removed; the screen is built in the Plynling card's own format (Components V2),
not as an embed.

**Built on 8.2.7.**
A cosmetic purchase keeps its ephemeral receipt (`ShopCards.Receipt` around `CosmeticCards.Bought`).

## What the player sees

One ephemeral Components V2 message, redrawn in place on every click:

```
┌ (accent = thème worn or tried) ──────────────┐
│ 🌌 · 🌌 · 🌌 · 🌌 · 🌌      (thème banner)      │
│ ## Pépin                          [sprite]  │  ← the card's own heading + animated sprite
│ *« Le Rêveur »* … Porte 🎩 un haut-de-forme   │
│ -# Essai : Haut-de-forme — pas encore à toi  │  ← only while trying something not worn
├──────────────────────────────────────────────┤
│ [Garde-robe] [Boutique] [Fabrication]         │  ← selected tab blurple, others grey
│ (tab body: list text + one select)            │
│ (item view: price/recipe + action buttons)    │
│ -# notice of what just happened               │
└──────────────────────────────────────────────┘
```

- **Garde-robe tab** (default for `/plynling wardrobe`): four slot buttons (Thème · Titre ·
  Accessoire · Cadre), then one select of what the person owns in that slot, led by « Aucun ».
  **A pick here wears it at once** (wearing is free and reversible), as today. The cadre slot shows
  the grave line as it would look (`PlynlingCardUi.GraveLine`'s frame) under the preview.
- **Boutique tab** (default for `/inventory cosmetics`): the week's shop as today (basics, rotation,
  season; « nouvelle sélection <t:…:R> », balance). One select of every shop item, owned ones marked
  « déjà à toi ». **A pick tries it on** (preview updates, nothing bought) and opens the item view:
  slot, rarity/source, price, and buttons « Acheter » and « Acheter et porter » (green), or « Porter »
  if already owned, plus « Retour » (grey). Unaffordable → buttons disabled with the shortfall shown.
- **Fabrication tab**: the 8 recipes. A pick tries it on and shows the recipe as a checklist —
  each ingredient `have/need ✅/❌`, cailloux likewise, and a line **naming exactly what is missing**
  (« Il te manque : 2 × Améthyste, 1 × Opale »). Buttons « Fabriquer » / « Fabriquer et porter »
  (green, disabled until ready), or « Porter » if owned.
- **No living Plynling**: shop and crafting work; the preview block is replaced by one line
  (« Adopte un Plynling pour essayer tes cosmétiques. »), « …et porter » / « Porter » are hidden, and
  the Garde-robe tab lists what is owned without selects.
- After a purchase: the card redraws on the item view (now owned, « Porter » offered, or already worn
  for « Acheter et porter ») **and** the existing ephemeral receipt follows (`ShopCards.Receipt`);
  its footer no longer says « À porter depuis `/plynling wardrobe` » — the button is right there.
  Craft keeps an on-card notice (not a purchase).

## How it is built

**Shared rendering, no look-alike.** Introduce an `Outfit` value (new, `Helpers/Economy/`, next to
`CosmeticSlots`): the four worn `CosmeticInfo?`, built by `Outfit.Of(Plynling)` and changed by
`outfit.With(CosmeticInfo)` / `outfit.Without(slot)`. `PlynlingCardUi.Heading` and `GraveLine` take
an optional `Outfit` (default `Outfit.Of(p)`), so try-on never mutates the tracked EF entity. Extract
the card's head — accent colour, banner `TextDisplay`, section with heading + sprite thumbnail — from
`PlynlingModule.BuildCard` (`Commands/Plynling/PlynlingModule/PlynlingModule.Rendering.cs:114-139`)
into one static builder both the card and the cosmetics screen call. Piece B will reuse `Outfit`.

**Files**
- `Commands/Economy/CosmeticCards.cs` — rewritten: one static `Build(CosmeticScreen state, …)`
  returning a `MessageComponent` (V2), Context-free so the checks can measure it. `Bought` and
  `Notice` stay (wording updated).
- `Interactions/Components/CosmeticComponentHandler.cs` — rewritten for the new verbs; every redraw
  re-asserts `MessageFlags.ComponentsV2`, sends `AllowedMentions.None`, clears `Embed`. Handlers that
  touch the DB `DeferAsync` first when they can't answer at once (3 s rule) — the update path is
  `DeferAsync` + `ModifyOriginalResponseAsync`.
- `Services/Economy/CosmeticService.cs` — `BuyAsync` / `CraftAsync` gain an optional `wear` flag that
  puts the item on the living Plynling **in the same `EconomySave` unit** (no second save). Existing
  checks (week, already owned, ingredients, balance) unchanged.
- `Commands/Economy/InventoryModule.cs` — `/inventory cosmetics` opens the screen on Boutique;
  `/inventory craft` removed (frees a slot: `/inventory` 8 → 7). Update the « Pour habiller… » hint
  (`:386-387`) to point at `/inventory cosmetics`.
- `Commands/Plynling/PlynlingModule/PlynlingModule.cs:420` — `/plynling wardrobe` opens the same screen
  on Garde-robe (count stays 15).
- `Helpers/Plynling/PlynlingCardUi.cs`, `PlynlingModule.Rendering.cs` — `Outfit` parameter, shared head.

**Custom-ids** — a **new prefix `cosm:`**, so no new handler's wildcard can match an old screen's
`cos:buy:*` / `cos:wear:*` id (old open ephemerals simply stop answering; the old handlers are
deleted). One verb per control so no id repeats on a message; state carried in the id, so it
survives a restart; every id < 100 chars:
`cosm:tab:{tab}`, `cosm:slot:{slot}` (wardrobe slot buttons), `cosm:pick:{tab}:{week}` (the one
select — shop picks carry the week; on the wardrobe tab « Aucun »'s value names the slot, e.g.
`none:Theme`), `cosm:buy:{week}:{key}`, `cosm:buyw:{week}:{key}`, `cosm:make:{key}`,
`cosm:makew:{key}`, `cosm:wear:{tab}:{key}`, `cosm:back:{tab}`. Shop actions keep refusing a week other
than the current one (`NotInShop`).

**Edge cases (Claude's calls, from the builder check)**
- Cadre preview on a living Plynling: `PlynlingCardUi.GraveLine` reads `p.DiedAt!.Value` and would
  throw — add a frame-only preview line built from the same `Outfit` instead of calling `GraveLine`.
- `/plynling wardrobe` with no living Plynling opens the screen (no-Plynling Garde-robe view) instead
  of today's refusal.
- « …et porter » when the Plynling died after the screen was drawn: the purchase/craft stands,
  nothing is worn, and the notice says so.

**Button colours** (CLAUDE.md theme): selected tab / slot blurple, others grey; Acheter, Acheter et
porter, Fabriquer, Fabriquer et porter green; Porter blurple; Retour grey.

**Caps.** Selects ≤ 25 options (shop ≤ 16; a slot ≤ 18 with « Aucun »; recipes 8). The busiest view
is ~20 components of the 40; measure it.

## Docs, version, changelog

- `docs/agents/economy.md` *Cosmetics*: rewrite the screens paragraph (one screen, tabs, try-on via
  `Outfit`, the verbs, `…et porter` in one save).
- `HelpModule.cs:176-177`, `PlynlingModule.Help.cs:117-121`, `README.md:582-585`: drop `/inventory craft`,
  describe the one screen and try-on. Measure `/help` and `/plynling help` (near the 6000 cap).
- **Minor bump** (a removed command + new behaviour): `config.yaml` 8.2.7 → **8.3.0** (check
  `git diff` first), `ProjectSYNCS/CHANGELOG.md` entry in English.
- Design spec copied to `docs/superpowers/specs/2026-11-23-cosmetics-screen-design.md`.

## Left to the builder

Exact French wording; whether the preview heading includes the partner / epithet lines (match what
the card shows if cheap); whether Retour returns to the tab list or the tab's last pick.

## Verification

1. `cd ProjectSYNCS && dotnet build` with no warnings (`-warnaserror` clean).
2. `dotnet run --project tests/ProjectSYNCS.Checks` — add a section that builds every tab and item
   view at its fullest (every cosmetic owned, longest names, no-Plynling variant) through the existing
   `CardCheck` (≤ 40 components, ≤ 4000 chars, no duplicate id), and measures `/help` + `/plynling help`.
3. Run against the dev guild: `/inventory cosmetics` → try on a rotating thème (card colour + banner
   change, nothing charged) → « Acheter et porter » → receipt arrives, card shows it worn →
   `/plynling view` shows it. Fabrication: a recipe with missing items names them; buttons disabled.
   `/plynling wardrobe` opens on Garde-robe; « Aucun » takes a slot off. With no Plynling: shop works,
   no wear buttons. Confirm `/inventory craft` is gone from the dev guild's command list.
