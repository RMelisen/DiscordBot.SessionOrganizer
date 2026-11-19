// Le chapeau envolé — wave 2 (adulte, ancien). Rewrites grown_hat_wind on the Sommeille: the passeur, a
// willow, ferries people for the pleasure (lore § 12); the Fou finds everything (fiche 13).

# arc: flying_hat
title: Le chapeau envolé
family: village
stage: adult, elder
trigger: draw
size: short

## memories
fou.found_hat | fou | private | +2 | Lui a demandé de retrouver son chapeau envolé. Il l'a retrouvé sur un canard.

## scene: gust
title: Le chapeau envolé
pace: now

Une rafale arrache le chapeau de {A}. Il file au-dessus des toits, fait un tour au-dessus du puits, et part vers la Sommeille.

* run | Courir après  [roll courage 3]
  [lean energy +2]
  ? success
    {A} court, saute le muret, traverse le pré, et attrape le chapeau au vol, au bord de l'eau. Le passeur applaudit depuis sa barque.
    [grow courage] [end]
  ? failure
    {A} court, saute le muret, et atterrit dans le pré de la Sommeille, la partie boueuse. Le chapeau, lui, continue.
    [modifier muddy_paws] [end]

* let_go | Le laisser partir
  [stress greedy 10]
  [lean energy -2]
  {A} le regarde s'en aller, et trouve ça plutôt beau. Une semaine plus tard, le passeur le rapporte. Il l'a porté toute la semaine, et il lui va bien.
  [modifier light_heart] [end]

* fou | Demander au Fou de le retrouver
  [lean sociability +2]
  Le Fou ferme les yeux, compte jusqu'à quatre, et part vers l'étang. Il revient avec le chapeau. Il était sur un canard, qui avait l'air d'y tenir.
  [remember fou.found_hat] [end]
