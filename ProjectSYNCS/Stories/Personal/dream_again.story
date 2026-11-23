# arc: dream_again
title: Le rêve de la visite
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: dream
title: Le rêve de la visite
pace: now

Cette nuit, {A} refait la visite en rêve. Tout est pareil, sauf que {B} est grand{b:|e} comme le Pont-Vieux, ne parle qu'en grenouille, et tient absolument à servir le thé.

* back | Se rendormir pour voir la fin
  [lean energy -2]
  {A} referme les yeux très fort et retrouve le rêve là où il s'était arrêté. La fin est excellente. Au réveil, {A} ne se souvient de rien, sauf qu'elle était excellente.
  [modifier well_rested] [end]

* tell | Raconter le rêve à {B}
  [stress shy 10]
  [lean sociability +1] [lean boldness +1]
  {A} raconte le rêve à {B}, grenouille comprise, en imitant. {B} tient bon jusqu'au thé, puis rit si fort qu'il faut s'asseoir.
  [affinity +5] [modifier light_heart] [end]

* draw | Le dessiner avant qu'il s'en aille
  [lean zeal +1] [lean energy +1]
  Au réveil, {A} dessine le rêve au charbon, sur le mur, avant qu'il s'en aille : le pont, la grenouille, le thé. Le dessin est raté, mais le pont est très ressemblant.
  [modifier inspired] [end]
