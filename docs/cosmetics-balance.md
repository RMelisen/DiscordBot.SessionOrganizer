# Cosmetics — balancing sheet

Snapshot of `CosmeticCatalog` at the **Gentleman batch** (2026-11-25): 117 cosmetics, six tiers. Forage counts
come from the checks' simulation (section 61, `CraftingChecks.ExpectedForages`: mean of 300 aimed
runs from nothing). Working sheet, not a rule file — regenerate it after a catalog change.

**Partly stale:** sections 3–5 and 7 have the rows of 8.22.0's eight newly crafted pieces updated
(Le Gourmet, Petite couronne, Chapeau-champignon, Mouches, Étoiles filantes, Plumes, Bonhomme de
neige, Guirlande de fleurs — ex « Couronne de fleurs »); the counts, odds and totals of section 2
are still the Gentleman snapshot (Soleil-Levant and batch 3 are not in them either).

**Collections (section 7) are in the code**: a label only, each cosmetic in at most one, shown only
on `/inventory view`'s garde-robe page.

Where to change things: tiers and prices `ProjectSYNCS/Helpers/Economy/CosmeticCatalog.cs`
(`TierPrice`, `RotatingCount`, the `Build()` lines); collections
`ProjectSYNCS/Helpers/Economy/CosmeticCollections.cs` (name and keys; nothing stored, so free to
change); the rules that pin them `tests/ProjectSYNCS.Checks/CraftingChecks.cs` (`Rarities`,
`TrophyPrice`, `Bands`, `Collections`).

## 1. Price rules

| Tier | Shop price (rotating and seasonal) | Days of income¹ | Who has it |
|---|---|---|---|
| Commun | 600 | ~3 | rotating; the 8 basics read commun |
| Peu commun | 1 200 | ~7 | rotating, seasonal titles and accessories, all Ouvrages |
| Rare | 2 400 | ~13 | rotating, seasonal thèmes, auras and cadre |
| **Épique** | 3 600 | ~20 | rotating only |
| Légendaire | 5 000 | ~28 | rotating only |
| **Mythique** | never sold | — | the 14 Chefs-d'œuvre |

| Other prices | |
|---|---|
| Basique, titre / accessoire | 300 (two per slot, except Aura: none) |
| Basique, thème / cadre | 500 (two per slot) |
| Ouvrage | 100 cailloux + its ingredients (16–24 forages) |
| Chef-d'œuvre | 200 under 90 forages, 300 for 90–115, 400 above 115 (+ ingredients) |

¹ About 180 cailloux a day for an active player: 3 `/work` shifts (25–40 each) ≈ 100, `/daily`
≈ 50, passive XP cap 29. An estimate, not measured: compare with `/admin dashboard`. Forage cooldown
4 h → at most 6 forages a day.

Shop each week: the basics + **2 rotating per slot, 1 for the Aura** (seeded on the ISO week) + the
season's items. Crafted items are never sold; cosmetics can't be sold back.

## 2. Counts

### Slot × tier

| Slot | Commun | Peu commun | Rare | Épique | Légendaire | Mythique | **Total** |
|---|---|---|---|---|---|---|---|
| Thème | 6 | 5 | 6 | 1 | 1 | 2 | **21** |
| Titre | 10 | 13 | 2 | 2 | 2 | 5 | **34** |
| Accessoire | 6 | 12 | 2 | 1 | 1 | 4 | **26** |
| Aura | 1 | 2 | 4 | 1 | 1 | 1 | **10** |
| Cadre | 9 | 10 | 3 | 1 | 1 | 2 | **26** |
| **Total** | **32** | **42** | **17** | **6** | **6** | **14** | **117** |

### Slot × source

| Slot | Basique | Rotation | Saison | Ouvrage | Chef-d'œuvre | **Total** |
|---|---|---|---|---|---|---|
| Thème | 2 | 11 | 4 | 2 | 2 | **21** |
| Titre | 2 | 20 | 2 | 5 | 5 | **34** |
| Accessoire | 2 | 11 | 6 | 3 | 4 | **26** |
| Aura | — | 6 | 3 | — | 1 | **10** |
| Cadre | 2 | 17 | 1 | 4 | 2 | **26** |
| **Total** | **8** | **65** | **16** | **14** | **14** | **117** |

### Rotation odds

| Slot | Pool | Drawn a week | Chance a given item is on sale | Mean wait |
|---|---|---|---|---|
| Thème | 11 | 2 | 18.2 % | ~5.5 weeks |
| Titre | 20 | 2 | 10 % | ~10 weeks |
| Accessoire | 11 | 2 | 18.2 % | ~5.5 weeks |
| Aura | 6 | 1 | 16.7 % | ~6 weeks |
| Cadre | 17 | 2 | 11.8 % | ~8.5 weeks |

### What owning everything costs (cailloux only)

| Group | 8.18.1 | 8.19.0 | Gentleman batch |
|---|---|---|---|
| Basics (8) | 1 600 | 1 600 | 3 200 |
| Rotating (65) | 82 600 | 85 400 | 111 600 |
| Seasonal (16) | 38 400 | 28 800 | 28 800 |
| Ouvrages (14) | 1 400 | 1 400 | 1 400 |
| Chefs-d'œuvre (14) | 3 400 | 3 600 | 3 800 |
| **Total** | **127 400** | **120 800** (~670 days of income) | **148 800** (~830 days of income) |

## 3. Shop catalogue (Basique + Rotation)

### Thèmes

| Item | Source | Tier | Price | Collection |
|---|---|---|---|---|
| 🌿 Prairie | Basique | commun | 500 | Le Communal |
| 🌤️ Ciel | Basique | commun | 500 | — |
| 🌸 Aurore | Rotation | commun | 600 | Jardin en fleurs |
| 🌊 Océan | Rotation | commun | 600 | Au fil de la Sommeille |
| 🔥 Braises | Rotation | commun | 600 | Veillée d'automne |
| 🌲 Forêt | Rotation | commun | 600 | Cœur de la Lisière |
| 💜 Lavande | Rotation | peu commun | 1 200 | Tout doux |
| 🌵 Désert | Rotation | peu commun | 1 200 | — |
| 🌈 Arc-en-ciel | Rotation | peu commun | 1 200 | Jour de fête |
| ✨ Nuit étoilée | Rotation | rare | 2 400 | Ciel de nuit |
| 🍷 Velours | Rotation | rare | 2 400 | Gentleman |
| 🌌 Aurore boréale | Rotation | épique | 3 600 | Ciel de nuit |
| 👑 Royal | Rotation | légendaire | 5 000 | Cour royale |

### Titres

| Item | Source | Tier | Price | Collection |
|---|---|---|---|---|
| Le Petit / La Petite | Basique | commun | 300 | Tout doux |
| Le Gentil / La Gentille | Basique | commun | 300 | — |
| Le Rêveur / La Rêveuse | Rotation | commun | 600 | Ciel de nuit |
| Le Curieux / La Curieuse | Rotation | commun | 600 | Carnet du naturaliste |
| Le Dormeur / La Dormeuse | Rotation | commun | 600 | Ciel de nuit |
| Le Grognon / La Grognonne | Rotation | commun | 600 | — |
| Le Câlin / La Câline | Rotation | commun | 600 | Tout doux |
| Le Distrait / La Distraite | Rotation | commun | 600 | — |
| Le Studieux / La Studieuse | Rotation | commun | 600 | — |
| Le Mystérieux / La Mystérieuse | Rotation | peu commun | 1 200 | Gentleman |
| Le Farceur / La Farceuse | Rotation | peu commun | 1 200 | Jour de fête |
| L'Aventurier / L'Aventurière | Rotation | peu commun | 1 200 | Chasse au trésor |
| Le Bavard / La Bavarde | Rotation | peu commun | 1 200 | Jour de fête |
| Le Timide / La Timide | Rotation | peu commun | 1 200 | Tout doux |
| Le Distingué / La Distinguée | Rotation | peu commun | 1 200 | Gentleman |
| Le Champion / La Championne | Rotation | rare | 2 400 | — |
| Le Fripon / La Friponne | Rotation | rare | 2 400 | Gentleman |
| Le Sage / La Sage | Rotation | épique | 3 600 | — |
| L'Immortel / L'Immortelle | Rotation | épique | 3 600 | — |
| Le Légendaire / La Légendaire | Rotation | légendaire | 5 000 | Cour royale |
| Le Magnifique / La Magnifique | Rotation | légendaire | 5 000 | Gentleman |

### Accessoires

| Item | Source | Tier | Price | Collection |
|---|---|---|---|---|
| 🎀 Nœud | Basique | commun | 300 | Tout doux |
| ☕ Tasse de café | Basique | commun | 300 | — |
| 🧣 Écharpe | Rotation | commun | 600 | Grand-Froid |
| 🌼 Fleur | Rotation | commun | 600 | Jardin en fleurs |
| 🧢 Casquette | Rotation | commun | 600 | — |
| 👓 Lunettes rondes | Rotation | commun | 600 | — |
| 🎈 Ballon | Rotation | peu commun | 1 200 | Jour de fête |
| 🕶️ Lunettes de soleil | Rotation | peu commun | 1 200 | Grandes Vacances |
| 🎀 Nœud papillon | Rotation | peu commun | 1 200 | Gentleman |
| 🎩 Haut-de-forme | Rotation | rare | 2 400 | Gentleman |
| 🧐 Monocle | Rotation | rare | 2 400 | Gentleman |
| 🦯 Canne à pommeau | Rotation | épique | 3 600 | Gentleman |

### Auras

| Item | Source | Tier | Price | Collection |
|---|---|---|---|---|
| 🫧 Bulles | Rotation | peu commun | 1 200 | Jour de fête |
| 🎊 Confettis | Rotation | peu commun | 1 200 | Jour de fête |
| ✨ Scintillements | Rotation | rare | 2 400 | Ciel de nuit |
| ⚡ Éclairs | Rotation | épique | 3 600 | — |

### Cadres de tombe

| Item | Source | Tier | Price | Collection |
|---|---|---|---|---|
| 🕯️ Bougies | Basique | commun | 500 | — |
| 🌼 Pâquerettes | Basique | commun | 500 | — |
| 🍂 Feuilles | Rotation | commun | 600 | Veillée d'automne |
| 🪨 Galets | Rotation | commun | 600 | Au fil de la Sommeille |
| 🌾 Blé | Rotation | commun | 600 | Le Communal |
| ☁️ Nuages | Rotation | commun | 600 | Tout doux |
| 🐚 Coquillages | Rotation | commun | 600 | Grandes Vacances |
| ⭐ Étoiles | Rotation | peu commun | 1 200 | Ciel de nuit |
| 🕊️ Colombes | Rotation | peu commun | 1 200 | — |
| 🦋 Papillons | Rotation | peu commun | 1 200 | Jardin en fleurs |
| 🫧 Bulles de savon | Rotation | peu commun | 1 200 | Jour de fête |
| 💕 Cœurs | Rotation | peu commun | 1 200 | Tout doux |
| 🌹 Roses rouges | Rotation | peu commun | 1 200 | Gentleman |
| 🌙 Clair de lune | Rotation | rare | 2 400 | Ciel de nuit |
| 🎶 Notes de musique | Rotation | rare | 2 400 | Jour de fête |
| 🏮 Lanternes | Rotation | épique | 3 600 | Veillée d'automne |
| ✨ Halo doré | Rotation | légendaire | 5 000 | Cour royale |

## 4. Seasonal (on sale only in their season, priced by tier)

| Season | Thème (rare) | Titre (peu commun) | Accessoire (peu commun) | Aura (rare) | Cadre (rare) | Whole season | Season-locked crafts |
|---|---|---|---|---|---|---|---|
| 🌸 Printemps (Mar–May) | 🍒 Cerisiers | — | 🌷 Tulipe | — | — | 3 600 | Aura Pétales ★, Le Gourmet ★, Couronne de fleurs, Guirlande de fleurs |
| ☀️ Été (Jun–Aug) | 🏖️ Plage | Le Bronzé | 🍦 Glace, 👒 Chapeau de paille | 🌟 Lucioles d'été | — | 8 400 | Collier de coquillages ★ |
| 🍁 Automne (Sep–Nov) | 🍂 Feuilles mortes | — | 🪔 Petite lanterne | 🍁 Feuilles mortes | — | 6 000 | — |
| ❄️ Hiver (Dec–Feb) | ❄️ Flocons | Le Frileux | 🧤 Moufles, 🧶 Bonnet | ❄️ Flocons | — | 8 400 | Cadre Cristaux ★, Bonhomme de neige |

★ = Chef-d'œuvre. Thème, aura and cadre are 2 400; titre and accessoire 1 200. Since 8.22.0 no
cadre is sold in a season (the Bonhomme de neige is crafted). Every seasonal item
is in its season's collection: Jardin en fleurs, Grandes Vacances, Veillée d'automne, Grand-Froid.

## 5. Crafting recipes

Ingredient rarity: **C** commun, **U** peu commun, **R** rare, **L** légendaire (collectibles keep
four tiers). « any X » = any commons of that set. Places: Sommeille (cailloux, coquillage, cristal de
givre), Lisière (champignons, châtaigne), Mouilles (insectes), Communal (nature, primevère, cerise,
coquelicot, flocon), Puits (trésors, citrouille). Sell value = what the ingredients would fetch at
2 / 5 / 15 / 50.

### Ouvrages — peu commun, 100 cailloux, band 10–25 forages

| Slot | Item | Recipe | Place | Forages | Sell value | Collection |
|---|---|---|---|---|---|---|
| Thème | 🏞️ Rivière | Galet ×2 C, Caillou plat ×1 C, Quartz ×1 U | Sommeille | 22 | 11 | Au fil de la Sommeille |
| Thème | 🌳 Sous-bois | any champignon ×6 C | Lisière | 16 | 12 | Cœur de la Lisière |
| Titre | Le Glaneur | any nature ×5 C | Communal | 16 | 10 | Le Communal |
| Titre | Le Brocanteur | Bouton ×2 C, Bille ×2 C, Clé rouillée ×1 C | Puits | 23 | 10 | Chasse au trésor |
| Titre | Le Sommeillant | Silex ×2 C, Galet ×2 C, Agate ×1 U | Sommeille | 24 | 13 | Au fil de la Sommeille |
| Titre | Le Mouillé | Mouche ×2 C, Moustique ×2 C, Chenille ×1 U | Mouilles | 23 | 13 | Carnet du naturaliste |
| Titre | Le Puisatier | Clé rouillée ×2 C, Bille ×2 C, Pièce ancienne ×1 U | Puits | 23 | 13 | Chasse au trésor |
| Accessoire | 🫙 Bocal à insectes | Fourmi ×2 C, Mouche ×1 C, Coccinelle ×1 U | Mouilles | 22 | 11 | Carnet du naturaliste |
| Accessoire | 🎣 Petite canne à pêche | Silex ×2 C, Caillou plat ×2 C, Galet ×1 C | Sommeille | 22 | 10 | Au fil de la Sommeille |
| Accessoire | 💐 Couronne de fleurs | Primevère ×1 C (printemps), any nature ×3 C | Communal | 16 | 8 | Jardin en fleurs |
| Accessoire | 🍄 Chapeau-champignon | any champignon ×5 C | Lisière | 13 | 10 | Cœur de la Lisière |
| Aura | 🪰 Mouches | Mouche ×3 C, Moustique ×1 C | Mouilles | 24 | 8 | — |
| Cadre | 💫 Lucioles | Moustique ×2 C, Fourmi ×1 C, Chenille ×1 U | Mouilles | 20 | 11 | Ciel de nuit |
| Cadre | 🌰 Glands | any nature ×4 C, Pomme de pin ×1 U | Communal | 16 | 13 | Cœur de la Lisière |
| Cadre | 🌿 Mousse | any caillou ×6 C | Sommeille | 16 | 12 | Au fil de la Sommeille |
| Cadre | 🌾 Roseaux | any insecte ×6 C | Mouilles | 16 | 12 | Carnet du naturaliste |
| Cadre | 🪶 Plumes | Plume ×1 C, Feuille de chêne ×1 C | Communal | 19 | 4 | Le Communal |
| Cadre | ⛄ Bonhomme de neige | Flocon ×1 C (hiver), any nature ×2 C | Communal | 14 | 6 | Grand-Froid |
| Cadre | 💐 Guirlande de fleurs | Primevère ×1 C (printemps), Trèfle ×1 C | Communal | 17 | 4 | Jardin en fleurs |

The Guirlande de fleurs was the « Couronne de fleurs » cadre until 8.22.0 (key
`cos.grave.couronne_fleurs` kept): a crafted name must be unique, and the accessory has it.

### Chefs-d'œuvre — mythique, band 60–150 forages, cailloux by effort

| Slot | Item | Recipe | Places | Forages | Cailloux | Sell value | Collection |
|---|---|---|---|---|---|---|---|
| Thème | 🍄 Champignonnière | Girolle ×1 U, Coprin chevelu ×2 C, Cèpe de Bordeaux ×1 R | Lisière | 118 | 400 | 24 | Cœur de la Lisière |
| Thème | 💎 Caverne aux trésors | Quartz ×3 U, Améthyste ×2 R, Opale ×1 R | Sommeille | 75 | 200 | 60 | Chasse au trésor |
| Titre | Le Chercheur de trésors | Carte au trésor ×1 R, Boussole ×1 R, Pièce ancienne ×5 U | Puits | 71 | 200 | 55 | Chasse au trésor |
| Titre | Le Botaniste | Feuille de chêne ×3 C, Trèfle ×3 C, Trèfle à quatre feuilles ×2 R | Communal | 74 | 200 | 42 | Carnet du naturaliste |
| Titre | Le Naturaliste | Morpho bleu ×1 L, Coccinelle ×2 U, Chenille ×2 U | Mouilles | 86 | 200 | 70 | Carnet du naturaliste |
| Titre | Le Mycologue | Lactaire indigo ×1 R, Pholiote dorée ×1 U, Polypore soufré ×1 U | Lisière | 123 | 400 | 25 | Cœur de la Lisière |
| Titre | L'Entomologiste | Grillon ×2 R, Abeille ×1 R, Coccinelle ×2 U | Mouilles | 73 | 200 | 55 | Carnet du naturaliste |
| Titre | Le Gourmet | Morille conique ×1 R (printemps), Cèpe de Bordeaux ×1 R, Girolle ×1 U | Lisière | 144 | 400 | 35 | — |
| Accessoire | 👑 Petite couronne | Couronne ×1 L, Pièce ancienne ×3 U, Améthyste ×1 R | Puits + Sommeille | 116 | 400 | 80 | Cour royale |
| Accessoire | 🪶 Plume d'or | Plume ×5 C, Plume dorée ×1 L | Communal | 102 | 300 | 60 | Le Communal |
| Accessoire | 🐚 Collier de coquillages | Coquillage ×2 R (été), Galet ×3 C | Sommeille | 101 | 300 | 36 | Grandes Vacances |
| Accessoire | 📿 Médaillon ancien | Couronne ×1 L, Bague ×2 U | Puits | 85 | 200 | 60 | Cour royale |
| Accessoire | 🕰️ Montre à gousset | Boussole ×2 R, Pièce ancienne ×3 U, Bague ×2 U | Puits | 71 | 200 | 55 | Gentleman |
| Aura | 🌸 Pétales | Primevère ×8 C, Cerise ×2 R (printemps) | Communal | 110 | 300 | 46 | Jardin en fleurs |
| Aura | 🌠 Étoiles filantes | Météorite ×1 L, Quartz ×3 U, Opale ×1 R | Sommeille | 91 | 300 | 80 | Ciel de nuit |
| Cadre | 💎 Cristaux | Cristal de givre ×2 R, Flocon ×3 C (hiver) | Sommeille + Communal | 127 | 400 | 36 | Grand-Froid |
| Cadre | 🍄 Cercle de fées | Russule dorée ×1 U, Trompette de la mort ×1 R | Lisière | 111 | 300 | 20 | Cœur de la Lisière |

## 6. Open points for the next pass

1. **Épique and légendaire are still the thinnest tiers**: 6 items each, though every slot now has
   at least one of each. The Aura slot now has commun, épique and légendaire (6 rotating items).
2. **The tiers are lopsided**: 42 peu commun against 17 rare. Titres hold 13 peu commun and only 2 rare.
3. **Titles and cadres still rotate slowly** (a given title every ~10 weeks, a cadre ~8.5);
   accessories and thèmes every ~5.5, auras every ~6.
4. **Spring has the thinnest seasonal shop** (2 items, 3 600 for the lot, against 10 800 in winter).
   Spring and autumn have no seasonal title; only winter has a seasonal cadre.
5. **The « any commons » Ouvrages all sit at 16 forages**; the named ones take 20–24 for the same 100.
6. **Four Chefs-d'œuvre sit near a price line**: Champignonnière (118) and Cercle de fées (111) are
   3–4 forages from the 115 line, the Petite couronne (116) one above it and Étoiles filantes (91)
   one above the 90 line. A small retune of any of them — or of the draw — moves its price, and the
   checks will say so.
7. **Basics still split by slot** (300 / 500) and read « commun » while costing less than a commun.
   There are now two per slot, except Aura (8 in all, 3 200 cailloux for the lot).
8. ~~`/inventory view`'s garde-robe page broke for a big collector~~ — fixed in 8.19.0: a slot past
   1 024 continues in « (suite) » fields, and a wardrobe too big for one embed shows icons only
   (everything owned: 5 043 characters). Checks section 23 measures it.
9. **The garde-robe page is close to its cap again**: with everything owned and every icon uploaded,
   `/inventory view`'s page is already in its icons-only fallback and measures 5 818 of 6 000
   characters (4 988 before the Gentleman batch, after the baguette left; 5 043 at 8.19.0). About
   five more cosmetics and even the fallback overflows — checks section 23 will fail first. The next
   batch needs a second page for the wardrobe (or a per-slot cap) before it adds items.

## 7. Collections

14 collections hold 99 of the 117 cosmetics; 18 belong to none. Three kinds: **seasonal** (on sale
in one season, with that season's Chef-d'œuvre), **shop** (from the weekly rotation), **forage**
(mostly crafted, named after the places of Humeval). Slots: **Th** thème, **Ti** titre,
**Ac** accessoire, **Au** aura, **Ca** cadre. Cost = shop prices + crafting cailloux; the forage count is
the recipes' means added up (an upper bound: one forage can serve two recipes).

### Overview

| Collection | Kind | Items | Slots | Tiers | Cost | Forages | When |
|---|---|---|---|---|---|---|---|
| Grand-Froid | seasonal | 8 | Th Ti Ac Au Ca | C ×1, U ×3, R ×3, M ×1 | 11 400 | 127 | winter (Écharpe all year) |
| Grandes Vacances | seasonal | 8 | Th Ti Ac Au Ca | C ×1, U ×4, R ×2, M ×1 | 10 500 | 101 | summer (Lunettes, Coquillages all year) |
| Veillée d'automne | seasonal | 6 | Th Ac Au Ca | C ×2, U ×1, R ×2, É ×1 | 10 800 | — | autumn (Braises, Feuilles, Lanternes all year) |
| Jardin en fleurs | seasonal | 8 | Th Ac Au Ca | C ×3, U ×3, R ×1, M ×1 | 7 000 | 126 | spring (Aurore, Fleur, Couronne, Papillons all year) |
| Cour royale | shop | 5 | Th Ti Ac Ca | L ×4, M ×1 | 20 200 | 85 | rotation |
| Ciel de nuit | shop | 9 | Th Ti Au Ca | C ×2, U ×2, R ×3, É ×1, L ×1 | 18 300 | 20 | rotation |
| Gentleman | shop | 11 | Th Ti Ac Ca | U ×4, R ×4, É ×1, L ×1, M ×1 | 23 200 | 71 | rotation |
| Jour de fête | shop | 8 | Th Ti Ac Au Ca | U ×7, R ×1 | 10 800 | — | rotation |
| Tout doux | shop | 7 | Th Ti Ac Ca | C ×4, U ×3 | 5 400 | — | basics + rotation |
| Cœur de la Lisière | forage | 6 | Th Ti Ca | C ×1, U ×2, M ×3 | 1 900 | 384 | all year |
| Au fil de la Sommeille | forage | 6 | Th Ti Ac Ca | C ×2, U ×4 | 1 600 | 84 | all year |
| Carnet du naturaliste | forage | 7 | Ti Ac Ca | C ×1, U ×3, M ×3 | 1 500 | 294 | all year |
| Chasse au trésor | forage | 5 | Th Ti | U ×3, M ×2 | 1 800 | 192 | all year |
| Le Communal | forage | 5 | Th Ti Ac Ca | C ×3, U ×1, M ×1 | 2 100 | 118 | all year |

### Members

| Collection | Thème | Titre | Accessoire | Aura | Cadre |
|---|---|---|---|---|---|
| Grand-Froid | Flocons | Le Frileux | Bonnet, Écharpe, Moufles | Flocons | Bonhomme de neige, Cristaux ★ |
| Grandes Vacances | Plage | Le Bronzé | Chapeau de paille, Glace, Lunettes de soleil, Collier de coquillages ★ | Lucioles d'été | Coquillages |
| Veillée d'automne | Feuilles mortes, Braises | — | Petite lanterne | Feuilles mortes | Feuilles, Lanternes |
| Jardin en fleurs | Cerisiers, Aurore | — | Tulipe, Fleur, Couronne de fleurs | Pétales ★ | Guirlande de fleurs, Papillons |
| Cour royale | Royal | Le Légendaire | Petite couronne ★, Médaillon ancien ★ | — | Halo doré |
| Ciel de nuit | Nuit étoilée, Aurore boréale | Le Rêveur, Le Dormeur | — | Scintillements, Étoiles filantes ★ | Étoiles, Clair de lune, Lucioles |
| Gentleman | Velours | Le Mystérieux, Le Fripon, Le Distingué, Le Magnifique | Haut-de-forme, Nœud papillon, Monocle, Canne à pommeau, Montre à gousset ★ | — | Roses rouges |
| Jour de fête | Arc-en-ciel | Le Farceur, Le Bavard | Ballon | Bulles, Confettis | Bulles de savon, Notes de musique |
| Tout doux | Lavande | Le Petit, Le Câlin, Le Timide | Nœud | — | Cœurs, Nuages |
| Cœur de la Lisière | Forêt, Sous-bois, Champignonnière ★ | Le Mycologue ★ | — | — | Cercle de fées ★, Glands |
| Au fil de la Sommeille | Rivière, Océan | Le Sommeillant | Petite canne à pêche | — | Galets, Mousse |
| Carnet du naturaliste | — | Le Naturaliste ★, L'Entomologiste ★, Le Botaniste ★, Le Mouillé, Le Curieux | Bocal à insectes | — | Roseaux |
| Chasse au trésor | Caverne aux trésors ★ | Le Chercheur de trésors ★, L'Aventurier, Le Brocanteur, Le Puisatier | — | — | — |
| Le Communal | Prairie | Le Glaneur | Plume d'or ★ | — | Blé, Plumes (Ouvrage) |

★ = Chef-d'œuvre (mythique).

### In no collection (18)

| Slot | Items |
|---|---|
| Thème | Ciel, Désert |
| Titre | Le Gentil, Le Gourmet ★, Le Grognon, Le Distrait, Le Studieux, Le Champion, Le Sage, L'Immortel |
| Accessoire | Tasse de café, Lunettes rondes, Casquette |
| Aura | Mouches (Ouvrage), Éclairs |
| Cadre | Bougies, Pâquerettes, Colombes |

### Choices made, to revisit

- **The four seasonal collections take every seasonal item**, plus all-year items that fit (Écharpe,
  Lunettes, Braises…). So they can be gathered piece by piece through the year, finished in season.
- **Every seasonal collection has a Chef-d'œuvre except Veillée d'automne**: autumn has no
  season-locked recipe (point 4 of section 6).
- **Cour royale is the prestige collection**: four légendaires and the Médaillon, 20 200 cailloux.
- **The forage collections are cheap in cailloux and long in forages**: Cœur de la Lisière holds
  three Chefs-d'œuvre (384 forages added up).
- **Mood titles are left out on purpose** (Gourmet, Grognon, Distrait, Studieux) so a personality title doesn't
  pull a theme with it. Le Champion, Le Sage and L'Immortel could make a « Hall des héros » with a few
  new items.
- **Gaps:** with only 10 auras, eight collections have none. Veillée d'automne and Jardin en fleurs
  have no title; Carnet du naturaliste has no thème; Chasse au trésor is only
  thème and titles (no accessory or cadre).

### Decided (8.19.0)

- **The word stays « Collection »**, even though `/inventory collection` is the book of collectible sets.
- **A label only**: nothing is paid or unlocked for owning or wearing a collection.
- **Shown only on `/inventory view`'s garde-robe page**, as one « Collections » field: every
  collection with a piece owned, « Grand-Froid 3/8 », a complete one in bold. It is one line rather
  than a tag on each item because the per-slot fields are already near Discord's 1 024 characters.
