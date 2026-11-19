// Replaces on_adult.
# arc: adult
title: Adulte, maintenant
family: milestone
stage: adult, elder
trigger: stage adult
size: short

## scene: grown
title: Adulte, maintenant
was: on_adult
pace: now

{A} a fini de grandir, et quitte la Maison des Pousses pour une petite maison à soi. Sur ses traits se lit maintenant un dernier trait de caractère : {T}.

* party | Fêter ça à l'auberge
  [lean sociability +1]
  La Courge offre une part de tarte, dit « déjà, mon chou ? » trois fois, et le raconte à toute la salle.
  [grow diplomacy] [end]

* plan | Faire des projets
  [lean rationality +1]
  {A} sort un carnet et y écrit « projets ». Puis, en dessous : « à voir ».
  [grow stewardship] [end]
