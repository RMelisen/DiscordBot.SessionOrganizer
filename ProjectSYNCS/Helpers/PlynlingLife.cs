using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

public enum FreezeOutcome { Frozen, NoPlynling, Dead, AlreadyFrozen, TooHungry, Cooldown, Sick }

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
    public static readonly TimeSpan HygieneLife = TimeSpan.FromDays(3);
    public static readonly TimeSpan SelfFreezeMax = TimeSpan.FromDays(14);
    public static readonly TimeSpan SelfFreezeCooldown = TimeSpan.FromDays(7);
    public static readonly TimeSpan WarningLead = TimeSpan.FromHours(3);
    public static readonly TimeSpan PetCooldown = TimeSpan.FromHours(4);
    public static readonly TimeSpan BathCooldown = TimeSpan.FromHours(6);
    public static readonly TimeSpan AbandonCooldown = TimeSpan.FromMinutes(30);

    // /plynling play and /plynling visit. Playing always cheers it up, winning more so; a win
    // also pays the player a few cailloux. A visit cheers both Plynlings and pays nothing, so
    // two accounts cannot farm it.
    public static readonly TimeSpan PlayCooldown = TimeSpan.FromHours(1);
    public const double PlayAmount = 0.15;
    public const double PlayWinBonus = 0.10;
    public const double VisitAmount = 0.20;
    public static readonly TimeSpan VisitInviteLife = TimeSpan.FromHours(1);
    public const int PlayWinPebblesMin = 5;
    public const int PlayWinPebblesMax = 10;

    // Every Plynling sleeps from 01:00 to 05:00, Paris time. Hunger keeps dropping, but none
    // dies in its sleep: a death due at night happens at 05:00. And the warning DM never goes
    // out at night — from 23:00 on it would find the owner asleep too — so it moves to 23:00.
    public static readonly TimeSpan NightStart = TimeSpan.FromHours(1);
    public static readonly TimeSpan NightEnd = TimeSpan.FromHours(5);
    public static readonly TimeSpan QuietStart = TimeSpan.FromHours(23);

    public const double StartNeeds = 0.70;
    public const double ResurrectNeeds = 0.50;
    public const double SelfFreezeMinHunger = 0.50;
    public const double PetAmount = 0.25;
    public const double StarvingBelow = 0.25;
    public const double HungryBelow = 0.50;
    public const double SadBelow = 0.30;
    public const double HappyAbove = 0.80;

    // Below DirtyBelow a Plynling is « sale »: its happiness drains DirtyHappinessFactor times
    // faster and the card shows it. Strict, like the thresholds above. A bath washes BathAmount
    // off; an outing (a forage, a game) costs a little.
    public const double DirtyBelow = 0.33;
    public const double DirtyHappinessFactor = 1.5;
    public const double BathAmount = 0.60;
    public const double ForageDirt = 0.10;
    public const double PlayDirt = 0.05;
    public const double ResurrectHygiene = 0.50;

    // Mood changes what a meal is worth — its hunger only, never its price or its happiness —
    // and at 0 % it sulks and refuses to eat at all, unless it is starving: the sulk must never
    // be what kills it (at night nothing could lift it, since petting is refused).
    public const double HappyMealBonus = 0.15;
    public const double SadMealPenalty = 0.25;
    public const double SulkBelow = 0.005;              // what the card rounds to « 0 % »

    // The happy gift: once a Paris day, the owner's first look while it is happy draws once.
    public const double GiftChance = 0.5;
    public const int GiftMin = 5;
    public const int GiftMax = 15;

    // What the card shows as 100% counts as full, so feeding is never refused at a value
    // the owner can see is not full, nor allowed at one they can see is.
    private const double Full = 0.995;

    public static bool IsDead(Plynling p) => p.DiedAt is not null;
    public static bool IsFrozen(Plynling p) => p.FrozenAt is not null;
    public static bool IsSick(Plynling p) => p.SickSince is not null;

    public static void Cure(Plynling p)
    {
        p.SickSince = null;
        p.Recovery = 0;
        p.SickNotified = false;
    }

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
        Hygiene = 1.0,
        NeedsAsOf = now,
        LiveSince = now,
        LastMorningDay = MorningDayAtOrBefore(now),
    };

    public static double HungerAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Hunger : Clamp(p.Hunger - (t - p.NeedsAsOf) / HungerLife);

    public static double HygieneAt(Plynling p, DateTimeOffset t) =>
        IsFrozen(p) || IsDead(p) ? p.Hygiene : Clamp(p.Hygiene - (t - p.NeedsAsOf) / HygieneLife);

    public static bool IsDirty(Plynling p, DateTimeOffset t) => HygieneAt(p, t) < DirtyBelow;

    // Happiness drains at its own rate while clean and DirtyHappinessFactor times faster once
    // hygiene has fallen below DirtyBelow. Hygiene falls linearly, so it crosses at one exact
    // instant and the drain is two straight pieces — exact, not approximated.
    public static double HappinessAt(Plynling p, DateTimeOffset t)
    {
        if (IsFrozen(p) || IsDead(p)) return p.Happiness;
        var elapsed = Math.Max(0, (t - p.NeedsAsOf).TotalSeconds);
        var cleanFor = p.Hygiene < DirtyBelow ? 0 : (p.Hygiene - DirtyBelow) * HygieneLife.TotalSeconds;
        var clean = Math.Min(elapsed, cleanFor);
        var dirty = elapsed - clean;
        return Clamp(p.Happiness - (clean + dirty * DirtyHappinessFactor) / HappinessLife.TotalSeconds);
    }

    // When it will starve if nothing changes. Null when it cannot: frozen, or already dead.
    // The raw instant its hunger reaches zero — EffectiveDeathAt is when it actually dies.
    public static DateTimeOffset? DeathAt(Plynling p) =>
        IsFrozen(p) || IsDead(p) ? null : p.NeedsAsOf + p.Hunger * HungerLife;

    public static bool IsAsleep(DateTimeOffset t)
    {
        var time = AppTime.ToZoned(t).TimeOfDay;
        return time >= NightStart && time < NightEnd;
    }

    // 05:00 Paris on the morning of t. Built from the wall clock, so the nights the clocks
    // change (both inside the sleep window) still wake at 05:00 local.
    public static DateTimeOffset WakeAfter(DateTimeOffset t) => AtWallClock(AppTime.ToZoned(t).Date + NightEnd);

    // When it dies: its starvation instant, or 05:00 when that falls while it sleeps.
    public static DateTimeOffset? EffectiveDeathAt(Plynling p) =>
        DeathAt(p) is { } death ? (IsAsleep(death) ? WakeAfter(death) : death) : null;

    // When the warning DM goes out: WarningLead before the effective death, pulled back to
    // 23:00 when that would land between 23:00 and 05:00, so it reaches the owner before bed.
    public static DateTimeOffset? WarnAt(Plynling p)
    {
        if (EffectiveDeathAt(p) is not { } death) return null;
        var warn = death - WarningLead;
        var zoned = AppTime.ToZoned(warn);
        if (zoned.TimeOfDay >= QuietStart) return AtWallClock(zoned.Date + QuietStart);
        if (zoned.TimeOfDay < NightEnd) return AtWallClock(zoned.Date.AddDays(-1) + QuietStart);
        return warn;
    }

    private static DateTimeOffset AtWallClock(DateTime wall) => new(wall, AppTime.Zone.GetUtcOffset(wall));

    // ---- mornings: 05:00 Paris, when every Plynling wakes -----------------------------------------

    public static DateTimeOffset MorningAt(int dayKey) => AtWallClock(FromDayKey(dayKey) + NightEnd);

    public static int NextDay(int dayKey) => ToDayKey(FromDayKey(dayKey).AddDays(1));

    // The day of the latest morning at or before t: today's from 05:00, yesterday's before.
    public static int MorningDayAtOrBefore(DateTimeOffset t)
    {
        var zoned = AppTime.ToZoned(t);
        return ToDayKey(zoned.TimeOfDay >= NightEnd ? zoned.Date : zoned.Date.AddDays(-1));
    }

    private static DateTime FromDayKey(int dayKey) => new(dayKey / 10000, dayKey / 100 % 100, dayKey % 100);

    private static int ToDayKey(DateTime day) => day.Year * 10000 + day.Month * 100 + day.Day;

    public static TimeSpan Age(Plynling p, DateTimeOffset now) =>
        TimeSpan.FromSeconds(p.AgeBankedSeconds) + (IsFrozen(p) || IsDead(p) ? TimeSpan.Zero : now - p.LiveSince);

    // Cosmetic only: the card's label and, for staged species, the picture. Measured on
    // time actually lived, so a frozen Plynling does not grow up. 6 months = 180 days,
    // a month taken as 30 days like the memorial tiers.
    public static PlynlingStage Stage(Plynling p, DateTimeOffset now)
    {
        var age = Age(p, now);
        return age >= StageStart(PlynlingStage.Elder) ? PlynlingStage.Elder
            : age >= StageStart(PlynlingStage.Adult) ? PlynlingStage.Adult
            : age >= StageStart(PlynlingStage.Teen) ? PlynlingStage.Teen
            : PlynlingStage.Baby;
    }

    // The age each stage begins at — the one table Stage and the journal's « est devenu… »
    // moments both read, so the two cannot disagree about when it grew up.
    public static TimeSpan StageStart(PlynlingStage stage) => stage switch
    {
        PlynlingStage.Teen => TimeSpan.FromDays(2),
        PlynlingStage.Adult => TimeSpan.FromDays(14),
        PlynlingStage.Elder => TimeSpan.FromDays(180),
        _ => TimeSpan.Zero,
    };

    public static PlynlingMood Mood(Plynling p, DateTimeOffset now)
    {
        if (IsFrozen(p)) return PlynlingMood.Frozen;
        if (IsAsleep(now)) return PlynlingMood.Sleeping;
        if (IsSick(p)) return PlynlingMood.Sick;
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
    /// moment it was due; then every 05:00 morning since the last one played is played, oldest
    /// first (sickness, PlayMorning); then it dies at the moment it starved. Returns whether
    /// anything changed, so the caller knows to save.
    /// </summary>
    /// <remarks>
    /// Every read goes through this first. That is what makes a death discovered by a
    /// command identical to one the hourly sweep found — same instant, same age, same
    /// memorial — and why the sweep is a safety net rather than the source of truth. The
    /// morning rolls are hashed, never random, for the same reason. Time order is kept: a
    /// starvation due before a morning wins, and no later morning is played.
    /// </remarks>
    public static bool Settle(Plynling p, DateTimeOffset now, SicknessRoll? roll = null)
    {
        if (IsDead(p)) return false;
        roll ??= PlynlingSickness.Roll;
        var changed = false;

        // Frozen until when? Mornings before the thaw are skipped, even once it has thawed below.
        var frozenUntil = IsFrozen(p) ? p.FreezeUntil ?? DateTimeOffset.MaxValue : DateTimeOffset.MinValue;
        if (IsFrozen(p) && p.FreezeUntil is { } until && until <= now)
        {
            EndFreeze(p, until);
            changed = true;
        }

        while (true)
        {
            var day = NextDay(p.LastMorningDay);
            var morning = MorningAt(day);
            if (morning > now) break;
            if (EffectiveDeathAt(p) is { } starve && starve <= morning) break;
            p.LastMorningDay = day;
            changed = true;
            if (morning < frozenUntil || IsFrozen(p)) continue;         // the illness pauses with the rest
            PlayMorning(p, morning, day, roll);
            if (IsDead(p)) return true;
        }

        if (EffectiveDeathAt(p) is { } death && death <= now)
        {
            Die(p, death, DeathCause.Starvation);
            changed = true;
        }

        return changed;
    }

    // One morning: the onset roll when healthy; otherwise, from the second sick morning, the death
    // roll (from the third, unless dosed since the previous morning) and then the day's recovery.
    private static void PlayMorning(Plynling p, DateTimeOffset morning, int day, SicknessRoll roll)
    {
        if (!IsSick(p))
        {
            if (roll(p.Id, day, RollPurpose.Onset) < PlynlingSickness.OnsetChance(HygieneAt(p, morning)))
            {
                p.SickSince = morning;
                p.Recovery = 0;
                p.SickNotified = false;
                p.PendingMoments.Add((JournalKind.FellSick, morning));
            }
            return;
        }

        // 0 = the onset morning. Rounded, because a clock-change night is 23 or 25 hours.
        var sickMorning = (int)Math.Round((morning - p.SickSince!.Value).TotalDays);
        if (sickMorning == 0) return;
        var previous = MorningAt(MorningDayAtOrBefore(morning - TimeSpan.FromHours(1)));
        var dosed = p.LastMedicineAt is { } dose && dose >= previous;
        if (sickMorning >= PlynlingSickness.GraceMornings && !dosed
            && roll(p.Id, day, RollPurpose.Death) < PlynlingSickness.IllnessDeathChance)
        {
            Die(p, morning, DeathCause.Illness);
            return;
        }

        p.Recovery += PlynlingSickness.Gain(roll(p.Id, day, RollPurpose.Recovery),
            PlynlingSickness.RecoveryDailyMin, PlynlingSickness.RecoveryDailyMax);
        if (dosed)
            p.Recovery += PlynlingSickness.Gain(roll(p.Id, day, RollPurpose.Medicine),
                PlynlingSickness.RecoveryMedicineMin, PlynlingSickness.RecoveryMedicineMax);
        if (p.Recovery >= PlynlingSickness.Healed)
        {
            Cure(p);
            p.PendingMoments.Add((JournalKind.Recovered, morning));
        }
    }

    private static void Die(Plynling p, DateTimeOffset at, DeathCause cause)
    {
        Rebase(p, at);
        p.AgeBankedSeconds += (long)(at - p.LiveSince).TotalSeconds;
        if (cause == DeathCause.Starvation) p.Hunger = 0;
        p.DiedAt = at;
        p.DeathCause = cause;
    }

    // Null when an owner may freeze their own Plynling now; otherwise why not.
    public static FreezeOutcome? SelfFreezeBlocker(Plynling p, DateTimeOffset now) =>
        IsDead(p) ? FreezeOutcome.Dead
        : IsFrozen(p) ? FreezeOutcome.AlreadyFrozen
        : HungerAt(p, now) < SelfFreezeMinHunger ? FreezeOutcome.TooHungry
        : IsSick(p) ? FreezeOutcome.Sick
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

    // Anyone may feed anyone's Plynling, but only the owner pays the menu price: everyone
    // else pays double — generosity has a cost, and it keeps feeding a friend's a gesture.
    public static long FeedPrice(FoodInfo food, bool isOwner) => isOwner ? food.Price : food.Price * 2;

    public static bool WouldWaste(Plynling p, FoodInfo food, DateTimeOffset now) =>
        (food.Hunger <= 0 || HungerAt(p, now) >= Full) && (food.Happiness <= 0 || HappinessAt(p, now) >= Full);

    public static double MealFactor(Plynling p, DateTimeOffset now)
    {
        var happiness = HappinessAt(p, now);
        return happiness > HappyAbove ? 1 + HappyMealBonus
            : happiness < SadBelow ? 1 - SadMealPenalty
            : 1.0;
    }

    // A draw's result: the cailloux it found, or 0.
    public static long GiftDraw(Random rng) => rng.NextDouble() < GiftChance ? rng.Next(GiftMin, GiftMax + 1) : 0;

    // Whether looking at it now draws today's gift: alive, awake, not frozen, happy, and no draw
    // yet today. An unhappy look does not spend the day.
    public static bool CanDrawGift(Plynling p, DateTimeOffset now) =>
        !IsDead(p) && !IsFrozen(p) && !IsAsleep(now) && !IsSick(p) && HappinessAt(p, now) > HappyAbove
        && p.LastGiftDay != AppTime.DayKey(now);

    public static bool IsSulking(Plynling p, DateTimeOffset now) =>
        HappinessAt(p, now) < SulkBelow && HungerAt(p, now) >= StarvingBelow;

    public static void Feed(Plynling p, FoodInfo food, DateTimeOffset now)
    {
        // its mood *before* this meal cheers it; sick, it only eats half
        var factor = MealFactor(p, now) * (IsSick(p) ? PlynlingSickness.SickMealFactor : 1);
        Rebase(p, now);
        p.Hunger = Clamp(p.Hunger + food.Hunger * factor);
        p.Happiness = Clamp(p.Happiness + food.Happiness);
        if (WarnAt(p) is { } warn && now < warn) p.WarningSent = false;
    }

    public static bool WouldWasteBath(Plynling p, DateTimeOffset now) => HygieneAt(p, now) >= Full;

    public static void Bath(Plynling p, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Hygiene = Clamp(p.Hygiene + BathAmount);
    }

    // What an outing costs in hygiene (a forage, a game).
    public static void Dirty(Plynling p, DateTimeOffset now, double amount)
    {
        Rebase(p, now);
        p.Hygiene = Clamp(p.Hygiene - amount);
    }

    public static long RollPlayPebbles(Random rng) => rng.Next(PlayWinPebblesMin, PlayWinPebblesMax + 1);

    public static void Play(Plynling p, DateTimeOffset now, bool won)
    {
        Rebase(p, now);
        p.Happiness = Clamp(p.Happiness + PlayAmount + (won ? PlayWinBonus : 0));
        p.Hygiene = Clamp(p.Hygiene - PlayDirt);
        p.Plays++;
        if (won) p.PlaysWon++;
    }

    public static void Visit(Plynling p, DateTimeOffset now) => Visit(p, now, VisitAmount);

    // A visit's happiness depends on the bond it ends on (PlynlingBonds.VisitHappiness) —
    // negative between enemies.
    public static void Visit(Plynling p, DateTimeOffset now, double happiness)
    {
        Rebase(p, now);
        p.Happiness = Clamp(p.Happiness + happiness);
        p.Visits++;
    }

    // A refused confession's sting.
    public static void Sadden(Plynling p, DateTimeOffset now, double amount)
    {
        Rebase(p, now);
        p.Happiness = Clamp(p.Happiness - amount);
    }

    // Losing a best friend or a partner: its happiness falls to the grief ceiling at most.
    public static void Grieve(Plynling p, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Happiness = Math.Min(p.Happiness, PlynlingBonds.GriefCeiling);
    }

    public static void Pet(Plynling p, DateTimeOffset now)
    {
        Rebase(p, now);
        p.Happiness = Clamp(p.Happiness + PetAmount);
    }

    public static bool ShouldWarn(Plynling p, DateTimeOffset now) =>
        !p.WarningSent && EffectiveDeathAt(p) is { } death && death > now && WarnAt(p) is { } warn && now >= warn;

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
        p.Hygiene = ResurrectHygiene;
        p.NeedsAsOf = now;
        p.LiveSince = now;
        p.LastMorningDay = MorningDayAtOrBefore(now);
        p.DeathCause = DeathCause.Starvation;
        Cure(p);
    }

    // Stores the current values as of `at`. Only meaningful while alive and not frozen.
    // Every value is computed before any is stored: HappinessAt reads the stored hygiene.
    private static void Rebase(Plynling p, DateTimeOffset at)
    {
        var hunger = HungerAt(p, at);
        var happiness = HappinessAt(p, at);
        var hygiene = HygieneAt(p, at);
        p.Hunger = hunger;
        p.Happiness = happiness;
        p.Hygiene = hygiene;
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
