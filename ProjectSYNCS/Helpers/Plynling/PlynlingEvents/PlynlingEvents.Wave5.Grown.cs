using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 5 story cycles: adulte, ancien.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave5Grown() => new[]
    {
        // adulte et ancien: la carpe dorée (CK3's hunt for a mystical animal: rumour, the dawn, the tale)
        new EventDef("grown_golden_carp", EventType.Pulse, Grown, "La carpe dorée",
            "Au café, le vieux blaireau raconte, pour la centième fois, qu'une carpe dorée vit au fond de l'étang. Une carpe énorme, centenaire, qui ne remonte qu'à l'aube des jours de brume, et qui aurait avalé, dans sa jeunesse, la clé de l'ancienne mairie. Personne ne le croit. Ce matin, {A} n'en est pas si {a:sûr|sûre}.",
            new[]
            {
                Try("inquire", "Interroger les anciens, un par un", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "La tortue l'a vue une fois. Le héron dit ne l'avoir jamais vue, ce qui, chez le héron, veut dire oui. L'escargot a une carte dessinée de mémoire, avec une croix à l'endroit exact. Au prochain matin de brume, {A} saura où regarder.",
                    "Chacun a sa version : la carpe est dorée, argentée, invisible, ou c'est un caillou. {A} rentre avec quatre légendes et aucune piste, mais l'envie d'y aller quand même.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_golden_carp_dawn", 24, 72)), E(new FollowUp("grown_golden_carp_dawn", 24, 72)),
                    Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
                Plain("watch", "Partir guetter, dès le prochain matin de brume",
                    "{A} prépare un thermos, une couverture et un carnet. Le blaireau regarde les préparatifs par-dessus sa tasse et marmonne quelque chose qui ressemble beaucoup à « enfin quelqu'un ».",
                    E(new FollowUp("grown_golden_carp_dawn", 24, 72)), Ai((AiAxis.Boldness, 1))),
                Plain("doubt", "Hausser les épaules : une carpe reste une carpe",
                    "{A} hausse les épaules et commande une autre tartine. Le blaireau soupire. Le soir même, en passant près de l'étang, {A} croit voir un reflet doré sous la surface. Sûrement la lune.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2)), Stress(("zealous", 20))),
            }),

        new EventDef("grown_golden_carp_dawn", EventType.FollowUp, AnyStage, "Le matin de brume",
            "L'aube se lève sur l'étang, et la brume est si épaisse qu'on ne voit plus l'autre rive. {A} attend sur la berge depuis une heure, une couverture sur les épaules. Puis l'eau se ride. Une forme immense remonte, lentement, et une écaille d'or perce la brume.",
            new[]
            {
                Try("still", "Ne plus bouger, ne plus respirer", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} ne bouge pas. La carpe remonte jusqu'à deux pattes de la berge et regarde {A} d'un œil rond, vieux comme l'étang. Puis la carpe ouvre la bouche, dépose sur la vase quelque chose de rond et de terni, et replonge : une pièce ancienne.",
                    "{A} éternue. La carpe disparaît dans un remous d'or, et la brume se referme. Sur l'eau, un cercle s'élargit, puis plus rien. Mais {A} l'a vue. Pour de vrai.",
                    E(new GrowStat(PlynlingStat.Courage), new GiveItem("col.piece_ancienne"), new FollowUp("grown_golden_carp_tale", 24, 48)),
                    E(new FollowUp("grown_golden_carp_tale", 24, 48)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, -1))),
                Plain("offer", "Lui lancer la moitié de sa tartine",
                    "{A} lance la moitié de sa tartine. La carpe remonte, l'avale d'une bouchée, et reste un moment à la surface, comme pour remercier. Puis la carpe replonge, et l'étang redevient un étang. {A} rentre avec un secret, et l'autre moitié de la tartine.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_golden_carp_tale", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("sketch", "La dessiner dans le carnet, vite", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} dessine à toute vitesse : les écailles, la nageoire déchirée, l'œil. C'est le plus beau dessin que {A} ait jamais fait. Au café, le blaireau le regarde longtemps, puis l'accroche au-dessus du comptoir, sans un mot.",
                    "Le temps d'ouvrir le carnet, la carpe est repartie. Le dessin ressemble à une saucisse avec des yeux. {A} le garde quand même.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired"), new FollowUp("grown_golden_carp_tale", 24, 48)),
                    E(new FollowUp("grown_golden_carp_tale", 24, 48)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_golden_carp_tale", EventType.FollowUp, AnyStage, "Ce qu'on raconte au café",
            "Au café, tout le monde veut savoir. Le blaireau a gardé la meilleure place pour {A}, et la tortue a servi le chocolat avant même la commande. Toute la salle se tait quand {A} s'assoit.",
            new[]
            {
                Try("tell", "Tout raconter, avec les détails", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} raconte la brume, l'œil, l'écaille d'or. Le café retient son souffle jusqu'au bout. Le blaireau, pour la première fois depuis des années, n'a rien à ajouter. Le soir même, la légende a un nouveau chapitre, et {A} un surnom : « {a:celui|celle} qui a vu la carpe ».",
                    "{A} raconte trop vite, mélange la brume et la tartine, et la carpe finit par avoir des moustaches. Le café rit beaucoup. Plus personne ne sait ce qui est vrai, ce qui, pour une légende, est très bon signe.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2))),
                Plain("secret", "Garder le secret de la carpe",
                    "{A} sourit et ne dit rien. « Une carpe ? Quelle carpe ? » Le blaireau plisse les yeux, puis sourit à son tour : le blaireau a compris. Certaines légendes vivent mieux quand on ne les raconte pas.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        // adulte et ancien: le mystère des cuillères (CK3's court mystery story cycle, with nobody hurt)
        new EventDef("grown_spoons", EventType.Pulse, Grown, "Le mystère des cuillères",
            "Depuis une semaine, les cuillères disparaissent. Une chez la tortue, deux chez l'ours, la petite cuillère à confiture de la chouette. Ce matin, c'est la cuillère préférée de {A} qui manque à l'appel. Au café, tout le monde soupçonne tout le monde, et le hérisson a commencé à cacher la sienne dans sa chaussette.",
            new[]
            {
                Try("map", "Mener l'enquête, carnet en main", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} note chaque disparition sur une carte du village. Les croix dessinent une ligne bien droite, qui part des maisons et file vers la rivière. Les cuillères ne s'envolent pas : quelqu'un les emporte, toujours dans la même direction.",
                    "{A} interroge tout le village et ne récolte que des soupçons. La pie a un alibi ; l'escargot aussi, très lent. Mais en rentrant, {A} trouve devant sa porte une piste de petites traces mouillées.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_spoons_river", 24, 48)), E(new FollowUp("grown_spoons_river", 24, 48)),
                    Ai((AiAxis.Rationality, 2))),
                Try("bait", "Laisser une cuillère en appât, et guetter", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} pose une cuillère brillante sur le rebord de la fenêtre et se cache derrière le rideau. À minuit, une patte mouillée l'attrape et file. {A} a tout vu : quelque chose de brun, de souple, qui sent la rivière.",
                    "{A} s'endort derrière le rideau. Au matin, l'appât a disparu. Le rideau aussi.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("grown_spoons_river", 24, 48)), E(new FollowUp("grown_spoons_river", 24, 48)),
                    Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("fork", "Manger sa soupe à la fourchette, en attendant",
                    "{A} décide que ce n'est pas si grave. Manger de la soupe à la fourchette prend du temps, mais apprend la patience. Le village, de son côté, continue à s'accuser mutuellement, avec de plus en plus d'imagination.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("grown_spoons_river", EventType.FollowUp, AnyStage, "Sous les saules",
            "La piste mène au bord de la rivière, sous les saules. De là monte un son étrange : un tintement léger, comme des cuillères qui se cognent doucement dans le vent. Entre les branches, une jeune loutre est assise au milieu d'un tas de cuillères. En voyant {A}, la loutre sursaute.",
            new[]
            {
                Plain("ask", "S'asseoir, et demander pourquoi",
                    "La loutre explique, tout bas. La nuit, sous les saules, le vent fait peur. Alors la loutre a fabriqué un carillon, pour que la nuit fasse de la musique au lieu de faire peur. Le carillon est magnifique, mais incomplet : une cuillère manque encore.",
                    E(new FollowUp("grown_spoons_end", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("firm", "Exiger que tout soit rendu", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} parle fermement, sans crier. La loutre baisse les oreilles et promet de tout rendre avant demain. Puis, tout bas, la loutre parle du carillon, et de la peur du vent. La fermeté de {A} fond un peu.",
                    "La loutre plonge et disparaît avec la cuillère préférée de {A}. Le lendemain, la cuillère est revenue sur le paillasson, propre, avec un petit mot : « Pardon. »",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_spoons_end", 24, 48)), E(new FollowUp("grown_spoons_end", 24, 48)),
                    Ai((AiAxis.Honor, 1), (AiAxis.Compassion, -1)), Stress(("compassionate", 20))),
                Plain("help", "Aider à finir le carillon",
                    "{A} s'assoit et aide à accrocher les dernières cuillères. Le soir tombe. Le vent se lève, et sous les saules, la nuit se met à tinter doucement, comme une berceuse. La loutre s'endort au milieu d'une phrase.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_spoons_end", 24, 48)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_spoons_end", EventType.FollowUp, AnyStage, "Le carillon des saules",
            "Le village a fini par apprendre la vérité sur les cuillères. Ce soir, tout le monde est descendu sous les saules : la tortue, l'ours, la chouette, et la jeune loutre, toute petite au milieu de la foule, qui n'ose pas lever les yeux. Tout le monde attend de voir ce que {A} va dire.",
            new[]
            {
                Plain("give", "Accrocher sa propre cuillère au carillon",
                    "{A} sort sa cuillère de sa poche et l'accroche au carillon. Un silence. Puis la tortue accroche la sienne. Puis l'ours, puis la chouette. Au bout d'une heure, le carillon des saules compte une cuillère par habitant, et la loutre pleure de joie dans les bras de l'ours.",
                    E(new ApplyModifier("clear_conscience"), new LiftNeed(Need.Happiness, 0.25)), Ai((AiAxis.Compassion, 2), (AiAxis.Sociability, 1))),
                Plain("bells", "Rendre les cuillères, et offrir des clochettes à la place",
                    "{A} rend chaque cuillère à sa maison, et revient avec un sac de clochettes achetées au marché. Le nouveau carillon sonne plus clair que l'ancien. Pour dire merci, la loutre offre à {A} un caillou plat, poli par la rivière.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.caillou_plat")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("verdict", "Laisser le village décider",
                    "Le village délibère longuement, au café. Verdict : la loutre gardera le carillon, mais viendra laver la vaisselle du café chaque dimanche, en échange des cuillères. La loutre accepte avec enthousiasme. La tortue n'a jamais eu une vaisselle aussi propre.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Rationality, 1))),
            }),

        // adulte et ancien: le grand banquet (CK3's feast activity: the preparations, an incident, the toast)
        new EventDef("grown_feast", EventType.Pulse, Grown, "Le grand banquet",
            "Voilà des années que personne n'a organisé de vrai banquet au village. {A} vient de décider que ça suffit. Une grande table sous le chêne, des lanternes, de la confiture, des gâteaux, et tout le monde invité. Reste un détail : tout préparer, en trois jours.",
            new[]
            {
                Try("grand", "Voir grand : des lanternes partout, trois desserts", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} court du marché au café, du café au jardin. Trois jours plus tard, la table croule sous les plats, les lanternes pendent à chaque branche, et l'ours a promis de jouer de l'accordéon. Tout est prêt, à une cuillère près.",
                    "Le premier dessert brûle, le deuxième s'effondre, le troisième est mangé par l'escargot pendant la nuit. Mais les lanternes sont magnifiques, et le banquet aura lieu quand même.",
                    E(new GrowStat(PlynlingStat.Stewardship), new FollowUp("grown_feast_evening", 24, 48)), E(new FollowUp("grown_feast_evening", 24, 48)),
                    Ai((AiAxis.Energy, 2), (AiAxis.Greed, -1)), Stress(("lazy", 20))),
                Plain("potluck", "Demander à chacun d'apporter un plat",
                    "{A} passe dans chaque maison avec une liste. La tortue apportera ses tartelettes, l'ours ses beignets, la chouette « quelque chose de surprenant ». {A} n'a plus qu'à mettre la table, et à s'inquiéter du « surprenant ».",
                    E(new FollowUp("grown_feast_evening", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Plain("small", "Un petit banquet, juste les plus proches",
                    "{A} dresse une petite table pour six, avec une nappe à carreaux et une seule lanterne. Ce ne sera pas le banquet du siècle. Ce sera peut-être mieux.",
                    E(new FollowUp("grown_feast_evening", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, -1))),
            }),

        new EventDef("grown_feast_evening", EventType.FollowUp, AnyStage, "Le soir du banquet",
            "Les lanternes s'allument une à une. Les invités arrivent, les plats circulent, les rires montent sous le chêne. Puis, au milieu du repas, l'ours renverse le grand pot de soupe, en plein sur la nappe. Silence. Tous les regards se tournent vers {A}.",
            new[]
            {
                Plain("slide", "Éclater de rire, et glisser sur la soupe",
                    "{A} éclate de rire et glisse sur la soupe jusqu'au bout de la table. Une seconde plus tard, tout le village glisse, l'ours le premier. On parlera longtemps du « banquet de la soupe », et toujours en riant.",
                    E(new LiftNeed(Need.Happiness, 0.2), new FollowUp("grown_feast_toast", 2, 4)), Ai((AiAxis.Sociability, 2), (AiAxis.Rationality, -1))),
                Try("save", "Sauver la soirée : nappe propre, plat de réserve", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} change la nappe en trois gestes, sort le plat de réserve et lance une chanson pour couvrir le bruit. Personne n'a presque rien vu. L'ours, soulagé, glisse à {A} un pot de son miel secret.",
                    "{A} tire sur la nappe trop fort, et le reste du repas rejoint la soupe. Long silence. Puis la chouette déclare que c'était « surprenant », et c'est le mot de la soirée.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("hearty"), new FollowUp("grown_feast_toast", 2, 4)), E(new FollowUp("grown_feast_toast", 2, 4)),
                    Ai((AiAxis.Rationality, 1), (AiAxis.Compassion, 1))),
                Plain("comfort", "Aller consoler l'ours",
                    "{A} laisse la soupe et va s'asseoir à côté de l'ours, qui se cache derrière ses grosses pattes. « Une soupe, ça se refait. » L'ours renifle, se redresse, et le banquet reprend, avec un ours encore un peu rouge et très, très reconnaissant.",
                    E(new ApplyModifier("clear_conscience"), new FollowUp("grown_feast_toast", 2, 4)), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("grown_feast_toast", EventType.FollowUp, AnyStage, "Le toast",
            "La nuit est bien avancée. Les lanternes fatiguent, les assiettes sont vides, et quelqu'un réclame un discours. Puis tout le monde. {A} se lève, un verre de jus de pomme à la patte. Le chêne entier se tait.",
            new[]
            {
                Try("speech", "Faire un grand discours", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} parle du village, du chêne, de la soupe, et de chacun, par son nom. À la fin, l'ours se mouche bruyamment et la tortue applaudit avec une rapidité inédite. Le banquet est déclaré le meilleur depuis toujours, et les invités, en partant, laissent une petite bourse sur la table, pour le prochain.",
                    "{A} commence un discours, oublie la suite, et finit par dire : « Voilà. » La salle applaudit quand même, très fort, parce que c'était sincère.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveCailloux(15)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2)), Stress(("shy", 20))),
                Plain("short", "Lever son verre : « À nous. »",
                    "« À nous. » C'est tout. Le chêne entier répète « À nous ! », et les verres s'entrechoquent jusqu'à l'aube. Les discours les plus courts sont ceux dont on se souvient.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Rationality, 1))),
                Plain("thanks", "Remercier chaque invité, un par un",
                    "{A} fait le tour de la table et remercie chacun, avec un mot pour chaque plat, même le « surprenant ». Ça prend une heure. Personne ne trouve le temps long. En partant, la chouette glisse dans la poche de {A} une plume, « pour écrire le prochain menu ».",
                    E(new GiveItem("col.plume"), new ApplyModifier("cherished")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        // ancien: voir la mer (CK3 pilgrimages and travel events)
        new EventDef("elder_sea", EventType.Pulse, Elder, "Voir la mer",
            "Toute une vie au village, et {A} n'a jamais vu la mer. La cigogne en parle comme d'un étang qui n'aurait pas de bout. Ce matin, {A} a préparé un baluchon : une écharpe, trois tartines, une carte dessinée par le héron. La route est longue, les pattes sont vieilles. Mais la décision est prise.",
            new[]
            {
                Plain("alone", "Partir {a:seul|seule}, à son rythme",
                    "{A} part avant l'aube, sans prévenir, une tartine à la patte. Sur le vieux pont, le héron regarde passer le baluchon et ne dit rien. C'est sa plus belle façon de dire bon voyage.",
                    E(new FollowUp("elder_sea_road", 24, 48)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, -1))),
                Plain("stork", "Demander à la cigogne de montrer le chemin",
                    "La cigogne accepte, à condition de voler devant et d'attendre à chaque croisement. Le voyage sera lent : la cigogne s'arrête aussi pour raconter des histoires, et chaque histoire a une suite.",
                    E(new FollowUp("elder_sea_road", 24, 48)), Ai((AiAxis.Sociability, 1))),
                Plain("snail", "Emmener l'escargot, qui n'a jamais voyagé non plus",
                    "L'escargot fait son baluchon en une minute : l'escargot est son propre baluchon. Les deux voyageurs partent côte à côte, à la vitesse de l'escargot. Personne n'est pressé. C'est tout l'intérêt.",
                    E(new FollowUp("elder_sea_road", 24, 48)), Ai((AiAxis.Compassion, 1), (AiAxis.Energy, -1))),
            }),

        new EventDef("elder_sea_road", EventType.FollowUp, AnyStage, "Sur la route",
            "Le troisième jour, la route traverse une lande battue par le vent. Au bord du chemin, une jeune cane pleure, assise sur sa valise : partie trop tard, et toute sa bande a déjà filé vers la mer. La nuit tombe, et le vent forcit.",
            new[]
            {
                Plain("shelter", "Partager l'abri d'un rocher, et la dernière tartine",
                    "{A} trouve un rocher creux, partage la dernière tartine et raconte des histoires du village jusqu'à ce que la cane s'endorme. Au matin, le vent est tombé. La cane marche à côté de {A}, et ne pleure plus du tout.",
                    E(new ApplyModifier("cherished"), new FollowUp("elder_sea_shore", 24, 48)), Ai((AiAxis.Compassion, 2))),
                Try("night", "Marcher de nuit, pour rattraper la bande", new EventChallenge(PlynlingStat.Courage, 8),
                    "{A} marche toute la nuit, la cane sur le dos. À l'aube, au sommet d'une dune, la bande est là, endormie. La cane court la rejoindre, puis revient serrer {A} très fort. Derrière la dune monte un bruit immense et doux : la mer.",
                    "La nuit est trop noire et les pattes trop vieilles. {A} et la cane dorment sous un buisson, à l'abri du vent. Au matin, la bande est revenue les chercher.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("elder_sea_shore", 12, 24)), E(new FollowUp("elder_sea_shore", 24, 48)), Ai((AiAxis.Boldness, 2))),
                Try("geese", "Héler les oies qui passent, pour demander de l'aide", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "{A} agite l'écharpe. Une escadrille d'oies descend, écoute l'histoire, et repart avec la cane, en promettant de retrouver sa bande avant midi. La dernière oie, en partant, laisse tomber une plume aux pattes de {A}.",
                    "Les oies passent trop haut et n'entendent rien. {A} et la cane continuent ensemble, à pied, et la cane apprend en chemin trois chansons du village.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new GiveItem("col.plume"), new FollowUp("elder_sea_shore", 24, 48)), E(new FollowUp("elder_sea_shore", 24, 48)),
                    Ai((AiAxis.Sociability, 1))),
            }),

        new EventDef("elder_sea_shore", EventType.FollowUp, AnyStage, "La mer",
            "Au sommet de la dernière dune, {A} s'arrête. Devant, la mer : immense, grise et bleue, qui respire. Le vent sent le sel. Les vagues arrivent une par une, comme si l'océan venait saluer {A} en personne. La cigogne avait raison : un étang qui n'a pas de bout.",
            new[]
            {
                Plain("waves", "Tremper les pattes dans les vagues",
                    "{A} descend jusqu'à l'eau et laisse la première vague mouiller ses vieilles pattes. C'est froid. C'est salé. C'est exactement comme dans les histoires, en beaucoup plus grand. {A} reste là jusqu'au soir à compter les vagues, perd le compte, et recommence.",
                    E(new LiftNeed(Need.Happiness, 0.3), new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
                Plain("pebble", "Choisir un galet à rapporter au village",
                    "{A} cherche longtemps, et choisit un galet tout rond, poli par des milliers de vagues. Au retour, le galet passera de patte en patte au café, et chacun, en le tenant, entendra un peu la mer.",
                    E(new GiveItem("col.galet"), new ApplyModifier("inspired")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("sit", "S'asseoir dans le sable, et ne plus penser à rien",
                    "{A} s'assoit dans le sable tiède et, pour la première fois depuis très longtemps, ne pense plus à rien. Le soleil se couche dans l'eau. Au village, plus tard, {A} dira simplement : « C'était grand. » Et ce sera tout, et ce sera assez.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1))),
            }),
    };
}
