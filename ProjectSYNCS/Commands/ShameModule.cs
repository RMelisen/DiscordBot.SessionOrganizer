using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The wall of shame. Two titles on one page: "Le Malfaisant", earned by being hostile
// and counted by ShameTracker, and "Le Banni", voted by people one vote a day through
// this same command.
//
// One flat command with an optional `user` option rather than a group with
// subcommands, because Discord will not let a parent with subcommands be invoked bare
// — `/shame voir` and `/shame vote` would mean plain `/shame` no longer exists, and the
// board is the thing people open ten times for every vote cast.
//
// An embed rather than Components V2, unlike /level and /leaderboard: the wall ranks
// six names with no avatars, no levels and no podium art, so the flag would buy nothing
// and cost the 40-component budget. Same reasoning /emotestats and /goodbot stayed
// embeds.
//
// Guild-only: this module reads Context.Guild, which is null in a DM, and config.yaml
// ships register_globally: true (a global command is DM-enabled by default).
[CommandContextType(InteractionContextType.Guild)]
public class ShameModule : InteractionModuleBase<SocketInteractionContext>
{
    // The wall opens on 30 days. A title is a standing, but "Le Malfaisant" starts at
    // zero the day this ships and both counters are sparse, so all-time would read as
    // a permanent hall of fame that nobody can move — a month is long enough to have
    // names in it and short enough that the wall still changes.
    private const StatsPeriod DefaultPeriod = StatsPeriod.Month;

    // Fixed lines, deliberately not ResponsePicker pools: the pools exist so repeated
    // *chatter* doesn't repeat itself, and a command refusal is not chatter. Same call
    // as /level's refusal for a bot.
    private const string RefusalSelfTarget =
        "Non. Tu ne me mets pas au mur. C'est mon mur ദ്ദി◝ ⩊ ◜.ᐟ";

    private const string RefusalBotTarget =
        "Les autres bots n'ont pas d'honneur à perdre. Garde ton vote pour un vrai coupable ✨";

    private const string RefusalAlreadyVoted =
        "Tu as déjà voté aujourd'hui. Un par jour, c'est ce qui fait qu'il compte (ᵕ • ᴗ •)";

    private readonly ShameService _shame;
    private readonly ResponsePicker _picker;

    public ShameModule(ShameService shame, ResponsePicker picker)
    {
        _shame = shame;
        _picker = picker;
    }

    [SlashCommand("shame", "Le mur de la honte — ou dénonce quelqu'un pour l'y mettre")]
    public async Task ShameAsync(
        [Summary("user", "La personne à dénoncer. Laisse vide pour voir le mur.")]
        SocketUser? user = null)
    {
        if (user is not null)
        {
            await VoteAsync(user);
            return;
        }

        await DeferAsync();
        await FollowupAsync(embed: await BuildWallAsync(DefaultPeriod), components: BuildComponents(DefaultPeriod));
    }

    // Only one button row exists on this message, so nothing can collide with it — but
    // the id still carries its own verb (`:win:`) rather than being bare, because the
    // day a second row is added the collision is silent, instant, and rejected by
    // Discord as COMPONENT_CUSTOM_ID_DUPLICATED. StatsPeriodUi appends the page
    // segment, which this board has no use for and ignores.
    [ComponentInteraction("shame:win:*:*", ignoreGroupNames: true)]
    public async Task OnPeriodAsync(string periodStr, string _)
    {
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = DefaultPeriod;

        var embed = await BuildWallAsync(period);
        var components = BuildComponents(period);

        var component = (SocketMessageComponent)Context.Interaction;
        await component.UpdateAsync(m =>
        {
            m.Embed = embed;
            m.Components = components;
        });
    }

    private async Task VoteAsync(SocketUser target)
    {
        // Refusals are ephemeral: only the person who tried needs to know, and a public
        // "you can't do that" would be a second message about a vote that never
        // happened.
        if (target.Id == Context.Client.CurrentUser.Id)
        {
            await RespondAsync(RefusalSelfTarget, ephemeral: true);
            return;
        }

        if (target.IsBot)
        {
            await RespondAsync(RefusalBotTarget, ephemeral: true);
            return;
        }

        await DeferAsync();

        var result = await _shame.TryVoteAsync(Context.Guild.Id, Context.User.Id, target.Id);
        if (result == ShameVoteResult.AlreadyVotedToday)
        {
            await FollowupAsync(RefusalAlreadyVoted, ephemeral: true);
            return;
        }

        var voterName = NameOf(Context.User);
        var targetName = NameOf(target);

        // Self-shaming is allowed and gets its own pool: the joke is that it cost them
        // the only vote they had, which a line about two different people cannot make.
        var line = Context.User.Id == target.Id
            ? string.Format(_picker.Pick(Context.Channel.Id, BotResponses.ShameSelfVoteLines), voterName)
            : string.Format(_picker.Pick(Context.Channel.Id, BotResponses.ShameVoteLines), voterName, targetName);

        // Names are rendered as text, not mentions: the announcement is public and
        // pinging the person you just shamed turns a joke into a notification.
        await FollowupAsync(line, allowedMentions: AllowedMentions.None);
    }

    private async Task<Embed> BuildWallAsync(StatsPeriod period)
    {
        var wall = await _shame.GetWallAsync(Context.Guild.Id, period);

        return new EmbedBuilder()
            .WithTitle($"Le mur de la honte — {StatsPeriodUi.Label(period)}")
            .WithColor(Color.DarkRed)
            .AddField(
                $"{Emotes.GooseKnife} Le Malfaisant",
                Section(wall.Malfaisants, BotResponses.ShameEmptyMalfaisant, "méchanceté", "méchancetés"))
            .AddField(
                $"{Emotes.PrisonerFlat} Le Banni",
                Section(wall.Bannis, BotResponses.ShameEmptyBanni, "vote", "votes"))
            .WithFooter("Un vote par personne et par jour — `/shame user`")
            .Build();
    }

    private MessageComponent BuildComponents(StatsPeriod period) =>
        new ComponentBuilder().AddFilterRow("shame:win", period).Build();

    // A title with nobody in it still renders, with a line in her voice instead of the
    // ranking. Hiding it would make the wall change shape between filters, and there is
    // no minimum count: a window holding one vote must show that vote, or the vote
    // looks like it vanished.
    private string Section(
        IReadOnlyList<ShameTally> rows, string[] emptyPool, string unit, string units) =>
        rows.Count == 0
            ? $"*{_picker.Pick(Context.Channel.Id, emptyPool)}*"
            : string.Join("\n", rows.Select((t, i) =>
            {
                // Mentions render as a name without pinging: the embed carries no
                // AllowedMentions of its own, so the wall never notifies the people on it.
                var count = $"**{t.Count}** {(t.Count == 1 ? unit : units)}";
                return $"{LevelCardUi.RankMarker(i + 1)} <@{t.UserId}> — {count}";
            }));

    private static string NameOf(IUser user) => BotResponses.DisplayNameFor(
        user.Id, (user as SocketGuildUser)?.Nickname ?? user.GlobalName ?? user.Username);
}
