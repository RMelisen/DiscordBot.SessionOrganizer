# Accessories drawn on the sprite — design (piece C)

## Intent

Last piece of the cosmetics rework (A one screen, D crafting shipped; B dropped by the owner). An
accessory today is a line of text (« Porte 🎩 un haut-de-forme »). Players don't buy what they can't
see: the accessory should be **on the Plynling's picture**, and the try-on in the cosmetics screen
should show it there before buying.

## Goals / non-goals

- Goal: every accessory drawn on every living Plynling picture shown alone — the card
  (`/plynling view`), the cosmetics screen try-on, games, the journal, the visit's first card, the
  announcer's resurrection/abandon posts (everything that goes through `PlynlingArt.SpriteOf`).
- Goal: some accessories come in **styles the player picks** — a colour or a position — free, in
  the Garde-robe, kept per Plynling.
- Goal: a new accessory later costs one drawing and a re-export.
- Non-goals: visit gallery pictures (`VisitSprite`), event cards (always the content face),
  memorials and graves, thèmes/titres/cadres on the picture.
- Anti-goal: the bot doing image work at runtime, or anything that can break a card at display time.

## Approaches

1. **Pre-rendered files** (chosen by the owner) — the art tool draws each accessory on each Plynling,
   every card face and frame, and exports them beside today's files. The bot only picks a filename.
2. Runtime compositing — the bot keeps bare sprites and accessory layers and glues them on the Pi,
   attaching the image to each message. Rejected: a new image library, Pi work per card, every
   sprite-showing card changes how it sends images, no Discord caching.

The cost is files, not drawing work: 17 drawings (and their styles), the rest is generated.

## Decided (owner's review of every species, 2026-11-23)

- **17 accessories.** The sac à dos and the casque audio are **retired from the game**: gone from
  the catalogue and the shop, every copy refunded at its price (1 200 cailloux each, both peu
  communs) and taken off any Plynling wearing it — one deliberate data migration.
- **Where they sit** (`tools/plynling-art/accessories.py`, placed from the drawing itself):
  - Hats — couronne, haut-de-forme, casquette: upright, centred, resting on the cap's top, never in
    it. An adult's cap fills its picture nearly to the top, so **an adult wearing a hat is drawn on a
    taller canvas** (40 px, the visit canvas: the Plynling fills 80 % of it, the hat sits above).
    Babies keep the 32 px picture. Some shapes settle the hat a row or two into the cap (owner's
    eye: adult Amanite, Mycène, Girolle, Coprin by 1; baby Mycène by 2).
  - On the cap — nœud (left), fleur (right): a symmetric daisy.
  - On the eyes — lunettes.
  - At the neck — écharpe, collier de coquillages, médaillon.
  - On the hands — moufles. Held in the left hand — ballon, baguette, canne à pêche, bocal,
    tulipe, glace, plume d'or (left: the heart, the sweat and the « z » are on the right).
- **Styles** — one or two choices per accessory, each with its options; the first option of each is
  the default, shown until the player picks another:
  - nœud — couleur: rose, bleu, doré, blanc;
  - fleur — couleur: blanche, rose, bleue, jaune;
  - écharpe — couleur: rouge, bleue, verte; **and** position: haut, bas;
  - casquette — couleur: bleue, rouge, verte; ballon — couleur: rouge, bleu, jaune;
    moufles — couleur: rouges, bleues, vertes;
  - collier de coquillages, médaillon — position: haut, bas.
  - Position: haut (right under the cap), bas (a little lower for an adult; the first stem row for a
    baby). Where the low place would cover the eyes (adult Cèpe and Rosé), it is the same picture.
  - Picked in the Garde-robe, under the worn accessory (one select per choice), free, any time;
    stored on the Plynling as one `AccessoryStyle` string (the options joined by `_`, colour first:
    `bleue_haut`; null = default). Wearing another accessory or taking it off resets it. The shop and
    recipe try-on show the default style.
- Follows the breath and the hop; nothing moves sideways. Frozen: under the same ice tint as the
  body. Sleeping: kept. Dirty: drawn over the mud (it stays clean). Gold stays plain gold (an
  outline was tried and dropped).
- **Files**: for each shape (7 adults, 7 babies), each card face (happy, content, sad, hungry,
  starving, frozen, sleeping, sick), its `_dirty` version where one exists, and each look:
  `plynling_<species>[_baby]_<face>[_dirty]_with_<accessory>[_<style>]_v4.webp` — every style of a
  styled accessory has its own suffix, the default included. 36 looks → 14 × 15 × 36 = 7 560 files
  (~50 MB; today's 319 weigh 1.9 MB). New names, so no `ART_VERSION` bump and every existing file
  re-exports byte-identical. Angry is a visit face: no accessory version.
- **Bot**: `PlynlingArt.Sprite` takes an optional accessory and style; `SpriteOf` reads what the
  Plynling wears, or the try-on's `Outfit`. An unknown or retired key falls back to the plain
  picture; an unknown style to the default one.
- **Guard**: a checks section fails if any URL the bot can build for an accessory has no file under
  `assets/plynlings/`, and if the bot's styles and the drawn ones disagree.
- Images resolve only once pushed to GitHub `main`: testing on the dev guild needs a push, asked first.
- Minor bump: 8.4.0 → 8.5.0 (user-visible behaviour, a migration).

## Left to the builder

The wording of the style menu and the refund notice in the changelog; the exact Garde-robe layout
of the style select (within the 40-component cap).
