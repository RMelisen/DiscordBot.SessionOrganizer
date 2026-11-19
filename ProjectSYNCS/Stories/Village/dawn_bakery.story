// Le fournil à l'aube — wave 2 (adulte only), new. Pain-Blond needs arms before dawn and pays in warm
// bread; he speaks little to people and a lot to the dough (fiche 7). Reads cellar_oven's
// pain_blond.honest_taster.

# arc: dawn_bakery
title: Le fournil à l'aube
family: village
stage: adult
trigger: draw
size: short

## memories
pain_blond.sang_to_dough | pain_blond | private | +2 | A chanté pour faire lever la pâte, au fournil, avant l'aube. Elle a levé.

## scene: bakehouse
title: Le fournil à l'aube
pace: now

Trois heures du matin. Pain-Blond a besoin de bras, et il le dit tout bas, comme on s'excuse. Le fournil est chaud, sombre, et sent si bon que c'en est injuste.
Son père est déjà assis devant la porte, prêt à commenter la croûte.
[if memory pain_blond.honest_taster] Pain-Blond tend à {A} le premier pain de la fournée. « Dis-moi. Franchement. »

* knead | Pétrir
  [lean energy +2]
  {A} pétrit jusqu'à l'aube, les bras dans la pâte jusqu'aux coudes. Pain-Blond paie en cailloux et en pain chaud, et oublie de compter le pain.
  [cailloux 10] [modifier hearty] [end]

* sing | Chanter pour que la pâte lève
  [stress shy 10]
  [lean sociability +2]
  {A} chante tout bas, au-dessus de la pâte. Pain-Blond, d'abord gêné, finit par fredonner aussi. La pâte lève comme jamais. Le père, dehors, n'a rien à redire.
  [remember pain_blond.sang_to_dough] [end]

* deliver | Livrer les miches dans le hameau
  [lean compassion +1]
  {A} porte les miches de porte en porte avant le jour. Chez Bertille, la mère de Pain-Blond, il faut attendre : elle renifle le pain, puis {A}, et sourit.
  [grow diplomacy] [end]
