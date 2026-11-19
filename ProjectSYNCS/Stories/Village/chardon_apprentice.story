// L'apprenti du Chardon — wave 2 (adulte only), new. The Chardon grumbles first and helps after; he
// dreams of being told his flour is the best in the valley; he weighs everything, compliments included;
// he carves his apprentices' names on a beam (fiche 4). Reads mill_ducks' memories.

# arc: chardon_apprentice
title: L'apprenti du Chardon
family: village
stage: adult
trigger: draw
size: short

## memories
chardon.best_flour | chardon | private | +3 | Lui a dit que sa farine était la meilleure de la vallée. Il a pesé le compliment : il pesait juste.

## scene: mill
title: L'apprenti du Chardon
pace: now

Le Chardon a besoin d'un apprenti pour la journée : Mathurin est parti au gué, comme par hasard. Il grogne d'abord, et tend un tablier ensuite, toujours dans cet ordre.
[if memory chardon.kept_ducks] Il tend aussi un quignon de pain, sans regarder. « Pour les canards. Que je ne nourris pas. »
[if memory chardon.told_ducks] « Toi, tu parles trop. Tiens, prends ce sac. »

* sacks | Porter les sacs  [roll courage 3]
  [lean energy +2]
  ? success
    Un sac, deux sacs, vingt sacs. Le Chardon les compte, les pèse, et paie au poids, au caillou près, en grognant que c'est trop.
    [cailloux 15] [end]
  ? failure
    Le troisième sac éclate. {A} passe l'après-midi enfariné{a:|e} de la tête aux pieds. Le Chardon rit, fort, et s'en excuse presque.
    [modifier hearty] [end]

* flour | Lui dire que sa farine est la meilleure de la vallée
  [lean compassion +2]
  Le Chardon s'arrête. Il pose le compliment sur la balance, pour ainsi dire, et le regarde longtemps. « Exact. » Il a les yeux qui brillent.
  [remember chardon.best_flour] [end]

* beam | Demander qui sont les noms sur la poutre  [mystery]
  [lean sociability +1]
  « Pour mes comptes », grogne le Chardon. Puis il les lit tous, un par un, avec ce que chacun mangeait à midi. Le dernier est Mathurin. À côté, il y a une place.
  [grow learning] [end]
