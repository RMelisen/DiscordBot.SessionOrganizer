// Le guet du gué — wave 1 (ado), new. Aude and Mathurin meet at the ford and the whole village keeps
// watch for them; the Noyer, Aude's father, is the only one who doesn't know, and never learns
// (lore § 11, § 12, fiche 9).

# arc: ford_watch
title: Le guet du gué
family: village
stage: teen
trigger: draw
size: short

## memories
noyer.pumpkin_talk | noyer | private | +2 | L'a écouté parler de ses citrouilles tout un après-midi, au bord du gué. Il n'a jamais su pourquoi.

## scene: ford
title: Le guet du gué
pace: now

Au gué de la Sommeille, Aude, la fille du Noyer, et Mathurin, l'apprenti du Chardon, se parlent tout bas. {A} fait le guet, comme tout le village l'a fait un jour.
Sur le chemin, le Noyer arrive. Il chantonne.

* whistle | Siffler le signal
  [lean energy +1]
  {A} siffle comme un merle enrhumé. Au gué, il n'y a plus qu'un canard, et des roseaux qui bougent encore.
  Le Noyer passe, salue le canard, et continue de chantonner.
  [modifier light_heart] [end]

* stall | Retenir le Noyer en parlant de ses citrouilles  [roll diplomacy 3]
  [lean sociability +2]
  ? success
    « Votre citrouille de l'an dernier… » Le Noyer s'arrête net. Une heure plus tard, il en est encore à la taille de la tige.
    Au gué, Aude et Mathurin ont eu tout le temps. Le Noyer repart ravi, sans savoir pourquoi.
    [remember noyer.pumpkin_talk] [end]
  ? failure
    {A} confond sa citrouille avec celle du Chardon. Le Noyer, vexé, passe le gué sans rien voir : il regarde ses pieds en ruminant l'affront.
    [grow diplomacy] [end]

* hide | Se cacher dans les roseaux
  [stress brave 10]
  [lean boldness +1]
  {A} se coule dans les roseaux et ne bouge plus. Le Noyer passe à deux pas. Aude et Mathurin aussi, cachés dans les roseaux voisins.
  Ils se regardent tous les trois, et retiennent un fou rire.
  [grow intrigue] [end]
