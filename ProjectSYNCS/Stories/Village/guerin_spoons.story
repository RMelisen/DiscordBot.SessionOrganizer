// Les cuillères de Guérin — wave 2 (adulte, ancien). Rewrites grown_spoons: Guérin, the Ortie's
// husband, carves more spoons than will ever sell, each with a villager's head; one recognises oneself,
// not always with pleasure (fiche 10, lore § 12).

# arc: guerin_spoons
title: Les cuillères de Guérin
family: village
stage: adult, elder
trigger: draw
size: short

## memories
ortie.spoon_order | ortie | private | +2 | A commandé à Guérin une cuillère pour la Courge. Elle a fait une tarte, en échange, sans dire merci.

## scene: spoon
title: Les cuillères de Guérin
pace: now

Sur le banc devant chez l'Ortie, Guérin taille des cuillères. Chacune a une tête : le Noyer, la Courge, le Chardon, très ressemblant.
Celle d'aujourd'hui a la tête de {A}. Le nez est très réussi. Très grand, aussi.

* buy | L'acheter, et rire
  [lean energy +1]
  {A} achète la cuillère et rit si fort que Guérin rit aussi, ce qui n'arrive jamais. Elle sert pour la soupe, et un peu pour se regarder.
  [modifier light_heart] [end]

* nose | Lui demander un nez plus petit
  [lean rationality +1]
  Guérin écoute, très sérieux, et taille une nouvelle cuillère. Le nez est encore plus grand. « Elle te ressemble mieux », dit-il. Il n'a pas tort.
  [grow diplomacy] [end]

* order | En commander une pour la Courge
  [lean compassion +2]
  Guérin taille une Courge qui rit, avec des vrilles jusqu'au manche. Le lendemain, l'Ortie dépose une tarte devant chez {A}, sans un mot. C'est sa façon.
  [remember ortie.spoon_order] [end]
