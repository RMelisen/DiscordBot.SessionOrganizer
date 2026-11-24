# Questions fréquentes

Les questions qu'on me pose le plus souvent. Enfin, qu'on me poserait, si on lisait les messages d'erreur.

## Pourquoi je ne gagne pas d'XP dans ce salon ?

Parce que le staff l'a exclu : dans certains salons, rien ne compte, ni l'XP ni le mur de la honte. Les administrateurs gèrent cette liste avec `/config excluded-channels add` et `/config excluded-channels remove`, et `/config show` l'affiche (voir [Staff](staff.md)). Un fil compte comme le salon dont il dépend.

## Pourquoi je ne gagne pas d'XP en vocal ?

Pour gagner de l'XP en vocal, il faut :

- être **accompagné** d'au moins une autre personne qui remplit les mêmes conditions (les bots ne comptent pas) ;
- avoir le **micro ouvert** et **ne pas être en sourdine** (couper son micro ou son casque soi-même suffit à ne plus compter) ;
- ne pas être dans le salon **AFK** du serveur, qui ne rapporte rien.

Et le gain en vocal diminue au fil de la journée, avant de repartir à zéro chaque nuit. Tout est détaillé dans [Niveaux et classements](niveaux.md).

## J'ai cliqué sur un bouton et rien ne s'est passé.

Le plus souvent, ce que tu as cliqué n'est plus valable, et je te le dis dans un message que toi seul vois :

- une session a **commencé** ou a été **annulée** : les inscriptions sont fermées ;
- un sondage ou un vote est **clôturé** ;
- une session ou un sondage **en préparation** expire au bout de 30 minutes sans y toucher : relance la commande ;
- un `/wouldyourather` est perdu si je redémarre pendant qu'il est ouvert.

Si Discord affiche « Échec de l'interaction », j'étais sans doute en train de redémarrer ou un peu lente : attends quelques secondes et réessaie. Sur une session, pas de risque : un deuxième clic ne t'inscrit jamais deux fois. Sur un sondage, vérifie d'abord avec 🗳️ **Mes votes**, car recliquer sur un choix retire ton vote.

## Comment je gagne des cailloux ?

- `/work`, toutes les 4 heures ;
- `/daily`, une fois par jour : la récompense grandit chaque jour d'affilée ;
- en parlant et en étant actif : chaque gain d'XP rapporte quelques cailloux, avec un plafond par jour ;
- au [quiz](quiz.md) et à l'[arcade](arcade.md).

Le détail, et comment les dépenser : [Cailloux et objets](cailloux.md).

## Je peux modifier ou annuler la session de quelqu'un d'autre ?

Non, sauf si tu es administrateur (droit Administrateur ou Gérer le serveur). Une session se gère par son organisateur, avec ⚙️ **Gérer** sur sa carte, `/schedule edit` ou `/schedule cancel`. C'est la même règle pour les sondages, les votes et les tirages : leur organisateur ou un administrateur.

## La session est complète. Je fais comment ?

Clique **Peut-être** : sur une session complète, c'est la liste d'attente. Dès qu'une place se libère, je préviens tous les « peut-être » en message privé, et le premier qui clique **Rejoindre** la prend. Voir [Sessions et soirées](sessions.md).

## Où je retrouve l'ID d'une session ou d'un sondage ?

En bas de sa carte. Mais tu n'en as presque jamais besoin : dans `/schedule edit`, `/schedule cancel`, `/when delete` et `/vote delete`, l'option te propose la liste pendant que tu tapes.

## Comment voir ou annuler mes rappels ?

`/reminders` affiche tes rappels en attente, avec un menu pour en annuler un. Tu peux en avoir 15 à la fois. Pour en créer un : `/remindme` (voir [Sessions et soirées](sessions.md)).

## Mes messages privés sont fermés. Qu'est-ce que je rate ?

Je préviens beaucoup de choses en privé : le rappel avant une session, la relance des « peut-être », l'annulation d'une session, la place libérée sur une session complète. Si je ne peux pas t'écrire, je laisse tomber sans rien dire : tu ne recevras rien.

Seule exception : un rappel `/remindme` demandé sur le serveur. Si je ne peux pas te l'envoyer en privé, je te mentionne dans le salon où tu l'as demandé.

## Pourquoi je ne peux pas lancer de tirage au sort ?

`/giveaway create` est réservé au staff. Pour tout le reste (sessions, sondages, votes, `/tonight`), n'importe qui peut lancer le sien.

## Mon sondage se termine quand ?

À l'heure affichée en bas de sa carte : 2 jours après sa publication si tu n'as rien choisi avec l'option `ends`. Il se clôture tout seul, et je réponds sous la carte avec le résultat. Tu peux le prolonger ou le clôturer plus tôt avec ⚙️ **Gérer**. Voir [Sondages, votes et tirages](sondages.md).

## Est-ce que tes commandes marchent en message privé ?

Quelques-unes, oui : par exemple `/help`, `/remindme`, `/reminders`, `/yesno`, `/rate` et `/ship` (sans le tirage au sort). Tout ce qui touche au serveur (sessions, sondages, niveaux…) se fait sur le serveur.

## Il y a un mode d'emploi plus court ?

`/help`, directement dans Discord : la liste de toutes mes commandes, en trois pages, visible par toi seul.
