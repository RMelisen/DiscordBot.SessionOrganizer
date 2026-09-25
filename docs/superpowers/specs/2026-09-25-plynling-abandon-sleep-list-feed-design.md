# Plynlings: abandoning, sleep, the list, feeding others — design

**Date:** 2026-09-25 · **Status:** approved, sleep art pending review

Four independent additions to the Plynlings, built and committed one at a time.

## 1. `/plynling abandon`

- A **slash command** (not a card button), owner-only by construction (it acts on the caller's
  own living Plynling). It opens a **modal**: « Tape le nom de ton Plynling pour confirmer ».
  The typed name must match the Plynling's name, compared trimmed, case-insensitively and with
  runs of whitespace collapsed; anything else is refused ephemerally and nothing happens.
- A frozen Plynling can be abandoned too (only a living one: dead ones are already gone).
- **What happens:** the Plynling **leaves for good** — its row is deleted. It does not go to
  `/graveyard` and cannot be resurrected.
- **The shame:**
  - a **public announcement** in the Plynling channel (`PlynlingAnnouncer`), gendered, naming
    the owner, with the Plynling's sad sprite: « 💔 @x a abandonné **Pouf**… »
    (`BotResponses.PlynlingAbandonLines`);
  - a new **`/shame` title « 💔 L'Indigne »** counting abandonments: `AbandonHits` on
    `ShameRecord` and `ShameDailyStat` (one migration, `AddShameAbandonHits`), recorded by
    `ShameService.AddAbandonHitAsync`, empty-title pool `ShameEmptyIndigne`. The wall grows from
    26 to **31 of 40** components, leaving room for exactly one more title;
  - a **30-minute adoption cooldown** after abandoning, held in memory (`PlynlingCooldowns`), so a
    restart clears it — acceptable at 30 minutes.

## 2. Sleep, 01:00–05:00 Europe/Paris

- **`PlynlingMood.Sleeping`**, computed: `Mood` returns it when the Paris wall clock is in
  [01:00, 05:00). Frozen still wins. Card label « endormi / endormie »; the status line shows
  « 💤 Endormi(e) jusqu'à 5 h ».
- **Death is deferred to the night's end:** hunger keeps dropping exactly as now, but when the
  raw starvation instant falls in [01:00, 05:00) the Plynling dies at **05:00** that morning
  (`PlynlingLife.EffectiveDeathAt`). `Settle`, the card and the sweep all use the effective
  instant.
- **The warning never goes out at night:** its moment is 3 h before the effective death; if that
  falls in [23:00, 05:00) it moves to **23:00** the evening before (`PlynlingLife.WarnAt`).
  Feeding re-arms it when `now < WarnAt`.
- **Care while asleep:** feeding works; **petting is refused** (« Chut… il/elle dort. Reviens
  après 5 h. »), costs no cooldown, and the card hides « Caresser » while it sleeps.
- **Art:** a `sleeping` state for all 7 species, adult and baby — **14 new animated WebPs**
  (ado/ancien wear the adult). The frozen pose without the frost: eyes closed, a small open
  mouth, the usual slow breath, species touches off, and small « z » glyphs rising straight up
  and fading. Nothing moves side to side; frame 0 is the still. The Coprin sleeps with its resting
  ink (level 2; baby 0). No existing file changes.
- Pure helpers in `PlynlingLife` (`IsAsleep`, `NightEnd`, `EffectiveDeathAt`, `WarnAt`), checked
  on ordinary days and both DST days (2026-03-29, 2026-10-25).

## 3. `/plynling list`

The server's **living** Plynlings (settled first), **oldest first**, 10 per page, as an **embed**
(mentions in embeds never ping). Each line: name + gender sign, species, stage, owner, age, and
💤/❄️ when asleep/frozen. ◀ ▶ buttons with distinct verbs `plyn:lprev:{page}` / `plyn:lnext:{page}`
(a shared verb would collide with `COMPONENT_CUSTOM_ID_DUPLICATED`).

## 4. Feeding someone else's Plynling

Anyone may feed anyone's Plynling; a non-owner pays **double**, from their own wallet, in the same
single save as the meal. The card's « Nourrir » options say « … · N pour un autre » (N = double);
`/plynling feed` gains an optional `user:`. The card line credits the feeder and the price.
Refusals unchanged otherwise (dead, frozen, wasted, too poor). `CareOutcome.NotOwner` and
`PlynlingText.NotYours` become unused and are removed.

## Checks

Pure C# harness checks: the sleep window, `NightEnd`, `EffectiveDeathAt` and `WarnAt` (incl. DST
days), `Mood` at night, the double price, the name comparison, the `/shame` component count
(≤ 40), the help embed caps. Art: artcheck (98 sprite URLs), animcheck (14 new loops slot-exact,
123 existing files byte-identical). Build `-warnaserror`.

## Docs

`/plynling help`, `README.md`, `CLAUDE.md` (sleep and deferred death, abandon = deletion + in-memory
cooldown, the shame wall at 31/40), `tools/plynling-art/README.md`.
