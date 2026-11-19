using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// Plays a visit once both choices are made: the once-a-day claim (in memory, and the day stored on the
// relation), the odds of the two choices judged by the other Plynling's personality, the scene itself
// (decided and saved by PlynlingService.VisitAsync), and the claim released whenever nothing happened.
// Shared by the host's reaction and her own Plynling's (whose reaction she picks herself), so they
// cannot drift apart. Transient: it wraps PlynlingService. Everything the callers check *before* this
// (dead, frozen, asleep, sick) stays with them, since each words its refusals for how it was reached.
public class PlynlingVisitRunner
{
    private readonly PlynlingService _plynlings;
    private readonly PlynlingCooldowns _cooldowns;

    public PlynlingVisitRunner(PlynlingService plynlings, PlynlingCooldowns cooldowns)
    {
        _plynlings = plynlings;
        _cooldowns = cooldowns;
    }

    /// <summary>
    /// The scene a visit opens on: one the pair may have — its bond before the visit, the Paris hour,
    /// both stages (<see cref="VisitCatalog.Eligible"/>, never empty: the catalog's coverage holds it) —
    /// through the picker, so the same scene does not come back twice in a row.
    /// </summary>
    public static VisitScene DrawScene(PlynlingBond bond, DateTimeOffset now, PlynlingStage visitor, PlynlingStage host, ResponsePicker picker)
    {
        var eligible = VisitCatalog.Eligible(bond, AppTime.ToZoned(now).Hour, visitor, host);
        var key = picker.Pick(eligible.Select(s => PickKey + s.Key).ToArray());
        return VisitCatalog.ByKey(key[PickKey.Length..])!;
    }

    // The picker's memory is shared with every pool she speaks from: a prefix keeps a scene key from
    // ever being the same string as one of her lines.
    private const string PickKey = "visit-scene:";

    // The outcome, or the refusal to send privately. Untold: the event stories the visit brought that the
    // mascot already decided (an after-visit event at her home) — the caller tells them after the
    // ending; this service is its own PlynlingService instance, so only it can hand them over.
    public async Task<(VisitOutcome? Outcome, string? Refusal, IReadOnlyList<int> Untold)> RunAsync(
        Plynling visitor, Plynling host, VisitScene scene, VisitChoice option, VisitChoice reaction, DateTimeOffset now)
    {
        // OpenVisitAsync refuses a visit to oneself; a custom-id is the client's to send, so the
        // play refuses it too rather than trust the id it was handed.
        if (visitor.OwnerId == host.OwnerId) return (null, PlynlingText.VisitSelf, Array.Empty<int>());

        // The stored day is what survives a restart; the memory claim, taken after it (so a failed
        // read holds nothing), turns a double click away before the database is asked.
        var day = AppTime.DayKey(now);
        if (await _plynlings.VisitedTodayAsync(visitor.Id, host.Id, now)
            || !_cooldowns.TryClaimVisit(visitor.OwnerId, host.OwnerId, day))
            return (null, PlynlingText.VisitedToday(visitor.OwnerId), Array.Empty<int>());

        // A throw here means nothing was saved (the save is VisitAsync's last step), so the claim
        // goes back — left held, the pair would be told « déjà vus » for a visit that never happened.
        VisitOutcome? met;
        try
        {
            // The visitor's option is judged by the host, the host's reaction by the visitor.
            var shift = VisitOdds.Shift(option.Lean, await _plynlings.AxesAsync(host))
                        + VisitOdds.Shift(reaction.Lean, await _plynlings.AxesAsync(visitor));
            met = await _plynlings.VisitAsync(visitor.Id, host.Id, now, new VisitPlay(shift, scene.Confession));
        }
        catch
        {
            _cooldowns.ReleaseVisit(visitor.OwnerId, host.OwnerId, day);
            throw;
        }
        if (met is not { TooLate: false } pair)
        {
            _cooldowns.ReleaseVisit(visitor.OwnerId, host.OwnerId, day);
            return (met, met is null ? PlynlingText.Unknown : PlynlingText.VisitTooLate, _plynlings.TakeUntold());
        }
        return (pair, null, _plynlings.TakeUntold());
    }

    /// <summary>
    /// Puts a visit's ending in place of its message, after the visit is saved. If Discord refuses the
    /// ending, the message is closed with <paramref name="outcome"/> instead (a plain card, no buttons),
    /// so the knock never stays live over a visit that happened with nobody told; if that fails too,
    /// both errors are logged and nothing escapes (CLAUDE.md: a Discord side effect never breaks the
    /// flow). Returns whether the ending itself was shown.
    /// </summary>
    public static async Task<bool> ShowEndingAsync(Func<Discord.MessageComponent, Task> edit, Discord.MessageComponent ending,
        string outcome, Action<Exception> log)
    {
        try
        {
            await edit(ending);
            return true;
        }
        catch (Exception ex)
        {
            log(ex);
        }
        try
        {
            await edit(ProjectSYNCS.Commands.PlynlingVisitCards.BuildClosed(outcome));
        }
        catch (Exception ex)
        {
            log(ex);
        }
        return false;
    }
}
