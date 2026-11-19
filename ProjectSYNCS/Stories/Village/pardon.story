// Pardon ? — wave 2 (ancien only). Rewrites elder_pardon: an ancien who hears less; the Tilleul is half
// deaf and talks very loud (fiche 12).

# arc: pardon
title: Pardon ?
family: village
stage: elder
trigger: draw
size: short

## memories
tilleul.shouted_together | tilleul | private | +2 | A passé un après-midi à crier avec lui au verger, aussi sourd{a:|e} que lui, et très content.

## scene: market
title: Pardon ?
pace: now

Depuis quelque temps, {A} entend un peu moins bien. Ce matin, au marché, {A} a demandé du miel, et reçu un chapeau.
Le chapeau est très joli. Ce n'est pas du miel.

* hat | Garder le chapeau, et le porter
  [lean energy -1]
  {A} met le chapeau. Il va très bien. Le lendemain, trois personnes demandent où on trouve ce chapeau, et {A} répond « du miel », avec assurance.
  [modifier light_heart] [end]

* trumpet | Se fabriquer un cornet acoustique
  [stress arrogant 10]
  [lean rationality +2]
  Un cornet de cuivre, un entonnoir, un peu de patience. {A} entend de nouveau tout, y compris ce que le Tilleul dit à ses abeilles, de l'autre bout du hameau.
  [grow stewardship] [end]

* tilleul | Aller voir le Tilleul, qui n'entend pas mieux
  [lean sociability +2]
  « QUOI ? » « LE MIEL ! » « OUI, IL FAIT BEAU ! » Ils passent l'après-midi à crier au verger, et ne se comprennent jamais. C'est la meilleure conversation de la semaine.
  [remember tilleul.shouted_together] [end]
