using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 5 story cycles: bébé and ado.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave5Young() => new[]
    {
        // ---- wave 5: story cycles (CK3's story_cycles and activities) — longer scenes, three or four
        // steps, with the rewards CK3 hands out: gold (GiveCailloux), artifacts (GiveItem) and relief.
        // bébé: la cabane (bp2 « And Den They Were », playdates)
        new EventDef("baby_den", EventType.Pulse, Baby, "La cabane",
            "Au fond du jardin, entre deux grosses racines, {A} a trouvé l'endroit parfait pour une cabane. Le toit manque, les murs aussi, et à peu près tout le reste. {A} a déjà ramassé trois brindilles et un vieux torchon : c'est un début. {B} passe par là, les mains dans les poches, l'air de rien, et regarde la cabane avec énormément d'intérêt.",
            new[]
            {
                Plain("together", "Proposer à {B} de construire ensemble",
                    "{A} et {B} passent l'après-midi à porter des branches plus grandes qu'eux. Le torchon devient un toit, une feuille de rhubarbe devient une porte, et un caillou plat devient une table. Au coucher du soleil, la cabane tient debout. Un peu de travers, ce qui lui donne du caractère.",
                    E(new AffinityShift(15), new FollowUp("baby_den_storm", 24, 48)), Ai((AiAxis.Sociability, 2))),
                Try("alone", "La construire {a:tout seul|toute seule}, pour la surprise", new EventChallenge(PlynlingStat.Stewardship, 4),
                    "{A} construit, démolit, reconstruit. Le soir, une cabane minuscule et parfaite se dresse entre les racines, avec une fenêtre ronde et une sonnette en coquille de noix. {B} sonne trois fois avant d'oser entrer.",
                    "La cabane s'effondre chaque fois que {A} pose le toit. À la quatrième fois, {B} vient tenir le mur sans rien dire, et le toit tient enfin.",
                    E(new GrowStat(PlynlingStat.Stewardship), new AffinityShift(5), new FollowUp("baby_den_storm", 24, 48)),
                    E(new AffinityShift(10), new FollowUp("baby_den_storm", 24, 48)), Ai((AiAxis.Rationality, 1))),
                Plain("club", "Fonder un club secret, avec un mot de passe",
                    "{A} décrète que la cabane est un club. Le mot de passe est « confiture ». {B} est {b:admis|admise} après un examen très sévère : dire « confiture » sans rire. Ça prend trois essais.",
                    E(new AffinityShift(10), new ApplyModifier("sly"), new FollowUp("baby_den_storm", 24, 48)), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1)),
                    gate: new TraitGate("bossy")),
            },
            Target: TargetKind.Anyone,
            TargetCondition: t => t.Affinity >= 0),

        new EventDef("baby_den_storm", EventType.FollowUp, AnyStage, "La nuit de l'orage",
            "Cette nuit, le vent souffle fort et la pluie tape aux carreaux. Au fond du jardin, la cabane affronte l'orage toute seule. {A} n'arrive pas à dormir. Et si le toit s'envolait ? Et si {B} y avait oublié son goûter ?",
            new[]
            {
                Try("rescue", "Courir protéger la cabane, sous la pluie", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} file sous la pluie, coince le torchon sous trois grosses pierres et rentre {a:trempé|trempée} jusqu'aux os, mais {a:fier|fière}. Le lendemain, la cabane est le seul endroit sec du jardin. {B} n'en revient pas.",
                    "{A} glisse dans la boue au deuxième pas et rentre aussitôt, {a:couvert|couverte} de terre du museau aux pattes. Au matin, la cabane a perdu son toit, mais gagné une flaque à l'intérieur, que {B} trouve formidable.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("baby_den_party", 24, 48)), E(new ApplyModifier("muddy_paws"), new FollowUp("baby_den_party", 24, 48)),
                    Ai((AiAxis.Boldness, 2))),
                Plain("trust", "Faire confiance à la cabane, et se rendormir",
                    "{A} décide que la cabane est solide, et se rendort. Au matin, le toit est dans le jardin du voisin et la porte dans l'arbre. Mais les murs tiennent, et les murs, c'est le plus important.",
                    E(new FollowUp("baby_den_party", 24, 48)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, -1))),
            },
            Target: TargetKind.Anyone),

        new EventDef("baby_den_party", EventType.FollowUp, AnyStage, "L'inauguration",
            "La cabane est prête pour sa grande inauguration. {A} a invité {B}, l'escargot et, après une longue hésitation, le héron, qui ne tiendra jamais à l'intérieur. Sur la table en caillou plat attendent trois glands, une fraise coupée en quatre et un ruban à couper.",
            new[]
            {
                Plain("ribbon", "Couper le ruban avec {B}",
                    "{A} et {B} coupent le ruban ensemble, avec les dents, faute de ciseaux. L'escargot applaudit lentement. Le héron passe la tête par la fenêtre et déclare la cabane ouverte. C'est officiel : le village compte un bâtiment de plus.",
                    E(new AffinityShift(10), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("dig", "Creuser une cave sous la cabane, « pour l'hiver »",
                    "À la troisième pelletée, la pelle heurte quelque chose de rond et de brillant : une bille ancienne, bleue comme un bout de ciel. {B} la déclare trésor officiel du club, et l'escargot vote pour, au bout d'un quart d'heure.",
                    E(new AffinityShift(5), new GiveItem("col.bille")), Ai((AiAxis.Boldness, 1), (AiAxis.Greed, 1))),
                Plain("snack", "Manger les glands et la fraise, puis faire la sieste",
                    "La fête dure exactement le temps de manger trois glands et une fraise. Ensuite, tout le monde fait la sieste dans la cabane, en tas, même l'escargot. Le héron monte la garde dehors, sur une patte.",
                    E(new LiftNeed(Need.Hunger, 0.2), new ApplyModifier("well_rested")), Ai((AiAxis.Greed, 1), (AiAxis.Energy, -1))),
            },
            Target: TargetKind.Anyone),

        // bébé: le loir du tiroir (the pet_animal story cycle: it moves in, brings gifts, then sleeps)
        new EventDef("baby_dormouse", EventType.Pulse, Baby, "Le loir du tiroir",
            "Depuis trois jours, les chaussettes de {A} disparaissent une par une. Ce matin, {A} ouvre le tiroir et découvre la vérité : un loir, roulé en boule dans un nid de chaussettes. Le loir ouvre un œil, bâille, et se rendort sans la moindre gêne.",
            new[]
            {
                Plain("keep", "Le laisser dormir, et lui donner un nom",
                    "{A} referme le tiroir tout doucement, en laissant une fente pour l'air. Le loir s'appelle désormais Chaussette. Chaussette ne réagit pas à son nom, ni à rien d'autre, d'ailleurs. Chaussette dort.",
                    E(new LiftNeed(Need.Happiness, 0.15), new FollowUp("baby_dormouse_gifts", 48, 96)), Ai((AiAxis.Compassion, 2))),
                Try("explain", "Expliquer poliment que c'est un tiroir à chaussettes", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{A} explique, longtemps, avec des gestes. Le loir écoute les yeux fermés, puis déménage dans le tiroir du dessous, avec les chaussettes. C'est un compromis. Tout le monde a l'air satisfait, surtout le loir.",
                    "Le loir ronfle pendant toute l'explication. À la fin, {A} range les chaussettes qui restent dans une boîte à chapeau, et laisse le tiroir au loir.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_dormouse_gifts", 48, 96)), E(new FollowUp("baby_dormouse_gifts", 48, 96)),
                    Ai((AiAxis.Rationality, 1))),
                Plain("show", "Porter le tiroir jusqu'au café pour le montrer",
                    "{A} porte le tiroir entier jusqu'au café. Le village défile devant le loir comme devant une vitrine. La tortue déclare n'avoir rien vu d'aussi paisible depuis des années, et le loir ne se réveille pas une seule fois.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("baby_dormouse_gifts", 48, 96)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("baby_dormouse_gifts", EventType.FollowUp, AnyStage, "Les cadeaux du loir",
            "Ce matin, devant le tiroir, {A} trouve un gland, posé bien droit. Le lendemain, un deuxième. Le surlendemain, une plume. Le loir dort toujours, ou fait très bien semblant.",
            new[]
            {
                Plain("trade", "Laisser une noisette en échange",
                    "{A} pose une noisette devant le tiroir. Le soir, la noisette a disparu, et un trèfle attend à sa place. Les échanges durent toute la semaine sans que personne ne voie jamais personne. C'est la plus belle correspondance du village.",
                    E(new GiveItem("col.trefle"), new FollowUp("baby_dormouse_sleep", 72, 120)), Ai((AiAxis.Compassion, 1))),
                Try("watch", "Guetter la nuit, pour le prendre sur le fait", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "Minuit. Le tiroir s'entrouvre. Le loir sort sur la pointe des pattes, un gland dans les bras, le pose devant le lit de {A}, le redresse, recule pour juger de l'effet, et retourne se coucher. {A} n'a jamais rien vu d'aussi sérieux.",
                    "{A} s'endort à onze heures et demie, {a:adossé|adossée} au lit. Au réveil, une plume est posée sur son museau.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.gland"), new FollowUp("baby_dormouse_sleep", 72, 120)),
                    E(new GiveItem("col.plume"), new FollowUp("baby_dormouse_sleep", 72, 120)), Ai((AiAxis.Boldness, 1))),
                Plain("box", "Ranger chaque cadeau dans une boîte",
                    "{A} range les cadeaux dans une boîte, par ordre d'arrivée. La boîte se remplit vite. Sur le couvercle, {A} écrit : « Trésor du loir. Ne pas toucher (sauf moi). »",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.gland"), new FollowUp("baby_dormouse_sleep", 72, 120)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_dormouse_sleep", EventType.FollowUp, AnyStage, "La grande sieste",
            "Le loir ne sort plus du tiroir. Ni pour les glands, ni pour les noisettes. {A} soulève la chaussette du dessus : le loir dort, roulé en boule, le museau dans la queue, avec un tout petit sourire. La chouette, consultée, explique qu'un loir peut dormir des mois. C'est ce que les loirs font de mieux.",
            new[]
            {
                Plain("tuck", "Le border avec la chaussette la plus douce",
                    "{A} choisit la chaussette la plus douce, celle à pois, et la pose sur le loir comme une couverture. Le loir soupire dans son sommeil. Chaque soir, avant de se coucher, {A} vérifie le tiroir et dit bonne nuit. Le loir ne répond jamais, mais le tiroir a l'air content.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Compassion, 2))),
                Plain("whisper", "Lui chuchoter de revenir vite",
                    "{A} chuchote au tiroir : « Reviens vite. » Le loir remue une oreille. Quand le loir se réveillera, {A} aura gardé pour lui la meilleure noisette du village, et toutes ses chaussettes, sauf une.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Sociability, 1))),
            }),

        // ado: le grand apprentissage (CK3 childhood education: a guardian, lessons, a final test)
        new EventDef("teen_apprentice", EventType.Pulse, Teen, "Le grand apprentissage",
            "C'est la tradition : à un certain âge, chaque jeune du village choisit un maître pour la saison. Trois portes sont ouvertes à {A}. La chouette de la bibliothèque, qui sait tout et le dit à voix basse. Le hérisson de la gare, qui sait l'heure de tout. Et la pie du marché, qui sait où sont les choses, surtout celles des autres.",
            new[]
            {
                Plain("owl", "Suivre la chouette",
                    "{A} pousse la porte de la bibliothèque. La chouette lève un œil, puis l'autre, et pousse vers {A} une pile de livres plus haute que {A}. « On commence par le premier. Ensuite, on en parle. »",
                    E(new FollowUp("teen_apprentice_owl", 48, 96)), Ai((AiAxis.Rationality, 2))),
                Plain("hedgehog", "Suivre le hérisson",
                    "{A} se présente à la gare. Le hérisson tend une montre, un carnet et un crayon, sans un mot. « Leçon un : on n'est jamais en avance. On est à l'heure, ou on est en retard. » {A} note tout.",
                    E(new FollowUp("teen_apprentice_station", 48, 96)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1))),
                Plain("magpie", "Suivre la pie",
                    "{A} rejoint la pie derrière son tonneau. La pie examine {A} de la tête aux pattes, lui rend un bouton que {A} n'avait pas vu partir, et soupire : « Bon. Tu as tout à apprendre. »",
                    E(new FollowUp("teen_apprentice_market", 48, 96)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 10))),
            }),

        new EventDef("teen_apprentice_owl", EventType.FollowUp, AnyStage, "L'examen de la chouette",
            "La saison touche à sa fin. La chouette a préparé un examen : une seule question, écrite à la plume sur un carton. « Que sais-tu, maintenant, que tu ne savais pas avant ? » La chouette attend. Le silence de la bibliothèque attend aussi.",
            new[]
            {
                Try("recite", "Réciter tout ce qui a été appris, dans l'ordre", new EventChallenge(PlynlingStat.Learning, 7),
                    "{A} récite les étoiles, les rivières, les noms latins des fougères et l'année où le vieux pont a été construit. La chouette écoute jusqu'au bout, puis hoche la tête, une seule fois. Venant d'une chouette, c'est une médaille.",
                    "{A} commence par les étoiles et se perd dans les rivières. La chouette lève une aile : « Tu en sais plus que tu ne crois. Reviens l'an prochain, on vérifiera. »",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired"), new FollowUp("grown_master_owl", 168, 240)), E(new FollowUp("grown_master_owl", 168, 240)), Ai((AiAxis.Rationality, 2))),
                Plain("humble", "Répondre : « Que je ne sais presque rien. »",
                    "La chouette ferme les yeux un long moment. Puis la chouette sort de son tiroir une petite clé et la tend à {A} : la clé de la réserve, où dorment les livres que personne n'a le droit de lire. « Maintenant, tu peux commencer. »",
                    E(new GrowStat(PlynlingStat.Learning), new GiveItem("col.cle_rouillee"), new FollowUp("grown_master_owl", 168, 240)), Ai((AiAxis.Honor, 1), (AiAxis.Rationality, 1)), Stress(("arrogant", 20))),
            }),

        new EventDef("teen_apprentice_station", EventType.FollowUp, AnyStage, "La tournée d'inspection",
            "Dernier jour d'apprentissage. Le hérisson confie à {A} la grande tournée : vérifier chaque horloge du village, du café jusqu'au puits, et revenir à la gare avant le train de 17 h 03. Le hérisson ne regarde pas {A} partir. Le hérisson regarde sa montre.",
            new[]
            {
                Try("run", "Faire la tournée au pas de course", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} vérifie onze horloges, en remet trois à l'heure, réveille le coucou du café et pousse la porte de la gare à 17 h 02. Le hérisson range sa montre et tend à {A} une vieille pièce frappée d'une locomotive. « Pour ta première minute d'avance. »",
                    "L'horloge de la tortue retarde de deux heures, par principe. {A} discute, perd du temps, et arrive à 17 h 05. Le hérisson ne dit rien. Le silence est pire qu'un sermon.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveItem("col.piece_ancienne"), new FollowUp("grown_master_station", 168, 240)), E(new FollowUp("grown_master_station", 168, 240)), Ai((AiAxis.Energy, 2)), Stress(("lazy", 20))),
                Plain("plan", "Tracer d'abord le chemin le plus court sur une carte",
                    "{A} passe une heure à tracer l'itinéraire, puis fait la tournée en marchant, sans jamais courir, et arrive à 17 h 03 pile. Le hérisson regarde le plan, longtemps. Le plan est accroché depuis au mur de la gare, sous verre.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense"), new FollowUp("grown_master_station", 168, 240)), Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("teen_apprentice_market", EventType.FollowUp, AnyStage, "La leçon de la pie",
            "Pour la dernière leçon, la pie emmène {A} au marché, le jour de la plus grande foule. « Aujourd'hui, tu me rapportes trois choses. Peu importe quoi. Mais personne ne doit te voir. Et tout doit être revenu à sa place avant ce soir. »",
            new[]
            {
                Try("borrow", "Emprunter, puis tout rendre sans être {a:vu|vue}", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} emprunte une cuillère, un ruban et la casquette du marchand de miel, les montre à la pie, et rend tout avant le soir sans que personne ne remarque rien. La pie fait la révérence, pour la première fois de sa vie, et offre à {A} une bague trouvée « on ne sait où ».",
                    "Le marchand de miel remarque l'absence de sa casquette au moment où {A} la porte sur la tête. Explications. Excuses. Pot de miel acheté pour se faire pardonner. Derrière le tonneau, la pie rit à s'en étouffer.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveItem("col.bague"), new FollowUp("grown_master_market", 168, 240)), E(new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 20))),
                Plain("refuse", "Refuser : rendre, oui ; prendre, non",
                    "La pie penche la tête. « Bien. C'est la vraie leçon. Tout le monde sait prendre ; les meilleurs savent quand ne pas le faire. » Puis la pie rend à {A}, discrètement, trois boutons perdus depuis le début de la saison.",
                    E(new GiveItem("col.bouton"), new ApplyModifier("clear_conscience"), new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Honor, 2))),
                Plain("notes", "Regarder la pie faire, et prendre des notes",
                    "{A} observe la pie toute la journée. Le soir, le carnet contient trois pages de croquis, deux de théories et une liste intitulée « Comment ne plus jamais se faire avoir ». La pie lit la liste, et ajoute une ligne à la fin.",
                    E(new ApplyModifier("sly"), new FollowUp("grown_master_market", 168, 240)), Ai((AiAxis.Rationality, 1))),
            }),

        // ado: les jeux du village (CK3 tournaments: sign up, train, compete)
        new EventDef("teen_games", EventType.Pulse, Teen, "Les jeux du village",
            "Tous les trois ans, le village organise ses grands jeux. Le moineau a sorti son sifflet, l'ours a monté une estrade, et la liste des épreuves est clouée sur le vieux chêne : course en sac, lancer de pomme de pin, concours de grimaces. Une seule inscription par personne. {A} relit la liste pour la cinquième fois.",
            new[]
            {
                Plain("sack", "S'inscrire à la course en sac",
                    "{A} choisit son sac au marché : ni trop grand, ni trop petit, avec une vague odeur de pomme de terre. Toute la semaine, {A} s'entraîne à sauter dans l'allée. Les voisins, à force, ne lèvent même plus la tête.",
                    E(new FollowUp("teen_games_day", 48, 72)), Ai((AiAxis.Energy, 2))),
                Plain("pinecone", "S'inscrire au lancer de pomme de pin",
                    "{A} ramasse trente pommes de pin, les classe par poids, et lance les plus belles contre le mur du jardin jusqu'à la nuit. Le mur, d'abord étonné, a fini par se faire une raison.",
                    E(new FollowUp("teen_games_day", 48, 72)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("faces", "S'inscrire au concours de grimaces",
                    "{A} passe la semaine devant le miroir. Certaines grimaces font peur, d'autres font pitié. Une seule fait rire le miroir lui-même. C'est celle-là.",
                    E(new FollowUp("teen_games_day", 48, 72)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("teen_games_day", EventType.FollowUp, AnyStage, "Le jour des jeux",
            "Le grand jour est arrivé. Tout le village s'est installé dans l'herbe, des tartes plein les pattes. Le moineau siffle : c'est au tour de {A}. Devant, l'épreuve. Derrière, tout le village. Et au premier rang, le champion en titre, un écureuil qui n'a jamais perdu.",
            new[]
            {
                Try("all_in", "Tout donner, sans réfléchir", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} s'élance, et pendant quelques secondes, plus rien d'autre n'existe. Quand le bruit revient, c'est celui des applaudissements. L'écureuil, beau joueur, serre la patte de {A}. Le moineau accroche une médaille en bouchon au cou de {A}, et l'ours y ajoute une bourse.",
                    "{A} s'élance trop vite et finit dans la haie, sous les applaudissements, car le village applaudit tout. L'écureuil aide {A} à sortir. Le moineau invente un prix spécial : « Le plus bel élan ».",
                    E(new GrowStat(PlynlingStat.Courage), new GiveCailloux(20)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("copy", "Observer l'écureuil, et copier sa technique", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} remarque que l'écureuil se gratte l'oreille gauche avant chaque essai. {A} se gratte l'oreille gauche aussi. Ça marche. Personne ne sait pourquoi, l'écureuil non plus. Deuxième place, à une moustache, et une petite bourse quand même.",
                    "{A} se gratte l'oreille gauche, puis la droite pour faire bonne mesure, et perd complètement le fil. Avant-dernière place. Intrigué, l'écureuil se gratte les deux oreilles à son tour.",
                    E(new GrowStat(PlynlingStat.Intrigue), new GiveCailloux(10)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Honor, -1))),
                Plain("crowd", "Jouer pour le plaisir, en saluant la foule",
                    "{A} fait son épreuve en saluant la foule à chaque pas, sous les acclamations. Classement : septième sur huit. Applaudimètre : premier, de très loin. Le moineau, ému, décerne une mention « Ambiance ».",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 2))),
            }),
    };
}
