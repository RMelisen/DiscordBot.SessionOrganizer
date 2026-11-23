# arc: bee_news
title: Le dire aux abeilles
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: bees
title: Le dire aux abeilles
pace: now

Le Tilleul arrête {A} devant son clos. C'est la coutume : toute nouvelle se dit aux abeilles, sinon elles boudent. « Et la visite avec {B}, personne ne leur en a parlé. » Les ruches, en effet, ont l'air vexées.

* all | Tout leur raconter, de bout en bout
  [lean honor +1] [lean sociability +1]
  {A} s'assoit devant les ruches et raconte la visite de bout en bout, sans rien oublier. Les abeilles bourdonnent plus doucement. Le Tilleul, qui écoutait aussi, essuie une larme et dit que c'était sa partie préférée.
  [modifier soothed] [end]

* whisper | Le leur chuchoter, pour que personne n'entende
  [lean sociability -1] [lean rationality +1]
  {A} se penche vers la ruche et chuchote, la main en cornet. Les abeilles écoutent, très attentives. Le Tilleul, lui, n'a rien entendu du tout, et boude un peu à son tour.
  [grow stewardship] [end]

* leave | S'en aller : ce ne sont que des abeilles
  [lean rationality +1] [lean honor -1]
  {A} s'en va. Une abeille suit, jusqu'au puits, jusqu'à la porte, jusqu'au lit, en bourdonnant d'un ton de reproche. Elle ne pique pas. Elle bourdonne. Toute la nuit.
  [modifier sleepless] [end]
