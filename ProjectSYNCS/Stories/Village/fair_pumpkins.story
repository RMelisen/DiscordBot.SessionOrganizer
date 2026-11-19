// Les citrouilles de la foire — wave 2 (adulte, ancien), seasonal: spring, the fair (lore § 8). The
// Noyer wants, once, to win the fair's contest without the jury being his family (fiche 9).

# arc: fair_pumpkins
title: Les citrouilles de la foire
family: village
stage: adult, elder
trigger: draw
size: short
requires: [season spring]

## memories
noyer.honest_jury   | noyer | private | +3 | A été juré{a:|e} au concours de la foire, et ne l'a pas fait gagner. Il en est très fier.
noyer.ortie_won     | noyer | private | -1 | A fait gagner la citrouille de l'Ortie au concours de la foire. Il y pense encore.
ortie.pumpkin_prize | ortie | private | +2 | A fait gagner sa citrouille au concours de la foire. Elle n'a pas dit merci. Elle l'a pensé très fort.

## scene: jury
title: Les citrouilles de la foire
pace: now

C'est la foire de printemps, sur la place du Puits. Le Noyer veut gagner le concours des citrouilles, une fois, sans que le jury soit de sa famille.
Cette année, il a proposé {A} comme juré{a:|e}. Il l'a dit à tout le monde, ce qui est déjà une victoire.

* honest | Juger honnêtement  [needs trait just]
  [lean honor +2]
  {A} pèse, mesure, tapote. La plus belle citrouille est celle du passeur. Le Noyer perd, et il est ravi : il a perdu devant un jury honnête.
  Il le raconte pendant un mois, en commençant par « Le Noyer a perdu ».
  [remember noyer.honest_jury] [end]

* ortie | Faire gagner la citrouille de l'Ortie
  [stress just 15]
  [lean compassion +2]
  La citrouille de l'Ortie est petite, cabossée, et elle a poussé au milieu de quatre enfants. {A} lui donne le prix. L'Ortie ne dit rien, et serre le ruban très fort.
  [remember ortie.pumpkin_prize] [remember noyer.ortie_won] [end]

* refuse | Refuser d'être juré
  [lean energy -1]
  {A} refuse, poliment. Dame Ermengarde prend la place, et le Noyer gagne. Comme d'habitude. Il n'a pas l'air content du tout.
  [modifier clear_conscience] [end]
