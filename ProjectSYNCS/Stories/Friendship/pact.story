// Replaces social_pact and reply_pact.
# arc: pact
title: Le pacte
family: friendship
stage: adult, elder
trigger: draw
size: short
other: friends

## scene: pact
title: Le pacte
was: social_pact
pace: now

{A} et {B} ont partagé leur dernier gâteau, leur dernier secret et une averse entière sous la même feuille.

* pact | Proposer un pacte de meilleurs amis
  [lean sociability +2] [lean honor +1]
  {A} propose un pacte, le petit doigt tendu.
  [-> answer]

* enjoy | Profiter de l'amitié telle quelle
  [lean rationality +1]
  {A} ne propose rien. Ce qui marche n'a pas besoin de contrat.
  [affinity +5] [end]

## scene: answer
title: Le petit doigt
was: reply_pact
pace: now
handover

{B} tend le petit doigt à {A} : un pacte de meilleurs amis, pour la vie, ou au moins jusqu'à jeudi.

* yes | Serrer le petit doigt  [accept]
  [lean sociability +2]
  Les deux petits doigts se serrent. C'est officiel : le Fou, qui passait, sert de témoin, et l'écrit sur sa main.
  [bond bestfriends] [end]

* later | Dire « pas encore »  [refuse]
  [lean rationality +1]
  {A} replie le doigt de {B}, gentiment. « Pas encore. Mais garde-le tendu. »
  [affinity -5] [end]
