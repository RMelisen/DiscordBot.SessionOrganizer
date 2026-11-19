// La colonne vide — wave 2 (adulte, ancien), new. Maître Frêne keeps the register of the pousses with a
// « veilleur » column he politely leaves empty (fiche 2). The keepers are never named as players (lore
// § 3). Reads comet's frene.comet_register.

# arc: empty_column
title: La colonne vide
family: village
stage: adult, elder
trigger: draw
size: short

## memories
frene.left_it_empty | frene | private | +2 | Lui a conseillé de laisser vide la colonne « veilleur » du registre. Il l'a trouvé{a:|e} très sage.

## scene: column
title: La colonne vide
pace: now

Dans le registre des pousses, Maître Frêne tient une colonne « veilleur », qu'il laisse poliment vide depuis toujours.
Aujourd'hui, la plume en l'air au-dessus de la ligne de {A}, il hésite, et demande ce qu'il conviendrait d'y mettre.
[if memory frene.comet_register] « Ton nom est déjà à côté d'une comète. Je ne voudrais pas mal faire. »

* empty | Laisser la colonne vide
  [lean honor +2]
  « Vide », dit {A}. Maître Frêne repose sa plume, soulagé. « Il convient. On n'inscrit pas quelqu'un sans lui demander. »
  [remember frene.left_it_empty] [end]

* someone | Faire écrire « quelqu'un »
  [lean zeal +2]
  Maître Frêne écrit « quelqu'un », de sa plus belle main. C'est le seul mot de la colonne. Il le regarde longtemps, et a l'air content.
  [modifier inspired] [end]

* eye | Y dessiner un petit œil  [mystery]
  [lean boldness +2]
  {A} prend la plume et dessine un petit œil, ouvert. Maître Frêne ouvre la bouche pour protester.
  À la fenêtre, le Fou applaudit très fort. « Ah ! Ça, c'est ressemblant. » Personne ne sait à quoi.
  [modifier light_heart] [end]
