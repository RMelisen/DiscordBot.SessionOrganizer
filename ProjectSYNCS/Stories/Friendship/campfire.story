// Replaces grown_campfire and reply_campfire.
# arc: campfire
title: Autour du feu
family: friendship
stage: adult, elder
trigger: draw
size: short
other: anyone
other_if: [relation acquaintances] [affinity >= 0]

## scene: fire
title: Autour du feu
was: grown_campfire
pace: now

Le soir, au bord de la Sommeille, le hameau fait griller du miel sur des bâtons. {A} se retrouve à côté de {B}, qu'on ne connaît que de vue. Le miel de {B} vient de tomber dans le feu.

* share | Partager son bâton avec {B}
  [lean compassion +1] [lean sociability +1]
  {A} tend son bâton. {B} croque, {A} croque, le miel coule partout. Ça colle aux doigts, et ça colle un peu les gens ensemble aussi.
  [affinity +15] [modifier hearty] [end]

* ask | Demander à {B} : « Tu veux qu'on soit amis ? »  [owner-only]
  [lean sociability +2] [lean boldness +1]
  {A} prend une grande inspiration et pose la question, comme ça, au-dessus du feu.
  [-> answer]

* sparks | Regarder les étincelles monter, sans rien dire
  [lean energy -1] [lean rationality +1]
  Les étincelles montent, montent, et deviennent des étoiles. Ou alors c'étaient déjà des étoiles. {B} regarde aussi.
  [modifier soothed] [affinity +5] [end]

## scene: answer
title: Une question au-dessus du feu
was: reply_campfire
pace: now
handover

Entre deux bouchées de miel, {B} a demandé à {A} : « Tu veux qu'on soit amis ? »

* yes | Dire oui, la bouche pleine de miel  [accept]
  [lean sociability +2]
  {A} dit oui, ou quelque chose d'approchant, la bouche pleine de miel. {B} comprend très bien.
  [bond friends] [end]

* later | « Reviens au prochain feu, on verra. »  [refuse]
  [lean rationality +1]
  {A} sourit : « Reviens au prochain feu, on verra. » {B} promet d'apporter son propre miel, cette fois.
  [affinity +5] [end]
