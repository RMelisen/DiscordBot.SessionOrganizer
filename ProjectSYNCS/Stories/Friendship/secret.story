// Replaces teen_secret and reply_secret.
# arc: secret
title: Un secret trop lourd
family: friendship
stage: teen
trigger: draw
size: short
other: friends, bestfriends

## scene: secret
title: Un secret trop lourd
was: teen_secret
pace: now

Depuis des jours, {A} porte un secret. Un petit secret, mais lourd, comme un gland qui serait en plomb. {B} est {b:le seul|la seule} à qui {A} pourrait le dire.

* tell | Tout raconter à {B}
  [lean sociability +2]
  {A} entraîne {B} sous le Pont-Vieux et chuchote tout, d'une traite.
  [-> answer]

* journal | L'écrire dans un carnet, puis cacher le carnet
  [lean sociability -1] [lean rationality +1]
  {A} écrit le secret, ferme le carnet et le glisse sous une latte du plancher. Le secret pèse beaucoup moins. Le plancher grince un peu plus.
  [modifier soothed] [end]

* keep | Le garder pour soi
  [lean rationality +1] [stress gregarious 10]
  {A} garde le secret. Certains jours, ça pèse. D'autres jours, ça tient chaud.
  [grow intrigue] [end]

## scene: answer
title: Un secret confié
was: reply_secret
pace: now
handover

Sous le Pont-Vieux, {B} a chuchoté un secret à {A}, d'une traite, sans respirer.

* promise | Promettre de ne jamais le répéter  [accept]
  [lean honor +2]
  {A} promet, la main sur le cœur. {B} respire enfin. Le secret dort maintenant chez deux personnes, et dort beaucoup mieux.
  [affinity +15] [end]

* swap | Confier un secret en retour  [accept]
  [lean sociability +2]
  {A} se penche et chuchote à son tour. {B} écarquille les yeux. Les deux secrets se ressemblent comme deux glands.
  [affinity +20] [end]

* laugh | Éclater de rire : ce n'est vraiment pas grave  [refuse]
  [lean rationality +1]
  « C'est tout ? » {B} rougit, puis rit aussi, un peu {b:vexé|vexée}. D'un coup, le secret ne pèse plus rien.
  [affinity +5] [end]
