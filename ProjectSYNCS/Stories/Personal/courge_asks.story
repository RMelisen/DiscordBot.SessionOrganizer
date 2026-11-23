# arc: courge_asks
title: « Alors, cette visite ? »
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: asks
title: « Alors, cette visite ? »
pace: now

Le lendemain, la Courge attrape {A} par la manche au milieu de la place. « Alors, mon chou, cette visite avec {B} ? Raconte. Non, attends, je devine. » Elle ne devine pas. Elle attend, toutes vrilles frémissantes.

* truth | Raconter la vérité, simplement
  [lean honor +2]
  {A} raconte tout, simplement, sans rien ajouter. La Courge est presque déçue, puis plus du tout : à midi, sur la place, elle en a fait une épopée, et {A} y a le beau rôle.
  [modifier clear_conscience] [end]

* embellish | En rajouter un peu
  [stress honest 15]
  [lean honor -1] [lean sociability +1]
  {A} en rajoute un peu, puis beaucoup : une averse, un canard héroïque, un pont qui tangue. La Courge boit chaque mot, et en sert le soir même une version encore plus belle, avec deux canards.
  [modifier party_soul] [end]

* mystery | Faire mystère
  [lean sociability -1] [lean rationality +1]
  {A} sourit et ne dit rien. Rien du tout. La Courge en reste muette, ce qui ne lui arrive jamais, puis invente toute seule une histoire bien plus belle que la vraie.
  [modifier sly] [end]
