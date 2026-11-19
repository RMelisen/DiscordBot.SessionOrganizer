// Le galet du tas — wave 2 (ancien only), new. The Mère Lichen's pile: one pebble per pousse she welcomed,
// no names, she knows which is which, and a pousse grown up may come for its own (fiche 1). Reads
// Arrival's keepsake and epithet.

# arc: pile_pebble
title: Le galet du tas
family: village
stage: elder
trigger: draw
size: short

## memories
lichen.sat_by_the_pile | lichen | private | +3 | Est revenu{a:|e} au tas de galets, {a:très vieux|très vieille}, et s'est assis{a:|e} à côté d'elle sans rien prendre.

## scene: pile
title: Le galet du tas
pace: now

Devant la cabane de la Lisière, le tas de galets a grandi : un par pousse que la Mère Lichen a vue sortir. Aucun ne porte de nom. Elle sait lequel est qui.
Une pousse devenue grande peut venir chercher le sien. {A} est devenu{a:|e} très grand{a:|e}.
[if keepsake lichen_pebble] « Le tien est déjà dans ta poche. Tu es venu{a:|e} pour rien. Tant mieux. »
[if epithet lisiere_child] « Tu sais où c'est, chez toi. Je te l'avais dit. »

* take | Prendre son galet
  [lean greed +1]
  La Mère Lichen tend la main sans regarder, et sort du tas un galet, un seul, lisse et tiède. Ce n'est peut-être pas le bon. C'est le bon.
  [item col.galet] [end]

* sit | Le laisser, et s'asseoir à côté d'elle
  [lean compassion +2]
  {A} s'assoit sur la pierre, à côté d'elle. {a:Ils|Elles} regardent la Lisière, comme le premier jour. La Souche n'est pas pressée. {a:Eux|Elles} non plus.
  [stress -15] [remember lichen.sat_by_the_pile] [end]

* add | Ajouter un galet neuf, pour la prochaine  [mystery]
  [lean zeal +2]
  {A} pose un galet de la Sommeille sur le tas, tout en haut. La Mère Lichen le regarde longtemps. « Pas encore. Mais bientôt. »
  Elle le laisse là. Il ne roule pas.
  [modifier inspired] [end]
