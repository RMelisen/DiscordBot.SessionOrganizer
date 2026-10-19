using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>One person's line in a report: <c>Count</c> out of <c>Of</c> (Of is 0 when it means nothing).</summary>
public sealed record PersonTally(ulong UserId, int Count, int Of);

/// <summary>A weekday or an hour of the day, and how its sessions went.</summary>
public sealed record SlotTally(int Slot, int Sessions, int Expected, int Came);

/// <summary>What /report sessions shows. Built by <see cref="SessionReport.Compute"/>.</summary>
public sealed record SessionReportData(
    int Held,
    int Cancelled,
    IReadOnlyList<(SessionCategory Category, int Count)> ByCategory,
    int SignUps,
    int Maybes,
    double? FillRate,
    int RollCalls,
    int Expected,
    int Came,
    int Missed,
    int Late,
    int? MedianLateMinutes,
    IReadOnlyList<SlotTally> Weekdays,
    IReadOnlyList<SlotTally> Hours,
    IReadOnlyList<PersonTally> Organisers,
    IReadOnlyList<PersonTally> Reliable,
    IReadOnlyList<PersonTally> NoShows,
    IReadOnlyList<PersonTally> Latecomers);

/// <summary>
/// The session logistics of one window, from the session rows with their participants. Pure, so the
/// checks can feed it cases.
/// </summary>
/// <remarks>
/// <para>A session belongs to the window its <c>ScheduledAt</c> falls in (Paris day); there is no
/// CancelledAt, so a cancellation is dated by the slot it cancelled. Only sessions already started
/// count as held: one later today is not news yet.</para>
/// <para>Attendance only reads sessions whose roll she called (<c>RecapSent</c>), through
/// <see cref="SessionAttendance.IsMissed"/> and <see cref="SessionAttendance.CameLate"/>, so it
/// agrees with /recap and her chat facts. Expected means Joined: « peut-être » means no.</para>
/// </remarks>
public static class SessionReport
{
    /// <summary>How many people each named list shows.</summary>
    public const int ListSize = 5;

    public static SessionReportData Compute(IEnumerable<SessionEvent> sessions, ReportWindow window, DateTimeOffset now)
    {
        var inWindow = sessions.Where(s => window.Contains(s.ScheduledAt)).ToList();
        var held = inWindow.Where(s => !s.IsCancelled && s.ScheduledAt <= now).ToList();
        var cancelled = inWindow.Count(s => s.IsCancelled);

        var byCategory = held.GroupBy(s => s.Category)
            .Select(g => (g.Key, g.Count()))
            .OrderByDescending(x => x.Item2)
            .ToList();

        var signUps = held.Sum(s => s.Participants.Count(p => p.Status == ParticipantStatus.Joined));
        var maybes = held.Sum(s => s.Participants.Count(p => p.Status == ParticipantStatus.Maybe));
        var capped = held.Where(s => s.MaxPlayers > 0).ToList();
        double? fill = capped.Count == 0 ? null
            : capped.Average(s => Math.Min(1.0, s.Participants.Count(p => p.Status == ParticipantStatus.Joined) / (double)s.MaxPlayers));

        // The roll: recapped sessions, and the people who said they'd come to them.
        var called = held.Where(s => s.RecapSent).ToList();
        var expected = called
            .SelectMany(s => s.Participants.Where(p => p.Status == ParticipantStatus.Joined).Select(p => (Session: s, Person: p)))
            .ToList();
        var came = expected.Count(x => x.Person.FirstSeenInVoiceAt is not null);
        var missed = expected.Count(x => SessionAttendance.IsMissed(x.Person, x.Session));
        var lateMinutes = expected
            .Where(x => SessionAttendance.CameLate(x.Person, x.Session))
            .Select(x => (int)Math.Round((x.Person.FirstSeenInVoiceAt!.Value - x.Session.ScheduledAt).TotalMinutes))
            .OrderBy(m => m)
            .ToList();

        SlotTally Slot(int slot, IEnumerable<SessionEvent> group)
        {
            var list = group.ToList();
            var roll = list.Where(s => s.RecapSent)
                .SelectMany(s => s.Participants.Where(p => p.Status == ParticipantStatus.Joined)).ToList();
            return new SlotTally(slot, list.Count, roll.Count, roll.Count(p => p.FirstSeenInVoiceAt is not null));
        }

        var weekdays = held
            .GroupBy(s => (int)AppTime.ToZoned(s.ScheduledAt).DayOfWeek)
            .Select(g => Slot(g.Key, g))
            .OrderBy(x => (x.Slot + 6) % 7) // Monday first
            .ToList();
        var hours = held
            .GroupBy(s => AppTime.ToZoned(s.ScheduledAt).Hour)
            .Select(g => Slot(g.Key, g))
            .OrderByDescending(x => x.Sessions).ThenBy(x => x.Slot)
            .Take(ListSize)
            .OrderBy(x => x.Slot)
            .ToList();

        var organisers = inWindow.Where(s => s.ScheduledAt <= now || s.IsCancelled)
            .GroupBy(s => s.OrganizerId)
            .Select(g => new PersonTally(g.Key, g.Count(s => !s.IsCancelled), g.Count()))
            .OrderByDescending(x => x.Count).ThenByDescending(x => x.Of)
            .Take(ListSize)
            .ToList();

        var people = expected
            .GroupBy(x => x.Person.UserId)
            .Select(g => (
                UserId: g.Key,
                Expected: g.Count(),
                Came: g.Count(x => x.Person.FirstSeenInVoiceAt is not null),
                Missed: g.Count(x => SessionAttendance.IsMissed(x.Person, x.Session)),
                Late: g.Count(x => SessionAttendance.CameLate(x.Person, x.Session))))
            .ToList();

        var reliable = people.Where(p => p.Came >= 2)
            .OrderByDescending(p => p.Came / (double)p.Expected).ThenByDescending(p => p.Came)
            .Take(ListSize).Select(p => new PersonTally(p.UserId, p.Came, p.Expected)).ToList();
        var noShows = people.Where(p => p.Missed > 0)
            .OrderByDescending(p => p.Missed).ThenByDescending(p => p.Missed / (double)p.Expected)
            .Take(ListSize).Select(p => new PersonTally(p.UserId, p.Missed, p.Expected)).ToList();
        var latecomers = people.Where(p => p.Late > 0)
            .OrderByDescending(p => p.Late).ThenByDescending(p => p.Late / (double)p.Came)
            .Take(ListSize).Select(p => new PersonTally(p.UserId, p.Late, p.Came)).ToList();

        return new SessionReportData(
            held.Count, cancelled, byCategory, signUps, maybes, fill,
            called.Count, expected.Count, came, missed, lateMinutes.Count,
            lateMinutes.Count == 0 ? null : lateMinutes[lateMinutes.Count / 2],
            weekdays, hours, organisers, reliable, noShows, latecomers);
    }
}
