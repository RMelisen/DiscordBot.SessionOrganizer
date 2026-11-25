namespace ProjectSYNCS.Helpers;

/// <summary>Where on the body an accessory sits. Not stored. Two items of the same category clash, except the sided ones.</summary>
public enum AccessoryCategory { Hat, Head, Eyes, Neck, Hands, Held }

/// <summary>The worn accessories after a change, in slot order with no gap, and what the change displaced.</summary>
/// <param name="Worn">The slots that hold something, first first (at most <see cref="AccessoryFit.MaxWorn"/>).</param>
/// <param name="Replaced">The key taken off by a clash, else null.</param>
/// <param name="Moved">The key whose side flipped to make room, else null.</param>
public sealed record AccessoryChange(IReadOnlyList<WornAccessory> Worn, string? Replaced, string? Moved)
{
    /// <summary>The item in slot <paramref name="slot"/>, null past the last.</summary>
    public WornAccessory? At(int slot) => slot >= 0 && slot < Worn.Count ? Worn[slot] : null;
}

/// <summary>
/// The rules for wearing up to <see cref="MaxWorn"/> accessories: categories (a clash is the same
/// non-sided category), the sides of the sided ones (<see cref="AccessoryCategory.Head"/>,
/// <see cref="AccessoryCategory.Held"/>: at most two of a category, one per side), and the order they
/// are drawn in. Pure: the callers store the result. Must match the families in
/// tools/plynling-art/accessories.py (section 71 compares them).
/// </summary>
public static class AccessoryFit
{
    private const string Left = "gauche", Right = "droite";

    /// <summary>How many accessories a Plynling wears at most: one column pair per slot on Plynling.</summary>
    public const int MaxWorn = 3;

    /// <summary>The category of an accessory (a cosmetic key); null for an unknown or retired one.</summary>
    public static AccessoryCategory? Category(string? key) => key switch
    {
        "cos.accessory.casquette" or "cos.accessory.haut_de_forme" or "cos.accessory.couronne"
            or "cos.accessory.chapeau_paille" or "cos.accessory.bonnet" or "cos.accessory.couronne_fleurs"
            or "cos.accessory.sugegasa" or "cos.accessory.chapeau_chinois"
            or "cos.accessory.bonnet_nuit" or "cos.accessory.chapeau_champignon" or "cos.accessory.aureole" => AccessoryCategory.Hat,
        "cos.accessory.noeud" or "cos.accessory.fleur" or "cos.accessory.kanzashi" => AccessoryCategory.Head,
        "cos.accessory.lunettes" or "cos.accessory.lunettes_rondes" or "cos.accessory.monocle"
            or "cos.accessory.lunettes_coeur" or "cos.accessory.cache_oeil" => AccessoryCategory.Eyes,
        "cos.accessory.echarpe" or "cos.accessory.collier_coquillages" or "cos.accessory.medaillon"
            or "cos.accessory.noeud_papillon" => AccessoryCategory.Neck,
        "cos.accessory.moufles" => AccessoryCategory.Hands,
        "cos.accessory.ballon" or "cos.accessory.tulipe" or "cos.accessory.glace"
            or "cos.accessory.plume_doree" or "cos.accessory.bocal" or "cos.accessory.canne_peche"
            or "cos.accessory.lanterne" or "cos.accessory.cafe" or "cos.accessory.canne" or "cos.accessory.montre_gousset"
            or "cos.accessory.onigiri" or "cos.accessory.eventail" or "cos.accessory.katana"
            or "cos.accessory.sucette" or "cos.accessory.doudou" => AccessoryCategory.Held,
        _ => null,
    };

    private static bool IsSided(AccessoryCategory? c) => c is AccessoryCategory.Head or AccessoryCategory.Held;

    /// <summary>
    /// « gauche » or « droite » for a sided accessory (the style read through
    /// <see cref="AccessoryStyles.Resolve"/>, so an old style without a side reads as the default); else null.
    /// </summary>
    public static string? Side(string key, string? style)
    {
        if (!IsSided(Category(key))) return null;
        return AccessoryStyles.Resolve(key, style)?.Split('_')[^1];
    }

    /// <summary>
    /// Worn items in the one canonical order: by <see cref="DrawRank"/>, then by key, so two items of
    /// the same rank (a nœud and a fleur, two held ones) come out the same whichever slot holds which.
    /// </summary>
    public static List<WornAccessory> InDrawOrder(IEnumerable<WornAccessory> items) =>
        items.OrderBy(i => DrawRank(i.Key)).ThenBy(i => i.Key, StringComparer.Ordinal).ToList();

    /// <summary>The order pictures are drawn in, lowest first (under).</summary>
    public static int DrawRank(string key) => Category(key) switch
    {
        AccessoryCategory.Neck => 0,
        AccessoryCategory.Hands => 1,
        AccessoryCategory.Eyes => 2,
        AccessoryCategory.Hat => 3,
        AccessoryCategory.Head => 4,
        AccessoryCategory.Held => 5,
        _ => 6,
    };

    /// <summary>
    /// The accessory picked in one picker (<paramref name="picker"/> 0 to <see cref="MaxWorn"/> − 1; a
    /// null <paramref name="picked"/> takes that slot off, the later ones moving up). A clash with
    /// another slot replaces that item where it is and leaves the picker's own on; a sided item whose
    /// two sides are already taken in other slots replaces the one on its side, where it is; otherwise
    /// the pick takes its picker's slot. Never the same item twice (the two swap); the same side as
    /// another sided item of its category moves that one. An unknown key changes nothing.
    /// </summary>
    public static AccessoryChange Pick(IReadOnlyList<WornAccessory> worn, int picker, WornAccessory? picked)
    {
        var slots = Slots(worn);
        if (picker is < 0 or >= MaxWorn) return Unchanged(slots);
        if (picked is null)
        {
            slots[picker] = null;
            return Done(slots, null, null);
        }
        var category = Category(picked.Key);
        if (category is null) return Unchanged(slots);
        // An item already worn keeps its worn style when the pick names none.
        if (picked.Style is null && slots.FirstOrDefault(s => s?.Key == picked.Key) is { } same) picked = picked with { Style = same.Style };
        picked = Resolved(picked);

        var elsewhere = Find(slots, s => s.Key == picked.Key, except: picker);
        if (elsewhere >= 0)
        {
            slots[elsewhere] = slots[picker];
            slots[picker] = picked;
            return Done(slots, null, FlipIfSameSide(slots, picker));
        }
        var taken = Taken(slots, picked, except: picker);
        if (taken >= 0)
        {
            var replaced = slots[taken]!.Key;
            slots[taken] = picked;
            return Done(slots, replaced, FlipIfSameSide(slots, taken));
        }
        slots[picker] = picked;
        return Done(slots, null, FlipIfSameSide(slots, picker));
    }

    /// <summary>
    /// Puts an accessory on: it replaces a clashing worn item (or, sided with both sides taken, the
    /// one on its side), else fills the first empty slot, else replaces the last. The same item already
    /// worn just takes the new style. An unknown key changes nothing.
    /// </summary>
    public static AccessoryChange TryOn(IReadOnlyList<WornAccessory> worn, WornAccessory item)
    {
        var slots = Slots(worn);
        if (Category(item.Key) is null) return Unchanged(slots);
        item = Resolved(item);
        string? replaced = null;
        var at = Find(slots, s => s.Key == item.Key);
        if (at < 0 && (at = Taken(slots, item)) >= 0) replaced = slots[at]!.Key;
        if (at < 0) at = Array.IndexOf(slots, null);
        if (at < 0)
        {
            at = MaxWorn - 1;
            replaced = slots[at]!.Key;
        }
        slots[at] = item;
        return Done(slots, replaced, FlipIfSameSide(slots, at));
    }

    /// <summary>
    /// Sets the style of the item in <paramref name="slot"/>; a side another sided item of the same
    /// category already has makes that one flip. An empty or unknown slot changes nothing.
    /// </summary>
    public static AccessoryChange Restyle(IReadOnlyList<WornAccessory> worn, int slot, string style)
    {
        var slots = Slots(worn);
        if (slot is < 0 or >= MaxWorn || slots[slot] is null) return Unchanged(slots);
        slots[slot] = slots[slot]! with { Style = AccessoryStyles.Resolve(slots[slot]!.Key, style) };
        return Done(slots, null, FlipIfSameSide(slots, slot));
    }

    private static WornAccessory Resolved(WornAccessory w) => w with { Style = AccessoryStyles.Resolve(w.Key, w.Style) };

    // The worn items in fixed slots, gaps closed, padded with empties.
    private static WornAccessory?[] Slots(IReadOnlyList<WornAccessory?> worn)
    {
        var slots = new WornAccessory?[MaxWorn];
        var i = 0;
        foreach (var item in worn)
            if (item is not null && i < MaxWorn) slots[i++] = item;
        return slots;
    }

    private static AccessoryChange Unchanged(WornAccessory?[] slots) => Done(slots, null, null);

    // No slot stays empty under a later one.
    private static AccessoryChange Done(WornAccessory?[] slots, string? replaced, string? moved) =>
        new(slots.OfType<WornAccessory>().ToList(), replaced, moved);

    private static int Find(WornAccessory?[] slots, Func<WornAccessory, bool> match, int except = -1)
    {
        for (var i = 0; i < slots.Length; i++)
            if (i != except && slots[i] is { } s && match(s)) return i;
        return -1;
    }

    // The slot (not `except`) that `item` must replace where it is: the same non-sided category, or,
    // sided, the one on its side when both sides are already taken. -1 when there is none.
    private static int Taken(WornAccessory?[] slots, WornAccessory item, int except = -1)
    {
        var category = Category(item.Key);
        if (!IsSided(category)) return Find(slots, s => Category(s.Key) == category, except);
        var same = Enumerable.Range(0, slots.Length).Where(i => i != except && slots[i] is { } s && Category(s.Key) == category).ToList();
        if (same.Count < 2) return -1;
        var side = Side(item.Key, item.Style);
        return same.FirstOrDefault(i => Side(slots[i]!.Key, slots[i]!.Style) == side, same[0]);
    }

    // Another slot's item flips side when it is sided in the same category and on the same side as slot `at`.
    private static string? FlipIfSameSide(WornAccessory?[] slots, int at)
    {
        var mine = slots[at];
        if (mine is null) return null;
        var category = Category(mine.Key);
        if (!IsSided(category)) return null;
        var side = Side(mine.Key, mine.Style);
        if (side is null) return null;
        var other = Find(slots, s => Category(s.Key) == category && Side(s.Key, s.Style) == side, except: at);
        if (other < 0) return null;
        var key = slots[other]!.Key;
        slots[other] = WithSide(slots[other]!, side == Left ? Right : Left);
        return key;
    }

    private static WornAccessory WithSide(WornAccessory item, string side)
    {
        var parts = (AccessoryStyles.Resolve(item.Key, item.Style) ?? "").Split('_');
        parts[^1] = side;
        return item with { Style = AccessoryStyles.Resolve(item.Key, string.Join('_', parts)) };
    }
}
