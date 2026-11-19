// Replaces break_cloud.
# arc: break_cloud
title: Le petit nuage gris
family: hardship
stage: baby, teen, adult, elder
trigger: break 1
size: short

## scene: break
title: Le petit nuage gris
was: break_cloud
pace: now

Depuis ce matin, {A} traîne un petit nuage gris au-dessus du chapeau. Ce n'est pas un vrai nuage. Ça y ressemble beaucoup, et ça pleut un peu dessus quand personne ne regarde.

* shout | Crier un bon coup à la Lisière
  [lean boldness +2] [lean vengefulness +1]
  {A} crie si fort que trois corbeaux changent d'arbre. La Mère Lichen ne lève même pas la tête. Le nuage s'en va. Reste une petite humeur de chien.
  [stress -80] [modifier grumpy] [end]

* tell | Aller tout raconter à quelqu'un
  [lean sociability +2]
  {A} parle longtemps à la Courge, qui écoute, pour une fois, sans rien répéter. Le nuage s'éloigne. Quelque chose a changé dans sa façon de faire face.
  [stress -70] [coping] [end]

* curl | Se rouler en boule sous une feuille de bardane
  [lean energy -1] [lean sociability -1]
  {A} reste sous la feuille jusqu'au soir. Le nuage finit par s'ennuyer, et va pleuvoir sur les semis du Noyer, qui en avaient besoin.
  [stress -60] [end]
