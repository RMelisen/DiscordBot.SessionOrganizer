// Après l'extinction des feux — wave 1 (ado). Rewrites teen_pillow_fight in the Maison's dormitory.
// The Bruyère tucks the pousses in once they sleep, so none can tell it (fiche 8).

# arc: lights_out
title: Après l'extinction des feux
family: village
stage: teen
trigger: draw
size: short
other: friends

## memories
bruyere.felt_tucked | bruyere | private | +2 | A fait semblant de dormir, un soir, et l'a sentie venir {a:le|la} border. N'en a jamais parlé.

## scene: dormitory
title: Après l'extinction des feux
pace: now

Minuit, au dortoir de la Maison des Pousses. Les lanternes sont éteintes depuis longtemps.
Dans le noir, un oreiller vole, et atterrit sur {A}. Il vient du lit de {B}.

* fight | Riposter, de toutes ses forces
  [lean energy +2]
  {A} riposte. {B} riposte. En une minute, tout le dortoir est debout, et il neige des plumes.
  Quand la Bruyère ouvre la porte, il n'y a que des pousses profondément endormies, et un oreiller vide.
  [affinity +5] [modifier party_soul] [end]

* pretend | Faire semblant de dormir  [mystery]
  [lean energy -2]
  {A} ne bouge pas. {B} se lasse. Le dortoir se tait. Bien plus tard, la porte grince.
  Des pas, très légers. Quelqu'un remonte la couverture de {A} jusqu'au menton, et rentre un coin qui dépassait.
  [remember bruyere.felt_tucked] [modifier soothed] [end]

* truce | Proclamer une trêve  [needs trait just]
  [lean honor +2]
  {A} se lève et proclame une trêve, avec un règlement en trois articles. {B} signe, à la craie, sur le montant du lit.
  L'article trois autorise un oreiller par semaine. On l'utilise dans la minute.
  [grow diplomacy] [end]
