// Replaces baby_grow_up, grown_dream_explore, grown_dream_cafe, grown_dream_owl and grown_dream_chief.
# arc: dream
title: Plus tard, je serai…
family: personal
stage: baby
trigger: draw
size: short

## scene: dream
title: Plus tard, je serai…
was: baby_grow_up
pace: now

{A} surgit d'un buisson de la Lisière, une brindille à la main, et la pointe vers la Mère Lichen : « Quand je serai {a:grand|grande}, je serai… » La Mère Lichen attend la suite, immobile comme un rocher, très intéressée.

* explore | « …partir explorer le monde entier ! »
  [lean boldness +2]
  « Commence par le fond de la Lisière », dit la Mère Lichen. {A} part aussitôt, et revient avec une carte, un ver de terre et de grandes ambitions.
  [grow courage] [-> explore]

* cafe | « …tenir l'auberge, comme la Courge ! »
  [lean greed +1] [lean sociability +1]
  « Alors entraîne-toi », dit la Mère Lichen, en tendant un bol vide. {A} sert une soupe imaginaire, rend une monnaie imaginaire, et réclame un pourboire bien réel.
  [grow stewardship] [-> cafe]

* owl | « …tout savoir, comme Maître Frêne ! »
  [lean rationality +2]
  « Alors compte mes galets », dit la Mère Lichen, d'un signe vers le tas devant la porte. {A} commence à compter. Le soleil se couche au galet trois cent quatre.
  [grow learning] [-> owl]

* chief | « …chef du village, et de tout le monde ! »  [needs trait bossy] [secret]
  [lean boldness +1] [lean sociability +1]
  « Très bien, chef », dit la Mère Lichen, sans bouger d'un poil de mousse. {A} donne trois ordres à la Mère Lichen, deux au Fou qui passait et un à la Sommeille. Seul le Fou obéit, mais le Fou allait déjà dans ce sens.
  [grow diplomacy] [-> chief]

## scene: explore
title: Le fond de la Lisière
was: grown_dream_explore
pace: 14d-16d

En rangeant, {A} retrouve une vieille carte dessinée au crayon : le fond de la Lisière, un ver de terre, une flèche, et en lettres énormes, « LE MONDE ENTIER ». La Mère Lichen avait dit, ce jour-là : « Commence par le fond de la Lisière. » La Lisière est faite depuis longtemps. Le reste du monde, pas encore.

* hills | Partir enfin, au-delà de la colline  [roll courage 8]
  [lean boldness +2]
  ? success
    {A} part à l'aube, passe la colline derrière les Mouilles, puis la suivante, et découvre un vallon que personne au hameau n'a jamais dessiné. {A} revient le surlendemain, avec une boussole trouvée sur un rocher, et de nouvelles flèches à dessiner.
    [grow courage] [item col.boussole] [end]
  ? failure
    {A} part à l'aube, se perd derrière la deuxième colline, et tourne en rond jusqu'au soir. Au retour, la Mère Lichen lève les yeux : « Le monde est grand. Recommence par le fond de la Lisière. » {A} rit, pour la première fois depuis des jours.
    [lift happiness 0.15] [end]

* map | Compléter la carte d'enfance, au crayon
  [lean rationality +1]
  {A} reprend la carte d'enfant et y ajoute, un à un, tous les chemins connus : le Pont-Vieux, le moulin, les Longues Raies, la Côte qui monte vers Roncebelle. Le ver de terre reste au centre, par respect.
  [grow learning] [modifier inspired] [end]

* heron | Aller montrer la carte à la Mère Lichen
  [lean sociability +1] [lean compassion +1]
  {A} déplie la carte devant la cabane. La Mère Lichen la regarde très longtemps, puis pose le doigt sur un endroit vide, tout au bord. « Là. Je n'y suis jamais allée. Pas encore. » Depuis, les deux en parlent parfois, assises sur la même pierre.
  [modifier cherished] [end]

## scene: cafe
title: Le bol ébréché
was: grown_dream_cafe
pace: 14d-16d

À l'auberge, la Courge sort de sous le comptoir un petit bol ébréché. « Tu te souviens, mon chou ? Tu servais des soupes imaginaires à la Mère Lichen, et tu réclamais un pourboire. Elle me l'a raconté. Une fois. » La Courge pose le bol devant {A}. « Mes enfants veulent leur samedi. L'auberge aurait bien besoin d'aide. »

* apron | Prendre le tablier, ce samedi  [roll stewardship 8]
  [lean sociability +1] [lean energy +1]
  ? success
    Samedi, {A} sert quarante bols, rend la monnaie juste, et prépare la chope du Noyer avant même que le Noyer n'ouvre la bouche. Le soir, la Courge verse dans le bol ébréché les pourboires de la journée.
    [grow stewardship] [cailloux 20] [end]
  ? failure
    Samedi, {A} renverse trois bols, confond le sel et le sucre, et sert le Noyer deux fois. Le soir, la Courge rit si fort qu'on l'entend du moulin. « Moi, le premier jour, j'ai mis le feu au rideau. »
    [lift happiness 0.2] [end]

* regular | Garder le bol, et venir en client
  [lean compassion +1]
  {A} garde le bol et promet de venir chaque samedi, comme client. Un très bon client, qui laisse toujours un pourboire dans le bol ébréché, posé au bout du comptoir. La Courge fait semblant de ne pas comprendre, et le raconte à tout le monde.
  [modifier cherished] [end]

* secret | Demander d'abord le secret de sa soupe
  [lean rationality +1] [lean greed +1]
  La Courge regarde à droite, à gauche, puis chuchote le secret à l'oreille de {A}. C'est très simple, et très surprenant. {A} jure de ne jamais le répéter. La Courge, pour une fois, non plus.
  [grow learning] [modifier hearty] [end]

## scene: owl
title: Trois cent cinq
was: grown_dream_owl
pace: 14d-16d

Un soir, on frappe chez {A}. C'est la Mère Lichen, qui ne quitte jamais la Lisière. « Trois cent cinq », dit-elle. {A} met une seconde à comprendre : le soir où {A} comptait ses galets, le soleil s'était couché au trois cent quatre. La Mère Lichen a attendu tout ce temps pour donner la suite.

* count | Aller reprendre le compte, à partir de trois cent six
  [lean rationality +2]
  {A} suit la Mère Lichen jusqu'au tas et reprend le compte à voix haute. La Mère Lichen s'assoit, très patiente. À la nuit tombée, le compte est fini : sept cent douze. La Mère Lichen hoche la tête, une fois. Le grand mystère d'enfance est résolu, et c'était beau.
  [grow learning] [modifier inspired] [end]

* keep | Garder la plume qu'elle a posée sur la table
  [lean compassion +1]
  En partant, la Mère Lichen a laissé une plume de geai sur la table, sans un mot. {A} la glisse dans le livre de chevet, comme marque-page. Chaque soir, en l'ouvrant, {A} lit une page de plus. La Mère Lichen n'en parle jamais.
  [item col.plume] [modifier cherished] [end]

* riddle | Lui répondre par une énigme  [roll learning 8]
  [lean rationality +1] [lean boldness +1]
  ? success
    « Et toi, combien d'hivers ? » La Mère Lichen ouvre la bouche, la referme, et repart, l'air soucieux. Trois jours plus tard, un galet attend sur le rebord, avec un mot du Fou, qui écrit pour elle : « Pas encore compté. »
    [grow learning] [end]
  ? failure
    « Combien de pierres compte le Pont-Vieux ? » La Mère Lichen répond aussitôt : « Mille quarante et une. » Le Fou les a comptées pour elle, il y a longtemps.
    [lift happiness 0.1] [end]

## scene: chief
title: Le chef du banquet
was: grown_dream_chief
pace: 14d-16d

Maître Frêne cherche quelqu'un pour organiser le banquet de moisson. Personne ne se propose. Puis le Fou lève la main : « Quand {A} était {a:petit|petite}, {A} m'a donné un ordre, à la Lisière. J'ai obéi. Je propose {A}. » Le conseil, à la surprise générale, approuve.

* lead | Accepter, et tout diriger  [roll diplomacy 8]
  [lean boldness +1] [lean sociability +1]
  ? success
    {A} donne des ordres à tout le monde, avec tact, cette fois. Le banquet est le plus beau depuis des années. Au premier rang, le Fou porte un ruban : « Premier à avoir obéi ».
    [grow diplomacy] [modifier well_spoken] [end]
  ? failure
    {A} donne trop d'ordres, trop vite. Le banquet finit en désordre, mais en joyeux désordre, et seul le Fou a suivi chaque consigne à la lettre. Ça compte.
    [lift happiness 0.15] [end]

* deputy | Accepter, à condition que le Fou soit adjoint
  [lean compassion +1] [lean rationality +1]
  Le Fou, ému, prend son rôle très au sérieux, et compte chaque lampion, deux fois, à voix haute. Les préparatifs prennent deux fois plus de temps, et sont deux fois plus soignés.
  [grow stewardship] [modifier cherished] [end]

* decline | Refuser poliment : c'était un rêve d'enfant
  [lean rationality +1] [lean boldness -1]
  {A} refuse, avec un sourire. Le conseil insiste un peu, pour la forme, puis se tourne vers le Noyer, ravi. Le soir, le Fou passe chez {A} pour dire : « Tu aurais fait un très bon chef. » {A} garde la phrase pour les mauvais jours.
  [modifier soothed] [end]
