// Les registres à l'eau — wave 2 (adulte, ancien), new. The Intendant's clerk, a young reed, carries the
// registers and drops them (lore § 12); the Intendant notes everything, the weather included (fiche 3).

# arc: wet_registers
title: Les registres à l'eau
family: village
stage: adult, elder
trigger: draw
size: short

## memories
intendant.saved_registers | intendant | private | +3 | A plongé dans la Sommeille repêcher ses registres. Il l'a noté, avec la température de l'eau.

## scene: bridge
title: Les registres à l'eau
pace: now

Sur le Pont-Vieux, le clerc de l'Intendant trébuche, comme d'habitude. Cette fois, trois registres passent par-dessus le parapet.
Ils flottent, pour l'instant. L'Intendant, au bout du pont, a la bouche ouverte et pas un mot dedans.

* dive | Plonger  [roll courage 3]
  [stress craven 15]
  [lean boldness +2]
  ? success
    {A} plonge, attrape les trois registres, et remonte sur la berge, ruisselant{a:|e}. L'Intendant prend les registres, puis la main de {A}, et ne lâche ni les uns ni l'autre.
    [remember intendant.saved_registers] [end]
  ? failure
    {A} plonge, attrape deux registres, et le troisième part vers le moulin. {A} rentre {a:trempé|trempée} jusqu'aux os, en éternuant.
    [modifier woods_cold] [end]

* dry | Faire sécher les pages une par une
  [lean rationality +2]
  Le passeur repêche les registres. {A} étend les pages sur toute la place, une par une, avec des cailloux dessus. On marche sur la pointe des pieds jusqu'au soir.
  [grow stewardship] [end]

* rewrite | Les réécrire de mémoire avec le Fou  [mystery]
  [lean zeal +1]
  Le Fou se souvient de tous les chiffres, ou presque, et en ajoute quelques-uns qui ont l'air bien. L'Intendant relit tout, deux fois.
  « C'est plus juste qu'avant. » Il a l'air un peu inquiet de le dire.
  [modifier inspired] [end]
