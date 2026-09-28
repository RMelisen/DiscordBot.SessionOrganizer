using System.Text.RegularExpressions;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// Where a visit happens. Open from FromHour (inclusive) to ToHour (exclusive), Paris time; a
// place whose FromHour is after its ToHour is open across midnight. {B} in the name is the host.
public sealed record VisitPlace(string Emoji, string Name, int FromHour, int ToHour, string[] Scenes)
{
    public bool IsOpenAt(int hour) =>
        FromHour <= ToHour ? hour >= FromHour && hour < ToHour : hour >= FromHour || hour < ToHour;
}

// One Plynling as the story shows it — captured when the visit happens, so paging back through
// the story later needs no database and shows them as they were.
public sealed record VisitCast(string Name, string Sprite, string SpeciesName, PlynlingGender Gender);

// A visit told in three beats: arrival, the activity with a little exchange, parting with the
// outcome. Id is empty until VisitStories keeps it.
public sealed record VisitStory(string Id, string Heading, IReadOnlyList<string> Beats, VisitCast Visitor, VisitCast Host, uint Accent);

// Which pools a visit draws from: the bond after it, or Conflict when the scene went badly (or
// the two are enemies — there is no friendly way to be that).
public enum VisitMood { Acquaintances, Friends, BestFriends, Lovers, Rivals, Conflict }

/// <summary>
/// Turns a visit's outcome into a little story. Pure: <see cref="PlynlingService.VisitAsync"/> has
/// already decided everything, so this only chooses the place and the words — a story that fails
/// to play can never change what happened.
/// <para>
/// Lines are templates: {A} and {B} are the visitor and the host (bold), {ils} / {Ils} the pair
/// (« elles » only when both are girls), and {a:m|f}, {b:m|f}, {p:m|f} a word agreeing with the
/// visitor, the host or the pair. An exchange is two lines, the visitor's then the host's,
/// separated by a newline.
/// </para>
/// </summary>
public static class PlynlingVisitStory
{
    public static readonly IReadOnlyList<VisitPlace> Places = new VisitPlace[]
    {
        new("🌳", "Au parc", 0, 24, new[] { "Les feuilles bruissent doucement.", "Un écureuil passe en coup de vent." }),
        new("🍂", "Sous le grand chêne", 0, 24, new[] { "Des glands tombent de temps en temps, avec un petit « toc ».", "Le vieux chêne fait de l'ombre à tout le monde." }),
        new("🏡", "Chez {B}", 0, 24, new[] { "Ça sent bon le gâteau aux noisettes.", "La maison est petite, mais très bien rangée." }),
        new("☕", "Au café du coin", 0, 24, new[] { "Ça sent le chocolat chaud.", "La serveuse, une vieille tortue, prend tout son temps." }),
        new("🪵", "Au bord de l'étang", 0, 24, new[] { "Une grenouille observe la scène depuis son nénuphar.", "L'eau fait des ronds tout doucement." }),
        new("🌼", "Dans la prairie", 6, 20, new[] { "Les fleurs sentent bon et les abeilles bourdonnent.", "L'herbe haute chatouille tout le monde." }),
        new("☀️", "Au soleil, sur les rochers", 10, 19, new[] { "Les pierres sont toutes chaudes. Parfait pour une sieste.", "Un lézard leur cède la place, de mauvaise grâce." }),
        new("🌙", "Sur le toit, sous les étoiles", 20, 6, new[] { "La lune est ronde et le toit encore tiède.", "Une chouette hulule quelque part." }),
        new("🌉", "Sur le vieux pont de pierre", 0, 24, new[] { "L'eau murmure sous les arches.", "Un héron immobile fait semblant de ne rien voir." }),
        new("🌲", "Dans la clairière", 0, 24, new[] { "Un rayon de lumière traverse les branches.", "Des papillons zigzaguent entre les troncs." }),
        new("🍃", "Sous une grande feuille, à l'abri de la pluie", 0, 24, new[] { "Les gouttes tambourinent sur la feuille, tout là-haut.", "Il fait sec, tiède, et un peu trop calme." }),
        new("🌿", "Dans le jardin de {B}", 6, 21, new[] { "Les plates-bandes sont parfaitement alignées.", "Un petit ~~Ina~~ nain de jardin en cailloux monte la garde." }),
        new("🌅", "Sur la colline, au lever du jour", 5, 9, new[] { "Le ciel passe doucement du rose à l'orange.", "La rosée brille sur l'herbe." }),
        new("🥖", "À la boulangerie", 6, 19, new[] { "Ça sent le pain chaud jusque dans la rue.", "Une abeille goûte discrètement la confiture." }),
        new("🌾", "Dans le champ de blé", 7, 20, new[] { "Les épis ondulent comme une mer dorée.", "Un épouvantail leur fait un clin d'œil. Enfin, il a l'air." }),
        new("🍎", "Au verger", 8, 20, new[] { "Ça sent la pomme mûre et l'herbe coupée.", "Un ver, très poli, s'excuse d'être là." }),
        new("🛒", "Au marché du village", 8, 19, new[] { "Des étals de baies et de noisettes à perte de vue.", "Un marchand de cailloux vante sa marchandise avec passion." }),
        new("📚", "À la bibliothèque du village", 9, 19, new[] { "Ça sent le vieux papier et la poussière dorée.", "La bibliothécaire, un hibou, fait « chut » d'un seul œil." }),
        new("🏖️", "Sur la plage de galets", 9, 21, new[] { "Les vagues font rouler les galets avec un joli bruit.", "Un crabe pince-sans-rire surveille la plage." }),
        new("🛶", "Sur une barque, au milieu de l'étang", 10, 20, new[] { "La barque tangue un peu. Personne ne sait ramer.", "Un poisson curieux vient voir de plus près." }),
        new("🎡", "À la fête foraine", 14, 23, new[] { "Une petite musique de manège flotte dans l'air.", "Des lampions colorés se balancent au vent." }),
        new("🔥", "Autour du feu de camp", 18, 1, new[] { "Les flammes crépitent et font danser les ombres.", "Ça sent la noisette grillée." }),
        new("✨", "Dans la grotte aux lucioles", 20, 1, new[] { "Les lucioles dessinent des constellations sur les parois.", "L'écho répète tout, un peu de travers." }),
        new("🛖", "Dans la cabane dans l'arbre", 0, 24, new[] { "L'échelle de corde grince à chaque pas.", "Le plancher est couvert de coussins moelleux." }),
        new("🕰️", "Dans le vieux grenier", 0, 24, new[] { "La poussière danse dans un rayon de lumière.", "Une malle entrouverte déborde de vieux trésors." }),
        new("🌈", "Sous l'arc-en-ciel", 8, 20, new[] { "Les couleurs se reflètent dans les flaques.", "Personne n'a trouvé le pot d'or, mais tout le monde a cherché." }),
        new("🏔️", "Sur la falaise, face au vent", 8, 19, new[] { "Le vent leur ébouriffe tout ce qui dépasse.", "En bas, le monde a l'air minuscule." }),
        new("⛲", "Près de la fontaine de la place", 7, 22, new[] { "L'eau clapote et des pièces brillent au fond du bassin.", "Un pigeon se prend pour le maître des lieux." }),
        new("🐝", "Devant la ruche", 8, 19, new[] { "Le bourdonnement est presque une berceuse.", "Une abeille très occupée leur fait signe de ne pas gêner." }),
        new("🎣", "Au bord de la rivière", 6, 20, new[] { "Le courant emporte une feuille comme un petit bateau.", "Un poisson passe en ricanant. Il a compris qu'il n'y avait pas d'appât." }),
        new("🍦", "Chez le glacier", 12, 22, new[] { "Des boules de glace colorées s'alignent derrière la vitre.", "Le glacier, un vieil ours, sert chaque cornet avec un soin infini." }),
        new("🌌", "Dans un champ, à regarder les étoiles", 21, 1, new[] { "Une étoile filante passe, puis une autre.", "L'herbe est fraîche et le ciel n'en finit pas." }),
        new("🚂", "À la petite gare", 6, 22, new[] { "Un petit train siffle au loin.", "Le chef de gare, un hérisson, consulte sa montre avec gravité." }),
        new("🎭", "Au petit théâtre du village", 15, 23, new[] { "Le rideau rouge est un peu mité, mais très fier.", "Dans les coulisses, quelqu'un répète la même réplique en boucle." }),
        new("🍵", "Au salon de thé", 13, 19, new[] { "Les tasses sont minuscules et les gâteaux aussi.", "Une théière ronronne doucement sur la table." }),
        new("🎨", "Dans l'atelier du peintre", 9, 18, new[] { "Ça sent la peinture et le bois. Il y a des taches partout.", "Un tableau à moitié fini attend qu'on lui trouve un titre." }),
        new("🛝", "Au terrain de jeux", 8, 20, new[] { "La balançoire grince gentiment.", "Le toboggan est bien trop grand pour tout le monde, et c'est parfait." }),
        new("🥾", "Sur le sentier de randonnée", 7, 19, new[] { "Un petit panneau indique trois directions, toutes fausses.", "Les cailloux du chemin roulent sous les pas." }),
        new("🌬️", "Dans les ruines du vieux moulin", 9, 20, new[] { "Le moulin ne tourne plus, mais le vent essaie encore.", "Les pierres racontent des histoires que personne n'écoute." }),
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Arrivals = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "{A} arrive un peu {a:intimidé|intimidée}. {B} lui fait un petit signe poli.",
            "{A} toque timidement ; {B} l'accueille d'un sourire poli.",
            "{B} attendait {A} en se demandant de quoi {ils} allaient bien pouvoir parler.",
            "{A} arrive en avance et fait semblant de regarder ailleurs, en attendant que {B} {a:le|la} remarque.",
            "« Ah, c'est toi ! » s'exclame {B}, un peu trop enthousiaste. Puis {b:il|elle} se reprend, très digne.",
            "{A} tend la patte à {B}, se ravise, et finit par un petit salut de la tête.",
            "{A} et {B} se disent bonjour en même temps, puis « pardon » en même temps aussi.",
            "{A} apporte un petit cadeau emballé dans une feuille. {B} ne sait pas trop où poser les yeux.",
            "{B} avait préparé trois sujets de conversation. Le premier est déjà épuisé quand {A} arrive.",
        },
        [VisitMood.Friends] = new[]
        {
            "{A} déboule en courant : « Me voilà ! »",
            "{B} saute de joie en voyant arriver {A}.",
            "{A} arrive les poches pleines de cailloux à montrer à {B}.",
            "{A} arrive en sifflotant, les mains derrière le dos : {B} sait qu'il y a une surprise.",
            "« Devine qui c'est ! » crie {A} de loin. {B} devine très vite.",
            "{B} n'a pas fini d'ouvrir que {A} lui saute déjà dessus pour un câlin.",
            "{A} arrive avec un grand sourire et un goûter à partager. {B} est déjà {b:ravi|ravie}.",
            "{A} et {B} se tapent dans la main, ratent, recommencent, et finissent par éclater de rire.",
            "Ça faisait trop longtemps ! {A} raconte sa semaine avant même d'être {a:arrivé|arrivée}.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "{A} et {B} se reconnaissent de loin et courent {p:l'un vers l'autre|l'une vers l'autre}.",
            "{A} n'a même pas besoin de frapper : {B} avait laissé la porte ouverte.",
            "{A} frappe avec leur signal secret : deux petits coups, puis un troisième. {B} rit déjà.",
            "{A} arrive sans prévenir, comme d'habitude. {B} avait déjà mis deux tasses.",
            "{A} et {B} n'ont même pas besoin de se dire bonjour : un regard suffit, puis un fou rire.",
            "{B} sent {A} arriver avant de {a:le|la} voir. C'est le lien qui fait ça.",
            "« C'est moi ! » crie {A}. « Je sais ! » répond {B}, sans même se retourner.",
            "{A} arrive avec le goûter préféré de {B}, sans qu'on le lui ait demandé. {B} en a les yeux qui brillent.",
            "Le rituel commence : {A} fait la poignée de main compliquée, et {B} se trompe volontairement à la fin.",
        },
        [VisitMood.Lovers] = new[]
        {
            "{A} arrive, un peu {a:rougissant|rougissante}. {B} l'attendait avec une fleur.",
            "{B} a tout rangé pour la venue de {A}. 💕",
            "{A} et {B} se retrouvent avec un grand sourire un peu gêné. 💞",
            "{A} arrive avec le cœur qui bat un peu trop fort. {B} l'entend d'ici, et sourit. 💕",
            "{B} a mis ce qu'il y a de plus joli pour recevoir {A}. Personne n'ose le dire, mais ça se voit. 💞",
            "{A} apparaît au coin du chemin, et {B} oublie complètement ce qu'{b:il|elle} était en train de faire. 💞",
            "{A} et {B} s'arrêtent à deux pas {p:l'un de l'autre|l'une de l'autre}, sans savoir qui doit avancer. 💕",
            "{A} arrive avec des fleurs cueillies en chemin. Elles sont un peu écrasées, mais {B} les trouve parfaites. 💐",
            "{B} guettait {A} depuis un moment. Quand {a:il|elle} arrive enfin, tout le monde autour fait semblant de ne rien voir. 💞",
        },
        [VisitMood.Rivals] = new[]
        {
            "{A} arrive en bombant le torse. {B} fait semblant de ne pas {a:le|la} remarquer.",
            "« Encore toi ? » lance {B} en voyant arriver {A}. Mais avec un sourire en coin.",
            "{A} arrive, bien {a:décidé|décidée} à prouver qui est {a:le meilleur|la meilleure}.",
            "{A} arrive en s'échauffant les jambes, comme pour un vrai duel. {B} lève un sourcil.",
            "« Tu es {b:prêt|prête} à perdre ? » lance {A} en approchant.",
            "{B} a tracé une ligne dans la terre. « On commence quand tu veux. » {A} sourit.",
            "{A} et {B} se saluent d'un signe de tête très, très sec. La compétition est ouverte.",
            "{A} arrive avec un carnet de scores. {B} en sort un identique. Aucun des deux n'avait prévu ça.",
            "{B} attendait {A} de pied ferme, les bras croisés. « Tu es en retard. » « Toi, tu as peur. »",
        },
        [VisitMood.Conflict] = new[]
        {
            "{A} arrive en traînant des pieds. {B} croise les bras.",
            "{B} ouvre à {A} sans un mot. L'ambiance est... tendue.",
            "{A} arrive déjà de mauvaise humeur, et {B} ne fait rien pour arranger ça.",
            "{A} arrive sans dire bonjour. {B} ne dit pas bonjour non plus. Égalité parfaite.",
            "{B} regarde arriver {A} en soupirant très fort, pour être bien sûr{b:|e} que ça se remarque.",
            "{A} fait exprès de marcher sur les fleurs de {B}. {B} a tout vu.",
            "L'air se refroidit d'un coup quand {A} apparaît. {B} serre les dents.",
            "{A} arrive en tapant du pied, {B} tourne la tête de l'autre côté. Ça commence bien.",
            "« Ah. C'est toi. » dit {B}. « Ah. C'est toi aussi. » répond {A}.",
            "{A} arrive en retard exprès. {B} l'a remarqué, et l'a noté quelque part de très vexant.",
            "{B} avait promis d'être aimable. Cette promesse a tenu jusqu'à ce que {A} ouvre la bouche.",
            "{A} entre sans un mot et s'installe à la place préférée de {B}. Provocation pure.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Activities = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "{Ils} parlent de la pluie et du beau temps. Surtout de la pluie.",
            "{B} sert une tasse de rosée à {A}. {Ils} la boivent poliment, à petites gorgées.",
            "{Ils} comparent leurs plus beaux cailloux, avec beaucoup de sérieux.",
            "{Ils} s'assoient côte à côte et regardent passer une coccinelle.",
            "{B} propose un petit biscuit à {A}. {A} accepte, puis hésite à en reprendre un second.",
            "{Ils} constatent, chacun son tour, qu'il fait beau. Le silence qui suit est un peu long.",
            "{A} montre une plume trouvée par terre. {B} dit « Oh, jolie ! » avec une sincérité inattendue.",
            "{B} lit quelques lignes d'un livre à voix haute. {A} hoche la tête à chaque phrase, sans tout suivre.",
            "{Ils} se partagent une noisette en se répétant : « Non, prends la plus grosse. »",
            "{A} sort un petit jeu de cartes. {B} connaît les règles, à peu près.",
            "{Ils} se demandent quel chemin est le plus joli. {Ils} ne sont pas d'accord, mais avec beaucoup de tact.",
            "{A} et {B} se surprennent à se regarder en même temps, puis regardent ailleurs, en même temps aussi.",
        },
        [VisitMood.Friends] = new[]
        {
            "Course jusqu'au grand arbre ! {A} gagne d'un souffle.",
            "Partie de cache-cache : {B} se cache derrière une feuille bien trop petite.",
            "Grande bataille de feuilles mortes ! Personne ne gagne, tout le monde rit.",
            "{Ils} organisent une course d'escargots. Celui de {A} s'endort en route.",
            "{A} a apporté un ballon. Il finit coincé dans un arbre en moins de trois minutes. Tout le monde rit.",
            "{Ils} se partagent un goûter et se disputent gentiment la dernière miette.",
            "Partie de dés très sérieuse. Les règles changent à chaque tour, et personne ne s'en plaint.",
            "{A} invente un jeu sur le moment. {B} en devient {b:le champion|la championne} avant même de connaître les règles.",
            "{Ils} chantent à tue-tête une chanson dont personne ne connaît les paroles.",
            "{B} apprend à {A} à faire voler une feuille. Elle retombe. On recommence. Elle retombe. Fou rire.",
            "{Ils} mènent l'enquête : qui a mangé la dernière noisette ? Le suspect principal se trouve être {A}.",
            "{A} tente une galipette pour épater {B}. La réception est… originale. {B} applaudit quand même.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "{Ils} construisent une cabane en brindilles et jurent de ne jamais révéler où.",
            "{Ils} se racontent des secrets à voix basse, en pouffant de rire.",
            "{Ils} peignent un grand tableau à quatre petits bras.",
            "{A} et {B} inventent une danse que personne d'autre ne connaîtra jamais.",
            "{Ils} se tressent mutuellement des bracelets d'herbe sans même regarder, et ça marche.",
            "{Ils} sautent dans les flaques en se tenant la main, sans se soucier de qui est le plus mouillé.",
            "{A} cuisine, {B} goûte, {A} recommence. Ça dure tout l'après-midi, et c'est parfait.",
            "{Ils} rédigent la charte officielle de leur amitié, en trois exemplaires. Personne ne sait pourquoi trois.",
            "{Ils} fredonnent la même mélodie sans s'être concertés, puis éclatent de rire en s'en rendant compte.",
            "{A} et {B} jouent à un jeu qu'{ils} ont inventé il y a longtemps, et dont personne d'autre ne comprend les règles.",
            "{A} pose la tête sur l'épaule de {B}, qui reste parfaitement immobile pour ne pas déranger.",
            "{Ils} finissent les phrases {p:l'un de l'autre|l'une de l'autre}, se trompent une fois sur deux, et ça ne change rien.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Goûter en tête-à-tête : {A} garde la plus grosse fraise pour {B}.",
            "{Ils} comptent les étoiles filantes, {p:serrés l'un contre l'autre|serrées l'une contre l'autre}.",
            "{A} a cueilli un petit bouquet de trèfles pour {B}.",
            "{B} fredonne une chanson douce ; {A} écoute, les yeux fermés.",
            "{A} récite un poème un peu maladroit. {B} le trouve magnifique. 💕",
            "{Ils} partagent un pot de miel avec une seule cuillère. C'est romantique, et très collant. 💕",
            "{A} enlève délicatement une feuille de l'épaule de {B}. Le geste dure un peu plus longtemps que nécessaire. 💞",
            "{B} offre à {A} un caillou parfaitement rond, emballé dans trois feuilles. {A} l'ouvre avec des yeux immenses. 💕",
            "{Ils} marchent lentement, épaule contre épaule, sans trop savoir où {ils} vont. 💞",
            "{A} se regarde dans une flaque, remet une mèche en place, et espère que {B} n'a rien vu. {B} a tout vu. 💕",
            "{Ils} s'assoient côte à côte pour regarder le ciel changer de couleur, sans rien dire. 💞",
            "{A} glisse un mot doux dans la poche de {B}, qui le trouvera plus tard et rougira très fort. 💕",
        },
        [VisitMood.Rivals] = new[]
        {
            "Concours de grimaces ! Le jury (une fourmi) refuse de départager.",
            "Qui saute le plus haut ? {A} jure avoir gagné, {B} exige une revanche.",
            "Course jusqu'à l'étang. Match nul, et personne ne veut l'admettre.",
            "{Ils} empilent des cailloux : la tour de {B} tient, celle de {A} s'écroule.",
            "Concours de qui mange le plus vite. {A} s'étouffe, {B} déclare la victoire trop tôt.",
            "Tir aux cailloux sur une cible en feuille. Chacun jure avoir visé le centre.",
            "{Ils} grimpent sur la même souche. {A} arrive en haut {a:le premier|la première}, {B} exige un chronomètre.",
            "Bras de fer ! Il dure très longtemps et ne mène à rien, ce qui arrange tout le monde.",
            "{B} propose un jeu. {A} le gagne. {B} change les règles. {A} le gagne encore. {B} déclare la partie nulle.",
            "{Ils} mesurent leurs ombres à midi. La discussion sur la méthode dure plus longtemps que la mesure.",
            "Concours de poses héroïques. Le jury (un escargot) met un temps infini à se décider, puis s'endort.",
            "{A} et {B} passent la ligne d'arrivée au même instant. {p:Chacun|Chacune} est persuadé{p:|e} d'avoir gagné.",
        },
        [VisitMood.Conflict] = new[]
        {
            "Dispute pour le dernier gland. {Ils} finissent par le couper en deux, en boudant.",
            "{Ils} boudent dos à dos pendant un bon moment.",
            "{A} et {B} ne sont d'accord sur rien, pas même sur la couleur du ciel.",
            "{B} soupire à chaque phrase de {A}. Ambiance.",
            "Match de regards noirs. {A} cligne des yeux en premier, mais jure que non.",
            "{B} s'assoit sur la seule chaise disponible. {A} reste debout, exprès, pour que ça se voie.",
            "Il ne reste qu'une part de gâteau. {Ils} la fixent longtemps sans que personne bouge. Elle finit par sécher.",
            "{A} commence une phrase, {B} la termine de travers. Ça dégénère tout de suite.",
            "{A} trace une ligne par terre : « Ton côté, mon côté. » {B} déborde déjà.",
            "Chacun a sa version de l'histoire, et chaque version est très, très injuste pour l'autre.",
            "Un oiseau se pose entre {A} et {B}, sent l'ambiance, et repart aussitôt.",
            "{A} propose un jeu, {B} refuse. {B} en propose un autre, {A} refuse. Il n'y aura pas de jeu.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Exchanges = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "Belle journée, non ?\nOui… très belle.",
            "Tu aimes les cailloux ?\nQui n'aime pas les cailloux ?",
            "Tu viens souvent par ici ?\nParfois. C'est calme.",
            "Je ne voudrais pas te déranger…\nMais non, pas du tout. Enfin, un peu. Non, pas du tout.",
            "C'est joli, ici.\nMerci. Je n'y suis pour rien, mais merci.",
            "Tu as l'heure ?\nNon. Mais il fait jour. Enfin, il me semble.",
            "On s'est déjà vus, non ?\nPeut-être. Ou alors j'ai un visage courant.",
            "Tu aimes les nuages ?\nJe les trouve… corrects.",
            "Tu veux qu'on se tutoie ?\nOn se tutoie déjà. Depuis tout à l'heure.",
            "Je m'appelle {A}, au cas où.\nJe sais. Moi, c'est {B}, au cas où aussi.",
            "Il va peut-être pleuvoir.\nOu peut-être pas. C'est ce qui est passionnant.",
            "Bon… on se revoit ?\nPourquoi pas. Un jour. Bientôt. Ou pas trop tard.",
        },
        [VisitMood.Friends] = new[]
        {
            "Tu crois que les nuages ont un goût ?\nSûrement fraise.",
            "On refait la course ?\nSeulement si tu me laisses de l'avance !",
            "Je t'ai apporté un caillou tout rond !\nOh, c'est le plus beau de ma collection !",
            "J'ai inventé un jeu ! Personne ne peut gagner.\nParfait, je suis déjà {b:le meilleur|la meilleure}.",
            "Tu as vu ma galipette ?\nJ'ai vu ta galipette. J'ai vu ton atterrissage aussi.",
            "Tu me gardes la dernière noisette ?\nJe te la garde. Je te promets de ne pas la goûter. Juste un peu.",
            "On fait quoi, après ?\nOn improvise. On est très {p:bons|bonnes} en improvisation.",
            "Tu sais pourquoi je t'aime bien ?\nParce que je ris à toutes tes blagues, même les mauvaises.",
            "C'était la meilleure journée du mois !\nEt on n'a même pas fini !",
            "Tu as un secret ?\nJ'en ai trois. Je t'en donne un contre une noisette.",
            "Ne me dis pas que tu as encore gagné.\nJe ne dis rien. Je souris juste très fort.",
            "Ça fait du bien de te voir.\nÇa fait du bien de te voir aussi. Bon, c'est dit, on passe à autre chose.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "Tu te souviens de la fois où on s'est {p:perdus|perdues} ?\nOn ne s'est pas {p:perdus|perdues}. On explorait.",
            "Promis, je ne le dirai à personne.\nMême pas au caillou ?",
            "{p:Meilleurs amis|Meilleures amies} pour toujours ?\nEt même un peu plus longtemps.",
            "Tu sais ce que je pense ?\nOui. Moi aussi.",
            "J'ai gardé ton caillou préféré.\nTu es la seule personne qui sait lequel c'est.",
            "Tu boudes ?\nJe réfléchis. Fort. Avec la bouche.",
            "Si un jour je disparais…\nJe te retrouve. Je sais toujours où tu te caches.",
            "Tu as encore pleuré devant une feuille qui tombe ?\nElle était très belle, cette feuille.",
            "Je te dois quelque chose.\nUn câlin. Pas de discussion.",
            "Ça reste entre nous.\nEntre nous, et cet écureuil qui écoute depuis tout à l'heure.",
            "Rappelle-moi pourquoi on est amis.\nParce que personne d'autre ne nous supporte.",
            "Tu es la seule personne qui me comprend.\nJe ne te comprends pas toujours. Mais je reste.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Tu es {b:le plus mignon|la plus mignonne} de tout le jardin.\nArrête, tu me fais rougir…",
            "On pourrait rester là pour toujours.\nD'accord. Mais après le goûter.",
            "Tu as pensé à moi, aujourd'hui ?\nToute la journée, voyons.",
            "Tu rougis.\nC'est le soleil. Enfin, c'est toi. Mais aussi le soleil.",
            "Je t'ai gardé la meilleure place.\nJe la prends. À condition que tu la partages.",
            "Tu penses à quoi ?\nÀ toi. Puis à mon goûter. Mais surtout à toi.",
            "Je suis {a:désolé|désolée}, je suis toujours en retard.\nCe n'est pas grave. Je t'aurais {a:attendu|attendue} toute la journée.",
            "Tu me manques déjà.\nMoi aussi. Et tu n'es même pas encore {a:parti|partie}.",
            "Regarde, une étoile !\nOù ça ? …Je regardais ailleurs. {b:Désolé|Désolée}.",
            "Tu as encore oublié ton écharpe.\nC'est pour que tu me la ramènes.",
            "Chut, écoute.\nJe n'entends rien. À part ton cœur.",
            "Je t'aime bien.\nJe t'aime bien aussi. Enfin, un peu plus que bien.",
        },
        [VisitMood.Rivals] = new[]
        {
            "J'ai gagné, admets-le.\nJamais de la vie.",
            "La prochaine fois, je t'écrase.\nTu dis ça à chaque fois.",
            "Tu t'es {b:entraîné|entraînée} en cachette ?\nMoi ? Jamais. …Un peu.",
            "Tu as triché.\nJe n'ai jamais triché. J'ai optimisé.",
            "Je m'entraîne depuis trois jours.\nDeux jours de trop, alors.",
            "Tu trembles ?\nC'est de froid. Ou d'impatience. Ou de froid.",
            "On parie un caillou ?\nUn caillou ? Je te le gagne en deux coups.",
            "Ce n'est pas terminé.\nOh si. Depuis un moment.",
            "Tu es un adversaire redoutable.\nTu es {a:le seul|la seule} qui m'oblige à me dépasser. Ne t'en vante pas.",
            "Je te laisse gagner cette fois.\nTu ne me laisses jamais rien. C'est ce que je préfère chez toi.",
            "Tu as peur de perdre ?\nJe n'ai pas peur. Je ne perds pas, c'est différent.",
            "Même heure, même endroit, la prochaine fois ?\nJe serai là. Avec un plan.",
        },
        [VisitMood.Conflict] = new[]
        {
            "C'était mon gland.\nTon nom n'était pas écrit dessus.",
            "Tu parles trop fort.\nEt toi, tu boudes trop fort.",
            "Je rentre chez moi.\nBonne idée.",
            "C'est toi qui as commencé.\nNon, c'est toi. Depuis le début.",
            "Tu fais toujours la tête.\nJe ne fais pas la tête. C'est mon visage.",
            "Tu m'écoutes ?\nJe fais semblant. Ça compte ?",
            "Excuse-toi.\nAprès toi. Prends ton temps.",
            "Tu as changé.\nOui. J'ai arrêté de t'écouter.",
            "On peut parler ?\nNon. Mais tu peux crier, ça, je sais faire.",
            "Tu pourrais faire un effort.\nJe l'ai fait. Tu n'as pas vu, comme d'habitude.",
            "Ça t'aurait coûté quoi de me prévenir ?\nUn peu de dignité.",
            "Je ne suis pas en colère.\nÇa se voit tellement pas.",
            "C'était pas drôle.\nJe ne riais pas de toi. Je riais près de toi.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Departures = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "{A} repart en faisant un petit signe. C'était… agréable.",
            "{Ils} se quittent poliment, en se promettant de se revoir.",
            "{A} repart en se disant que {B} est finalement quelqu'un de bien. {B} pense exactement la même chose.",
            "{Ils} se serrent la main, un peu trop longtemps, puis un peu trop vite.",
            "« Au plaisir ! » lance {A}. « Oui, au plaisir. » répond {B}. {p:Ni l'un ni l'autre|Ni l'une ni l'autre} ne sait exactement ce que ça veut dire.",
            "{A} s'éloigne en se retournant une fois, par politesse. {B} agite la main, par politesse aussi.",
            "{B} glisse un petit biscuit dans la main de {A} pour la route. C'est modeste, mais c'est un début.",
            "« Ça m'a fait plaisir. » « À moi aussi. » {Ils} se sourient, {p:soulagés|soulagées} que tout se soit bien passé.",
            "{A} et {B} se saluent poliment de loin, deux fois, parce que la première n'était pas assez claire.",
            "{A} rentre en repensant à la conversation. La prochaine fois, il faudra parler d'autre chose que de la pluie.",
        },
        [VisitMood.Friends] = new[]
        {
            "{A} repart en sautillant, {a:ravi|ravie} de sa journée.",
            "« À la prochaine ! » crie {A} en s'éloignant. {B} agite la main jusqu'au bout.",
            "{A} part en courant et en criant « À demain ! ». Rien ne garantit que {ils} se verront demain, mais c'est joli.",
            "{B} a droit à un dernier câlin avant que {A} ne parte. Il est très long. Personne ne compte.",
            "« La prochaine fois, c'est moi qui choisis le jeu ! » crie {A}. « On verra ! » répond {B}, sans aucune intention de céder.",
            "{A} s'en va en fredonnant. {B} reconnaît la chanson et la fredonne aussi, longtemps après.",
            "{Ils} se font le signe de la victoire à distance, jusqu'à ce que {A} disparaisse derrière un tournant.",
            "{A} repart avec un caillou offert par {B}, le garde dans sa poche, et le touche souvent.",
            "{B} accompagne {A} sur une bonne partie du chemin, « juste un peu plus loin ». Ça finit par durer un moment.",
            "Au moment de partir, {A} se retourne pour lancer une dernière blague. {B} rit encore, longtemps après.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "{Ils} se disent au revoir trois fois, puis une quatrième, pour être {p:sûrs|sûres}.",
            "{A} repart avec un bracelet d'herbes tressé par {B}. {Ils} ont le même.",
            "{Ils} se quittent sans un mot. Pas besoin : un simple regard, et tout est dit.",
            "{A} repart avec le reste du goûter, emballé par {B} dans une grande feuille. Comme d'habitude.",
            "{B} dit « À tout de suite ». {A} répond « À tout de suite ». {Ils} savent bien que ce sera plutôt demain.",
            "{A} fait trois pas, revient, serre {B} dans ses bras, repart. Puis recommence, pour vérifier.",
            "{Ils} échangent leur signal secret à distance : deux petits coups, puis un troisième. Ça marche même de très loin.",
            "{A} s'en va avec un caillou de plus dans la poche, et un poids de moins sur le cœur. {B} aussi.",
            "Le temps a filé, comme toujours quand {ils} sont ensemble. {A} repart en promettant de revenir très vite.",
            "{B} regarde {A} s'éloigner et pense déjà à la prochaine fois. {A} aussi, d'ailleurs, à l'autre bout du chemin.",
        },
        [VisitMood.Lovers] = new[]
        {
            "{A} repart sur un petit nuage. {B} reste un long moment sur le pas de la porte.",
            "{Ils} se séparent à regret, en se retournant tous les trois pas.",
            "{A} s'en va, et {B} reste un moment à regarder le chemin, comme si {A} pouvait revenir. 💞",
            "« À bientôt. » murmure {A}. « Très bientôt. » répond {B}, sans lâcher sa main. 💕",
            "{A} repart avec la fleur que {B} lui a offerte, en la serrant contre son cœur. 💞",
            "{B} dit qu'{b:il|elle} n'est pas triste. {A} n'est pas dupe, et revient pour un dernier câlin. 💕",
            "{A} et {B} se disent « à demain » en sachant que ce n'est peut-être pas vrai, mais que ça sonne bien. 💞",
            "{A} rentre le cœur léger, un peu en retard, et sourit sans raison tout le long du chemin. 💕",
            "{B} regarde {A} partir, puis se rend compte qu'{b:il|elle} sourit toujours, {b:tout seul|toute seule}. 💞",
            "{Ils} se promettent de se revoir très vite, et de ne pas faire semblant de ne pas s'être manqué. 💕",
        },
        [VisitMood.Rivals] = new[]
        {
            "{A} repart en jurant de revenir plus {a:fort|forte}. {B} a déjà hâte.",
            "« Match retour la prochaine fois ! » lance {B}. {A} ricane en s'éloignant.",
            "{A} repart en promettant une revanche. {B} promet de l'attendre, avec un plan d'avance.",
            "« Je reviens, et cette fois, je gagne. » « Tu dis toujours ça. » {A} s'en va en riant.",
            "{Ils} se serrent la main d'un air très sérieux. {p:Chacun|Chacune} est {p:persuadé|persuadée} d'avoir gagné.",
            "{B} tient la porte à {A}, avec un sourire poli qui dit « je ne te dois rien ». {A} sort avec le même.",
            "{A} s'éloigne en tapant dans ses mains pour se chauffer. Le prochain match sera plus rude.",
            "Avant de partir, {A} lance un dernier défi. {B} l'accepte, même si personne n'a bien compris de quoi il s'agissait.",
            "{A} et {B} se saluent d'un signe de tête très net. Le respect est là, la rivalité aussi, et c'est parfait.",
            "{B} regarde {A} s'éloigner en marmonnant : « Pas mal. » Puis, plus bas : « Pas mal du tout. »",
        },
        [VisitMood.Conflict] = new[]
        {
            "{A} repart en claquant la porte. {B} marmonne quelque chose d'inaudible.",
            "{A} rentre en boudant. Ce n'était pas une bonne journée.",
            "{Ils} se quittent sans un regard. Ça ira mieux la prochaine fois… peut-être.",
            "{A} s'en va sans se retourner. {B} fait semblant de ne pas regarder, et regarde.",
            "« Je ne reviendrai plus ! » lance {A}. « C'est ça. À demain. » répond {B}.",
            "{Ils} se disent au revoir en même temps, sur le même ton glacial. Coordination parfaite.",
            "{B} referme la porte derrière {A} avec un peu trop de conviction.",
            "Juste avant de partir, {A} hésite… puis change d'avis. {B} soupire, {b:soulagé|soulagée} et {b:déçu|déçue} à la fois.",
            "{A} et {B} partent {p:chacun|chacune} de leur côté, et prennent pourtant le même chemin. Silence total pendant dix minutes.",
            "Personne ne dit au revoir. Personne ne dit rien. Quelque part, une petite mouche rit.",
        },
    };

    public static VisitMood MoodFor(bool goodScene, PlynlingBond after) => !goodScene ? VisitMood.Conflict : after switch
    {
        PlynlingBond.Friends => VisitMood.Friends,
        PlynlingBond.BestFriends => VisitMood.BestFriends,
        PlynlingBond.Lovers => VisitMood.Lovers,
        PlynlingBond.Rivals => VisitMood.Rivals,
        PlynlingBond.Enemies => VisitMood.Conflict,
        _ => VisitMood.Acquaintances,
    };

    /// <summary>A place open at <paramref name="now"/> (Paris time). Always at least the always-open ones.</summary>
    public static VisitPlace PlaceFor(DateTimeOffset now, Random rng)
    {
        var hour = AppTime.ToZoned(now).Hour;
        var open = Places.Where(p => p.IsOpenAt(hour)).ToList();
        return open[rng.Next(open.Count)];
    }

    /// <summary>
    /// The story of <paramref name="outcome"/>. <paramref name="outcomeLines"/> — what the visit
    /// changed (confession, bond, badges, finds, happiness) — closes the third beat.
    /// <paramref name="pick"/> chooses a line from a pool (the handler passes ResponsePicker, so a
    /// channel does not see the same line twice in a row); by default it draws from <paramref name="rng"/>.
    /// </summary>
    public static VisitStory Build(VisitOutcome outcome, string outcomeLines, DateTimeOffset now, Random rng,
        Func<string[], string>? pick = null)
    {
        pick ??= pool => pool[rng.Next(pool.Length)];
        var visitor = Cast(outcome.Visitor, now);
        var host = Cast(outcome.Host, now);
        string X(string template) => Expand(template, visitor.Name, visitor.Gender, host.Name, host.Gender);

        var place = PlaceFor(now, rng);
        var mood = MoodFor(outcome.GoodScene, outcome.After);
        var exchange = X(pick(Exchanges[mood])).Split('\n');

        var beats = new[]
        {
            $"*{pick(place.Scenes)}*\n{X(pick(Arrivals[mood]))}",
            $"{X(pick(Activities[mood]))}\n💬 **{visitor.Name}** : {exchange[0]}\n💬 **{host.Name}** : {exchange[1]}",
            string.IsNullOrWhiteSpace(outcomeLines) ? X(pick(Departures[mood])) : $"{X(pick(Departures[mood]))}\n{outcomeLines}",
        };
        var heading = $"{place.Emoji} {place.Name.Replace("{B}", host.Name)}";
        return new VisitStory("", heading, beats, visitor, host, PlynlingCatalog.Info(outcome.Host.Species).Accent);
    }

    private static VisitCast Cast(Plynling p, DateTimeOffset now) => new(
        PlynlingCardUi.SafeName(p.Name),
        PlynlingArt.Sprite(p.Species, PlynlingLife.Stage(p, now), PlynlingLife.Mood(p, now)),
        PlynlingCatalog.Info(p.Species).Name,
        p.Gender);

    private static readonly Regex Choice = new(@"\{([abp]):([^|}]*)\|([^}]*)\}", RegexOptions.Compiled);

    /// <summary>Fills a template's names, pronouns and agreements.</summary>
    public static string Expand(string template, string a, PlynlingGender ga, string b, PlynlingGender gb)
    {
        var girls = ga == PlynlingGender.Female && gb == PlynlingGender.Female;
        var text = Choice.Replace(template, m =>
        {
            var female = m.Groups[1].Value switch { "a" => ga == PlynlingGender.Female, "b" => gb == PlynlingGender.Female, _ => girls };
            return female ? m.Groups[3].Value : m.Groups[2].Value;
        });
        return text
            .Replace("{A}", $"**{a}**").Replace("{B}", $"**{b}**")
            .Replace("{ils}", girls ? "elles" : "ils").Replace("{Ils}", girls ? "Elles" : "Ils");
    }
}
