// Replaces teen_apprentice, teen_apprentice_owl, teen_apprentice_station, teen_apprentice_market,
// grown_master_owl, grown_master_station and grown_master_market.
# arc: apprentice
title: Le grand apprentissage
family: personal
stage: teen
trigger: draw
size: short

## scene: choice
title: Le grand apprentissage
was: teen_apprentice
pace: now

C'est la coutume : à la Maison des Pousses, chaque pousse choisit un maître pour la saison. Trois portes sont ouvertes à {A}. Maître Frêne, à la Maison commune, qui sait lire, écrire et tout mettre en règlement. Le Chardon, au moulin, qui pèse tout, compliments compris. Et le Fou, qui retrouve tout ce qui est perdu, sauf ses chaussures.

* owl | Suivre Maître Frêne
  [lean rationality +2]
  {A} pousse la porte de la Maison commune. Maître Frêne pose devant {A} une pile de registres plus haute que {A}. « Examinons d'abord le premier. Ensuite, selon la coutume, nous en parlerons. »
  [-> owl]

* hedgehog | Suivre le Chardon
  [lean honor +1] [lean rationality +1]
  {A} se présente au moulin. Le Chardon tend une balance, un carnet et un crayon, sans un mot, puis grogne : « Leçon un : un sac pèse ce qu'il pèse. Pas à peu près. » {A} note tout.
  [-> station]

* magpie | Suivre le Fou
  [lean boldness +1] [lean honor -1] [stress honest 10]
  {A} rejoint le Fou sous l'auvent de l'auberge. Le Fou examine {A} du chapeau aux pieds, rend un bouton que {A} n'avait pas vu partir, et chantonne : « Un bouton. Un. Tu as tout à apprendre. »
  [-> market]

## scene: owl
title: L'examen de Maître Frêne
was: teen_apprentice_owl
pace: 2d-4d

La saison touche à sa fin. Maître Frêne a préparé un examen : une seule question, écrite à la plume en haut d'une page blanche. « Que sais-tu, maintenant, que tu ne savais pas avant ? » Maître Frêne attend. La Maison commune attend aussi.

* recite | Réciter tout ce qui a été appris, dans l'ordre  [roll learning 7]
  [lean rationality +2]
  ? success
    {A} récite les raies et leurs feux, les fêtes et leurs bancs, les règlements du four et l'année où la Sommeille a changé de lit. Maître Frêne écoute jusqu'au bout, puis hoche la tête, une seule fois. Venant de Maître Frêne, c'est une médaille.
    [grow learning] [modifier inspired] [-> master_owl]
  ? failure
    {A} commence par les raies et se perd dans les règlements. Maître Frêne lève la main : « Tu en sais plus que tu ne crois. Il convient de revenir l'an prochain. Nous vérifierons. »
    [-> master_owl]

* humble | Répondre : « Que je ne sais presque rien. »
  [lean honor +1] [lean rationality +1] [stress arrogant 20]
  Maître Frêne se tait un long moment. Puis sort de sa poche une petite clé rouillée et la tend à {A} : la clé du vieux coffre aux registres, ceux que personne n'ouvre plus. « Maintenant, tu peux commencer. »
  [grow learning] [item col.cle_rouillee] [-> master_owl]

## scene: station
title: La tournée des sacs
was: teen_apprentice_station
pace: 2d-4d

Dernier jour d'apprentissage. Le Chardon confie à {A} la grande tournée : peser chaque sac de farine livré dans la semaine, du four de Pain-Blond jusqu'à l'auberge, et revenir au moulin avant que la roue s'arrête. Le Chardon ne regarde pas {A} partir. Le Chardon regarde la roue.

* run | Faire la tournée au pas de course  [roll stewardship 7]
  [lean energy +2] [stress lazy 20]
  ? success
    {A} pèse onze sacs, en trouve trois trop légers d'une poignée, et pousse la porte du moulin au dernier tour de roue. Le Chardon range la balance et tend à {A} un vieux caillou, frappé d'une marque que plus personne n'utilise. « Pour ton premier compte juste. »
    [grow stewardship] [item col.piece_ancienne] [-> master_station]
  ? failure
    La Courge refuse qu'on pèse sa farine, par principe. {A} discute, perd du temps, et arrive quand la roue est déjà arrêtée. Le Chardon ne dit rien. Le silence est pire qu'un sermon.
    [-> master_station]

* plan | Tracer d'abord le chemin le plus court sur une carte
  [lean rationality +2]
  {A} passe une heure à tracer l'itinéraire, puis fait la tournée en marchant, sans jamais courir, et arrive au dernier tour de roue, pile. Le Chardon regarde le plan, longtemps. Le plan est cloué depuis à la poutre du moulin, et le Chardon prétend que c'est pour ses comptes.
  [grow stewardship] [modifier trade_sense] [-> master_station]

## scene: market
title: La leçon du Fou
was: teen_apprentice_market
pace: 2d-4d

Pour la dernière leçon, le Fou emmène {A} sur la place du Puits, le jour du marché. Ses poches sont pleines de choses trouvées. « Aujourd'hui, on rend. Trois choses, à leurs trois maisons. Mais personne ne doit te voir. Sinon ce n'est plus un cadeau, c'est une conversation. »

* borrow | Tout rendre sans être {a:vu|vue}  [roll intrigue 7]
  [lean honor -2] [stress honest 30] [stress just 20]
  ? success
    {A} glisse une cuillère sur le rebord de la Courge, un ruban sous la porte de la Bruyère et une pipe dans la poche du Noyer, sans que personne ne remarque rien. Le Fou fait la révérence, pour la première fois de sa vie, et offre à {A} une bague trouvée « on ne sait où ».
    [grow intrigue] [item col.bague] [-> master_market]
  ? failure
    Le Noyer sent une main dans sa poche au moment où {A} y glisse la pipe. Explications. Excuses. Un après-midi aux Longues Raies pour se faire pardonner. Sous l'auvent, le Fou rit à s'en faire envoler une touffe.
    [-> master_market]

* refuse | Refuser : rendre, oui, mais en frappant à la porte
  [lean honor +2]
  Le Fou penche la tête. « Bien. C'est l'autre leçon. Tout le monde sait se cacher ; les meilleurs savent quand frapper. » Puis le Fou rend à {A}, l'air de rien, trois boutons perdus depuis le début de la saison.
  [item col.bouton] [modifier clear_conscience] [-> master_market]

* notes | Regarder le Fou faire, et prendre des notes
  [lean rationality +1]
  {A} suit le Fou toute la journée. Le soir, le carnet contient trois pages de croquis, deux de théories et une liste intitulée « Comment ne plus jamais rien perdre ». Le Fou lit la liste, et ajoute une ligne à la fin : « Sauf ses chaussures. »
  [modifier sly] [-> master_market]

## scene: master_owl
title: Le coffre aux registres
was: grown_master_owl
pace: 7d-10d

Maître Frêne frappe chez {A}, un rouleau sous le bras. « Je monte trois jours à Roncebelle, comparer nos registres avec ceux de l'Intendant. La Maison commune a besoin de quelqu'un qui sache dire « examinons ». » Maître Frêne tend la clé du vieux coffre, celui que personne n'ouvre. « Il ne convient pas de te demander de ne pas l'ouvrir. »

* guard | Garder la Maison commune, sans ouvrir le coffre
  [lean honor +2]
  {A} garde la Maison commune trois jours, règle deux querelles de bancs, et ne touche pas au coffre. Au retour, Maître Frêne vérifie la poussière sur la serrure, intacte, et sourit. « Tu as retenu la leçon d'autrefois. »
  [modifier clear_conscience] [end]

* read | Ouvrir le coffre, et lire un seul registre  [roll learning 8]
  [lean rationality +1] [lean honor -1]
  ? success
    {A} ouvre le coffre et prend le registre du dessus : c'est le premier de Maître Frêne, écrit à l'âge de {A}, avec un règlement pour l'ordre des goûters. {A} le lit d'une traite, et le remet exactement à sa place. Au retour, Maître Frêne regarde {A} et dit seulement : « Alors, tu sais. »
    [grow learning] [modifier inspired] [end]
  ? failure
    {A} ouvre le coffre. Un nuage de poussière en sort, avec une phalène très âgée et très vexée, qui suit {A} pendant trois jours. Au retour, Maître Frêne l'inscrit au registre, à la colonne des visiteurs.
    [lift happiness 0.1] [end]

## scene: master_station
title: Le moulin enrhumé
was: grown_master_station
pace: 7d-10d

Le Chardon a attrapé un rhume, le premier de sa carrière. Couché, une tisane de la Sauge à côté et la mine outrée, le Chardon tend à {A} la balance, le carnet et la clé du moulin. « La grande mouture de l'année. Tu te souviens de la leçon un ? » {A} s'en souvient : un sac pèse ce qu'il pèse.

* station | Tenir le moulin toute la journée, comme le Chardon  [roll stewardship 8]
  [lean rationality +1] [lean energy +1]
  ? success
    {A} ouvre le moulin à l'aube, pèse chaque sac, chasse un canard du bief, et rend la farine juste à tout le hameau. Le soir, {A} rapporte le carnet au Chardon, qui le relit, regarde {A}, et éternue de fierté.
    [grow stewardship] [cailloux 15] [end]
  ? failure
    {A} se trompe d'une poignée sur le sac de Pain-Blond. Personne ne remarque rien. Mais au lit, le Chardon a relu le carnet, et a compté. Le Chardon n'en dira jamais rien. Mais le Chardon sait.
    [end]

* soup | Tenir le moulin, et porter la soupe entre deux sacs
  [lean compassion +2]
  Entre chaque sac, {A} monte porter une soupe, une tisane, des nouvelles du bief. Le Chardon finit par guérir, surtout pour pouvoir grogner tranquille. La mouture est finie à temps. À peu près.
  [modifier cherished] [end]

## scene: master_market
title: Le grand sac du Fou
was: grown_master_market
pace: 7d-10d

Le Fou vide son grenier, sous le toit de l'auberge. Il en sort un grand sac. « Tout ce que j'ai trouvé sans jamais trouver à qui c'était. Trente ans. Avant l'hiver, je veux que tout soit rentré à la maison. Tu m'aides ? » Le sac fait un bruit de cuillères.

* night | Tout rendre en une nuit, sans être {a:vu|vue}  [roll intrigue 8]
  [lean honor +1] [lean boldness +1]
  ? success
    Toute la nuit, {A} et le Fou déposent sur les rebords les cuillères, les boutons, les dés à coudre et une paire de lunettes que Maître Frêne cherchait depuis le grand hiver. Au matin, le hameau retrouve trente ans d'objets perdus. Le Fou laisse à {A} une bague. « Celle-là, elle était dans un nid de geai. Elle est à toi. »
    [grow intrigue] [item col.bague] [end]
  ? failure
    {A} et le Fou se font surprendre par le Noyer, au moment de remettre sa propre pipe sur son propre rebord. Long silence. Puis le Noyer éclate de rire, et aide à rendre le reste.
    [lift happiness 0.15] [end]

* stall | Tout rendre au grand jour, au marché de la place
  [lean honor +2] [lean sociability +1]
  {A} étale tout sur une table, au milieu du marché, avec une pancarte : « Retrouvé par le Fou ». Le hameau vient chercher ses affaires en riant. Le Fou, très gêné, reçoit en échange tant de cadeaux que son grenier déborde à nouveau. Même le Chardon dit merci, après avoir grogné.
  [modifier clear_conscience] [modifier cherished] [end]
