# arc: same_tune
title: L'air qui reste
family: friendship
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation friends]
cooldown: 7d

## scene: tune
title: L'air qui reste
pace: now

Le lendemain de la visite, {A} se surprend à fredonner un air que {B} chantonnait sans y penser. Il revient au puits, au four, en se lavant les mains. Impossible de s'en débarrasser.

* hum | Le fredonner partout, tant pis
  [lean sociability +1] [lean energy +1]
  {A} le fredonne partout, et à midi, la tisserande le fredonne aussi, puis Pain-Blond, puis la moitié de la place. Personne ne sait d'où il vient. {A}, si.
  [modifier light_heart] [end]

* origin | Demander au Tilleul d'où il vient
  [lean rationality +2]
  Le Tilleul reconnaît l'air dès la troisième note : une chanson de la fondation, dit-il, ou du grand hiver, ou des deux. Il en chante quatre couplets, tous différents de la fois d'avant.
  [grow learning] [end]

* sing | Aller le chanter en entier
  [stress shy 10]
  [lean compassion +1] [lean boldness +1]
  {A} va chanter l'air à {B}, en entier, avec des paroles inventées en route. {B} rit, reconnaît l'air, et ne se souvient pas de l'avoir chanté. Maintenant, il est à {A} et à {B}.
  [affinity +5] [modifier inspired] [end]
