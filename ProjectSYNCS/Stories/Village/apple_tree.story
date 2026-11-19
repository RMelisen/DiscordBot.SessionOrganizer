// Tout en haut du pommier — wave 1 (bébé). Rewrites baby_apple_tree in the Tilleul's orchard: he
// tells everything to his bees, loudly (fiche 12), and the founding story changes every time.

# arc: apple_tree
title: Tout en haut du pommier
family: village
stage: baby
trigger: draw
size: short

## memories
tilleul.called_from_tree | tilleul | private | +2 | L'a appelé du haut de son pommier, une pomme à la main. Il l'a dit aux abeilles.

## scene: tree
title: Tout en haut du pommier
pace: now

Au clos du Tilleul, {A} a grimpé chercher une pomme. La pomme est attrapée.
Le problème, c'est la descente : vue d'en haut, la branche du bas est très, très loin.

* call | Appeler à l'aide
  [stress bossy 10]
  [lean sociability +2]
  Le Tilleul arrive, lentement, tend les bras, et {A} atterrit dedans avec la pomme.
  Puis il se retourne vers les ruches : « IL Y A UNE POUSSE DANS LE POMMIER. Il n'y en a plus. » À midi, tout le hameau le sait.
  [remember tilleul.called_from_tree] [end]

* climb | Redescendre sans aide  [roll courage 3]
  [lean boldness +2]
  ? success
    Branche par branche, sans regarder en bas. {A} pose un pied dans l'herbe, puis l'autre, et croque la pomme pour fêter ça.
    [grow courage] [lift hunger 0.2] [end]
  ? failure
    La dernière branche cède un peu plus tôt que prévu. {A} atterrit dans la boue, la pomme intacte, levée bien haut.
    [modifier muddy_paws] [end]

* wait | Attendre que le pommier se baisse  [needs trait pensive]
  [lean energy -2]
  Le pommier ne se baisse pas. Le Tilleul, lui, s'assoit dessous sans voir {A}, et raconte la fondation du village à ses abeilles.
  La version d'aujourd'hui a un dragon. {A} écoute tout, jusqu'au bout, puis descend quand il s'endort.
  [grow learning] [end]
