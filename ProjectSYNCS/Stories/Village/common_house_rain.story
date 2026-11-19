// L'averse à la Maison commune — wave 2 (adulte, ancien). Rewrites grown_storm_shelter: the Maison commune
// is where the whole village goes when it rains too hard (lore § 5). With a rival.

# arc: common_house_rain
title: L'averse à la Maison commune
family: village
stage: adult, elder
trigger: draw
size: short
other: rivals

## scene: shelter
title: L'averse à la Maison commune
pace: now

L'averse tombe d'un coup sur la place. Le seul abri ouvert, c'est la Maison commune. {A} entre en courant, {a:trempé|trempée}.
{B} est déjà là, {b:assis|assise} sur le seul banc sec. {p:Ils|Elles} ne s'entendent pas, et la pluie n'a pas l'air pressée.

* share | Partager son pain
  [stress greedy 10]
  [lean compassion +2]
  {A} coupe sa miche en deux et en tend la moitié à {B}, sans un mot. {B} hésite, la prend, et pousse un peu sur le banc.
  Dehors, il pleut tout l'après-midi. Dedans, ce n'est pas si mal.
  [affinity +10] [modifier clear_conscience] [end]

* far | S'asseoir à l'autre bout, sans un mot
  [lean energy -1]
  {A} s'assoit sur une caisse, à l'autre bout. La pluie tambourine sur le toit de pierre. Au bout d'une heure, c'est presque reposant de ne pas se parler.
  [modifier soothed] [end]

* out | Ressortir sous la pluie, par fierté
  [stress humble 15]
  [lean boldness +2]
  {A} ressort, le menton haut. La pluie redouble. {A} rentre chez soi trempé{a:|e} jusqu'aux os, très {a:fier|fière}, en éternuant.
  [modifier fired_up] [end]
