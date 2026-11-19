// Les genoux savent — wave 2 (ancien only). Rewrites elder_knees: the Tilleul's knee knows the weather too
// (fiche 12); the Sauge's balms, and her irritation at what she cannot bottle (fiche 5).

# arc: knowing_knees
title: Les genoux savent
family: village
stage: elder
trigger: draw
size: short

## memories
sauge.took_balm | sauge | private | +2 | A pris son baume pour les genoux sans discuter. Elle a trouvé ça reposant.

## scene: knees
title: Les genoux savent
pace: now

Les genoux de {A} annoncent la pluie. Ce matin, ils annoncent un orage, un gros. Le ciel est tout bleu.
La Sauge, qui passait, hausse un sourcil. Elle ne croit pas aux genoux. Elle n'aime pas ce qu'elle ne peut pas mettre en bocal.

* warn | Prévenir tout le hameau
  [lean sociability +2]
  {A} prévient tout le monde. On rit gentiment. À quatre heures, l'orage éclate, et tout le hameau est déjà à l'abri, avec le linge rentré. On ne rit plus. On applaudit.
  [modifier well_spoken] [end]

* compare | Comparer ses genoux avec ceux du Tilleul
  [lean compassion +1]
  Au verger, le Tilleul tâte son genou, puis celui de {A}. « Le tien dit quatre heures. Le mien dit cinq. » Ils parient une pomme. C'est quatre heures et demie.
  [modifier light_heart] [end]

* balm | Prendre le baume de la Sauge
  [lean rationality +1]
  La Sauge sort un baume vert qui sent la cave. Il marche. Les genoux se taisent, et l'orage arrive quand même, sans prévenir.
  [stress -15] [remember sauge.took_balm] [modifier soothed] [end]
