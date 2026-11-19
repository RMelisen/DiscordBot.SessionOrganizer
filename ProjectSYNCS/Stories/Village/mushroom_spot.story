// Le coin à champignons — wave 2 (adulte, ancien). Rewrites grown_forage with the Étrangère, who knows
// mushroom spots she only shows to those who can keep quiet (fiche 11). A secret option for whoever
// followed her light as a bébé (lost_at_lisiere).

# arc: mushroom_spot
title: Le coin à champignons
family: village
stage: adult, elder
trigger: draw
size: short

## memories
etrangere.kept_the_spot | etrangere | private | +3 | A juré de ne jamais montrer son coin à champignons, et ne l'a jamais montré.

## scene: dawn
title: Le coin à champignons
pace: now

L'Étrangère frappe chez {A} bien avant l'aube, un panier au bras. Elle propose de l'emmener à la cueillette.
Une condition, dite très bas : ne jamais montrer le chemin à personne.

* swear | Jurer le secret
  [lean honor +2]
  {A} jure, une main sur le cœur, comme elle. Le coin est sous trois hêtres, à la Lisière, et il ne ressemble à rien. Le panier, lui, déborde.
  [remember etrangere.kept_the_spot] [item food.morel] [end]

* note | Accepter, et noter le chemin en douce
  [stress honest 15]
  [lean greed +2]
  {A} casse une brindille à chaque tournant. Au retour, les brindilles ont disparu, toutes. L'Étrangère ne dit rien. Le panier est plein quand même.
  [item food.morel] [modifier sly] [end]

* sleep | Refuser : avant l'aube, très peu pour soi
  [lean energy -2]
  {A} referme la porte et se recouche. À midi, un champignon attend sur le rebord de la fenêtre, tout seul, comme une carte de visite.
  [stress -10] [modifier well_rested] [end]

* three | Retourner au coin des trois morilles  [needs memory etrangere.followed_light "Suivi sa lumière, enfant"] [secret]
  [lean boldness +1]
  « Là où je t'ai trouvé{a:|e} ? » L'Étrangère sourit sous sa capuche. Le coin est toujours là, et cette fois, sous la mousse, il y a une truffe.
  [item food.truffle] [end]
