// La comète — wave 1 (bébé). Rewrites baby_comet on the place du Puits with Maître Frêne, who keeps
// the register of the pousses (fiche 2). Once in a lifetime: a comet does not come twice.

# arc: comet
title: La comète
family: village
stage: baby
trigger: draw
size: short
once: life

## memories
frene.comet_register | frene | private | +2 | Lui a demandé d'écrire son nom à côté de la comète, dans le registre. Il l'a fait, de sa plus belle plume.

## scene: sky
title: La comète
pace: now

Ce soir, une étoile à longue traîne traverse le ciel, très lentement. Tout le hameau est sur la place, le nez en l'air.
Maître Frêne, à la lumière d'une lanterne, l'inscrit déjà au registre : la date, l'heure, et « belle ».

* wish | Faire un vœu, très fort
  [lean zeal +2]
  {A} serre les poings si fort que le chapeau tremble. Le vœu reste secret. Le lendemain, {A} vérifie trois fois.
  Pas encore. Mais bientôt.
  [modifier light_heart] [end]

* chase | Courir derrière la comète
  [lean energy +2]
  {A} court derrière la comète jusqu'au bout des Longues Raies. La comète gagne. {A} revient {a:essoufflé|essoufflée}, et très {a:fier|fière} d'avoir fait la course avec elle.
  [grow courage] [end]

* register | Demander à Maître Frêne d'écrire son nom à côté
  [lean rationality +1]
  Maître Frêne réfléchit. « Il convient d'examiner… » Puis il trempe sa plume, et écrit, à côté de la comète, le nom de {A}.
  C'est la première fois qu'une pousse entre au registre pour autre chose que sa sortie de la Souche.
  [remember frene.comet_register] [end]
