// Le mot dans la pierre — wave 2 (adulte only). Rewrites grown_letters at the Pont-Vieux, the old bridge
// everyone touches when crossing (lore § 5, § 9.2). With a friend. Never makes a couple: declaring stays
// the C# social_declare, which asks the other owner.

# arc: bridge_stone
title: Le mot dans la pierre
family: village
stage: adult
trigger: draw
size: short
other: friends

## scene: stone
title: Le mot dans la pierre
pace: now

Depuis des semaines, {A} pense à {B}. Le matin, le soir, et beaucoup entre les deux. Le dire en face semble impossible.
Sous le Pont-Vieux, entre deux pierres, on glisse des mots qu'on n'ose pas dire. Tout le hameau le fait, et personne ne lit ceux des autres.

* poem | Écrire un poème, le plus beau possible  [mystery]
  [lean zeal +2]
  {A} écrit toute la nuit, rature, recommence, et glisse le poème entre les pierres à l'aube. Le lendemain, il a disparu. À sa place, une petite fleur séchée.
  [affinity +8] [modifier inspired] [end]

* simple | Écrire simplement : « Je pense à toi. »
  [lean compassion +2]
  Quatre mots, pliés en quatre. {A} les glisse dans la pierre. Le soir, {B} traverse le pont, touche la pierre comme tout le monde, et s'arrête un peu plus longtemps.
  [affinity +5] [modifier light_heart] [end]

* keep | Garder ça pour soi
  [lean energy -1]
  {A} traverse le pont, touche la pierre, et garde les mots pour soi. Ils sont bien, là où ils sont. Pour l'instant.
  [modifier soothed] [end]
