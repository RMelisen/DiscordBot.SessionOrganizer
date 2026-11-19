# Visites Plynling — fiche d'écriture des scènes (vague 1)

Fiche de relecture pour `docs/superpowers/specs/2026-11-19-plynling-visit-choices-design.md` (tâche 1 du
plan `docs/superpowers/plans/2026-11-19-plynling-visit-choices.md`). **À relire par le propriétaire.**
Aucun fichier `.visit` n'est écrit avant l'accord ; une fois approuvée, chaque bloc ci-dessous est
recopié tel quel dans `ProjectSYNCS/Stories/Visits/<clé>.visit`.

## Comment lire un bloc

- `{A}` est toujours le **visiteur**, `{B}` toujours l'**hôte** ; `{a:m|f}`, `{b:m|f}` accordent avec l'un,
  `{p:m|f}` avec les deux (féminin seulement pour deux filles), `{ils}` / `{Ils}` aussi.
- `*` : une option du visiteur (son veilleur choisit). `>` : une réaction de l'hôte (son veilleur choisit).
  Le libellé est le texte du bouton ; sur la carte de l'hôte, il devient « **{A}** a choisi : *libellé en
  minuscule* ».
- `[lean axe ±n]` : ce vers quoi penche le choix. **L'option du visiteur est jugée par la personnalité de
  l'hôte, la réaction de l'hôte par celle du visiteur.** Rien ne s'affiche : pas de 🎲, pas de pourcentage.
  Axes : boldness (audace), compassion, greed (avidité), energy, honor, rationality, sociability,
  vengefulness, zeal (superstition).
- `? good` / `? bad` : les deux demi-fins. La fin d'une visite = la demi-fin de l'option, puis celle de la
  réaction, **de même signe** (un seul tirage), puis les lignes de résultat habituelles (lien, bonheur,
  trouvailles, badges). **Une demi-fin de l'hôte doit se lire après n'importe quelle option** : elle parle
  de l'attitude de l'hôte, pas du geste exact du visiteur.
- L'aveu (`# confession:`) a `? yes` / `? no` au lieu de `good` / `bad`. Son intro est ce que montre la
  carte de l'hôte : elle dit clairement que `{A}` est venu{a:|e} avouer.
- Une « mauvaise » fin est drôle ou un peu penaude, jamais une punition (§ 1 de la bible). Pas de
  chapeau, de lamelles ni de spores (les lignes restent neutres de famille), pas d'animaux de l'ancienne
  couche.

## La liste

Heures : Paris ; sans `hours`, toute la journée. Étapes : sans `stages`, les quatre.

| # | Clé | Lieu | Liens | Heures | Étapes | En une ligne |
|---|---|---|---|---|---|---|
| 1 | `ricochets` | 🌊 Au bord de la Sommeille | connaissances, amis, meilleurs amis, rivaux | 7–21 | toutes | Le passeur et un record de ricochets que personne ne se rappelle |
| 2 | `oven_queue` | 🥖 Devant le four du village | connaissances, amis | 5–12 | toutes | La file du four, et Pain-Blond qui cherche un goûteur |
| 3 | `tilleul_bees` | 🍯 Au clos du Tilleul | connaissances, amis, meilleurs amis, couple | 8–19 | toutes | Le Tilleul annonce la visite aux abeilles, et demande quoi ajouter |
| 4 | `last_ribbon` | 🎀 Au marché, sur la place du Puits | connaissances, rivaux, ennemis | 8–18 | toutes | Le dernier ruban de la colporteuse, deux mains tendues |
| 5 | `frog_count` | 🐸 Au bord de l'étang des Mouilles | connaissances, amis, meilleurs amis, rivaux | toute | toutes | Le Fou compte les grenouilles et il lui faut des yeux de plus |
| 6 | `card_partners` | 🃏 À l'auberge du Pont-Vieux | amis, rivaux, ennemis | 17–1 | ado, adulte, ancien | La Courge les met ensemble aux cartes, contre le Noyer et le Fou |
| 7 | `greet_souche` | 🍄 À la Lisière, devant la Souche | connaissances, amis, meilleurs amis, couple | toute | toutes | Saluer la Souche, sous l'œil immobile de la Mère Lichen |
| 8 | `missing_rule` | 🏠 À la Maison des Pousses | connaissances, amis, meilleurs amis, rivaux | toute | ado | Il n'y a pas de règle vingt-trois, et la Bruyère est au potager |
| 9 | `lichen_tisane` | 🪨 Chez la Mère Lichen | connaissances, amis, meilleurs amis | toute | bébé | Deux bols de tisane que personne n'a demandés |
| 10 | `house_call` | 🏡 Chez {B} | connaissances, amis, meilleurs amis, couple | toute | adulte, ancien | La petite maison de {B}, la chaise qui boite, la marmite qui chante |
| 11 | `meadow_quarrel` | 🌾 Au pré de la Sommeille | connaissances, rivaux, ennemis | 6–21 | toutes | Le Noyer et le Chardon leur demandent à qui est le pré |
| 12 | `goat_follows` | 🐐 Au Communal | les six | toute | toutes | La chèvre commune a décidé de suivre {B}, partout |
| 13 | `secret_ducks` | 🦆 Au bief du moulin | connaissances, amis, meilleurs amis | 6–20 | toutes | Le Chardon surpris à nourrir ses canards, qu'il ne nourrit pas |
| 14 | `river_dare` | 💦 À la baignade de la Sommeille | amis, meilleurs amis, couple, rivaux | 10–20 | toutes | L'eau est froide, et quelqu'un doit y aller le premier |
| 15 | `rain_lights` | 🌧️ À la Lisière, une nuit de pluie | amis, meilleurs amis, couple | 20–1 | toutes | Guetter les lumières avec le Fou, sous une fougère |
| 16 | `courge_table` | 🍲 À l'auberge, à la table de la Courge | connaissances, amis, couple, ennemis | 11–22 | toutes | « Comment vous vous êtes connus ? » — et la Courge n'attend pas |
| 17 | `forgotten_nuts` | 🌰 Sur le chemin du Noyer | rivaux, ennemis | toute | toutes | Un seul panier de noix « oublié », et deux qui arrivent en même temps |
| 18 | `grimace_contest` | 🌿 À la Maison aux Simples | amis, meilleurs amis, rivaux, ennemis | 8–19 | toutes | Le nouveau remède de la Sauge, et deux goûteurs |
| 19 | `rain_shelter` | ☔ Sous l'auvent de la Maison commune | les six | toute | toutes | Une averse, et un auvent fait pour une personne et demie |
| 20 | `lookout` | 🌾 Près du gué, à la tombée du jour | amis, meilleurs amis, couple | 17–22 | ado, adulte, ancien | Leur tour de faire le guet pour Aude et Mathurin |
| ♥ | `confess` | 🌉 Sur le Pont-Vieux | (aveu) | toute | toutes | {A} est venu{a:\|e} avouer ses sentiments |

**Couverture** — scènes possibles par lien : connaissances 14, amis 17, meilleurs amis 14, couple 9,
rivaux 11, ennemis 8. Toute la journée et à toute étape, chaque lien a au moins une scène :
connaissances, amis et meilleurs amis (`frog_count`, `greet_souche`, `goat_follows`, `rain_shelter`),
couple (`greet_souche`, `goat_follows`, `rain_shelter`), rivaux (`frog_count`, `goat_follows`,
`forgotten_nuts`, `rain_shelter`), ennemis (`goat_follows`, `forgotten_nuts`, `rain_shelter`).

Les scènes qui servent à la fois des liens amicaux et hostiles (`goat_follows`, `rain_shelter`,
`last_ribbon`, `courge_table`, `grimace_contest`, `card_partners`, `meadow_quarrel`…) sont écrites sur la
situation, sans supposer d'amitié ni de rancune : une bonne fin entre ennemis est une trêve d'un moment.

---

## 1. Les ricochets

```
# visit: ricochets
title: Les ricochets
place: 🌊 Au bord de la Sommeille
bonds: acquaintances friends bestfriends rivals
hours: 7-21

Le passeur de la Sommeille a aligné des galets plats sur la rive, rangés par taille. Il dit que le record du gué est de sept rebonds, et que personne ne se souvient de qui le tient. Lui s'en souvient, mais il ne le dira pas.

* far | Viser le record  [lean boldness +1] [lean energy +1]
  ? good
  Le galet de {A} rebondit six fois, sept, huit, et va se poser sur l'autre rive. Le passeur hoche la tête, très lentement, comme on salue.
  ? bad
  Le galet de {A} fait un seul rebond, énorme, et retombe dans la barque du passeur. Il le rend sans un mot.

* pick | Offrir le plus beau galet  [lean compassion +1] [lean sociability +1]
  ? good
  {A} fouille la rive et tend à {B} un galet si plat qu'on pourrait écrire dessus. Il rebondit presque tout seul.
  ? bad
  {A} cherche si longtemps le galet parfait qu'il n'en reste plus un seul de plat sur la rive : le passeur les a tous lancés en attendant.

* count | Compter les rebonds  [lean rationality +2]
  ? good
  {A} compte à voix haute, sans tricher, et annonce les chiffres comme le ferait l'Intendant. Personne n'a jamais été aussi bien compté.
  ? bad
  {A} compte trop vite, recompte, et finit par donner sept à tout le monde, par prudence. Le passeur fronce un sourcil.

> cheer | Applaudir chaque essai  [lean sociability +1] [lean compassion +1]
  ? good
  {B} applaudit chaque rebond, même ceux qui n'en sont pas. À la fin, le passeur s'y met aussi.
  ? bad
  {B} applaudit si fort qu'un canard s'envole, vexé, et renverse la pile de galets en partant.

> beat | Essayer de faire mieux  [lean greed +1] [lean energy +1]
  ? good
  {B} lance à son tour, pour faire mieux, et fait exactement aussi bien. {Ils} décident que c'est un record partagé.
  ? bad
  {B} lance à son tour, pour faire mieux, glisse sur la rive et finit les pieds dans la Sommeille. L'eau est froide. Le galet, lui, a fait trois rebonds.

> watch | Regarder l'eau, simplement  [lean energy -1] [lean rationality +1]
  ? good
  {B} s'assoit et regarde les ronds s'élargir jusqu'à disparaître. Au bout d'un moment, plus personne ne lance rien, et c'est très bien comme ça.
  ? bad
  {B} s'assoit pour regarder l'eau et s'endort contre un rocher. Le passeur pose un galet plat sur la tête de {B}, pour voir. Il y tient.
```

## 2. La file du four

```
# visit: oven_queue
title: La file du four
place: 🥖 Devant le four du village
bonds: acquaintances friends
hours: 5-12

C'est jour de cuisson, et la file devant le four suit le règlement de Maître Frêne, affiché sur la porte : article un, on ne double pas. Pain-Blond sort la tête du fournil, tout rouge, un petit pain à la main qu'il n'a montré à personne. « Quelqu'un veut goûter ? » demande-t-il, tout bas.

* taste | Goûter le petit pain  [lean boldness +1] [lean sociability +1]
  ? good
  {A} goûte. C'est aux orties, avec une pointe de miel, et c'est délicieux. Pain-Blond devient plus rouge que son four.
  ? bad
  {A} goûte. C'est aux orties, sans le miel. {A} dit « intéressant » avec beaucoup de conviction, et Pain-Blond range la réponse dans un coin de sa tête.

* place | Garder la place dans la file  [lean honor +2]
  ? good
  {A} garde la place, droit{a:|e} comme un article du règlement. Quand vient leur tour, Pain-Blond glisse deux petits pains de plus dans le panier, pour la patience.
  ? bad
  {A} garde la place, mais la Courge passe devant en disant « je ne fais que passer, mon chou ». Elle passe avec trois paniers.

* help | Aider à porter les fournées  [lean energy +1] [lean compassion +1]
  ? good
  {A} porte les planches de pain jusqu'à la place, deux à la fois. Pain-Blond paie en pain chaud, comme toujours.
  ? bad
  {A} porte une planche de trop, et un pain roule jusque sous le banc de la tisserande. Elle regarde le pain, puis {A}, et ne dit rien. Longtemps.

> share | Goûter aussi  [lean greed +1] [lean boldness +1]
  ? good
  {B} goûte aussi, et redemande. Pain-Blond reste sur le seuil, sans savoir quoi faire de ses mains. C'est la première fois qu'on redemande.
  ? bad
  {B} goûte aussi, mâche longtemps, et demande poliment s'il y a des noix dedans. Il n'y en a pas. Pain-Blond rentre réfléchir.

> chat | Faire la conversation à la file  [lean sociability +2]
  ? good
  {B} fait la conversation à toute la file et récolte trois nouvelles et une recette. Le temps passe sans que personne le remarque.
  ? bad
  {B} fait la conversation à toute la file et rate son tour. Il faut tout recommencer derrière le Noyer, qui en profite pour raconter sa citrouille.

> wait | Attendre en silence  [lean sociability -1] [lean rationality +1]
  ? good
  {B} attend sans un mot, les yeux mi-clos, à respirer l'odeur du pain. Bertille, qui passe, renifle et dit : « Toi, tu sens le pain de seigle. » C'est un compliment, chez elle.
  ? bad
  {B} attend en silence, si bien que la file finit par oublier qu'il y a quelqu'un à cette place, et passe devant, une personne après l'autre.
```

## 3. Ce qu'on dit aux abeilles

```
# visit: tilleul_bees
title: Ce qu'on dit aux abeilles
place: 🍯 Au clos du Tilleul
bonds: acquaintances friends bestfriends lovers
hours: 8-19

Le Tilleul les voit arriver par le chemin, se tourne vers ses ruches et leur annonce, très fort, que {A} est venu{a:|e} voir {B}. Puis il se retourne. « Et qu'est-ce que je leur dis d'autre ? Il faut tout leur dire, sinon elles boudent. »

* truth | Lui dicter la vérité, mot pour mot  [lean honor +2]
  ? good
  {A} dicte une phrase simple et vraie, et le Tilleul la crie aux ruches mot pour mot. Les abeilles bourdonnent plus fort. Il dit que c'est oui.
  ? bad
  {A} dicte une phrase simple et vraie. Le Tilleul, un peu sourd, la crie aux ruches toute de travers. Les jumelles du Communal la répéteront ce soir, encore plus de travers.

* tale | Inventer une grande histoire  [lean honor -1] [lean boldness +1]
  ? good
  {A} invente une traversée de la Lisière, un orage, un sanglier. Le Tilleul ajoute un deuxième sanglier. Les abeilles n'ont jamais entendu une si belle visite.
  ? bad
  {A} invente une grande histoire, et le Tilleul s'arrête au milieu pour la raconter à sa façon, puis la recommence depuis le début. Ça dure jusqu'au goûter.

* honey | Demander des nouvelles des ruches  [lean compassion +1] [lean sociability +1]
  ? good
  {A} demande comment vont les ruches. Le Tilleul en oublie les nouvelles et fait le tour des ruches, une par une. Il a enfin quelqu'un qui écoute jusqu'au bout.
  ? bad
  {A} demande comment vont les ruches, et le Tilleul répond pour la ruche numéro un, puis la deux, puis la trois. Il y en a onze.

> blush | Rougir de la tête aux pieds  [lean sociability -1] [lean compassion +1]
  ? good
  {B} rougit de la tête aux pieds. Une abeille vient s'y poser, comme sur une fleur, et {B} n'ose plus bouger. C'est très joli.
  ? bad
  {B} rougit de la tête aux pieds, et trois abeilles viennent s'y poser, comme sur une fleur. {B} passe le reste de la visite immobile, à loucher dessus.

> correct | Corriger le Tilleul, poliment  [lean rationality +1] [lean honor +1]
  ? good
  {B} corrige un mot, poliment. Le Tilleul remercie, corrige auprès des ruches, et offre un rayon de miel « pour l'exactitude ».
  ? bad
  {B} corrige un mot, poliment. Le Tilleul entend un autre mot, encore plus faux, et l'annonce aux ruches avec beaucoup de joie.

> add | Ajouter sa propre nouvelle  [lean sociability +2]
  ? good
  {B} ajoute sa propre nouvelle, et le Tilleul la crie aux ruches avec la première. Le soir, tout le hameau en parle, et gentiment.
  ? bad
  {B} ajoute sa propre nouvelle, très fort, pour être entendu{b:|e}. Les abeilles décident qu'on a assez crié et rentrent toutes ensemble.
```

## 4. Le dernier ruban

```
# visit: last_ribbon
title: Le dernier ruban
place: 🎀 Au marché, sur la place du Puits
bonds: acquaintances rivals enemies
hours: 8-18

La colporteuse est de passage, et il ne lui reste qu'un ruban : vert mousse, juste de la bonne longueur. {A} et {B} tendent la main en même temps. La colporteuse croise les bras et attend de voir ce que vont faire ces deux-là.

* offer | Le laisser à l'autre  [lean compassion +2]
  ? good
  {A} retire la main. La colporteuse hoche la tête, coupe le ruban en deux, et ne fait payer que la moitié. Elle ne dit toujours pas son nom.
  ? bad
  {A} retire la main, avec un grand geste. Le geste est si grand qu'il renverse le panier de sel. La colporteuse compte les grains. Tous.

* bid | Offrir un caillou de plus  [lean greed +1] [lean boldness +1]
  ? good
  {A} offre un caillou de plus. La colporteuse l'empoche, puis tire de son sac un deuxième ruban, qu'elle gardait « pour une bonne vente ».
  ? bad
  {A} offre un caillou de plus, puis deux, et la place entière s'arrête pour regarder. Le ruban finit autour du cou de la chèvre commune, qui passait par là. Personne ne sait comment.

* turns | Proposer de le porter chacun son tour  [lean sociability +1] [lean rationality +1]
  ? good
  {A} propose de le porter chacun son tour, un jour sur deux. La colporteuse trouve l'idée si raisonnable qu'elle note la date sur un bout d'écorce.
  ? bad
  {A} propose de le porter à deux, en même temps, pour gagner du temps. On essaie. Le ruban est vraiment juste de la bonne longueur pour une seule personne.

> grab | Le prendre quand même  [lean greed +2]
  ? good
  {B} prend le ruban, puis s'arrête, et le noue au poignet de {A}. Personne, sur la place, ne s'y attendait. Surtout pas {B}.
  ? bad
  {B} prend le ruban d'un coup, si vite que le nœud se défait et que le ruban s'envole jusqu'au toit de la Maison commune. Il y est encore.

> laugh | En rire  [lean sociability +1] [lean vengefulness -1]
  ? good
  {B} éclate de rire, et la colporteuse aussi, et la tisserande au bord de la place. D'un coup, le ruban a l'air beaucoup moins important.
  ? bad
  {B} éclate de rire, un peu trop fort et un peu trop longtemps. La colporteuse range le ruban, ferme son sac et repart vers la Côte. Elle reviendra au printemps.

> haggle | Négocier avec la colporteuse  [lean rationality +1] [lean greed +1]
  ? good
  {B} négocie si bien que la colporteuse lâche le ruban, un sachet de sel et la moitié d'une nouvelle d'ailleurs. Elle garde l'autre moitié pour la prochaine fois.
  ? bad
  {B} négocie, la colporteuse aussi, et au bout d'un quart d'heure le ruban coûte trois cailloux de plus qu'au début. Personne ne sait comment.
```

## 5. Le compte des grenouilles

```
# visit: frog_count
title: Le compte des grenouilles
place: 🐸 Au bord de l'étang des Mouilles
bonds: acquaintances friends bestfriends rivals

Le Fou est assis au bord de l'étang et compte les grenouilles à voix haute. « Trente et une, trente-deux… non. Trente et une. » Il se tourne vers {A} et {B}. « Elles bougent. Il me faudrait des yeux de plus. »

* count | Compter avec lui, sérieusement  [lean rationality +1] [lean honor +1]
  ? good
  {A} compte la rive de gauche sans en sauter une, le Fou celle de droite. Ils tombent sur trente-sept, et le Fou dit que c'est le bon chiffre, parce qu'hier aussi, c'était trente-sept.
  ? bad
  {A} compte la rive de gauche, sérieusement, et trouve trente-sept. Le Fou trouve trente-sept aussi, sur la rive de droite. Il faut tout recommencer.

* gather | Les rassembler pour les compter  [lean energy +2]
  ? good
  {A} entre dans l'eau et pousse doucement les grenouilles vers un seul nénuphar. Elles s'y serrent, vexées, et se laissent compter. Trente-quatre.
  ? bad
  {A} entre dans la boue des Mouilles pour les rassembler, et y reste planté{a:|e} jusqu'aux genoux. Les grenouilles viennent s'asseoir tout autour, pour regarder.

* guess | Annoncer un chiffre avec aplomb  [lean boldness +1] [lean rationality -1]
  ? good
  {A} annonce « quarante-deux » avec un tel aplomb que le Fou l'écrit. Il dit que c'est le premier compte juste de la semaine. Peut-être.
  ? bad
  {A} annonce un chiffre avec aplomb. Le Fou le regarde longtemps, très sérieux, puis dit : « Non. Ça, c'est le nombre de pierres du pré. » Il a l'air d'avoir raison.

> name | Leur donner des noms  [lean compassion +1] [lean sociability +1]
  ? good
  {B} donne un nom à chaque grenouille. Ça aide beaucoup : on ne compte pas deux fois Madeleine. Le Fou adopte la méthode.
  ? bad
  {B} donne un nom à chaque grenouille, puis n'arrive plus à les reconnaître. Il y a maintenant quatre Madeleine.

> doubt | Douter du compte  [lean rationality +2]
  ? good
  {B} doute du compte, à voix haute, et le Fou est ravi : d'habitude, on se contente de lui sourire. On recompte ensemble, et c'est le meilleur moment de sa journée.
  ? bad
  {B} doute du compte, et le Fou doute avec, puis du nombre d'étangs, puis du nombre de jours dans la semaine. On en est à huit.

> sing | Chanter pour les faire sortir  [lean boldness +1] [lean sociability +1]
  ? good
  {B} chante une chanson de la Maison commune, et les grenouilles répondent toutes ensemble. On les compte au bruit. Le Fou trouve ça très scientifique.
  ? bad
  {B} chante, et les grenouilles se taisent toutes, d'un coup. On n'en voit plus une seule. Le Fou note : « Zéro. Chanson. »
```

## 6. Les partenaires

```
# visit: card_partners
title: Les partenaires
place: 🃏 À l'auberge du Pont-Vieux
bonds: friends rivals enemies
hours: 17-1
stages: teen adult elder

Il manque deux joueurs à la table du coin, et la Courge a décidé : {A} et {B} joueront ensemble, contre le Noyer et le Fou. « Ensemble, mes choux. C'est ma table, c'est ma règle. » Le Noyer bat déjà les cartes, très sûr de lui.

* signs | Inventer des signes secrets  [lean honor -1] [lean rationality +1]
  ? good
  {A} invente des signes : un sourcil pour les cœurs, une toux pour les piques. Ça marche si bien que le Noyer demande à voir les manches de tout le monde.
  ? bad
  {A} invente des signes secrets, mais la fumée du feu fait tousser pour de bon. On joue pique toute la soirée.

* bold | Jouer gros dès la première main  [lean boldness +2]
  ? good
  {A} joue gros dès la première main, et gagne. Le Noyer parle de lui à la troisième personne pour se consoler.
  ? bad
  {A} joue gros dès la première main et perd tout ce qu'il y avait à perdre, c'est-à-dire trois noisettes. La Courge les note sur l'ardoise.

* fair | Jouer franc jeu, carte après carte  [lean honor +2]
  ? good
  {A} joue franc jeu, carte après carte. {Ils} perdent la première partie, gagnent la deuxième, et le Fou déclare que c'est l'égalité la plus juste qu'il ait jamais vue.
  ? bad
  {A} joue franc jeu, et le dit à chaque carte : « Je n'ai pas de cœur. » Le Noyer, qui écoute, gagne toutes les mains.

> follow | Suivre sans discuter  [lean honor +1] [lean vengefulness -1]
  ? good
  {B} suit sans discuter, et pour une soirée, {ils} jouent comme une seule main. La Courge raconte déjà à la table d'à côté qu'on ne peut plus les séparer.
  ? bad
  {B} suit sans discuter, même quand il aurait fallu discuter. Le Fou gagne, et s'excuse auprès de chaque carte.

> argue | Discuter chaque coup  [lean boldness +1] [lean vengefulness +1]
  ? good
  {B} discute chaque coup, à voix basse puis à voix haute. Le Noyer et le Fou, fascinés, oublient de jouer. La partie est gagnée par forfait.
  ? bad
  {B} discute chaque coup, si fort que la Courge confisque les cartes et sert la soupe. La partie reprendra demain. Elle ne reprendra pas.

> bluff | Bluffer avec aplomb  [lean honor -2]
  ? good
  {B} bluffe avec un aplomb parfait, sans un battement de cils. Le Noyer se couche, puis demande à voir. Il n'y avait rien. Il rit plus fort que tout le monde.
  ? bad
  {B} bluffe avec aplomb, mais la main qui tient les cartes tremble un peu. Le Fou le voit, le dit tout haut, et compte les tremblements.
```

## 7. Le salut à la Souche

```
# visit: greet_souche
title: Le salut à la Souche
place: 🍄 À la Lisière, devant la Souche
bonds: acquaintances friends bestfriends lovers

Le chemin passe devant la Souche, et la coutume veut qu'on la salue. Personne ne sait plus très bien comment. À quelques pas, la Mère Lichen est assise sur sa pierre, si immobile qu'on la prend pour une autre pierre.

* bow | Saluer bien bas  [lean zeal +2]
  ? good
  {A} salue bien bas, la tête jusqu'à l'herbe. La Mère Lichen fait un tout petit signe de tête. Pour elle, c'est un grand discours.
  ? bad
  {A} salue si bas qu'il faut ensuite un bon moment pour se relever. La Mère Lichen regarde faire, sans aider. Elle dit que la Souche n'est pas pressée.

* sit | S'asseoir à côté de la Mère Lichen  [lean energy -1] [lean compassion +1]
  ? good
  {A} s'assoit à côté de la Mère Lichen, sans rien demander. Elle ne dit rien. Au bout d'un long moment, elle pose une main sur la sienne.
  ? bad
  {A} s'assoit à côté de la Mère Lichen, puis pose une question. Puis une autre. Elle répond « pas encore » aux deux, et sort sa tisane.

* touch | Poser la main sur l'écorce  [lean boldness +1] [lean energy +1]
  ? good
  {A} pose la main sur l'écorce. Elle est tiède, comme une joue. Sous la mousse, un fil blanc de la Trame luit un instant, puis plus rien.
  ? bad
  {A} pose la main sur l'écorce et la retire aussitôt : une fourmi. Puis deux. La Souche a des habitants, et eux sont pressés.

> copy | Faire exactement pareil  [lean sociability +1] [lean zeal +1]
  ? good
  {B} fait exactement pareil, geste pour geste, et la Mère Lichen dit : « Tous les deux. Bien. » Elle n'avait pas dit autant de mots depuis l'hiver.
  ? bad
  {B} fait exactement pareil, mais un temps trop tard, comme un écho. La Mère Lichen ferme les yeux. On ne sait pas si elle rit.

> why | Demander pourquoi on la salue  [lean rationality +2]
  ? good
  {B} demande pourquoi on salue la Souche. La Mère Lichen réfléchit très longtemps, puis répond : « Parce qu'elle est là. » {B} trouve que c'est la meilleure réponse possible.
  ? bad
  {B} demande pourquoi on salue la Souche. La Mère Lichen répond « pas encore », ce qui ne répond à rien, et pour toujours.

> pebble | Laisser un petit galet  [lean compassion +1] [lean honor +1]
  ? good
  {B} laisse un petit galet au pied de la Souche. La Mère Lichen le ramasse, le regarde, et le met dans sa poche. Pour son tas, sans doute.
  ? bad
  {B} cherche un petit galet et n'en trouve que des gros. {B} en pose un, énorme. La Mère Lichen regarde le galet, puis {B}, et ne dit rien du tout.
```

## 8. La règle qui manque (ado)

```
# visit: missing_rule
title: La règle qui manque
place: 🏠 À la Maison des Pousses
bonds: acquaintances friends bestfriends rivals
stages: teen

Les règles de la Bruyère sont affichées dans l'entrée, numérotées de un à quarante. {A} et {B} remarquent en même temps qu'il n'y a pas de règle vingt-trois. La Bruyère est au potager. Elle revient dans un quart d'heure.

* write | Écrire une règle vingt-trois  [lean boldness +2]
  ? good
  {A} écrit à la craie : « Règle 23 : le goûter peut être pris deux fois. » La Bruyère la lit en rentrant, ne dit rien, et la laisse. Le goûter est pris deux fois.
  ? bad
  {A} écrit une règle vingt-trois. La Bruyère la lit en rentrant, dit le nom complet de {A} à voix haute, et ajoute une règle quarante et un. Elle parle des règles vingt-trois.

* tell | Prévenir la Bruyère  [lean honor +2]
  ? good
  {A} va prévenir la Bruyère au potager. Elle fronce les sourcils, puis dit : « Mange. » Un gâteau apparaît sur la table du goûter.
  ? bad
  {A} va prévenir la Bruyère, qui le savait déjà, et depuis toujours. Elle tend un panier de haricots à équeuter, pour l'avoir dérangée.

* search | Chercher la règle perdue  [lean energy +1] [lean rationality +1]
  ? good
  {A} trouve la règle vingt-trois glissée dans l'armoire de l'entrée : « On n'ouvre pas l'armoire. » Dans l'armoire, il y a une pile d'écharpes. {Ils} n'en parleront à personne.
  ? bad
  {A} cherche la règle perdue partout, et renverse la boîte à boutons de la Bruyère. Il y en a des centaines. {Ils} les ramassent jusqu'au soir.

> secret | Garder le secret  [lean sociability -1] [lean compassion +1]
  ? good
  {B} promet de garder le secret, et le garde, ce qui est rare dans cette maison. Les autres pousses ne sauront jamais rien, et c'est ce qui rend la chose si bonne.
  ? bad
  {B} promet de garder le secret, et le dit à une pousse, qui le dit à une autre. Au dîner, toute la Maison le sait, sauf la Bruyère. Enfin, c'est ce qu'on croit.

> dare | En rajouter une  [lean boldness +1] [lean energy +1]
  ? good
  {B} en rajoute une, à la craie, juste en dessous : « Règle 23 bis : la Bruyère a toujours raison. » La Bruyère la lit et file au potager, très vite, pour qu'on ne la voie pas sourire.
  ? bad
  {B} en rajoute une, puis une autre, et bientôt il n'y a plus de place sur le mur de l'entrée. La Bruyère rentre. Elle ne dit rien. Elle sort la brosse et le seau.

> giggle | Rire sous cape  [lean sociability +1] [lean honor -1]
  ? good
  {B} rit sous cape, et le fou rire gagne toute la Maison, une pousse après l'autre, sans que personne sache pourquoi. Même la Bruyère, au potager, s'arrête un instant.
  ? bad
  {B} rit sous cape, puis plus du tout sous cape. La Bruyère passe la tête par la fenêtre, et le rire s'arrête net, partout à la fois.
```

## 9. La tisane de la Mère Lichen (bébé)

```
# visit: lichen_tisane
title: La tisane de la Mère Lichen
place: 🪨 Chez la Mère Lichen
bonds: acquaintances friends bestfriends
stages: baby

La Mère Lichen a posé deux bols fumants devant la porte de sa cabane. Personne n'a demandé de tisane. Elle a décidé que c'était ce qu'il fallait à ces deux-là, et elle s'est rassise sur sa pierre pour regarder.

* drink | Boire d'un trait  [lean boldness +2]
  ? good
  {A} boit d'un trait, et c'est bon, étonnamment bon, comme l'herbe après la pluie. La Mère Lichen hoche la tête. Elle le savait.
  ? bad
  {A} boit d'un trait, et fait une grimace si longue que la Mère Lichen a le temps de finir son propre bol en regardant.

* blow | Souffler dessus, longtemps  [lean rationality +1] [lean energy -1]
  ? good
  {A} souffle sur le bol, longtemps, en faisant des petites vagues. La tisane refroidit juste comme il faut. La Mère Lichen dit : « Patient. » C'est un compliment.
  ? bad
  {A} souffle si fort sur le bol qu'il en reste la moitié sur le seuil. La Mère Lichen en reverse sans un mot. Elle a toujours une marmite d'avance.

* pebbles | Aller voir le tas de galets  [lean energy +2]
  ? good
  {A} trottine jusqu'au tas de galets devant la porte, un par pousse, et en trouve un tout neuf, tout en haut. La Mère Lichen ne dit pas lequel est à qui. Mais elle regarde {A}.
  ? bad
  {A} trottine jusqu'au tas de galets et en fait tomber trois. La Mère Lichen les remet un par un, chacun à sa place exacte, pendant que la tisane refroidit.

> same | Faire pareil  [lean sociability +1] [lean honor +1]
  ? good
  {B} fait pareil, en surveillant {A} du coin de l'œil. {Ils} finissent en même temps, posent les bols en même temps, et se trouvent très grand{p:s|es}.
  ? bad
  {B} fait pareil, mais à l'envers, et tout le monde finit mouillé. La Mère Lichen dit que la tisane, c'est aussi pour l'extérieur.

> hide | Se cacher derrière la Mère Lichen  [lean boldness -2]
  ? good
  {B} se cache derrière la Mère Lichen, passe la tête, puis tout le reste. Elle ne bouge pas d'un fil. On peut se cacher derrière elle aussi longtemps qu'on veut : c'est à ça qu'elle sert, dit-elle.
  ? bad
  {B} se cache derrière la Mère Lichen et s'endort contre la pierre tiède. La visite se finit sans réveil : la Mère Lichen dit qu'on ne réveille pas une pousse qui dort.

> share | Partager son bol  [lean compassion +2]
  ? good
  {B} tend son bol, pour partager. La Mère Lichen sourit, c'est-à-dire qu'une ride bouge. Elle reverse de la tisane dans les deux bols, à ras bord.
  ? bad
  {B} tend son bol pour partager et en renverse la moitié dans l'herbe. La Mère Lichen dit « pardon » à l'herbe, à la place de {B}.
```

## 10. La petite maison (adulte, ancien)

```
# visit: house_call
title: La petite maison
place: 🏡 Chez {B}
bonds: acquaintances friends bestfriends lovers
stages: adult elder

{B} a une petite maison au bout du hameau, avec une chaise qui boite et une marmite qui chante toujours un peu sur le feu. {A} frappe, et la porte s'ouvre toute seule. Elle fait ça, paraît-il.

* fix | Réparer la chaise qui boite  [lean energy +1] [lean compassion +1]
  ? good
  {A} cale la chaise avec un bout d'écorce plié en quatre. Elle ne boite plus, et ne grince même pas. C'est presque dommage.
  ? bad
  {A} répare la chaise qui boite. Maintenant, elle boite de l'autre côté.

* cook | Goûter ce qui mijote  [lean greed +1] [lean boldness +1]
  ? good
  {A} soulève le couvercle de la marmite et goûte. C'est une soupe aux noix, et elle a le goût d'un dimanche, même un mardi.
  ? bad
  {A} soulève le couvercle, goûte, et se brûle la langue. Pendant toute la visite, {A} parle la langue dehors, pour la refroidir. On ne comprend pas grand-chose.

* tidy | Ranger un peu, sans demander  [lean rationality +1] [lean sociability -1]
  ? good
  {A} range un peu, sans rien demander : les cuillères par taille, les bocaux par couleur. La maison a l'air d'avoir respiré.
  ? bad
  {A} range un peu, sans demander, et range si bien qu'on ne retrouve plus le sel. Ni la marmite. La marmite était sur le feu.

> jam | Sortir la bonne confiture  [lean greed -1] [lean sociability +1]
  ? good
  {B} sort la bonne confiture, celle des grandes occasions, et décide que c'en est une. Le pot est fini avant la fin de la visite.
  ? bad
  {B} sort la bonne confiture, celle des grandes occasions, et le couvercle refuse de s'ouvrir. {Ils} essaient chacun leur tour. Il est toujours fermé.

> sorry | S'excuser pour le désordre  [lean honor +1] [lean boldness -1]
  ? good
  {B} s'excuse pour le désordre, alors qu'il n'y en a pas. On en rit, puis on oublie de s'excuser du reste, et la visite dure jusqu'au soir.
  ? bad
  {B} s'excuse pour le désordre, puis pour la chaise, pour la marmite, pour le temps qu'il fait. {A} finit par s'excuser aussi, pour ne pas être en reste.

> tour | Faire visiter la maison  [lean sociability +1] [lean energy +1]
  ? good
  {B} fait visiter la maison. Il n'y a qu'une pièce, mais elle se visite dans le détail : le coin où le soleil arrive à midi, le clou qui ne tient rien, la fenêtre d'où l'on voit la Souche.
  ? bad
  {B} fait visiter la maison. Il n'y a qu'une pièce, et la visite dure quand même une heure. {A} connaît maintenant l'histoire de chaque clou.
```

## 11. La querelle du pré

```
# visit: meadow_quarrel
title: La querelle du pré
place: 🌾 Au pré de la Sommeille
bonds: acquaintances rivals enemies
hours: 6-21

Le Noyer est planté à un bout du pré, le Chardon à l'autre, et chacun jure que tout ce qui se trouve entre eux est à lui. En voyant arriver {A} et {B}, ils crient en même temps : « Vous, là ! Dites-lui à qui est ce pré ! »

* noyer | Donner raison au Noyer  [lean boldness +1] [lean greed +1]
  ? good
  {A} donne raison au Noyer, qui en parle de lui à la troisième personne pendant dix minutes. Le Chardon pèse l'argument, le trouve trop léger, et rit quand même, une fois.
  ? bad
  {A} donne raison au Noyer. Le Chardon grogne si fort que les canards du bief s'envolent. Il reprendra la querelle demain, et le Noyer aussi.

* chardon | Donner raison au Chardon  [lean honor +1] [lean rationality +1]
  ? good
  {A} donne raison au Chardon, preuves à l'appui : la rivière, la borne, le vieux saule. Le Chardon en devient presque aimable, et le Noyer applaudit le raisonnement, beau joueur.
  ? bad
  {A} donne raison au Chardon, qui en profite pour réclamer aussi le bief, le saule et le bout du chemin. Même le Chardon trouve qu'il exagère.

* fou | Aller chercher le Fou pour compter les bornes  [lean vengefulness -1] [lean energy +1]
  ? good
  {A} va chercher le Fou, qui compte les bornes : cinq. « C'est une de trop. » Le Noyer et le Chardon arrachent chacun la sienne, en même temps, et rentrent chez eux contents.
  ? bad
  {A} va chercher le Fou, qui compte les bornes et en trouve neuf. Personne n'en a planté neuf. Le Noyer et le Chardon s'accusent l'un l'autre, ravis.

> other | Prendre l'autre camp  [lean vengefulness +1] [lean boldness +1]
  ? good
  {B} prend aussitôt l'autre camp, par principe. Le pré a maintenant quatre avocats, et le Noyer et le Chardon s'assoient dans l'herbe pour écouter, enchantés. On se quitte bons perdants, de tous les côtés.
  ? bad
  {B} prend aussitôt l'autre camp, par principe, et {ils} se disputent le pré plus fort que ses deux propriétaires. Le Noyer et le Chardon finissent par les séparer.

> peace | Proposer de partager le pré  [lean compassion +1] [lean rationality +1]
  ? good
  {B} propose de partager le pré en deux, à la ficelle. Personne n'est d'accord, et tout le monde trouve que {B} a bien parlé. Maître Frêne, qui passait, prend des notes.
  ? bad
  {B} propose de partager le pré. Le Noyer et le Chardon, pour une fois d'accord, trouvent l'idée scandaleuse et le disent en même temps, très longtemps.

> sit | S'asseoir dans l'herbe et attendre  [lean energy -2]
  ? good
  {B} s'assoit dans l'herbe du pré, qui n'est à personne, et attend. La dispute s'épuise toute seule. Au bout d'un moment, on se retrouve à quatre dans l'herbe, et plus personne ne se rappelle de quoi on parlait.
  ? bad
  {B} s'assoit dans l'herbe et attend. La dispute, elle, n'attend pas : elle fait trois fois le tour du pré, et repasse à chaque fois au-dessus de la tête de {B}.
```

## 12. La chèvre commune

```
# visit: goat_follows
title: La chèvre commune
place: 🐐 Au Communal
bonds: acquaintances friends bestfriends lovers rivals enemies

La chèvre commune a décidé de suivre {B}. Partout. Elle ne mange rien, elle ne bêle pas : elle suit, à un pas exactement, et s'arrête quand {B} s'arrête.

* lure | L'attirer avec une poignée d'herbe  [lean rationality +1] [lean compassion +1]
  ? good
  {A} tend une poignée d'herbe tendre. La chèvre réfléchit, longtemps, puis choisit de suivre {A} à la place. {B} est libre. {A}, beaucoup moins.
  ? bad
  {A} tend une poignée d'herbe. La chèvre la mange, puis mange la manche de {A}, puis se remet à suivre {B}, à un pas exactement.

* lead | La ramener à son enclos  [lean energy +1] [lean honor +1]
  ? good
  {A} ramène la chèvre à son enclos, en lui parlant tout du long comme à une personne raisonnable. Elle se laisse faire. Elle a eu sa journée.
  ? bad
  {A} ramène la chèvre à son enclos. Le temps de refermer la barrière, elle est de nouveau derrière {B}. Personne ne l'a vue sortir.

* walk | Faire une promenade à trois  [lean sociability +2]
  ? good
  {A} décide qu'on se promènera à trois, voilà tout. La chèvre marche au milieu, très digne, et tout le Communal salue le cortège.
  ? bad
  {A} décide qu'on se promènera à trois. La chèvre choisit le chemin, et le chemin passe par les Mouilles. Tout le monde rentre crotté jusqu'aux genoux, sauf la chèvre.

> run | Essayer de la semer  [lean energy +2]
  ? good
  {B} part en courant, fait trois fois le tour du Communal et s'arrête, essoufflé{b:|e}. La chèvre est là, à un pas exactement, pas essoufflée du tout. On en rit jusqu'au soir.
  ? bad
  {B} essaie de la semer en courant, et la chèvre trouve ce jeu formidable. Elle bêle pour la première fois de la journée, et on l'entend jusqu'à la place.

> sign | Décider que c'est un signe  [lean zeal +2]
  ? good
  {B} décide que c'est un signe, et un bon. La chèvre a l'air d'accord. Elle porte bonheur pour le reste de la visite, ou du moins, elle n'en porte pas de mauvais.
  ? bad
  {B} décide que c'est un signe, et passe la visite à se demander de quoi. La chèvre aussi, à en juger par sa tête.

> ask | Lui demander poliment ce qu'elle veut  [lean honor +1] [lean sociability +1]
  ? good
  {B} se retourne et demande poliment à la chèvre ce qu'elle veut. Elle ne répond pas, mais s'en va, satisfaite. Elle voulait qu'on lui demande.
  ? bad
  {B} se retourne et demande poliment à la chèvre ce qu'elle veut. Elle veut l'écharpe de {B}. Elle l'a.
```

## 13. Les canards du bief

```
# visit: secret_ducks
title: Les canards du bief
place: 🦆 Au bief du moulin
bonds: acquaintances friends bestfriends
hours: 6-20

Derrière le moulin, {A} et {B} surprennent le Chardon accroupi au bord du bief, les poches pleines de grain, entouré de canards. Il se relève d'un coup. « Je ne les nourris pas. Je compte le grain. Dehors. Avec des canards. »

* agree | Le croire, très sérieusement  [lean compassion +1] [lean rationality -1]
  ? good
  {A} hoche la tête avec beaucoup de sérieux et demande combien de grains il reste à compter. Le Chardon, soulagé, en tend une poignée. Les canards approuvent.
  ? bad
  {A} hoche la tête avec sérieux, puis éclate de rire malgré tout. Le Chardon rentre au moulin en grognant. Les canards le suivent jusqu'à la porte.

* count | Compter les canards  [lean rationality +2]
  ? good
  {A} compte les canards : neuf. Le Chardon corrige : neuf et demi, il y en a un petit. Il les connaît tous par leur nom, et il nie aussi ça.
  ? bad
  {A} compte les canards à voix haute, si fort qu'ils s'envolent tous vers le pré. Le Chardon soupire. « Ils reviendront. Pas pour moi. Pour le grain. »

* names | Demander comment ils s'appellent  [lean boldness +1] [lean sociability +1]
  ? good
  {A} demande comment s'appellent les canards. Le Chardon dit qu'ils n'ont pas de nom, puis appelle « Farine ! », et un canard accourt. Il rit fort, une fois, et s'en excuse presque.
  ? bad
  {A} demande comment s'appellent les canards. Le Chardon devient mauve jusqu'à la couronne et répond qu'il ne connaît pas ces canards. Un canard lui tire la manche.

> keep | Promettre de ne rien dire  [lean honor +2]
  ? good
  {B} promet de ne rien dire à personne. Le Chardon pèse la promesse, la trouve d'un bon poids, et donne à chacun un petit sac de farine fine, « en trop ».
  ? bad
  {B} promet de ne rien dire, juste au moment où la Courge passe sur le chemin. Elle a tout vu. À midi, toute la place saura.

> feed | Nourrir les canards aussi  [lean compassion +1] [lean energy +1]
  ? good
  {B} prend une poignée de grain et nourrit les canards aussi. Le Chardon fait semblant de ne pas voir, et en tend une deuxième poignée, sans regarder.
  ? bad
  {B} nourrit les canards aussi, et les canards se mettent à suivre {B} jusqu'au bout du chemin. Le Chardon a l'air jaloux. Il le nie.

> praise | Complimenter sa farine  [lean sociability +1] [lean honor -1]
  ? good
  {B} dit que la farine du Chardon est la meilleure de la vallée, et que les canards en sont la preuve. Le Chardon pèse le compliment, longtemps, et le garde.
  ? bad
  {B} complimente sa farine, mais un peu vite. Le Chardon pèse le compliment et le trouve léger. « Repasse quand tu le penseras. »
```

## 14. L'eau froide

```
# visit: river_dare
title: L'eau froide
place: 💦 À la baignade de la Sommeille
bonds: friends bestfriends lovers rivals
hours: 10-20

L'eau de la Sommeille est si claire qu'on voit les galets du fond, et si froide que même les poissons ont l'air pressés. {A} et {B} sont au bord, les pieds au sec. Quelqu'un doit y aller le premier.

* jump | Sauter d'un coup  [lean boldness +2]
  ? good
  {A} saute d'un coup, ressort aussitôt, et crie que l'eau est délicieuse. Elle ne l'est pas. Mais c'est dit avec tant de cœur qu'on y croirait.
  ? bad
  {A} saute d'un coup et ressort aussitôt, bleu{a:|e} de froid, en jurant que ce n'est pas si froid. Personne n'y croit, pas même {A}.

* toe | Tremper le bout du pied d'abord  [lean rationality +1] [lean boldness -1]
  ? good
  {A} trempe le bout du pied, puis la cheville, puis tout le reste, très lentement. Ça prend une demi-heure, mais une fois dans l'eau, {A} y reste plus longtemps que tout le monde.
  ? bad
  {A} trempe le bout du pied, le ressort, le retrempe, le ressort. Au bout d'une heure, le pied est très propre, et c'est tout.

* stone | Plonger chercher un galet au fond  [lean energy +1] [lean greed +1]
  ? good
  {A} plonge chercher le plus beau galet du fond et remonte avec un galet blanc, rond comme une lune. On se le passe de main en main, sans rien dire.
  ? bad
  {A} plonge chercher le plus beau galet du fond et remonte avec une vieille sandale. De loin, le passeur crie que c'est la sienne, perdue au printemps.

> follow | Sauter derrière  [lean boldness +1] [lean sociability +1]
  ? good
  {B} saute derrière, sans réfléchir, et d'un coup l'eau est beaucoup moins froide. {Ils} nagent jusqu'au Pont-Vieux et reviennent en riant.
  ? bad
  {B} saute derrière, sans réfléchir, et en ressort plus vite encore. {Ils} passent la fin de la visite enroulé{p:s|es} dans la même couverture, à claquer des dents.

> judge | Noter le plongeon, très sévèrement  [lean honor +1] [lean compassion -1]
  ? good
  {B} reste au bord et note, très sévèrement, sur dix. Sept. Puis huit, après réflexion. Puis neuf, pour le courage. {B} finit par se jeter à l'eau pour remettre le dix en main propre.
  ? bad
  {B} reste au bord et note, très sévèrement : quatre sur dix. Puis {B} glisse en se levant, et tombe à l'eau avec une note bien pire.

> fire | Préparer un feu sur la rive  [lean compassion +1] [lean rationality +1]
  ? good
  {B} ramasse du bois flotté et allume un petit feu sur la rive. À la sortie de l'eau, il y a de la chaleur, des noisettes grillées, et un bon silence.
  ? bad
  {B} prépare un feu sur la rive, mais le bois flotté est encore mouillé. Ça fait beaucoup de fumée et très peu de feu. {Ils} sèchent quand même, un peu fumé{p:s|es}.
```

## 15. Les lumières de la Lisière

```
# visit: rain_lights
title: Les lumières de la Lisière
place: 🌧️ À la Lisière, une nuit de pluie
bonds: friends bestfriends lovers
hours: 20-1

Il bruine. Le Fou est assis sous une grande fougère, à la Lisière, et guette le sol depuis le début de la soirée. « C'est une nuit à lumières, dit-il. Je le sens dans les genoux. » Il fait une place à côté de lui. Elle est mouillée.

* watch | Guetter avec lui, sans bouger  [lean zeal +1] [lean energy -1]
  ? good
  {A} s'assoit et guette sans bouger. Vers minuit, sous la mousse, un fil pâle s'allume, puis un autre. Le Fou ne dit rien. Il serre juste très fort le bras de {A}.
  ? bad
  {A} guette sans bouger, si longtemps et si bien que le sommeil finit par gagner. Le Fou dira que les lumières sont venues juste à ce moment-là. Il a l'air sincère.

* lantern | Apporter une lanterne  [lean rationality +2]
  ? good
  {A} apporte une lanterne, pour mieux voir. Le Fou la trouve très belle et demande qu'on l'éteigne : les lumières sont timides. Dans le noir, après, on voit mieux qu'avant.
  ? bad
  {A} apporte une lanterne, pour mieux voir, et ne voit plus que la lanterne. Le Fou soupire : les lumières n'aiment pas la concurrence.

* story | Lui demander de raconter la nuit des lumières  [lean sociability +1] [lean compassion +1]
  ? good
  {A} demande au Fou de raconter la nuit des lumières, et il la raconte, avec trois détails nouveaux, dont un qui fait frissonner. La pluie s'arrête pour écouter.
  ? bad
  {A} demande au Fou de raconter la nuit des lumières, et il la raconte. Puis il la recommence, parce qu'il a oublié un détail au début. Le détail, c'était la pluie.

> close | Se serrer pour avoir chaud  [lean sociability +1] [lean compassion +1]
  ? good
  {B} se serre contre {A} pour avoir chaud. Le Fou fait semblant de regarder le sol avec beaucoup d'attention. Il sourit au sol.
  ? bad
  {B} se serre contre {A} pour avoir chaud, et une grosse goutte tombe de la fougère, pile entre les deux. Puis une autre. La fougère vise.

> doubt | Douter à voix haute  [lean rationality +1] [lean zeal -1]
  ? good
  {B} doute à voix haute, gentiment. Le Fou est ravi d'avoir un sceptique : il explique, il dessine dans la boue, il démontre. À la fin, {B} doute un peu moins, le Fou un peu plus, et c'est très bien.
  ? bad
  {B} doute à voix haute, et le Fou, vexé, se met à douter de tout le reste : de la pluie, de la nuit, de ses propres genoux. Il faut le consoler avec une noisette.

> wish | Faire un vœu  [lean zeal +2]
  ? good
  {B} fait un vœu, tout bas, et ne le dit à personne. Le Fou affirme qu'un vœu fait une nuit à lumières compte double. Il en fait un aussi, pour {B}.
  ? bad
  {B} fait un vœu, tout haut, sans faire exprès. Le Fou dit que ça ne marche plus, maintenant, et qu'il faudra attendre la prochaine nuit de pluie. Il pleut toujours. Il réfléchit.
```

## 16. La table de la Courge

```
# visit: courge_table
title: La table de la Courge
place: 🍲 À l'auberge, à la table de la Courge
bonds: acquaintances friends lovers enemies
hours: 11-22

La Courge les installe à la même table, la seule libre, et pose deux bols de soupe devant eux sans rien demander. « Alors, mes choux, comment vous vous êtes connus ? Non, attendez, laissez-moi deviner. » Elle ne devine pas. Elle pose une autre question.

* tell | Raconter leur histoire, toute simple  [lean honor +1] [lean sociability +1]
  ? good
  {A} raconte leur histoire, toute simple, en trois phrases. La Courge écoute jusqu'au bout, ce qui n'arrive jamais. Puis elle la raconte à la table voisine, embellie, et c'est encore mieux.
  ? bad
  {A} commence à raconter leur histoire. La Courge finit la phrase, puis l'histoire, puis en raconte une autre, qui n'est pas du tout la leur.

* invent | Inventer une rencontre fabuleuse  [lean honor -1] [lean boldness +1]
  ? good
  {A} invente une rencontre fabuleuse : une tempête, un pont qui tangue, une chèvre héroïque. La Courge applaudit. Demain, tout le hameau connaîtra la chèvre.
  ? bad
  {A} invente une rencontre fabuleuse, et la Courge y croit, entièrement. Elle va la répéter partout. Il va falloir s'en souvenir, maintenant, et dans le même ordre.

* eat | Manger la soupe sans rien dire  [lean sociability -1] [lean greed +1]
  ? good
  {A} mange la soupe sans un mot, et en redemande. La Courge, ravie, oublie sa question : elle juge un Plynling à son appétit, et {A} vient d'être très bien jugé{a:|e}.
  ? bad
  {A} mange la soupe sans rien dire. La Courge y voit un secret, et un secret, à l'auberge, ne dure pas jusqu'au soir. Elle en invente un.

> deny | Tout démentir, point par point  [lean honor +1] [lean sociability -1]
  ? good
  {B} dément tout, poliment, point par point. La Courge adore : un démenti, c'est encore une histoire. Elle sert une deuxième part de tarte pour entendre la suite.
  ? bad
  {B} dément tout, et la Courge hoche la tête avec un clin d'œil énorme, de ceux qu'on voit depuis la porte. Personne ne croira le démenti.

> join | En rajouter  [lean sociability +2]
  ? good
  {B} en rajoute, de bon cœur, et l'histoire grandit entre {A}, {B} et la Courge jusqu'à prendre toute la soirée. La table voisine tire sa chaise pour écouter.
  ? bad
  {B} en rajoute, la Courge aussi, et bientôt l'histoire a un sanglier grand comme la Maison commune. Il n'y en a pas, à Humeval. Il y en a un, maintenant, dans l'histoire.

> change | Changer de sujet  [lean rationality +1] [lean boldness -1]
  ? good
  {B} change de sujet et demande à la Courge comment va son secret, le seul qu'elle garde. Elle rougit, toutes vrilles dehors, et pour une fois ne pose plus de question. Elle sert la tarte.
  ? bad
  {B} change de sujet, et la Courge suit avec bonheur : elle a une question sur chaque sujet. On parle de la pluie, du four, de la chèvre, et on revient à la première question.
```

## 17. Le panier oublié

```
# visit: forgotten_nuts
title: Le panier oublié
place: 🌰 Sur le chemin du Noyer
bonds: rivals enemies

Au bord du chemin, il y a un panier de noix « oublié ». Tout le monde sait que le Noyer l'oublie exprès, et tout le monde fait semblant de ne pas le savoir. {A} et {B} arrivent devant en même temps. Il n'y a qu'un panier.

* half | Partager à la noix près  [lean honor +2]
  ? good
  {A} compte les noix et fait deux tas parfaitement égaux. Il en reste une. {A} la pose sur le muret, pour le Noyer. C'est le geste le plus juste de la semaine.
  ? bad
  {A} compte les noix pour partager à la noix près. Il en reste une. La dernière noix occupe tout le reste de la visite.

* grab | Prendre le panier et filer  [lean greed +2]
  ? good
  {A} prend le panier et file, puis s'arrête au tournant et attend, en riant. On finit les noix ensemble, assis{p:|es} sur le muret, à se disputer les plus grosses.
  ? bad
  {A} prend le panier et file, droit sur le Noyer, qui revenait le chercher. « Ah ! Le Noyer l'avait oublié ! » Il reprend le panier, ravi, et donne une noix à chacun. Une.

* proud | Ne pas y toucher, par fierté  [lean greed -2]
  ? good
  {A} ne touche pas au panier, par fierté, et c'est si digne que la colère s'en va toute seule. Ce soir, Jacquet viendra prendre les noix, comme chaque soir, et tout sera à sa place.
  ? bad
  {A} ne touche pas au panier, par fierté, et attend qu'on en fasse autant en face. On reste là, longtemps, à ne pas toucher au panier. Un écureuil passe et prend la plus grosse.

> crack | Casser une noix, l'air de rien  [lean boldness +1] [lean vengefulness -1]
  ? good
  {B} casse une noix, l'air de rien, et en tend la moitié. On ne se dit rien. On mange chacun sa moitié. C'est une trêve, et elle a bon goût.
  ? bad
  {B} casse une noix, l'air de rien, et elle est vide. Les suivantes aussi. Le Noyer, qui guettait derrière la haie, a l'air très gêné.

> contest | En faire un concours  [lean energy +1] [lean vengefulness +1]
  ? good
  {B} propose un concours : qui casse le plus de noix sans outil. Personne ne gagne, ce qui arrange tout le monde. Le chemin est couvert de coquilles.
  ? bad
  {B} propose un concours, et le concours tourne à la dispute, puis à la bataille de coquilles. Jacquet, caché dans la haie, compte les points.

> report | Aller le dire au Noyer  [lean honor +1] [lean vengefulness +1]
  ? good
  {B} va dire au Noyer qu'il a oublié son panier. Le Noyer, enchanté qu'on lui en parle, l'offre aux deux, et racontera pendant un mois qu'il l'a offert.
  ? bad
  {B} va dire au Noyer qu'il a oublié son panier. Le Noyer est très vexé : on n'était pas censé le savoir. Il repart avec le panier, et revient l'oublier un peu plus loin.
```

## 18. Le remède de la Sauge

```
# visit: grimace_contest
title: Le remède de la Sauge
place: 🌿 À la Maison aux Simples
bonds: friends bestfriends rivals enemies
hours: 8-19

La Sauge a un nouveau remède contre le rhume, et il lui faut deux goûteurs. Elle remplit deux petits verres d'un liquide vert foncé. « Il ne soigne rien si on ne le boit pas. Et ne faites pas cette tête. » Tout le monde fait cette tête.

* gulp | Le boire d'un trait  [lean boldness +2]
  ? good
  {A} boit d'un trait, sans une grimace, ce que la Sauge n'a jamais vu. Elle note quelque chose dans son carnet. C'est illisible, mais ça a l'air élogieux.
  ? bad
  {A} boit d'un trait, et fait la grimace la plus longue de l'histoire de la Maison aux Simples. La Sauge la note dans son carnet, avec la date.

* honey | Demander du miel du Tilleul  [lean rationality +1] [lean sociability +1]
  ? good
  {A} demande s'il y aurait du miel du Tilleul. Il y en a, il y en a toujours. Le remède devient presque bon, et la Sauge, vexée, en reprend pour vérifier.
  ? bad
  {A} demande du miel. La Sauge répond qu'un remède avec du miel, c'est de la confiture, et double la dose. Pour la science.

* sniff | Le sentir d'abord, prudemment  [lean boldness -2]
  ? good
  {A} sent le verre, longuement, et déclare que ça sent l'herbe après la pluie. La Sauge rosit : c'était exactement l'idée. Elle permet de n'en boire que la moitié.
  ? bad
  {A} sent le verre et éternue. Le rhume est guéri d'avance, dit la Sauge, alors autant boire quand même.

> faster | Boire plus vite, pour gagner  [lean boldness +1] [lean energy +1]
  ? good
  {B} boit plus vite, pour gagner, et gagne. La Sauge déclare un vainqueur, ce qu'elle ne fait jamais, et lui donne un pot de baume « en trop ».
  ? bad
  {B} boit plus vite, pour gagner, et avale de travers. Il faut taper dans le dos, longtemps. La Sauge prend des notes tout le temps que ça dure.

> face | Faire la grimace exprès  [lean sociability +1] [lean rationality -1]
  ? good
  {B} fait la grimace exprès, la plus belle possible, et le concours de grimaces commence, celui qui ne dit pas son nom. Au bout d'un moment, la Sauge en fait une aussi. Elle gagne.
  ? bad
  {B} fait la grimace exprès, puis le remède agit, et ce n'est plus exprès du tout. La grimace reste un bon moment.

> thank | Remercier, poliment  [lean honor +1] [lean compassion +1]
  ? good
  {B} remercie poliment, comme pour une tisane d'auberge. La Sauge n'a pas l'habitude. Elle ne sait pas quoi répondre, et sert un deuxième verre, de bon cœur.
  ? bad
  {B} remercie poliment, et la Sauge, touchée, ressert un deuxième verre. Puis un troisième. Il faut s'enfuir.
```

## 19. L'averse

```
# visit: rain_shelter
title: L'averse
place: ☔ Sous l'auvent de la Maison commune
bonds: acquaintances friends bestfriends lovers rivals enemies

Une averse tombe d'un coup sur la place du Puits. {A} et {B} se retrouvent sous l'auvent de la Maison commune, qui n'a été bâti que pour une personne et demie. La pluie n'a pas l'air pressée.

* room | Se serrer pour faire de la place  [lean compassion +1] [lean sociability +1]
  ? good
  {A} se serre contre le mur pour laisser de la place. Ça suffit : l'auvent tient deux, finalement, si on ne respire pas trop fort.
  ? bad
  {A} se serre contre le mur pour laisser de la place, et la gouttière, juste au-dessus, choisit cet instant pour déborder.

* dash | Courir jusqu'à l'auberge  [lean boldness +1] [lean energy +1]
  ? good
  {A} lance : « À l'auberge ! », et on court. On arrive comme si on avait traversé la Sommeille, mais il y a le feu, la Courge, et du lait chaud au miel.
  ? bad
  {A} part en courant vers l'auberge, glisse dans la première flaque, et revient sous l'auvent, beaucoup plus mouillé{a:|e} qu'avant.

* drops | Compter les gouttes  [lean rationality +1] [lean energy -1]
  ? good
  {A} se met à compter les gouttes qui tombent de l'auvent. À cent, on rit. À deux cents, on parie sur la suivante. La pluie s'arrête à trois cent douze, et c'est presque dommage.
  ? bad
  {A} compte les gouttes, à voix haute. À quatre cents, la pluie redouble. {A} recommence à zéro, par honnêteté.

> scarf | Prêter son écharpe  [lean compassion +2]
  ? good
  {B} prête son écharpe, sans rien dire. Elle est sèche, chaude, et sent le feu de bois. On ne se dit pas grand-chose, mais on se le dit bien.
  ? bad
  {B} prête son écharpe, qui est déjà trempée. On la tord à deux. Il en sort un plein bol de pluie.

> sing | Chanter pour couvrir la pluie  [lean sociability +1] [lean boldness +1]
  ? good
  {B} chante pour couvrir le bruit de la pluie. Une fenêtre s'ouvre, puis une autre, et toute la place finit par chanter le refrain. La pluie, vexée, s'arrête.
  ? bad
  {B} chante pour couvrir la pluie, et la pluie redouble, comme pour couvrir la chanson. Ça devient une question d'honneur, des deux côtés.

> sulk | Bouder la pluie  [lean vengefulness +1] [lean sociability -1]
  ? good
  {B} boude la pluie, les bras croisés, avec tant de conviction que c'en est drôle. On se met à bouder à deux. La pluie finit par céder.
  ? bad
  {B} boude la pluie, puis l'auvent, puis la place entière. Tout le monde attend que ça passe. La pluie passe avant.
```

## 20. Le guet du gué (ado, adulte, ancien)

```
# visit: lookout
title: Le guet du gué
place: 🌾 Près du gué, à la tombée du jour
bonds: friends bestfriends lovers
hours: 17-22
stages: teen adult elder

Ce soir, Aude et Mathurin se voient au gué, et c'est au tour de {A} et {B} de faire le guet, comme tout le village le fait, chacun son tour. Le chemin est vide. Pour l'instant. Au loin, on entend la voix du Noyer.

* distract | Aller distraire le Noyer  [lean sociability +1] [lean boldness +1]
  ? good
  {A} va au-devant du Noyer et lui demande comment va sa grosse citrouille. Le Noyer parle de la citrouille pendant une heure, et ne regarde pas une seule fois vers le gué.
  ? bad
  {A} va distraire le Noyer et lui demande où il va. « Au gué, prendre le frais. » {A} invente un raccourci. Le raccourci passe par le gué.

* signal | Faire le signal convenu  [lean rationality +1] [lean honor +1]
  ? good
  {A} fait le signal convenu : deux cris de grenouille. Au gué, deux silhouettes se séparent sans bruit. Le Noyer passe, salue, et ne voit que des grenouilles. Ce sont de très bonnes grenouilles.
  ? bad
  {A} fait le signal convenu : deux cris de grenouille. Toutes les grenouilles de la Sommeille répondent. Plus personne ne sait qui a crié quoi, au gué non plus.

* hedge | Rester caché derrière la haie  [lean boldness -1] [lean energy -1]
  ? good
  {A} reste derrière la haie, sans un bruit. Le Noyer passe sans rien voir. Au gué, deux voix rient tout bas. On a bien guetté.
  ? bad
  {A} se cache derrière la haie, si bien caché{a:|e} que le Noyer s'y assoit pour renouer sa chaussure. Il y reste un moment. Il chantonne.

> giggle | Pouffer de rire  [lean sociability +1] [lean honor -1]
  ? good
  {B} pouffe de rire, tout bas, et le rire se transmet jusqu'au gué, où quelqu'un pouffe aussi. Le Noyer croit que c'est la rivière.
  ? bad
  {B} pouffe de rire, pas tout bas du tout. Le Noyer s'arrête, tend l'oreille, puis repart. Il a entendu. Il fera semblant que non, et c'est presque pire.

> serious | Prendre le guet très au sérieux  [lean honor +1] [lean zeal +1]
  ? good
  {B} prend le guet très au sérieux, l'œil sur le chemin, l'oreille sur la rivière. Rien ne passe. Le lendemain, un petit sac de farine fine attend devant la porte de {B}.
  ? bad
  {B} prend le guet très au sérieux et crie « Qui va là ? » à un canard. Le canard s'arrête. Le Noyer, au loin, demande qui est là.

> dream | Regarder le gué, rêveusement  [lean compassion +1] [lean rationality -1]
  ? good
  {B} regarde le gué, rêveusement, sans plus surveiller grand-chose. Ça ne fait rien : le chemin reste vide, la soirée est douce, et {A} est à côté.
  ? bad
  {B} regarde le gué si rêveusement qu'il faut un coup de coude pour revenir, et le coup de coude est un peu fort. Au gué, on a entendu « aïe ».
```

## ♥ L'aveu

Proposé à la place d'une scène quand la paire peut se déclarer (un garçon et une fille, personne d'autre
en couple, pas de refus depuis 3 jours) : le bouton « 💌 Lui avouer ses sentiments » rejoint les
options de la scène tirée, et y bascule. Les demi-fins sont `? yes` (accepté) et `? no` (refusé). Un
refus reste tendre : on n'est jamais triste en refermant une scène, même si le résultat dit le
chagrin.

```
# confession: confess
title: L'aveu
place: 🌉 Sur le Pont-Vieux

{A} n'est pas venu{a:|e} pour se promener. {A} est venu{a:|e} dire à {B} quelque chose qui ne tient plus en place, et ça se voit de loin. Au milieu du Pont-Vieux, {A} pose la main sur la pierre, comme on le fait en passant, et ne la retire plus.

* straight | Le dire d'un seul trait  [lean boldness +2]
  ? yes
  {A} le dit d'un seul trait, sans respirer, et sans regarder ailleurs une seule fois.
  ? no
  {A} le dit d'un seul trait, sans respirer. Après, le silence est très long, et la Sommeille passe dessous sans se presser.

* flower | Offrir d'abord une fleur de la Lisière  [lean compassion +1] [lean sociability +1]
  ? yes
  {A} tend une petite fleur cueillie à la Lisière, puis dit le reste. La fleur tremble un peu. {A} aussi.
  ? no
  {A} tend une petite fleur cueillie à la Lisière, puis dit le reste. La fleur tremble un peu, et finit par tomber dans la rivière.

* pebble | L'écrire sur un galet  [lean rationality +1] [lean sociability -1]
  ? yes
  {A} tend un galet plat. Dessus, à la craie, il y a tous les mots, écrits tout petit pour qu'ils tiennent.
  ? no
  {A} tend un galet plat, avec les mots écrits dessus à la craie. La craie a un peu bavé. Ça se lit quand même.

* bees | Le faire dire aux abeilles par le Tilleul  [lean sociability +2] [lean boldness -1]
  ? yes
  Ce matin, le Tilleul l'a crié à ses ruches de la part de {A}, et tout le chemin jusqu'au pont l'a entendu. {A} n'a plus qu'à hocher la tête. C'est le plus dur.
  ? no
  Ce matin, le Tilleul l'a crié à ses ruches de la part de {A}, un peu de travers. Il a fallu tout redire, sur le pont, en entier.

> answer | Répondre tout de suite  [lean boldness +1] [lean honor +1]
  ? yes
  {B} répond tout de suite, avant même la fin : oui. Puis encore oui, au cas où. La pierre du pont est tiède sous leurs deux mains.
  ? no
  {B} répond tout de suite, doucement : non. Pas comme ça, pas tout à fait. {B} reste sur le pont jusqu'à ce que {A} soit prêt{a:|e} à rentrer.

> blush | Rougir et regarder la rivière  [lean boldness -1] [lean compassion +1]
  ? yes
  {B} rougit et regarde la rivière très longtemps. Puis {B} pose la main à côté de celle de {A}, sur la pierre. Tout près. C'est un oui, et tout le Pont-Vieux le sait.
  ? no
  {B} rougit et regarde la rivière très longtemps. Puis {B} dit non, si doucement que c'est presque une excuse. La rivière, elle, continue de passer.

> laugh | Rire, d'un rire tout neuf  [lean sociability +1] [lean energy +1]
  ? yes
  {B} rit, d'un rire que personne ne lui connaissait, et dit oui au milieu du rire. Sur la rive, le passeur fait semblant de regarder ailleurs.
  ? no
  {B} rit, gentiment, d'un rire un peu gêné, et dit que non, mais que c'est la plus belle chose qu'on lui ait dite. On rentre ensemble, en parlant d'autre chose.
```

## Questions pour la relecture

- **Nombre d'options** : 3 options et 3 réactions par scène, 4 options pour l'aveu. On peut en varier
  quelques-unes (2 ou 4) si la carte paraît trop régulière.
- **Couverture plus large que « environ 4 » par lien** : la plupart des scènes servent plusieurs liens,
  ce qui donne 8 à 17 scènes possibles par lien au lieu de 4. Plus de variété, au prix d'une écriture
  plus neutre (les scènes mixtes ne supposent ni amitié ni rancune). À dire si tu préfères des scènes
  plus marquées par lien.
- **Le lieu de `house_call`** contient `{B}` (« 🏡 Chez {B} »), comme l'ancien lieu « Chez {B} ».
- **`lichen_tisane`** (bébé) et **`missing_rule`** (ado) ne servent qu'à leur étape : une paire bébé/ado
  ne les verra jamais, et c'est voulu.
