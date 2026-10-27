using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// Wave 8 short events: ado.
public static partial class PlynlingEvents
{
    private static EventDef[] ShortTeen() => new[]
    {
        // ---- ado
        new EventDef("teen_catapult", EventType.Pulse, Teen, "La catapulte",
            "Avec une cuillère, un élastique et une planche, {A} a construit une catapulte. La catapulte lance des glands très loin. Personne ne sait encore exactement où.",
            new[]
            {
                Try("aim", "Viser le seau, au bout du jardin", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "Le gland atterrit pile dans le seau. Encore : pile dans le seau. Le blaireau, impressionné, commande une catapulte pour son magasin de jouets.",
                    "Le gland part de travers, traverse la haie, et rebondit sur le chapeau du hérisson. Le hérisson regarde le gland, puis la haie, puis note quelque chose dans son carnet.",
                    E(new GrowStat(PlynlingStat.Stewardship), new GiveCailloux(5)), Nothing, Ai((AiAxis.Rationality, 1), (AiAxis.Energy, 1))),
                Plain("far", "Lancer le plus loin possible",
                    "Le gland s'envole au-dessus du toit, de la gare, de la rivière. On ne le retrouvera jamais. Un chêne poussera peut-être un jour, très loin, grâce à {A}.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
                Plain("dismantle", "Démonter la catapulte avant l'accident",
                    "{A} démonte la catapulte et rend la cuillère à la cuisine. Personne ne saura jamais ce qui a failli arriver. C'est souvent le mieux.",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
            }),

        new EventDef("teen_experiment", EventType.Pulse, Teen, "L'expérience",
            "Dans la cuisine, {A} mélange du vinaigre, du bicarbonate et un peu de jus de mûre, pour voir. Le bol se met à mousser. Beaucoup. De plus en plus.",
            new[]
            {
                Plain("notes", "Observer et prendre des notes",
                    "La mousse violette monte, déborde, et coule jusqu'au plancher. {A} note tout : le temps, la couleur, la hauteur. La chouette, à qui {A} montre le carnet, en demande une copie.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
                Plain("lid", "Poser vite un couvercle",
                    "{A} pose un couvercle. Le couvercle s'envole jusqu'au plafond, et y reste collé. Le couvercle y est toujours. On s'y est habitué.",
                    E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Boldness, 1))),
                Plain("show", "Appeler tout le monde pour regarder",
                    "{A} appelle les voisins. Le temps que les voisins arrivent, la mousse a envahi la cuisine. On applaudit quand même, en pataugeant.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_flute", EventType.Pulse, Teen, "La flûte du grenier",
            "{A} a trouvé une vieille flûte au grenier. Personne ne sait en jouer. Ça n'a jamais arrêté personne.",
            new[]
            {
                Try("practice", "S'entraîner tous les soirs", new EventChallenge(PlynlingStat.Learning, 6),
                    "Au bout d'une semaine, {A} joue un air entier, presque juste. Le moineau s'arrête de chanter pour écouter. C'est un compliment.",
                    "Au bout d'une semaine, {A} joue un air entier. Les chats du quartier aussi, en chœur. Les voisins proposent des leçons, avec insistance.",
                    E(new GrowStat(PlynlingStat.Learning)), Nothing, Ai((AiAxis.Rationality, 1))),
                Plain("window", "Jouer à la fenêtre, pour tout le village",
                    "{A} joue à la fenêtre, très fort, très faux, avec un enthousiasme magnifique. Le village ferme ses volets, un par un. Seul l'escargot reste, et applaudit à la fin, une heure plus tard.",
                    E(new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
            }),

        new EventDef("teen_flower_thief", EventType.Pulse, Teen, "Le voleur de fleurs",
            "Chaque matin, une fleur de plus disparaît du jardin de {A}. Proprement coupée, sans un brin par terre.",
            new[]
            {
                Try("watch", "Guetter à l'aube, derrière la fenêtre", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "À l'aube, un jeune lapin se glisse dans le jardin, coupe une fleur, et file… chez la vieille taupe aveugle, à qui le lapin apporte une fleur chaque matin, pour l'odeur. {A} plante une rangée de plus, exprès.",
                    "{A} guette, et s'endort à l'aube. Au réveil, une fleur manque, et à sa place, un petit mot : « Merci. C'est pour la taupe. »",
                    E(new GrowStat(PlynlingStat.Intrigue), new ApplyModifier("cherished")), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Rationality, 1))),
                Plain("sign", "Planter une pancarte : « Défense de cueillir »",
                    "{A} plante une pancarte. Le lendemain, une fleur manque, et la pancarte a été complétée : « Défense de cueillir (sauf pour la taupe) ».",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Honor, 1))),
            }),

        new EventDef("teen_arm_wrestling", EventType.Pulse, Teen, "Le bras de fer",
            "Au café, {B} pose le coude sur la table et tend la patte à {A}. « Bras de fer. Le perdant paie les chocolats. » Le café entier se retourne.",
            new[]
            {
                Try("wrestle", "Accepter, et serrer la patte", new EventChallenge(PlynlingStat.Courage, 6, VsTarget: true),
                    "{A} gagne, après une minute de grimaces des deux côtés. {B} paie les chocolats en riant, et réclame la revanche pour demain.",
                    "{B} gagne, d'un coup sec. {A} paie les chocolats. {B} boit la moitié du sien, et pousse le reste vers {A}.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(10)), E(new AffinityShift(10)), Ai((AiAxis.Boldness, 2))),
                Plain("thumbs", "Proposer plutôt un bras de fer… de pouces",
                    "Le bras de fer de pouces dure vingt minutes et finit en fou rire. Personne ne paie. Le café réclame une finale.",
                    E(new AffinityShift(15)), Ai((AiAxis.Sociability, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_pillow_fight", EventType.Pulse, Teen, "Les polochons",
            "{A} dort chez {B}, pour la première fois. Minuit. Les lumières sont éteintes depuis longtemps. Dans le noir, un oreiller vole, et atterrit sur {A}.",
            new[]
            {
                Plain("fight", "Riposter, de toutes ses forces",
                    "{A} riposte. La bataille dure une heure, et se termine dans un nuage de plumes. On s'endort au milieu, à bout de souffle.",
                    E(new AffinityShift(15), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Energy, 2))),
                Plain("still", "Faire semblant de dormir",
                    "{A} ne bouge pas. {B} attend, perplexe, puis s'approche pour vérifier. {A} attrape {B} par surprise, et on rit jusqu'à l'aube.",
                    E(new GrowStat(PlynlingStat.Intrigue), new AffinityShift(10)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_broken_mirror", EventType.Pulse, Teen, "Le miroir cassé",
            "{A} a cassé le miroir de l'entrée. La pie arrive aussitôt, l'air grave : d'après le règlement du village, qu'elle est la seule à avoir lu, qui casse un miroir doit tous les morceaux brillants à la pie. « Article sept. »",
            new[]
            {
                Plain("laugh", "Rire, et demander à voir l'article sept",
                    "{A} rit et demande à voir le règlement. La pie fouille son nid très longtemps, et revient avec une liste de courses. Le lendemain, la liste a un article sept, ajouté au crayon, en tout petit.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Rationality, 2))),
                Plain("ritual", "Obéir à la pie",
                    "{A} ramasse chaque morceau, les compte devant témoin, et les remet à la pie dans une boîte à chaussures. La pie signe un reçu, très sérieusement. Depuis, son nid brille comme une vitrine, et elle salue {A} d'un hochement de tête.",
                    E(new ApplyModifier("light_heart")), Ai((AiAxis.Zeal, 1)), Stress(("cynical", 15))),
                Plain("mosaic", "Faire une mosaïque avec les morceaux",
                    "{A} colle les morceaux sur une planche. Le résultat reflète le monde en cent petits bouts. Le café l'accroche au mur, assez haut pour que la pie ne l'atteigne pas.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_slang", EventType.Pulse, Teen, "Tout mirabelle",
            "Par accident, {A} a dit « c'est tout mirabelle » pour dire que c'était bien. Le lendemain, la moitié des jeunes du village le disent aussi.",
            new[]
            {
                Plain("claim", "Revendiquer l'invention",
                    "{A} rappelle à qui veut l'entendre qui a dit « tout mirabelle » en premier. Personne ne s'en souvient. Mais tout le monde le dit, et ça, c'est un peu à {A}.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Boldness, 1))),
                Plain("another", "Inventer une nouvelle expression",
                    "{A} tente « ça fait pomme ». Ça ne prend pas. Puis « c'est très escargot », pour dire lent. Ça prend tout de suite, au grand déplaisir de l'escargot.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1), (AiAxis.Rationality, 1))),
            }),

        new EventDef("teen_cabbage_hats", EventType.Pulse, Teen, "La mode du chou",
            "Au village, une nouvelle mode est arrivée : les chapeaux en feuilles de chou. Tous les jeunes en portent un. {A} n'en a pas.",
            new[]
            {
                Plain("make", "Fabriquer le plus beau chapeau de chou",
                    "{A} fabrique un chapeau de chou à trois étages, avec une plume. Le lendemain, la mode a changé : c'est {A} qui l'a changée.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Boldness, 1))),
                Plain("refuse", "Refuser la mode, par principe",
                    "{A} sort tête nue, très dignement. Une semaine plus tard, plus personne ne porte de chou, et {A} est la seule personne du village à n'avoir jamais eu l'air ridicule.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Rationality, 1), (AiAxis.Boldness, 1))),
                Plain("eat", "En acheter un, et le manger au déjeuner",
                    "{A} achète un chapeau de chou et le mange au déjeuner. C'est la mode la plus nourrissante de l'année.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Greed, 1))),
            }),

        new EventDef("teen_fence", EventType.Pulse, Teen, "La palissade de la gare",
            "Quelqu'un a dessiné un énorme escargot à moustache sur la palissade de la gare. Le hérisson, furieux, dévisage tous les jeunes du village, un par un. Son regard s'arrête sur {A}, qui a de la craie sur les doigts. Pour une tout autre raison.",
            new[]
            {
                Try("explain", "Expliquer la craie, calmement", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "{A} montre le cahier de géométrie, plein de craie. Le hérisson vérifie, s'excuse, et demande à {A} de l'aider à trouver le vrai coupable. C'était l'escargot, qui voulait son portrait.",
                    "{A} explique, mais le hérisson ne croit rien. {A} passe l'après-midi à frotter la palissade. Le soir, le vrai coupable passe : l'escargot, qui voulait son portrait.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Nothing, Ai((AiAxis.Honor, 1), (AiAxis.Sociability, 1))),
                Plain("improve", "Améliorer le dessin, tant qu'à faire",
                    "{A} ajoute un chapeau, un parapluie et un coucher de soleil. Le hérisson, d'abord furieux, finit par trouver ça « pas mal ». Le dessin reste.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
            }),

        new EventDef("teen_seed", EventType.Pulse, Teen, "La graine mystère",
            "La chouette a donné une graine à chaque jeune du village. Personne ne sait ce qui poussera. {A} a mis la sienne dans un pot, sur le rebord de la fenêtre.",
            new[]
            {
                Plain("care", "S'en occuper chaque jour, avec un carnet",
                    "Arrosage, soleil, notes. Au bout de dix jours : un petit pied de tomates cerises. Les premières sont pour la chouette.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Plain("talk", "Lui parler tous les soirs",
                    "{A} raconte sa journée au pot chaque soir. La plante pousse, pousse, et devient une courge rampante qui envahit la cuisine. Les histoires aident peut-être un peu trop.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Sociability, 1), (AiAxis.Compassion, 1))),
                Plain("forget", "L'oublier un peu",
                    "{A} oublie le pot. Trois semaines plus tard, une fleur rouge magnifique a poussé, toute seule. Certaines choses poussent mieux quand on ne les regarde pas.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("teen_shopping", EventType.Pulse, Teen, "La liste des courses",
            "Pour la première fois, on a confié à {A} la liste des courses du mois, et la bourse qui va avec. La liste fait deux pages. La bourse, beaucoup moins.",
            new[]
            {
                Try("budget", "Tout acheter, et rapporter la monnaie", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} négocie les pommes, compare les farines, et rentre avec tout, plus trois cailloux de monnaie, rendus fièrement.",
                    "{A} achète tout… sauf le sel, oublié. Et deux pots de miel en trop, achetés par erreur. Personne ne s'en plaint vraiment.",
                    E(new GrowStat(PlynlingStat.Stewardship)), E(new LiftNeed(Need.Hunger, 0.1)), Ai((AiAxis.Rationality, 1))),
                Plain("treat", "Tout acheter, plus un petit plaisir",
                    "{A} achète tout, et un chou à la crème, mangé sur le chemin du retour. La liste ne mentionnait pas de chou à la crème. La liste ne l'interdisait pas non plus.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Greed, 1)), Stress(("temperate", 10))),
            }),

        new EventDef("teen_song_bridge", EventType.Pulse, Teen, "La chanson du pont",
            "{A} chante en traversant le vieux pont. Fort. Très fort. Au milieu du pont, le héron ouvre un œil.",
            new[]
            {
                Plain("louder", "Chanter encore plus fort",
                    "{A} chante plus fort. Le héron ouvre l'autre œil, puis s'envole sur la rive d'en face. Depuis, chaque fois que {A} approche, le héron change de rive, par précaution.",
                    E(new GrowStat(PlynlingStat.Courage)), Ai((AiAxis.Boldness, 2))),
                Plain("duet", "Inviter le héron à chanter aussi",
                    "Le héron réfléchit, puis pousse un cri long et grave, parfaitement juste. Personne n'avait jamais entendu chanter le héron. {A} non plus.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new LiftNeed(Need.Happiness, 0.15)), Ai((AiAxis.Sociability, 2))),
            }),

        new EventDef("teen_boredom", EventType.Pulse, Teen, "L'ennui",
            "Rien à faire. Tout le monde est occupé, le ciel est gris, et {A} a déjà relu tous ses livres deux fois. L'après-midi s'étire comme un chat.",
            new[]
            {
                Plain("tidy", "Ranger enfin sa chambre",
                    "{A} range la chambre, par désespoir. Sous le lit : un livre perdu, deux glands et une lettre jamais envoyée. L'après-midi finit en lecture.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 1))),
                Plain("game", "Inventer un jeu",
                    "{A} invente un jeu avec une balle, un mur et des règles de plus en plus compliquées. Le soir, trois jeunes du village jouent avec {A}. Les règles tiennent sur quatre pages.",
                    E(new GrowStat(PlynlingStat.Learning)), Ai((AiAxis.Sociability, 1), (AiAxis.Energy, 1))),
                Plain("nap", "Faire une sieste de quatre heures",
                    "{A} fait une sieste de quatre heures. Au réveil, c'est déjà le soir, et ce n'est plus l'ennui : c'est le dîner.",
                    E(new ApplyModifier("well_rested")), Ai((AiAxis.Energy, -2))),
            }),

        new EventDef("teen_fish_tale", EventType.Pulse, Teen, "Grand comme ça",
            "Au café, {A} raconte avoir pêché un brochet grand comme ça. Les bras écartés au maximum. Tout le monde se tourne vers l'ours, qui a pêché le vrai brochet du village, l'an dernier.",
            new[]
            {
                Try("bluff", "Maintenir l'histoire, et en rajouter", new EventChallenge(PlynlingStat.Intrigue, 6),
                    "{A} en rajoute : le brochet parlait, et réclamait un avocat. Le café rit si fort que l'ours oublie de vérifier. L'histoire fait le tour du village avant le soir.",
                    "« Combien de kilos ? » demande l'ours. {A} dit un chiffre. L'ours dit le sien. Le brochet de {A} rétrécit à vue d'œil.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Nothing, Ai((AiAxis.Honor, -1), (AiAxis.Boldness, 1)), Stress(("honest", 15))),
                Plain("truth", "Avouer : c'était une sardine",
                    "« C'était une sardine. Une grosse. » Le café éclate de rire, et l'ours offre à {A} une leçon de pêche pour dimanche.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new ApplyModifier("clear_conscience")), Ai((AiAxis.Honor, 2))),
            }),

        new EventDef("teen_burnt_cake", EventType.Pulse, Teen, "Le gâteau brûlé",
            "{A} a voulu faire un gâteau pour la première fois. Une odeur de brûlé envahit la maison, et par la porte du four s'échappe une petite fumée noire.",
            new[]
            {
                Plain("save", "Sauver ce qui peut l'être",
                    "{A} gratte le dessus brûlé, et dessous, le gâteau est… presque bon. Avec beaucoup de confiture, très bon même.",
                    E(new LiftNeed(Need.Hunger, 0.15)), Ai((AiAxis.Rationality, 1))),
                Plain("again", "Tout recommencer, en lisant la recette, cette fois",
                    "Deuxième essai, recette sous les yeux. Le gâteau sort doré, parfait. {A} découvre que les recettes disent des choses utiles.",
                    E(new GrowStat(PlynlingStat.Stewardship)), Ai((AiAxis.Rationality, 2))),
                Plain("smoked", "Le servir comme un « gâteau fumé », exprès",
                    "{A} sert le gâteau fumé aux voisins, en expliquant que c'est une spécialité de la ville d'en bas. Les voisins sont polis. Très polis.",
                    E(new GrowStat(PlynlingStat.Intrigue)), Ai((AiAxis.Boldness, 1), (AiAxis.Honor, -1))),
            }),

        new EventDef("teen_lost_bet", EventType.Pulse, Teen, "Le pari perdu",
            "{A} a parié avec {B} que le moineau arriverait en retard au concours. Le moineau est arrivé en avance, pour la première fois de sa vie. Le gage : porter une tenue choisie par {B}, une journée entière.",
            new[]
            {
                Plain("honor", "Honorer le gage, la tête haute",
                    "{B} choisit un chapeau à fleurs, une cape et des chaussettes dépareillées. {A} porte tout, la tête haute, toute la journée. Le village applaudit au passage.",
                    E(new GrowStat(PlynlingStat.Courage), new AffinityShift(15)), Ai((AiAxis.Honor, 2))),
                Try("haggle", "Négocier un gage plus raisonnable", new EventChallenge(PlynlingStat.Diplomacy, 6),
                    "Après négociation, le gage devient : un nœud papillon, pendant une heure. {B} admire la manœuvre, à contrecœur.",
                    "{A} négocie si mal que le gage passe à deux jours, avec un chapeau en plus. {B} en rit encore en rentrant.",
                    E(new GrowStat(PlynlingStat.Diplomacy), new AffinityShift(5)), E(new AffinityShift(10)), Ai((AiAxis.Rationality, 1))),
            },
            Target: TargetKind.Known),

        new EventDef("teen_first_catch", EventType.Pulse, Teen, "La première prise",
            "L'ours a prêté sa vieille canne à {A}. Trois heures au bord de l'eau, sans une touche. Puis le bouchon plonge, d'un coup.",
            new[]
            {
                Try("pull", "Tirer de toutes ses forces", new EventChallenge(PlynlingStat.Courage, 6),
                    "{A} tire, tire, et sort de l'eau une truite dorée qui se débat dans le soleil. L'ours en a les larmes aux yeux : la canne n'avait rien pris depuis des années.",
                    "{A} tire de toutes ses forces, et sort de l'eau… une vieille botte. Dans la botte, une grenouille, très contrariée.",
                    E(new GrowStat(PlynlingStat.Courage), new LiftNeed(Need.Hunger, 0.2)), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Energy, 1))),
                Plain("release", "Remonter doucement, et relâcher le poisson",
                    "{A} remonte doucement un petit gardon, le regarde une seconde, et le remet à l'eau. L'ours hoche la tête : « Celui-là reviendra plus gros. »",
                    E(new ApplyModifier("clear_conscience")), Ai((AiAxis.Compassion, 2))),
            }),

        new EventDef("teen_rowboat", EventType.Pulse, Teen, "La barque du blaireau",
            "Le blaireau prête sa vieille barque à {A} pour l'après-midi. L'étang est calme, le ciel bleu, et les rames sont presque de la même taille.",
            new[]
            {
                Try("island", "Ramer jusqu'à l'île du milieu", new EventChallenge(PlynlingStat.Stewardship, 6),
                    "{A} rame jusqu'à l'île, en zigzag mais sans erreur, et pique-nique sous le saule. Au retour, le blaireau demande si la barque a pris l'eau. Non. Tant mieux.",
                    "{A} rame, mais toujours plus fort à gauche. La barque tourne en rond tout l'après-midi, sous l'œil très intéressé des canards.",
                    E(new GrowStat(PlynlingStat.Stewardship)), E(new LiftNeed(Need.Happiness, 0.1)), Ai((AiAxis.Energy, 1))),
                Plain("drift", "Se laisser dériver, au fond de la barque",
                    "{A} s'allonge au fond de la barque et regarde les nuages. La barque dérive jusqu'aux roseaux, où {A} s'endort. Le soir, le blaireau vient chercher sa barque, et {A} avec.",
                    E(new ApplyModifier("soothed")), Ai((AiAxis.Energy, -1))),
            }),

        new EventDef("teen_bullies", EventType.Pulse, Teen, "Le goûter du mulot",
            "Derrière l'école, deux grands renardeaux ont pris le goûter d'un petit mulot, et le tiennent très haut, hors de portée. Le mulot ne pleure pas encore. Presque.",
            new[]
            {
                Try("stand", "S'interposer", new EventChallenge(PlynlingStat.Courage, 7),
                    "{A} s'interpose, très droit. Les renardeaux hésitent, se regardent, et rendent le goûter en marmonnant que c'était pour rire. Le mulot partage le goûter avec {A}.",
                    "{A} s'interpose, et un renardeau pose le goûter sur la tête de {A}, très haut. Le mulot et {A} sautillent ensemble. Puis le hérisson passe, ne rit pas du tout, et les renardeaux filent.",
                    E(new GrowStat(PlynlingStat.Courage), new ApplyModifier("clear_conscience")), Nothing, Ai((AiAxis.Boldness, 1), (AiAxis.Honor, 1)), Stress(("craven", 20))),
                Plain("fetch", "Aller chercher le hérisson",
                    "{A} court chercher le hérisson, qui arrive un sifflet à la bouche. Les renardeaux rendent le goûter en un temps record. Le mulot suit {A} toute la semaine, comme une ombre reconnaissante.",
                    E(new ApplyModifier("cherished")), Ai((AiAxis.Rationality, 1), (AiAxis.Honor, 1))),
                Plain("share", "Partager son propre goûter avec le mulot",
                    "{A} partage sa tartine avec le mulot. Les renardeaux, sans public, s'ennuient et rendent l'autre goûter. Le mulot repart avec deux goûters.",
                    E(new GrowStat(PlynlingStat.Diplomacy)), Ai((AiAxis.Compassion, 2))),
            }),
    };
}
