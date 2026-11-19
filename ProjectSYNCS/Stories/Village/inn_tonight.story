// Ce soir à l'auberge — wave 2 (adulte only), new. The inn always needs something before tonight: a
// musician, mushrooms, a fourth at cards (fiche 6). Aide d'auberge is a trade the Music passion opens
// (lore § 6).

# arc: inn_tonight
title: Ce soir à l'auberge
family: village
stage: adult
trigger: draw
size: short

## memories
courge.served_instead | courge | private | +2 | A servi toute une soirée à l'auberge, le soir où il manquait un musicien.

## scene: inn
title: Ce soir à l'auberge
pace: now

L'auberge a toujours besoin de quelque chose avant ce soir. Aujourd'hui, c'est un musicien : celui de Roncebelle s'est trompé de jour.
La Courge pose les deux mains sur les épaules de {A}. « Mon chou. Tu as une tête à savoir faire quelque chose. »

* play | Jouer  [needs passion music]
  [lean sociability +2]
  {A} joue toute la soirée, et le Noyer danse, ce qu'on n'avait pas vu depuis le grand hiver. La Courge paie bien, et le dit à tout le monde.
  [cailloux 25] [end]

* sing | Chanter quand même  [roll diplomacy 3]
  [stress shy 20]
  [lean boldness +2]
  ? success
    {A} chante faux, mais si fort et si bien que toute l'auberge reprend le refrain. On chante jusqu'à minuit, le Fou plus fort que tout le monde.
    [modifier party_soul] [end]
  ? failure
    {A} oublie les paroles au deuxième couplet, et en invente d'autres. Elles sont meilleures. Personne n'ose le dire.
    [modifier well_spoken] [end]

* serve | Servir à la place
  [lean compassion +1]
  {A} sert les chopes, les bols et les ragots toute la soirée. Il n'y a pas de musique, mais la Courge chante en cuisine, et ça suffit.
  [remember courge.served_instead] [end]
