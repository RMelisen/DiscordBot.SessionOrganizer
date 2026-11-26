using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 73. Ping-Qilin's daily outfit (MascotWardrobe): every look is real cosmetics that fit and draw, a
// season's pool fills a week, and years of days never repeat a look two days running nor wear a
// seasonal one out of its season.
public static class MascotWardrobeChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("mascot wardrobe: " + what);
    }

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        Looks();
        Pools();
        Days();
        Wear();
    }

    private static void Looks()
    {
        Expect(MascotWardrobe.All.Select(l => l.Name).Distinct().Count() == MascotWardrobe.All.Count, "two looks share a name");
        foreach (var l in MascotWardrobe.All)
        {
            var where = $"« {l.Name} »";
            foreach (var (slot, key) in new[] { (CosmeticSlot.Theme, l.Theme), (CosmeticSlot.Title, l.Title), (CosmeticSlot.Aura, l.Aura) }
                         .Concat(l.Accessories.Select(a => (CosmeticSlot.Accessory, a.Key))))
            {
                var c = CosmeticCatalog.ByKey(key);
                Expect(c is not null && c.Slot == slot, $"{where}: {key} is not a {slot} in the catalogue");
                if (c is null) continue;
                Expect(c.Season == Season.None || c.Season == l.Season, $"{where}: {key} is a {c.Season} cosmetic in a {l.Season} look");
            }
            Expect(l.Accessories.Count is > 0 and <= AccessoryFit.MaxWorn, $"{where}: {l.Accessories.Count} accessories");
            foreach (var a in l.Accessories)
                Expect(AccessoryStyles.Resolve(a.Key, a.Style) == a.Style, $"{where}: {a.Key} style « {a.Style} » resolves to « {AccessoryStyles.Resolve(a.Key, a.Style)} »");

            // Put on one by one, nothing clashes, moves or is replaced.
            var worn = new List<WornAccessory>();
            foreach (var a in l.Accessories)
            {
                var change = AccessoryFit.TryOn(worn, a);
                Expect(change.Replaced is null && change.Moved is null, $"{where}: {a.Key} replaces {change.Replaced} or moves {change.Moved}");
                worn = change.Worn.ToList();
            }
            Expect(worn.SequenceEqual(l.Accessories), $"{where}: the accessories don't come out as listed");

            // Her picture draws, on the card and the visit canvas.
            var drawn = AccessoryFit.InDrawOrder(l.Accessories);
            WornAccessory? At(int i) => i < drawn.Count ? drawn[i] : null;
            var look = new PlynlingLook("amanite", "content", false, At(0), At(1), l.Aura["cos.aura.".Length..], At(2));
            Expect(PlynlingPainter.Draw(look) is not null, $"{where}: {look.FileName} doesn't draw");
            Expect(PlynlingPainter.Draw(look with { Visit = true }) is not null, $"{where}: its visit picture doesn't draw");
        }
    }

    private static void Pools()
    {
        foreach (var season in new[] { Season.Spring, Season.Summer, Season.Autumn, Season.Winter })
        {
            Expect(MascotWardrobe.All.Count(l => l.Season == season) == 1, $"{season} has {MascotWardrobe.All.Count(l => l.Season == season)} looks, expected one");
            Expect(MascotWardrobe.Pool(season).Count >= MascotWardrobe.DeckDays,
                $"{season}'s pool holds {MascotWardrobe.Pool(season).Count} looks, fewer than a week's {MascotWardrobe.DeckDays}");
        }
    }

    // Ten years of mornings from today's.
    private static void Days()
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        MascotWardrobe.Look? previous = null;
        var seen = new HashSet<string>();
        for (var i = 0; i < 3653; i++, day = day.AddDays(1))
        {
            var key = day.Year * 10000 + day.Month * 100 + day.Day;
            var look = MascotWardrobe.For(key);
            var season = ItemCatalog.SeasonAt(new DateTimeOffset(day.ToDateTime(new TimeOnly(12, 0)), TimeSpan.Zero));
            Expect(look.Season == Season.None || look.Season == season, $"{key}: « {look.Name} » out of season ({season})");
            Expect(look != previous, $"{key}: « {look.Name} » two days running");
            Expect(MascotWardrobe.For(key) == look, $"{key}: two reads disagree");
            previous = look;
            seen.Add(look.Name);
        }
        Expect(seen.SetEquals(MascotWardrobe.All.Select(l => l.Name)), $"never worn in ten years: {string.Join(", ", MascotWardrobe.All.Select(l => l.Name).Except(seen))}");

        // Every week wears each of its pool's looks once (a week inside one season).
        var monday = new DateOnly(2027, 1, 4);
        var week = Enumerable.Range(0, 7).Select(d => monday.AddDays(d)).Select(d => MascotWardrobe.For(d.Year * 10000 + d.Month * 100 + d.Day).Name).ToList();
        Expect(week.Distinct().Count() == 7, $"the week of {monday} repeats a look: {string.Join(", ", week)}");
    }

    // Wear writes the look and is a no-op the second time.
    private static void Wear()
    {
        var p = new Plynling { ThemeKey = "cos.theme.prairie", AccessoryKey = "cos.accessory.cafe", AccessoryStyle = "droite" };
        var look = MascotWardrobe.All.First(l => l.Accessories.Count == 3);
        Expect(MascotWardrobe.Wear(p, look), "dressing a row in another look reports no change");
        Expect(Outfit.Of(p).Theme?.Key == look.Theme && Outfit.Of(p).Title?.Key == look.Title && Outfit.Of(p).Aura?.Key == look.Aura,
            "the dressed row's thème, titre or aura isn't the look's");
        Expect(CosmeticSlots.Accessories(p).SequenceEqual(look.Accessories), "the dressed row's accessories aren't the look's");
        Expect(!MascotWardrobe.Wear(p, look), "dressing a row already in the look reports a change");
        var fewer = MascotWardrobe.All.First(l => l.Accessories.Count == 1);
        MascotWardrobe.Wear(p, fewer);
        Expect(p.Accessory2Key is null && p.Accessory3Key is null, "a one-accessory look leaves the old second and third on");
    }
}
