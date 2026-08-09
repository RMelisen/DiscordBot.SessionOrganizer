using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Manual XP adjustment, for staff. Kept apart from LevelModule, which owns the
// player-facing surfaces (/level, /leaderboard) and is entirely Components V2 — these
// are two plain ephemeral replies and share nothing with it but XpService.
//
// Guild-only: every adjustment is scoped to Context.Guild.Id, which is null in a DM,
// and config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this the command is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
// Hides the commands in Discord's own picker for anyone without ManageGuild, so people
// who cannot use them never see them. This is presentation, not security: a server can
// override it under Integrations, and it says nothing about the bot's owner, so the
// IsStaff check below still decides. Belt and braces on purpose.
[DefaultMemberPermissions(GuildPermission.ManageGuild)]
public class XpAdminModule : InteractionModuleBase<SocketInteractionContext>
{
    // Generous but finite. The cap is not about balance — staff can simply run the
    // command again — it is so a slipped digit is caught by Discord's own validation
    // instead of quietly making someone level 400.
    private const int MaxAdjustment = 1_000_000;

    private const string Denied =
        "Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";

    private readonly XpService _xp;

    public XpAdminModule(XpService xp)
    {
        _xp = xp;
    }

    [SlashCommand("addxp", "Ajouter de l'XP à quelqu'un (admins/modérateurs)")]
    public Task AddXpAsync(
        [Summary("membre", "À qui donner de l'XP")] IUser user,
        [Summary("montant", "Combien d'XP ajouter")]
        [MinValue(1)] [MaxValue(MaxAdjustment)] int amount) => AdjustAsync(user, amount);

    [SlashCommand("removexp", "Retirer de l'XP à quelqu'un (admins/modérateurs)")]
    public Task RemoveXpAsync(
        [Summary("membre", "À qui retirer de l'XP")] IUser user,
        [Summary("montant", "Combien d'XP retirer")]
        [MinValue(1)] [MaxValue(MaxAdjustment)] int amount) => AdjustAsync(user, -amount);

    private async Task AdjustAsync(IUser target, long delta)
    {
        // Ephemeral throughout: an XP correction is staff business, and announcing it
        // in the channel would invite an argument about it.
        await DeferAsync(ephemeral: true);

        if (!SessionPermissions.IsStaff(Context.User))
        {
            await FollowupAsync(Denied, ephemeral: true);
            return;
        }

        // Bots earn no XP anywhere else (XpTracker skips them), so letting one be
        // topped up by hand would put a row on the leaderboard that nothing else can
        // ever produce — and /level refuses to render a bot's card at all.
        if (target.IsBot)
        {
            await FollowupAsync("Les bots ne gagnent pas d'XP, même par piston.", ephemeral: true);
            return;
        }

        var (oldLevel, newLevel, total) = await _xp.AdjustXpAsync(Context.Guild.Id, target.Id, delta);

        // No level-up card, even when this crosses a threshold: the card celebrates
        // something earned, and firing it for a manual grant would misrepresent it.
        var verb = delta >= 0 ? "ajouté" : "retiré";
        var levelNote = newLevel != oldLevel
            ? $" — niveau **{oldLevel}** → **{newLevel}**"
            : $" — toujours niveau **{newLevel}**";

        await FollowupAsync(
            $"{LevelCardUi.Xp(Math.Abs(delta))} XP {verb} à {target.Mention}. "
            + $"Total : **{LevelCardUi.Xp(total)}** XP{levelNote}.",
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }
}
