# Plynling cosmetics — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A cailloux sink: 60 cosmetics (Thème, Titre, Accessoire, Cadre de tombe) that decorate a
Plynling's card and grave, bought in a weekly-rotating shop or crafted from collectibles.

**Architecture:** Cosmetics are a third `ItemKind` held as ordinary `InventoryItem` rows; their
slot data lives in a static `CosmeticCatalog`. What a Plynling wears is four nullable key columns
on `Plynling`. A transient `CosmeticService` buys, crafts and dresses in one save each; the
weekly rotation is a pure function of the Paris ISO week.

**Tech Stack:** C# / .NET 10, Discord.Net 3.20 (Components V2 card, embeds + selects elsewhere),
EF Core SQLite.

Spec: `docs/superpowers/specs/2026-09-27-plynling-cosmetics-design.md`.

## Global Constraints

- Command and option names English; every user-facing string French (CLAUDE.md « Language »).
- Item keys are stored: `cos.<slot>.<name>`, append-only, never renamed.
- Every action and what it moves (cailloux, items, what is worn) lands in **one** `SaveChanges`;
  static inventory helpers never save.
- No `DateTime.Now`; the week is the Paris ISO week via `AppTime`.
- `[ComponentInteraction]` in a group module needs `ignoreGroupNames: true`; every select or
  button row gets its own custom-id verb.
- Plynling-specific text follows its gender (`GenderedLines` / `Agree`); titles have both forms.
- Discord caps: 25 select options, 40 components (the card), embed 6000 / field 1024.
- No test project: checks go in the scratch harness (`extrascheck`, in-memory SQLite), written
  before the code they check. Build with `dotnet build -warnaserror`.
- Commit after each task; push only when the owner asks.

---

### Task 1: The catalog and the weekly rotation

**Files:**
- Create: `ProjectSYNCS/Helpers/CosmeticCatalog.cs`
- Modify: `ProjectSYNCS/Helpers/ItemCatalog.cs` (`ItemKind.Cosmetic`; `All` includes cosmetics)

**Interfaces — Produces:**
- `enum CosmeticSlot { Theme, Title, Accessory, Grave }`
- `enum CosmeticSource { Basic, Rotating, Seasonal, Crafted }`
- `record CosmeticInfo(string Key, CosmeticSlot Slot, CosmeticSource Source, ItemRarity Rarity,
  Season Season, string Emoji, string Name, string? NameF, uint? Accent, string? Banner,
  string? GraveLeft, string? GraveRight, long Price, IReadOnlyList<(string ItemKey, int Count)> Recipe)`
  — `Name` is the masculine title, the accessory with its article (« une écharpe »), or the
  theme/frame name; `NameF` only for titles; `Price` is the shop price, or the crafting cailloux.
- `CosmeticCatalog.All`, `ByKey(key)`, `InSlot(slot)`
- `CosmeticCatalog.Label(CosmeticInfo)` — the inventory name: « Thème Aurore »,
  « Titre « Le Gourmet / La Gourmande » », « Écharpe », « Cadre Bougies »
- `CosmeticCatalog.TitleFor(CosmeticInfo, PlynlingGender)`
- `CosmeticCatalog.WeekKey(DateTimeOffset now)` → `int` `yyyyww` (Paris ISO week)
- `CosmeticCatalog.Shop(DateTimeOffset now)` → `IReadOnlyList<CosmeticInfo>`: the 4 basics, then
  2 rotating per slot drawn with `new Random(weekKey * 10 + (int)slot)`, then the season's items
- Prices (spec): Basic 300 (titre, accessoire) / 500 (thème, cadre); Rotating 600 / 1 200 /
  2 400 / 5 000 by rarity; Seasonal 2 400.
- `ItemCatalog.All` gains one `ItemInfo(Key, ItemKind.Cosmetic, Emoji, Label, Set: null, Rarity,
  Season, Food: null)` per cosmetic, so give/trade/autocomplete see them.

- [ ] **Step 1: Write the checks** — 60 cosmetics; 17/13/17/13 per slot; 10 rotating per slot
  with 4/3/2/1 rarities; keys unique and `cos.`-prefixed; every title has `NameF`; every recipe
  ingredient is an existing collectible; every Thème has an accent and a banner, every Cadre both
  sides; `Shop` is 4 basics + 8 rotating (2 distinct per slot) + 2 seasonal, identical for two
  instants in the same week, different across at least one of 10 consecutive weeks, and changes
  at Monday 00:00 Paris (check 2026-10-25/26, across the DST change); seasonal items only in
  their season; cosmetics are not `Collectibles` (the book still counts 62).
- [ ] **Step 2: Run the harness** — expect compile failures on the missing types.
- [ ] **Step 3: Implement** `CosmeticCatalog` with the spec's 60 entries and the rotation.
- [ ] **Step 4: Run the harness and build** — all pass, 0 errors.
- [ ] **Step 5: Commit** — « Added the cosmetics catalog and the weekly shop rotation ».

### Task 2: Wearing, buying and crafting

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling.cs` (`ThemeKey`, `TitleKey`, `AccessoryKey`, `GraveKey`,
  all `string?`)
- Create: migration `AddPlynlingCosmetics` (`dotnet ef migrations add AddPlynlingCosmetics`)
- Create: `ProjectSYNCS/Services/CosmeticService.cs` (transient, registered in `Program.cs`)
- Modify: `ProjectSYNCS/Services/InventoryService.cs` (`TakeAsync` takes off a cosmetic that
  reaches 0; `SellAsync` refuses cosmetics with `GiveOutcome.NotSellable`)

**Interfaces — Consumes:** Task 1's catalog. **Produces:**
- `CosmeticSlots.Get(Plynling, CosmeticSlot)` / `Set(Plynling, CosmeticSlot, string?)`
- `enum CosmeticOutcome { Done, UnknownItem, AlreadyOwned, TooPoor, NotInShop, MissingIngredients,
  NotOwned, NoPlynling }`
- `CosmeticService.BuyAsync(ulong guildId, ulong userId, string key, int week, DateTimeOffset now)`
  → `(CosmeticOutcome, long Balance)` — refuses unless `week == WeekKey(now)` and the item is in
  `Shop(now)`
- `CosmeticService.CraftAsync(ulong guildId, ulong userId, string key, DateTimeOffset now)`
  → `(CosmeticOutcome, long Balance)` — all ingredients and cailloux, or nothing
- `CosmeticService.WearAsync(ulong guildId, ulong userId, CosmeticSlot slot, string? key,
  DateTimeOffset now)` → `(CosmeticOutcome, Plynling?)` — `key` null takes the slot off; needs
  the person's living Plynling and at least 1 of the item
- `CosmeticService.OwnedAsync(ulong guildId, ulong userId)` → `List<CosmeticInfo>` (quantity ≥ 1)

- [ ] **Step 1: Write the checks** (in-memory DB): buying pays and grants in one save; refused
  when owned, too poor (nothing moves), not in this week's shop, or from last week's key;
  crafting takes every ingredient and the cailloux, or nothing when one is short; wearing needs
  ownership and a living Plynling, and clears with null; giving a worn cosmetic away takes it off
  the giver's living Plynling in the same save, while a dead Plynling keeps its keys; trading
  likewise; selling a cosmetic is refused and moves nothing.
- [ ] **Step 2: Run the harness** — expect failures.
- [ ] **Step 3: Implement** the columns, the migration, `CosmeticService`, and the two
  `InventoryService` changes.
- [ ] **Step 4: Run the harness and build.**
- [ ] **Step 5: Commit** — « Added wearing, buying and crafting cosmetics ».

### Task 3: On screen — card, grave, inventory

**Files:**
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`BuildCard`: theme accent + banner line)
- Modify: `ProjectSYNCS/Helpers/PlynlingCardUi.cs` (`Heading` adds the title and « porte … »;
  `GraveLine` wraps in the frame)
- Modify: `ProjectSYNCS/Commands/InventoryModule.cs` (`BuildInventoryEmbed`: « Garde-robe » field)

**Interfaces — Consumes:** Tasks 1–2.

- [ ] **Step 1: Write the checks:** a themed card uses the theme's accent and starts with its
  banner, an unthemed one is unchanged; the title follows the Plynling's gender; « porte 🎀 un
  nœud » appears; a card wearing all four stays well under 40 components; a grave line is wrapped
  in its frame; the inventory lists owned cosmetics and still fits 6000 / 1024 when someone owns
  all 60.
- [ ] **Step 2: Run the harness** — expect failures.
- [ ] **Step 3: Implement.**
- [ ] **Step 4: Run the harness and build.**
- [ ] **Step 5: Commit** — « Showed cosmetics on the card, the grave and the inventory ».

### Task 4: The commands

**Files:**
- Modify: `ProjectSYNCS/Commands/InventoryModule.cs` (`cosmetics`, `craft`; static
  `BuildShopEmbed`, `BuildCraftEmbed`)
- Modify: `ProjectSYNCS/Commands/PlynlingModule.cs` (`wardrobe`; static `BuildWardrobe`)
- Create: `ProjectSYNCS/Interactions/Components/CosmeticComponentHandler.cs` —
  `cos:buy:{week}` (select), `cos:craft` (select), `cos:wear:{slot}` (four selects)

**Interfaces — Consumes:** Tasks 1–3.

- [ ] **Step 1: Write the checks:** the shop lists « Toujours » and « Cette semaine », marks
  owned items, and its select carries the week; the recipes show « n/m ✅/❌ » per ingredient;
  the wardrobe has 4 selects with distinct custom-ids, each ≤ 25 options with « Aucun » and the
  worn item as default; `/inventory` holds 8 subcommands and `/plynling` 14; every builder fits
  the embed caps.
- [ ] **Step 2: Run the harness** — expect failures.
- [ ] **Step 3: Implement** the three commands (all ephemeral) and the handler; buying and
  crafting answer with an ephemeral line; a wardrobe pick redraws the wardrobe message.
- [ ] **Step 4: Run the harness and build.**
- [ ] **Step 5: Commit** — « Added the cosmetics shop, crafting and the wardrobe ».

### Task 5: Docs and version

**Files:** `ProjectSYNCS/Commands/PlynlingModule.cs` (`/plynling help`: a « Cosmétiques » field;
re-check 6000 / 1024), `README.md`, `CLAUDE.md` (cosmetics note: items, worn keys, taken off on
loss, not sellable, the rotation, custom-ids), `ProjectSYNCS/config.yaml` (version).

- [ ] **Step 1:** Update the three docs; run the harness (help caps) and build.
- [ ] **Step 2:** Commit the docs; bump the version as its own « Updated version » commit;
  push only if the owner asks.
