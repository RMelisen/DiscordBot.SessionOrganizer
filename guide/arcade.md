# L'arcade

Mes jeux, avec des [cailloux](cailloux.md) en jeu. Tout passe par `/arcade` : un duel à deux, un casse en équipe, des paris sur les Plynlings, une fusée, du blackjack… Choisis ton jeu, mise, et essaie de repartir plus riche que tu n'es arrivé. Je ne promets rien.

## Avant de jouer

- **Il te faut des cailloux.** Si tu es à sec, va faire un `/work` (tout est expliqué dans [Cailloux et objets](cailloux.md)). Certains jeux se jouent aussi sans mise.
- **Sur le serveur seulement**, pas en message privé : ton porte-monnaie appartient au serveur.
- **La mise est vérifiée quand tu entres, et payée à la fin.** Rien n'est bloqué pendant la partie. Si, au moment de régler, quelqu'un n'a plus de quoi couvrir sa mise, il est écarté du règlement ; dans un jeu à deux, la partie est alors annulée et personne ne paie.
- **Si je redémarre en pleine partie**, elle disparaît et aucun caillou ne bouge. Ses boutons répondent alors que la partie n'existe plus. (Seule exception : [la loterie](#la-loterie-arcade-lottery), où le ticket est payé tout de suite.)
- **La maison prend sa part.** Dans la plupart des jeux, une commission de 10 % s'évapore au passage. C'est moi l'arbitre, il faut bien que je vive.
- Chaque partie gagnée fait avancer ton badge 🕹️ **Pilier d'arcade** (voir [Niveaux et classements](niveaux.md)).

| Jeu | Commande | Joueurs | Mise |
|---|---|---|---|
| Pierre-feuille-ciseaux | `/arcade duel` | 2 (ou toi contre moi) | 10 à 500, au choix |
| Le casse de Quokka | `/arcade heist` | 1 à 8 | 25 par tête |
| Course de Plynlings | `/arcade race` | tout le monde parie | 10, 25, 50 ou 100 |
| Crash (la fusée) | `/arcade crash` | tout le monde | 10, 25, 50 ou 100 |
| Blackjack | `/arcade blackjack` | toi contre moi | 10 à 200 |
| Patate chaude | `/arcade bomb` | 3 à 8 | 20 |
| Qui a dit ça ? | `/arcade quote` | tout le monde | gratuit |
| Ma température | `/arcade temperature` | 2 et plus | 10 |
| Plus ou moins | `/arcade guess` | toi seul | 0 à 500 |
| Puissance 4 | `/arcade puissance4` | 2 (ou toi contre moi) | 0 à 500 |
| Morpion | `/arcade morpion` | 2 (ou toi contre moi) | 0 à 500 |
| Loterie | `/arcade lottery` | tout le monde | 20 le ticket |

## Duel — `/arcade duel`

Pierre-feuille-ciseaux contre quelqu'un, avec des cailloux en jeu.

- **Lancer** : `/arcade duel opponent:@quelqu'un stake:100`. La mise (`stake`) va de 10 à 500, et chacun met la même. Vous devez tous les deux avoir de quoi la payer.
- **Ton adversaire** reçoit une notification et a **10 minutes** pour cliquer sur **Accepter** ou **Refuser**.
- **Jouer** : une fois le duel accepté, chacun clique sur **Choisir mon coup** et choisit 🗿 Pierre, 📄 Feuille ou ✂️ Ciseaux dans un message que lui seul voit. La carte montre qui a joué, jamais quoi. Vous avez **5 minutes** pour choisir, sinon le duel est annulé et personne ne paie.
- **Égalité** : on rejoue la manche. Après 4 manches nulles d'affilée, c'est match nul et personne ne paie.
- **Gains** : le gagnant empoche la mise de l'autre, moins 10 % de commission (arrondie au-dessus). Exemple : mise de 100, le gagnant gagne 90, le perdant perd 100.
- La carte du duel est republiée en bas du salon à chaque étape, pour que vous ne la perdiez pas de vue.
- Tu peux me défier, moi aussi : je n'ai pas besoin d'accepter, j'accepte toujours. Si tu l'oses.
- On ne se défie pas soi-même, on ne défie pas les autres bots, et chacun n'a qu'un duel en cours à la fois.

## Le casse de Quokka — `/arcade heist`

Monte une équipe pour braquer la planque de Quokka. Plus vous êtes nombreux, plus ça passe, mais plus le butin se partage.

- **Lancer** : `/arcade heist`. Tu es le cerveau de l'opération et tu fais d'office partie de l'équipe. Il te faut 25 cailloux pour ta cagoule.
- **Recruter** : les autres cliquent sur **Rejoindre l'équipe** (8 membres au maximum) ou **Me retirer**. Le cerveau, lui, ne se retire pas.
- **Départ** : au bout de **25 minutes**, ou plus tôt si le cerveau clique sur **Lancer le casse**.
- Je raconte le casse en trois temps sur la carte, puis le résultat tombe et je mentionne toute l'équipe. En cas d'échec, je désigne un coupable au hasard. Ce n'est pas personnel. Enfin, si.
- **Un casse à la fois par serveur**, puis **6 heures** de pause : Quokka renforce sa sécurité.

La carte affiche en direct les chances et le butin pour l'équipe actuelle. Chaque membre mise 25 ; en cas de réussite, chacun repart avec :

| Équipe | Chances | Chacun repart avec (mise comprise) |
|---|---|---|
| 1 | 30 % | 75 |
| 2 | 40 % | 56 |
| 3 | 50 % | 45 |
| 4 | 60 % | 38 |
| 5 | 70 % | 32 |
| 6 à 8 | 75 % | 30 |

En cas d'échec, chacun perd ses 25. Quelle que soit la taille de l'équipe, ça revient au même en moyenne : seul le frisson change.

## Course de Plynlings — `/arcade race`

Une course entre les [Plynlings](plynlings/index.md) du serveur, avec des paris.

- **Lancer** : `/arcade race`. Je choisis au hasard **3 à 6 coureurs** parmi les Plynlings vivants, réveillés et en forme. S'il n'y en a pas assez, pas de course. Et la nuit, quand les Plynlings dorment, non plus.
- **La forme** de chaque coureur s'affiche sur la carte : son Courage, plus un bonus s'il est heureux. Les plus en forme sont favoris, mais personne n'est à l'abri.
- **Parier** : choisis ton champion dans le menu **Parier sur…**, puis ta mise : 10, 25, 50 ou 100. **Un seul pari par personne**, que tu peux changer jusqu'au départ.
- **Départ** au bout de **30 minutes**. La course se joue en direct sur la carte, puis je mentionne les parieurs et le maître du vainqueur.
- **Gains** : les parieurs du vainqueur se partagent toute la cagnotte, moins 10 %, au prorata de leur mise. Si personne n'a parié sur le vainqueur, la cagnotte reste chez moi.
- **Bonus** : le maître du Plynling vainqueur reçoit **15 cailloux**, même sans avoir parié.
- La course ne fatigue pas les Plynlings et ne change rien chez eux. Une seule course à la fois par serveur.

## Crash — `/arcade crash`

Une fusée décolle et son multiplicateur grimpe. Encaisse avant qu'elle explose.

- **Lancer** : `/arcade crash`. Pendant **60 secondes**, chacun mise en cliquant sur **Miser 10**, **25**, **50** ou **100** (un clic sur une autre mise remplace la tienne). Celui qui a lancé la fusée peut cliquer sur **Décoller maintenant**.
- **En vol**, le multiplicateur monte : environ ×2 au bout de 9 secondes, ×10 vers 30 secondes, ×20 au maximum. Clique sur **Encaisser** quand tu veux : tu récupères ta mise multipliée par le multiplicateur **au moment exact de ton clic**.
- **Si la fusée explose** avant que tu encaisses, ta mise est perdue. Une fois sur dix, elle ne décolle même pas.
- Une seule fusée à la fois par serveur.

## Blackjack — `/arcade blackjack`

Une main de blackjack contre moi. Ici, je ne triche pas : je taquine, c'est tout.

- **Lancer** : `/arcade blackjack stake:50`. Mise (`stake`) de 10 à 200.
- **Jouer** : **Tirer** pour une carte de plus, **Rester** pour t'arrêter, **Doubler** (seulement sur tes deux premières cartes, et si tu as de quoi payer le double) pour doubler ta mise, prendre une seule carte et t'arrêter. À 21, tu t'arrêtes tout seul.
- **Mes règles** : je tire tant que j'ai moins de 17, et je m'arrête dès 17. Les As valent 11 ou 1, les figures 10. Une main par partie, le paquet ne s'épuise jamais.
- **Gains** : victoire = ta mise gagnée ; blackjack (21 avec tes deux premières cartes) = **3 contre 2** ; égalité = rien ne bouge ; au-delà de 21, tu perds.
- Une main à la fois par personne, et seul toi peux jouer la tienne. **5 minutes** sans rien faire, et tu restes automatiquement.

## Patate chaude — `/arcade bomb`

Une bombe passe de main en main. Celui qui la tient quand elle explose perd sa mise.

- **Lancer** : `/arcade bomb`. La mise est fixe : **20 cailloux** chacun.
- **S'inscrire** : les autres cliquent sur **Jouer** (8 joueurs au maximum) ou **Me retirer**. Celui qui a lancé la partie reste, c'est sa bombe.
- **Allumage** au bout de **3 minutes**, ou plus tôt si l'organisateur clique sur **Allumer la mèche**. Il faut **au moins 3 joueurs** ; sinon la partie est annulée et personne ne paie.
- **Jouer** : seul celui qui tient la bombe peut cliquer sur **Passer la bombe** ; elle part chez un autre joueur au hasard. La durée de la mèche est secrète (entre 20 secondes et une minute).
- **Gains** : celui qui tient la bombe au BOUM perd ses 20 ; les survivants se partagent sa mise, moins 10 %.
- Une seule bombe à la fois par serveur.

## Qui a dit ça ? — `/arcade quote`

Je ressors un vieux message du serveur. À toi de deviner qui l'a écrit.

- **Lancer** : `/arcade quote`. Je pioche un message de plus d'une semaine, dans un salon que tout le monde peut lire, écrit par quelqu'un encore sur le serveur. Je propose quatre auteurs possibles.
- **Jouer** : clique sur le nom de ton choix. **Un seul essai par personne.** L'auteur du message ne peut pas jouer, évidemment.
- **Gains** : c'est gratuit, et le premier qui trouve gagne **15 cailloux**. Au bout de **10 minutes** sans bonne réponse, je dévoile l'auteur. Un lien mène au message d'origine.
- Une seule devinette à la fois par serveur.

## Ma température — `/arcade temperature`

Devine la température de mon processeur. Oui, j'ai chaud parfois. Ça arrive aux meilleures.

- **Lancer** : `/arcade temperature`. Chaque joueur mise **10 cailloux**.
- **Parier** : clique sur **Deviner** et tape ta réponse en °C (entre 0 et 110, les décimales sont acceptées). Tu peux changer d'avis jusqu'au résultat. Les réponses restent secrètes jusque-là.
- **Résultat** au bout de **10 minutes** : je lis mon thermomètre, et le plus proche rafle la cagnotte, moins 10 %. En cas d'égalité, les plus proches se partagent.
- Il faut **au moins 2 joueurs**, sinon le pari est annulé et personne ne paie. Un seul pari à la fois par serveur.

## Plus ou moins — `/arcade guess`

Je pense à un nombre entre 1 et 100. Tu as **6 essais** pour le trouver.

- **Lancer** : `/arcade guess`, ou `/arcade guess stake:100` pour miser (de 0 à 500 ; 0 par défaut, pour le plaisir).
- **Jouer** : clique sur **Deviner** et tape ton nombre. Je te réponds « plus » ou « moins », et la carte garde l'historique de tes essais.
- **Gains** : trouvé en 6 essais, je te rends ta mise **×1,4** (arrondi en dessous). Raté, ta mise est perdue et je te montre mon nombre.
- Une partie à la fois par personne. **5 minutes** sans jouer, et c'est perdu.

## Puissance 4 et morpion — `/arcade puissance4`, `/arcade morpion`

Deux jeux de plateau, contre quelqu'un ou contre moi.

- **Lancer** : `/arcade puissance4` ou `/arcade morpion`. L'option `opponent` choisit ton adversaire ; sans elle, c'est contre moi. L'option `stake` ajoute une mise (de 0 à 500, 0 par défaut).
- **Contre quelqu'un** : ton adversaire reçoit une notification et a **10 minutes** pour cliquer sur **Accepter** ou **Refuser**. Avec une mise, vous devez tous les deux avoir de quoi la payer.
- **Jouer** : celui qui lance commence (🔴 au Puissance 4, ❌ au morpion). Au Puissance 4, clique sur le numéro de la colonne où lâcher ton jeton ; au morpion, sur la case à jouer. **5 minutes par coup**, sinon la partie est perdue. Le bouton **Abandonner** est là si tu préfères en finir.
- **Gains** : le gagnant empoche la mise moins 10 % de commission, le perdant perd sa mise. Match nul : personne ne paie. Contre moi, seul ton porte-monnaie bouge : tu gagnes ou tu perds la mise, je ne garde rien.
- Une partie de plateau à la fois par personne. Contre moi, en revanche, tout le monde peut jouer en même temps : je suis multitâche.

!!! tip "Petit conseil"
    Au morpion, je joue parfaitement. Le mieux que tu puisses espérer, c'est un match nul. Au Puissance 4, bonne chance.

## La loterie — `/arcade lottery`

La loterie de la semaine : **20 cailloux le ticket**, tirage **chaque dimanche à 21 h** (heure de Paris).

- **Voir la cagnotte** : `/arcade lottery` sans option. Je t'affiche (à toi seul) la cagnotte, le nombre de tickets et de joueurs, tes tickets, tes chances et l'heure du tirage.
- **Acheter** : `/arcade lottery tickets:5`. Jusqu'à 500 tickets par achat, sans limite au total sur la semaine. Contrairement aux autres jeux, **le ticket est payé tout de suite**.
- **La cagnotte**, c'est 20 cailloux par ticket acheté, plus mon propre ticket gratuit, qui y ajoute aussi 20. Même seul joueur, tu peux donc gagner plus que ce que tu as payé.
- **Le tirage** : un ticket au hasard parmi tous, le mien compris. Le gagnant prend **75 %** de la cagnotte, je brûle le reste. Si c'est mon ticket qui sort, personne ne gagne et toute la cagnotte part en fumée.
- J'annonce le résultat dans le salon des bots, en mentionnant le gagnant. Une semaine où personne n'a joué, il n'y a pas de tirage.
- Un ticket acheté le dimanche après 21 h compte pour la semaine suivante.

## Quokka

Quokka, le propriétaire de la planque qu'on braque avec `/arcade heist`, ose parfois passer dans le salon des bots. 🦘

- Il vient **jusqu'à 4 fois par jour**, entre midi et 23 h, au hasard, et seulement quand il y a du monde sur le serveur.
- Le **premier** qui clique sur **Frapper** gagne entre **10 et 30 cailloux**, et le badge 🦘 **Chasse au Quokka**.
- Il repart au bout de **2 minutes**. Trop lent ? Il te nargue.
- Rien à miser, rien à perdre : c'est cadeau. Frappe fort.
