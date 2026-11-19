// Aude au gué — wave 2 (adulte, ancien), new. Aude, the Noyer's daughter, loves Mathurin, the Chardon's
// apprentice; the whole village keeps watch for them, the Noyer alone doesn't know (lore § 11, § 12).
// Reads ford_watch's noyer.pumpkin_talk. Aude and Mathurin hold no memory (not in the cast).

# arc: aude_note
title: Aude au gué
family: village
stage: adult, elder
trigger: draw
size: short

## scene: note
title: Aude au gué
pace: now

Aude, la fille du Noyer, tend à {A} un petit mot plié en huit. « Pour le moulin. Tu ne l'ouvres pas. »
Au moulin, à la fenêtre du haut, Mathurin attend déjà, rouge jusqu'aux oreilles.
[if memory noyer.pumpkin_talk] Aude sourit. « Et si mon père arrive, toi, tu sais parler citrouilles. »

* carry | Porter le mot sans l'ouvrir
  [stress curious 15]
  [lean honor +2]
  {A} porte le mot au moulin, à travers le pré, sous le nez du Chardon, qui regarde ailleurs avec beaucoup d'application.
  [modifier light_heart] [end]

* reply | Aider Mathurin à écrire la réponse
  [lean compassion +2]
  Mathurin a écrit trois mots et les a barrés. {A} l'aide à en trouver d'autres. Ils en trouvent sept. C'est beaucoup, pour Mathurin.
  [grow diplomacy] [end]

* detour | Faire un détour par la place, très visible  [needs trait sadistic]
  [lean boldness +1]
  {A} traverse la place en agitant le mot. Tout le village fait semblant de ne pas voir, comme d'habitude, avec un très grand sérieux.
  Le Noyer, au puits, demande ce qu'on fête.
  [modifier sly] [end]
