// Les jumelles du Communal — wave 1 (bébé), new. The twins repeat on the square what the Tilleul tells
// his bees, all wrong (lore § 12); the Courge's gossip improves everything, kindly (fiche 6).

# arc: twins_rumour
title: Les jumelles du Communal
family: village
stage: baby
trigger: draw
size: short

## scene: square
title: Les jumelles du Communal
pace: now

Sur la place du Puits, les jumelles du Communal répètent à tout le monde ce que le Tilleul a dit à ses abeilles ce matin. Tout de travers.
À les entendre, {A} a avalé le puits.

* correct | Rétablir la vérité, calmement
  [lean honor +2]
  {A} explique : pas le puits, une pomme du puits. Enfin, une pomme tombée près du puits. Les jumelles écoutent avec beaucoup d'attention.
  Le lendemain, à les entendre, {A} a avalé une pomme grosse comme le puits. C'est un progrès.
  [grow diplomacy] [end]

* embellish | En rajouter : le puits, et le seau
  [lean boldness +2]
  « Et le seau. Et la corde. » Les jumelles repartent en courant.
  À midi, la Courge raconte à toute l'auberge que {A} a bu le puits pour sauver une grenouille. On applaudit.
  [modifier well_spoken] [end]

* chase | Leur courir après  [needs trait rowdy]
  [lean energy +2]
  {A} poursuit les jumelles trois fois autour du puits, une fois dans le lavoir, et une fois dans les jupes de la tisserande. Tout le monde finit par rire, même la tisserande.
  [modifier light_heart] [end]
