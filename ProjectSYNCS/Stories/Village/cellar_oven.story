// Le four de la cave — wave 1 (ado), new (from teen_burnt_cake / teen_bakery). At night Pain-Blond
// tries recipes he shows nobody; a pousse who catches him becomes his taster (fiche 7). Reads Arrival's
// pain_blond.ate_crust: a line, and a secret option for the « thing with nuts » he never talks about.

# arc: cellar_oven
title: Le four de la cave
family: village
stage: teen
trigger: draw
size: short

## memories
pain_blond.honest_taster | pain_blond | private | +3 | Lui a dit franchement ce qu'{a:il|elle} pensait de sa recette de la cave. Il en a fait son goûteur{a:|se}.
pain_blond.nut_thing     | pain_blond | private | +3 | A goûté la chose aux noix, celle dont il ne parle à personne.

## scene: cellar
title: Le four de la cave
pace: now

La nuit, il y a de la lumière sous le fournil. Dans la cave, Pain-Blond essaie une recette qu'il n'ose montrer à personne : un pain aux orties, vert jusqu'au cœur.
Il sursaute en voyant {A}, la plaque encore à la main.
[if memory pain_blond.ate_crust] Puis il sourit. « Toi… la miette sur le rebord. »

* honest | Goûter, et dire franchement
  [lean honor +2]
  « Trop d'orties. Pas assez de sel. Mais la croûte, oui. » Pain-Blond écoute comme on écoute un oracle, et note tout sur le mur, à la farine.
  « Reviens demain. J'en ferai un autre. »
  [remember pain_blond.honest_taster] [end]

* more | Goûter, et en redemander
  [stress temperate 10]
  [lean greed +1]
  {A} en reprend. Puis une troisième fois. Pain-Blond ne dit rien, mais ses oreilles deviennent toutes dorées.
  Il emballe le reste dans un torchon, « pour la Maison », et oublie d'en garder pour lui.
  [modifier hearty] [end]

* leave | Remonter sans bruit
  [lean compassion +1]
  {A} recule d'un pas, et fait semblant de n'avoir rien vu. Pain-Blond, soulagé, refait semblant de ne pas avoir été vu.
  Le secret reste dans la cave, où il était bien.
  [modifier clear_conscience] [end]

* nuts | Goûter la chose aux noix  [needs memory pain_blond.ate_crust "La croûte des veillés"] [secret] [mystery]
  [lean boldness +2]
  Pain-Blond hésite très longtemps. Puis il ouvre une boîte en fer et en sort un petit carré brun.
  {A} goûte. Pain-Blond retient son souffle. Personne d'autre ne saura jamais ce que c'est, mais {A} en redemande.
  [remember pain_blond.nut_thing] [modifier inspired] [end]
