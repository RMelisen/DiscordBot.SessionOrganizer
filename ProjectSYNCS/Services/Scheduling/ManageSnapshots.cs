using System.Collections.Concurrent;
using ProjectSYNCS.Models;
using Poll = ProjectSYNCS.Models.Poll;

namespace ProjectSYNCS.Services;

/// <summary>
/// The session or poll the « ⚙️ Gérer » panel was opened on, kept a few minutes per (person, row).
/// « Modifier » and « Renommer » must answer with a modal inside Discord's 3 s, before any defer
/// is possible; with the snapshot they open without a database read on the Pi, and fall back to
/// one once it's gone.
/// </summary>
/// <remarks>Singleton: it owns the state. A stale snapshot only pre-fills a form: every submit re-reads the row.</remarks>
public sealed class ManageSnapshots
{
    private static readonly TimeSpan Ttl = TimeSpan.FromMinutes(10);

    private readonly ConcurrentDictionary<(ulong UserId, int Id), (SessionEvent Row, DateTimeOffset At)> _sessions = new();
    private readonly ConcurrentDictionary<(ulong UserId, int Id), (Poll Row, DateTimeOffset At)> _polls = new();

    public void Remember(ulong userId, SessionEvent session) => Put(_sessions, userId, session.Id, session);

    public void Remember(ulong userId, Poll poll) => Put(_polls, userId, poll.Id, poll);

    public SessionEvent? Session(ulong userId, int eventId) => Get(_sessions, userId, eventId);

    public Poll? Poll(ulong userId, int pollId) => Get(_polls, userId, pollId);

    /// <summary>Drops every snapshot of a session that just changed (edited, cancelled).</summary>
    public void Forget(int eventId) => Drop(_sessions, eventId);

    public void ForgetPoll(int pollId) => Drop(_polls, pollId);

    private static void Put<T>(ConcurrentDictionary<(ulong, int), (T, DateTimeOffset)> map, ulong userId, int id, T row)
    {
        Prune(map);
        map[(userId, id)] = (row, DateTimeOffset.UtcNow);
    }

    private static T? Get<T>(ConcurrentDictionary<(ulong, int), (T Row, DateTimeOffset At)> map, ulong userId, int id)
        where T : class
    {
        Prune(map);
        return map.TryGetValue((userId, id), out var entry) ? entry.Row : null;
    }

    private static void Drop<T>(ConcurrentDictionary<(ulong UserId, int Id), (T, DateTimeOffset)> map, int id)
    {
        foreach (var key in map.Keys)
            if (key.Id == id) map.TryRemove(key, out _);
    }

    private static void Prune<T>(ConcurrentDictionary<(ulong, int), (T Row, DateTimeOffset At)> map)
    {
        var cutoff = DateTimeOffset.UtcNow - Ttl;
        foreach (var (key, entry) in map)
            if (entry.At < cutoff) map.TryRemove(key, out _);
    }
}
