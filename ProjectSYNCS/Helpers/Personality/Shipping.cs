using System.Text;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// What /ship recognised. Normal, Papa and Nemesis carry a hashed percent; every other kind is a
// fixed verdict whose lines carry their own score (BotResponses.Ship*Lines).
public enum ShipKind
{
    Normal,
    // The same thing on both sides.
    Self,
    HerselfPapa,
    HerselfQuokka,
    // Her with one of her robot-heart characters: ShipResult.Heart says which.
    HerselfHeart,
    HerselfOther,
    WallEEve,
    Papa,
    Nemesis,
}

/// <param name="Percent">0–100 for Normal, 5–25 for Papa, −100 to −1 for Nemesis; unused by the
/// fixed verdicts.</param>
public sealed record ShipResult(ShipKind Kind, int Percent, HeartCharacter? Heart = null);

/// <summary>
/// /ship's verdict, pure. Each side is recognised the way /rate recognises a subject (her, Papa,
/// WALL-E, Quokka, « moi ») plus her robot-heart characters (MessageCues), so the names live in
/// one place. The percent hashes the two sides' keys *sorted*: « a × b » and « b × a » agree, and
/// the same pair gets the same score every time, whoever asks.
/// </summary>
public static class Shipping
{
    private const int PercentSalt = 9101;
    private const int PapaSalt = 9102;
    private const int NemesisSalt = 9103;

    // Who a side is. Herself and Owner also stand for their own identity, so « syncs × toi » is the
    // same thing twice even though the texts differ.
    private enum Who { Plain, Herself, Owner, Nemesis, Heart }

    public static ShipResult Ship(string a, string b, ulong askerId, ulong selfId, ulong ownerId, ulong tataId)
    {
        var (whoA, heartA) = Identify(a, askerId, selfId, ownerId, tataId);
        var (whoB, heartB) = Identify(b, askerId, selfId, ownerId, tataId);
        var keyA = IdentityKey(whoA, heartA, Rating.Key(a, askerId));
        var keyB = IdentityKey(whoB, heartB, Rating.Key(b, askerId));

        if (keyA == keyB) return new(ShipKind.Self, 100);

        // Her first, then everything else that involves her, so « syncs × papa » never reaches the
        // ordinary Papa verdict.
        if (whoA == Who.Herself || whoB == Who.Herself)
        {
            var (other, otherHeart) = whoA == Who.Herself ? (whoB, heartB) : (whoA, heartA);
            return other switch
            {
                Who.Owner => new(ShipKind.HerselfPapa, 0),
                Who.Nemesis => new(ShipKind.HerselfQuokka, -100),
                Who.Heart => new(ShipKind.HerselfHeart, 0, otherHeart),
                _ => new(ShipKind.HerselfOther, 0),
            };
        }

        if ((heartA, heartB) is (HeartCharacter.WallE, HeartCharacter.Eve) or (HeartCharacter.Eve, HeartCharacter.WallE))
            return new(ShipKind.WallEEve, 0);

        var hash = PairHash(keyA, keyB);
        if (whoA == Who.Owner || whoB == Who.Owner)
            return new(ShipKind.Papa, 5 + (int)(StableRoll.Unit(hash, PapaSalt, 0) * 21));
        if (whoA == Who.Nemesis || whoB == Who.Nemesis)
            return new(ShipKind.Nemesis, -1 - (int)(StableRoll.Unit(hash, NemesisSalt, 0) * 100));

        return new(ShipKind.Normal, (int)(StableRoll.Unit(hash, PercentSalt, 0) * 101));
    }

    /// <summary>
    /// One side is her and the other is the asker themselves (« moi », or their own mention): the
    /// pair that earns the heart badge. Call it with the sides after any @random draw.
    /// </summary>
    public static bool IsSelfWithHer(string a, string b, ulong askerId, ulong selfId, ulong ownerId, ulong tataId)
    {
        var herA = Rating.Rate(a, askerId, selfId, ownerId, tataId).Kind == RatingKind.Herself;
        var herB = Rating.Rate(b, askerId, selfId, ownerId, tataId).Kind == RatingKind.Herself;
        return (herA && IsAsker(b, askerId)) || (herB && IsAsker(a, askerId));
    }

    // « moi » and the asker's own mention both key as the asker's id (Rating.Key). RatingResult.AboutAsker
    // is no use here: it is only set for Papa and Tata, not for an ordinary asker.
    private static bool IsAsker(string side, ulong askerId) => Rating.Key(side, askerId) == askerId.ToString();

    private static (Who, HeartCharacter?) Identify(string side, ulong askerId, ulong selfId, ulong ownerId, ulong tataId)
    {
        switch (Rating.Rate(side, askerId, selfId, ownerId, tataId).Kind)
        {
            case RatingKind.Herself: return (Who.Herself, null);
            case RatingKind.Owner: return (Who.Owner, null);
            case RatingKind.Nemesis: return (Who.Nemesis, null);
            case RatingKind.Crush: return (Who.Heart, HeartCharacter.WallE);
        }
        // ReadHeartCharacter turns « WALL-E et EVE » into EVE; one side naming both is just EVE.
        return MessageCues.ReadHeartCharacter(side) is { } heart ? (Who.Heart, heart) : (Who.Plain, null);
    }

    private static string IdentityKey(Who who, HeartCharacter? heart, string key) => who switch
    {
        Who.Herself => "@herself",
        Who.Owner => "@owner",
        Who.Heart => $"@heart:{heart}",
        _ => key,
    };

    private static int PairHash(string a, string b) =>
        Rating.Hash(string.CompareOrdinal(a, b) <= 0 ? $"{a}\u0001{b}" : $"{b}\u0001{a}");

    /// <summary>
    /// The couple's name: the first half of <paramref name="a"/> and the second half of
    /// <paramref name="b"/>, letters and digits only, the first letter capitalised. Null when
    /// either side has nothing usable (emotes only). Callers sanitize it: it is built from user text.
    /// </summary>
    public static string? CoupleName(string a, string b)
    {
        var left = LettersOnly(a);
        var right = LettersOnly(b);
        if (left.Length == 0 || right.Length == 0) return null;

        var name = left[..((left.Length + 1) / 2)] + right[(right.Length / 2)..];
        return char.ToUpperInvariant(name[0]) + name[1..].ToLowerInvariant();
    }

    // A side that asks for someone at random: « @random », « random », « au hasard »… Matched on
    // the normalised text (no @, no accents), and only as the whole side.
    private static readonly HashSet<string> RandomWords =
        new() { "random", "hasard", "au hasard", "aleatoire", "n importe qui", "quelqu un" };

    public static bool IsRandom(string side) => RandomWords.Contains(Rating.Normalize(side));

    // Each half is cut from at most this many characters, so a long side doesn't make a long name.
    private const int NameSource = 16;

    private static string LettersOnly(string text)
    {
        var sb = new StringBuilder();
        foreach (var c in text)
            if (char.IsLetterOrDigit(c) && sb.Length < NameSource) sb.Append(c);
        return sb.ToString();
    }

    /// <summary>Ten hearts, filled to the percent; a negative percent breaks them all.</summary>
    public static string Bar(int percent)
    {
        if (percent < 0) return string.Concat(Enumerable.Repeat("💔", 10));
        var full = Math.Clamp((int)Math.Round(percent / 10.0, MidpointRounding.AwayFromZero), 0, 10);
        return string.Concat(Enumerable.Repeat("❤️", full)) + string.Concat(Enumerable.Repeat("🖤", 10 - full));
    }
}
