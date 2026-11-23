# arc: rematch
title: La revanche
family: rivalry
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation rivals]
cooldown: 7d

## scene: rematch
title: La revanche
pace: now

La visite s'est jouée à pas grand-chose, comme toujours avec {B}. Le soir venu, {A} n'arrive pas à penser à autre chose : la prochaine fois, ce sera différent. La prochaine fois, {A} sera prêt{a:|e}.

* train | S'entraîner jusqu'à la nuit  [roll courage 3]
  [lean energy +2]
  ? success
    {A} s'entraîne jusqu'à la nuit au bord de la Sommeille : courir, sauter, lancer des galets. Le passeur regarde faire, et finit par compter les points.
    [grow courage] [modifier fired_up] [end]
  ? failure
    {A} s'entraîne jusqu'à la nuit, et glisse dans la boue au dernier saut. Retour crotté{a:|e} jusqu'aux oreilles, mais prêt{a:|e}, très prêt{a:|e}.
    [modifier muddy_paws] [end]

* spy | Observer l'autre en cachette
  [stress honest 10]
  [lean honor -1] [lean rationality +1]
  {A} observe {B} depuis une haie, toute une après-midi, et note tout : la façon de marcher, de rire, de manger une pomme. On ne sait pas encore à quoi ça servira. Mais c'est noté.
  [grow intrigue] [modifier sly] [end]

* admit | Reconnaître que c'était bien joué
  [lean honor +1] [lean compassion +1]
  {A} va le dire à {B}, tout simplement : c'était bien joué. {B} ne sait pas quoi répondre. C'est une première victoire, d'une autre sorte.
  [affinity +5] [grow diplomacy] [end]
