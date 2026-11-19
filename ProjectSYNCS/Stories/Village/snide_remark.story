// La remarque — wave 2 (adulte, ancien). Rewrites grown_snide at the inn, with a rival; the Courge's gossip
// turns everything into a good story, kindly (fiche 6).

# arc: snide_remark
title: La remarque
family: village
stage: adult, elder
trigger: draw
size: short
other: rivals

## memories
courge.hat_comeback | courge | private | +2 | A répondu à une remarque sur son chapeau par « Autrefois, oui. Et ça reviendra. » Elle le répète à tout le monde.

## scene: inn
title: La remarque
pace: now

À l'auberge, en passant près de la table de {A}, {B} glisse que ce chapeau « a dû être très à la mode, autrefois ».
La Courge a entendu. Demain, toute la place l'aura entendu aussi.

* retort | Répondre du tac au tac  [roll intrigue 3]
  [lean boldness +2]
  ? success
    La réponse fuse, si juste que le Fou la répète deux fois, pour l'apprendre. {B} ouvre la bouche, la referme, et va s'asseoir.
    [modifier well_spoken] [end]
  ? failure
    La réponse arrive le lendemain matin, dans le lit, parfaite. Trop tard. {A} boude toute la journée.
    [modifier sulky] [end]

* smile | Sourire, et ne rien dire
  [stress wrathful 15]
  [lean compassion +1]
  {A} sourit, et reprend sa soupe. {B} attend une réponse, qui ne vient pas. C'est {B}, finalement, qui a l'air bête.
  [modifier clear_conscience] [end]

* agree | Approuver : « Autrefois, oui. Et ça reviendra. »
  [lean sociability +2]
  La salle éclate de rire. La Courge le répète à chaque client pendant une semaine, en mieux. Trois jours plus tard, le Noyer porte un chapeau d'autrefois.
  [remember courge.hat_comeback] [end]
