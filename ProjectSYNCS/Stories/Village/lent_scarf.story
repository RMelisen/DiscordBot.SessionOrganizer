// L'écharpe prêtée — wave 2 (adulte, ancien). Rewrites grown_scarf, with a friend.

# arc: lent_scarf
title: L'écharpe prêtée
family: village
stage: adult, elder
trigger: draw
size: short
other: friends

## scene: scarf
title: L'écharpe prêtée
pace: now

Voilà trois semaines que {A} porte l'écharpe que {B} lui a prêtée, un soir de vent. Elle tient très chaud, et elle sent un peu {B}.
{B} n'a rien redemandé. {B} ne la regarde même pas. Très exprès.

* return | La rendre, lavée et pliée
  [lean honor +2]
  {A} rend l'écharpe, lavée au savon de la Sauge, pliée en quatre. {B} la prend, la renifle, et a l'air un peu déçu{b:|e} qu'elle ne sente plus rien.
  [affinity +5] [modifier clear_conscience] [end]

* keep | La garder encore un peu
  [lean greed +1]
  {A} la garde. Il fait froid, après tout. {B} passe, voit l'écharpe, et sourit sans rien dire.
  [modifier cherished] [end]

* share | Proposer de la partager, un jour chacun  [mystery]
  [lean sociability +2]
  Le lundi à {A}, le mardi à {B}. Tout le hameau apprend le calendrier, et vérifie chaque matin qui la porte. La Courge tient les comptes.
  [affinity +8] [modifier light_heart] [end]
