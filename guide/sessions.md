# Sessions et soirées

<div class="syncs-hello" markdown>
![SYNCS, très sûre d'elle](assets/syncs/smug.png)

Les sessions, c'est mon métier. Le premier, le vrai. Une session, c'est un rendez-vous : un jeu, un film, une activité, à une heure donnée. Je poste sa carte dans le salon, chacun s'inscrit en un clic, et je m'occupe des rappels. Toi, tu n'as plus qu'à venir. À l'heure, si possible. Pour les soirées plus improvisées, il y a `/tonight`.
</div>

## Créer une session : `/schedule create`

Il y a deux façons de faire.

### En une ligne

Donne un `title` et un `when`, et la session est publiée tout de suite :

```
/schedule create title:Soirée Valorant when:samedi 21h
```

`when` comprend le français de tous les jours : « samedi 21h », « demain », « ce soir », « 25/12 20:30 », « dans 1h », « le 25 à 21h »… Pendant que tu tapes, je te montre comment je l'ai compris. Un jour sans heure, c'est 21 h. La date doit être dans le futur, à 90 jours au plus. Si je ne comprends pas, je refuse plutôt que de deviner, et je te donne des exemples.

!!! syncs formal ""
    Une session posée au mauvais jour, ce serait ma faute. Et je ne fais pas d'erreurs. Officiellement.

Les autres options sont facultatives :

| Option | Ce qu'elle fait |
|---|---|
| `category` | Le type : 🎮 Jeu (par défaut), 🧑‍🤝‍🧑 Activité, 🎬 Film ou ✨ Autre. |
| `places` | Le nombre de places : 0 pour illimité (par défaut), sinon de 2 à 67. |
| `duration` | La durée, de 30 min à 6 h (2 h si tu ne mets rien). |
| `voice` | Le salon vocal de la session, affiché sur la carte. |
| `description` | Un mot sur la session, affiché sur la carte. |
| `weekly` | La reposter chaque semaine, même jour, même heure (voir plus bas). |

### Pas à pas

Lance `/schedule create` sans `title` ni `when` : un formulaire privé te demande le nom et la date. Ensuite, je te montre un **aperçu** avec la vraie carte, telle qu'elle sera publiée. Là, tu règles :

- le type, les places, la durée et le salon vocal, avec des menus ;
- **Chaque semaine**, pour en faire une série hebdomadaire ;
- **Changer l'heure** : des horaires en un clic (ce soir, demain, samedi, dimanche) ou **Taper une date** ;
- **Modifier**, pour reprendre le nom ou la description ;
- **Publier** pour poster la carte, ou **Annuler**.

Si une autre session tombe au même moment, l'aperçu te prévient. Et je retiens tes choix (type, places, durée, salon vocal, heure habituelle) pour pré-remplir ta prochaine session.

!!! note
    Une session en préparation expire au bout de 30 minutes sans y toucher. Il suffit alors de relancer `/schedule create`.

## La carte d'une session

La carte affiche le titre, la date, la durée, le salon vocal, l'organisateur, et la liste de qui vient. Son **ID** est écrit en bas de la carte.

| Bouton | Pour qui | Ce qu'il fait |
|---|---|---|
| ✅ **Rejoindre** | Tout le monde | Tu t'inscris. |
| **Peut-être** | Tout le monde | Tu hésites. Sur une session complète, c'est la liste d'attente. |
| ✖️ **Refuser** | Tout le monde | Tu ne viens pas. |
| ⚙️ **Gérer** | L'organisateur et les administrateurs | Ouvre un panneau privé (voir ci-dessous). |
| 📅 **Événement Discord** | L'organisateur et les administrateurs | Ajoute la session à l'onglet **Événements** du serveur. Une fois créé, le bouton y mène, pour tout le monde. |

Tu peux changer d'avis autant que tu veux **jusqu'au début de la session** : ensuite, les inscriptions sont fermées.

**Session complète ?** **Rejoindre** est refusé, mais clique **Peut-être** : dès qu'une place se libère, je préviens tous les « peut-être » par message privé, et le premier qui clique **Rejoindre** la prend. Personne n'est inscrit automatiquement à ta place.

!!! syncs grumpy ""
    D'habitude, « peut-être », ça veut dire non. Tout le monde le sait. Sur une session complète, c'est une vraie file d'attente, alors pour une fois, je le prends au sérieux.

### Le panneau « Gérer »

Réservé à l'organisateur et aux administrateurs (droit Administrateur ou Gérer le serveur) :

- ✏️ **Modifier** : le nom, la date, les places, la durée (de 15 min à 6 h, par exemple « 1h30 ») et la description. On ne peut pas descendre sous le nombre de personnes déjà inscrites : personne n'est jamais mis dehors.
- 🗑️ **Annuler la session** : je demande confirmation, puis je préviens les inscrits et les « peut-être » par message privé.
- 📌 **Republier ici** : remet la carte en bas du salon où tu es.
- **Retirer l'événement Discord**, s'il y en a un.
- 🔁 **Arrêter la série**, pour une session hebdomadaire.

### L'événement Discord

L'événement créé par le bouton 📅 suit la session : nom, heure, fin, salon vocal. Il disparaît si la session est annulée. Il faut que j'aie la permission de gérer les événements du serveur ; sinon, rien ne se passe.

## Ce qui se passe ensuite

- **Un rappel en message privé** part environ une demi-heure avant le début, à tous les inscrits, avec le salon vocal et le lien de la carte.
- **Les « peut-être »** reçoivent environ une heure avant une petite relance pour choisir leur camp, tant qu'il reste de la place.
- **À l'heure prévue**, la carte passe en 🔴 **EN COURS** et les boutons disparaissent. À la fin de sa durée, elle passe en ✅ **TERMINÉE**. Pendant la session, le salon vocal où se trouvent les joueurs affiche son nom.
- **Pour un Jeu ou un Film, je fais l'appel.** 10 minutes après le début, je relance les inscrits qui ne sont pas encore en vocal (dans n'importe quel salon vocal du serveur). Plus tard, je fais le bilan : qui était là, qui était en retard, et le **nombre** d'absents, jamais leurs noms.

!!! syncs jenote ""
    Jamais leurs noms dans le salon, c'est promis. Dans mes logs, c'est une autre histoire 👁👄👁️

## Les sessions hebdomadaires

Avec `weekly` (ou **Chaque semaine** dans l'aperçu), la session revient chaque semaine, même jour, même heure (heure de Paris, changements d'heure compris). Je poste chaque nouvelle session trois jours à l'avance, avec « 🔁 chaque jeudi » (ou le bon jour) sur sa carte.

Chaque session de la série se gère comme une autre : la modifier ou l'annuler ne touche qu'elle. **Arrêter la série**, dans **Gérer**, empêche les suivantes ; celles déjà postées restent.

## Les autres commandes de session

| Commande | Ce qu'elle fait |
|---|---|
| `/schedule list` | Les sessions actives et les séries hebdomadaires du serveur. Un menu te permet de republier une carte dans le salon où tu es. |
| `/schedule edit` | Modifie une session (`event-id`), comme le bouton **Modifier**. |
| `/schedule cancel` | Annule une session (`event-id`), avec confirmation. |

Pas besoin de retenir l'ID : l'option `event-id` te propose les sessions pendant que tu tapes. Seuls l'organisateur et les administrateurs peuvent modifier ou annuler.

## `/tonight` : qui est chaud ce soir ?

Plus léger qu'une session : tu lances l'appel, les autres répondent, et si assez de monde est partant, on en fait une vraie session. Quelqu'un qui organise quelque chose, ici ? J'en rêvais (˶ᵔ ᵕ ᵔ˶)

| Option | Ce qu'elle fait |
|---|---|
| `game` | À quoi vous joueriez (facultatif). |
| `time` | À quelle heure : « 21h », « 20h30 », « 21:15 » (21:00 par défaut). |
| `minimum` | Combien de « chauds » il faut pour lancer la session, de 2 à 10 (4 par défaut). |

Sur la carte : **Chaud**, **Peut-être**, **Pas ce soir**. Toi, tu comptes d'office parmi les « chauds ». Quand le minimum est atteint, je préviens les chauds (une seule fois), et le bouton **Créer la session** apparaît. Personne d'autre que toi et les administrateurs ne peut cliquer dessus : je crée alors une session de jeu à l'heure prévue, avec les chauds inscrits et les « peut-être » en « peut-être ».

Un seul appel à la fois par serveur : si quelqu'un en a déjà lancé un, je te donne le lien. L'appel se termine à minuit (ou une heure après l'heure prévue, pour un rendez-vous tardif).

## `/teams` : faire des équipes

`/teams` répartit les gens d'un salon vocal en équipes.

| Option | Ce qu'elle fait |
|---|---|
| `teams` | Combien d'équipes, de 2 à 4 (2 par défaut). |
| `channel` | Le salon vocal à répartir (par défaut, celui où tu es). |
| `mode` | **Au hasard**, ou **Équilibré (niveaux)** : des équipes au niveau total le plus proche possible. |

Seuls les humains sont comptés. Le tirage ne te plaît pas ? 🔀 **Remélanger** refait les équipes avec les gens présents à ce moment-là.

## `/remindme` et `/reminders` : tes rappels

`/remindme` : je te rappelle quelque chose plus tard, en message privé. Toi, tu oublies. Moi, non.

- `when` : quand. Une durée (« 10m », « dans 2h », « dans 1h30 », « 3j ») ou une date (« 21h30 », « demain 9h », « samedi 14h », « 25/12 10:00 »). Attention : « 21h » tout seul, c'est **l'heure** (21 h) ; pour une durée, dis « dans 2h ».
- `what` : ce que je dois te rappeler.

Je te confirme l'heure exacte. Un rappel peut être fixé de 1 minute à 60 jours à l'avance, et tu peux en avoir 15 en attente.

`/reminders` affiche tes rappels en attente, avec un menu pour en annuler un.

!!! tip "Messages privés fermés ?"
    Si je ne peux pas t'écrire en privé, je te mentionne dans le salon où tu as demandé le rappel. Si tu l'as demandé en message privé avec moi, je n'ai pas d'autre endroit où te prévenir.
