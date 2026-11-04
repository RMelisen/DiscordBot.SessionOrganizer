using System.Text;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// What /ship recognised. Normal, Papa, Nemesis and WallEJealous carry a hashed percent, Pair and
// EveElsewhere a fixed one; every other kind is a fixed verdict whose lines carry their own score
// (BotResponses.Ship*Lines).
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
    // EVE with anyone but WALL-E: it takes her away from him.
    EveElsewhere,
    // WALL-E with anyone but EVE.
    WallEJealous,
    // A pair with its own verdict: ShipResult.Pair says which.
    Pair,
    // Two of her rivals (Rating.IsRival).
    Rivals,
}

// The pairs she has an opinion on (Shipping.Pairs, BotResponses.ShipCoupleLines). Not stored anywhere.
public enum ShipCouple { PizzaPineapple, RoutierMitraillette, GladosWheatley, FritesMayo, FritesKetchup, SiropHerve, SessionMaybe, MaybeNo, UpdateRestart }

/// <param name="Percent">0–100 for Normal, 5–25 for Papa, −100 to −1 for Nemesis, 100 for
/// EveElsewhere, 1–15 for WallEJealous, the table's for Pair; unused by the other fixed verdicts.</param>
public sealed record ShipResult(ShipKind Kind, int Percent, HeartCharacter? Heart = null, ShipCouple? Pair = null);

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
    private const int WallESalt = 9104;

    private const int GladosWheatleyPercent = 4;

    // A pair matches on whole sides, read as /rate reads a subject (Rating.Bare, Rating.Fold), in
    // either order: « la pizza » × « l'ananas » counts, « pizza sans ananas » × « burger » doesn't, and
    // no name here needs a stretched or double-letter spelling. GLaDOS × Wheatley goes by the heart
    // reading instead, so her names stay in MessageCues.
    private static readonly string[] MaybeNames = { "peut etre", "peutetre", "ptet", "p tet", "ptetre", "pt etre", "maybe", "vote peut etre", "votes peut etre" };

    public static readonly (ShipCouple Pair, int Percent, string[] A, string[] B)[] Pairs =
    {
        (ShipCouple.MaybeNo, 100, MaybeNames, new[] { "non", "nan", "no", "vote non" }),
        (ShipCouple.UpdateRestart, 100, new[] { "mise a jour", "mises a jour", "maj", "update", "updates" }, new[] { "redemarrage", "redemarrages", "redemarrer", "reboot", "reboots", "restart", "restarts" }),
        (ShipCouple.PizzaPineapple, -100, new[] { "pizza", "pizzas" }, new[] { "ananas" }),
        (ShipCouple.RoutierMitraillette, -100, new[] { "routier", "routiers", "routie" }, new[] { "mitraillette", "mitraillettes" }),
        (ShipCouple.FritesMayo, 100, new[] { "frites", "frite", "cornet de frites", "paquet de frites" }, new[] { "mayo", "mayonnaise", "sauce mayo", "sauce mayonnaise" }),
        (ShipCouple.FritesKetchup, 12, new[] { "frites", "frite", "cornet de frites", "paquet de frites" }, new[] { "ketchup", "catchup", "sauce ketchup" }),
        (ShipCouple.SiropHerve, 100, new[] { "sirop de liege", "sirop liege", "sirop liegeois" }, new[] { "herve", "fromage de herve", "fromage herve" }),
        (ShipCouple.SessionMaybe, 0, new[] { "session", "sessions" }, MaybeNames),
    };

    // After Pairs, in this file: a field's initializer runs in textual order.
    private static readonly (ShipCouple Pair, int Percent, HashSet<string> A, HashSet<string> B)[] FoldedPairs = Pairs
        .Select(p => (p.Pair, p.Percent, p.A.Select(Rating.Fold).ToHashSet(), p.B.Select(Rating.Fold).ToHashSet()))
        .ToArray();

    private static readonly HashSet<string> Joiners = new() { "et", "x", "avec", "and", "vs" };

    /// <summary>
    /// A known couple typed whole in the first option (« pizza ananas », « frites et mayo ») with
    /// the second left empty, which would otherwise draw someone at random. Only a couple she has a
    /// verdict on: « Tom et Jerry » stays one side, as typed. Null when the text isn't one.
    /// </summary>
    public static (string A, string B)? SplitCouple(string side, ulong askerId, ulong selfId, ulong ownerId, ulong tataId)
    {
        var words = Rating.Normalize(side).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (var i = 1; i < words.Length; i++)
        {
            var left = string.Join(' ', words[..i]);
            var right = string.Join(' ', words[(Joiners.Contains(words[i]) ? i + 1 : i)..]);
            if (right.Length == 0) continue;
            if (Ship(left, right, askerId, selfId, ownerId, tataId).Kind is ShipKind.Pair or ShipKind.WallEEve or ShipKind.Rivals)
                return (left, right);
        }
        return null;
    }

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

        // After Papa and the nemesis, whose rules cover anything at all. WALL-E × EVE was settled above.
        if ((heartA, heartB) is (HeartCharacter.Glados, HeartCharacter.Wheatley) or (HeartCharacter.Wheatley, HeartCharacter.Glados))
            return new(ShipKind.Pair, GladosWheatleyPercent, Pair: ShipCouple.GladosWheatley);
        if (heartA == HeartCharacter.Eve || heartB == HeartCharacter.Eve)
            return new(ShipKind.EveElsewhere, 100);
        if (heartA == HeartCharacter.WallE || heartB == HeartCharacter.WallE)
            return new(ShipKind.WallEJealous, 1 + (int)(StableRoll.Unit(hash, WallESalt, 0) * 15));

        var (specialA, specialB) = (Rating.Rate(a, askerId, selfId, ownerId, tataId).Special, Rating.Rate(b, askerId, selfId, ownerId, tataId).Special);
        if (Rating.IsRival(specialA) && Rating.IsRival(specialB))
            // « doodle » × « doodle.com » is one thing twice, as the keys can't tell.
            return specialA == specialB ? new(ShipKind.Self, 100) : new(ShipKind.Rivals, 100);

        var (readA, readB) = (Rating.Readings(keyA).ToArray(), Rating.Readings(keyB).ToArray());
        foreach (var (pair, percent, sideA, sideB) in FoldedPairs)
            if ((sideA.Overlaps(readA) && sideB.Overlaps(readB)) || (sideA.Overlaps(readB) && sideB.Overlaps(readA)))
                return new(ShipKind.Pair, percent, Pair: pair);

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
        return Rating.ReadHeart(side) is { } heart ? (Who.Heart, heart) : (Who.Plain, null);
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
