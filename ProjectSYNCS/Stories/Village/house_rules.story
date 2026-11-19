// Les règles de la Maison — wave 1 (ado). Rewrites teen_shortcut and teen_broken_mirror at the
// Maison des Pousses: the Bruyère's rules are numbered and a clever pousse always finds the one that
// is missing; angry, she calls a pousse by its full name (fiche 8).

# arc: house_rules
title: Les règles de la Maison
family: village
stage: teen
trigger: draw
size: short

## memories
bruyere.kept_rule | bruyere | private | +2 | Est resté{a:|e} à la Maison le soir où toutes les autres pousses sont parties au Pont-Vieux, parce que c'était la règle.

## scene: rule
title: Les règles de la Maison
pace: now

Sur le mur de la Maison des Pousses, la règle 14 est écrite en grosses lettres : « On ne sort pas après la soupe. »
Ce soir, toutes les pousses vont au Pont-Vieux regarder les grenouilles. Une pousse maligne trouve toujours la règle qui manque.

* obey | Obéir à la règle 14
  [stress rowdy 10]
  [lean honor +2]
  {A} reste. La Bruyère ne dit rien, mais elle pose à côté de son bol une deuxième part de tarte, et s'assoit en face pour écosser des haricots.
  {a:Ils|Elles} écossent ensemble jusqu'au retour des autres.
  [remember bruyere.kept_rule] [end]

* loophole | Trouver la règle qui manque  [roll intrigue 3]
  [lean rationality +2]
  ? success
    {A} relit tout le mur. Rien n'interdit de sortir *pendant* la soupe. {A} emporte son bol.
    La Bruyère lit, relit, et ajoute à la craie une règle 15. Elle a l'air presque fière.
    [modifier sly] [end]
  ? failure
    « Rien ne dit qu'on ne peut pas… » La Bruyère prononce le nom complet de {A}, lentement. C'est mauvais signe.
    {A} passe la soirée au lavoir, avec tout le linge de la Maison.
    [modifier grumpy] [end]

* go | Sortir quand même
  [stress just 15]
  [lean boldness +2]
  {A} enjambe la fenêtre et court jusqu'au Pont-Vieux. Les grenouilles sont magnifiques. Le retour, beaucoup moins.
  La Bruyère attend à la porte, une lanterne à la main. Elle ne dit rien. Elle montre la règle 14, du doigt.
  [modifier fired_up] [end]
