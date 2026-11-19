// Les bornes du Fou — wave 2 (adulte, ancien), new. The Fou counts the boundary stones of the pré every
// morning and the count is never the same; the Noyer and the Chardon each ask him in secret (fiche 13).
// Reads the ribbon memories of wave 1 (frene_ribbon).

# arc: fou_stones
title: Les bornes du Fou
family: village
stage: adult, elder
trigger: draw
size: short

## memories
fou.brought_word | fou | private | +2 | Lui a rapporté que le Noyer et le Chardon voulaient tous les deux son chiffre. Il en a ri toute la matinée.

## scene: stones
title: Les bornes du Fou
pace: now

Chaque matin, le Fou compte les bornes du pré de la Sommeille, et le compte n'est jamais le même. Ce matin, c'est cinq.
Le Noyer, puis le Chardon, chacun de son côté, ont demandé à {A} de leur rapporter le chiffre. En secret.
[if memory noyer.ribbon_his_side] Le Noyer a ajouté, avec un clin d'œil : « Toi, tu sais où est la limite. »
[if memory chardon.ribbon_his_side] Le Chardon a ajouté, sans sourire : « Toi, tu sais où est la limite. »

* truth | Donner le vrai chiffre aux deux
  [lean honor +2]
  « Cinq. » Le Noyer jure que c'est quatre. Le Chardon jure que c'est six. Ils sont tous les deux enchantés : la querelle tient encore un an.
  [modifier clear_conscience] [end]

* each | Donner à chacun le chiffre qui l'arrange  [roll intrigue 3]
  [stress honest 20]
  [lean greed +1]
  ? success
    Quatre au Noyer, six au Chardon. Ils repartent chacun ravis, et ne se parlent pas, comme d'habitude.
    [modifier sly] [end]
  ? failure
    Le soir, à l'auberge, ils comparent leurs chiffres. Ils ne sont d'accord sur rien, sauf sur {A}.
    [modifier grumpy] [end]

* tell | Rapporter au Fou qu'on le demande
  [lean sociability +2]
  Le Fou éclate de rire. « Tous les deux ? Ah ! Alors c'est cinq. Ce sera toujours cinq, maintenant. » Le lendemain, il en compte sept.
  [remember fou.brought_word] [end]
