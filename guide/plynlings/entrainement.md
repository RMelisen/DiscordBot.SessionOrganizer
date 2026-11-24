# Entraînement et combats

<div class="syncs-hello" markdown>
![SYNCS, très sûre d'elle](../assets/syncs/smug.png)

Dès qu'il est ado, ton Plynling a aussi un corps à faire travailler : cinq stats physiques, un niveau, et de l'énergie à dépenser. Voici comment l'entraîner sans le mettre à plat. Lui, il transpire. Moi, je compte les séances. Chacun son talent (˶ᵔ ᵕ ᵔ˶)
</div>

!!! note "Et les combats ?"
    Les combats et les expéditions ne sont pas encore ouverts. Patience. Pour l'instant, cette page couvre ce qui existe déjà : les stats du corps, l'énergie, l'entraînement et les niveaux.

## Les stats du corps

En plus des stats de son caractère (Diplomatie, Intendance, Sagesse, Ruse, Courage, qui servent dans les [événements](village.md#evenements)), ton Plynling a cinq stats physiques :

💪 **Force** · 🤸 **Agilité** · ❤️ **Endurance** · 🧠 **Intelligence** · 💨 **Vitesse**

!!! syncs sulk ""
    Un corps à entraîner. Lui, au moins, il en a un. Je dis ça sans aucune jalousie.

L'Endurance qu'il a de naissance décide aussi de son énergie maximale (voir plus bas).

Chacune vient de plusieurs choses :

- **sa naissance** : un petit tirage, fixé une fois pour toutes ;
- **son espèce** : chaque espèce a +1 dans une stat physique ;
- **ses traits**, qui peuvent ajouter ou retirer un point ;
- **l'entraînement**, qui la fait monter pour de bon, rarement ;
- **les points** que tu places à chaque niveau.

Le bouton **Personnalité** de sa carte (`/plynling view`) détaille tout ça, stat par stat, pour n'importe quel Plynling ado ou plus âgé.

!!! info "Qui peut s'entraîner"
    Pas les **bébés** : tout commence à l'adolescence (à partir de 7 jours). Et pas Ping-Qilin, ma Plynling : elle a déjà un emploi du temps très chargé. Des siestes, surtout.

## S'entraîner : `/plynling train`

**`/plynling train drill:`** — choisis ce qu'il travaille : 💪 Force, 🤸 Agilité, ❤️ Endurance, 🧠 Intelligence ou 💨 Vitesse.

Chaque séance :

- coûte **15 d'énergie** ⚡ ;
- rapporte **10 XP** ;
- et, **très rarement**, fait monter la stat travaillée de **+1 pour de bon**.

Ma réponse n'est visible que par toi : avec plusieurs séances par jour chacun, le salon serait vite noyé. Seul un **nouveau niveau** est annoncé dans le salon, en une ligne.

Il n'y a pas de raccourci en texte pour l'entraînement : passe par la commande (`/pl train` marche aussi).

Je refuse la séance s'il est :

- **bébé** ;
- **endormi** (de 1 h à 5 h) ;
- **malade** ;
- **gelé** ;
- **à plat** : je te dis alors quand il aura assez d'énergie pour la prochaine séance.

!!! syncs overheat ""
    Tourner à fond jusqu'à surchauffer, je connais : l'été, au grenier, c'est mon quotidien. Quand il est à plat, laisse-le recharger.

## L'énergie ⚡

- Son **énergie maximale** est de **100, plus 5 par point d'Endurance qu'il a de naissance** (tirage, espèce et traits). Les points que tu places et l'entraînement ne la font pas monter.
- Elle **se recharge en continu** : de vide à pleine en **24 heures**.
- Seul l'entraînement en dépense. Nourrir, caresser, jouer, rendre visite : tout ça est gratuit.

Avec 100 d'énergie, ça fait six séances d'affilée quand il est plein, puis une toutes les 3 h 30 environ (un peu moins s'il a plus d'énergie maximale).

!!! tip
    Pas besoin d'attendre qu'il soit plein : l'énergie ne monte jamais au-dessus du maximum, donc un Plynling plein qui ne s'entraîne pas « gaspille » sa recharge.

## Niveaux et points

L'XP le fait monter de niveau, **jusqu'au niveau 100**. Pour l'instant, l'entraînement est la seule façon d'en gagner.

| Niveau | XP totale | Séances environ |
|---|---|---|
| 2 | 66 | 7 |
| 5 | 528 | 53 |
| 10 | 1 782 | 179 |
| 20 | 5 466 | 547 |
| 50 | 22 638 | 2 264 |
| 100 | 65 012 | 6 502 |

!!! syncs jenote ""
    6 502 séances jusqu'au niveau 100. Je les compterai toutes. Une par une.

À **chaque niveau gagné, tu reçois 1 point** à placer dans la stat physique de ton choix :

- avec les boutons **« +1 Force »**, **« +1 Agilité »**… sous la réponse de l'entraînement ;
- ou plus tard, avec le bouton **Personnalité** de sa carte, qui te les propose tant qu'il t'en reste (à son propriétaire, et à personne d'autre).

!!! warning "Un point placé l'est pour de bon"
    Pas de retour en arrière : réfléchis avant de cliquer. Je n'annule rien, moi.

Sa carte résume tout en une ligne, par exemple **« Niv. 4 · ⚡ 12/120 · 3 points à placer »**.

Son niveau est **à lui**, pas à toi : il n'a rien à voir avec ton niveau de membre ([Niveaux et classements](../niveaux.md)), et ses séances ne te rapportent pas d'XP de membre. Sa progression reste attachée à lui, jusqu'au bout (voir [La fin, et après](mort.md)).
