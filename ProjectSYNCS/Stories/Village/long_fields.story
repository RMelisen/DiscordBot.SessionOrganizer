// Une journée aux Longues Raies — wave 2 (adulte only), new. The Noyer hires day-labourers; the Ortie, his
// best, tells him her four truths (fiches 9, 10). Journalier is a trade (lore § 6).

# arc: long_fields
title: Une journée aux Longues Raies
family: village
stage: adult
trigger: draw
size: short

## memories
ortie.sided_with_her | ortie | private | +2 | Lui a donné raison devant le Noyer, aux Longues Raies. Elle a rendu le service en double, comme toujours.
noyer.sided_against  | noyer | private | -1 | A donné raison à l'Ortie contre lui, aux Longues Raies, devant tout le monde.

## scene: fields
title: Une journée aux Longues Raies
pace: now

Le Noyer embauche à la journée, aux Longues Raies. L'Ortie, sa meilleure journalière, lui tient tête depuis le matin : la raie du bas est mal drainée, et elle a raison.
Le Noyer se tourne vers {A}. « Le Noyer voudrait un avis. »

* work | Travailler dur, et ne rien dire
  [stress lazy 15]
  [lean energy +2]
  {A} travaille jusqu'au soir sans prendre parti. Le Noyer paie bien, et en public, pour qu'on le dise.
  [cailloux 20] [end]

* ortie | Donner raison à l'Ortie, devant lui
  [lean honor +2]
  « La raie du bas est mal drainée. » Le Noyer gonfle, dégonfle, et fait creuser une rigole. L'Ortie ne dit rien, mais elle porte le panier de {A} jusqu'au soir.
  [remember ortie.sided_with_her] [remember noyer.sided_against] [end]

* nap | Faire la sieste sous le Noyer  [needs trait lazy]
  [lean energy -2]
  {A} s'allonge dans l'ombre du Noyer, qui est la meilleure de la vallée. Le Noyer, au lieu de se fâcher, en rit, et le raconte pendant un mois.
  [stress -20] [modifier well_rested] [end]
