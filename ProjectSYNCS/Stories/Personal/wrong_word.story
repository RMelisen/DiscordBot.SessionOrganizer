# arc: wrong_word
title: La phrase de trop
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: word
title: La phrase de trop
pace: now

En pleine nuit, {A} se réveille avec une phrase en tête : une phrase dite à {B} pendant la visite. Sur le moment, elle semblait drôle. Dans le noir, elle semble beaucoup moins drôle.

* sorry | Aller s'excuser dès l'aube
  [stress arrogant 15]
  [lean honor +1] [lean compassion +1]
  Dès l'aube, {A} va trouver {B} et s'excuse, d'une traite. {B} ne se souvient pas de la phrase. Pas du tout. {B} demande même à l'entendre, pour voir, et la trouve plutôt drôle.
  [affinity +3] [grow diplomacy] [end]

* sleep | Décider que ce n'était rien, et se rendormir
  [lean energy -1] [lean rationality +1]
  {A} décide que ce n'était rien, se retourne, et se rendort. Ce n'était rien, en effet. Il y a des nuits où le noir exagère.
  [modifier well_rested] [end]

* turn | La retourner dans tous les sens jusqu'au matin
  [lean rationality +1] [lean zeal +1]
  {A} retourne la phrase dans tous les sens jusqu'au matin : trop longue, trop courte, mal placée. À l'aube, elle n'a plus aucun sens, ni dans un sens, ni dans l'autre.
  [modifier sleepless] [end]
