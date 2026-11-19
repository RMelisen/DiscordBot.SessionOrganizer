// Le caillou de compagnie — wave 1 (bébé). Rewrites baby_pebble: cailloux are pebbles of the
// Sommeille stamped by Roncebelle (lore § 7). Hiding it under the Mère Lichen's pile brings a second
// beat the next day: there are two now. She never explains.

# arc: pet_pebble
title: Le caillou de compagnie
family: village
stage: baby
trigger: draw
size: short

## memories
lichen.left_one | lichen | private | +2 | A repris son caillou sous le tas, et lui a laissé l'autre.

## scene: pebble
title: Le caillou de compagnie
pace: now

{A} shoote dans un galet. Il roule, s'arrête, et a l'air d'attendre la suite. C'est décidé : c'est un ami.
Petit détail : il porte la marque de Roncebelle. Au hameau, un caillou comme ça, c'est de l'argent.

* name | Lui donner un nom et l'emmener partout
  [lean compassion +2]
  Il s'appelle Bernard. Bernard voit la Lisière, le Pont-Vieux et l'intérieur d'une poche. Bernard a une vie bien remplie.
  [item col.galet] [end]

* spend | Le dépenser : c'est quand même un caillou
  [lean greed +2]
  {A} présente Bernard au marché. Bernard part avec la marchande de rubans. {A} lui fait au revoir de la main, puis compte la monnaie.
  [cailloux 10] [end]

* hide | Le cacher sous le tas de la Mère Lichen  [mystery]
  [lean boldness +1]
  {A} glisse le galet sous le tas, devant la cabane, bien au fond. Personne n'a rien vu. La Mère Lichen tricote, et ne lève pas les yeux.
  [-> two]

## scene: two
title: Deux galets
pace: next_day

Le lendemain, sous le tas, il y a deux galets là où {A} en avait caché un.
La Mère Lichen tricote devant sa porte. Elle n'a toujours pas levé les yeux.

* both | Prendre les deux
  [lean greed +1]
  {A} les met dans la même main. Ils tintent. Ils ont l'air de bien s'entendre.
  [cailloux 5] [item col.galet] [end]

* mine | Reprendre le sien, et lui laisser l'autre
  [lean compassion +2]
  {A} reprend le galet de la veille et pousse l'autre vers les pieds de la Mère Lichen. Elle hoche la tête, une seule fois.
  [remember lichen.left_one] [end]

* ask | Lui demander d'où vient l'autre
  [lean sociability +1]
  « Il avait l'air seul », dit la Mère Lichen. Elle n'en dit pas plus, mais elle garde {A} contre son genou tout l'après-midi.
  [modifier cherished] [end]
