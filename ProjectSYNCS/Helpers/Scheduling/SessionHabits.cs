using System.Collections.Concurrent;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>What someone chose for their last session in a server, to start their next one from.</summary>
public sealed record SessionHabit(SessionCategory Category, int MaxPlayers, int DurationMinutes, ulong VoiceChannelId, int MinuteOfDay);

/// <summary>
/// The session picker's memory of each person's last choices, per server (a voice channel belongs to
/// one): the next wizard starts from them, so a group that always plays at 21:00 for 3 hours in the
/// same channel picks nothing twice. In memory, so a restart forgets it, like the drafts; what was
/// last published is the only thing remembered, never a wizard left halfway.
/// </summary>
public sealed class SessionHabits
{
    private readonly ConcurrentDictionary<(ulong Guild, ulong User), SessionHabit> _last = new();

    public SessionHabit? For(ulong guildId, ulong userId) =>
        _last.TryGetValue((guildId, userId), out var habit) ? habit : null;

    public void Remember(SessionEvent session)
    {
        var zoned = AppTime.ToZoned(session.ScheduledAt);
        _last[(session.GuildId, session.OrganizerId)] = new SessionHabit(session.Category, session.MaxPlayers,
            session.DurationMinutes, session.VoiceChannelId, zoned.Hour * 60 + zoned.Minute);
    }
}
