# arc: slate_line
title: La ligne sur l'ardoise
family: village
stage: baby, teen, adult, elder
trigger: visit
size: short
other: anyone
cooldown: 7d

## scene: slate
title: La ligne sur l'ardoise
pace: now

À l'auberge, une ligne est apparue sur l'ardoise de {A} depuis la visite : « un lait au miel, pour {B} ». L'écriture ressemble beaucoup à celle de {B}. Elle ressemble aussi un peu à celle des enfants de la Courge, qui rient très fort derrière le comptoir.

* pay | Payer, sans rien dire
  [stress greedy 15]
  [lean greed -2]
  {A} pose un caillou sur le comptoir et efface la ligne. Le soir même, {B} boit un lait au miel sans savoir d'où il vient, et {A} le regarde boire du fond de la salle, l'air de rien.
  [affinity +3] [modifier light_heart] [end]

* reply | Ajouter une ligne sur l'ardoise de {B}
  [lean boldness +1] [lean vengefulness +1]
  {A} prend la craie et écrit, sur l'ardoise de {B} : « deux laits au miel, pour {A} ». Le lendemain, il y en a trois sur la sienne. L'affaire promet de durer longtemps.
  [modifier sly] [end]

* kids | Questionner les enfants de la Courge  [roll intrigue 3]
  [lean rationality +1]
  ? success
    {A} pose trois questions, l'air de rien, et les enfants de la Courge avouent à la quatrième : c'était pour voir. Ils effacent la ligne, et offrent le lait au miel pour la peine.
    [grow intrigue] [end]
  ? failure
    Les trois enfants de la Courge jurent, la main sur le cœur, qu'ils n'ont rien vu. Ils jurent si fort, et si longtemps, que {A} n'en saura jamais rien.
    [modifier distracted] [end]
