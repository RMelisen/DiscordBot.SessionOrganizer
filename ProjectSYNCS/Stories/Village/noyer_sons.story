// Clément contre Gaspard — wave 2 (adulte, ancien). Rewrites grown_pie_contest with the Noyer's two sons,
// always in a contest with each other (lore § 12); the Noyer speaks of himself in the third person when
// he wants to impress (fiche 9).

# arc: noyer_sons
title: Clément contre Gaspard
family: village
stage: adult, elder
trigger: draw
size: short

## scene: contest
title: Clément contre Gaspard
pace: now

Clément et Gaspard, les deux grands fils du Noyer, sont en concours, comme toujours : qui porte le plus de sacs, qui mange le plus de tartes.
Aujourd'hui, ils sont à égalité. Il leur faut un juge, et un troisième concurrent pour départager.

* compete | Concourir contre eux  [roll courage 3]
  [lean energy +2]
  ? success
    {A} porte un sac de plus que Clément et mange une tarte de plus que Gaspard. Ils réclament une revanche, le jour même, et la perdent aussi.
    [modifier fired_up] [end]
  ? failure
    {A} mange trois tartes de trop et porte un sac de moins. Les deux frères se disputent pour savoir lequel a gagné contre {A}.
    [modifier hearty] [end]

* referee | Arbitrer, sans favori  [needs trait just]
  [lean honor +2]
  {A} pèse les sacs, compte les tartes, et déclare l'égalité parfaite, preuve à l'appui. Les deux frères sont furieux, et se réconcilient contre l'arbitre.
  [grow diplomacy] [end]

* quiet | Proposer le concours du plus silencieux  [mystery]
  [lean rationality +1]
  Les deux frères se taisent. Une minute. Dix. Un silence immense tombe sur la ferme. Il dure jusqu'à ce que le Noyer arrive.
  « Le Noyer voudrait savoir ce qui se passe ici. » Les deux frères éclatent de rire en même temps. Égalité.
  [modifier light_heart] [end]
