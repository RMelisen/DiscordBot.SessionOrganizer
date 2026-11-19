// La première maison — wave 2 (adulte only), new. An adulte has its own little house in the hamlet, its
// place left open for each event to decide (lore § 5). Once in a lifetime. The Bruyère never says
// tenderness aloud (fiche 8).

# arc: first_house
title: La première maison
family: village
stage: adult
trigger: draw
size: short
once: life

## scene: house
title: La première maison
pace: now

{A} a sa maison, maintenant. Petite, un peu de travers, avec une porte qui frotte. La Courge a apporté une marmite, Pain-Blond une miche encore chaude.
La Bruyère a apporté un balai. Elle l'a posé contre le mur, sans un mot, et elle est repartie.

* party | Pendre la crémaillère
  [stress shy 15]
  [lean sociability +2]
  Tout le hameau tient dans la petite maison, et une partie sur le chemin. Le Fou compte les invités, et trouve un de plus. On ne sait pas qui.
  [modifier party_soul] [end]

* alone | Dormir la première nuit, enfin {a:seul|seule}
  [lean energy -2]
  {A} ferme la porte qui frotte, se couche dans le lit qui grince, et écoute la maison. Elle fait de tout petits bruits, comme quelqu'un qui s'installe aussi.
  [stress -15] [modifier well_rested] [end]

* paint | Peindre la porte  [needs passion painting]
  [lean zeal +1]
  {A} peint la porte d'une couleur que personne n'a au hameau. On vient la voir le dimanche. La Bruyère passe, regarde, et ne dit rien, ce qui est un compliment.
  [grow stewardship] [end]
