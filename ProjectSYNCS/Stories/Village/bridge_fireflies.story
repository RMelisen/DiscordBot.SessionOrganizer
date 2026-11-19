// Les lucioles du Pont-Vieux — wave 1 (ado). Rewrites teen_fireflies: one night a year, after
// curfew. The Bruyère says « mange » for « je t'aime », and her tenderness is never said (fiche 8).

# arc: bridge_fireflies
title: Les lucioles du Pont-Vieux
family: village
stage: teen
trigger: draw
size: short

## memories
bruyere.on_the_bridge | bruyere | private | +3 | L'a trouvée sur le Pont-Vieux, la nuit des lucioles, bien après l'heure. Personne n'en a jamais reparlé.

## scene: night
title: Les lucioles du Pont-Vieux
pace: now

Une nuit par an, les lucioles se donnent rendez-vous sur le Pont-Vieux. Tout le monde le sait.
C'est ce soir, bien après l'heure où l'on doit être au lit.

* sneak | Sortir sans bruit par la fenêtre  [mystery]
  [lean boldness +2]
  {A} court jusqu'au pont. Les lucioles sont là, des centaines. Et sur le parapet, quelqu'un en châle les regarde déjà.
  C'est la Bruyère. Elle regarde {A}, puis les lucioles, et fait une place à côté d'elle.
  [remember bruyere.on_the_bridge] [end]

* ask | Demander la permission
  [lean honor +2]
  « Non », dit la Bruyère. Puis, au bout d'un moment : « Mets ton écharpe. Et reviens avant que je compte. »
  Elle ne dit pas jusqu'à combien.
  [modifier cherished] [end]

* dream | Rester au lit et en rêver
  [lean energy -2]
  {A} reste au lit et ferme les yeux. Les lucioles arrivent quand même, une par une, derrière les paupières.
  [stress -10] [modifier well_rested] [end]
