// Le conseil qui ne finit pas — wave 2 (adulte, ancien), new. Maître Frêne's wish: a council session that
// ends on time, just once; Renaud always votes against on principle; people come for the cider after
// (fiche 2, lore § 4, § 12).

# arc: long_council
title: Le conseil qui ne finit pas
family: village
stage: adult, elder
trigger: draw
size: short

## memories
frene.held_hourglass | frene | private | +3 | A tenu le sablier du conseil, un soir, et la séance a fini à l'heure. La seule fois.

## scene: session
title: Le conseil qui ne finit pas
pace: now

À la Maison commune, le conseil siège depuis trois heures. Renaud vient de voter contre l'ordre du jour, par principe.
L'envie de Maître Frêne, tout le monde la connaît : une séance qui finisse à l'heure. Une seule fois.

* hourglass | Tenir le sablier, très sérieusement
  [stress impatient 15]
  [lean zeal +2]
  {A} pose un sablier sur la table et le retourne à chaque point. Dame Ermengarde vote comme le Noyer, Renaud contre, et le sable tombe.
  La séance finit à l'heure. Maître Frêne reste assis un long moment, ému, devant la salle vide.
  [remember frene.held_hourglass] [end]

* cider | Sortir le cidre en avance
  [lean sociability +2]
  {A} débouche le cidre au milieu du troisième point. Le conseil vote tout le reste en dix minutes, à l'unanimité, Renaud compris.
  [modifier party_soul] [end]

* leave | Partir avant la fin  [needs trait impatient]
  [lean energy +2]
  {A} se lève au quatrième point, salue, et sort. Dehors, l'air du soir sent l'herbe coupée. On n'apprendra jamais la fin de la séance. Personne ne la connaît.
  [modifier fired_up] [end]
