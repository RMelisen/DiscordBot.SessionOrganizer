// Le dimanche de la Bruyère — wave 2 (adulte, ancien), new. The Bruyère wants pousses who leave ready and
// come back on Sundays; « s'ils veillent si bien, qu'ils viennent laver le linge » (fiche 8). Reads
// the Maison memories of wave 1 (lights_out, bridge_fireflies, house_rules).

# arc: bruyere_sunday
title: Le dimanche de la Bruyère
family: village
stage: adult, elder
trigger: draw
size: short

## memories
bruyere.washed_linen | bruyere | private | +3 | Est revenu{a:|e} à la Maison un dimanche, des années après, laver le linge avec elle.

## scene: sunday
title: Le dimanche de la Bruyère
pace: now

C'est dimanche. {A} repasse à la Maison des Pousses, comme on repasse chez soi. La Bruyère est au lavoir, les manches retroussées, et ne se retourne pas.
« Te voilà. Mange. » Il n'y a pourtant rien à manger, ici.
[if memory bruyere.felt_tucked] « Tu ne dormais pas, ce soir-là. Je le savais. »
[if memory bruyere.on_the_bridge] « Les lucioles sont revenues. Personne n'y était. Ni moi. »
[if memory bruyere.kept_rule] « La règle 14 tient toujours. Grâce à toi, un peu. »

* linen | Laver le linge avec elle
  [lean compassion +2]
  {A} retrousse ses manches. {a:Ils|Elles} lavent le linge de toute la Maison jusqu'à midi, sans parler. C'est la plus belle conversation de la semaine.
  [remember bruyere.washed_linen] [end]

* ados | Raconter une bêtise aux ados de la Maison
  [stress shy 10]
  [lean sociability +2]
  {A} raconte aux pousses d'aujourd'hui la plus belle bêtise de son temps. Ils la recommencent le soir même. La Bruyère fait semblant d'être furieuse.
  [modifier party_soul] [end]

* bench | Réparer le banc qu'elle réclame à Maître Frêne  [needs trait diligent]
  [lean zeal +1]
  Le banc boite depuis trois printemps. {A} le cale, le visse, le ponce. La Bruyère s'assoit dessus, longtemps, pour vérifier.
  [grow stewardship] [end]
