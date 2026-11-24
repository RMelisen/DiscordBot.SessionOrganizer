using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a Plynling wears, slot by slot, as a value: the card reads it, and the cosmetics screen's
/// try-on changes a copy of it (<see cref="With"/>) so nothing is ever written to the tracked
/// Plynling to show a preview. <see cref="AccessoryStyle"/> is the worn accessory's style
/// (<see cref="AccessoryStyles"/>), null for its default; it goes with that accessory only. The
/// aura is worn with the accessory: changing one never touches the other.
/// </summary>
public sealed record Outfit(CosmeticInfo? Theme, CosmeticInfo? Title, CosmeticInfo? Accessory, CosmeticInfo? Grave,
    CosmeticInfo? Aura = null, string? AccessoryStyle = null, CosmeticInfo? Accessory2 = null, string? Accessory2Style = null)
{
    // Accessory and AccessoryStyle are the first slot; Accessory2 and Accessory2Style the second,
    // never set under an empty first. A key the catalogue no longer knows is dropped, the other moving up.
    public static Outfit Of(Plynling p)
    {
        var (first, second) = CosmeticSlots.AccessorySlots(p);
        var one = CosmeticCatalog.ByKey(first?.Key);
        var two = CosmeticCatalog.ByKey(second?.Key);
        var style1 = one is null ? null : first?.Style;      // a retired key leaves no stray style behind
        var style2 = two is null ? null : second?.Style;
        if (one is null) (one, style1, two, style2) = (two, style2, null, null);
        return new(
            CosmeticSlots.Worn(p, CosmeticSlot.Theme),
            CosmeticSlots.Worn(p, CosmeticSlot.Title),
            one,
            CosmeticSlots.Worn(p, CosmeticSlot.Grave),
            CosmeticSlots.Worn(p, CosmeticSlot.Aura),
            style1, two, style2);
    }

    /// <summary>
    /// The outfit with <paramref name="c"/> tried on: an accessory as it would be worn (AccessoryFit.TryOn,
    /// in the style the Plynling remembers for it), any other slot as <see cref="With"/>.
    /// </summary>
    public Outfit TryOn(CosmeticInfo c, IReadOnlyDictionary<string, string> saved)
    {
        if (c.Slot != CosmeticSlot.Accessory) return With(c);
        var change = AccessoryChangeFor(c, saved)!;
        return this with
        {
            Accessory = CosmeticCatalog.ByKey(change.First?.Key), AccessoryStyle = change.First?.Style,
            Accessory2 = CosmeticCatalog.ByKey(change.Second?.Key), Accessory2Style = change.Second?.Style,
        };
    }

    /// <summary>What trying the accessory <paramref name="c"/> on would do to the two slots; null for any other slot.</summary>
    public AccessoryChange? AccessoryChangeFor(CosmeticInfo c, IReadOnlyDictionary<string, string> saved)
    {
        if (c.Slot != CosmeticSlot.Accessory) return null;
        var first = Accessory is null ? null : new WornAccessory(Accessory.Key, AccessoryStyle);
        var second = Accessory2 is null ? null : new WornAccessory(Accessory2.Key, Accessory2Style);
        var style = first?.Key == c.Key ? first.Style : second?.Key == c.Key ? second.Style : saved.GetValueOrDefault(c.Key);
        return AccessoryFit.TryOn(first, second, new WornAccessory(c.Key, style));
    }

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
    /// accessory (the old single-slot meaning); real wearing, with two slots, goes through
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
        CosmeticSlot.Accessory => this with { Accessory = null, AccessoryStyle = null, Accessory2 = null, Accessory2Style = null },
        CosmeticSlot.Aura => this with { Aura = null },
        _ => this with { Grave = null },
    };
}
