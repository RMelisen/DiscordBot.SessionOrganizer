// Les quatre de l'Ortie — wave 1 (ado), new. The Ortie gladly leaves her four children with a pousse,
// which is an adventure in itself; she returns every favour twice, so as to owe nothing (fiche 10).

# arc: ortie_four
title: Les quatre de l'Ortie
family: village
stage: teen
trigger: draw
size: short

## memories
ortie.found_the_one | ortie | private | +2 | A retrouvé l'enfant qui lui manque toujours, du premier coup.

## scene: children
title: Les quatre de l'Ortie
pace: now

L'Ortie confie ses quatre enfants à {A} pour l'après-midi, « une heure, pas plus », et s'en va avant qu'on ait le temps de dire non.
Au bout de dix minutes, il en manque un. Toujours le même.

* count | Les compter toutes les cinq minutes  [needs trait diligent]
  [lean rationality +2]
  Un, deux, trois, quatre. Cinq minutes. Un, deux, trois… quatre, sous la table. {A} tient le compte jusqu'au soir sans en perdre un seul.
  L'Ortie paie le double, sans un mot, pour ne rien devoir.
  [cailloux 20] [end]

* river | Les emmener tous à la rivière
  [stress lazy 15]
  [lean energy +2]
  {A} emmène tout le monde à la Sommeille, et celui qui manque y est déjà, assis dans l'eau jusqu'au ventre. On y passe l'après-midi.
  Quatre enfants rentrent mouillés et ravis. {A} aussi.
  [modifier light_heart] [end]

* search | Chercher celui qui manque  [roll learning 3]
  [lean compassion +1]
  ? success
    {A} réfléchit comme un enfant de six ans qui ne veut pas être trouvé. Le grenier à foin du Noyer, évidemment.
    Le soir, l'Ortie regarde {A} longtemps. « Personne ne le trouve jamais, celui-là. » C'est presque un merci.
    [remember ortie.found_the_one] [end]
  ? failure
    {A} fouille la haie, le lavoir, et la mare, surtout la mare. L'enfant rentre tout seul pour le goûter. {A}, un peu plus tard.
    [modifier muddy_paws] [end]
