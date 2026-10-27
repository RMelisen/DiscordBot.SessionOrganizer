using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 7 story cycles: adulte, ancien.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave7Grown() => new[]
    {
        // adulte et ancien: les lettres du vieux pont (CK3's courting scheme; ends in the existing declaration)
        new EventDef("grown_letters", EventType.Pulse, Grown, "Les lettres du vieux pont",
            "Depuis des semaines, {A} pense à {B}. Le matin, le soir, et beaucoup entre les deux. Le dire en face semble impossible. Mais sous le vieux pont, entre deux pierres, se cache une petite boîte aux lettres que le village connaît depuis toujours. On y laisse des mots sans signature. Le héron les garde, sans jamais les lire.",
            new[]
            {
                Try("poem", "Écrire un poème, le plus beau possible", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} écrit douze vers sur les yeux de {B}, le vent, et le chocolat de la tortue. Le poème est glissé sous le pont, adressé à {B}. Le lendemain, la boîte contient une réponse, un seul mot : « Encore ? »",
                    "{A} écrit, rature, recommence, et finit par glisser sous le pont une lettre qui ne parle que de la météo. Le lendemain, la boîte contient une réponse : « Oui, très beau temps. Et toi ? »",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(10), new FollowUp("grown_letters_more", 24, 72)), E(new AffinityShift(5), new FollowUp("grown_letters_more", 24, 72)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
                Plain("simple", "Écrire simplement : « Je pense à toi. »",
                    "{A} écrit trois mots, sans signature, et les glisse sous le pont, adressés à {B}. Trois jours passent. Puis, dans la boîte, une réponse : « Moi aussi, je pense à quelqu'un. » Ce n'est pas un oui. Ce n'est pas un non.",
                    E(new AffinityShift(5), new FollowUp("grown_letters_more", 24, 72)), Ai((AiAxis.Honor, 1))),
                Plain("drawer", "Garder ça pour soi, encore un peu",
                    "{A} prend une feuille, la regarde longtemps, puis la range dans un tiroir. Certaines lettres ont besoin de mûrir. Sous le pont, le héron attend : le héron a l'habitude.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => t.CanCouple),

        new EventDef("grown_letters_more", EventType.FollowUp, AnyStage, "La correspondance",
            "Depuis deux semaines, les lettres vont et viennent sous le vieux pont. On s'y raconte la journée, les nuages, une chanson entendue au café. {B} signe toujours d'un petit dessin de feuille. Ce matin, la dernière lettre se termine par une question : « Qui es-tu ? »",
            new[]
            {
                Try("meet", "Répondre : « Demain, au vieux pont, à l'aube. »", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} écrit l'heure et le lieu, et ajoute un petit dessin de feuille, en réponse. Le lendemain à l'aube, {B} est sur le pont, une écharpe autour du cou, et sourit en voyant arriver {A}. Le héron, très discret, regarde ailleurs avec une grande concentration.",
                    "La pluie mouille l'encre de l'invitation. Le lendemain, {B} attend au pont… à midi. {A}, à l'aube. Les deux se ratent de six heures, et le comprennent le soir, en riant chacun de son côté.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(15), new FollowUp("grown_letters_bridge", 24, 48)), E(new AffinityShift(5), new FollowUp("grown_letters_bridge", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("riddle", "Répondre par une devinette",
                    "{A} répond par une devinette : « Je prends mon chocolat avec trois sucres et je déteste les lundis. » Le lendemain, {B} a trouvé. La réponse dans la boîte dit seulement : « Je savais. »",
                    E(new AffinityShift(10), new FollowUp("grown_letters_bridge", 24, 48)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_letters_bridge", EventType.FollowUp, AnyStage, "Sur le vieux pont",
            "Le soleil se lève sur le vieux pont. {A} et {B} sont là, côte à côte, à regarder la rivière. Toutes les lettres sont dans la poche de {A}, et une bonne partie dans celle de {B}. Le moment est venu de dire tout haut ce qui a été écrit tout bas.",
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

        // adulte et ancien: le juge d'un jour (CK3's hold court activity: three petitions)
        new EventDef("grown_judge", EventType.Pulse, Grown, "Le juge d'un jour",
            "Le maire du village, un vieux castor, a attrapé un rhume. Quelqu'un doit tenir l'audience du samedi, et le village a désigné {A}, à l'unanimité moins une voix (celle de {A}). Sous le chêne, la première affaire arrive déjà, en se disputant : l'ours accuse le hérisson de siffler ses horaires à l'aube sous sa fenêtre ; le hérisson accuse l'ours de ronfler si fort que les trains en tremblent.",
            new[]
            {
                Try("compromise", "Trouver un compromis", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} propose : le hérisson sifflera ses horaires à voix basse, et l'ours dormira la fenêtre fermée, avec un bonnet. Les deux réfléchissent, grognent, et se serrent la patte. Le village applaudit. Affaire suivante.",
                    "Le compromis ne plaît à personne. L'ours ronfle de vexation sur place, le hérisson siffle de protestation. {A} déclare l'affaire « en délibéré » et passe à la suivante, très vite.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_judge_tree", 1, 3)), E(new FollowUp("grown_judge_tree", 1, 3)),
                    Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("rules", "Appliquer le règlement, à la lettre",
                    "{A} ouvre le vieux règlement du village. Article douze : « Nul ne sifflera avant le chant du coq. » Article treize : « Nul ne ronflera au-delà du raisonnable. » Les deux sont en tort, et paient chacun un pot de miel au café. La tortue est ravie.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_judge_tree", 1, 3)), Ai((AiAxis.Honor, 2))),
                Plain("swap", "Proposer un échange de maisons, pour une nuit",
                    "L'ours dort à la gare, le hérisson chez l'ours. Le lendemain, les deux reviennent avec des cernes : la gare est trop bruyante, la maison de l'ours trop silencieuse. Chacun comprend un peu mieux l'autre.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_judge_tree", 1, 3)), Ai((AiAxis.Rationality, -1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("grown_judge_tree", EventType.FollowUp, AnyStage, "La deuxième affaire",
            "Deuxième affaire. Deux écureuils se disputent un noisetier qui pousse exactement entre leurs deux jardins. Chacun jure l'avoir planté. Le noisetier a l'air de s'en moquer complètement. Sous le chêne, le public retient son souffle.",
            new[]
            {
                Plain("split", "Partager : une branche sur deux",
                    "{A} décrète que le noisetier sera partagé, une branche sur deux. Les écureuils comptent les branches : onze. Un long silence. Puis {A} attribue la onzième aux oiseaux. Tout le monde trouve ça juste, sauf les oiseaux, qui n'ont rien demandé.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_judge_snail", 1, 3)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Try("truth", "Chercher la vérité : qui l'a vraiment planté ?", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} examine les racines, les vieux registres et la mémoire de la tortue. Verdict : personne ne l'a planté. Un geai a oublié une noisette, voilà vingt ans. Le noisetier appartient donc au geai, qui ne réclame rien. Vaincus par la logique, les deux écureuils se partagent la récolte.",
                    "{A} enquête, et découvre que les deux écureuils disent vrai : chacun a planté une noisette, au même endroit, le même jour. Plus personne n'y comprend rien. L'affaire est classée sous le titre « Mystère ».",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_judge_snail", 1, 3)), E(new FollowUp("grown_judge_snail", 1, 3)), Ai((AiAxis.Rationality, 2))),
                Plain("harvest", "Organiser une grande récolte commune",
                    "{A} propose une récolte commune, avec goûter. Les deux écureuils grimpent, cueillent, et finissent par se lancer des noisettes en riant. À la fin, plus personne ne se souvient de qui a planté quoi.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_judge_snail", 1, 3)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_judge_snail", EventType.FollowUp, AnyStage, "La dernière affaire",
            "La dernière affaire est la plus délicate. L'escargot porte plainte contre le village tout entier, pour excès de vitesse. Selon l'escargot, tout le monde va beaucoup trop vite, tout le temps, et c'est épuisant à regarder. Le public est debout. Depuis sa fenêtre, le maire éternue d'impatience.",
            new[]
            {
                Plain("slow_hour", "Instaurer une heure lente, chaque jour",
                    "{A} décrète que, dès demain, de quatre à cinq heures, tout le village ira au rythme de l'escargot. Le premier jour est une catastrophe. Le deuxième, un peu moins. Au bout d'une semaine, l'heure lente est devenue le moment préféré de tout le monde.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Try("dismiss", "Débouter l'escargot, avec beaucoup de tact", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} explique, avec mille précautions, que le village ne peut pas ralentir. Mais offre à l'escargot une place d'honneur au café, d'où l'on voit tout passer sans avoir à suivre. L'escargot accepte. L'audience est levée sous les applaudissements, et le maire envoie, depuis son lit, une petite bourse « pour le dérangement ».",
                    "L'escargot se vexe et quitte l'audience, très lentement. La sortie dure vingt minutes, dans un silence gêné. Le maire envoie quand même une petite bourse, avec un mot : « Courage. »",
                    E(new GiveCailloux(15), new ApplyModifier("well_spoken")), E(new GiveCailloux(5)), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, -1))),
                Plain("recuse", "Se déclarer trop rapide pour être impartial",
                    "{A} se lève et déclare, très solennellement, ne pas pouvoir juger cette affaire : {A} marche beaucoup trop vite. L'escargot apprécie l'honnêteté. L'audience est levée. Le village rentre chez soi, plus lentement que d'habitude.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        // adulte et ancien: la chronique du village (CK3's commissioned epics and legacies)
        new EventDef("grown_chronicle", EventType.Pulse, Grown, "La chronique du village",
            "À la bibliothèque, la chouette a ressorti le vieux livre de la chronique du village. La dernière page écrite date de l'année où le pont a gelé. Depuis, plus personne n'a rien noté. « Un village qui n'écrit pas son histoire finit par l'oublier », dit la chouette, en tendant la plume à {A}.",
            new[]
            {
                Plain("elders", "Accepter, et commencer par interroger les anciens",
                    "{A} prend la plume, un carnet, et fait le tour des anciens : la tortue, le blaireau, le hérisson. Chacun a une histoire, et chacun raconte celle des autres un peu de travers. Le carnet se remplit vite.",
                    E(new FollowUp("grown_chronicle_tarts", 24, 72)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Try("memory", "Accepter, et tout écrire de mémoire", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} s'installe à la bibliothèque et écrit d'une traite : la grande partie de balle, la course d'escargots, le banquet de la soupe. Le style est vif, les détails justes. La chouette lit par-dessus l'épaule de {A} et ne corrige que deux virgules.",
                    "{A} écrit trois pages, puis s'aperçoit que la course d'escargots se déroule avant la naissance de l'escargot. Tout est à refaire, avec l'aide des anciens.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_chronicle_tarts", 24, 72)), E(new FollowUp("grown_chronicle_tarts", 24, 72)),
                    Ai((AiAxis.Rationality, 2))),
                Plain("decline", "Refuser : trop de responsabilité",
                    "{A} rend la plume. La chouette ne dit rien, range le livre, et le pose bien en vue, sur la plus haute étagère. Chaque fois que {A} passe à la bibliothèque, le livre est là. Le livre attend.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        new EventDef("grown_chronicle_tarts", EventType.FollowUp, AnyStage, "Ce qu'on écrit, ce qu'on tait",
            "La chronique avance. Mais en relisant ses notes, {A} tombe sur une histoire que quelqu'un n'a pas envie de voir écrite : l'année où l'ours a mangé, seul, en une nuit, toutes les tartes de la fête des moissons. L'ours a supplié qu'on n'en parle pas. Le reste du village en parle encore.",
            new[]
            {
                Plain("write", "Tout écrire : la chronique doit être vraie",
                    "{A} écrit l'histoire des tartes, avec tous les détails. La chronique y gagne en vérité. L'ours, en lisant, rougit jusqu'aux oreilles, puis éclate de rire : après tout, c'étaient de très bonnes tartes.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_chronicle_reading", 24, 48)), Ai((AiAxis.Honor, 2)), Stress(("compassionate", 10))),
                Try("tender", "L'écrire, mais avec tendresse", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} écrit : « Cette année-là, l'ours aima tant les tartes de la fête qu'aucune ne fut laissée de côté. » L'ours lit la phrase trois fois et demande une copie, pour l'encadrer.",
                    "La tendresse se voit trop. Le village lit entre les lignes, et l'histoire des tartes devient encore plus célèbre qu'avant. L'ours soupire.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_chronicle_reading", 24, 48)), E(new FollowUp("grown_chronicle_reading", 24, 48)),
                    Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("skip", "Laisser cette histoire de côté",
                    "{A} tourne la page. Certaines histoires appartiennent à ceux qui les ont vécues. L'ours, qui l'apprend, dépose le lendemain devant la porte de {A} une tarte. Entière.",
                    E(new LiftNeed(Need.Hunger, 0.2), new FollowUp("grown_chronicle_reading", 24, 48)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_chronicle_reading", EventType.FollowUp, AnyStage, "La première lecture",
            "Ce soir, la chouette a organisé la première lecture publique de la nouvelle chronique. La bibliothèque est pleine, jusque dans les escaliers. Les anciens sont au premier rang. {A} monte sur l'estrade, le livre dans les pattes, et ouvre la première page.",
            new[]
            {
                Try("read", "Lire avec le ton, les voix, tout", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} lit et fait toutes les voix : la tortue en très lent, le hérisson en très exact. La salle rit, pleure un peu, et applaudit longtemps. En rangeant la chronique, la chouette glisse dans la poche de {A} une vieille carte, trouvée dans la reliure.",
                    "{A} lit trop vite, saute une page et fait s'effondrer le vieux pont deux fois. La salle rit beaucoup. Au premier rang, la chouette prend des notes pour la deuxième édition.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveItem("col.carte_tresor")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("tortoise", "Laisser la tortue lire à sa place",
                    "{A} tend le livre à la tortue. La tortue lit, très lentement, pendant trois heures. Personne ne part. À la fin, la tortue referme le livre et dit simplement : « C'est bien. C'est nous. »",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("shelf", "Poser la chronique sur l'étagère, sans discours",
                    "{A} pose la chronique sur l'étagère, sans un mot, et s'en va. Le lendemain, la file d'attente pour l'emprunter fait le tour de la bibliothèque. La chouette doit instaurer des tickets.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: le vieux pont (CK3's great projects and building investments)
        new EventDef("grown_bridge", EventType.Pulse, Grown, "La fissure du vieux pont",
            "Ce matin, une fissure est apparue sur le vieux pont. Une grande, qui part du milieu et file vers la rive. Le héron, qui vit dessous depuis toujours, l'a regardée toute la journée sans bouger. Le maire parle de « faire quelque chose », ce qui, chez le maire, veut dire ne rien faire. {A} n'a pas l'intention d'attendre.",
            new[]
            {
                Try("organize", "Organiser les travaux, avec tout le village", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} dresse un plan, répartit les tâches et fixe le début des travaux au samedi. L'ours portera les pierres, le hérisson tiendra le planning, la pie fournira les clous, sans dire d'où.",
                    "Le plan est parfait, mais le premier samedi tombe le jour de la course d'escargots, et personne ne vient. Les travaux sont repoussés d'une semaine.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_bridge_box", 48, 96)), E(new FollowUp("grown_bridge_box", 72, 120)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1)), Stress(("lazy", 20))),
                Plain("collect", "Faire une collecte au café",
                    "{A} pose un bocal sur le comptoir du café, avec une étiquette : « Pour le pont. » Le bocal se remplit vite : chacun a une histoire avec le vieux pont. Le héron, sans un mot, y dépose un caillou très ancien.",
                    E(new FollowUp("grown_bridge_box", 48, 96)), Ai((AiAxis.Sociability, 2))),
                Plain("heron", "Demander d'abord l'avis du héron",
                    "{A} s'assoit à côté du héron, sous le pont. Après un très long silence, le héron parle : du pont, de la rivière, d'avant le village. Puis, tout bas : « Répare-le. Mais garde la fissure. Ce pont a vécu. » {A} comprend.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_bridge_box", 48, 96)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_bridge_box", EventType.FollowUp, AnyStage, "Le chantier",
            "Le chantier dure depuis une semaine. Les pierres sont posées, le mortier sèche, et le village s'est habitué à passer par le gué. Mais ce matin, en retirant la vieille pierre centrale, l'ours a trouvé quelque chose : une petite boîte en fer, rouillée, scellée, coincée dans les fondations depuis des générations.",
            new[]
            {
                Try("open", "Ouvrir la boîte, devant tout le monde", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} force le couvercle. Dedans : une boussole en cuivre, une lettre à moitié effacée, et un dessin du pont tout neuf, signé par les premiers bâtisseurs. Le village entier se penche. La boussole indique toujours le nord, et la lettre dit simplement : « Prenez-en soin. »",
                    "Le couvercle résiste, puis cède d'un coup, et tout le contenu tombe dans la rivière. Une lettre file au fil de l'eau. Seul un vieux bouton est sauvé, de justesse, par le héron.",
                    E(new GiveItem("col.boussole"), new FollowUp("grown_bridge_open", 48, 96)), E(new GiveItem("col.bouton"), new FollowUp("grown_bridge_open", 48, 96)),
                    Ai((AiAxis.Boldness, 1))),
                Plain("reseal", "La remettre dans les fondations, avec un mot de plus",
                    "{A} glisse dans la boîte un mot pour les suivants : la date, les noms de tous ceux qui ont aidé, et un dessin du héron. Puis la boîte retourne dans les fondations, sous la nouvelle pierre. Dans cent ans, quelqu'un la trouvera.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_bridge_open", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_bridge_open", EventType.FollowUp, AnyStage, "L'inauguration du pont",
            "Le pont est terminé. Les pierres neuves brillent à côté des anciennes, et, au milieu, la vieille fissure a été gardée, rebouchée de mortier doré. Tout le village est là, sur les deux rives. Le maire, guéri, a préparé un discours de quarante minutes. Sous le pont, le héron attend.",
            new[]
            {
                Plain("heron", "Laisser le héron traverser le premier",
                    "{A} interrompt le discours du maire et s'écarte. Le héron monte sur le pont, très lentement, sur une patte, puis sur l'autre, et le traverse d'un bout à l'autre, dans un silence complet. Arrivé de l'autre côté, le héron s'incline. Le village explose en applaudissements.",
                    E(new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Compassion, 2))),
                Try("speech", "Faire un discours de deux phrases", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "« Ce pont a vécu, et nous aussi. Merci à tous. » Deux phrases. Le maire, dont le discours en comptait cent vingt, applaudit plus fort que tout le monde, et remet à {A} ce qui reste de la bourse des travaux.",
                    "{A} commence ses deux phrases, puis en ajoute une troisième, puis une dixième. Le maire, ravi, prend la suite. L'inauguration dure jusqu'au soir.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveCailloux(20)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Sociability, 1)), Stress(("shy", 20))),
                Plain("run", "Traverser en courant, pour tester",
                    "{A} traverse le pont en courant, aller-retour, puis saute à pieds joints au milieu, sur la fissure dorée. Le pont ne bouge pas d'un millimètre. Le village applaudit, le héron soupire, et l'ours, rassuré, traverse à son tour.",
                    E(new ApplyModifier("fired_up")), Ai((AiAxis.Boldness, 2))),
            }),

        // ancien: le dernier élève (CK3's mentor and student relation events)
        new EventDef("elder_student", EventType.Pulse, Elder, "Le dernier élève",
            "Un petit du village vient chaque jour s'asseoir devant la maison de {A}, sans rien dire, à regarder. Aujourd'hui, le petit se décide enfin : « Tu veux bien m'apprendre ce que tu sais ? » Les pattes de {A} sont vieilles, mais la tête est pleine, et cette question, {A} l'attendait sans le savoir.",
            new[]
            {
                Plain("garden", "Lui apprendre le jardin",
                    "{A} et le petit commencent par le potager : semer, arroser, attendre, surtout attendre. Le petit est impatient. {A} aussi l'était, autrefois. Les premières pousses sortent au bout d'une semaine, et le petit danse autour.",
                    E(new FollowUp("elder_student_grows", 48, 96)), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("stories", "Lui apprendre les histoires du village",
                    "{A} raconte : le pont, la crue, la grande partie de balle d'autrefois. Le petit écoute, répète, se trompe, recommence. Bientôt, le petit raconte les histoires mieux que {A}, avec des détails en plus.",
                    E(new FollowUp("elder_student_grows", 48, 96)), Ai((AiAxis.Sociability, 1))),
                Try("patience", "Lui apprendre la patience, la plus difficile des leçons", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Première leçon : regarder l'escargot traverser la place. En entier. Le petit tient dix minutes, puis vingt, puis l'heure entière. À la fin, le petit dit : « L'escargot a des rayures. Je ne l'avais jamais vu. » {A} sourit : c'est exactement la leçon.",
                    "Le petit tient trois minutes avant de courir après l'escargot pour l'aider à avancer. L'escargot, vexé, rebrousse chemin. {A} rit si fort que la leçon est reportée.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("elder_student_grows", 48, 96)), E(new FollowUp("elder_student_grows", 48, 96)),
                    Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("elder_student_grows", EventType.FollowUp, AnyStage, "Le petit qui grandit",
            "Les saisons passent. Le petit a grandi d'une tête. Ce matin, pour la première fois, c'est le petit qui montre quelque chose à {A} : une façon de faire que {A} ne connaissait pas, plus rapide, plus maligne. Le petit attend, un peu inquiet, ce que {A} va en dire.",
            new[]
            {
                Plain("praise", "Le féliciter, et apprendre à son tour",
                    "{A} essaie la nouvelle façon, la rate, recommence, et réussit. « Tu m'as appris quelque chose. » Le petit devient tout rouge de fierté. Le soir, {A} y repense longtemps : le plus beau cadeau d'un élève, c'est de dépasser son maître.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("cherished"), new FollowUp("elder_student_leaves", 72, 120)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, 1)),
                    Stress(("arrogant", 20))),
                Plain("old_way", "Rappeler gentiment que l'ancienne méthode a fait ses preuves",
                    "{A} montre à nouveau l'ancienne méthode, lentement, en expliquant pourquoi. Le petit écoute, hoche la tête, et garde les deux méthodes. Un jour, le petit choisira. C'est aussi ça, apprendre.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("elder_student_leaves", 72, 120)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("elder_student_leaves", EventType.FollowUp, AnyStage, "Le départ du petit",
            "Le petit, qui n'est plus si petit, frappe à la porte de {A} avec un baluchon. Une grande école, loin, au-delà de la rivière, a accepté le petit. Le départ est demain matin. Le petit tient quelque chose, caché derrière le dos.",
            new[]
            {
                Plain("notebook", "Lui offrir ce qu'on a de plus précieux",
                    "{A} va chercher dans le tiroir un vieux carnet plein de notes, de dessins et de taches de confiture : tout ce que {A} a appris, depuis toujours. Le petit le serre contre son cœur. Puis sort de derrière son dos un trèfle à quatre feuilles, trouvé, jure le petit, après trois jours de recherches.",
                    E(new GiveItem("col.trefle_quatre"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
                Plain("walk", "L'accompagner jusqu'au vieux pont",
                    "{A} accompagne le petit jusqu'au vieux pont, à petits pas. Sur le pont, le petit se retourne, fait de grands signes et crie : « Je reviendrai te montrer ! » {A} reste sur la rive jusqu'à ce que le baluchon ne soit plus qu'un point. Le héron, à côté, reste aussi.",
                    E(new LiftNeed(Need.Happiness, 0.2), new ApplyModifier("soothed")), Ai((AiAxis.Sociability, 1))),
            }),
    };
}
