// Les chaussures du Fou — wave 2 (adulte, ancien), new. The Fou finds everything that is lost, except
// his own shoes (fiche 13). Laughed with, never at.

# arc: fou_shoes
title: Les chaussures du Fou
family: village
stage: adult, elder
trigger: draw
size: short

## memories
fou.shoes_back | fou | private | +2 | Lui a rapporté ses chaussures, repêchées dans la Sommeille. Il les a perdues de nouveau le soir même.

## scene: river
title: Les chaussures du Fou
pace: now

Le Fou retrouve tout ce qui est perdu, sauf ses propres chaussures. Tout le hameau le sait.
Ce matin, {A} vient d'en repêcher une paire dans la Sommeille, près du Pont-Vieux. Elles ont une tête de chaussures du Fou.

* return | Les lui rapporter
  [lean compassion +2]
  Le Fou les regarde comme de vieilles amies. « Vous étiez où ? » Il les met, danse un peu, et les perd de nouveau le soir même, sur la place.
  [remember fou.shoes_back] [end]

* trade | Lui demander de retrouver quelque chose en échange  [mystery]
  [lean greed +1]
  « Ah. » Le Fou ferme les yeux, compte jusqu'à neuf, et part en courant. Il revient avec une vieille clé que {A} ne savait même pas avoir perdue.
  Elle ouvre quelque chose, c'est sûr. Il ne dit pas quoi.
  [item col.cle_rouillee] [end]

* pots | En faire deux pots de fleurs
  [lean energy +1]
  {A} les remplit de terre et y plante deux pâquerettes. Le Fou passe devant, s'arrête, et les trouve magnifiques. Il ne les reconnaît pas du tout.
  [modifier light_heart] [end]
