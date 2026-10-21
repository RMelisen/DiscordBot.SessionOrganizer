namespace ProjectSYNCS.Helpers;

/// <summary>What she's doing, as a clause for /minecraft status (« elle pêche pour Bob »). Pure.</summary>
public static class McDoing
{
    /// <summary>
    /// <see cref="Describe"/> for anyone on Discord: a whispered order keeps its place or coordinates to itself, since the
    /// player chose not to say them in the open.
    /// </summary>
    public static string ForStatus(McOrder? order, string? forWho, string? place, bool whispered) =>
        Describe(order, forWho, whispered ? null : place);

    /// <param name="order">The order under way (or just sent); null when she's on nobody's order.</param>
    /// <param name="forWho">Whose order it is, as she calls them (« Papa », « Tata », else the in-game name).</param>
    /// <param name="place">What the order is about, when it has one: a place's label, coordinates, goods.</param>
    public static string Describe(McOrder? order, string? forWho, string? place)
    {
        var who = string.IsNullOrEmpty(forWho) ? "" : $" pour {forWho}";
        var at = string.IsNullOrEmpty(place) ? "" : $" {place}";
        return order switch
        {
            null or McOrder.None or McOrder.Unknown => "elle ne fait rien de spécial",
            McOrder.Come => $"elle rejoint {forWho ?? "quelqu'un"}",
            McOrder.Follow => $"elle suit {forWho ?? "quelqu'un"}",
            McOrder.Guard => $"elle protège {forWho ?? "quelqu'un"}",
            McOrder.Stay or McOrder.Sit => "elle attend sur place",
            McOrder.GoTo => $"elle va en{(at.Length > 0 ? at : " un point précis")}{who}",
            McOrder.GoToPlace => $"elle va à{(at.Length > 0 ? at : " un endroit")}{who}",
            McOrder.Guide => $"elle guide {forWho ?? "quelqu'un"}{(at.Length > 0 ? $" jusqu'à{at}" : "")}",
            McOrder.Patrol => $"elle garde{(at.Length > 0 ? at : " le coin")}{who}",
            McOrder.Collect => $"elle récolte des ressources{who}",
            McOrder.Harvest => $"elle moissonne{who}",
            McOrder.Farm => $"elle s'occupe de la ferme{who}",
            McOrder.Ranch => $"elle s'occupe des animaux{who}",
            McOrder.Fish => $"elle pêche{who}",
            McOrder.Smelt => $"elle fait cuire quelque chose{who}",
            McOrder.Sleep => "elle dort",
            McOrder.Give => $"elle apporte quelque chose{(forWho is null ? "" : $" à {forWho}")}",
            McOrder.Store => $"elle range ses affaires{who}",
            _ => $"elle s'occupe d'une demande{who}",
        };
    }
}
