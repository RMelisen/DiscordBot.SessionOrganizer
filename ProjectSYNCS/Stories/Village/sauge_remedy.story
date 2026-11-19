// Le remède de la Sauge — wave 2 (adulte, ancien). Rewrites grown_marsh_remedy with the Sauge, whose
// remedies work and are all undrinkable; her wish is the one that cures and tastes good; the Tilleul's
// honey is the only thing that makes them go down (fiches 5, 12). Reads two_tisanes' sauge.drank_hers.

# arc: sauge_remedy
title: Le remède de la Sauge
family: village
stage: adult, elder
trigger: draw
size: short

## memories
sauge.honeyed_remedy | sauge | private | +3 | Lui a rapporté du miel du Tilleul pour adoucir son remède. Il avait presque bon goût.
sauge.double_dose    | sauge | private | +2 | A réclamé la double dose de son remède, et l'a bue sans une grimace.

## scene: remedy
title: Le remède de la Sauge
pace: now

{A} a mal au dos depuis une semaine. À la Maison aux Simples, la Sauge a le remède : un bol brun, épais, qui sent la cave et la racine.
Il marche très bien. Le hameau a même un concours de grimaces qui ne dit pas son nom.
[if memory sauge.drank_hers] « Toi, tu bois sans faire d'histoires. Ça me repose. »

* gulp | Boire d'un trait
  [lean boldness +2]
  {A} boit d'un trait. Le monde tourne un peu, puis se remet droit. Le dos aussi. La grimace, elle, dure jusqu'au soir.
  [modifier soothed] [end]

* honey | Aller chercher du miel du Tilleul pour l'adoucir
  [lean compassion +2]
  Le Tilleul donne le miel avec une histoire, qu'il faut écouter. Avec le miel, le remède a presque bon goût.
  La Sauge goûte, fronce les sourcils, regoûte, et note la recette dans un carnet que personne ne saura lire.
  [remember sauge.honeyed_remedy] [end]

* plank | Refuser, et dormir sur une planche
  [lean energy -1]
  {A} dort trois nuits sur une planche. Le dos guérit, par entêtement. La Sauge hausse les épaules. « Ça marche aussi. Moins bien. »
  [grow courage] [end]

* double | Réclamer la double dose  [needs memory sauge.drank_hers "Bu son remède, enfant"] [secret]
  [lean zeal +2]
  La Sauge sert la double dose sans un mot. {A} la boit sans une grimace. Pour la première fois de sa vie, la Sauge applaudit.
  [remember sauge.double_dose] [modifier well_rested] [end]
