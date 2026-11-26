using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 75. Plynling badge art (docs/agents/plynling/badges.md): every tier and moment has a rosette icon key
// with a valid emoji name, clashing with no member or collection badge; the icon falls back to medal +
// emoji until uploaded; every key's PNG is shipped and a stray one is skipped; and the journal at its
// fullest, every icon uploaded, still fits its text display.
public static class PlynlingBadgeArtChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("badge art: " + what);
    }

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        ItemEmojis.Clear();
        Keys();
        Icons();
        Shipped();
        Length();
        ItemEmojis.Clear();
    }

    private static void Keys()
    {
        var all = PlynlingBadges.All;
        Expect(all.Select(b => b.IconKey).Distinct().Count() == 62, $"{all.Select(b => b.IconKey).Distinct().Count()} distinct icon keys, expected 62");
        foreach (var b in all)
        {
            var name = ItemEmojis.EmojiName(b.IconKey);
            Expect(PlynlingBadges.IsIconKey(b.IconKey), $"{b.IconKey} is not an icon key");
            Expect(name is not null && name.StartsWith("pb_", StringComparison.Ordinal) && name.Length <= 32
                   && name.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'), $"{b.IconKey}: emoji name '{name}'");
            Expect(!MemberBadges.IsIconKey(b.IconKey) && !CollectionTiers.IsBadgeKey(b.IconKey), $"{b.IconKey} clashes with another badge");
        }
        Expect(PlynlingBadges.ByKey("week")!.IconKey == "plbadge.longevity_bronze", "week → plbadge.longevity_bronze");
        Expect(PlynlingBadges.ByKey("games_100")!.IconKey == "plbadge.player_platinum", "games_100 → plbadge.player_platinum");
        Expect(PlynlingBadges.ByKey("two_years")!.IconKey == "plbadge.longevity_diamond", "two_years → plbadge.longevity_diamond");
        Expect(PlynlingBadges.ByKey("couple")!.IconKey == "plbadge.couple", "couple → plbadge.couple");
        Expect(ItemEmojis.EmojiName("plbadge.beloved_platinum") == "pb_beloved_platinum", "pb_beloved_platinum");
        Expect(!PlynlingBadges.IsIconKey("plbadge.nope") && !PlynlingBadges.IsIconKey("badge.voice_gold"), "IsIconKey refuses others");
    }

    private static void Icons()
    {
        ItemEmojis.Clear();
        Expect(PlynlingBadges.ByKey("week")!.Icon == "🥉🌳", $"fallback tier icon '{PlynlingBadges.ByKey("week")!.Icon}'");
        Expect(PlynlingBadges.ByKey("couple")!.Icon == "💞", $"fallback moment icon '{PlynlingBadges.ByKey("couple")!.Icon}'");
        ItemEmojis.Set("plbadge.couple", "<:pb_couple:1>");
        ItemEmojis.Set("plbadge.longevity_bronze", "<:pb_longevity_bronze:2>");
        Expect(PlynlingBadges.ByKey("couple")!.Icon == "<:pb_couple:1>", "uploaded moment icon");
        Expect(PlynlingBadges.ByKey("week")!.Icon == "<:pb_longevity_bronze:2>", "uploaded tier icon");
        ItemEmojis.Clear();
    }

    private static void Shipped()
    {
        var iconDir = Enumerable.Range(0, 8)
            .Select(up => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, string.Concat(Enumerable.Repeat("../", up)), "ProjectSYNCS", "Assets", "Icons")))
            .FirstOrDefault(Directory.Exists);
        if (iconDir is null)
        {
            Expect(false, "ProjectSYNCS/Assets/Icons not found above the checks");
            return;
        }
        var shipped = ItemEmojis.Sources("", iconDir).ToDictionary(s => s.Key, s => s.EmojiName);
        foreach (var b in PlynlingBadges.All)
            Expect(shipped.TryGetValue(b.IconKey, out var name) && name == ItemEmojis.EmojiName(b.IconKey),
                $"{b.IconKey}.png missing from Assets/Icons (draw it in tools/item-art), or its emoji name is wrong");

        // A plbadge file no catalog badge claims is skipped, never uploaded under a made-up name.
        var tmp = Path.Combine(Path.GetTempPath(), "syncs-plbadge-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tmp);
        try
        {
            File.Copy(Path.Combine(iconDir, "plbadge.couple.png"), Path.Combine(tmp, "plbadge.nope.png"));
            Expect(ItemEmojis.Sources("", tmp).Single().EmojiName is null, "a stray plbadge.nope.png is skipped");
        }
        catch (IOException e)
        {
            Expect(false, "stray file: " + e.Message);
        }
        finally
        {
            Directory.Delete(tmp, true);
        }
    }

    private static void Length()
    {
        var now = new DateTimeOffset(2026, 11, 26, 12, 0, 0, TimeSpan.Zero);
        foreach (var b in PlynlingBadges.All)
            ItemEmojis.Set(b.IconKey, ItemEmojis.Markup("pb_" + new string('x', 29), ulong.MaxValue));
        var p = new Plynling
        {
            Name = new string('*', 32), Species = PlynlingSpecies.Amanite, AdoptedAt = now.AddDays(-9999), NeedsAsOf = now,
            Meals = 99_999, Pets = 99_999, Plays = 99_999, PlaysWon = 99_999, Visits = 99_999, Gender = PlynlingGender.Female,
        };
        var badges = PlynlingBadges.All.Select(b => new PlynlingBadge { Key = b.Key, EarnedAt = now }).ToList();
        // The longest badge name (feminine), on every moment of the page.
        var longest = PlynlingBadges.All.MaxBy(b => b.Name(PlynlingGender.Female).Length)!.Key;
        var moments = Enumerable.Range(0, PlynlingJournalCards.MomentsPerPage)
            .Select(_ => new PlynlingJournalEntry { Kind = JournalKind.Badge, Detail = longest, At = now }).ToList();
        // A partner and ten living best friends, every name at its 32-character cap.
        Plynling Other() => new() { Name = new string('*', 32), Species = PlynlingSpecies.Amanite, AdoptedAt = now, NeedsAsOf = now };
        var relations = new[] { (new PlynlingRelation { Bond = PlynlingBond.Lovers, Affinity = 100 }, Other()) }
            .Concat(Enumerable.Range(0, 10).Select(i => (new PlynlingRelation { Bond = PlynlingBond.BestFriends, Affinity = 90 - i }, Other())))
            .ToList();
        var facts = new BadgeFacts(TimeSpan.FromDays(9999), BestBodyStat: 99);
        var text = PlynlingJournalCards.JournalText(p, badges, moments, 0, now, relations, facts);
        Expect(text.Length <= 4000, $"the fullest journal is {text.Length} characters, over 4000");
        ItemEmojis.Clear();
    }
}
