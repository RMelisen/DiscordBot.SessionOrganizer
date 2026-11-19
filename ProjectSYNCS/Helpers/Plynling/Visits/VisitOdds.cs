namespace ProjectSYNCS.Helpers;

/// <summary>
/// The hidden odds of a visit with choices (docs/agents/plynling.md, Relationships). The visitor's
/// option is judged by the host's personality and the host's reaction by the visitor's: each shifts
/// the base chance by at most <see cref="MaxShiftPerChoice"/>, so luck always stays. Nothing here is
/// ever shown to players. Tuned by simulation (tests/ProjectSYNCS.Checks, <c>-- visit-sim</c>), never by feel.
/// </summary>
public static class VisitOdds
{
    public const double MaxShiftPerChoice = 0.075;

    /// <summary>
    /// How much match it takes to approach the cap: a lean of ±2 on a ±200 trait axis is a match of
    /// 400, so at 1000 it gives tanh(0.4) ≈ 0.38 of the cap, and only a strong match on two axes nears
    /// it. Set by the simulation, with the confession offered on a quarter of the days
    /// (<see cref="PlynlingBonds.ConfessionOfferChance"/>): best play then stays above 0.8 × the old
    /// couple time.
    /// </summary>
    public const double LeanSoftness = 1000;

    /// <summary>The raw match of a choice with a judge: Σ lean × the judge's axis.</summary>
    public static double Match(IReadOnlyDictionary<AiAxis, int> lean, IReadOnlyDictionary<AiAxis, int> judgeAxes) =>
        lean.Sum(kv => (double)kv.Value * judgeAxes.GetValueOrDefault(kv.Key));

    /// <summary>The shift one choice gives: the sign of the match, never past the cap.</summary>
    public static double Shift(IReadOnlyDictionary<AiAxis, int> lean, IReadOnlyDictionary<AiAxis, int> judgeAxes) =>
        MaxShiftPerChoice * Math.Tanh(Match(lean, judgeAxes) / LeanSoftness);

    /// <summary>Whether the visit goes well: today's scene chance plus both choices' shift, within 5–95 %.</summary>
    public static double GoodChance(int compatibility, PlynlingBond bond, int affinity, double shift) =>
        Math.Clamp(PlynlingBonds.GoodSceneChance(compatibility, bond, affinity) + shift, 0.05, 0.95);

    /// <summary>
    /// Whether a confession is accepted: 50 % for an average pair, 85 % / 15 % at the extremes of
    /// compatibility, plus both choices' shift, within 5–95 %.
    /// </summary>
    public static double AcceptChance(int compatibility, double shift) =>
        Math.Clamp(0.5 + 0.35 * compatibility / PlynlingBonds.MaxCompatibility + shift, 0.05, 0.95);

    /// <summary>Her own Plynling reacts on her own: the reaction whose lean best matches her axes; a tie goes to file order.</summary>
    public static VisitChoice MascotReaction(VisitScene scene, IReadOnlyDictionary<AiAxis, int> herAxes)
    {
        var best = scene.Reactions[0];
        var bestMatch = Match(best.Lean, herAxes);
        foreach (var r in scene.Reactions.Skip(1))
        {
            var m = Match(r.Lean, herAxes);
            if (m > bestMatch) (best, bestMatch) = (r, m);
        }
        return best;
    }
}
