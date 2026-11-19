// La page tachée — wave 2 (adulte, ancien), new. Maître Frêne once spilled ink on the old register of
// the raies and redrew the page from memory; nobody noticed, he thinks of it every spring (fiche 2).
// The secret stays a secret: noticing it is the most that happens.

# arc: stained_page
title: La page tachée
family: village
stage: adult, elder
trigger: draw
size: short

## memories
frene.saw_the_page | frene | private | +3 | A recopié la page redessinée du registre des raies sans rien dire. Il sait qu'{a:il|elle} a vu.

## scene: register
title: La page tachée
pace: now

À la Maison commune, {A} recopie pour Maître Frêne le vieux registre des raies. Une page n'a pas tout à fait la même encre que les autres.
On l'a redessinée de mémoire, un jour, très soigneusement. Maître Frêne, à l'autre bout de la table, ne lève pas les yeux. Trop exprès.

* copy | Recopier sans rien dire
  [stress honest 10]
  [lean compassion +2]
  {A} recopie la page comme les autres, sans ralentir. Maître Frêne pose sa plume, un instant, et la reprend.
  Le soir, il range le registre en disant seulement : « Il convient de vous remercier. » Il ne dit pas de quoi.
  [remember frene.saw_the_page] [end]

* show | Lui montrer la page, tout bas  [mystery]
  [lean honor +2]
  « Examinons… » Maître Frêne regarde la page très longtemps. Puis il souffle, comme quelqu'un qui pose enfin un sac trop lourd.
  « Personne n'a tort, et personne n'a tout à fait raison. Surtout pas cette page. » Il a l'air plus léger tout l'après-midi.
  [modifier light_heart] [end]

* compare | Comparer avec le registre de l'Intendant  [needs trait diligent]
  [lean rationality +2]
  {A} sort le registre de Roncebelle. Les deux ne tombent d'accord sur rien, cette page comprise, ni plus ni moins que les autres.
  La page redessinée est donc exactement aussi juste que le reste. C'est très rassurant.
  [grow stewardship] [end]
