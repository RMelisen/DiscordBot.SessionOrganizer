// L'ardoise du Noyer — wave 2 (adulte only), new. The inn's slates are a local joke, nobody worries; the
// Noyer has the longest (lore § 7, fiches 6, 9).

# arc: noyer_slate
title: L'ardoise du Noyer
family: village
stage: adult
trigger: draw
size: short

## scene: slate
title: L'ardoise du Noyer
pace: now

Derrière le comptoir, la plus longue ardoise de l'auberge est celle du Noyer. Elle déborde sur le mur.
La Courge demande à {A}, très gentiment, de le lui rappeler. Très, très gentiment. « C'est une plaisanterie, mon chou. Mais une longue. »

* compliment | Le lui rappeler avec un compliment  [roll diplomacy 3]
  [lean sociability +2]
  ? success
    « Une ardoise de cette taille, c'est une ardoise de notable. » Le Noyer, flatté, en paie la moitié sur-le-champ. La Courge glisse un pourboire à {A}.
    [cailloux 15] [end]
  ? failure
    Le Noyer entend « notable » et rien d'autre. Il parle de lui à la troisième personne pendant une heure, et commande une tournée. Sur l'ardoise.
    [grow diplomacy] [end]

* chalk | Écrire le total à la craie sur sa porte, pour rire
  [lean boldness +2]
  Le lendemain, la porte du Noyer porte un chiffre, à la craie, souligné deux fois. Il le trouve, rit très fort, et ne l'efface pas. Il le montre.
  [modifier sly] [end]

* leave | Laisser l'ardoise tranquille : c'est une plaisanterie locale
  [lean compassion +1]
  {A} sourit, et ne dit rien au Noyer. La Courge hausse les épaules, et ajoute une ligne à l'ardoise : la tarte de {A}. « Il paiera. Un jour. »
  [modifier clear_conscience] [end]
