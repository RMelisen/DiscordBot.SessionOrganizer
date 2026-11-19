using System.Text.RegularExpressions;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// One Plynling as a told story shows it — captured when the visit happened, so paging back through
// the story later needs no database and shows them as they were. The picture is built per step
// from the species, the stage and that step's face.
public sealed record VisitCast(string Name, PlynlingSpecies Species, PlynlingStage Stage, string SpeciesName,
    PlynlingGender Gender, IReadOnlyList<Passion> Passions)
{
    public string Sprite(PlynlingMood face) => PlynlingArt.VisitSprite(Species, Stage, face);
}

// One step of a story: its text and each Plynling's face on it.
public sealed record VisitBeat(string Text, PlynlingMood VisitorFace, PlynlingMood HostFace);

// A face tag read off the start of a line: [sad] (Both), or [A:sad B:happy] for the visitor and
// the host separately. See PlynlingVisitStory.Untag.
public sealed record FaceTag(PlynlingMood? Both, PlynlingMood? A, PlynlingMood? B);

// A visit told in beats, from before 8.2.0. Visits are no longer told (they are played, through
// PlynlingVisitCards); the record stays so the stories VisitStories still keeps page until they expire.
public sealed record VisitStory(string Id, string Heading, IReadOnlyList<VisitBeat> Beats, VisitCast Visitor, VisitCast Host, uint Accent);

// The mood a visit ends on: the bond after it, or Conflict when the scene went badly (or the two
// are enemies — there is no friendly way to be that). It picks the ending's faces.
public enum VisitMood { Acquaintances, Friends, BestFriends, Lovers, Rivals, Conflict }

/// <summary>
/// What a visit's text shares with the played visit (PlynlingVisitCards): the moods and faces, the
/// face tags, and the templates.
/// <para>
/// Lines are templates: {A} and {B} are the visitor and the host (bold), {ils} / {Ils} the pair
/// (« elles » only when both are girls), and {a:m|f}, {b:m|f}, {p:m|f} a word agreeing with the
/// visitor, the host or the pair.
/// </para>
/// </summary>
public static class PlynlingVisitStory
{
    public static VisitMood MoodFor(bool goodScene, PlynlingBond after) => !goodScene ? VisitMood.Conflict : after switch
    {
        PlynlingBond.Friends => VisitMood.Friends,
        PlynlingBond.BestFriends => VisitMood.BestFriends,
        PlynlingBond.Lovers => VisitMood.Lovers,
        PlynlingBond.Rivals => VisitMood.Rivals,
        PlynlingBond.Enemies => VisitMood.Conflict,
        _ => VisitMood.Acquaintances,
    };

    // The default faces by mood: whoever talks, whoever listens, and both during narration.
    internal static (PlynlingMood Speaker, PlynlingMood Listener, PlynlingMood Narration) Faces(VisitMood mood) => mood switch
    {
        VisitMood.Acquaintances => (PlynlingMood.Happy, PlynlingMood.Content, PlynlingMood.Content),
        VisitMood.Rivals => (PlynlingMood.Happy, PlynlingMood.Angry, PlynlingMood.Content),
        VisitMood.Conflict => (PlynlingMood.Angry, PlynlingMood.Angry, PlynlingMood.Angry),
        _ => (PlynlingMood.Happy, PlynlingMood.Happy, PlynlingMood.Happy),
    };

    // The parting faces a visit's outcome imposes, or null for an ordinary one.
    internal static PlynlingMood? OutcomeFace(VisitOutcome o) =>
        o.Confession == Confession.Refused || (o.Before == PlynlingBond.Lovers && o.After != PlynlingBond.Lovers) ? PlynlingMood.Sad
        : o.Confession == Confession.Accepted ? PlynlingMood.Happy
        : o.After == PlynlingBond.Enemies && o.Before != PlynlingBond.Enemies ? PlynlingMood.Angry
        : null;

    private static readonly Regex TagRx = new(
        @"^\[(?:(happy|content|sad|angry)|(?:A:(happy|content|sad|angry))?\s*(?:B:(happy|content|sad|angry))?)\]\s*",
        RegexOptions.Compiled);

    /// <summary>
    /// Reads a face tag off the start of a line — [sad], or [A:sad B:happy] — and returns the line
    /// without it. A line with no tag comes back unchanged with a null tag.
    /// </summary>
    public static (string Text, FaceTag? Tag) Untag(string line)
    {
        var m = TagRx.Match(line);
        if (!m.Success || m.Length <= 2) return (line, null);
        static PlynlingMood? Face(Group g) => g.Success ? Enum.Parse<PlynlingMood>(g.Value, ignoreCase: true) : null;
        return (line[m.Length..], new FaceTag(Face(m.Groups[1]), Face(m.Groups[2]), Face(m.Groups[3])));
    }

    private static readonly Regex Choice = new(@"\{([abp]):([^|}]*)\|([^}]*)\}", RegexOptions.Compiled);

    /// <summary>Fills a template's names, pronouns and agreements.</summary>
    public static string Expand(string template, string a, PlynlingGender ga, string b, PlynlingGender gb)
    {
        var girls = ga == PlynlingGender.Female && gb == PlynlingGender.Female;
        var text = Choice.Replace(template, m =>
        {
            var female = m.Groups[1].Value switch
            {
                "a" => ga == PlynlingGender.Female,
                "b" => gb == PlynlingGender.Female,
                _ => girls,
            };
            return female ? m.Groups[3].Value : m.Groups[2].Value;
        });
        return text
            .Replace("{A}", $"**{a}**").Replace("{B}", $"**{b}**")
            .Replace("{ils}", girls ? "elles" : "ils").Replace("{Ils}", girls ? "Elles" : "Ils");
    }
}
