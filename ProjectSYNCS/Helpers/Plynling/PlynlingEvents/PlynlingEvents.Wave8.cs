using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8: single events by stage.
public static partial class PlynlingEvents
{
    private static EventDef[] Wave8() => new[]
    {
        // ==== wave 8 (CK3: playdates, festivals, court guests, the imperial examination, the chariot race,
        // the party baron, natural disasters, adult education, debates; echoes are long follow-ups that land
        // in adulte: bébé and ado last a week each)

        // ---- wave 8: bébé
        new EventDef("baby_swing", EventType.Pulse, Baby, "La balançoire",
            "Sur la place, la balançoire est prise. Un jeune blaireau s'y balance depuis le matin et crie à chaque passage : « Encore dix ! » On en est au quarantième « encore dix ». {A} attend, les pattes croisées.",
            new[]
            {
                Try("push", "Proposer de pousser le blaireau, très haut", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "{A} pousse, le blaireau monte, crie de joie, et au retour propose de lui-même d'échanger. Depuis, les deux se poussent à tour de rôle, en comptant jusqu'à dix pour de vrai.",
                    "{A} pousse trop fort. Le blaireau atterrit dans le bac à sable, ravi, et réclame qu'on recommence. La balançoire reste prise.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Try("stand", "Se planter devant, et réclamer son tour", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} se plante devant la balançoire, les poings sur les hanches. Le blaireau ralentit, réfléchit, et descend : « Bon. Dix. Pas un de plus. » {A} en fait onze, par principe.",
                    "{A} se plante devant, mais un peu trop près. La balançoire repasse. {A} aussi, dans l'autre sens, sur les fesses.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2))),
                Plain("rope", "Trouver mieux : une corde et une branche basse",
                    "{A} noue une corde à une branche basse et invente sa propre balançoire, qui tourne, en plus. Le soir, le blaireau fait la queue derrière.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_bath", EventType.Pulse, Baby, "L'heure du bain",
            "C'est l'heure du bain. {A} a disparu. Une bassine d'eau tiède attend au milieu de la pièce, et sous le lit, deux yeux brillent dans le noir, très décidés.",
            new[]
            {
                Plain("ducks", "Accepter, à condition d'emmener un canard en bois",
                    "{A} entre dans l'eau avec un canard en bois, puis un deuxième, puis une flotte entière. Le bain dure une heure et finit en bataille navale. Le plancher est trempé ; {A} brille comme un sou neuf.",
                    E(new LiftNeed(Need.Hygiene, 0.3)), Ai((AiAxis.Energy, 1), (AiAxis.Sociability, 1))),
                Try("escape", "Filer par la fenêtre", new EventChallenge(PlynlingStat.Intrigue, 4),
                    "{A} file par la fenêtre, traverse le jardin et plonge dans le tas de feuilles. Personne ne trouve {A} avant le dîner. Les feuilles, en revanche, ont trouvé un locataire.",
                    "{A} file par la fenêtre et atterrit pile dans le tonneau de pluie. Le bain a eu lieu quand même, en plus froid.",
                    E(new GrowStat(PlynlingStat.Intrigue)), E(new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
                Plain("bubbles", "Étudier les bulles, très sérieusement",
                    "{A} passe le bain à observer les bulles : les grosses montent plus vite, les petites restent en bande. À la fin, {A} est propre sans s'en être {a:aperçu|aperçue}.",
                    E(new GrowStat(PlynlingStat.Learning), new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Rationality, 2)), gate: new TraitGate("curious")),
            }),

        new EventDef("baby_dress_up", EventType.Pulse, Baby, "Le petit chef de gare",
            "{A} a trouvé une casquette trop grande, une pomme de pin en guise de montre, et s'est {a:installé|installée} sur le quai. Le hérisson chef de gare observe la scène depuis son guichet, sans un mot. Le train de 10 h 12 arrive dans trois minutes.",
            new[]
            {
                Try("whistle", "Siffler le départ, comme un vrai chef", new EventChallenge(PlynlingStat.Courage, 4),
                    "{A} siffle. Le train part. Pile à l'heure. Le hérisson sort de son guichet, regarde sa montre, regarde {A}, et note quelque chose dans son carnet. On ne saura jamais quoi, mais c'était souligné.",
                    "{A} siffle trop tôt. Le train ne part pas. Les voyageurs applaudissent quand même. Le hérisson soupire, puis siffle à son tour, un peu moins fort, pour ne vexer personne.",
                    E(new GrowStat(PlynlingStat.Courage)), Nothing, Ai((AiAxis.Boldness, 2))),
                Plain("tickets", "Vérifier les billets de tous les voyageurs",
                    "{A} poinçonne les billets avec les dents. L'ours a payé deux fois ; l'escargot n'a pas de billet, mais un abonnement d'avant le pont. Le hérisson, impressionné, laisse {A} tenir le carnet jusqu'au soir.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("ask", "Demander au hérisson comment on devient chef",
                    "Le hérisson réfléchit longuement. « On arrive avant tout le monde. On part après tout le monde. Et on ne court jamais. » {A} répète la phrase toute la journée, en marchant très lentement.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
                Plain("orders", "Donner des ordres à tout le quai",
                    "{A} ordonne aux voyageurs de monter, aux pigeons de descendre et au train d'attendre. Le train n'attend pas. Mais les pigeons, bizarrement, obéissent.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Boldness, 2)), gate: new TraitGate("bossy")),
            }),

        new EventDef("baby_imaginary", EventType.Pulse, Baby, "L'ami invisible",
            "Depuis mardi, {A} a un nouvel ami : Plouf. Plouf est invisible, mange beaucoup de biscuits, et a toujours une place à table. Ce matin, Plouf a une idée, et {A} est {a:seul|seule} à le savoir.",
            new[]
            {
                Plain("cafe", "Commander un chocolat pour Plouf au café",
                    "{A} commande deux chocolats. La tortue en sert deux, sans poser de questions, et essuie la tasse de Plouf avec le même soin que l'autre. Plouf, paraît-il, a tout bu.",
                    E(new LiftNeed(Need.Happiness, 0.15), new FollowUp("baby_imaginary_bye", 72, 120)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("blame", "Accuser Plouf pour les biscuits disparus",
                    "La boîte à biscuits est vide. « C'est Plouf. » Le héron, consulté, confirme que Plouf avait l'air coupable. {A} s'essuie la bouche, discrètement.",
                    E(new GrowStat(PlynlingStat.Intrigue), new FollowUp("baby_imaginary_bye", 72, 120)), Ai((AiAxis.Honor, -1), (AiAxis.Greed, 1))),
                Plain("explore", "Partir avec Plouf explorer le fond du jardin",
                    "Plouf connaît le chemin. Plouf a peur des limaces, alors {A} doit être {a:courageux|courageuse} pour deux. Le soir, {A} raconte l'expédition : Plouf a été très bien.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("baby_imaginary_bye", 72, 120)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
            }),

        new EventDef("baby_imaginary_bye", EventType.FollowUp, AnyStage, "Au revoir, Plouf",
            "Ce matin, {A} met la table et s'arrête, une tasse à la patte. Plouf n'est pas là. {A} cherche sous le lit, derrière la porte, dans la boîte à biscuits. Puis comprend, sans trop savoir comment, que Plouf est parti voir un autre petit, quelque part, qui en a plus besoin.",
            new[]
            {
                Plain("letter", "Écrire une lettre à Plouf",
                    "{A} écrit une lettre de trois mots, avec un dessin de biscuit, et la confie au héron, qui promet de la porter « au bon endroit ». Le héron la range sous son aile, et l'y garde encore.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1))),
                Plain("chair", "Laisser sa chaise libre, au cas où",
                    "{A} laisse la chaise de Plouf libre. Un jour, quelqu'un s'y assoit : un vrai ami, cette fois, qui demande la permission de rester.",
                    E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Sociability, 1))),
                Plain("grown", "Annoncer qu'on est trop grand pour ces choses-là",
                    "{A} annonce à tout le village qu'on est désormais trop {a:grand|grande} pour ce genre de choses. Le soir, en se couchant, {A} dit quand même bonne nuit au plafond. Juste au cas où.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("baby_otter", EventType.Pulse, Baby, "La loutre du ponton",
            "Chaque soir, une vieille loutre s'assoit au bout du ponton et regarde l'eau, sans bouger, jusqu'à la nuit. Personne ne sait ce que la loutre attend. Ce soir, {A} s'approche, à petits pas.",
            new[]
            {
                Plain("sit", "S'asseoir à côté, sans rien dire",
                    "{A} s'assoit. La loutre ne dit rien. {A} non plus. Au bout d'un long moment, la loutre pose une patte sur la tête de {A}, juste une seconde. Puis les deux regardent l'eau jusqu'à la nuit.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 2))),
                Try("ask", "Demander ce qu'on attend, au bout d'un ponton", new EventChallenge(PlynlingStat.Diplomacy, 4),
                    "« Un bateau à voile rouge, parti quand j'avais ton âge. » La loutre sourit. « Je sais bien que le bateau ne reviendra pas. Mais j'aime l'heure où on l'attend. »",
                    "La loutre ne répond pas, mais tend à {A} un caillou plat. {A} le lance : trois ricochets. La loutre hoche la tête, et c'est tout pour ce soir.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), E(new GiveItem("col.caillou_plat")), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("boat", "Fabriquer un petit bateau, avec une voile rouge",
                    "{A} plie une feuille, la colore au jus de framboise, et pose le bateau sur l'eau, devant la loutre. La loutre le regarde partir, très loin. Ce soir-là, pour la première fois, la loutre rentre avant la nuit.",
                    E(new GrowStat(PlynlingStat.Stewardship), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Compassion, 1), (AiAxis.Rationality, 1))),
            }),

        // ---- wave 8: ado
        new EventDef("teen_fox_wedding", EventType.Pulse, Teen, "La noce des renards",
            "Grand soleil, et pourtant la pluie tombe. Au village, tout le monde sait ce que ça veut dire : les renards se marient. La noce traverse le bois en ce moment même, et la tradition est claire : personne ne doit regarder. {A} entend déjà les clochettes, juste derrière les fougères.",
            new[]
            {
                Try("peek", "Écarter les fougères, juste un peu", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} écarte deux fougères. Un long cortège de renards passe sous la pluie dorée, en silence, des clochettes aux oreilles. La mariée tourne la tête, regarde {A} droit dans les yeux, et sourit. {A} ne racontera jamais ça à personne.",
                    "Une fougère craque. Le cortège s'arrête net. Puis un vieux renard sort du rang, pose une part de gâteau de noce sur une souche, devant {A}, et repart. Personne ne sait si c'était une punition.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("inspired")), E(new LiftNeed(Need.Hunger, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
                Plain("count", "Fermer les yeux et compter jusqu'à cent",
                    "{A} ferme les yeux et compte. À quatre-vingt-dix, les clochettes s'éloignent. À cent, la pluie s'arrête. Sur la souche, devant {A}, quelqu'un a laissé une châtaigne bien ronde.",
                    E(new GiveItem("col.chataigne"), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Plain("flowers", "Laisser un bouquet au bord du chemin, sans regarder",
                    "{A} cueille trois fleurs mouillées et les pose au bord du chemin, les yeux fermés. Le lendemain, le bouquet a disparu. À sa place attend une plume rousse.",
                    E(new GiveItem("col.plume"), new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_tug_of_war", EventType.Pulse, Teen, "La corde de la rivière",
            "Chaque année, le village et celui d'en face se disputent la rivière au tir à la corde, d'une rive à l'autre. Les perdants finissent dans l'eau. Cette année, le moineau a désigné {A} pour l'équipe. Sur l'autre rive, l'équipe adverse compte un castor. Un vrai.",
            new[]
            {
                Try("front", "Prendre la tête de la corde", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} tient bon, les pattes plantées dans la boue, et crie le rythme. Le castor glisse, puis toute l'équipe adverse, un par un, dans la rivière. Le village porte {A} en triomphe jusqu'au café, où l'ours offre la tournée et une petite bourse.",
                    "{A} tient bon, très bon, trop bon : quand l'équipe lâche, {A} part {a:seul|seule} dans la rivière. Le castor aide {A} à sortir, ce qui est humiliant, puis tend une serviette, ce qui l'est encore plus.",
                    E(new GrowStat(PlynlingStat.Courage), new GiveCailloux(15)), E(new LiftNeed(Need.Hygiene, 0.2)), Ai((AiAxis.Boldness, 2)), Stress(("craven", 20))),
                Try("anchor", "S'enrouler la corde autour de la taille, en dernier", new EventChallenge(PlynlingStat.Stewardship, 7),
                    "{A} s'enroule la corde autour de la taille et s'assoit. Tout simplement. L'équipe adverse tire, tire, et ne comprend pas. La victoire doit plus au poids qu'à la force, mais une victoire est une victoire.",
                    "{A} s'enroule la corde trois fois autour de la taille, se retrouve {a:ficelé|ficelée} comme un saucisson, puis {a:traîné|traînée} doucement jusqu'au bord de l'eau. On s'arrête juste avant. Le castor, beau joueur, déclare le match nul.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(10)), Nothing, Ai((AiAxis.Rationality, 2))),
                Try("butter", "Beurrer discrètement le bout de corde adverse", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "Le bout adverse glisse comme une anguille. Victoire éclatante. Le soir, le castor renifle sa patte, et regarde {A} longuement. Pas un mot. Ça viendra.",
                    "Le beurre a coulé du mauvais côté. C'est l'équipe de {A} qui glisse dans la rivière, en entier. Personne ne sait d'où vient le beurre. Presque personne.",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("sly")), Nothing, Ai((AiAxis.Honor, -2), (AiAxis.Greed, 1)), Stress(("honest", 30), ("just", 20))),
                Plain("song", "Rester sur la berge, et mener les chants",
                    "{A} invente une chanson sur le castor, qui rime avec « pas si fort », et tout le village la reprend. Le castor, vexé, perd sa concentration. Le moineau se demande si une chanson compte comme une aide extérieure, et décide que non.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_runaway", EventType.Pulse, Teen, "Partir pour toujours",
            "Ce matin, {A} a décidé de quitter le village. Pour toujours. Le baluchon est prêt : deux tartines, un joli caillou rond, une chaussette de rechange. La lettre d'adieu est punaisée sur la porte. Sur son pont, le héron regarde {A} approcher avec beaucoup d'intérêt.",
            new[]
            {
                Plain("hill", "Partir loin, jusqu'au bout du chemin",
                    "{A} marche jusqu'au bout du chemin, puis un peu plus loin, jusqu'à la colline d'où l'on voit tout le village, minuscule. Les tartines sont mangées là-haut. Puis le village a l'air de faire signe, et {A} rentre pour le dîner, avec une nouvelle façon de le regarder.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Try("raft", "Construire un radeau, et descendre la rivière", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "Le radeau flotte. Le radeau avance. Le radeau, après un dernier virage, accoste… au ponton du village, par l'autre côté. {A} débarque sous les applaudissements de l'escargot, qui croyait à une course.",
                    "Le radeau flotte trois secondes. {A} rentre à la nage, le baluchon sur la tête, et décrète que partir pour toujours peut attendre l'été.",
                    E(new GrowStat(PlynlingStat.Stewardship)), E(new LiftNeed(Need.Hygiene, 0.15)), Ai((AiAxis.Energy, 1), (AiAxis.Rationality, 1))),
                Plain("heron", "Demander d'abord conseil au héron",
                    "« Partir pour toujours, dit le héron, ça se prépare. Reviens demain. » {A} revient le lendemain, puis le surlendemain. Au bout d'une semaine, {A} a oublié de partir, mais connaît par cœur tous les oiseaux migrateurs.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("teen_crush", EventType.Pulse, Teen, "Un nom dans la marge",
            "Depuis quelque temps, {A} rougit quand {B} passe. {A} a écrit le nom de {B} dans la marge d'un livre de la bibliothèque, puis l'a effacé, puis l'a réécrit, plus petit. La chouette a remarqué. La chouette remarque tout.",
            new[]
            {
                Plain("flower", "Laisser une fleur devant la porte de {B}, sans signer",
                    "{A} dépose la fleur à l'aube et s'enfuit. {B} la trouve, la met dans un verre d'eau, et demande à tout le village qui l'a laissée. Personne ne sait. {A}, {a:interrogé|interrogée}, rougit jusqu'aux oreilles et parle très vite de la météo.",
                    E(new AffinityShift(10)), Ai((AiAxis.Compassion, 1), (AiAxis.Boldness, -1))),
                Try("talk", "Aller parler à {B}, de n'importe quoi", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} s'approche et parle des nuages pendant dix minutes. {B} écoute, répond, rit au bon moment. Ce n'est pas une déclaration. C'est mieux : c'est une conversation.",
                    "{A} s'approche, ouvre la bouche, et dit « bonjour la météo ». {B} cligne des yeux. {A} s'en va très vite, très dignement, dans la mauvaise direction.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(15)), E(new AffinityShift(5)), Ai((AiAxis.Boldness, 2)), Stress(("shy", 20))),
                Plain("owl", "Demander à la chouette un livre sur la question",
                    "La chouette pose devant {A} un recueil de poèmes, un seul, avec un marque-page déjà glissé à la bonne page. {A} lit le poème trois fois. Personne ne saura jamais lequel.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known,
            TargetCondition: t => !t.EitherInCouple),

        // ---- wave 8: adulte et ancien
        new EventDef("grown_moon_party", EventType.Pulse, Grown, "La fête de la lune",
            "{B} a glissé une invitation sous la porte de {A} : « Pleine lune, sur la colline. Apporte une couverture et une histoire. » Ce soir, la lune est énorme, posée sur la colline comme une tarte sur une table. {B} a déjà étendu une nappe, et attend.",
            new[]
            {
                Plain("story", "Apporter la plus belle histoire qu'on connaisse",
                    "{A} raconte l'histoire du lapin qui vit sur la lune et y fait des confitures. {B} jure voir le lapin, là, à gauche. Les deux restent jusqu'à ce que la lune ait traversé tout le ciel.",
                    E(new AffinityShift(15), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("tea", "Apporter une théière, deux tasses et du miel",
                    "Le thé fume dans le froid. {B} boit sa tasse en silence, puis dit : « C'est la première fois que quelqu'un vient. » {A} ressert du thé, sans rien dire. Rien d'autre à dire.",
                    E(new AffinityShift(20), new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 2))),
                Try("poem", "Improviser un poème à la lune", new EventChallenge(PlynlingStat.Learning, 8),
                    "{A} improvise douze vers à la lune. {B} applaudit, puis en improvise douze autres, moins bons, mais avec beaucoup plus de rimes en « -ette ». C'est la meilleure soirée du mois.",
                    "{A} commence un poème et s'arrête au troisième vers, faute de rime pour « lune ». {B} propose « prune ». Le poème devient une recette. Personne ne s'en plaint.",
                    E(new GrowStat(PlynlingStat.Learning), new AffinityShift(15)), E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1), (AiAxis.Sociability, 1)), Stress(("shy", 15))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_spilled", EventType.Pulse, Grown, "La soupe renversée",
            "Au banquet du marché, {B} arrive avec un bol de soupe de potiron, trébuche sur une racine, et le renverse en entier sur le plus beau pull de {A}. Le marché se tait. Le moineau suspend sa fourchette en l'air. Tout le monde attend de voir ce que {A} va faire.",
            new[]
            {
                Try("laugh", "Éclater de rire, et en reprendre un bol", new EventChallenge(PlynlingStat.Diplomacy, 7),
                    "{A} éclate de rire, cueille un peu de soupe sur sa manche, la goûte, et la déclare excellente. Le marché rit aussi. {B}, rouge comme le potiron, apporte un deuxième bol, en marchant très, très prudemment.",
                    "{A} rit, mais un peu trop longtemps, d'un rire de plus en plus aigu. {B} ne sait plus si c'est grave. Personne ne le sait. Le moineau repose sa fourchette.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(20)), E(new AffinityShift(5)), Ai((AiAxis.Sociability, 2)), Stress(("wrathful", 20))),
                Plain("fashion", "Annoncer que c'est la nouvelle mode",
                    "« C'est voulu », annonce {A}. « Soupe sur laine. Très en vogue au-delà de la rivière. » Le lendemain, deux jeunes du village portent un pull taché de potiron. {B} n'en revient pas.",
                    E(new AffinityShift(10), new ApplyModifier("well_spoken")), Ai((AiAxis.Boldness, 1), (AiAxis.Sociability, 1))),
                Plain("sulk", "Rentrer se changer, sans un mot",
                    "{A} se lève et rentre se changer, sans un mot, avec une dignité de héron. {B} passe la soirée à fixer son bol vide. Le lendemain, un pull neuf, tricoté de travers, attend devant la porte de {A}.",
                    E(new AffinityShift(-5)), Ai((AiAxis.Vengefulness, 1), (AiAxis.Sociability, -1)), Stress(("forgiving", 15))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_empty_village", EventType.Pulse, Grown, "Le village vide",
            "Minuit. {A} a une envie terrible de tartine au miel, mais le pot est vide. Le café est fermé, la boulangerie aussi. Chez l'ours, chez le blaireau, chez la tortue : personne. Le village entier a disparu. Seule une lanterne brille au loin, du côté du moulin.",
            new[]
            {
                Try("follow", "Suivre la lanterne, sans bruit", new EventChallenge(PlynlingStat.Intrigue, 7),
                    "{A} se faufile jusqu'au moulin et colle un œil à la fenêtre : tout le village est là, en pyjama, autour d'un énorme gâteau. L'escargot vient de finir le tour de la mare, commencé au printemps. Personne ne sait qui a oublié d'inviter {A}. {A} entre, et on fait comme si de rien n'était, avec une part en plus.",
                    "{A} se faufile, marche sur une branche, et la porte du moulin s'ouvre d'un coup : « SURPRISE ! » La surprise n'est pas pour {A}, mais pour l'escargot, qui arrive juste derrière. L'escargot est très touché qu'on ait crié pour {A} aussi.",
                    E(new GrowStat(PlynlingStat.Intrigue), new LiftNeed(Need.Hunger, 0.2)), E(new LiftNeed(Need.Happiness, 0.2)), Ai((AiAxis.Boldness, 1), (AiAxis.Rationality, 1))),
                Plain("bed", "Retourner se coucher, le ventre vide",
                    "{A} retourne se coucher et rêve de tartines. Au matin, devant la porte : une part de gâteau, une bougie plantée dedans, et un mot de l'escargot. « On t'a {a:cherché|cherchée} partout. »",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Energy, -1))),
                Plain("bake", "Inventer une tartine avec ce qui reste",
                    "Pas de miel, mais un fond de confiture de mûres, trois noisettes et une idée. La tartine de minuit de {A} est si réussie que, le lendemain, la tortue en veut la recette. {A} ne s'en souvient plus du tout.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("hearty")), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("grown_hare", EventType.Pulse, Grown, "Le voyageur sans bagage",
            "Un lièvre frappe à la porte de {A} à la nuit tombée. Le manteau est usé, les poches vides, les oreilles pleines de poussière de route. « Je n'ai rien pour payer, dit le lièvre. Seulement des histoires. Des vraies. Presque toutes. »",
            new[]
            {
                Plain("host", "Ouvrir sa porte, et écouter les histoires",
                    "Le lièvre raconte les montagnes, la mer, une ville où les maisons flottent. À l'aube, le lièvre repart, et laisse sur la table une vieille carte dessinée à la main, avec une croix tout au bout.",
                    E(new GiveItem("col.carte_tresor")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Try("deal", "Proposer un marché : le couvert contre un peu de travail", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "Le lièvre répare la gouttière, le volet et la chaise bancale avant le petit-déjeuner, en racontant une histoire par clou. En partant, le lièvre glisse à {A} quelques cailloux trouvés « sur une route d'argent ».",
                    "Le lièvre accepte, répare la chaise, et casse la table. Les deux finissent la nuit à rire, assis par terre, et le lièvre explique comment on mange sans table, dans le désert.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(15)), E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1)), Stress(("generous", 15))),
                Plain("proof", "Demander une preuve, une seule",
                    "« Une preuve ? » Le lièvre sourit, retire son chapeau, et en sort un coquillage qui sent encore la mer. {A} le porte à son oreille : la mer est bien là, toute petite, au fond. Le lièvre reprend le coquillage. « Celui-là, je le garde. »",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
            }),

        new EventDef("grown_clock_feud", EventType.Pulse, Grown, "La guerre des horloges",
            "L'horloge du café retarde de deux heures depuis toujours. La tortue dit que c'est voulu. Le hérisson chef de gare dit que c'est une honte. Ce matin, la dispute a éclaté sur la place, devant tout le monde, et les deux se tournent vers {A} en même temps : « Dis-lui, toi ! »",
            new[]
            {
                Try("mediate", "Proposer un compromis : une heure de retard seulement", new EventChallenge(PlynlingStat.Diplomacy, 8),
                    "Après une longue négociation, l'horloge du café ne retarde plus que d'une heure, et la tortue offre au hérisson un chocolat chaque jour, à l'heure exacte de la gare. Les deux font semblant d'avoir gagné. C'est le signe d'un bon compromis.",
                    "Le compromis ne plaît à personne. La tortue et le hérisson, enfin d'accord sur quelque chose, se retournent ensemble contre {A}. {A} paie les chocolats.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("well_spoken")), Nothing, Ai((AiAxis.Sociability, 1), (AiAxis.Honor, 1))),
                Try("fix", "Régler l'horloge soi-même, en pleine nuit", new EventChallenge(PlynlingStat.Stewardship, 8),
                    "{A} règle l'horloge à minuit. Au matin, le café est à l'heure. La tortue arrive en avance pour la première fois de sa vie, trouve la porte fermée, et s'assoit sur le banc pour attendre. La tortue trouve ça très reposant. On ne touche plus à l'horloge.",
                    "{A} ouvre l'horloge. Un coucou en sort, très en colère, et refuse d'y retourner. Le café a désormais un coucou de comptoir, qui chante quand ça lui chante.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("both", "Donner raison aux deux, à tour de rôle",
                    "{A} donne raison à la tortue le matin et au hérisson l'après-midi. Ça marche trois jours. Le quatrième, les deux comparent leurs notes. {A} prend des vacances au fond du jardin.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Honor, -1), (AiAxis.Sociability, 1))),
            }),

        new EventDef("grown_lost_letter", EventType.Pulse, Grown, "La lettre égarée",
            "Le facteur, un pigeon distrait, a glissé dans la boîte de {A} une lettre adressée à {B}. L'enveloppe est mal fermée. Très mal fermée. Un souffle suffirait à l'ouvrir, et on distingue déjà, en travers, les mots « ne le dis à personne ».",
            new[]
            {
                Plain("deliver", "Porter la lettre à {B}, sans l'ouvrir",
                    "{A} traverse le village, la lettre tenue du bout des doigts comme une braise. {B} l'ouvre devant {A}, lit, et éclate de rire : c'est la recette secrète de la tarte du blaireau. {B} promet une part. La promesse est tenue.",
                    E(new AffinityShift(10), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("read", "Lire juste la première ligne", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} lit la première ligne, puis toutes les autres, et recolle l'enveloppe sans une trace. C'est la recette secrète de la tarte du blaireau. Le soir même, {A} fait une tarte. Le blaireau la goûte, plisse les yeux, et ne dit rien. Pour l'instant.",
                    "L'enveloppe se déchire en deux. Impossible de faire comme si de rien n'était. {A} porte les deux moitiés à {B}, avec des excuses et un pot de colle. {B} recolle la lettre, la lit, et regarde {A} d'un drôle d'air.",
                    E(new GrowStat(PlynlingStat.Intrigue), new LiftNeed(Need.Hunger, 0.2)), E(new AffinityShift(-10)), Ai((AiAxis.Honor, -2)), Stress(("honest", 30), ("just", 15))),
                Plain("pigeon", "Rattraper le pigeon, et lui faire la leçon",
                    "{A} rattrape le pigeon et lui fait un exposé de vingt minutes sur l'importance des adresses. Le pigeon écoute, hoche la tête, et repart livrer la lettre… chez l'escargot.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("grown_board_game", EventType.Pulse, Grown, "La partie de la semaine",
            "La chouette a sorti son vieux plateau de jeu, celui avec les pions en noyaux de cerise. « Une partie, dit la chouette. Un coup par jour, posé sur le rebord de ma fenêtre. » La dernière partie de la chouette a duré un hiver entier. {A} pose le premier pion.",
            new[]
            {
                Try("attack", "Attaquer tout de suite, sans réfléchir", new EventChallenge(PlynlingStat.Courage, 7),
                    "Le premier coup de {A} fait tomber trois pions d'un coup. Le lendemain, sur le rebord de la fenêtre, un mot de la chouette : « Intéressant. » La partie s'annonce longue.",
                    "Le premier coup de {A} est audacieux. Le deuxième coup de la chouette le rend ridicule. La partie s'annonce longue, et un peu humiliante.",
                    E(new GrowStat(PlynlingStat.Courage), new FollowUp("grown_board_game_end", 72, 120)), E(new FollowUp("grown_board_game_end", 72, 120)),
                    Ai((AiAxis.Boldness, 2))),
                Plain("slow", "Jouer lentement, en étudiant chaque coup",
                    "{A} réfléchit une journée entière avant chaque coup, la nuit aussi. La chouette trouve enfin un adversaire à sa mesure, et commence à arriver en retard à la bibliothèque.",
                    E(new GrowStat(PlynlingStat.Learning), new FollowUp("grown_board_game_end", 72, 120)), Ai((AiAxis.Rationality, 2))),
                Plain("notes", "Glisser un petit mot à côté de chaque coup",
                    "Chaque jour, {A} pose son pion et un petit mot à côté : la météo, un potin, une question sur les étoiles. La chouette répond à tout, en tout petit, au dos du mot. La partie avance lentement. La correspondance, très vite.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new FollowUp("grown_board_game_end", 72, 120)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("grown_board_game_end", EventType.FollowUp, AnyStage, "Le dernier coup",
            "Une semaine plus tard, deux pions seulement restent sur le plateau : un à {A}, un à la chouette. Le village entier passe devant la fenêtre pour regarder, mine de rien. Le prochain coup décide de tout, et c'est à {A} de jouer.",
            new[]
            {
                Try("win", "Jouer le coup préparé depuis trois jours", new EventChallenge(PlynlingStat.Learning, 9),
                    "{A} pose le pion. La chouette regarde le plateau longtemps, très longtemps, puis enlève ses lunettes et les essuie. « Personne ne m'avait battue depuis l'hiver de la grande neige. » La chouette offre à {A} le pion vainqueur, un noyau de cerise poli par les années.",
                    "{A} pose le pion. La chouette pose le sien juste à côté, avec un petit bruit sec. Fin de la partie. « Belle partie, dit la chouette. On recommence ? » Et {A} dit oui, malgré tout.",
                    E(new GrowStat(PlynlingStat.Learning), new ApplyModifier("inspired")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 2))),
                Plain("draw", "Proposer la nulle, et un thé",
                    "{A} propose la nulle. La chouette réfléchit, puis accepte, et sort deux tasses. Le plateau reste sur le rebord, tel quel. Les deux pions y sont encore, et personne n'ose y toucher.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Compassion, 1), (AiAxis.Sociability, 1))),
                Plain("lose", "Perdre exprès, pour faire plaisir",
                    "{A} joue un coup volontairement mauvais. La chouette le voit tout de suite, fronce les sourcils, et refuse de prendre le pion. « On ne me fait pas de cadeau. Rejoue. » {A} rejoue, et perd quand même. Tout le monde est content.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 1), (AiAxis.Honor, -1)), Stress(("honest", 10))),
            }),

        new EventDef("grown_rumor", EventType.Pulse, Grown, "La rumeur du marché",
            "Une rumeur court au marché : {A} serait riche, très riche, et cacherait un trésor sous son lit. La pie jure que ça ne vient pas du marché, ce qui veut dire que ça vient du marché. Depuis ce matin, on salue {A} beaucoup plus bas.",
            new[]
            {
                Plain("open", "Démentir, en ouvrant grand sa porte",
                    "{A} ouvre sa porte à tout le village : sous le lit, trois chaussettes et un gland. La foule repart, déçue. Seule la pie reste, et examine le gland de très près.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
                Try("play", "Laisser courir, et jouer le jeu", new EventChallenge(PlynlingStat.Intrigue, 8),
                    "{A} achète un chapeau, prend un air mystérieux, et paie son pain avec une lenteur de millionnaire. Le boulanger, impressionné, offre une brioche « pour un client de marque ». La rumeur double.",
                    "{A} joue au riche trois jours, jusqu'à ce que passe le bonnet de la collecte pour la fête du village. Tout le monde regarde. {A} donne un caillou. Fin de la rumeur.",
                    E(new GrowStat(PlynlingStat.Intrigue), new LiftNeed(Need.Hunger, 0.2)), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1)), Stress(("honest", 20))),
                Plain("piggy", "En faire une vérité : commencer une tirelire",
                    "{A} décide qu'au fond, ce n'est pas une mauvaise idée, et commence une tirelire. Le premier caillou y tombe avec un joli bruit. Le deuxième, un peu plus tard. On commence petit.",
                    E(new GrowStat(PlynlingStat.Stewardship), new ApplyModifier("trade_sense")), Ai((AiAxis.Greed, 1), (AiAxis.Rationality, 1))),
            }),
    };
}
