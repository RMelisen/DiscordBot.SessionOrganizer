// La poutre du moulin — wave 2 (ancien only), new. The Chardon carves on a beam the name of every
// apprentice he had, and claims it is for his accounts; he is proud of Mathurin without saying so
// (fiche 4). Reads mill_ducks' chardon.kept_ducks.

# arc: mill_beam
title: La poutre du moulin
family: village
stage: elder
trigger: draw
size: short

## memories
chardon.carved_name | chardon | private | +3 | Lui a demandé de graver son nom sur la poutre du moulin. Il l'a fait, « pour ses comptes ».

## scene: beam
title: La poutre du moulin
pace: now

Au moulin, le Chardon grave sur une poutre le nom de chaque apprenti qu'il a eu. « Pour mes comptes », dit-il. Il y en a beaucoup.
{A} les lit un par un, à la lumière de la porte. Le dernier est Mathurin, gravé plus profond que les autres.
[if memory chardon.kept_ducks] À côté d'un des noms, quelqu'un a gravé un tout petit canard. Le Chardon regarde ailleurs.

* carve | Lui demander de graver son nom
  [lean sociability +2]
  Le Chardon grogne, sort son couteau, et grave le nom de {A}, lentement, au bout de la poutre. « Pour les comptes. Tu m'as aidé, une fois. Ou deux. »
  [remember chardon.carved_name] [end]

* count | Compter les noms
  [lean rationality +2]
  {A} compte. Trente-deux. Le Chardon recompte derrière, et trouve trente-deux aussi, ce qui le met de bonne humeur pour la semaine.
  [grow stewardship] [end]

* heart | Graver un petit cœur près du nom de Mathurin  [needs trait lustful] [mystery]
  [lean compassion +2]
  {A} grave un tout petit cœur, à côté du nom de Mathurin, et un A minuscule. Le Chardon le voit, évidemment. Il ne dit rien. Il fera semblant de ne pas le voir pendant des années.
  [modifier light_heart] [end]
