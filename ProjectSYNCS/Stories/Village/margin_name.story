// Un nom dans la marge — wave 1 (ado). Rewrites teen_crush at the Maison des Pousses, with another
// Plynling ({B}). Events never make a couple. Telling the Courge means the whole place knows by noon,
// kindly (fiche 6), hence a public memory.

# arc: margin_name
title: Un nom dans la marge
family: village
stage: teen
trigger: draw
size: short
other: friends

## memories
courge.knows_crush | courge | public | +2 | Lui a confié un béguin. À midi, toute la place était au courant, en mieux.

## scene: margin
title: Un nom dans la marge
pace: now

Depuis quelque temps, {A} rougit quand {B} passe. Dans la marge d'un livre de la Maison, il y a le nom de {B}.
Effacé, puis réécrit. Plus petit.

* flower | Laisser une fleur sur le lit de {B}, sans signer  [mystery]
  [lean compassion +2]
  {A} pose une pâquerette sur l'oreiller de {B}, et file. Le soir, {B} la tourne entre ses doigts, et regarde tout le dortoir, un par un.
  {B} s'arrête un peu plus longtemps sur {A}. Ou peut-être pas.
  [affinity +5] [end]

* talk | Aller lui parler, de n'importe quoi  [roll diplomacy 3]
  [stress shy 20]
  [lean sociability +2]
  ? success
    {A} parle du temps, des grenouilles, d'une pomme. {B} répond, rit, et {p:ils|elles} parlent jusqu'à la soupe.
    [affinity +8] [grow diplomacy] [end]
  ? failure
    {A} ouvre la bouche, et c'est un long discours sur les navets qui en sort. Très documenté. {B} écoute jusqu'au bout, poliment.
    [modifier well_spoken] [end]

* courge | En parler à la Courge
  [lean sociability +1]
  « Mon chou ! » La Courge garde le secret jusqu'à la porte. À midi, toute la place sait qu'il y a un béguin à la Maison des Pousses, et que c'est très beau.
  Personne ne sait qui. Tout le monde regarde {A}.
  [remember courge.knows_crush] [end]
