using System.Text.RegularExpressions;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

// 67. Wearing two accessories: categories, sides, the draw order, and what a pick, a try-on or a
// restyle does to the other slot (AccessoryFit, AccessoryStyles side choices).
public static class AccessoryFitChecks
{
    private static Action<string> _fail = _ => { };

    private static void Expect(bool ok, string what)
    {
        if (!ok) _fail("accessory fit: " + what);
    }

    private const string P = "cos.accessory.";

    private static WornAccessory W(string name, string? style = null) => new(P + name, style);

    private static string Show(AccessoryChange c) =>
        $"[{c.First?.Key[P.Length..]}:{c.First?.Style} | {c.Second?.Key[P.Length..]}:{c.Second?.Style}] replaced={c.Replaced} moved={c.Moved}";

    private static void Is(AccessoryChange c, string? first, string? second, string? replaced, string? moved, string what)
    {
        string? Of(WornAccessory? w) => w is null ? null : w.Key[P.Length..] + (w.Style is null ? "" : ":" + w.Style);
        Expect(Of(c.First) == first && Of(c.Second) == second && c.Replaced == (replaced is null ? null : P + replaced)
               && c.Moved == (moved is null ? null : P + moved),
            $"{what}: got {Show(c)}, expected [{first} | {second}] replaced={replaced} moved={moved}");
    }

    public static void Run(Action<string> fail)
    {
        _fail = fail;
        Picks();
        TryOns();
        Restyles();
        Resolving();
        Ranks();
        CategoriesMatchTheArt();
    }

    private static void Picks()
    {
        Is(AccessoryFit.Pick(W("couronne"), W("noeud", "rose_gauche"), 1, W("bonnet", "rouge")), "bonnet:rouge", "noeud:rose_gauche", "couronne", null,
            "a hat picked in the nœud's slot replaces the couronne where it is");
        Is(AccessoryFit.Pick(W("couronne"), null, 1, W("noeud")), "couronne", "noeud:rose_gauche", null, null,
            "a nœud next to a couronne clashes with nothing");
        Is(AccessoryFit.Pick(W("ballon", "rouge_gauche"), null, 1, W("baguette", "gauche")), "ballon:rouge_droite", "baguette:gauche", null, "ballon",
            "a held item on the same side moves the other");
        Is(AccessoryFit.Pick(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), 1, W("noeud", "bleu_gauche")), "ballon:rouge_gauche", "noeud:bleu_gauche", null, null,
            "never the same item twice: the two swap");
        Is(AccessoryFit.Pick(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), 0, null), "ballon:rouge_gauche", null, null, null,
            "taking the first off moves the second up");
        Is(AccessoryFit.Pick(W("noeud", "rose_gauche"), W("ballon", "rouge_gauche"), 1, null), "noeud:rose_gauche", null, null, null,
            "taking the second off");
        Is(AccessoryFit.Pick(W("noeud", "rose_gauche"), null, 0, W("fleur", "blanche_gauche")), "fleur:blanche_gauche", null, null, null,
            "the picker's own item is replaced by the pick (no clash with the empty slot)");
        Is(AccessoryFit.Pick(W("noeud", "rose_gauche"), W("fleur", "blanche_droite"), 0, W("fleur", "rose_droite")), "fleur:rose_droite", "noeud:rose_gauche", null, null,
            "the other slot holds the pick: swap, and the nœud goes to the other slot");
        Is(AccessoryFit.Pick(W("noeud"), W("couronne"), 0, W("fleur", "rose_gauche")), "fleur:rose_gauche", "couronne", null, null,
            "a pick that clashes with nothing takes its slot");
        Is(AccessoryFit.Pick(W("ballon", "rouge_gauche"), W("baguette", "droite"), 0, W("baguette", "gauche")), "baguette:gauche", "ballon:rouge_droite", null, "ballon",
            "a swap that lands two held items on one side flips the moved one");
        Is(AccessoryFit.Pick(W("noeud", "rose_droite"), W("fleur", "blanche_gauche"), 0, W("fleur", "blanche_droite")), "fleur:blanche_droite", "noeud:rose_gauche", null, "noeud",
            "a swap that lands two head items on one side flips the moved one");
        Is(AccessoryFit.Pick(W("ballon", "rouge_gauche"), W("baguette", "droite"), 0, W("baguette")), "baguette:droite", "ballon:rouge_gauche", null, null,
            "a pick naming no style keeps the worn style, swap included");
        Is(AccessoryFit.Pick(W("noeud", "bleu_droite"), null, 1, W("noeud")), "noeud:bleu_droite", null, null, null,
            "picking the worn item in the empty slot leaves it alone, in the first slot");
        var retired = new WornAccessory("cos.accessory.sac_a_dos", "x");
        Is(AccessoryFit.Pick(retired, null, 1, W("couronne")), "sac_a_dos:x", "couronne", null, null, "a retired item never clashes (Pick)");
        Is(AccessoryFit.Pick(W("ballon", "rouge_gauche"), retired, 0, W("baguette", "gauche")), "baguette:gauche", "sac_a_dos:x", null, null, "a retired item has no side (Pick)");
        var unknown = AccessoryFit.Pick(W("noeud"), null, 1, new WornAccessory("cos.accessory.nope", null));
        Is(unknown, "noeud", null, null, null, "an unknown key changes nothing (Pick)");
    }

    private static void TryOns()
    {
        Is(AccessoryFit.TryOn(W("couronne"), W("ballon", "rouge_gauche"), W("bonnet")), "bonnet:rouge", "ballon:rouge_gauche", "couronne", null,
            "a hat tried on replaces the worn hat");
        Is(AccessoryFit.TryOn(W("noeud", "rose_gauche"), null, W("ballon")), "noeud:rose_gauche", "ballon:rouge_gauche", null, null,
            "a ballon fills the second slot");
        Is(AccessoryFit.TryOn(W("lunettes"), W("echarpe", "rouge_haut"), W("ballon")), "lunettes", "ballon:rouge_gauche", "echarpe", null,
            "with both slots full and no clash, the second goes");
        Is(AccessoryFit.TryOn(W("noeud", "rose_gauche"), null, W("fleur", "blanche_gauche")), "noeud:rose_droite", "fleur:blanche_gauche", null, "noeud",
            "a fleur tried on the nœud's side flips the nœud");
        Is(AccessoryFit.TryOn(null, null, W("baguette")), "baguette:gauche", null, null, null, "a first accessory");
        Is(AccessoryFit.TryOn(W("noeud", "rose_gauche"), W("couronne"), W("noeud", "bleu_droite")), "noeud:bleu_droite", "couronne", null, null,
            "the same item worn takes the new style");
        Is(AccessoryFit.TryOn(W("noeud"), null, new WornAccessory("cos.accessory.nope", null)), "noeud", null, null, null, "an unknown key changes nothing (TryOn)");
        var retired = new WornAccessory("cos.accessory.sac_a_dos", "x");
        Is(AccessoryFit.TryOn(retired, null, W("ballon")), "sac_a_dos:x", "ballon:rouge_gauche", null, null, "a retired item never clashes (TryOn)");
        Is(AccessoryFit.TryOn(retired, W("couronne"), W("bonnet")), "sac_a_dos:x", "bonnet:rouge", "couronne", null, "a hat still replaces the hat beside a retired item");
        Is(AccessoryFit.TryOn(W("noeud", "rose"), null, W("fleur", "blanche_gauche")), "noeud:rose_droite", "fleur:blanche_gauche", null, "noeud",
            "flipping an old style without a side gives rose_droite");
    }

    private static void Restyles()
    {
        Is(AccessoryFit.Restyle(W("noeud", "rose_gauche"), W("fleur", "blanche_droite"), 0, "rose_droite"),
            "noeud:rose_droite", "fleur:blanche_gauche", null, "fleur", "a nœud moved to the fleur's side flips the fleur");
        Is(AccessoryFit.Restyle(W("noeud", "rose_gauche"), W("fleur", "blanche_droite"), 0, "bleu_gauche"),
            "noeud:bleu_gauche", "fleur:blanche_droite", null, null, "a colour change moves nothing");
        Is(AccessoryFit.Restyle(W("noeud", "rose_gauche"), null, 1, "rose_droite"), "noeud:rose_gauche", null, null, null, "an empty slot changes nothing");
        var retired = new WornAccessory("cos.accessory.sac_a_dos", "x");
        Is(AccessoryFit.Restyle(retired, W("fleur", "blanche_droite"), 0, "gauche"), "sac_a_dos", "fleur:blanche_droite", null, null, "a retired item restyled changes nothing else");
        Is(AccessoryFit.Restyle(W("noeud", "rose_gauche"), retired, 0, "rose_droite"), "noeud:rose_droite", "sac_a_dos:x", null, null, "a retired neighbour has no side to flip");
        Is(AccessoryFit.Restyle(W("noeud", "rose_gauche"), null, 5,"rose_droite"), "noeud:rose_gauche", null, null, null, "a bad slot changes nothing");
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
