// Les ruches du Tilleul — wave 2 (adulte, ancien), new. The Tilleul wants someone to show his hives to,
// who listens to the end; he restarts a story he just finished (fiche 12). Reads apple_tree's
// tilleul.called_from_tree.

# arc: tilleul_hives
title: Les ruches du Tilleul
family: village
stage: adult, elder
trigger: draw
size: short

## memories
tilleul.listened_to_end | tilleul | private | +3 | A écouté son histoire des ruches jusqu'au bout. Les deux fois.

## scene: hives
title: Les ruches du Tilleul
pace: now

Le Tilleul n'a qu'une envie : quelqu'un à qui montrer ses ruches, et qui écoute jusqu'au bout. Il commence par la reine de l'an dernier.
L'histoire est longue. Au milieu, elle recommence.
[if memory tilleul.called_from_tree] « Toi, tu tombais de mon pommier. Et regarde-toi. »

* listen | Écouter jusqu'au bout
  [lean compassion +2]
  {A} écoute tout, la reine de l'an dernier, puis de l'an d'avant, puis de nouveau celle de l'an dernier. À la fin, le Tilleul a les yeux qui brillent.
  « Personne n'avait jamais entendu la fin. Moi non plus, je crois. »
  [remember tilleul.listened_to_end] [end]

* frames | L'aider à sortir les cadres
  [lean energy +1]
  {A} soulève les cadres, lourds de miel. Les abeilles tournent sans piquer, poliment. Le Tilleul en coupe un morceau, rien que pour {A}.
  [modifier hearty] [end]

* bees | Parler aux abeilles soi-même  [needs passion insects]
  [lean rationality +2]
  {A} parle aux abeilles tout bas, comme il faut. Le Tilleul écoute, la main en cornet, et ne comprend rien. Les abeilles, elles, rentrent toutes.
  [grow learning] [end]
