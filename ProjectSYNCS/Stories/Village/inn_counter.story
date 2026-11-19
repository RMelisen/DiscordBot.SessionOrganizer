// Cinq minutes à l'auberge — wave 1 (bébé). Rewrites baby_ice_cream at the Courge's inn: she wants
// everything that matters to happen at hers, and judges a Plynling by its appetite (fiche 6).

# arc: inn_counter
title: Cinq minutes à l'auberge
family: village
stage: baby
trigger: draw
size: short

## memories
courge.told_story | courge | private | +2 | A tenu toute l'auberge suspendue à une histoire, le jour où elle lui a confié le comptoir.

## scene: counter
title: Cinq minutes à l'auberge
pace: now

La Courge doit s'absenter « cinq minutes » et pose {A} sur un tabouret, derrière le comptoir. « Tu ne touches à rien, mon chou. Sauf aux clients. »
Trois marmites, une louche, et le Noyer qui pousse déjà la porte.

* serve | Servir les clients
  [stress pensive 10]
  [lean energy +2]
  Une louche pour le Noyer, une pour le passeur, une demie pour le clerc de l'Intendant, qui la renverse. {A} sert sans s'arrêter, et les pourboires tombent dans un bol.
  [cailloux 15] [end]

* taste | Goûter chaque marmite, pour bien conseiller
  [lean greed +1]
  {A} goûte la première, pour savoir. La deuxième, pour comparer. La troisième, pour être juste.
  Quand la Courge revient, elle regarde les marmites, puis {A}, et elle a l'air très fière. « Ça, c'est un appétit. »
  [modifier hearty] [end]

* story | Raconter une histoire à toute la salle  [needs trait charming]
  [lean sociability +2]
  {A} grimpe sur le comptoir et raconte la Souche, une flaque et un scarabée, dans le désordre. Personne ne mange plus.
  La Courge revient une heure plus tard. Elle trouve une salle silencieuse, et le Fou qui pleure de rire.
  [remember courge.told_story] [end]
