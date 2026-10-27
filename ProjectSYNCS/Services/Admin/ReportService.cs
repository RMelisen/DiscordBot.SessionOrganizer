using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>The reports, by name. Used in custom-ids by name: append only, never rename.</summary>
public enum ReportKind
{
    Sessions,
    Polls,
    Community,
    Syncs,
    Activity,
    Commands,
    Quiz,
    Plynlings,
    Emotes,
    Sleeping,
    Minecraft,
}

/// <summary>
/// Which reports each monthly digest sends after its « Le mois en bref », in order. Papa's is about
/// her running well (and her evenings in Minecraft, left out for a month she didn't play), Tata's about the server's
/// life; the others stay on demand.
/// </summary>
public static class ReportKinds
{
    public static readonly IReadOnlyList<ReportKind> OwnerDigest = new[] { ReportKind.Syncs, ReportKind.Commands, ReportKind.Quiz, ReportKind.Minecraft };

    public static readonly IReadOnlyList<ReportKind> TataDigest = new[]
    {
        ReportKind.Community, ReportKind.Sessions, ReportKind.Activity, ReportKind.Sleeping,
    };

    public static IReadOnlyList<ReportKind> Digest(DigestRecipient recipient) =>
        recipient == DigestRecipient.Tata ? TataDigest : OwnerDigest;
}

/// <summary>
/// One report as an embed, whoever asks: /report, its period buttons and the monthly digest all go
/// through here, so they can't drift apart.
/// </summary>
/// <remarks>
/// Transient: it wraps the transient report services. The live guild (channels, emotes, members,
/// registered commands) is turned into plain records here, so the helpers stay pure; when the guild
/// is unavailable (gateway down) those snapshots are empty and the reports render without them.
/// </remarks>
public class ReportService
{
    private readonly SchedulingReportService _scheduling;
    private readonly CommunityReportService _community;
    private readonly SyncsReportService _syncs;
    private readonly ActivityReportService _activity;
    private readonly QuizReportService _quiz;
    private readonly PlynlingReportService _plynlings;
    private readonly EmoteReportService _emotes;
    private readonly MinecraftDataService _minecraft;
    private readonly DiscordSocketClient _client;
    private readonly InteractionService _interactions;
    private readonly GuildConfigService _config;

    public ReportService(SchedulingReportService scheduling, CommunityReportService community, SyncsReportService syncs,
        ActivityReportService activity, QuizReportService quiz, PlynlingReportService plynlings, EmoteReportService emotes,
        MinecraftDataService minecraft,
        DiscordSocketClient client, InteractionService interactions, GuildConfigService config)
    {
        _scheduling = scheduling;
        _community = community;
        _syncs = syncs;
        _activity = activity;
        _quiz = quiz;
        _plynlings = plynlings;
        _emotes = emotes;
        _minecraft = minecraft;
        _client = client;
        _interactions = interactions;
        _config = config;
    }

    public async Task<Embed> BuildAsync(ReportKind kind, ulong guildId, ReportWindow window, bool names)
    {
        var now = DateTimeOffset.UtcNow;
        var who = NamesIn(guildId);
        return kind switch
        {
            ReportKind.Sessions => ReportCards.BuildSessions(await _scheduling.GetSessionsAsync(guildId, window, now), window, names, who),
            // Her Wednesday poll is the one she organised herself.
            ReportKind.Polls => ReportCards.BuildPolls(
                await _scheduling.GetPollsAsync(guildId, window, _client.CurrentUser?.Id ?? 0), window, names, who),
            ReportKind.Community => ReportCards.BuildCommunity(await _community.GetAsync(guildId, window), window, names, who),
            ReportKind.Syncs => ReportCards.BuildSyncs(await _syncs.GetAsync(guildId, window, now), window, await _syncs.RecordAsync(), names, who),
            ReportKind.Activity => ReportCards.BuildActivity(
                await _activity.GetActivityAsync(guildId, window, await ChannelsAsync(guildId)), window, names),
            ReportKind.Commands => ReportCards.BuildCommands(
                await _activity.GetCommandsAsync(guildId, window, RegisteredCommands()), window, names),
            ReportKind.Quiz => ReportCards.BuildQuiz(await _quiz.GetAsync(guildId, window), window, names, who),
            ReportKind.Plynlings => ReportCards.BuildPlynlings(await _plynlings.GetAsync(guildId, window, now), window, names, who),
            ReportKind.Emotes => ReportCards.BuildEmotes(await _emotes.GetAsync(guildId, window, ServerEmotes(guildId)), window, names),
            ReportKind.Minecraft => ReportCards.BuildMinecraft(await _minecraft.ReportAsync(window), window, names),
            // No window: a stray period button on it opens the default silence.
            ReportKind.Sleeping => await BuildSleepingAsync(guildId, SleepingReport.Choices[0]),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "No report for this kind."),
        };
    }

    /// <summary>
    /// A report with nothing worth a digest message: only Minecraft, for a month she didn't play. Everything else is
    /// sent even when quiet, since a quiet month says something too.
    /// </summary>
    public async Task<bool> IsEmptyAsync(ReportKind kind, ReportWindow window) =>
        kind == ReportKind.Minecraft && !(await _minecraft.ReportAsync(window)).Current.Played;

    /// <summary>/report sleeping: no window, a silence of <paramref name="days"/> days.</summary>
    public async Task<Embed> BuildSleepingAsync(ulong guildId, int days) =>
        ReportCards.BuildSleeping(await SleepingAsync(guildId, days), NamesIn(guildId));

    /// <summary>
    /// The monthly digest's first message for <paramref name="recipient"/>: the month in a few figures
    /// and « À regarder » (DigestBrief), or <paramref name="quietLine"/> when nothing needs a look.
    /// </summary>
    public async Task<Embed> BuildBriefAsync(DigestRecipient recipient, ulong guildId, ReportWindow window, string quietLine)
    {
        var now = DateTimeOffset.UtcNow;
        if (recipient == DigestRecipient.Owner)
        {
            var commands = await _activity.GetCommandsAsync(guildId, window, RegisteredCommands());
            var syncs = await _syncs.GetAsync(guildId, window, now);
            var quiz = await _quiz.GetAsync(guildId, window);
            var minecraft = await _minecraft.ReportAsync(window);
            var alerts = DigestBrief.Owner(commands, syncs, await _syncs.RecordAsync(), quiz, window);
            return ReportCards.BuildOwnerBrief(commands, syncs, quiz, minecraft, alerts, window, quietLine);
        }

        var community = await _community.GetAsync(guildId, window);
        var sessions = await _scheduling.GetSessionsAsync(guildId, window, now);
        var activity = await _activity.GetActivityAsync(guildId, window, await ChannelsAsync(guildId));
        var sleeping = await SleepingAsync(guildId, SleepingReport.Choices[0]);
        var emotes = await _emotes.GetAsync(guildId, window, ServerEmotes(guildId));
        return ReportCards.BuildTataBrief(community, sessions, activity.Current, sleeping, emotes.Current.UnusedOwn.Count,
            DigestBrief.Tata(community, sleeping), window, quietLine, NamesIn(guildId));
    }

    // Who someone is, written out for ReportCards.Who: the member as the server knows them, else the
    // user as Discord does (gone from the server), else null and the card keeps the mention alone.
    private Func<ulong, string?> NamesIn(ulong guildId)
    {
        var guild = _client.GetGuild(guildId);
        return id => guild?.GetUser(id) is { } member ? BotResponses.DisplayNameFor(member)
            : _client.GetUser(id) is { } user ? BotResponses.DisplayNameFor(user)
            : null;
    }

    private async Task<SleepingReportData> SleepingAsync(ulong guildId, int days)
    {
        var members = _client.GetGuild(guildId)?.Users.Select(u => new MemberSnapshot(u.Id, u.JoinedAt, u.IsBot)).ToList()
            ?? new List<MemberSnapshot>();
        var last = await _community.LastActiveDaysAsync(guildId);
        return SleepingReport.Compute(members, last, days, DateTimeOffset.UtcNow);
    }

    // The guild's text and voice channels as plain data, with the excluded ones marked (the hardcoded
    // floor plus /config's additions). Threads, voice-room chats and the AFK room are left out.
    private async Task<IReadOnlyList<ChannelInfo>> ChannelsAsync(ulong guildId)
    {
        if (_client.GetGuild(guildId) is not { } guild) return Array.Empty<ChannelInfo>();
        var added = (await _config.GetAsync(guildId)).ExcludedChannels;
        bool Excluded(ulong id) => XpTracker.HardcodedExcludedChannels.Contains(id) || added.Contains(id);
        var text = guild.TextChannels
            .Where(c => c is not SocketVoiceChannel && c is not SocketThreadChannel)
            .Select(c => new ChannelInfo(c.Id, false, Excluded(c.Id)));
        var voice = guild.VoiceChannels
            .Where(c => c.Id != guild.AFKChannel?.Id)
            .Select(c => new ChannelInfo(c.Id, true, Excluded(c.Id)));
        return text.Concat(voice).ToList();
    }

    // Every slash path registered now, /pl folded into /plynling (CommandPath).
    private IReadOnlyCollection<string> RegisteredCommands() =>
        _interactions.SlashCommands.Select(CommandPath.Of).ToHashSet();

    // The guild's own emotes as plain data.
    private IReadOnlyList<ServerEmote> ServerEmotes(ulong guildId) =>
        _client.GetGuild(guildId)?.Emotes.Select(e => new ServerEmote(e.Id, e.ToString())).ToList()
        ?? (IReadOnlyList<ServerEmote>)Array.Empty<ServerEmote>();
}
