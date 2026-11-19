// Replaces break_nightmares.
# arc: break_nightmares
title: Le même rêve
family: hardship
stage: baby, teen, adult, elder
trigger: break 2
size: short

## scene: break
title: Le même rêve
was: break_nightmares
pace: now

Depuis une semaine, {A} dort mal. Toutes les nuits, le même rêve : une charrette qui monte la Côte sans {A}, une porte qui ne s'ouvre pas, et l'Intendant qui dit « trop tard », très lentement, en le notant. Ce soir, {A} n'ose plus fermer les yeux.

* lantern | Rester debout toute la nuit, lanterne allumée
  [lean boldness +1]
  {A} reste {a:assis|assise} jusqu'à l'aube, la lanterne allumée, devant un livre dont les pages ne tournent pas. Pas de rêve, pas de charrette. Mais au matin, les yeux piquent, et les nuits suivantes ne valent guère mieux.
  [stress -90] [modifier sleepless] [end]

* heron | Tout raconter au Fou, à l'aube
  [lean sociability +2]
  {A} raconte le rêve au Fou, sur le Pont-Vieux. Le Fou écoute sans rien dire, puis : « Moi, c'est une pierre qui me demande l'heure. Je lui réponds toujours. » Ça fait du bien de ne pas être {a:seul|seule}. Depuis, {A} raconte tout à quelqu'un.
  [stress -90] [coping confider] [end]

* pillow | Changer d'oreiller, et réessayer
  [lean rationality +1]
  {A} change d'oreiller, de côté, de couverture, puis de coin pour dormir. La nuit est courte, mais le rêve ne revient qu'une fois. On progresse.
  [stress -60] [end]
