// Les étiquettes de la Sauge — wave 2 (adulte only), new. The Sauge labels everything and her writing is
// unreadable, even to her; she dislikes what she cannot put in a jar (fiche 5). Apprenti herboriste is a
// trade the Gardening passion opens (lore § 6).

# arc: sauge_labels
title: Les étiquettes de la Sauge
family: village
stage: adult
trigger: draw
size: short

## memories
sauge.relabelled | sauge | private | +3 | A réécrit toutes les étiquettes de la Maison aux Simples en grosses lettres. Elle peut enfin se relire.

## scene: jars
title: Les étiquettes de la Sauge
pace: now

À la Maison aux Simples, il y a trois cents bocaux, et une étiquette sur chacun. La Sauge les a toutes écrites. Personne ne peut les lire, même pas elle.
« Celui-là, c'est soit pour la toux, soit pour la confiture. » Elle a besoin d'aide.

* smell | Tout reconnaître à l'odeur  [needs passion gardening]
  [lean rationality +2]
  {A} ouvre chaque bocal, renifle, et donne le nom. La Sauge vérifie, d'abord méfiante, puis admirative. C'était bien la toux.
  [grow learning] [end]

* taste | Goûter pour vérifier  [mystery]
  [lean boldness +2]
  {A} goûte un bocal. Puis un autre, pour comparer. La Sauge note tout, très intéressée, et ne dit pas lesquels étaient des remèdes. {A} dort très bien, ce soir-là.
  [modifier well_rested] [end]

* relabel | Tout réécrire en grosses lettres
  [lean zeal +1]
  {A} recopie trois cents étiquettes en lettres bien rondes. La Sauge relit tout, une par une, et paie en silence. Puis elle sourit, ce qui n'est pas prévu.
  [remember sauge.relabelled] [cailloux 10] [end]
