// Le jour du Compte — wave 2 (adulte, ancien), seasonal: autumn (lore § 8). The Intendant comes down to
// count everything and the village hides one thing from him; he always finds it, never before evening.
// He is afraid of goats (fiche 3). Reads pousse_count's intendant.stood_still.

# arc: count_day
title: Le jour du Compte
family: village
stage: adult, elder
trigger: draw
size: short
requires: [season autumn]

## memories
intendant.whispered_goat | intendant | private | +2 | Lui a soufflé où était cachée la chèvre, le jour du Compte, avant le soir. Il ne l'a dit à personne.

## scene: count
title: Le jour du Compte
pace: now

C'est le jour du Compte. L'Intendant descend la Côte, son registre sous le bras, et le hameau lui cache une chose, comme chaque année.
Cette année, c'est une chèvre. Et c'est {A} qui doit la cacher. L'Intendant a peur des chèvres.
[if memory intendant.stood_still] Il s'arrête devant {A}. « Vous, au moins, vous ne bougez pas. D'habitude. »

* home | La cacher chez soi
  [lean boldness +1]
  La chèvre passe la journée dans la cuisine de {A}, et mange un rideau. L'Intendant la trouve au soir, comme toujours, et note : « Chèvre : une. Rideau : un de moins. »
  [modifier sly] [end]

* bench | La cacher dans la Maison commune, sous un banc  [roll intrigue 3]
  [lean rationality +1]
  ? success
    Sous le banc, derrière une nappe, la chèvre se tient tranquille toute la journée. L'Intendant ne la trouve qu'au banquet du soir. Tout le monde applaudit, lui aussi.
    [modifier party_soul] [end]
  ? failure
    La chèvre mange la nappe, puis sort. Elle passe devant l'Intendant à midi. Il pousse un petit cri, le note, et barre le petit cri.
    [modifier muddy_paws] [end]

* tell | Lui souffler où elle est, avant le soir
  [stress zealous 10]
  [lean compassion +2]
  {A} souffle la cachette à l'Intendant, tout bas. Il hoche la tête, très sérieux, et attend quand même le soir pour la « trouver ». La coutume est sauve.
  [remember intendant.whispered_goat] [end]
