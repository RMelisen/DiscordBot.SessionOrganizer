using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// What a Plynling wears, slot by slot, as a value: the card reads it, and the cosmetics screen's
/// try-on changes a copy of it (<see cref="With"/>) so nothing is ever written to the tracked
/// Plynling to show a preview.
/// </summary>
public sealed record Outfit(CosmeticInfo? Theme, CosmeticInfo? Title, CosmeticInfo? Accessory, CosmeticInfo? Grave)
{
    public static Outfit Of(Plynling p) => new(
        CosmeticSlots.Worn(p, CosmeticSlot.Theme),
        CosmeticSlots.Worn(p, CosmeticSlot.Title),
        CosmeticSlots.Worn(p, CosmeticSlot.Accessory),
        CosmeticSlots.Worn(p, CosmeticSlot.Grave));

    public CosmeticInfo? In(CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => Theme,
        CosmeticSlot.Title => Title,
        CosmeticSlot.Accessory => Accessory,
        _ => Grave,
    };

    public Outfit With(CosmeticInfo c) => c.Slot switch
    {
        CosmeticSlot.Theme => this with { Theme = c },
        CosmeticSlot.Title => this with { Title = c },
        CosmeticSlot.Accessory => this with { Accessory = c },
        _ => this with { Grave = c },
    };

    public Outfit Without(CosmeticSlot slot) => slot switch
    {
        CosmeticSlot.Theme => this with { Theme = null },
        CosmeticSlot.Title => this with { Title = null },
        CosmeticSlot.Accessory => this with { Accessory = null },
        _ => this with { Grave = null },
    };
}
