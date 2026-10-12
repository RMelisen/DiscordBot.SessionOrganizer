using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

public sealed record PassionInfo(PlynlingPassion Passion, string Emoji, string Label, string[] Keywords,
    string[] Activities);

/// <summary>
/// One passion a Plynling has: a catalog one (rich, hand-written lines) or a custom text taught by
/// its owner (generic templates). Exactly one of the two is set.
/// </summary>
public sealed record Passion(PlynlingPassion? Catalog, string? Custom)
{
    // Two passions are the same when both are the same catalog one, or both custom with equal
    // normalised text ("Les trains !" = "les trains").
    public string Key => Catalog is { } c ? "cat:" + c : "txt:" + PlynlingPassions.Normalize(Custom ?? "");
    public bool SameAs(Passion other) => Key == other.Key;

    /// <summary>For {P} and SYNCS's lines: « la cuisine », or the typed text in « guillemets ».</summary>
    public string Render() => Catalog is { } c ? PlynlingPassions.Info(c).Label : $"« {PlynlingCardUi.SafeName(Custom ?? "")} »";

    /// <summary>For the card: the same as <see cref="Render"/>. The catalog emoji used to lead it;
    /// the owner took it off the card.</summary>
    public string Display() => Render();
}

/// <summary>
/// The passions. Pure: no database, no clock. An innate passion is one of the catalog below; a taught
/// one is free text, upgraded to a catalog passion when <see cref="Resolve"/> recognises it.
/// What a passion says in a visit is in <see cref="PlynlingScripts"/>; here are its keywords, its
/// activities (step 7) and the pair combos. Lines are story templates (see <see cref="PlynlingVisitStory"/>):
/// {S}/{L}, {Ils}/{ils}, {p:m|f}.
/// </summary>
public static class PlynlingPassions
{
    public static readonly TimeSpan TeachCooldown = TimeSpan.FromHours(24);

    // How often a shared passion, when there is one, is what they talk about.
    public const double SharedTopicChance = 0.6;

    public static readonly IReadOnlyList<PassionInfo> All = new PassionInfo[]
    {
        new(PlynlingPassion.Cooking, "🍳", "la cuisine",
            new[] { "cuisine", "cuisiner", "recette", "recettes", "patisserie", "gateau", "gateaux", "crepe", "crepes", "cuisson", "chef" },
            new[] { "🥞 {Ils} se lancent dans des crêpes. La première est ratée, comme le veut la tradition. La deuxième aussi, par solidarité.",
                "🍪 {Ils} font des biscuits en forme de cailloux. Ils ont aussi le goût de cailloux, mais c'est l'intention qui compte.",
                "🍲 {S} prépare une soupe ; {L} goûte à chaque étape, donne des notes très sévères, et redemande quand même.",
                "🥧 Tarte à quatre mains. Il manque la moitié des ingrédients. Elle est délicieuse, et personne ne comprend pourquoi.",
                "🍓 {Ils} font de la confiture de fraises. Il en reste un pot. Il y en avait dix au début.",
                "🥐 {S} tente des croissants. Ils ressemblent à des cailloux. {L} les trouve délicieux, par politesse ou par faim.",
                "🧑‍🍳 Restaurant ouvert pour l'après-midi ! Le seul client est un escargot. Il laisse un pourboire : une feuille.",
                "🍵 {S} prépare une infusion de feuilles mystère. {L} la boit courageusement, puis demande ce que c'était. Mauvaise idée.",
                "🧈 {Ils} battent du beurre à la main pendant une heure. Résultat : des bras en coton et un tout petit beurre. {Ils} en sont très {p:fiers|fières}.",
                "🎂 {Ils} décorent un gâteau. Le gâteau disparaît sous la décoration. On ne le retrouvera qu'en le mangeant." }),
        new(PlynlingPassion.Music, "🎵", "la musique",
            new[] { "musique", "chanson", "chansons", "chanter", "chant", "piano", "guitare", "violon", "flute", "batterie", "melodie" },
            new[] { "🎶 {S} apprend un petit air à {L}, qui le massacre avec un enthousiasme magnifique.",
                "🥁 Batterie en coquilles de noix ! Le voisinage apprécie moyennement. Un pic-vert, lui, bat la mesure.",
                "🎤 Petit concert pour une coccinelle. Elle reste jusqu'à la fin, par politesse, puis s'envole sans applaudir.",
                "🎻 {L} tient le rythme, {S} chante. Puis {ils} échangent, et c'est pire. Bien pire. {Ils} adorent.",
                "🎵 {Ils} écrivent une chanson sur la journée. Le refrain rime avec « caillou ». Tout le reste aussi.",
                "🎹 Xylophone en brindilles de toutes les tailles. Il sonne faux. Magnifiquement faux.",
                "🪕 {S} gratte une ficelle tendue entre deux branches ; {L} invente les paroles au fur et à mesure. Elles parlent surtout de ficelle.",
                "🎺 {Ils} imitent tous les instruments d'un orchestre avec la bouche. Le tuba est très réussi. Le violon, beaucoup moins.",
                "🎧 {Ils} écoutent les bruits autour, les yeux fermés, et en font une chanson. Le soliste est un moineau.",
                "🎼 {Ils} composent une berceuse si réussie qu'{ils} s'endorment avant le deuxième couplet. Il n'y aura pas de deuxième couplet." }),
        new(PlynlingPassion.Gaming, "🎮", "les jeux vidéo",
            new[] { "jeux video", "jeu video", "gaming", "console", "manette", "minecraft" },
            new[] { "🎮 {Ils} inventent un jeu vidéo sans écran : il suffit de crier les actions très fort. « SAUTER ! » Personne ne saute.",
                "🕹️ {Ils} rejouent le dernier niveau en vrai, en sautant par-dessus des racines. Personne n'a de vie supplémentaire : on fait attention.",
                "🏰 Donjon en brindilles, avec un boss en pomme de pin. Le boss gagne. Il gagne toujours.",
                "👾 {S} explique les règles d'un jeu à {L}, qui perd dès la première seconde et réclame une revanche dès la deuxième.",
                "🎯 Tournoi à deux. La finale est très disputée. Les demi-finales aussi, bizarrement.",
                "🧩 {Ils} construisent un niveau entier avec des cailloux et des feuilles. Il est injouable. {Ils} en sont très {p:fiers|fières}.",
                "💾 {S} explique à {L} comment sauvegarder une partie. {L} ne comprend pas, mais sauvegarde quand même, par précaution.",
                "🪙 {Ils} ramassent toutes les petites choses brillantes du coin en faisant « pling » à chaque fois. Score final : quarante plings.",
                "🤝 Partie en coopération. {Ils} perdent ensemble, avec beaucoup de dignité, et recommencent pour perdre mieux.",
                "🏃 {Ils} font le tour du jardin le plus vite possible, comme dans un jeu. Record : quatre secondes. Personne n'a vérifié." }),
        new(PlynlingPassion.Astronomy, "🔭", "l'astronomie",
            new[] { "astronomie", "etoile", "etoiles", "planete", "planetes", "lune", "espace", "galaxie", "cosmos", "constellation", "constellations" },
            new[] { "🔭 {Ils} fabriquent un télescope avec une feuille roulée. On ne voit rien, mais on le voit très bien.",
                "🌙 {Ils} dessinent la lune à toutes ses étapes, sur une seule feuille. Elle a l'air de faire une grimace.",
                "⭐ {S} montre les constellations à {L} sur une carte ; {L} en voit d'autres, plus drôles, aux mêmes endroits.",
                "🪐 Maquette du système solaire en baies. Certaines planètes sont mangées en route. Jupiter la première.",
                "🌠 {Ils} guettent une étoile filante. Elle ne vient pas. {Ils} en fabriquent une avec un caillou lancé très haut. Vœu accepté.",
                "☄️ Comète en pomme de pin et en ruban. Elle vole très mal, mais avec panache.",
                "🗺️ {Ils} dessinent une carte de constellations inventées. La plus grande s'appelle « le Goûter ».",
                "🌒 {S} explique les phases de la lune à {L} avec un biscuit, croqué petit à petit. À la nouvelle lune, il n'y a plus de biscuit.",
                "🚀 Fusée en brindilles. Elle ne décolle pas. {Ils} font quand même le compte à rebours, trois fois.",
                "🌞 {S} fabrique un cadran solaire avec un bâton planté dans la terre. Il indique midi toute la journée. C'est très rassurant." }),
        new(PlynlingPassion.Gardening, "🌱", "le jardinage",
            new[] { "jardinage", "jardin", "jardiner", "plante", "plantes", "fleur", "fleurs", "potager", "graine", "graines" },
            new[] { "🌱 {Ils} plantent une graine et la regardent pousser. Elle ne pousse pas. {Ils} restent quand même, pour l'encourager.",
                "🌷 {Ils} plantent des fleurs en forme d'étoile. Vu d'en haut, ça ressemble plutôt à une patate. Une jolie patate.",
                "💧 {S} arrose, {L} tient l'arrosoir. Puis l'inverse. Tout le monde est mouillé, les plantes aussi, un peu.",
                "🥕 {Ils} tirent sur une carotte récalcitrante. Elle cède d'un coup ; {ils} tombent à la renverse. La carotte est minuscule.",
                "🪴 {Ils} rempotent une petite plante et lui cherchent un nom. Le désaccord sur le nom dure plus longtemps que le rempotage.",
                "🐌 {Ils} déménagent poliment une famille d'escargots hors du potager. Les escargots reviennent aussitôt, poliment aussi.",
                "🍓 Cueillette de fraises. La moitié finit dans le panier ; l'autre moitié n'y arrive jamais.",
                "🌾 {S} apprend à {L} à désherber. {L} arrache une fleur par erreur, et s'excuse longuement auprès d'elle.",
                "🪱 {Ils} présentent un ver de terre à tout le jardin, comme un invité d'honneur. Le ver n'a rien demandé.",
                "🌸 {Ils} sèment des fleurs en dessinant leurs initiales. Dans quelques semaines, on les lira du ciel. En attendant, il faut de l'imagination." }),
        new(PlynlingPassion.Rocks, "🪨", "les cailloux",
            new[] { "caillou", "cailloux", "pierre", "pierres", "galet", "galets", "roche", "roches", "mineraux", "cristaux" },
            new[] { "🪨 {Ils} classent des cailloux par couleur, puis par taille, puis par « personnalité ». Le dernier classement fait débat.",
                "🔍 Chasse au caillou parfait. {Ils} en trouvent trois, et passent le reste du temps à décider lequel est le plus parfait.",
                "🏔️ {Ils} construisent une montagne de cailloux. Elle tient debout presque une minute. Une minute historique.",
                "🖌️ {S} dessine des visages sur des galets ; {L} leur invente des prénoms, et des histoires très tristes.",
                "🏆 Concours du plus beau caillou. Deux candidats, deux vainqueurs, aucun perdant, et un jury très partial.",
                "🌊 Ricochets ! Le record est battu, puis re-battu, puis contesté, puis re-battu en cachette.",
                "🏗️ {Ils} construisent une petite maison en cailloux pour une fourmi. Elle refuse de signer le bail.",
                "📛 {Ils} donnent un nom et un prénom à chaque caillou du coin. Il y a désormais trois « Bernard ».",
                "⚖️ Pesée officielle des cailloux. Le plus lourd reçoit une médaille en feuille. Il ne dit rien. Il est ému.",
                "🔦 {S} montre à {L} un caillou qui brille dans le noir. Il ne brille pas vraiment. {L} fait semblant de le voir, très fort." }),
        new(PlynlingPassion.Stories, "📚", "les histoires",
            new[] { "histoire", "histoires", "lecture", "lire", "livre", "livres", "conte", "contes", "roman", "romans" },
            new[] { "📖 {S} lit une histoire à voix haute ; {L} fait toutes les voix des méchants, avec beaucoup trop de talent.",
                "🐉 {Ils} jouent une histoire de dragon. {L} fait le dragon, avec beaucoup trop de conviction. Un voisin s'inquiète.",
                "✍️ {Ils} écrivent une histoire à deux, une phrase à tour de rôle. Elle part dans tous les sens, et c'est parfait.",
                "🏰 {S} raconte un conte de chevaliers ; {L} essaie de deviner la fin, se trompe à chaque fois, et préfère ses propres fins.",
                "📚 {Ils} construisent un fort avec des livres et lisent dedans jusqu'à ce que le fort s'écroule. Il s'écroule au meilleur moment.",
                "🎭 Pièce de théâtre à deux personnages. Chaque personnage change trois fois de costume, et deux fois d'avis.",
                "[sad] 🕯️ {S} raconte une histoire qui fait peur. {L} a peur. {S} aussi, un peu. Beaucoup, en fait.",
                "🦉 {Ils} inventent une histoire sur le hibou du coin. Le hibou écoute, et n'approuve pas la fin.",
                "📜 Conte sur une grande feuille roulée. Il commence par « Il était une fois » et finit par « etc. ».",
                "🧚 {S} lit un passage à voix haute ; {L} mime tous les personnages, y compris le vent. Surtout le vent." }),
        new(PlynlingPassion.Dance, "💃", "la danse",
            new[] { "danse", "danser", "ballet", "valse", "tango" },
            new[] { "💃 {S} apprend à {L} une petite danse. {L} marche sur tous les pieds disponibles, y compris les siens.",
                "🩰 Cours de danse. Premier exercice : ne pas tomber. {L} échoue brillamment, et gracieusement.",
                "💫 {Ils} dansent sous les feuilles qui tombent, en essayant de les rattraper au vol. Score : deux feuilles.",
                "🕺 Battle de danse ! Le jury, un escargot, met si longtemps à juger que la battle recommence. Et recommence.",
                "🎊 Chorégraphie en trois mouvements. Le troisième est un câlin, par accident. On le garde.",
                "🪩 {Ils} organisent un bal pour les insectes. Les coccinelles viennent en couple, les fourmis en groupe, les scarabées en retard.",
                "🌀 {Ils} tournent {p:sur eux-mêmes|sur elles-mêmes} jusqu'à ce que le monde tourne tout seul. Il tourne encore un peu après.",
                "🥁 {S} tape un rythme, {L} invente une danse dessus. Puis on échange, et c'est {S} qui danse, bien moins bien.",
                "🦩 {Ils} tiennent sur un pied le plus longtemps possible, et tombent en même temps, par solidarité.",
                "🎩 Danse de salon. Beaucoup de pieds écrasés, et beaucoup d'excuses. À la fin, plus d'excuses que de pas." }),
        new(PlynlingPassion.Painting, "🎨", "la peinture",
            new[] { "peinture", "peindre", "dessin", "dessiner", "aquarelle", "tableau", "tableaux" },
            new[] { "🎨 {Ils} peignent le portrait {p:l'un de l'autre|l'une de l'autre}. Personne n'est ressemblant. Tout le monde est flatté.",
                "🖼️ Même paysage, côte à côte. Les deux tableaux n'ont absolument rien en commun. On dirait deux pays.",
                "🌈 {S} mélange toutes les couleurs pour trouver la plus belle. {L} annonce que c'est marron. C'est marron.",
                "✏️ {S} fait un croquis rapide de {L}, qui prend la pose beaucoup trop longtemps, et beaucoup trop sérieusement.",
                "🪨 Fresque sur un grand rocher. Un lézard proteste, puis finit par poser, de profil, son meilleur côté.",
                "🖍️ Dessins à la craie sur une pierre plate. La pluie fera le reste. La pluie a du talent.",
                "🍂 Tableau en feuilles collées. Le vent n'est pas d'accord, et emporte le ciel.",
                "🌅 {Ils} peignent le coucher de soleil. Le soleil se couche trop vite ; {ils} finissent de mémoire, en en rajoutant un peu.",
                "🟤 {S} apprend à {L} à mélanger les couleurs. Tout finit en marron, mais un joli marron, avec de la personnalité.",
                "🙂 {Ils} décorent des cailloux avec de petits visages. Chaque visage a l'air légèrement surpris. On comprend." }),
        new(PlynlingPassion.Sport, "🏃", "le sport",
            new[] { "sport", "course", "courir", "football", "foot", "natation", "nager", "velo", "escalade", "tennis" },
            new[] { "🏃 Course jusqu'au bout du chemin ! {S} part trop vite et s'essouffle à mi-parcours. {L} s'arrête pour attendre. Victoire morale.",
                "⚽ Match de ballon avec une noix. La noix gagne.",
                "🏁 Course jusqu'au prochain arbre, puis jusqu'au suivant, puis jusqu'à l'épuisement. L'épuisement gagne.",
                "🤸 {S} apprend la roue à {L}. Le résultat ressemble surtout à une chute bien organisée.",
                "🏅 Jeux olympiques à deux. Une médaille par épreuve, trois épreuves, et une cérémonie de remise interminable.",
                "🏐 {Ils} se renvoient une feuille par-dessus une ligne tracée au sol. La feuille ne coopère pas. Elle a ses propres plans.",
                "🧗 Escalade d'une grosse racine. Au sommet, {ils} plantent un drapeau en brindille, et redescendent très {p:fiers|fières}.",
                "🏋️ {Ils} soulèvent des cailloux de plus en plus gros. Le dernier reste par terre. Il a gagné.",
                "🛷 {Ils} dévalent une petite pente sur une grande feuille. Puis remontent. Puis redévalent. Encore. Encore.",
                "🎽 {S} entraîne {L} comme un vrai coach, avec un sifflet imaginaire. {L} obéit, en râlant juste assez pour que ce soit drôle." }),
        new(PlynlingPassion.Insects, "🐞", "les insectes",
            new[] { "insecte", "insectes", "fourmi", "fourmis", "coccinelle", "coccinelles", "papillon", "papillons", "scarabee", "abeille", "abeilles" },
            new[] { "🐞 {Ils} construisent un petit hôtel pour coccinelles. Il n'a pas encore de clients. Il a déjà un règlement.",
                "🦋 {Ils} suivent un papillon très longtemps, sans jamais savoir où il va. Lui non plus, apparemment.",
                "🔎 {S} inspecte un brin d'herbe à la loupe. {L} trouve trois insectes que {S} n'avait pas vus, et ne le fait pas remarquer. Trop.",
                "🐜 {Ils} aident une fourmi à porter sa miette. Elle n'a rien demandé, mais elle accepte, avec dignité.",
                "🏠 Maison en brindilles pour un scarabée. Il préfère son caillou. {Ils} respectent son choix.",
                "🐛 Course de chenilles. Les chenilles s'arrêtent pour manger en plein milieu. Tout le monde attend. Elles ont le temps.",
                "🍯 Visite à la ruche, à distance respectueuse. Très respectueuse. Encore plus respectueuse, après une abeille.",
                "🪲 Parcours d'obstacles pour scarabées. Le scarabée le contourne entièrement, et il a raison.",
                "🌼 {S} montre à {L} comment les abeilles choisissent leurs fleurs. {L} choisit la même fleur. Elle est très bien.",
                "🦗 {Ils} essaient de répondre aux grillons en sifflant. Les grillons se taisent, vexés. Puis reprennent plus fort." }),
        new(PlynlingPassion.Naps, "😴", "les siestes",
            new[] { "sieste", "siestes", "dormir", "sommeil", "dodo", "roupiller" },
            new[] { "😴 {Ils} testent tous les coins d'herbe pour trouver le meilleur endroit où faire la sieste, et s'endorment avant la fin des essais.",
                "☁️ {Ils} regardent les nuages et leur trouvent des formes. Le troisième ressemble à un oreiller. C'est la fin.",
                "🛌 Lit de mousse pour deux. Il est parfait. Trop parfait : personne ne se relèvera avant longtemps.",
                "💤 {S} raconte une histoire à {L} pour l'aider à s'endormir. C'est {S} qui s'endort en premier, au milieu d'un mot.",
                "🌿 Sieste à l'ombre d'une grande feuille. Un escargot s'installe à côté, et s'endort aussi. Il ronfle.",
                "🧸 {Ils} construisent un nid de feuilles géant, et s'y installent « juste pour essayer ». L'essai dure une heure.",
                "🌤️ {Ils} suivent le soleil d'un coin d'herbe à l'autre, pour rester au chaud. Six changements de place, sans jamais ouvrir les yeux.",
                "🎐 {S} fabrique un petit mobile qui tinte au vent pour bercer {L}. Ça marche très bien. Trop bien. Sur {S} aussi.",
                "⏳ {Ils} chronomètrent la sieste parfaite avec un sablier. Le sablier se termine bien avant la sieste. Personne ne le retourne.",
                "🐑 {Ils} comptent les nuages au lieu des moutons. Au quatrième, plus personne ne compte. Au cinquième, plus personne n'est réveillé." }),
    };

    // Pair activities, keyed in either order (see ComboFor). They join the subject's own activities
    // (see PlynlingVisitStory.ActivityPool), so a pair gets a few, never just one.
    private static readonly Dictionary<(PlynlingPassion, PlynlingPassion), string[]> Combos = new()
    {
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Music)] = new[]
        {
            "🎶 {Ils} composent une chanson sur les crêpes. Le refrain dit seulement « crêpe », mais avec beaucoup d'émotion.",
            "🥄 {Ils} jouent de la batterie sur des casseroles vides en attendant que la pâte lève. La pâte ne lève pas : elle écoute.",
            "🎵 {S} chante pendant que la confiture cuit, pour qu'elle prenne mieux. {L} fait les chœurs. La confiture prend. Personne ne peut prouver le contraire.",
        },
        [Pair(PlynlingPassion.Astronomy, PlynlingPassion.Naps)] = new[]
        {
            "🌌 {Ils} s'allongent pour regarder les étoiles. Au bout de la troisième, tout le monde dort.",
            "🌙 {S} affirme que la lune fait la sieste le jour. {L} propose de l'imiter, par respect. Le respect dure longtemps.",
            "🪐 {Ils} fabriquent un oreiller en forme de planète. Il est trop rond pour dormir dessus. {Ils} dorment à côté, pour lui tenir compagnie.",
        },
        [Pair(PlynlingPassion.Insects, PlynlingPassion.Gardening)] = new[]
        {
            "🐛 {Ils} font la visite du potager en saluant chaque insecte par son prénom.",
            "🐞 {Ils} engagent trois coccinelles pour garder les salades. Le salaire : des pucerons. Les salades n'ont jamais été aussi bien gardées.",
            "🪱 {S} organise une réunion entre les vers de terre et les radis, pour que chacun respecte l'espace de l'autre. Personne ne dit rien. C'est un succès.",
        },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Gardening)] = new[]
        {
            "🥕 {Ils} cueillent des légumes et en font une soupe. Elle est délicieuse et un peu terreuse.",
            "🌿 {Ils} font une tisane avec tout ce qui pousse au bord du chemin. {L} demande ce qu'il y a dedans. {S} répond « du vert ». C'est vrai.",
            "🍅 {Ils} attendent qu'une tomate soit mûre pour en faire une sauce. Elle rougit très lentement sous leurs regards. On la comprend.",
        },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Insects)] = new[]
        {
            "🐝 {Ils} vont demander un peu de miel aux abeilles. Les abeilles négocient durement.",
            "🐜 {Ils} préparent un pique-nique minuscule pour les fourmis. Les fourmis emportent tout, y compris la nappe.",
            "🦗 {S} cuisine au rythme des grillons. Quand les grillons accélèrent, la cuillère aussi. La pâte finit un peu partout, surtout sur {L}.",
        },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Dance)] = new[]
        {
            "🎶 {S} joue, {L} danse, puis {ils} échangent. Personne n'est doué pour le rôle de l'autre, et c'est très drôle.",
            "🥁 {S} change de rythme sans prévenir, pour voir. {L} suit, puis devine, puis impose le sien. À la fin, c'est la musique qui suit la danse.",
            "🎶 {Ils} inventent une danse pour une chanson qui n'existe pas encore, puis la chanson pour aller avec. Elles vont très bien ensemble. Quelle chance.",
        },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Stories)] = new[]
        {
            "🎼 {Ils} transforment un conte en chanson. Ça dure longtemps. Personne ne s'en plaint.",
            "📖 {S} lit un conte à voix haute ; {L} fait la musique de fond avec la bouche. Le suspense est insoutenable, surtout la porte qui grince.",
            "🎺 {Ils} écrivent l'opéra d'un escargot qui voulait voir la mer. Trois actes. L'escargot n'entre en scène qu'au troisième, et chante très lentement.",
        },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Sport)] = new[]
        {
            "🏃 {Ils} jouent à un jeu vidéo… en vrai. Il faut courir, sauter, et crier « pause » quand on n'en peut plus.",
            "🕹️ {S} donne les ordres comme une manette : « gauche, gauche, saute ! » {L} exécute, avec un léger retard qu'on met sur le compte de la connexion.",
            "🏆 Chaque épreuve rapporte des points d'expérience. Au niveau 3, {ils} débloquent enfin le droit de s'asseoir.",
        },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Stories)] = new[]
        {
            "🗺️ {Ils} inventent l'histoire d'un jeu vidéo qui n'existe pas. Le héros est un Plynling, évidemment.",
            "📜 {S} raconte une quête ; {L} choisit ce que fait le héros à chaque carrefour. Le héros ouvre tous les coffres et ne sauve personne.",
            "🐉 {Ils} écrivent la notice d'un jeu imaginaire. Elle fait douze pages. Le jeu tient en une règle : ne pas réveiller le dragon.",
        },
        [Pair(PlynlingPassion.Astronomy, PlynlingPassion.Stories)] = new[]
        {
            "🌌 {Ils} inventent une légende pour chaque étoile. Il y en a beaucoup. La journée ne suffit pas.",
            "🌙 {S} raconte pourquoi la lune change de forme. {L} a une autre version, où la lune grignote en cachette. Elle est plus convaincante.",
            "✨ {Ils} tracent une constellation en cailloux et lui écrivent une légende : un héros qui a perdu son écharpe. Il la cherche encore, là-haut.",
        },
        [Pair(PlynlingPassion.Painting, PlynlingPassion.Gardening)] = new[]
        {
            "🌷 {Ils} peignent les fleurs du jardin. Les fleurs, flattées, se tiennent bien droites.",
            "🎨 {Ils} peignent les pots de fleurs, chacun d'une couleur. Une fleur refuse de pousser dans le violet. Personne n'insiste.",
            "🌻 {S} peint une fleur ; {L} en plante une vraie à côté, pour comparer. Le jury, une abeille, se pose sur le tableau.",
        },
        [Pair(PlynlingPassion.Painting, PlynlingPassion.Astronomy)] = new[]
        {
            "🎨 {Ils} peignent un ciel étoilé sur une grande pierre. Il y a plus d'étoiles sur le tableau que dans le vrai.",
            "🌌 {Ils} peignent la carte du ciel de mémoire. Il y a des désaccords. Une étoile est ajoutée pour faire plaisir à {L}.",
            "🪐 {S} peint Saturne ; {L} lui fait des anneaux en brins d'herbe. Saturne n'a jamais été aussi bien habillée.",
        },
        [Pair(PlynlingPassion.Rocks, PlynlingPassion.Painting)] = new[]
        {
            "🪨 {Ils} peignent des cailloux et les cachent un peu partout, pour que d'autres les trouvent.",
            "🖌️ {Ils} peignent un caillou pour qu'il ressemble à un autre caillou. Personne ne voit la différence. C'est ça, l'art.",
            "🌈 {S} écrase des cailloux de couleur pour faire de la peinture ; {L} peint avec. Le tableau représente des cailloux. La boucle est bouclée.",
        },
        [Pair(PlynlingPassion.Sport, PlynlingPassion.Naps)] = new[]
        {
            "😴 Course jusqu'au meilleur coin pour faire la sieste ! Le vainqueur s'endort sur la ligne d'arrivée.",
            "🏋️ {S} s'entraîne à la sieste comme à un sport : échauffement, étirements, bâillement réglementaire. {L} arbitre, et s'endort avant le coup de sifflet.",
            "🏃 {Ils} courent jusqu'à l'épuisement pour « mériter » la sieste. La sieste est méritée au bout de trente secondes.",
        },
        [Pair(PlynlingPassion.Dance, PlynlingPassion.Sport)] = new[]
        {
            "🤸 {Ils} inventent un sport qui ressemble beaucoup à de la danse. Ou une danse qui ressemble beaucoup à du sport.",
            "🧘 Des étirements qui tournent à la chorégraphie. {S} dit que c'est du sport, {L} que c'est de la danse. Leurs jambes disent que c'est trop.",
            "🏅 {Ils} notent chaque pirouette comme dans un concours : 9,5 pour la technique, 10 pour la chute. La chute est très réussie.",
        },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Stories)] = new[]
        {
            "📖 {Ils} écrivent un livre de recettes qui est aussi un conte. Chaque plat a son méchant.",
            "🍲 {S} cuisine en racontant la vie de chaque ingrédient. La carotte a eu une vie difficile. {L} la mange avec respect.",
            "🧁 {Ils} préparent le gâteau d'un conte : trois étages, une fève et une malédiction. La malédiction, c'est la vaisselle.",
        },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Naps)] = new[]
        {
            "🥛 {Ils} préparent le goûter idéal avant la sieste. {Ils} s'endorment avant de le manger.",
            "🍯 {S} prépare un lait chaud au miel, « pour bien dormir ». {L} le boit, et s'endort avant d'avoir posé la tasse.",
            "🥖 {Ils} laissent une brioche lever et font une sieste en attendant. La brioche lève. {Ils}, beaucoup moins.",
        },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Astronomy)] = new[]
        {
            "🎶 {Ils} composent une chanson pour chaque planète. Celle de la lune est une berceuse.",
            "🔭 {S} affirme que chaque étoile est une note. {L} les joue toutes, de gauche à droite. C'est la chanson la plus longue du monde.",
            "🎵 {Ils} donnent un concert à la lune. Elle ne dit rien, mais elle reste jusqu'au bout. C'est déjà beaucoup.",
        },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Insects)] = new[]
        {
            "🦗 {Ils} forment un orchestre avec les grillons. Les grillons exigent d'être en tête d'affiche.",
            "🐝 {S} fredonne sur la même note que les abeilles. Une abeille s'approche, intéressée, puis repart, déçue par l'accent.",
            "🦋 {Ils} jouent une valse lente pour un papillon. Il bat des ailes en mesure. Enfin, presque.",
        },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Rocks)] = new[]
        {
            "🪨 {Ils} inventent un jeu où il faut collectionner des cailloux rares. Le niveau final est une rivière.",
            "🎲 {Ils} font des dés, des pions et un plateau avec une poignée de cailloux. Les règles changent à chaque partie, en faveur de qui les invente.",
            "🗝️ {S} cache un caillou « légendaire » ; {L} doit le trouver en résolvant des énigmes. Dernier indice : « c'est un caillou ». Il y en a cent.",
        },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Painting)] = new[]
        {
            "🎨 {Ils} dessinent les décors d'un jeu vidéo imaginaire. Le boss final est un très grand escargot.",
            "👾 {Ils} peignent des personnages en petits carrés, comme dans les vieux jeux. Même le héros a l'air d'un escalier.",
            "🗺️ {S} dessine la carte d'un monde imaginaire ; {L} y ajoute des monstres partout. Il ne reste plus de place pour le héros.",
        },
        [Pair(PlynlingPassion.Astronomy, PlynlingPassion.Rocks)] = new[]
        {
            "☄️ {Ils} cherchent une météorite parmi les cailloux. {Ils} en trouvent une au moins quatre fois.",
            "🌑 {Ils} cherchent un caillou qui ressemble à la lune. {Ils} en trouvent un, plein de cratères. C'était une éponge.",
            "⭐ {S} range les cailloux en constellations ; {L} en pose un tout seul, loin des autres. C'est l'étoile polaire. Elle a besoin d'espace.",
        },
        [Pair(PlynlingPassion.Gardening, PlynlingPassion.Naps)] = new[]
        {
            "🌿 {Ils} plantent un carré de mousse spécialement pour les siestes. Il est testé immédiatement.",
            "🌾 {S} arrose ; {L} « surveille la terre », {l:allongé|allongée} dans l'herbe. La terre est très bien surveillée. {L} aussi : par un escargot.",
            "🥬 {Ils} font la sieste au milieu du potager, pour que les salades ne se sentent pas seules. Les salades ont l'air reposées.",
        },
        [Pair(PlynlingPassion.Dance, PlynlingPassion.Painting)] = new[]
        {
            "💃 {Ils} peignent en dansant, un pinceau dans chaque main. Le tableau est plein de tourbillons.",
            "🖌️ {S} peint ; {L} danse les couleurs au fur et à mesure. Le jaune est facile. Le gris demande beaucoup de travail.",
            "👣 {Ils} trempent leurs pieds dans la peinture et dansent sur une grande feuille. L'œuvre s'appelle « Valse ». Le nettoyage s'appelle « Punition ».",
        },
        [Pair(PlynlingPassion.Stories, PlynlingPassion.Insects)] = new[]
        {
            "🐞 {Ils} inventent l'épopée d'une coccinelle courageuse. Elle affronte une flaque gigantesque.",
            "🐜 {S} raconte aux fourmis la légende d'une fourmi géante. Elles écoutent sans s'arrêter de travailler. {L} trouve ça un peu vexant.",
            "🦗 {Ils} prêtent une voix à chaque insecte. Le scarabée a une grosse voix grave. Le scarabée n'était pas d'accord.",
        },
    };

    private static (PlynlingPassion, PlynlingPassion) Pair(PlynlingPassion a, PlynlingPassion b) => a <= b ? (a, b) : (b, a);

    public static string[]? ComboFor(PlynlingPassion a, PlynlingPassion b) => Combos.GetValueOrDefault(Pair(a, b));

    public static PassionInfo Info(PlynlingPassion passion) => All[(int)passion];

    public static PlynlingPassion RollInnate(Random rng) => (PlynlingPassion)rng.Next(All.Count);

    private static readonly Regex LeadingArticle = new(@"^(de la |de l'|du |des |les |le |la |l'|un |une )", RegexOptions.Compiled);
    private static readonly Regex NotWord = new(@"[^a-z0-9' ]+", RegexOptions.Compiled);
    private static readonly Regex Spaces = new(@"\s+", RegexOptions.Compiled);

    /// <summary>Lowercase, no accents, no leading article, no punctuation, single spaces.</summary>
    public static string Normalize(string text)
    {
        var decomposed = text.Replace('’', '\'').ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var bare = new StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
            if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) bare.Append(ch);
        var s = Spaces.Replace(NotWord.Replace(bare.ToString(), " "), " ").Trim();
        s = LeadingArticle.Replace(s, "");          // the apostrophe is still there, so « l' » is caught
        return Spaces.Replace(s.Replace("'", " "), " ").Trim();
    }

    /// <summary>What gets stored: line breaks and runs of spaces collapsed, trimmed.</summary>
    public static string Clean(string? raw) => raw is null ? "" : Spaces.Replace(raw, " ").Trim();

    public static bool ContainsLink(string text)
    {
        var lower = text.ToLowerInvariant();
        return lower.Contains("http") || lower.Contains("www.") || lower.Contains("discord.gg");
    }

    /// <summary>The catalog passion a typed text names, if a keyword appears in it as a whole word.</summary>
    public static PlynlingPassion? Resolve(string text)
    {
        var norm = " " + Normalize(text) + " ";
        foreach (var info in All)
            foreach (var keyword in info.Keywords)
                if (norm.Contains(" " + keyword + " ")) return info.Passion;
        return null;
    }

    public static Passion FromText(string text) =>
        Resolve(text) is { } c ? new Passion(c, null) : new Passion(null, text);

    public static Passion? Taught(Plynling p) =>
        string.IsNullOrWhiteSpace(p.TaughtPassion) ? null : FromText(p.TaughtPassion);

    /// <summary>
    /// The taught passion when it stayed free text — one the catalog did not recognise. This is what
    /// gets quoted back, Tomodachi-style: SYNCS's card lines and the visits drop the owner's own words
    /// into ordinary moments (see <see cref="PickLines"/> and <see cref="PlynlingVisitStory"/>).
    /// </summary>
    public static Passion? Typed(Plynling p) => Taught(p) is { Catalog: null } t ? t : null;

    /// <summary>How often a card line quotes the typed passion back, when there is one.</summary>
    public const double TypedLineChance = 0.15;

    /// <summary>
    /// A card pool for <paramref name="p"/>: now and then the one quoting its typed passion, with the
    /// passion rendered (« … », sanitised) as the last format argument; otherwise the usual pool and "".
    /// Callers always pass the text, so a usual line simply ignores it.
    /// </summary>
    public static (string[] Pool, string Typed) PickLines(GenderedLines usual, GenderedLines typed, Plynling p, Random rng) =>
        Typed(p) is { } t && rng.NextDouble() < TypedLineChance ? (typed.For(p.Gender), t.Render()) : (usual.For(p.Gender), "");

    /// <summary>How often <c>/plynling view</c> shows what it is thinking about, when it has a typed passion.</summary>
    public const double ThoughtChance = 0.25;

    /// <summary>
    /// The thought bubble on <c>/plynling view</c>: now and then its typed passion, as a thought — or a
    /// dream while it sleeps — with the rendered passion as {1}. Null when there is nothing to show:
    /// no typed passion, dead, frozen, or the roll said no.
    /// </summary>
    public static (string[] Pool, string Typed)? Thought(Plynling p, DateTimeOffset now, Random rng) =>
        p.DiedAt is null && p.FrozenAt is null && Typed(p) is { } t && rng.NextDouble() < ThoughtChance
            ? ((PlynlingLife.IsAsleep(now) ? BotResponses.PlynlingDreamTypedLines : BotResponses.PlynlingThoughtTypedLines).For(p.Gender), t.Render())
            : null;

    /// <summary>Its passions: the innate one, then the taught one unless it is the same.</summary>
    public static IReadOnlyList<Passion> Of(Plynling p)
    {
        var innate = new Passion(p.Passion, null);
        return Taught(p) is { } taught && !taught.SameAs(innate) ? new[] { innate, taught } : new[] { innate };
    }
}
