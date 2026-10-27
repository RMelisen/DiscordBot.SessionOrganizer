using System.Globalization;
using System.Text;
using Discord;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Commands;

// The poll and vote card (both kinds, branching on Poll.Kind), the one render path for it: the
// wizards, the buttons, the Gérer panel, auto-close, her weekend poll and CardRepost all draw
// through here. Static and Context-free, so the checks can build the worst case.
//
// Components V2, like the session card: ApplyTo clears content and embeds (which converts a legacy
// embed card on its next update), re-asserts the flag, and silences the voters' <@id>.
public static class PollCards
{
    public const int FillBarWidth = 10;

    // Options named in a closed card's tie line.
    private const int MaxTieNamed = 3;

    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    /// <summary>
    /// The card in its current state. <paramref name="sessionLink"/> is the card of the session made
    /// from it, when the caller has it; a linked poll drawn without it gets a disabled button.
    /// </summary>
    /// <remarks>
    /// Budget: container 1 + heading 1 + separator 1 + options 1 + separator 1 + footer 1 = 6, then
    /// ten option buttons on two rows (2 + 10) and the last row (1 + 2) = 21 of 40. Text: ten labels
    /// of 80 with their bars and <see cref="SessionSeats.MaxPollVoterNames"/> mentions each stay
    /// under the 4000 a V2 message holds; the checks build that case.
    /// </remarks>
    public static MessageComponent Build(Poll poll, string? sessionLink = null, DateTimeOffset? at = null)
    {
        var now = at ?? DateTimeOffset.UtcNow;
        var ordered = PollRules.Ordered(poll);
        var leaders = PollRules.Leaders(poll).Select(o => o.Id).ToHashSet();
        int voters = PollRules.VoterCount(poll);

        var container = new ContainerBuilder()
            .WithAccentColor(poll.IsClosed ? Color.DarkGrey : Color.Purple)
            .AddComponent(new TextDisplayBuilder(Heading(poll)))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(Options(poll, ordered, leaders, voters)))
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(Footer(poll, ordered, leaders, voters)));

        var builder = new ComponentBuilderV2().AddComponent(container);

        if (!poll.IsClosed)
        {
            // Up to five vote buttons per row; ten options fit in two. One verb per row is not
            // needed here: every button carries its own option id, so none repeats.
            ActionRowBuilder? row = null;
            foreach (var o in ordered)
            {
                if (row is null || row.Components.Count == 5)
                {
                    if (row is not null) builder.AddComponent(row);
                    row = new ActionRowBuilder();
                }
                var label = ButtonLabel(poll, o);
                row.WithButton(label, $"poll:vote:{poll.Id}:{o.Id}",
                    leaders.Contains(o.Id) ? ButtonStyle.Primary : ButtonStyle.Secondary);
            }
            if (row is not null) builder.AddComponent(row);

            builder.AddComponent(new ActionRowBuilder()
                .WithButton("Mes votes", $"poll:mine:{poll.Id}", ButtonStyle.Secondary, new Emoji("🗳️"))
                .WithButton("Gérer", $"poll:manage:{poll.Id}", ButtonStyle.Secondary, new Emoji("⚙️")));
            return builder.Build();
        }

        var last = new ActionRowBuilder();
        if (poll.Kind == PollKind.DateSlots)
        {
            if (sessionLink is not null)
                last.WithButton(new ButtonBuilder().WithLabel("Voir la session").WithStyle(ButtonStyle.Link)
                    .WithEmote(new Emoji("🗓️")).WithUrl(sessionLink));
            else if (poll.SessionEventId is not null)
                last.WithButton("Session créée", $"poll:tosession:{poll.Id}", ButtonStyle.Secondary, new Emoji("🗓️"),
                    disabled: true);
            // A closed date poll can become a session while one of its slots is still ahead.
            else if (poll.Options.Any(o => o.ScheduledAt > now))
                last.WithButton("Créer une session", $"poll:tosession:{poll.Id}", ButtonStyle.Success, new Emoji("🗓️"));
        }
        last.WithButton("Mes votes", $"poll:mine:{poll.Id}", ButtonStyle.Secondary, new Emoji("🗳️"));
        builder.AddComponent(last);
        return builder.Build();
    }

    /// <summary>Sets a message to this card: components, the V2 flag, no content, no embeds, no pings.</summary>
    public static void ApplyTo(MessageProperties props, Poll poll, string? sessionLink = null)
    {
        props.Components = Build(poll, sessionLink);
        props.Flags = MessageFlags.ComponentsV2;
        props.Content = string.Empty;
        props.Embeds = Array.Empty<Embed>();
        props.AllowedMentions = AllowedMentions.None;
    }

    public static Task<IUserMessage> SendAsync(IMessageChannel channel, Poll poll) =>
        channel.SendMessageAsync(components: Build(poll), flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);

    /// <summary>
    /// The button under her reply to a closed date poll: the card's « Créer une session », under its
    /// own verb (<c>poll:plan</c>) so the reply never reads as the card (<see cref="CardRefs"/>).
    /// </summary>
    public static MessageComponent PlanButton(int pollId) =>
        new ComponentBuilder()
            .WithButton("Créer une session", $"poll:plan:{pollId}", ButtonStyle.Success, new Emoji("🗓️"))
            .Build();

    /// <summary>How one option reads on the card and in her lines: a date as a timestamp, a label sanitised.</summary>
    public static string OptionText(Poll poll, PollOption option) =>
        poll.Kind == PollKind.Text
            ? $"**{Format.Sanitize(option.Label)}**"
            : $"<t:{option.ScheduledAt.ToUnixTimeSeconds()}:F>";

    /// <summary>A slot as a short button or select label: « Sam 01/11 21:00 ».</summary>
    public static string ShortLabel(DateTimeOffset slot)
    {
        var zoned = AppTime.ToZoned(slot);
        return Fr.TextInfo.ToTitleCase(zoned.ToString("ddd dd/MM HH:mm", Fr));
    }

    public static string ButtonLabel(Poll poll, PollOption option)
    {
        // Discord caps button labels at 80 characters.
        var label = poll.Kind == PollKind.Text ? option.Label : ShortLabel(option.ScheduledAt);
        return label.Length > 80 ? label[..77] + "…" : label;
    }

    private static string Heading(Poll poll)
    {
        var icon = poll.Kind == PollKind.Text ? "🗳️" : "📊";
        var tags = new List<string> { $"par <@{poll.OrganizerId}>" };
        tags.Add(poll.SingleChoice ? "un seul choix" : "plusieurs choix possibles");
        if (poll.Anonymous) tags.Add("anonyme");
        return $"### {icon} {Format.Sanitize(poll.Title)}\n-# {string.Join(" · ", tags)}";
    }

    private static string Options(Poll poll, List<PollOption> ordered, HashSet<int> leaders, int voters)
    {
        if (ordered.Count == 0) return "-# Aucune option.";
        var sb = new StringBuilder();
        foreach (var o in ordered)
        {
            int n = o.Votes.Count;
            if (sb.Length > 0) sb.Append('\n');
            sb.Append(leaders.Contains(o.Id) ? "🏆 " : "▫️ ").Append(OptionText(poll, o)).Append('\n');
            // Percent of the people who voted, so a multi-choice poll can show 100 % twice.
            sb.Append($"`{TextChart.Bar(n, voters, FillBarWidth)}` **{n}** · {TextChart.Percent(n, voters)}");
            if (!poll.Anonymous && n > 0)
                sb.Append('\n').Append(SessionSeats.Mentions(o.Votes.OrderBy(v => v.VotedAt).Select(v => v.UserId).ToList(),
                    SessionSeats.MaxPollVoterNames, " "));
        }
        return sb.ToString();
    }

    private static string Footer(Poll poll, List<PollOption> ordered, HashSet<int> leaders, int voters)
    {
        var who = voters switch { 0 => "Personne n'a voté", 1 => "1 votant", _ => $"{voters} votants" };
        if (!poll.IsClosed)
            return $"-# {who} · se termine <t:{poll.ClosesAt.ToUnixTimeSeconds()}:R> · ID {poll.Id}";

        var winners = ordered.Where(o => leaders.Contains(o.Id)).ToList();
        // A tie names three at most: the 🏆 in the list above already marks every one.
        var named = string.Join(" · ", winners.Take(MaxTieNamed).Select(o => OptionText(poll, o)));
        var result = winners.Count switch
        {
            0 => "Clôturé sans un vote.",
            1 => $"**Retenu** : {OptionText(poll, winners[0])}",
            <= MaxTieNamed => $"**Égalité** : {named}",
            _ => $"**Égalité** : {named} · et {winners.Count - MaxTieNamed} autre(s)",
        };
        return $"{result}\n-# Clôturé · {who} · ID {poll.Id}";
    }
}
