using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Services;

// What the hourly sweep owes one Plynling's owner once its unit is saved (PlynlingService.SweepOneAsync).
public enum SweepNotice { None, Death, Sick, Warning }

// Told: the event instances its sweep resolved, for the sweep to tell.
public sealed record SweepItem(Plynling Plynling, SweepNotice Notice, IReadOnlyList<int> Told);

// Units of work and the sweep's: how PlynlingService saves under concurrency (see *Concurrency* in
// docs/agents/plynling.md), and the one unit the hourly sweep runs per Plynling.
public partial class PlynlingService
{
    // Units of work. The Plynling and relation rows carry a Version token (AppDbContext), so of
    // two saves that read the same row — the sweep and a meal, two people feeding one Plynling — the
    // slower throws instead of silently undoing the faster (and its cailloux with it). Every public
    // entry point that may save runs through UnitAsync, which runs it again from a clean context:
    // the retry re-reads and re-checks everything, like EconomySave. Nested calls (a meal's
    // GetByIdAsync) pass straight through, so only the outermost unit retries, and from its start.
    //
    // A unit may run more than once, so nothing in it may act outside the database before its last
    // save (DMs, posts and BadgeAwarder come after). A caller holding a Plynling from an earlier unit
    // may find it detached after a retry; act on the one a unit returns.

    private const int UnitAttempts = 3;      // a third conflict in a row is no race; let it surface
    private int _unitDepth;

    private async Task<T> UnitAsync<T>(Func<Task<T>> work)
    {
        if (_unitDepth > 0) return await work();
        for (var attempt = 1; ; attempt++)
        {
            _unitDepth++;
            try
            {
                return await work();
            }
            catch (DbUpdateConcurrencyException) when (attempt < UnitAttempts)
            {
                // Not the queued on-actions: a fact an attempt saved before the conflict (a morning
                // played) is not replayed by the next, so its on-action must survive. The flush
                // skips an event already open, so one queued twice lands once.
                _db_context.ChangeTracker.Clear();
                _askedMascot = false;
            }
            finally
            {
                _unitDepth--;
            }
        }
    }

    private Task UnitAsync(Func<Task> work) => UnitAsync(async () => { await work(); return true; });

    // After a refused save that is not retried (TryGiftAsync, which works on the caller's Plynling),
    // forgets every change it held, so no later save on this context retries it. The on-actions and the
    // mascot's ask that unit queued go too: they belong to work that was never saved.
    private void DiscardChanges()
    {
        ResetTracked();
        _onActions.Clear();
        _askedMascot = false;
    }

    // Forgets every unsaved change the context holds, and nothing else.
    private void ResetTracked()
    {
        foreach (var entry in _db_context.ChangeTracker.Entries().ToList())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.State = EntityState.Detached;
                    break;
                case EntityState.Modified:
                case EntityState.Deleted:
                    entry.CurrentValues.SetValues(entry.OriginalValues);
                    entry.State = EntityState.Unchanged;
                    break;
            }
        }
    }

    // Every Plynling the hourly sweep has to look at: the living, and deaths not yet announced. Ids
    // only: the sweep runs each one in its own scope (SweepOneAsync).
    public async Task<List<int>> GetSweepIdsAsync() =>
        await _db_context.Plynlings.Where(x => x.DiedAt == null || !x.DeathAnnounced).Select(x => x.Id).ToListAsync();

    // The sweep's turn for one Plynling, as one unit of work: settle it, its moments, time's badges and
    // stage moments, its events (cancelled, decided alone, the day's pulse), and the flag of the one
    // notice it is owed — saved *before* the sweep sends it, so a failed post is logged once rather than
    // retried every hour. Null if it was abandoned, or its death announced, since the ids were read.
    // Told gathers across a retry: what an attempt resolved and saved stays resolved, and the next
    // attempt does not resolve it again.
    public async Task<SweepItem?> SweepOneAsync(int id, DateTimeOffset now)
    {
        var told = new List<int>();
        return await UnitAsync(async () =>
        {
            var p = await _db_context.Plynlings.FirstOrDefaultAsync(x => x.Id == id && (x.DiedAt == null || !x.DeathAnnounced));
            if (p is null) return null;

            PlynlingLife.Settle(p, now);
            await FlushMomentsAsync(p);                 // fell sick / recovered, if a morning did it
            await ProgressAsync(p, now);                // time's badges and stage moments
            told.AddRange(await TickEventsAsync(p, now));   // cancels, decides alone, pulses — saves

            var notice = SweepNotice.None;
            if (p.DiedAt is not null && !p.DeathAnnounced)
            {
                p.DeathAnnounced = true;
                await JournalDeathAsync(p);
                notice = SweepNotice.Death;
            }
            else if (PlynlingLife.IsSick(p) && !p.SickNotified)
            {
                // An onset is always at 05:00, so this never lands in the night.
                p.SickNotified = true;
                notice = SweepNotice.Sick;
            }
            else if (PlynlingLife.ShouldWarn(p, now))
            {
                p.WarningSent = true;
                notice = SweepNotice.Warning;
            }
            await _db_context.SaveChangesAsync();
            return new SweepItem(p, notice, told.Distinct().ToList());
        });
    }
}
