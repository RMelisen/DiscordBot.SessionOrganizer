namespace ProjectSYNCS.Helpers;

public enum BlameKind
{
    // Someone in the call, not the asker.
    Person,
    // The asker drew themselves.
    Self,
    Quokka,
    // The roll landed on Papa: she names him, then edits it into a denial and someone else
    // (BlameResult.TargetId).
    PapaDodge,
    // The roll landed on Papa and nobody else is there: the same slip, corrected to Quokka.
    PapaDodgeQuokka,
    // Nobody in voice: Quokka, by default.
    Nobody,
}

/// <param name="TargetId">Who gets the blame, for Person, Self and PapaDodge; 0 otherwise.</param>
public sealed record BlameResult(BlameKind Kind, ulong TargetId = 0);

/// <summary>
/// /blame's draw, pure. Everyone in the call has one slot, and Quokka one more — he isn't there,
/// but it's always a little bit his fault. She is never a candidate, and she never blames Papa:
/// a roll on him is visibly taken back.
/// </summary>
public static class Blame
{
    /// <summary>
    /// A roll on Papa is posted as a plain verdict, then edited into the denial after a delay drawn
    /// in [Min, Max) milliseconds: long enough for everyone to read his name, as with her robot
    /// heart's edit slips.
    /// </summary>
    public static readonly (int Min, int Max) SlipDelayMs = (3500, 5000);

    public static bool IsPapaSlip(BlameKind kind) => kind is BlameKind.PapaDodge or BlameKind.PapaDodgeQuokka;

    /// <param name="candidates">The humans in the call (her and other bots already left out).</param>
    public static BlameResult Pick(IReadOnlyList<ulong> candidates, ulong askerId, ulong ownerId, Random rng)
    {
        if (candidates.Count == 0) return new(BlameKind.Nobody);

        var slot = rng.Next(candidates.Count + 1);
        if (slot == candidates.Count) return new(BlameKind.Quokka);

        var target = candidates[slot];
        if (target == ownerId)
        {
            var others = candidates.Where(id => id != ownerId).ToList();
            return others.Count == 0
                ? new(BlameKind.PapaDodgeQuokka)
                : new(BlameKind.PapaDodge, others[rng.Next(others.Count)]);
        }

        return new(target == askerId ? BlameKind.Self : BlameKind.Person, target);
    }
}
