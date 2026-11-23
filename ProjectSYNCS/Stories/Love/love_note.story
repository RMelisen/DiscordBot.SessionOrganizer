# arc: love_note
title: Le mot dans la poche
family: love
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation lovers]
cooldown: 7d

## scene: note
title: Le mot dans la poche
pace: now

Le soir, en vidant ses poches, {A} trouve une feuille de tilleul pliée en quatre. Dedans, un petit dessin de {B} : deux silhouettes et un soleil beaucoup trop grand.

* answer | Répondre par un dessin
  [lean sociability +1] [lean compassion +1]
  {A} dessine une réponse au charbon de bois : les deux silhouettes, le soleil, et cette fois une lune aussi, pour la nuit. Le dessin part le lendemain matin, glissé là où {B} le trouvera.
  [affinity +5] [grow diplomacy] [end]

* pillow | Le garder sous l'oreiller
  [lean energy -1] [lean zeal +1]
  {A} glisse le dessin sous l'oreiller et s'endort dessus. Les rêves, cette nuit-là, ont un soleil beaucoup trop grand.
  [modifier cherished] [end]

* show | Le montrer à la Courge
  [stress shy 15]
  [lean sociability +2]
  La Courge regarde le dessin, pose une vrille sur son cœur, et dit « oh, mon chou » quatre fois, de plus en plus fort. Le lendemain, tout le hameau sait qu'il y a un soleil trop grand dans la poche de {A}.
  [modifier party_soul] [end]
