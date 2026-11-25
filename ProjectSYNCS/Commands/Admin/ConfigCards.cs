using System.Text;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /config show — one Components V2 card, a section per setting. Static and Context-free, so
// its size is checkable without a gateway: 1 container + 1 title + 16 × (separator + text)
// = 34 of the 40 components. The checks build it and measure the text too (4000 per message).
//
// Every list shows its hardcoded part and its configured part apart: only one of them can
// be edited, and merging them would invite someone to remove a hardcoded entry and be
// told no for reasons the display never hinted at. Sent with AllowedMentions.None — a
// TextDisplay is real content, and the voter list is user mentions.
public static class ConfigCards
{
    /// <param name="config">This guild's configuration.</param>
    /// <param name="isHomeGuild">Whether the main and idle channels apply here at all.</param>
    /// <param name="defaultGameChannelHere">Whether the hardcoded game channel lives in this guild.</param>
    /// <param name="defaultBotsChannelHere">Whether the hardcoded bots channel lives in this guild.</param>
    /// <param name="plynlingRoleProblem">Why she can't manage the configured Plynling role, or null.</param>
    /// <param name="quizRoleProblem">Same, for the Grosse tête role.</param>
    /// <param name="health">ServerHealth's lines for this server.</param>
    /// <param name="unset">What is not set here yet (ServerHealth.Unset).</param>
    public static MessageComponent Build(GuildConfig config, bool isHomeGuild, bool defaultGameChannelHere,
        bool defaultBotsChannelHere, string? plynlingRoleProblem, string? quizRoleProblem,
        IReadOnlyList<HealthLine> health, IReadOnlyList<string> unset)
    {
        var container = new ContainerBuilder()
            .WithAccentColor(Color.Purple)
            .AddComponent(new TextDisplayBuilder("## Configuration du serveur"));

        // First, because it is what to act on: what fails here, and what is not set yet.
        Section(container, HealthText(health, unset));

        Section(container,
            "**Rôle modérateur** — peut voter avec `/shame`",
            config.ModeratorRoleId == 0 ? "> *non configuré*" : $"> <@&{config.ModeratorRoleId}>");

        Section(container,
            "**Rôle Plynling** — tous les maîtres d'un Plynling vivant, et personne d'autre",
            config.PlynlingRoleId == 0
                ? "> *non configuré*"
                : $"> <@&{config.PlynlingRoleId}>" + (plynlingRoleProblem is null ? "" : $"\n> ⚠️ {plynlingRoleProblem}"));

        Section(container,
            "**Rôle Grosse tête** — la meilleure tête du quiz de la semaine, couronnée le lundi",
            config.QuizRoleId == 0
                ? "> *non configuré : pas de couronne*"
                : $"> <@&{config.QuizRoleId}>" + (quizRoleProblem is null ? "" : $"\n> ⚠️ {quizRoleProblem}"));

        // The hardcoded parts exist on the home server only: elsewhere a « Par défaut » line
        // would list home people and home channels that mean nothing here.
        Section(container,
            "**Votants `/shame`** — en plus du staff et du rôle modérateur",
            isHomeGuild
                ? $"> Par défaut : {Users(ShameModule.HardcodedExtraVotersFor(HomeGuild.Id))}\n> Ajoutés ici : {Users(config.ShameVoters)}"
                : $"> {Users(config.ShameVoters)}");

        Section(container,
            "**Salon de jeu** — morts, retours et histoires des Plynlings",
            "> " + Single(config.GameChannelId,
                defaultGameChannelHere ? PlynlingAnnouncer.DefaultGameChannelId : 0,
                "*aucun : pas d'annonces sur ce serveur*"));

        Section(container,
            "**Salon des bots** — ce qu'elle annonce d'elle-même et qui compte peu (les visites de Quokka)",
            "> " + Single(config.BotsChannelId,
                defaultBotsChannelHere ? BotsChannel.DefaultId : 0,
                "*aucun : le salon de jeu prend le relais*"));

        Section(container,
            "**Salon des niveaux** — où tombent les cartes de passage de niveau",
            config.LevelUpChannelId == 0
                ? "> *non configuré : le salon des bots*"
                : $"> <#{config.LevelUpChannelId}>");

        Section(container,
            "**Salon du quiz** — ses questions, deux ou trois par semaine",
            "> " + Single(config.QuizChannelId, 0, "*aucun : pas de quiz sur ce serveur*"));

        Section(container,
            "**Salon des confessions** — où `/confess` poste, anonymement",
            "> " + Single(config.ConfessChannelId, 0, "*aucun : `/confess` est fermé*"));

        Section(container,
            "**Salon des jeux gratuits** — les jeux Steam gratuits à garder",
            "> " + Single(config.FreeGamesChannelId, 0, "*aucun : pas d'annonces*"));

        Section(container,
            "**Salon principal** — bonjour du matin, vie ambiante, sondage du week-end",
            "> " + Single(config.MainChannelId, isHomeGuild ? MorningGreetingService.DefaultChannelId : 0,
                "*aucun : rien de tout ça ici*"));

        Section(container,
            "**Salons calmes** — tous silencieux, elle parle dans le vide (le principal compte)",
            isHomeGuild
                ? $"> Par défaut : {Channels(AmbientService.HardcodedIdleChannels)}\n> Ajoutés ici : {Channels(config.IdleChannels)}"
                : $"> {Channels(config.IdleChannels)}");

        Section(container, FeaturesText(config, isHomeGuild));

        Section(container,
            "**Salons sans XP** — ni XP, ni mur de la honte",
            isHomeGuild
                ? $"> Par défaut (non modifiables) : {Channels(XpTracker.HardcodedExcludedChannels)}\n> Ajoutés ici : {Channels(config.ExcludedChannels)}"
                : $"> {Channels(config.ExcludedChannels)}");

        return new ComponentBuilderV2().AddComponent(container).Build();
    }

    // What she does of her own here (ServerFeatures), and a warning when one that needs the main
    // channel is on without one.
    private static string FeaturesText(GuildConfig config, bool isHomeGuild)
    {
        var on = ServerFeatures.All.Where(f => ServerFeatures.IsOn(config.Features, f.Key, isHomeGuild)).ToList();
        var off = ServerFeatures.All.Except(on).ToList();
        string Names(IEnumerable<ServerFeature> features) =>
            features.Any() ? string.Join(", ", features.Select(f => f.Label)) : "*aucune*";

        var sb = new StringBuilder("**Fonctionnalités** — `/config feature`");
        sb.Append("\n> Allumées : ").Append(Names(on));
        sb.Append("\n> Éteintes : ").Append(Names(off));
        bool noMain = config.MainChannelId == 0 && !isHomeGuild;
        if (noMain && on.Any(f => f.NeedsMainChannel))
            sb.Append("\n> ⚠️ Sans salon principal : ").Append(Names(on.Where(f => f.NeedsMainChannel))).Append(" ne font rien.");
        return sb.ToString();
    }

    /// <summary>The « État » section: the health lines, then what is not set yet. Shared with /config setup's summary.</summary>
    public static string HealthText(IReadOnlyList<HealthLine> health, IReadOnlyList<string> unset)
    {
        var sb = new StringBuilder("**État** — ce que je peux faire ici");
        foreach (var line in health) sb.Append("\n> ").Append(line);
        if (unset.Count > 0) sb.Append("\n> Pas encore réglé : ").Append(string.Join(", ", unset)).Append(". `/config setup` s'en occupe.");
        return sb.ToString();
    }

    private static void Section(ContainerBuilder container, params string[] lines)
    {
        var sb = new StringBuilder();
        foreach (var line in lines) sb.AppendLine(line);
        container
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(sb.ToString().TrimEnd()));
    }

    // A configured channel wins; otherwise the default, when there is one here.
    private static string Single(ulong configured, ulong fallback, string none) =>
        configured != 0 ? $"<#{configured}> · *configuré*"
        : fallback != 0 ? $"<#{fallback}> · *par défaut*"
        : none;

    // Mentions rather than names: Discord resolves them client-side, so this stays correct
    // after a rename and needs no gateway lookup here.
    private static string Channels(IReadOnlyCollection<ulong> ids) =>
        ids.Count == 0 ? "*aucun*" : string.Join(" ", ids.Select(id => $"<#{id}>"));

    private static string Users(IReadOnlyCollection<ulong> ids) =>
        ids.Count == 0 ? "*aucun*" : string.Join(" ", ids.Select(id => $"<@{id}>"));
}
