// Replaces teen_befriend, teen_befriend_rain, teen_befriend_ask and reply_befriend.
# arc: befriend
title: L'amitié en trois étapes
family: friendship
stage: teen
trigger: draw
size: short
other: anyone
other_if: [relation acquaintances] [affinity >= 0]

## scene: plan
title: L'amitié en trois étapes
was: teen_befriend
pace: now

Depuis quelque temps, {A} aimerait bien devenir {a:ami|amie} avec {B}. Le problème, c'est que {A} ne sait pas du tout comment on fait. Maître Frêne a sûrement un règlement là-dessus. En attendant, {A} a écrit un plan en trois étapes sur un bout de papier. L'étape un est soulignée deux fois.

* hello | Suivre le plan : étape un, dire bonjour
  [lean rationality +1] [lean sociability +1]
  {A} attend {B} au coin de la place du Puits et dit bonjour. {B} répond bonjour. Fin de l'étape un. {A} rentre le cœur battant, et coche la case avec un soin particulier.
  [affinity +5] [-> rain]

* jam | Commencer par un petit cadeau anonyme
  [lean compassion +1]
  {A} laisse devant la porte de {B} un pot de confiture, avec un mot : « De la part de quelqu'un. » {B} goûte, et passe la journée à chercher qui. {A} passe la journée à faire semblant de ne pas savoir.
  [affinity +10] [-> rain]

* blunt | Aller droit au but : « Tu veux être mon {b:ami|amie} ? »  [roll diplomacy 6]
  [lean boldness +2] [stress shy 20]
  ? success
    {B} cligne des yeux, {b:surpris|surprise}, puis rit. « On peut commencer par un goûter. » C'est une réponse. C'est même une très bonne réponse.
    [affinity +15] [-> rain]
  ? failure
    {B} répond poliment « peut-être » et s'éclipse. {A} range le plan dans sa poche. Retour à l'étape un, mais en plus discret.
    [affinity +5] [-> rain]

## scene: rain
title: L'étape deux
was: teen_befriend_rain
pace: 24h-48h

L'étape deux du plan dit : « Faire quelque chose ensemble. » {A} avait tout prévu : la pêche, les cerfs-volants, le concours de grimaces. Mais ce matin, la pluie tombe à verse, et {B} est {b:coincé|coincée} sous le même auvent que {A}, devant le four de Pain-Blond, fermé ce jour-là.

* cards | Sortir le jeu de cartes de sa poche
  [lean sociability +1]
  {A} sort le jeu de cartes. {A} et {B} jouent sous l'auvent jusqu'à ce que la pluie s'arrête, puis encore un peu après. {B} triche très mal, et {A} fait semblant de ne rien voir. C'est le début de quelque chose.
  [affinity +10] [-> ask]

* race | Proposer une course sous la pluie jusqu'à l'auberge  [roll courage 6]
  [lean boldness +2]
  ? success
    {A} et {B} courent sous la pluie, sautent dans toutes les flaques, et arrivent à l'auberge en dégoulinant, riant si fort que la Courge sert deux bols de soupe sans rien demander, et pose dix questions sans attendre les réponses.
    [grow courage] [affinity +15] [-> ask]
  ? failure
    {A} glisse dans la première flaque et s'assoit dedans. {B} hésite, puis s'assoit à côté, dans la flaque, par solidarité. Les deux restent là un moment, sous la pluie, à rire comme des idiots.
    [affinity +10] [-> ask]

* talk | Parler, simplement
  [lean sociability +2]
  Sous l'auvent, {A} et {B} parlent de tout : des nuages, du moulin, d'un rêve bizarre. Au bout d'une heure, {A} se rend compte que l'étape deux est en train de se faire toute seule, sans plan.
  [grow diplomacy] [affinity +10] [-> ask]

## scene: ask
title: L'étape trois
was: teen_befriend_ask
pace: 24h-48h

L'étape trois du plan est écrite en tout petit, en bas de la page : « Demander. » {A} relit le mot une dizaine de fois. Ça paraît simple, sur le papier. {B} est là, à quelques pas, en train de donner du pain aux canards.

* ask | Demander à {B} : « On est amis, maintenant ? »  [owner-only]
  [lean sociability +2] [lean boldness +1]
  {A} prend une grande inspiration, s'approche, et pose la question, le papier serré dans la main.
  [-> answer]

* ducks | Jeter le plan, et nourrir les canards avec {B}
  [lean rationality +1]
  {A} froisse le plan et le fourre au fond de sa poche. Puis s'approche, prend un morceau de pain et le donne aux canards, à côté de {B}. Personne ne demande rien. Pas besoin, finalement.
  [affinity +10] [end]

## scene: answer
title: Une question au bord de l'étang
was: reply_befriend
pace: now
handover

Au bord de l'étang, {B} s'est {b:approché|approchée} de {A}, un papier froissé à la main, et a demandé : « On est amis, maintenant ? »

* yes | Dire oui, et partager le pain  [accept]
  [lean sociability +2]
  {A} dit oui, et tend un morceau de pain. {B} le donne aux canards, puis lit le papier à voix haute, étape par étape. Les deux rient jusqu'à la nuit tombée.
  [bond friends] [end]

* soon | « Presque. Encore un goûter ou deux. »  [refuse]
  [lean rationality +1]
  {A} sourit : « Presque. Encore un goûter ou deux. » {B} sort un crayon et ajoute une étape quatre au plan : « Goûters. »
  [affinity +5] [end]
