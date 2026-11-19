// La noce des renards — wave 1 (ado). Rewrites teen_fox_wedding at the Lisière: sun and rain at once
// means the foxes are marrying, and custom says nobody looks. Animals are animals (lore § 2).

# arc: fox_wedding
title: La noce des renards
family: village
stage: teen
trigger: draw
size: short

## scene: shower
title: La noce des renards
pace: now

Grand soleil, et pourtant il pleut. Au hameau, tout le monde sait ce que ça veut dire : les renards se marient.
La noce passe à la Lisière en ce moment même, de l'autre côté des fougères, et la coutume est claire : personne ne regarde.

* peek | Écarter les fougères, juste un peu  [mystery]
  [stress zealous 15]
  [lean boldness +2]
  Une file de renards traverse la clairière sous la pluie dorée, très dignes. Le dernier s'arrête, regarde {A} droit dans les yeux, et cligne de l'œil.
  Personne ne le croira jamais. {A} ne le racontera qu'au Fou, qui le croit tout de suite.
  [modifier inspired] [end]

* count | Fermer les yeux et compter jusqu'à cent
  [stress impatient 10]
  [lean zeal +2]
  {A} ferme les yeux et compte. À soixante, on entend des petits pas dans les feuilles. À cent, il ne pleut plus, et l'air sent la fête.
  [modifier soothed] [end]

* bouquet | Laisser un bouquet au bord du chemin, sans regarder
  [lean compassion +2]
  {A} pose un bouquet de pâquerettes au bord du chemin, le dos tourné, et rentre sans se retourner.
  Le lendemain matin, à la place du bouquet, il y a une plume rousse.
  [item col.plume] [end]
