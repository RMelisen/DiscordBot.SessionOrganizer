// Le gland — wave 2 (ancien only). Rewrites elder_acorn: an oak takes a hundred years; Maître Frêne writes
// everything in the register (fiche 2); the twins of the Communal (lore § 12).

# arc: acorn
title: Le gland
family: village
stage: elder
trigger: draw
size: short

## memories
frene.planted_oak | frene | private | +2 | A planté un gland au Communal, {a:très vieux|très vieille} déjà. Il l'a inscrit au registre, avec la date et « à suivre ».

## scene: acorn
title: Le gland
pace: now

{A} a ramassé un gland, ce matin, au bord du Communal. Lisse, bien rond, encore vert d'un côté.
Un chêne met cent ans à devenir grand. {A} retourne le gland dans sa main, et sourit.

* plant | Le planter au Communal
  [lean zeal +2]
  {A} creuse un petit trou, y pose le gland, et tape la terre. Maître Frêne, qui passait, sort son registre et l'inscrit : la date, l'endroit, et « à suivre ».
  [remember frene.planted_oak] [end]

* twins | Le donner aux jumelles
  [lean compassion +2]
  Les jumelles du Communal prennent le gland, se disputent pour savoir laquelle le plantera, et le plantent à deux. Elles viendront vérifier chaque matin.
  [modifier light_heart] [end]

* keep | Le garder dans sa poche
  [lean energy -1]
  {A} le garde dans sa poche. Il y sera bien. Les chênes, c'est pour ceux qui ont le temps, et {A} a encore celui de garder un gland.
  [item col.gland] [end]
