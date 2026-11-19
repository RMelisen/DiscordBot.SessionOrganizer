// La colporteuse — wave 2 (adulte, ancien), new. A belladonna who comes twice a year with ribbons, salt
// and news from elsewhere, and never says her name (lore § 12). Twice a year, so a long cooldown.

# arc: peddler
title: La colporteuse
family: village
stage: adult, elder
trigger: draw
size: short
cooldown: 90d

## scene: stall
title: La colporteuse
pace: now

La colporteuse est arrivée par la Côte, comme deux fois l'an : des rubans, du sel, des boutons de nacre, et les nouvelles d'ailleurs.
Elle ne dit jamais son nom. Les enfants essaient chaque fois.

* button | Acheter un bouton de nacre
  [lean greed +1]
  Elle choisit le bouton elle-même, sans demander, et le pose dans la main de {A}. « Celui-là. Il est pour toi depuis le printemps. »
  [item col.bouton] [end]

* trade | Échanger une histoire du village contre ses nouvelles
  [stress shy 10]
  [lean sociability +2]
  {A} raconte la querelle du pré. Elle raconte une ville où les toits sont bleus. Les deux histoires sont peut-être vraies.
  [grow learning] [end]

* name | Lui demander son nom  [mystery]
  [lean boldness +2]
  Elle sourit. « Je m'appelle comme la chose qu'on trouve au fond de la poche, quand on croyait l'avoir perdue. » Puis elle repart vers la Côte.
  {A} cherche toute la soirée. C'est un très bon nom.
  [modifier inspired] [end]
