# arc: small_debt
title: La petite dette
family: personal
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: debt
title: La petite dette
pace: now

Pendant la visite, {B} a rendu un petit service à {A}, sans même y penser : une porte tenue, une miette chassée d'un chapeau. Depuis, {A} a une petite dette, et elle gratte comme une écharde.

* honey | La rendre tout de suite, avec un pot de miel
  [stress greedy 15]
  [lean greed -1] [lean honor +1]
  {A} porte un pot de miel à {B}, en expliquant que c'est pour la miette. {B} ne se souvient d'aucune miette, mais garde le miel. Les comptes sont bons.
  [affinity +3] [modifier clear_conscience] [end]

* secret | La rendre en secret  [roll intrigue 3]
  [lean rationality +1] [lean sociability -1]
  ? success
    {A} rend le service en cachette : un seuil balayé à l'aube devant chez {B}, sans témoin. {B} cherche qui pendant une semaine, et soupçonne tout le hameau, sauf {A}.
    [grow intrigue] [modifier sly] [end]
  ? failure
    {A} balaie le seuil de {B} à l'aube, en cachette, et {B} ouvre la porte au troisième coup de balai. On se regarde. {A} continue à balayer, très naturellement.
    [modifier tense] [end]

* even | Décider qu'on est quittes
  [lean rationality +1] [lean greed +1]
  {A} refait le compte de la visite, longuement, et finit par trouver un service rendu à {B} : un tabouret avancé, ou un silence au bon moment. On est quittes. La dette ne gratte presque plus.
  [modifier trade_sense] [end]
