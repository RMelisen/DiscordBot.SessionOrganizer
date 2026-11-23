# Visites Plynling — fiche d'écriture des scènes (vague 3 : points faibles et scènes classiques)

Fiche de relecture, **approuvée telle quelle le 2026-11-23** et livrée en 8.2.9 : chaque bloc est recopié
tel quel dans `ProjectSYNCS/Stories/Visits/<clé>.visit`. Toute retouche d'un texte repasse par le
propriétaire. Questions de relecture laissées par défaut : `field_stone` réservée aux adultes,
`bruyere_round` sans le couple, l'aînée de la Courge est une fille.
Même format et mêmes conventions que la vague 1 (`docs/plynling-visit-scenes-sheet.md`, *Comment lire un
bloc*).

## Pourquoi cette vague

Après la vague 2 (8.2.8), le compte des scènes possibles par lien, par étape et par heure montre où le
tirage reste maigre :

- **les rivaux** sont devenus le lien le plus pauvre : 11 scènes, 6,5 en moyenne à une heure donnée pour
  des bébés ;
- **la nuit** (0 h–5 h) : 4 ou 5 scènes seulement, pour tous les liens ;
- **le petit matin** : vers 6 h, un couple de bébés ou d'ados n'a que 3 scènes ;
- **les étapes** : aucune scène réservée aux adultes, une seule aux ados (`missing_rule`).

Et **les visites classiques** (connaissances, amis, meilleurs amis : la plupart des visites) n'ont rien
reçu depuis le lancement. Cette vague mélange les deux : 5 scènes qui visent un point faible, 3 scènes
classiques de la vie du hameau, toutes partagées et donc **écrites neutres** (ni tendresse ni rancune
supposées), sauf la première, réservée aux rivaux.

## La liste

| # | Clé | Lieu | Liens | Heures | Étapes | En une ligne |
|---|---|---|---|---|---|---|
| 1 | `taste_jury` | 🥧 Sur la place du Puits | rivaux | 10–19 | toutes | Pain-Blond et la Courge prennent chacun un juré, et chacun défend son camp |
| 2 | `dawn_dew` | 💧 Au pré de la Sommeille, avant le soleil | amis, meilleurs amis, couple | 4–9 | toutes | La rosée du trèfle pour la Sauge, avant que le soleil la boive |
| 3 | `glow_spot` | ✨ À la Lisière, au cœur de la nuit | connaissances, amis, meilleurs amis, rivaux | 22–5 | toutes | L'Étrangère montre un coin secret, à qui saura se taire tout le chemin |
| 4 | `bruyere_round` | 🛏️ À la Maison des Pousses, après le couvre-feu | connaissances, amis, meilleurs amis, rivaux, ennemis | 21–2 | ado | Faire semblant de dormir pour prendre la Bruyère à border les pousses |
| 5 | `field_stone` | 🌾 Aux Longues Raies | connaissances, amis, meilleurs amis, rivaux, ennemis | 7–19 | adulte | Une borne penche entre leurs deux raies, et Maître Frêne arrive avec son ruban |
| 6 | `wool_skein` | 🧶 Au coin de la tisserande, sur la place | connaissances, amis, meilleurs amis, rivaux | 8–19 | toutes | La tisserande leur tend chacun un bout d'écheveau : « Bougez pas. » |
| 7 | `clerk_papers` | 📃 Sur la place du Puits, un jour de vent | les six | 8–18 | toutes | Le vent emporte les feuilles du clerc de l'Intendant |
| 8 | `tavern_music` | 🎻 À l'auberge du Pont-Vieux, un soir sans musicien | connaissances, amis, meilleurs amis, couple, rivaux | 17–23 | toutes | Le musicien n'est pas venu, et on leur colle un pipeau et un tambourin |

**Couverture après la vague** — scènes possibles par lien : connaissances 14 → 20, amis 17 → 24,
meilleurs amis 14 → 21, couple 13 → 16, **rivaux 11 → 18**, ennemis 12 → 15. Au creux de la journée
(l'heure la plus pauvre), les liens classiques passent de 5 à 6 scènes, les rivaux de bébés restent à 4
la nuit mais montent de 6,5 à 8,3 en moyenne. La nuit, chaque lien sauf
le couple et les ennemis gagne `glow_spot` (les deux autres ont eu `dark_path` et `night_mill`) ; les ados
gagnent aussi `bruyere_round`. Vers 6 h, le couple gagne `dawn_dew`. Les adultes ont leur première scène,
les ados leur deuxième.

**Les astuces** : un jury où chacun défend son camp ; une course contre le soleil ; un pacte de silence ;
faire semblant de dormir ; une borne qui penche et un ruban à tenir ; tenir la laine sans bouger ; des
feuilles dans le vent ; une musique improvisée. La plus proche d'une scène existante est `field_stone`
(une limite disputée, comme `meadow_quarrel`) : là-bas, on arbitre la querelle des autres ; ici, la
borne est entre leurs propres raies, et le sujet est le ruban de Maître Frêne.

**Villageois** : Pain-Blond et la Courge (ensemble, leur rivalité), la Sauge (de loin), l'Étrangère, la
Bruyère, Maître Frêne, la tisserande, le clerc de l'Intendant (première apparition), l'aînée de la
Courge (première apparition ; le canon dit « trois grands enfants » sans les genrer, la fiche en fait
une fille).

---

## 1. Le jury du goûter (rivaux)

```
# visit: taste_jury
title: Le jury du goûter
place: 🥧 Sur la place du Puits
bonds: rivals
hours: 10-19

Pain-Blond et la Courge ont encore parié sur qui nourrit le mieux le village, et il leur faut un jury. La Courge attrape {B} par le bras : « Toi, tu goûtes ma tarte. » Pain-Blond, tout rouge, tend sa brioche à {A}. Chacun devra défendre ce qu'il a goûté. Évidemment.

* praise | Vanter la brioche avec emphase  [lean sociability +1] [lean boldness +1]
  ? good
  {A} vante la brioche avec tant d'emphase que des passants s'arrêtent pour en demander. Pain-Blond, qui n'a jamais eu d'avocat, cache sa joie dans son tablier.
  ? bad
  {A} vante la brioche si fort que Pain-Blond, gêné, finit par dire lui-même que la tarte est meilleure. Il ne sait pas mentir, même pour lui.

* honest | Juger franchement, miette par miette  [lean honor +1] [lean rationality +1]
  ? good
  {A} juge franchement, miette par miette : la croûte, la mie, le parfum. C'est si précis que la Courge elle-même se tait pour écouter. Ça n'arrive pas souvent.
  ? bad
  {A} juge si consciencieusement, miette par miette, qu'à la fin il ne reste plus de brioche à juger. {A} demande une deuxième part, pour vérifier.

* both | Goûter la tarte aussi, en douce  [lean greed +1] [lean honor -1]
  ? good
  {A} goûte la tarte aussi, en douce, pour comparer, puis déclare, la bouche pleine, qu'il faudra une troisième épreuve. Tout le monde est d'accord : il y aura un goûter demain aussi.
  ? bad
  {A} goûte la tarte en douce, et la Courge s'en aperçoit, évidemment : ses vrilles voient tout. « Alors, mon chou ? » Il faut répondre, la bouche pleine de tarte.

> top | Surenchérir pour la tarte  [lean vengefulness +1] [lean sociability +1]
  ? good
  {B} surenchérit pour la tarte, un mot plus haut à chaque fois, jusqu'à « la meilleure tarte du monde connu ». Personne à Humeval ne connaît le monde. Tout le monde applaudit quand même.
  ? bad
  {B} surenchérit pour la tarte, si haut que la Courge, prise au jeu, promet d'en faire une pour tout le village ce soir. Elle n'a plus de pommes. Elle regarde vers le clos du Tilleul.

> fair | Reconnaître que la brioche est bonne aussi  [lean honor +1] [lean compassion +1]
  ? good
  {B} reconnaît, de bonne grâce, que la brioche est bonne aussi. Pain-Blond en reste muet, la Courge aussi, et pour une fois, le concours finit sur une égalité que tout le monde trouve juste.
  ? bad
  {B} reconnaît que la brioche est bonne aussi, et la Courge, trahie, reprend sa tarte avec dignité. Elle la mange elle-même, assise sur le muret, en regardant tout le monde.

> mix | Poser la tarte sur la brioche  [lean boldness +1] [lean rationality -1]
  ? good
  {B} pose une part de tarte sur une tranche de brioche, et mord dedans. Silence sur la place. Pain-Blond et la Courge goûtent à leur tour, se regardent, et décident de ne plus jamais en parler, parce que c'est trop bon.
  ? bad
  {B} pose la tarte sur la brioche, et le tout glisse par terre, côté confiture. Le Noyer, qui passait, ramasse, souffle dessus, et mange. « Le Noyer ne gâche rien. »
```

## 2. La rosée

```
# visit: dawn_dew
title: La rosée
place: 💧 Au pré de la Sommeille, avant le soleil
bonds: friends bestfriends lovers
hours: 4-9

La Sauge a besoin de rosée pour un baume, la rosée du trèfle, avant que le soleil la boive. Elle a confié deux petits bols à {A} et {B}, avec ses instructions : « Une goutte par feuille. Ne secouez pas. Et ne la buvez pas, je vous connais. » Le pré est gris et silencieux. Le soleil n'est pas loin.

* careful | Cueillir la rosée goutte à goutte  [lean rationality +1] [lean energy -1]
  ? good
  {A} cueille la rosée goutte à goutte, feuille après feuille, sans en perdre une. Quand le soleil se lève, le bol est plein, et tout le pré brille comme si on l'avait remercié.
  ? bad
  {A} cueille goutte à goutte, avec tant de soin qu'au lever du soleil, il y a juste de quoi remplir un dé à coudre. La Sauge l'examine longtemps. « C'est un début. »

* shake | Secouer les trèfles au-dessus du bol  [lean boldness +1] [lean honor -1]
  ? good
  {A} secoue les trèfles au-dessus du bol, malgré les instructions, et la rosée tombe comme une petite averse. Le bol est plein en un rien de temps. La Sauge n'a pas besoin de le savoir.
  ? bad
  {A} secoue les trèfles au-dessus du bol, et les gouttes partent partout, sauf dans le bol. {A} rentre trempé{a:|e}, le bol sec.

* sip | Goûter une goutte, juste une  [lean zeal +1] [lean greed +1]
  ? good
  {A} goûte une goutte, juste une. Elle a le goût du matin, de l'herbe et du ciel. {A} jure que c'est pour vérifier la qualité, et la Sauge, plus tard, fera semblant de le croire.
  ? bad
  {A} goûte une goutte, puis une autre, pour vérifier, puis une autre encore. Au lever du soleil, le bol est vide, et {A} est très bien hydraté{a:|e}.

> sunrise | S'arrêter pour regarder le soleil se lever  [lean energy -1] [lean zeal +1]
  ? good
  {B} s'arrête pour regarder le soleil se lever sur la Sommeille, le bol oublié dans les mains. Il y a un instant où tout le pré devient doré d'un coup. On s'en souviendra longtemps.
  ? bad
  {B} s'arrête pour regarder le soleil se lever, et le soleil, en se levant, boit la rosée du bol de {B} sans demander. Il ne reste qu'un petit rond humide au fond.

> race | Proposer une course aux bols  [lean energy +1] [lean sociability +1]
  ? good
  {B} propose une course, à qui remplira son bol le premier. On court d'un trèfle à l'autre, on rit tout bas, et le soleil arrive sur deux bols pleins et deux nez mouillés.
  ? bad
  {B} propose une course aux bols, et le bol de {B} prend de l'avance tout seul, en roulant jusqu'à la rivière. Le passeur le repêche et le rend, plein d'eau de la Sommeille. Ça ne compte pas.

> share | Verser un peu de sa rosée dans l'autre bol  [lean compassion +2]
  ? good
  {B} verse un peu de sa rosée dans le bol de {A}, sans rien dire, pour que les deux soient pareils. Au retour, la Sauge remarque que les deux bols sont exactement au même niveau. Elle ne pose pas de question.
  ? bad
  {B} verse un peu de sa rosée dans le bol de {A}, et la main tremble : il y en a maintenant sur les pieds, dans l'herbe, partout sauf dans les bols. Le soleil, lui, s'en régale.
```

## 3. Le coin de l'Étrangère

```
# visit: glow_spot
title: Le coin de l'Étrangère
place: ✨ À la Lisière, au cœur de la nuit
bonds: acquaintances friends bestfriends rivals
hours: 22-5

L'Étrangère passe devant eux sans s'arrêter, puis se retourne. « Je connais un coin. Je ne le montre qu'à qui sait se taire. Tout le chemin. » Elle remonte sa capuche et part vers la Lisière. {A} et {B} la suivent. Elle n'a pas dit ce qu'il y avait au bout.

* silent | Se taire, vraiment, tout le chemin  [lean honor +1] [lean sociability -1]
  ? good
  {A} se tait tout le chemin, vraiment, même quand une ronce attrape un pied. Au bout, sous les fougères, il y a un cercle de petits champignons qui luisent, bleus et verts, comme un village vu de très haut. L'Étrangère sourit pour la première fois de la soirée.
  ? bad
  {A} se tait si bien, tout le chemin, qu'au bout, quand l'Étrangère demande « alors ? », {A} n'ose plus parler du tout. On rentre sans un mot, très impressionné{p:s|es}.

* steps | Marcher exactement dans ses pas  [lean compassion +1] [lean zeal +1]
  ? good
  {A} pose les pieds exactement dans les pas de l'Étrangère, sans un bruit, comme une ombre. Elle s'en aperçoit, et raccourcit ses pas. Au bout du chemin, elle tient une fougère levée, pour qu'on passe sans se mouiller.
  ? bad
  {A} marche exactement dans les pas de l'Étrangère, et l'Étrangère s'arrête net. {A}, non. Tout le monde finit dans la mousse, et elle rit, sans un bruit, ce qui compte.

* ask | Lui demander d'où elle vient, tout bas  [lean boldness +1] [lean sociability +1]
  ? good
  {A} demande, tout bas, d'où elle vient. L'Étrangère ne répond pas : elle demande où {A} va. {A} y réfléchit tout le reste du chemin. Personne n'avait posé une si belle question à {A} depuis longtemps.
  ? bad
  {A} demande, tout bas, d'où elle vient. L'Étrangère s'arrête, pose un doigt sur ses lèvres, et le chemin dure ensuite deux fois plus longtemps, par principe.

> cape | Tenir le bout de sa cape pour ne pas se perdre  [lean compassion +1] [lean boldness -1]
  ? good
  {B} attrape le bout de la cape de l'Étrangère, pour ne pas se perdre. Elle ne dit rien, mais elle ralentit. À la fin, elle laisse sa capuche baissée, et luit tout doucement, rien que pour éclairer {B}.
  ? bad
  {B} attrape le bout de la cape pour ne pas se perdre, et la capuche glisse. D'un coup, toute la Lisière s'éclaire en vert. L'Étrangère la remet très vite, très gênée. On a vu tout le chemin, une seconde.

> touch | Toucher ce qui luit sous les fougères  [lean boldness +1] [lean energy +1]
  ? good
  Au bout du chemin, sous les fougères, quelque chose luit. {B} tend un doigt et effleure un tout petit champignon bleu, qui luit un peu plus fort, comme pour dire bonjour. L'Étrangère dit qu'il ne fait ça qu'avec ceux qu'il aime bien.
  ? bad
  Au bout du chemin, {B} tend la main vers ce qui luit sous les fougères, et tout s'éteint d'un coup, vexé. L'Étrangère soupire. « Ils sont timides. Comme moi. »

> whisper | Chuchoter un mot, quand même  [lean sociability +2]
  ? good
  {B} chuchote, quand même, une toute petite chose : « merci ». L'Étrangère entend, et décide que ça ne compte pas comme parler. Ça compte comme autre chose.
  ? bad
  {B} chuchote un seul mot, et la Lisière est si calme que le mot fait le tour des fougères avant de revenir. L'Étrangère compte jusqu'à dix, sans rien dire, puis repart.
```

## 4. La ronde de la Bruyère (ado)

```
# visit: bruyere_round
title: La ronde de la Bruyère
place: 🛏️ À la Maison des Pousses, après le couvre-feu
bonds: acquaintances friends bestfriends rivals enemies
hours: 21-2
stages: teen

Toute la Maison des Pousses le sait : la Bruyère borde les pousses une fois qu'elles dorment, pour qu'aucune ne puisse le raconter. Ce soir, {A} et {B}, dans deux lits voisins, ont décidé de faire semblant de dormir pour la prendre sur le fait. On entend ses pas dans le couloir.

* snore | Ronfler un peu, pour faire vrai  [lean boldness +1] [lean honor -1]
  ? good
  {A} ronfle un peu, juste ce qu'il faut. La Bruyère entre, borde {A} jusqu'au menton, et murmure quelque chose que personne ne doit entendre. {A} l'entend, et ne le dira jamais.
  ? bad
  {A} ronfle un peu, puis beaucoup, pour faire vrai. La Bruyère s'arrête sur le seuil. « Personne ne ronfle comme ça. Pas même le Noyer. » Elle ressort sans border personne.

* peek | Garder un œil entrouvert  [lean boldness +1] [lean rationality +1]
  ? good
  {A} garde un œil entrouvert, et voit tout : la Bruyère qui borde chaque lit, un par un, et s'arrête au dernier pour remonter une couverture de plus. Elle a l'air de quelqu'un qui compte ses trésors.
  ? bad
  {A} garde un œil entrouvert, et croise celui de la Bruyère, entrouvert lui aussi. Elle fait semblant de rien. {A} aussi. On reste là, chacun à faire semblant, très longtemps.

* really | S'endormir pour de bon  [lean energy -2]
  ? good
  {A} ferme les yeux pour faire semblant, et s'endort pour de bon. Au matin, la couverture est bordée si serré que {A} met un quart d'heure à sortir du lit. C'est la preuve.
  ? bad
  {A} s'endort pour de bon, et rate tout. Au petit déjeuner, la Bruyère demande, l'air de rien, si on a bien dormi. Elle sait. Elle sait toujours.

> whisper | Chuchoter pour commenter  [lean sociability +1] [lean boldness +1]
  ? good
  {B} chuchote un commentaire, puis un autre, si bas que la Bruyère croit entendre le vent. Elle ferme la fenêtre, borde un peu plus fort, et s'en va contente.
  ? bad
  {B} chuchote un commentaire, et la Bruyère, sans un mot, s'assoit au bout du lit de {B}, et y reste. Elle a toute la nuit. Elle l'a déjà fait.

> grab | Lui attraper la main au passage  [lean boldness +2]
  ? good
  {B} attrape la main de la Bruyère au passage, pour la prendre sur le fait. Elle se fige. Puis elle serre, une seconde, et dit : « Mange. » Il n'y a rien à manger dans le dortoir. Tout le monde a compris quand même.
  ? bad
  {B} attrape la main de la Bruyère au passage, et la Bruyère, surprise, lâche la pile de couvertures sur tout le dortoir. Il faut tout rebrousser, lit par lit. Personne ne dort avant très tard.

> dream | Dire une gentillesse en faisant semblant de rêver  [lean compassion +1] [lean boldness +1]
  ? good
  {B} fait semblant de parler en dormant, et dit, comme en rêve : « La Bruyère est la meilleure. » Elle s'arrête net, puis borde {B} avec un soin infini. Le lendemain, il y a une part de gâteau de plus dans l'assiette de {B}, et personne ne sait pourquoi.
  ? bad
  {B} fait semblant de parler en dormant, et se met à raconter, comme en rêve, toutes les bêtises de la semaine. La Bruyère écoute jusqu'au bout, très attentive, et prend des notes.
```

## 5. La borne des raies (adulte)

```
# visit: field_stone
title: La borne des raies
place: 🌾 Aux Longues Raies
bonds: acquaintances friends bestfriends rivals enemies
hours: 7-19
stages: adult

Le conseil a redistribué les Longues Raies, et la raie de {A} touche maintenant celle de {B}. Entre les deux, une borne penche, et personne ne sait plus de quel côté. Maître Frêne arrive avec son ruban à mesurer. « Examinons. Il me faut quelqu'un pour tenir le bout. »

* hold | Tenir le bout du ruban sans bouger  [lean honor +1] [lean rationality +1]
  ? good
  {A} tient le bout du ruban sans bouger d'un cheveu, droit{a:|e} comme une borne. Maître Frêne mesure deux fois, trouve deux fois la même chose, et en est si ému qu'il le note dans la marge.
  ? bad
  {A} tient le bout du ruban sans bouger, si longtemps que Maître Frêne, à l'autre bout du champ, a oublié qu'il mesurait. Il est en train de rédiger un règlement sur les bornes.

* nudge | Pousser la borne d'un doigt, l'air de rien  [lean greed +1] [lean honor -1]
  ? good
  {A} pousse la borne d'un doigt, l'air de rien, et elle se redresse toute seule, bien droite, exactement au milieu. Maître Frêne déclare la question réglée. {A} n'est pas sûr{a:|e} de l'avoir voulu.
  ? bad
  {A} pousse la borne d'un doigt, l'air de rien, et la borne se couche dans le sillon, de tout son long. Maître Frêne la regarde, puis regarde {A}, et sort une feuille neuve.

* beans | Proposer de semer la même chose des deux côtés  [lean compassion +1] [lean rationality +1]
  ? good
  {A} propose de semer des fèves des deux côtés de la borne, pour que plus personne ne sache où finit quoi. Maître Frêne trouve l'idée si paisible qu'il demande des fèves pour sa raie aussi.
  ? bad
  {A} propose de semer la même chose des deux côtés, et Maître Frêne, enthousiaste, en fait un règlement de quatre pages, avec un article sur la couleur des fèves.

> pace | Remesurer soi-même, à grands pas  [lean energy +1] [lean boldness +1]
  ? good
  {B} remesure à grands pas, d'un bout à l'autre, et tombe exactement sur le chiffre de Maître Frêne. Il en lâche son ruban : c'est la première fois que deux mesures tombent d'accord à Humeval.
  ? bad
  {B} remesure à grands pas, mais les pas de {B} ne sont pas tous de la même taille. On trouve onze pas, puis quatorze, puis neuf et demi. Maître Frêne les note tous.

> leave | Déclarer que la borne est très bien comme ça  [lean vengefulness -1] [lean energy -1]
  ? good
  {B} déclare que la borne est très bien comme ça, penchée, et qu'elle en a bien le droit. Maître Frêne réfléchit longtemps, puis l'inscrit au registre : « borne penchée, d'un commun accord ». Il n'en a jamais écrit d'aussi rapide.
  ? bad
  {B} déclare que la borne est très bien comme ça, et s'assoit dessus pour le prouver. La borne penche un peu plus. Maître Frêne note l'angle.

> lunch | Sortir le casse-croûte pour tout le monde  [lean sociability +1] [lean greed -1]
  ? good
  {B} sort le casse-croûte et le partage en trois, sur la borne, comme sur une table. Maître Frêne, qui n'avait pas prévu de manger, en oublie de mesurer. La question attendra demain, et personne ne s'en plaint.
  ? bad
  {B} pose le casse-croûte sur la borne, comme sur une table, et la borne, qui penchait déjà, le fait glisser dans le sillon. Maître Frêne ramasse le pain et le coupe en parts parfaitement égales quand même.
```

## 6. L'écheveau

```
# visit: wool_skein
title: L'écheveau
place: 🧶 Au coin de la tisserande, sur la place
bonds: acquaintances friends bestfriends rivals
hours: 8-19

La tisserande dévide un écheveau de laine, et il lui faut deux paires de mains pour le tenir tendu. Elle ne demande pas : elle passe un bout de laine à {A}, l'autre à {B}, et se met à enrouler sa pelote. « Bougez pas. » Elle a tout son temps. La laine, elle, est très longue.

* still | Ne pas bouger d'un fil  [lean honor +1] [lean energy -1]
  ? good
  {A} ne bouge pas d'un fil, bras tendus, et la pelote grossit, grossit. Quand c'est fini, la tisserande dit « bien ». C'est un mot qu'elle n'avait pas dit depuis la foire.
  ? bad
  {A} ne bouge pas d'un fil, et au bout d'une heure, les bras de {A} restent levés tout seuls. {A} rentre comme ça, les bras en l'air, et salue tout le monde sans le vouloir.

* chat | Faire la conversation à la tisserande  [lean sociability +2]
  ? good
  {A} fait la conversation à la tisserande, qui voit tout passer depuis son coin de place. Elle répond par petits morceaux, et chaque morceau est un ragot de première qualité, plus ancien que tous ceux de la Courge.
  ? bad
  {A} fait la conversation avec de grands gestes, la laine à la main. La laine se retrouve nouée à la manche de {A}, puis à celle de {B}, puis au banc.

* wind | Proposer d'enrouler à sa place  [lean compassion +1] [lean boldness +1]
  ? good
  {A} propose d'enrouler à sa place. La tisserande regarde longtemps, puis donne la pelote, et prend le bout de laine. Elle le tient mieux que personne. On dirait qu'elle n'attendait que ça.
  ? bad
  {A} propose d'enrouler à sa place, et enroule si vite que la pelote prend la forme d'un chou. La tisserande la contemple. Elle dit qu'elle en fera un bonnet. Pour un chou.

> sway | Se balancer au rythme de la pelote  [lean sociability +1] [lean energy +1]
  ? good
  {B} se balance un peu, au rythme de la pelote, et la laine se met à danser entre les deux mains. La tisserande ne dit rien, mais elle enroule en mesure.
  ? bad
  {B} se balance au rythme de la pelote, et la laine fait un nœud, puis un deuxième, puis un très gros. La tisserande les défait un par un, sans un mot, et ça prend jusqu'au soir.

> knot | Repérer les nœuds avant la pelote  [lean rationality +2]
  ? good
  {B} repère un nœud dans la laine avant qu'il n'arrive à la pelote, et le défait du bout des doigts. La tisserande hoche la tête : à son âge, on ne voit plus les nœuds, on les sent.
  ? bad
  {B} signale un nœud, puis un autre, puis un autre. Il n'y en avait qu'un ; les autres étaient des taches de soleil. La tisserande soupire comme soupire une ronce : longuement, et un peu piquant.

> whom | Demander pour qui est la laine  [lean compassion +1] [lean sociability +1]
  ? good
  {B} demande pour qui est la laine. La tisserande s'arrête, ce qui n'arrive jamais. « Pour la Bruyère. Une écharpe par an, et elle n'a jamais assez de laine. » Tout le monde sait pour qui sont les écharpes. Personne ne le dit.
  ? bad
  {B} demande pour qui est la laine. La tisserande répond « pour qui la demande », et sort un deuxième écheveau.
```

## 7. Les feuilles du clerc

```
# visit: clerk_papers
title: Les feuilles du clerc
place: 📃 Sur la place du Puits, un jour de vent
bonds: acquaintances friends bestfriends lovers rivals enemies
hours: 8-18

Le clerc de l'Intendant traverse la place, les bras chargés de registres, quand un coup de vent arrache toutes les feuilles du dessus. Elles s'envolent partout : vers le puits, vers l'auberge, vers le toit de la Maison commune. Le jeune roseau reste planté au milieu, plié en deux, l'air de quelqu'un qui va pleurer.

* chase | Courir après les feuilles  [lean energy +2]
  ? good
  {A} court après les feuilles, saute, plonge, et les rattrape presque toutes au ras des pavés. Le clerc les serre contre lui comme des poussins retrouvés.
  ? bad
  {A} court après les feuilles et les rattrape toutes, sauf une, qui se pose tranquillement dans le puits. On la voit flotter, tout au fond. Elle y est encore.

* calm | Consoler le clerc d'abord  [lean compassion +2]
  ? good
  {A} s'occupe du clerc d'abord, et lui dit que ce n'est que du vent. Le roseau se redresse un peu, puis tout à fait. Les feuilles, pendant ce temps, retombent toutes seules : le vent a eu sa journée.
  ? bad
  {A} console le clerc d'abord, si bien qu'il se met à raconter toute sa vie à Roncebelle, et les feuilles ont tout le temps de quitter le hameau.

* weigh | Poser une pierre sur chaque feuille tombée  [lean rationality +1] [lean energy -1]
  ? good
  {A} pose une pierre sur chaque feuille tombée, sans courir, l'une après l'autre. Quand le vent retombe, la place est couverte de feuilles bien rangées, chacune sous sa pierre, comme une marelle.
  ? bad
  {A} pose une pierre sur chaque feuille tombée, et le vent, vexé, emporte les pierres les plus légères avec les feuilles. On n'avait jamais vu ça. Le clerc non plus.

> roof | Grimper chercher celle du toit  [lean boldness +2]
  ? good
  {B} grimpe sur le toit de la Maison commune par la gouttière, et redescend avec la dernière feuille entre les dents. Toute la place applaudit. Le clerc la range tout en haut de la pile, comme une médaille.
  ? bad
  {B} grimpe sur le toit chercher la dernière feuille, et une fois en haut, découvre qu'on ne sait pas redescendre. Il faut aller chercher l'échelle du Noyer, et le Noyer avec.

> read | Lire ce qu'il y a sur les feuilles  [lean rationality +1] [lean honor -1]
  ? good
  {B} jette un œil sur une feuille, par curiosité. C'est la liste des pousses du hameau, avec, à côté de chaque nom, un petit dessin. Le clerc rougit jusqu'à la tige : c'est lui qui les fait, en cachette.
  ? bad
  {B} lit une feuille, par curiosité. C'est le compte des chèvres du Communal. Il y en a une. Elle est marquée « à surveiller », trois fois souligné.

> sort | Aider à remettre les feuilles dans l'ordre  [lean honor +1] [lean sociability +1]
  ? good
  {B} s'assoit avec le clerc sur la margelle du puits, et on remet les feuilles dans l'ordre, une par une. Il parle des registres comme d'autres parlent de chevaux. À la fin, il offre à chacun une feuille blanche, son plus beau cadeau.
  ? bad
  {B} aide à remettre les feuilles dans l'ordre, et le vent revient, juste un peu, juste pour voir. Tout est à refaire. Le clerc rit, cette fois : il a pris le pli.
```

## 8. Le musicien du soir

```
# visit: tavern_music
title: Le musicien du soir
place: 🎻 À l'auberge du Pont-Vieux, un soir sans musicien
bonds: acquaintances friends bestfriends lovers rivals
hours: 17-23

Le musicien du soir n'est pas venu, et l'aînée de la Courge court entre les tables, affolée : une auberge sans musique, c'est une soupe sans sel. Elle s'arrête devant {A} et {B}, et leur colle dans les mains un pipeau et un tambourin. « Vous deux. Juste une chanson. S'il vous plaît. »

* play | Jouer du pipeau de son mieux, même mal  [lean boldness +1] [lean sociability +1]
  ? good
  {A} joue de son mieux, et c'est faux, mais faux avec tant de cœur que l'auberge entière se met à taper du pied. Même le Chardon, au comptoir, bat la mesure, en le niant.
  ? bad
  {A} joue de son mieux, et le pipeau rend un son si aigu que toutes les poules du hameau répondent depuis leur perchoir. Elles connaissent la chanson mieux que {A}.

* learn | Demander qu'on montre les notes d'abord  [lean rationality +1] [lean boldness -1]
  ? good
  {A} demande qu'on montre les notes d'abord. L'aînée de la Courge en montre trois. Avec trois notes, {A} fait une chanson entière, et l'auberge la redemande deux fois.
  ? bad
  {A} demande qu'on montre les notes d'abord, et toute l'auberge veut aider : chacun montre une note différente. À la fin de la soirée, on a beaucoup appris, et rien joué.

* dance | Poser le pipeau et danser à la place  [lean energy +2]
  ? good
  {A} pose le pipeau et se met à danser, faute de mieux. Une table suit, puis une autre, et bientôt l'auberge danse sans musique, en se marchant sur les pieds, sans que personne remarque qu'il manque quelque chose.
  ? bad
  {A} danse à la place, et entraîne dans la danse un plateau de chopes, qui ne savait pas danser. La Courge note le plateau sur l'ardoise de {A}.

> beat | Battre la mesure au tambourin, très fort  [lean energy +1] [lean boldness +1]
  ? good
  {B} bat la mesure au tambourin, très fort, et le tambourin couvre tout ce qui est faux. Le Noyer déclare que c'est la meilleure musique qu'il ait entendue depuis la foire.
  ? bad
  {B} bat la mesure au tambourin, très fort, mais pas tout à fait la même. On joue deux chansons à la fois. L'auberge, polie, danse sur les deux.

> soft | Accompagner tout doucement  [lean compassion +1] [lean sociability -1]
  ? good
  {B} accompagne tout doucement, juste un frisson de tambourin sous le reste. C'est presque rien, et c'est ce qui tient tout. La Courge, derrière son comptoir, pose sa louche pour écouter.
  ? bad
  {B} accompagne si doucement que personne n'entend le tambourin, pas même {B}. À la fin, quelqu'un demande pourquoi {B} agitait les mains.

> collect | Faire passer le tambourin pour une quête  [lean greed +1] [lean rationality +1]
  ? good
  {B} fait le tour des tables avec le tambourin retourné, et revient avec des noix, un bout de fromage, deux cailloux et un bouton. La Courge déclare que c'est le plus beau cachet jamais payé à l'auberge.
  ? bad
  {B} fait passer le tambourin pour une quête, et le Noyer, généreux, y verse tant de noix que le tambourin, plein, ne fait plus que « ploc ». Le reste de la soirée, on joue du ploc.
```

## Questions pour la relecture

- **« Les visites classiques »** : je l'ai pris pour les scènes partagées de tous les jours
  (connaissances, amis, meilleurs amis, ouvertes à d'autres liens). Si tu pensais à autre chose — par
  exemple réécrire ou étoffer les 20 scènes du lancement — dis-le, c'est une autre vague.
- **`field_stone`** est réservée aux adultes : les anciens ont aussi leur raie (un feu ne la rend pas en
  vieillissant). Je l'ai gardée adulte pour leur donner une scène à eux ; on peut l'ouvrir aux anciens.
- **`bruyere_round`** exclut le couple : deux amoureux dans des lits voisins, sous la garde de la
  Bruyère, la fiche a préféré ne pas y aller.
- **L'aînée de la Courge** devient une fille ; si tu préfères un fils, trois mots changent.
