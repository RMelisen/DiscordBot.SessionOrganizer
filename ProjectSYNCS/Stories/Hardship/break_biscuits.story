// Replaces break_biscuits.
# arc: break_biscuits
title: La boîte à biscuits
family: hardship
stage: baby, teen, adult, elder
trigger: break 1
size: short

## scene: break
title: La boîte à biscuits
was: break_biscuits
pace: now

Rien ne va depuis trois jours, et {A} se retrouve à minuit devant la boîte à biscuits de Pain-Blond, sans trop savoir comment. La boîte est pleine. Pour l'instant.

* eat | Tout manger, jusqu'au dernier biscuit
  [lean greed +1] [lean rationality -1]
  {A} mange les biscuits un par un, puis deux par deux. Au fond de la boîte, plus que des miettes, et un grand calme. Depuis ce soir-là, la boîte à biscuits est devenue une amie très proche.
  [stress -80] [coping comfort_eater] [end]

* shut | Refermer la boîte, et ne plus rien avaler
  [lean greed -1] [lean zeal +1]
  {A} referme la boîte d'un geste sec, puis ne mange plus rien de la journée, ni du lendemain. Le ventre proteste. La tête, curieusement, se tait. L'habitude aussi s'installe.
  [stress -75] [coping inappetetic] [end]

* one | Un seul biscuit, et retour au lit
  [lean rationality +1]
  {A} prend un seul biscuit, le mange très lentement, et retourne se coucher. Ce n'est pas grand-chose. C'est déjà ça.
  [stress -45] [end]
