# arc: mixed_names
title: Les prénoms mélangés
family: friendship
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation bestfriends]
cooldown: 7d

## scene: names
title: Les prénoms mélangés
pace: now

Depuis cette visite, le Tilleul appelle {A} « {B} ». Il appelle aussi {B} « {A} ». Il s'excuse chaque fois avec une pomme, puis recommence. Ça commence à se savoir dans tout le hameau.

* answer | Répondre quand même, avec le sourire
  [lean compassion +1] [lean energy -1]
  {A} répond quand même, avec le sourire. Le Tilleul est ravi, et donne une pomme pour la peine. Le soir, {A} a cinq pommes et un deuxième prénom.
  [modifier light_heart] [end]

* correct | Le corriger, gentiment
  [lean honor +1] [lean rationality +1]
  {A} corrige le Tilleul, gentiment, en articulant. Il hoche la tête, s'excuse avec une pomme, et dit : « Vous êtes comme les deux doigts d'une main. » Il ne précise pas lesquels.
  [grow diplomacy] [end]

* tell | Courir le raconter
  [lean sociability +1] [lean energy +1]
  {A} court le raconter à {B}, qui a eu droit au même traitement, et à autant de pommes. On fait un tas avec toutes les pommes, et on décide que, pour le Tilleul, ça ne change rien. Pour {A} et {B} non plus.
  [affinity +5] [modifier cherished] [end]
