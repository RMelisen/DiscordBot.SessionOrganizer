// Replaces on_sick.
# arc: sick
title: Le nez qui coule
family: personal
stage: baby, teen, adult, elder
trigger: sick
size: short

## scene: sniff
title: Le nez qui coule
was: on_sick
pace: now

{A} éternue, renifle, et décide que le monde est injuste.

* tea | Réclamer une tisane
  [lean sociability +1]
  La Sauge envoie une tisane, imbuvable comme toujours, avec un mot : « Bois-la chaude. Et ne fais pas cette tête. »
  [stress -5] [end]

* brave | Faire comme si de rien n'était
  [lean boldness +1]
  {A} fait comme si de rien n'était, entre deux éternuements spectaculaires.
  [grow courage] [end]
