# Ping-Qilin's daily outfit — short design

**Intent.** Ping-Qilin, the bot's own Plynling, wears real cosmetics: a hand-picked set of complete
outfits, a different one each day, so she shows off the best of the catalogue and feels looked after.

**Goals.** Whole looks that match her (naps, Vaniteuse, Moqueuse, Adorable), never a clashing mix;
a new one each Plynling morning (05:00 Paris), never the same two days running; seasonal looks only
in their season. **Non-goals.** No announcement, no new lines, no inventory for the bot, the cadre
left alone (she cannot die). **Anti-goal.** A daily write or a new loop for this.

**Approaches.** (1) *Curated looks* — chosen by the owner. (2) Random per slot from the rare-and-up
items — rejected: clashes (katana + doudou + auréole).

**How.**
- `Helpers/Plynling/MascotWardrobe` (pure): the looks (thème, titre, up to three accessories with
  styles, aura, optional season) and `For(dayKey)`: each Monday-to-Sunday week deals the pool of its
  Monday's season (year-round looks plus the season's) as a deck seeded by the week, so each look
  shows once a week; a deck opening on Sunday's look swaps its first two; a seasonal look on a day of
  the next season gives way to that season's.
- `PlynlingLife.Tend` (every read of her row) dresses her in that look, writing only when a slot
  differs, through `CosmeticSlots` (accessories via `Apply`). The card, sprite and list read the
  slots unchanged; nothing checks ownership.
- Checks: every key exists in its slot, styles resolve to themselves, accessories fit
  (`AccessoryFit` accepts them as listed), seasonal items only in their season's look, no two days
  running the same over a year.
- Minor bump (new user-visible behaviour, no migration) and a changelog line.

**The looks** (owner-approved after renders; the Pirate set was dropped): Sieste royale, Grasse
matinée (doudou only), Soleil-Levant (indigo kanzashi and éventail), Gentlewoman (red ribbon),
Coquette (yellow balloon, left hand), L'Immortelle; seasonal: Hiver (all blue), Printemps (yellow
tulip), Été, Automne (scarf high). The data is `MascotWardrobe.All`.
