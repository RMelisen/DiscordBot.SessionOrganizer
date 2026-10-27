using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ProjectSYNCS.Services;

/// <summary>
/// The owner's silence switch (<c>/debug silence</c>): while it is on, she says nothing on her own
/// initiative — no chatter, reactions, hellos, ambient lines, level-up cards, pop quizzes, Quokka,
/// Plynling announcements… Answers to commands and buttons still go out, and so do the notices a
/// command set up (session reminders and recaps, giveaway draws, <c>/remindme</c>).
/// </summary>
/// <remarks>Singleton. Unlike the absent flag it survives a restart: silence-state.json, next to the
/// database (so under /data in production). A read or write failure is logged and treated as « not
/// silent » / kept in memory, never a crash.</remarks>
public sealed class SilenceService
{
    private sealed record SilenceState(bool Silent, DateTimeOffset? Since);

    private readonly string _statePath;
    private readonly ILogger<SilenceService> _logger;
    private readonly object _lock = new();

    // volatile: written from the slash-command path, read from every gateway handler and loop.
    private volatile bool _silent;
    private DateTimeOffset? _since;

    public SilenceService(IConfiguration config, ILogger<SilenceService> logger)
    {
        _logger = logger;
        var dbPath = Path.GetFullPath(config["Database:Path"] ?? "ProjectSYNCS.db");
        _statePath = Path.Combine(Path.GetDirectoryName(dbPath) ?? ".", "silence-state.json");

        try
        {
            if (File.Exists(_statePath)
                && JsonSerializer.Deserialize<SilenceState>(File.ReadAllText(_statePath)) is { } state)
            {
                _silent = state.Silent;
                _since = state.Since;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read {Path}; starting with her voice on.", _statePath);
        }
    }

    public bool IsSilent => _silent;

    /// <summary>When the current silence began; null while she speaks.</summary>
    public DateTimeOffset? Since => _silent ? _since : null;

    public void Set(bool silent)
    {
        lock (_lock)
        {
            if (_silent == silent) return;
            _silent = silent;
            _since = silent ? DateTimeOffset.UtcNow : null;
            try
            {
                File.WriteAllText(_statePath, JsonSerializer.Serialize(new SilenceState(_silent, _since)));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not write {Path}; silence holds until the next restart only.", _statePath);
            }
            _logger.LogInformation("Silence mode {State}.", silent ? "on" : "off");
        }
    }
}
