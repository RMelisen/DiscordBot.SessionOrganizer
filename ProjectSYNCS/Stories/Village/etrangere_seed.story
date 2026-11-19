// La graine de l'Étrangère — wave 1 (ado). Rewrites teen_seed: the Étrangère brings a seed from the
// Bois Sourd (fiche 11). Two beats: what came up shows the next day, steered by the first choice.
// Nothing is explained: a seed that glows is just that.

# arc: etrangere_seed
title: La graine de l'Étrangère
family: village
stage: teen
trigger: draw
size: short

## memories
etrangere.showed_sprout | etrangere | private | +3 | Lui a montré ce qui avait poussé de sa graine du Bois. Elle a souri, sous sa capuche.

## scene: seed
title: La graine de l'Étrangère
pace: now

L'Étrangère passe à la Maison des Pousses et donne à chacun une graine pâle, venue du Bois. Elle n'en a jamais vu pousser ici.
{A} met la sienne dans un pot, sur le rebord de la fenêtre.

* tend | S'en occuper chaque jour, avec un carnet
  [stress lazy 10]
  [lean rationality +2]
  {A} arrose à heure fixe, mesure la terre avec une brindille, et note tout dans un carnet. « Jour un : rien. Jour un, le soir : toujours rien. »
  [grow learning] [flag tended] [-> sprout]

* talk | Lui parler tous les soirs  [mystery]
  [lean compassion +2]
  Chaque soir, {A} raconte sa journée au pot. Le pot écoute très bien.
  [flag talked] [-> sprout]

* forget | L'oublier un peu
  [lean energy -1]
  {A} oublie le pot sur le rebord. La pluie s'en occupe à sa place.
  [modifier well_rested] [-> sprout]

## scene: sprout
title: Ce qui a poussé
pace: next_day

Ce matin, quelque chose sort du pot.
[if flag tended] C'est un radis. Un radis tout à fait ordinaire, bien droit, exactement où le carnet l'avait prévu.
[if flag talked] C'est une pousse pâle, à peine plus grande qu'un doigt. La nuit, elle luit tout doucement, d'un vert que {A} a déjà vu quelque part.
[unless flag tended] [unless flag talked] C'est une herbe folle, très fière d'elle, qui a déjà débordé du pot.

* show | Montrer le pot à l'Étrangère
  [lean sociability +2]
  {A} porte le pot jusqu'à la cabane du Communal. L'Étrangère le regarde longtemps, la main sur le cœur, et sourit sous sa capuche.
  « Chez moi, on dit qu'une graine choisit son jardin. »
  [remember etrangere.showed_sprout] [end]

* keep | Garder le pot sur le rebord
  [lean energy -1]
  Le pot reste sur le rebord. Chaque matin, {A} lui dit bonjour avant tout le monde.
  [modifier light_heart] [end]

* plant | Tout replanter au jardin de la Maison
  [lean zeal +1]
  {A} replante tout entre les haricots de la Bruyère, avec un tuteur et une pancarte. La Bruyère fait semblant de ne pas voir, et l'arrose.
  [grow stewardship] [end]
