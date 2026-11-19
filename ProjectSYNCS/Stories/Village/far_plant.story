// La plante qui pousse loin — wave 2 (adulte, ancien), new. The Sauge always needs a plant that grows
// far, high or in the mud; the Étrangère knows plants of the Bois she has never seen (fiches 5, 11).
// Reads lost_at_lisiere's etrangere.followed_light.

# arc: far_plant
title: La plante qui pousse loin
family: village
stage: adult, elder
trigger: draw
size: short

## memories
etrangere.mud_guide | etrangere | private | +2 | Lui a demandé le chemin dans les Mouilles. Elle l'a mené{a:|e} sans un mot jusqu'à la bonne plante.

## scene: errand
title: La plante qui pousse loin
pace: now

La Sauge a toujours besoin d'une plante qui pousse loin, haut, ou dans la boue. Aujourd'hui, c'est une fleur jaune au milieu des Mouilles.
Elle paie bien, et elle ne dit jamais comment la reconnaître. « Tu verras. »
[if memory etrangere.followed_light] Au bord du marais, l'Étrangère attend déjà, sa capuche sur les yeux. Personne ne l'a prévenue.

* wade | Entrer dans les Mouilles  [roll courage 3]
  [stress craven 15]
  [lean boldness +2]
  ? success
    De la boue jusqu'aux genoux, deux grenouilles vexées, et la fleur jaune, au milieu, exactement comme prévu. La Sauge paie sans discuter.
    [cailloux 20] [end]
  ? failure
    De la boue jusqu'aux genoux, puis jusqu'à la taille. {A} rentre avec une fleur jaune. Ce n'est pas la bonne. Elle est très jolie quand même.
    [modifier muddy_paws] [end]

* guide | Demander le chemin à l'Étrangère
  [lean sociability +1]
  L'Étrangère marche devant, sur des pierres que personne n'avait vues. Elle cueille la fleur, la tend à {A}, et repart avant qu'on la remercie.
  [remember etrangere.mud_guide] [cailloux 10] [end]

* lookalike | Rapporter « une plante qui y ressemble »  [mystery]
  [stress honest 15]
  [lean greed +1]
  {A} rapporte une fleur jaune cueillie au bord du chemin. La Sauge la regarde longtemps. Puis elle la met en bocal. « Ce n'est pas celle-là. Mais celle-là, je ne l'avais pas. »
  [grow learning] [end]
