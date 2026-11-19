// Où sont les autres ? — wave 1 (bébé). Rewrites baby_lost at the Lisière: the Étrangère glows
// faintly and knows mushroom spots she only shows to those who can keep quiet (fiche 11).

# arc: lost_at_lisiere
title: Où sont les autres ?
family: village
stage: baby
trigger: draw
size: short

## memories
etrangere.followed_light | etrangere | private | +3 | L'a suivie jusqu'au hameau, perdu{a:|e} dans les mûres, sans dire un mot. Elle lui a montré un coin à champignons.

## scene: brambles
title: Où sont les autres ?
pace: now

{A} cueillait des mûres à la Lisière avec tout le monde. {A} relève la tête : plus personne. Juste des ronces, et le bruit de la Sommeille, quelque part.
Entre les fougères, une petite lumière verte s'arrête, comme si elle attendait.

* light | Suivre la lumière verte
  [lean boldness +1]
  La lumière, c'est l'Étrangère, sous sa capuche. Elle ne pose aucune question. Elle tend la main, et marche.
  En chemin, elle s'arrête près d'une souche, écarte la mousse, et montre à {A} trois morilles. Un doigt sur les lèvres.
  [remember etrangere.followed_light] [item food.morel] [end]

* stream | Suivre le ruisseau, sans aide  [roll courage 3]
  [lean boldness +2]
  ? success
    Le ruisseau mène à la Sommeille, la Sommeille au Pont-Vieux. {A} touche la pierre en passant, comme les grands, et rentre {a:fier|fière} comme un meunier.
    [grow courage] [end]
  ? failure
    Le ruisseau mène surtout à d'autres ruisseaux. {A} rentre à la nuit tombée, les pieds trempés, en éternuant.
    [modifier woods_cold] [end]

* wait | Ne pas bouger et attendre
  [stress rowdy 10]
  [lean rationality +2]
  {A} s'assoit au milieu des ronces et mange les mûres une par une, pour patienter. Avant la dernière, la Mère Lichen est là.
  « Tu as bien fait. Les ronces ne vont nulle part. Toi non plus. »
  [modifier soothed] [end]
