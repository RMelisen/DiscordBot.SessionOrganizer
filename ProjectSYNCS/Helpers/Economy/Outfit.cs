using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a Plynling wears, slot by slot, as a value: the card reads it, and the cosmetics screen's
/// try-on changes a copy of it (<see cref="TryOn"/>) so nothing is ever written to the tracked
/// Plynling to show a preview. The accessory slot holds up to <see cref="AccessoryFit.MaxWorn"/>
/// items: <see cref="Accessory"/>, <see cref="Accessory2"/>, <see cref="Accessory3"/>, each with its
/// style (<see cref="AccessoryStyles"/>, null for its default), never one under an empty earlier one;
/// read them through <see cref="Accessories"/>. The aura is worn with the accessories: changing one
/// never touches the other.
/// </summary>
public sealed record Outfit(CosmeticInfo? Theme, CosmeticInfo? Title, CosmeticInfo? Accessory, CosmeticInfo? Grave,
    CosmeticInfo? Aura = null, string? AccessoryStyle = null, CosmeticInfo? Accessory2 = null, string? Accessory2Style = null,
    CosmeticInfo? Accessory3 = null, string? Accessory3Style = null)
{
    // A key the catalogue no longer knows is dropped, with its style, the later ones moving up.
    public static Outfit Of(Plynling p) =>
        new Outfit(
            CosmeticSlots.Worn(p, CosmeticSlot.Theme),
            CosmeticSlots.Worn(p, CosmeticSlot.Title),
            null,
            CosmeticSlots.Worn(p, CosmeticSlot.Grave),
            CosmeticSlots.Worn(p, CosmeticSlot.Aura))
        .WithAccessories(CosmeticSlots.Accessories(p));

    /// <summary>The worn accessories with their styles, in slot order (empties skipped).</summary>
    public IReadOnlyList<(CosmeticInfo Item, string? Style)> Items =>
        new[] { (Item: Accessory, Style: AccessoryStyle), (Item: Accessory2, Style: Accessory2Style), (Item: Accessory3, Style: Accessory3Style) }
            .Where(a => a.Item is not null)
            .Select(a => (a.Item!, a.Style))
            .ToList();

    /// <summary>The worn accessories as <see cref="WornAccessory"/>, in slot order — what <see cref="AccessoryFit"/> takes.</summary>
    public IReadOnlyList<WornAccessory> Accessories => Items.Select(a => new WornAccessory(a.Item.Key, a.Style)).ToList();

    /// <summary>
    /// The outfit wearing exactly <paramref name="worn"/> in its accessory slots, in order; a key the
    /// catalogue doesn't know is dropped with its style, the later ones moving up.
    /// </summary>
    public Outfit WithAccessories(IEnumerable<WornAccessory> worn)
    {
        var known = worn.Select(w => (Item: CosmeticCatalog.ByKey(w.Key), w.Style)).Where(w => w.Item is not null).Take(AccessoryFit.MaxWorn).ToList();
        (CosmeticInfo? Item, string? Style) At(int i) => i < known.Count ? known[i] : (null, null);
        return this with
        {
            Accessory = At(0).Item, AccessoryStyle = At(0).Style,
            Accessory2 = At(1).Item, Accessory2Style = At(1).Style,
            Accessory3 = At(2).Item, Accessory3Style = At(2).Style,
        };
    }

    /// <summary>
    /// The outfit with <paramref name="c"/> tried on: an accessory as it would be worn (AccessoryFit.TryOn,
    /// in the style the Plynling remembers for it), any other slot as <see cref="With"/>.
    /// </summary>
    public Outfit TryOn(CosmeticInfo c, IReadOnlyDictionary<string, string> saved) =>
        c.Slot != CosmeticSlot.Accessory ? With(c) : WithAccessories(AccessoryChangeFor(c, saved)!.Worn);

    /// <summary>What trying the accessory <paramref name="c"/> on would do to the accessory slots; null for any other slot.</summary>
    public AccessoryChange? AccessoryChangeFor(CosmeticInfo c, IReadOnlyDictionary<string, string> saved)
    {
        if (c.Slot != CosmeticSlot.Accessory) return null;
        var worn = Accessories;
        var style = worn.FirstOrDefault(w => w.Key == c.Key) is { } same ? same.Style : saved.GetValueOrDefault(c.Key);
        return AccessoryFit.TryOn(worn, new WornAccessory(c.Key, style));
    }

    /// <summary>Whether <paramref name="c"/> is worn, in any accessory slot for an accessory.</summary>
    public bool Wears(CosmeticInfo c) =>
        c.Slot == CosmeticSlot.Accessory ? Items.Any(a => a.Item.Key == c.Key) : In(c.Slot)?.Key == c.Key;

    public CosmeticInfo? In(CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => Theme,
        CosmeticSlot.Title => Title,
        CosmeticSlot.Accessory => Accessory,
        CosmeticSlot.Aura => Aura,
        _ => Grave,
    };

    /// <summary>
    /// Puts <paramref name="c"/> in its slot. On the accessory slot it only replaces the first
    /// accessory (the old single-slot meaning); real wearing, with several slots, goes through
    /// <see cref="TryOn"/> / <see cref="AccessoryFit"/>.
    /// </summary>
    public Outfit With(CosmeticInfo c) => c.Slot switch
    {
        CosmeticSlot.Theme => this with { Theme = c },
        CosmeticSlot.Title => this with { Title = c },
        CosmeticSlot.Accessory => this with { Accessory = c, AccessoryStyle = c.Key == Accessory?.Key ? AccessoryStyle : null },
        CosmeticSlot.Aura => this with { Aura = c },
        _ => this with { Grave = c },
    };

    public Outfit Without(CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => this with { Theme = null },
        CosmeticSlot.Title => this with { Title = null },
        CosmeticSlot.Accessory => WithAccessories([]),
        CosmeticSlot.Aura => this with { Aura = null },
        _ => this with { Grave = null },
    };
}
