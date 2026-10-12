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
    public string Sprite(PlynlingMood face) => PlynlingArt.VisitSprite(Species, Stage, face);
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
        new("🌳", "Au parc", 0, 24, new[] { "Un banc garde encore la chaleur de quelqu'un qui vient de partir.", "Un écureuil traverse l'allée, s'arrête, reconsidère sa vie, et repart.", "Les feuilles bruissent comme si elles se racontaient quelque chose." }),
        new("🍂", "Sous le grand chêne", 0, 24, new[] { "Des glands tombent de temps en temps, avec un petit « toc » poli.", "Le vieux chêne fait de l'ombre à tout le monde, sans rien demander.", "Une racine forme un banc parfait. Quelqu'un l'a usée, avant eux." }),
        new("🏡", "Chez {B}", 0, 24, new[] { "Ça sent bon le gâteau aux noisettes.", "La maison est petite, mais chaque chose y a sa place, et le sait.", "Un coussin trop gonflé attend les invités avec impatience." }),
        new("☕", "Au café du coin", 0, 24, new[] { "La tortue qui sert a pris une commande il y a une heure. Elle arrive.", "Ça sent le chocolat chaud et le bois ciré.", "Une cuillère tinte contre une tasse, quelque part, puis plus rien." }),
        new("🪵", "Au bord de l'étang", 0, 24, new[] { "Une grenouille observe la scène depuis son nénuphar, sans prendre parti.", "L'eau fait des ronds qui s'agrandissent jusqu'à disparaître.", "Une libellule s'arrête en plein vol, comme pour écouter." }),
        new("🌼", "Dans la prairie", 6, 20, new[] { "Les fleurs sentent bon, et les abeilles bourdonnent, très affairées.", "L'herbe haute chatouille tout le monde, sans exception.", "Un grillon répète la même note, très sûr de lui." }),
        new("☀️", "Au soleil, sur les rochers", 10, 19, new[] { "Les pierres sont toutes chaudes. Parfait pour une sieste.", "Un lézard leur cède la place, de très mauvaise grâce.", "Le soleil tape juste assez pour qu'on ferme les yeux sans dormir." }),
        new("🌙", "Sur le toit, sous les étoiles", 20, 6, new[] { "Le toit est encore tiède du soleil de la journée.", "La lune est ronde, et une chouette la commente à voix basse.", "Les tuiles craquent doucement sous chaque pas." }),
        new("🌉", "Sur le vieux pont de pierre", 0, 24, new[] { "L'eau murmure sous les arches, toujours la même histoire.", "Un héron immobile fait semblant de ne rien voir.", "Une pierre du parapet bouge un peu. Tout le monde le sait. Personne ne la répare." }),
        new("🌲", "Dans la clairière", 0, 24, new[] { "Un rayon de lumière traverse les branches et se pose au milieu, comme un tapis.", "Des papillons zigzaguent entre les troncs, sans itinéraire.", "Le silence y est si épais qu'on a envie de chuchoter." }),
        new("🍃", "Sous une grande feuille, à l'abri de la pluie", 0, 24, new[] { "Les gouttes tambourinent sur la feuille, tout là-haut.", "Il fait sec, tiède, et un peu trop calme.", "De temps en temps, une goutte réussit à passer, et atterrit toujours au même endroit." }),
        new("🌿", "Dans le jardin de {B}", 6, 21, new[] { "Les plates-bandes sont parfaitement alignées. Presque toutes.", "Un petit ~~Ina~~ nain de jardin en cailloux monte la garde.", "Un arrosoir oublié s'est rempli de pluie, et un escargot y prend son bain." }),
        new("🌅", "Sur la colline, au lever du jour", 5, 9, new[] { "Le ciel passe doucement du rose à l'orange.", "La rosée brille sur l'herbe, et mouille tous les pieds.", "En bas, le village dort encore. Un seul toit fume." }),
        new("🥖", "À la boulangerie", 6, 19, new[] { "Ça sent le pain chaud jusque dans la rue.", "Une abeille goûte discrètement la confiture.", "Le boulanger, un blaireau enfariné, chante en pétrissant, faux et heureux." }),
        new("🌾", "Dans le champ de blé", 7, 20, new[] { "Les épis ondulent comme une mer dorée.", "Un épouvantail leur fait un clin d'œil. Enfin, il en a l'air.", "Un sentier minuscule traverse le champ, tracé par quelqu'un de très petit." }),
        new("🍎", "Au verger", 8, 20, new[] { "Ça sent la pomme mûre et l'herbe coupée.", "Un ver, très poli, s'excuse d'être là.", "Une pomme tombe, roule, et s'arrête pile devant eux. Comme une invitation." }),
        new("🛒", "Au marché du village", 8, 19, new[] { "Des étals de baies et de noisettes, à perte de vue.", "Un marchand de cailloux vante sa marchandise avec passion.", "Une pie examine les bijoux d'un air très professionnel." }),
        new("📚", "À la bibliothèque du village", 9, 19, new[] { "Ça sent le vieux papier et la poussière dorée.", "La bibliothécaire, un hibou, fait « chut » d'un seul œil.", "Un livre est resté ouvert sur une table, à la page la plus intéressante." }),
        new("🏖️", "Sur la plage de galets", 9, 21, new[] { "Les vagues font rouler les galets avec un joli bruit.", "Un crabe pince-sans-rire surveille la plage.", "Le vent sent le sel et les secrets." }),
        new("🛶", "Sur une barque, au milieu de l'étang", 10, 20, new[] { "La barque tangue un peu. Personne ne sait ramer.", "Un poisson curieux vient voir de plus près.", "Une rame flotte tranquillement un peu plus loin. Elle a pris son indépendance." }),
        new("🎡", "À la fête foraine", 14, 23, new[] { "Une petite musique de manège flotte dans l'air.", "Des lampions colorés se balancent au vent.", "Ça sent la barbe à papa et le sucre brûlé." }),
        new("🔥", "Autour du feu de camp", 18, 1, new[] { "Les flammes crépitent et font danser les ombres.", "Ça sent la noisette grillée.", "Une étincelle monte, monte, et devient presque une étoile." }),
        new("✨", "Dans la grotte aux lucioles", 20, 1, new[] { "Les lucioles dessinent des constellations sur les parois.", "L'écho répète tout, un peu de travers.", "Une goutte tombe quelque part, toujours au même rythme, comme une horloge." }),
        new("🛖", "Dans la cabane dans l'arbre", 0, 24, new[] { "L'échelle de corde grince à chaque pas.", "Le plancher est couvert de coussins moelleux.", "Par la fenêtre ronde, on voit tout le village, en tout petit." }),
        new("🕰️", "Dans le vieux grenier", 0, 24, new[] { "La poussière danse dans un rayon de lumière, sans se presser.", "Une malle entrouverte laisse dépasser la manche d'un vieux costume.", "Quelque part sous les poutres, un loir ronfle." }),
        new("🌈", "Sous l'arc-en-ciel", 8, 20, new[] { "Les couleurs se reflètent dans les flaques.", "Personne n'a trouvé le pot d'or, mais tout le monde a cherché.", "L'arc-en-ciel pâlit doucement, comme s'il savait qu'on le regarde." }),
        new("🏔️", "Sur la falaise, face au vent", 8, 19, new[] { "Le vent ébouriffe tout ce qui dépasse.", "En bas, le monde a l'air minuscule.", "Une mouette plane sans un battement d'ailes, très fière d'elle." }),
        new("⛲", "Près de la fontaine de la place", 7, 22, new[] { "L'eau clapote, et des pièces brillent au fond du bassin.", "Un pigeon se prend pour le maître des lieux.", "Chaque pièce au fond du bassin est un vœu. Certaines ont l'air très anciennes." }),
        new("🐝", "Devant la ruche", 8, 19, new[] { "Le bourdonnement est presque une berceuse.", "Une abeille très occupée leur fait signe de ne pas gêner.", "Ça sent le miel et la cire tiède." }),
        new("🎣", "Au bord de la rivière", 6, 20, new[] { "Le courant emporte une feuille comme un petit bateau.", "Un poisson passe en ricanant : il a compris qu'il n'y avait pas d'appât.", "Des cailloux plats attendent sur la rive qu'on les fasse ricocher." }),
        new("🍦", "Chez le glacier", 12, 22, new[] { "Des boules de glace colorées s'alignent derrière la vitre.", "Le glacier, un vieil ours, sert chaque cornet avec un soin infini.", "Une goutte de glace fond sur le comptoir, lentement, avec regret." }),
        new("🌌", "Dans un champ, à regarder les étoiles", 21, 1, new[] { "Une étoile filante passe, puis une autre.", "L'herbe est fraîche, et le ciel n'en finit pas.", "Quelque part, un grillon joue la même note que la plus petite étoile." }),
        new("🚂", "À la petite gare", 6, 22, new[] { "Le chef de gare, un hérisson, consulte sa montre, puis le ciel, puis encore sa montre.", "Un petit train siffle au loin, sans jamais avoir l'air d'approcher.", "Sur le quai, une valise attend quelqu'un depuis très longtemps." }),
        new("🎭", "Au petit théâtre du village", 15, 23, new[] { "Le rideau rouge est un peu mité, mais très fier.", "Dans les coulisses, quelqu'un répète la même réplique en boucle.", "Les sièges grincent chacun sur une note différente." }),
        new("🍵", "Au salon de thé", 13, 19, new[] { "Les tasses sont minuscules, et les gâteaux aussi.", "Une théière ronronne doucement sur la table.", "La nappe à carreaux a une tache en forme de lapin. Tout le monde la regarde." }),
        new("🎨", "Dans l'atelier du peintre", 9, 18, new[] { "Ça sent la peinture et le bois. Il y a des taches partout.", "Un tableau à moitié fini attend qu'on lui trouve un titre.", "Un pinceau sèche dans un pot, la tête en l'air, comme s'il réfléchissait." }),
        new("🛝", "Au terrain de jeux", 8, 20, new[] { "La balançoire grince gentiment.", "Le toboggan est bien trop grand pour tout le monde, et c'est parfait.", "Le bac à sable garde les traces d'un château très ambitieux." }),
        new("🥾", "Sur le sentier de randonnée", 7, 19, new[] { "Un petit panneau indique trois directions, toutes fausses.", "Les cailloux du chemin roulent sous les pas.", "Une fourmi porte une miette dans l'autre sens. Elle sait ce qu'elle fait." }),
        new("🌬️", "Dans les ruines du vieux moulin", 9, 20, new[] { "Le moulin ne tourne plus, mais le vent essaie encore.", "Les pierres racontent des histoires que personne n'écoute.", "Une fleur a poussé dans une fente du mur, là où personne ne l'attendait." }),
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Arrivals = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "{A} arrive avec trois phrases préparées. {a:Il|Elle} les oublie toutes en voyant {B}, et dit : « Ah. Bonjour. »",
            "{B} fait un petit signe de la main. {A} en fait un aussi, puis se demande si le deuxième était vraiment nécessaire.",
            "{A} et {B} se disent bonjour en même temps, puis « pardon » en même temps, puis se taisent en même temps. Au moins, c'est synchronisé.",
            "{A} s'arrête à bonne distance et fait un salut très poli. {B} répond par un salut encore plus poli. Ça pourrait durer longtemps.",
            "{A} tend un petit cadeau emballé dans une feuille, puis hésite à le lâcher. {B} tire doucement. Le cadeau finit par changer de main.",
            "{B} a préparé un sujet de conversation. Il s'appelle « la météo ». Il tient environ quatre secondes.",
            "{A} arrive pile à l'heure, {a:essoufflé|essoufflée} d'avoir couru pour ne surtout pas être en avance.",
            "{B} s'est longtemps demandé s'il fallait dire « bonjour » ou « salut ». {b:Il|Elle} dit « bonlut ».",
            "{A} fait un pas en avant, {B} un pas en arrière, pour laisser de la place. {Ils} recommencent. On dirait une danse, en moins sûr.",
            "{A} regarde ses pieds, {B} regarde le ciel. {Ils} finissent par se regarder, par accident, et sourient, par réflexe.",
            "{B} tend la main. {A} tend la sienne une seconde trop tard. Deuxième essai : réussi. {Ils} en sont un peu {p:fiers|fières}.",
            "{A} arrive en répétant le prénom de {B} dans sa tête, pour ne pas se tromper. {a:Il|Elle} ne se trompe pas. C'est déjà une victoire.",
            "{B} avait prévu d'avoir l'air très {b:détendu|détendue}. {b:Il|Elle} l'a trop prévu : ça se voit.",
            "« Tu as trouvé facilement ? » demande {B}. « Oui ! » répond {A}, qui s'est {a:perdu|perdue} deux fois.",
            "{A} a mis sa plus belle écharpe. {B} la remarque tout de suite, et ne trouve aucune façon de le dire qui ne soit pas bizarre.",
            "Un petit silence, quand {A} arrive. Pas un mauvais silence. Le genre de silence qui attend qu'on le remplisse.",
        },
        [VisitMood.Friends] = new[]
        {
            "{A} arrive les mains fermées sur quelque chose. « Devine. » {B} devine trois fois de travers, exprès, pour faire durer.",
            "{B} entend {A} arriver bien avant de {a:le|la} voir : {a:il|elle} chante faux, fort, et toujours la même chanson.",
            "{A} arrive les poches pleines : un caillou rayé, une plume, un gland mordillé. « J'ai tout gardé pour te le montrer. »",
            "{B} fait semblant d'être très {b:occupé|occupée}. Ça tient trois secondes, puis {b:il|elle} court au-devant de {A}.",
            "{A} arrive en marchant sur les mains. Enfin, sur une main et demie. {B} applaudit avant la chute, par prudence.",
            "{A} et {B} se tapent dans la main, ratent, recommencent, ratent encore. Au troisième essai, {ils} renoncent et se font un câlin.",
            "{B} a gardé pour {A} la meilleure part de son goûter. Elle a un peu fondu en attendant. {A} la trouve parfaite quand même.",
            "{A} arrive avec une feuille collée dans le dos. {B} décide de ne rien dire. Pour l'instant.",
            "{A} arrive en racontant déjà une histoire, visiblement commencée bien avant d'être à portée de voix.",
            "{B} attendait {A} en faisant des ricochets imaginaires. {b:Il|Elle} lance le dernier en {a:le|la} voyant, et annonce : « Sept. »",
            "{A} arrive avec deux bâtons presque identiques. « Un pour toi. » Personne ne sait encore à quoi ils serviront. Ça viendra.",
            "{B} crie le nom de {A} en {a:le|la} voyant. Un oiseau s'envole. {A} crie le nom de {B}. Un deuxième oiseau s'envole.",
            "{A} surgit derrière {B} avec un « Bouh ! » très réussi. {B} sursaute, puis jure qu'{b:il|elle} l'avait vu venir.",
            "{A} arrive avec un mot tout juste appris, et le place dès sa première phrase. Il ne va pas du tout. {B} hoche la tête, très {b:impressionné|impressionnée}.",
            "{B} a dessiné {A} dans la poussière, pour patienter. Le dessin a un sourire immense. {A} le trouve très ressemblant.",
            "{A} arrive {a:essoufflé|essoufflée} : {a:il|elle} a couru tout le chemin, sans raison, juste parce que c'était {B} au bout.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "{A} et {B} se reconnaissent de loin et se mettent à courir en même temps. Le choc est violent. Le câlin aussi.",
            "{B} entend arriver {A} au bruit de ses pas. Personne d'autre ne marche comme ça. Heureusement.",
            "{A} arrive sans rien dire et s'assoit à sa place. {B} lui tend la moitié de son goûter sans rien dire non plus. Le rituel est parfait.",
            "« C'est moi ! » crie {A}. « Je sais ! » répond {B}, sans même se retourner.",
            "{A} et {B} font leur salut secret. Il dure maintenant presque une minute, et comporte une révérence.",
            "{A} arrive avec une histoire à raconter. {B} en a deux. {Ils} décident de tout se dire en même temps, et comprennent tout.",
            "{B} a déjà préparé le goûter préféré de {A}. Comme toujours. Comme depuis toujours.",
            "{A} arrive un peu {a:fatigué|fatiguée}. {B} le voit tout de suite et, sans un mot, fait de la place à côté de {b:lui|elle}.",
            "{A} et {B} reprennent la conversation exactement là où {ils} l'avaient laissée. La dernière fois, c'était au milieu d'un mot.",
            "{B} fait semblant d'être {b:fâché|fâchée} : « Tu es en retard. » {A} fait semblant de s'excuser. {Ils} rient avant la fin de la phrase.",
            "{A} arrive avec deux cailloux identiques. « Un pour toi, un pour moi. » {B} a exactement les mêmes dans sa poche.",
            "{B} n'a pas besoin de lever les yeux pour savoir que c'est {A}. Il n'y a qu'une seule personne qui fredonne aussi faux.",
            "{A} arrive avec un mot plié en quatre : « Je t'ai écrit en venant. » {B} le lira plus tard. {B} sait déjà ce qu'il y a dedans.",
            "{A} débarque et commence : « Alors, tu ne devineras jamais. » {B} s'assoit. {b:Il|Elle} connaît la suite : ce sera long, et ce sera bien.",
            "{B} regarde arriver {A} et sent quelque chose se remettre en place, comme un caillou qui retrouve son trou.",
            "{A} et {B} se font la grimace de bienvenue. Elle a évolué avec le temps. Elle est maintenant très laide, et très précieuse.",
        },
        [VisitMood.Lovers] = new[]
        {
            "{A} arrive avec des fleurs cueillies en chemin. Elles sont un peu écrasées. {B} les trouve parfaites, et le dit deux fois. 💐",
            "{B} guettait {A} depuis un bon moment, en faisant semblant de lire. La page n'a pas bougé. 💞",
            "{A} et {B} se sourient de loin pendant tout le temps qu'il faut pour se rejoindre. C'est long, et c'est exactement la bonne durée.",
            "{B} tend la main à {A} sans rien dire. {A} la prend sans rien dire non plus. 💕",
            "{A} arrive un peu trop vite, puis ralentit pour avoir l'air {a:détendu|détendue}. {B} a vu les deux vitesses.",
            "{B} s'est {b:recoiffé|recoiffée} trois fois avant l'arrivée de {A}. {A} remarque tout de suite la troisième. 💞",
            "{A} rougit en arrivant. {B} rougit en {a:le|la} voyant rougir. Autour, même les fleurs ont l'air gênées. 💕",
            "{A} apporte un petit caillou en forme de cœur. {B} le range aussitôt dans sa poche, du côté du cœur. Le vrai.",
            "{A} apparaît au bout du chemin, et {B} oublie complètement ce qu'{b:il|elle} était en train de dire, et à qui. 💞",
            "{A} et {B} s'arrêtent à deux pas {p:l'un de l'autre|l'une de l'autre}, sans savoir qui doit faire le dernier. {Ils} le font en même temps. 💕",
            "{B} a gardé la meilleure place, et fait semblant de ne pas l'avoir gardée. {A} fait semblant de ne pas le savoir.",
            "{A} arrive avec un poème appris par cœur. Il en reste la moitié à l'arrivée. {B} applaudit la moitié. 💞",
            "« Tu es en avance », dit {B}. « Je n'arrivais pas à attendre », avoue {A}. C'est dit. {Ils} ne savent plus où regarder. 💕",
            "{B} a préparé deux tasses. Puis une troisième, au cas où la première refroidirait pendant qu'{b:il|elle} regarde {A}.",
            "{A} arrive, et tout devient un peu flou autour, comme quand on regarde quelque chose de très près. 💞",
            "{A} et {B} se disent bonjour. Ça devrait être simple, bonjour. Ce ne l'est pas du tout. 💕",
        },
        [VisitMood.Rivals] = new[]
        {
            "{A} arrive en bombant le torse. {B} fait semblant de ne pas {a:le|la} remarquer, en {a:le|la} remarquant très fort.",
            "« Encore toi ? » lance {B}. Mais {b:il|elle} avait gardé une place libre, juste à côté.",
            "{A} arrive en s'échauffant les jambes, comme avant un vrai duel. {B} lève un sourcil, puis s'échauffe aussi, discrètement.",
            "{B} a tracé une ligne dans la poussière avant même l'arrivée de {A}. Juste au cas où. Il y a toujours un cas où.",
            "{A} et {B} se saluent d'un signe de tête très sec. Si sec qu'on l'entend presque craquer.",
            "{A} arrive avec un carnet de scores. {B} sort le sien. Les deux scores ne sont pas du tout les mêmes.",
            "[angry] {B} attendait {A} de pied ferme. « Tu es en retard. » « Toi, tu as peur. » Match nul, déjà.",
            "{A} arrive très lentement, pour bien montrer qu'{a:il|elle} n'est pas {a:pressé|pressée}. Ça prend un temps fou. {B} s'impatiente : c'était le but.",
            "« Tiens, te voilà. » « Tiens, tu es encore là. » Les politesses sont terminées. Place aux choses sérieuses.",
            "[angry] {A} et {B} se fixent en silence. Le premier qui cligne des yeux a perdu. {Ils} clignent en même temps, et contestent.",
            "{A} arrive en sifflotant l'air de la victoire. {B} connaît la suite, et la siffle plus fort, et plus juste.",
            "{B} a mis ses plus beaux habits, pour que {A} voie bien qu'{b:il|elle} ne s'est pas {b:habillé|habillée} spécialement pour {a:lui|elle}.",
            "{A} arrive avec un sourire en coin. {B} a le même, de l'autre côté. Vu de face, ça fait un sourire entier.",
            "{B} fait mine de s'étirer quand {A} arrive, comme avant un match. Il va peut-être y avoir un match. Il y a toujours un match.",
            "{A} dépose un petit caillou aux pieds de {B}. « Pour ta collection de défaites. » {B} le garde. Pour la revanche.",
            "{A} et {B} arrivent au même endroit au même moment, par deux chemins différents. Personne n'avouera avoir couru.",
        },
        [VisitMood.Conflict] = new[]
        {
            "{A} arrive en traînant des pieds. {B} croise les bras. Personne n'a encore rien dit, et c'est déjà trop.",
            "{A} arrive sans dire bonjour. {B} ne dit pas bonjour non plus. Égalité parfaite, et glaciale.",
            "{B} regarde arriver {A} en soupirant très fort, pour être {b:sûr|sûre} que le soupir arrive jusqu'à {a:lui|elle}.",
            "L'air se refroidit d'un coup quand {A} apparaît. Même les oiseaux se taisent, par précaution.",
            "« Ah. C'est toi. » « Ah. C'est toi aussi. » Voilà. On ne pourra pas dire qu'{ils} ne se sont pas parlé.",
            "{A} arrive en retard exprès. {B} l'a remarqué, et l'a noté quelque part de très vexant.",
            "{B} avait promis d'être aimable. La promesse a tenu jusqu'à ce que {A} ouvre la bouche. Trois secondes, environ.",
            "{A} s'installe exactement là où {B} voulait s'asseoir. Par hasard, jure {a:il|elle}. Personne ne croit au hasard, ici.",
            "{A} arrive avec une liste de reproches. {B} a préparé la sienne. Elle est plus longue, et mieux écrite.",
            "{A} et {B} se disent bonjour du bout des lèvres. Même le bonjour a l'air vexé.",
            "[angry] {A} marche exprès dans la flaque, juste devant {B}. {B} ne bouge pas, exprès aussi. {Ils} sont {p:mouillés|mouillées}, et {p:fiers|fières} de l'être.",
            "{B} a mis ses plus vieilles affaires. Pour {A}, c'est bien suffisant.",
            "{A} arrive, s'arrête, et attend que {B} dise quelque chose. {B} attend aussi. {Ils} pourraient attendre très longtemps.",
            "{A} arrive en sifflotant, très faux et très exprès. {B} connaît cet air : c'est celui qu'{b:il|elle} déteste le plus.",
            "{B} voit arriver {A} et range discrètement tout ce qui pourrait servir de projectile. On ne sait jamais.",
            "Un pigeon passe entre {A} et {B}, sent l'ambiance, et fait demi-tour. Le pigeon a du flair.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Activities = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "{Ils} s'assoient côte à côte et regardent passer une coccinelle. Elle prend son temps. {Ils} aussi.",
            "{B} sert une tasse de rosée à {A}. {Ils} la boivent à toutes petites gorgées, pour que ça dure.",
            "{A} et {B} comparent leurs plus beaux cailloux. {p:Chacun|Chacune} trouve celui de l'autre plus joli, le dit, et le pense.",
            "{B} propose un biscuit. {A} accepte, puis hésite longuement devant le deuxième. {B} fait semblant de ne rien voir.",
            "{Ils} se demandent quel chemin est le plus joli. {Ils} ne sont pas d'accord, mais avec énormément de tact.",
            "{A} montre une plume trouvée par terre. « Oh, jolie ! » dit {B}, avec une sincérité qui surprend tout le monde, {b:lui compris|elle comprise}.",
            "Partie de cartes. {B} connaît les règles, à peu près. {A} fait semblant de les connaître aussi. Ça marche étonnamment bien.",
            "{Ils} se partagent une noisette en se répétant : « Non, prends la plus grosse. » La noisette finit coupée en trois.",
            "{A} et {B} se surprennent à regarder la même chose, puis {p:l'un l'autre|l'une l'autre}, puis ailleurs. Ça fait beaucoup de regards pour si peu de temps.",
            "{B} lit quelques lignes d'un livre à voix haute. {A} hoche la tête à chaque phrase, sans tout suivre, mais avec conviction.",
            "{Ils} font une petite promenade. Il y a des silences, mais de moins en moins, et de plus en plus confortables.",
            "{A} apprend à {B} un jeu de ficelle. Au bout de dix minutes, la ficelle est un nœud, et {ils} rient enfin pour de vrai.",
        },
        [VisitMood.Friends] = new[]
        {
            "Course jusqu'au grand arbre ! {A} gagne d'un souffle. {B} conteste d'un autre.",
            "Partie de cache-cache : {B} se cache derrière une feuille bien trop petite. {A} fait semblant de chercher très longtemps.",
            "Grande bataille de feuilles mortes. Personne ne gagne. Tout le monde a des feuilles dans des endroits surprenants.",
            "{Ils} organisent une course d'escargots. Celui de {A} s'endort en route, celui de {B} fait demi-tour. Match nul, très sportif.",
            "{A} a apporté un ballon. Il finit coincé dans un arbre en moins de trois minutes. L'arbre refuse de le rendre.",
            "Partie de dés. Les règles changent à chaque tour, et chaque changement est voté à l'unanimité, c'est-à-dire par {A}.",
            "{A} invente un jeu sur le moment. {B} en devient {b:le champion|la championne} avant même d'en connaître les règles.",
            "{Ils} chantent à tue-tête une chanson dont personne ne connaît les paroles. Le refrain est inventé deux fois, différemment.",
            "{B} apprend à {A} à faire voler une feuille. Elle retombe. On recommence. Au dixième essai, elle vole trois secondes. Triomphe.",
            "Enquête : qui a mangé la dernière noisette ? Le principal suspect est {A}. Le deuxième aussi.",
            "{A} tente une galipette pour épater {B}. La réception est… originale. {B} applaudit la réception.",
            "{Ils} construisent une cabane avec trois bâtons et beaucoup d'imagination. Elle tient debout. {Ils} n'osent plus respirer.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "{Ils} construisent une cabane en brindilles et jurent de ne jamais révéler où. Le lendemain, plus personne ne sait où. Le secret est parfaitement gardé.",
            "{Ils} se racontent des secrets à voix basse, en pouffant. Certains ont déjà été racontés dix fois. Ils sont toujours aussi bons.",
            "{A} et {B} inventent une danse que personne d'autre ne connaîtra jamais. Elle comporte un pas appelé « l'escargot fâché ».",
            "{Ils} se tressent des bracelets d'herbe sans même regarder leurs mains. Ça marche. Ça marche toujours.",
            "{Ils} sautent dans les flaques en se tenant la main, sans chercher à savoir qui est le plus mouillé. C'est {B}. De loin.",
            "{A} cuisine, {B} goûte, {A} recommence. Ça dure longtemps, et c'est exactement ce qu'{ils} voulaient.",
            "{Ils} rédigent la charte officielle de leur amitié, en trois exemplaires. Personne ne sait pourquoi trois. C'est dans la charte.",
            "{Ils} fredonnent la même mélodie sans s'être concertés, puis éclatent de rire en s'en rendant compte.",
            "{A} pose la tête sur l'épaule de {B}, qui ne bouge plus du tout, pour ne pas déranger. Au bout d'un moment, crampe. {B} ne bouge toujours pas.",
            "{Ils} finissent les phrases {p:l'un de l'autre|l'une de l'autre}, se trompent une fois sur deux, et trouvent que les mauvaises fins sont meilleures.",
            "{Ils} font la liste de tout ce qu'{ils} feront plus tard, ensemble. La liste est longue. Il faudra vivre très vieux.",
            "{A} et {B} regardent les nuages et leur trouvent des formes. Tous ressemblent à des souvenirs communs. Même le rond.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Goûter en tête-à-tête : {A} garde la plus grosse fraise pour {B}. {B} la coupe en deux. Tout le monde y gagne. 💕",
            "{Ils} comptent les nuages, {p:serrés l'un contre l'autre|serrées l'une contre l'autre}. Au troisième, plus personne ne compte.",
            "{A} a cueilli un petit bouquet de trèfles pour {B}. L'un d'eux a quatre feuilles. {A} jure que c'est un hasard. 💞",
            "{B} fredonne une chanson douce. {A} écoute, les yeux fermés, sans se rendre compte qu'{a:il|elle} sourit.",
            "{A} récite un poème un peu maladroit. La dernière rime ne rime pas. {B} le trouve magnifique, surtout la dernière rime. 💕",
            "{Ils} partagent un pot de miel avec une seule cuillère. C'est romantique, et très collant. 💕",
            "{A} enlève délicatement une feuille de l'épaule de {B}. Le geste dure un peu plus longtemps que nécessaire. Beaucoup plus. 💞",
            "{B} offre à {A} un caillou parfaitement rond, emballé dans trois feuilles. {A} met très longtemps à l'ouvrir, pour faire durer.",
            "{Ils} marchent lentement, épaule contre épaule, sans savoir où {ils} vont. Ça n'a aucune importance. 💞",
            "{A} se regarde dans une flaque, remet une mèche en place, et espère que {B} n'a rien vu. {B} a tout vu. {B} a trouvé ça adorable. 💕",
            "{Ils} regardent le ciel changer de couleur, sans rien dire. Il y a beaucoup de choses dans ce silence.",
            "{A} glisse un mot doux dans la poche de {B}, qui le trouvera plus tard et rougira très fort, {b:tout seul|toute seule}, en plein chemin. 💕",
        },
        [VisitMood.Rivals] = new[]
        {
            "Concours de grimaces ! Le jury, une fourmi, refuse de départager. Elle s'en va, choquée.",
            "[A:happy B:angry] Qui saute le plus haut ? {A} jure avoir gagné. {B} exige une revanche, puis une deuxième, puis un arbitre.",
            "Course jusqu'à l'étang. Match nul. Personne ne veut l'admettre, alors {ils} recommencent. Match nul. {Ils} n'en parlent plus.",
            "[A:angry B:happy] {Ils} empilent des cailloux : la tour de {B} tient, celle de {A} s'écroule. {A} accuse le vent. Il n'y a pas de vent.",
            "Concours de celui qui mange le plus vite. {A} s'étouffe, {B} déclare sa victoire trop tôt, et s'étouffe aussi.",
            "Tir aux cailloux sur une cible en feuille. {p:Chacun|Chacune} jure avoir visé le centre. La feuille n'a plus de centre.",
            "[A:happy B:angry] {Ils} grimpent sur la même souche. {A} arrive en haut {a:le premier|la première}. {B} exige un chronomètre, et un recomptage.",
            "Bras de fer ! Il dure très longtemps et ne mène à rien, ce qui arrange tout le monde.",
            "{B} propose un jeu. {A} le gagne. {B} change les règles. {A} le gagne encore. {B} déclare le jeu nul et non avenu.",
            "{Ils} mesurent leurs ombres. La discussion sur la méthode dure plus longtemps que la mesure, et que l'ombre.",
            "Concours de poses héroïques. Le jury, une coccinelle, s'envole au milieu de la délibération. Égalité, par forfait du jury.",
            "{A} et {B} passent la ligne d'arrivée au même instant. {p:Chacun|Chacune} est {p:persuadé|persuadée} d'avoir gagné. Le ruban aussi a son avis : il est cassé.",
        },
        [VisitMood.Conflict] = new[]
        {
            "Dispute pour le dernier gland. {Ils} finissent par le couper en deux, en boudant. Les deux moitiés sont inégales. Ça recommence.",
            "{Ils} boudent dos à dos pendant un bon moment. C'est le moment le plus calme de la journée.",
            "{A} et {B} ne sont d'accord sur rien, pas même sur la couleur du ciel. Le ciel ne prend pas parti.",
            "{B} soupire à chaque phrase de {A}. {A} parle plus lentement, pour faire durer les soupirs.",
            "Match de regards noirs. {A} cligne des yeux en premier, mais jure que non. Il faudrait un témoin. Il n'y a pas de témoin.",
            "{B} s'installe sur le seul coin confortable. {A} reste debout, exprès, pour que ça se voie. Ça se voit.",
            "Il reste une seule part de gâteau. {Ils} la fixent longtemps sans que personne bouge. Elle finit par sécher. Personne n'a gagné.",
            "{A} commence une phrase, {B} la termine de travers. Ça dégénère tout de suite.",
            "{A} trace une ligne par terre : « Ton côté, mon côté. » {B} déborde déjà. Exprès. D'un orteil.",
            "{p:Chacun|Chacune} a sa version de l'histoire, et chaque version est très, très injuste pour l'autre.",
            "Un oiseau se pose entre {A} et {B}, sent l'ambiance, et repart aussitôt. Même l'oiseau a compris.",
            "{A} propose un jeu, {B} refuse. {B} en propose un autre, {A} refuse. Il n'y aura pas de jeu. Il y aura une dispute sur le jeu.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Exchanges = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "Tu viens souvent par ici ?\nJe viens. Parfois. Là, par exemple.",
            "Je m'appelle {A}, au cas où.\nJe sais. Moi, c'est {B}, au cas où aussi.",
            "C'est joli, ici.\nMerci. Je n'y suis pour rien, mais merci.",
            "Tu veux qu'on se tutoie ?\nOn se tutoie depuis tout à l'heure. Mais c'est gentil de demander.",
            "Je ne te dérange pas, au moins ?\nPas du tout. Enfin, un peu. Non, pas du tout.",
            "Tu préfères le matin ou le soir ?\nJe préfère quand il ne se passe rien de grave. Ça arrive à toute heure.",
            "C'est bizarre, j'ai l'impression de te connaître.\nMoi aussi. On s'est peut-être {p:croisés|croisées} en rêve.",
            "Tu ris toujours comme ça ?\nSeulement quand c'est drôle. Donc oui : tu es drôle.",
            "Je peux te poser une question bizarre ?\nTu viens d'en poser une. Mais vas-y, pose l'autre.",
            "Pardon, je parle beaucoup.\nNon, continue. Moi, je parle peu. À nous deux, ça fait une moyenne.",
            "Il faudrait qu'on refasse ça.\nOui. Pas trop tard. Sinon, il faudra tout recommencer depuis « bonjour ».",
            "Tu aimes les nuages ?\nJe les trouve corrects. Surtout celui-là, qui a l'air de s'ennuyer.",
        },
        [VisitMood.Friends] = new[]
        {
            "Tu crois que les escargots se font des amis ?\nForcément. Ils ont tout leur temps pour ça.",
            "J'ai rêvé de toi, cette nuit.\nJ'espère que j'étais drôle, au moins.",
            "Tu as de la boue sur le nez.\nJe sais. Je la garde pour plus tard.",
            "Si tu étais un fruit, tu serais lequel ?\nUne cerise. Ça va toujours par deux.",
            "On devrait faire ça plus souvent.\nOn le fait déjà tout le temps. Plus souvent, ce serait tout le temps et demi.",
            "Tu m'as manqué. Un peu.\nToi aussi. Un peu beaucoup.",
            "Je te dois un caillou, non ?\nTu m'en dois trois. Mais je ne tiens pas les comptes. Enfin, si. Trois.",
            "Pourquoi tu souris ?\nJe ne sais pas. C'est toi qui as commencé.",
            "Tu as triché, là, non ?\nUn tout petit peu. Pour que ce soit plus drôle.",
            "C'est quoi, ton secret, pour toujours avoir faim ?\nL'entraînement. Beaucoup d'entraînement.",
            "Tu crois qu'on sera encore {p:amis|amies} dans cent ans ?\nDans cent ans, on sera deux vieux cailloux qui rigolent au soleil.",
            "Tu gardes le secret ?\nQuel secret ? … Voilà. Déjà gardé.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "Tu te souviens de la fois où on s'est {p:perdus|perdues} ?\nOn ne s'était pas {p:perdus|perdues}. On explorait. Longtemps.",
            "Tu sais ce que je pense ?\nOui. Et je suis d'accord, même si c'est bête.",
            "J'ai gardé ton caillou préféré.\nTu es la seule personne qui sait lequel c'est.",
            "Si un jour je disparais…\nJe te retrouve. Je sais toujours où tu te caches.",
            "Tu as encore pleuré devant une feuille qui tombe ?\n[sad] Elle était très belle, cette feuille.",
            "Je te dois quelque chose.\nUn câlin. Pas de discussion.",
            "Ça reste entre nous ?\nEntre nous, et cet écureuil qui écoute depuis tout à l'heure. Il est de confiance.",
            "Rappelle-moi pourquoi on est {p:amis|amies}.\nParce que personne d'autre ne nous supporterait.",
            "Tu me connais par cœur, hein ?\nPar cœur. Même les passages que tu voudrais qu'on oublie.",
            "On sera encore comme ça, plus tard ?\nPlus tard, on sera pires. C'est promis.",
            "Tu boudes ?\n[angry] Je réfléchis. Fort. Avec la bouche.",
            "Merci.\nPour quoi ? … Ah. Pour tout. Alors de rien. Pour tout.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Tu es {b:le plus mignon|la plus mignonne} de tout le coin.\nArrête, tu me fais rougir… Non, continue.",
            "On pourrait rester là pour toujours.\nD'accord. Mais après le goûter.",
            "Tu as pensé à moi, aujourd'hui ?\nUne fois. Longtemps.",
            "Tu rougis.\nC'est le soleil. Enfin, c'est toi. Mais un peu le soleil aussi.",
            "Tu penses à quoi ?\nÀ toi. Puis à mon goûter. Puis encore à toi.",
            "[sad] Je suis {a:désolé|désolée}, je suis toujours en retard.\nJe t'aurais {a:attendu|attendue} toute la journée. D'ailleurs, je t'ai {a:attendu|attendue} toute la journée.",
            "Tu me manques déjà.\nJe suis juste à côté de toi. … Toi aussi, tu me manques déjà.",
            "Tu as encore oublié ton écharpe.\nC'est pour que tu me la rapportes.",
            "Chut, écoute.\nJe n'entends rien. Juste ton cœur. Il est bavard.",
            "Je t'aime bien.\nJe t'aime bien aussi. Beaucoup plus que bien. Oublie ce que je viens de dire. Non, ne l'oublie pas.",
            "Tu crois qu'on a l'air bêtes ?\nTrès. C'est la meilleure partie.",
            "Tu as quelque chose sur la joue.\nOù ça ? … Tu mens. Tu voulais juste me toucher la joue.",
        },
        [VisitMood.Rivals] = new[]
        {
            "J'ai gagné, admets-le.\nJamais. Même dans cent ans.",
            "La prochaine fois, je t'écrase.\nTu dis ça à chaque fois. C'est presque attendrissant.",
            "Tu t'es {b:entraîné|entraînée} en cachette ?\nMoi ? Jamais. … Un peu. Tous les jours.",
            "Tu as triché.\nJe n'ai pas triché. J'ai optimisé.",
            "Je m'entraîne depuis trois jours.\nDeux jours de trop. Il en suffisait d'un pour me battre. Et tu ne l'as pas trouvé.",
            "Tu trembles ?\nC'est de froid. Ou d'impatience. Ou de froid.",
            "On parie un caillou ?\nUn caillou ? Je t'en gagne trois, et je te rends le plus moche.",
            "Tu es {b:un adversaire redoutable|une adversaire redoutable}.\nTu es {a:le seul|la seule} qui m'oblige à me dépasser. Ne t'en vante pas.",
            "Je te laisse gagner, cette fois.\nTu ne me laisses jamais rien. C'est ce que je préfère chez toi.",
            "Tu as peur de perdre ?\nJe n'ai pas peur. Je ne perds pas. C'est différent.",
            "Même heure, même endroit, la prochaine fois ?\nJe serai là. Avec un plan. Et un plan de secours.",
            "Avoue que tu t'amuses.\nJ'avoue que je m'amuserai davantage quand j'aurai gagné.",
        },
        [VisitMood.Conflict] = new[]
        {
            "C'était mon gland.\nTon nom n'était pas écrit dessus.",
            "Tu parles trop fort.\nEt toi, tu boudes trop fort.",
            "C'est toi qui as commencé.\nNon, c'est toi. Depuis le début. Depuis avant le début.",
            "Tu fais toujours la tête.\nJe ne fais pas la tête. C'est mon visage.",
            "Tu m'écoutes ?\nJe fais semblant. Ça compte ?",
            "Excuse-toi.\nAprès toi. Prends ton temps. Prends des années.",
            "On peut parler ?\nNon. Mais tu peux crier. Ça, tu sais faire.",
            "Tu pourrais faire un effort.\nJe l'ai fait. Tu n'as pas vu. Comme d'habitude.",
            "Je ne suis pas en colère.\nÇa se voit tellement pas.",
            "C'était pas drôle.\nJe ne riais pas de toi. Je riais près de toi. Très près.",
            "Je rentre chez moi.\nBonne idée. La meilleure de la journée.",
            "Tu as changé.\nOui. J'ai arrêté de t'écouter. Ça me va très bien.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Departures = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "{Ils} se quittent poliment, et {p:chacun|chacune} se retourne une fois. Pas en même temps. La prochaine fois, peut-être.",
            "{A} repart en se disant que {B} est quelqu'un de bien. De son côté, {B} pense exactement la même chose, avec les mêmes mots.",
            "{Ils} se serrent la main un peu trop longtemps, puis un peu trop vite, pour rattraper.",
            "« Au plaisir ! » lance {A}. « Oui, au plaisir », répond {B}. {Ils} ne savent pas bien ce que ça veut dire, mais ça sonne bien.",
            "{B} glisse un petit biscuit dans la main de {A}, pour la route. C'est modeste. C'est un début.",
            "{A} remercie {B} trois fois, puis une quatrième, par sécurité.",
            "{A} repart en se promettant d'oser raconter une blague, la prochaine fois. {a:Il|Elle} en a déjà choisi une. Elle n'est pas très bonne.",
            "{Ils} se saluent de loin, deux fois, parce que la première n'était pas assez claire.",
            "{B} regarde {A} s'éloigner et se rend compte, un peu {b:surpris|surprise}, que c'était un bon moment.",
            "{A} s'en va avec un petit sourire discret. {B} garde le même un moment, sans trop savoir quoi en faire.",
            "« À un de ces jours ! » Et, pour une fois, la formule a l'air sincère.",
            "{A} rentre en repassant la conversation dans sa tête, et en corrigeant les phrases ratées. Il y en a peu. C'est encourageant.",
            "{Ils} se disent au revoir, puis marchent dans la même direction : il reste un petit bout de chemin commun. C'est gênant. C'est joli.",
            "{B} fait un petit signe. {A} répond d'un petit signe. Au loin, un oiseau s'envole, comme pour faire un petit signe aussi.",
            "{A} repart un peu plus {a:léger|légère} qu'à l'aller, sans savoir dire pourquoi. {B} saurait. {B} ne dira rien.",
            "« Merci d'être {a:venu|venue}. » « Merci de m'avoir {a:reçu|reçue}. » Les formules sont un peu raides. Les sourires, pas du tout.",
        },
        [VisitMood.Friends] = new[]
        {
            "{A} s'en va à reculons pour faire durer les au revoir, et manque de tomber dans un buisson. {B} rit jusqu'à ce qu'{a:il|elle} disparaisse.",
            "{B} glisse une noisette dans la poche de {A} sans rien dire. {A} la trouvera plus tard, et saura tout de suite d'où elle vient.",
            "{A} part en courant, revient chercher son écharpe, repart, revient dire une dernière chose, et repart pour de bon. Presque.",
            "{Ils} se séparent au croisement, et continuent de se parler en criant, jusqu'à ce que les mots ne soient plus que du bruit.",
            "{A} s'éloigne en sifflotant un air que {B} ne connaît pas. Plus tard, {B} se surprend à le siffloter aussi.",
            "{B} raccompagne {A} « juste jusqu'au grand arbre ». Puis jusqu'au suivant. Au troisième, {ils} se disent que c'est ridicule, et continuent.",
            "{A} repart avec un caillou que {B} lui a donné « pour rien ». Les cailloux donnés pour rien sont ceux qu'on garde le plus longtemps.",
            "« À la prochaine ! » « À la prochaine prochaine ! » « À la… » {Ils} pourraient continuer longtemps. {Ils} continuent un peu.",
            "{A} se retourne une dernière fois pour faire une grimace. {B} répond par une grimace pire. Match nul : on rejouera.",
            "{B} regarde {A} partir en se demandant pourquoi les bonnes journées passent toujours deux fois plus vite que les autres.",
            "{A} part en sautillant. Au bout du chemin, {a:il|elle} saute un peu moins haut : la visite est finie, et ça se sent jusque dans les pieds.",
            "{Ils} se promettent de se revoir bientôt. Ce n'est pas une formule : {ils} ont déjà choisi le jour.",
            "{A} laisse derrière {a:lui|elle} une plume, une miette et un fou rire. {B} range la plume, et garde le reste.",
            "{B} fait de grands signes jusqu'à ce que {A} ne soit plus qu'un point. Puis encore un peu, au cas où.",
            "[sad] {A} repart un peu plus lentement qu'à l'aller. Ce n'est pas la fatigue. C'est qu'il faut partir.",
            "{A} et {B} se disent au revoir comme on se lance une balle : vite, fort, et en espérant que l'autre la renverra.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "{Ils} se disent au revoir trois fois, puis une quatrième, parce que la troisième ne comptait pas.",
            "{A} repart avec un bracelet d'herbes tressé par {B}. {Ils} ont le même. Personne d'autre n'en aura jamais.",
            "{Ils} se quittent sans un mot. Pas besoin : tout a été dit, et le reste s'entend.",
            "{A} repart avec le reste du goûter, que {B} a emballé dans une grande feuille en faisant semblant de ne pas y avoir pensé.",
            "{A} fait trois pas, revient serrer {B} dans ses bras, repart. Puis recommence, pour vérifier que ça marche toujours.",
            "{Ils} échangent leur signal secret à distance. Ça marche même de très loin. Surtout de très loin.",
            "{A} s'en va avec un caillou de plus dans la poche et un poids de moins sur le cœur. {B} aussi, dans l'autre sens.",
            "{B} raccompagne {A} jusqu'au bout du chemin. Puis {A} raccompagne {B}. Puis c'est l'heure de rentrer pour de bon, et {ils} n'ont pas avancé.",
            "{A} laisse un petit mot caché pour {B}. {B} en a caché un aussi. {Ils} les trouveront plus tard, et riront au même moment, {p:chacun|chacune} chez soi.",
            "{A} et {B} se serrent fort, très fort, puis encore un peu. Ça devrait tenir jusqu'à la prochaine fois.",
            "{A} part avec la moitié d'un secret. {B} garde l'autre moitié, bien au chaud.",
            "« À tout de suite. » « À tout de suite. » {Ils} savent bien que ce sera demain, ou après-demain. Ça reste « tout de suite ».",
            "{B} regarde {A} s'éloigner, et pense déjà à tout ce qu'{b:il|elle} oubliera de lui dire la prochaine fois.",
            "{A} se retourne au tournant. {B} est toujours là. {B} ne rentre jamais avant le tournant.",
            "{Ils} se font la grimace d'au revoir, la plus laide de toutes. C'est un honneur de la recevoir.",
            "[sad] {A} repart un peu trop vite, pour ne pas faire durer l'au revoir. {B} le sait. {B} fait pareil.",
        },
        [VisitMood.Lovers] = new[]
        {
            "{A} repart sur un petit nuage. {B} reste un long moment sur place, à regarder l'endroit où était le nuage. 💞",
            "« À bientôt », murmure {A}. « Très bientôt », répond {B}, sans lâcher sa main. Il faut bien finir par la lâcher. Ça prend du temps. 💕",
            "{A} s'éloigne à reculons pour regarder {B} le plus longtemps possible, et trébuche un peu. {B} fait semblant de n'avoir rien vu. 💕",
            "{B} regarde {A} partir, puis se rend compte qu'{b:il|elle} sourit toujours, {b:tout seul|toute seule}, {b:comme un idiot|comme une idiote}. {b:Un idiot|Une idiote} très {b:heureux|heureuse}.",
            "{A} rentre le cœur léger, un peu en retard, et sourit sans raison tout le long du chemin. Enfin, avec une raison. Une seule. 💞",
            "[B:sad] {B} murmure « déjà ? » quand {A} se lève. {A} se rassoit un petit moment. Puis encore un. 💕",
            "{A} repart avec la fleur que {B} lui a offerte, en la tenant comme on tient quelque chose de fragile. C'est quelque chose de fragile.",
            "{Ils} se disent au revoir trois fois. Chaque fois, c'est un peu plus difficile, et un peu plus doux. 💞",
            "{A} s'en va en chantonnant la chanson préférée de {B}. {B} l'entend jusqu'au bout du chemin. Personne d'autre ne l'entendrait d'aussi loin.",
            "{B} garde la main levée bien après que {A} a disparu. Au cas où {A} se retournerait. {A} s'est {a:retourné|retournée}. 💕",
            "{Ils} se promettent de se revoir très vite, et de ne pas faire semblant de ne pas s'être manqué.",
            "{A} part en emportant sans le savoir un peu du parfum de {B}. {a:Il|Elle} le sentira encore ce soir, et sourira.",
            "Au moment de se quitter, {A} dit quelque chose tout bas. {B} n'a pas bien entendu. {B} a très bien compris. 💞",
            "{A} s'éloigne. Deux pas, trois pas, puis {a:il|elle} revient en courant pour un dernier câlin, parce que le précédent était trop court.",
            "[sad] Se quitter, c'est toujours un peu triste, même pour quelques jours. Surtout pour quelques jours. 💕",
            "{B} regarde partir {A} et pense : c'est bien. C'est bien, comme ça. C'est très bien. 💞",
        },
        [VisitMood.Rivals] = new[]
        {
            "{A} repart en jurant de revenir plus {a:fort|forte}. {B} a déjà hâte, et le cache très mal.",
            "« Match retour la prochaine fois ! » lance {B}. {A} ricane en s'éloignant, déjà en train d'y réfléchir.",
            "{Ils} se serrent la main d'un air très sérieux. {p:Chacun|Chacune} est {p:persuadé|persuadée} d'avoir gagné, et {p:chacun|chacune} a raison, d'une certaine façon.",
            "{B} regarde {A} s'éloigner en marmonnant : « Pas mal. » Puis, plus bas : « Pas mal du tout. » Puis, plus bas encore : « Je vais {a:le|la} battre. »",
            "{A} et {B} se serrent la main un peu trop fort, et font semblant que ça ne fait pas mal. Ça fait mal. Aux deux.",
            "{B} note le score du jour dans son carnet. {A} le note différemment dans le sien. Les deux carnets sont formels.",
            "« Profite de ta victoire, elle ne durera pas ! » lance {A} en partant. {B} en profite. Largement. Bruyamment.",
            "{A} et {B} se tournent le dos au même moment, avec beaucoup de dignité. Puis se jettent un dernier coup d'œil, au même moment aussi.",
            "{B} regarde {A} partir et sourit en coin. Sans {a:lui|elle}, les journées seraient bien plus calmes. Et bien plus ennuyeuses.",
            "Avant de partir, {A} lance un dernier défi. {B} l'accepte, même si personne n'a bien compris en quoi il consistait.",
            "{A} s'éloigne en s'étirant déjà pour la prochaine fois. {B} fait la même chose de son côté, en cachette. Pas si en cachette.",
            "« À la prochaine défaite », dit {A}. « La tienne ? » demande {B}. {A} ne répond pas. C'est une réponse.",
            "{Ils} partent {p:chacun|chacune} de son côté, et {p:chacun|chacune} court un peu, une fois hors de vue. Pour s'entraîner. Sans le dire.",
            "{B} range le carnet de scores avec un soin exagéré. La page du jour est cornée. C'est une page importante.",
            "{A} part la tête haute. {B} reste la tête haute. Un moment, il n'y a plus que deux têtes hautes, et un peu de vent.",
            "{A} se retourne une dernière fois : « Au fait… c'était bien. » Puis s'enfuit avant que {B} puisse répondre. {B} répond quand même, à personne : « Oui. »",
        },
        [VisitMood.Conflict] = new[]
        {
            "{A} repart sans se retourner. {B} fait semblant de ne pas regarder, et regarde jusqu'au bout.",
            "[sad] {A} rentre en boudant. Ce n'était pas une bonne journée. Ce n'était même pas une journée moyenne.",
            "« Je ne reviendrai plus ! » lance {A}. « C'est ça. À demain », répond {B}.",
            "{Ils} se disent au revoir en même temps, sur le même ton glacial. Une coordination parfaite, gâchée.",
            "[sad] Juste avant de partir, {A} hésite… puis change d'avis. {B} soupire, {b:soulagé|soulagée} et {b:déçu|déçue} à la fois.",
            "{A} et {B} partent {p:chacun|chacune} de son côté, et prennent pourtant le même chemin. Dix minutes de silence, côte à côte. Record battu.",
            "Personne ne dit au revoir. Personne ne dit rien. Quelque part, une mouche ricane.",
            "{A} part sans dire au revoir. {B} répond quand même, très fort, à personne.",
            "{Ils} se quittent sur un « on verra » qui ne promet rien de bon, ni de mauvais. Juste rien.",
            "{A} s'éloigne en marmonnant. {B} marmonne aussi. {Ils} marmonnent la même chose, sans le savoir.",
            "{B} tourne les talons, revient pour avoir le dernier mot, puis tourne à nouveau les talons. Le dernier mot était « pff ».",
            "[sad] {A} part en boudant, s'arrête, hésite à revenir s'excuser… et repart en boudant. C'était presque.",
            "[sad] {Ils} se séparent {p:fâchés|fâchées}. Le soir, {p:chacun|chacune} trouve enfin la bonne réplique. Trop tard.",
            "{A} s'en va en claquant des talons, faute de porte à claquer.",
            "{B} regarde {A} partir, et se dit qu'{b:il|elle} ne {a:le|la} regrettera pas. {b:Il|Elle} se le redit, pour être {b:sûr|sûre}.",
            "{A} lance une dernière pique en partant. {B} la rattrape au vol et la renvoie. {A} est déjà trop loin. Victoire pour personne.",
        },
    };

    // The typed-passion moments (see Build): {S} is the Plynling whose typed passion it is, {L} the other,
    // {P} the typed text in « guillemets ». Like every {P} line, the text never follows « de », « à »,
    // « du » or « au », nothing refers back to it by pronoun, and it never opens a sentence (it is
    // lowercase). Keyed by flavour: the arrival uses the bond before the visit, the rest the mood.
    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedArrivals = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "{S} arrive en fredonnant une chanson dont les paroles, en entier, sont : {P}.",
            "{S} arrive avec un dessin roulé sous le bras. Titre : {P}. {L} n'ose pas demander ce que c'est. {L} demande quand même.",
            "Premier mot de {S} en arrivant, avant même bonjour : {P}. Le bonjour vient après, un peu en retard.",
            "{S} a écrit sur son bras, à l'encre, en lettres appliquées : {P}. « C'est pour ne pas oublier. » Personne n'aurait oublié.",
            "{L} voit arriver {S} de loin, avec une pancarte. Sur la pancarte : {P}. C'est tout ce qu'il y a sur la pancarte.",
            "{S} arrive les yeux brillants : {s:il|elle} a rêvé de sa passion toute la nuit. Sa passion, c'est {P}. Le rêve était en couleurs.",
            "{S} arrive en retard. Son excuse : {P}. {L} ne pose pas de questions. Avec {S}, l'excuse est toujours la même.",
            "« Tu ne devineras jamais à quoi j'ai pensé en venant », lance {S}. {L} devine du premier coup : {P}.",
            "{S} arrive avec un cadeau pour {L}, emballé dans un papier où {s:il|elle} a écrit partout, en tout petit : {P}.",
            "{S} arrive, s'arrête, et annonce solennellement, sans que personne n'ait rien demandé : {P}. Puis dit bonjour.",
            "{L} n'a pas encore dit bonjour que {S} en est déjà au troisième point de son exposé. Sujet : {P}.",
            "{S} a brodé un petit écusson pour l'occasion. Dessus, un peu de travers : {P}.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "{S} arrive avec un badge bien en vue : {P}. C'est une provocation. {L} le prend comme une provocation.",
            "{S} arrive en marmonnant quelque chose. {L} tend l'oreille. C'est encore sa passion : {P}.",
            "« Je ne suis pas {s:venu|venue} pour toi, mais pour ma passion : {P}. » {L} lève les yeux au ciel, très haut.",
            "{S} arrive, jette un regard à {L}, et écrit dans la poussière, en grosses lettres : {P}. Comme on plante un drapeau.",
            "{L} attendait {S} de pied ferme. {S} arrive, et commence par un avertissement : aujourd'hui, pas un mot contre sa passion. Pour mémoire : {P}.",
            "{S} arrive en sifflotant l'hymne qu'{s:il|elle} a inventé pour sa passion. Titre : {P}. {L} sifflote plus fort, autre chose.",
            "{S} arrive avec une pile de feuilles, qui portent toutes le même titre : {P}. {L} sent venir un très long moment.",
            "« Tiens. Toi. » « Tiens. Toi. » Puis {S} ajoute, parce qu'il faut toujours qu'{s:il|elle} ajoute quelque chose : {P}.",
            "{S} arrive avec une écharpe brodée. Dessus : {P}. {L} n'a rien contre l'écharpe. {L} a tout contre {S}.",
            "{S} arrive et pose ses conditions. Première condition : {P}. Il n'y en a pas d'autre.",
        },
    };

    // A typed exchange whose first line (the visitor's) belongs to the passion's owner…
    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedExchangesOwnerFirst = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "Tu sais à quoi je pense ?\nLaisse-moi deviner : {P}.",
            "J'ai rêvé de ma passion, cette nuit : {P}, en couleurs.\nEt j'étais dans le rêve ?",
            "Si je devais choisir entre toi et {P}…\nNe finis pas cette phrase. Je préfère ne pas savoir.",
            "Tu veux que je t'explique {P} ?\nD'accord. Mais cette fois, on s'arrête avant la nuit.",
            "Un jour, j'écrirai un livre sur ma passion : {P}.\nJe le lirai. Même les passages ennuyeux. Surtout les passages ennuyeux.",
            "J'ai appris un nouveau truc sur {P}.\nRaconte. J'ai tout mon temps, et un goûter.",
            "Ça te dérange, si je reparle de ma passion ?\nTu veux dire : {P} ? Vas-y. Je m'assois.",
            "Tu me promets de ne jamais te moquer de ma passion ?\nJamais. C'est sacré, {P}.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "Au moins, moi, j'ai une vraie passion : {P}.\nEt moi, une patience infinie. Sinon, je t'aurais {s:quitté|quittée} depuis longtemps.",
            "Tu n'as jamais rien compris à ma passion.\nJ'ai très bien compris : {P}. C'est bien ça, le problème.",
            "Ma passion ne t'a rien fait, tu sais.\nNon. C'est toi qui m'en parles depuis une heure.",
            "Je parie que tu ne sais même pas ce que c'est, {P}.\nJe sais. Tu me l'as expliqué quatorze fois.",
            "Un jour, tout le village aimera {P}.\nEt ce jour-là, je déménage.",
            "Ris, si tu veux. Moi, au moins, j'ai {P}.\nEt moi, j'avais ma tranquillité. Avant que tu arrives.",
        },
    };

    // …and one whose second line (the host's) belongs to the owner.
    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedExchangesOwnerSecond = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "Tu penses encore à ta passion, là ?\nÀ quoi d'autre ? C'est toute ma vie, {P}.",
            "Tu as l'air ailleurs.\nJe suis ailleurs. Je suis quelque part avec {P}.",
            "Raconte-moi encore ta passion.\nEncore ? Bon. Tout a commencé avec {P}…",
            "Tu m'apprendrais ta passion ?\nTu veux vraiment apprendre {P} ? Alors dès demain.",
            "Qu'est-ce que tu fais, quand tu es triste ?\nJe pense à ma passion : {P}. Ça marche presque toujours.",
            "Tu as un secret ?\nUn seul, et tu le connais déjà : {P}.",
            "Si tu devais emporter une seule chose sur une île déserte ?\nToi. Et {P}, si ça rentre dans le sac.",
            "Je peux te poser une question sur {P} ?\nTu viens de me rendre très {s:heureux|heureuse}.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "Tu vas encore nous parler de ta passion ?\nOui. Ça s'appelle {P}, et tu vas écouter.",
            "Tu ne penses vraiment qu'à ça ?\nNon. Parfois, je pense aussi à te battre. Mais surtout, oui : {P}.",
            "Ta passion est ridicule.\nTa jalousie aussi. Et {P} se porte très bien, merci.",
            "Je ne veux plus entendre parler de ta passion.\nTrop tard. Je l'ai brodée sur ton coussin : {P}.",
            "Pourquoi tu souris ?\nJe pensais à ma passion : {P}. Et à ta tête, tout à l'heure.",
            "Tu as encore écrit ta passion quelque part chez moi ?\nSur ta porte. En rouge : {P}.",
        },
    };

    public static readonly IReadOnlyDictionary<ConvoFlavor, string[]> TypedDepartures = new Dictionary<ConvoFlavor, string[]>
    {
        [ConvoFlavor.Friendly] = new[]
        {
            "{S} repart en répétant tout bas : {P}, {P}, {P}. {L} ne sait pas si c'est une chanson ou une prière.",
            "Au moment de partir, {S} glisse un petit papier dans la main de {L}. Dessus : {P}. Rien d'autre. {L} le garde quand même.",
            "{S} s'éloigne en racontant sa passion à qui veut l'entendre. Il n'y a personne. {S} continue quand même : {P}.",
            "« Tu penseras à moi ? » demande {L}. « Chaque fois que je penserai à ma passion », répond {S}. Ça fait souvent : sa passion, c'est {P}.",
            "{L} regarde {S} partir. Au loin, on l'entend encore dire : {P}.",
            "{S} laisse derrière {s:lui|elle} une petite affiche, au bord du chemin : {P}. {L} ne l'enlève pas. Ça fait joli.",
            "{S} part en courant, avec une idée toute neuve pour sa passion : {P}, en mieux.",
            "Avant de partir, {S} fait promettre à {L} d'essayer au moins une fois : {P}. {L} promet. {L} tiendra peut-être.",
            "{S} s'en va en dessinant dans l'air des formes que {L} ne comprend pas. Ça a sûrement un rapport avec {P}.",
            "Dernier mot de {S}, de loin, les mains en porte-voix : {P} ! {L} fait un signe. C'est devenu leur au revoir.",
        },
        [ConvoFlavor.Tense] = new[]
        {
            "{S} s'en va en criant, pour que tout le quartier l'entende : {P} ! {L} se bouche les oreilles.",
            "{S} repart en laissant derrière {s:lui|elle}, bien en évidence, une brochure sur sa passion : {P}. {L} s'en sert pour s'éventer.",
            "Juste avant de partir, {S} lance : « Un jour, tu comprendras ma passion. » {L} répond : « Un jour, tu comprendras les silences. »",
            "{S} part sans dire au revoir, mais en fredonnant sa passion : {P}. C'est pire.",
            "{L} regarde {S} s'éloigner, et se surprend à murmurer : {P}. Horreur. C'est contagieux.",
            "{S} griffonne sa passion sur le premier caillou venu, et le laisse derrière {s:lui|elle} : {P}. {L} le retourne, face contre terre.",
            "« Je reviendrai avec des preuves », annonce {S}. Des preuves pour sa passion : {P}. {L} a déjà hâte de ne pas les lire.",
            "{Ils} se séparent sur un désaccord total. Sur quoi ? Sur {P}, évidemment.",
        },
    };

    // The parting after a refused confession: the visitor ({A}) confessed, the host ({B}) said no.
    // The outcome line follows and the faces are sad whatever the line says (see DeparturePool).
    public static readonly string[] RefusedDepartures =
    {
        "{B} a répondu avec beaucoup de douceur. C'est presque pire. {A} repart en regardant ses pieds.",
        "{A} se répète « ce n'est rien » tout le long du chemin. Ça ne marche pas encore.",
        "{Ils} se disent au revoir un peu trop poliment, comme deux personnes qui ne savent plus quoi faire de leurs mains.",
        "{B} regarde {A} partir, le cœur serré {b:lui aussi|elle aussi}. Dire non ne fait de bien à personne.",
        "{A} repart en serrant, au fond de sa poche, le caillou qu'{a:il|elle} voulait offrir. Il pèse bien plus lourd qu'à l'aller.",
        "« On reste {p:amis|amies} ? » demande {B}. {A} hoche la tête. Il faudra un peu de temps. Mais oui.",
        "Sur le chemin, {A} donne un coup de pied dans un gland, puis s'excuse auprès du gland. Ce n'était pas sa faute non plus.",
        "{B} voudrait dire quelque chose de gentil. Tout ce qui lui vient est trop petit. {b:Il|Elle} se tait, et c'est mieux.",
        "{A} s'éloigne très droit, très digne. C'est au premier tournant qu'{a:il|elle} s'autorise à renifler.",
        "Autour, le monde continue comme si de rien n'était, ce qui est un peu vexant. {A} rentre quand même, un pas après l'autre.",
    };

    // The parting after a break-up: the two were a couple when the visit began and are not any more.
    public static readonly string[] BreakUpDepartures =
    {
        "{Ils} se séparent sans se disputer. C'est ce qui rend la chose si triste : il n'y a personne à qui en vouloir.",
        "{A} rend à {B} la fleur séchée qu'{a:il|elle} gardait sur {a:lui|elle}. {B} la lui redonne. Certaines choses peuvent rester.",
        "{Ils} font une partie du chemin ensemble, une dernière fois, sans se tenir la main. C'est étrange, des mains vides.",
        "« Tu vas me manquer. » « Toi aussi. » C'est vrai des deux côtés, et ça ne change rien. C'est bien ça, le plus dur.",
        "{B} regarde {A} s'éloigner, et se surprend à attendre qu'{a:il|elle} se retourne. {A} se retourne. Un tout petit signe. C'est tout.",
        "{A} et {B} se quittent comme on referme un livre qu'on a aimé : doucement, en gardant un doigt entre les pages.",
        "{Ils} décident de rester {p:amis|amies}. {Ils} le disent en même temps, pour se donner du courage.",
        "Le caillou en forme de cœur reste là où {ils} l'ont posé. Personne ne sait plus très bien à qui il appartient.",
        "{A} rentre par le chemin le plus long. Il faut du temps pour réapprendre à rentrer {a:seul|seule}.",
        "Plus tard, chez {b:lui|elle}, {B} sort deux tasses par habitude. {b:Il|Elle} en range une, très lentement.",
    };

    // Beat 3: the listener reacts to the subject, then the speaker answers — one \n between them.
    // Keyed by mood and by whether the listener shares the passion. Generic, so {P} stands alone
    // (after a colon, « pour », « sur », or as a subject — never after « de » or « à »).
    // Step 6: the talk winds down and turns into doing something. {S} is whoever says it here.
    public static readonly IReadOnlyDictionary<VisitMood, string[]> Closers = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "Bon… et si on essayait quelque chose, pour voir ?",
            "On pourrait… faire un truc ? Enfin, si tu veux. Tu n'es pas {l:obligé|obligée}.",
            "Je crois qu'on a épuisé la conversation. On passe à l'action ? Doucement ?",
            "J'ai une petite idée. Elle est petite, mais elle est à nous.",
            "Tu veux essayer ? Si c'est raté, on dira que c'était exprès.",
            "Viens. Enfin, si tu veux venir. Je veux dire : viens.",
            "On fait quelque chose ensemble ? Il paraît que ça aide, pour se connaître.",
            "Bon. Je propose qu'on arrête de parler avant de dire quelque chose de bizarre.",
        },
        [VisitMood.Friends] = new[]
        {
            "Bon, assez parlé. Viens, j'ai une idée. Elle est presque bonne.",
            "Tu sais ce qui serait encore mieux que d'en parler ? Le faire. Tout de suite.",
            "Le dernier arrivé range tout, après !",
            "Allez, viens. On verra bien ce que ça donne : c'est toujours le meilleur moment.",
            "J'ai une idée. Ne demande pas. Suis-moi.",
            "On arrête de parler, sinon on va encore oublier de s'amuser. Viens !",
            "Tope là. On essaie, et si on rate, on aura au moins bien ri.",
            "Et si on arrêtait d'en parler pour commencer, plutôt ?",
        },
        [VisitMood.BestFriends] = new[]
        {
            "Tu sais ce qu'on va faire ? La même chose que d'habitude. En mieux.",
            "Viens. J'ai gardé le meilleur pour toi.",
            "Arrête, je sais déjà ce que tu vas dire. Viens plutôt.",
            "J'ai eu une idée en t'écoutant. Tu vas adorer. Ou détester. Les deux, sûrement.",
            "Allez, on y va. Comme toujours : à deux, et sans plan.",
            "Tu te souviens de ce qu'on s'était promis de faire un jour ? C'est aujourd'hui.",
            "Plus un mot. Suis-moi, comme la dernière fois. Mais cette fois, on revient.",
            "On en reparlera plus tard. Là, on a mieux à faire.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Viens. N'importe où, du moment que c'est avec toi.",
            "Donne-moi la main. On y va.",
            "J'ai une surprise. Ferme les yeux. Non, ouvre-les, sinon tu vas tomber. 💕",
            "On fait quelque chose de joli ensemble ? 💞",
            "Assez parlé. Je veux juste être avec toi. Et faire quelque chose, aussi. Mais surtout être avec toi.",
            "Tu viens ? J'ai une idée, et elle a besoin de nous deux.",
            "Allez, avant que je dise encore quelque chose de gênant. Viens. 💕",
            "Suis-moi. Promis, cette fois, je sais où on va. Presque.",
        },
        [VisitMood.Rivals] = new[]
        {
            "Assez parlé. Prouve-le.",
            "On règle ça tout de suite.",
            "Tu veux une démonstration ? Tu vas l'avoir.",
            "Un défi. Maintenant. Pas de discussion, et pas de revanche avant la fin.",
            "Bon. On va voir qui a raison. Indice : moi.",
            "Suis-moi, si tu l'oses. Et tu l'oses, je le sais.",
            "Trois, deux, un… Pourquoi tu ne bouges pas ? C'est parti !",
            "Le perdant range tout. Le gagnant regarde. Je vais regarder.",
        },
        [VisitMood.Conflict] = new[]
        {
            "Bon. On fait quelque chose, ou on reste là à se regarder ?",
            "Faisons quelque chose. Ça nous évitera de parler.",
            "J'ai une idée. Elle ne va pas te plaire. Tant mieux.",
            "On n'est d'accord sur rien, alors faisons quelque chose, pour changer.",
            "Allez. Qu'on en finisse.",
            "Et si on faisait quelque chose, avant que je m'énerve pour de bon ?",
            "Debout. On bouge. Je ne supporte plus ce silence.",
            "Très bien. Puisque c'est comme ça, on va jouer. Et je vais gagner.",
        },
    };

    // Beat 4 for a custom subject. No Conflict: in a conflict the activity is always the squabble.
    public static readonly IReadOnlyDictionary<VisitMood, string[]> CustomActivities = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[] { "{S} montre à {L} deux ou trois choses sur sa passion : {P}. {L} pose des questions très polies, et en pense une sur deux.", "{Ils} essaient ensemble, pour voir. Thème : {P}. C'est hésitant, mais personne ne se moque.", "{S} fait une petite démonstration. Sujet : {P}. {L} applaudit, par politesse d'abord, puis pour de vrai.", "{S} prête ses notes à {L}. Titre en haut de la page : {P}. {L} les lit en entier. Même les ratures.", "{S} explique les bases. Sujet du jour : {P}. {L} hoche la tête très souvent, et retient au moins un mot.", "{Ils} découvrent le sujet ensemble, petit à petit : {P}. Pour une première fois, ce n'est pas si mal.", "{S} dessine un schéma dans la poussière pour expliquer sa passion : {P}. {L} penche la tête pour le voir dans le bon sens.", "{L} pose une question sur la passion de {S} : {P}. La réponse dure longtemps. {L} ne regrette pas d'avoir demandé. Presque pas." },
        [VisitMood.Friends] = new[] { "{Ils} inventent un jeu sur le moment. Thème imposé : {P}. Les règles changent dès le deuxième tour.", "{Ils} fabriquent une affiche géante. En gros, au milieu : {P}. Il y a beaucoup trop de paillettes.", "{S} invente un quiz. Sujet : {P}. {L} répond « caillou » à toutes les questions, et gagne quand même.", "{Ils} passent un long moment sur un seul sujet : {P}. Le temps file sans prévenir.", "{Ils} prennent des poses ridicules sur le thème : {P}. Chaque pose est plus absurde que la précédente.", "{Ils} improvisent une chanson dont le refrain, en entier, est : {P}.", "{Ils} fabriquent un petit objet souvenir. Gravé dessus, de travers : {P}.", "{S} fait visiter à {L} son coin secret, celui de sa passion : {P}. {L} promet de ne le dire à personne, et tiendra parole." },
        [VisitMood.BestFriends] = new[] { "{Ils} fondent un club secret. Thème : {P}. Membres : deux. Mot de passe : secret.", "{Ils} rangent leurs souvenirs dans une boîte. Sur le couvercle : {P}.", "{S} a préparé une surprise pour {L}, sur le thème qu'{s:il|elle} adore : {P}. {L} fait semblant d'être {l:étonné|étonnée}, puis l'est vraiment.", "{Ils} inventent un spectacle entier. Sujet : {P}. Public : un moineau. Il reste jusqu'au bout.", "{Ils} créent un langage secret qui ne sert qu'à parler d'une seule chose. La chose en question : {P}.", "{Ils} tiennent un carnet commun, rien que pour ça : {P}. Il est déjà presque plein.", "{Ils} montent une cabane dont le nom officiel est : {P}. Accès réservé aux deux membres.", "{L} offre à {S} un cadeau fait main, sur le thème : {P}. {S} ne s'en remet pas." },
        [VisitMood.Lovers] = new[] { "{S} fabrique un petit cadeau pour {L}, sur un thème bien précis : {P}. 💕", "{S} écrit un poème pour {L}. Titre : {P}. La dernière rime est « toujours ».", "{Ils} partagent un goûter en parlant d'un seul sujet : {P}. Ou peut-être d'autre chose. Surtout d'autre chose. 💞", "{S} emmène {L} voir quelque chose de spécial. Thème : {P}. {L} ne regarde que {S}.", "{Ils} s'écrivent des petits mots, tous sur le même thème : {P}. Et tous signés d'un cœur.", "{Ils} passent un long moment côte à côte, à rêver ensemble. Le rêve a un thème : {P}. 💞", "{L} a préparé une surprise pour {S}, sur le thème : {P}. {S} en a les yeux qui brillent.", "{L} essaie pour la première fois la passion de {S} : {P}. C'est raté. C'est le plus beau raté que {S} ait jamais vu. 💕" },
        [VisitMood.Rivals] = new[] { "Concours improvisé, un seul sujet : {P}. {Ils} se déclarent {p:tous|toutes} les deux {p:vainqueurs|gagnantes}.", "Duel de connaissances. Thème : {P}. Le score est serré. Il l'est toujours.", "{Ils} rédigent le règlement officiel. Sujet : {P}. Chaque règle a une exception inventée par l'autre.", "{S} lance un défi. Thème : {P}. {L} le relève sans même demander les règles.", "Concours de précision, thème imposé : {P}. {Ils} contestent tous les résultats, y compris les leurs.", "Défi chronométré. Sujet : {P}. Le chronomètre est un escargot. Personne ne sait qui a gagné.", "{S} se proclame {s:champion incontesté|championne incontestée}. Discipline : {P}. {L} conteste immédiatement.", "{Ils} fabriquent un trophée en écorce pour le vainqueur. Discipline : {P}. {Ils} se le disputent encore." },
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
    /// The story of <paramref name="outcome"/>: arrival; a script in which A raises the subject and each line
    /// answers the one before — the opener on its own step, then the rest two lines to a step, the closer
    /// finishing the last pair; the activity; then parting with <paramref name="outcomeLines"/> — what the
    /// visit changed. Four steps plus half the script's lines: five for a two-line script, eight for an eight-line one. Each step carries both faces.
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

        // Tomodachi-style: a typed passion — its owner's own words — comes up once, out of nowhere, in
        // the arrival, the exchange or the parting. In those lines {S} is the Plynling whose passion it
        // is, {L} the other, {P} the typed text.
        var typedOwners = new[] { visitor, host }.Where(x => x.Passions.Any(p => p.Catalog is null)).ToList();
        var typedMoment = typedOwners.Count > 0 && rng.NextDouble() < TypedMomentChance ? (TypedMoment)rng.Next(3) : TypedMoment.None;
        var typedOwner = typedMoment == TypedMoment.None ? null : typedOwners[rng.Next(typedOwners.Count)];
        var typedIsVisitor = ReferenceEquals(typedOwner, visitor);
        string T(string t) => Expand(t, visitor.Name, visitor.Gender, host.Name, host.Gender, typedIsVisitor,
            typedOwner!.Passions.First(p => p.Catalog is null).Render());

        var beats = new List<VisitBeat>();

        // 1. Arrival — from the bond *before* the visit: two acquaintances who become friends during it
        // still arrive as acquaintances, and a scene that will go badly has not gone badly yet.
        var arrivalMood = MoodFor(true, outcome.Before);
        var typedArrival = typedMoment == TypedMoment.Arrival;
        var (arrival, arrivalTag) = Untag(pick(typedArrival ? TypedArrivals[FlavorOf(arrivalMood)] : Arrivals[arrivalMood]));
        var arrivalFace = Faces(arrivalMood).Narration;
        var f = new FacePair(arrivalFace, arrivalFace).Narrate(arrivalTag);
        beats.Add(f.Beat($"*{pick(place.Scenes)}*\n{(typedArrival ? T(arrival) : X(arrival))}"));

        // The conversation: one script whose lines answer each other — the opener (A's narration and
        // words) on its own step, from whichever of them the script says.
        var flavor = FlavorOf(mood);
        var scripts = subject.Catalog is { } passion
            ? PlynlingScripts.For(passion, flavor, shared)
            : PlynlingScripts.ForCustom(flavor, shared);
        var openerKey = pick(scripts.Select(x => x.Lines[0].Text).ToArray());
        var script = scripts.First(x => x.Lines[0].Text == openerKey);

        var opener = script.Lines[0].Text.Split('\n');
        var (narration, narrationTag) = Untag(opener[0]);
        var (said, saidTag) = Untag(opener[1]);
        f = Talking(aIsVisitor).Narrate(narrationTag).Speak(saidTag, aIsVisitor);
        beats.Add(f.Beat($"{C(narration)}\n{Said(a, C(said))}"));

        // Then the rest of the script and the closer, two lines to a step, so both of them talk on
        // it. The closer goes to whoever did not say the script's last line, so the final step is
        // always an exchange; {S} in it is whoever says it. Two lines in a row from the same one
        // share one bubble.
        var spoken = script.Lines.Skip(1).Select(line =>
        {
            var (talk, tag) = Untag(line.Text);
            return (Visitor: line.ByA == aIsVisitor, Text: C(talk), Tag: tag);
        }).ToList();
        var closerIsVisitor = !spoken[^1].Visitor;
        var (closer, closerTag) = Untag(pick(Closers[mood]));
        spoken.Add((closerIsVisitor, By(closer, closerIsVisitor), closerTag));
        foreach (var pair in spoken.Chunk(2))
        {
            f = Talking(pair[0].Visitor);
            var lines = new List<(bool Visitor, string Text)>();
            foreach (var (byVisitor, talk, tag) in pair)
            {
                f.Speak(tag, byVisitor);
                if (lines.Count > 0 && lines[^1].Visitor == byVisitor) lines[^1] = (byVisitor, $"{lines[^1].Text} {talk}");
                else lines.Add((byVisitor, talk));
            }
            beats.Add(f.Beat(string.Join("\n", lines.Select(l => Said(l.Visitor ? visitor : host, l.Text)))));
        }

        // 7. The activity, then a little exchange (visitor, then host). A typed exchange is picked by who
        // owns the passion, since the visitor always speaks first.
        var (activity, activityTag) = Untag(pick(ActivityPool(mood, info, b, rng)));
        var typedExchange = typedMoment == TypedMoment.Exchange;
        var exchange = pick(typedExchange
            ? (typedIsVisitor ? TypedExchangesOwnerFirst : TypedExchangesOwnerSecond)[flavor]
            : Exchanges[mood]).Split('\n');
        string E(string t) => typedExchange ? T(t) : X(t);
        var (first, firstTag) = Untag(exchange[0]);
        var (second, secondTag) = Untag(exchange[1]);
        f = new FacePair(faces.Narration, faces.Narration).Narrate(activityTag).Speak(firstTag, true).Speak(secondTag, false);
        beats.Add(f.Beat($"{C(activity)}\n{Said(visitor, E(first))}\n{Said(host, E(second))}"));

        // 8. Parting. What the visit did sets the faces first; a tag only colours an ordinary parting. A
        // typed parting only replaces an ordinary one — never a refusal's or a break-up's.
        var departurePool = DeparturePool(outcome, mood);
        var typedDeparture = typedMoment == TypedMoment.Departure && ReferenceEquals(departurePool, Departures[mood]);
        var (departure, departureTag) = Untag(pick(typedDeparture ? TypedDepartures[flavor] : departurePool));
        var parting = typedDeparture ? T(departure) : X(departure);
        var ending = OutcomeFace(outcome);
        f = ending is { } end ? new FacePair(end, end) : new FacePair(faces.Narration, faces.Narration).Narrate(departureTag);
        beats.Add(f.Beat(string.IsNullOrWhiteSpace(outcomeLines) ? parting : $"{parting}\n{outcomeLines}"));

        var heading = $"{place.Emoji} {place.Name.Replace("{B}", host.Name)}";
        return new VisitStory("", heading, beats, visitor, host, PlynlingCatalog.Info(outcome.Host.Species).Accent);
    }

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

    // Step 7, in a conflict: always the squabble. Otherwise, MoodActivityShare of the time an activity
    // that fits the bond rather than the subject (Activities[mood]); the rest of the time the subject's
    // own — a catalog passion's activities plus any combo with one of the listener's catalog passions,
    // or the custom ones for this mood. Combos join the passion's pool rather than replacing it: a
    // pair has one combo at most, and on its own it made every Cooking-meets-Music visit the same.
    private static string[] ActivityPool(VisitMood mood, PassionInfo? subject, VisitCast listener, Random rng)
    {
        if (mood == VisitMood.Conflict) return Activities[VisitMood.Conflict];
        if (rng.NextDouble() < MoodActivityShare) return Activities[mood];
        if (subject is null) return CustomActivities[mood];
        var combos = listener.Passions
            .Where(p => p.Catalog is { } lc && lc != subject.Passion)
            .Select(p => PlynlingPassions.ComboFor(subject.Passion, p.Catalog!.Value))
            .OfType<string[]>()
            .SelectMany(x => x);
        return subject.Activities.Concat(combos).ToArray();
    }

    // How often the activity comes from the bond's pool instead of the subject's.
    private const double MoodActivityShare = 0.3;

    // How often a visit with a typed passion on either side quotes it once outside the conversation.
    private const double TypedMomentChance = 0.45;

    private enum TypedMoment { Arrival, Exchange, Departure, None }

    private static ConvoFlavor FlavorOf(VisitMood mood) =>
        mood is VisitMood.Rivals or VisitMood.Conflict ? ConvoFlavor.Tense : ConvoFlavor.Friendly;

    // The parting's pool: the outcome decides it when the visit broke something — a refused
    // confession or a break-up — and the mood otherwise. The mood is the bond *after* the visit, so
    // a couple who just split would otherwise leave on a cheerful Friends line. These are exactly the
    // outcomes OutcomeFace turns sad.
    private static string[] DeparturePool(VisitOutcome o, VisitMood mood) =>
        o.Confession == Confession.Refused ? RefusedDepartures
        : o.Before == PlynlingBond.Lovers && o.After != PlynlingBond.Lovers ? BreakUpDepartures
        : Departures[mood];

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
