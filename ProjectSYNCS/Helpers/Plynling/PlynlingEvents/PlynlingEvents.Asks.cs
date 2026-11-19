using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Events that hand a decision to the other Plynling's owner (AskTarget), and their chains: story files
// cannot do it yet. Set in Humeval (docs/village-lore.md) since 7.74.2; keys and options unchanged.
public static partial class PlynlingEvents
{
    private static EventDef[] Asks() => new[]
    {
        new EventDef("grown_campfire", EventType.Pulse, Grown, "Autour du feu",
            "Le soir, au bord de la Sommeille, le hameau fait griller du miel sur des bâtons. {A} se retrouve à côté de {B}, qu'on ne connaît que de vue. Le miel de {B} vient de tomber dans le feu.",
            new[]
            {
                Plain("share", "Partager son bâton avec {B}",
                    "{A} tend son bâton. {B} croque, {A} croque, le miel coule partout. Ça colle aux doigts, et ça colle un peu les gens ensemble aussi.",
                    E(new AffinityShift(15), new ApplyModifier("hearty")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Ask("ask", "Demander à {B} : « Tu veux qu'on soit amis ? »",
                    "{A} prend une grande inspiration et pose la question, comme ça, au-dessus du feu.", "reply_campfire",
                    Ai((AiAxis.Sociability, 2), (AiAxis.Boldness, 1))),
                Plain("sparks", "Regarder les étincelles monter, sans rien dire",
                    "Les étincelles montent, montent, et deviennent des étoiles. Ou alors c'étaient déjà des étoiles. {B} regarde aussi.",
                    E(new ApplyModifier("soothed"), new AffinityShift(5)), Ai((AiAxis.Energy, -1), (AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone,
            TargetCondition: t => t.Bond == PlynlingBond.Acquaintances && t.Affinity >= 0),

        new EventDef("reply_campfire", EventType.Response, AnyStage, "Une question au-dessus du feu",
            "Entre deux bouchées de miel, {B} a demandé à {A} : « Tu veux qu'on soit amis ? »",
            new[]
            {
                Answer("yes", "Dire oui, la bouche pleine de miel",
                    "{A} dit oui, ou quelque chose d'approchant, la bouche pleine de miel. {B} comprend très bien.",
                    Stance.Accept, E(new SetAffinityAtLeast(PlynlingBonds.FriendsFrom + PlynlingBonds.BondMargin)), Ai((AiAxis.Sociability, 2))),
                Answer("later", "« Reviens au prochain feu, on verra. »",
                    "{A} sourit : « Reviens au prochain feu, on verra. » {B} promet d'apporter son propre miel, cette fois.",
                    Stance.Refuse, E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),

        // ado: un secret trop lourd
        new EventDef("teen_secret", EventType.Pulse, Teen, "Un secret trop lourd",
            "Depuis des jours, {A} porte un secret. Un petit secret, mais lourd, comme un gland qui serait en plomb. {B} est {b:le seul|la seule} à qui {A} pourrait le dire.",
            new[]
            {
                Ask("tell", "Tout raconter à {B}", "{A} entraîne {B} sous le Pont-Vieux et chuchote tout, d'une traite.", "reply_secret",
                    Ai((AiAxis.Sociability, 2)), ownerOnly: false),
                Plain("journal", "L'écrire dans un carnet, puis cacher le carnet",
                    "{A} écrit le secret, ferme le carnet et le glisse sous une latte du plancher. Le secret pèse beaucoup moins. Le plancher grince un peu plus.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Sociability, -1), (AiAxis.Rationality, 1))),
                Plain("keep", "Le garder pour soi",
                    "{A} garde le secret. Certains jours, ça pèse. D'autres jours, ça tient chaud.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1)), Stress(("gregarious", 10))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.Bond is PlynlingBond.Friends or PlynlingBond.BestFriends),

        new EventDef("reply_secret", EventType.Response, AnyStage, "Un secret confié",
            "Sous le Pont-Vieux, {B} a chuchoté un secret à {A}, d'une traite, sans respirer.",
            new[]
            {
                Answer("promise", "Promettre de ne jamais le répéter",
                    "{A} promet, la main sur le cœur. {B} respire enfin. Le secret dort maintenant chez deux personnes, et dort beaucoup mieux.",
                    Stance.Accept, E(new AffinityShift(15)), Ai((AiAxis.Honor, 2))),
                Answer("swap", "Confier un secret en retour",
                    "{A} se penche et chuchote à son tour. {B} écarquille les yeux. Les deux secrets se ressemblent comme deux glands.",
                    Stance.Accept, E(new AffinityShift(20)), Ai((AiAxis.Sociability, 2))),
                Answer("laugh", "Éclater de rire : ce n'est vraiment pas grave",
                    "« C'est tout ? » {B} rougit, puis rit aussi, un peu {b:vexé|vexée}. D'un coup, le secret ne pèse plus rien.",
                    Stance.Refuse, E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: les lettres du Fou (CK3's courting scheme; ends in the existing declaration)
        new EventDef("grown_letters", EventType.Pulse, Grown, "Les lettres du Fou",
            "Depuis des semaines, {A} pense à {B}. Le matin, le soir, et beaucoup entre les deux. Le dire en face semble impossible. Mais le Fou porte les mots de tout le hameau, à pied, et sans signature si on préfère. Le Fou ne les lit jamais. Le Fou compte les mots, c'est tout.",
            new[]
            {
                Try("poem", "Écrire un poème, le plus beau possible", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} écrit douze vers sur les yeux de {B}, le vent, et la soupe de la Courge. Le Fou part avec, en comptant. Le lendemain, le Fou rapporte une réponse : « Encore ? » « Un mot », précise le Fou.",
                    "{A} écrit, rature, recommence, et finit par confier au Fou une lettre qui ne parle que du temps qu'il fait. Le lendemain, la réponse arrive : « Oui, très beau temps. Et toi ? » « Six mots », dit le Fou, l'air déçu.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(10), new FollowUp("grown_letters_more", 24, 72)), E(new AffinityShift(5), new FollowUp("grown_letters_more", 24, 72)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
                Plain("simple", "Écrire simplement : « Je pense à toi. »",
                    "{A} écrit trois mots, sans signature, et les confie au Fou, pour {B}. Trois jours passent. Puis le Fou revient : « Moi aussi, je pense à quelqu'un. » Ce n'est pas un oui. Ce n'est pas un non. « Six mots », dit le Fou. « C'est bon signe, six. »",
                    E(new AffinityShift(5), new FollowUp("grown_letters_more", 24, 72)), Ai((AiAxis.Honor, 1))),
                Plain("drawer", "Garder ça pour soi, encore un peu",
                    "{A} prend une feuille, la regarde longtemps, puis la range dans un tiroir. Certaines lettres ont besoin de mûrir. Sous la fenêtre, le Fou ralentit, puis repart : le Fou a l'habitude.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.CanCouple),

        new EventDef("grown_letters_more", EventType.FollowUp, AnyStage, "La correspondance",
            "Depuis deux semaines, les lettres vont et viennent dans les poches du Fou. On s'y raconte la journée, les nuages, une chanson entendue à l'auberge. {B} signe toujours d'un petit dessin de feuille. Ce matin, la dernière lettre se termine par une question : « Qui es-tu ? »",
            new[]
            {
                Try("meet", "Répondre : « Demain, au Pont-Vieux, à l'aube. »", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} écrit l'heure et le lieu, et ajoute un petit dessin de feuille, en réponse. Le lendemain à l'aube, {B} est sur le pont, une écharpe autour du cou, et sourit en voyant arriver {A}. Le Fou, très discret, compte les pierres du parapet avec une grande concentration.",
                    "La pluie mouille l'encre de l'invitation. Le lendemain, {B} attend au pont… à midi. {A}, à l'aube. Les deux se ratent de six heures, et le comprennent le soir, en riant chacun de son côté.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15), new FollowUp("grown_letters_bridge", 24, 48)), E(new AffinityShift(5), new FollowUp("grown_letters_bridge", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("riddle", "Répondre par une devinette",
                    "{A} répond par une devinette : « Je prends ma soupe avec trois croûtons et je déteste les lundis. » Le lendemain, {B} a trouvé. La réponse que rapporte le Fou dit seulement : « Je savais. »",
                    E(new AffinityShift(10), new FollowUp("grown_letters_bridge", 24, 48)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_letters_bridge", EventType.FollowUp, AnyStage, "Sur le Pont-Vieux",
            "Le soleil se lève sur le Pont-Vieux. {A} et {B} sont là, côte à côte, la main sur la pierre, à regarder la Sommeille. Toutes les lettres sont dans la poche de {A}, et une bonne partie dans celle de {B}. Le moment est venu de dire tout haut ce qui a été écrit tout bas.",
            new[]
            {
                Ask("declare", "Tout avouer à {B}, tout haut",
                    "{A} prend une grande inspiration, se tourne vers {B}, et dit enfin, à voix haute, tout ce que les lettres disaient.", "reply_declare",
                    Ai((AiAxis.Boldness, 2), (AiAxis.Sociability, 1))),
                Plain("keep", "Proposer de garder les lettres, et rester amis",
                    "{A} sort les lettres et propose de les garder dans une boîte, à deux, en souvenir. {B} sourit, un peu {b:surpris|surprise}, et accepte. Certaines histoires sont plus belles à mi-chemin.",
                    E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        // ado: l'amitié en trois étapes (CK3's befriend scheme, ending in a real ask)
        new EventDef("teen_befriend", EventType.Pulse, Teen, "L'amitié en trois étapes",
            "Depuis quelque temps, {A} aimerait bien devenir {a:ami|amie} avec {B}. Le problème, c'est que {A} ne sait pas du tout comment on fait. Maître Frêne a sûrement un règlement là-dessus. En attendant, {A} a écrit un plan en trois étapes sur un bout de papier. L'étape un est soulignée deux fois.",
            new[]
            {
                Plain("hello", "Suivre le plan : étape un, dire bonjour",
                    "{A} attend {B} au coin de la place du Puits et dit bonjour. {B} répond bonjour. Fin de l'étape un. {A} rentre le cœur battant, et coche la case avec un soin particulier.",
                    E(new AffinityShift(5), new FollowUp("teen_befriend_rain", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
                Plain("jam", "Commencer par un petit cadeau anonyme",
                    "{A} laisse devant la porte de {B} un pot de confiture, avec un mot : « De la part de quelqu'un. » {B} goûte, et passe la journée à chercher qui. {A} passe la journée à faire semblant de ne pas savoir.",
                    E(new AffinityShift(10), new FollowUp("teen_befriend_rain", 24, 48)), Ai((AiAxis.Compassion, 1))),
                Try("blunt", "Aller droit au but : « Tu veux être mon {b:ami|amie} ? »", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{B} cligne des yeux, {b:surpris|surprise}, puis rit. « On peut commencer par un goûter. » C'est une réponse. C'est même une très bonne réponse.",
                    "{B} répond poliment « peut-être » et s'éclipse. {A} range le plan dans sa poche. Retour à l'étape un, mais en plus discret.",
                    E(new AffinityShift(15), new FollowUp("teen_befriend_rain", 24, 48)), E(new AffinityShift(5), new FollowUp("teen_befriend_rain", 24, 48)),
                    Ai((AiAxis.Boldness, 2)), Stress(("shy", 20))),
            },
            Target: TargetKind.Anyone,
            TargetCondition: t => t.Bond == PlynlingBond.Acquaintances && t.Affinity >= 0),

        new EventDef("teen_befriend_rain", EventType.FollowUp, AnyStage, "L'étape deux",
            "L'étape deux du plan dit : « Faire quelque chose ensemble. » {A} avait tout prévu : la pêche, les cerfs-volants, le concours de grimaces. Mais ce matin, la pluie tombe à verse, et {B} est {b:coincé|coincée} sous le même auvent que {A}, devant le four de Pain-Blond, fermé ce jour-là.",
            new[]
            {
                Plain("cards", "Sortir le jeu de cartes de sa poche",
                    "{A} sort le jeu de cartes. {A} et {B} jouent sous l'auvent jusqu'à ce que la pluie s'arrête, puis encore un peu après. {B} triche très mal, et {A} fait semblant de ne rien voir. C'est le début de quelque chose.",
                    E(new AffinityShift(10), new FollowUp("teen_befriend_ask", 24, 48)), Ai((AiAxis.Sociability, 1))),
                Try("race", "Proposer une course sous la pluie jusqu'à l'auberge", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} et {B} courent sous la pluie, sautent dans toutes les flaques, et arrivent à l'auberge en dégoulinant, riant si fort que la Courge sert deux bols de soupe sans rien demander, et pose dix questions sans attendre les réponses.",
                    "{A} glisse dans la première flaque et s'assoit dedans. {B} hésite, puis s'assoit à côté, dans la flaque, par solidarité. Les deux restent là un moment, sous la pluie, à rire comme des idiots.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(15), new FollowUp("teen_befriend_ask", 24, 48)), E(new AffinityShift(10), new FollowUp("teen_befriend_ask", 24, 48)),
                    Ai((AiAxis.Boldness, 2))),
                Plain("talk", "Parler, simplement",
                    "Sous l'auvent, {A} et {B} parlent de tout : des nuages, du moulin, d'un rêve bizarre. Au bout d'une heure, {A} se rend compte que l'étape deux est en train de se faire toute seule, sans plan.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(10), new FollowUp("teen_befriend_ask", 24, 48)), Ai((AiAxis.Sociability, 2))),
            },
            Target: TargetKind.Anyone),

        new EventDef("teen_befriend_ask", EventType.FollowUp, AnyStage, "L'étape trois",
            "L'étape trois du plan est écrite en tout petit, en bas de la page : « Demander. » {A} relit le mot une dizaine de fois. Ça paraît simple, sur le papier. {B} est là, à quelques pas, en train de donner du pain aux canards.",
            new[]
            {
                Ask("ask", "Demander à {B} : « On est amis, maintenant ? »",
                    "{A} prend une grande inspiration, s'approche, et pose la question, le papier serré dans la main.", "reply_befriend",
                    Ai((AiAxis.Sociability, 2), (AiAxis.Boldness, 1))),
                Plain("ducks", "Jeter le plan, et donner du pain aux canards avec {B}",
                    "{A} froisse le plan et le fourre au fond de sa poche. Puis s'approche, prend un morceau de pain et le donne aux canards, à côté de {B}. Personne ne demande rien. Pas besoin, finalement.",
                    E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Anyone),

        new EventDef("reply_befriend", EventType.Response, AnyStage, "Une question au bord de l'étang",
            "Au bord de l'étang, {B} s'est {b:approché|approchée} de {A}, un papier froissé à la main, et a demandé : « On est amis, maintenant ? »",
            new[]
            {
                Answer("yes", "Dire oui, et partager le pain",
                    "{A} dit oui, et tend un morceau de pain. {B} le donne aux canards, puis lit le papier à voix haute, étape par étape. Les deux rient jusqu'à la nuit tombée.",
                    Stance.Accept, E(new SetAffinityAtLeast(PlynlingBonds.FriendsFrom + PlynlingBonds.BondMargin)), Ai((AiAxis.Sociability, 2))),
                Answer("soon", "« Presque. Encore un goûter ou deux. »",
                    "{A} sourit : « Presque. Encore un goûter ou deux. » {B} sort un crayon et ajoute une étape quatre au plan : « Goûters. »",
                    Stance.Refuse, E(new AffinityShift(5)), Ai((AiAxis.Rationality, 1))),
            }),
    };
}
