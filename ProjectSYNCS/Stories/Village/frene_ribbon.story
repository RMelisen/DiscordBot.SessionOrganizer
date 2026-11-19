// Le ruban de Maître Frêne — wave 1 (ado), new. Maître Frêne always needs someone to hold the end of
// the ribbon (fiche 2); the Noyer and the Chardon each swear the pré de la Sommeille is theirs, and
// both secretly ask the Fou how many boundary stones he counted (fiches 4, 9, 13, lore § 11).
// The memories are for the adulte events of the pré quarrel.

# arc: frene_ribbon
title: Le ruban de Maître Frêne
family: village
stage: teen
trigger: draw
size: short

## memories
frene.held_straight   | frene   | private | +2 | A tenu le bout de son ruban bien droit, le jour où il a mesuré le pré de la Sommeille.
noyer.ribbon_his_side | noyer   | private | +2 | A tiré le ruban de son côté, le jour où Maître Frêne mesurait le pré.
noyer.ribbon_against  | noyer   | private | -1 | A tiré le ruban du côté du Chardon, le jour où Maître Frêne mesurait le pré.
chardon.ribbon_his_side | chardon | private | +2 | A tiré le ruban de son côté, le jour où Maître Frêne mesurait le pré.
chardon.ribbon_against  | chardon | private | -1 | A tiré le ruban du côté du Noyer, le jour où Maître Frêne mesurait le pré.
fou.counted_stones    | fou     | private | +2 | A compté les bornes du pré avec lui. Le compte n'était pas le même que la veille.

## scene: meadow
title: Le ruban de Maître Frêne
pace: now

Maître Frêne mesure le pré de la Sommeille, et il lui faut quelqu'un pour tenir le bout du ruban. Il tend le bout à {A}.
D'un côté du pré, le Noyer regarde. De l'autre, le Chardon. Tous les deux font semblant de ne pas regarder.

* straight | Tenir le ruban bien droit
  [lean honor +2]
  {A} tient le ruban droit comme un fil à plomb. Maître Frêne note le chiffre, le relit, et hoche la tête.
  « Personne n'a tort, et personne n'a tout à fait raison. » Les deux voisins repartent mécontents, ce qui est la paix.
  [remember frene.held_straight] [modifier clear_conscience] [end]

* noyer | Le tirer un peu côté Noyer
  [stress just 15]
  [lean greed +1]
  Le ruban glisse d'une main vers le Noyer. Le Noyer sourit dans sa barbe. Le Chardon, lui, a tout vu, et il le note quelque part dans sa tête.
  [remember noyer.ribbon_his_side] [remember chardon.ribbon_against] [end]

* chardon | Le tirer un peu côté Chardon
  [stress just 15]
  [lean greed +1]
  Le ruban glisse d'une main vers le Chardon. Le Chardon grogne, ce qui chez lui veut dire merci. Le Noyer, lui, a tout vu, et il en parlera à l'auberge.
  [remember chardon.ribbon_his_side] [remember noyer.ribbon_against] [end]

* stones | Lâcher le ruban et compter les bornes avec le Fou  [needs trait eccentric] [mystery]
  [lean zeal +2]
  {A} lâche le ruban et rejoint le Fou, qui compte les bornes à voix haute. « Cinq. Cinq, c'est une de trop. »
  Ils en comptent quatre. Puis six. Maître Frêne renonce à mesurer, et vient compter avec eux.
  [remember fou.counted_stones] [modifier inspired] [end]
