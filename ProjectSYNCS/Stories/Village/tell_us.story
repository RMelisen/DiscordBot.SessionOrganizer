// Raconte ! — wave 2 (ancien only). Rewrites elder_story with the Ortie's four children; one is always
// missing, always the same (fiche 10).

# arc: tell_us
title: Raconte !
family: village
stage: elder
trigger: draw
size: short

## memories
ortie.true_story | ortie | private | +2 | A raconté à ses enfants la vraie histoire de son enfance, toute simple. Elle a écouté depuis la porte.

## scene: circle
title: Raconte !
pace: now

Les enfants de l'Ortie s'assoient en rond devant {A}, les yeux brillants. « Raconte quand tu étais {a:petit|petite} ! »
Ils sont trois. Le quatrième manque. Toujours le même.

* embellish | Embellir. Beaucoup.
  [stress honest 15]
  [lean boldness +2]
  Dans l'histoire, {A} a sauté du Pont-Vieux, attrapé une comète et battu le Chardon aux bras de fer. Les enfants n'en croient pas un mot, et en redemandent.
  [modifier well_spoken] [end]

* true | Raconter la vraie histoire, toute simple
  [lean honor +2]
  Une flaque, un galet, une soupe. Les enfants écoutent, très sages. Depuis la porte, l'Ortie écoute aussi. Elle ne dit rien. Elle ne repart pas.
  [remember ortie.true_story] [end]

* sleep | Commencer, et s'endormir au milieu
  [lean energy -2]
  « Il était une fois une pousse qui… » {A} se réveille une heure plus tard, une couverture sur les genoux. Le quatrième enfant est là, endormi contre {A}.
  [stress -15] [modifier well_rested] [end]
