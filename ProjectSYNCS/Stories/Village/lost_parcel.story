// Le colis égaré — wave 2 (adulte, ancien). Rewrites grown_parcel, the design's own example of options
// that all felt the same: here each brings a different kind of result. The Intendant's clerk drops
// everything (lore § 12); the Courge knows everyone's business (fiche 6).

# arc: lost_parcel
title: Le colis égaré
family: village
stage: adult, elder
trigger: draw
size: short

## memories
courge.found_owner | courge | private | +2 | A fait le tour du hameau avec un colis sans adresse jusqu'à trouver à qui il était. Elle l'a aidé{a:|e}, bien sûr.

## scene: doorstep
title: Le colis égaré
pace: now

Un colis attend devant la porte de {A}. Pas d'adresse, juste une ficelle et un nœud trop serré. Ça tinte quand on le secoue.
Plus loin sur le chemin, le clerc de l'Intendant court, ses registres sous le bras, et en laisse tomber un deuxième.

* open | L'ouvrir  [roll courage 3]
  [lean boldness +2]
  ? success
    Dedans, dans de la paille, une vieille pièce frappée d'une marque que personne ne connaît. Le clerc, revenu en courant, jure qu'elle n'est pas à Roncebelle. Elle est donc à {A}.
    [item col.piece_ancienne] [end]
  ? failure
    Dedans, un réveil de Roncebelle, réglé sur trois heures du matin. Il sonne à trois heures du matin. Puis à quatre, par principe.
    [modifier sleepless] [end]

* owner | Chercher le propriétaire
  [lean honor +2]
  À l'auberge, la Courge reconnaît le nœud au premier coup d'œil. « C'est la Sauge, mon chou. Elle noue comme elle écrit. »
  La Sauge ouvre le colis devant {A} : des bocaux vides. Elle est ravie.
  [remember courge.found_owner] [end]

* keep | Le garder pour plus tard
  [stress just 15]
  [lean greed +2]
  {A} range le colis sous le lit. Une semaine plus tard, le clerc revient le chercher, rouge de confusion, et paie la garde de sa poche.
  [cailloux 15] [end]
