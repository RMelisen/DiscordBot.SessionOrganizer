// Le quatrième aux cartes — wave 2 (adulte only), new. The inn always lacks a fourth at cards (fiche 6);
// the Noyer has the longest slate (fiche 9); the Fou counts things aloud (fiche 13).

# arc: fourth_at_cards
title: Le quatrième aux cartes
family: village
stage: adult
trigger: draw
size: short

## memories
fou.let_him_win | fou | private | +2 | L'a fait gagner aux cartes, un soir, à l'auberge. Il a compté ses gains à voix haute toute la nuit.

## scene: table
title: Le quatrième aux cartes
pace: now

À l'auberge, il manque un quatrième aux cartes. Le Noyer joue pour gagner. La Courge joue pour parler. Le Fou joue pour compter les cartes à voix haute, ce qui ne l'aide pas.
La chaise vide est tirée vers {A}.

* win | Jouer pour gagner  [roll intrigue 3]
  [lean greed +2]
  ? success
    {A} gagne la première main, puis la deuxième. Le Noyer paie, beau joueur, et commande une tournée qu'il met sur son ardoise.
    [cailloux 20] [end]
  ? failure
    {A} perd tout ce qu'il y avait à perdre, c'est-à-dire trois cailloux et beaucoup de fierté. Le Noyer en parle à la troisième personne jusqu'à minuit.
    [modifier grumpy] [end]

* fou | Faire gagner le Fou
  [stress greedy 10]
  [lean compassion +2]
  {A} perd exprès, très habilement. Le Fou gagne, et ne s'en remet pas. « Trois, quatre, cinq cailloux. Cinq ! C'est un de trop. Je suis riche. »
  [remember fou.let_him_win] [end]

* stories | Raconter des histoires entre les donnes
  [lean sociability +2]
  {A} raconte une histoire à chaque donne. Au bout d'une heure, plus personne ne joue. Les cartes restent sur la table, oubliées.
  [modifier well_spoken] [end]
