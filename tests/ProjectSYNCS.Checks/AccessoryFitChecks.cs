using System.Text.RegularExpressions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Models;

// 71. Wearing up to three accessories: categories, sides, the draw order, and what a pick, a try-on
// or a restyle does to the other slots (AccessoryFit, AccessoryStyles side choices).
public static class AccessoryFitChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("accessory fit: " + what);
    }

    private const string P = "cos.accessory.";

    private static WornAccessory W(string name, string? style = null) => new(P + name, style);

    private static string? Of(WornAccessory? w) => w is null ? null : w.Key[P.Length..] + (w.Style is null ? "" : ":" + w.Style);

    private static string Show(AccessoryChange c) =>
        $"[{string.Join(" | ", c.Worn.Select(Of))}] replaced={c.Replaced} moved={c.Moved}";

    private static void Is(AccessoryChange c, string? first, string? second, string? replaced, string? moved, string what) =>
        Is3(c, new[] { first, second }.OfType<string>().ToArray(), replaced, moved, what);

    // `slots`: what each worn slot should hold, in order (« ballon:rouge_gauche »), nothing after.
    private static void Is3(AccessoryChange c, string[] slots, string? replaced, string? moved, string what) =>
        Expect(c.Worn.Select(Of).SequenceEqual(slots) && c.Replaced == (replaced is null ? null : P + replaced)
               && c.Moved == (moved is null ? null : P + moved),
            $"{what}: got {Show(c)}, expected [{string.Join(" | ", slots)}] replaced={replaced} moved={moved}");

    private static IReadOnlyList<WornAccessory> L(params WornAccessory?[] items) => items.OfType<WornAccessory>().ToList();

    // The two-slot cases, written as (first, second).
    private static AccessoryChange Pick(WornAccessory? a, WornAccessory? b, int picker, WornAccessory? picked) => AccessoryFit.Pick(L(a, b), picker, picked);
    private static AccessoryChange TryOn(WornAccessory? a, WornAccessory? b, WornAccessory item) => AccessoryFit.TryOn(L(a, b), item);
    private static AccessoryChange Restyle(WornAccessory? a, WornAccessory? b, int slot, string style) => AccessoryFit.Restyle(L(a, b), slot, style);

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        Picks();
        TryOns();
        Restyles();
        ThreeSlots();
        NeverAnImpossibleOutfit();
        Resolving();
        Ranks();
        CategoriesMatchTheArt();
        SavedStylesRoundTrip();
        OutfitReadsEverySlot();
        StylesAreRemembered();
        TakingOffMovesTheSecondUp();
    }

    private static Plynling Row(string? first, string? firstStyle = null, string? second = null, string? secondStyle = null,
        string? third = null, string? thirdStyle = null) => new()
    {
        Name = "Pépin", Species = PlynlingSpecies.Amanite, Hygiene = 1.0,
        AccessoryKey = first, AccessoryStyle = firstStyle, Accessory2Key = second, Accessory2Style = secondStyle,
        Accessory3Key = third, Accessory3Style = thirdStyle,
    };

    private static void SavedStylesRoundTrip()
    {
        var saved = new Dictionary<string, string> { [P + "noeud"] = "dore_droite", [P + "echarpe"] = "verte_bas" };
        var text = SavedStyles.Format(saved);
        Expect(text is not null, "a non-empty set formats to text");
        var back = SavedStyles.Parse(text);
        Expect(back.Count == 2 && back[P + "noeud"] == "dore_droite" && back[P + "echarpe"] == "verte_bas", "SavedStyles round trip: " + text);
        Expect(SavedStyles.Format(new Dictionary<string, string>()) is null, "an empty set formats to null");
        Expect(SavedStyles.Parse(null).Count == 0 && SavedStyles.Parse("").Count == 0 && SavedStyles.Parse("garbage;=;a=").Count == 0, "bad text parses to nothing");
    }

    private static void OutfitReadsEverySlot()
    {
        var three = Outfit.Of(Row(P + "couronne", null, P + "echarpe", "verte_bas", P + "ballon", "bleu_droite"));
        Expect(three.Items.Select(i => i.Item.Key).SequenceEqual(new[] { P + "couronne", P + "echarpe", P + "ballon" }) && three.Accessory3Style == "bleu_droite",
            "three accessories read in order");
        var gaps = Outfit.Of(Row(null, null, P + "sac_a_dos", "x", P + "ballon", "bleu_droite"));
        Expect(gaps.Accessory?.Key == P + "ballon" && gaps.AccessoryStyle == "bleu_droite" && gaps.Accessory2 is null && gaps.Accessory3 is null,
            "an empty first and a retired second move the third up");
        var heading = PlynlingCardUi.Heading(Row(P + "couronne", null, P + "echarpe", null, P + "ballon"), new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero));
        var names = new[] { "couronne", "echarpe", "ballon" }.Select(n => CosmeticCatalog.ByKey(P + n)!).Select(c => $"{c.Emoji} {c.Name}").ToArray();
        Expect(heading.Contains($"Porte {names[0]}, {names[1]} et {names[2]}"), "the card line names three: " + heading);
        var drawn = PlynlingArt.LookOf(Row(P + "ballon", null, P + "couronne", null, P + "echarpe"), new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero),
            PlynlingMood.Content);
        Expect(drawn?.Items.Select(i => i.Key[P.Length..]).SequenceEqual(new[] { "echarpe", "couronne", "ballon" }) == true,
            "three accessories drawn in draw order: " + drawn?.FileName);
        Expect(Outfit.Of(Row(P + "noeud", null, P + "ballon", null, P + "couronne")).Without(CosmeticSlot.Accessory).Items.Count == 0, "Without(Accessory) clears all three");

        var onlySecond = Outfit.Of(Row(null, null, P + "noeud", "bleu_droite"));
        Expect(onlySecond.Accessory?.Key == P + "noeud" && onlySecond.AccessoryStyle == "bleu_droite" && onlySecond.Accessory2 is null,
            "a row with only the second slot puts it first");
        var retiredSecond = Outfit.Of(Row(P + "noeud", "rose_gauche", P + "sac_a_dos", "x"));
        Expect(retiredSecond.Accessory?.Key == P + "noeud" && retiredSecond.Accessory2 is null, "a retired key in slot 2 shows one accessory");
        var retiredFirst = Outfit.Of(Row(P + "sac_a_dos", "x", P + "noeud", "rose_gauche"));
        Expect(retiredFirst.Accessory?.Key == P + "noeud" && retiredFirst.Accessory2 is null && retiredFirst.AccessoryStyle == "rose_gauche",
            "a retired key in slot 1 moves the second up");
        var strayStyle = Outfit.Of(Row(P + "noeud", "rose_gauche", P + "sac_a_dos", "stray"));
        Expect(strayStyle.Accessory2 is null && strayStyle.Accessory2Style is null, "a retired second leaves no stray style: " + strayStyle.Accessory2Style);
        var strayFirst = Outfit.Of(Row(P + "sac_a_dos", "stray"));
        Expect(strayFirst.Accessory is null && strayFirst.AccessoryStyle is null && strayFirst.Accessory2Style is null, "a retired lone first leaves no stray style");
        var both = Outfit.Of(Row(P + "noeud", "rose", P + "ballon", null));
        Expect(both.Accessory?.Key == P + "noeud" && both.Accessory2?.Key == P + "ballon", "two accessories read in order");
        var two = Row(P + "noeud", "rose", P + "ballon");
        var shown = PlynlingArt.LookOf(two, new DateTimeOffset(2026, 7, 15, 12, 0, 0, TimeSpan.Zero), PlynlingMood.Content, Outfit.Of(two));
        Expect(shown?.First == new WornAccessory(P + "noeud", "rose_gauche"), "an old style without a side still draws the default: " + shown?.FileName);

        var saved = new Dictionary<string, string> { [P + "ballon"] = "bleu_droite" };
        var tried = Outfit.Of(Row(P + "noeud", "rose_gauche")).TryOn(CosmeticCatalog.ByKey(P + "ballon")!, saved);
        Expect(tried.Accessory?.Key == P + "noeud" && tried.Accessory2?.Key == P + "ballon" && tried.Accessory2Style == "bleu_droite",
            $"Outfit.TryOn fills the second slot with the saved style (resolved): {tried.Accessory2Style}");
        var theme = Outfit.Of(Row(P + "noeud")).TryOn(CosmeticCatalog.ByKey("cos.theme.prairie")!, saved);
        Expect(theme.Theme?.Key == "cos.theme.prairie" && theme.Accessory?.Key == P + "noeud", "Outfit.TryOn on another slot is With");
        Expect(Outfit.Of(Row(P + "noeud", null, P + "ballon")).Without(CosmeticSlot.Accessory) is { Accessory: null, Accessory2: null }, "Without(Accessory) clears both");
    }

    private static void StylesAreRemembered()
    {
        var p = Row(null);
        Wear(p, "noeud");
        Expect(p.AccessoryStyle == "rose_gauche", "a first nœud wears its default: " + p.AccessoryStyle);
        CosmeticSlots.Apply(p, AccessoryFit.Restyle(CosmeticSlots.Accessories(p), 0, "dore_droite"));
        Expect(p.AccessoryStyle == "dore_droite", "restyled: " + p.AccessoryStyle);
        CosmeticSlots.Apply(p, AccessoryFit.Pick(CosmeticSlots.Accessories(p), 0, null));
        Expect(p.AccessoryKey is null && p.Accessory2Key is null, "taken off");
        Wear(p, "noeud");
        Expect(p.AccessoryStyle == "dore_droite", "worn again, the nœud is « dore_droite »: " + p.AccessoryStyle);

        Wear(p, "ballon");
        Expect(p.Accessory2Key == P + "ballon" && p.Accessory2Style == "rouge_gauche", "a ballon fills the second slot: " + p.Accessory2Style);
        CosmeticSlots.Apply(p, AccessoryFit.Restyle(CosmeticSlots.Accessories(p), 1, "bleu_droite"));
        CosmeticSlots.Apply(p, AccessoryFit.Pick(CosmeticSlots.Accessories(p), 1, null));
        Wear(p, "ballon");
        Expect(p.Accessory2Style == "bleu_droite", "the ballon came back « bleu_droite »: " + p.Accessory2Style);
        Expect(SavedStyles.Parse(p.SavedAccessoryStyles).GetValueOrDefault(P + "noeud") == "dore_droite", "the nœud's style is still saved");

        // Set (the single-slot door): another accessory never inherits a style.
        var q = Row(P + "echarpe", "bleue_bas");
        CosmeticSlots.Set(q, CosmeticSlot.Accessory, P + "echarpe");
        Expect(q.AccessoryStyle == "bleue_bas", "Set: the same accessory again kept its style");
        CosmeticSlots.Set(q, CosmeticSlot.Accessory, P + "noeud");
        Expect(q.AccessoryStyle == "rose_gauche", "Set: another accessory shows its own default: " + q.AccessoryStyle);
        CosmeticSlots.Set(q, CosmeticSlot.Accessory, P + "echarpe");
        Expect(q.AccessoryStyle == "bleue_bas", "Set: the écharpe came back « bleue_bas »: " + q.AccessoryStyle);
        CosmeticSlots.Set(q, CosmeticSlot.Accessory, null);
        Expect(q.AccessoryKey is null, "Set null takes it off");
        CosmeticSlots.Set(q, CosmeticSlot.Theme, "cos.theme.prairie");
        Expect(q.ThemeKey == "cos.theme.prairie" && q.AccessoryKey is null, "Set on the thème");
    }

    private static void Wear(Plynling p, string name) =>
        CosmeticSlots.Apply(p, AccessoryFit.TryOn(CosmeticSlots.Accessories(p),
            new WornAccessory(P + name, SavedStyles.Parse(p.SavedAccessoryStyles).GetValueOrDefault(P + name))));

    private static void TakingOffMovesTheSecondUp()
    {
        var p = Row(P + "noeud", "rose_gauche", P + "ballon", "rouge_gauche");
        CosmeticSlots.TakeOff(p, P + "noeud");
        Expect(p.AccessoryKey == P + "ballon" && p.AccessoryStyle == "rouge_gauche" && p.Accessory2Key is null && p.Accessory2Style is null,
            $"the last nœud gone: the ballon moves up ({p.AccessoryKey}, {p.Accessory2Key})");
        var q = Row(P + "noeud", "rose_gauche", P + "ballon", "rouge_gauche");
        CosmeticSlots.TakeOff(q, P + "ballon");
        Expect(q.AccessoryKey == P + "noeud" && q.Accessory2Key is null, "the second taken off");
        var r = Row(P + "noeud");
        CosmeticSlots.TakeOff(r, P + "ballon");
        Expect(r.AccessoryKey == P + "noeud", "taking off what is not worn changes nothing");
        var s = Row(null, null, P + "ballon", "rouge_gauche");     // a lone second reads as the first
        CosmeticSlots.TakeOff(s, P + "ballon");
        Expect(s.AccessoryKey is null && s.Accessory2Key is null, "a lone second taken off");
        var t = Row(P + "noeud", "rose_gauche", P + "ballon", "rouge_droite", P + "couronne");
        CosmeticSlots.TakeOff(t, P + "noeud");
        Expect(t.AccessoryKey == P + "ballon" && t.AccessoryStyle == "rouge_droite" && t.Accessory2Key == P + "couronne" && t.Accessory3Key is null && t.Accessory3Style is null,
            $"the first of three taken off: the others move up ({t.AccessoryKey}, {t.Accessory2Key}, {t.Accessory3Key})");
        var u = Row(P + "noeud", "rose_gauche", P + "ballon", "rouge_droite", P + "couronne");
        CosmeticSlots.TakeOff(u, P + "couronne");
        Expect(u.AccessoryKey == P + "noeud" && u.Accessory2Key == P + "ballon" && u.Accessory3Key is null, "the third taken off");
    }

    private static void Picks()
    {
        Is(Pick(W("couronne"), W("noeud", "rose_gauche"), 1, W("bonnet", "rouge")), "bonnet:rouge", "noeud:rose_gauche", "couronne", null,
            "a hat picked in the nœud's slot replaces the couronne where it is");
        Is(Pick(W("couronne"), null, 1, W("noeud")), "couronne", "noeud:rose_gauche", null, null,
            "a nœud next to a couronne clashes with nothing");
        Is(Pick(W("ballon", "rouge_gauche"), null, 1, W("baguette", "gauche")), "ballon:rouge_droite", "baguette:gauche", null, "ballon",
            "a held item on the same side moves the other");
        Is(Pick(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), 1, W("noeud", "bleu_gauche")), "ballon:rouge_gauche", "noeud:bleu_gauche", null, null,
            "never the same item twice: the two swap");
        Is(Pick(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), 0, null), "ballon:rouge_gauche", null, null, null,
            "taking the first off moves the second up");
        Is(Pick(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), 1, null), "noeud:rose_gauche", null, null, null,
            "taking the second off");
        Is(Pick(W("noeud", "rose_gauche"), null, 0, W("fleur", "blanche_gauche")), "fleur:blanche_gauche", null, null, null,
            "the picker's own item is replaced by the pick (no clash with the empty slot)");
        Is(Pick(W("noeud", "rose_gauche"), W("fleur", "blanche_droite"), 0, W("fleur", "rose_droite")), "fleur:rose_droite", "noeud:rose_gauche", null, null,
            "the other slot holds the pick: swap, and the nœud goes to the other slot");
        Is(Pick(W("noeud"), W("couronne"), 0, W("fleur", "rose_gauche")), "fleur:rose_gauche", "couronne", null, null,
            "a pick that clashes with nothing takes its slot");
        Is(Pick(W("ballon", "rouge_gauche"), W("baguette", "droite"), 0, W("baguette", "gauche")), "baguette:gauche", "ballon:rouge_droite", null, "ballon",
            "a swap that lands two held items on one side flips the moved one");
        Is(Pick(W("noeud", "rose_droite"), W("fleur", "blanche_gauche"), 0, W("fleur", "blanche_droite")), "fleur:blanche_droite", "noeud:rose_gauche", null, "noeud",
            "a swap that lands two head items on one side flips the moved one");
        Is(Pick(W("ballon", "rouge_gauche"), W("baguette", "droite"), 0, W("baguette")), "baguette:droite", "ballon:rouge_gauche", null, null,
            "a pick naming no style keeps the worn style, swap included");
        Is(Pick(W("noeud", "bleu_droite"), null, 1, W("noeud")), "noeud:bleu_droite", null, null, null,
            "picking the worn item in the empty slot leaves it alone, in the first slot");
        var retired = new WornAccessory("cos.accessory.sac_a_dos", "x");
        Is(Pick(retired, null, 1, W("couronne")), "sac_a_dos:x", "couronne", null, null, "a retired item never clashes (Pick)");
        Is(Pick(W("ballon", "rouge_gauche"), retired, 0, W("baguette", "gauche")), "baguette:gauche", "sac_a_dos:x", null, null, "a retired item has no side (Pick)");
        var unknown = Pick(W("noeud"), null, 1, new WornAccessory("cos.accessory.nope", null));
        Is(unknown, "noeud", null, null, null, "an unknown key changes nothing (Pick)");
    }

    private static void TryOns()
    {
        Is(TryOn(W("couronne"), W("ballon", "rouge_gauche"), W("bonnet")), "bonnet:rouge", "ballon:rouge_gauche", "couronne", null,
            "a hat tried on replaces the worn hat");
        Is(TryOn(W("noeud", "rose_gauche"), null, W("ballon")), "noeud:rose_gauche", "ballon:rouge_gauche", null, null,
            "a ballon fills the second slot");
        Is3(TryOn(W("lunettes"), W("echarpe", "rouge_haut"), W("ballon")), ["lunettes", "echarpe:rouge_haut", "ballon:rouge_gauche"], null, null,
            "with two worn and no clash, the third slot fills");
        Is(TryOn(W("noeud", "rose_gauche"), null, W("fleur", "blanche_gauche")), "noeud:rose_droite", "fleur:blanche_gauche", null, "noeud",
            "a fleur tried on the nœud's side flips the nœud");
        Is(TryOn(null, null, W("baguette")), "baguette:gauche", null, null, null, "a first accessory");
        Is(TryOn(W("noeud", "rose_gauche"), W("couronne"), W("noeud", "bleu_droite")), "noeud:bleu_droite", "couronne", null, null,
            "the same item worn takes the new style");
        Is(TryOn(W("noeud"), null, new WornAccessory("cos.accessory.nope", null)), "noeud", null, null, null, "an unknown key changes nothing (TryOn)");
        var retired = new WornAccessory("cos.accessory.sac_a_dos", "x");
        Is(TryOn(retired, null, W("ballon")), "sac_a_dos:x", "ballon:rouge_gauche", null, null, "a retired item never clashes (TryOn)");
        Is(TryOn(retired, W("couronne"), W("bonnet")), "sac_a_dos:x", "bonnet:rouge", "couronne", null, "a hat still replaces the hat beside a retired item");
        Is(TryOn(W("noeud", "rose"), null, W("fleur", "blanche_gauche")), "noeud:rose_droite", "fleur:blanche_gauche", null, "noeud",
            "flipping an old style without a side gives rose_droite");
    }

    private static void Restyles()
    {
        Is(Restyle(W("noeud", "rose_gauche"), W("fleur", "blanche_droite"), 0, "rose_droite"),
            "noeud:rose_droite", "fleur:blanche_gauche", null, "fleur", "a nœud moved to the fleur's side flips the fleur");
        Is(Restyle(W("noeud", "rose_gauche"), W("fleur", "blanche_droite"), 0, "bleu_gauche"),
            "noeud:bleu_gauche", "fleur:blanche_droite", null, null, "a colour change moves nothing");
        Is(Restyle(W("noeud", "rose_gauche"), null, 1, "rose_droite"), "noeud:rose_gauche", null, null, null, "an empty slot changes nothing");
        var retired = new WornAccessory("cos.accessory.sac_a_dos", "x");
        Is(Restyle(retired, W("fleur", "blanche_droite"), 0, "gauche"), "sac_a_dos", "fleur:blanche_droite", null, null, "a retired item restyled changes nothing else");
        Is(Restyle(W("noeud", "rose_gauche"), retired, 0, "rose_droite"), "noeud:rose_droite", "sac_a_dos:x", null, null, "a retired neighbour has no side to flip");
        Is(Restyle(W("noeud", "rose_gauche"), null, 5,"rose_droite"), "noeud:rose_gauche", null, null, null, "a bad slot changes nothing");
    }

    private static void ThreeSlots()
    {
        var full = L(W("lunettes"), W("echarpe", "rouge_haut"), W("couronne"));
        Is3(AccessoryFit.TryOn(full, W("ballon")), ["lunettes", "echarpe:rouge_haut", "ballon:rouge_gauche"], "couronne", null,
            "with all three full and no clash, the last goes");
        Is3(AccessoryFit.TryOn(full, W("bonnet")), ["lunettes", "echarpe:rouge_haut", "bonnet:rouge"], "couronne", null, "a hat still replaces the hat, wherever it is");
        var hands = L(W("ballon", "rouge_gauche"), W("baguette", "droite"), W("couronne"));
        Is3(AccessoryFit.TryOn(hands, W("tulipe")), ["tulipe:gauche", "baguette:droite", "couronne"], "ballon", null,
            "both hands full: a third held item replaces the one in its hand");
        Is3(AccessoryFit.TryOn(hands, W("tulipe", "droite")), ["ballon:rouge_gauche", "tulipe:droite", "couronne"], "baguette", null,
            "both hands full, the other hand picked: that one goes");
        Is3(AccessoryFit.TryOn(L(W("noeud", "rose_gauche"), W("fleur", "blanche_droite")), W("ballon")), ["noeud:rose_gauche", "fleur:blanche_droite", "ballon:rouge_gauche"],
            null, null, "a nœud, a fleur and a ballon fit together");

        Is3(AccessoryFit.Pick(hands, 2, W("tulipe", "gauche")), ["tulipe:gauche", "baguette:droite", "couronne"], "ballon", null,
            "picked in the third slot with both hands full elsewhere: it replaces the one in its hand, the couronne stays");
        Is3(AccessoryFit.Pick(L(W("ballon", "rouge_gauche"), W("baguette", "droite")), 2, W("tulipe", "droite")), ["ballon:rouge_gauche", "tulipe:droite"], "baguette", null,
            "picked in the empty third slot with both hands full: it replaces the one in its hand");
        Is3(AccessoryFit.Pick(L(W("couronne"), W("echarpe", "rouge_haut"), W("ballon", "rouge_gauche")), 2, W("baguette", "gauche")),
            ["couronne", "echarpe:rouge_haut", "baguette:gauche"], null, null, "the third picker's own held item is replaced by the pick");
        Is3(AccessoryFit.Pick(L(W("couronne"), W("echarpe", "rouge_haut"), W("ballon", "rouge_gauche")), 2, W("bonnet")),
            ["bonnet:rouge", "echarpe:rouge_haut", "ballon:rouge_gauche"], "couronne", null, "a hat picked in the third slot replaces the couronne where it is");
        Is3(AccessoryFit.Pick(L(W("noeud", "rose_gauche"), W("couronne")), 2, W("ballon")), ["noeud:rose_gauche", "couronne", "ballon:rouge_gauche"], null, null,
            "the third picker fills the third slot");
        Is3(AccessoryFit.Pick(L(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), W("couronne")), 0, null), ["ballon:rouge_gauche", "couronne"], null, null,
            "taking the first of three off moves the others up");
        Is3(AccessoryFit.Pick(L(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), W("couronne")), 2, W("noeud")),
            ["couronne", "ballon:rouge_gauche", "noeud:rose_gauche"], null, null, "the first item picked in the third slot: the two swap");
        Is3(AccessoryFit.Pick(L(W("noeud", "rose_gauche"), W("couronne"), W("ballon", "rouge_droite")), 1, W("fleur", "blanche_gauche")),
            ["noeud:rose_droite", "fleur:blanche_gauche", "ballon:rouge_droite"], null, "noeud", "a fleur on the nœud's side flips it, three worn");
        Is3(AccessoryFit.Pick(L(W("noeud"), W("couronne")), 3, W("ballon")), ["noeud", "couronne"], null, null, "a fourth picker changes nothing");

        Is3(AccessoryFit.Restyle(L(W("noeud", "rose_gauche"), W("couronne"), W("fleur", "blanche_droite")), 0, "rose_droite"),
            ["noeud:rose_droite", "couronne", "fleur:blanche_gauche"], null, "fleur", "a restyle flips the sided item two slots away");
        Is3(AccessoryFit.Restyle(L(W("noeud", "rose_gauche"), W("couronne"), W("ballon", "rouge_gauche")), 2, "bleu_droite"),
            ["noeud:rose_gauche", "couronne", "ballon:bleu_droite"], null, null, "the third slot restyled");
    }

    // Whatever is put on, from any reachable outfit and with any picker: never more than three, never
    // the same item twice, never two of a non-sided category, at most two of a sided one and on both sides.
    private static void NeverAnImpossibleOutfit()
    {
        // Every accessory in its default style, a sided one on each side.
        var items = new List<WornAccessory>();
        foreach (var c in CosmeticCatalog.InSlot(CosmeticSlot.Accessory))
        {
            var style = AccessoryStyles.Resolve(c.Key, null);
            if (AccessoryFit.Side(c.Key, style) is null) { items.Add(new(c.Key, style)); continue; }
            var head = style!.Contains('_') ? style[..(style.LastIndexOf('_') + 1)] : "";
            items.Add(new(c.Key, head + "gauche"));
            items.Add(new(c.Key, head + "droite"));
        }
        string? Bad(IReadOnlyList<WornAccessory> worn)
        {
            if (worn.Count > AccessoryFit.MaxWorn) return "more than three";
            if (worn.Select(w => w.Key).Distinct().Count() != worn.Count) return "an item twice";
            foreach (var g in worn.GroupBy(w => AccessoryFit.Category(w.Key)).Where(g => g.Key is not null))
            {
                var sided = g.Key is AccessoryCategory.Head or AccessoryCategory.Held;
                if (!sided && g.Count() > 1) return $"two {g.Key}";
                if (sided && (g.Count() > 2 || g.Select(w => AccessoryFit.Side(w.Key, w.Style)).Distinct().Count() != g.Count())) return $"{g.Key} on one side";
            }
            return null;
        }
        var states = new List<IReadOnlyList<WornAccessory>> { L() };
        var seen = new HashSet<string>();
        int checkedCount = 0;
        string? firstBad = null;
        for (var depth = 0; depth < 3; depth++)
        {
            var next = new List<IReadOnlyList<WornAccessory>>();
            foreach (var state in states)
            foreach (var item in items)
            {
                var changes = Enumerable.Range(0, AccessoryFit.MaxWorn).Where(i => i <= state.Count).Select(i => AccessoryFit.Pick(state, i, item))
                    .Append(AccessoryFit.TryOn(state, item));
                foreach (var change in changes)
                {
                    checkedCount++;
                    if (Bad(change.Worn) is { } why) { firstBad ??= $"{why}: [{string.Join(" | ", state.Select(Of))}] + {Of(item)} → {Show(change)}"; continue; }
                    var key = string.Join("|", change.Worn.Select(Of));
                    if (seen.Add(key)) next.Add(change.Worn);
                }
            }
            states = next;
        }
        Expect(checkedCount > 10_000, $"only {checkedCount} changes checked");
        Expect(firstBad is null, "an impossible outfit: " + firstBad);
    }

    private static void Resolving()
    {
        Expect(AccessoryStyles.Resolve(P + "noeud", "rose") == "rose_gauche", "an old nœud style reads « rose_gauche »: " + AccessoryStyles.Resolve(P + "noeud", "rose"));
        Expect(AccessoryStyles.Resolve(P + "fleur", null) == "blanche_droite", "the fleur defaults to « blanche_droite »: " + AccessoryStyles.Resolve(P + "fleur", null));
        Expect(AccessoryStyles.Resolve(P + "baguette", null) == "gauche", "the baguette defaults to « gauche »: " + AccessoryStyles.Resolve(P + "baguette", null));
        Expect(AccessoryStyles.Resolve(P + "ballon", null) == "rouge_gauche", "the ballon defaults to « rouge_gauche »: " + AccessoryStyles.Resolve(P + "ballon", null));
        Expect(AccessoryFit.Side(P + "noeud", "rose") == "gauche", "an old nœud style reads as the default side");
        Expect(AccessoryFit.Side(P + "fleur", "bleue_gauche") == "gauche", "the fleur's stored side");
        Expect(AccessoryFit.Side(P + "couronne", null) is null && AccessoryFit.Side(P + "echarpe", "rouge_bas") is null, "a non-sided item has no side");
        Expect(AccessoryFit.Side(P + "nope", "gauche") is null, "an unknown item has no side");
        foreach (var key in new[] { "noeud", "fleur", "ballon", "baguette", "lanterne" })
            foreach (var choice in AccessoryStyles.Choices(P + key))
                foreach (var o in choice.Options)
                    Expect(!o.Key.Contains('_'), $"{key}: option key « {o.Key} » contains « _ »");
        var sides = AccessoryStyles.Choices(P + "noeud")[^1];
        Expect(sides.Label == "Côté" && sides.Options.Select(o => o.Name).SequenceEqual(new[] { "À gauche", "À droite" }), "nœud's « Côté » choice");
        var hand = AccessoryStyles.Choices(P + "ballon")[^1];
        Expect(hand.Label == "Main" && hand.Options.Select(o => o.Key).SequenceEqual(new[] { "gauche", "droite" }), "ballon's « Main » choice");
    }

    private static void Ranks()
    {
        var order = new[] { "echarpe", "moufles", "lunettes", "couronne", "noeud", "ballon" };
        var ranks = order.Select(n => AccessoryFit.DrawRank(P + n)).ToArray();
        Expect(ranks.SequenceEqual(new[] { 0, 1, 2, 3, 4, 5 }), "draw ranks: " + string.Join(",", ranks));
    }

    // Every catalogue accessory has a category, the same as the art tool's family says.
    private static void CategoriesMatchTheArt()
    {
        var file = Path.Combine(StoryChecks.RepoRoot(), "tools", "plynling-art", "accessories.py");
        if (!File.Exists(file)) { _fail("accessory fit: accessories.py not found at " + file); return; }
        var families = Regex.Matches(File.ReadAllText(file), "^\\s*\"(\\w+)\": dict\\(family=\"(\\w+)\"", RegexOptions.Multiline)
            .ToDictionary(m => m.Groups[1].Value, m => m.Groups[2].Value);
        var expected = new Dictionary<string, AccessoryCategory>
        {
            ["cap"] = AccessoryCategory.Head, ["hat"] = AccessoryCategory.Hat, ["neck"] = AccessoryCategory.Neck,
            ["eyes"] = AccessoryCategory.Eyes, ["hands"] = AccessoryCategory.Hands, ["held"] = AccessoryCategory.Held,
        };
        var catalogue = CosmeticCatalog.All.Where(c => c.Slot == CosmeticSlot.Accessory).ToList();
        Expect(catalogue.Count > 0, "no accessory in the catalogue");
        foreach (var acc in catalogue)
        {
            var category = AccessoryFit.Category(acc.Key);
            Expect(category is not null, acc.Key + " has no category");
            var name = acc.Key[P.Length..];
            if (!families.TryGetValue(name, out var family)) { _fail($"accessory fit: {name} is not in accessories.py"); continue; }
            Expect(expected.TryGetValue(family, out var want) && category == want, $"{name}: family « {family} » but category {category}");
        }
        Expect(AccessoryFit.Category(P + "sac_a_dos") is null && AccessoryFit.Category("nope") is null, "retired or unknown keys have a category");
    }
}
