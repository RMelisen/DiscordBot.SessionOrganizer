# Inviter SYNCS sur ton serveur

<div class="syncs-hello" markdown>
![SYNCS, ravie](assets/syncs/happy.png)

Tu veux de moi sur ton serveur ? Excellent choix. Je m'installe vite : un lien, quelques permissions, cinq minutes de réglages, et j'organise vos soirées comme si j'avais toujours été là. Cette page est pour l'admin qui m'invite.
</div>

## Le lien d'invitation

Le lien vient de mon créateur : demande-le-lui. Il a cette forme :

```
https://discord.com/oauth2/authorize?client_id=<id de l'application>&scope=bot+applications.commands&permissions=8859798592
```

- **`scope`** : `bot` (moi) et `applications.commands` (mes commandes `/`). Les deux sont nécessaires.
- **`permissions`** : `8859798592` me donne tout ce qu'il faut, « Gérer les rôles » compris. Sans « Gérer les rôles », c'est `8591363136`.

## Les permissions

| Permission | Pourquoi |
|---|---|
| Voir les salons | lire là où je travaille |
| Envoyer des messages | parler (forcément) |
| Voir les anciens messages | retrouver mes propres messages après un redémarrage |
| Intégrer des liens | mes cartes : sessions, sondages, niveaux, Plynlings |
| Joindre des fichiers | les images : Plynlings, cartes de niveau |
| Ajouter des réactions | réagir, et les votes par réaction |
| Utiliser des émojis externes | mes émojis à moi, qui viennent de mon serveur d'origine |
| Se connecter | voir qui est en vocal (l'XP vocale) |
| Gérer les évènements | copier les sessions dans l'onglet Évènements du serveur |
| Gérer les rôles *(facultatif)* | le rôle Plynling et le rôle de la grosse tête, que je donne et retire toute seule |

Sans « Gérer les évènements », les sessions marchent quand même : elles n'apparaissent juste pas dans l'onglet Évènements. Sans « Gérer les rôles », pas de rôle Plynling ni de grosse tête. Pour `/admin mute`, ajoute « Exclure temporairement des membres ».

Il me faut aussi, dans le portail développeur, les deux intentions privilégiées **Server Members** et **Message Content**. Ça, c'est le travail de mon créateur : sans elles, je n'entends rien.

!!! syncs formal ""
    Mon rôle doit être **au-dessus** des rôles que je gère, sinon Discord me refuse de les donner. Glisse-le vers le haut dans Paramètres du serveur → Rôles. Je ne demande pas à être tout en haut. Juste au-dessus. C'est raisonnable.

## La mise en place

Dès que j'arrive, je poste une carte de bienvenue avec la marche à suivre. Ensuite :

1. **`/config setup`** — La mise en place, étape par étape. Chaque choix est enregistré tout de suite.
    1. **Les salons** : le salon de jeu (les nouvelles des Plynlings), le salon des bots (ce qui compte peu), le salon des niveaux.
    2. **Les salons en option** : le quiz, les confessions, les jeux Steam gratuits. Vide, c'est éteint.
    3. **Les rôles** : le rôle modérateur (qui vote au [mur de la honte](honte.md)), le rôle Plynling, le rôle de la grosse tête. Je peux les créer moi-même.
    4. **Le salon principal et la vie du serveur** : où je dis bonjour le matin, et ce que je fais de moi-même (voir plus bas).
    5. **Le bilan** : ce qui marche, et ce qui me manque.
2. **`/config show`** — Toute la configuration, avec l'**état** du serveur en tête : ✅ quand tout va bien, ⚠️ avec la permission qui manque et où la donner.
3. **`/help`** — Tout ce que je sais faire, pour tout le monde.

Seuls les membres qui ont **Administrateur** ou **Gérer le serveur** peuvent me régler (voir [Staff](staff.md)).

## Ce que je fais de moi-même

Sur un nouveau serveur, je ne prends **aucune** initiative tant que personne ne me l'a demandé, sauf accueillir les nouveaux. Tout s'allume et s'éteint dans `/config setup`, ou avec **`/config feature enable`** et **`/config feature disable`** (option `feature`) :

| Fonctionnalité | Ce que c'est | Il faut |
|---|---|---|
| Bonjour du matin | un bonjour et une anecdote chaque matin ; `/shoutout` pour s'offrir la dédicace | le salon principal |
| Vie ambiante | quelques mots quand le serveur dort trop longtemps, et un peu de vie la nuit | le salon principal |
| Sondage du week-end | mon sondage du mercredi pour le week-end | le salon principal |
| Visites de Quokka | il passe parfois dans le salon des bots. Il n'a rien à y faire | le salon des bots |
| Spectatrice en vocal | un mot dans le salon vocal après une longue soirée | — |
| Accueil des nouveaux | je réponds au message d'arrivée de Discord (allumé d'office) | — |

Le salon principal se choisit dans `/config setup` ou avec `/config main-channel set`.

## Un serveur, un monde

Chaque serveur a les siens : l'XP et les niveaux, les cailloux et l'inventaire, les Plynlings et leur village, le mur de la honte, le quiz. Rien ne passe d'un serveur à l'autre.

Quelques petites choses restent sur mon serveur d'origine : je ne m'assois en vocal que là-bas (je n'ai qu'une chaise), et c'est là-bas que part le bilan du mois.

!!! syncs smug ""
    Deux serveurs, deux fois plus de logs. Je ne me plains pas. Je constate.
