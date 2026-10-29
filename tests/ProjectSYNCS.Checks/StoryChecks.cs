using ProjectSYNCS.Helpers;

// 34. Story files (docs/agents/plynling-storylines.md): the tag splitter, the vocabulary, the parser,
// the validator, the walks, and the path report written by `-- story <arc>`.
public static class StoryChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("story: " + what);
    }

    public static void Run(Action<string> fail, string[] args)
    {
        _fail = fail;
        TagsAndVocabulary();
        ParserHeader();
        ParserScenes();
        Validator();
        Library();
        Walks();
        Reports(args);
        HandEdits();
    }

    // 34h. Hand-edit mistakes found in review: each must be an error, never silent.
    private static void HandEdits()
    {
        // Scene and ending fields: a typo, or a field after the blank line, is not prose.
        ExpectError(Valid.Replace("public: {A} hésite.", "publik: {A} hésite."), "field");
        ExpectError(Valid.Replace("public: Et maintenant ?\n", "public: Et maintenant ?\nhandvoer\n"), "handvoer");
        ExpectError(Valid.Replace("pace: next_day\npublic: {A} hésite.", "pace: next_day\n\npublic: {A} hésite."), "field");
        ExpectError(Valid.Replace("title: Un\n", "title: Un\ntitle: Une\n"), "twice");
        ExpectError(Valid.Replace("title: Le galet\n", "title: Le galet\npace: now\n"), "field");
        Parse(Valid, out var pe);
        Expect(pe.Count == 0, "the valid arc still parses: " + string.Join("; ", pe));
        Parse(Valid.Replace("Elle se tait.", "Silence : elle se tait."), out pe);
        Expect(pe.Count == 0, "French « Silence : … » stays prose: " + string.Join("; ", pe));
        // [other remember] lands on the other Plynling: only in a handed-over scene.
        var withOther = Valid.Replace("public: Une pousse.", "public: Une pousse.\nother: friends");
        ExpectInvalid("handed-over", withOther.Replace("[flag met] [remember lichen.x]", "[flag met] [other remember lichen.x]"));
        var handed = Validate(withOther.Replace("[flag met] [remember lichen.x]", "[flag met] [other remember lichen.x]").Replace("public: {A} hésite.", "public: {A} hésite.\nhandover"));
        Expect(!handed.Any(e => e.Message.Contains("handed-over")), "[other remember] in a handover scene is fine: " + string.Join("; ", handed));
        ExpectInvalid("handed-over", withOther.Replace("[keepsake pebble] [cailloux 20]", "[keepsake pebble] [cailloux 20] [other remember lichen.x]"));
        // A // note at the end of a line never reaches players.
        ExpectError(Valid.Replace("Elle se tait.", "Elle se tait. // à revoir"), "own line");
        ExpectError(Valid.Replace("title: Un\n", "title: Un // provisoire\n"), "own line");
        ExpectError(Valid.Replace("[flag met] [remember lichen.x] [-> two]", "[flag met] [remember lichen.x] [-> two] // vers deux"), "own line");
        // A file under a story folder that is not exactly *.story is reported, not left out.
        var probe = Path.Combine(RepoRoot(), "tests", "ProjectSYNCS.Checks", "out", "probe-stories");
        if (Directory.Exists(probe)) Directory.Delete(probe, true);
        Directory.CreateDirectory(probe);
        File.WriteAllText(Path.Combine(probe, "a.story.txt"), Valid);
        File.WriteAllText(Path.Combine(probe, "B.STORY"), Valid.Replace("# arc: v", "# arc: b"));
        var load = StoryLibrary.Load(Path.Combine(RepoRoot(), "ProjectSYNCS", "Stories"), probe);
        Expect(load.Errors.Count(e => e.Message.Contains(".story")) == 2, "stray files reported: " + string.Join("; ", load.Errors));
        Directory.Delete(probe, true);
    }

    // 34g. The path report, and `-- story <arc|all>`, which writes it to tests/ProjectSYNCS.Checks/out/.
    private static void Reports(string[] args)
    {
        var t = _fixture.Arcs.SingleOrDefault(x => x.Key == "test_everything");
        if (t is null) return;   // reported in 34e
        var rep = StoryReport.Write(t, StoryWalker.Walk(t, _fixture.Arcs));
        Expect(rep.StartsWith(t.Header.Title + " (test_everything)"), "report title line");
        foreach (var head in new[] { "Fins atteignables", "Décidé seul", "En personnage", "Chemin 1 / ", "Marques :", "Mémoires :", "jet Diplomatie 7", "🔒", "[si " })
            Expect(rep.Contains(head), "report has " + head);
        Expect(t.Endings.Where(x => x.Key is not ("interrupted" or "loss")).All(x => rep.Contains(x.Key)), "report names every ending");
        Expect(rep.Contains("« Le Fidèle »") && !rep.Contains("{a:"), "epithets in the masculine, no agreement left");

        if (args.Length < 2 || args[0] != "story") return;
        var arcs = _shipped.Arcs.Select(a => (a, _shipped)).Concat(_fixture.Arcs.Select(a => (a, _fixture)))
            .Where(x => args[1] == "all" || x.a.Key == args[1]).ToList();
        if (arcs.Count == 0) { _fail($"story: no arc '{args[1]}'"); return; }
        var outDir = Path.Combine(RepoRoot(), "tests", "ProjectSYNCS.Checks", "out");
        Directory.CreateDirectory(outDir);
        foreach (var (arc, load) in arcs)
        {
            var path = Path.Combine(outDir, arc.Key + ".paths.txt");
            File.WriteAllText(path, StoryReport.Write(arc, StoryWalker.Walk(arc, load.Arcs)));
            Console.WriteLine("story report: " + path);
        }
    }

    private static StoryArc ParseOne(string text)
    {
        var errors = new List<StoryError>();
        return StoryParser.Parse("t.story", text, errors) ?? throw new InvalidOperationException(string.Join("; ", errors));
    }

    private static void ExpectProblem(string text, string fragment)
    {
        var arc = ParseOne(text);
        var problems = StoryWalker.Walk(arc, new[] { arc }).Problems;
        Expect(problems.Any(p => p.Contains(fragment)), $"expected a walk problem with '{fragment}', got: {string.Join("; ", problems)}");
    }

    // 34f. The walks: every path, reachability, flags, deciding alone, cailloux, gender pairs.
    private static void Walks()
    {
        var valid = ParseOne(Valid);
        var ok = StoryWalker.Walk(valid, new[] { valid });
        Expect(ok.Problems.Count == 0, "valid arc walks clean: " + string.Join("; ", ok.Problems));
        Expect(ok.Paths.Count == 8, $"valid arc has 8 paths (2 × 4), got {ok.Paths.Count}");
        Expect(ok.Paths.Any(p => p.Alone) && ok.Paths.Any(p => !p.Alone), "alone and owner-only paths");
        Expect(ok.Paths.Where(p => p.Ending.Key == "e1").All(p => p.Cailloux == 40), "cailloux summed with the ending's");
        Expect(ok.Paths.Any(p => p.StressCosts.GetValueOrDefault("rowdy") == 10), "stress costs on a path");
        ExpectProblem(Valid.Replace("[-> e3]", "[-> e2]"), "unreachable e3");
        ExpectProblem(Valid.Replace("[-> e1]", "[-> one]"), "loop through one");
        ExpectProblem(Valid.Replace("[flag met]", "[flag mett]"), "met never set");
        ExpectProblem(Valid.Replace("[flag met]", "[flag mett]"), "mett never tested");
        ExpectProblem(Valid.Replace("[cailloux 20]", "[cailloux 90]"), "cailloux 180");
        ExpectProblem(Valid.Replace("{a:sorti|sortie}", "{a:sorti|sortie"), "template");
        ExpectProblem(Valid.Replace("[-> e3]", "[-> e2]").Replace("## ending: e3\ntitle: L'attente\nFin trois.\n[cailloux 10]\n", ""), "endings: 2");
        ExpectProblem(Valid.Replace("* leave | Partir", "* leave | Partir [owner-only]").Replace("* gift | Prendre le galet", "* gift | Prendre le galet [needs trait curious]")
            .Replace("* wait | Attendre  [roll learning 7]", "* wait | Attendre  [roll learning 7] [secret]"), "alone dead end at two");
        // The flag's own gate reads it exactly: with 'met' unset, 'stay' cannot see an option gated on it.
        var gated = ParseOne(Valid.Replace("* wait | Attendre  [roll learning 7]", "* wait | Attendre  [roll learning 7] [needs flag met \"Rencontrée\"]"));
        Expect(StoryWalker.Walk(gated, new[] { gated }).Paths.Where(p => p.Steps.Any(s => s.Option?.Key == "wait")).All(p => p.Steps[0].Option!.Key == "go"),
            "a flag gate is read exactly");
        var shares = ok.InCharacter.Single(x => x.Profile == "neutre").EndingShare;
        Expect(Math.Abs(shares.Values.Sum() - 1) < 1e-9, "in-character shares sum to 1");
        Expect(ok.InCharacter.Count == 1 + PlynlingTraits.All.Count(t => t.Kind == ProjectSYNCS.Models.TraitKind.Childhood), "a bébé arc is read for each childhood trait");
        foreach (var arc in _fixture.Arcs)
            foreach (var pr in StoryWalker.Walk(arc, _fixture.Arcs).Problems) _fail($"fixture walk: {arc.Key}: {pr}");
        foreach (var arc in _shipped.Arcs)
            foreach (var pr in StoryWalker.Walk(arc, _shipped.Arcs).Problems) _fail($"story walk: {arc.Key}: {pr}");
    }

    // The repository root: the folder holding ProjectSYNCS/config.yaml, above the checks' bin folder.
    public static string RepoRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "ProjectSYNCS", "config.yaml"))) return dir.FullName;
        throw new InvalidOperationException("repository root not found above " + AppContext.BaseDirectory);
    }

    private static StoryLoad _shipped = null!;
    private static StoryLoad _fixture = null!;

    // 34e. The shipped stories and the cast, and the fixture arc that uses every tag.
    private static void Library()
    {
        var root = RepoRoot();
        var stories = Path.Combine(root, "ProjectSYNCS", "Stories");
        _shipped = StoryLibrary.Load(stories, stories);
        foreach (var err in _shipped.Errors) _fail("story file: " + err);
        Expect(_shipped.Cast.Count == 13 && _shipped.Cast.Any(c => c is { Key: "lichen", GivenName: "Ysaure", Byname: "la Mère Lichen" }), "village.cast");
        _fixture = StoryLibrary.Load(stories, Path.Combine(root, "tests", "ProjectSYNCS.Checks", "Stories"));
        foreach (var err in _fixture.Errors) _fail("fixture: " + err);
        var t = _fixture.Arcs.SingleOrDefault(x => x.Key == "test_everything");
        if (t is null) { Expect(false, "fixture arc test_everything loads"); return; }
        var effects = StoryValidator.Effects(t).Select(x => x.Kind).ToHashSet();
        foreach (var k in Enum.GetValues<EffectKind>()) Expect(effects.Contains(k), $"fixture uses effect {k}");
        var conditions = StoryValidator.Conditions(t).Select(x => x.Kind).ToHashSet();
        foreach (var k in Enum.GetValues<ConditionKind>()) Expect(conditions.Contains(k), $"fixture uses condition {k}");
        var options = t.Scenes.SelectMany(s => s.Options).ToList();
        Expect(options.Any(o => o.Roll is not null) && options.Any(o => o.OwnerOnly) && options.Any(o => o.Secret) && options.Any(o => o.Retired)
            && options.Any(o => o.Public is not null) && options.Any(o => o.LockText is not null) && options.Any(o => o.StressCosts.Count > 0), "fixture uses every option attribute");
        Expect(t.Scenes.Any(s => s.Narration is not null) && t.Scenes.Any(s => s.RetiredTo is not null) && t.Scenes.Any(s => s.Handover)
            && t.Scenes.Any(s => s.Bump) && t.Scenes.Any(s => s.Key == "loss"), "fixture has narration, retired, handover, bump and loss scenes");
        var lines = t.Scenes.SelectMany(s => s.Prose).SelectMany(p => p.Lines).ToList();
        Expect(lines.Any(l => l.When.Any(c => !c.Negated)) && lines.Any(l => l.When.Any(c => c.Negated)), "fixture has [if] and [unless] lines");
        var texts = string.Join("\n", StoryValidator.Texts(t).Select(x => x.Text));
        Expect(texts.Contains("{a:") && texts.Contains("{b:") && texts.Contains("{p:") && texts.Contains("{B}"), "fixture uses every agreement");
        Expect(t.Memories.Any(m => m.Public) && t.Memories.Any(m => !m.Public) && t.Epithets.Count > 0 && t.Keepsakes.Count > 0, "fixture declares memories, epithet, keepsake");
    }

    // A small arc that passes every check. The checks below and the walk checks each change one spot of it.
    private const string Valid = """
        # arc: v
        title: V
        family: milestone
        stage: baby
        trigger: adoption
        public: Une pousse.

        ## memories
        lichen.x | lichen | private | +2 | Est venue.

        ## keepsakes
        pebble | Le galet | Un galet tiède.

        ## scene: one
        title: Un
        pace: next_day
        public: {A} hésite.
        {A} est {a:sorti|sortie} de la Souche.

        * go | Y aller
          [lean sociability +1]
          {A} y va.
          [flag met] [remember lichen.x] [-> two]

        * stay | Rester  [needs trait curious]
          [stress rowdy 10]
          {A} attend.
          [-> two]

        ## scene: two
        title: Deux
        pace: now
        public: Et maintenant ?
        [if flag met] Elle sourit.
        Elle se tait.

        * gift | Prendre le galet
          {A} le prend.
          [cailloux 20] [-> e1]

        * leave | Partir
          {A} part.
          [-> e2]

        * wait | Attendre  [roll learning 7]
          ? success
            {A} tient bon.
            [-> e3]
          ? failure
            {A} s'endort.
            [-> e2]

        ## ending: e1
        title: Le galet
        Fin un.
        [keepsake pebble] [cailloux 20]

        ## ending: e2
        title: Le départ
        Fin deux.
        [cailloux 10]

        ## ending: e3
        title: L'attente
        Fin trois.
        [cailloux 10]

        ## ending: interrupted
        title: Interrompu
        La Souche reprend {A}.
        """;

    private const string Cast = "# cast\nlichen | Ysaure | la Mère Lichen\n";

    // Parse errors and validation errors of the texts, each its own file.
    private static List<StoryError> Validate(params string[] texts)
    {
        var errors = new List<StoryError>();
        var arcs = texts.Select((t, i) => StoryParser.Parse($"t{i}.story", t, errors)).OfType<StoryArc>().ToList();
        var cast = StoryParser.ParseCast("village.cast", Cast, errors);
        errors.AddRange(StoryValidator.Validate(arcs, cast));
        return errors;
    }

    private static void ExpectInvalid(string fragment, params string[] texts)
    {
        var errors = Validate(texts);
        Expect(errors.Any(e => e.Message.Contains(fragment)), $"expected a validation error with '{fragment}', got: {string.Join("; ", errors)}");
    }

    // 34d. The validator: keys, catalogs, targets, references, tokens, options, marks, ranges, caps.
    private static void Validator()
    {
        var clean = Validate(Valid);
        Expect(clean.Count == 0, "valid arc: " + string.Join("; ", clean));
        ExpectInvalid("nowhere", Valid.Replace("[-> two]", "[-> nowhere]"));
        ExpectInvalid("interrupted", Valid.Replace("## ending: interrupted", "## ending: cut"));
        ExpectInvalid("loss", Valid.Replace("family: milestone", "family: love\nother: lovers"));
        ExpectInvalid("other", Valid.Replace("family: milestone", "family: rivalry"));
        ExpectInvalid("lichen.y", Valid.Replace("[remember lichen.x]", "[remember lichen.y]"));
        ExpectInvalid("holder", Valid.Replace("lichen.x | lichen |", "lichen.x | sauge |"));
        ExpectInvalid("private", Valid.Replace("[remember lichen.x]", "[reveal lichen.x]").Replace("| private |", "| public |"));
        ExpectInvalid("curiouss", Valid.Replace("trait curious", "trait curiouss"));
        ExpectInvalid("nope", Valid.Replace("[cailloux 20] [-> e1]", "[modifier nope] [-> e1]"));
        ExpectInvalid("{B}", Valid.Replace("{A} attend.", "{B} attend."));
        ExpectInvalid("{C}", Valid.Replace("{A} attend.", "{C} attend."));
        ExpectInvalid("90", Valid.Replace("[stress rowdy 10]", "[stress rowdy 90]"));
        ExpectInvalid("80", Valid.Replace("* stay | Rester", "* stay | " + new string('x', 79)));
        ExpectInvalid("ending", Valid.Replace("[cailloux 20] [-> e1]", "[keepsake pebble] [-> e1]"));
        ExpectInvalid("free", Valid.Replace("* gift | Prendre le galet", "* gift | Prendre le galet [owner-only]").Replace("* leave | Partir", "* leave | Partir [secret]")
            .Replace("* wait | Attendre  [roll learning 7]", "* wait | Attendre  [roll learning 7] [owner-only]"));
        ExpectInvalid("2 to 4", Valid.Replace("* stay | Rester  [needs trait curious]", "* stay | Rester  [needs trait curious] [retired]"));
        ExpectInvalid("lock text", Valid.Replace("[needs trait curious]", "[needs flag met]"));
        ExpectInvalid("other_arc", Valid.Replace("[if flag met]", "[if flag other_arc.met]"));
        ExpectInvalid("lichen.x", Valid, Valid.Replace("# arc: v", "# arc: v2"));
        ExpectInvalid("'v'", Valid, Valid);
        // Two problems in one file are both reported.
        var two = Validate(Valid.Replace("[-> two]", "[-> nowhere]").Replace("trait curious", "trait curiouss"));
        Expect(two.Count >= 2, "every error reported, not the first only: " + string.Join("; ", two));
        Expect(two.All(e => e.Pos.File == "t0.story" && e.Pos.Line > 0), "errors carry file and line");
    }

    // The spec's sample arc, with an ending.
    private const string Sample = """
        # arc: arrival
        title: L'arrivée
        family: milestone
        stage: baby
        trigger: adoption
        public: Une pousse vient de sortir de la Souche.

        ## memories
        lichen.came_first    | lichen  | private | +5 | Est venue à elle d'elle-même, le premier matin.
        lichen.ran_to_wood   | lichen  | private | -3 | A filé vers le Bois Sourd avant même de savoir marcher.

        ## keepsakes
        lichen_pebble | Le galet de la Mère Lichen | Un galet de la Sommeille, tiède, que la Mère Lichen a posé près de {A} le premier soir.

        ## scene: first_steps
        title: Les premiers pas
        pace: next_day
        public: {A} hésite au bord de la Souche.

        La pluie s'est arrêtée dans la nuit. {A} est {a:sorti|sortie} de la Souche avant l'aube, et reste là, collé{a:|e} à l'écorce.
        [if trait curious] {A} a déjà goûté une feuille, puis une deuxième.
        À quelques pas, une vieille femme grise est assise sur une pierre. Elle ne dit rien. Elle attend.

        * to_her | Aller vers la vieille femme
          [lean sociability +2]
          {A} fait trois pas, s'arrête, en fait quatre autres. La vieille femme ne bouge pas, mais quelque chose se détend dans ses épaules.
          [flag came_to_her] [remember lichen.came_first] [-> the_cabin]

        * stay | Rester contre la Souche
          [lean boldness -1] [stress rowdy 10]
          {A} se colle à l'écorce jusqu'à midi. La vieille femme non plus ne bouge pas. Elles sont deux à savoir attendre.
          [-> the_cabin]

        * wood | Partir vers les grands arbres  [needs trait curious]  [roll learning 7]
          [lean boldness +2]
          ? success
            {A} trottine jusqu'à la lisière, renifle, et revient de son propre chef, le bout du chapeau trempé.
            [-> the_cabin]
          ? failure
            {A} s'enfonce sous les fougères et ne retrouve plus la lumière. C'est elle qui vient le chercher.
            [remember lichen.ran_to_wood] [stress 10] [-> lost_at_edge]

        ## ending: welcomed
        title: Le premier soir
        La vieille femme pose un galet près de {A}, sans un mot.
        [keepsake lichen_pebble] [cailloux 50]
        """;

    // 34c. Scenes, prose, options, rolls, narration, retired scenes, endings.
    private static void ParserScenes()
    {
        var a = Parse(Sample, out var pe);
        Expect(pe.Count == 0, "sample parses: " + string.Join("; ", pe));
        var s = a?.Scenes.SingleOrDefault();
        if (s is null) { Expect(false, "sample has one scene"); return; }
        Expect(s is { Key: "first_steps", Title: "Les premiers pas", Pace.Kind: PaceKind.NextDay } && s.Public!.Contains("hésite"), "scene header");
        Expect(s.Prose.Count == 1 && s.Prose[0].Lines.Count == 3 && s.Prose[0].Lines[1].When.SingleOrDefault() is { Kind: ConditionKind.Trait, Key: "curious" }
            && s.Prose[0].Lines[1].Text.StartsWith("{A} a déjà"), "conditional line");
        Expect(s.Options.Select(o => o.Key).SequenceEqual(new[] { "to_her", "stay", "wood" }), "options in order");
        if (s.Options.Count != 3) return;
        var wood = s.Options[2];
        Expect(wood.Label == "Partir vers les grands arbres" && wood.Roll == new StoryRoll(PlynlingStat.Learning, 7)
            && wood.Needs.SingleOrDefault()?.Key == "curious" && wood.Lean.GetValueOrDefault(AiAxis.Boldness) == 2, "wood attributes");
        Expect(wood.Outcome is null && wood.Success?.Next == "the_cabin" && wood.Failure?.Next == "lost_at_edge"
            && wood.Failure.Effects.Any(x => x.Kind == EffectKind.Stress && x.Amount == 10), "roll outcomes");
        Expect(s.Options[1].StressCosts.GetValueOrDefault("rowdy") == 10 && s.Options[1].Lean.GetValueOrDefault(AiAxis.Boldness) == -1
            && s.Options[0].Outcome?.Effects.Count == 2 && s.Options[0].Outcome!.Next == "the_cabin", "stress cost vs effects");
        Expect(a!.Endings.SingleOrDefault()?.Marks.Select(m => m.Kind).SequenceEqual(new[] { EffectKind.Keepsake, EffectKind.Cailloux }) == true
            && a.Endings[0].Title == "Le premier soir" && a.Endings[0].Prose.Count == 1, "ending");
        Expect(s.Options[0].Pos.Line == 24 && wood.Failure!.Pos.Line > wood.Success!.Pos.Line, "option positions");
        // Indentation is ignored.
        Parse(Sample.Replace("\n  ", "\n\t").Replace("\n    ", "\n"), out pe);
        Expect(pe.Count == 0, "tabs or no indent: " + string.Join("; ", pe));
        ExpectError(Sample.Replace("[-> the_cabin]\n\n* stay", "\n\n* stay"), "->");
        ExpectError(Sample.Replace("  [roll learning 7]", ""), "roll");
        ExpectError(Sample.Replace("? failure", "").Replace("[remember lichen.ran_to_wood] [stress 10] [-> lost_at_edge]", ""), "failure");
        ExpectError(Sample.Replace("[lean sociability +2]", "[remeber lichen.x]"), "remeber");
        ExpectError(Sample.Replace("## scene:", "## scnee:"), "scnee");
        ExpectError(Sample.Replace("pace: next_day", "pace: nextday"), "nextday");
        ExpectError(Sample.Replace("La pluie s'est", "La pluie [flag x] s'est"), "prose");
        ExpectError(Sample.Replace("[flag came_to_her] [remember lichen.came_first] [-> the_cabin]",
            "[flag came_to_her] [-> the_cabin]\n  [lean honor +1]"), "before");
        ExpectError(Sample.Replace("[cailloux 50]", "[cailloux 50] [-> first_steps]"), "ending");
        ExpectError(Sample.Replace("title: Les premiers pas\n", ""), "title");
        ExpectError(Head + "* early | Trop tôt\n", "option");
        var n = Parse(Head + "## scene: beat\ntitle: B\npace: now\nUne ligne.\n[flag seen] [-> end]\n## ending: end\ntitle: E\nFin.\n", out pe);
        Expect(pe.Count == 0 && n!.Scenes[0].Options.Count == 0 && n.Scenes[0].Narration?.Next == "end"
            && n.Scenes[0].Narration!.Effects.Single().Kind == EffectKind.Flag && n.Scenes[0].Pace.Kind == PaceKind.Now, "narration scene: " + string.Join("; ", pe));
        var h = Parse(Head + "## scene: later\ntitle: B\npace: +6h\nbump\nhandover\nTexte.\n* a | A\n  Oui.\n  [-> end]\n* b | B [owner-only] [secret]\n  [retired]\n## ending: end\ntitle: E\nFin.\n", out pe);
        Expect(pe.Count == 0 && h!.Scenes[0] is { Bump: true, Handover: true, Pace: { Kind: PaceKind.Hours, Hours: 6 } }
            && h.Scenes[0].Options[1] is { OwnerOnly: true, Secret: true, Retired: true, Outcome: null }, "pace in hours, bump, handover, retired option: " + string.Join("; ", pe));
        var r = Parse(Head + "## scene: old\nretired -> beat\n", out pe);
        Expect(pe.Count == 0 && r!.Scenes[0].RetiredTo == "beat", "retired scene: " + string.Join("; ", pe));
        var lk = Parse(Head + "## scene: x\ntitle: X\npace: now\nTexte.\n* a | A [needs flag owes \"La pie vous doit une faveur\"] [public \"{A} file.\"]\n  Oui.\n  [-> end]\n", out pe);
        Expect(lk?.Scenes[0].Options[0] is { LockText: "La pie vous doit une faveur", Public: "{A} file." }, "lock text and public line: " + string.Join("; ", pe));
    }

    private static StoryArc? Parse(string text, out List<StoryError> errors)
    {
        errors = new List<StoryError>();
        return StoryParser.Parse("t.story", text, errors);
    }

    // Fails unless parsing the text gives an error whose message contains the fragment.
    private static void ExpectError(string text, string fragment)
    {
        Parse(text, out var errors);
        Expect(errors.Any(e => e.Message.Contains(fragment)), $"expected an error with '{fragment}', got: {string.Join("; ", errors)}");
    }

    private const string Head = "# arc: t\ntitle: T\nfamily: milestone\nstage: baby\ntrigger: adoption\npublic: P\n";

    // 34b. The header, the declarations, the cast.
    private static void ParserHeader()
    {
        var a = Parse(Head + "## memories\nlichen.x | lichen | private | +5 | Est venue.\n## epithets\nshy_one | {a:Le Timide|La Timide}\n"
            + "## keepsakes\npebble | Le galet | Un galet tiède. | hurts\n", out var pe);
        Expect(pe.Count == 0, "valid header: " + string.Join("; ", pe));
        Expect(a is { Key: "t", Header.Family: StoryFamily.Milestone, Header.Trigger.Kind: TriggerKind.Adoption }
            && a.Header.Stages.SequenceEqual(new[] { PlynlingStage.Baby }) && a.Header.Title == "T" && a.Header.Public == "P", "header fields");
        Expect(a?.Memories.SingleOrDefault() is { Key: "lichen.x", Holder: "lichen", Public: false, Shift: 5, Wording: "Est venue." }, "memory");
        Expect(a?.Keepsakes.SingleOrDefault() is { Hurts: true, Name: "Le galet" } && a.Epithets.Single().Title == "{a:Le Timide|La Timide}", "keepsake, epithet");
        a = Parse(Head.Replace("trigger: adoption", "trigger: draw\nweight: 10\nweight: +5 [if passion gardening] [if season autumn]\nshelf: 7d\nother: friends\nrequires: [stage adult] [not trait shy]")
            .Replace("stage: baby", "stage: adult, elder"), out pe);
        Expect(pe.Count == 0, "valid drawn header: " + string.Join("; ", pe));
        Expect(a is { Header.Weight: 10, Header.Other: PlynlingBond.Friends } && a.Header.Shelf == TimeSpan.FromDays(7)
            && a.Header.WeightBonuses.Single() is { Bonus: 5, When.Count: 2 } && a.Header.Requires.Count == 2
            && a.Header.Stages.SequenceEqual(new[] { PlynlingStage.Adult, PlynlingStage.Elder }), "weight, shelf, other, requires, stages");
        a = Parse(Head.Replace("trigger: adoption", "trigger: death lovers"), out pe);
        Expect(a?.Header.Trigger == new StoryTrigger(TriggerKind.Death, "lovers"), "trigger with an argument");
        // CRLF and a BOM read like LF, with the right line numbers.
        Parse("﻿" + (Head + "colour: blue\n").Replace("\n", "\r\n"), out pe);
        Expect(pe.Any(x => x.Pos.Line == 7 && x.Message.Contains("colour")), "CRLF+BOM: unknown field on line 7, got: " + string.Join("; ", pe));
        ExpectError(Head.Replace("public: P\n", ""), "public");
        ExpectError(Head.Replace("stage: baby", "stage: toddler"), "toddler");
        ExpectError(Head.Replace("family: milestone", "family: drama"), "drama");
        ExpectError(Head.Replace("trigger: adoption", "trigger: break 7"), "break");
        ExpectError(Head + "title: Encore\n", "twice");
        ExpectError("# beat: x\n", "not supported");
        ExpectError("title: T\n", "# arc:");
        ExpectError(Head + "## memories\nlichen.x | lichen | private | 5\n", "columns");
        ExpectError(Head + "## memories\nlichen.x | lichen | secret | 5 | Mot.\n", "private or public");
        ExpectError(Head + "## memories\nlichen.x | lichen | private | beaucoup | Mot.\n", "beaucoup");
        var castErrors = new List<StoryError>();
        var cast = StoryParser.ParseCast("c", "# cast\n// note\nlichen | Ysaure | la Mère Lichen\n", castErrors);
        Expect(castErrors.Count == 0 && cast.SingleOrDefault() is { Key: "lichen", GivenName: "Ysaure", Byname: "la Mère Lichen" }, "cast");
        StoryParser.ParseCast("c", "# cast\nlichen | Ysaure\n", castErrors);
        Expect(castErrors.Count == 1, "cast line with two columns");
    }

    // 34a. Splitting a line into tags and text, and reading the words of a tag.
    private static void TagsAndVocabulary()
    {
        var p = StoryTags.Split("[if trait curious] {A} goûte une feuille.");
        Expect(p.Leading.SequenceEqual(new[] { "if trait curious" }) && p.Text == "{A} goûte une feuille." && p.Trailing.Count == 0, "split leading");
        p = StoryTags.Split("* wood | Partir  [needs trait curious]  [roll learning 7]");
        Expect(p.Text == "* wood | Partir" && p.Trailing.SequenceEqual(new[] { "needs trait curious", "roll learning 7" }), "split trailing");
        p = StoryTags.Split("[flag a] [remember lichen.came_first] [-> the_cabin]");
        Expect(p.Text == "" && p.Leading.Count == 3, "split tag-only line");
        Expect(StoryTags.Split("[needs flag x \"a ] b\"]").Leading.SingleOrDefault() == "needs flag x \"a ] b\"", "bracket inside quotes");
        Expect(StoryTags.Split("{A} est {a:sorti|sortie}.").Problem is null, "braces are text");
        Expect(StoryTags.Split("Elle dit [quelque chose] puis part.").Problem is not null, "tag in the middle of prose");
        Expect(StoryTags.Split("Une phrase [sans fin").Problem is not null, "unclosed bracket");
        Expect(StoryTags.Split("Une phrase] de trop").Problem is not null, "stray closing bracket");
        var w = StoryTags.Words("needs flag owes \"La pie vous doit une faveur\"");
        Expect(w.Count == 4 && w[3].Quoted && w[3].Text == "La pie vous doit une faveur", "quoted word");
        Expect(StoryTags.Words("trait shy -> brave").Select(x => x.Text).SequenceEqual(new[] { "trait", "shy", "->", "brave" }), "arrow word");
        Expect(StoryTags.Words("->the_cabin").Select(x => x.Text).SequenceEqual(new[] { "->", "the_cabin" }), "arrow glued to its target");

        var errs = new List<StoryError>();
        var at = new StoryPos("t.story", 1);
        var c = StoryVocabulary.Condition(StoryTags.Words("stat learning >= 6"), false, at, errs);
        Expect(c is { Kind: ConditionKind.Stat, Key: "learning", Op: CompareOp.AtLeast, Number: 6 }, "stat condition");
        c = StoryVocabulary.Condition(StoryTags.Words("standing lichen < 0"), false, at, errs);
        Expect(c is { Kind: ConditionKind.Standing, Key: "lichen", Op: CompareOp.Below, Number: 0 }, "standing condition");
        c = StoryVocabulary.Condition(StoryTags.Words("not flag arrival.came_to_her"), false, at, errs);
        Expect(c is { Kind: ConditionKind.Flag, Key: "arrival.came_to_her", Negated: true }, "negated cross-arc flag");
        c = StoryVocabulary.Condition(StoryTags.Words("other trait brave"), true, at, errs);
        Expect(c is { Kind: ConditionKind.OtherTrait, Key: "brave", Negated: true }, "other trait, unless");
        c = StoryVocabulary.Condition(StoryTags.Words("relation bestfriends"), false, at, errs);
        Expect(c is { Kind: ConditionKind.Relation, Key: "bestfriends" }, "relation");
        var e = StoryVocabulary.Effect(StoryTags.Words("trait shy -> brave"), at, errs);
        Expect(e is { Kind: EffectKind.Trait, Key: "shy", To: "brave" }, "trait swap");
        e = StoryVocabulary.Effect(StoryTags.Words("lift happiness 0.3"), at, errs);
        Expect(e is { Kind: EffectKind.Lift, Key: "happiness", Amount: 0.3 }, "lift");
        e = StoryVocabulary.Effect(StoryTags.Words("other remember lichen.x"), at, errs);
        Expect(e is { Kind: EffectKind.Remember, Key: "lichen.x", Other: true }, "other remember");
        e = StoryVocabulary.Effect(StoryTags.Words("stress -20"), at, errs);
        Expect(e is { Kind: EffectKind.Stress, Amount: -20 }, "stress relief");
        e = StoryVocabulary.Effect(StoryTags.Words("affinity +10"), at, errs);
        Expect(e is { Kind: EffectKind.Affinity, Amount: 10 }, "affinity");
        e = StoryVocabulary.Effect(StoryTags.Words("grow learning"), at, errs);
        Expect(e is { Kind: EffectKind.Grow, Key: "learning" }, "grow");
        Expect(errs.Count == 0, "no errors on valid words: " + string.Join("; ", errs));

        Expect(StoryVocabulary.Effect(StoryTags.Words("remeber lichen.x"), at, errs) is null && errs.Any(x => x.Message.Contains("remeber")), "typo named");
        Expect(StoryVocabulary.Condition(StoryTags.Words("stage toddler"), false, at, errs) is null && errs.Any(x => x.Message.Contains("baby")), "bad stage lists the choices");
        Expect(StoryVocabulary.Effect(StoryTags.Words("cailloux beaucoup"), at, errs) is null, "bad number");
        Expect(StoryVocabulary.Effect(StoryTags.Words("flag"), at, errs) is null, "missing word");
        Expect(StoryVocabulary.Condition(StoryTags.Words("stat learning 6"), false, at, errs) is null, "stat without a comparison");
        Expect(StoryVocabulary.Duration("7d") == TimeSpan.FromDays(7) && StoryVocabulary.Duration("36h") == TimeSpan.FromHours(36)
            && StoryVocabulary.Duration("7") is null, "durations");
        Expect(StoryVocabulary.TryEnum<PlynlingStage>("teen", out var st) && st == PlynlingStage.Teen
            && !StoryVocabulary.TryEnum<PlynlingStage>("Teen", out _) && !StoryVocabulary.TryEnum<PlynlingStage>("1", out _), "enum words are lowercase names");
    }
}
