using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Growing up: the bébé dream and the ado apprenticeship, whose follow-ups land in adulte. Set in
// Humeval (docs/village-lore.md) since 7.74.2; keys and options are the old catalog's (stored), so
// "owl", "hedgehog", "magpie", "cafe" and "heron" now name Maître Frêne, the Chardon, the Fou,
// the inn and the Mère Lichen.
public static partial class PlynlingEvents
{
    private static EventDef[] GrowingUp() => new[]
    {
        // bébé: « Plus tard, je serai… », told to the Mère Lichen at the Lisière
        new EventDef("baby_grow_up", EventType.Pulse, Baby, "Plus tard, je serai…",
            "{A} surgit d'un buisson de la Lisière, une brindille à la main, et la pointe vers la Mère Lichen : « Quand je serai {a:grand|grande}, je serai… » La Mère Lichen attend la suite, immobile comme un rocher, très intéressée.",
            new[]
            {
                Plain("explore", "« …partir explorer le monde entier ! »",
                    "« Commence par le fond de la Lisière », dit la Mère Lichen. {A} part aussitôt, et revient avec une carte, un ver de terre et de grandes ambitions.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_dream_explore", 336, 384)), Ai((AiAxis.Boldness, 2))),
                Plain("cafe", "« …tenir l'auberge, comme la Courge ! »",
                    "« Alors entraîne-toi », dit la Mère Lichen, en tendant un bol vide. {A} sert une soupe imaginaire, rend une monnaie imaginaire, et réclame un pourboire bien réel.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_dream_cafe", 336, 384)), Ai((AiAxis.Greed, 1), (AiAxis.Sociability, 1))),
                Plain("owl", "« …tout savoir, comme Maître Frêne ! »",
                    "« Alors compte mes galets », dit la Mère Lichen, d'un signe vers le tas devant la porte. {A} commence à compter. Le soleil se couche au galet trois cent quatre.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_dream_owl", 336, 384)), Ai((AiAxis.Rationality, 2))),
                Plain("chief", "« …chef du village, et de tout le monde ! »",
                    "« Très bien, chef », dit la Mère Lichen, sans bouger d'un poil de mousse. {A} donne trois ordres à la Mère Lichen, deux au Fou qui passait et un à la Sommeille. Seul le Fou obéit, mais le Fou allait déjà dans ce sens.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_dream_chief", 336, 384)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)), gate: new TraitGate("bossy")),
            }),

        // ado: le grand apprentissage (CK3 childhood education: a guardian, lessons, a final test)
        new EventDef("teen_apprentice", EventType.Pulse, Teen, "Le grand apprentissage",
            "C'est la coutume : à la Maison des Pousses, chaque pousse choisit un maître pour la saison. Trois portes sont ouvertes à {A}. Maître Frêne, à la Maison commune, qui sait lire, écrire et tout mettre en règlement. Le Chardon, au moulin, qui pèse tout, compliments compris. Et le Fou, qui retrouve tout ce qui est perdu, sauf ses chaussures.",
            new[]
            {
                Plain("owl", "Suivre Maître Frêne",
                    "{A} pousse la porte de la Maison commune. Maître Frêne pose devant {A} une pile de registres plus haute que {A}. « Examinons d'abord le premier. Ensuite, selon la coutume, nous en parlerons. »",
                    E(new FollowUp("teen_apprentice_owl", 48, 96)), Ai((AiAxis.Rationality, 2))),
                Plain("hedgehog", "Suivre le Chardon",
                    "{A} se présente au moulin. Le Chardon tend une balance, un carnet et un crayon, sans un mot, puis grogne : « Leçon un : un sac pèse ce qu'il pèse. Pas à peu près. » {A} note tout.",
                    E(new FollowUp("teen_apprentice_station", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Plain("magpie", "Suivre le Fou",
                    "{A} rejoint le Fou sous l'auvent de l'auberge. Le Fou examine {A} du chapeau aux pieds, rend un bouton que {A} n'avait pas vu partir, et chantonne : « Un bouton. Un. Tu as tout à apprendre. »",
                    E(new FollowUp("teen_apprentice_market", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 10))),
            }),

        new EventDef("teen_apprentice_owl", EventType.FollowUp, AnyStage, "L'examen de Maître Frêne",
            "La saison touche à sa fin. Maître Frêne a préparé un examen : une seule question, écrite à la plume en haut d'une page blanche. « Que sais-tu, maintenant, que tu ne savais pas avant ? » Maître Frêne attend. La Maison commune attend aussi.",
            new[]
            {
                Try("recite", "Réciter tout ce qui a été appris, dans l'ordre", new EventChallenge(PlynlingStat.Learning, 7),
                    "{A} récite les raies et leurs feux, les fêtes et leurs bancs, les règlements du four et l'année où la Sommeille a changé de lit. Maître Frêne écoute jusqu'au bout, puis hoche la tête, une seule fois. Venant de Maître Frêne, c'est une médaille.",
                    "{A} commence par les raies et se perd dans les règlements. Maître Frêne lève la main : « Tu en sais plus que tu ne crois. Il convient de revenir l'an prochain. Nous vérifierons. »",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired"), new FollowUp("grown_master_owl", 168, 240)), E(new FollowUp("grown_master_owl", 168, 240)), Ai((AiAxis.Rationality, 2))),
                Plain("humble", "Répondre : « Que je ne sais presque rien. »",
                    "Maître Frêne se tait un long moment. Puis sort de sa poche une petite clé rouillée et la tend à {A} : la clé du vieux coffre aux registres, ceux que personne n'ouvre plus. « Maintenant, tu peux commencer. »",
                    E(new GrowStat(PlynlingStat.Learning), new GiveItem("col.cle_rouillee"), new FollowUp("grown_master_owl", 168, 240)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1)), Stress(("arrogant", 20))),
            }),

        new EventDef("teen_apprentice_station", EventType.FollowUp, AnyStage, "La tournée des sacs",
            "Dernier jour d'apprentissage. Le Chardon confie à {A} la grande tournée : peser chaque sac de farine livré dans la semaine, du four de Pain-Blond jusqu'à l'auberge, et revenir au moulin avant que la roue s'arrête. Le Chardon ne regarde pas {A} partir. Le Chardon regarde la roue.",
            new[]
            {
                Try("run", "Faire la tournée au pas de course", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} pèse onze sacs, en trouve trois trop légers d'une poignée, et pousse la porte du moulin au dernier tour de roue. Le Chardon range la balance et tend à {A} un vieux caillou, frappé d'une marque que plus personne n'utilise. « Pour ton premier compte juste. »",
                    "La Courge refuse qu'on pèse sa farine, par principe. {A} discute, perd du temps, et arrive quand la roue est déjà arrêtée. Le Chardon ne dit rien. Le silence est pire qu'un sermon.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.piece_ancienne"), new FollowUp("grown_master_station", 168, 240)), E(new FollowUp("grown_master_station", 168, 240)), Ai((AiAxis.Energy, 2)), Stress(("lazy", 20))),
                Plain("plan", "Tracer d'abord le chemin le plus court sur une carte",
                    "{A} passe une heure à tracer l'itinéraire, puis fait la tournée en marchant, sans jamais courir, et arrive au dernier tour de roue, pile. Le Chardon regarde le plan, longtemps. Le plan est cloué depuis à la poutre du moulin, et le Chardon prétend que c'est pour ses comptes.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense"), new FollowUp("grown_master_station", 168, 240)), Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("teen_apprentice_market", EventType.FollowUp, AnyStage, "La leçon du Fou",
            "Pour la dernière leçon, le Fou emmène {A} sur la place du Puits, le jour du marché. Ses poches sont pleines de choses trouvées. « Aujourd'hui, on rend. Trois choses, à leurs trois maisons. Mais personne ne doit te voir. Sinon ce n'est plus un cadeau, c'est une conversation. »",
            new[]
            {
                Try("borrow", "Tout rendre sans être {a:vu|vue}", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} glisse une cuillère sur le rebord de la Courge, un ruban sous la porte de la Bruyère et une pipe dans la poche du Noyer, sans que personne ne remarque rien. Le Fou fait la révérence, pour la première fois de sa vie, et offre à {A} une bague trouvée « on ne sait où ».",
                    "Le Noyer sent une main dans sa poche au moment où {A} y glisse la pipe. Explications. Excuses. Un après-midi aux Longues Raies pour se faire pardonner. Sous l'auvent, le Fou rit à s'en faire envoler une touffe.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.bague"), new FollowUp("grown_master_market", 168, 240)), E(new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
                Plain("refuse", "Refuser : rendre, oui, mais en frappant à la porte",
                    "Le Fou penche la tête. « Bien. C'est l'autre leçon. Tout le monde sait se cacher ; les meilleurs savent quand frapper. » Puis le Fou rend à {A}, l'air de rien, trois boutons perdus depuis le début de la saison.",
                    E(new GiveItem("col.bouton"), new ApplyModifier("clear_conscience"), new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Honor, 2))),
                Plain("notes", "Regarder le Fou faire, et prendre des notes",
                    "{A} suit le Fou toute la journée. Le soir, le carnet contient trois pages de croquis, deux de théories et une liste intitulée « Comment ne plus jamais rien perdre ». Le Fou lit la liste, et ajoute une ligne à la fin : « Sauf ses chaussures. »",
                    E(new ApplyModifier("sly"), new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: échos de « Plus tard, je serai… » (CK3's I Want a Pony!: the dream comes back)
        new EventDef("grown_dream_explore", EventType.FollowUp, AnyStage, "Le fond de la Lisière",
            "En rangeant, {A} retrouve une vieille carte dessinée au crayon : le fond de la Lisière, un ver de terre, une flèche, et en lettres énormes, « LE MONDE ENTIER ». La Mère Lichen avait dit, ce jour-là : « Commence par le fond de la Lisière. » La Lisière est faite depuis longtemps. Le reste du monde, pas encore.",
            new[]
            {
                Try("hills", "Partir enfin, au-delà de la colline", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} part à l'aube, passe la colline derrière les Mouilles, puis la suivante, et découvre un vallon que personne au hameau n'a jamais dessiné. {A} revient le surlendemain, avec une boussole trouvée sur un rocher, et de nouvelles flèches à dessiner.",
                    "{A} part à l'aube, se perd derrière la deuxième colline, et tourne en rond jusqu'au soir. Au retour, la Mère Lichen lève les yeux : « Le monde est grand. Recommence par le fond de la Lisière. » {A} rit, pour la première fois depuis des jours.",
                    E(new GrowStat(PlynlingStat.Courage), new GiveItem("col.boussole")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 2))),
                Plain("map", "Compléter la carte d'enfance, au crayon",
                    "{A} reprend la carte d'enfant et y ajoute, un à un, tous les chemins connus : le Pont-Vieux, le moulin, les Longues Raies, la Côte qui monte vers Roncebelle. Le ver de terre reste au centre, par respect.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), Ai((AiAxis.Rationality, 1))),
                Plain("heron", "Aller montrer la carte à la Mère Lichen",
                    "{A} déplie la carte devant la cabane. La Mère Lichen la regarde très longtemps, puis pose le doigt sur un endroit vide, tout au bord. « Là. Je n'y suis jamais allée. Pas encore. » Depuis, les deux en parlent parfois, assises sur la même pierre.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
            }),

        new EventDef("grown_dream_cafe", EventType.FollowUp, AnyStage, "Le bol ébréché",
            "À l'auberge, la Courge sort de sous le comptoir un petit bol ébréché. « Tu te souviens, mon chou ? Tu servais des soupes imaginaires à la Mère Lichen, et tu réclamais un pourboire. Elle me l'a raconté. Une fois. » La Courge pose le bol devant {A}. « Mes enfants veulent leur samedi. L'auberge aurait bien besoin d'aide. »",
            new[]
            {
                Try("apron", "Prendre le tablier, ce samedi", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "Samedi, {A} sert quarante bols, rend la monnaie juste, et prépare la chope du Noyer avant même que le Noyer n'ouvre la bouche. Le soir, la Courge verse dans le bol ébréché les pourboires de la journée.",
                    "Samedi, {A} renverse trois bols, confond le sel et le sucre, et sert le Noyer deux fois. Le soir, la Courge rit si fort qu'on l'entend du moulin. « Moi, le premier jour, j'ai mis le feu au rideau. »",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(20)), E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1), (AiAxis.Energy, 1))),
                Plain("regular", "Garder le bol, et venir en client",
                    "{A} garde le bol et promet de venir chaque samedi, comme client. Un très bon client, qui laisse toujours un pourboire dans le bol ébréché, posé au bout du comptoir. La Courge fait semblant de ne pas comprendre, et le raconte à tout le monde.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
                Plain("secret", "Demander d'abord le secret de sa soupe",
                    "La Courge regarde à droite, à gauche, puis chuchote le secret à l'oreille de {A}. C'est très simple, et très surprenant. {A} jure de ne jamais le répéter. La Courge, pour une fois, non plus.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("hearty")), Ai((AiAxis.Rationality, 1), (AiAxis.Greed, 1))),
            }),

        new EventDef("grown_dream_owl", EventType.FollowUp, AnyStage, "Trois cent cinq",
            "Un soir, on frappe chez {A}. C'est la Mère Lichen, qui ne quitte jamais la Lisière. « Trois cent cinq », dit-elle. {A} met une seconde à comprendre : le soir où {A} comptait ses galets, le soleil s'était couché au trois cent quatre. La Mère Lichen a attendu tout ce temps pour donner la suite.",
            new[]
            {
                Plain("count", "Aller reprendre le compte, à partir de trois cent six",
                    "{A} suit la Mère Lichen jusqu'au tas et reprend le compte à voix haute. La Mère Lichen s'assoit, très patiente. À la nuit tombée, le compte est fini : sept cent douze. La Mère Lichen hoche la tête, une fois. Le grand mystère d'enfance est résolu, et c'était beau.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), Ai((AiAxis.Rationality, 2))),
                Plain("keep", "Garder la plume qu'elle a posée sur la table",
                    "En partant, la Mère Lichen a laissé une plume de geai sur la table, sans un mot. {A} la glisse dans le livre de chevet, comme marque-page. Chaque soir, en l'ouvrant, {A} lit une page de plus. La Mère Lichen n'en parle jamais.",
                    E(new GiveItem("col.plume"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1))),
                Try("riddle", "Lui répondre par une énigme", new EventChallenge(PlynlingStat.Learning, 8),
                    "« Et toi, combien d'hivers ? » La Mère Lichen ouvre la bouche, la referme, et repart, l'air soucieux. Trois jours plus tard, un galet attend sur le rebord, avec un mot du Fou, qui écrit pour elle : « Pas encore compté. »",
                    "« Combien de pierres compte le Pont-Vieux ? » La Mère Lichen répond aussitôt : « Mille quarante et une. » Le Fou les a comptées pour elle, il y a longtemps.",
                    E(new GrowStat(PlynlingStat.Learning)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("grown_dream_chief", EventType.FollowUp, AnyStage, "Le chef du banquet",
            "Maître Frêne cherche quelqu'un pour organiser le banquet de moisson. Personne ne se propose. Puis le Fou lève la main : « Quand {A} était {a:petit|petite}, {A} m'a donné un ordre, à la Lisière. J'ai obéi. Je propose {A}. » Le conseil, à la surprise générale, approuve.",
            new[]
            {
                Try("lead", "Accepter, et tout diriger", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} donne des ordres à tout le monde, avec tact, cette fois. Le banquet est le plus beau depuis des années. Au premier rang, le Fou porte un ruban : « Premier à avoir obéi ».",
                    "{A} donne trop d'ordres, trop vite. Le banquet finit en désordre, mais en joyeux désordre, et seul le Fou a suivi chaque consigne à la lettre. Ça compte.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("deputy", "Accepter, à condition que le Fou soit adjoint",
                    "Le Fou, ému, prend son rôle très au sérieux, et compte chaque lampion, deux fois, à voix haute. Les préparatifs prennent deux fois plus de temps, et sont deux fois plus soignés.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
                Plain("decline", "Refuser poliment : c'était un rêve d'enfant",
                    "{A} refuse, avec un sourire. Le conseil insiste un peu, pour la forme, puis se tourne vers le Noyer, ravi. Le soir, le Fou passe chez {A} pour dire : « Tu aurais fait un très bon chef. » {A} garde la phrase pour les mauvais jours.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, -1))),
            }),

        // adulte et ancien: échos du grand apprentissage (the old master comes back)
        new EventDef("grown_master_owl", EventType.FollowUp, AnyStage, "Le coffre aux registres",
            "Maître Frêne frappe chez {A}, un rouleau sous le bras. « Je monte trois jours à Roncebelle, comparer nos registres avec ceux de l'Intendant. La Maison commune a besoin de quelqu'un qui sache dire « examinons ». » Maître Frêne tend la clé du vieux coffre, celui que personne n'ouvre. « Il ne convient pas de te demander de ne pas l'ouvrir. »",
            new[]
            {
                Plain("guard", "Garder la Maison commune, sans ouvrir le coffre",
                    "{A} garde la Maison commune trois jours, règle deux querelles de bancs, et ne touche pas au coffre. Au retour, Maître Frêne vérifie la poussière sur la serrure, intacte, et sourit. « Tu as retenu la leçon d'autrefois. »",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("read", "Ouvrir le coffre, et lire un seul registre", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} ouvre le coffre et prend le registre du dessus : c'est le premier de Maître Frêne, écrit à l'âge de {A}, avec un règlement pour l'ordre des goûters. {A} le lit d'une traite, et le remet exactement à sa place. Au retour, Maître Frêne regarde {A} et dit seulement : « Alors, tu sais. »",
                    "{A} ouvre le coffre. Un nuage de poussière en sort, avec une phalène très âgée et très vexée, qui suit {A} pendant trois jours. Au retour, Maître Frêne l'inscrit au registre, à la colonne des visiteurs.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, -1))),
            }),

        new EventDef("grown_master_station", EventType.FollowUp, AnyStage, "Le moulin enrhumé",
            "Le Chardon a attrapé un rhume, le premier de sa carrière. Couché, une tisane de la Sauge à côté et la mine outrée, le Chardon tend à {A} la balance, le carnet et la clé du moulin. « La grande mouture de l'année. Tu te souviens de la leçon un ? » {A} s'en souvient : un sac pèse ce qu'il pèse.",
            new[]
            {
                Try("station", "Tenir le moulin toute la journée, comme le Chardon", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} ouvre le moulin à l'aube, pèse chaque sac, chasse un canard du bief, et rend la farine juste à tout le hameau. Le soir, {A} rapporte le carnet au Chardon, qui le relit, regarde {A}, et éternue de fierté.",
                    "{A} se trompe d'une poignée sur le sac de Pain-Blond. Personne ne remarque rien. Mais au lit, le Chardon a relu le carnet, et a compté. Le Chardon n'en dira jamais rien. Mais le Chardon sait.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(15)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("soup", "Tenir le moulin, et porter la soupe entre deux sacs",
                    "Entre chaque sac, {A} monte porter une soupe, une tisane, des nouvelles du bief. Le Chardon finit par guérir, surtout pour pouvoir grogner tranquille. La mouture est finie à temps. À peu près.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_master_market", EventType.FollowUp, AnyStage, "Le grand sac du Fou",
            "Le Fou vide son grenier, sous le toit de l'auberge. Il en sort un grand sac. « Tout ce que j'ai trouvé sans jamais trouver à qui c'était. Trente ans. Avant l'hiver, je veux que tout soit rentré à la maison. Tu m'aides ? » Le sac fait un bruit de cuillères.",
            new[]
            {
                Try("night", "Tout rendre en une nuit, sans être {a:vu|vue}", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "Toute la nuit, {A} et le Fou déposent sur les rebords les cuillères, les boutons, les dés à coudre et une paire de lunettes que Maître Frêne cherchait depuis le grand hiver. Au matin, le hameau retrouve trente ans d'objets perdus. Le Fou laisse à {A} une bague. « Celle-là, elle était dans un nid de geai. Elle est à toi. »",
                    "{A} et le Fou se font surprendre par le Noyer, au moment de remettre sa propre pipe sur son propre rebord. Long silence. Puis le Noyer éclate de rire, et aide à rendre le reste.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.bague")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Honor, 1), (AiAxis.Boldness, 1))),
                Plain("stall", "Tout rendre au grand jour, au marché de la place",
                    "{A} étale tout sur une table, au milieu du marché, avec une pancarte : « Retrouvé par le Fou ». Le hameau vient chercher ses affaires en riant. Le Fou, très gêné, reçoit en échange tant de cadeaux que son grenier déborde à nouveau. Même le Chardon dit merci, après avoir grogné.",
                    E(new ApplyModifier("clear_conscience"), new ApplyModifier("cherished")), Ai((AiAxis.Honor, 2), (AiAxis.Sociability, 1))),
            }),
    };
}
