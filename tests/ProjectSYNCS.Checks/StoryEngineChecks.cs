using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 50. Story engine (conditions, knowledge, standing, prose, the compiler and the catalog): what a
// story reads about a Plynling and the village, and the events it becomes. Contexts are built by
// hand, as the story walker does.
public static class StoryEngineChecks
{
    private static Action<string> _fail = _ => { };
    private static readonly StoryPos P = new("t.story", 1);
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("story engine: " + what);
    }

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        Conditions();
        Standing();
        Render();
        Arrival();
        Fixtures();
        Openings();
        Triggered();
        Rules();
        Lookups();
        Runs();
        Stakes();
        ChoiceCaps();
        Probes();  // last: it swaps StoryCatalog.Folder and puts it back
    }

    private static EventOption Fx(string key, IReadOnlyList<EventEffect> success, IReadOnlyList<EventEffect>? failure = null,
        EventGate? gate = null, EventChallenge? roll = null, bool mystery = false, IReadOnlyDictionary<string, int>? stress = null) =>
        new(key, key, "", failure is null ? null : "", gate, roll, success, failure ?? [], stress ?? new Dictionary<string, int>(),
            new Dictionary<AiAxis, int>(), Mystery: mystery);

    private static EventDef StakesDef(params EventOption[] options) =>
        new("t.def", EventType.Pulse, [PlynlingStage.Baby], "T", "Scène", options);

    private static string Stake(EventOption o, EventContext? ctx = null, EventDef? def = null) =>
        PlynlingEventStakes.Line(o, def ?? StakesDef(o), ctx ?? Ctx(), PlynlingGender.Female, null);

    // 50l. The stakes line: costs, then gains; a padlock for a locked option; « on verra bien » for a mystery.
    private static void Stakes()
    {
        var cailloux = Stake(Fx("a", [new GiveCailloux(15)]));
        Expect(cailloux.Contains("🪙 15") && !cailloux.Contains("🎲"), "stakes: cailloux: " + cailloux);
        var soap = ItemCatalog.ByKey("care.soap")!;
        var roll = new EventChallenge(PlynlingStat.Courage, 6);
        var rolled = Stake(Fx("r", [new GiveItem("care.soap")], [], roll: roll));
        Expect(rolled.Contains("🎲") && rolled.Contains("%") && rolled.Contains(ItemCatalog.ClearName(soap)) && !rolled.Contains("sinon"),
            "stakes: a roll with an item and nothing on failure: " + rolled);
        var both = Stake(Fx("r", [new GiveItem("care.soap")], [new GiveCailloux(5)], roll: roll));
        Expect(both.Contains("sinon") && both.Contains("🪙 5"), "stakes: sinon when failure gives something else: " + both);
        var same = Stake(Fx("r", [new GiveCailloux(5)], [new GiveCailloux(5)], roll: roll));
        Expect(!same.Contains("sinon"), "stakes: no sinon when failure gives the same: " + same);
        var mystery = Stake(Fx("m", [new GiveItem("care.soap")], [], roll: roll, mystery: true, stress: new Dictionary<string, int> { ["shy"] = 20 }));
        Expect(mystery.Contains("🎲") && mystery.Contains("❔ on verra bien") && !mystery.Contains(ItemCatalog.ClearName(soap)),
            "stakes: a mystery shows the odds and hides the reward: " + mystery);
        // Costs survive the mystery: a shy one pays the stress, shown beside « on verra bien ».
        var shyMystery = Stake(Fx("m", [new GiveItem("care.soap")], [], mystery: true, stress: new Dictionary<string, int> { ["shy"] = 20 }), Ctx(traits: ["shy"]));
        Expect(shyMystery.Contains("😣") && shyMystery.Contains("❔ on verra bien") && !shyMystery.Contains(ItemCatalog.ClearName(soap)),
            "stakes: a mystery still shows its stress cost: " + shyMystery);
        Expect(!Stake(Fx("m", [], [], mystery: true, stress: new Dictionary<string, int> { ["shy"] = 20 })).Contains("😣"),
            "stakes: no stress cost without the trait that pays it");
        // « de » contracts with the article of a byname.
        Expect(PlynlingEventStakes.De("le Fou") == "du Fou" && PlynlingEventStakes.De("les Jumeaux") == "des Jumeaux"
               && PlynlingEventStakes.De("la Mère Lichen") == "de la Mère Lichen" && PlynlingEventStakes.De("l'Intendant") == "de l'Intendant"
               && PlynlingEventStakes.De("Gilles") == "de Gilles", "stakes: « de » contracts with the article");
        var standingNeed = Stake(Fx("s", [], gate: new StoryGate([C(ConditionKind.Standing, "fou", op: CompareOp.AtLeast, n: 3)], null, false)));
        Expect(standingNeed.Contains("l'estime du Fou") && !standingNeed.Contains("de le"), "stakes: a standing need says « du Fou »: " + standingNeed);
        var lowStanding = Stake(Fx("s", [], gate: new StoryGate([C(ConditionKind.Standing, "fou", op: CompareOp.Below, n: -3)], null, false)));
        Expect(lowStanding.Contains("mal vu du Fou"), "stakes: a low standing need says « mal vu du Fou »: " + lowStanding);
        CardChecks();
        var mem = Stake(Fx("c", [new Remember("courge.ate_well")]));
        var courge = StoryCatalog.Cast["courge"].Byname;
        Expect(mem.Contains($"❤️ {courge} s'en souviendra"), "stakes: a positive memory: " + mem);
        var need = new StoryGate([C(ConditionKind.Flag, "f")], "{A} ne sait pas", false);
        var locked = Stake(Fx("l", [new GiveCailloux(15)], gate: need));
        Expect(locked.StartsWith("🔒 ") && !locked.Contains("🪙") && !locked.Contains("{A}"), "stakes: a locked option: " + locked);
        Expect(Stake(Fx("l", [new GiveCailloux(15)], gate: need), Ctx(K(flags: ["f"]))).Contains("🪙 15"), "stakes: unlocked, it shows its gains");
        var words = Stake(Fx("l", [], gate: new StoryGate([C(ConditionKind.Trait, "rowdy")], null, false)));
        Expect(words.StartsWith("🔒 ") && words.Length > 3, "stakes: locked without a lock text says its need: " + words);
        var keep = PlynlingEvents.ByKey("arrival.the_pebble")!;
        Expect(Stake(keep.Options.Single(o => o.Key == "keep"), def: keep).Contains("🎁 un souvenir à garder"), "stakes: the keepsake ending");
        Expect(Stake(keep.Options.Single(o => o.Key == "nod_up"), def: keep).Contains("🏷️ un titre"), "stakes: the epithet ending");
        var stat = Stake(Fx("g", [new GrowStat(PlynlingStat.Courage), new ApplyModifier("inspired")]));
        Expect(stat.Contains(PlynlingStats.Emoji(PlynlingStat.Courage)) && stat.Contains(" · "), "stakes: a lesson and a modifier: " + stat);
        Expect(Stake(Fx("x", [new SetFlag("f")])) == "", "stakes: nothing to say, nothing said");

        var village = PlynlingEvents.ByKey("arrival.the_village")!;
        Expect(PlynlingEventStory.SceneText(village, Ctx(K(flags: ["came_to_her"]))).Contains("tient un coin de son châle"),
            "scene: a flag's line shows");
        Expect(!PlynlingEventStory.SceneText(village, Ctx()).Contains("tient un coin de son châle"), "scene: without the flag, no line");
        Expect(!PlynlingEventStory.SceneText(village, null).Contains("tient un coin de son châle")
               && PlynlingEventStory.SceneText(village, null).Contains("Sur le Pont-Vieux"), "scene: no context, unconditional lines only");
        var plain = PlynlingEvents.ByKey("on_welcome")!;
        Expect(PlynlingEventStory.SceneText(plain, Ctx()) == plain.Scene, "scene: a C# def keeps its text");
        var nod = keep.Options.Single(o => o.Key == "nod_up");
        var told = PlynlingEventStory.OutcomeProse(keep, nod, true, Ctx());
        Expect(told.Contains("fait le signe du menton") && told.Contains("Bien veillé{a:|e}") && told.Contains("Tu es bien"),
            "outcome: the option's prose, then the ending's title and prose: " + told);
        Expect(PlynlingEventStory.OutcomeProse(plain, plain.Options[0], true, Ctx()) == plain.Options[0].Outcome, "outcome: a C# def keeps its text");
    }

    private static List<Discord.ButtonComponent> ChoiceButtons(EventDef def, EventContext ctx)
    {
        var inst = new PlynlingEventInstance { Id = 1, PlynlingId = 1, EventKey = def.Key, CreatedAt = Now, AvailableAt = Now, ExpiresAt = Now.AddDays(1) };
        var self = new EventCast("Brindille", PlynlingGender.Female, default, PlynlingStage.Baby, 1);
        var card = ProjectSYNCS.Commands.PlynlingEventCards.BuildChoice(inst, def, ctx, self, null, 1);
        return card.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components).OfType<Discord.ButtonComponent>().ToList();
    }

    private static string Outcome(EventDef def, EventOption o, string? gained, bool alone = false, EventContext? ctx = null)
    {
        var inst = new PlynlingEventInstance { Id = 1, PlynlingId = 1, EventKey = def.Key, OptionKey = o.Key, GainedTraitKey = gained, DecidedAlone = alone };
        return PlynlingEventStory.OutcomeLines(inst, def, o, new EventCast("Brindille", PlynlingGender.Female, default, PlynlingStage.Baby, 1), null,
            ReplyState.Waiting, ctx);
    }

    // 50l2. What the cards and the story say: buttons, heading, outcome lines, the record of marks and memories.
    private static void CardChecks()
    {
        // The choice card: a locked option's button is disabled, a secret one is absent, the dice never show,
        // and the trait icon appears only for a trait need.
        var rowdy = PlynlingTraits.ByKey("rowdy")!;
        var roll = new EventChallenge(PlynlingStat.Courage, 6);
        var def = StakesDef(
            Fx("plain", [new GiveCailloux(1)], roll: roll),
            Fx("needs_trait", [], gate: new StoryGate([C(ConditionKind.Trait, "rowdy")], null, false)),
            Fx("needs_flag", [], gate: new StoryGate([C(ConditionKind.Flag, "f")], null, false)),
            Fx("secret", [], gate: new StoryGate([C(ConditionKind.Flag, "f")], null, true)));
        var without = ChoiceButtons(def, Ctx());
        Expect(without.Select(b => b.CustomId).Count(i => i is not null) == 3, "card: the secret option is absent while its need is unmet: " + without.Count);
        Expect(without.Any(b => b.IsDisabled) && !without.Single(b => b.Label == "plain").IsDisabled, "card: a plain option can be picked");
        Expect(without.Single(b => b.Label == "needs_trait").IsDisabled && without.Single(b => b.Label == "needs_flag").IsDisabled, "card: a locked option's button is disabled");
        Expect(without.All(b => b.Emote?.Name != "🎲" && !b.Label.Contains("🎲")), "card: no button carries the dice");
        Expect(without.Single(b => b.Label == "needs_trait").Emote?.Name == rowdy.Emoji, "card: the trait it needs is the button's icon");
        Expect(without.Single(b => b.Label == "plain").Emote is null && without.Single(b => b.Label == "needs_flag").Emote is null,
            "card: no icon unless the need is a trait");
        var with = ChoiceButtons(def, Ctx(K(flags: ["f"]), traits: ["rowdy"]));
        Expect(with.Count == 4 && with.All(b => !b.IsDisabled), "card: met needs show the secret option and enable every button: " + with.Count);

        // The heading: the title after the name, its article lower-cased; alone it keeps its capital.
        Expect(PlynlingCardUi.LowerArticle("La Nez-en-l'air") == "la Nez-en-l'air" && PlynlingCardUi.LowerArticle("Le Fou") == "le Fou"
               && PlynlingCardUi.LowerArticle("Les Bras-cassés") == "les Bras-cassés" && PlynlingCardUi.LowerArticle("L'Ours") == "l'Ours"
               && PlynlingCardUi.LowerArticle("de la Lisière") == "de la Lisière" && PlynlingCardUi.LowerArticle("Lenny") == "Lenny",
            "title: a leading article loses its capital, nothing else changes");
        var brindille = new Plynling { Name = "Brindille", Gender = PlynlingGender.Female };
        var sky = PlynlingEvents.Expand(StoryCatalog.Epithets["sky_gazer"].Title, "", PlynlingGender.Female).Replace("**", "");
        var heading = PlynlingCardUi.Heading(brindille, Now, null, sky);
        Expect(sky.StartsWith("La ") && heading.StartsWith("## Brindille, la Nez-en-l'air\n"), "heading: the title follows the name in lower case: " + heading.Split('\n')[0]);
        Expect(PlynlingCardUi.Heading(brindille, Now).StartsWith("## Brindille\n"), "heading: no title, no comma");

        // The outcome: a title (article lower-cased), a keepsake, a swap that happened, and one that did not.
        var keep = PlynlingEvents.ByKey("arrival.the_pebble")!;
        var nod = PlynlingEventStory.OutcomeLines(new PlynlingEventInstance { Id = 1, EventKey = keep.Key, OptionKey = "nod_up" }, keep,
            keep.Options.Single(o => o.Key == "nod_up"), new EventCast("Brindille", PlynlingGender.Female, default, PlynlingStage.Baby, 1), null, ReplyState.Waiting, null);
        Expect(nod.Contains("🏷️ **Brindille** est désormais *la Nez-en-l'air*"), "outcome: a title, its article in lower case: " + nod);
        var kept = PlynlingEventStory.OutcomeLines(new PlynlingEventInstance { Id = 1, EventKey = keep.Key, OptionKey = "keep" }, keep,
            keep.Options.Single(o => o.Key == "keep"), new EventCast("Brindille", PlynlingGender.Female, default, PlynlingStage.Baby, 1), null, ReplyState.Waiting, null);
        Expect(kept.Contains("🎁 **Le galet de la Mère Lichen** — gardé"), "outcome: a keepsake: " + kept);
        var swapDef = StakesDef(Fx("s", [new SwapTrait("rowdy", "pensive")]));
        var swapped = Outcome(swapDef, swapDef.Options[0], "pensive");
        Expect(swapped.Contains("☁️ Turbulente → **Rêveuse**") && !swapped.Contains("Nouveau trait"), "outcome: a swap that happened is told once: " + swapped);
        var silent = Outcome(swapDef, swapDef.Options[0], null);
        Expect(!silent.Contains("→") && !silent.Contains("☁️"), "outcome: no swap line when the trait did not change: " + silent);
        Expect(!Outcome(swapDef, swapDef.Options[0], "rowdy").Contains("→"), "outcome: no swap line when the gained trait is not the swap's target");

        // The record: marks oldest first, memories prefixed with who remembers.
        var older = Now.AddDays(-2);
        var marks = new[]
        {
            new PlynlingMark { Id = 2, Kind = MarkKind.Epithet, Key = "sky_gazer", AcquiredAt = Now },
            new PlynlingMark { Id = 1, Kind = MarkKind.Epithet, Key = "lisiere_child", AcquiredAt = older },
        };
        var memories = new[]
        {
            new PlynlingMemory { Id = 2, MemoryKey = "fou.looked_up_with_him", AcquiredAt = Now },
            new PlynlingMemory { Id = 1, MemoryKey = "tilleul.met_bees", AcquiredAt = older },
        };
        var record = PlynlingPersonality.StoryRecordOf(brindille, marks, memories);
        Expect(record.Titles.Count == 2 && record.Titles[0].StartsWith("de la") && record.Titles[1].EndsWith("Nez-en-l'air"), "record: titles oldest first: " + string.Join(" / ", record.Titles));
        var tilleul = StoryCatalog.Cast["tilleul"].Byname; var fou = StoryCatalog.Cast["fou"].Byname;
        Expect(record.Memories.Count == 2 && record.Memories[0].StartsWith(tilleul + " : ") && record.Memories[1].StartsWith(fou + " : "),
            "record: memories oldest first, each by who remembers: " + string.Join(" / ", record.Memories));
        Expect(record.Lines().Any(l => l.StartsWith("💭 " + tilleul + " : ")), "record: the memory line names the holder");

        // A first line longer than the field still shows (cut), not only a count.
        var longMemory = new StoryRecord([], [], [new string('x', 1500), "b"]);
        var embed = PlynlingPersonality.DetailEmbed(brindille, [], longMemory);
        var field = embed.Fields.Single(f => f.Name == "Histoire").Value;
        Expect(field.Length <= 1024 && field.Contains("xxxx") && field.Contains("… et 1 de plus"), "field: an over-long first line is cut, not dropped: " + field.Length);
    }

    // 50m. The choice card at its fullest: every event (C# and story), long names, locked options shown
    // and every trait held. Components V2: 40 components in the tree, 4000 characters of text, 80-character
    // button labels, no custom-id twice. The story's pages and « Personnalité » stay under their caps too.
    private static void ChoiceCaps()
    {
        var name = new string('W', 40);
        var allTraits = PlynlingTraits.All.Select(t => t.Key).ToArray();
        var worstCount = 0; var worstText = 0;
        // Every flag any story reads (in a need, a line's condition or a header), switched on at once.
        var flags = PlynlingEvents.All.Where(d => d.Story is not null).SelectMany(d => d.Story!.Arc.Header.Requires
                .Concat(d.Story.Arc.Header.WeightBonuses.SelectMany(b => b.When))
                .Concat(d.Story.Arc.Scenes.SelectMany(s => s.Options.SelectMany(o => o.Needs)
                    .Concat(s.Prose.SelectMany(p => p.Lines).SelectMany(l => l.When))))
                .Concat(d.Story.Arc.Endings.SelectMany(e => e.Prose.SelectMany(p => p.Lines).SelectMany(l => l.When))))
            .Where(c => c.Kind == ConditionKind.Flag).Select(c => c.Key).Distinct().ToArray();
        Expect(flags.Length > 0, "choice card: the arcs' flags were collected");
        foreach (var real in PlynlingEvents.All)
        {
            // A story scene measured with every prose line in, whatever its condition: the true upper bound.
            var def = real.Story is { } link
                ? real with { Story = link with { Scene = link.Scene with { Prose = link.Scene.Prose
                    .Select(p => new StoryParagraph(p.Lines.Select(l => l with { When = [] }).ToList())).ToList() } } }
                : real;
            var inst = new PlynlingEventInstance { Id = int.MaxValue, PlynlingId = int.MaxValue, EventKey = def.Key, CreatedAt = Now, AvailableAt = Now, ExpiresAt = Now.AddDays(1) };
            var self = new EventCast(name, PlynlingGender.Female, default, PlynlingStage.Baby, ulong.MaxValue);
            var other = def.Target == TargetKind.None ? null : new EventCast(name, PlynlingGender.Male, default, PlynlingStage.Baby, ulong.MaxValue);
            foreach (var ctx in new[] { Ctx(), Ctx(traits: allTraits), Ctx(K(flags: flags), traits: allTraits) })
            {
                var card = ProjectSYNCS.Commands.PlynlingEventCards.BuildChoice(inst, def, ctx, self, other, ulong.MaxValue);
                var parts = card.Components.Select(EconomySinkChecks.Measure).ToList();
                var (text, count, ids) = (parts.Sum(p => p.Text), parts.Sum(p => p.Count), parts.SelectMany(p => p.Ids).ToList());
                var labels = card.Components.OfType<Discord.ActionRowComponent>().SelectMany(r => r.Components).OfType<Discord.ButtonComponent>().ToList();
                worstCount = Math.Max(worstCount, count); worstText = Math.Max(worstText, text);
                Expect(text <= 4000 && count <= 40 && ids.Count == ids.Distinct().Count() && labels.All(b => b.Label.Length <= 80) && labels.Count <= 5,
                    $"choice card {def.Key}: {text} chars, {count} components, {labels.Count} buttons, {ids.Count - ids.Distinct().Count()} duplicate id(s)");
            }
        }
        Console.WriteLine($"event choice card at worst: {worstText} chars, {worstCount} components");
        // The public line of a title earned: the name made safe, the article lowered, one sentence.
        var titled = PlynlingEventStory.TitleLine(new EventCast("Brindille", PlynlingGender.Female, default, PlynlingStage.Baby, 1), "sky_gazer");
        Expect(titled == "🏷️ **Brindille** est désormais *le Nez-en-l'air*." || titled == "🏷️ **Brindille** est désormais *la Nez-en-l'air*.",
            "title line: " + titled);
        Expect(PlynlingEventStory.TitleLine(new EventCast("x", PlynlingGender.Male, default, PlynlingStage.Baby, 1), "no_such_title") is null, "title line: an unknown key says nothing");
        Expect(PlynlingEventStory.TitleLine(new EventCast(PlynlingCardUi.SafeName("*a_b*"), PlynlingGender.Male, default, PlynlingStage.Baby, 1), "sky_gazer")!.Contains("\\*a\\_b\\*"),
            "title line: the name is the sanitized one");
        // Titles and keepsakes of every story declaration, worded, fit a field with the cut.
        var record = PlynlingPersonality.StoryRecordOf(new Plynling { Name = name },
            StoryCatalog.Epithets.Keys.Select(k => new PlynlingMark { Kind = MarkKind.Epithet, Key = k })
                .Concat(StoryCatalog.Keepsakes.Keys.Select(k => new PlynlingMark { Kind = MarkKind.Keepsake, Key = k })),
            StoryCatalog.Memories.Keys.Select(k => new PlynlingMemory { MemoryKey = k }));
        // Everything at its largest: every trait, every modifier, maximum stress.
        var fullest = new Plynling { Name = name, Stress = PlynlingStress.Max };
        PlynlingModifiers.Write(fullest, PlynlingModifiers.All.Select(m => (m.Key, Now.AddDays(30))));
        // The most traits a Plynling can hold: one from childhood, three of personality, two coping — the longest of each.
        IEnumerable<TraitInfo> Longest(TraitKind kind, int n) => PlynlingTraits.All.Where(t => t.Kind == kind).OrderByDescending(t => t.Description.Length).Take(n);
        var held = Longest(TraitKind.Childhood, 1).Concat(Longest(TraitKind.Personality, 3)).Concat(Longest(TraitKind.Coping, PlynlingTraits.MaxCoping)).ToList();
        var embed = PlynlingPersonality.DetailEmbed(fullest, held, record);
        Expect(PlynlingModifiers.Active(fullest).Count == PlynlingModifiers.All.Count, "personality embed: every modifier is held");
        Expect(new[] { "Statistiques", "État", "Histoire" }.All(n => embed.Fields.Any(f => f.Name == n)), "personality embed: every field is present");
        Expect(embed.Fields.All(f => f.Value.Length <= 1024) && embed.Length <= 6000, "personality embed with every trait, modifier, title, keepsake and memory: " + embed.Length);
        Console.WriteLine($"personality embed at worst: {embed.Length} chars, fields " + string.Join("/", embed.Fields.Select(f => f.Value.Length)));
    }

    private static readonly string Root = StoryChecks.RepoRoot();
    private static readonly string Shipped = Path.Combine(Root, "ProjectSYNCS", "Stories");

    // 50d. Arrival, compiled into the catalog: types, gates, effects, the ending's mark.
    private static void Arrival()
    {
        var first = PlynlingEvents.ByKey("arrival.first_steps");
        Expect(first is { Type: EventType.Triggered, Trigger: OnAction.Adopted, Target: TargetKind.None, Story: not null }
               && first.Stages.SequenceEqual([PlynlingStage.Baby]), "arrival.first_steps is triggered by adoption, bébé");
        Expect(first is { Weight: StoryCompiler.OrdinaryWeight }, "a triggered arc with no weight: weighs the ordinary 100");
        Expect(PlynlingEvents.ByKey("arrival.the_village") is { Type: EventType.FollowUp, Trigger: null, Condition: null }, "arrival.the_village is a follow-up");
        Expect(PlynlingEvents.ByKey("arrival.the_pebble") is { Type: EventType.FollowUp }, "arrival.the_pebble is a follow-up");
        if (first is null) return;
        Expect(first.Title == "Les premiers pas" && first.Scene.StartsWith("Il a plu toute la nuit.") && !first.Scene.Contains("[if")
               && !first.Scene.Contains("Le caillou était le moins bon"), "scene text is the unconditional prose");
        var dig = first.Options.SingleOrDefault(o => o.Key == "dig");
        Expect(dig?.Gate is StoryGate { Secret: false } g && g.Needs.Any(c => c is { Kind: ConditionKind.Trait, Key: "rowdy", Negated: false }),
            "dig: a StoryGate needing trait rowdy, not secret");
        Expect(dig?.Source?.Key == "dig" && dig.Ai.GetValueOrDefault(AiAxis.Energy) == 2, "dig: its source option and lean");
        var toHer = first.Options.SingleOrDefault(o => o.Key == "to_her");
        Expect(toHer is { Gate: null, Challenge: null, FailOutcome: null }
               && toHer.OnSuccess.Contains(new SetFlag("came_to_her")) && toHer.OnSuccess.Contains(new Remember("lichen.came_first"))
               && toHer.OnSuccess.Contains(new FollowUp("arrival.the_village", 20, 28)),
            "to_her: flag, memory and a next_day follow-up: " + string.Join(", ", toHer?.OnSuccess ?? []));
        Expect(first.Options.Single(o => o.Key == "wood").OnSuccess.Contains(new GrowStat(PlynlingStat.Courage)), "wood: [grow courage]");
        var keep = PlynlingEvents.ByKey("arrival.the_pebble")?.Options.SingleOrDefault(o => o.Key == "keep");
        var fx = keep?.OnSuccess.ToList() ?? [];
        var at = fx.IndexOf(new EndStory("arrival", "kept_pebble"));
        Expect(at >= 0 && fx.ElementAtOrDefault(at + 1) == new GiveMark(MarkKind.Keepsake, "lichen_pebble", false),
            "keep: EndStory then its keepsake: " + string.Join(", ", fx));
        Expect(PlynlingEvents.ByKey("arrival.the_pebble")!.Options.Single(o => o.Key == "nod_up").OnSuccess
            .SequenceEqual(new EventEffect[] { new EndStory("arrival", "watched"), new GiveMark(MarkKind.Epithet, "sky_gazer") }), "nod_up: ending and epithet");
        // Catalog-wide: no key twice (an alias is not listed), every story def can be decided alone.
        Expect(PlynlingEvents.All.Select(d => d.Key).Distinct().Count() == PlynlingEvents.All.Count, "no key twice in PlynlingEvents.All");
        foreach (var d in PlynlingEvents.All.Where(d => d.Story is not null))
            Expect(d.Options.Any(o => o.Gate is null && o.StressCosts.Count == 0 && !o.OwnerOnly), $"{d.Key}: a free option");
        Expect(StoryCatalog.Cast.ContainsKey("lichen") && StoryCatalog.Memories.ContainsKey("lichen.came_first")
               && StoryCatalog.Keepsakes.ContainsKey("lichen_pebble") && StoryCatalog.Epithets.ContainsKey("sky_gazer"), "the catalog's declarations");
        Expect(StoryCatalog.Compiled.All(d => ReferenceEquals(PlynlingEvents.ByKey(d.Key), d)), "every compiled def is in ByKey");
    }

    private static StoryLoad _fixture = null!;
    private static EventDef? Def(IReadOnlyList<EventDef> defs, string key) => defs.SingleOrDefault(d => d.Key == key);

    // 50e. The fixtures: test_short ([end], one pulse) and test_everything (every word compiled).
    private static void Fixtures()
    {
        _fixture = StoryLibrary.Load(Shipped, Path.Combine(Root, "tests", "ProjectSYNCS.Checks", "Stories"));
        if (_fixture.Errors.Count > 0) { Expect(false, "fixtures load: " + string.Join("; ", _fixture.Errors)); return; }
        var defs = StoryCompiler.Compile(_fixture);
        var shorts = defs.Where(d => d.Story?.Arc.Key == "test_short").ToList();
        Expect(shorts.Count == 1 && shorts[0] is { Key: "test_short.only", Type: EventType.Pulse, Weight: 10, Trigger: null }, "test_short: one pulse");
        Expect(shorts.SelectMany(d => d.Options).SelectMany(o => o.OnSuccess.Concat(o.OnFailure)).All(e => e is not (FollowUp or EndStory)),
            "test_short: [end] adds no follow-up and no ending");
        var toss = shorts.SingleOrDefault()?.Options.SingleOrDefault(o => o.Key == "toss");
        Expect(toss is { Challenge: { Stat: PlynlingStat.Courage, Difficulty: 6, VsTarget: false }, Outcome: "Le caillou ricoche trois fois.",
            FailOutcome: "Le caillou tombe dans l'herbe." }, "toss: a roll, success and failure prose");
        Expect(shorts.SingleOrDefault()?.Options.SingleOrDefault(o => o.Key == "keep")?.OnSuccess.SequenceEqual([new GiveCailloux(10)]) == true, "keep: [cailloux 10]");

        const string a = "test_everything";
        var start = Def(defs, a + ".start");
        Expect(start is { Type: EventType.Pulse, Target: TargetKind.Known, TargetCondition: not null, WeightBonus: not null }
               && start.Stages.SequenceEqual([PlynlingStage.Adult, PlynlingStage.Elder]), "test_everything.start: a social pulse");
        Expect(start?.TargetCondition?.Invoke(new(new Plynling(), PlynlingBond.Lovers, 0, 0, false)) == true
               && start.TargetCondition.Invoke(new(new Plynling(), PlynlingBond.Friends, 0, 0, false)) == false, "other: lovers filters the target by bond");
        Expect(start?.WeightBonus?.Invoke(Ctx(K(season: Season.Autumn), passion: PlynlingPassion.Gardening)) == 5
               && start.WeightBonus.Invoke(Ctx(K(season: Season.Spring), passion: PlynlingPassion.Gardening)) == 0, "weight bonus when its conditions hold");
        Expect(defs.All(d => d.Key != a + ".old_scene") && Def(defs, a + ".apart") is not null, "a retired scene compiles to no def of its own");
        Expect(StoryCompiler.Aliases(_fixture).GetValueOrDefault(a + ".old_scene") == a + ".apart", "retired scene: alias to its successor");
        var opts = start?.Options.ToDictionary(o => o.Key) ?? new();
        Expect(opts.GetValueOrDefault("old") is { Gate: RetiredGate, Outcome: "" }, "a retired option stays, behind a RetiredGate");
        Expect(opts.GetValueOrDefault("read") is { Mystery: true } r && r.OnSuccess.Contains(new FollowUp(a + ".answer", 6, 6))
               && r.OnSuccess.Contains(new SetFlag("read")) && r.OnSuccess.Contains(new Remember("courge.saw_them")), "read: mystery, +6h follow-up");
        Expect(opts.GetValueOrDefault("burn") is { Gate: StoryGate, StressCosts: var sc } b && sc.GetValueOrDefault("shy") == 20
               && b.OnSuccess.Contains(new ClearFlag("read")), "burn: gate, stress cost, unflag");
        Expect(opts.GetValueOrDefault("hide") is { OwnerOnly: true } h && h.OnSuccess.Contains(new ApplyModifier("inspired")), "hide: owner-only, modifier");
        var answer = Def(defs, a + ".answer")?.Options.ToDictionary(o => o.Key) ?? new();
        Expect(answer.GetValueOrDefault("yes")?.Gate is StoryGate { LockText: "{A} a lu la lettre" }
               && answer["yes"].OnSuccess.SequenceEqual(new EventEffect[] { new AffinityShift(10), new FollowUp(a + ".walk", 0, 0) }), "yes: lock text, affinity, now");
        Expect(answer.GetValueOrDefault("no")?.OnSuccess.Contains(new StressChange(10)) == true, "no: [stress 10]");
        Expect(answer.GetValueOrDefault("chance") is { Gate: null, Challenge.Difficulty: 7 } ch
               && ch.OnSuccess.Contains(new LiftNeed(Need.Happiness, 0.2)) && ch.OnSuccess.Contains(new GrowStat(PlynlingStat.Diplomacy))
               && ch.OnFailure.SequenceEqual([new FollowUp(a + ".apart", 20, 28)]), "chance: roll, lift, grow; failure leads apart");
        var walk = Def(defs, a + ".walk");
        Expect(StoryCompiler.NarrationOption == "continue", "the narration option's stored key is « continue »");
        Expect(walk?.Options is [{ Key: "continue", Label: "Continuer", Gate: null, Source: not null } n]
               && n.OnSuccess.SequenceEqual(new EventEffect[] { new RevealMemory("lichen.secret"), new FollowUp(a + ".bridge", 0, 0) }),
            "a narration scene: one free option carrying its effects");
        var apart = Def(defs, a + ".apart")?.Options.ToDictionary(o => o.Key) ?? new();
        Expect(apart.GetValueOrDefault("wait")?.OnSuccess.SequenceEqual(new EventEffect[]
            { new Forget("courge.saw_them"), new GiveCailloux(30), new EndStory(a, "alone"), new SwapTrait("brave", "craven") }) == true, "wait: ending alone swaps a trait");
        Expect(apart.GetValueOrDefault("write")?.OnSuccess.SequenceEqual(new EventEffect[]
            { new GiveItem("care.soap"), new EndStory(a, "faithful_end"), new GiveMark(MarkKind.Epithet, "faithful"), new GiveCailloux(40) }) == true,
            "write: an ending's cailloux too");
        Expect(apart.GetValueOrDefault("keep")?.OnSuccess.SequenceEqual(new EventEffect[]
            { new EndStory(a, "gone"), new GiveMark(MarkKind.Keepsake, "letter", true) }) == true, "keep: a keepsake that hurts");
        Expect(Def(defs, a + ".bridge")?.Options.Single(o => o.Key == "cross").OnSuccess.SequenceEqual(new EventEffect[]
            { new EndStory(a, "together"), new GiveMark(MarkKind.Epithet, "faithful"), new GiveMark(MarkKind.Unlock, a) }) == true, "cross: epithet and unlock");
    }

    private static StoryKnowledge KT(Dictionary<string, DateTimeOffset>? started = null, DateTimeOffset? lastLong = null,
        (MarkKind, string)[]? marks = null) =>
        StoryKnowledge.Empty(Now) with
        {
            LastStarted = started ?? new Dictionary<string, DateTimeOffset>(),
            LastLongStart = lastLong,
            Marks = new HashSet<(MarkKind, string)>(marks ?? []),
        };

    // 50f. When an arc may open: once, cooldown, locked, and the 21 days between long drawn arcs.
    private static void Openings()
    {
        var arc = _fixture?.Arcs.SingleOrDefault(x => x.Key == "test_short");
        if (arc is null) return;
        bool Opens(StoryArc a, StoryKnowledge k) =>
            StoryCompiler.Compile(new StoryLoad([a], _fixture!.Cast, [])).First().Condition?.Invoke(Ctx(k, stage: PlynlingStage.Teen)) ?? true;
        var plain = arc with { Header = arc.Header with { Once = false, Cooldown = null, Locked = false } };
        Expect(Opens(plain, KT()) && Opens(plain, KT(lastLong: Now.AddDays(-1))), "a short arc ignores the long-arc gap");
        var lng = plain with { Header = plain.Header with { Size = StorySize.Long } };
        Expect(!Opens(lng, KT(lastLong: Now.AddDays(-20))), "long draw arc: closed 20 days after a long start");
        Expect(Opens(lng, KT(lastLong: Now.AddDays(-21))), "long draw arc: open 21 days after a long start");
        Expect(Opens(lng, KT()), "long draw arc: open with no long start");
        var once = plain with { Header = plain.Header with { Once = true } };
        Expect(Opens(once, KT()) && !Opens(once, KT(new() { ["test_short"] = Now.AddDays(-400) })), "once: closed once started");
        var cd = plain with { Header = plain.Header with { Cooldown = TimeSpan.FromDays(30) } };
        Expect(!Opens(cd, KT(new() { ["test_short"] = Now.AddDays(-29) })), "cooldown 30d: closed at 29 days");
        Expect(Opens(cd, KT(new() { ["test_short"] = Now.AddDays(-30) })), "cooldown 30d: open at 30 days");
        Expect(Opens(cd, KT(new() { ["other_arc"] = Now })), "cooldown reads its own arc only");
        var locked = plain with { Header = plain.Header with { Locked = true } };
        Expect(!Opens(locked, KT()) && !Opens(locked, KT(marks: [(MarkKind.Unlock, "other_arc")])), "locked: closed without its unlock");
        Expect(Opens(locked, KT(marks: [(MarkKind.Unlock, "test_short")])), "locked: open with its unlock");
        var req = plain with { Header = plain.Header with { Requires = [C(ConditionKind.Flag, "x.y")] } };
        Expect(!Opens(req, KT()) && Opens(req, KT() with { ArcFlags = new HashSet<string> { "x.y" } }), "requires");
    }

    // 50g. A story replaces the C# moment for its on-action: Arrival, never on_welcome.
    private static void Triggered()
    {
        Expect(PlynlingEvents.ByKey("on_welcome") is { Trigger: OnAction.Adopted }, "on_welcome still in the catalog");
        for (var id = 1; id <= 40; id++)
        {
            var got = PlynlingEventEngine.PickTriggered(OnAction.Adopted, id, id * 7919 % 1_000_000, PlynlingEvents.All, Ctx());
            if (got?.Key == "arrival.first_steps") continue;
            Expect(false, $"adoption picks {got?.Key ?? "nothing"} for Plynling {id}");
            break;
        }
        var teen = PlynlingEventEngine.PickTriggered(OnAction.BecameTeen, 1, 1, PlynlingEvents.All, Ctx(stage: PlynlingStage.Teen));
        Expect(teen is null || teen.Story is null, "no story for becoming teen yet: the C# moment stays");
        var gate = new EventOption("o", "O", "", null, new StoryGate([C(ConditionKind.Flag, "f")], null, false), null, [], [],
            new Dictionary<string, int>(), new Dictionary<AiAxis, int>());
        Expect(!PlynlingEventEngine.Visible(gate, Ctx()) && PlynlingEventEngine.Visible(gate, Ctx(K(flags: ["f"]))), "Visible reads a StoryGate's needs");
        Expect(!PlynlingEventEngine.Visible(gate with { Gate = new RetiredGate() }, Ctx()), "a retired option is never visible");
    }

    private static EventOption Opt(string key, EventGate? gate, bool ownerOnly = false) =>
        new(key, key, "", null, gate, null, [], [], new Dictionary<string, int>(), new Dictionary<AiAxis, int>(), OwnerOnly: ownerOnly);

    // 50i. Engine rules: shown vs pickable, deciding alone, the long-event draw, and the compile defaults.
    private static void Rules()
    {
        var need = new[] { C(ConditionKind.Flag, "f") };
        var locked = Opt("locked", new StoryGate(need, "{A} ne sait pas", false));
        var secret = Opt("secret", new StoryGate(need, null, true));
        var no = Ctx();
        var yes = Ctx(K(flags: ["f"]));
        Expect(PlynlingEventEngine.Shown(locked, no) && !PlynlingEventEngine.Visible(locked, no), "a locked non-secret option is shown, not pickable");
        Expect(!PlynlingEventEngine.Shown(secret, no) && !PlynlingEventEngine.Visible(secret, no), "a secret unmet option is neither shown nor pickable");
        Expect(PlynlingEventEngine.Shown(secret, yes) && PlynlingEventEngine.Visible(secret, yes), "a secret met option is shown and pickable");
        Expect(PlynlingEventEngine.Shown(locked, yes) && PlynlingEventEngine.Visible(locked, yes), "a met option is shown and pickable");
        Expect(PlynlingEventEngine.Shown(Opt("free", null), no) && PlynlingEventEngine.Shown(Opt("t", new TraitGate("brave")), no) == PlynlingEventEngine.Visible(Opt("t", new TraitGate("brave")), no),
            "old gates: Shown is Visible");
        var def = new EventDef("x.y", EventType.Pulse, [PlynlingStage.Baby], "T", "S", [locked, secret, Opt("free", null)]);
        Expect(PlynlingEventEngine.AloneOptions(def, no).Select(o => o.Key).SequenceEqual(["free"]), "alone: never a locked option");
        Expect(!PlynlingEventEngine.AppliesWhenAlone(new SwapTrait("brave", "craven"), def), "alone: a trait swap does not apply");
        Expect(!PlynlingEventEngine.AppliesWhenAlone(new GiveMark(MarkKind.Keepsake, "k", true), def), "alone: a keepsake that hurts does not apply");
        Expect(PlynlingEventEngine.AppliesWhenAlone(new GiveMark(MarkKind.Keepsake, "k"), def)
               && PlynlingEventEngine.AppliesWhenAlone(new GiveMark(MarkKind.Epithet, "e"), def)
               && PlynlingEventEngine.AppliesWhenAlone(new SetFlag("f"), def) && PlynlingEventEngine.AppliesWhenAlone(new EndStory("a", "b"), def)
               && PlynlingEventEngine.AppliesWhenAlone(new Remember("m"), def), "alone: the other story effects apply");

        // The long-event draw: two hand-made defs sharing one eligible pool.
        var defs = _fixture is null ? [] : StoryCompiler.Compile(_fixture);
        var lng = Def(defs, "test_everything.start");
        var shrt = Def(defs, "test_short.only");
        if (lng is null || shrt is null) { Expect(false, "the fixtures' pulses are there"); return; }
        Expect(PlynlingEventEngine.IsLong(lng) && !PlynlingEventEngine.IsLong(shrt) && !PlynlingEventEngine.IsLong(def), "IsLong reads the arc's size");
        Expect(PlynlingEventEngine.BlocksPulse([lng]) && PlynlingEventEngine.BlocksPulse([def, lng]) && !PlynlingEventEngine.BlocksPulse([shrt, def])
               && !PlynlingEventEngine.BlocksPulse([]), "an open long event blocks the pulse");
        var allStages = Enum.GetValues<PlynlingStage>();
        EventDef Plain(EventDef d, string key) => d with { Key = key, Stages = allStages, Condition = null, Target = TargetKind.None, WeightBonus = null, Weight = 100 };
        var pool = new[] { Plain(lng, "pool.long"), Plain(shrt, "pool.short") };
        var picks = new HashSet<string>();
        var longSeen = false;
        for (var id = 1; id <= 60; id++)
        {
            var off = PlynlingEventEngine.PickPulse(id, 20260101 + id, pool, no, [], _ => true, allowLong: false);
            picks.Add(off?.Key ?? "none");
            longSeen |= PlynlingEventEngine.PickPulse(id, 20260101 + id, pool, no, [], _ => true, allowLong: true)?.Key == "pool.long";
        }
        Expect(picks.SetEquals(["pool.short"]), "allowLong false: never a long def: " + string.Join(",", picks));
        Expect(longSeen, "allowLong true: a long def can be drawn");
        Expect(PlynlingEventEngine.PickPulse(1, 20260101, [Plain(lng, "pool.long")], no, [], _ => true, allowLong: false) is null, "only long defs and not allowed: nothing");

        // Compile defaults (carry-overs): a drawn arc with no weight weighs the ordinary 100, a long arc is once by default.
        var arc = _fixture?.Arcs.SingleOrDefault(x => x.Key == "test_short");
        if (arc is not null)
        {
            var unweighted = StoryCompiler.Compile(new StoryLoad([arc with { Header = arc.Header with { Weight = 0 } }], _fixture!.Cast, [])).First();
            Expect(unweighted.Weight == StoryCompiler.OrdinaryWeight && unweighted.Type == EventType.Pulse, "a drawn arc with no weight: weighs 100");
        }
        var longStart = lng.Condition;
        var started = new Dictionary<string, DateTimeOffset> { ["test_everything"] = Now.AddDays(-400) };
        var unlock = new (MarkKind, string)[] { (MarkKind.Unlock, "test_everything") };
        Expect(longStart is not null && longStart(Ctx(KT(marks: unlock), stage: PlynlingStage.Adult)) && !longStart(Ctx(KT(started, marks: unlock), stage: PlynlingStage.Adult)),
            "a long arc with no once: opens once, then never again");
    }

    // 50h. Loading a folder: a broken file stops the load naming file and line; a retired scene is an alias.
    private static void Probes()
    {
        var saved = StoryCatalog.Folder;
        var outDir = Path.Combine(Root, "tests", "ProjectSYNCS.Checks", "out");
        var broken = Path.Combine(outDir, "probe-broken");
        var good = Path.Combine(outDir, "probe-good");
        foreach (var dir in new[] { broken, good })
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            Directory.CreateDirectory(dir);
            File.Copy(Path.Combine(Shipped, StoryLibrary.CastFile), Path.Combine(dir, StoryLibrary.CastFile));
        }
        var text = File.ReadAllText(Path.Combine(Root, "tests", "ProjectSYNCS.Checks", "Stories", "test_short.story")).Replace("\r\n", "\n");
        File.WriteAllText(Path.Combine(broken, "bad.story"), text.Replace("[cailloux 10] [end]", "[frobnicate 10] [end]")
            .Replace("    Le caillou tombe dans l'herbe.\n    [end]", "    Le caillou tombe dans l'herbe.\n    [-> nowhere]"));
        File.Copy(Path.Combine(Root, "tests", "ProjectSYNCS.Checks", "Stories", "test_everything.story"), Path.Combine(good, "test_everything.story"));
        try
        {
            StoryCatalog.Folder = broken;
            try
            {
                _ = StoryCatalog.Compiled;
                Expect(false, "a broken story folder loads");
            }
            catch (StoryLoadException e)
            {
                var lines = e.Message.Split('\n');
                Expect(lines.Any(l => l.Contains("probe-broken/bad.story:") && l.Contains("frobnicate")), "the parse error names file and line: " + e.Message);
                Expect(lines.Any(l => l.Contains("probe-broken/bad.story:") && l.Contains("nowhere")), "the validation error names file and line: " + e.Message);
                Expect(e.Errors.Count >= 2, "every error is listed");
            }
            catch (Exception e)
            {
                Expect(false, "a broken story folder throws a StoryLoadException, not " + e.GetType().Name + ": " + e.Message);
            }
            StoryCatalog.Folder = good;
            Expect(StoryCatalog.Aliases.GetValueOrDefault("test_everything.old_scene") is { Key: "test_everything.apart" } alias
                   && ReferenceEquals(alias, StoryCatalog.Compiled.Single(d => d.Key == "test_everything.apart")), "the catalog maps a retired scene to its successor's def");
            Expect(StoryCatalog.Compiled.Count(d => d.Key == "test_everything.apart") == 1 && StoryCatalog.Compiled.All(d => d.Key != "test_everything.old_scene"),
                "an alias is not listed twice");
        }
        catch (Exception e)
        {
            Expect(false, "the probe folders load: " + e.GetType().Name + ": " + e.Message);
        }
        finally
        {
            // Restoring makes StoryCatalog build a fresh load of the shipped folder: its defs are new objects, no longer
            // the ones PlynlingEvents.All holds. Nothing after this section compares them by reference.
            StoryCatalog.Folder = saved;
            foreach (var dir in new[] { broken, good }) Directory.Delete(dir, true);
        }
    }

    // 50j. The lookup and the ado pool take their input, so they are checked on hand-built defs.
    private static void Lookups()
    {
        EventDef D(string key, params EventOption[] options) => new(key, EventType.Pulse, [PlynlingStage.Teen], "T", "S", options);
        var live = D("a.live", Opt("o", null));
        var map = PlynlingEvents.KeyMap([live, D("b.other", Opt("o", null))], new Dictionary<string, EventDef> { ["a.retired"] = live });
        Expect(ReferenceEquals(map.GetValueOrDefault("a.retired"), live) && ReferenceEquals(map.GetValueOrDefault("a.live"), live)
               && !map.ContainsKey("a.gone"), "KeyMap: a retired scene's key resolves to its successor's def");

        var mixed = D("c.mixed", Opt("old", new RetiredGate()), Opt("new", null));
        var onlyRetired = D("c.dead", Opt("old", new RetiredGate()));
        var adult = D("c.adult", Opt("o", null)) with { Stages = [PlynlingStage.Adult] };
        var pool = PlynlingEventEngine.AdoPool([mixed, onlyRetired, adult]);
        Expect(pool.Length == 1 && pool[0].Key == "c.mixed" && pool[0].Options.Select(o => o.Key).SequenceEqual(["new"]),
            "AdoPool: retired options cut, a def left with none dropped, teen only");

        // The long-once default is only covered while the fixture leaves `once:` out of its header.
        var header = File.ReadAllLines(Path.Combine(Root, "tests", "ProjectSYNCS.Checks", "Stories", "test_everything.story"))
            .TakeWhile(l => !l.StartsWith("## ")).ToList();
        Expect(header.Any(l => l.StartsWith("size: long")) && !header.Any(l => l.StartsWith("once:")), "test_everything writes size: long and no once:");
    }

    // 50k. A run's flags, and what finished and started runs leave (StoryRuns): the service's pure half.
    private static void Runs()
    {
        Expect(StoryRuns.ParseFlags(null).Count == 0 && StoryRuns.ParseFlags("").Count == 0, "flags: none stored reads none");
        Expect(StoryRuns.ParseFlags(";a;;b;").SetEquals(["a", "b"]), "flags: parsed, empty parts skipped");
        Expect(StoryRuns.WriteFlags([]) is null, "flags: an empty set writes null, never \"\"");
        Expect(StoryRuns.WriteFlags(["b", "a", "b"]) == "a;b", "flags: written sorted, once each");
        Expect(StoryRuns.NextFlags("a;b", [new SetFlag("c"), new ClearFlag("a"), new GrowStat(PlynlingStat.Courage)]).SetEquals(["b", "c"]),
            "next flags: the run's, plus [flag], minus [unflag]");
        Expect(!StoryRuns.NextFlags(null, [new SetFlag("x"), new ClearFlag("x")]).Contains("x")
               && StoryRuns.NextFlags(null, [new ClearFlag("x"), new SetFlag("x")]).Contains("x"), "next flags: applied in order");
        Expect(StoryRuns.NextFlags("a", []).SetEquals(["a"]), "next flags: carried unchanged");
        Expect(StoryRuns.ArcOf("arrival.the_pebble") == "arrival" && StoryRuns.ArcOf("on_welcome") is null, "ArcOf: the key's part before the dot");

        var (ended, arcFlags) = StoryRuns.Finished([("arrival.the_pebble", "kept_pebble", "came_to_her;dug"),
            ("arrival.the_village", null, "x"), ("on_welcome", "kept", "y")]);
        Expect(ended.SetEquals(["arrival.kept_pebble"]) && arcFlags.SetEquals(["arrival.came_to_her", "arrival.dug"]),
            "finished runs: arc.ending and arc.flag, only from a run that reached an ending: " + string.Join(",", ended.Concat(arcFlags)));

        var shipped = StoryRuns.Openers(PlynlingEvents.All);
        Expect(shipped.ContainsKey("arrival.first_steps") && !shipped.ContainsKey("arrival.the_village") && shipped.Keys.All(k => k.Contains('.')),
            "openers: each arc's first scene, no C# key");
        if (_fixture is null || _fixture.Errors.Count > 0) return;
        var openers = StoryRuns.Openers(StoryCompiler.Compile(_fixture));
        var t0 = Now.AddDays(-40);
        var (last, lastLong) = StoryRuns.Starts([("test_short.only", t0), ("test_short.only", t0.AddDays(5)), ("test_everything.start", t0.AddDays(2)),
            ("test_everything.answer", t0.AddDays(9)), ("on_welcome", t0.AddDays(9))], openers);
        Expect(last.Count == 2 && last.GetValueOrDefault("test_short") == t0.AddDays(5) && last.GetValueOrDefault("test_everything") == t0.AddDays(2),
            "LastStarted: each arc's latest first scene, later scenes ignored");
        Expect(lastLong == t0.AddDays(2), "LastLongStart: the latest long arc's start, short ones ignored");
        Expect(StoryRuns.Starts([], openers) is ({ Count: 0 }, null), "no run started: nothing");
    }

    private static StoryCondition C(ConditionKind kind, string key, bool negated = false, CompareOp op = CompareOp.None, int n = 0) =>
        new(kind, key, negated, op, n, P);

    private static EventContext Ctx(StoryKnowledge? k = null, PlynlingGender gender = PlynlingGender.Female, PlynlingStage stage = PlynlingStage.Baby,
        TargetInfo? other = null, IReadOnlyList<string>? otherTraits = null, string[]? traits = null, PlynlingPassion passion = PlynlingPassion.Cooking,
        string? taught = null)
    {
        var self = new Plynling { Gender = gender, Passion = passion, TaughtPassion = taught };
        var held = (traits ?? Array.Empty<string>()).Select(PlynlingTraits.ByKey).OfType<TraitInfo>().ToList();
        return new EventContext(self, held, PlynlingStats.Compute(self, held), stage, other, null, otherTraits, k);
    }

    private static StoryKnowledge K(string[]? flags = null, string[]? memories = null, (MarkKind, string)[]? marks = null,
        string[]? ended = null, string[]? arcFlags = null, Dictionary<string, int>? standing = null, Season season = Season.Spring) =>
        new(new HashSet<string>(flags ?? []), new HashSet<string>(memories ?? []), new HashSet<(MarkKind, string)>(marks ?? []),
            new HashSet<string>(ended ?? []), new HashSet<string>(arcFlags ?? []), standing ?? new Dictionary<string, int>(),
            new Dictionary<string, DateTimeOffset>(), null, season, Now);

    private static void Both(StoryCondition c, EventContext yes, EventContext no, string what)
    {
        Expect(StoryConditions.Holds(c, yes), what + " holds");
        Expect(!StoryConditions.Holds(c, no), what + " fails");
        var neg = c with { Negated = true };
        Expect(!StoryConditions.Holds(neg, yes) && StoryConditions.Holds(neg, no), what + " negated");
    }

    private static TargetInfo Other(PlynlingBond bond) => new(new Plynling(), bond, 0, 0, false);

    private static void Conditions()
    {
        var trait = PlynlingTraits.All[0].Key;
        Both(C(ConditionKind.Trait, trait), Ctx(traits: [trait]), Ctx(), "trait");
        Both(C(ConditionKind.OtherTrait, trait), Ctx(otherTraits: [trait], other: Other(PlynlingBond.Friends)), Ctx(other: Other(PlynlingBond.Friends)), "other trait");
        Expect(!StoryConditions.Holds(C(ConditionKind.OtherTrait, trait), Ctx(otherTraits: [trait])), "other trait false without an other");
        Both(C(ConditionKind.Stage, "baby"), Ctx(stage: PlynlingStage.Baby), Ctx(stage: PlynlingStage.Adult), "stage");
        Both(C(ConditionKind.Flag, "met"), Ctx(K(flags: ["met"])), Ctx(K()), "flag");
        Expect(!StoryConditions.Holds(C(ConditionKind.Flag, "met"), Ctx(K(arcFlags: ["a.met"]))), "a bare flag does not read arc flags");
        Both(C(ConditionKind.Flag, "a.met"), Ctx(K(arcFlags: ["a.met"])), Ctx(K(flags: ["a.met"])), "arc flag");
        Both(C(ConditionKind.Memory, "m1"), Ctx(K(memories: ["m1"])), Ctx(K()), "memory");
        Both(C(ConditionKind.Keepsake, "k1"), Ctx(K(marks: [(MarkKind.Keepsake, "k1")])), Ctx(K(marks: [(MarkKind.Epithet, "k1")])), "keepsake");
        Both(C(ConditionKind.Epithet, "e1"), Ctx(K(marks: [(MarkKind.Epithet, "e1")])), Ctx(K(marks: [(MarkKind.Keepsake, "e1")])), "epithet");
        var ctx = Ctx();
        var courage = ctx.Stat(PlynlingStat.Courage);
        Expect(StoryConditions.Holds(C(ConditionKind.Stat, "courage", op: CompareOp.AtLeast, n: courage), ctx), "stat >= own total");
        Expect(!StoryConditions.Holds(C(ConditionKind.Stat, "courage", op: CompareOp.AtLeast, n: courage + 1), ctx), "stat >= above total fails");
        Expect(StoryConditions.Holds(C(ConditionKind.Stat, "courage", op: CompareOp.Below, n: courage + 1), ctx), "stat < above total");
        Expect(!StoryConditions.Holds(C(ConditionKind.Stat, "courage", op: CompareOp.Below, n: courage), ctx), "stat < own total fails");
        Expect(StoryConditions.Holds(C(ConditionKind.Stat, "courage", negated: true, op: CompareOp.Below, n: courage), ctx), "stat negated");
        var st = Ctx(K(standing: new() { ["lichen"] = 3 }));
        Expect(StoryConditions.Holds(C(ConditionKind.Standing, "lichen", op: CompareOp.AtLeast, n: 3), st), "standing >=");
        Expect(!StoryConditions.Holds(C(ConditionKind.Standing, "lichen", op: CompareOp.Below, n: 3), st), "standing <");
        Expect(StoryConditions.Holds(C(ConditionKind.Standing, "moss", op: CompareOp.Below, n: 1), st), "missing standing is 0");
        Expect(StoryConditions.Holds(C(ConditionKind.Standing, "moss", negated: true, op: CompareOp.AtLeast, n: 3), st), "standing negated");
        Both(C(ConditionKind.Relation, "friends"), Ctx(other: Other(PlynlingBond.Friends)), Ctx(other: Other(PlynlingBond.Rivals)), "relation");
        Expect(!StoryConditions.Holds(C(ConditionKind.Relation, "friends"), Ctx()), "relation false without an other");
        Both(C(ConditionKind.Season, "spring"), Ctx(K(season: Season.Spring)), Ctx(K(season: Season.Winter)), "season");
        Both(C(ConditionKind.Passion, "cooking"), Ctx(passion: PlynlingPassion.Cooking), Ctx(passion: PlynlingPassion.Music), "passion");
        var taught = Ctx(passion: PlynlingPassion.Music, taught: "cuisine");
        Expect(StoryConditions.Holds(C(ConditionKind.Passion, "cooking"), taught), "taught catalog passion overrides the innate one");
        Expect(!StoryConditions.Holds(C(ConditionKind.Passion, "music"), taught), "innate passion hidden by a taught one");
        Both(C(ConditionKind.Ended, "a.good"), Ctx(K(ended: ["a.good"])), Ctx(K(ended: ["a.bad"])), "ended");
        Expect(!StoryConditions.Holds(C(ConditionKind.Chain, "x"), Ctx()), "chain is false");
        Expect(StoryConditions.Holds(C(ConditionKind.Chain, "x", negated: true), Ctx()), "negated chain is true");
        Both(C(ConditionKind.Gender, "f"), Ctx(gender: PlynlingGender.Female), Ctx(gender: PlynlingGender.Male), "gender f");
        Both(C(ConditionKind.Gender, "m"), Ctx(gender: PlynlingGender.Male), Ctx(gender: PlynlingGender.Female), "gender m");
        Expect(StoryConditions.All([C(ConditionKind.Flag, "a"), C(ConditionKind.Flag, "b")], Ctx(K(flags: ["a", "b"]))), "All holds");
        Expect(!StoryConditions.All([C(ConditionKind.Flag, "a"), C(ConditionKind.Flag, "b")], Ctx(K(flags: ["a"]))), "All fails on one");
        Expect(StoryConditions.All([], Ctx()), "All of nothing holds");
        Expect(Ctx().Knowledge.Flags.Count == 0, "no knowledge reads as empty");
    }

    // The cast is passed in: a villager with no declarations still takes the public +-1.
    private static void Standing()
    {
        var decls = new Dictionary<string, MemoryDecl>
        {
            ["secret"] = new("secret", "lichen", false, 3, "w", P),
            ["open"] = new("open", "lichen", true, 3, "w", P),
            ["bad"] = new("bad", "moss", true, -2, "w", P),
            ["other"] = new("other", "fern", false, 1, "w", P),
        };
        string[] cast = ["lichen", "moss", "fern", "pebble"]; // pebble declares no memory of their own
        var priv = StoryStanding.Compute(["secret"], decls, [], cast);
        Expect(priv.Count == 1 && priv["lichen"] == 3, "private memory: holder only");
        var pub = StoryStanding.Compute(["open"], decls, [], cast);
        Expect(pub["lichen"] == 3 && pub["moss"] == 1 && pub["fern"] == 1 && pub["pebble"] == 1 && pub.Count == 4, "public memory: holder 3, every other cast member +1, even one with no declaration");
        var revealed = StoryStanding.Compute(["secret"], decls, ["secret"], cast);
        Expect(revealed["lichen"] == 3 && revealed["moss"] == 1 && revealed["fern"] == 1 && revealed["pebble"] == 1, "a revealed memory counts as public");
        var bad = StoryStanding.Compute(["bad"], decls, [], cast);
        Expect(bad["moss"] == -2 && bad["lichen"] == -1 && bad["fern"] == -1 && bad["pebble"] == -1, "negative public memory: others -1");
        var mix = StoryStanding.Compute(["open", "bad"], decls, [], cast);
        Expect(mix["lichen"] == 3 - 1 && mix["moss"] == -2 + 1, "public memories cross over");
        Expect(StoryStanding.Compute(["gone"], decls, [], cast).Count == 0, "unknown memory ignored");
    }

    private static void Render()
    {
        StoryLine L(string t, params StoryCondition[] when) => new(t, when, P);
        var prose = new List<StoryParagraph>
        {
            new([L("Un."), L("Deux.", C(ConditionKind.Flag, "met")), L("Trois.", C(ConditionKind.Flag, "met", negated: true))]),
            new([L("Rien.", C(ConditionKind.Flag, "never"))]),
            new([L("Autre.")]),
        };
        var without = StoryText.Render(prose, Ctx(K()));
        Expect(without == "Un.\nTrois.\n\nAutre.", "render without the flag: " + without.Replace("\n", "|"));
        var with = StoryText.Render(prose, Ctx(K(flags: ["met"])));
        Expect(with == "Un.\nDeux.\n\nAutre.", "render with the flag: " + with.Replace("\n", "|"));
    }
}
