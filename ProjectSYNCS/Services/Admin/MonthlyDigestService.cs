using System.Text;
using System.Text.Json;
using Discord;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Her monthly reports: on the 1st, from 09:00 Paris, for the month just ended (a calendar month,
// ReportWindow.CalendarMonth), one digest to Papa and one to Tata, each in their DMs. Each opens
// with a line of hers and « Le mois en bref » (ReportService.BuildBriefAsync: a few figures and
// « À regarder », DigestBrief), then the reports of ReportKinds.Digest(recipient), one per message.
// Papa's is about her running well (commands, the Pi, what she can't answer, the quiz) and also gets
// the chat-answer counts and the last unanswered messages as a file; Tata's is the server's life.
//
// report-state.json, next to the database (so under /data in production), keeps the last month sent
// to each; it is written *before* sending, so a crash costs a digest, never a second one. With no
// file yet (first start after the update, or a lost /data), or no field yet for a recipient, the
// previous month is marked as sent without sending: a deploy never DMs a surprise. The two are
// claimed and sent apart: closed DMs on one side never cost the other. /debug digest sends either
// version to Papa on demand.
//
// Its own hourly loop, like the other loops (root CLAUDE.md): the 1st at 09:00 give or take an
// hour is fine, and a digest missed while she was down goes out at the next check that month.
public sealed class MonthlyDigestService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);

    private readonly DiscordSocketClient _client;
    private readonly IServiceProvider _services;
    private readonly ILogger<MonthlyDigestService> _logger;
    private readonly UnansweredLog _unanswered;
    private readonly string _statePath;

    public MonthlyDigestService(DiscordSocketClient client, IServiceProvider services, IConfiguration config,
        UnansweredLog unanswered, ILogger<MonthlyDigestService> logger)
    {
        _client = client;
        _services = services;
        _unanswered = unanswered;
        _logger = logger;

        var dbPath = Path.GetFullPath(config["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "report-state.json");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // No file, or a recipient with no field yet: last month counts as sent, so nobody gets a
        // surprise digest the day this ships.
        var (year, month) = MonthlyDigest.PreviousMonth(DateTimeOffset.UtcNow);
        var previous = MonthlyDigest.MonthKey(year, month);
        var state = LoadState();
        if (state is null) SaveState(new DigestState(previous, previous));
        else if (state.TataLastMonth is null) SaveState(state with { TataLastMonth = previous });

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(CheckInterval, stoppingToken);
            if (_client.ConnectionState != ConnectionState.Connected) continue;
            try
            {
                await TickAsync();
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // An exception escaping the loop would stop the whole bot.
                _logger.LogWarning(ex, "Monthly digest check failed.");
            }
        }
    }

    private async Task TickAsync()
    {
        foreach (var recipient in Enum.GetValues<DigestRecipient>())
        {
            try
            {
                var state = LoadState() ?? new DigestState(0, 0);
                var last = recipient == DigestRecipient.Tata ? state.TataLastMonth ?? 0 : state.LastMonth;
                if (MonthlyDigest.Due(DateTimeOffset.UtcNow, last) is not { } due) continue;

                // Claimed before sending: a crash mid-send costs this digest, never a second one.
                var key = MonthlyDigest.MonthKey(due.Year, due.Month);
                SaveState(recipient == DigestRecipient.Tata ? state with { TataLastMonth = key } : state with { LastMonth = key });
                await SendAsync(recipient, due.Year, due.Month, toOwner: recipient == DigestRecipient.Owner);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Monthly digest: the {Recipient} digest failed.", recipient);
            }
        }
    }

    /// <summary>
    /// Sends <paramref name="recipient"/>'s digest for one month now, without touching the state: the
    /// loop's send, and /debug digest's. <paramref name="toOwner"/> sends it to Papa whoever it is for
    /// (the preview of Tata's version). Returns whether the first message went out (the reports after
    /// it may have failed one by one, logged).
    /// </summary>
    public async Task<bool> SendAsync(DigestRecipient recipient, int year, int month, bool toOwner)
    {
        var window = ReportWindow.CalendarMonth(year, month);
        var monthIndex = year * 12 + month;
        var targetId = toOwner ? AvailabilityService.OwnerId : BotResponses.TataId;
        var opening = recipient == DigestRecipient.Tata ? BotResponses.TataDigestLines : BotResponses.OwnerDigestLines;
        var line = string.Format(DailyRotation.Pick(opening, monthIndex, salt: 0xD16E57), window.Label);
        if (recipient == DigestRecipient.Tata && toOwner) line = "-# Aperçu : la version de Tata.\n" + line;
        var quiet = DailyRotation.Pick(BotResponses.DigestQuietLines, monthIndex, salt: 0x9E1E7);

        IDMChannel dm;
        try
        {
            var user = await _client.GetUserAsync(targetId);
            if (user is null)
            {
                _logger.LogWarning("Monthly digest: could not resolve {Recipient}.", recipient);
                return false;
            }
            dm = await user.CreateDMChannelAsync();

            Embed? brief = null;
            try
            {
                await using var scope = _services.CreateAsyncScope();
                brief = await scope.ServiceProvider.GetRequiredService<ReportService>()
                    .BuildBriefAsync(recipient, HomeGuild.Id, window, quiet);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Monthly digest: the {Recipient} brief failed for {Month}.", recipient, window.Label);
            }
            await dm.SendMessageAsync(line, embed: brief, allowedMentions: AllowedMentions.None);
        }
        catch (HttpException ex) when (ex.DiscordCode == DiscordErrorCode.CannotSendMessageToUser)
        {
            _logger.LogWarning("Monthly digest: {Recipient}'s DMs are closed.", recipient);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monthly digest: could not open {Recipient}'s DMs.", recipient);
            return false;
        }

        // One report per message, each in its own try: a failed one doesn't cost the others, and
        // several embeds in one message could pass the 6000-character cap.
        foreach (var kind in ReportKinds.Digest(recipient))
        {
            try
            {
                await using var scope = _services.CreateAsyncScope();
                var reports = scope.ServiceProvider.GetRequiredService<ReportService>();
                var embed = await reports.BuildAsync(kind, HomeGuild.Id, window, names: true);
                await dm.SendMessageAsync(embed: embed, allowedMentions: AllowedMentions.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Monthly digest: the {Kind} report failed for {Month}.", kind, window.Label);
            }
        }

        if (recipient == DigestRecipient.Owner) await SendAnswersAsync(dm, window);
        _logger.LogInformation("Monthly digest for {Month} sent ({Recipient}).", window.Label, recipient);
        return true;
    }

    // Papa's « messages sans réponse » in full: the chat-answer counts by path over 30 days (as
    // /debug answers shows them; they are kept by rolling period, not by calendar month), and the
    // last messages she found nothing specific to say to, as a file. That log is in memory, so it
    // covers the time since her last restart.
    private async Task SendAnswersAsync(IDMChannel dm, ReportWindow window)
    {
        try
        {
            await using var scope = _services.CreateAsyncScope();
            var counts = await scope.ServiceProvider.GetRequiredService<ChatAnswerService>().GetAsync(HomeGuild.Id, StatsPeriod.Month);
            await dm.SendMessageAsync(embed: AnswerStatsCards.BuildTypes(counts, StatsPeriod.Month), allowedMentions: AllowedMentions.None);

            var messages = _unanswered.For(HomeGuild.Id);
            if (messages.Count == 0) return;
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(AnswerStatsCards.BuildExamples(messages)));
            await dm.SendFileAsync(stream, "sans-reponse.txt",
                $"**{messages.Count}** message(s) sans réponse précise depuis mon dernier redémarrage.",
                allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monthly digest: the chat answers failed for {Month}.", window.Label);
        }
    }

    // What a restart must not forget: the last month whose digest went out (yyyymm), to Papa
    // (LastMonth, the field's name from before Tata had one) and to Tata (null until first set).
    private sealed record DigestState(int LastMonth, int? TataLastMonth = null);

    private DigestState? LoadState()
    {
        try
        {
            if (File.Exists(_statePath))
                return JsonSerializer.Deserialize<DigestState>(File.ReadAllText(_statePath));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monthly digest: could not read {Path}.", _statePath);
        }
        return null;
    }

    private void SaveState(DigestState state)
    {
        try
        {
            File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Monthly digest: could not write {Path}.", _statePath);
        }
    }
}
