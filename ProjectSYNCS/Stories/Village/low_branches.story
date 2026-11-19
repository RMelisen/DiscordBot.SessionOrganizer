// Les branches basses — wave 2 (adulte only), new. The Tilleul pretends not to see the twins steal his
// apples and prunes the low branches so they can reach (fiche 12, lore § 12).

# arc: low_branches
title: Les branches basses
family: village
stage: adult
trigger: draw
size: short

## memories
tilleul.understood_branches | tilleul | private | +2 | A compris pourquoi il laisse les branches basses à ses pommiers, et n'en a rien dit aux jumelles.

## scene: orchard
title: Les branches basses
pace: now

Le Tilleul taille son verger. Il coupe haut, toujours haut, et laisse exprès les branches basses, chargées de pommes.
Puis il tend un bâton à {A}. « Tu gardes le verger, cet après-midi. Il y a des voleuses. » Il a l'air ravi.

* guard | Garder le verger, très sérieusement
  [lean zeal +2]
  {A} monte la garde avec le bâton. Les jumelles du Communal passent par la haie, par le toit de la ruche, et par une ruse que {A} admire encore. Il manque six pommes.
  [grow intrigue] [end]

* help | Aider les jumelles
  [lean compassion +2]
  {A} tient le panier pendant que les jumelles cueillent. Le Tilleul passe, regarde ailleurs avec beaucoup de soin, et taille une branche encore un peu plus bas.
  [modifier light_heart] [end]

* understood | Lui dire qu'on a compris
  [lean honor +1]
  « Les branches basses. C'est pour elles. » Le Tilleul pose un doigt sur ses lèvres, et se retourne vers ses ruches. « IL N'Y A PAS DE VOLEUSES DANS MON VERGER. » Toute la place a entendu.
  [remember tilleul.understood_branches] [end]
