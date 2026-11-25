using System.Text.Json;
using Discord;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// The owner's silence switch (<c>/debug silence</c>), one per server: while a server is silenced,
/// she says nothing there on her own initiative — no chatter, reactions, hellos, ambient lines, pop
/// quizzes, Quokka, Plynling announcements… Answers to commands and buttons still go out, and so do
/// level-up cards and the notices a command set up (session reminders and recaps, giveaway draws,
/// <c>/remindme</c>). A DM falls under the home server's switch.
/// </summary>
/// <remarks>Singleton. Unlike the absent flag it survives a restart: silence-state.json, next to the
/// database (so under /data in production). A read or write failure is logged and treated as « not
/// silent » / kept in memory, never a crash. The file's older one-switch shape is read as the home
/// server silenced (<see cref="SilenceStateFile"/>).</remarks>
public sealed class SilenceService
{
    private readonly string _statePath;
    private readonly ILogger<SilenceService> _logger;
    private readonly object _lock = new();

    // Replaced whole under the lock, read without it: every gateway handler and loop reads it.
    private volatile IReadOnlyDictionary<ulong, DateTimeOffset> _silenced = new Dictionary<ulong, DateTimeOffset>();

    public SilenceService(IConfiguration config, ILogger<SilenceService> logger)
    {
        _logger = logger;
        var dbPath = Path.GetFullPath(config["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "silence-state.json");

        try
        {
            if (File.Exists(_statePath))
                _silenced = SilenceStateFile.Parse(File.ReadAllText(_statePath), HomeGuild.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read {Path}; starting with her voice on.", _statePath);
        }
    }

    /// <summary>Whether she is silenced on this server.</summary>
    public bool IsSilent(ulong guildId) => _silenced.ContainsKey(guildId);

    /// <summary>Whether she is silenced where this channel is: its server, or the home server for a DM.</summary>
    public bool IsSilentIn(IChannel channel) =>
        IsSilent((channel as IGuildChannel)?.GuildId ?? HomeGuild.Id);

    /// <summary>When this server's silence began; null while she speaks there.</summary>
    public DateTimeOffset? Since(ulong guildId) =>
        _silenced.TryGetValue(guildId, out var since) ? since : null;

    /// <summary>Every silenced server.</summary>
    public IReadOnlyCollection<ulong> SilencedGuilds => _silenced.Keys.ToList();

    public void Set(ulong guildId, bool silent)
    {
        lock (_lock)
        {
            if (_silenced.ContainsKey(guildId) == silent) return;
            var next = new Dictionary<ulong, DateTimeOffset>(_silenced);
            if (silent) next[guildId] = DateTimeOffset.UtcNow;
            else next.Remove(guildId);
            _silenced = next;
            try
            {
                File.WriteAllText(_statePath, SilenceStateFile.Write(next));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write {Path}; silence holds until the next restart only.", _statePath);
            }
            _logger.LogInformation("Silence mode {State} in guild {GuildId}.", silent ? "on" : "off", guildId);
        }
    }
}

/// <summary>
/// The silence file's two shapes, pure so the checks can read both: today's
/// <c>{ "Guilds": [ { "Id": …, "Since": … } ] }</c>, and the one-switch shape it replaced,
/// <c>{ "Silent": true, "Since": … }</c>, which meant the home server (the only one she lived on).
/// </summary>
public static class SilenceStateFile
{
    private sealed record Entry(ulong Id, DateTimeOffset Since);
    private sealed record State(List<Entry>? Guilds, bool? Silent, DateTimeOffset? Since);

    public static IReadOnlyDictionary<ulong, DateTimeOffset> Parse(string json, ulong homeGuildId)
    {
        var result = new Dictionary<ulong, DateTimeOffset>();
        if (JsonSerializer.Deserialize<State>(json) is not { } state) return result;

        if (state.Guilds is { } guilds)
            foreach (var entry in guilds) result[entry.Id] = entry.Since;
        else if (state.Silent == true)
            result[homeGuildId] = state.Since ?? DateTimeOffset.UtcNow;
        return result;
    }

    public static string Write(IReadOnlyDictionary<ulong, DateTimeOffset> silenced) =>
        JsonSerializer.Serialize(new State(
            silenced.Select(p => new Entry(p.Key, p.Value)).OrderBy(e => e.Id).ToList(), null, null));
}
