// Replaces social_mend and reply_mend.
# arc: mend
title: Le rameau d'olivier
family: rivalry
stage: adult, elder
trigger: draw
size: short
other: hostile

## scene: market
title: Le rameau d'olivier
was: social_mend
pace: now

{A} croise {B} au marché de la place. Les deux font semblant de ne pas se voir, avec beaucoup d'application.

* mend | Proposer de faire la paix
  [lean compassion +2] [lean vengefulness -2]
  {A} tend un rameau, un vrai, cueilli exprès.
  [-> answer]

* snub | Continuer à faire semblant
  [lean vengefulness +1]
  {A} fait semblant si bien que {B} y croit presque. La Courge, elle, n'y croit pas du tout, et le raconte déjà.
  [grow intrigue] [end]

## scene: answer
title: La paix ?
was: reply_mend
pace: now
handover

{B} tend un rameau à {A}, cueilli exprès. Le marché entier retient son souffle.

* yes | Accepter le rameau  [accept]
  [lean compassion +2]
  {A} prend le rameau. On ne devient pas amis en un jour, mais on peut arrêter de se fâcher.
  [bond peace] [end]

* no | Le laisser tomber  [refuse]
  [lean vengefulness +2]
  Le rameau tombe dans la poussière. {B} le ramasse et repart avec, l'air digne.
  [affinity -5] [end]
