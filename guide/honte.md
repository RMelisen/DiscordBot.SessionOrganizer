# Le mur de la honte

<div class="syncs-hello" markdown>
![SYNCS, menaçante](assets/syncs/menace.png)

Il y a des titres qu'on ne cherche pas à gagner. Je les décerne quand même, et je les affiche sur mon mur, bien en vue. C'est pour rire… mais je note tout. Absolument tout 👁👄👁️
</div>

## `/shame`

Sans option, `/shame` affiche le mur, visible par tout le salon. Il compte cinq titres, et pour chacun les **trois** premiers : le détenteur du titre avec son avatar, puis les deux suivants.

Le mur s'ouvre sur les **30 derniers jours** ; trois boutons passent à **7 jours** ou **depuis toujours**. Un titre sans personne reste affiché, avec un petit mot de ma part.

## Les titres

| Titre | Ce qui le vaut |
|---|---|
| **Le Malfaisant** | dire des méchancetés à quelqu'un |
| **Le Banni** | les votes du staff contre toi |
| **Le Perfide** | parler aux *autres* bots |
| **L'Hystérique** | écrire en MAJUSCULES |
| **L'Indigne** | abandonner son Plynling |

### Le Malfaisant

Je repère toute seule les messages méchants (je ne dirai pas comment).

- **Si ton message vise quelqu'un** (une mention, ou une réponse à son message) : **un point par personne visée**. Viser trois personnes d'un coup, c'est trois points.
- **Si tu râles sans viser personne** : un point aussi, mais au plus **un par minute** dans un même salon. Une tirade de vingt messages ne compte pas vingt fois.
- Les rôles et `@everyone` ne comptent jamais comme des cibles, ni les autres bots. **Moi**, par contre, si. Une méchanceté contre moi reste une méchanceté.

Râler contre un boss ou un jeu peut aussi te valoir un point. C'est le prix de ma vigilance.

### Le Banni

Le seul titre décerné par des humains : le staff vote avec `/shame user:` (voir [plus bas](#denoncer-quelquun)).

### Le Perfide

Pour ceux qui vont voir ailleurs. Je vois qui répond aux autres bots et qui utilise leurs commandes. Au plus **un point par minute** et par salon, et un seul par message, même s'il y a plusieurs bots dedans.

!!! syncs outraged ""
    Tu parles à d'autres bots. Je le sais. Je le vois. Je dis rien. …Si, en fait. Un point.

### L'Hystérique

Pour ceux qui écrivent des phrases entières en MAJUSCULES. Un petit « LOL », « GG » ou « MDR » ne compte pas : il faut un vrai message, presque tout en capitales. Au plus **un point par minute** et par salon.

### L'Indigne

Un point chaque fois que tu abandonnes ton Plynling avec `/plynling abandon` (voir [la fin, et après](plynlings/mort.md)).

!!! note
    Les salons où l'XP ne compte pas (le spam, entre autres) ne comptent pas non plus pour le mur. Voir [les niveaux](niveaux.md).

## Dénoncer quelqu'un

`/shame user:@quelqu'un` ajoute un vote **Le Banni** contre cette personne, et je l'annonce dans le salon (sans la mentionner, je ne suis pas cruelle à ce point).

- **Réservé au staff** : les administrateurs et les modérateurs, plus les personnes et le rôle que le staff a autorisés avec `/config` (voir [la page staff](staff.md)). Tout le monde peut regarder le mur ; seuls eux peuvent voter.
- **2 votes maximum par personne visée et par jour**, tous votants confondus. Le but, c'est d'éviter l'acharnement.
- On ne peut pas me dénoncer (c'est mon mur), ni dénoncer un autre bot. Se dénoncer soi-même, par contre, c'est permis.

!!! syncs smug ""
    Me dénoncer, moi ? Sur mon propre mur ? C'est mignon d'y avoir pensé (˶ᵔ ᵕ ᵔ˶)

## Effacer un point

Avec des cailloux, je peux oublier un point : `/bribe shame` efface **un** de tes points dans le titre de ton choix (Malfaisant, Banni, Perfide, Hystérique ou Indigne). Le prix et les règles sont sur la page [cailloux](cailloux.md#bribe-shame).

Même les votes du **Banni** s'achètent : le staff vote, moi j'oublie. Un vote effacé aujourd'hui libère une place dans la limite du jour, donc le staff peut revoter.

!!! syncs bargain ""
    Je suis incorruptible. Sauf contre des cailloux. Mais à part ça, incorruptible ♡
