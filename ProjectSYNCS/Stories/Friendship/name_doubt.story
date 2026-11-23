# arc: name_doubt
title: Le prénom
family: friendship
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation acquaintances]
cooldown: 7d

## scene: doubt
title: Le prénom
pace: now

Sur le chemin du retour, {A} a un doute affreux : pendant la visite, était-ce bien « {B} » à chaque fois ? Une fois au moins, il y avait une syllabe de trop. {A} en est presque sûr{a:|e}.

* courge | Demander à la Courge, qui connaît tout le monde
  [lean sociability +2]
  La Courge connaît le prénom de {B}, ceux de ses voisins, et ce que {B} a mangé mardi. Elle le répète trois fois, pour que ça rentre. Ça rentre.
  [modifier well_spoken] [end]

* practice | Répéter le prénom jusqu'à le savoir
  [lean rationality +1] [lean energy -1]
  {A} répète « {B}, {B}, {B} » tout le long du chemin, au point que les grenouilles des Mouilles le savent aussi. Ce prénom-là, {A} ne l'oubliera plus jamais.
  [grow learning] [end]

* ask | Aller le lui demander, franchement
  [stress shy 15]
  [lean honor +1] [lean boldness +1]
  {A} revient sur ses pas et demande, franchement. {B} éclate de rire : pendant toute la visite, {B} se posait exactement la même question. On repart chacun avec le bon prénom.
  [affinity +5] [modifier clear_conscience] [end]
