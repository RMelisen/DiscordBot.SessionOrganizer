using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// /plynling train: what the drill did, or why it could not (EnoughAt with NoEnergy).
public enum TrainOutcome { Done, NoPlynling, Dead, Frozen, Mascot, TooYoung, Asleep, Sick, NoEnergy, NotABodyStat }

public sealed record TrainResult(TrainOutcome Outcome, Plynling? Plynling, DrillResult? Drill = null, int MaxEnergy = 0,
    DateTimeOffset? EnoughAt = null);

// A level point placed from a drill's buttons.
public enum PointOutcome { Done, NoPlynling, NotOwner, NoPoint }

// The RPG (docs/agents/plynling-rpg.md). The rules are PlynlingRpg's; this only loads, checks and saves.
public partial class PlynlingService
{
    // One drill of a body stat for the owner's living Plynling. Every refusal comes before anything is
    // spent; a drill is one save (energy, XP, growth, the count and the dashboard line together).
    public Task<TrainResult> TrainAsync(ulong guildId, ulong ownerId, PlynlingStat drill, DateTimeOffset now) =>
        UnitAsync(() => TrainOnceAsync(guildId, ownerId, drill, now));

    private async Task<TrainResult> TrainOnceAsync(ulong guildId, ulong ownerId, PlynlingStat drill, DateTimeOffset now)
    {
        if (!PlynlingStats.IsBody(drill)) return new TrainResult(TrainOutcome.NotABodyStat, null);
        var p = await GetCurrentAsync(guildId, ownerId, now);
        if (p is null) return new TrainResult(TrainOutcome.NoPlynling, null);
        TrainOutcome? refusal = p.DiedAt is not null ? TrainOutcome.Dead
            : p.FrozenAt is not null ? TrainOutcome.Frozen
            : PlynlingMascot.Is(p) ? TrainOutcome.Mascot
            : !PlynlingRpg.CanTrain(p, now) ? TrainOutcome.TooYoung
            : PlynlingLife.IsAsleep(now) ? TrainOutcome.Asleep
            : PlynlingLife.IsSick(p) ? TrainOutcome.Sick
            : null;
        if (refusal is { } r) return new TrainResult(r, p);

        var traits = await GetTraitsAsync(p);
        var max = PlynlingRpg.MaxEnergy(p, traits);
        if (PlynlingRpg.EnergyAt(p, max, now) < PlynlingRpg.DrillCost)
            return new TrainResult(TrainOutcome.NoEnergy, p, MaxEnergy: max,
                EnoughAt: PlynlingRpg.EnergyReadyAt(p, max, PlynlingRpg.DrillCost, now));

        var result = PlynlingRpg.Drill(p, traits, drill, now);
        await EconomyLog.AddAsync(_db_context, guildId, EconomyLog.ActTrain, 1, now);
        await PlynlingScore.AddAsync(_db_context, p,
            PlynlingScore.Drill + (result.LevelAfter - result.LevelBefore) * PlynlingScore.LevelUp, now);
        await _db_context.SaveChangesAsync();
        return new TrainResult(TrainOutcome.Done, p, result, max);
    }

    // One level point into a body stat, by its owner only, and only while one is left to place — so a
    // button clicked twice, or left open after the points went elsewhere, finds none.
    public Task<(PointOutcome Outcome, Plynling? Plynling)> SpendPointAsync(int plynlingId, ulong userId, PlynlingStat stat, DateTimeOffset now) =>
        UnitAsync(() => SpendPointOnceAsync(plynlingId, userId, stat, now));

    private async Task<(PointOutcome, Plynling?)> SpendPointOnceAsync(int plynlingId, ulong userId, PlynlingStat stat, DateTimeOffset now)
    {
        var p = await SettledAsync(await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == plynlingId), now);
        if (p is null) return (PointOutcome.NoPlynling, null);
        if (p.OwnerId != userId) return (PointOutcome.NotOwner, p);
        if (p.DiedAt is not null || !PlynlingStats.IsBody(stat) || PlynlingRpg.UnspentPoints(p) == 0) return (PointOutcome.NoPoint, p);

        PlynlingStats.AddPoint(p, stat);
        await _db_context.SaveChangesAsync();
        return (PointOutcome.Done, p);
    }
}
