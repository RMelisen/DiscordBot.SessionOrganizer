// Le panier oublié — wave 1 (bébé). Rewrites baby_berry_stall with the Noyer, who leaves a basket of
// nuts « forgotten » by the path for people to take, and tells for a month what he gave (fiche 9).

# arc: forgotten_basket
title: Le panier oublié
family: village
stage: baby
trigger: draw
size: short

## memories
noyer.took_a_nut | noyer | private | +2 | A pris une seule noix dans son panier « oublié ». Il le raconte encore.

## scene: basket
title: Le panier oublié
pace: now

Au bord du chemin, un panier plein de noix, et personne autour. {A} regarde à gauche, à droite, et encore à gauche.
{A} ne sait pas encore que le Noyer en « oublie » un là chaque semaine, exprès.

* take | Prendre une noix, juste une
  [lean greed +1]
  Une seule. {A} la serre fort et part en courant. Derrière la haie, le Noyer, ravi, se frotte les mains.
  Pendant un mois, il raconte à tout le hameau qu'une pousse a pris une noix chez lui. « Une seule. Bien élevée. »
  [remember noyer.took_a_nut] [end]

* guard | Garder le panier jusqu'au retour de son propriétaire
  [stress rowdy 10]
  [lean honor +2]
  {A} s'assoit à côté du panier et monte la garde. Une heure. Deux. Le Noyer finit par sortir de derrière la haie, un peu gêné.
  Il paie la garde, très cher, « pour que ça se sache ».
  [cailloux 15] [end]

* return | Rapporter le panier chez le Noyer  [needs trait bossy]
  [lean zeal +1]
  {A} traîne le panier jusqu'à la grande maison et frappe. « Vous avez perdu ça. Faites attention. »
  Le Noyer ne sait plus du tout quoi dire. Il remercie, et le replace au bord du chemin dès que {A} a tourné le dos.
  [grow stewardship] [end]
