# The Insectes collection — design

A sixth collection set, pictured with Unicode emoji and found like the other small sets.

## Decisions

- **Pictures:** Unicode emoji — no sprites, nothing to upload.
- **Finding:** the insects join the existing finds (forage, the daily gift, won games, good
  visits) in the non-mushroom half of `ItemCatalog.DrawCollectible` / `DrawForage`, drawn by
  rarity like Cailloux or Nature. No new command.
- **Size and reward:** 8 items, 3 commun / 2 peu commun / 2 rare / 1 légendaire, all year;
  completing the set pays **250 cailloux**, once.

## The set

`CollectionSet("insectes", "🐞", "Insectes", 250)`, after Saisons and before Champignons in
`ItemCatalog.Sets`, so the book's menu reads small sets first, then the big one.

| Key | Emoji | Name | Rarity |
|---|---|---|---|
| `col.fourmi` | 🐜 | Fourmi | commun |
| `col.mouche` | 🪰 | Mouche | commun |
| `col.moustique` | 🦟 | Moustique | commun |
| `col.chenille` | 🐛 | Chenille | peu commun |
| `col.coccinelle` | 🐞 | Coccinelle | peu commun |
| `col.grillon` | 🦗 | Grillon | rare |
| `col.abeille` | 🐝 | Abeille | rare |
| `col.morpho_bleu` | 🦋 | Morpho bleu | légendaire |

Keys are stored and never renamed.

## Effects

- 70 collectibles instead of 62; 6 sets instead of 5. The non-mushroom half now holds 40 items,
  so each existing one turns up slightly less often; the mushroom share (60 % of forages, 10 %
  elsewhere) is unchanged.
- The book's menu gains an option (7 of 25); the book and `/inventory view` each gain a set.

## Checks

- 70 collectibles, 6 sets, the insects' 3/2/2/1 rarities, unique keys and names.
- Insects are drawn by both `DrawCollectible` and `DrawForage`, never in the Champignons half.
- Completing the set pays 250 once.
- The book (every page) and the inventory stay within Discord's embed caps with every item held.

## Docs

`/plynling help` (« 5 collections de 8 objets et une grande de 30 champignons »), README,
CLAUDE.md (every « 62 » and « five sets »).
