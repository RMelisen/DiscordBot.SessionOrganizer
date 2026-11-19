// Gratte, gratte — wave 1 (bébé). Rewrites baby_night_noise in Humeval: the noise under the window is
// the Fou, who comes to the Lisière on rainy nights to watch for the lights (lore § 9.4, fiche 13).
// Reads Arrival's fou.looked_up_with_him for one line. Nothing is revealed: the lights stay a maybe.

# arc: night_scratching
title: Gratte, gratte
family: village
stage: baby
trigger: draw
size: short

## memories
fou.watched_lights | fou | private | +2 | Est sorti{a:|e} guetter les lumières avec lui, une nuit de pluie, sous une couverture.

## scene: window
title: Gratte, gratte
pace: now

En pleine nuit, quelque chose gratte sous la fenêtre de la cabane. Gratte, gratte. Puis ça tousse, poliment.
Sous la fenêtre, à genoux dans la mousse mouillée, le Fou guette le sol de la Lisière.
[if memory fou.looked_up_with_him] Il lève les yeux vers {A}. « Ah, c'est toi. Toi, tu sais regarder. »

* out | Sortir le rejoindre, la couverture sur la tête  [mystery]
  [lean boldness +2]
  {A} s'assoit à côté du Fou, sous la couverture. Il pleut. Longtemps, il ne se passe rien.
  Puis, sous la mousse, quelque chose pâlit à peine, comme un fil qui se souvient d'avoir été blanc. Ou c'était la lune. « Tu as vu ? » chuchote le Fou. « Moi non plus. C'est bon signe. »
  [remember fou.watched_lights] [modifier inspired] [end]

* stay | Rester sous la couverture
  [stress curious 10]
  [lean energy -1]
  {A} remonte la couverture jusqu'aux yeux. Dehors, le Fou compte la pluie à voix basse, et c'est la meilleure berceuse du monde.
  [modifier well_rested] [end]

* count | Compter les gouttes avec lui, par la fenêtre  [needs trait pensive]
  [lean rationality +2]
  « Une… deux… » Le Fou compte les gouttes sur le carreau, {A} celles du toit. Ils ne tombent jamais d'accord, et c'est très instructif.
  Au matin, la Mère Lichen trouve deux colonnes de chiffres tracées dans la buée.
  [grow learning] [end]
