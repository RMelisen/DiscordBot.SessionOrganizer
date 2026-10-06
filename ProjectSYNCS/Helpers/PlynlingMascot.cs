using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// The bot's own Plynling: Ping-Qilin, a girl Amanite whose passion is naps, one per guild the bot
// is in. It is an ordinary row whose owner is the bot's own user id — so view, pet, feed, list and
// the journal all work on it unchanged — and the few places where "owned by nobody who can press a
// button" matters ask <see cref="Is"/>.
//
// The owner id is *bound at runtime* (PlynlingMascotService, on Ready), never hardcoded: the dev
// and the production bot are different applications with different ids, and each must own its own.
// Until it is bound the id is 0, which matches no real row.
public static class PlynlingMascot
{
    public const string Name = "Ping-Qilin";
    public const PlynlingSpecies Species = PlynlingSpecies.Amanite;
    public const PlynlingGender Gender = PlynlingGender.Female;
    public const PlynlingPassion Passion = PlynlingPassion.Naps;

    // Chosen, not drawn, so she is the same character on every guild and on dev: Adorable,
    // Vaniteuse, Méfiante, Moqueuse. PlynlingService.EnsureTraitsAsync gives her rows these four.
    public static readonly IReadOnlyList<string> TraitKeys = new[] { "charming", "arrogant", "paranoid", "sadistic" };

    // Her die for every stat (PlynlingStats.Base), instead of a roll hashed from a row id that
    // differs per guild.
    public const int BaseStat = 4;

    private static ulong _ownerId;

    public static ulong OwnerId => Volatile.Read(ref _ownerId);

    public static void Bind(ulong botUserId) => Volatile.Write(ref _ownerId, botUserId);

    public static bool Is(Plynling p) => OwnerId != 0 && p.OwnerId == OwnerId;
}
