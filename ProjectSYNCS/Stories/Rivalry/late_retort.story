# arc: late_retort
title: La réplique
family: rivalry
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
requires: [relation enemies]
cooldown: 7d

## scene: retort
title: La réplique
pace: now

Des heures après la visite, au beau milieu d'un bol de soupe, {A} trouve enfin la réplique parfaite, celle qu'il aurait fallu dire à {B}. Elle est piquante, elle est juste, et elle arrive bien trop tard.

* go | Aller la dire maintenant  [roll courage 3]
  [lean boldness +1] [lean vengefulness +1]
  ? success
    {A} traverse le hameau, trouve {B}, et dit la réplique, mot pour mot. {B} reste sans voix, puis rit, malgré tout. C'était vraiment une bonne réplique.
    [grow courage] [affinity +3] [end]
  ? failure
    {A} traverse le hameau, trouve {B}, ouvre la bouche, et ne se souvient plus de rien. Pas un mot. {A} repart très dignement.
    [modifier grumpy] [end]

* ducks | La dire aux canards du bief
  [lean sociability -1] [lean vengefulness -1]
  {A} va la dire aux canards du bief, avec le ton qu'il faut. Les canards la trouvent excellente. Ça ne règle rien, mais ça soulage beaucoup.
  [stress -10] [end]

* keep | La garder pour la prochaine fois
  [lean rationality +1] [lean vengefulness +1]
  {A} répète la réplique jusqu'à la savoir par cœur, et la range dans un coin, pour la prochaine fois. Il y aura une prochaine fois. Il y en a toujours une.
  [grow intrigue] [end]

* drop | Laisser tomber, ce n'est pas si grave
  [lean compassion +1] [lean vengefulness -2]
  {A} sourit à son bol de soupe, et laisse la réplique où elle est. Le lendemain, en croisant {B}, {A} n'y pense même plus.
  [affinity +5] [modifier clear_conscience] [end]
