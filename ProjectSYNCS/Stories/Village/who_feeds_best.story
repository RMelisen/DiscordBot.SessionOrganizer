// Qui nourrit le mieux — wave 2 (adulte, ancien). Rewrites grown_feast with the oldest friendly rivalry
// of the village: Pain-Blond and the Courge, who feeds Humeval best (lore § 11). The quarrel resumes
// the next day, as it should.

# arc: who_feeds_best
title: Qui nourrit le mieux
family: village
stage: adult, elder
trigger: draw
size: short

## memories
courge.won_judging       | courge     | private | +2 | L'a déclarée meilleure que Pain-Blond, un soir de dispute. Elle le raconte encore.
pain_blond.lost_judging  | pain_blond | private | -1 | A préféré la soupe de la Courge à son pain, le soir où on l'a fait{a:|e} juge.
pain_blond.won_judging   | pain_blond | private | +2 | A préféré son pain à la soupe de la Courge, le soir où on l'a fait{a:|e} juge. Il n'en revient toujours pas.
courge.lost_judging      | courge     | private | -1 | A préféré le pain de Pain-Blond à sa soupe, le soir où on l'a fait{a:|e} juge.

## scene: table
title: Qui nourrit le mieux
pace: now

Pain-Blond et la Courge se disputent depuis toujours pour savoir qui nourrit le mieux le village. Ce soir, sur la table de l'auberge : une miche dorée, une marmite fumante.
Ils ont désigné un juge. C'est {A}. Toute la salle s'est tue.

* soup | La soupe de la Courge
  [lean sociability +1]
  « La soupe. » La Courge pousse un cri de victoire qu'on entend du moulin. Pain-Blond hoche la tête, très doucement, et reprend un bol, pour comprendre.
  [remember courge.won_judging] [remember pain_blond.lost_judging] [end]

* bread | Le pain de Pain-Blond
  [lean compassion +1]
  « Le pain. » Pain-Blond devient tout rouge, jusqu'à l'épi. La Courge, bonne joueuse, coupe une tranche, la goûte, et dit : « Bon. Bon, bon, bon. »
  [remember pain_blond.won_judging] [remember courge.lost_judging] [end]

* both | Déclarer l'égalité, et reprendre des deux
  [stress temperate 10]
  [lean energy +1]
  {A} trempe le pain dans la soupe, et reprend des deux. Personne n'a gagné. Tout le monde a mangé. La dispute reprend le lendemain, ravie.
  [modifier hearty] [end]
