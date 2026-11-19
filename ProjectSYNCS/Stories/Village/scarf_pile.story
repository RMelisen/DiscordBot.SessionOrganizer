// La pile d'écharpes — wave 1 (ado), new. Every year the Bruyère knits a scarf for Gauvain (the
// Intendant) and never dares give it: there is a pile in the wardrobe (fiche 8). The tender secret
// stays a secret: no option takes a scarf to him.

# arc: scarf_pile
title: La pile d'écharpes
family: village
stage: teen
trigger: draw
size: short

## memories
bruyere.saw_scarves | bruyere | private | +2 | A trouvé la pile d'écharpes dans son armoire, et l'a refermée sans un mot. Elle l'a remarqué.

## scene: wardrobe
title: La pile d'écharpes
pace: now

La Bruyère a envoyé {A} chercher une couverture dans sa grande armoire. Derrière les couvertures, il y a une pile d'écharpes tricotées.
Toutes de la même couleur, le vert sombre du houx. Une par hiver, à peu près.

* close | Refermer l'armoire et ne rien dire
  [lean compassion +2]
  {A} prend la couverture et referme l'armoire doucement, comme on referme un livre qui n'est pas à soi.
  Le soir, la Bruyère sert {A} en premier. Elle ne dit rien. Elle a remarqué.
  [remember bruyere.saw_scarves] [end]

* count | Compter les écharpes
  [lean rationality +2]
  {A} compte. Onze. Bien pliées, sans une maille de travers, et pas une n'a jamais été portée.
  {A} referme l'armoire en se demandant qui a un cou aussi patient.
  [grow stewardship] [end]

* ask | Lui demander pour qui c'est  [mystery]
  [lean sociability +2]
  La Bruyère regarde {A} longtemps. Puis elle pose une assiette devant {A}, la remplit à ras bord, et dit : « Mange. »
  C'est tout. Mais elle reste à côté pendant que {A} mange, ce qu'elle ne fait jamais.
  [modifier cherished] [end]
