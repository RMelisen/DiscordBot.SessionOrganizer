using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// « Grosse tête de la semaine » (docs/agents/quiz.md): every Monday at 10:00 Paris, the best answerer
// of the calendar week that just ended gets the configured role for the whole week and a purse of
// cailloux, and SYNCS says so in the quiz channel, pinging them. Off until staff set a role
// (`/config quiz-role`).
//
// Driven by QuizMasterService's 1-minute tick (no loop of its own): the first tick past Monday 10:00
// saves the week as crowned, pays the purse, gives the role and announces; between Mondays an hourly pass keeps the
// role mirrored (a hand-made change, a rejoin). The role itself is applied by RoleMirror.
//
// Singleton: it holds the hourly gate. It scopes per unit of work and never injects a DB service.
public sealed class QuizCrownService
{
    public const string DefaultName = "Grosse tête de la semaine";

    // Weekly lines: ResponsePicker's history would never survive a week of restarts, so DailyRotation,
    // keyed by a week index (day number / 7) — it must step by one per pick for the rotation to avoid
    // repeats. Changing the salt reshuffles the walk.
    private const ulong CrownSalt = 0x5359_4E43_5343_5257; // "SYNCSCRW"

    // The mirror between Mondays, per guild. Its own gate, shared with nothing.
    private readonly CooldownGate<ulong> _mirrorGate = new(TimeSpan.FromHours(1));

    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly RoleMirror _mirror;
    private readonly ILogger<QuizCrownService> _logger;
    private readonly BadgeAwarder _badges;

    public QuizCrownService(IServiceProvider services, GuildConfigService config, RoleMirror mirror,
        ILogger<QuizCrownService> logger, BadgeAwarder badges)
    {
        _badges = badges;
        _services = services;
        _config = config;
        _mirror = mirror;
        _logger = logger;
    }

    /// <summary>Who holds the crown right now: the champion(s) of the week reigning at this instant.</summary>
    public async Task<HashSet<ulong>> LoadHoldersAsync(ulong guildId) =>
        (await LoadChampionsAsync(guildId, QuizCrown.RewardedWeekStart(DateTimeOffset.UtcNow)))
            .Select(e => e.UserId).ToHashSet();

    /// <summary>
    /// One tick for one guild. Called inside QuizMasterService's per-guild try; may throw on a
    /// database error, which that try logs.
    /// </summary>
    public async Task TickAsync(SocketGuild guild, DateTimeOffset now)
    {
        var config = await _config.GetAsync(guild.Id);
        if (config.QuizRoleId == 0) return;   // off until staff set a role

        int week = QuizCrown.RewardedWeekStart(now);
        if (week > config.QuizCrownedWeek)
        {
            // Saved first: a crash or a failed post after this is logged, never announced twice.
            await _config.SetQuizCrownedWeekAsync(guild.Id, week);
            await CrownAsync(guild, config, week, pay: true);
            return;
        }

        if (_mirrorGate.TryClaim(guild.Id))
            await _mirror.MirrorGuildAsync(guild, RoleKind.QuizCrown, config.QuizRoleId, () => LoadHoldersAsync(guild.Id));
    }

    /// <summary>
    /// <c>/debug quiz action:Crown</c>: crowns the last finished week now, announcement included,
    /// without reading or writing the once-a-week marker — so without the purse, or every test would
    /// pay it again. Returns a French report.
    /// </summary>
    public async Task<string> CrownNowAsync(SocketGuild guild)
    {
        var config = await _config.GetAsync(guild.Id);
        if (config.QuizRoleId == 0)
            return "Pas de rôle Grosse tête ici : `/config quiz-role create` d'abord.";

        int week = QuizCrown.RewardedWeekStart(DateTimeOffset.UtcNow);
        var champions = await CrownAsync(guild, config, week, pay: false);
        var since = $"<t:{AppTime.AtWallClock(AppTime.FromDayKey(week)).ToUnixTimeSeconds()}:D>";
        return champions.Count == 0
            ? $"Aucune bonne réponse la semaine du {since}. Personne n'a le rôle."
            : $"Couronné pour la semaine du {since} : {QuizCrown.JoinMentions(champions.Select(c => c.UserId).ToList())} (sans la bourse).";
    }

    private async Task<IReadOnlyList<CrownEntry>> LoadChampionsAsync(ulong guildId, int weekStart)
    {
        var (from, to) = QuizCrown.WeekRange(weekStart);
        await using var scope = _services.CreateAsyncScope();
        var wins = await scope.ServiceProvider.GetRequiredService<QuizService>().GetWeekWinsAsync(guildId, from, to);
        return QuizCrown.Champions(wins);
    }

    private async Task<IReadOnlyList<CrownEntry>> CrownAsync(SocketGuild guild, GuildConfig config, int week, bool pay)
    {
        var champions = await LoadChampionsAsync(guild.Id, week);
        var holders = champions.Select(c => c.UserId).ToHashSet();

        // The purse is paid even when the role is broken or nobody is told: it rewards the week, not
        // the announcement. A failed payment is logged and the announcement says nothing of it.
        long purse = 0;
        if (pay && champions.Count > 0)
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                await scope.ServiceProvider.GetRequiredService<QuizService>()
                    .PayCrownAsync(guild.Id, holders.ToList(), QuizCrown.Purse, DateTimeOffset.UtcNow);
                purse = QuizCrown.Purse;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to pay the quiz crown purse in guild {GuildId}.", guild.Id);
            }
        }

        // The real Monday crowning only (the week was saved as crowned before this): /debug's dry run
        // (pay: false) earns nothing. Awarded even if the purse failed — the crown was still worn.
        if (pay)
            foreach (var id in holders) _badges.Award(guild.Id, id, MemberBadges.Keys.Crowned);

        await _mirror.MirrorGuildAsync(guild, RoleKind.QuizCrown, config.QuizRoleId, () => Task.FromResult(holders));

        // « Le rôle est à toi » would be a lie with a role she can't hand out (deleted, above hers):
        // no announcement until staff fix it — /config show and the log say why.
        bool roleWorks = RoleMirror.Problem(guild, guild.GetRole(config.QuizRoleId), adopting: false, RoleKind.QuizCrown) is null;
        if (champions.Count > 0 && roleWorks) await AnnounceAsync(guild, config, champions, week, purse);
        return champions;
    }

    private async Task AnnounceAsync(SocketGuild guild, GuildConfig config, IReadOnlyList<CrownEntry> champions, int week,
        long purse)
    {
        // No quiz channel (or one she can't find): the role still moves, nobody is told.
        if (config.QuizChannelId == 0 || guild.GetTextChannel(config.QuizChannelId) is not { } channel) return;

        int weekIndex = AppTime.DayNumber(AppTime.AtWallClock(AppTime.FromDayKey(week))) / 7;
        var ids = champions.Select(c => c.UserId).ToList();
        string line;
        if (champions.Count > 1)
        {
            line = string.Format(DailyRotation.Pick(BotResponses.QuizCrownSharedLines, weekIndex, CrownSalt),
                QuizCrown.JoinMentions(ids));
        }
        else
        {
            var pool = ids[0] == AvailabilityService.OwnerId ? BotResponses.QuizCrownOwnerLines : BotResponses.QuizCrownLines;
            line = string.Format(DailyRotation.Pick(pool, weekIndex, CrownSalt), $"<@{ids[0]}>", QuizCrown.WinsText(champions[0].Wins));
        }
        // After the line's sticker tag: BotChat strips a tag wherever it sits.
        line += QuizCrown.PurseText(purse, shared: champions.Count > 1);

        // Meant to ping: the champions and nobody else — never the role, never everyone.
        await BotChat.PostWithTypingAsync(channel, line, _logger, "quiz crown", new AllowedMentions { UserIds = ids });
    }
}
