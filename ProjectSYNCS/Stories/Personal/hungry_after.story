# arc: hungry_after
title: La faim d'après
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: hunger
title: La faim d'après
pace: now

Le soir de la visite, {A} a une faim de moisson, une faim à manger le Pont-Vieux, pierres comprises. Il paraît que voir {B}, ça creuse. Le garde-manger, lui, n'a pas l'air d'être au courant.

* feast | Tout manger, jusqu'à la dernière noix
  [stress temperate 10]
  [lean greed +1] [lean energy +1]
  {A} mange tout : le pain, la confiture, trois noix, et un bout de fromage dont personne ne réclamera jamais la propriété. Le ventre rond, la tête lourde, la nuit est excellente.
  [modifier hearty] [end]

* share | Partager avec un voisin
  [lean compassion +1] [lean sociability +1]
  {A} frappe chez un voisin avec une miche, et le voisin, qui avait faim aussi, sort une soupe. On mange à deux ce qu'on aurait mangé seul, et c'est meilleur.
  [grow diplomacy] [end]

* wait | Attendre le petit-déjeuner, dignement
  [stress impatient 10]
  [lean energy -1] [lean honor +1]
  {A} se couche le ventre vide, très dignement. Au matin, le petit-déjeuner a un goût de fête, et {A} le mange très, très lentement.
  [grow stewardship] [end]
