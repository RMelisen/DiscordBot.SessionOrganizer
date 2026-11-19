// Le compte des pousses — wave 1 (bébé), new. The Intendant counts the pousses and is always off by
// one, because they move (fiche 3). The memory is meant for the autumn count events of a later wave.

# arc: pousse_count
title: Le compte des pousses
family: village
stage: baby
trigger: draw
size: short

## memories
intendant.stood_still | intendant | private | +2 | Est resté{a:|e} immobile pendant tout son compte, à la Lisière. Pour une fois, le chiffre était juste.

## scene: count
title: Le compte des pousses
pace: now

L'Intendant est monté de Roncebelle compter les pousses de la saison. Il se trompe toujours d'une, parce qu'elles bougent.
Aujourd'hui, il pointe sa plume sur {A}, et commence.

* still | Ne plus bouger d'un poil
  [stress rowdy 15]
  [lean honor +2]
  {A} ne bouge plus. Pas un cil. L'Intendant compte, recompte, et regarde son registre comme s'il venait d'y trouver un trésor.
  « Juste. Le chiffre est juste. » Il le souligne deux fois.
  [remember intendant.stood_still] [end]

* move | Changer de place à chaque fois
  [lean energy +2]
  À chaque coup de plume, {A} est ailleurs. L'Intendant compte une pousse de plus, puis deux.
  Il note, très digne : « Pousses : nombreuses. »
  [modifier sly] [end]

* help | L'aider à compter
  [lean rationality +1]
  {A} tient l'encrier et compte sur ses doigts, à voix haute. Les doigts ne suffisent pas, on ajoute les orteils.
  [grow stewardship] [end]

* line | Mettre toutes les pousses en rang  [needs trait bossy]
  [lean sociability +1]
  En deux minutes, toutes les pousses sont alignées par taille. L'Intendant en reste la plume en l'air.
  Il sort dix cailloux de sa bourse et inscrit dans la marge : « Frais de rangement. »
  [cailloux 10] [end]
