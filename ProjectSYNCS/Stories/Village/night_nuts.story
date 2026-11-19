// Les noix de la nuit — wave 1 (ado), new. Jacquet, the Ortie's eldest, steals the Noyer's nuts at night;
// she scolds him and makes a very good nut tart (fiche 10). The Noyer pretends to be outraged and leaves
// a basket « forgotten » by the path (fiche 9).

# arc: night_nuts
title: Les noix de la nuit
family: village
stage: teen
trigger: draw
size: short

## memories
noyer.warned_him | noyer | private | +2 | L'a prévenu, une nuit, que Jacquet venait chiper ses noix. Il en a ri pendant une semaine.

## scene: window
title: Les noix de la nuit
pace: now

Tap, tap, au carreau de la Maison des Pousses. C'est Jacquet, l'aîné de l'Ortie, un sac vide sur l'épaule.
Il va chiper des noix chez le Noyer, et il lui faut quelqu'un pour tenir le sac.

* hold | Tenir le sac
  [stress honest 15]
  [lean boldness +2]
  Sous le grand noyer, Jacquet secoue les branches et {A} tient le sac ouvert. Les noix tombent comme de la grêle, et une sur la tête.
  Le lendemain, l'Ortie gronde Jacquet très fort, puis apporte à la Maison une tarte aux noix encore chaude.
  [modifier sly] [lift hunger 0.2] [end]

* refuse | Refuser, et retourner se coucher
  [lean honor +2]
  « Pas ce soir. » Jacquet hausse les épaules et part seul. {A} se recouche, et s'endort tout de suite.
  [modifier clear_conscience] [end]

* warn | Prévenir le Noyer  [mystery]
  [lean honor +1]
  {A} court frapper chez le Noyer. Il écoute, très sérieux, et promet de monter la garde.
  Le lendemain matin, au bord du chemin, le panier « oublié » est deux fois plus gros que d'habitude. Jacquet n'y comprend rien.
  [remember noyer.warned_him] [end]
