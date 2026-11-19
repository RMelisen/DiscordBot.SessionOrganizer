// Quelqu'un qu'on vient voir — wave 2 (ancien only), new. An ancien becomes someone people come to see
// (lore § 6). Aude asks how to tell her father about Mathurin; nothing is told, the village never changes
// (lore § 11). A secret option for whoever kept the Noyer talking pumpkins at the ford (ford_watch).

# arc: come_for_advice
title: Quelqu'un qu'on vient voir
family: village
stage: elder
trigger: draw
size: short

## scene: advice
title: Quelqu'un qu'on vient voir
pace: now

On vient voir {A}, maintenant, pour un conseil. On s'assoit, on tourne son chapeau dans ses mains, et on finit par demander.
Ce matin, c'est Aude, la fille du Noyer. Comment dire à son père, pour Mathurin ?

* spring | « Attends le printemps. »
  [lean rationality +2]
  « Au printemps, tout le monde est de meilleure humeur. Même ton père. Surtout ton père. » Aude réfléchit, et hoche la tête. Le printemps a l'air loin. C'est le but.
  [grow diplomacy] [end]

* village | « Tout le village est déjà de ton côté. »
  [lean compassion +2]
  Aude rougit jusqu'aux oreilles, comme Mathurin. « Tout le village ? » Tout le village. Elle repart en riant, et ne dit rien à son père, pas encore.
  [modifier light_heart] [end]

* yours | « Ça, c'est à toi de voir. »
  [lean honor +1]
  Aude soupire. Puis elle sourit. « C'est ce que dit la Mère Lichen. » Elle repart, un peu plus droite qu'en arrivant.
  [modifier clear_conscience] [end]

* pumpkins | « Commence par ses citrouilles. »  [needs memory noyer.pumpkin_talk "Parlé citrouilles au gué"] [secret]
  [lean sociability +2]
  « Parle-lui d'abord de ses citrouilles. Une heure. Après, il dira oui à tout. » Aude éclate de rire. Elle a l'air de garder la recette pour plus tard.
  [modifier well_spoken] [end]
