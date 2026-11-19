// La Longue Nuit — wave 2 (adulte, ancien), seasonal: winter, the banquet of the longest night at the
// Maison commune, and the Tilleul's stories (lore § 8, fiche 12). The Mère Lichen almost never comes down.
// Reads not_yet's lichen.sat_quiet.

# arc: long_night
title: La Longue Nuit
family: village
stage: adult, elder
trigger: draw
size: short
requires: [season winter]

## memories
tilleul.long_night_story | tilleul | private | +2 | A écouté son récit de la fondation jusqu'au bout, la nuit la plus longue. La version avait un loup, cette fois.
lichen.long_night_bench  | lichen  | private | +2 | S'est assis{a:|e} à côté d'elle, la nuit la plus longue, à la Maison commune.

## scene: banquet
title: La Longue Nuit
pace: now

C'est la nuit la plus longue. Tout le village dîne à la Maison commune, sous la neige. Le Tilleul raconte la fondation, une version neuve.
Au bout de la table, la Mère Lichen est venue. Ça n'arrive presque jamais.
[if memory lichen.sat_quiet] Elle tapote le banc à côté d'elle, une fois, en regardant {A}.

* listen | Écouter le Tilleul jusqu'au bout
  [lean compassion +1]
  Cette année, la fondation a un loup, une barque et un grand-père qui chante. {A} écoute jusqu'au bout. Le Tilleul s'en aperçoit, et recommence, de joie.
  [stress -15] [remember tilleul.long_night_story] [end]

* lichen | S'asseoir à côté de la Mère Lichen
  [lean zeal +1]
  {A} s'assoit à côté d'elle. Elle ne dit rien de toute la soirée. Au dessert, elle pose sur l'assiette de {A} la moitié de sa part. « Pas pour moi. Pour toi. »
  [remember lichen.long_night_bench] [end]

* tell | Raconter sa propre histoire
  [stress shy 15]
  [lean sociability +2]
  {A} se lève et raconte une histoire de son enfance, un peu arrangée. Le Tilleul en reprend la moitié dans la sienne, l'année suivante, en l'embellissant.
  [modifier well_spoken] [end]
