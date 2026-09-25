using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum FreezeOutcome { Frozen, NoPlynling, Dead, AlreadyFrozen, TooHungry, Cooldown }

// All of a Plynling's time-based behaviour, as pure functions of the row and an instant.
// No Discord, no database: this is the part that is checkable outright, and it is where
// the subtle bugs would live (a frozen day counted as lived, a death at the wrong time).
//
// Thresholds are strict ("under 50%", "above 80%") so that "hungry face" and "can no
// longer self-freeze" are exact complements: both begin just below 50%.
public static class PlynlingLife
{
    // Needs are computed from elapsed time since NeedsAsOf, so changing either of these
    // applies retroactively to every living Plynling's current stretch — shortening
    // HungerLife moves deaths earlier, and one already past fires on the next settle.
    public static readonly TimeSpan HungerLife = TimeSpan.FromDays(2);
    public static readonly TimeSpan HappinessLife = TimeSpan.FromHours(36);
    public static readonly TimeSpan SelfFreezeMax = TimeSpan.FromDays(14);
    public static readonly TimeSpan SelfFreezeCooldown = TimeSpan.FromDays(7);
    public static readonly TimeSpan WarningLead = TimeSpan.FromHours(3);
    public static readonly TimeSpan PetCooldown = TimeSpan.FromHours(4);

    public const double StartNeeds = 0.70;
    public const double ResurrectNeeds = 0.50;
    public const double SelfFreezeMinHunger = 0.50;
    public const double PetAmount = 0.25;
    public const double StarvingBelow = 0.25;
    public const double HungryBelow = 0.50;
    public const double SadBelow = 0.30;
    public const double HappyAbove = 0.80;

    // What the card shows as 100% counts as full, so feeding is never refused at a value
    // the owner can see is not full, nor allowed at one they can see is.
    private const double Full = 0.995;

    public static bool IsDead(Plynling p) => p.DiedAt is not null;
    public static bool IsFrozen(Plynling p) => p.FrozenAt is not null;

    public static Plynling Create(ulong guildId, ulong ownerId, string name, PlynlingSpecies species, PlynlingGender gender, DateTimeOffset now) => new()
    {
        GuildId = guildId,
        OwnerId = ownerId,
        Name = name,
        Species = species,
        Gender = gender,
        AdoptedAt = now,
        Hunger = StartNeeds,
        Happiness = StartNeeds,
        NeedsAsOf = now,
        LiveSince = now,
    };

    public static double HungerAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Hunger : Clamp(p.Hunger - (t - p.NeedsAsOf) / HungerLife);

    public static double HappinessAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Happiness : Clamp(p.Happiness - (t - p.NeedsAsOf) / HappinessLife);

    // When it will starve if nothing changes. Null when it cannot: frozen, or already dead.
    public static DateTimeOffset? DeathAt(Plynling p) =>
        IsFrozen(p) || IsDead(p) ? null : p.NeedsAsOf + p.Hunger * HungerLife;

    public static TimeSpan Age(Plynling p, DateTimeOffset now) =>
        TimeSpan.FromSeconds(p.AgeBankedSeconds) + (IsFrozen(p) || IsDead(p) ? TimeSpan.Zero : now - p.LiveSince);

    // Cosmetic only: the card's label and, for staged species, the picture. Measured on
    // time actually lived, so a frozen Plynling does not grow up. 6 months = 180 days,
    // a month taken as 30 days like the memorial tiers.
    public static PlynlingStage Stage(Plynling p, DateTimeOffset now) => Age(p, now).TotalDays switch
    {
        < 2 => PlynlingStage.Baby,
        < 14 => PlynlingStage.Teen,
        < 180 => PlynlingStage.Adult,
        _ => PlynlingStage.Elder,
    };

    public static PlynlingMood Mood(Plynling p, DateTimeOffset now)
    {
        if (IsFrozen(p)) return PlynlingMood.Frozen;
        var hunger = HungerAt(p, now);
        if (hunger < StarvingBelow) return PlynlingMood.Starving;
        if (hunger < HungryBelow) return PlynlingMood.Hungry;
        var happiness = HappinessAt(p, now);
        if (happiness < SadBelow) return PlynlingMood.Sad;
        if (happiness > HappyAbove) return PlynlingMood.Happy;
        return PlynlingMood.Content;
    }

    /// <summary>
    /// Brings a Plynling up to <paramref name="now"/>: an expired self-freeze thaws at the
    /// moment it was due, then it dies at the moment it starved. Returns whether anything
    /// changed, so the caller knows to save.
    /// </summary>
    /// <remarks>
    /// Every read goes through this first. That is what makes a death discovered by a
    /// command identical to one the hourly sweep found — same instant, same age, same
    /// memorial — and why the sweep is a safety net rather than the source of truth.
    /// Order matters: the thaw is applied first, so the death is computed from the thaw.
    /// </remarks>
    public static bool Settle(Plynling p, DateTimeOffset now)
    {
        if (IsDead(p)) return false;
        var changed = false;

        if (IsFrozen(p) && p.FreezeUntil is { } until && until <= now)
        {
            EndFreeze(p, until);
            changed = true;
        }

        if (DeathAt(p) is { } death && death <= now)
        {
            Rebase(p, death);
            p.AgeBankedSeconds += (long)(death - p.LiveSince).TotalSeconds;
            p.Hunger = 0;
            p.DiedAt = death;
            changed = true;
        }

        return changed;
    }

    // Null when an owner may freeze their own Plynling now; otherwise why not.
    public static FreezeOutcome? SelfFreezeBlocker(Plynling p, DateTimeOffset now) =>
        IsDead(p) ? FreezeOutcome.Dead
        : IsFrozen(p) ? FreezeOutcome.AlreadyFrozen
        : HungerAt(p, now) < SelfFreezeMinHunger ? FreezeOutcome.TooHungry
        : p.LastSelfThawAt is { } thawed && now - thawed < SelfFreezeCooldown ? FreezeOutcome.Cooldown
        : null;

    public static void Freeze(Plynling p, DateTimeOffset now, bool byStaff)
    {
        Rebase(p, now);
        p.AgeBankedSeconds += (long)(now - p.LiveSince).TotalSeconds;
        p.FrozenAt = now;
        p.FreezeUntil = byStaff ? null : now + SelfFreezeMax;
        p.FrozenByStaff = byStaff;
    }

    public static void Thaw(Plynling p, DateTimeOffset now) => EndFreeze(p, now);

    public static bool WouldWaste(Plynling p, FoodInfo food, DateTimeOffset now) =>
        (food.Hunger <= 0 || HungerAt(p, now) >= Full) && (food.Happiness <= 0 || HappinessAt(p, now) >= Full);

    public static void Feed(Plynling p, FoodInfo food, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Hunger = Clamp(p.Hunger + food.Hunger);
        p.Happiness = Clamp(p.Happiness + food.Happiness);
        if (DeathAt(p) is { } death && death - now > WarningLead) p.WarningSent = false;
    }

    public static void Pet(Plynling p, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Happiness = Clamp(p.Happiness + PetAmount);
    }

    public static bool ShouldWarn(Plynling p, DateTimeOffset now) =>
        !p.WarningSent && DeathAt(p) is { } death && death > now && death - now <= WarningLead;

    // The same row comes back: same name, same species, age carrying on. Time spent dead
    // was never added to AgeBankedSeconds, so it does not count.
    public static void Resurrect(Plynling p, DateTimeOffset now)
    {
        p.DiedAt = null;
        p.DeathAnnounced = false;
        p.WarningSent = false;
        p.FrozenAt = null;
        p.FreezeUntil = null;
        p.FrozenByStaff = false;
        p.Hunger = ResurrectNeeds;
        p.Happiness = ResurrectNeeds;
        p.NeedsAsOf = now;
        p.LiveSince = now;
    }

    // Stores the current values as of `at`. Only meaningful while alive and not frozen.
    private static void Rebase(Plynling p, DateTimeOffset at)
    {
        p.Hunger = HungerAt(p, at);
        p.Happiness = HappinessAt(p, at);
        p.NeedsAsOf = at;
    }

    private static void EndFreeze(Plynling p, DateTimeOffset at)
    {
        if (!p.FrozenByStaff) p.LastSelfThawAt = at;
        p.FrozenAt = null;
        p.FreezeUntil = null;
        p.FrozenByStaff = false;
        p.NeedsAsOf = at;
        p.LiveSince = at;
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 1);
}
