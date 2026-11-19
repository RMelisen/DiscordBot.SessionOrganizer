// Le plat mijoté — wave 2 (adulte, ancien). Rewrites grown_bad_dish with a friend's cooking; the Sauge
// secretly tastes things « for science » (fiche 5).

# arc: simmered_dish
title: Le plat mijoté
family: village
stage: adult, elder
trigger: draw
size: short
other: friends

## scene: dish
title: Le plat mijoté
pace: now

Toute la semaine, {B} a mijoté un plat « rien que pour {A} ». Le plat est vert. Il fait un petit bruit.
{B} attend, les yeux brillants, une cuillère tendue.

* finish | Dire que c'est délicieux, et finir l'assiette
  [stress honest 10]
  [lean compassion +2]
  {A} finit l'assiette, jusqu'au bout, en souriant. {B} est si {b:heureux|heureuse} qu'{b:il|elle} en ressert. {A} finit aussi la deuxième.
  [affinity +8] [modifier hearty] [end]

* truth | Dire la vérité, avec douceur
  [lean honor +2]
  « C'est… vert. Très vert. » {B} goûte à son tour, réfléchit, et éclate de rire. {p:Ils|Elles} refont le plat ensemble, en moins vert.
  [grow diplomacy] [end]

* sauge | En porter une part à la Sauge, « pour la science »  [mystery]
  [lean rationality +1]
  La Sauge goûte, très sérieusement, et note quelque chose. « Ça, c'est un remède contre le hoquet. Un très bon. » {B} est ravi{b:|e} : {b:il|elle} a inventé un remède.
  [modifier light_heart] [end]
