using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// A moment in a Plynling's journal. Stored as an int, so **append-only**: a kind inserted in
// the middle would silently rewrite every later entry into its neighbour.
public enum JournalKind
{
    Adopted, FirstMeal, GrewUp, FirstWin, Visited, Hosted, Frozen, Thawed, FedByFriend, Badge, Resurrected, Died,
}

// The wording of each moment — pure string work, gendered at display (the entry stores the
// kind and a detail, never the French). Detail: GrewUp → the PlynlingStage name; Visited and
// Hosted → the other Plynling's name; FedByFriend → the friend's user id; Badge → its key.
public static class PlynlingJournalUi
{
    public static string Line(JournalKind kind, string? detail, string name, PlynlingGender g) => kind switch
    {
        JournalKind.Adopted => $"**{name}** arrive dans sa nouvelle maison.",
        JournalKind.FirstMeal => "Premier repas. Miam !",
        JournalKind.GrewUp => Enum.TryParse<PlynlingStage>(detail, out var stage)
            ? $"**{name}** est {g.Agree("devenu", "devenue")} {PlynlingCardUi.StageLabel(stage, g)}."
            : $"**{name}** a grandi.",
        JournalKind.FirstWin => "Première partie gagnée !",
        JournalKind.Visited => $"Visite chez **{PlynlingCardUi.SafeName(detail ?? "?")}**.",
        JournalKind.Hosted => $"A reçu la visite de **{PlynlingCardUi.SafeName(detail ?? "?")}**.",
        JournalKind.Frozen => $"{g.Agree("Gelé", "Gelée")} pour un temps.",
        JournalKind.Thawed => $"{g.Agree("Dégelé", "Dégelée")}, la vie reprend.",
        JournalKind.FedByFriend => ulong.TryParse(detail, out var friend)
            ? $"Un premier repas offert par <@{friend}>."
            : "Un premier repas offert par un ami.",
        JournalKind.Badge => PlynlingBadges.ByKey(detail ?? "") is { } badge
            ? $"Badge obtenu : {badge.Emoji} {badge.Name(g)}."
            : "Badge obtenu.",
        JournalKind.Resurrected => $"{g.Agree("Revenu", "Revenue")} d'entre les morts !",
        JournalKind.Died => $"{g.Agree("Mort", "Morte")} de faim.",
        _ => "…",
    };
}
