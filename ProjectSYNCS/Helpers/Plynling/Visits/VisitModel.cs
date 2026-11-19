namespace ProjectSYNCS.Helpers;

// A parsed visit scene (`Stories/Visits/*.visit`, docs/agents/plynling-storylines.md, Visit files).
// Scene, option and reaction keys travel in custom-ids and are forever: never renamed, never reused.

/// <summary>
/// A choice's two half-endings. In an ordinary scene, the good and the bad one; in the confession, the
/// answer: <see cref="Good"/> is yes, <see cref="Bad"/> is no. A face tag may open either.
/// </summary>
public sealed record VisitHalves(string Good, string Bad);

/// <summary>
/// One of the visitor's options or one of the host's reactions. <see cref="Lean"/> is judged by the
/// <em>other</em> Plynling's personality (<see cref="VisitOdds.Shift"/>).
/// </summary>
public sealed record VisitChoice(string Key, string Label, IReadOnlyDictionary<AiAxis, int> Lean, VisitHalves Halves, StoryPos Pos);

/// <summary>
/// One visit scene. <c>{A}</c> is always the visitor, <c>{B}</c> always the host. Hours are Paris
/// wall-clock hours, <see cref="FromHour"/> included and <see cref="ToHour"/> excluded, across midnight
/// when From is above To (20-1). The confession scene has no bonds: it is offered whenever a pair may
/// confess, whatever the bond.
/// </summary>
public sealed record VisitScene(string Key, bool Confession, string Title, string Place,
    IReadOnlySet<PlynlingBond> Bonds, int FromHour, int ToHour, IReadOnlySet<PlynlingStage> Stages,
    string Intro, IReadOnlyList<VisitChoice> Options, IReadOnlyList<VisitChoice> Reactions, StoryPos Pos)
{
    public bool IsOpenAt(int hour) =>
        FromHour <= ToHour ? hour >= FromHour && hour < ToHour : hour >= FromHour || hour < ToHour;

    /// <summary>May this ordinary scene be drawn for a pair with this bond (before the visit), at this Paris hour?</summary>
    public bool Fits(PlynlingBond bond, int hour, PlynlingStage visitor, PlynlingStage host) =>
        !Confession && Bonds.Contains(bond) && IsOpenAt(hour) && Stages.Contains(visitor) && Stages.Contains(host);

    public VisitChoice? Option(string key) => Options.FirstOrDefault(o => o.Key == key);

    public VisitChoice? Reaction(string key) => Reactions.FirstOrDefault(r => r.Key == key);
}
