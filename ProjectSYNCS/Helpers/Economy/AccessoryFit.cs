namespace ProjectSYNCS.Helpers;

/// <summary>Where on the body an accessory sits. Not stored. Two items of the same category clash, except the sided ones.</summary>
public enum AccessoryCategory { Hat, Head, Eyes, Neck, Hands, Held }

/// <summary>The two worn accessories after a change, and what the change displaced.</summary>
/// <param name="First">The first slot (never empty while the second is set).</param>
/// <param name="Second">The second slot.</param>
/// <param name="Replaced">The key taken off by a clash, else null.</param>
/// <param name="Moved">The key whose side flipped to make room, else null.</param>
public sealed record AccessoryChange(WornAccessory? First, WornAccessory? Second, string? Replaced, string? Moved);

/// <summary>
/// The rules for wearing two accessories: categories (a clash is the same non-sided category), the
/// sides of the sided ones (<see cref="AccessoryCategory.Head"/>, <see cref="AccessoryCategory.Held"/>),
/// and the order they are drawn in. Pure: the callers store the result. Must match the families in
/// tools/plynling-art/accessories.py (section 67 compares them).
/// </summary>
public static class AccessoryFit
{
    private const string Left = "gauche", Right = "droite";

    /// <summary>The category of an accessory (a cosmetic key); null for an unknown or retired one.</summary>
    public static AccessoryCategory? Category(string? key) => key switch
    {
        "cos.accessory.casquette" or "cos.accessory.haut_de_forme" or "cos.accessory.couronne"
            or "cos.accessory.chapeau_paille" or "cos.accessory.bonnet" or "cos.accessory.couronne_fleurs" => AccessoryCategory.Hat,
        "cos.accessory.noeud" or "cos.accessory.fleur" => AccessoryCategory.Head,
        "cos.accessory.lunettes" => AccessoryCategory.Eyes,
        "cos.accessory.echarpe" or "cos.accessory.collier_coquillages" or "cos.accessory.medaillon" => AccessoryCategory.Neck,
        "cos.accessory.moufles" => AccessoryCategory.Hands,
        "cos.accessory.ballon" or "cos.accessory.baguette" or "cos.accessory.tulipe" or "cos.accessory.glace"
            or "cos.accessory.plume_doree" or "cos.accessory.bocal" or "cos.accessory.canne_peche"
            or "cos.accessory.lanterne" => AccessoryCategory.Held,
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
    /// The accessory picked in one picker (<paramref name="picker"/> 0 or 1; a null
    /// <paramref name="picked"/> takes that slot off). A clash with the other slot replaces that item
    /// where it is and leaves the picker's own on; otherwise the pick takes its picker's slot; never
    /// the same item twice (the two swap); the same side as the other sided item moves the other one.
    /// An unknown key changes nothing.
    /// </summary>
    public static AccessoryChange Pick(WornAccessory? first, WornAccessory? second, int picker, WornAccessory? picked)
    {
        var slots = Slots(first, second);
        if (picker is not (0 or 1)) return new AccessoryChange(first, second, null, null);
        if (picked is null)
        {
            slots[picker] = null;
            return Done(slots, null, null);
        }
        var category = Category(picked.Key);
        if (category is null) return new AccessoryChange(first, second, null, null);
        picked = Resolved(picked);
        var other = slots[1 - picker];

        if (other is not null && other.Key == picked.Key)
        {
            slots[1 - picker] = slots[picker];
            slots[picker] = picked;
            return Done(slots, null, null);
        }
        if (other is not null && !IsSided(category) && Category(other.Key) == category)
        {
            slots[1 - picker] = picked;
            return Done(slots, other.Key, null);
        }
        slots[picker] = picked;
        return Done(slots, null, FlipIfSameSide(slots, picker));
    }

    /// <summary>
    /// Puts an accessory on: it replaces a clashing worn item, else fills the empty slot, else
    /// replaces the second. The same item already worn just takes the new style. An unknown key
    /// changes nothing.
    /// </summary>
    public static AccessoryChange TryOn(WornAccessory? first, WornAccessory? second, WornAccessory item)
    {
        var category = Category(item.Key);
        if (category is null) return new AccessoryChange(first, second, null, null);
        item = Resolved(item);
        var slots = Slots(first, second);
        string? replaced = null;
        int at;
        if (slots[0]?.Key == item.Key) at = 0;
        else if (slots[1]?.Key == item.Key) at = 1;
        else if (!IsSided(category) && slots[0] is { } a && Category(a.Key) == category) { at = 0; replaced = a.Key; }
        else if (!IsSided(category) && slots[1] is { } b && Category(b.Key) == category) { at = 1; replaced = b.Key; }
        else if (slots[0] is null) at = 0;
        else if (slots[1] is null) at = 1;
        else { at = 1; replaced = slots[1]!.Key; }
        slots[at] = item;
        return Done(slots, replaced, FlipIfSameSide(slots, at));
    }

    /// <summary>
    /// Sets the style of the item in <paramref name="slot"/>; a side the other sided item of the same
    /// category already has makes that one flip. An empty or unknown slot changes nothing.
    /// </summary>
    public static AccessoryChange Restyle(WornAccessory? first, WornAccessory? second, int slot, string style)
    {
        if (slot is not (0 or 1) || (slot == 0 ? first : second) is null) return new AccessoryChange(first, second, null, null);
        var slots = Slots(first, second);
        var at = first is null ? 0 : slot;     // a lone second reads as the first
        slots[at] = slots[at]! with { Style = AccessoryStyles.Resolve(slots[at]!.Key, style) };
        return Done(slots, null, FlipIfSameSide(slots, at));
    }

    private static WornAccessory Resolved(WornAccessory w) => w with { Style = AccessoryStyles.Resolve(w.Key, w.Style) };

    private static WornAccessory?[] Slots(WornAccessory? first, WornAccessory? second) =>
        first is null ? new[] { second, (WornAccessory?)null } : new[] { first, second };

    // The first slot never stays empty under a second.
    private static AccessoryChange Done(WornAccessory?[] slots, string? replaced, string? moved) =>
        slots[0] is null ? new AccessoryChange(slots[1], null, replaced, moved) : new AccessoryChange(slots[0], slots[1], replaced, moved);

    // The other slot's item flips side when it is sided in the same category and on the same side as slot `at`.
    private static string? FlipIfSameSide(WornAccessory?[] slots, int at)
    {
        var mine = slots[at];
        var other = slots[1 - at];
        if (mine is null || other is null) return null;
        var category = Category(mine.Key);
        if (!IsSided(category) || Category(other.Key) != category) return null;
        var side = Side(mine.Key, mine.Style);
        if (side is null || side != Side(other.Key, other.Style)) return null;
        slots[1 - at] = WithSide(other, side == Left ? Right : Left);
        return other.Key;
    }

    private static WornAccessory WithSide(WornAccessory item, string side)
    {
        var parts = (AccessoryStyles.Resolve(item.Key, item.Style) ?? "").Split('_');
        parts[^1] = side;
        return item with { Style = AccessoryStyles.Resolve(item.Key, string.Join('_', parts)) };
    }
}
