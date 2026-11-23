# Second accessory slot, drawn at runtime — design

## Intent

Give players more ways to dress their Plynling: **wear two accessories at once**, and **choose
which side** a side-bound accessory sits on (a nœud on the right instead of the left, a ballon in
the right hand). Picking a side for one item moves the other side-bound item of the same kind to the
other side by itself.

Today one accessory is worn, and every dressed picture is a pre-rendered file on GitHub (piece C,
`2026-11-23-accessories-on-sprite-design.md`). Pre-rendering every pair would mean ~500 000 files
(~5 GB), so the bot now **draws dressed pictures itself** from a bare sprite plus one small layer
per item. The spike on branch `spike/runtime-layering` (`tools/layering-spike/`, not merged)
proved it with ImageSharp (108 pictures on 3.1.12, then all 8 820 on 4.1.3): all 8 820 dressed pictures rebuilt pixel-exact from bare sprite +
layer, ~66 ms per picture on a desktop (lossless, `WebpEncodingMethod.Level0`; `Level6` takes 1.4 s,
never use it), ~400 bytes per layer.

## Goals / non-goals / anti-goals

- Goal: a Plynling wears **up to two accessories**, from different categories (below).
- Goal: **sides are a style choice** for nœud, fleur and every held item; changing it swaps the
  other side-bound item of the same category.
- Goal: **styles are remembered per accessory per Plynling**: taking an item off and putting it
  back keeps its colour and side (today they reset).
- Goal: every picture that shows accessories today (everything through `PlynlingArt.SpriteOf`:
  card, cosmetics try-on, games, journal, visit's first card, announcer posts) shows both, and
  the aura.
- Goal: a new accessory later still costs one drawing and a re-export.
- Non-goals (unchanged from piece C): visit gallery pictures (`VisitSprite`), event cards,
  memorials and graves, thèmes/titres/cadres on the picture, the angry face.
- Non-goal: deleting today's pre-rendered `_with_` / `_aura_` files under `assets/plynlings/`.
  Messages already posted link to them; removing them is a later owner call.
- **Anti-goal: a card that fails to show because drawing failed.** Any drawing error logs and
  falls back to the bare sprite link.
- Anti-goal: the Pi redrawing the same picture over and over.

## Constraints

- Pi (arm64): drawing is estimated 0.2–0.4 s per picture there; every interaction still answers
  within 3 s (`CLAUDE.md`, Reliability).
- Components V2 cap of 40 per message; the Garde-robe's worst case is 25 today (checks section 59).
- **ImageSharp 3.1.12, not 4** (owner): 4.x needs a Six Labors licence key at build time (Release
  fails without one) and the owner doesn't want one. 3.1.12 is the last 3.x release; NuGet lists six
  advisories against it. They concern decoding crafted images: the bot only ever decodes its own
  shipped files, never user uploads, so the exposure is low. NuGet's audit warnings show in the build
  but don't fail it (no `-warnaserror` on the csproj). The spike's 3.1.12 API is in commit
  `da70e8e4` (`WebpFrameMetadata.BlendMethod = WebpBlendMethod.Source`, `DisposalMethod =
  WebpDisposalMethod.RestoreToBackground`); the full 8 820 check is rerun on 3.1.12.
- The image is built from `ProjectSYNCS/` only (`assets/` at the repo root is not in it).
  `ProjectSYNCS/Assets/` already ships icons and avatars via the csproj's `None Include`.
- Keys are append-only; accessory keys stay as they are.
- **Unverified, and the plan's first task**: that Discord plays an *attached* animated WebP in a
  Components V2 thumbnail (`attachment://…`), including after the message is edited and after the
  attachment is swapped. If it doesn't, stop and come back to the owner: the approach depends on it.
  The check covers the three edit paths the bot uses (my call): a public message edited by REST
  `ModifyAsync` (loops), a public card edited by a button's `UpdateAsync`, and an **ephemeral,
  deferred message edited through the interaction webhook** (the cosmetics screen, where most
  redraws happen). The owner sets the dev token in user secrets; the builder then runs it on the
  dev guild. Until it passes, only work that needs no Discord may proceed, on the feature branch:
  layer export, data model and pure rules, the drawer, checks. Nothing wires a card to attachments
  before it passes.

## Approaches

1. **The bot draws every dressed picture** (chosen by the owner). Any Plynling wearing an
   accessory or an aura gets its picture built from the bare sprite + layers and attached to the
   message; a Plynling wearing nothing keeps today's GitHub link. One drawing path, so a nœud on
   the right looks the same alone or with a ballon. Cost: every send/edit of a picture-carrying
   card learns to attach a file; Discord no longer caches dressed pictures by URL.
2. Draw only two-accessory pictures, keep links for one. Rejected: every card still needs the
   attachment path (any Plynling can wear two), switched sides alone have no file (~13 000 more
   exports or drawing anyway), and two drawing paths must agree pixel for pixel.
3. Pre-render every pair. Rejected: ~500 000 files, ~5 GB.

Layers ship **in the image** (`ProjectSYNCS/Assets/`), not downloaded from GitHub: no network at
draw time, no push to `main` needed to test on the dev guild.

## Decided

### Categories (owner: two at most; hat + nœud/fleur allowed)

| Category | Accessories | Two at once? |
|---|---|---|
| Chapeau | casquette, haut-de-forme, couronne, chapeau de paille, bonnet, couronne de fleurs | no |
| Sur la tête (sided) | nœud, fleur | yes, one per side |
| Yeux | lunettes | no |
| Cou | écharpe, collier de coquillages, médaillon | no |
| Mains | moufles | no |
| Tenu (sided) | ballon, baguette, tulipe, glace, plume d'or, bocal, canne à pêche, lanterne | yes, one per hand |

(The catalogue is the source; the list above is today's. A new accessory gets a category in the
catalogue, and the art tool's family must agree — checks.)

- Any two accessories of **different categories** combine (hat + nœud included, the nœud drawn on
  top). Two of the same sided category combine when they are on **different sides**.
- **Wearing an item that clashes** with the other worn one (same non-sided category) replaces it,
  with a notice: « Le bonnet a remplacé la couronne. »
- **Same side**: wearing a second sided item, or switching one's side, onto the side the other
  occupies **moves the other one to the opposite side** (its remembered side is updated). It never
  removes it.
- **Never twice**: picking in one picker the accessory worn in the other moves it, never duplicates.
- **A pick in one picker that clashes with the other slot** replaces the clashing item where it
  is, and leaves the picker's own item on (my call: one pick never takes off two things). Couronne
  + nœud, bonnet picked in the second picker → bonnet + nœud, « Le bonnet a remplacé la
  couronne. » With no clash, the pick takes its picker's slot.

### Sides as styles

- New style choice in `AccessoryStyles`: « Côté » (gauche, droite) for nœud and fleur, « Main »
  (gauche, droite) for held items. **First option = default = where it sits today**: nœud gauche,
  fleur droite, held gauche. Existing style strings stay valid (colour first, then side, joined by
  `_`); the art tool's style names must match (checks section 62 extends to it).
- Placement: the mirrored side uses the same drawing (`on_cap` at `1 - at`; held items at
  `hand_r`, drawing mirrored). The art tool gives a preview sheet of every switched side on every
  shape for the owner's eye before export, as piece C did.

### Draw order (back to front)

aura → body (bare sprite) → neck → mittens → glasses → hat → head ornaments → held items.

- **The aura keeps today's rule**: a particle shows only where the frame under it (body and every
  accessory) has alpha < 90 (`auras.py`, `SHADOW_ALPHA`), and replaces that pixel (no blending).
  So the bot draws it **last with that mask**, from a raw aura layer — exactly what the art tool does.
  Auras stay off the frozen, sick and angry faces.
- **Mood marks on top of right-hand items** (owner): the heart, the sweat drops and the « z » sit
  right of the body, where a right-hand item goes. The art tool also exports each face's marks as a
  layer, and the bot repaints it on top when a right-hand item is worn, so the mood stays readable.
  The starving face's left sweat drop stays under left-hand items, as today.

### Garde-robe, try-on, card (owner approved)

```
[ Thème ] [ Titre ] [ Accessoire ] [ Aura ] [ Cadre ]
  (picture)  Pomme, la Rêveuse
             Porte 🎀 un nœud et 🎈 un ballon
[ Premier accessoire : 🎀 Nœud        ▾ ]
[ Couleur : Rose                      ▾ ]
[ Côté : Gauche                       ▾ ]
[ Second accessoire : 🎈 Ballon       ▾ ]
[ Couleur : Rouge                     ▾ ]
[ Main : Gauche                       ▾ ]
```

- Each worn accessory's style selects sit under its picker, one per row. Worst case ~31–33
  components; section 59 re-measures and must stay ≤ 40.
- The card line becomes « Porte 🎀 un nœud et 🎈 un ballon » (`PlynlingCardUi`); the aura line
  stays under it.
- **Shop/recipe try-on and « Acheter et porter »**: the new item replaces a clashing worn item;
  otherwise fills the empty slot; otherwise replaces the second accessory. The notice says which.
  The try-on shows the item in its remembered style, else its default.
- Taking off the first accessory moves the second up (my call: the order only matters for the
  card line and for which one a try-on replaces).

### Data

My proposal, **schema-only** (no data migration, per `architecture.md`):

- Keep `AccessoryKey` + `AccessoryStyle` as the first slot.
- Add `Accessory2Key` + `Accessory2Style` (nullable).
- Add `SavedAccessoryStyles` (nullable string, e.g. `noeud=dore_droite;ballon=bleu`): written
  whenever a style is picked or an accessory is taken off; read when an accessory is put on. A
  worn accessory's style column stays the truth while it is worn.
- `CosmeticSlots.Set` stops wiping the style on change; it saves the outgoing one and restores the
  incoming one. `Outfit` grows the second accessory and its style.
- A future retirement migration (like `RetireBackpackAndHeadphones`) must clear both slots — note
  in `economy.md`.

### Drawing in the bot

- **Assets in the image**, under `ProjectSYNCS/Assets/Plynlings/` (csproj `None Include` extended
  to `*.webp`): the bare v5 sprites (~280), one layer per shape (14) × card face (8) × look, one
  raw aura layer per shape × aura face × aura, and the marks layers. Today's 42 looks + 18 switched
  sides (nœud ×4, fleur ×4, held ×10) ≈ 6 700 accessory layers; a few MB in all.
- **Layers are exported by the art tool directly** (each accessory drawn alone on a transparent
  canvas, frozen tint included), not diffed from finished pictures: a diff loses pixels where the
  accessory matches the body, which shows when a second accessory sits under it.
- Compositing follows the spike: a 125 ms tick timeline (`motion.py` FRAME_MS; ImageSharp frame
  delays expanded to ticks, identical ticks merged on encode), every frame written with
  Source blend and RestoreToBackground disposal, the largest canvas wins (an
  adult in a hat: the 40 px hat canvas) and every layer sits at its foot, lossless WebP Level0.
- **One singleton drawer** with an in-memory, bounded cache keyed by the full look (shape, face,
  dirty, both accessories + styles, aura). Rendered bytes only; nothing on disk; resets on restart.
- **Every picture-carrying card** sends the file as an attachment and points its thumbnail at
  `attachment://<name>.webp` when dressed; bare Plynlings keep the link. Builders stay static and
  measurable: they take the picture reference, the sender attaches the bytes. This covers the 12
  calls in 7 files: `PlynlingEventCards`, `PlynlingJournalCards`, `PlynlingModule.Rendering`,
  `PlynlingPlayCards`, `PlynlingVisitCards`, `PlynlingVisitStory`, `PlynlingAnnouncer`, plus the
  cosmetics screen.
- Failure anywhere in drawing → log, bare sprite link (anti-goal above).

### Checks (`tests/ProjectSYNCS.Checks`)

- Every layer the bot can ask for exists under `Assets/Plynlings/`.
- Single-accessory (and accessory + aura) pictures drawn by the bot are **pixel-exact** against
  today's pre-rendered files (the spike's test, kept as a guard while those files exist).
- Category rules: clash, side swap, never twice, try-on placement — as pure functions.
- Catalogue categories and sides agree with the art tool; styles agree (section 62).
- Section 59 re-measures the Garde-robe at its worst.

### Shipping

- Next **minor** bump (8.8.0 → 8.9.0 if nothing lands first): a new behaviour and a migration.
  CHANGELOG entry.
- Docs: `economy.md` (two slots, categories, remembered styles), `plynling.md` Art (runtime
  drawing replaces links for dressed pictures), `deployment.md` (the assets shipped in the image, and why ImageSharp stays on 3.x).

## Left to the builder

- Names: the drawer class, the asset folder layout and layer file names, the cache size.
- Whether dirty faces share the clean face's layer (they should, if the export proves identical).
- Exactly how each send path attaches (respond with files, `UpdateAsync`/`ModifyAsync` with
  `Attachments`, channel sends from loops); whether an edit that doesn't change the picture
  re-attaches.
- The style string format inside `SavedAccessoryStyles`; select labels and placeholders (French);
  the wording of the replace/move notices.
- Whether the bot draws at the first need or pre-warms common looks.
