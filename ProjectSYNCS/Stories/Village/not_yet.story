// Pas encore — wave 1 (bébé). Rewrites baby_why with the Mère Lichen, who never says « je ne sais
// pas » but « pas encore », and whose one wish is someone sitting by her without asking (fiche 1).

# arc: not_yet
title: Pas encore
family: village
stage: baby
trigger: draw
size: short

## memories
lichen.sat_quiet | lichen | private | +3 | S'est assis{a:|e} à côté d'elle tout un après-midi, sans rien demander.

## scene: why
title: Pas encore
pace: now

Depuis ce matin, {A} suit la Mère Lichen avec des « pourquoi ». Pourquoi la Souche. Pourquoi la pluie. Pourquoi tu ne dis rien.
Elle répond toujours la même chose : « Pas encore. »

* again | Encore un pourquoi, juste un
  [lean rationality +2]
  « Pourquoi tu dis toujours pas encore ? » La Mère Lichen s'arrête. Elle réfléchit si longtemps que {A} a le temps de compter les fourmis.
  « Parce que c'est vrai. » {A} trouve que c'est une très bonne réponse, et la note dans sa tête.
  [grow learning] [end]

* sit | Se taire et s'asseoir à côté d'elle
  [stress curious 10]
  [lean compassion +2]
  {A} s'assoit sur la pierre, à côté d'elle. {a:Ils|Elles} regardent la Lisière. Personne ne dit rien. Le soleil passe d'un arbre à l'autre.
  Le soir, la Mère Lichen pose sa main sur le chapeau de {A}. « Voilà. C'est ça, la réponse. »
  [remember lichen.sat_quiet] [end]

* tilleul | Aller demander au Tilleul  [mystery]
  [lean sociability +2]
  Le Tilleul a mal entendu. Il répond à une tout autre question, longuement, avec des abeilles, un hiver de neige et un grand-père qui n'était peut-être pas le sien.
  {A} rentre sans aucune réponse, la tête pleine d'histoires.
  [modifier inspired] [end]
