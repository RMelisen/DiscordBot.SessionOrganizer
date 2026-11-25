using System.Reflection;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

// 72. More than one server: what stays on the home server (real names, personal comebacks, known
// genders, /shame's hardcoded voters), what follows Papa and Tata, and the per-server silence file.
public static class MultiServerChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("multi-server: " + what);
    }

    // BotResponses is internal: its lookups are called by reflection, like the rest of the checks.
    private static readonly Type Br = typeof(ChatFacts).Assembly.GetType("ProjectSYNCS.Services.BotResponses")!;

    private static object? Call(string name, params object?[] args) =>
        (Br.GetMethod(name, BindingFlags.Public | BindingFlags.Static)
         ?? throw new InvalidOperationException("no BotResponses." + name)).Invoke(null, args);

    private const ulong Other = 123456789012345678;
    private const ulong Amandine = 324768221372743681;   // a home member with real name and comebacks

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        var tata = (ulong)Br.GetField("TataId")!.GetValue(null)!;
        var owner = AvailabilityService.OwnerId;

        // ---- Home server -------------------------------------------------------------------------
        Expect(HomeGuild.Is(HomeGuild.Id) && !HomeGuild.Is(Other) && !HomeGuild.Is(null), "HomeGuild.Is: home only, a DM is not");

        // ---- Real names: home only, nobody excepted ------------------------------------------------
        Expect((string?)Call("RealNameFor", owner, (ulong?)HomeGuild.Id, "x") == "Romain", "a real name shows on the home server");
        Expect((string?)Call("RealNameFor", owner, (ulong?)Other, "pseudo") == "pseudo", "no real name on another server, even Papa's");
        Expect((string?)Call("RealNameFor", tata, (ulong?)null, "pseudo") == "pseudo", "no real name in a DM");

        // ---- Personal comebacks: home only, Tata's follow her ---------------------------------------
        Expect(Call("PersonalComebacksFor", Amandine, (ulong?)HomeGuild.Id) is string[] { Length: > 0 },
            "a home member's comebacks on the home server");
        Expect(Call("PersonalComebacksFor", Amandine, (ulong?)Other) is null, "no home comebacks on another server");
        Expect(Call("PersonalComebacksFor", Amandine, (ulong?)null) is null, "no home comebacks in a DM");
        Expect(Call("PersonalComebacksFor", tata, (ulong?)Other) is string[] { Length: > 0 }, "Tata's comebacks follow her");

        // ---- Known genders: home only, Papa and Tata follow -----------------------------------------
        Expect(Call("GenderFor", Amandine, (ulong?)HomeGuild.Id) is not null, "a home member's gender on the home server");
        Expect(Call("GenderFor", Amandine, (ulong?)Other) is null, "no home member's gender elsewhere (neutral pool)");
        Expect(Call("GenderFor", owner, (ulong?)Other) is not null && Call("GenderFor", tata, (ulong?)null) is not null,
            "Papa's and Tata's genders follow them");

        // ---- The display name: Tata stays « Tata » everywhere (her tier follows her) -----------------
        Expect((string?)Br.GetMethod("DisplayNameFor", new[] { typeof(ulong), typeof(string) })!
            .Invoke(null, new object[] { tata, "Analuz" }) == "Tata", "Tata is « Tata » on any server");

        // ---- /shame's hardcoded voters --------------------------------------------------------------
        Expect(ShameModule.HardcodedExtraVotersFor(HomeGuild.Id).Contains(Amandine), "the hardcoded voters vote at home");
        Expect(ShameModule.HardcodedExtraVotersFor(Other).Count == 0, "no hardcoded voter on another server");

        // ---- Silence file -------------------------------------------------------------------------
        var since = new DateTimeOffset(2026, 11, 20, 18, 0, 0, TimeSpan.Zero);
        var old = SilenceStateFile.Parse("{\"Silent\":true,\"Since\":\"2026-11-20T18:00:00+00:00\"}", HomeGuild.Id);
        Expect(old.Count == 1 && old.TryGetValue(HomeGuild.Id, out var s) && s == since,
            "the old one-switch file reads as the home server silenced");
        Expect(SilenceStateFile.Parse("{\"Silent\":false,\"Since\":null}", HomeGuild.Id).Count == 0,
            "the old file, not silent, silences nothing");
        var both = new Dictionary<ulong, DateTimeOffset> { [HomeGuild.Id] = since, [Other] = since.AddHours(1) };
        var back = SilenceStateFile.Parse(SilenceStateFile.Write(both), HomeGuild.Id);
        Expect(back.Count == 2 && back[HomeGuild.Id] == since && back[Other] == since.AddHours(1),
            "the per-server file round-trips");
        Expect(SilenceStateFile.Parse(SilenceStateFile.Write(new Dictionary<ulong, DateTimeOffset>()), HomeGuild.Id).Count == 0,
            "an empty file silences nothing (not the home server by the old rule)");

        Health();
        Cards();
        Features();
    }

    // ---- Feature switches (ServerFeatures) and the morning state file -----------------------------------

    private static void Features()
    {
        // Stored strings: append-only. A rename here silently resets every server's choice.
        var frozen = new[] { "morning", "ambient", "weekend-poll", "quokka", "voice-spectator", "welcome" };
        Expect(ServerFeatures.All.Select(f => f.Key).SequenceEqual(frozen), "feature keys changed: they are stored (append-only)");
        Expect(ServerFeatures.All.Select(f => f.Key).Distinct().Count() == ServerFeatures.All.Count, "a feature key twice");
        Expect(ServerFeatures.All.All(f => f.Label.Length <= 100 && f.Description.Length <= 100), "a feature label or description over 100 (select caps)");

        var none = new Dictionary<string, bool>();
        Expect(ServerFeatures.All.All(f => ServerFeatures.IsOn(none, f.Key, HomeGuild.Id)), "everything on at home by default (= before)");
        Expect(ServerFeatures.All.All(f => ServerFeatures.IsOn(none, f.Key, Other) == (f.Key == ServerFeatures.Welcome)),
            "elsewhere only the welcome is on by default");
        Expect(!ServerFeatures.IsOn(new Dictionary<string, bool> { [ServerFeatures.Morning] = false }, ServerFeatures.Morning, HomeGuild.Id)
               && ServerFeatures.IsOn(new Dictionary<string, bool> { [ServerFeatures.Quokka] = true }, ServerFeatures.Quokka, Other),
            "a stored choice wins over the default, both ways");
        Expect(ServerFeatures.All.Where(f => f.NeedsMainChannel).Select(f => f.Key)
                .SequenceEqual(new[] { ServerFeatures.Morning, ServerFeatures.Ambient, ServerFeatures.WeekendPoll }),
            "the main-channel features are the hello, ambient life and the weekend poll");

        // The /config feature choices map onto the keys one to one.
        var keys = Enum.GetValues<ConfigModule.FeatureChoice>().Select(ConfigModule.KeyOf).ToList();
        Expect(keys.Order().SequenceEqual(frozen.Order()), "/config feature choices don't match the feature keys: " + string.Join(", ", keys));

        // The main channel: the home default on home only.
        var empty = GuildConfig.Empty;
        Expect(MainChannelOf(empty, HomeGuild.Id) != 0 && MainChannelOf(empty, Other) == 0,
            "no main channel set: the home server has its default, another has none");
        Expect(MainChannelOf(empty with { MainChannelId = 42 }, Other) == 42, "a set main channel wins anywhere");

        // morning-state.json: the old one-server shape is the home server's.
        var old = MorningStateFile.Parse("{\"LastDay\":20261124}", HomeGuild.Id);
        Expect(old.Count == 1 && old[HomeGuild.Id] == 20261124, "the old morning file reads as the home server's day");
        var days = new Dictionary<ulong, int> { [HomeGuild.Id] = 20261125, [Other] = 20261124 };
        var back = MorningStateFile.Parse(MorningStateFile.Write(days), HomeGuild.Id);
        Expect(back.Count == 2 && back[HomeGuild.Id] == 20261125 && back[Other] == 20261124, "the per-server morning file round-trips");
        Expect(MorningStateFile.Parse("{\"LastDay\":0}", HomeGuild.Id).Count == 0, "a never-greeted old file claims nothing");
    }

    // MorningGreetingService is internal: its static lookup is called by reflection.
    private static ulong MainChannelOf(GuildConfig config, ulong guildId) =>
        (ulong)typeof(ChatFacts).Assembly.GetType("ProjectSYNCS.Services.MorningGreetingService")!
            .GetMethod("MainChannelOf", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { config, guildId })!;

    // ---- Health check (ServerHealth, pure) ---------------------------------------------------------

    private static void Health()
    {
        var all = (ChannelNeed)0x7F;
        SlotFacts Slot(HealthSlot s, ulong id, ChannelNeed have, bool exists = true) => new(s, id, exists, have);
        var fine = new GuildFacts(ManageEvents: true, PlynlingRoleSet: false, null, QuizRoleSet: false, null);

        var ok = ServerHealth.Judge(new[] { Slot(HealthSlot.Game, 1, all), Slot(HealthSlot.Bots, 2, all), Slot(HealthSlot.Quiz, 0, 0) }, fine);
        Expect(ok.All(l => l.Ok) && ok.Count == 3, "every permission given: all ✅, an unset quiz says nothing: " + string.Join(" | ", ok));

        var noEmbed = ServerHealth.Judge(new[] { Slot(HealthSlot.Bots, 2, all & ~ChannelNeed.Embed) }, fine);
        Expect(noEmbed.Any(l => !l.Ok && l.Text.Contains("« Intégrer des liens »") && l.Text.Contains("<#2>")),
            "a bots channel without Embed Links names « Intégrer des liens »");

        var twoMissing = ServerHealth.Judge(new[]
        {
            Slot(HealthSlot.Main, 3, ChannelNeed.View | ChannelNeed.Send | ChannelNeed.Embed | ChannelNeed.ExternalEmojis),
        }, fine);
        Expect(twoMissing.Any(l => l.Text.Contains("« Ajouter des réactions » et « Voir les anciens messages »")),
            "two missing permissions are joined with « et », in Discord's order");

        var gone = ServerHealth.Judge(new[] { Slot(HealthSlot.Game, 9, 0, exists: false) }, fine);
        Expect(gone.Any(l => !l.Ok && l.Text.Contains("n'existe plus") && l.Text.Contains("/config game-channel set")),
            "a deleted channel says so and names its command");
        Expect(gone.Any(l => !l.Ok && l.Text.StartsWith("Aucun salon de jeu ni des bots", StringComparison.Ordinal)),
            "no game nor bots channel: the announcements have nowhere to go");

        var fresh = ServerHealth.Judge(Array.Empty<SlotFacts>(), new GuildFacts(false, true, RoleMirrorRules.NoManageRoles, true, null));
        Expect(fresh.Any(l => !l.Ok && l.Text.Contains("« Gérer les évènements »")), "no Manage Events: the Events tab line");
        Expect(fresh.Any(l => !l.Ok && l.Text == "Rôle Plynling : " + RoleMirrorRules.NoManageRoles), "a role problem reuses RoleMirror's text");
        Expect(fresh.Any(l => l.Ok && l.Text == "Rôle Grosse tête"), "a role she can manage is ✅");

        var unset = ServerHealth.Unset(new[] { Slot(HealthSlot.Game, 1, all), Slot(HealthSlot.Quiz, 0, 0) }, moderatorRoleSet: false);
        Expect(unset.SequenceEqual(new[] { "salon du quiz", "rôle modérateur" }),
            "Unset names what has no channel and the moderator role: " + string.Join(", ", unset));

        Expect(ServerHealth.Missing(ServerHealth.NeedsOf(HealthSlot.Main), ChannelNeed.None).SequenceEqual(new[] { "Voir le salon" }),
            "a channel she can't see names « Voir le salon » alone (Discord denies the rest there)");
        Expect(ServerHealth.Missing(ServerHealth.NeedsOf(HealthSlot.Game), ChannelNeed.View)
                .SequenceEqual(new[] { "Envoyer des messages", "Utiliser des émojis externes" }),
            "a channel she can't write in: no links or files named (Discord drops them without Send)");
        Expect(ServerHealth.PermissionNames.Select(p => p.Need).Distinct().Count() == 7, "seven permissions, each named once");
        foreach (var s in Enum.GetValues<HealthSlot>())
            Expect(ServerHealth.NeedsOf(s).HasFlag(ChannelNeed.View | ChannelNeed.Send), $"{s} needs at least view and send");
    }

    // ---- Cards: /config show, /config setup, the hello card -----------------------------------------

    private static void Cards()
    {
        // The worst /config show: every slot broken, both roles broken, everything unset, six of each list.
        var broken = ServerHealth.Judge(
            Enum.GetValues<HealthSlot>().Select(s => new SlotFacts(s, 100_000_000_000_000_000UL + (ulong)s, true, ChannelNeed.View)).ToList(),
            new GuildFacts(false, true,
                "Ce rôle est au-dessus du mien, ou au même niveau : monte mon rôle plus haut dans la liste (Paramètres du serveur → Rôles).",
                true, RoleMirrorRules.NoManageRoles));
        var everything = Enum.GetValues<HealthSlot>().Select(s => ServerHealth.LabelOf(s).ToLowerInvariant()).Append("rôle modérateur").ToList();
        var config = GuildConfig.Empty with
        {
            ShameVoters = Enumerable.Range(1, 6).Select(i => 100_000_000_000_000_000UL + (ulong)i).ToHashSet(),
            ExcludedChannels = Enumerable.Range(1, 6).Select(i => 200_000_000_000_000_000UL + (ulong)i).ToHashSet(),
            IdleChannels = Enumerable.Range(1, 6).Select(i => 300_000_000_000_000_000UL + (ulong)i).ToHashSet(),
        };
        foreach (var home in new[] { true, false })
        {
            var where = home ? "home" : "elsewhere";
            var card = ConfigCards.Build(config, home, home, home, "x", "y", broken, everything);
            int count = card.Components.Sum(Count);
            int text = card.Components.Sum(TextLength);
            Expect(count <= 40, $"/config show ({where}): {count} of 40 components");
            Expect(text <= 4000, $"/config show ({where}): {text} of 4000 characters");
            Console.WriteLine($"/config show at worst ({where}): {count} components, {text} characters");
        }

        var view = new SetupView(new Dictionary<HealthSlot, ulong> { [HealthSlot.Game] = 1, [HealthSlot.Quiz] = 2 },
            3, 0, 0, broken, everything,
            ServerFeatures.All.ToDictionary(f => f.Key, f => f.Key != ServerFeatures.Quokka));
        var ids = new HashSet<string>();
        foreach (var page in ConfigSetupCards.Order)
        {
            var card = ConfigSetupCards.Build(page, view);
            int count = card.Components.Sum(Count);
            var pageIds = CustomIds(card.Components).ToList();
            Expect(count <= 40, $"/config setup {page}: {count} of 40 components");
            Expect(card.Components.Sum(TextLength) <= 4000, $"/config setup {page}: text over 4000");
            Expect(pageIds.Count == pageIds.Distinct().Count(), $"/config setup {page}: a custom-id twice");
            Expect(pageIds.All(id => id.StartsWith(ConfigSetupCards.Prefix + ":", StringComparison.Ordinal) && id.Length <= 100),
                $"/config setup {page}: a custom-id off the prefix or over 100");
            Expect(ConfigSetupCards.Parse(page.ToString()) == page, $"/config setup {page}: its name parses back");
            ids.UnionWith(pageIds);
            Console.WriteLine($"/config setup {page}: {count} components");
        }
        Expect(ids.Contains("cfgsetup:done") && ids.Contains($"cfgsetup:ch:Game:{SetupPage.Channels}")
               && ids.Contains("cfgsetup:create:plynling:Roles"), "the setup pages carry the ids the handler listens for");
        Expect(ids.Contains($"cfgsetup:feat:{SetupPage.Life}") && ids.Contains($"cfgsetup:ch:{HealthSlot.Main}:{SetupPage.Life}"),
            "the life page carries the main-channel select and the features select");
        Expect(ConfigSetupCards.Index(SetupPage.Life) == ConfigSetupCards.Index(SetupPage.Summary) - 1,
            "the life page comes just before the summary");
        Expect(ConfigSetupCards.Parse("Renamed") == ConfigSetupCards.Order[0], "an unknown page falls back to the first");
        var done = ConfigSetupCards.BuildDone(view);
        Expect(!CustomIds(done.Components).Any() && done.Components.Sum(Count) <= 40, "the finished setup card has no buttons");

        var pool = (string[])Br.GetField("GuildHelloLines")!.GetValue(null)!;
        Expect(pool.Length >= 4 && pool.Distinct().Count() == pool.Length, "GuildHelloLines: at least four, no two alike");
        Expect(pool.All(l => !l.Contains('{') && !l.Contains('\n') && !l.Contains("onjour") && !l.Contains("onsoir")),
            "GuildHelloLines: no placeholder, one line, no bonjour/bonsoir (it fires at any hour)");
        foreach (var line in pool)
        {
            var hello = GuildHelloCards.Build(line);
            Expect(hello.Components.Sum(Count) <= 40 && hello.Components.Sum(TextLength) <= 4000, "the hello card fits");
        }
    }

    // Components V2: count the whole tree, like the other cards.
    private static int Count(Discord.IMessageComponent c) => 1 + c switch
    {
        Discord.ContainerComponent x => x.Components.Sum(Count),
        Discord.SectionComponent x => x.Components.Sum(Count) + (x.Accessory is null ? 0 : Count(x.Accessory)),
        Discord.ActionRowComponent x => x.Components.Sum(Count),
        _ => 0,
    };

    private static int TextLength(Discord.IMessageComponent c) => c switch
    {
        Discord.ContainerComponent x => x.Components.Sum(TextLength),
        Discord.SectionComponent x => x.Components.Sum(TextLength),
        Discord.TextDisplayComponent x => x.Content.Length,
        _ => 0,
    };

    private static IEnumerable<string> CustomIds(IEnumerable<Discord.IMessageComponent> components)
    {
        foreach (var c in components)
        {
            switch (c)
            {
                case Discord.ContainerComponent x: foreach (var id in CustomIds(x.Components)) yield return id; break;
                case Discord.ActionRowComponent x: foreach (var id in CustomIds(x.Components)) yield return id; break;
                case Discord.ButtonComponent { CustomId: { } id }: yield return id; break;
                case Discord.SelectMenuComponent { CustomId: { } id }: yield return id; break;
            }
        }
    }
}
