using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// /plynling stats' read (docs/agents/plynling.md, *Stats*): one sheet per Plynling, everything loaded untracked,
// nothing written. The counters are written by the actions themselves (PlynlingCounters).
public partial class PlynlingService
{
    public async Task<PlynlingStatSheet> GetStatSheetAsync(Plynling p, DateTimeOffset now)
    {
        var counters = await _db_context.PlynlingCounters.AsNoTracking()
            .Where(c => c.PlynlingId == p.Id)
            .ToDictionaryAsync(c => c.Key, c => c.Value);
        var carers = await _db_context.PlynlingCarers.AsNoTracking().Where(c => c.PlynlingId == p.Id).ToListAsync();
        return new PlynlingStatSheet(p, now, PlynlingCounters.IsPartial(p), counters, carers,
            await StatFactsAsync(p), await RanksAsync(p, now), await StatScoreAsync(p, now));
    }

    /// <summary>What the two Plynlings of a duel are to each other, or null when they never met.</summary>
    public async Task<StatPair?> GetStatPairAsync(Plynling a, Plynling b)
    {
        var (lo, hi) = a.Id < b.Id ? (a.Id, b.Id) : (b.Id, a.Id);
        var r = await _db_context.PlynlingRelations.AsNoTracking().FirstOrDefaultAsync(x => x.PlynlingAId == lo && x.PlynlingBId == hi);
        return r is null ? null
            : new StatPair(r.Bond, r.Meetings, r.Since, a.Gender == PlynlingGender.Female && b.Gender == PlynlingGender.Female);
    }

    // Its buckets, read as of today (or its death day: a grave's streak ends there).
    private async Task<StatScore> StatScoreAsync(Plynling p, DateTimeOffset now)
    {
        var byDay = await _db_context.PlynlingScoreDays.AsNoTracking().Where(b => b.PlynlingId == p.Id)
            .GroupBy(b => b.Day).Select(g => new { g.Key, Points = g.Sum(b => b.Points) })
            .ToDictionaryAsync(g => g.Key, g => g.Points);
        return StatScore.From(byDay, AppTime.DayKey(p.DiedAt ?? now));
    }

    private static readonly JournalKind[] StatKinds =
    {
        JournalKind.FellSick, JournalKind.Frozen, JournalKind.BrokeUp, JournalKind.Heartbroken,
        JournalKind.FirstWin, JournalKind.BecameFriends,
    };

    private async Task<StatFacts> StatFactsAsync(Plynling p)
    {
        var moments = await _db_context.PlynlingJournalEntries.AsNoTracking()
            .Where(e => e.PlynlingId == p.Id && StatKinds.Contains(e.Kind))
            .Select(e => new { e.Kind, e.At }).ToListAsync();
        int Moments(JournalKind k) => moments.Count(m => m.Kind == k);
        // From its adoption to the first such moment (wall clock: a freeze in between counts).
        TimeSpan? After(JournalKind k)
        {
            var first = moments.Where(m => m.Kind == k).Select(m => (DateTimeOffset?)m.At).Min();
            if (first is not { } at) return null;
            var span = at - p.AdoptedAt;
            return span > TimeSpan.Zero ? span : TimeSpan.Zero;
        }

        var badges = KnownBadges(await _db_context.PlynlingBadges.AsNoTracking().Where(b => b.PlynlingId == p.Id).Select(b => b.Key).ToListAsync());
        var memories = await _db_context.PlynlingMemories.AsNoTracking().CountAsync(m => m.PlynlingId == p.Id);
        var traits = (await _db_context.PlynlingTraits.AsNoTracking().Where(t => t.PlynlingId == p.Id).Select(t => t.Kind).ToListAsync())
            .GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());
        var marks = (await _db_context.PlynlingMarks.AsNoTracking().Where(m => m.PlynlingId == p.Id).Select(m => m.Kind).ToListAsync())
            .GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());

        var relations = await _db_context.PlynlingRelations.AsNoTracking()
            .Where(r => r.PlynlingAId == p.Id || r.PlynlingBId == p.Id).ToListAsync();
        int OtherOf(PlynlingRelation r) => r.PlynlingAId == p.Id ? r.PlynlingBId : r.PlynlingAId;
        var bonds = relations.GroupBy(r => r.Bond).ToDictionary(g => g.Key, g => g.Count());
        // The Plynling it met most; ties go to the lower id, so the line is stable.
        var most = relations
            .Select(r => (Other: OtherOf(r), r.Meetings))
            .OrderByDescending(x => x.Meetings).ThenBy(x => x.Other)
            .FirstOrDefault();
        // Its partner, and its closest best friend (highest affinity, then the oldest bond).
        var partner = relations.Where(r => r.Bond == PlynlingBond.Lovers).OrderBy(r => r.Since).FirstOrDefault();
        var best = relations.Where(r => r.Bond == PlynlingBond.BestFriends)
            .OrderByDescending(r => r.Affinity).ThenBy(r => r.Since).FirstOrDefault();
        var nameIds = new[] { most.Meetings > 0 ? most.Other : 0, partner is null ? 0 : OtherOf(partner), best is null ? 0 : OtherOf(best) }
            .Where(id => id != 0).Distinct().ToList();
        var names = await _db_context.Plynlings.AsNoTracking().Where(x => nameIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.Name);
        var mostName = most.Meetings > 0 ? names.GetValueOrDefault(most.Other) : null;
        StatFriend? Friend(PlynlingRelation? r) => r is not null && names.TryGetValue(OtherOf(r), out var n) ? new StatFriend(n, r.Since) : null;

        // Every other Plynling of the guild, the dead and the mascot included: any of them can be met.
        var population = await _db_context.Plynlings.AsNoTracking().CountAsync(x => x.GuildId == p.GuildId && x.Id != p.Id);

        var events = await _db_context.PlynlingEventInstances.AsNoTracking()
            .Where(e => e.PlynlingId == p.Id && e.ResolvedAt != null)
            .Select(e => new { e.ChallengeSucceeded, e.ChancePercent, e.EndingKey, e.DecidedAlone, e.StressDelta }).ToListAsync();

        return new StatFacts(
            TimesSick: Moments(JournalKind.FellSick),
            TimesFrozen: Moments(JournalKind.Frozen),
            Badges: badges, Memories: memories, Traits: traits, Marks: marks, Bonds: bonds,
            Meetings: relations.Sum(r => r.Meetings), MostMetName: mostName, MostMetMeetings: mostName is null ? 0 : most.Meetings,
            EventsLived: events.Count,
            ChallengesTried: events.Count(e => e.ChallengeSucceeded is not null),
            ChallengesWon: events.Count(e => e.ChallengeSucceeded == true),
            StoriesFinished: events.Count(e => e.EndingKey is not null),
            Met: relations.Count, Population: population,
            Partner: Friend(partner), BestFriend: Friend(best),
            BreakUps: Moments(JournalKind.BrokeUp), Heartbreaks: Moments(JournalKind.Heartbroken),
            FirstWinAfter: After(JournalKind.FirstWin), FirstFriendAfter: After(JournalKind.BecameFriends),
            DecidedAlone: events.Count(e => e.DecidedAlone),
            LuckiestPercent: events.Where(e => e.ChallengeSucceeded == true && e.ChancePercent is not null).Min(e => e.ChancePercent),
            EventStress: events.Sum(e => e.StressDelta ?? 0));
    }

    // A win rate ranks only past this many games: three lucky ones are no record.
    public const int WinRateMinPlays = 10;

    // Its place among every Plynling of the guild, dead included, never the mascot (the population of
    // /leaderboard's Plynlings view). A stat at 0 gets no place; ties share the better one.
    private async Task<IReadOnlyDictionary<RankedStat, StatRank>> RanksAsync(Plynling p, DateTimeOffset now)
    {
        var ranks = new Dictionary<RankedStat, StatRank>();
        if (PlynlingMascot.Is(p)) return ranks;

        var mascot = PlynlingMascot.OwnerId;
        var rows = await _db_context.Plynlings.AsNoTracking()
            .Where(x => x.GuildId == p.GuildId && (mascot == 0 || x.OwnerId != mascot))
            .Select(x => new { x.Id, x.Score, x.RpgXp, x.Meals, x.Pets, x.Plays, x.PlaysWon, x.Drills, x.Visits, x.Hosted })
            .ToListAsync();
        var ids = rows.Select(r => r.Id).ToList();
        var badges = (await _db_context.PlynlingBadges.AsNoTracking().Where(b => ids.Contains(b.PlynlingId))
                .Select(b => new { b.PlynlingId, b.Key }).ToListAsync())
            .GroupBy(b => b.PlynlingId).ToDictionary(g => g.Key, g => (long)KnownBadges(g.Select(b => b.Key)));
        var events = await _db_context.PlynlingEventInstances.AsNoTracking().Where(e => ids.Contains(e.PlynlingId) && e.ResolvedAt != null)
            .GroupBy(e => e.PlynlingId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(g => g.Key, g => (long)g.N);
        var bathKeys = new[] { PlynlingCounters.BathFree, PlynlingCounters.BathSoap };
        var baths = (await _db_context.PlynlingCounters.AsNoTracking().Where(c => ids.Contains(c.PlynlingId) && bathKeys.Contains(c.Key))
                .Select(c => new { c.PlynlingId, c.Value }).ToListAsync())
            .GroupBy(c => c.PlynlingId).ToDictionary(g => g.Key, g => g.Sum(c => c.Value));
        // The same seven days as the sheet's « Points sur 7 jours » and /leaderboard's week, filtered by day in SQL.
        var since = AppTime.DayKey(now.AddDays(-6));
        var week = (await _db_context.PlynlingScoreDays.AsNoTracking().Where(b => b.GuildId == p.GuildId && b.Day >= since)
                .Select(b => new { b.PlynlingId, b.Points }).ToListAsync())
            .GroupBy(b => b.PlynlingId).ToDictionary(g => g.Key, g => g.Sum(b => b.Points));

        void Rank(RankedStat stat, Func<int, long> value)
        {
            var mine = value(p.Id);
            if (mine <= 0) return;
            ranks[stat] = new StatRank(1 + ids.Count(id => value(id) > mine), ids.Count);
        }

        var byId = rows.ToDictionary(r => r.Id);
        Rank(RankedStat.Score, id => byId[id].Score);
        Rank(RankedStat.Level, id => PlynlingRpg.Level(byId[id].RpgXp));   // by level, so a tie on the level shares
        Rank(RankedStat.Meals, id => byId[id].Meals);
        Rank(RankedStat.Pets, id => byId[id].Pets);
        Rank(RankedStat.Plays, id => byId[id].Plays);
        Rank(RankedStat.Drills, id => byId[id].Drills);
        Rank(RankedStat.Visits, id => byId[id].Visits);
        Rank(RankedStat.Hosted, id => byId[id].Hosted);
        Rank(RankedStat.EventsLived, id => events.GetValueOrDefault(id));
        Rank(RankedStat.Badges, id => badges.GetValueOrDefault(id));
        Rank(RankedStat.Baths, id => baths.GetValueOrDefault(id));
        Rank(RankedStat.Week, id => week.GetValueOrDefault(id));
        // In hundredths of a percent, and only past WinRateMinPlays: below it a Plynling has no rate to rank.
        Rank(RankedStat.WinRate, id => byId[id].Plays >= WinRateMinPlays ? byId[id].PlaysWon * 10_000 / byId[id].Plays : 0);
        return ranks;
    }

    // Badges counted like the journal (PlynlingJournalCards.BadgeText): catalogue keys only, once each, so
    // « n / total » can never disagree with it nor pass the total.
    private static int KnownBadges(IEnumerable<string> keys) =>
        keys.Distinct().Count(k => PlynlingBadges.ByKey(k) is not null);
}
