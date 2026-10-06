using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

// A moment in a Plynling's journal. Stored as an int, so **append-only**: a kind inserted in
// the middle would silently rewrite every later entry into its neighbour.
public enum JournalKind
{
    // Visited and Hosted are no longer written (too noisy); kept so old rows still render.
    Adopted, FirstMeal, GrewUp, FirstWin, Visited, Hosted, Frozen, Thawed, FedByFriend, Badge, Resurrected, Died,
    // relationships (detail: the other Plynling's name)
    BecameFriends, BecameBestFriends, BecameLovers, BecameRivals, BecameEnemies, Heartbroken, BrokeUp, Grieving,
    // detail: the taught text
    LearnedPassion,
    // sickness (Died's detail is "illness" for an illness death)
    FellSick, Recovered,
    // detail: the trait key (Helpers/PlynlingTraits)
    TraitGained,
    // detail: the event key (Helpers/PlynlingEvents)
    EventStory,
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
        JournalKind.Died => detail == "illness"
            ? $"{g.Agree("Mort", "Morte")} de maladie."
            : $"{g.Agree("Mort", "Morte")} de faim.",
        JournalKind.FellSick => $"🤒 {g.Agree("Tombé", "Tombée")} malade.",
        JournalKind.Recovered => $"💊 {g.Agree("Guéri", "Guérie")} !",
        JournalKind.TraitGained => PlynlingTraits.ByKey(detail ?? "") is { } trait
            ? $"{trait.Emoji} Un nouveau trait : **{trait.Name(g)}**."
            : "🎭 Un nouveau trait.",
        JournalKind.EventStory => PlynlingEvents.ByKey(detail ?? "") is { } evt ? $"📜 {evt.Title}." : "📜 Une petite aventure.",
        JournalKind.BecameFriends => $"🤝 Une nouvelle amitié avec **{Other(detail)}**.",
        JournalKind.BecameBestFriends => $"💛 Meilleurs amis avec **{Other(detail)}**.",
        JournalKind.BecameLovers => $"💞 {g.Agree("Amoureux", "Amoureuse")} de **{Other(detail)}**.",
        JournalKind.BecameRivals => $"⚡ Rivalité avec **{Other(detail)}**.",
        JournalKind.BecameEnemies => $"😠 Brouille avec **{Other(detail)}**.",
        JournalKind.Heartbroken => $"💔 Un chagrin d'amour avec **{Other(detail)}**.",
        JournalKind.BrokeUp => $"💔 Rupture avec **{Other(detail)}**.",
        JournalKind.Grieving => $"🕯️ Pleure **{Other(detail)}**.",
        JournalKind.LearnedPassion => $"💭 S'est {g.Agree("pris", "prise")} de passion pour « {PlynlingCardUi.SafeName(detail ?? "?")} ».",
        _ => "…",
    };

    private static string Other(string? detail) => PlynlingCardUi.SafeName(detail ?? "?");
}
