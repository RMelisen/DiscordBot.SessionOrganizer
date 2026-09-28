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
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Arrivals = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "🚪 {A} arrive un peu {a:intimidé|intimidée}. {B} lui fait un petit signe poli.",
            "🚪 {A} toque timidement ; {B} l'accueille d'un sourire poli.",
            "🚪 {B} attendait {A} en se demandant de quoi {ils} allaient bien pouvoir parler.",
        },
        [VisitMood.Friends] = new[]
        {
            "🚪 {A} déboule en courant : « Me voilà ! »",
            "🚪 {B} saute de joie en voyant arriver {A}.",
            "🚪 {A} arrive les poches pleines de cailloux à montrer à {B}.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "🚪 {A} et {B} se reconnaissent de loin et courent {p:l'un vers l'autre|l'une vers l'autre}.",
            "🚪 {A} n'a même pas besoin de frapper : {B} avait laissé la porte ouverte.",
            "🚪 {A} frappe avec leur signal secret : deux petits coups, puis un troisième. {B} rit déjà.",
        },
        [VisitMood.Lovers] = new[]
        {
            "🚪 {A} arrive, un peu {a:rougissant|rougissante}. {B} l'attendait avec une fleur.",
            "🚪 {B} a tout rangé pour la venue de {A}. 💕",
            "🚪 {A} et {B} se retrouvent avec un grand sourire un peu gêné. 💞",
        },
        [VisitMood.Rivals] = new[]
        {
            "🚪 {A} arrive en bombant le torse. {B} fait semblant de ne pas {a:le|la} remarquer.",
            "🚪 « Encore toi ? » lance {B} en voyant arriver {A}. Mais avec un sourire en coin.",
            "🚪 {A} arrive, bien {a:décidé|décidée} à prouver qui est {a:le meilleur|la meilleure}.",
        },
        [VisitMood.Conflict] = new[]
        {
            "🚪 {A} arrive en traînant des pieds. {B} croise les bras.",
            "🚪 {B} ouvre à {A} sans un mot. L'ambiance est… tendue.",
            "🚪 {A} arrive déjà de mauvaise humeur, et {B} ne fait rien pour arranger ça.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Activities = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "☁️ {Ils} parlent de la pluie et du beau temps. Surtout de la pluie.",
            "🍵 {B} sert une tasse de rosée à {A}. {Ils} la boivent poliment, à petites gorgées.",
            "🪨 {Ils} comparent leurs plus beaux cailloux, avec beaucoup de sérieux.",
            "🐞 {Ils} s'assoient côte à côte et regardent passer une coccinelle.",
        },
        [VisitMood.Friends] = new[]
        {
            "🏃 Course jusqu'au grand arbre ! {A} gagne d'un souffle.",
            "🙈 Partie de cache-cache : {B} se cache derrière une feuille bien trop petite.",
            "🍂 Grande bataille de feuilles mortes ! Personne ne gagne, tout le monde rit.",
            "🐌 {Ils} organisent une course d'escargots. Celui de {A} s'endort en route.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "🛖 {Ils} construisent une cabane en brindilles et jurent de ne jamais révéler où.",
            "🤫 {Ils} se racontent des secrets à voix basse, en pouffant de rire.",
            "🎨 {Ils} peignent un grand tableau à quatre petits bras.",
            "💃 {A} et {B} inventent une danse que personne d'autre ne connaîtra jamais.",
        },
        [VisitMood.Lovers] = new[]
        {
            "🍓 Goûter en tête-à-tête : {A} garde la plus grosse fraise pour {B}.",
            "🌠 {Ils} comptent les étoiles filantes, {p:serrés l'un contre l'autre|serrées l'une contre l'autre}.",
            "💐 {A} a cueilli un petit bouquet de trèfles pour {B}.",
            "🎶 {B} fredonne une chanson douce ; {A} écoute, les yeux fermés.",
        },
        [VisitMood.Rivals] = new[]
        {
            "😝 Concours de grimaces ! Le jury (une fourmi) refuse de départager.",
            "🦘 Qui saute le plus haut ? {A} jure avoir gagné, {B} exige une revanche.",
            "🏁 Course jusqu'à l'étang. Match nul, et personne ne veut l'admettre.",
            "🪨 {Ils} empilent des cailloux : la tour de {B} tient, celle de {A} s'écroule.",
        },
        [VisitMood.Conflict] = new[]
        {
            "🌰 Dispute pour le dernier gland. {Ils} finissent par le couper en deux, en boudant.",
            "😤 {Ils} boudent dos à dos pendant un bon moment.",
            "💢 {A} et {B} ne sont d'accord sur rien, pas même sur la couleur du ciel.",
            "🙄 {B} soupire à chaque phrase de {A}. Ambiance.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Exchanges = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "Belle journée, non ?\nOui… très belle.",
            "Tu aimes les cailloux ?\nQui n'aime pas les cailloux ?",
            "Tu viens souvent par ici ?\nParfois. C'est calme.",
        },
        [VisitMood.Friends] = new[]
        {
            "Tu crois que les nuages ont un goût ?\nSûrement fraise.",
            "On refait la course ?\nSeulement si tu me laisses de l'avance !",
            "Je t'ai apporté un caillou tout rond !\nOh, c'est le plus beau de ma collection !",
        },
        [VisitMood.BestFriends] = new[]
        {
            "Tu te souviens de la fois où on s'est {p:perdus|perdues} ?\nOn ne s'est pas {p:perdus|perdues}. On explorait.",
            "Promis, je ne le dirai à personne.\nMême pas au caillou ?",
            "{p:Meilleurs amis|Meilleures amies} pour toujours ?\nEt même un peu plus longtemps.",
        },
        [VisitMood.Lovers] = new[]
        {
            "Tu es {b:le plus mignon|la plus mignonne} de tout le jardin.\nArrête, tu me fais rougir…",
            "On pourrait rester là pour toujours.\nD'accord. Mais après le goûter.",
            "Tu as pensé à moi, aujourd'hui ?\nToute la journée, voyons.",
        },
        [VisitMood.Rivals] = new[]
        {
            "J'ai gagné, admets-le.\nJamais de la vie.",
            "La prochaine fois, je t'écrase.\nTu dis ça à chaque fois.",
            "Tu t'es {b:entraîné|entraînée} en cachette ?\nMoi ? Jamais. …Un peu.",
        },
        [VisitMood.Conflict] = new[]
        {
            "C'était mon gland.\nTon nom n'était pas écrit dessus.",
            "Tu parles trop fort.\nEt toi, tu boudes trop fort.",
            "Je rentre chez moi.\nBonne idée.",
        },
    };

    public static readonly IReadOnlyDictionary<VisitMood, string[]> Departures = new Dictionary<VisitMood, string[]>
    {
        [VisitMood.Acquaintances] = new[]
        {
            "👋 {A} repart en faisant un petit signe. C'était… agréable.",
            "👋 {Ils} se quittent poliment, en se promettant de se revoir.",
        },
        [VisitMood.Friends] = new[]
        {
            "👋 {A} repart en sautillant, {a:ravi|ravie} de sa journée.",
            "👋 « À la prochaine ! » crie {A} en s'éloignant. {B} agite la main jusqu'au bout.",
        },
        [VisitMood.BestFriends] = new[]
        {
            "👋 {Ils} se disent au revoir trois fois, puis une quatrième, pour être {p:sûrs|sûres}.",
            "👋 {A} repart avec un bracelet d'herbes tressé par {B}. {Ils} ont le même.",
        },
        [VisitMood.Lovers] = new[]
        {
            "💞 {A} repart sur un petit nuage. {B} reste un long moment sur le pas de la porte.",
            "💞 {Ils} se séparent à regret, en se retournant tous les trois pas.",
        },
        [VisitMood.Rivals] = new[]
        {
            "⚡ {A} repart en jurant de revenir plus {a:fort|forte}. {B} a déjà hâte.",
            "⚡ « Match retour la prochaine fois ! » lance {B}. {A} ricane en s'éloignant.",
        },
        [VisitMood.Conflict] = new[]
        {
            "💢 {A} repart en claquant la porte. {B} marmonne quelque chose d'inaudible.",
            "😤 {A} rentre en boudant. Ce n'était pas une bonne journée.",
            "🙄 {Ils} se quittent sans un regard. Ça ira mieux la prochaine fois… peut-être.",
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

    /// <summary>A place open at <paramref name="now"/> (Paris time). Always at least the five always-open ones.</summary>
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
