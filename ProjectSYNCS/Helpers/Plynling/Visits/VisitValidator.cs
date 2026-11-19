namespace ProjectSYNCS.Helpers;

/// <summary>
/// The checks a set of visit scenes needs before the bot may draw from it. Every problem is reported,
/// with its file and line. <see cref="Coverage"/> is separate: the fixtures validate without covering
/// the draw, the shipped scenes must do both (<see cref="VisitLibrary.Load"/>).
/// </summary>
public static class VisitValidator
{
    // Custom-id budget (PlynlingVisitCards): scene and choice keys ride in every button's id.
    public const int SceneKeyCap = 20;
    public const int ChoiceKeyCap = 12;
    public const int MinChoices = 2, MaxChoices = 4;
    public const int MaxLeans = 2;
    public const int LeanCap = 3;

    // {ils} / {Ils} are visit tokens on top of the story format's (PlynlingVisitStory.Expand renders them).
    private static string WithoutPronouns(string text) => text.Replace("{ils}", "elles").Replace("{Ils}", "Elles");

    public static string WorstCase(string text) => StoryValidator.WorstCase(WithoutPronouns(text));

    public static IReadOnlyList<StoryError> Validate(IReadOnlyList<VisitScene> scenes)
    {
        var errors = new List<StoryError>();
        void Error(StoryPos at, string message) => errors.Add(new StoryError(at, message));

        var byKey = new Dictionary<string, VisitScene>();
        foreach (var s in scenes)
            if (!byKey.TryAdd(s.Key, s)) Error(s.Pos, $"scene key '{s.Key}' already used at {byKey[s.Key].Pos}");

        var confessions = scenes.Where(s => s.Confession).ToList();
        if (confessions.Count != 1)
            Error(confessions.Count > 1 ? confessions[1].Pos : new StoryPos("Visits", 0),
                $"exactly one confession scene ('# confession: <key>'), found {confessions.Count}");

        foreach (var s in scenes) Scene(s, Error);
        return errors;
    }

    private static void Scene(VisitScene s, Action<StoryPos, string> error)
    {
        var said = $"scene '{s.Key}'";
        if (s.Key.Length > SceneKeyCap) error(s.Pos, $"{said}: the key is over {SceneKeyCap} characters (it rides in every button's id)");
        if (!s.Confession && s.Bonds.Count == 0) error(s.Pos, $"{said}: no bond; write 'bonds:' with at least one of {StoryVocabulary.Choices<PlynlingBond>()}");
        if (s.Title.Length == 0) error(s.Pos, $"{said}: no title");
        if (s.Place.Length == 0) error(s.Pos, $"{said}: no place");
        else if (WorstCase(s.Place).Length > StoryValidator.TitleCap) error(s.Pos, $"{said}: the place is over {StoryValidator.TitleCap} characters at worst");
        if (s.Intro.Length == 0) error(s.Pos, $"{said}: no intro");
        if (s.Options.Count is < MinChoices or > MaxChoices) error(s.Pos, $"{said}: {MinChoices} to {MaxChoices} options, found {s.Options.Count}");
        if (s.Reactions.Count is < MinChoices or > MaxChoices) error(s.Pos, $"{said}: {MinChoices} to {MaxChoices} reactions, found {s.Reactions.Count}");

        var keys = new HashSet<string>();
        foreach (var c in s.Options.Concat(s.Reactions))
        {
            var what = $"choice '{c.Key}'";
            if (!keys.Add(c.Key)) error(c.Pos, $"{what}: key given twice in the scene (options and reactions share one set of keys)");
            if (c.Key.Length > ChoiceKeyCap) error(c.Pos, $"{what}: the key is over {ChoiceKeyCap} characters (it rides in the button's id)");
            if (c.Lean.Count is < 1 or > MaxLeans) error(c.Pos, $"{what}: 1 or {MaxLeans} leans, found {c.Lean.Count}");
            foreach (var (axis, n) in c.Lean)
                if (n == 0 || Math.Abs(n) > LeanCap) error(c.Pos, $"{what}: [lean {axis.ToString().ToLowerInvariant()} {n}]: write -{LeanCap} to +{LeanCap}, not 0");
            if (c.Label.Length == 0) error(c.Pos, $"{what}: empty label");
            else if (WorstCase(c.Label).Length > StoryValidator.LabelCap) error(c.Pos, $"{what}: the label is over {StoryValidator.LabelCap} characters at worst");
            var (good, bad) = s.Confession ? ("yes", "no") : ("good", "bad");
            if (c.Halves.Good.Length == 0) error(c.Pos, $"{what}: the '{good}' half is empty");
            if (c.Halves.Bad.Length == 0) error(c.Pos, $"{what}: the '{bad}' half is empty");
        }

        foreach (var (text, pos, what) in Texts(s))
        {
            foreach (var bad in StoryValidator.BadTokens(WithoutPronouns(text)))
                error(pos, $"{what}: '{bad}' is not a visit token ({{A}}, {{B}}, {{a:…|…}}, {{b:…|…}}, {{p:…|…}}, {{ils}}, {{Ils}})");
            if (text.Contains("{T}")) error(pos, $"{what}: '{{T}}' is not a visit token (it is a story's revealed trait)");
        }
    }

    /// <summary>Every text a player may read in the scene, with where it is and what it is.</summary>
    public static IEnumerable<(string Text, StoryPos Pos, string What)> Texts(VisitScene s)
    {
        yield return (s.Title, s.Pos, $"scene '{s.Key}' title");
        yield return (s.Place, s.Pos, $"scene '{s.Key}' place");
        yield return (s.Intro, s.Pos, $"scene '{s.Key}' intro");
        foreach (var c in s.Options.Concat(s.Reactions))
        {
            yield return (c.Label, c.Pos, $"choice '{c.Key}' label");
            yield return (c.Halves.Good, c.Pos, $"choice '{c.Key}' half");
            yield return (c.Halves.Bad, c.Pos, $"choice '{c.Key}' half");
        }
    }

    /// <summary>
    /// The draw can never come up empty: every bond, at every Paris hour, for every pair of stages, has a
    /// scene. One error per bond and stage pair that misses an hour, naming the hours.
    /// </summary>
    public static IReadOnlyList<StoryError> Coverage(IReadOnlyList<VisitScene> scenes)
    {
        var errors = new List<StoryError>();
        foreach (var bond in Enum.GetValues<PlynlingBond>())
            foreach (var visitor in Enum.GetValues<PlynlingStage>())
                foreach (var host in Enum.GetValues<PlynlingStage>())
                {
                    var missing = Enumerable.Range(0, 24).Where(h => !scenes.Any(s => s.Fits(bond, h, visitor, host))).ToList();
                    if (missing.Count > 0)
                        errors.Add(new StoryError(new StoryPos("Visits", 0),
                            $"no visit scene for {bond.ToString().ToLowerInvariant()} (visitor {visitor.ToString().ToLowerInvariant()}, host {host.ToString().ToLowerInvariant()}) at {string.Join(", ", missing.Select(h => h + "h"))}"));
                }
        return errors;
    }
}
