namespace ProjectSYNCS.Helpers;

/// <summary>
/// What the body says she's doing, reported each time it changes (its `doing` event). <see cref="Kind"/> is an order's
/// task kind (`follow`, `fish`…) or her own life: `routine` (<see cref="What"/>: farm, fish, ranch, home), `tagAlong`,
/// `seek` (<see cref="Player"/>: who), `forage`, `retrieve`, `danger` (<see cref="What"/>: fight, flee, hazard, dodge,
/// defend), `sleep`, `held` (<see cref="What"/>: sitting or nothing), `idle`.
/// </summary>
public sealed record McBodyDoing(string Kind, string Player, string What);

/// <summary>What she's doing, as a clause for /minecraft status (« elle pêche pour Bob ») and « tu fais quoi ? ». Pure.</summary>
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
            McOrder.Camp => $"elle monte le camp{who}",
            McOrder.Uncamp => $"elle démonte le camp{who}",
            McOrder.Free => "elle reprend sa vie",
            McOrder.Fetch => $"elle va chercher quelque chose dans un coffre{who}",
            _ => $"elle s'occupe d'une demande{who}",
        };
    }

    /// <summary>
    /// <see cref="Describe"/> in her own words, for « tu fais quoi ? » in game (« je suis Papa », « je pêche pour Bob »).
    /// </summary>
    /// <param name="toAsker">The order is the asker's own: « je te suis », « je pêche pour toi ».</param>
    public static string FirstPerson(McOrder order, string? forWho, string? place, bool toAsker = false)
    {
        var them = forWho ?? "quelqu'un";
        var who = toAsker ? " pour toi" : string.IsNullOrEmpty(forWho) ? "" : $" pour {forWho}";
        var at = string.IsNullOrEmpty(place) ? "" : $" {place}";
        return order switch
        {
            McOrder.None or McOrder.Unknown => "je ne fais rien de spécial",
            McOrder.Come => toAsker ? "je te rejoins" : $"je rejoins {them}",
            McOrder.Follow => toAsker ? "je te suis" : $"je suis {them}",
            McOrder.Guard => toAsker ? "je te protège" : $"je protège {them}",
            McOrder.Stay or McOrder.Sit => "j'attends ici",
            McOrder.GoTo => $"je vais en{(at.Length > 0 ? at : " un point précis")}{who}",
            McOrder.GoToPlace => $"je vais à{(at.Length > 0 ? at : " un endroit")}{who}",
            McOrder.Guide => $"{(toAsker ? "je te guide" : $"je guide {them}")}{(at.Length > 0 ? $" jusqu'à{at}" : "")}",
            McOrder.Patrol => $"je garde{(at.Length > 0 ? at : " le coin")}{who}",
            McOrder.Collect => $"je ramasse des ressources{who}",
            McOrder.Harvest => $"je moissonne{who}",
            McOrder.Farm => $"je m'occupe de la ferme{who}",
            McOrder.Ranch => $"je m'occupe des animaux{who}",
            McOrder.Fish => $"je pêche{who}",
            McOrder.Smelt => $"je fais cuire quelque chose{who}",
            McOrder.Sleep => "je dors",
            McOrder.Give => toAsker ? "je t'apporte quelque chose" : $"j'apporte quelque chose à {them}",
            McOrder.Store => $"je range mes affaires{who}",
            McOrder.Camp => $"je monte le camp{who}",
            McOrder.Uncamp => $"je démonte le camp{who}",
            McOrder.Free => "je reprends ma vie",
            McOrder.Fetch => $"je vais chercher un truc dans un coffre{who}",
            _ => $"je m'occupe d'une demande{who}",
        };
    }

    /// <summary>
    /// What she does on her own, from the body's last report: in her words for « tu fais quoi ? » (<paramref name="firstPerson"/>),
    /// else for /minecraft status, where her routine says so (« elle pêche (sa routine) »). Null when she does nothing,
    /// or the report is an order's (the service knows those better).
    /// </summary>
    /// <param name="who">Who she's with, as she calls them, for tagging along and walking up to someone.</param>
    /// <param name="toAsker">That someone is the asker: « je traîne avec toi ».</param>
    public static string? OwnLife(McBodyDoing? doing, string? who, bool firstPerson, bool toAsker = false)
    {
        if (doing is null) return null;
        var them = toAsker ? "toi" : who ?? "la famille";
        string Say(string mine, string hers) => firstPerson ? mine : hers;
        return doing.Kind switch
        {
            "routine" => doing.What switch
            {
                "farm" => Say("je m'occupe de la ferme", "elle s'occupe de la ferme (sa routine)"),
                "fish" => Say("je pêche un peu", "elle pêche (sa routine)"),
                "ranch" => Say("je m'occupe des animaux", "elle s'occupe des animaux (sa routine)"),
                "home" => Say("je rentre à la maison", "elle rentre à la maison (sa routine)"),
                _ => Say("je fais ma tournée", "elle fait sa tournée (sa routine)"),
            },
            "tagAlong" => Say($"je traîne avec {them}", $"elle traîne avec {who ?? "la famille"}"),
            "seek" => Say(toAsker ? "je viens te voir" : $"je vais voir {them}", $"elle va voir {who ?? "la famille"}"),
            "forage" => Say("je cherche de quoi manger", "elle cherche de quoi manger"),
            "retrieve" => Say("je vais récupérer mes affaires", "elle récupère ses affaires"),
            "danger" => doing.What switch
            {
                "fight" => Say("je me bats", "elle se bat"),
                "flee" => Say("je m'enfuis", "elle s'enfuit"),
                "hazard" => Say("j'essaie de m'éteindre", "elle essaie de s'éteindre"),
                "dodge" => Say("j'évite un creeper", "elle évite un creeper"),
                "defend" => Say("je défends quelqu'un", "elle défend quelqu'un"),
                _ => Say("je me défends", "elle se défend"),
            },
            "sleep" => Say("je dors", "elle dort"),
            "held" => doing.What == "sitting" ? Say("je suis assise", "elle est assise") : Say("j'attends ici", "elle attend sur place"),
            _ => null,
        };
    }
}
