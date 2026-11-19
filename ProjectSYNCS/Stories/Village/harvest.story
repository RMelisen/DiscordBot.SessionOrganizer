// La moisson — wave 2 (adulte, ancien), seasonal: summer, the harvest (lore § 8). The Noyer hires and pays
// well, so it gets said; the Ortie tells him her four truths (fiches 9, 10).

# arc: harvest
title: La moisson
family: village
stage: adult, elder
trigger: draw
size: short
requires: [season summer]

## scene: fields
title: La moisson
pace: now

Tout le hameau est aux Longues Raies. Le Noyer paie bien, pour qu'on le dise. Entre deux gerbes, l'Ortie lui dit ses quatre vérités, et il les écoute en hochant la tête.
Il fait chaud. La Sommeille brille au bout du champ.

* reap | Moissonner jusqu'au soir
  [stress lazy 15]
  [lean energy +2]
  Gerbe après gerbe, jusqu'à ce que le soleil touche la colline. Le Noyer paie en public, très fort, en comptant à voix haute.
  [cailloux 25] [end]

* nap | Faire la sieste à l'ombre du Noyer
  [lean energy -2]
  L'ombre du Noyer est la meilleure de la vallée, tout le monde le sait, lui le premier. {A} y dort tout l'après-midi. Le Noyer est très flatté.
  [stress -20] [modifier well_rested] [end]

* swim | Se baigner dans la Sommeille avec les enfants
  [lean sociability +2]
  {A} rejoint les enfants de l'Ortie dans la rivière. Il en manque un : il est déjà dans l'eau, sous le saule. Le soir, tout le monde sent la rivière et le foin.
  [modifier light_heart] [end]
