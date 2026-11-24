# Second Accessory Slot (runtime drawing) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A Plynling wears up to two accessories, and nœud, fleur and held items pick their side. Every
dressed picture is drawn by the bot from a bare sprite and layers, and sent as an attachment.

**Architecture:** The art tool exports bare sprites, per-item layers, raw aura layers and mood-mark
layers into `ProjectSYNCS/Assets/Plynlings/`, which ships in the image. `PlynlingArt.SpriteOf` returns
the GitHub link for a bare Plynling. For a dressed one, it returns `attachment://<look name>`, where
the name encodes the whole look. Every place that sends such a card asks `PlynlingPainter.FilesFor(card)`
for the files and attaches them. Pure rules (`AccessoryFit`) decide clashes, sides and try-on placement.

**Tech Stack:** .NET 10, Discord.Net 3.20.1, EF Core + SQLite, **SixLabors.ImageSharp 3.1.12**, Python
+ Pillow (`tools/plynling-art`).

**Spec:** `docs/superpowers/specs/2026-11-23-second-accessory-slot-design.md`

## Global Constraints

- ImageSharp **3.1.12** exactly, never 4.x (no licence). Frame API: `WebpFrameMetadata.BlendMethod = WebpBlendMethod.Source`, `DisposalMethod = WebpDisposalMethod.RestoreToBackground`; encode lossless, `WebpEncodingMethod.Level0` (never Level6).
- Timeline of **125 ms ticks** (`motion.py` FRAME_MS): frame delays expand to ticks; identical consecutive ticks merge on encode.
- Every layer sits **at its foot** on the largest canvas in play (centred horizontally, bottom-aligned). Hat on an adult means the 320 px hat canvas; otherwise 256 px.
- Draw order, back to front: body → neck → mittens → glasses → hat → head ornaments → held items → mood marks (only when a right-hand item is worn) → aura last, **only where the composed pixel's alpha < 90, replacing it** (`auras.py` SHADOW_ALPHA).
- Auras only on happy, content, sad, hungry, starving, sleeping. Angry shows no accessory and no aura.
- Side defaults: nœud **gauche**, fleur **droite**, held items **gauche**. Style string: colour first, then position/side, joined by `_`.
- Any drawing failure logs and falls back to the bare sprite link. A card never fails to send because of drawing.
- Every edit of a picture-carrying card sets `Attachments` explicitly, even to empty, so a stale file never lingers.
- User-facing strings are French; code, comments and logs are English. Custom-ids: grep before adding; one verb per select row.
- Components V2 messages: 40 components at most, measured by checks section 59.
- Work on branch `feature/second-accessory-slot`. Task 1 runs on `spike/runtime-layering`. No push without the owner's say-so.
- Bot changes bump `ProjectSYNCS/config.yaml` once: **8.8.0 → 8.9.0** (minor), with a CHANGELOG entry (Task 10).

## Review Focus

1. **A dressed Plynling whose art is missing** (no base file, as for the sunflowers, or a missing layer): the card shows the bare link, logs once, and sends. Test in Task 4.
2. **Taking everything off on an open card**: the next edit clears the attachment, and the thumbnail is the link again. Covered by Task 1's Discord check and the `Attachments` rule in Tasks 8–9.
3. **Old stored styles** (`rose`, with no side part) and retired or unknown keys in either slot: they resolve to defaults or are ignored, never an exception. Tests in Tasks 5 and 6.
4. **An inconsistent row** (`Accessory2Key` set, `AccessoryKey` null): `Outfit.Of` moves it up. Test in Task 6.
5. **First draw latency on a button that doesn't defer** (care buttons, game moves): drawing happens inside the builder. The cache must make repeats free. Test in Task 4 (second draw of the same look is a cache hit).

---

### Task 1: Discord gate — attached animated WebP in a V2 thumbnail (on `spike/runtime-layering`)

**Files:**
- Modify: `tools/layering-spike/LayeringSpike.csproj` (ImageSharp back to 3.1.12)
- Modify: `tools/layering-spike/Program.cs` (3.x frame API, new `discord-interactive` mode)
- Modify: `tools/layering-spike/README.md` (results)

**Interfaces:** none (throwaway). The 3.x API calls are in commit `da70e8e4`.

- [ ] **Step 1:** `git switch spike/runtime-layering`. Set ImageSharp to 3.1.12 and restore the 3.x frame API (`BlendMethod`, `DisposalMethod`).
- [ ] **Step 2:** Run `dotnet run --project tools/layering-spike -c Release -- all`. Expected: `8820/8820 pixel-exact`.
- [ ] **Step 3: Add the mode `discord-interactive <guildId> <channelId>`.** It registers a guild command `/spike-art` and covers the three edit paths:
  - **(a) Public post, REST edit:** posts a public V2 card with thumbnail `attachment://a.webp` (a dressed picture). It then edits it via REST `ModifyAsync`: first text only, then swapping in `attachment://b.webp`, then back to a GitHub link with `Attachments` set to empty.
  - **(b) Button edit:** that public card carries a button. Its `UpdateAsync` swaps the attachment.
  - **(c) Ephemeral webhook edit:** `/spike-art` answers ephemeral and deferred. It then calls `ModifyOriginalResponseAsync` with an attachment, and a button on it edits through the interaction webhook again.
- [ ] **Step 4:** Ask the owner to set the token: `dotnet user-secrets set "Discord:Token" "<token>" --project tools/layering-spike`. Never handle the token yourself. Then run the mode on the dev guild.
- [ ] **Step 5: Owner checks on desktop and mobile:**
  - the picture animates in every path, after every edit;
  - a link-only edit with empty `Attachments` shows no leftover file;
  - an unreferenced attachment is never shown below the card.
- [ ] **Step 6:** Write the results in the README and commit on the spike branch. **If any path fails, STOP and report to the owner. Tasks 8–9 must not start.** Tasks 2–7 may proceed meanwhile. Switch back: `git switch feature/second-accessory-slot`.

### Task 2: Art tool — sides, and the owner's preview

**Files:**
- Modify: `tools/plynling-art/accessories.py` (`on_cap`, `held`, `ACCESSORIES`)
- Modify: `tools/plynling-art/README.md` (sides)

**Interfaces:**
- Produces: style names with a side. nœud is `<colour>_<gauche|droite>`, fleur is `<colour>_<droite|gauche>`. Held items are `<colour>_<gauche|droite>` for ballon, and `gauche|droite` alone for the others. The first option is the default.

- [ ] **Step 1:** Give `held(drawing, grip=None, side="gauche")` a right-hand placement. On `droite`, stamp the drawing mirrored (`pen.stamp(..., mirror=True)`) with its grip just outside `b.hand_r`. Give `on_cap` the mirrored `at` (`1 - at`) and centre for the other side.
- [ ] **Step 2:** Make the nœud, fleur and every held entry in `ACCESSORIES` carry the side as the last style part, default first.
- [ ] **Step 3:** Add a `sides` preview to `__main__`: every switched side on all 14 shapes, content and happy faces. Write it to `<outdir>/accessories_sides.png`. Run `python tools/plynling-art/accessories.py <scratch>` and send the sheet to the owner.
- [ ] **Step 4: GATE:** the owner approves placements. Tweaks go in the drawing offsets only.
- [ ] **Step 5:** Commit: `Art: nœud, fleur and held items on either side`.

### Task 3: Art tool — layer export into the bot's assets

**Files:**
- Modify: `tools/plynling-art/export.py` (new `layers` mode)
- Modify: `tools/plynling-art/sprites.py` (expose drawing the mood extras alone)
- Create: `ProjectSYNCS/Assets/Plynlings/**` (generated)

**Interfaces:**
- Produces the layout below, as 8× animated lossless WebP on the shared idle-loop timing. Each file is drawn alone on a transparent canvas, not diffed. `<shape>` is `<sp>` or `<sp>_baby` for the 7 species.
  - `base/<shape>_<face>[_dirty].webp`: today's plain sprite, every card face and dirty version.
  - `acc/<shape>_<face>_<acc>[_<style>].webp`: one item, frozen tint included. Clean faces only, since dirty shares the clean layer. The canvas is 320 for a hat on an adult, else 256.
  - `aura/<shape>_<face>_<aura>[_tall].webp`: raw particles, unmasked. `_tall` is the 320 canvas version, because `_Spray.clamp` depends on the canvas.
  - `marks/<shape>_<face>.webp`: the face's mood extras alone (heart, sweat, « z », frost).

- [ ] **Step 1:** Add `export.py layers`, writing the layout above (deleting `ProjectSYNCS/Assets/Plynlings/` first). Do not touch `assets/plynlings/`.
- [ ] **Step 2:** Run `python tools/plynling-art/export.py layers`. Expected: roughly 7 000–8 000 files, a few MB. Print the count and size.
- [ ] **Step 3:** In `ProjectSYNCS/ProjectSYNCS.csproj`, extend the `None Include` to `Assets\**\*.webp` (same copy settings as the png/gif). Run `dotnet build ProjectSYNCS`. Expected: the files are under `bin/.../Assets/Plynlings`.
- [ ] **Step 4:** Commit: `Art: export accessory, aura and mark layers for runtime drawing`.

### Task 4: `PlynlingLook` and `PlynlingPainter`

**Files:**
- Create: `ProjectSYNCS/Helpers/Plynling/PlynlingLook.cs`
- Create: `ProjectSYNCS/Helpers/Plynling/PlynlingPainter.cs`
- Modify: `ProjectSYNCS/ProjectSYNCS.csproj` (add `SixLabors.ImageSharp` 3.1.12)
- Create: `tests/ProjectSYNCS.Checks/PaintChecks.cs`; Modify: `tests/ProjectSYNCS.Checks/Program.cs` (section 66, and a `-- art` mode)

**Interfaces:**
- Produces:
  - `public sealed record WornAccessory(string Key, string? Style)`: a full cosmetic key; the style is already resolved.
  - `public sealed record PlynlingLook(string Shape, string Face, bool Dirty, WornAccessory? First, WornAccessory? Second, string? Aura)`. `Face` is the lowercase mood; `Aura` is the key without `cos.aura.`.
  - `string PlynlingLook.FileName`, for example `pl__amanite_baby__content__dirty__noeud-rose_droite__ballon-rouge__aura-bulles.webp`. Segments are separated by `__`; each item is `<key without cos.accessory.>-<style>`, with `-<style>` omitted when there is no style. Both items always appear in draw order.
  - `static PlynlingLook? PlynlingLook.Parse(string fileName)`: the inverse; null for anything else.
  - `static class PlynlingPainter`:
    - `byte[]? Draw(PlynlingLook look)`: null on any missing file or exception, logged once per look.
    - `IReadOnlyList<FileAttachment> FilesFor(MessageComponent components)`: walks the tree, takes every `attachment://pl__…` media, and draws or caches each.
    - `string AssetsRoot { get; set; }`: defaults to `AppContext.BaseDirectory/Assets/Plynlings`; the checks point it at the repo copy.
    - The cache is bounded (about 256 looks, oldest out) and guarded by a lock.

- [ ] **Step 1: Write failing checks in `PaintChecks.cs`:**
  - `LookNamesRoundTrip`: for every look over 2 shapes × every face × every accessory pair × aura or none, `Parse(l.FileName) == l`, and the name contains only `[A-Za-z0-9_.-]`.
  - `SinglesMatchToday`: on adult amanite and baby coprin, every face and every look, `Draw` with one accessory (default side) decodes **pixel-identical, tick for tick,** to `assets/plynlings/plynling_<shape>_<face>[_dirty]_with_<acc>[_<old style>]_v5.webp`. The old style is today's string without the side part. The same goes with an aura (`…_aura_<aura>_v5.webp`) on the aura faces.
  - `MarksRepaintIsInvisible`: base + marks layer is pixel-identical to the base, for every shape and face.
  - `MissingArtIsNull`: a look whose `Shape` has no base (`tournesol`) returns null and doesn't throw.
  - `CacheHits`: drawing the same look twice returns the same array instance.
  - `FilesForFindsPictures`: a V2 card with a thumbnail `attachment://<name>` yields one attachment of that name; a card with a link yields none.
- [ ] **Step 2:** Run `dotnet run --project tests/ProjectSYNCS.Checks`. Expected: FAIL (types missing).
- [ ] **Step 3:** Implement `PlynlingLook` and `PlynlingPainter`.
  - Port `Load`, `Encode`, `Foot`, and the compositing from the spike (`tools/layering-spike/Program.cs` on the spike branch), keeping **Source** frame blending.
  - Item order comes from the category's draw rank (Task 5 provides `AccessoryFit.DrawRank`). Until then, keep the first/second order and leave a `// Task 5` note.
  - The aura pass replaces a pixel only where the composed alpha is < 90.
  - Marks are drawn only when an item's side is `droite` and it is held.
- [ ] **Step 4:** Add the `-- art` mode: `SinglesMatchToday` over **all 14 shapes**, every face, look and aura. Expected: zero differences.
- [ ] **Step 5:** Run both: default checks, then `-- art`. Expected: `ALL CHECKS PASSED`, and the art sweep reports no differences.
- [ ] **Step 6:** Commit: `Plynling: draw dressed pictures from layers (ImageSharp 3.1.12)`.

### Task 5: Categories, sides and the wearing rules (pure)

**Files:**
- Create: `ProjectSYNCS/Helpers/Economy/AccessoryFit.cs`
- Modify: `ProjectSYNCS/Helpers/Economy/AccessoryStyles.cs` (side choices)
- Create: `tests/ProjectSYNCS.Checks/AccessoryFitChecks.cs`; Modify: `Program.cs` (section 67)

**Interfaces:**
- Consumes: `WornAccessory` (Task 4).
- Produces:
  - `enum AccessoryCategory { Hat, Head, Eyes, Neck, Hands, Held }`: not stored. `Head` and `Held` are sided.
  - `AccessoryFit.Category(string key) -> AccessoryCategory?`
  - `AccessoryFit.Side(string key, string? style) -> string?`: `"gauche"`, `"droite"` or null.
  - `AccessoryFit.DrawRank(string key) -> int`: Neck 0, Hands 1, Eyes 2, Hat 3, Head 4, Held 5.
  - `public sealed record AccessoryChange(WornAccessory? First, WornAccessory? Second, string? Replaced, string? Moved)`. `Replaced` is the key taken off by a clash; `Moved` is the key whose side flipped.
  - `AccessoryFit.Pick(WornAccessory? first, WornAccessory? second, int picker, WornAccessory? picked)`: `picker` is 0 or 1; null `picked` takes that slot off.
  - `AccessoryFit.TryOn(WornAccessory? first, WornAccessory? second, WornAccessory item)`
  - `AccessoryFit.Restyle(WornAccessory? first, WornAccessory? second, int slot, string style)`
  - `AccessoryStyles.Choices` adds `Choice("Côté", gauche/droite)` to nœud, `("Côté", droite/gauche)` to fleur, and `Choice("Main", gauche/droite)` to every held item, after any colour. Option names: « À gauche », « À droite ».

- [ ] **Step 1: Write failing checks in `AccessoryFitChecks.cs`, with these exact cases:**
  - couronne + nœud, `Pick(picker 1, bonnet)` gives bonnet + nœud, with `Replaced = couronne`.
  - couronne, `Pick(1, nœud)` gives couronne + nœud.
  - nœud(gauche) + fleur(gauche), via `Restyle(slot 0, "rose_droite")`, gives nœud droite + fleur gauche, with `Moved = fleur`.
  - ballon(gauche), `Pick(1, baguette gauche)` gives ballon droite + baguette gauche, with `Moved = ballon`.
  - nœud + ballon, `Pick(1, nœud)` swaps them: never twice.
  - nœud + ballon, `Pick(0, null)` gives ballon alone, in the first slot.
  - `TryOn`: couronne + ballon, trying the bonnet, replaces the couronne. Nœud alone, trying the ballon, fills the second slot. Lunettes + écharpe, trying the ballon, replaces the second (écharpe), with `Replaced = echarpe`.
  - `Resolve("cos.accessory.noeud", "rose") == "rose_gauche"`; `Resolve("cos.accessory.fleur", null) == "blanche_droite"`; `Resolve("cos.accessory.baguette", null) == "gauche"`.
  - `Category` of every catalogue accessory is non-null, and agrees with `family` in `accessories.py`: cap is Head, held is Held, and so on. Read the Python file like section 62 does.
  - An unknown key passed to `Pick` or `TryOn` changes nothing.
- [ ] **Step 2:** Run the checks. Expected: FAIL.
- [ ] **Step 3:** Implement `AccessoryFit` and the side choices. Clash means the same non-sided category. A same-category sided item on the same side flips the other one; it is never removed.
- [ ] **Step 4:** Replace the `// Task 5` ordering in `PlynlingPainter` with `DrawRank`. Update section 62's `StylesMatchTheArt` so the styles still agree with the art tool.
- [ ] **Step 5:** Run the checks. Expected: PASS.
- [ ] **Step 6:** Commit: `Cosmetics: accessory categories, sides and wearing rules`.

### Task 6: Data — second slot and remembered styles

**Files:**
- Modify: `ProjectSYNCS/Models/Plynling/Plynling.cs` (around lines 171–178)
- Create: migration `AddSecondAccessory` (`dotnet ef migrations add AddSecondAccessory`, schema-only)
- Modify: `ProjectSYNCS/Helpers/Economy/Outfit.cs`, `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs` (`CosmeticSlots`)
- Create: `ProjectSYNCS/Helpers/Economy/SavedStyles.cs`
- Modify: `ProjectSYNCS/Services/Economy/CosmeticService.cs`
- Test: `tests/ProjectSYNCS.Checks/AccessoryFitChecks.cs`

**Interfaces:**
- Consumes: `AccessoryFit`, `AccessoryChange`, `WornAccessory`.
- Produces:
  - Columns: `string? Accessory2Key`, `string? Accessory2Style`, `string? SavedAccessoryStyles`.
  - `SavedStyles.Parse(string?) -> IReadOnlyDictionary<string,string>` and `SavedStyles.Format(IReadOnlyDictionary<string,string>) -> string?`. The format is `key=style;key=style` with full keys; empty means null.
  - `Outfit` gains `CosmeticInfo? Accessory2 = null, string? Accessory2Style = null`. `Outfit.Of` normalises: a second without a first moves up, and unknown keys are dropped. `Outfit.TryOn(CosmeticInfo c, IReadOnlyDictionary<string,string> saved)` uses `AccessoryFit.TryOn` with the saved style.
  - `CosmeticSlots.Apply(Plynling p, AccessoryChange change)` writes both slots, saves the outgoing items' styles, and restores incoming ones from `SavedAccessoryStyles`. `Set(p, CosmeticSlot.Accessory, key)` stays for non-accessory callers but no longer wipes the style.
  - `CosmeticService.WearAsync(..., CosmeticSlot slot, string? key, DateTimeOffset now, int picker = 0)` returns `(CosmeticOutcome, Plynling?, AccessoryChange?)`.
  - `StyleAsync(guildId, userId, accessoryKey, choice, option)` accepts the key worn in either slot and returns the same triple, via `Restyle`.
  - `BuyAsync` and `CraftAsync` with `wear` use `TryOn`, and `CosmeticResult` gains `AccessoryChange? Change`.

- [ ] **Step 1: Write failing checks:**
  - `SavedStyles` round-trip.
  - `Outfit.Of` on a row with only `Accessory2Key` puts it first.
  - `Outfit.Of` with a retired key in slot 2 shows one accessory.
  - Wear nœud, set `dore_droite`, take it off, wear it again: style `dore_droite`. Done on an in-memory `Plynling` through `CosmeticSlots.Apply`.
- [ ] **Step 2:** Run. Expected: FAIL.
- [ ] **Step 3:** Implement the model, the migration (inspect it: `AddColumn` ×3, nothing else), `SavedStyles`, `Outfit`, `CosmeticSlots.Apply` and the service changes.
- [ ] **Step 4:** Run the checks and `dotnet build`. Expected: PASS, 0 errors.
- [ ] **Step 5:** Commit: `Cosmetics: second accessory slot and remembered styles (migration)`.

### Task 7: Card line and the Garde-robe's two pickers

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingCardUi.cs:48`
- Modify: `ProjectSYNCS/Commands/Economy/CosmeticCards.cs` (~150–176, the try-on `tried` outfit)
- Modify: `ProjectSYNCS/Interactions/Components/CosmeticComponentHandler.cs` (`cosm:pick`, `cosm:wear`, `cosm:buyw`, `cosm:makew`, `cosm:style`, plus the new `cosm:pick2`)
- Test: `tests/ProjectSYNCS.Checks/CosmeticScreenChecks.cs` (section 59), `AccessoryArtChecks.cs` (`WardrobeStylePicker`)

**Interfaces:**
- Consumes: Task 6's `Outfit`, `WearAsync(picker)`, `AccessoryChange`.
- Produces: custom-id `cosm:pick2:{i}`, for the second picker, chunked by 25 like the first. Its handler calls `WearAsync(..., picker: 1)`.

- [ ] **Step 1: Write failing checks:**
  - The heading of an outfit with nœud + ballon contains `Porte 🎀 un nœud et 🎈 un ballon`, using the catalogue's emoji and names.
  - The worst-case wardrobe has two accessories with two style choices each (écharpe + nœud), with every select present. It builds, and its component count is ≤ 40. Print the count.
  - With two accessories worn, the custom-ids are unique.
- [ ] **Step 2:** Run. Expected: FAIL.
- [ ] **Step 3: Implement:**
  - The heading line, in the form « Porte {e1} {n1} et {e2} {n2} ».
  - Under the first picker, its style selects. Then « Second accessoire… » with its own selects; the first picker is relabelled « Premier accessoire… ».
  - The notices. Suggested wording: « Le bonnet a remplacé la couronne. » for `Replaced`, and « La fleur est passée à gauche. » for `Moved`.
  - The try-on through `Outfit.TryOn`.
- [ ] **Step 4:** Run the checks. Expected: PASS.
- [ ] **Step 5:** Commit: `Cosmetics: two accessory pickers and the card line`.

### Task 8: `SpriteOf` draws, and the card and cosmetics screen attach (after Task 1 passed)

**Files:**
- Modify: `ProjectSYNCS/Helpers/Plynling/PlynlingArt.cs` (`Sprite` loses `accessory`, `style` and `aura`; `SpriteOf` builds a `PlynlingLook`)
- Modify the send sites:
  - `Commands/Plynling/PlynlingModule/PlynlingModule.Rendering.cs:19-28` (`RespondCardAsync`)
  - `Commands/Plynling/PlynlingModule/PlynlingModule.cs:184` (opening card), `:472` (Garde-robe follow-up)
  - `Interactions/Components/PlynlingComponentHandler.cs:600-610` (care reply)
  - `Interactions/Components/PlynlingEventHandler.cs:135-140` (REST card refresh)
  - `Services/Plynling/PlynlingShortcutService.cs:131, :160`
  - `Commands/Economy/InventoryModule.cs:49`
  - `Interactions/Components/CosmeticComponentHandler.cs` `RedrawAsync`
- Test: `tests/ProjectSYNCS.Checks/AccessoryArtChecks.cs` (`SpriteOfReadsTheOutfit`, `EveryLookHasItsFiles` now checks `Assets/Plynlings` layers)

**Interfaces:**
- Consumes: `PlynlingPainter.Draw`, `PlynlingPainter.FilesFor`, `PlynlingLook.FileName`.
- Produces: `SpriteOf` returns `attachment://{look.FileName}` when the Plynling wears an accessory or an aura (not angry) and `Draw` succeeds; otherwise the bare link.

- [ ] **Step 1: Write failing checks:**
  - `SpriteOf` for a bare Plynling equals `PlynlingArt.Sprite(...)`, the link.
  - With nœud + ballon it starts with `attachment://pl__`.
  - Angry with nœud gives the link.
  - A sunflower with nœud gives the link (Review Focus 1).
  - `EveryLookHasItsFiles`: every layer the bot can request exists under `ProjectSYNCS/Assets/Plynlings`, for every shape, face, accessory, style and side, every aura (tall and not) and every marks face.
- [ ] **Step 2:** Run. Expected: FAIL.
- [ ] **Step 3:** Implement `SpriteOf`. Update each listed send site:
  - Respond and follow-up: `RespondWithFilesAsync` / `FollowupWithFilesAsync` when `FilesFor` is non-empty, otherwise the current call.
  - Modify and Update: always `m.Attachments = new Optional<IEnumerable<FileAttachment>>(files)`.
  - Channel send: `SendFilesAsync`.
  - Dispose the attachments after sending.
- [ ] **Step 4:** Run the checks and `dotnet build`. Expected: PASS.
- [ ] **Step 5:** Ask the owner to run the bot on the dev guild with a dressed Plynling: `/plynling view`, a care button, the Garde-robe, with picks and try-on. Expected: the picture shows and animates, and taking everything off shows the link with no leftover file.
- [ ] **Step 6:** Commit: `Plynling: dressed pictures drawn and attached on the card and the Garde-robe`.

### Task 9: Attach on games, journal, visits and announcements

**Files:**
- `Commands/Plynling/PlynlingModule/PlynlingModule.cs:164` (game), `:224` (visit), `:243` (journal)
- `Interactions/Components/PlynlingComponentHandler.cs:176` (journal page), `:224/:269/:272` via `EditVisitAsync` (`:368`), `:335` (ending), `:501` (game)
- `Services/Plynling/PlynlingAnnouncer.cs` `PostAsync` (`:153`): its `imageUrl` may now be `attachment://…`
- `PlynlingEventCards` uses the bare `Sprite` (content face): unchanged.

**Interfaces:** Consumes `PlynlingPainter.FilesFor`. Same send rules as Task 8.

- [ ] **Step 1:** Grep `SpriteOf(` and every caller of `BuildGame`, `BuildJournal`, `BuildChoice`, `BuildKnock`, `BuildEnding`, `CardHead` and `BuildCardAsync`. Confirm every send site of a picture-carrying card is in Task 8 or 9's list. Add any that are missing.
- [ ] **Step 2:** Update each site as in Task 8.
- [ ] **Step 3:** Run `dotnet build` and the checks. Expected: 0 errors, PASS.
- [ ] **Step 4:** Dev guild, run by the owner: a game, the journal with paging, a visit through to its ending, and a resurrection if one is handy. Expected: the dressed picture appears on each.
- [ ] **Step 5:** Commit: `Plynling: dressed pictures attached on games, journal, visits and posts`.

### Task 10: Docs, version, changelog

**Files:**
- `docs/agents/economy.md`: two slots, the categories table, sides, remembered styles. Also note that a retirement migration must clear both slots.
- `docs/agents/plynling.md` (Art): runtime drawing for dressed pictures; layers in `ProjectSYNCS/Assets/Plynlings`; `export.py layers`. Old `_with_`/`_aura_` files stay for posted messages.
- `docs/agents/deployment.md`: the image now carries the Plynling layers. ImageSharp is pinned to 3.x because 4.x needs a licence key; it decodes only shipped files.
- `docs/agents/architecture.md`: nothing (schema-only migration).
- `ProjectSYNCS/config.yaml`: `8.9.0`. `ProjectSYNCS/CHANGELOG.md`: `## 8.9.0 — <date>` with bullets for two accessories, sides and remembered styles.

- [ ] **Step 1:** Write the docs and the bump. If `git diff main -- ProjectSYNCS/config.yaml` already shows a bump, don't bump again.
- [ ] **Step 2:** Run the full checks once more, then `-- art`. Expected: `ALL CHECKS PASSED`, no art differences.
- [ ] **Step 3:** Commit: `Cosmetics: second accessory slot (8.9.0)`. Ask the owner before pushing or merging.
