// Les canards du bief — wave 1 (bébé), new. The Chardon feeds the ducks of the millrace in secret and
// denies it; he grumbles first and helps after (fiche 4). The memories are for the mill events later.

# arc: mill_ducks
title: Les canards du bief
family: village
stage: baby
trigger: draw
size: short

## memories
chardon.kept_ducks | chardon | private | +3 | L'a surpris à nourrir les canards du bief, et ne l'a répété à personne.
chardon.told_ducks | chardon | private | -1 | A raconté à tout le hameau qu'il nourrit les canards du bief.

## scene: millrace
title: Les canards du bief
pace: now

Au moulin de la Sommeille, {A} surprend le Chardon accroupi au bord du bief, en train d'émietter du pain pour les canards.
Il se fige, les mains pleines de miettes. « Je ne nourris rien du tout. »

* secret | Garder le secret
  [lean honor +2]
  {A} hoche la tête, très sérieusement, et s'en va sans se retourner.
  Le soir, dans le panier de la Mère Lichen, il y a un sac de farine avec une poignée de trop.
  [remember chardon.kept_ducks] [end]

* feed | Nourrir les canards avec lui
  [lean compassion +2]
  {A} s'accroupit à côté du Chardon et tend la main. Il grogne, et lui donne la moitié de son pain.
  Ils restent là jusqu'à ce qu'il n'y ait plus de pain, et deux canards de plus.
  [modifier soothed] [end]

* tell | Le répéter à tout le hameau
  [lean sociability +2]
  À midi, tout le hameau sait. Le Chardon grogne toute la journée, puis glisse cinq cailloux à {A}, « pour la paix ».
  Le lendemain, les canards ont un nichoir neuf, que personne n'a construit.
  [cailloux 5] [remember chardon.told_ducks] [end]
