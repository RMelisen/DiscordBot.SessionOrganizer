# arc: fou_threads
title: Ce qu'a vu le Fou
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: threads
title: Ce qu'a vu le Fou
pace: now

Le Fou arrête {A} sur le Pont-Vieux, très sérieux. Pendant la visite avec {B}, il passait à la Lisière, et il l'a vu : sous la mousse, un fil de la Trame s'est allumé, juste un instant. « Ça veut dire quelque chose », dit-il. Il ne sait pas quoi.

* look | Aller voir à la Lisière, la nuit venue  [roll courage 3]
  [lean boldness +1] [lean zeal +1]
  ? success
    {A} attend à la Lisière jusqu'à la nuit noire, le nez dans la mousse. Rien, puis, peut-être, une lueur très pâle, le temps d'un battement de cils. {A} ne le dira à personne. Sauf au Fou.
    [grow courage] [modifier inspired] [end]
  ? failure
    {A} attend à la Lisière jusqu'à la nuit noire, puis un craquement dans les fougères, et {A} rentre au hameau au pas de course. C'était un hérisson. Très ordinaire, mais très soudain.
    [modifier on_edge] [end]

* explain | Lui expliquer que c'était juste une visite
  [lean rationality +2]
  {A} explique, patiemment : une visite, c'est deux Plynlings, une table, et rien qui brille. Le Fou écoute, hoche la tête, et répond : « Justement. » Puis il s'en va, enchanté de la conversation.
  [grow learning] [end]

* tell | Courir le raconter à {B}
  [lean sociability +1] [lean energy +1]
  {A} court le raconter à {B}, qui écoute avec des yeux ronds, éclate de rire, puis ne rit plus du tout. Le soir, chacun de son côté, {A} et {B} regardent un peu la mousse, au cas où.
  [affinity +5] [modifier light_heart] [end]
