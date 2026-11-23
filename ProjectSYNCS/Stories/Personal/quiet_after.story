# arc: quiet_after
title: Le calme d'après
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: quiet
title: Le calme d'après
pace: now

La visite avec {B} est finie, et tout est soudain très calme. Il reste deux bols sur la table, un rire qui traîne encore dans l'air, et une grande envie de ne plus rien faire du tout.

* nap | Faire une longue sieste
  [lean energy -2]
  {A} s'allonge « juste un instant » et se réveille à la nuit tombée, reposé{a:|e} comme après une semaine de pluie.
  [stress -15] [modifier well_rested] [end]

* tidy | Ranger en repensant à la visite
  [lean rationality +1] [lean energy +1]
  {A} lave les deux bols en repensant à chaque moment de la visite, dans l'ordre. Le soir, la table est propre, et la journée bien rangée aussi.
  [grow stewardship] [end]

* out | Ressortir aussitôt voir du monde
  [stress shy 10]
  [lean sociability +2]
  {A} ressort aussitôt, file à l'auberge, et raconte la visite à qui veut l'entendre. Ça fait une deuxième visite, en quelque sorte, avec plus de monde.
  [grow diplomacy] [end]
