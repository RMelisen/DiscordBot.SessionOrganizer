// Replaces social_rival and reply_rival.
# arc: rival
title: Le défi
family: rivalry
stage: adult, elder
trigger: draw
size: short
other: hostile

## scene: boast
title: Le défi
was: social_rival
pace: now

{B} s'est encore vanté{b:|e} d'être {b:le plus rapide|la plus rapide} du hameau. Devant tout le monde. Devant {A}.

* challenge | {b:Le|La} défier à la course jusqu'au Pont-Vieux  [owner-only]
  [lean boldness +2] [lean vengefulness +1]
  {A} lance le défi, et {B} relève le menton.
  [-> answer]

* ignore | Hausser les épaules
  [lean rationality +1]
  {A} hausse les épaules si haut que ça devient un exercice. Personne n'a gagné, mais personne n'a perdu.
  [grow learning] [end]

## scene: answer
title: Le défi relevé
was: reply_rival
pace: now
handover

{B} défie {A} à la course jusqu'au Pont-Vieux. Sur le parapet, le Fou compte déjà les foulées.

* race | Courir  [accept] [roll courage vs]
  [lean boldness +2]
  ? success
    {A} gagne d'un chapeau. Le Fou annonce le compte : trois foulées d'avance, et une de trop pour {B}.
    [grow courage] [affinity -5] [end]
  ? failure
    {B} gagne d'un chapeau. {A} réclame une revanche avant même d'avoir repris son souffle.
    [affinity -10] [end]

* decline | Refuser, avec un sourire en coin  [refuse]
  [lean rationality +1]
  {A} refuse, et sourit en coin. {B} ne sait plus si c'est une victoire ou une défaite.
  [affinity -5] [end]
