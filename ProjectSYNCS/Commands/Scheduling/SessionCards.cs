using Discord;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// The session card, the one render path for it: /schedule create, the buttons, /tonight, the
// lifecycle pass, a weekly series and CardRepost all draw through here. Static and Context-free,
// so the checks can build the worst case.
//
// Components V2, so (root CLAUDE.md): no content and no embeds — ApplyTo clears both, which is
// also what turns a legacy embed card into this one on its next update —, the flag re-asserted
// on every edit, and AllowedMentions.None on every send, since the roster's <@id> are TextDisplays.
public static class SessionCards
{
    public const int FillBarWidth = 10;

    /// <summary>
    /// The card in its current state. <paramref name="organizer"/> gives the header its avatar;
    /// null (left the server, or no gateway in the checks) falls back to Discord's default one.
    /// </summary>
    /// <remarks>
    /// Budget: container 1 + section 1 + heading 1 + thumbnail 1 + separator 1 + details 1 +
    /// separator 1 + roster 1 = 8, then the answer row 1 + 3 and the second row 1 + 2 = 15 of 40.
    /// Text: title 150, description 1000 and 60 capped mentions stay far under the 4000 a V2
    /// message holds.
    /// </remarks>
    public static MessageComponent Build(SessionEvent session, IUser? organizer, DateTimeOffset? at = null)
    {
        var now = at ?? DateTimeOffset.UtcNow;
        var joined = session.Participants.Where(p => p.Status == ParticipantStatus.Joined).OrderBy(p => p.JoinedAt).ToList();
        var maybes = session.Participants.Where(p => p.Status == ParticipantStatus.Maybe).OrderBy(p => p.JoinedAt).ToList();
        var declined = session.Participants.Where(p => p.Status == ParticipantStatus.Declined).OrderBy(p => p.JoinedAt).ToList();

        bool unlimited = session.MaxPlayers == 0;
        bool full = !unlimited && joined.Count >= session.MaxPlayers;
        var phase = session.PhaseAt(now);
        bool open = !session.IsCancelled && phase == SessionPhase.Scheduled;

        var container = new ContainerBuilder()
            .WithAccentColor(Accent(session, phase, full))
            .AddComponent(new SectionBuilder()
                .WithAccessory(AvatarUi.Thumbnail(organizer))
                .AddComponent(new TextDisplayBuilder(Heading(session, phase))))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(Details(session)))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(Roster(session, joined, maybes, declined, open, full)));

        var builder = new ComponentBuilderV2().AddComponent(container);
        if (!open) return builder.Build();

        // Every row has its own verbs (root CLAUDE.md, custom-ids), and the row id stays third:
        // CardRefs recognises the card by it.
        int id = session.Id;
        builder.AddComponent(new ActionRowBuilder()
            .WithButton("Rejoindre", $"event:join:{id}", ButtonStyle.Success, new Emoji("✅"))
            .WithButton("Peut-être", $"event:sub:{id}", ButtonStyle.Secondary, new Emoji("🤔"))
            .WithButton("Refuser", $"event:decline:{id}", ButtonStyle.Danger, new Emoji("✖️")));

        var second = new ActionRowBuilder()
            .WithButton("Gérer", $"event:manage:{id}", ButtonStyle.Secondary, new Emoji("⚙️"));
        if (session.NativeEventId != 0)
            second.WithButton(new ButtonBuilder()
                .WithLabel("Événement Discord")
                .WithStyle(ButtonStyle.Link)
                .WithEmote(new Emoji("📅"))
                .WithUrl($"https://discord.com/events/{session.GuildId}/{session.NativeEventId}"));
        else
            // Organiser-only: the handler re-checks.
            second.WithButton("Événement Discord", $"event:createnative:{id}", ButtonStyle.Secondary, new Emoji("📅"));
        builder.AddComponent(second);

        return builder.Build();
    }

    /// <summary>The organiser as a gateway user, when the guild still has them.</summary>
    public static IUser? Organizer(IGuild? guild, SessionEvent session) =>
        (guild as SocketGuild)?.GetUser(session.OrganizerId);

    public static MessageComponent Build(SessionEvent session, IGuild? guild) =>
        Build(session, Organizer(guild, session));

    /// <summary>Sets a message to this card: components, the V2 flag, no content, no embeds, no pings.</summary>
    public static void ApplyTo(MessageProperties props, SessionEvent session, IGuild? guild)
    {
        props.Components = Build(session, guild);
        props.Flags = MessageFlags.ComponentsV2;
        props.Content = string.Empty;
        props.Embeds = Array.Empty<Embed>();
        props.AllowedMentions = AllowedMentions.None;
    }

    public static Task<IUserMessage> SendAsync(IMessageChannel channel, SessionEvent session, IGuild? guild) =>
        channel.SendMessageAsync(components: Build(session, guild), flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);

    /// <summary>The card's message link, for DMs that send people to it.</summary>
    public static string Link(SessionEvent session) =>
        $"https://discord.com/channels/{session.GuildId}/{session.ChannelId}/{session.MessageId}";

    private static Color Accent(SessionEvent session, SessionPhase phase, bool full)
    {
        if (session.IsCancelled) return Color.DarkRed;
        return phase switch
        {
            SessionPhase.InProgress => Color.Gold,
            SessionPhase.Finished => Color.DarkGrey,
            _ => full ? Color.Orange : Color.Green,
        };
    }

    private static string Heading(SessionEvent session, SessionPhase phase)
    {
        var title = Format.Sanitize(session.Title);
        var emoji = SessionCategories.Emoji(session.Category);
        var top = session.IsCancelled
            ? $"### ~~{title}~~ — ANNULÉE"
            : phase switch
            {
                SessionPhase.InProgress => $"### 🔴 EN COURS — {emoji} {title}",
                SessionPhase.Finished => $"### ✅ TERMINÉE — {emoji} {title}",
                _ => $"### {emoji} {title}",
            };
        var sub = $"-# {SessionCategories.Name(session.Category)} · par <@{session.OrganizerId}>";
        if (session.RecurringSessionId is not null) sub += $" · {Recurrence.Badge(session.ScheduledAt)}";
        return $"{top}\n{sub}";
    }

    private static string Details(SessionEvent session)
    {
        var ts = session.ScheduledAt.ToUnixTimeSeconds();
        var lines = new List<string>
        {
            $"📅 <t:{ts}:F> · <t:{ts}:R>",
            session.VoiceChannelId != 0
                ? $"⏱️ {SessionTime.DurationLabel(session.DurationMinutes)} · 🔊 <#{session.VoiceChannelId}>"
                : $"⏱️ {SessionTime.DurationLabel(session.DurationMinutes)}",
        };
        if (!string.IsNullOrWhiteSpace(session.Description))
            lines.Add(Format.Sanitize(session.Description.Trim()));
        return string.Join("\n", lines);
    }

    private static string Roster(SessionEvent session, List<Participant> joined, List<Participant> maybes,
        List<Participant> declined, bool open, bool full)
    {
        // Capped (SessionSeats.MaxCardNames): a crowd can't push the card past Discord's limits.
        static string Names(List<Participant> people) =>
            SessionSeats.Mentions(people.Select(p => p.UserId).ToList(), SessionSeats.MaxCardNames, ", ");

        var lines = new List<string>();
        if (session.MaxPlayers == 0)
            lines.Add($"**Inscrits : {joined.Count}**");
        else
            lines.Add($"**{(full ? "Complet" : "Inscrits")} {joined.Count}/{session.MaxPlayers}**  "
                + $"`{TextChart.Bar(joined.Count, session.MaxPlayers, FillBarWidth)}`");
        lines.Add(joined.Count > 0 ? Names(joined) : "-# Personne pour l'instant.");
        if (maybes.Count > 0) lines.Add($"**Peut-être ({maybes.Count})** : {Names(maybes)}");
        if (declined.Count > 0) lines.Add($"**Absents ({declined.Count})** : {Names(declined)}");
        if (open && full) lines.Add("-# Complet : « Peut-être » est la liste d'attente, je préviens par DM quand une place se libère.");
        lines.Add($"-# ID de la session : {session.Id}");
        return string.Join("\n", lines);
    }
}
