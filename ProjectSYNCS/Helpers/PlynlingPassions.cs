using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using ProjectSYNCS.Models;

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

    /// <summary>For the card: with the catalog emoji.</summary>
    public string Display() => Catalog is { } c ? $"{PlynlingPassions.Info(c).Emoji} {PlynlingPassions.Info(c).Label}" : Render();
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
            new[] { "🥞 {Ils} se lancent dans des crêpes. La première est ratée, comme le veut la tradition.",
                "🍪 {Ils} font des biscuits en forme de cailloux. Ils ont aussi le goût de cailloux, mais c'est l'intention qui compte.",
                "🍲 {S} prépare une soupe ; {L} goûte à chaque étape en donnant des notes très sévères.",
                "🧁 Concours de décoration de petits gâteaux. Le jury (un moineau) mange les deux avant de voter.",
                "🥧 {Ils} préparent une tarte à quatre mains. Il manque la moitié des ingrédients. Elle est quand même délicieuse.",
                "🍓 {Ils} font de la confiture de fraises. Il en reste un pot. Il y en avait dix au début.",
                "🥐 {S} tente des croissants. Ils ressemblent à des cailloux. {L} les trouve délicieux, par politesse ou par faim.",
                "🍳 {Ils} font sauter des crêpes de plus en plus haut. La dernière n'est jamais redescendue.",
                "🧑‍🍳 {Ils} ouvrent un restaurant pour l'après-midi. Le seul client est un escargot. Il laisse un pourboire.",
                "🍵 {S} prépare une infusion de feuilles mystère. {L} la boit courageusement, puis demande ce que c'était." }),
        new(PlynlingPassion.Music, "🎵", "la musique",
            new[] { "musique", "chanson", "chansons", "chanter", "chant", "piano", "guitare", "violon", "flute", "batterie", "melodie" },
            new[] { "🎶 {S} apprend un petit air à {L}, qui le massacre avec beaucoup d'enthousiasme.",
                "🥁 {Ils} fabriquent une batterie avec des coquilles de noix. Le voisinage apprécie moyennement.",
                "🎤 {Ils} donnent un petit concert à une coccinelle. Elle reste jusqu'à la fin, par politesse.",
                "🎻 {L} tient le rythme, {S} chante. Puis {ils} échangent, et c'est pire. Bien pire. {Ils} adorent.",
                "🎵 {Ils} écrivent une chanson sur la journée. Le refrain rime avec « caillou » ; tout le reste aussi.",
                "🎹 {Ils} fabriquent un xylophone avec des brindilles de toutes les tailles. Il sonne faux. Magnifiquement faux.",
                "🪕 {S} gratte une ficelle tendue entre deux branches ; {L} invente les paroles au fur et à mesure.",
                "🎺 {Ils} imitent tous les instruments d'un orchestre avec la bouche. Le tuba est particulièrement réussi.",
                "🎧 {Ils} écoutent les bruits autour, les yeux fermés, et en font une chanson. Le refrain, c'est un moineau.",
                "🎙️ {Ils} donnent un concert devant trois fourmis. Les fourmis ne s'arrêtent pas, mais ralentissent." }),
        new(PlynlingPassion.Gaming, "🎮", "les jeux vidéo",
            new[] { "jeux video", "jeu video", "gaming", "console", "manette", "minecraft" },
            new[] { "🎮 {Ils} inventent un jeu vidéo sans écran : il suffit de crier les actions très fort.",
                "🕹️ {Ils} rejouent le dernier niveau en vrai, en sautant par-dessus des racines. Personne n'a de vie supplémentaire.",
                "🏰 {Ils} construisent un donjon en brindilles, avec un boss en pomme de pin. Le boss gagne.",
                "👾 {S} explique les règles d'un jeu à {L}, qui perd dès la première seconde et réclame une revanche.",
                "🎯 {Ils} organisent un tournoi à deux. La finale est très disputée, les demi-finales aussi, bizarrement.",
                "🗡️ {Ils} font un combat de boss avec des brindilles. Le boss est une pomme de pin. Elle se défend bien.",
                "🧩 {Ils} construisent un niveau entier avec des cailloux et des feuilles. Il est injouable. {Ils} en sont très {p:fiers|fières}.",
                "💾 {S} explique à {L} comment sauvegarder une partie. {L} ne comprend pas, mais hoche la tête en sauvegardant quand même.",
                "🪙 {Ils} ramassent toutes les petites choses brillantes du coin en faisant « pling » à chaque fois.",
                "🤝 {Ils} jouent à un jeu en coopération. {Ils} perdent ensemble, avec beaucoup de dignité." }),
        new(PlynlingPassion.Astronomy, "🔭", "l'astronomie",
            new[] { "astronomie", "etoile", "etoiles", "planete", "planetes", "lune", "espace", "galaxie", "cosmos", "constellation", "constellations" },
            new[] { "🔭 {Ils} fabriquent un télescope avec une feuille roulée. On ne voit rien, mais c'est magnifique.",
                "🌙 {Ils} dessinent la lune à toutes ses étapes, sur une seule feuille. Elle a l'air de faire une grimace.",
                "⭐ {S} montre les constellations à {L}, qui en voit d'autres, plus drôles, aux mêmes endroits.",
                "🪐 {Ils} construisent une maquette du système solaire avec des baies. Certaines planètes sont mangées en route.",
                "🌠 {Ils} guettent une étoile filante. Elle ne vient pas. {Ils} en inventent une avec un caillou lancé très haut.",
                "☄️ {Ils} fabriquent une comète avec une pomme de pin et un ruban. Elle vole très mal, mais avec panache.",
                "🗺️ {Ils} dessinent une carte des constellations inventées. La plus grande s'appelle « le Goûter ».",
                "🔦 {Ils} jouent à être des étoiles avec des lucioles. Les lucioles ne sont pas vraiment d'accord.",
                "🌒 {S} explique les phases de la lune à {L} avec un biscuit, croqué petit à petit.",
                "🚀 {Ils} construisent une fusée en brindilles. Elle ne décolle pas. {Ils} font le compte à rebours quand même." }),
        new(PlynlingPassion.Gardening, "🌱", "le jardinage",
            new[] { "jardinage", "jardin", "jardiner", "plante", "plantes", "fleur", "fleurs", "potager", "graine", "graines" },
            new[] { "🌱 {Ils} plantent une graine ensemble et la regardent pousser. Elle ne pousse pas. {Ils} restent quand même.",
                "🌻 {Ils} plantent des fleurs en forme d'étoile. Vu d'en haut, ça ressemble plutôt à une patate.",
                "💧 {S} arrose, {L} tient l'arrosoir. Puis l'inverse. Tout le monde est mouillé, les plantes aussi.",
                "🥕 {Ils} tirent sur une carotte récalcitrante. Elle cède d'un coup ; {ils} tombent à la renverse.",
                "🪴 {Ils} rempotent une petite plante et lui cherchent un nom. {Ils} ne sont pas d'accord sur le nom.",
                "🌸 {Ils} sèment des fleurs en dessinant leurs initiales. Dans quelques semaines, on les verra du ciel.",
                "🐌 {Ils} déménagent poliment une famille d'escargots hors du potager. Les escargots reviennent aussitôt.",
                "🍓 {Ils} cueillent des fraises. La moitié finit dans le panier ; l'autre moitié n'y arrive jamais.",
                "🌾 {S} apprend à {L} à désherber. {L} arrache une fleur par erreur et s'excuse auprès d'elle.",
                "🪱 {Ils} présentent un ver de terre à tout le jardin, comme un invité d'honneur." }),
        new(PlynlingPassion.Rocks, "🪨", "les cailloux",
            new[] { "caillou", "cailloux", "pierre", "pierres", "galet", "galets", "roche", "roches", "mineraux", "cristaux" },
            new[] { "🪨 {Ils} classent des cailloux par couleur, puis par taille, puis par « personnalité ».",
                "🔍 {Ils} partent à la chasse au caillou parfait. {Ils} en trouvent trois, et se disputent le plus parfait des trois.",
                "🏔️ {Ils} construisent une montagne de cailloux. Elle tient debout pendant presque une minute.",
                "🖌️ {S} dessine des visages sur des galets ; {L} leur invente des prénoms et des histoires tristes.",
                "🏆 {Ils} organisent un concours du plus beau caillou. Il y a deux candidats et deux vainqueurs.",
                "🌊 {Ils} font des ricochets. Le record est battu, puis re-battu, puis contesté.",
                "🏗️ {Ils} construisent une petite maison en cailloux pour une fourmi. Elle refuse de signer le bail.",
                "📛 {Ils} donnent un nom et un prénom à chaque caillou du coin. Il y a désormais trois « Bernard ».",
                "⚖️ {Ils} organisent une pesée officielle des cailloux. Le plus lourd reçoit une médaille en feuille.",
                "🔦 {S} montre à {L} un caillou qui brille dans le noir. Il ne brille pas vraiment, mais {L} fait semblant de le voir." }),
        new(PlynlingPassion.Stories, "📚", "les histoires",
            new[] { "histoire", "histoires", "lecture", "lire", "livre", "livres", "conte", "contes", "roman", "romans" },
            new[] { "📖 {S} lit une histoire à voix haute ; {L} fait toutes les voix des méchants.",
                "🐉 {Ils} jouent une histoire de dragon. {L} fait le dragon, avec beaucoup trop de conviction.",
                "✍️ {Ils} écrivent une histoire à deux, une phrase à tour de rôle. Elle part dans tous les sens, et c'est parfait.",
                "🏰 {S} raconte un conte de chevaliers ; {L} essaie de deviner la fin et se trompe à chaque fois.",
                "📚 {Ils} construisent un fort avec des livres et lisent dedans jusqu'à ce que le fort s'écroule.",
                "🎭 {Ils} jouent une pièce de théâtre à deux personnages. Chaque personnage change trois fois de costume.",
                "[sad] 🕯️ {S} raconte une histoire qui fait peur. {L} a peur. {S} aussi, un peu.",
                "🦉 {Ils} inventent une histoire sur le hibou du coin. Le hibou écoute, et n'approuve pas la fin.",
                "📜 {Ils} écrivent un conte sur une grande feuille roulée. Il commence par « Il était une fois » et finit par « etc. ».",
                "🧚 {S} lit un passage à voix haute ; {L} mime tous les personnages, y compris le vent." }),
        new(PlynlingPassion.Dance, "💃", "la danse",
            new[] { "danse", "danser", "ballet", "valse", "tango" },
            new[] { "💃 {S} apprend à {L} une petite danse. {L} marche sur tous les pieds disponibles.",
                "🩰 {S} donne un cours de danse à {L}. Premier exercice : ne pas tomber. {L} échoue brillamment.",
                "💫 {Ils} dansent sous les feuilles qui tombent, en essayant de les rattraper au vol.",
                "🕺 Battle de danse ! Le jury (un escargot) met si longtemps à juger que la battle recommence.",
                "🎊 {Ils} créent une chorégraphie en trois mouvements. Le troisième est un câlin, par accident.",
                "🪩 {Ils} organisent un bal pour les insectes. Les coccinelles viennent en couple, les fourmis en groupe.",
                "🌀 {Ils} tournent {p:sur eux-mêmes|sur elles-mêmes} jusqu'à ce que le monde tourne tout seul.",
                "🥁 {S} tape un rythme, {L} invente une danse dessus. Puis {L} tape, et c'est {S} qui danse, bien moins bien.",
                "🦩 {Ils} tiennent sur un pied le plus longtemps possible. {Ils} tombent en même temps, par solidarité.",
                "🎩 {Ils} apprennent une danse de salon. Il y a beaucoup de pieds écrasés, et beaucoup d'excuses." }),
        new(PlynlingPassion.Painting, "🎨", "la peinture",
            new[] { "peinture", "peindre", "dessin", "dessiner", "aquarelle", "tableau", "tableaux" },
            new[] { "🎨 {Ils} peignent le portrait {p:l'un de l'autre|l'une de l'autre}. Personne n'est ressemblant.",
                "🖼️ {Ils} peignent le même paysage côte à côte. Les deux tableaux n'ont absolument rien en commun.",
                "🌈 {S} mélange toutes les couleurs pour trouver la plus belle. {L} annonce que c'est marron.",
                "✏️ {S} fait un croquis rapide de {L}, qui prend la pose pendant beaucoup trop longtemps.",
                "🪨 {Ils} peignent une fresque sur un grand rocher. Un lézard proteste, puis finit par poser.",
                "🖍️ {Ils} dessinent à la craie sur un grand rocher plat. La pluie fera le reste.",
                "🍂 {Ils} font un tableau entièrement en feuilles collées. Le vent n'est pas d'accord.",
                "🌅 {Ils} peignent le coucher du soleil. Le soleil se couche trop vite ; {ils} finissent de mémoire.",
                "🟤 {S} apprend à {L} à mélanger les couleurs. Tout finit en marron, mais un joli marron.",
                "🙂 {Ils} décorent des cailloux avec des petits visages. Chaque visage a l'air légèrement surpris." }),
        new(PlynlingPassion.Sport, "🏃", "le sport",
            new[] { "sport", "course", "courir", "football", "foot", "natation", "nager", "velo", "escalade", "tennis" },
            new[] { "🏃 Course jusqu'au bout du chemin ! {S} part trop vite et s'essouffle à mi-parcours.",
                "⚽ {Ils} jouent au ballon avec une noix. La noix gagne.",
                "🏁 {Ils} font la course jusqu'au prochain arbre, puis jusqu'au suivant, puis jusqu'à l'épuisement.",
                "🤸 {S} apprend la roue à {L}. Le résultat ressemble surtout à une chute bien organisée.",
                "🏅 {Ils} organisent des jeux olympiques à deux. Il y a une médaille par épreuve et trois épreuves en tout.",
                "🏐 {Ils} se renvoient une feuille par-dessus une ligne tracée au sol. La feuille ne coopère pas.",
                "🧗 {Ils} escaladent une grosse racine. Au sommet, {ils} plantent un drapeau en brindille.",
                "🏋️ {Ils} soulèvent des cailloux de plus en plus gros. Le dernier reste par terre. Il a gagné.",
                "🛷 {Ils} dévalent une petite pente sur une grande feuille. Puis remontent. Puis redévalent. Tout l'après-midi.",
                "🎽 {S} entraîne {L} comme un vrai coach, avec un sifflet imaginaire. {L} obéit, en râlant un peu." }),
        new(PlynlingPassion.Insects, "🐞", "les insectes",
            new[] { "insecte", "insectes", "fourmi", "fourmis", "coccinelle", "coccinelles", "papillon", "papillons", "scarabee", "abeille", "abeilles" },
            new[] { "🐞 {Ils} construisent un petit hôtel pour coccinelles. Il n'a pas encore de clients.",
                "🦋 {Ils} suivent un papillon pendant très longtemps, sans jamais réussir à savoir où il va.",
                "🔎 {S} inspecte un brin d'herbe avec une loupe. {L} trouve trois insectes que {S} n'avait pas vus.",
                "🐜 {Ils} aident une fourmi à porter sa miette. Elle n'a rien demandé, mais elle accepte.",
                "🏠 {Ils} fabriquent une petite maison en brindilles pour un scarabée. Il préfère son caillou.",
                "🐛 {Ils} font une course de chenilles. Les chenilles s'arrêtent pour manger en plein milieu.",
                "🍯 {Ils} rendent visite à une ruche et restent à distance respectueuse. Très respectueuse.",
                "🪲 {Ils} construisent un parcours d'obstacles pour scarabées. Le scarabée le contourne entièrement.",
                "🌼 {S} montre à {L} comment les abeilles choisissent leurs fleurs. {L} choisit la même fleur. Elle est très bien.",
                "🦗 {Ils} essaient de répondre aux grillons en sifflant. Les grillons se taisent, vexés." }),
        new(PlynlingPassion.Naps, "😴", "les siestes",
            new[] { "sieste", "siestes", "dormir", "sommeil", "dodo", "roupiller" },
            new[] { "😴 {Ils} testent tous les coins d'herbe pour trouver le meilleur endroit où faire la sieste. {Ils} s'endorment avant la fin.",
                "☁️ {Ils} regardent les nuages et leur trouvent des formes. Le troisième ressemble à un oreiller ; c'est la fin.",
                "🛌 {Ils} construisent un lit de mousse pour deux. Il est parfait. Il est même trop parfait.",
                "💤 {S} raconte une histoire à {L} pour l'aider à s'endormir. C'est {S} qui s'endort en premier.",
                "🌿 {Ils} font la sieste à l'ombre d'une grande feuille. Un escargot s'installe à côté et s'endort aussi.",
                "🧸 {Ils} construisent un nid de feuilles géant et s'y installent « juste pour essayer ».",
                "🌤️ {Ils} suivent le soleil d'un coin d'herbe à l'autre pour rester au chaud tout l'après-midi.",
                "🎐 {S} fabrique un petit mobile qui tinte au vent pour bercer {L}. Ça marche très bien. Trop bien.",
                "⏳ {Ils} chronomètrent la sieste parfaite avec un sablier. Le sablier se termine bien avant la sieste.",
                "🐑 {Ils} comptent les nuages au lieu des moutons. Au quatrième, plus personne ne compte." }),
    };

    // Pair activities, keyed in either order (see ComboFor). Beat 4 prefers these.
    private static readonly Dictionary<(PlynlingPassion, PlynlingPassion), string[]> Combos = new()
    {
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Music)] = new[] { "🎶 {Ils} composent une chanson sur les crêpes. Le refrain dit seulement « crêpe », mais avec beaucoup d'émotion." },
        [Pair(PlynlingPassion.Astronomy, PlynlingPassion.Naps)] = new[] { "🌌 {Ils} s'allongent pour regarder les étoiles. Au bout de la troisième, tout le monde dort." },
        [Pair(PlynlingPassion.Insects, PlynlingPassion.Gardening)] = new[] { "🐛 {Ils} font la visite du potager en saluant chaque insecte par son prénom." },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Gardening)] = new[] { "🥕 {Ils} cueillent des légumes et en font une soupe. Elle est délicieuse et un peu terreuse." },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Insects)] = new[] { "🐝 {Ils} vont demander un peu de miel aux abeilles. Les abeilles négocient durement." },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Dance)] = new[] { "🎶 {S} joue, {L} danse, puis {ils} échangent. Personne n'est doué pour le rôle de l'autre, et c'est très drôle." },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Stories)] = new[] { "🎼 {Ils} transforment un conte en chanson. Ça dure longtemps. Personne ne s'en plaint." },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Sport)] = new[] { "🏃 {Ils} jouent à un jeu vidéo… en vrai. Il faut courir, sauter, et crier « pause » quand on n'en peut plus." },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Stories)] = new[] { "🗺️ {Ils} inventent l'histoire d'un jeu vidéo qui n'existe pas. Le héros est un Plynling, évidemment." },
        [Pair(PlynlingPassion.Astronomy, PlynlingPassion.Stories)] = new[] { "🌌 {Ils} inventent une légende pour chaque étoile. Il y en a beaucoup. La journée ne suffit pas." },
        [Pair(PlynlingPassion.Painting, PlynlingPassion.Gardening)] = new[] { "🌷 {Ils} peignent les fleurs du jardin. Les fleurs, flattées, se tiennent bien droites." },
        [Pair(PlynlingPassion.Painting, PlynlingPassion.Astronomy)] = new[] { "🎨 {Ils} peignent un ciel étoilé sur une grande pierre. Il y a plus d'étoiles sur le tableau que dans le vrai." },
        [Pair(PlynlingPassion.Rocks, PlynlingPassion.Painting)] = new[] { "🪨 {Ils} peignent des cailloux et les cachent un peu partout, pour que d'autres les trouvent." },
        [Pair(PlynlingPassion.Sport, PlynlingPassion.Naps)] = new[] { "😴 Course jusqu'au meilleur coin pour faire la sieste ! Le vainqueur s'endort sur la ligne d'arrivée." },
        [Pair(PlynlingPassion.Dance, PlynlingPassion.Sport)] = new[] { "🤸 {Ils} inventent un sport qui ressemble beaucoup à de la danse. Ou une danse qui ressemble beaucoup à du sport." },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Stories)] = new[] { "📖 {Ils} écrivent un livre de recettes qui est aussi un conte. Chaque plat a son méchant." },
        [Pair(PlynlingPassion.Cooking, PlynlingPassion.Naps)] = new[] { "🥛 {Ils} préparent le goûter idéal avant la sieste. {Ils} s'endorment avant de le manger." },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Astronomy)] = new[] { "🎶 {Ils} composent une chanson pour chaque planète. Celle de la lune est une berceuse." },
        [Pair(PlynlingPassion.Music, PlynlingPassion.Insects)] = new[] { "🦗 {Ils} forment un orchestre avec les grillons. Les grillons exigent d'être en tête d'affiche." },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Rocks)] = new[] { "🪨 {Ils} inventent un jeu où il faut collectionner des cailloux rares. Le niveau final est une rivière." },
        [Pair(PlynlingPassion.Gaming, PlynlingPassion.Painting)] = new[] { "🎨 {Ils} dessinent les décors d'un jeu vidéo imaginaire. Le boss final est un très grand escargot." },
        [Pair(PlynlingPassion.Astronomy, PlynlingPassion.Rocks)] = new[] { "☄️ {Ils} cherchent une météorite parmi les cailloux. {Ils} en trouvent une au moins quatre fois." },
        [Pair(PlynlingPassion.Gardening, PlynlingPassion.Naps)] = new[] { "🌿 {Ils} plantent un carré de mousse spécialement pour les siestes. Il est testé immédiatement." },
        [Pair(PlynlingPassion.Dance, PlynlingPassion.Painting)] = new[] { "💃 {Ils} peignent en dansant, un pinceau dans chaque main. Le tableau est plein de tourbillons." },
        [Pair(PlynlingPassion.Stories, PlynlingPassion.Insects)] = new[] { "🐞 {Ils} inventent l'épopée d'une coccinelle courageuse. Elle affronte une flaque gigantesque." },
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

    /// <summary>Its passions: the innate one, then the taught one unless it is the same.</summary>
    public static IReadOnlyList<Passion> Of(Plynling p)
    {
        var innate = new Passion(p.Passion, null);
        return Taught(p) is { } taught && !taught.SameAs(innate) ? new[] { innate, taught } : new[] { innate };
    }
}
