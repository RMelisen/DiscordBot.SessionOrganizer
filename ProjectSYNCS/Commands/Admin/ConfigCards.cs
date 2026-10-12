using System.Text;
using Discord;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /config show — one Components V2 card, a section per setting. Static and Context-free, so
// its size is checkable without a gateway: 1 container + 1 title + 7 × (separator + text)
// = 16 of the 40 components.
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
    public static MessageComponent Build(GuildConfig config, bool isHomeGuild, bool defaultGameChannelHere)
    {
        var container = new ContainerBuilder()
            .WithAccentColor(Color.Purple)
            .AddComponent(new TextDisplayBuilder("## Configuration du serveur"));

        Section(container,
            "**Rôle modérateur** — peut voter avec `/shame`",
            config.ModeratorRoleId == 0 ? "> *non configuré*" : $"> <@&{config.ModeratorRoleId}>");

        Section(container,
            "**Votants `/shame`** — en plus du staff et du rôle modérateur",
            $"> Par défaut : {Users(ShameModule.HardcodedExtraVoters)}",
            $"> Ajoutés ici : {Users(config.ShameVoters)}");

        Section(container,
            "**Salon de jeu** — morts, retours et histoires des Plynlings",
            "> " + Single(config.GameChannelId,
                defaultGameChannelHere ? PlynlingAnnouncer.DefaultGameChannelId : 0,
                "*aucun : pas d'annonces sur ce serveur*"));

        Section(container,
            "**Salon du quiz** — où elle pose ses questions, jusqu'à deux fois par jour",
            "> " + Single(config.QuizChannelId, 0, "*aucun : pas de quiz sur ce serveur*"));

        if (isHomeGuild)
        {
            Section(container,
                "**Salon principal** — bonjour du matin, ligne de 3 h, retour après un redémarrage",
                "> " + Single(config.MainChannelId, MorningGreetingService.DefaultChannelId, "*aucun*"));

            Section(container,
                "**Salons calmes** — s'ils se taisent tous, elle parle dans le vide (le salon principal compte toujours)",
                $"> Par défaut : {Channels(AmbientService.HardcodedIdleChannels)}",
                $"> Ajoutés ici : {Channels(config.IdleChannels)}");
        }
        else
        {
            Section(container,
                "**Salon principal et salons calmes**",
                "> *seulement sur le serveur principal*");
        }

        Section(container,
            "**Salons sans XP** — ni XP, ni mur de la honte",
            $"> Par défaut (non modifiables) : {Channels(XpTracker.HardcodedExcludedChannels)}",
            $"> Ajoutés ici : {Channels(config.ExcludedChannels)}");

        return new ComponentBuilderV2().AddComponent(container).Build();
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
