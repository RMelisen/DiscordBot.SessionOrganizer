using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

/// <summary>
/// Counts every warning and error the bot logs, by logger, for /report incidents: a background loop's
/// failure reaches nothing else. Built in Program.cs before the container, since logging exists first,
/// around the one ActivityCounter, so the count stays in memory and costs a lock. It never logs.
/// </summary>
public sealed class IncidentLogProvider : ILoggerProvider
{
    private readonly ActivityCounter _counter;

    public IncidentLogProvider(ActivityCounter counter)
    {
        _counter = counter;
    }

    public ILogger CreateLogger(string categoryName) => new Logger(categoryName, _counter);

    public void Dispose()
    {
    }

    private sealed class Logger : ILogger
    {
        private readonly string _category;
        private readonly string _key;
        private readonly ActivityCounter _counter;

        public Logger(string category, ActivityCounter counter)
        {
            _category = category;
            _key = IncidentKinds.LogKey(category);
            _counter = counter;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel is >= LogLevel.Warning and < LogLevel.None;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var now = DateTimeOffset.UtcNow;
            _counter.CountIncident(logLevel >= LogLevel.Error ? IncidentKinds.LogError : IncidentKinds.LogWarning, _key, now);
            if (IncidentKinds.IsBusy(_category, exception)) _counter.CountIncident(IncidentKinds.SqliteBusy, "", now);
        }
    }
}
