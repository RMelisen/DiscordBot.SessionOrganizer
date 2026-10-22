using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /ship: how well two things go together, out of 100. Hashed from both sides (Helpers/Personality/
// Shipping), so the same pair always gets the same score, in either order; a few pairs get fixed
// verdicts. Like /rate, no [CommandContextType]: nothing here needs Context.Guild, so it works in a
// DM too (names then come from the global profile).
public partial class ShipModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ResponsePicker _picker;

    private readonly BadgeAwarder _badges;

    public ShipModule(ResponsePicker picker, BadgeAwarder badges)
    {
        _picker = picker;
        _badges = badges;
    }

    [SlashCommand("ship", "Je calcule si deux choses (ou deux personnes) vont bien ensemble")]
    public Task ShipAsync(
        [Summary("a", "Le premier : quelqu'un, un jeu, un plat… ou « @random »")]
        [MaxLength(InputCaps.Ship)]
        string a,
        [Summary("b", "Le second (vide ou « @random » : quelqu'un du serveur au hasard)")]
        [MaxLength(InputCaps.Ship)]
        string? b = null)
    {
        // No second side means « at random ».
        b = string.IsNullOrWhiteSpace(b) ? "@random" : b;
        if (Shipping.IsRandom(a) || Shipping.IsRandom(b))
        {
            if (Context.Guild is null)
                return RespondAsync("Le « @random », c'est sur un serveur. Ici, il y a que toi et moi, et c'est 0 %.", ephemeral: true);
            if (!TryDrawRandom(ref a, b, Context.User.Id, Context.Guild.Users)
                || !TryDrawRandom(ref b, a, Context.User.Id, Context.Guild.Users))
                return RespondAsync("J'ai cherché quelqu'un à tirer au sort. Il y a personne d'autre ici.", ephemeral: true);
        }

        // After the draws, with the sides as shipped; a DM is guild 0 and earns nothing.
        if (Shipping.IsSelfWithHer(a, b, Context.User.Id, Context.Client.CurrentUser.Id, AvailabilityService.OwnerId, BotResponses.TataId))
            _badges.Award(Context.Guild?.Id ?? 0, Context.User.Id, MemberBadges.Keys.Heart);

        var content = Render(a, b, Context.User.Id, Context.Client.CurrentUser.Id, _picker,
            side => NameOf(side, Context.Guild, Context.Client));
        return RespondAsync(content, allowedMentions: AllowedMentions.None);
    }

    /// <summary>
    /// /ship's whole answer for a pair: the quoted pair, the meter (computed scores only, with the
    /// couple's name) and the verdict. Static and Context-free, so her chat answer
    /// (ChatterService, « ship X et Y ») says exactly what the command would. <paramref name="nameOf"/>
    /// turns a side into the name the couple's name is cut from. Send with mentions off.
    /// </summary>
    public static string Render(string a, string b, ulong askerId, ulong selfId, ResponsePicker picker,
        Func<string, string> nameOf)
    {
        var result = Shipping.Ship(a, b, askerId, selfId, AvailabilityService.OwnerId, BotResponses.TataId);

        var pool = result.Kind switch
        {
            ShipKind.Self => BotResponses.ShipSelfLines,
            ShipKind.HerselfPapa => BotResponses.ShipHerselfPapaLines,
            ShipKind.HerselfQuokka => BotResponses.ShipHerselfQuokkaLines,
            ShipKind.HerselfHeart => BotResponses.ShipHeartLines[result.Heart!.Value],
            ShipKind.HerselfOther => BotResponses.ShipHerselfOtherLines,
            ShipKind.WallEEve => BotResponses.ShipWallEEveLines,
            ShipKind.Papa => BotResponses.ShipPapaLines,
            ShipKind.Nemesis => BotResponses.ShipNemesisLines,
            _ => BotResponses.ShipBand(result.Percent),
        };

        // The pick first, the format second. {0} is the percent; the fixed verdicts carry their
        // own score and ignore it.
        var verdict = string.Format(picker.Pick(pool), result.Percent);

        // The meter only where the score is a computed one; the fixed verdicts say theirs.
        var meter = result.Kind is ShipKind.Normal or ShipKind.Papa or ShipKind.Nemesis
            ? $"{Shipping.Bar(result.Percent)} **{result.Percent} %**"
            : null;
        if (meter is not null && result.Kind == ShipKind.Normal
            && Shipping.CoupleName(nameOf(a), nameOf(b)) is { } couple)
            meter += $" · nom de couple : **{Format.Sanitize(couple)}**";

        // Quoted like /rate's subject, so it never reads as her own words, and sent with mentions
        // off: a shipped @someone keeps its pill, unpinged.
        var subject = MessageFormat.Quote($"{a.Trim()} × {b.Trim()}", 2 * InputCaps.Ship + 3);
        return meter is null ? $"{subject}\n{verdict}" : $"{subject}\n{meter}\n{verdict}";
    }

    // Marks a side that was drawn, in the quoted pair, so nobody thinks the asker chose it.
    private const string DrawnMark = "🎲 ";

    /// <summary>
    /// When <paramref name="side"/> asks for someone at random, replaces it with a member drawn
    /// from <paramref name="members"/>: humans only, and never whoever the other side already names
    /// (« moi » or a mention), so a draw can't turn into a self-ship. False when nobody is left to draw.
    /// </summary>
    public static bool TryDrawRandom(ref string side, string other, ulong askerId, IEnumerable<IGuildUser> members)
    {
        if (!Shipping.IsRandom(side)) return true;

        var taken = Rating.Key(other, askerId).Split(' ');
        var candidates = members
            .Where(u => !u.IsBot && !taken.Contains(u.Id.ToString()))
            .ToList();
        if (candidates.Count == 0) return false;

        side = $"{DrawnMark}<@{candidates[Random.Shared.Next(candidates.Count)].Id}>";
        return true;
    }

    [GeneratedRegex(@"<@!?(\d+)>")]
    private static partial Regex MentionRegex();

    /// <summary>
    /// What the couple's name is cut from: a mention becomes the person's display name, everything
    /// else stays as typed (Shipping keeps only the letters).
    /// </summary>
    public static string NameOf(string side, SocketGuild? guild, DiscordSocketClient client) =>
        MentionRegex().Replace(side, m =>
        {
            if (!ulong.TryParse(m.Groups[1].Value, out var id)) return "";
            IUser? user = guild?.GetUser(id) ?? client.GetUser(id);
            return user is null ? "" : BotResponses.DisplayNameFor(user);
        });
}
