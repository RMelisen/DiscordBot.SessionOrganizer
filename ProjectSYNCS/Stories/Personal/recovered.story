// Replaces on_recovered.
# arc: recovered
title: De nouveau sur pied
family: personal
stage: baby, teen, adult, elder
trigger: recovered
size: short

## scene: up
title: De nouveau sur pied
was: on_recovered
pace: now

{A} se réveille, respire par le nez — par les deux narines — et se sent {a:neuf|neuve}.

* run | Faire le tour du hameau en courant
  [lean energy +2]
  {A} court partout, juste pour vérifier que tout marche encore. Tout marche.
  [modifier fired_up] [end]

* thank | Remercier ceux qui ont aidé
  [lean compassion +1]
  {A} distribue des remerciements, et un pot de miel à la Sauge, pour changer.
  [grow diplomacy] [end]
