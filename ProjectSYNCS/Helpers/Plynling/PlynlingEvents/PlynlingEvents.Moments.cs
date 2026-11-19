using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The four big moments (the ask and its answer), and the on-actions life brings. Set in Humeval
// (docs/village-lore.md) since 7.74.2; keys and options unchanged.
public static partial class PlynlingEvents
{
    private static EventDef[] Moments() => new[]
    {
        // ---- the four big moments: the ask (pulse, social) and its answer (response, on the other's card)
        new EventDef("social_declare", EventType.Pulse, Grown, "Le cœur qui bat",
            "Depuis quelque temps, {A} rougit chaque fois que {B} passe. Même de loin. Même de dos.",
            new[]
            {
                Ask("declare", "Tout avouer à {B}", "{A} prend son courage à deux mains et va tout dire à {B}.", "reply_declare",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, 1))),
                Plain("wait", "Garder ça pour soi encore un peu",
                    "{A} garde le secret, bien au chaud. Certaines choses mûrissent mieux à l'ombre.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.CanCouple),

        new EventDef("reply_declare", EventType.Response, Grown, "Une déclaration",
            "{B} est venu{b:|e} tout avouer à {A}, les joues rouges et la voix qui tremble.",
            new[]
            {
                Answer("yes", "Dire oui",
                    "{A} dit oui. {B} en oublie de respirer, puis rit, puis respire. Tout le hameau est au courant avant le soir : la Courge y a veillé.",
                    Stance.Accept, E(new Couple()), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Answer("no", "Dire non, doucement",
                    "{A} dit non, avec beaucoup de douceur. {B} hoche la tête et rentre par le chemin le plus long.",
                    Stance.Refuse, E(new Heartbreak()), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("social_rival", EventType.Pulse, Grown, "Le défi",
            "{B} s'est encore vanté{b:|e} d'être {b:le plus rapide|la plus rapide} du hameau. Devant tout le monde. Devant {A}.",
            new[]
            {
                Ask("challenge", "{b:Le|La} défier à la course jusqu'au Pont-Vieux", "{A} lance le défi, et {B} relève le menton.", "reply_rival",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Vengefulness, 1))),
                Plain("ignore", "Hausser les épaules",
                    "{A} hausse les épaules si haut que ça devient un exercice. Personne n'a gagné, mais personne n'a perdu.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("reply_rival", EventType.Response, Grown, "Le défi relevé",
            "{B} défie {A} à la course jusqu'au Pont-Vieux. Sur le parapet, le Fou compte déjà les foulées.",
            new[]
            {
                new EventOption("race", "Courir", "{A} gagne d'un chapeau. Le Fou annonce le compte : trois foulées d'avance, et une de trop pour {B}.",
                    "{B} gagne d'un chapeau. {A} réclame une revanche avant même d'avoir repris son souffle.",
                    null, new EventChallenge(PlynlingStat.Courage, 0, VsTarget: true),
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(-5)), E(new AffinityShift(-10)),
                    NoStress, Ai((AiAxis.Boldness, 2)), Stance: Stance.Accept),
                Answer("decline", "Refuser, avec un sourire en coin",
                    "{A} refuse, et sourit en coin. {B} ne sait plus si c'est une victoire ou une défaite.",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("social_pact", EventType.Pulse, Grown, "Le pacte",
            "{A} et {B} ont partagé leur dernier gâteau, leur dernier secret et une averse entière sous la même feuille.",
            new[]
            {
                Ask("pact", "Proposer un pacte de meilleurs amis", "{A} propose un pacte, le petit doigt tendu.", "reply_pact",
                    Ai((AiAxis.Sociability, 2), (AiAxis.Honor, 1)), ownerOnly: false),
                Plain("enjoy", "Profiter de l'amitié telle quelle",
                    "{A} ne propose rien. Ce qui marche n'a pas besoin de contrat.",
                    E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond == PlynlingBond.Friends),

        new EventDef("reply_pact", EventType.Response, Grown, "Le petit doigt",
            "{B} tend le petit doigt à {A} : un pacte de meilleurs amis, pour la vie, ou au moins jusqu'à jeudi.",
            new[]
            {
                Answer("yes", "Serrer le petit doigt", "Les deux petits doigts se serrent. C'est officiel : le Fou, qui passait, sert de témoin, et l'écrit sur sa main.",
                    Stance.Accept, E(new SetAffinityAtLeast(PlynlingBonds.BestFriendsFrom + PlynlingBonds.BondMargin)), Ai((AiAxis.Sociability, 2))),
                Answer("later", "Dire « pas encore »", "{A} replie le doigt de {B}, gentiment. « Pas encore. Mais garde-le tendu. »",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("social_mend", EventType.Pulse, Grown, "Le rameau d'olivier",
            "{A} croise {B} au marché de la place. Les deux font semblant de ne pas se voir, avec beaucoup d'application.",
            new[]
            {
                Ask("mend", "Proposer de faire la paix", "{A} tend un rameau, un vrai, cueilli exprès.", "reply_mend",
                    Ai((AiAxis.Compassion, 2), (AiAxis.Vengefulness, -2)), ownerOnly: false),
                Plain("snub", "Continuer à faire semblant",
                    "{A} fait semblant si bien que {B} y croit presque. La Courge, elle, n'y croit pas du tout, et le raconte déjà.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Vengefulness, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("reply_mend", EventType.Response, Grown, "La paix ?",
            "{B} tend un rameau à {A}, cueilli exprès. Le marché entier retient son souffle.",
            new[]
            {
                Answer("yes", "Accepter le rameau", "{A} prend le rameau. On ne devient pas amis en un jour, mais on peut arrêter de se fâcher.",
                    Stance.Accept, E(new SetAffinityAtLeast(0)), Ai((AiAxis.Compassion, 2))),
                Answer("no", "Le laisser tomber", "Le rameau tombe dans la poussière. {B} le ramasse et repart avec, l'air digne.",
                    Stance.Refuse, E(new AffinityShift(-5)), Ai((AiAxis.Vengefulness, 2))),
            }),

        new EventDef("on_teen", EventType.Triggered, Teen, "Plus tout à fait bébé",
            "Un matin, {A} ne tient plus dans son coin de la cabane, chez la Mère Lichen. Quelque chose a changé : {T}, voilà ce que devient {A}. Ce soir, la Bruyère attend {A} à la Maison des Pousses.",
            new[]
            {
                Plain("proud", "Bomber le torse", "{A} se mesure contre la porte de la cabane, où la Mère Lichen garde la marque de chaque pousse, et grave un trait, très haut, en trichant un peu.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
                Plain("shy", "Faire comme si de rien n'était", "{A} reste encore un peu sur la pierre, à côté de la Mère Lichen. La Bruyère attendra l'après-midi. Grandir aussi.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Boldness, -1))),
            },
            Trigger: OnAction.BecameTeen),

        new EventDef("on_adult", EventType.Triggered, Grown, "Adulte, maintenant",
            "{A} a fini de grandir, et quitte la Maison des Pousses pour une petite maison à soi. Sur ses traits se lit maintenant un dernier trait de caractère : {T}.",
            new[]
            {
                Plain("party", "Fêter ça à l'auberge", "La Courge offre une part de tarte, dit « déjà, mon chou ? » trois fois, et le raconte à toute la salle.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1))),
                Plain("plan", "Faire des projets", "{A} sort un carnet et y écrit « projets ». Puis, en dessous : « à voir ».",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            },
            Trigger: OnAction.BecameAdult),

        new EventDef("on_visit_forgot", EventType.Triggered, AnyStage, "L'objet oublié",
            "Après la visite, {A} trouve un petit mouchoir brodé sous un coussin. C'est celui de {B}.",
            new[]
            {
                Plain("return", "Le rapporter tout de suite", "{A} court le rapporter. {B} ne s'était même pas rendu compte de l'oubli, et en est {b:touché|touchée}.",
                    E(new AffinityShift(5)), Ai((AiAxis.Honor, 1), (AiAxis.Energy, 1))),
                Plain("keep", "Le garder pour la prochaine fois", "{A} plie le mouchoir et le pose bien en vue. Une prochaine fois viendra bien.",
                    E(new FollowUp("follow_forgot", 24, 72)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone, Trigger: OnAction.AfterVisit),

        new EventDef("follow_forgot", EventType.FollowUp, AnyStage, "Le mouchoir brodé",
            "Le mouchoir de {B} attend toujours sur l'étagère de {A}. Le mouchoir commence à sentir la maison.",
            new[]
            {
                Plain("bring", "Le rendre, enfin", "{A} rend le mouchoir, plié avec soin, et une fleur glissée dedans. {B} fait comme si c'était prévu.",
                    E(new AffinityShift(10), new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Honor, 1))),
                Plain("adopt", "Garder le mouchoir pour de bon", "{A} adopte le mouchoir. {B} le reconnaîtra un jour, mais ce jour n'est pas aujourd'hui.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(-5)), Ai((AiAxis.Greed, 1))),
            },
            Target: TargetKind.Anyone),

        new EventDef("on_bereaved", EventType.Triggered, AnyStage, "Une place vide",
            "La place de {B} est vide, et le hameau est un peu plus grand sans. {A} ne sait pas trop quoi faire de ses mains.",
            new[]
            {
                Plain("remember", "Se souvenir des bons moments", "{A} raconte à voix haute leurs meilleures bêtises. La Mère Lichen écoute, sans rien dire, et pose sa main sur celle de {A}.",
                    E(new StressChange(-10)), Ai((AiAxis.Sociability, 1))),
                Plain("plant", "Planter une graine en sa mémoire", "{A} plante une graine près du Pont-Vieux. Au printemps, quelque chose poussera.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone, Trigger: OnAction.Bereaved),

        new EventDef("on_sick", EventType.Triggered, AnyStage, "Le nez qui coule",
            "{A} éternue, renifle, et décide que le monde est injuste.",
            new[]
            {
                Plain("tea", "Réclamer une tisane", "La Sauge envoie une tisane, imbuvable comme toujours, avec un mot : « Bois-la chaude. Et ne fais pas cette tête. »",
                    E(new StressChange(-5)), Ai((AiAxis.Sociability, 1))),
                Plain("brave", "Faire comme si de rien n'était", "{A} fait comme si de rien n'était, entre deux éternuements spectaculaires.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1))),
            },
            Trigger: OnAction.FellSick),

        new EventDef("on_recovered", EventType.Triggered, AnyStage, "De nouveau sur pied",
            "{A} se réveille, respire par le nez — par les deux narines — et se sent {a:neuf|neuve}.",
            new[]
            {
                Plain("run", "Faire le tour du hameau en courant", "{A} court partout, juste pour vérifier que tout marche encore. Tout marche.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Energy, 2))),
                Plain("thank", "Remercier ceux qui ont aidé", "{A} distribue des remerciements, et un pot de miel à la Sauge, pour changer.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1))),
            },
            Trigger: OnAction.Recovered),
    };
}
