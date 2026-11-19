// Replaces social_declare and reply_declare.
# arc: declare
title: Le cœur qui bat
family: love
stage: adult, elder
trigger: draw
size: short
other: known
other_if: [can_couple]

## scene: heart
title: Le cœur qui bat
was: social_declare
pace: now

Depuis quelque temps, {A} rougit chaque fois que {B} passe. Même de loin. Même de dos.

* declare | Tout avouer à {B}  [owner-only]
  [lean boldness +2] [lean sociability +1]
  {A} prend son courage à deux mains et va tout dire à {B}.
  [-> answer]

* wait | Garder ça pour soi encore un peu
  [lean boldness -1]
  {A} garde le secret, bien au chaud. Certaines choses mûrissent mieux à l'ombre.
  [grow learning] [end]

## scene: answer
title: Une déclaration
was: reply_declare
pace: now
handover

{B} est venu{b:|e} tout avouer à {A}, les joues rouges et la voix qui tremble.

* yes | Dire oui  [accept]
  [lean sociability +1] [lean compassion +1]
  {A} dit oui. {B} en oublie de respirer, puis rit, puis respire. Tout le hameau est au courant avant le soir : la Courge y a veillé.
  [couple] [end]

* no | Dire non, doucement  [refuse]
  [lean honor +1]
  {A} dit non, avec beaucoup de douceur. {B} hoche la tête et rentre par le chemin le plus long.
  [heartbreak] [end]
