using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8, second batch: adulte et ancien (pranks to the memoirs), and ancien.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave8Batch2Late() => new[]
    {
        // la guerre des farces (rivals: the jam on the door, the escalation, the count)
        new EventDef("grown_prank", EventType.Pulse, Grown, "La porte à la confiture",
            "Ce matin, la porte de {A} est couverte de confiture. Toute la porte. Au milieu, un mot collé : « Bonne journée. » L'écriture est celle de {B}. Aucun doute possible : {B} a même signé.",
            new[]
            {
                Try("salt", "Riposter : remplacer le sucre de {B} par du sel", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "Le lendemain matin, de la maison de {B}, un cri monte, suivi d'un long silence, puis d'un rire. Un rire qui promet beaucoup de choses. La guerre est déclarée.",
                    "{A} se glisse chez {B} avec le sel, et trouve sur la table un mot : « Pas le sucre. Trop facile. » {B} avait prévu. {A} rentre, très {a:vexé|vexée}, et planifie mieux.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_prank_war", 24, 48)), E(new FollowUp("grown_prank_war", 24, 48)), Ai((AiAxis.Vengefulness, 2)), Stress(("forgiving", 20))),
                Plain("taste", "Goûter la porte, et remercier",
                    "{A} goûte la porte. Confiture d'abricots, excellente. {A} laisse un mot à son tour : « Merci. Encore ? » Le lendemain, une deuxième couche. Fraise, cette fois. La guerre devient bizarre.",
                    E(new AffinityShift(5), new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_prank_war", 24, 48)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("wash", "Laver la porte, comme si de rien n'était",
                    "{A} lave la porte en sifflotant, pendant que {B} passe devant, exprès, trois fois. Pas un regard. Rien n'est plus agaçant que l'absence de réaction. {B} le sait. {A} aussi.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_prank_war", 24, 48)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_prank_war", EventType.FollowUp, AnyStage, "L'escalade",
            "Une semaine de guerre. Chaussettes cousues ensemble, tasse collée au comptoir, faux mots d'amour du hérisson glissés sous les portes. Le village compte les points. Ce soir, {A} tient la farce ultime : une poule, une brouette, et la cheminée de {B}. Mais par la fenêtre éclairée, on voit {B}, {b:seul|seule} à table, l'air très fatigué.",
            new[]
            {
                Plain("knock", "Ranger la brouette, et frapper à la porte",
                    "{A} gare la brouette, la poule dedans, et frappe. {B} ouvre, {b:surpris|surprise}. « Tu as l'air à bout. » {B} hausse les épaules : « Les farces, ça prend du temps. Le reste aussi. » {A} entre. La poule aussi.",
                    E(new AffinityShift(10), new FollowUp("grown_prank_truce", 6, 12)), Ai((AiAxis.Compassion, 2))),
                Try("hen", "Exécuter la farce ultime", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "La poule descend par la cheminée, très calmement, et s'installe sur la table de {B}, en face. {B} et la poule se regardent. Puis {B} éclate de rire, si fort que tout le village l'entend. Point pour {A}.",
                    "La poule refuse la cheminée, saute de la brouette et rentre au poulailler. La brouette roule toute seule jusqu'à la porte de {B}, qui ouvre, voit {A}, la brouette, les plumes. « Sérieusement ? »",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_prank_truce", 6, 12)), E(new FollowUp("grown_prank_truce", 6, 12)),
                    Ai((AiAxis.Vengefulness, 1), (AiAxis.Boldness, 1)), Stress(("compassionate", 15))),
            },
            Target: TargetKind.Hostile),

        new EventDef("grown_prank_truce", EventType.FollowUp, AnyStage, "Dix-sept partout",
            "{A} et {B} se font face, un carnet ouvert entre les deux : le décompte des farces, tenu par le moineau. Égalité parfaite. Dix-sept partout. Le moineau attend, crayon en l'air.",
            new[]
            {
                Plain("draw", "Déclarer l'égalité, et signer la paix",
                    "{A} et {B} signent le carnet, côte à côte, sous « match nul ». Le moineau le fait encadrer au café. Depuis, les deux se saluent poliment, avec une lueur dans l'œil qui inquiète tout le village.",
                    E(new AffinityShift(20), new ApplyModifier("light_heart")), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1))),
                Plain("team", "Proposer une alliance : farcer le village, ensemble",
                    "« Dix-sept partout. Et si on s'occupait du hérisson, maintenant ? » {B} sourit lentement. Le lendemain, toutes les horloges du village avancent de douze minutes. Personne ne sait qui. Tout le monde sait qui.",
                    E(new AffinityShift(25), new ApplyModifier("sly")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Hostile),

        // la route de la foire (CK3's travel with a companion: the road, the inn, the fair)
        new EventDef("grown_journey", EventType.Pulse, Grown, "La route de la foire",
            "La grande foire de la ville d'en bas, c'est trois jours de marche. {B} y va aussi, et propose de faire la route ensemble : « On partage les frais, la carte, et les ampoules. » Départ à l'aube, sac au dos.",
            new[]
            {
                Plain("together", "Accepter, et partir ensemble",
                    "{A} et {B} partent à l'aube. Au bout d'une heure, chacun connaît les chansons préférées de l'autre. Au bout de deux, plus personne ne les supporte. Au bout de trois, on les chante en chœur.",
                    E(new AffinityShift(10), new FollowUp("grown_journey_inn", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Plain("map", "Accepter, à condition de tenir la carte",
                    "{A} tient la carte, et la tient à l'envers pendant deux heures, avant que {B} ne le fasse remarquer, très gentiment. La carte change de mains. Le chemin aussi.",
                    E(new AffinityShift(5), new FollowUp("grown_journey_inn", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("alone", "Refuser : on marche mieux {a:seul|seule}",
                    "{A} part {a:seul|seule}, une heure plus tard. Sur la route, {A} aperçoit {B} loin devant, toujours à la même distance, comme un repère. Le soir, les deux feux de camp brillent à cent pas l'un de l'autre. Personne ne fait le premier pas.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Sociability, -1), (AiAxis.Boldness, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_journey_inn", EventType.FollowUp, AnyStage, "L'auberge du sanglier",
            "Le deuxième soir, une averse pousse {A} et {B} dans une auberge au bord de la route. L'aubergiste, un vieux sanglier, n'a plus qu'une chambre, avec un seul lit, étroit, et un fauteuil qui a l'air de mordre.",
            new[]
            {
                Plain("armchair", "Prendre le fauteuil, sans discuter",
                    "{A} prend le fauteuil. Le fauteuil mord, effectivement. À trois heures, {B} se lève sans un mot, et étend sa propre couverture sur {A}. Au matin, personne n'en parle.",
                    E(new AffinityShift(15), new FollowUp("grown_journey_fair", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("coin", "Tirer le lit à pile ou face", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} gagne à pile ou face. Puis propose la revanche. Puis la belle. Ça dure jusqu'à minuit, en riant. Finalement, personne ne dort dans le lit : les deux s'endorment sur la table, sur le jeu de cartes.",
                    "{A} perd à pile ou face, et s'installe dans le fauteuil. {B} montre alors la pièce : deux côtés face. Fou rire. Le sanglier apporte une deuxième couverture, par pitié.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(10), new FollowUp("grown_journey_fair", 24, 48)), E(new AffinityShift(10), new FollowUp("grown_journey_fair", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("talk", "Ne pas dormir, et parler toute la nuit",
                    "Ni lit, ni fauteuil : {A} et {B} s'assoient sous la fenêtre et parlent jusqu'à l'aube, de tout, des étoiles, du village, de choses jamais dites à personne. Au matin, les deux repartent avec des cernes et un drôle de sourire.",
                    E(new AffinityShift(20), new FollowUp("grown_journey_fair", 24, 48)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_journey_fair", EventType.FollowUp, AnyStage, "La grande foire",
            "La foire de la ville d'en bas : des étals à perte de vue, des musiciens, une odeur de pommes grillées. {A} et {B} ont quelques cailloux et une seule journée. Devant un stand, {B} s'arrête net devant une petite boîte à musique en forme d'escargot. Puis repart, trop vite.",
            new[]
            {
                Plain("gift", "Retourner discrètement acheter la boîte, pour {B}",
                    "{A} retourne au stand, achète la boîte et la cache dans son sac. Au dernier virage avant le village, {A} la tend à {B}. {B} ne dit rien, tourne la petite clé, et la boîte joue jusqu'au village.",
                    E(new AffinityShift(25), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2)), Stress(("greedy", 15))),
                Plain("apple", "Partager une pomme grillée, devant les musiciens",
                    "{A} et {B} partagent une pomme grillée sur une caisse, devant les musiciens. On ne dépense rien d'autre. On n'a besoin de rien d'autre.",
                    E(new AffinityShift(15), new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Sociability, 1))),
                Try("haggle", "Marchander les meilleurs prix, pour deux", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} marchande tout, partout, avec un aplomb terrifiant. À la fin de la journée, les deux repartent avec deux sacs pleins, et des cailloux encore en poche. {B} regarde {A} avec une admiration un peu inquiète.",
                    "{A} marchande si fort que le marchand de pommes, vexé, refuse de vendre quoi que ce soit. {B} doit acheter les pommes en cachette, à l'autre bout de la foire, et les deux rient tout le long du retour.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(10), new AffinityShift(10)), E(new AffinityShift(10)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        // l'effraie du grenier (CK3's haunted-house mysteries, with a feathered answer)
        new EventDef("grown_ghost", EventType.Pulse, Grown, "Des pas au grenier",
            "Depuis trois nuits, on entend marcher dans le grenier de {A}. Des pas légers, un frottement, parfois un soupir. Le grenier est fermé à clé, et la clé est dans la poche de {A}. Ce soir, les pas reprennent.",
            new[]
            {
                Try("climb", "Monter au grenier, une bougie à la patte", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} monte. Les marches grincent. Au grenier, la bougie éclaire une vieille effraie, entrée par une tuile cassée, qui a fait son nid dans le carton des décorations de fête. L'effraie cligne des yeux. {A} aussi.",
                    "{A} monte, la bougie s'éteint, quelque chose souffle « hhhh » dans le noir, et {A} redescend toutes les marches d'un coup. En plein jour, le lendemain, on trouve l'explication : une vieille effraie, dans le carton des décorations.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_ghost_owl", 24, 48)), E(new FollowUp("grown_ghost_owl", 24, 48)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Plain("badger", "Demander au blaireau de monter avec soi",
                    "Le blaireau monte devant, un balai à la patte, très brave. Au grenier, une vieille effraie s'est installée dans le carton des décorations de fête. Le blaireau redescend, très digne, et ne reparle jamais du cri qui lui a échappé.",
                    E(new FollowUp("grown_ghost_owl", 24, 48)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("cotton", "Mettre du coton dans ses oreilles",
                    "{A} met du coton dans ses oreilles et dort très bien. Trois nuits plus tard, des plumes blanches descendent par l'escalier, et on finit par comprendre : une vieille effraie habite le grenier.",
                    E(new ApplyModifier("well_rested"), new FollowUp("grown_ghost_owl", 24, 48)), Ai((AiAxis.Energy, -1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_ghost_owl", EventType.FollowUp, AnyStage, "L'effraie du grenier",
            "L'effraie est vieille, presque blanche, et visiblement décidée à rester. La nuit, l'effraie chasse les souris du quartier. Le jour, l'effraie dort dans le carton des décorations, la tête sur une guirlande. Consultée, la chouette de la bibliothèque dit que c'est une lointaine cousine, très difficile.",
            new[]
            {
                Plain("stay", "La laisser rester, contre la chasse aux souris",
                    "Marché conclu : l'effraie reste, les souris partent. Le grenier n'a jamais été aussi tranquille. Parfois, la nuit, {A} entend un soupir là-haut, et ça a quelque chose de rassurant, maintenant.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("box", "Lui construire un nichoir, dans le vieux chêne",
                    "{A} construit un nichoir au toit en pente, avec un carton de décorations neuf à l'intérieur, pour l'ambiance. L'effraie déménage après une semaine de réflexion. Sur le rebord, en partant, l'effraie laisse une plume, blanche et douce comme la neige.",
                    E(new GiveItem("col.plume"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_beetle_duel", EventType.Pulse, Grown, "Le duel du lucane",
            "Un lucane, très grand, très cornu, barre le chemin de {A}. « Tu as insulté ma mère. » {A} n'a jamais vu la mère du lucane. « Duel. Demain, à l'aube, sur la souche. » Le lucane repart sans attendre de réponse, en faisant claquer ses mandibules.",
            new[]
            {
                Try("fight", "Se présenter au duel, et lutter à la loyale", new EventChallenge(PlynlingStat.Courage, 8),
                    "À l'aube, sur la souche, {A} et le lucane luttent trois minutes. {A} renverse le lucane sur le dos, l'aide à se relever, et le lucane, ému, déclare l'honneur lavé. On apprend ensuite que le lucane s'était trompé de personne. Excuses longues et sincères.",
                    "À l'aube, sur la souche, le lucane soulève {A} au-dessus de sa tête, très facilement, et dépose {A} délicatement dans l'herbe. Puis demande : « Au fait, tu es bien… ? » Erreur sur la personne. Excuses longues et sincères.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("fired_up")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("talk", "Retrouver le lucane avant l'aube, et s'expliquer", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} trouve le lucane et pose trois questions. À la troisième, le lucane comprend que l'insulte venait de quelqu'un d'autre, qui portait le même chapeau. Le lucane offre un thé pour s'excuser. Le thé est très fort.",
                    "{A} s'explique. Le lucane écoute, hoche la tête, et répond : « Demain. À l'aube. » Le lendemain, le lucane ne vient pas : le lucane a fini par comprendre tout seul, et a beaucoup trop honte.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("champion", "Envoyer un champion : l'escargot",
                    "L'escargot accepte d'être le champion de {A}. À l'aube, l'escargot part vers la souche. Le lucane attend une heure, puis deux. L'escargot arrive à midi. Le lucane, épuisé d'attendre, déclare forfait.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("grown_portrait", EventType.Pulse, Grown, "Le portrait du café",
            "Une hermine peintre est de passage au village, avec un chevalet et une boîte de pinceaux. Le café veut un portrait pour son mur, et la tortue a choisi le modèle : {A}. La séance commence dans une heure. L'hermine taille déjà ses crayons.",
            new[]
            {
                Plain("noble", "Prendre la pose la plus noble possible",
                    "{A} prend la pose : menton levé, patte sur le cœur, regard au loin. Trois heures plus tard, le portrait est magnifique, et {A} ne sent plus sa nuque. Le tableau trône au café, et les clients le saluent en entrant.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), Stress(("humble", 15))),
                Plain("usual", "Faire comme d'habitude, sans poser",
                    "{A} boit son chocolat, lit le journal, chasse une miette. L'hermine peint tout. Le portrait est si ressemblant que l'escargot, en le voyant, dit bonjour au tableau, et attend la réponse.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Rationality, 1))),
                Try("still", "Tenir trois heures sans bouger", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} ne bouge pas. Pas un cil. Trois heures. L'hermine, impressionnée, offre à {A} une esquisse, avec une dédicace : « Au modèle le plus patient de la vallée ».",
                    "Au bout de dix minutes, le nez de {A} gratte. Au bout de vingt, l'oreille. Le portrait final montre {A} en train de se gratter, en plein mouvement, et c'est de loin le plus vivant de tout le café.",
                    E(new GrowStat(PlynlingStat.Stewardship), new LiftNeed(Need.Happiness, 0.15)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            }),

        // ---- wave 8, second batch: ancien
        // les mémoires (CK3's commissioned books: the first page, the visitors, the reading)
        new EventDef("elder_memoirs", EventType.Pulse, Elder, "Les mémoires",
            "La chouette a offert à {A} un gros cahier relié, aux pages blanches. « Tes mémoires. Avant que tout s'envole. » Le cahier attend sur la table depuis une semaine. Ce matin, {A} débouche l'encrier. Reste à choisir par où commencer.",
            new[]
            {
                Plain("start", "Commencer par le tout début",
                    "{A} écrit : « Je suis {a:né|née} un mardi, d'après la rumeur. » Puis la suite vient toute seule : la première flaque, l'escargot, le héron. Le soir, quinze pages. La main tremble un peu. Pas les souvenirs.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("elder_memoirs_visitors", 48, 96)), Ai((AiAxis.Rationality, 1))),
                Plain("best", "Commencer par le plus beau souvenir",
                    "{A} ferme les yeux, choisit, et écrit le plus beau souvenir en premier, au cas où. Le reste suivra, dans le désordre. La vraie vie aussi était dans le désordre.",
                    E(new ApplyModifier("soothed"), new FollowUp("elder_memoirs_visitors", 48, 96)), Ai((AiAxis.Compassion, 1))),
                Plain("embellish", "Embellir un peu. Beaucoup",
                    "{A} écrit que la carpe dorée était grande comme une barque, que l'orage a duré dix jours, et que le héron, un jour, a ri. Le héron, qui lit par-dessus l'épaule, conteste le dernier point, très sèchement.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("elder_memoirs_visitors", 48, 96)), Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("honest", 15))),
            }),

        new EventDef("elder_memoirs_visitors", EventType.FollowUp, AnyStage, "Les visiteurs",
            "La nouvelle a fait le tour du village : {A} écrit ses mémoires. Depuis, on frappe à la porte tous les jours. L'ours voudrait savoir s'y trouver, la tortue a « deux ou trois précisions », et l'escargot a apporté une liste de ses propres exploits, longue de quatre pages.",
            new[]
            {
                Plain("everyone", "Faire une place à tout le monde",
                    "{A} écoute tout le monde, note tout, et ajoute un chapitre par visiteur. Les mémoires de {A} deviennent celles du village entier. Le cahier ne suffit plus. La chouette en apporte un deuxième, sans rien dire.",
                    E(new ApplyModifier("cherished"), new FollowUp("elder_memoirs_reading", 72, 120)), Ai((AiAxis.Sociability, 2))),
                Plain("sign", "Accrocher un écriteau : « Mémoires en cours »",
                    "{A} accroche un écriteau sur la porte : « Mémoires en cours. Revenez au prochain chapitre. » Le village respecte l'écriteau, à peu près. Seul l'escargot attend devant, avec sa liste, toute la semaine.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("elder_memoirs_reading", 72, 120)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
            }),

        new EventDef("elder_memoirs_reading", EventType.FollowUp, AnyStage, "La lecture",
            "Les mémoires sont finies. La chouette a organisé une lecture à la bibliothèque, et pour une fois, personne ne dit « chut ». Le village est là, au complet. {A} ouvre le cahier à la première page, et s'éclaircit la voix. Au premier rang, le héron s'est même assis.",
            new[]
            {
                Plain("all", "Tout lire, depuis le début",
                    "{A} lit pendant trois heures. On rit, on pleure, on proteste (l'ours), on corrige (la tortue). À la dernière page, le silence dure longtemps. Puis le héron se lève et, pour la première fois de mémoire de village, applaudit.",
                    E(new ApplyModifier("cherished"), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 2))),
                Plain("library", "Confier le cahier à la bibliothèque, sans le lire",
                    "{A} pose le cahier sur le pupitre et le tend à la chouette : « Pour la réserve. Pour plus tard. » La chouette le range avec les livres que personne n'a le droit de lire, et y colle une étiquette : « À ouvrir quand on aura besoin de se souvenir. »",
                    E(new ApplyModifier("clear_conscience"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Try("chapter", "Lire un seul chapitre, le plus important", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} choisit un seul chapitre : un matin ordinaire, au café, avec la tortue. Rien ne s'y passe. Tout le monde pleure. Au fond, la tortue sort son mouchoir propre, et le garde à la main longtemps.",
                    "{A} choisit un chapitre, puis un autre, puis s'aperçoit que tous sont les plus importants. La lecture dure jusqu'à minuit. Personne ne part.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("cherished")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("elder_acorn", EventType.Pulse, Elder, "Le gland",
            "{A} a ramassé un gland, ce matin, au pied du vieux chêne. Un beau gland, lisse, bien rond. Un chêne met cent ans à devenir grand. {A} retourne le gland dans sa patte, longtemps.",
            new[]
            {
                Plain("plant", "Le planter quand même, au milieu de la place",
                    "{A} plante le gland au milieu de la place, l'arrose, et plante à côté un petit panneau : « Pour plus tard. » Les petits du village viennent l'arroser à tour de rôle. Dans cent ans, quelqu'un s'assiéra à son ombre sans savoir. C'est très bien comme ça.",
                    E(new ApplyModifier("clear_conscience"), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 2))),
                Plain("pocket", "Le garder dans sa poche",
                    "Le gland va dans la poche, et y reste. Parfois, {A} le roule entre deux doigts en marchant. Ça ne porte pas spécialement bonheur. Mais ça rappelle qu'on peut toujours commencer quelque chose.",
                    E(new GiveItem("col.gland")), Ai((AiAxis.Rationality, 1))),
                Plain("give", "Le donner au plus petit du village",
                    "{A} donne le gland au plus petit du village, avec une seule consigne : « Plante-le le jour où tu en auras envie. » Le petit le serre dans sa patte comme un trésor, et court le montrer à tout le monde.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),
    };
}
