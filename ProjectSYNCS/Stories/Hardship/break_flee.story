// Replaces break_flee.
# arc: break_flee
title: Partir loin
family: hardship
stage: baby, teen, adult, elder
trigger: break 3
size: short

## scene: break
title: Partir loin
was: break_flee
pace: now

Un matin, {A} se réveille avec une seule idée : partir. N'importe où, mais loin d'ici, loin de tout ce qui pèse. Le baluchon est à moitié fait avant même que {A} ait décidé quoi que ce soit.

* go | Partir sans prévenir personne
  [lean boldness +2] [lean sociability -1]
  {A} monte la Côte à l'aube, marche jusqu'au soir sans se retourner, et dort sous une haie. Au retour, le lendemain, tout le hameau guettait sur la place. {A} revient plus calme, mais {a:secoué|secouée}, et sursaute au moindre craquement de branche.
  [stress -120] [modifier shaken] [end]

* train | Monter dans la carriole du colporteur, sans billet de retour
  [lean boldness +1] [lean greed +1]
  Le colporteur fait de la place sans poser de question, mais en levant un sourcil. {A} descend au premier tournant de la Côte, lui achète un bocal de confiture inconnue, une lanterne et un chapeau, et rentre à pied. Le voyage a tout réglé. Les achats, eux, ne font que commencer.
  [stress -110] [coping profligate] [end]

* garden | Partir… jusqu'au bout du jardin
  [lean rationality +1] [lean energy -1]
  {A} prend le baluchon, marche jusqu'au bout du jardin, s'assoit sous le pommier et mange les tartines du voyage. Le grand départ attendra. Le pommier, lui, a passé une très bonne journée.
  [stress -70] [end]
