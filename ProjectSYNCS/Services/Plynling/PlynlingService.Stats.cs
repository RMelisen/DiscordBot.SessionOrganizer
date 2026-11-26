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
            await StatFactsAsync(p), await RanksAsync(p));
    }

    private async Task<StatFacts> StatFactsAsync(Plynling p)
    {
        var kinds = await _db_context.PlynlingJournalEntries.AsNoTracking()
            .Where(e => e.PlynlingId == p.Id && (e.Kind == JournalKind.FellSick || e.Kind == JournalKind.Frozen))
            .Select(e => e.Kind).ToListAsync();
        var badges = KnownBadges(await _db_context.PlynlingBadges.AsNoTracking().Where(b => b.PlynlingId == p.Id).Select(b => b.Key).ToListAsync());
        var memories = await _db_context.PlynlingMemories.AsNoTracking().CountAsync(m => m.PlynlingId == p.Id);
        var traits = (await _db_context.PlynlingTraits.AsNoTracking().Where(t => t.PlynlingId == p.Id).Select(t => t.Kind).ToListAsync())
            .GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());
        var marks = (await _db_context.PlynlingMarks.AsNoTracking().Where(m => m.PlynlingId == p.Id).Select(m => m.Kind).ToListAsync())
            .GroupBy(k => k).ToDictionary(g => g.Key, g => g.Count());

        var relations = await _db_context.PlynlingRelations.AsNoTracking()
            .Where(r => r.PlynlingAId == p.Id || r.PlynlingBId == p.Id).ToListAsync();
        var bonds = relations.GroupBy(r => r.Bond).ToDictionary(g => g.Key, g => g.Count());
        // The Plynling it met most; ties go to the lower id, so the line is stable.
        var most = relations
            .Select(r => (Other: r.PlynlingAId == p.Id ? r.PlynlingBId : r.PlynlingAId, r.Meetings))
            .OrderByDescending(x => x.Meetings).ThenBy(x => x.Other)
            .FirstOrDefault();
        var mostName = most.Meetings > 0
            ? await _db_context.Plynlings.AsNoTracking().Where(x => x.Id == most.Other).Select(x => x.Name).FirstOrDefaultAsync()
            : null;

        var events = await _db_context.PlynlingEventInstances.AsNoTracking()
            .Where(e => e.PlynlingId == p.Id && e.ResolvedAt != null)
            .Select(e => new { e.ChallengeSucceeded, e.EndingKey }).ToListAsync();

        return new StatFacts(
            TimesSick: kinds.Count(k => k == JournalKind.FellSick),
            TimesFrozen: kinds.Count(k => k == JournalKind.Frozen),
            Badges: badges, Memories: memories, Traits: traits, Marks: marks, Bonds: bonds,
            Meetings: relations.Sum(r => r.Meetings), MostMetName: mostName, MostMetMeetings: mostName is null ? 0 : most.Meetings,
            EventsLived: events.Count,
            ChallengesTried: events.Count(e => e.ChallengeSucceeded is not null),
            ChallengesWon: events.Count(e => e.ChallengeSucceeded == true),
            StoriesFinished: events.Count(e => e.EndingKey is not null));
    }

    // Its place among every Plynling of the guild, dead included, never the mascot (the population of
    // /leaderboard's Plynlings view). A stat at 0 gets no place; ties share the better one.
    private async Task<IReadOnlyDictionary<RankedStat, StatRank>> RanksAsync(Plynling p)
    {
        var ranks = new Dictionary<RankedStat, StatRank>();
        if (PlynlingMascot.Is(p)) return ranks;

        var mascot = PlynlingMascot.OwnerId;
        var rows = await _db_context.Plynlings.AsNoTracking()
            .Where(x => x.GuildId == p.GuildId && (mascot == 0 || x.OwnerId != mascot))
            .Select(x => new { x.Id, x.Score, x.RpgXp, x.Meals, x.Pets, x.Plays, x.Drills, x.Visits })
            .ToListAsync();
        var ids = rows.Select(r => r.Id).ToList();
        var badges = (await _db_context.PlynlingBadges.AsNoTracking().Where(b => ids.Contains(b.PlynlingId))
                .Select(b => new { b.PlynlingId, b.Key }).ToListAsync())
            .GroupBy(b => b.PlynlingId).ToDictionary(g => g.Key, g => (long)KnownBadges(g.Select(b => b.Key)));
        var events = await _db_context.PlynlingEventInstances.AsNoTracking().Where(e => ids.Contains(e.PlynlingId) && e.ResolvedAt != null)
            .GroupBy(e => e.PlynlingId).Select(g => new { g.Key, N = g.Count() }).ToDictionaryAsync(g => g.Key, g => (long)g.N);

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
        Rank(RankedStat.EventsLived, id => events.GetValueOrDefault(id));
        Rank(RankedStat.Badges, id => badges.GetValueOrDefault(id));
        return ranks;
    }

    // Badges counted like the journal (PlynlingJournalCards.BadgeText): catalogue keys only, once each, so
    // « n / total » can never disagree with it nor pass the total.
    private static int KnownBadges(IEnumerable<string> keys) =>
        keys.Distinct().Count(k => PlynlingBadges.ByKey(k) is not null);
}
