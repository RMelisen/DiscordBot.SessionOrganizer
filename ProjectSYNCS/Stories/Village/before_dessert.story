// Avant le dessert — wave 2 (adulte, ancien), new. The Étrangère always leaves before dessert, by
// shyness; her wish is to feel she is from here. The Courge knows where she comes from and never tells
// (fiches 6, 11). Nothing is revealed.

# arc: before_dessert
title: Avant le dessert
family: village
stage: adult, elder
trigger: draw
size: short

## memories
etrangere.brought_dessert | etrangere | private | +3 | L'a rattrapée sur le chemin avec deux parts de tarte, un soir où elle partait avant le dessert.
courge.almost_told        | courge    | private | +1 | Lui a demandé d'où venait l'Étrangère. Elle a failli le dire. Elle ne l'a pas dit.

## scene: dinner
title: Avant le dessert
pace: now

Au dîner de l'auberge, l'Étrangère se lève, comme toujours, juste avant le dessert. Elle remet sa capuche, salue d'une main sur le cœur, et sort.
La Courge la regarde partir. Elle sait d'où elle vient. Elle ne le dit pas.

* chase | La rattraper avec deux parts de tarte
  [lean compassion +2]
  {A} la rattrape au bout du chemin. Elles sont deux parts, il fait nuit, et l'Étrangère luit un peu. Elle mange sa part sur le muret, sans rien dire.
  « Chez moi, on ne mange pas le dessert. Ici, si. » Elle a l'air de trouver ça très bien.
  [remember etrangere.brought_dessert] [end]

* keep | Garder sa place et sa part
  [stress compassionate 10]
  [lean honor +1]
  {A} pose une assiette devant la chaise vide, et défend la part toute la soirée contre le Noyer. Au moment de fermer, l'Étrangère revient. Juste pour la part.
  [modifier light_heart] [end]

* ask | Demander à la Courge pourquoi  [mystery]
  [lean sociability +2]
  La Courge ouvre la bouche. La referme. La rouvre. « Mon chou, il y a des choses… » Elle s'arrête, mord dans un chou à la crème, et change de sujet trois fois.
  C'est la première fois qu'on la voit garder quelque chose. Ça lui coûte beaucoup.
  [remember courge.almost_told] [end]
