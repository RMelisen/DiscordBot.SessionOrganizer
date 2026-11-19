// Jacquet veut être cueilleur — wave 2 (ancien only). Rewrites elder_student: Jacquet, the Ortie's
// eldest, fourteen, dreams of being a forager (lore § 12); an ancien passes things on (lore § 6).

# arc: jacquet_forager
title: Jacquet veut être cueilleur
family: village
stage: elder
trigger: draw
size: short

## memories
ortie.taught_jacquet  | ortie     | private | +3 | A appris les champignons à Jacquet. Il est rentré le panier plein, et fier comme sa mère.
etrangere.sent_jacquet | etrangere | private | +2 | Lui a amené Jacquet pour qu'elle en fasse un cueilleur. Elle a accepté, à sa façon : sans un mot.

## scene: doorstep
title: Jacquet veut être cueilleur
pace: now

Depuis une semaine, Jacquet, l'aîné de l'Ortie, vient s'asseoir devant chez {A}, sans rien dire. Aujourd'hui, il ose.
Il veut être cueilleur. Et il veut que ce soit {A} qui lui apprenne.

* mushrooms | Lui apprendre les champignons
  [lean compassion +2]
  {A} l'emmène à la Lisière, et lui montre : celui-là oui, celui-là jamais, celui-là seulement si la Sauge est d'accord. Jacquet rentre le panier plein.
  L'Ortie gronde pour la boue, et fait une omelette pour tout le monde.
  [remember ortie.taught_jacquet] [end]

* etrangere | L'emmener chez l'Étrangère
  [lean sociability +1]
  {A} frappe à la cabane du Communal avec Jacquet. L'Étrangère le regarde longtemps, de sous sa capuche, puis lui tend un panier. C'est oui.
  [remember etrangere.sent_jacquet] [end]

* patience | Lui apprendre d'abord la patience  [mystery]
  [stress impatient 10]
  [lean rationality +2]
  {A} et Jacquet s'assoient à la Lisière et ne font rien. Une heure. Deux. Jacquet trépigne, puis se calme, puis voit : il y a des champignons partout, il suffisait d'attendre.
  [grow learning] [end]
