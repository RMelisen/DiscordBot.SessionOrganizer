// L'appel de l'Ortie — wave 1 (bébé), new (the hide-and-seek of baby_peekaboo). The Ortie is always
// one child short at roll call, always the same one, and never says merci (fiche 10).

# arc: ortie_roll_call
title: L'appel de l'Ortie
family: village
stage: baby
trigger: draw
size: short

## memories
ortie.answered_present | ortie | private | +2 | A répondu « présent » à son appel, une fois qu'il lui manquait un enfant. Elle l'a nourri{a:|e} avec les autres.

## scene: roll_call
title: L'appel de l'Ortie
pace: now

Devant sa petite maison, l'Ortie fait l'appel de ses quatre enfants. Il en manque un, comme toujours. Toujours le même.
{A}, qui passait par là, se retrouve au bout de la rangée, et l'Ortie compte sans regarder.

* present | Répondre « présent ! »
  [lean sociability +2]
  « Présent ! » L'Ortie hoche la tête, satisfaite, et pousse la rangée vers la table. {A} reçoit un bol, comme les autres.
  Elle ne dit pas merci. Mais elle ressert {A} deux fois.
  [remember ortie.answered_present] [end]

* hide | Se cacher mieux que l'enfant qui manque
  [lean boldness +1]
  {A} disparaît derrière le tas de bois. Il manque maintenant deux enfants. L'Ortie soupire, très fort, et commence à chercher.
  Elle trouve {A} en premier. « Pas mal. L'autre, je ne le trouve jamais. »
  [grow intrigue] [end]

* search | Aller chercher l'enfant qui manque  [needs trait curious]
  [lean rationality +2]
  {A} suit une traînée de paille jusqu'au foin du Noyer. Le petit est là, endormi sur ses billes.
  Il en donne une à {A}, pour ne pas être dénoncé. {A} le dénonce quand même, très doucement.
  [item col.bille] [end]
