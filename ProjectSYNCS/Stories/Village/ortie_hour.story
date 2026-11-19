// Une heure assise — wave 2 (adulte, ancien), new. The Ortie's one wish: one hour sitting down, without
// anyone shouting « maman »; she returns every favour twice, so as to owe nothing (fiche 10). Reads
// ortie_four's ortie.found_the_one.

# arc: ortie_hour
title: Une heure assise
family: village
stage: adult, elder
trigger: draw
size: short

## memories
ortie.one_hour | ortie | private | +3 | Lui a apporté une chaise et une tisane, et a monté la garde. Elle a eu son heure assise.

## scene: chair
title: Une heure assise
pace: now

L'Ortie passe en courant, un enfant sous chaque bras et un troisième accroché à la jupe. Le quatrième manque.
Elle n'a qu'une envie, tout le monde le sait : une heure assise, une seule, sans que personne ne crie « maman ».
[if memory ortie.found_the_one] Elle s'arrête devant {A}. « Toi. Tu trouves le petit. »

* kids | Prendre les quatre enfants pour l'après-midi
  [stress lazy 15]
  [lean energy +2]
  {A} emmène les quatre au bord de la Sommeille. Le quatrième y était déjà. Le soir, l'Ortie paie le double, sèchement, pour ne rien devoir.
  [cailloux 20] [end]

* guard | Lui apporter une chaise et une tisane, et monter la garde
  [lean compassion +2]
  {A} pose une chaise à l'ombre, une tisane à côté, et se plante devant la porte. Le premier « maman ! » est intercepté. Le deuxième aussi.
  L'Ortie dort assise une heure entière. Elle ne dit pas merci. Elle n'oubliera pas.
  [remember ortie.one_hour] [end]

* inn | L'emmener à l'auberge  [mystery]
  [lean sociability +2]
  À l'auberge, la Courge prend les enfants d'un geste, comme les soirs de marché. L'Ortie s'assoit près du feu, et commande un cidre.
  Elle le boit lentement. Elle rit, une fois, de quelque chose que personne n'a dit.
  [modifier light_heart] [end]
