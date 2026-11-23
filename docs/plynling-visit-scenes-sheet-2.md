# Visites Plynling — fiche d'écriture des scènes (vague 2 : couples et ennemis)

Fiche de relecture, **approuvée telle quelle le 2026-11-23** et livrée en 8.2.8 : chaque bloc est recopié
tel quel dans `ProjectSYNCS/Stories/Visits/<clé>.visit`. Toute retouche d'un texte repasse par le
propriétaire. Questions de relecture laissées par défaut : visages des trêves inchangés, `pousse_register`
réservée aux bébés en couple.
Même format et mêmes conventions que la vague 1 (`docs/plynling-visit-scenes-sheet.md`, *Comment lire un
bloc*).

## Pourquoi cette vague

Les couples n'avaient que 9 scènes possibles et les ennemis 8, contre 17 pour les amis : ce sont eux qui
revoient le plus vite les mêmes. Et toutes ces scènes étaient partagées avec des liens voisins, donc
écrites neutres : aucune ne pouvait supposer qu'on s'aime, ni qu'on se déteste.

**Choix retenu (A)** : 8 scènes **réservées à un seul lien**, 4 pour les couples, 4 pour les ennemis.
Une scène de couple peut être franchement tendre ; une scène d'ennemis, franchement chamailleuse (à la
façon de Humeval : une bonne fin est une trêve d'un moment, une mauvaise une petite catastrophe drôle).
Écartés : des scènes partagées avec un lien voisin (plus de variété partout, mais neutres à nouveau),
ou une première vague de 4 seulement (plus lent).

Chaque scène comble aussi un autre trou :

- **la nuit** (1 h–5 h, où seules les scènes « toute la journée » tombaient) : une scène de nuit par lien ;
- **les étapes** : une scène réservée aux bébés, une aux anciens (aucune jusqu'ici) ;
- **les villageois absents des visites** : l'Étrangère, l'Ortie (et Guérin, Jacquet), l'Intendant,
  Maître Frêne (enfin sur scène), Dame Ermengarde et Renaud, Clément et Gaspard.

**Les couples, c'est à tout âge** : `CanConfess` n'exige que d'être meilleurs amis (un garçon et une
fille), donc deux bébés peuvent être en couple. Les scènes de couple ouvertes à toutes les étapes
restent innocentes : une main tenue, un rouge aux joues, rien de plus.

## La liste

| # | Clé | Lieu | Lien | Heures | Étapes | En une ligne |
|---|---|---|---|---|---|---|
| 1 | `dark_path` | 🏮 Sur le chemin du Communal, la nuit | couple | 21–6 | toutes | La lanterne s'éteint, et la cabane de l'Étrangère luit au bout du chemin |
| 2 | `daisy_mirror` | 🌼 Dans les hautes herbes du Communal | couple | 8–20 | toutes | Les jumelles du Communal imitent chacun de leurs gestes |
| 3 | `ortie_nap` | 🪑 Devant chez l'Ortie | couple | 9–20 | ancien | L'Ortie s'est endormie sur son banc, et ses enfants commencent à crier |
| 4 | `pousse_register` | 📜 Devant la cabane de la Mère Lichen | couple | 8–18 | bébé | L'Intendant inscrit les pousses, et ces deux-là ne se lâchent pas la main |
| 5 | `night_mill` | 🌙 Au moulin de la Sommeille, la nuit | ennemis | 22–6 | toutes | Le Chardon les fait veiller la meule ensemble : ils ne dormiront pas, à se surveiller |
| 6 | `council_vote` | 🗳️ À la Maison commune, un soir de conseil | ennemis | 17–23 | ado, adulte, ancien | Égalité sur la couleur de la porte, et Maître Frêne demande leur voix |
| 7 | `long_bench` | 🪵 Sur le chemin de la place du Puits | ennemis | 7–20 | toutes | Le grand banc du banquet se porte à deux, et les fils du Noyer parient |
| 8 | `guerin_spoons` | 🥄 À l'établi de Guérin | ennemis | 8–20 | toutes | Guérin a taillé leurs deux têtes en cuillères, très ressemblantes |

**Couverture après la vague** — scènes possibles par lien : connaissances 14, amis 17, meilleurs amis
14, **couple 13** (9 + 4), rivaux 11, **ennemis 12** (8 + 4). La nuit, un couple a maintenant
`dark_path` en plus des trois scènes « toute la journée », des ennemis `night_mill`. Les bébés en
couple ont une scène à eux, les anciens en couple aussi.

**Les astuces** (aucune déjà prise par la vague 1) : une lumière qui manque et une autre qui arrive ;
des enfants qui imitent ; garder des enfants sages ; un fonctionnaire qui prend tout au pied de la
lettre ; une veille où l'on se surveille ; voter contre par principe ; un fardeau qu'on porte à deux ;
une caricature trop ressemblante.

---

## 1. La lanterne éteinte

```
# visit: dark_path
title: La lanterne éteinte
place: 🏮 Sur le chemin du Communal, la nuit
bonds: lovers
hours: 21-6

{A} et {B} rentrent tard par le chemin du Communal, une lanterne entre eux deux. À mi-chemin, la flamme fait « pff » et s'éteint. Il fait noir comme dans une poche. Tout au bout du chemin, derrière une fenêtre, il y a une petite lueur verte : la cabane de l'Étrangère.

* knock | Aller frapper chez l'Étrangère  [lean sociability +2]
  ? good
  {A} frappe chez l'Étrangère, qui sort et rabat sa capuche, rien que pour eux. Elle marche devant jusqu'au bout du chemin, sans un mot, et luit juste assez.
  ? bad
  {A} frappe chez l'Étrangère, qui ouvre, voit qui se tient là, et rougit si fort, en vert, qu'on y voit comme en plein jour. Puis elle referme, très gênée, et il refait nuit.

* hand | Prendre la main et avancer à tâtons  [lean boldness +1] [lean compassion +1]
  ? good
  {A} prend la main de {B} et avance à tâtons. On marche lentement, on se cogne un peu, et on ne se lâche pas. Le chemin n'a jamais été aussi court.
  ? bad
  {A} prend la main de {B} et avance à tâtons, avec beaucoup d'assurance, droit dans la haie. La haie est piquante, et ne s'excuse pas.

* relight | Rallumer la lanterne avec les moyens du bord  [lean rationality +2]
  ? good
  {A} frotte deux silex, souffle, patiente. La flamme revient, toute petite, et on la protège à quatre mains jusqu'au bout du chemin, comme un oisillon tombé du nid.
  ? bad
  {A} frotte deux silex, souffle, frotte encore. Au bout d'un moment, l'aube se lève, et règle la question.

> stars | Montrer les étoiles  [lean zeal +1] [lean energy -1]
  ? good
  {B} lève le nez : on voit toutes les étoiles, même les petites qui ne servent à rien. {B} les montre une par une, et donne à la plus petite le nom de {A}.
  ? bad
  {B} lève le nez pour montrer les étoiles, et ne regarde plus du tout où vont ses pieds. Il y a une flaque au milieu du chemin. Il y en a toujours une.

> brave | Faire semblant de ne pas avoir peur du noir  [lean boldness +1] [lean honor -1]
  ? good
  {B} jure n'avoir pas peur du noir, pas du tout, et le répète à chaque pas. Puis {B} serre le bras de {A}, juste pour rassurer {A}, bien sûr.
  ? bad
  {B} jure n'avoir pas peur du noir, et une feuille tombe. {B} fait un bond jusqu'au milieu du Communal. La chèvre commune, réveillée, n'est pas contente.

> hum | Fredonner pour se donner du courage  [lean sociability +1] [lean compassion +1]
  ? good
  {B} fredonne tout bas, et {A} reprend l'air. Le chemin du Communal a entendu beaucoup de chansons, mais pas celle-là : elle n'existe que ce soir.
  ? bad
  {B} fredonne pour se donner du courage, et une voix, quelque part dans le noir, reprend l'air. C'est le Fou, qui rentre de la Lisière. On a eu très peur, et maintenant on chante à trois.
```

## 2. Les jumelles

```
# visit: daisy_mirror
title: Les jumelles
place: 🌼 Dans les hautes herbes du Communal
bonds: lovers
hours: 8-20

Depuis le puits, {A} et {B} sont suivi{p:s|es} par deux petites têtes blanches qui dépassent des herbes : les jumelles du Communal. Quand {A} sourit à {B}, les jumelles se sourient. Quand {B} rougit, les jumelles rougissent aussi, très fort, exprès.

* apple | Leur promettre une pomme si elles filent  [lean rationality +1] [lean greed -1]
  ? good
  {A} promet une pomme à chacune si elles filent. Elles se consultent, acceptent, et filent vers le clos du Tilleul, où les pommes sont, de toute façon, à portée de main.
  ? bad
  {A} promet une pomme si elles filent. Les jumelles reviennent avec une pomme, et la croquent ensemble, chacune de son côté, en regardant {A} et {B} droit dans les yeux.

* stiff | Marcher dignement, comme si de rien n'était  [lean honor +1] [lean sociability -1]
  ? good
  {A} marche très dignement, les mains dans le dos, comme on va au conseil. Les jumelles imitent, les mains dans le dos, et trouvent ça si ennuyeux qu'elles abandonnent au bout de dix pas.
  ? bad
  {A} marche très dignement, comme si de rien n'était, et les jumelles aussi, en exagérant tant que la chèvre commune s'arrête de mâcher pour regarder.

* hold | Prendre la main, tant pis  [lean boldness +2]
  ? good
  {A} prend la main de {B}, tant pis, devant tout le monde. Les jumelles se prennent la main aussi, puis s'en vont en se balançant, comme si c'était leur idée depuis le début.
  ? bad
  {A} prend la main de {B}, tant pis. Les jumelles poussent un « oooh » si long qu'on l'entend jusqu'au moulin. Le Chardon sort voir ce qui se passe.

> chase | Leur courir après en riant  [lean energy +2]
  ? good
  {B} leur court après en riant, et les jumelles s'enfuient en piaillant dans les herbes. On ne les rattrape pas, on n'essaie pas vraiment. On revient essoufflé{p:s|es}, et plus du tout gêné{p:s|es}.
  ? bad
  {B} leur court après et se prend les pieds dans les herbes hautes. Les jumelles reviennent, s'assoient de chaque côté, et attendent poliment la suite.

> ignore | Faire semblant de ne pas les voir  [lean rationality +1] [lean energy -1]
  ? good
  {B} fait semblant de ne pas les voir, si bien qu'au bout d'un moment, les jumelles doutent d'exister. Elles vont vérifier auprès du Tilleul.
  ? bad
  {B} fait semblant de ne pas les voir, et les jumelles se plantent juste devant, nez à nez. Il devient très difficile de ne pas les voir.

> wink | Leur faire un clin d'œil complice  [lean sociability +1] [lean boldness +1]
  ? good
  {B} leur fait un clin d'œil complice. Les jumelles, flattées d'être dans le secret, montent la garde au bord des herbes et chassent tous les curieux, même le Noyer.
  ? bad
  {B} leur fait un clin d'œil complice. Les jumelles en rendent trois chacune, puis courent crier sur la place : « On est dans le secret ! » Personne ne sait lequel, mais tout le monde a une idée.
```

## 3. L'heure de l'Ortie (ancien)

```
# visit: ortie_nap
title: L'heure de l'Ortie
place: 🪑 Devant chez l'Ortie
bonds: lovers
hours: 9-20
stages: elder

{A} et {B} passent devant chez l'Ortie, bras dessus bras dessous, comme tous les jours. Sur le banc devant la porte, l'Ortie dort, assise, une cuillère de bois à la main. Autour d'elle, ses enfants commencent à crier. Il en manque un : toujours le même.

* story | Raconter une histoire d'autrefois aux petits  [lean sociability +1] [lean energy -1]
  ? good
  {A} rassemble les petits et raconte une histoire d'autrefois, du temps où {A} sortait à peine de la Souche. Pas un ne crie. Au fond, {B} écoute aussi, et connaît l'histoire par cœur : c'est la leur.
  ? bad
  {A} raconte une histoire d'autrefois, avec une tempête et un sanglier. Le plus petit pousse un cri de sanglier, très réussi, et les deux autres l'imitent, encore mieux.

* fetch | Aller chercher celui qui manque  [lean energy +1] [lean honor +1]
  ? good
  {A} va chercher celui qui manque et le trouve, comme toujours, dans le noyer du Noyer, les poches pleines. Jacquet redescend sans discuter : on ne discute pas avec un{a:|e} ancien{a:|ne}.
  ? bad
  {A} va chercher Jacquet dans le noyer du Noyer, et le Noyer se tient juste en dessous. On parle longtemps de noix, très poliment, pendant que Jacquet descend de l'autre côté.

* hush | Inventer le jeu du silence  [lean rationality +2]
  ? good
  {A} invente un jeu : le premier qui fait un bruit a perdu. Les petits deviennent muets comme des galets. Le plus petit tient si fort qu'il en devient tout rouge.
  ? bad
  {A} invente le jeu du silence, et les petits le prennent très au sérieux. Le premier qui fait un bruit, c'est {A}, en éternuant. Ils le crient tous en même temps.

> rock | Bercer le plus petit  [lean compassion +2]
  ? good
  {B} prend le plus petit sur ses genoux et le berce, et continue longtemps après qu'il s'est endormi, pour le plaisir. Ça faisait longtemps que {B} n'avait pas tenu quelqu'un d'aussi petit.
  ? bad
  {B} prend le plus petit pour le bercer, et le plus petit attrape le nez de {B}. Il ne le lâche plus. Il a la poigne de sa mère.

> guard | Veiller sur le sommeil de l'Ortie  [lean honor +1] [lean sociability -1]
  ? good
  {B} s'assoit près de l'Ortie et chasse d'un geste tout ce qui pourrait la réveiller : une mouche, un voisin, le Noyer qui passait dire bonjour. Elle dort une heure entière, la première depuis des années.
  ? bad
  {B} veille sur le sommeil de l'Ortie, de si près que, quand elle ouvre un œil, c'est la première chose qu'elle voit. « Je ne dormais pas », dit-elle. Elle se rendort aussitôt.

> nap | Faire la sieste aussi, tant qu'on y est  [lean energy -2]
  ? good
  {B} s'assoit au bout du banc et ferme les yeux, juste un instant. L'Ortie se réveille la tête sur l'épaule de {B}, et ne dit pas merci. Le lendemain, il y a une tarte aux noix sur le seuil.
  ? bad
  {B} fait la sieste aussi, et ronfle. Les petits trouvent ça très drôle et ronflent en chœur. L'Ortie se réveille en sursaut et demande qui a amené le moulin.
```

## 4. Le registre des pousses (bébé)

```
# visit: pousse_register
title: Le registre des pousses
place: 📜 Devant la cabane de la Mère Lichen
bonds: lovers
hours: 8-18
stages: baby

L'Intendant est descendu de Roncebelle pour inscrire les nouvelles pousses. Il a sorti son registre, sa plume et son air sérieux. « Nom, taille, signe particulier. » {A} et {B} se présentent devant lui en se tenant par la main, et ne la lâchent pas.

* answer | Répondre à tout, très sérieusement  [lean honor +2]
  ? good
  {A} répond à tout, très sérieusement, en se tenant bien droit{a:|e}. Au « signe particulier », {A} réfléchit longtemps, puis montre {B}. L'Intendant l'écrit : il note ce qu'on lui dit.
  ? bad
  {A} répond à tout, très sérieusement, mais à la place de {B}, et {B} à la place de {A}. Le registre de Roncebelle compte désormais deux pousses qui n'existent pas tout à fait.

* tiptoe | Se mettre sur la pointe des pieds  [lean boldness +1] [lean honor -1]
  ? good
  {A} se met sur la pointe des pieds pour la taille, et tire {B} vers le haut, pour être pareil{p:s|les}. L'Intendant mesure, note, et dit sans lever les yeux : « Il est d'usage de grandir. Vous êtes en avance. »
  ? bad
  {A} se met sur la pointe des pieds, très haut, et bascule sur {B}. On se relève couvert{p:s|es} de mousse. L'Intendant note : « Taille : variable. »

* gift | Lui offrir une fraise de la Lisière  [lean compassion +1] [lean sociability +1]
  ? good
  {A} lui tend une fraise de la Lisière, la plus rouge. L'Intendant ne sait pas où la noter. Il finit par la manger, et dit merci deux fois, la seconde tout bas.
  ? bad
  {A} lui tend une fraise de la Lisière, qui tombe sur le registre. À la page des pousses, il y a maintenant une tache rouge en forme de cœur. L'Intendant la contemple longtemps. Il n'ose pas la noter.

> other | Donner le nom de l'autre en premier  [lean compassion +1] [lean sociability +1]
  ? good
  Quand l'Intendant demande son nom, {B} répond par celui de {A}, {b:tout fier|toute fière}. L'Intendant hésite, puis écrit les deux noms sur la même ligne, en tout petit, pour qu'ils tiennent.
  ? bad
  {B} donne le nom de {A} en premier, puis le sien, puis celui de {A} encore, pour être sûr{b:|e}. L'Intendant compte trois pousses. Il recompte. Il en trouve toujours une de trop.

> squeeze | Serrer la main encore plus fort  [lean honor +1] [lean boldness +1]
  ? good
  {B} serre la main de {A} encore plus fort, au cas où l'Intendant voudrait les séparer. Il n'y pense pas une seconde : il en a vu d'autres, autrefois, à la Maison des Pousses. Il se détourne un instant, très occupé par sa plume.
  ? bad
  {B} serre la main de {A} si fort qu'on ne peut plus la lâcher du tout, même pour signer. L'Intendant attend, poliment. La Mère Lichen, sur sa pierre, dit « pas encore ».

> look | Demander à voir le registre  [lean rationality +2]
  ? good
  {B} demande à voir le registre. L'Intendant, flatté, montre les colonnes, les lignes, les ratures. {B} ne sait pas lire, mais hoche la tête à chaque page, et l'Intendant n'a jamais eu de public aussi attentif.
  ? bad
  {B} demande à voir le registre, et l'Intendant explique chaque colonne, chaque ligne, chaque rature. Ça dure jusqu'au goûter. {B} s'endort contre la cabane, à la colonne des tailles.
```

## 5. La meule de nuit

```
# visit: night_mill
title: La meule de nuit
place: 🌙 Au moulin de la Sommeille, la nuit
bonds: enemies
hours: 22-6

Le Chardon a du grain à moudre avant le marché, et il faut quelqu'un pour surveiller la meule jusqu'à l'aube. Il a choisi {A} et {B}, exprès. « Vous deux, vous ne dormirez pas. Vous serez trop occupé{p:s|es} à vous surveiller. » Puis il est monté se coucher.

* serious | Surveiller la meule, et rien d'autre  [lean honor +2]
  ? good
  {A} surveille la meule, et rien d'autre, toute la nuit. Au matin, la farine est fine comme de la poussière de lune. Le Chardon y passe la main, grogne, et grave une petite encoche sur la poutre, à côté des noms de ses apprentis.
  ? bad
  {A} surveille la meule si fixement qu'elle finit par tourner aussi dans sa tête. Au matin, {A} rentre en marchant en rond, très lentement.

* race | Faire la course aux sacs  [lean energy +1] [lean vengefulness +1]
  ? good
  {A} lance une course : qui remplira le plus de sacs. On ne se parle pas, on se dépasse. À l'aube, il y a deux tas exactement égaux. Personne n'a gagné, et personne n'a dormi.
  ? bad
  {A} lance une course aux sacs, et un sac crève. Au matin, le Chardon trouve deux fantômes tout blancs qui se disputent, et la moitié de sa farine sur les poutres.

* turns | Dormir chacun son tour, en premier  [lean greed +1] [lean rationality +1]
  ? good
  {A} propose de dormir chacun son tour, et prend le premier, bien sûr. Le premier tour dure toute la nuit. Au matin, {A} se réveille sous une couverture que personne ne reconnaît avoir posée.
  ? bad
  {A} propose de dormir chacun son tour, et s'endort au milieu de la phrase. On ne saura jamais qui devait prendre le deuxième tour. On en débat encore.

> watch | Surveiller l'autre plutôt que la meule  [lean vengefulness +1] [lean rationality +1]
  ? good
  {B} surveille {A} plutôt que la meule, toute la nuit, sans ciller. C'est exactement ce que voulait le Chardon : personne n'a fermé l'œil. Il le dit au matin, très fier de son calcul.
  ? bad
  {B} surveille {A} plutôt que la meule, et la meule, que plus personne ne surveille, se met à moudre du vide. Toute la nuit, il y a beaucoup de bruit et très peu de farine.

> tell | Raconter des histoires pour ne pas dormir  [lean sociability +2]
  ? good
  {B} raconte des histoires pour ne pas dormir, des histoires qu'on n'aime pas, exprès. Vers trois heures, on se surprend à rire au même endroit. On fait semblant que non.
  ? bad
  {B} raconte des histoires pour tenir tout le monde éveillé, si lentement que le moulin entier s'endort. Au matin, le grain a été moulu deux fois, et le Chardon ne sait plus s'il doit grogner.

> boo | Faire peur à l'autre dans le noir  [lean boldness +1] [lean vengefulness +1]
  ? good
  {B} se cache derrière un sac et fait « hou ! » au bon moment. {A} sursaute, et la peur réveille si bien qu'on tient jusqu'à l'aube, chacun guettant sa revanche.
  ? bad
  {B} se cache derrière un sac pour faire peur à {A}, et c'est le Chardon, descendu voir, qui fait le plus beau sursaut. Il remonte se coucher en grognant pour trois.
```

## 6. Le vote de la porte (ado, adulte, ancien)

```
# visit: council_vote
title: Le vote de la porte
place: 🗳️ À la Maison commune, un soir de conseil
bonds: enemies
hours: 17-23
stages: teen adult elder

Le conseil doit choisir la couleur de la nouvelle porte de la Maison commune, et il y a égalité : Dame Ermengarde a voté comme le Noyer, Renaud contre, par principe. Maître Frêne se tourne vers le public, c'est-à-dire vers {A} et {B}. « Il convient de départager. Examinons vos voix. »

* contra | Voter contre l'autre, par principe  [lean vengefulness +2]
  ? good
  {A} attend que {B} ouvre la bouche, puis vote le contraire, par principe. Renaud se lève et applaudit : il a enfin trouvé un{a:|e} élève.
  ? bad
  {A} vote contre l'autre, par principe, et se retrouve à défendre le rose. Personne ne voulait de rose. {A} non plus, mais il est trop tard pour reculer.

* noyer | Voter comme le Noyer  [lean sociability +1] [lean boldness -1]
  ? good
  {A} vote comme le Noyer, et Dame Ermengarde se retourne pour lui adresser un clin d'œil de vieille complice. Le Noyer, lui, fait servir le cidre avant la fin de la séance.
  ? bad
  {A} vote comme le Noyer, mais le Noyer, entre-temps, a changé d'avis, et Dame Ermengarde avec lui. {A} reste seul{a:|e} à défendre une couleur que plus personne ne veut.

* third | Proposer une troisième couleur  [lean rationality +1] [lean boldness +1]
  ? good
  {A} propose une troisième couleur : celle du bois, sans peinture. Il y a un silence, puis des hochements de tête tout autour de la table. Maître Frêne note qu'une séance pourrait finir à l'heure, et le souligne deux fois.
  ? bad
  {A} propose une troisième couleur. Renaud en propose une quatrième, par principe. À minuit, on en est à onze, et le cidre d'après est froid.

> switch | Changer de camp au dernier moment  [lean honor -1] [lean boldness +1]
  ? good
  {B} change de camp au dernier moment, rien que pour voir la tête de {A}. La tête de {A} vaut le détour. Toute la salle rit, et, un instant, {A} aussi.
  ? bad
  {B} change de camp, puis rechange, puis rechange encore. Maître Frêne, qui note tout, n'a plus de place sur sa feuille.

> speech | Se lever pour un discours  [lean sociability +1] [lean energy +1]
  ? good
  {B} se lève et fait un discours sur l'importance des portes, et c'est, contre toute attente, très beau. Dame Ermengarde essuie une larme. Elle ne sait plus pour quoi elle vote.
  ? bad
  {B} se lève et fait un discours. Il est si long que le cidre d'après devient le cidre de pendant.

> abstain | S'abstenir, très dignement  [lean honor +1] [lean energy -1]
  ? good
  {B} s'abstient, très dignement, les mains croisées. Maître Frêne, ému, cite le geste jusqu'à la fin de la séance : « Personne n'a tort, et personne n'a tout à fait raison. Voilà la paix. »
  ? bad
  {B} s'abstient, très dignement, et le conseil, dérouté, décide de s'abstenir aussi. La porte restera sans couleur. Le débat reprendra au printemps.
```

## 7. Le grand banc

```
# visit: long_bench
title: Le grand banc
place: 🪵 Sur le chemin de la place du Puits
bonds: enemies
hours: 7-20

Le conseil a décidé qui porterait les bancs du banquet, et c'est tombé sur {A} et {B}. Il ne reste que le grand banc, le plus long, celui qui se porte à deux. Clément et Gaspard, les fils du Noyer, sont assis sur le muret. Ils parient déjà sur qui lâchera son bout le premier.

* lead | Choisir le chemin sans demander  [lean boldness +2]
  ? good
  {A} choisit le chemin, sans demander, et c'est le bon : le plus court, le plus plat. Le banc arrive sur la place sans une égratignure, et les fils du Noyer perdent leur pari tous les deux.
  ? bad
  {A} choisit le chemin, sans demander, et le chemin passe par la porte de la tisserande, qui est très étroite. Le banc y reste coincé un bon moment. La tisserande file sans lever les yeux, et commente.

* yield | Laisser passer devant  [lean compassion +1] [lean honor +1]
  ? good
  {A} laisse passer devant, sans un mot. Ça surprend tellement que tout le chemin se fait en silence. Clément et Gaspard, déçus, se rendent leurs paris.
  ? bad
  {A} laisse passer devant, très poliment, et on se retrouve à vouloir porter le banc par le même bout. Clément et Gaspard en tombent du muret.

* fast | Porter son bout au pas de course  [lean energy +1] [lean vengefulness +1]
  ? good
  {A} porte son bout au pas de course, pour forcer l'autre à suivre. Il faut suivre, et on suit. On arrive sur la place essoufflé{p:s|es}, à égalité, ce qui ne plaît à personne et arrange tout le monde.
  ? bad
  {A} part au pas de course avec son bout, et le banc, qui ne court pas, s'arrête net. {A} aussi, un peu plus loin, mais sans le banc.

> pull | Tirer de son côté  [lean vengefulness +1] [lean energy +1]
  ? good
  {B} tire de son côté, par principe. Le banc ne va nulle part pendant un moment, puis on s'aperçoit que c'est très fatigant, et on se met d'accord sans le dire. Les fils du Noyer rentrent chez eux, déçus.
  ? bad
  {B} tire de toutes ses forces, juste quand l'autre bout lâche, et part s'asseoir dans le fossé avec le banc par-dessus, comme dans un fauteuil.

> rest | Poser son bout et s'asseoir dessus  [lean energy -2]
  ? good
  {B} pose son bout au milieu du chemin et s'assoit dessus. Au bout d'un moment, l'autre bout s'assoit aussi. On reste là, chacun à son bout, dos à dos, et c'est la meilleure pause de la journée.
  ? bad
  {B} pose son bout pour s'asseoir, et le banc bascule comme une balançoire. Clément et Gaspard réclament leur tour.

> bet | Parier avec les fils du Noyer  [lean greed +1] [lean boldness +1]
  ? good
  {B} va parier avec les fils du Noyer, sur soi-même, et gagne. {B} partage les gains, de très mauvaise grâce : trois noix chacun.
  ? bad
  {B} lâche son bout pour aller serrer la main des fils du Noyer et prendre le pari. Le pari est perdu sur-le-champ. Le banc, lui, est dans le fossé.
```

## 8. Les cuillères de Guérin

```
# visit: guerin_spoons
title: Les cuillères de Guérin
place: 🥄 À l'établi de Guérin
bonds: enemies
hours: 8-20

Guérin, le mari de l'Ortie, taille des cuillères à têtes de villageois, et il vient d'en finir deux : une avec la tête de {A}, une avec la tête de {B}. Elles sont posées côte à côte sur l'établi. Elles sont très ressemblantes. Trop, pense chacun, en regardant celle de l'autre.

* mine | Acheter sa propre cuillère, pour la cacher  [lean greed +1] [lean boldness -1]
  ? good
  {A} achète sa propre cuillère, pour la cacher au fond d'un tiroir. Mais à la maison, de près, {A} lui trouve un certain charme. Elle sert à la soupe, maintenant.
  ? bad
  {A} achète sa propre cuillère pour la cacher, et Guérin, ravi d'avoir vendu, en taille aussitôt une autre, pareille. « Elle part bien, celle-là. »

* theirs | Acheter celle de l'autre, pour la montrer  [lean vengefulness +1] [lean greed +1]
  ? good
  {A} achète la cuillère de {B}, pour la montrer partout. Mais à force de la montrer, {A} finit par la trouver assez réussie, et le dit, sans faire exprès.
  ? bad
  {A} achète la cuillère de {B}, pour la montrer partout, et la tend d'abord à la Courge, qui s'écrie : « Oh, mais c'est toi, mon chou ! » Elle parle de {A}.

* retouch | Demander à Guérin de retoucher le sourire  [lean rationality +1] [lean boldness +1]
  ? good
  {A} demande à Guérin de retoucher un peu le sourire. Guérin retouche, souffle les copeaux, et le sourire est parfait. Il retouche aussi l'autre cuillère, gratuitement, et la rend plus aimable.
  ? bad
  {A} demande à Guérin de retoucher le sourire. Guérin retouche, puis retouche encore, et la cuillère sourit maintenant jusqu'aux oreilles, et un peu au-delà.

> admit | Avouer que la sienne est ressemblante  [lean honor +2]
  ? good
  {B} prend sa cuillère, la regarde longtemps, et avoue qu'elle est très ressemblante. C'est si honnête que Guérin en pose son couteau, et que même {A}, un instant, ne trouve rien à redire.
  ? bad
  {B} avoue que sa cuillère est ressemblante, puis se regarde dans le creux pour vérifier. Dans le creux d'une cuillère, on est à l'envers. Ça n'aide pas.

> swap | Échanger les étiquettes en douce  [lean honor -2]
  ? good
  {B} échange les étiquettes en douce. Guérin s'en aperçoit, rit dans sa barbe de copeaux, et laisse faire. Personne ne voit la différence, et pour la première fois, on a envie de rire de la même chose.
  ? bad
  {B} échange les étiquettes en douce, sous les yeux de l'Ortie, qui sort sur le seuil, remet les étiquettes en place sans un mot, et reste là, les bras croisés, jusqu'à ce qu'on s'en aille.

> praise | Complimenter Guérin sur son travail  [lean compassion +1] [lean sociability +1]
  ? good
  {B} complimente Guérin, sincèrement. Guérin, qui n'en entend jamais, sort toutes ses cuillères, une par une : le Noyer, le Chardon, la Courge. On rit de chacune, ensemble, sans s'en apercevoir.
  ? bad
  {B} complimente Guérin, et Guérin, touché, offre une cuillère en cadeau. C'est celle de {A}.
```

## Questions pour la relecture

- **Les visages des ennemis.** Une bonne fin entre ennemis s'affiche avec les visages fâchés
  (`MoodFor(true, Enemies)` donne `Conflict`). C'est déjà le cas pour les scènes partagées, et une trêve
  boudeuse a son charme. Si tu préfères des visages apaisés sur les trêves, j'ouvre les demi-fins `good`
  des quatre scènes d'ennemis par `[content]`.
- **`pousse_register`** ne sert qu'aux bébés **en couple**, une paire rare : elle sortira peu, et c'est
  voulu (c'est le prix d'une scène réservée). Si tu veux qu'elle serve plus souvent, elle s'ouvre
  aussi aux meilleurs amis en changeant deux mots (« se tenant par la main » reste vrai).
- **`council_vote`** exclut les bébés (un bébé au conseil, le soir, ne tient pas debout).
- **Heures** : `dark_path` 21–6 et `night_mill` 22–6 traversent minuit, comme `rain_lights`.
