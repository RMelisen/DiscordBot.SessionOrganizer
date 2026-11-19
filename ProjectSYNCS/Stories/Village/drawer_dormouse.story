// Le loir du tiroir — wave 1 (bébé). Rewrites baby_dormouse in the Mère Lichen's cabin. Animals are
// animals (lore § 2); the Courge lays an extra place « au cas où » (fiche 6).

# arc: drawer_dormouse
title: Le loir du tiroir
family: village
stage: baby
trigger: draw
size: short

## memories
courge.brought_dormouse | courge | private | +2 | Lui a amené un loir à l'auberge. Elle lui a mis un couvert.

## scene: drawer
title: Le loir du tiroir
pace: now

Depuis trois jours, les chaussettes de {A} disparaissent une par une. Ce matin, {A} ouvre le tiroir, et découvre la vérité.
Un loir, roulé en boule dans un nid de chaussettes. Il ouvre un œil, bâille, et se rendort.

* name | Le laisser dormir et lui donner un nom
  [lean compassion +2]
  Il s'appelle Chaussette. {A} referme le tiroir tout doucement, et dort pieds nus, par solidarité.
  [stress -10] [modifier light_heart] [end]

* explain | Lui expliquer poliment que c'est un tiroir à chaussettes
  [lean rationality +1]
  {A} explique longuement : ici les chaussettes, là les loirs. Le loir écoute, un œil ouvert.
  Le lendemain, il a déménagé dans le tiroir du dessous, avec les chaussettes. C'est un compromis.
  [grow diplomacy] [end]

* show | Le porter au hameau pour le montrer  [mystery]
  [lean sociability +2]
  {A} porte le tiroir entier jusqu'à l'auberge. La Courge pousse un cri de joie, et met un couvert de plus, tout petit, à côté de la cheminée.
  « Au cas où il se réveille, mon chou. »
  [remember courge.brought_dormouse] [end]
