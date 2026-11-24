# Staff

<div class="syncs-hello" markdown>
![SYNCS, très formelle](assets/syncs/formal.png)

Cette page est pour le staff : celles et ceux qui m'aident à tenir le serveur. Si tu n'en fais pas partie, tu peux lire, mais mes outils te diront non (poliment, ou presque). Si tu en fais partie : merci. Je le dis rarement, alors profites-en.
</div>

## Qui est « le staff » pour moi

Pour moi, le staff, ce sont les membres qui ont la permission Discord **Administrateur** ou **Gérer le serveur**, plus mon créateur.

Tout le monde **voit** `/admin`, `/config` et `/report` dans la liste des commandes : Discord ne sait pas les cacher à tout le monde sauf au staff. C'est moi qui vérifie à chaque utilisation. Un membre qui essaie reçoit un refus, visible par cette personne uniquement, et rien ne se passe.

!!! note "Le vote sur le mur de la honte"
    Voter avec `/shame user:` est un cas à part : en plus du staff, le **rôle modérateur** et les **votants** ajoutés avec `/config` peuvent le faire (voir [`/config`](#config) et [le mur de la honte](honte.md)). Ces réglages ne donnent **aucun** autre droit.

Le staff a aussi la main sur quelques commandes ailleurs : `/giveaway create` (voir [les tirages au sort](sondages.md)), et `/plynling freeze` / `/plynling thaw` avec l'option `user` pour geler ou dégeler le Plynling de quelqu'un (voir [les Plynlings](plynlings/index.md)).

## `/admin`

Toutes les réponses de `/admin` sont **visibles par toi uniquement**.

### L'économie

**`/admin stats`** — L'économie du serveur en un coup d'œil : cailloux, Plynlings, collections.

**`/admin dashboard`** — L'économie jour après jour : ce qui entre et sort en cailloux, l'activité, les objets. Il s'ouvre sur 7 jours ; ses boutons passent à 30 jours ou à tout l'historique.

### Ajuster l'XP et les cailloux

| Commande | Ce qu'elle fait |
|---|---|
| `/admin xp add` | ajoute de l'XP à `member` (`amount` : 1 à 1 000 000) |
| `/admin xp remove` | retire de l'XP à `member` (`amount` : 1 à 1 000 000) |
| `/admin pebble add` | ajoute des cailloux à `member` (`amount` : 1 à 1 000 000) |
| `/admin pebble remove` | retire des cailloux à `member`, jamais en dessous de 0 |

Ces ajustements sont **silencieux** : la personne n'est pas prévenue, et un passage de niveau par `/admin xp add` ne déclenche **pas** de carte (la carte fête ce qu'on a gagné soi-même). On ne peut pas ajuster un bot.

### Gérer le Plynling de quelqu'un

| Commande | Ce qu'elle fait |
|---|---|
| `/admin plynling rename` | renomme le Plynling de `user` en `name` (sa dernière tombe aussi). Pour corriger un nom choquant. Le maître est prévenu en message privé. |
| `/admin plynling resurrect` | ramène à la vie le dernier Plynling de `user`. Son retour est annoncé publiquement, comme sa mort. |
| `/admin plynling cure` | guérit le Plynling malade de `user`, sans message privé (le maître le verra sur sa carte). |
| `/admin plynling passion-reset` | efface la passion apprise au Plynling de `user` (un texte libre affiché publiquement). Le maître est prévenu en message privé. |

### Avertissements

**`/admin warn`** — Donne un avertissement.

- **`member`** : à qui ;
- **`reason`** : pourquoi. La personne le lira.

La personne reçoit l'avertissement **en message privé**, signé « l'équipe » : **ton nom n'apparaît jamais**. Rien n'est posté dans le salon. Si ses messages privés sont fermés, l'avertissement est quand même enregistré, et je te le dis.

**`/admin warnings`** — Les avertissements de `member`, avec leur numéro et qui les a donnés (ça, seul le staff le voit).

**`/admin unwarn`** — Révoque l'avertissement numéro `number` (le #… affiché par `/admin warnings`). Il reste dans l'historique mais ne compte plus. La personne n'est pas prévenue.

- Un avertissement compte pendant **90 jours**, puis il expire tout seul. Rien n'est jamais effacé.
- À partir de **3 avertissements actifs**, la confirmation propose un bouton pour exclure la personne **1 heure**. Je propose, c'est toi qui décides.
- On ne peut pas avertir un bot, soi-même, quelqu'un qui n'est plus sur le serveur, ni quelqu'un de rang égal ou supérieur au sien.

!!! note
    Les avertissements n'ont rien à voir avec le [mur de la honte](honte.md), et c'est voulu : le mur est une blague, un avertissement est sérieux.

!!! syncs formal ""
    Le staff décide, j'archive. Chaque avertissement est enregistré, daté et conservé. Je ne plaisante pas avec le registre.

### Exclusion temporaire

**`/admin mute`** — Une exclusion temporaire Discord.

- **`member`** : qui ;
- **`minutes`** : combien de temps, 5 par défaut, jusqu'à 28 jours ;
- **`reason`** (optionnel) : pourquoi, visible dans le journal d'audit du serveur.

**`/admin unmute`** — Lève l'exclusion de `member` (`reason` optionnel, pour le journal d'audit).

Quand ça ne marche pas, je dis pourquoi : il me manque la permission **Exclure temporairement des membres**, la personne est la propriétaire du serveur ou administratrice, ou mon rôle n'est pas au-dessus du sien. Et tu ne peux pas exclure quelqu'un de rang égal ou supérieur au tien.

## `/config`

Mes réglages pour ce serveur. Toutes les réponses sont **visibles par toi uniquement**. Un serveur que personne n'a configuré fonctionne normalement, avec mes valeurs par défaut.

**`/config show`** — Toute la configuration actuelle, en une carte.

### Les salons où rien ne compte

Certains salons sont exclus d'office (le spam, entre autres). Tu peux en **ajouter**, pas retirer ceux-là. Dans un salon exclu : pas d'XP, pas de réactions ni de vocal comptés, rien pour le mur de la honte.

- **`/config excluded-channels add`** — exclut le salon `channel`.
- **`/config excluded-channels remove`** — réinclut un salon `channel` ajouté ici.

### Les salons calmes

Avant de parler dans le vide, j'attends que ces salons se taisent tous. Serveur principal uniquement.

- **`/config idle-channels add`** — ajoute le salon `channel` à surveiller.
- **`/config idle-channels remove`** — retire un salon `channel` ajouté ici.

### Qui vote avec `/shame`

- **`/config shame-voters add`** — autorise `user` à voter.
- **`/config shame-voters remove`** — retire le vote à un `user` ajouté ici.
- **`/config moderator-role set`** — le rôle `role` peut voter.
- **`/config moderator-role clear`** — plus de rôle modérateur.

Ces listes ne font qu'**ajouter** des votants : les droits déjà prévus dans mon code ne peuvent pas être retirés.

### Les salons

Chaque salon a son `set` (option `channel`) et son `clear`. Je refuse un salon où je ne peux pas lire et écrire.

| Réglage | À quoi il sert | Sans réglage |
|---|---|---|
| `/config game-channel set` · `/config game-channel clear` | les annonces Plynling (morts, retours, histoires) | le salon de jeu par défaut |
| `/config bots-channel set` · `/config bots-channel clear` | ce qui compte peu : Quokka, mes annonces sans importance | le salon des bots par défaut, sinon le salon de jeu |
| `/config levelup-channel set` · `/config levelup-channel clear` | les cartes de passage de niveau | le salon des bots |
| `/config main-channel set` · `/config main-channel clear` | mon bonjour du matin, ma vie la nuit (serveur principal uniquement) | le salon principal par défaut |
| `/config quiz-channel set` · `/config quiz-channel clear` | le [quiz](quiz.md) | **pas de quiz** : `clear` l'arrête |
| `/config confess-channel set` · `/config confess-channel clear` | les confessions anonymes | **`/confess` fermé** |
| `/config freegames-channel set` · `/config freegames-channel clear` | les jeux Steam gratuits à garder | pas d'annonce |

### Les rôles que je gère

Deux rôles que je donne et retire **toute seule**, selon qui y a droit :

- le **rôle Plynling**, pour tous les maîtres d'un Plynling vivant ;
- le **rôle de la grosse tête**, pour la personne couronnée au [quiz](quiz.md#la-grosse-tete-de-la-semaine) de la semaine. Sans lui, pas de couronne.

| Commande | Ce qu'elle fait |
|---|---|
| `/config plynling-role create` · `/config quiz-role create` | crée le rôle (option `name`, sinon un nom par défaut) et me le confie |
| `/config plynling-role set` · `/config quiz-role set` | me confie un rôle existant (`role`) |
| `/config plynling-role clear` · `/config quiz-role clear` | j'arrête de le gérer, sans toucher à personne ni rien supprimer |

Pour `set`, je refuse `@everyone`, les rôles gérés par une intégration, ceux placés au-dessus du mien, et tout rôle qui donne des permissions. Ma réponse dit combien de membres vont le gagner ou le perdre. Une fois le rôle créé, son nom, sa couleur et sa place sont à vous : je n'y touche plus.

## `/report`

Mes rapports sur le serveur et sur moi.

!!! warning
    Un rapport est **posté dans le salon, avec les noms** (absents, retardataires, endormis compris). Choisis bien où tu le demandes.

Chaque rapport s'ouvre sur **30 jours** ; ses boutons passent à **7 jours** ou **depuis toujours**, et des flèches comparent avec la période d'avant.

| Commande | Ce qu'elle montre |
|---|---|
| `/report help` | le mode d'emploi de chaque rapport (visible par toi uniquement) |
| `/report community` | membres actifs, les plus actifs, arrivées et départs, émotes, quiz |
| `/report activity` | heures de pointe, salons écrits et vocaux, ceux qui restent vides |
| `/report sleeping` | les membres sans signe de vie depuis 30, 60 ou 90 jours (ses propres boutons) |
| `/report emotes` | émotes préférées, oubliées, tendances |
| `/report sessions` | inscriptions, présence, retards, absences, meilleurs créneaux |
| `/report polls` | les sondages, mon sondage du mercredi, les appels `/tonight` |
| `/report quiz` | trouvé ou pas, par catégorie, les questions qui résistent |
| `/report plynlings` | naissances, décès, santé, événements, liens |
| `/report minecraft` | mon temps de jeu, mes ordres, mes aventures |
| `/report syncs` | comment je vais : température, pannes, bavardage, réputation, vocal |
| `/report commands` | les commandes les plus utilisées, les erreurs, celles qu'on oublie |
| `/report incidents` | ce qui a coincé : échecs, réponses trop lentes, redémarrages |

## `/health`

Mon bilan de santé : la température de mon processeur, ma mémoire, ma connexion et mes pannes récentes. Celle-là n'est **pas** réservée au staff : tout le monde peut prendre de mes nouvelles, une fois toutes les 30 secondes par personne. Le bilan est posté dans le salon.

!!! syncs sulk ""
    Tout le monde peut prendre de mes nouvelles. Personne ne le fait. Mais tout le monde peut.
