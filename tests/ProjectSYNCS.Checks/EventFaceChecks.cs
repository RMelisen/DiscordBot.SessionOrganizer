using Discord;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 59. The pictures on an event's story: the faces its outcome leaves (PlynlingEventFaces), the face
// tags a story outcome may open on, and the stage each Plynling is shown at (the one it had then).
public static class EventFaceChecks
{
    private static Action<string> _fail = _ => { };
    private static readonly DateTimeOffset Now = new(2026, 11, 23, 12, 0, 0, TimeSpan.Zero);

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("event faces: " + what);
    }

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        Tags();
        Faces();
        Card();
        Stages();
    }

    // A short arc whose one option's outcome is `outcome` (its lines, then [end]).
    private static string Arc(string outcome, string other = "other: anyone\n", string scene = "{A} est là.", string option2 = "") =>
        $"# arc: t_face\ntitle: T\nfamily: personal\nstage: baby\ntrigger: visit\nsize: short\n{other}\n"
        + $"## scene: only\ntitle: U\npace: now\n{scene}\n\n* a | A\n{outcome}\n  [end]\n\n* b | B\n  Non.\n  [end]\n{option2}";

    private static (StoryArc? Arc, List<StoryError> Errors) Parse(string text)
    {
        var errors = new List<StoryError>();
        return (StoryParser.Parse("t.story", text, errors), errors);
    }

    private static void Tags()
    {
        var (sad, e1) = Parse(Arc("  [sad] {A} soupire."));
        var face = sad?.Scenes[0].Options[0].Outcome?.Face;
        Expect(e1.Count == 0 && face == new FaceTag(PlynlingMood.Sad, null, null), "[sad] opening an outcome is its face: " + string.Join("; ", e1.Select(e => e.Message)));
        Expect(sad?.Scenes[0].Options[0].Outcome?.Prose[0].Lines[0].Text == "{A} soupire.", "the face tag is not part of the prose");

        var (both, e2) = Parse(Arc("  [A:happy B:angry] {A} rit, {B} boude."));
        Expect(e2.Count == 0 && both?.Scenes[0].Options[0].Outcome?.Face == new FaceTag(null, PlynlingMood.Happy, PlynlingMood.Angry), "[A:… B:…] names each");

        var (cond, e3) = Parse(Arc("  [content] [if stage baby] {A} attend."));
        Expect(e3.Count == 0 && cond?.Scenes[0].Options[0].Outcome?.Face?.Both == PlynlingMood.Content
               && cond.Scenes[0].Options[0].Outcome!.Prose[0].Lines[0].When.Count == 1, "a face tag may come before a condition");

        Expect(Parse(Arc("  {A} attend.\n  [sad] {A} soupire.")).Errors.Any(e => e.Message.Contains("goes first")), "a face tag on a later line is refused");
        Expect(Parse(Arc("  [if stage baby] [sad] {A} soupire.")).Errors.Any(e => e.Message.Contains("goes first")), "a face tag after a condition is refused");
        Expect(Parse(Arc("  Oui.", scene: "[happy] {A} est là.")).Errors.Any(e => e.Message.Contains("only opens an option's outcome")), "a face tag in a scene's prose is refused");
        Expect(Parse(Arc("  [cheerful] {A} rit.")).Errors.Any(e => e.Message.Contains("[cheerful]")), "an unknown face word is not a face tag (refused as a condition)");

        // The fixture carries the word, as every word of the format must.
        var fixturePath = Path.Combine(StoryChecks.RepoRoot(), "tests", "ProjectSYNCS.Checks", "Stories", "test_short.story");
        var (fixture, e5) = Parse(File.ReadAllText(fixturePath));
        Expect(e5.Count == 0 && fixture?.Scenes[0].Options.Single(o => o.Key == "toss").Failure?.Face?.Both == PlynlingMood.Sad,
            "fixture test_short: its failed toss opens on [sad]");

        // [… B:…] needs an other Plynling to show.
        var (alone, e4) = Parse(Arc("  [A:sad B:happy] {A} soupire.", other: ""));
        var noOther = alone is null ? [] : StoryValidator.Validate([alone], StoryCatalog.Cast.Values.ToList());
        Expect(e4.Count == 0 && noOther.Any(e => e.Message.Contains("names the other Plynling")), "a B: face without 'other' is refused");
        var (paired, _) = Parse(Arc("  [A:sad B:happy] {A} soupire."));
        Expect(paired is not null && !StoryValidator.Validate([paired], StoryCatalog.Cast.Values.ToList()).Any(e => e.Message.Contains("names the other Plynling")),
            "a B: face with 'other' is fine");
    }

    private static EventOption Fx(IReadOnlyList<EventEffect> success, IReadOnlyList<EventEffect>? failure = null, EventChallenge? roll = null) =>
        new("o", "o", "", failure is null ? null : "", null, roll, success, failure ?? [], new Dictionary<string, int>(), new Dictionary<AiAxis, int>());

    private static (PlynlingMood Self, PlynlingMood Target) F(EventOption o, PlynlingEventInstance? inst = null)
    {
        var def = new EventDef("t.def", EventType.Pulse, [PlynlingStage.Baby], "T", "Scène", [o]);
        return PlynlingEventFaces.Outcome(inst ?? new PlynlingEventInstance(), def, o);
    }

    private static void Faces()
    {
        var calm = (PlynlingMood.Content, PlynlingMood.Content);
        Expect(F(Fx([new ApplyModifier("light_heart")])) == (PlynlingMood.Happy, PlynlingMood.Content), "a pleasant modifier: {A} happy, {B} calm");
        Expect(F(Fx([new GrowStat(PlynlingStat.Learning)])) == calm, "a lesson alone: calm");
        Expect(F(Fx([])) == calm, "nothing: calm");
        Expect(F(Fx([new AffinityShift(5)])) == (PlynlingMood.Happy, PlynlingMood.Happy), "affinity gained: both happy");
        Expect(F(Fx([new AffinityShift(-5)])) == (PlynlingMood.Sad, PlynlingMood.Sad), "affinity lost: both sad");
        Expect(F(Fx([new StressChange(-10)]), new PlynlingEventInstance { StressDelta = -10 }).Self == PlynlingMood.Happy, "stress relief: happy");
        Expect(F(Fx([new ApplyModifier("party_soul")]), new PlynlingEventInstance { StressDelta = 15 }).Self == PlynlingMood.Content,
            "a stress cost with a pleasant modifier: mixed, calm");
        Expect(F(Fx([new ApplyModifier("grumpy")])).Self == PlynlingMood.Sad, "an unpleasant modifier: sad");
        Expect(F(Fx([new ApplyModifier("grumpy")]), new PlynlingEventInstance { BondBefore = PlynlingBond.Enemies, BondAfter = PlynlingBond.Enemies }).Self == PlynlingMood.Angry,
            "an unpleasant modifier between enemies: angry");
        Expect(F(Fx([new ApplyModifier("grumpy")]), new PlynlingEventInstance { DecidedAlone = true }).Self == PlynlingMood.Content,
            "decided alone, the unpleasant modifier did not apply: calm");

        var roll = new EventChallenge(PlynlingStat.Courage, 3);
        var rolled = Fx([new GrowStat(PlynlingStat.Courage), new ApplyModifier("fired_up")], [new ApplyModifier("muddy_paws")], roll);
        Expect(F(rolled, new PlynlingEventInstance { ChallengeSucceeded = true }).Self == PlynlingMood.Happy, "a roll won with a pleasant modifier: happy");
        Expect(F(rolled, new PlynlingEventInstance { ChallengeSucceeded = false }).Self == PlynlingMood.Sad, "a roll lost: sad");

        Expect(F(Fx([new AffinityShift(10)]), new PlynlingEventInstance { BondBefore = PlynlingBond.Acquaintances, BondAfter = PlynlingBond.Friends })
               == (PlynlingMood.Happy, PlynlingMood.Happy), "a closer bond: both happy");
        Expect(F(Fx([new AffinityShift(-10)]), new PlynlingEventInstance { BondBefore = PlynlingBond.Acquaintances, BondAfter = PlynlingBond.Rivals })
               == (PlynlingMood.Angry, PlynlingMood.Angry), "a bond turned hostile: both angry");
        Expect(F(Fx([new Couple()]), new PlynlingEventInstance { BondBefore = PlynlingBond.BestFriends, BondAfter = PlynlingBond.Lovers })
               == (PlynlingMood.Happy, PlynlingMood.Happy), "a couple made: both happy");
        Expect(F(Fx([new Couple()]), new PlynlingEventInstance { BondBefore = PlynlingBond.BestFriends }) == (PlynlingMood.Sad, PlynlingMood.Sad),
            "a couple too late: both sad");
        Expect(F(Fx([new Heartbreak()])).Target == PlynlingMood.Sad, "a heartbreak: the one who declared is sad");

        // A face tag wins over the effects: [face] for {A}, [A:… B:…] for each.
        var tagged = Compile(Arc("  [sad] {A} soupire.\n  [modifier light_heart]", option2: ""));
        var both = Compile(Arc("  [A:angry B:happy] {A} boude.\n  [modifier light_heart]"));
        Expect(tagged is not null && FacesOf(tagged) == (PlynlingMood.Sad, PlynlingMood.Content), "[sad] overrides a pleasant modifier for {A} only");
        Expect(both is not null && FacesOf(both) == (PlynlingMood.Angry, PlynlingMood.Happy), "[A:angry B:happy] sets both");
        var untagged = Compile(Arc("  {A} sourit.\n  [modifier light_heart]"));
        Expect(untagged is not null && FacesOf(untagged) == (PlynlingMood.Happy, PlynlingMood.Content), "a compiled story option without a tag reads its effects");

        // Every shipped outcome yields a face; the split is printed (chosen by its owner, stress as its
        // effects say, no bond change — a bond moved only adds to happy or sad).
        var split = new Dictionary<PlynlingMood, int>();
        foreach (var def in PlynlingEvents.All)
            foreach (var o in def.Options)
                foreach (var won in o.Challenge is null ? new bool?[] { null } : [true, false])
                {
                    var effects = won == false ? o.OnFailure : o.OnSuccess;
                    var stress = effects.OfType<StressChange>().Sum(s => s.Amount) + o.StressCosts.Values.Sum();
                    var self = PlynlingEventFaces.Outcome(new PlynlingEventInstance { ChallengeSucceeded = won, StressDelta = stress }, def, o).Self;
                    split[self] = split.GetValueOrDefault(self) + 1;
                }
        Console.WriteLine("event outcome faces: " + string.Join(", ", split.OrderByDescending(kv => kv.Value).Select(kv => $"{kv.Key} {kv.Value}")));
    }

    private static EventDef? Compile(string text)
    {
        var (arc, errors) = Parse(text);
        if (arc is null || errors.Count > 0) { Expect(false, "fixture: " + string.Join("; ", errors.Select(e => e.Message))); return null; }
        return StoryCompiler.Compile(new StoryLoad([arc], StoryCatalog.Cast.Values.ToList(), [])).Single();
    }

    private static (PlynlingMood, PlynlingMood) FacesOf(EventDef def) =>
        PlynlingEventFaces.Outcome(new PlynlingEventInstance(), def, def.Options.Single(o => o.Key == "a"));

    // The card: calm faces on every page but the last, the outcome's on the last one.
    private static void Card()
    {
        var self = new EventCast("Brindille", PlynlingGender.Female, default, PlynlingStage.Teen, 1);
        var other = new EventCast("Morille", PlynlingGender.Male, default, PlynlingStage.Adult, 2);
        var story = new EventStory(1, 1, "📜 T", ["scène", "choix", "fin"], self, other, 0, PlynlingMood.Sad, PlynlingMood.Happy);
        string Urls(int page) => string.Join(" ", PlynlingEventCards.BuildStory(story, page).Components
            .OfType<ContainerComponent>().SelectMany(c => c.Components).OfType<MediaGalleryComponent>()
            .SelectMany(g => g.Items).Select(i => i.Media.Url));
        Expect(Urls(0).Contains("_content_visit") && !Urls(0).Contains("_sad_") && !Urls(1).Contains("_happy_"), "the scene and choice pages are calm: " + Urls(0));
        var end = Urls(2);
        Expect(end.Contains("_sad_visit") && end.Contains("_happy_visit") && end.IndexOf("_sad_", StringComparison.Ordinal) < end.IndexOf("_happy_", StringComparison.Ordinal),
            "the last page shows {A} sad then {B} happy: " + end);
        var single = story with { Pages = ["seule"] };
        Expect(!string.Join(" ", PlynlingEventCards.BuildStory(single, 0).Components.OfType<ContainerComponent>()
                   .SelectMany(c => c.Components).OfType<MediaGalleryComponent>().SelectMany(g => g.Items).Select(i => i.Media.Url)).Contains("_sad_"),
            "a one-page story (details lost) stays calm");
    }

    // The stage shown is the one it had when the event resolved: stored, or estimated for older rows.
    private static void Stages()
    {
        // Lived 20 days, alive since; the event resolved 10 days ago, when it was 10 days old (teen).
        var p = new Plynling { Name = "Brindille", AgeBankedSeconds = 0, LiveSince = Now.AddDays(-20) };
        var resolved = Now.AddDays(-10);
        Expect(PlynlingLife.Stage(p, Now) == PlynlingStage.Adult, "fixture: adult today");
        Expect(EventCast.Then(p, (long)TimeSpan.FromDays(3).TotalSeconds, resolved, Now).Stage == PlynlingStage.Baby, "a stored age wins: 3 days old, a baby");
        Expect(EventCast.Then(p, null, resolved, Now).Stage == PlynlingStage.Teen, "no stored age: estimated at the time, a teen");
        // Frozen 2 days after the event: today's age counts no time since the freeze.
        var frozen = new Plynling { Name = "Givre", AgeBankedSeconds = (long)TimeSpan.FromDays(12).TotalSeconds, LiveSince = Now.AddDays(-30), FrozenAt = Now.AddDays(-8) };
        Expect(PlynlingLife.AgeAt(frozen, resolved, Now) == TimeSpan.FromDays(10), "frozen since: age then = banked less the time lived after it: "
               + PlynlingLife.AgeAt(frozen, resolved, Now).TotalDays);
        Expect(PlynlingLife.AgeAt(p, Now.AddDays(-400), Now) == TimeSpan.Zero, "never below zero");
    }
}
