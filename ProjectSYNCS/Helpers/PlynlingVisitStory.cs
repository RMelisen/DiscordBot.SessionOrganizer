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
// the story later needs no database and shows them as they were. The picture is built per step
// from the species, the stage and that step's face.
public sealed record VisitCast(string Name, PlynlingSpecies Species, PlynlingStage Stage, string SpeciesName,
    PlynlingGender Gender, IReadOnlyList<Passion> Passions)
{
    public string Sprite(PlynlingMood face) => PlynlingArt.Sprite(Species, Stage, face);
}

// One step of a story: its text and each Plynling's face on it.
public sealed record VisitBeat(string Text, PlynlingMood VisitorFace, PlynlingMood HostFace);

// A face tag read off the start of a line: [sad] (Both), or [A:sad B:happy] for the visitor and
// the host separately. See PlynlingVisitStory.Untag.
public sealed record FaceTag(PlynlingMood? Both, PlynlingMood? A, PlynlingMood? B);

// A visit told in three beats: arrival, the activity with a little exchange, parting with the
// outcome. Id is empty until VisitStories keeps it.
public sealed record VisitStory(string Id, string Heading, IReadOnlyList<VisitBeat> Beats, VisitCast Visitor, VisitCast Host, uint Accent);

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
/// <para>
/// Five beats: arrival, the subject (a speaker — visitor or host — raises one of their passions),
/// the listener's reaction, the activity with an exchange, then parting. Passion lines add {S} / {L},
/// the speaker and the listener, with {s:m|f} / {l:m|f}, and {P}, the subject passion — inserted
/// last, so text a person typed is never read as a template.
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
            "{A} arrive en répétant son bonjour à voix basse. Le vrai bonjour sort un peu de travers.",
            "{B} fait un petit signe de la main. {A} en fait un aussi, puis se demande si c'était bien nécessaire.",
            "{A} arrive pile à l'heure, {a:essoufflé|essoufflée} d'avoir couru pour ne pas être en avance.",
            "{B} a tout rangé avant l'arrivée de {A}, puis a remis un peu de désordre pour faire naturel.",
            "{A} s'arrête à bonne distance et fait un salut très poli. {B} répond par un salut encore plus poli.",
            "{A} et {B} échangent un sourire, puis un deuxième, pour être {p:sûrs|sûres} que le premier était bien arrivé.",
            "{A} tend une petite fleur à {B}, la reprend, puis la redonne. C'est compliqué, les premières fois.",
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
            "{A} arrive en faisant la roue. Enfin, presque. {B} applaudit quand même.",
            "{B} guettait {A} depuis un moment et fait semblant de ne pas avoir attendu.",
            "{A} crie le prénom de {B} bien avant d'être à portée de voix.",
            "{A} débarque avec trois idées de jeux et zéro plan. {B} adore le programme.",
            "{B} lance un « Enfin ! » en voyant {A}, qui n'a pourtant que deux minutes de retard.",
            "{A} arrive avec un sac de noisettes et un grand sourire. {B} ne sait pas lequel des deux lui fait le plus plaisir.",
            "{A} et {B} se retrouvent et parlent en même temps pendant une bonne minute. Personne n'écoute. Tout le monde est content.",
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
            "{A} arrive et s'installe à sa place habituelle, sans même demander. C'est sa place, après tout.",
            "{B} a déjà préparé le goûter préféré de {A}. Comme toujours. Comme depuis toujours.",
            "{A} et {B} font leur salut secret, qui dure maintenant presque une minute entière.",
            "{A} arrive avec une histoire à raconter. {B} en a deux. {Ils} décident de tout se dire en même temps.",
            "{B} reconnaît le pas de {A} de loin et ouvre les bras avant même de {a:le|la} voir.",
            "{A} arrive un peu {a:fatigué|fatiguée}. {B} s'en aperçoit tout de suite et pose un coussin à côté de {b:lui|elle}.",
            "{A} et {B} n'ont pas besoin de se dire bonjour. {Ils} reprennent la conversation exactement là où {ils} l'avaient laissée.",
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
            "{A} arrive avec le cœur qui fait des bonds. {B} a le même, mais le cache moins bien. 💕",
            "{B} s'est {b:recoiffé|recoiffée} trois fois avant l'arrivée de {A}. {A} le remarque tout de suite. 💞",
            "{A} arrive un peu trop vite, puis ralentit pour avoir l'air {a:détendu|détendue}. Personne n'est dupe. 💕",
            "{A} et {B} se sourient de loin pendant tout le temps qu'il faut pour se rejoindre. C'est long, et c'est parfait. 💞",
            "{B} tend la main à {A} sans rien dire. {A} la prend sans rien dire non plus. 💕",
            "{A} apporte un petit caillou en forme de cœur. {B} le range aussitôt dans sa poche, tout contre {b:lui|elle}. 💞",
            "{A} rougit en arrivant. {B} rougit en {a:le|la} voyant rougir. Tout le monde rougit. 💕",
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
            "[angry] {B} attendait {A} de pied ferme, les bras croisés. « Tu es en retard. » « Toi, tu as peur. »",
            "{A} arrive très lentement, pour bien montrer qu'{a:il|elle} n'est pas {a:pressé|pressée}.",
            "{B} fait mine de s'étirer quand {A} arrive, comme si le match allait commencer. Il va peut-être commencer.",
            "{A} arrive avec un carnet où sont notées toutes les défaites de {B}. {B} en a un aussi.",
            "« Tiens, te voilà. » « Tiens, tu es encore là. » L'échange de politesses est terminé.",
            "[angry] {A} et {B} se fixent en silence. Le premier qui cligne des yeux a perdu. {Ils} clignent en même temps.",
            "{B} a tracé une ligne de départ avant même que {A} arrive. Juste au cas où.",
            "{A} arrive en sifflotant l'air de la victoire. {B} connaît la suite et la siffle plus fort.",
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
            "{A} arrive avec une liste de reproches. {B} a préparé la sienne. Elle est plus longue.",
            "{B} voit arriver {A} et soupire assez fort pour que tout le coin l'entende.",
            "{A} arrive en retard et fait exprès de ne pas s'excuser. {B} fait exprès de le remarquer.",
            "{A} et {B} se disent bonjour du bout des lèvres. Même le bonjour a l'air vexé.",
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
            "[A:happy B:angry] Qui saute le plus haut ? {A} jure avoir gagné, {B} exige une revanche.",
            "Course jusqu'à l'étang. Match nul, et personne ne veut l'admettre.",
            "{Ils} empilent des cailloux : la tour de {B} tient, celle de {A} s'écroule.",
            "Concours de qui mange le plus vite. {A} s'étouffe, {B} déclare la victoire trop tôt.",
            "Tir aux cailloux sur une cible en feuille. Chacun jure avoir visé le centre.",
            "[A:happy B:angry] {Ils} grimpent sur la même souche. {A} arrive en haut {a:le premier|la première}, {B} exige un chronomètre.",
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
            "Tu boudes ?\n[angry] Je réfléchis. Fort. Avec la bouche.",
            "Si un jour je disparais…\nJe te retrouve. Je sais toujours où tu te caches.",
            "Tu as encore pleuré devant une feuille qui tombe ?\n[sad] Elle était très belle, cette feuille.",
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
            "[sad] Je suis {a:désolé|désolée}, je suis toujours en retard.\nCe n'est pas grave. Je t'aurais {a:attendu|attendue} toute la journée.",
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
            "{A} repart en se disant que la prochaine fois, {a:il|elle} osera raconter une blague.",
            "{Ils} se saluent une dernière fois, puis se retournent au même moment pour saluer encore. Un peu gênant. Un peu joli.",
            "{B} regarde {A} s'éloigner en se disant que c'était un bon après-midi. Un vrai bon après-midi.",
            "{A} remercie {B} trois fois, puis une quatrième, pour être {a:sûr|sûre}.",
            "{Ils} se quittent en promettant de se revoir « un de ces jours ». {Ils} ont l'air de le penser vraiment.",
            "{A} s'en va avec un petit sourire discret. {B} garde le même, un moment.",
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
            "{A} repart en courant, revient chercher ce qu'{a:il|elle} avait oublié, puis repart en courant.",
            "{Ils} se font une dernière grimace pour la route. La meilleure de la journée.",
            "« Demain, même heure ? » lance {A}. « Même heure ! » répond {B}, qui n'a aucune idée de l'heure.",
            "{B} glisse une noisette dans la poche de {A} au moment de dire au revoir. {A} ne la trouvera que ce soir.",
            "{A} s'éloigne en chantant la chanson qu'{ils} ont inventée ensemble. {B} la chante encore après.",
            "{Ils} se séparent au croisement, en se criant des au revoir jusqu'à ne plus s'entendre.",
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
            "{A} repart, mais {B} l'accompagne jusqu'au bout du chemin. Puis {A} raccompagne {B}. Puis c'est le soir.",
            "{Ils} font leur salut secret une dernière fois. Il est encore plus long qu'à l'arrivée.",
            "{A} laisse un petit mot caché pour {B}. {B} en a caché un aussi. {Ils} les trouveront plus tard.",
            "{B} regarde partir {A} en souriant. {Ils} se reverront bientôt. {Ils} se revoient toujours bientôt.",
            "{A} et {B} se serrent fort, très fort, puis encore un peu. Ça suffira jusqu'à la prochaine fois.",
            "{A} part en emportant la moitié d'un secret. {B} garde l'autre moitié, bien au chaud.",
        },
        [VisitMood.Lovers] = new[]
        {
            "{A} repart sur un petit nuage. {B} reste un long moment sur le pas de la porte.",
            "[sad] {Ils} se séparent à regret, en se retournant tous les trois pas.",
            "{A} s'en va, et {B} reste un moment à regarder le chemin, comme si {A} pouvait revenir. 💞",
            "« À bientôt. » murmure {A}. « Très bientôt. » répond {B}, sans lâcher sa main. 💕",
            "{A} repart avec la fleur que {B} lui a offerte, en la serrant contre son cœur. 💞",
            "[B:sad] {B} dit qu'{b:il|elle} n'est pas triste. {A} n'est pas dupe, et revient pour un dernier câlin. 💕",
            "{A} et {B} se disent « à demain » en sachant que ce n'est peut-être pas vrai, mais que ça sonne bien. 💞",
            "{A} rentre le cœur léger, un peu en retard, et sourit sans raison tout le long du chemin. 💕",
            "{B} regarde {A} partir, puis se rend compte qu'{b:il|elle} sourit toujours, {b:tout seul|toute seule}. 💞",
            "{Ils} se promettent de se revoir très vite, et de ne pas faire semblant de ne pas s'être manqué. 💕",
            "{A} s'éloigne à reculons pour regarder {B} le plus longtemps possible, et trébuche un peu. 💕",
            "[B:sad] {B} garde la main levée bien après que {A} a disparu. 💞",
            "[sad] {Ils} se disent au revoir trois fois, et chaque fois, c'est plus difficile. 💕",
            "{A} repart avec un sourire qui ne veut pas s'en aller. {B} a exactement le même. 💞",
            "[B:sad] {B} murmure « déjà ? » quand {A} se lève pour partir. {A} se rassoit un petit moment. Puis encore un. 💕",
            "{A} s'en va en chantonnant la chanson préférée de {B}. {B} l'entend jusqu'au bout. 💞",
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
            "{A} s'en va en jurant que la prochaine fois sera la bonne. {B} répond que c'est ce qu'{a:il|elle} dit toujours.",
            "{Ils} se serrent la main un peu trop fort, et font semblant que ça ne fait pas mal.",
            "{B} note le score du jour dans son carnet. {A} le note différemment dans le sien.",
            "« Profite de ta victoire, elle ne durera pas ! » lance {A} en partant. {B} en profite, largement.",
            "{A} et {B} se tournent le dos au même moment, avec beaucoup de dignité. Puis se jettent un dernier coup d'œil.",
            "{B} regarde {A} partir et sourit en coin. Sans rivalité, la vie serait bien plus ennuyeuse.",
        },
        [VisitMood.Conflict] = new[]
        {
            "{A} repart en claquant la porte. {B} marmonne quelque chose d'inaudible.",
            "[sad] {A} rentre en boudant. Ce n'était pas une bonne journée.",
            "[sad] {Ils} se quittent sans un regard. Ça ira mieux la prochaine fois… peut-être.",
            "{A} s'en va sans se retourner. {B} fait semblant de ne pas regarder, et regarde.",
            "« Je ne reviendrai plus ! » lance {A}. « C'est ça. À demain. » répond {B}.",
            "{Ils} se disent au revoir en même temps, sur le même ton glacial. Coordination parfaite.",
            "{B} referme la porte derrière {A} avec un peu trop de conviction.",
            "[sad] Juste avant de partir, {A} hésite… puis change d'avis. {B} soupire, {b:soulagé|soulagée} et {b:déçu|déçue} à la fois.",
            "{A} et {B} partent {p:chacun|chacune} de leur côté, et prennent pourtant le même chemin. Silence total pendant dix minutes.",
            "Personne ne dit au revoir. Personne ne dit rien. Quelque part, une petite mouche rit.",
            "{A} part sans dire au revoir. {B} répond quand même, très fort, à personne.",
            "{Ils} se quittent sur un « on verra » qui ne promet rien de bon.",
            "{A} s'éloigne en marmonnant. {B} marmonne aussi. {Ils} marmonnent la même chose, sans le savoir.",
            "{B} tourne les talons, revient pour avoir le dernier mot, puis tourne à nouveau les talons.",
            "[sad] {A} part en boudant, s'arrête, hésite à revenir s'excuser… et repart en boudant.",
            "[sad] {Ils} se séparent {p:fâchés|fâchées}. Mais {ils} y pensent {p:tous|toutes} les deux toute la soirée.",
        },
    };

    // Beat 3: the listener reacts to the subject, then the speaker answers — one \n between them.
    // Keyed by mood and by whether the listener shares the passion. Generic, so {P} stands alone
    // (after a colon, « pour », « sur », or as a subject — never after « de » or « à »).
    // Step 3: B reacts to A's opener — one line. Keyed by mood and by whether B shares the passion.
    // {S}/{L} are A and B throughout the conversation. Generic, so {P} stands alone (after a colon,
    // « pour », « sur », or as a subject — never after « de » or « à »).
    public static readonly IReadOnlyDictionary<(VisitMood Mood, bool Shared), string[]> Reactions =
        new Dictionary<(VisitMood, bool), string[]>
        {
            [(VisitMood.Acquaintances, false)] = new[] { "[content] Oh. {P}, alors. C'est… intéressant.", "Oh ! Et… ça consiste en quoi, exactement ?", "[content] Je ne m'y connais pas du tout.", "Et tu fais ça depuis longtemps ?", "[content] C'est… original.", "[content] Je ne savais même pas que ça existait.", "Ah. Et… tu en fais souvent ?", "[content] C'est très… précis, comme passion.", "[content] Je vais faire semblant de comprendre, d'accord ?", "Tu as l'air d'y tenir beaucoup." },
            [(VisitMood.Acquaintances, true)] = new[] { "Attends, toi aussi ? {P} ?", "Non ! Toi aussi ?", "Je n'en parle jamais, d'habitude. Personne ne comprend.", "Sujet préféré : {P}. On a au moins ça en commun.", "Ça alors. Je pensais être {l:le seul|la seule}.", "Attends… c'est vrai ? On a ça en commun ?", "Oh ! Je n'osais en parler à personne.", "Ça alors. Le monde est petit.", "Toi aussi, {P} ?", "Je crois qu'on vient de trouver notre sujet de conversation." },
            [(VisitMood.Friends, false)] = new[] { "Je n'y connais rien. Explique-moi tout !", "Attends, tu fais vraiment ça ?", "Je ne comprends rien, mais tu as l'air tellement {s:content|contente}.", "Montre-moi !", "Tu m'avais caché ça !", "Tu peux m'apprendre ?", "Je n'y comprends rien, mais je te fais confiance.", "Tu en parles avec tellement de passion…", "D'accord, mais tu m'expliques en version courte.", "Je vais essayer, pour voir." },
            [(VisitMood.Friends, true)] = new[] { "{P} ! C'est pour ça qu'on s'entend si bien !", "Non, sérieusement ? Toi aussi ?", "On devrait en faire un club !", "Je savais qu'on avait quelque chose en commun.", "Sujet du jour : {P}. Parfait.", "On est vraiment {p:faits|faites} pour s'entendre.", "Tu te rends compte qu'on aime ça {p:tous|toutes} les deux ?", "On pourrait monter une équipe !", "Je savais que tu avais bon goût.", "Sujet favori : {P}. Évidemment." },
            [(VisitMood.BestFriends, false)] = new[] { "Tu m'en parles tous les jours, tu sais.", "Encore ce sujet ?", "Je ne partage pas ta passion. Mais je partage tout le reste.", "Vas-y, raconte. Je te connais, tu ne tiens plus.", "Je ne comprends toujours pas ce qui te plaît là-dedans.", "Tu sais que je t'écouterai toujours, même sur ce sujet ?", "D'accord. Explique-moi pour la centième fois.", "Je ne partage pas ta passion, mais je partage ton goûter.", "Tu as encore trouvé quelque chose de nouveau là-dessus ?", "Si ça te rend {s:heureux|heureuse}, ça me rend {l:heureux|heureuse} aussi." },
            [(VisitMood.BestFriends, true)] = new[] { "On en parle encore ? Toujours {P} ?", "Tu te souviens de la première fois qu'on en a parlé ?", "Notre sujet. Rien qu'à nous.", "Tu as déjà vu quelqu'un d'aussi {s:passionné|passionnée} que toi ?", "On va encore en parler pendant des heures, hein ?", "Tu te rappelles notre serment ?", "On devrait écrire un livre là-dessus, à deux.", "Personne ne comprend ce sujet comme toi.", "Encore une fois ?", "Quand on sera {p:vieux|vieilles}, on en parlera encore." },
            [(VisitMood.Lovers, false)] = new[] { "Je pourrais t'écouter en parler pendant des heures.", "Tu es si {s:mignon|mignonne} quand tu en parles.", "Explique-moi encore. J'aime t'entendre.", "Je vais m'y mettre. Pour toi.", "Tu as les yeux qui brillent.", "Tu es {s:beau|belle} quand tu en parles.", "Apprends-moi. Je veux tout partager avec toi.", "Je n'y connais rien, mais j'adore ta voix quand tu en parles.", "Tu m'emmèneras, la prochaine fois ?", "Tu rougis quand tu en parles." },
            [(VisitMood.Lovers, true)] = new[] { "Tu sais ce que j'aime encore plus que ça ?", "Tu sais qu'on a la même passion ?", "On pourra en parler toute notre vie ?", "Tu es la seule personne avec qui j'aime en parler.", "On en fait quelque chose, ensemble ?", "C'est un signe, tu ne crois pas ?", "On pourra le faire ensemble, toute la vie ?", "Tu aimes ça aussi… Tu es {s:parfait|parfaite}.", "Je t'aimais déjà. Maintenant, c'est pire.", "Notre passion commune : {P}. Et l'autre : nous deux." },
            [(VisitMood.Rivals, false)] = new[] { "[angry] Pff. {P}, ce n'est même pas difficile.", "Franchement, c'est facile. Même moi, je pourrais.", "Ça, une passion ? J'ai vu mieux.", "Je parie que je deviendrais {l:meilleur|meilleure} que toi en une semaine.", "Tu y passes trop de temps.", "Je parie que tu n'es même pas si {s:doué|douée} que ça.", "[angry] Moi, je trouve ça surfait.", "Tout le monde peut faire ça.", "C'est ça, ta grande passion ? Pff.", "Je te laisse ce sujet. J'ai mieux à faire." },
            [(VisitMood.Rivals, true)] = new[] { "J'en sais bien plus que toi sur le sujet.", "Tu fais ça aussi ? Évidemment. Tu copies tout ce que je fais.", "Je suis {l:le meilleur|la meilleure} du coin dans ce domaine.", "On verra qui en sait le plus.", "Match retour sur le sujet : {P}. Quand tu veux.", "Tu ne m'arriveras jamais à la cheville sur ce sujet.", "Je m'y suis {l:mis|mise} avant toi.", "Défi officiel, sujet : {P}. Tu relèves ?", "Tu as encore copié mon idée.", "Il ne peut y avoir qu'un seul champion sur ce sujet." },
            [(VisitMood.Conflict, false)] = new[] { "Passionnant. Vraiment.", "Et ça t'intéresse vraiment, ça ?", "Tu ne parles que de ça.", "Personne n'a demandé.", "C'est d'un ennui…", "Tu vas encore en parler longtemps ?", "Franchement, c'est nul.", "Je m'ennuie déjà.", "Pourquoi tu me racontes ça, à moi ?", "Change de sujet." },
            [(VisitMood.Conflict, true)] = new[] { "Tu ne vas pas me l'apprendre, j'en fais depuis bien avant toi.", "Tu t'y prends mal, comme toujours.", "Même passion, et pourtant on ne s'entend pas.", "Tu n'y connais rien.", "Ne me parle pas de ça, pas toi.", "Tu fais tout de travers.", "On aime la même chose, et c'est la seule chose qu'on a en commun.", "Tu n'as rien compris au sujet.", "Ne me dis pas comment faire.", "Tu gâches tout, même ça." },
        };

    // Steps 4–5, when A talks: a reply that keeps the exchange going (A's other option is a Detail).
    // Each stands alone — the line before it may be B's reaction, B's follow-up, or A's own Detail.
    public static readonly IReadOnlyDictionary<(VisitMood Mood, bool Shared), string[]> AnswerFollowups =
        new Dictionary<(VisitMood, bool), string[]>
        {
            [(VisitMood.Acquaintances, false)] = new[]
            {
                "Tu dis ça poliment, mais je vais quand même continuer à t'en parler.",
                "C'est une longue histoire. Tu as l'après-midi ?",
                "Ce n'est pas grave si tu n'y connais rien. Je vais tout t'expliquer. Tout.",
                "Je m'y suis {s:mis|mise} un mardi. Depuis, je ne pense qu'à ça.",
                "Merci de m'écouter. Ce n'est pas tout le monde qui le fait.",
                "Maintenant que tu sais, tu ne pourras plus jamais l'oublier.",
                "J'y passe tous mes jours. Deux fois le dimanche.",
                "Je suis {s:content|contente} que ça t'intéresse. Enfin, que tu fasses semblant.",
                "[sad] Je vois bien que tu fais semblant de comprendre. C'est gentil quand même.",
                "Tu veux que je t'en parle encore une petite heure ?",
            },
            [(VisitMood.Acquaintances, true)] = new[]
            {
                "Je croyais être {s:le seul|la seule} ! On devrait se voir plus souvent.",
                "On ne se connaît pas encore très bien, mais je sens que ça va changer.",
                "Je n'en parle jamais, d'habitude. Avec toi, c'est facile.",
                "On a ça en commun. C'est déjà beaucoup, non ?",
                "On est deux, maintenant. C'est un club.",
                "Il faut qu'on se voie plus souvent. Pour en parler. Enfin, pas seulement.",
                "On pourrait en parler plus souvent. Si tu veux. Sans obligation.",
                "Le monde vient de devenir un peu plus sympathique.",
                "Toi aussi ! Enfin, moi aussi ! Enfin, nous deux !",
                "Je crois qu'on n'est pas près d'épuiser le sujet.",
            },
            [(VisitMood.Friends, false)] = new[]
            {
                "Tout t'expliquer ? Installe-toi, ça va prendre la journée.",
                "Un jour, je vais t'y mettre, tu vas voir.",
                "Tu n'as pas besoin de comprendre. Tu as juste besoin de m'écouter.",
                "On pourrait essayer tout de suite. Enfin, après le goûter.",
                "Je ne cache rien. Tu n'avais jamais demandé.",
                "Première leçon : l'enthousiasme. Tu en as déjà, c'est bien parti.",
                "C'est gentil de t'intéresser à ça. Vraiment.",
                "À d'autres, j'en parlerais juste beaucoup. À toi, j'en parle énormément.",
                "Il n'y a pas de version courte. Assieds-toi.",
                "Si tu aimes, on en fera ensemble. Si tu n'aimes pas… on en fera quand même.",
            },
            [(VisitMood.Friends, true)] = new[]
            {
                "Je le savais depuis le début, qu'on avait ça en commun.",
                "[angry] Je te l'avais dit il y a trois semaines. Tu ne m'écoutes jamais.",
                "Un club de deux. Le meilleur des clubs.",
                "Les goûters, et maintenant ça. On a vraiment tout en commun.",
                "On en reparle demain aussi, si tu veux.",
                "Je te le dis tout le temps : on est {p:faits|faites} pour s'entendre.",
                "C'est génial. Vraiment génial.",
                "Une équipe de deux. Imbattable.",
                "On a bon goût, {p:tous|toutes} les deux. C'est un fait.",
                "Évidemment. Personne n'est surpris.",
            },
            [(VisitMood.BestFriends, false)] = new[]
            {
                "Et tu m'écoutes tous les jours. C'est pour ça que je t'aime bien.",
                "Tu vas encore m'écouter jusqu'au bout. Comme toujours.",
                "Tu n'as pas besoin d'aimer ça. Tu as juste besoin de m'aimer, moi.",
                "Tu me connais trop bien. C'est effrayant.",
                "Je ne sais pas non plus pourquoi j'aime autant ça. C'est ça qui est bien.",
                "C'est ça, l'amitié : écouter l'autre parler de choses qu'on ne comprend pas.",
                "La cent-unième fois, tu vas enfin comprendre, j'en suis {s:sûr|sûre}.",
                "Tu m'écoutes, je te fais un goûter. C'est un excellent compromis.",
                "Tu es toujours la première personne à qui je raconte tout ça.",
                "C'est gentil de faire semblant de t'y intéresser. Je le vois, tu sais.",
            },
            [(VisitMood.BestFriends, true)] = new[]
            {
                "Et toi aussi, tu adores ça, avoue.",
                "On en a parlé jusqu'à la nuit, la dernière fois. On recommence ?",
                "Rien qu'à nous. Et à personne d'autre. C'est ça le plus beau.",
                "Tu es la seule personne qui comprenne vraiment.",
                "On pourrait en parler des heures. Des jours. Des années.",
                "On ne se souvient même plus comment ça a commencé. Et on continue.",
                "On écrit notre histoire là-dessus depuis le début, sans le savoir.",
                "On est les deux seules personnes au monde à comprendre ça.",
                "Encore une fois ? Encore une fois. Comme d'habitude.",
                "Quand on sera {p:vieux|vieilles}, on en parlera encore. Plus fort.",
            },
            [(VisitMood.Lovers, false)] = new[]
            {
                "Tu dis que tu écoutes, mais tu regardes surtout mes yeux. 💕",
                "Arrête de me regarder comme ça, je vais oublier ce que je disais.",
                "Tu n'écoutes pas vraiment, hein ?… Tant pis, je continue. 💕",
                "Tu n'es pas {l:obligé|obligée} d'aimer ça… Mais je suis très {s:touché|touchée} que tu essaies. 💞",
                "Je ne sais plus si c'est le sujet qui me plaît, ou toi. Un peu les deux. 💕",
                "Tu es {l:beau|belle} quand tu m'écoutes, tu sais. 💕",
                "Le plus important, c'est nous deux. Ça, c'est juste en deuxième. 💞",
                "Alors je vais en parler pendant très longtemps, rien que pour toi. 💕",
                "Un jour, je t'emmènerai. Partout. Même là où c'est ennuyeux. 💞",
                "Si je rougis, ce n'est pas à cause du sujet. C'est parce que tu me regardes. 💕",
            },
            [(VisitMood.Lovers, true)] = new[]
            {
                "On aime vraiment la même chose… Oh. Oh ! 💕",
                "Je crois que c'est pour ça que je t'aime. Enfin, pas seulement. 💞",
                "On en fera toute notre vie. Et un peu après. 💕",
                "Tu es la seule personne avec qui j'aime me taire aussi. 💞",
                "Tout ce que tu veux. Avec toi, c'est toujours mieux. 💕",
                "Tout ce qui te concerne est un signe, je crois. 💞",
                "Toute la vie, et même le dimanche. 💕",
                "On est {p:parfaits|parfaites} ensemble, tu ne trouves pas ? 💞",
                "Je t'aimais déjà. Maintenant, c'est pire. 💕",
                "Notre passion commune, c'est bien. Mais ma préférée, c'est nous deux. 💞",
            },
            [(VisitMood.Rivals, false)] = new[]
            {
                "Si c'est si facile, montre-moi.",
                "Essaie, pour voir. Je te regarde.",
                "Tu n'y connais rien. Tu es juste {l:jaloux|jalouse}.",
                "Pari tenu : rendez-vous dans une semaine.",
                "Tu passes beaucoup de temps à me regarder faire, pour quelqu'un qui s'en fiche.",
                "Viens vérifier. Je t'attends.",
                "Tu dis ça parce que tu n'y arrives pas.",
                "Si c'est si simple, pourquoi tu ne le fais pas ?",
                "Ta seule passion, à toi, c'est de me contredire.",
                "Regarde bien. Et apprends.",
            },
            [(VisitMood.Rivals, true)] = new[]
            {
                "On parie ?",
                "C'est toi qui copies. J'ai commencé avant.",
                "Tu te crois {l:le meilleur|la meilleure} du coin ? Plus pour longtemps.",
                "On sait déjà qui en sait le plus. Moi.",
                "Maintenant. Tout de suite. Prépare-toi.",
                "Attention, je suis juste derrière toi. Tout près.",
                "J'y suis {s:meilleur|meilleure} que toi, et tu le sais.",
                "Ton défi, je l'ai relevé avant même que tu le lances.",
                "C'était mon idée. Tu l'as eue après moi, voilà tout.",
                "Tu peux déjà aller chercher le trophée pour me le donner.",
            },
            [(VisitMood.Conflict, false)] = new[]
            {
                "Tu pourrais au moins faire semblant de t'intéresser.",
                "Ça m'intéresse plus que cette conversation, en tout cas.",
                "Et toi, tu ne parles que pour te plaindre.",
                "Personne ne t'a {l:obligé|obligée} à venir.",
                "Si ça t'ennuie, pourquoi tu écoutes ?",
                "J'en parlerai jusqu'à ce que tu partes. Ou plus.",
                "Franchement, tu es de mauvaise foi.",
                "Tu t'ennuies ? C'est ton état naturel, non ?",
                "Je t'en parle parce que tu es là. Pas par choix.",
                "C'est le seul sujet qui ne te concerne pas. C'est reposant.",
            },
            [(VisitMood.Conflict, true)] = new[]
            {
                "C'est ça. Continue de te vanter.",
                "C'est drôle, j'allais te dire exactement la même chose.",
                "Si on ne s'entend pas, c'est ta faute. Comme d'habitude.",
                "J'y connais plus que toi, et tu le sais.",
                "Très bien. J'en parlerai à quelqu'un de plus agréable.",
                "Tu donnes des leçons que personne ne demande.",
                "On a au moins ça en commun. Et c'est déjà trop.",
                "J'ai compris que tu adores avoir raison.",
                "Arrête de le faire mal, et on en reparlera.",
                "Tu gâches tout. C'est presque un talent.",
            },
        };

    // Steps 4–5, when B talks: a question or a comment on what A is saying. B may talk twice in a
    // row, so each stands alone too.
    public static readonly IReadOnlyDictionary<(VisitMood Mood, bool Shared), string[]> Followups =
        new Dictionary<(VisitMood, bool), string[]>
        {
            [(VisitMood.Acquaintances, false)] = new[]
            {
                "Et… ça consiste en quoi, exactement ?",
                "[content] Je ne suis pas {l:sûr|sûre} de tout suivre, mais continue.",
                "D'accord. Et c'est difficile ?",
                "Tu as appris ça {s:tout seul|toute seule} ?",
                "[content] C'est… intéressant. Vraiment. Enfin, je crois.",
            },
            [(VisitMood.Acquaintances, true)] = new[]
            {
                "Toi aussi, tu as commencé comment ?",
                "Je fais exactement pareil ! C'est fou.",
                "On devrait comparer nos techniques, un jour.",
                "Tu en connais beaucoup, des gens comme nous ?",
                "Je n'en parle jamais à personne, d'habitude.",
            },
            [(VisitMood.Friends, false)] = new[]
            {
                "Attends, attends. Refais-moi le passage important.",
                "Et moi, je pourrais essayer ?",
                "Tu m'expliques encore ? J'ai décroché au milieu.",
                "Tu as l'air tellement {s:heureux|heureuse} quand tu en parles.",
                "D'accord, mais qu'est-ce qui est si génial, exactement ?",
            },
            [(VisitMood.Friends, true)] = new[]
            {
                "Et tu connais celle-là ? Non ? Attends, je te raconte !",
                "On est vraiment {p:pareils|pareilles}, toi et moi.",
                "Tu te souviens de la première fois qu'on en a parlé ?",
                "Je savais que tu comprendrais !",
                "On devrait en faire un club. Avec des badges.",
            },
            [(VisitMood.BestFriends, false)] = new[]
            {
                "Encore ? Vas-y, je t'écoute. Comme toujours.",
                "Je ne comprends toujours rien, mais j'adore te voir en parler.",
                "Raconte-moi la suite, je sais qu'il y en a une.",
                "Tu me l'as déjà racontée hier. Raconte-la encore.",
                "Tu devrais écrire un livre là-dessus. Je le lirais. Enfin, je regarderais les images.",
            },
            [(VisitMood.BestFriends, true)] = new[]
            {
                "Tu te souviens quand on en a parlé jusqu'à la nuit ?",
                "On est {p:les seuls|les seules} à comprendre ça, hein ?",
                "Je te préviens : moi aussi, je vais tout te raconter.",
                "C'est notre truc. Rien qu'à nous.",
                "On pourrait ne parler que de ça pendant une semaine.",
            },
            [(VisitMood.Lovers, false)] = new[]
            {
                "Continue. J'aime t'entendre parler. 💕",
                "Je ne comprends pas tout, mais tu es adorable quand tu en parles.",
                "Tu m'apprendras ? Rien que pour être avec toi. 💞",
                "Tu as les yeux qui brillent. C'est beau.",
                "Je pourrais t'écouter des heures. Enfin, je t'écoute des heures. 💕",
            },
            [(VisitMood.Lovers, true)] = new[]
            {
                "On aime même les mêmes choses… C'est un signe. 💞",
                "Tu sais que c'est un peu pour ça que je suis {l:tombé amoureux|tombée amoureuse} ?",
                "On fera tout ça ensemble, hein ? Promis ? 💕",
                "Ton passage préféré, c'est aussi le mien.",
                "Même là-dessus, on est d'accord. C'est presque trop. 💞",
            },
            [(VisitMood.Rivals, false)] = new[]
            {
                "[angry] Pff. N'importe qui pourrait faire ça.",
                "[angry] Et c'est censé m'impressionner ?",
                "J'y arriverais en deux jours, moi.",
                "[angry] C'est tout ce que tu as ?",
                "Tu en parles beaucoup, pour quelqu'un qui n'est pas si {s:doué|douée}.",
            },
            [(VisitMood.Rivals, true)] = new[]
            {
                "Moi, je fais ça depuis plus longtemps que toi.",
                "Ma technique est meilleure, tu le sais.",
                "On verra qui en sait le plus.",
                "[angry] Tu copies tout ce que je fais, c'est fou.",
                "Match retour quand tu veux.",
            },
            [(VisitMood.Conflict, false)] = new[]
            {
                "Tu vas encore en parler longtemps ?",
                "Ça n'intéresse personne, tu sais.",
                "Change de sujet. Vraiment.",
                "Et alors ?",
                "Je fais semblant d'écouter, là. Ça se voit ?",
            },
            [(VisitMood.Conflict, true)] = new[]
            {
                "Tu t'y prends mal, comme toujours.",
                "Tu n'as rien compris au sujet.",
                "Ne me dis pas comment faire.",
                "Tu gâches même ça.",
                "C'est moi qui t'ai fait découvrir ça, je te rappelle.",
            },
        };

    // Step 6: the talk winds down and turns into doing something. {S} is whoever says it here.
    public static readonly IReadOnlyDictionary<VisitMood, string[]> Closers = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "Bon… et si on faisait quelque chose, maintenant ?",
            "On pourrait essayer, pour voir ?",
            "Ça te dirait de faire quelque chose ensemble ? Enfin, si tu veux.",
            "Je crois qu'on a assez parlé. On bouge ?",
            "J'ai une petite idée. Tu me suis ?",
            "Bon. Je ne sais plus quoi dire, alors faisons quelque chose.",
        },
        [VisitMood.Friends] = new[]
        {
            "Assez parlé : on y va !",
            "Allez, viens, j'ai une idée !",
            "Tu sais ce qui serait encore mieux que d'en parler ? Le faire !",
            "Le dernier arrivé est une pomme de pin !",
            "Bon, on arrête de parler et on s'amuse ?",
            "J'ai une idée géniale. Enfin, une idée. Viens !",
        },
        [VisitMood.BestFriends] = new[]
        {
            "Tu sais ce qu'on va faire ? La même chose que d'habitude. En mieux.",
            "Viens. J'ai gardé le meilleur pour toi.",
            "On fait comme la dernière fois ? Non : on fait encore mieux.",
            "J'ai eu une idée en t'écoutant. Tu vas adorer.",
            "Allez, on y va. Comme toujours, à deux.",
            "Arrête de parler, je sais déjà ce que tu vas dire. Viens plutôt.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Viens, on fait quelque chose ensemble. N'importe quoi, du moment que c'est avec toi.",
            "Donne-moi la main, on y va.",
            "J'ai une surprise. Ferme les yeux. Non, ouvre-les, sinon tu vas tomber. 💕",
            "On fait quelque chose de joli ensemble ? 💞",
            "Tu viens ? Je veux passer le reste de la journée avec toi.",
            "Assez parlé. Je veux juste être avec toi. 💕",
        },
        [VisitMood.Rivals] = new[]
        {
            "Assez parlé. Prouve-le.",
            "On règle ça tout de suite.",
            "Tu veux une démonstration ? Tu vas l'avoir.",
            "Un défi. Maintenant. Pas de discussion.",
            "Bon. On va voir qui a raison.",
            "Suis-moi, si tu l'oses.",
        },
        [VisitMood.Conflict] = new[]
        {
            "Bon. On va faire quelque chose, ou on reste là à se regarder ?",
            "Faisons quelque chose. Ça nous évitera de parler.",
            "J'ai une idée. Elle ne va pas te plaire.",
            "On n'est d'accord sur rien, alors faisons quelque chose, pour changer.",
            "Allez. Qu'on en finisse.",
            "Et si on faisait quelque chose avant que je m'énerve pour de bon ?",
        },
    };

    // Steps 4–5, when A talks about a typed passion: a fact or an anecdote, {P} standalone.
    public static readonly string[] CustomDetails =
    {
        "Le plus beau souvenir de ma vie ? Il a un rapport avec ça : {P}.",
        "J'y pense même en dormant. {P}, toujours {P}.",
        "Tout a commencé un mardi. Depuis, ma passion, c'est ça : {P}.",
        "Je pourrais en parler pendant des heures. Le sujet ? {P}. Évidemment.",
        "Je collectionne tout ce qui a un rapport avec ça : {P}. J'ai une boîte entière.",
        "Un jour, je serai {s:le meilleur|la meilleure} du monde. Discipline : {P}.",
        "Quand ça ne va pas, je pense à une seule chose : {P}. Et ça va mieux.",
        "Il y a encore tellement à apprendre. Le sujet est immense : {P}.",
        "J'en ai même rêvé la nuit dernière. Le thème du rêve : {P}. C'était très beau.",
        "Personne ne comprend pourquoi j'aime autant ça : {P}. Toi, peut-être ?",
    };

    // Beat 2 for a custom (typed) passion: narration, then the speaker's line.
    public static readonly string[] CustomOpeners =
    {
        "{S} se redresse, l'air très sérieux.\nJe dois t'avouer quelque chose. Ma passion ? {P}.",
        "{S} sort un carnet couvert de dessins.\nTout ça, c'est pour {P}. Oui, tout.",
        "{S} prend une grande inspiration.\nIl faut que je te parle de quelque chose d'important : {P}.",
        "{S} a l'air d'avoir attendu ce moment toute la journée.\nDevine à quoi j'ai pensé toute la semaine. {P}. Évidemment.",
        "{S} sort une petite affiche dessinée à la main. On y lit, en grand : {P}.\nJe l'ai faite ce matin. Tu la trouves comment ?",
        "{S} se met à parler très vite, les yeux brillants.\nTu sais ce qui me rend {s:heureux|heureuse} ? {P}. Tout le temps. Sans exception.",
        "{S} baisse la voix, comme pour un secret.\nPersonne ne le sait encore, mais ma grande passion, c'est… {P}.",
        "{S} montre un carnet rempli de notes.\nThème : {P}. J'ai tout noté. Tu veux voir la page quarante-deux ?",
        "{S} tapote sur un petit caillou pour réclamer le silence.\nAnnonce officielle. Mon sujet préféré au monde : {P}.",
        "{S} tend un dessin maladroit, visiblement fait avec amour. En légende : {P}.\nJe te l'offre. Tu comprendras en le regardant. Enfin, j'espère.",
        "{S} se racle la gorge, très {s:solennel|solennelle}.\nJe voulais te présenter ce qui compte le plus pour moi : {P}. Voilà. C'est dit.",
        "{S} ne tient pas plus de trois secondes sans en parler.\nBon, je craque. Mon sujet du jour : {P}.",
        "{S} arrive avec un petit panneau autour du cou. Dessus : {P}.\nComme ça, tout le monde sait. C'est plus simple.",
        "{S} fait durer le suspense en tournant autour de {L}.\nDevine ce qui me passionne en ce moment. Tu donnes ta langue au chat ? {P} !",
        "{S} ouvre une petite boîte avec cérémonie. À l'intérieur, une étiquette : {P}.\nC'est ma passion. Je la garde précieusement.",
        "{S} parle si vite que les mots se bousculent.\nJ'ai-découvert-un-truc-génial : {P}. Il-faut-que-je-t'explique-tout.",
    };

    // Beat 4 for a custom subject. No Conflict: in a conflict the activity is always the squabble.
    public static readonly IReadOnlyDictionary<VisitMood, string[]> CustomActivities = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[] { "{S} explique les bases à {L}. Sujet du jour : {P}. {L} hoche la tête très souvent.", "{S} montre à {L} une chose ou deux sur le sujet : {P}. {L} pose des questions très polies.", "{Ils} essaient ensemble, pour voir. Thème : {P}. C'est hésitant, mais sympathique.", "{S} fait une petite démonstration sur le thème : {P}. {L} applaudit, par politesse d'abord, puis pour de vrai.", "{S} prête ses notes à {L}. Titre en haut de la page : {P}. {L} les lit poliment, en entier.", "{Ils} essaient une petite activité sur le thème : {P}. C'est maladroit, mais personne ne se moque.", "{S} fait un exposé improvisé. Sujet : {P}. {L} applaudit à la fin, puis au milieu, puis encore à la fin.", "{Ils} découvrent un sujet ensemble, petit à petit : {P}. Pour une première fois, ce n'est pas si mal." },
        [VisitMood.Friends] = new[] { "{Ils} inventent un jeu sur le moment. Thème imposé : {P}.", "{Ils} fabriquent une affiche géante. En gros, au milieu : {P}. Il y a beaucoup trop de paillettes.", "{S} invente un quiz sur le sujet : {P}. {L} répond « caillou » à toutes les questions et gagne quand même.", "{Ils} passent l'après-midi sur un seul sujet : {P}. Le temps file sans prévenir.", "{Ils} inventent un jeu de société. Le thème : {P}. Les règles changent à chaque tour.", "{Ils} prennent des poses ridicules sur le thème : {P}. Chaque pose est plus absurde que la précédente.", "{Ils} improvisent une chanson dont le refrain est : {P}. Elle reste en tête toute la journée.", "{Ils} fabriquent un petit objet souvenir. Gravé dessus : {P}. Il est bancal et parfait." },
        [VisitMood.BestFriends] = new[] { "{Ils} fondent un club secret. Thème : {P}. Membres : deux. Mot de passe : secret.", "{Ils} rangent leurs souvenirs dans une boîte secrète. Sur le couvercle : {P}.", "{S} a préparé une surprise pour {L}, sur un thème qu'{s:il|elle} adore : {P}. {L} fait semblant d'être {l:étonné|étonnée}, puis l'est vraiment.", "{Ils} inventent un spectacle entier. Sujet : {P}. Public : un moineau. Il reste jusqu'au bout.", "{Ils} créent un langage secret qui ne sert qu'à parler d'une seule chose. La chose en question : {P}.", "{Ils} tiennent un carnet commun, rien que pour ça : {P}. Il est déjà presque plein.", "{Ils} montent une cabane dont le nom officiel est : {P}. Accès réservé aux deux membres.", "{L} offre à {S} un cadeau fait main, sur le thème : {P}. {S} ne s'en remet pas." },
        [VisitMood.Lovers] = new[] { "{S} fabrique un petit cadeau pour {L}, sur un thème bien précis : {P}.", "{S} écrit un petit poème pour {L}. Titre : {P}. La dernière rime est « toujours ».", "{Ils} partagent un goûter en parlant d'un seul sujet : {P}. Ou peut-être d'autre chose. Surtout d'autre chose.", "{S} emmène {L} voir quelque chose de spécial. Thème : {P}. {L} ne regarde que {S}.", "{Ils} s'écrivent des petits mots, tous sur le même thème : {P}. Et tous signés d'un cœur.", "{Ils} passent un long moment côte à côte, à rêver ensemble. Le rêve a un thème : {P}.", "{L} a préparé une surprise pour {S}, sur le thème : {P}. {S} en a les yeux qui brillent.", "{Ils} partagent un pique-nique en parlant d'un seul sujet : {P}. Enfin, surtout {p:l'un de l'autre|l'une de l'autre}." },
        [VisitMood.Rivals] = new[] { "Concours improvisé, un seul sujet : {P}. {Ils} se déclarent {p:tous|toutes} les deux {p:vainqueurs|gagnantes}.", "Duel de connaissances, un seul thème : {P}. Le score est serré. Il l'est toujours.", "{Ils} rédigent le règlement officiel. Sujet : {P}. Chaque règle a une exception inventée par l'autre.", "{S} lance un défi sur le thème : {P}. {L} le relève sans même demander les règles.", "Concours de précision sur un thème imposé : {P}. {Ils} contestent tous les résultats.", "Défi chronométré, sujet : {P}. Le chronomètre est un escargot. Personne ne sait qui a gagné.", "{S} se proclame {s:champion incontesté|championne incontestée}. Discipline : {P}. {L} conteste immédiatement.", "{Ils} fabriquent un trophée en écorce pour le vainqueur. Discipline : {P}. {Ils} se le disputent encore." },
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
    /// The story of <paramref name="outcome"/>, in eight steps: arrival, the opener (A raises the
    /// subject), B's reaction, two talk steps, a closer, the activity, then parting with
    /// <paramref name="outcomeLines"/> — what the visit changed. Each step carries both faces.
    /// <paramref name="pick"/> chooses a line from a pool (the handler passes ResponsePicker, so a
    /// channel does not see the same line twice in a row); by default it draws from <paramref name="rng"/>.
    /// </summary>
    public static VisitStory Build(VisitOutcome outcome, string outcomeLines, DateTimeOffset now, Random rng,
        Func<string[], string>? pick = null)
    {
        pick ??= pool => pool[rng.Next(pool.Length)];
        var visitor = Cast(outcome.Visitor, now);
        var host = Cast(outcome.Host, now);
        var place = PlaceFor(now, rng);
        var mood = MoodFor(outcome.GoodScene, outcome.After);
        var faces = Faces(mood);

        // A is the one whose passion is the subject; B the other.
        var aIsVisitor = rng.Next(2) == 0;
        var (a, b) = aIsVisitor ? (visitor, host) : (host, visitor);
        var subject = PickSubject(a.Passions, b.Passions, rng);
        var shared = b.Passions.Any(subject.SameAs);
        var info = subject.Catalog is { } c ? PlynlingPassions.Info(c) : null;

        // X: no speaker ({A}/{B} only). C: the conversation ({S} = A). By: {S} = the given one.
        string X(string t) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender);
        string C(string t) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender, aIsVisitor, subject.Render());
        string By(string t, bool visitorSays) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender, visitorSays, subject.Render());
        static string Said(VisitCast who, string line) => $"💬 **{who.Name}** : {line}";
        FacePair Talking(bool visitorTalks) => visitorTalks
            ? new FacePair(faces.Speaker, faces.Listener) : new FacePair(faces.Listener, faces.Speaker);

        var beats = new List<VisitBeat>();

        // 1. Arrival.
        var (arrival, arrivalTag) = Untag(pick(Arrivals[mood]));
        var f = new FacePair(faces.Narration, faces.Narration).Narrate(arrivalTag);
        beats.Add(f.Beat($"*{pick(place.Scenes)}*\n{X(arrival)}"));

        // 2. The opener: narration, then A's line.
        var opener = pick(info?.Openers ?? CustomOpeners).Split('\n');
        var (narration, narrationTag) = Untag(opener[0]);
        var (said, saidTag) = Untag(opener[1]);
        f = Talking(aIsVisitor).Narrate(narrationTag).Speak(saidTag, aIsVisitor);
        beats.Add(f.Beat($"{C(narration)}\n{Said(a, C(said))}"));

        // 3. B reacts; a shared catalog passion adds its shared line.
        var (reaction, reactionTag) = Untag(pick(Reactions[(mood, shared)]));
        f = Talking(!aIsVisitor).Speak(reactionTag, !aIsVisitor);
        var text = Said(b, C(reaction));
        if (shared && info is not null)
        {
            var (line, lineTag) = Untag(pick(info.SharedLines));
            f.Narrate(lineTag);
            text += "\n" + C(line);
        }
        beats.Add(f.Beat(text));

        // 4–5. Talk: usually the other one's turn, sometimes the same one again.
        var talkerIsA = false;
        for (var step = 0; step < 2; step++)
        {
            talkerIsA = rng.NextDouble() < TurnChance ? !talkerIsA : talkerIsA;
            var talkerIsVisitor = talkerIsA == aIsVisitor;
            var pool = !talkerIsA ? Followups[(mood, shared)]
                : rng.Next(2) == 0 ? (info?.Details ?? CustomDetails)
                : AnswerFollowups[(mood, shared)];
            var (talk, talkTag) = Untag(pick(pool));
            f = Talking(talkerIsVisitor).Speak(talkTag, talkerIsVisitor);
            beats.Add(f.Beat(Said(talkerIsA ? a : b, C(talk))));
        }

        // 6. The closer, from either of them; {S} is whoever says it.
        var closerIsVisitor = rng.Next(2) == 0;
        var (closer, closerTag) = Untag(pick(Closers[mood]));
        f = Talking(closerIsVisitor).Speak(closerTag, closerIsVisitor);
        beats.Add(f.Beat(Said(closerIsVisitor ? visitor : host, By(closer, closerIsVisitor))));

        // 7. The activity, then a little exchange (visitor, then host).
        var (activity, activityTag) = Untag(pick(ActivityPool(mood, info, b)));
        var exchange = pick(Exchanges[mood]).Split('\n');
        var (first, firstTag) = Untag(exchange[0]);
        var (second, secondTag) = Untag(exchange[1]);
        f = new FacePair(faces.Narration, faces.Narration).Narrate(activityTag).Speak(firstTag, true).Speak(secondTag, false);
        beats.Add(f.Beat($"{C(activity)}\n{Said(visitor, X(first))}\n{Said(host, X(second))}"));

        // 8. Parting. What the visit did sets the faces first; a tag only colours an ordinary parting.
        var (departure, departureTag) = Untag(pick(Departures[mood]));
        var ending = OutcomeFace(outcome);
        f = ending is { } end ? new FacePair(end, end) : new FacePair(faces.Narration, faces.Narration).Narrate(departureTag);
        beats.Add(f.Beat(string.IsNullOrWhiteSpace(outcomeLines) ? X(departure) : $"{X(departure)}\n{outcomeLines}"));

        var heading = $"{place.Emoji} {place.Name.Replace("{B}", host.Name)}";
        return new VisitStory("", heading, beats, visitor, host, PlynlingCatalog.Info(outcome.Host.Species).Accent);
    }

    // How often a talk step goes to the other one rather than the same one again.
    public const double TurnChance = 0.7;

    // The default faces by mood: whoever talks, whoever listens, and both during narration.
    private static (PlynlingMood Speaker, PlynlingMood Listener, PlynlingMood Narration) Faces(VisitMood mood) => mood switch
    {
        VisitMood.Acquaintances => (PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Content),
        VisitMood.Rivals => (PlynlingMood.Happy, PlynlingMood.Angry, PlynlingMood.Content),
        VisitMood.Conflict => (PlynlingMood.Angry, PlynlingMood.Angry, PlynlingMood.Angry),
        _ => (PlynlingMood.Happy, PlynlingMood.Happy, PlynlingMood.Happy),
    };

    // The parting faces a visit's outcome imposes, or null for an ordinary one.
    private static PlynlingMood? OutcomeFace(VisitOutcome o) =>
        o.Confession == Confession.Refused || (o.Before == PlynlingBond.Lovers && o.After != PlynlingBond.Lovers) ? PlynlingMood.Sad
        : o.Confession == Confession.Accepted ? PlynlingMood.Happy
        : o.After == PlynlingBond.Enemies && o.Before != PlynlingBond.Enemies ? PlynlingMood.Angry
        : null;

    // Both faces while a step is being put together.
    private sealed class FacePair(PlynlingMood visitor, PlynlingMood host)
    {
        private PlynlingMood _visitor = visitor, _host = host;

        // A narration tag: [face] for both, [A:…] / [B:…] for the visitor / the host.
        public FacePair Narrate(FaceTag? tag)
        {
            if (tag is null) return this;
            if (tag.Both is { } both) _visitor = _host = both;
            if (tag.A is { } va) _visitor = va;
            if (tag.B is { } hb) _host = hb;
            return this;
        }

        // A spoken line's tag: [face] is the speaker's own face.
        public FacePair Speak(FaceTag? tag, bool visitorSpeaks)
        {
            if (tag is null) return this;
            if (tag.Both is { } face)
            {
                if (visitorSpeaks) _visitor = face;
                else _host = face;
            }
            return Narrate(tag with { Both = null });
        }

        public VisitBeat Beat(string text) => new(text, _visitor, _host);
    }

    private static readonly Regex TagRx = new(
        @"^\[(?:(happy|content|sad|angry)|(?:A:(happy|content|sad|angry))?\s*(?:B:(happy|content|sad|angry))?)\]\s*",
        RegexOptions.Compiled);

    /// <summary>
    /// Reads a face tag off the start of a line — [sad], or [A:sad B:happy] — and returns the line
    /// without it. A line with no tag comes back unchanged with a null tag.
    /// </summary>
    public static (string Text, FaceTag? Tag) Untag(string line)
    {
        var m = TagRx.Match(line);
        if (!m.Success || m.Length <= 2) return (line, null);
        static PlynlingMood? Face(Group g) => g.Success ? Enum.Parse<PlynlingMood>(g.Value, ignoreCase: true) : null;
        return (line[m.Length..], new FaceTag(Face(m.Groups[1]), Face(m.Groups[2]), Face(m.Groups[3])));
    }

    // Step 7: a combo of the subject with one of the listener's catalog passions, else the subject's
    // own activities, else the custom ones for this mood. In a conflict, always the squabble.
    private static string[] ActivityPool(VisitMood mood, PassionInfo? subject, VisitCast listener)
    {
        if (mood == VisitMood.Conflict) return Activities[VisitMood.Conflict];
        if (subject is null) return CustomActivities[mood];
        var combos = listener.Passions
            .Where(p => p.Catalog is { } lc && lc != subject.Passion)
            .Select(p => PlynlingPassions.ComboFor(subject.Passion, p.Catalog!.Value))
            .OfType<string[]>()
            .SelectMany(x => x)
            .ToArray();
        return combos.Length > 0 ? combos : subject.Activities;
    }

    private static VisitCast Cast(Plynling p, DateTimeOffset now) => new(
        PlynlingCardUi.SafeName(p.Name),
        p.Species,
        PlynlingLife.Stage(p, now),
        PlynlingCatalog.Info(p.Species).Name,
        p.Gender,
        PlynlingPassions.Of(p));

    /// <summary>
    /// What the speaker talks about: a passion both share, <see cref="PlynlingPassions.SharedTopicChance"/>
    /// of the time when there is one, else any of the speaker's own.
    /// </summary>
    public static Passion PickSubject(IReadOnlyList<Passion> speaker, IReadOnlyList<Passion> listener, Random rng)
    {
        var shared = speaker.Where(p => listener.Any(p.SameAs)).ToList();
        if (shared.Count > 0 && rng.NextDouble() < PlynlingPassions.SharedTopicChance) return shared[rng.Next(shared.Count)];
        return speaker[rng.Next(speaker.Count)];
    }

    private static readonly Regex Choice = new(@"\{([abpsl]):([^|}]*)\|([^}]*)\}", RegexOptions.Compiled);

    /// <summary>
    /// Fills a template's names, pronouns and agreements. {S}/{L} are the speaker and the listener
    /// (the visitor speaks when <paramref name="visitorSpeaks"/>). {P} is inserted last, so text a
    /// person typed is never read as a template.
    /// </summary>
    public static string Expand(string template, string a, PlynlingGender ga, string b, PlynlingGender gb,
        bool visitorSpeaks = true, string? passion = null)
    {
        var girls = ga == PlynlingGender.Female && gb == PlynlingGender.Female;
        var (s, gs, l, gl) = visitorSpeaks ? (a, ga, b, gb) : (b, gb, a, ga);
        var text = Choice.Replace(template, m =>
        {
            var female = m.Groups[1].Value switch
            {
                "a" => ga == PlynlingGender.Female,
                "b" => gb == PlynlingGender.Female,
                "s" => gs == PlynlingGender.Female,
                "l" => gl == PlynlingGender.Female,
                _ => girls,
            };
            return female ? m.Groups[3].Value : m.Groups[2].Value;
        });
        text = text
            .Replace("{A}", $"**{a}**").Replace("{B}", $"**{b}**")
            .Replace("{S}", $"**{s}**").Replace("{L}", $"**{l}**")
            .Replace("{ils}", girls ? "elles" : "ils").Replace("{Ils}", girls ? "Elles" : "Ils");
        return passion is null ? text : text.Replace("{P}", passion);
    }

}
