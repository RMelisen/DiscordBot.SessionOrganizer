// Par la Côte — wave 1 (ado). Rewrites teen_runaway: the Côte is the road up to Roncebelle, which one
// sees from the village and never reaches (lore § 5). The Intendant grew up at the Maison and pretends
// not to know the shortcuts (fiche 3). Once in a lifetime: one runs away for ever only once.

# arc: up_the_cote
title: Par la Côte
family: village
stage: teen
trigger: draw
size: short
once: life

## memories
intendant.walked_down | intendant | private | +3 | L'a croisé{a:|e} à mi-Côte, un baluchon sur l'épaule, et l'a raccompagné{a:|e} au hameau. Il a pris un raccourci qu'il prétend ne pas connaître.

## scene: road
title: Par la Côte
pace: now

Ce matin, {A} quitte Humeval. Pour toujours. Le baluchon est prêt : deux tartines, un galet, une chaussette de rechange.
À mi-Côte, l'Intendant descend vers le hameau, son registre sous le bras. Il s'arrête, regarde le baluchon, et ne dit rien.

* top | Monter jusqu'en haut
  [stress lazy 15]
  [lean boldness +2]
  {A} monte jusqu'en haut de la Côte. De là, on voit la route qui continue, qui continue, et Roncebelle nulle part.
  {A} reste assis{a:|e} un moment sur la borne, puis redescend. Pour toujours, ça peut attendre demain.
  [grow courage] [end]

* intendant | Redescendre avec l'Intendant
  [lean sociability +2]
  Ils redescendent ensemble. L'Intendant ne pose aucune question. Au virage, il prend un raccourci qu'il prétend ne pas connaître, à travers le Communal.
  Il évite très soigneusement la chèvre.
  [remember intendant.walked_down] [end]

* eat | S'asseoir et manger les tartines
  [lean energy -1]
  Partir pour toujours, ça creuse. {A} s'assoit dans l'herbe et mange les deux tartines, puis regarde la chaussette de rechange avec un certain intérêt.
  {A} est de retour pour la soupe. La Bruyère ne demande rien. Elle dit : « Mange. »
  [stress -15] [modifier hearty] [end]
