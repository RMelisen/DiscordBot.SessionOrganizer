// La chèvre de Dame Ermengarde — wave 2 (adulte, ancien), new. The Intendant is afraid of goats and
// refuses to write it down (fiche 3); Dame Ermengarde is the council's old peony (lore § 12).

# arc: ermengarde_goat
title: La chèvre de Dame Ermengarde
family: village
stage: adult, elder
trigger: draw
size: short

## memories
intendant.goat_detour | intendant | private | +2 | A fait le grand tour avec lui pour éviter une chèvre, l'air de rien, et n'en a jamais parlé.

## scene: goat
title: La chèvre de Dame Ermengarde
pace: now

La chèvre de Dame Ermengarde s'est encore échappée. Elle broute devant la Maison commune, juste entre l'Intendant et la porte.
L'Intendant a peur des chèvres. Il regarde son registre avec une attention extraordinaire.

* catch | Attraper la chèvre  [roll courage 3]
  [stress craven 10]
  [lean boldness +2]
  ? success
    {A} attrape la chèvre par le collier et la ramène à Dame Ermengarde, qui sort sa bourse sans discuter.
    [cailloux 10] [end]
  ? failure
    La chèvre trouve l'idée amusante. {A} fait trois fois le tour de la place derrière elle, et finit dans l'abreuvoir.
    [modifier muddy_paws] [end]

* detour | Faire le grand tour avec l'Intendant, l'air de rien
  [lean compassion +2]
  « Il fait beau, de ce côté. » L'Intendant suit {A} par la ruelle, très digne, et arrive à la porte par derrière.
  Il ne dit rien. Il serre la main de {A} un peu plus longtemps que d'usage.
  [remember intendant.goat_detour] [end]

* grass | Proposer à la chèvre une meilleure herbe  [needs trait patient]
  [lean rationality +2]
  {A} arrache une poignée d'herbe du Communal, s'accroupit, et attend. Longtemps. La chèvre finit par venir, et par suivre {A} jusqu'à son pré.
  [grow diplomacy] [end]
