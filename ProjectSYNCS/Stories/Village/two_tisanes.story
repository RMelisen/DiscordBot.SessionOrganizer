// Les deux tisanes — wave 1 (bébé), new. The Mère Lichen and the Sauge never agree on a tisane
// (fiches 1 and 5, lore § 11). The Sauge's remedies work and are undrinkable.

# arc: two_tisanes
title: Les deux tisanes
family: village
stage: baby
trigger: draw
size: short

## memories
lichen.drank_hers | lichen | private | +2 | A bu sa tisane jusqu'au fond, un jour de rhume, alors que celle de la Sauge attendait à côté.
sauge.drank_hers  | sauge  | private | +2 | A bu son remède d'un trait, un jour de rhume, et n'a fait la grimace qu'après.

## scene: sniffle
title: Les deux tisanes
pace: now

{A} renifle depuis ce matin. La Mère Lichen a posé une tisane fumante à gauche. La Sauge, montée exprès du hameau, en a posé une autre à droite.
Elles ne se regardent pas. Elles attendent.

* lichen | Boire celle de la Mère Lichen
  [lean compassion +1]
  Ça sent la mousse et le dimanche. {A} boit tout, et s'endort sur le coin du châle.
  La Sauge reprend son bol sans un mot, et le renifle d'un air vexé.
  [stress -10] [remember lichen.drank_hers] [modifier soothed] [end]

* sauge | Boire celle de la Sauge
  [lean rationality +1]
  C'est vert, c'est épais, c'est atroce. {A} fait une grimace qui remporterait n'importe quel concours.
  Le soir, le rhume est parti. « La Souche ne fait pas les tisanes », dit la Sauge en rangeant ses pots. « Moi, si. »
  [remember sauge.drank_hers] [grow learning] [end]

* mix | Mélanger les deux  [mystery]
  [lean boldness +2]
  {A} verse un bol dans l'autre, et boit. Silence. Les deux femmes goûtent à leur tour.
  Pour la première fois, elles sont d'accord sur une tisane : c'est imbuvable. Elles en rient tout l'après-midi.
  [modifier light_heart] [end]
