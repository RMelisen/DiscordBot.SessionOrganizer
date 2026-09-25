# Plynlings: badges and the journal — design

**Date:** 2026-09-25 · **Status:** approved

Part 2 of the Plynlings' V2. Badges belong to **the Plynling**; the journal is **one card** with
its stats, its badges and its latest moments.

## 1. Badges

Defined in code (`Helpers/PlynlingBadges`), each with a stable key, an emoji, a French name, a
condition and a reward in cailloux paid to the owner. **Stored when earned**
(`PlynlingBadge` rows: Plynling, key, date), so each is paid exactly once, the journal can date
it, and a later threshold change never takes one away.

| Theme | Badges (reward) |
|---|---|
| Age, time lived | 🌱 Une semaine (10) · 🌿 Un mois (20) · 🌳 Six mois (40) · ☀️ Un an (50) |
| Play | 🎲 Première partie (10) · 🎯 10 victoires (20) · 🏆 50 victoires (40) · 🎮 100 parties (40) |
| Visits | 🏡 Première visite (10) · 💞 10 visites (20) · 🌍 30 visites (40) |
| Care | 🍄 100 repas (20) · 🤲 100 caresses (20) · 🎁 Nourri par un ami (10) |
| Survival | 😮‍💨 Sauvé de justesse — fed while starving (20) · ✨ Revenu d'entre les morts (0: only staff can resurrect) |

New counters on `Plynling`: `Meals`, `Pets` (received), `FedByOthers` — recorded from ship day,
like the play counts. Age badges use `PlynlingLife.Age` (time actually lived).

**When badges are checked:**
- **After an action that can earn one** (feed, pet, a finished game, a visit, a resurrection), in
  the **same save** as the action. The card that answered adds « 🏅 Nouveau badge : … · +N
  cailloux ». No public post.
- **Hourly, by the sweep**, for what time earns (the age badges) — and for Plynlings that already
  qualified on ship day. Those have no card: they only go into the journal.

## 2. The journal

**Moments** (`PlynlingJournalEntry` rows: Plynling, date, kind, detail), written in its voice and
gendered at display — only notable things, never every meal: adopted, first meal, grew into
ado / adulte / ancien, first game won, visited / received someone, frozen / thawed, first meal
paid by a friend, a badge earned, resurrected, died. **At most 100 per Plynling**; the oldest go.
`JournalKind` is stored as an int and is **append-only**.

Stage moments and the death moment are written by the **sweep** (a stage is reached by time; a
death found by a command is announced — and journaled — by the next sweep), dated when they
happened.

**`/plynling journal [user]`** — one Components V2 card, for the living and the dead: header
(name, species, stage, age), stats (meals, pets received, games played / won, visits), the badges
earned (« N/16 badges »), and the latest **6** moments with ◀ ▶ to page back
(`plyn:jprev:` / `plyn:jnext:` — distinct verbs). `AllowedMentions.None` (a moment may name
someone).

## 3. Storage

One migration, `AddPlynlingBadgesAndJournal`: the two tables (both cascading on the Plynling's
deletion, so an abandoned Plynling takes its journal with it; a unique index on
(Plynling, key) for badges) and the three counters.

## Checks

Every badge's condition; paid exactly once, in the action's save (an in-memory SQLite database in
the harness); the journal's cap; every moment's wording in both genders; the card's components
and ids; the help caps. Build `-warnaserror`.
