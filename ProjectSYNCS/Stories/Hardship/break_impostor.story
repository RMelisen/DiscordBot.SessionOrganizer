// Replaces break_impostor.
# arc: break_impostor
title: Pas à la hauteur
family: hardship
stage: baby, teen, adult, elder
trigger: break 2
size: short

## scene: break
title: Pas à la hauteur
was: break_impostor
pace: now

Ce matin, {A} a croisé son reflet dans l'eau du Puits sans le reconnaître. Depuis, une petite voix répète que {A} fait tout de travers, que tout le hameau le sait, et que ce n'est qu'une question de temps avant que quelqu'un le dise tout haut.

* sorry | S'excuser auprès de tout le monde, pour tout
  [lean honor +2]
  {A} fait le tour du hameau et s'excuse : auprès de la Courge pour une chope cassée, auprès du Pont-Vieux pour rien de précis. Tout le monde est très surpris, le pont aussi. La petite voix se tait enfin, mais {A} a pris l'habitude de s'excuser.
  [stress -90] [coping contrite] [end]

* hide | Ne plus sortir, pour que personne ne remarque rien
  [lean sociability -2]
  {A} reste chez soi trois jours, rideaux tirés : on ne juge pas ce qu'on ne voit pas. Au quatrième jour, la petite voix s'est lassée. Mais dehors, chaque bruit fait sursauter {A}.
  [stress -90] [modifier shaken] [end]

* ask | Demander au Tilleul : « Je fais tout de travers ? »
  [lean sociability +1] [lean rationality +1]
  Le Tilleul réfléchit très longtemps, une abeille sur l'épaule. « Tu as renversé mon miel, l'hiver dernier. Ou alors c'était un autre. À part ça, non. » Ce n'est pas tout à fait suffisant. Mais c'est un début.
  [stress -60] [end]
