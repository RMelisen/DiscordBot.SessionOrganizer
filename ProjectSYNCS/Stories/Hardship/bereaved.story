// Replaces on_bereaved.
# arc: bereaved
title: Une place vide
family: hardship
stage: baby, teen, adult, elder
trigger: death
size: short
other: anyone

## scene: empty
title: Une place vide
was: on_bereaved
pace: now

La place de {B} est vide, et le hameau est un peu plus grand sans. {A} ne sait pas trop quoi faire de ses mains.

* remember | Se souvenir des bons moments
  [lean sociability +1]
  {A} raconte à voix haute leurs meilleures bêtises. La Mère Lichen écoute, sans rien dire, et pose sa main sur celle de {A}.
  [stress -10] [end]

* plant | Planter une graine en sa mémoire
  [lean rationality +1]
  {A} plante une graine près du Pont-Vieux. Au printemps, quelque chose poussera.
  [grow stewardship] [end]
