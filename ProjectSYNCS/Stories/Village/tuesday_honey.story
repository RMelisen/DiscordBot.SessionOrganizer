// Le miel du mardi — wave 2 (ancien only), new. The Tilleul brings the Mère Lichen a pot of honey every
// week and is the only one who still calls her Ysaure; what binds them is never settled (fiches 1, 12,
// lore § 11). Reads not_yet's lichen.sat_quiet.

# arc: tuesday_honey
title: Le miel du mardi
family: village
stage: elder
trigger: draw
size: short

## memories
tilleul.carried_honey | tilleul | private | +3 | A porté son pot de miel à Ysaure, le mardi où son genou ne voulait plus monter. Sans rien dire.
lichen.stayed_tuesday | lichen   | private | +2 | Est resté{a:|e} s'asseoir avec elle, le mardi où le miel est arrivé dans d'autres mains.

## scene: honey
title: Le miel du mardi
pace: now

Le genou du Tilleul ne veut plus monter jusqu'à la Lisière. Il tend à {A} le pot de miel qu'il porte chaque mardi à la Mère Lichen.
« Pour Ysaure. » Il est le seul à l'appeler encore comme ça. Il le dit très bas, comme si les abeilles pouvaient entendre.

* carry | Le porter sans rien dire
  [lean honor +2]
  {A} porte le pot jusqu'à la cabane et le pose sur la pierre. La Mère Lichen le regarde, puis regarde le chemin, longtemps. « Le genou. » Ce n'est pas une question.
  [remember tilleul.carried_honey] [end]

* stay | Le porter, et rester s'asseoir avec elle
  [lean compassion +2]
  {A} pose le pot, et s'assoit.
  [if memory lichen.sat_quiet] La Mère Lichen a déjà sorti une deuxième pierre. Elle savait.
  {a:Ils|Elles} mangent le miel du bout du doigt, sans parler. Au soir, elle dit seulement : « Dis-lui mardi. »
  [stress -15] [remember lichen.stayed_tuesday] [end]

* ask | Lui demander ce qu'il faut dire  [mystery]
  [lean sociability +1]
  Le Tilleul ouvre la bouche. La referme. « Dis-lui… » Il regarde ses ruches, très longtemps. « Non. Dis-lui que c'est du miel. »
  {A} le lui dit. La Mère Lichen sourit, comme si elle avait entendu l'autre phrase.
  [modifier light_heart] [end]
