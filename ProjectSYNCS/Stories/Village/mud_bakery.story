// La pâtisserie de boue — wave 1 (bébé). Rewrites baby_mud_pie with Pain-Blond, who dares refuse
// nothing (fiche 7) and longs for someone to want more of his inventions.

# arc: mud_bakery
title: La pâtisserie de boue
family: village
stage: baby
trigger: draw
size: short

## memories
pain_blond.mud_pie | pain_blond | private | +2 | Lui a offert la plus belle part de sa pâtisserie de boue. C'était la première fois qu'on le nourrissait, lui.

## scene: stall
title: La pâtisserie de boue
pace: now

{A} a ouvert une pâtisserie au bord d'une flaque : tarte de boue, gâteau de boue, et une spécialité aux pâquerettes.
Le premier client est Pain-Blond, qui n'ose jamais rien refuser. Il regarde la tarte avec beaucoup de courage.

* sell | Lui vendre une tarte, très cher
  [lean greed +2]
  Pain-Blond paie sans discuter, et emporte la tarte à deux mains, comme une chose précieuse.
  Le lendemain, une vraie brioche attend sur le bord de la flaque. « Pour l'échange », dit un petit mot plein de farine.
  [cailloux 15] [end]

* offer | Lui offrir la plus belle part
  [lean compassion +2]
  Pain-Blond reste un long moment devant la part, sans rien dire. Personne ne lui avait jamais rien cuisiné.
  Il fait semblant de goûter, très poliment. Puis il demande la recette, et il a l'air sérieux.
  [remember pain_blond.mud_pie] [end]

* tiers | Inventer un gâteau à trois étages  [needs trait curious]
  [lean rationality +2]
  Un étage de boue, un étage de mousse, un étage de boue. {A} cale le tout avec des brindilles, et ça tient.
  Pain-Blond en oublie de partir. « Il faut une base plus large. » Il a raison.
  [grow stewardship] [end]
