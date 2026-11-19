// Replaces on_visit_forgot and follow_forgot.
# arc: forgot
title: L'objet oublié
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone

## scene: found
title: L'objet oublié
was: on_visit_forgot
pace: now

Après la visite, {A} trouve un petit mouchoir brodé sous un coussin. C'est celui de {B}.

* return | Le rapporter tout de suite
  [lean honor +1] [lean energy +1]
  {A} court le rapporter. {B} ne s'était même pas rendu compte de l'oubli, et en est {b:touché|touchée}.
  [affinity +5] [end]

* keep | Le garder pour la prochaine fois
  [lean rationality +1]
  {A} plie le mouchoir et le pose bien en vue. Une prochaine fois viendra bien.
  [-> shelf]

## scene: shelf
title: Le mouchoir brodé
was: follow_forgot
pace: 24h-72h

Le mouchoir de {B} attend toujours sur l'étagère de {A}. Le mouchoir commence à sentir la maison.

* bring | Le rendre, enfin
  [lean honor +1]
  {A} rend le mouchoir, plié avec soin, et une fleur glissée dedans. {B} fait comme si c'était prévu.
  [affinity +10] [grow diplomacy] [end]

* adopt | Garder le mouchoir pour de bon
  [lean greed +1]
  {A} adopte le mouchoir. {B} le reconnaîtra un jour, mais ce jour n'est pas aujourd'hui.
  [grow intrigue] [affinity -5] [end]
