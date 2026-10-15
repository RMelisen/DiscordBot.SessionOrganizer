using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
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

    public ShipModule(ResponsePicker picker)
    {
        _picker = picker;
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
            if (!TryDrawRandom(ref a, b) || !TryDrawRandom(ref b, a))
                return RespondAsync("J'ai cherché quelqu'un à tirer au sort. Il y a personne d'autre ici.", ephemeral: true);
        }

        var result = Shipping.Ship(a, b, Context.User.Id, Context.Client.CurrentUser.Id,
            AvailabilityService.OwnerId, BotResponses.TataId);

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
        var verdict = string.Format(_picker.Pick(pool), result.Percent);

        // The meter only where the score is a computed one; the fixed verdicts say theirs.
        var meter = result.Kind is ShipKind.Normal or ShipKind.Papa or ShipKind.Nemesis
            ? $"{Shipping.Bar(result.Percent)} **{result.Percent} %**"
            : null;
        if (meter is not null && result.Kind == ShipKind.Normal
            && Shipping.CoupleName(NameOf(a), NameOf(b)) is { } couple)
            meter += $" · nom de couple : **{Format.Sanitize(couple)}**";

        // Quoted like /rate's subject, so it never reads as her own words, and sent with mentions
        // off: a shipped @someone keeps its pill, unpinged.
        var subject = MessageFormat.Quote($"{a.Trim()} × {b.Trim()}", 2 * InputCaps.Ship + 3);
        var content = meter is null ? $"{subject}\n{verdict}" : $"{subject}\n{meter}\n{verdict}";
        return RespondAsync(content, allowedMentions: AllowedMentions.None);
    }

    // Marks a side that was drawn, in the quoted pair, so nobody thinks the asker chose it.
    private const string DrawnMark = "🎲 ";

    /// <summary>
    /// When <paramref name="side"/> asks for someone at random, replaces it with a member drawn
    /// from the server: humans only, and never whoever the other side already names (« moi » or a
    /// mention), so a draw can't turn into a self-ship. False when nobody is left to draw.
    /// </summary>
    private bool TryDrawRandom(ref string side, string other)
    {
        if (!Shipping.IsRandom(side)) return true;

        var taken = Rating.Key(other, Context.User.Id).Split(' ');
        var candidates = Context.Guild.Users
            .Where(u => !u.IsBot && !taken.Contains(u.Id.ToString()))
            .ToList();
        if (candidates.Count == 0) return false;

        side = $"{DrawnMark}<@{candidates[Random.Shared.Next(candidates.Count)].Id}>";
        return true;
    }

    [GeneratedRegex(@"<@!?(\d+)>")]
    private static partial Regex MentionRegex();

    // What the couple's name is cut from: a mention becomes the person's display name, everything
    // else stays as typed (Shipping keeps only the letters).
    private string NameOf(string side) =>
        MentionRegex().Replace(side, m =>
        {
            if (!ulong.TryParse(m.Groups[1].Value, out var id)) return "";
            IUser? user = Context.Guild?.GetUser(id) ?? Context.Client.GetUser(id);
            return user is null ? "" : BotResponses.DisplayNameFor(user);
        });
}
