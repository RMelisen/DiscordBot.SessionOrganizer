using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /admin — every moderation action in one place: /admin xp add|remove and /admin plynling
// rename|resurrect|passion-reset|cure. Settings are not actions and stay in /config. Staff freezing or thawing
// someone's Plynling stays in /plynling freeze|thaw user:, because players use those same
// commands on their own.
//
// Guild-only: everything here is scoped to Context.Guild.Id, which is null in a DM, and
// config.yaml ships register_globally: true (a global command is DM-enabled by default).
[CommandContextType(InteractionContextType.Guild)]
// Deliberately NOT [DefaultMemberPermissions(GuildPermission.ManageGuild)]. That gate is a
// Discord permission *bit*, which cannot express "ManageGuild holders, plus this one specific
// person" — it has no notion of AvailabilityService.OwnerId at all. On a server where the
// owner's roles carry no ManageGuild, Discord would hide (and refuse to invoke) the command for
// him even though IsStaff says he may use it. So everyone sees these in the picker;
// SessionPermissions.IsStaff in every handler is the only real gate — see CLAUDE.md's
// authorization-models note.
[Group("admin", "Outils de modération (admins/modérateurs)")]
public class AdminModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly AdminStatsService _stats;
    private readonly EconomyDashboardService _dashboard;

    public AdminModule(AdminStatsService stats, EconomyDashboardService dashboard)
    {
        _stats = stats;
        _dashboard = dashboard;
    }

    // The economy at a glance, to tune prices and rewards — private, staff only.
    [SlashCommand("stats", "Les statistiques de l'économie du serveur (cailloux, Plynlings, collections)")]
    public async Task StatsAsync()
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var stats = await _stats.GetAsync(Context.Guild.Id, DateTimeOffset.UtcNow);
        await FollowupAsync(embed: AdminCards.BuildStats(stats), ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    // The economy over time, day by day — the trends /admin stats cannot show. Opens on the last
    // 7 days; its buttons are handled by AdminComponentHandler, which re-checks IsStaff.
    [SlashCommand("dashboard", "L'économie jour après jour : flux de cailloux, activité, objets")]
    public async Task DashboardAsync()
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        await DeferAsync(ephemeral: true);
        var data = await _dashboard.GetAsync(Context.Guild.Id, DashboardWindow.Week, DateTimeOffset.UtcNow);
        var (embed, components) = AdminCards.BuildDashboard(data);
        await FollowupAsync(embed: embed, components: components, ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    // Discord's own ceiling for a timeout.
    private const int MaxMuteMinutes = 28 * 24 * 60;

    // A Discord timeout ("Exclure temporairement"). Every refusal Discord would answer with a bare
    // 403 is checked first and named, so a failed mute says which permission or role is missing.
    [SlashCommand("mute", "Faire taire quelqu'un pour un moment (exclusion temporaire)")]
    public async Task MuteAsync(
        [Summary("member", "Qui faire taire")] IUser user,
        [Summary("minutes", "Pour combien de minutes (28 jours max)")]
        [MinValue(1)] [MaxValue(MaxMuteMinutes)] int minutes = 5,
        [Summary("reason", "Pourquoi (visible dans le journal d'audit)")] [MaxLength(InputCaps.MuteReason)] string? reason = null)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        // The bot's owner can't be muted through her by anyone but himself, whatever his roles.
        if (user.Id == AvailabilityService.OwnerId && Context.User.Id != AvailabilityService.OwnerId)
        {
            await RespondAsync("Faire taire mon créateur ? Même pas en rêve.", ephemeral: true);
            return;
        }
        var target = Context.Guild.GetUser(user.Id);
        if (MuteRefusal(target, user) is { } refusal)
        {
            await RespondAsync(refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        if (target!.GuildPermissions.Administrator)
        {
            await RespondAsync($"J'ai pas les droits pour ça...",
                ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            await target.SetTimeOutAsync(TimeSpan.FromMinutes(minutes), AuditReason(reason));
        }
        catch (HttpException ex)
        {
            await FollowupAsync(DiscordRefused(ex), ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        var until = DateTimeOffset.UtcNow.AddMinutes(minutes).ToUnixTimeSeconds();
        await FollowupAsync($"Silence pour {target.Mention} jusqu'à <t:{until}:f> (<t:{until}:R>). Enfin (¬‿¬)",
            ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [SlashCommand("unmute", "Lever l'exclusion temporaire de quelqu'un")]
    public async Task UnmuteAsync(
        [Summary("member", "Qui libérer")] IUser user,
        [Summary("reason", "Pourquoi (visible dans le journal d'audit)")] [MaxLength(InputCaps.MuteReason)] string? reason = null)
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync("Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)", ephemeral: true);
            return;
        }
        var target = Context.Guild.GetUser(user.Id);
        if (MuteRefusal(target, user) is { } refusal)
        {
            await RespondAsync(refusal, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        if (target!.TimedOutUntil is not { } until || until <= DateTimeOffset.UtcNow)
        {
            await RespondAsync($"{target.Mention} n'est pas exclu temporairement.", ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await DeferAsync(ephemeral: true);
        try
        {
            await target.RemoveTimeOutAsync(AuditReason(reason));
        }
        catch (HttpException ex)
        {
            await FollowupAsync(DiscordRefused(ex), ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }

        await FollowupAsync($"{target.Mention} peut de nouveau parler.", ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    // What mute and unmute both need, in the order Discord would refuse it. Null means go ahead.
    // The invoker's rank is checked too: the bot acts with its own role, so without this a
    // moderator could silence someone ranked above them. The bot's owner is exempt, as IsStaff
    // makes him staff regardless of his roles.
    private string? MuteRefusal(SocketGuildUser? target, IUser user)
    {
        var me = Context.Guild.CurrentUser;
        if (target is null)
            return $"{user.Mention} n'est pas sur le serveur.";
        if (target.Id == me.Id)
            return "Me faire taire moi-même ? Non merci.";
        if (!me.GuildPermissions.ModerateMembers)
            return "Il me manque la permission **Exclure temporairement des membres** (Paramètres du serveur → Rôles → mon rôle).";
        if (target.Id == Context.Guild.OwnerId)
            return $"{target.Mention} est propriétaire du serveur : personne ne peut l'exclure temporairement.";
        if (me.Hierarchy <= target.Hierarchy)
            return $"Mon rôle le plus haut ({RoleName(me)}) doit être placé **au-dessus** de celui de {target.Mention} ({RoleName(target)}) dans la liste des rôles.";
        if (Context.User.Id != AvailabilityService.OwnerId
            && Context.User is SocketGuildUser invoker
            && invoker.Id != Context.Guild.OwnerId
            && invoker.Hierarchy <= target.Hierarchy)
            return $"{target.Mention} a un rôle aussi haut que le tien ou plus haut : je ne le fais pas à ta place.";
        return null;
    }

    private static string RoleName(SocketGuildUser member) =>
        member.Roles.MaxBy(r => r.Position) is { IsEveryone: false } top ? $"**{Format.Sanitize(top.Name)}**" : "aucun rôle";

    private RequestOptions AuditReason(string? reason) => new()
    {
        AuditLogReason = string.IsNullOrWhiteSpace(reason)
            ? $"Par {Context.User.Username}"
            : $"Par {Context.User.Username} : {reason.Trim()}",
    };

    // The checks above should leave nothing for Discord to refuse; if it still does (a role moved
    // between the check and the call), say what it said.
    private static string DiscordRefused(HttpException ex) =>
        ex.HttpCode == System.Net.HttpStatusCode.Forbidden
            ? $"Discord a refusé : permissions insuffisantes ({ex.DiscordCode?.ToString() ?? "403"})."
            : $"Discord a refusé : {ex.Reason ?? ex.Message}";

    // Manual XP adjustment. Kept apart from LevelModule, which owns the player-facing surfaces
    // (/level, /leaderboard) and is entirely Components V2 — these are two plain ephemeral
    // replies and share nothing with it but XpService.
    [Group("xp", "Ajuster l'XP de quelqu'un")]
    public class XpModule : InteractionModuleBase<SocketInteractionContext>
    {
        // Generous but finite. The cap is not about balance — staff can simply run the
        // command again — it is so a slipped digit is caught by Discord's own validation
        // instead of quietly making someone level 400.
        private const int MaxAdjustment = 1_000_000;

        private const string Denied =
            "Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";

        private readonly XpService _xp;

        public XpModule(XpService xp)
        {
            _xp = xp;
        }

        [SlashCommand("add", "Ajouter de l'XP à quelqu'un")]
        public Task AddXpAsync(
            [Summary("member", "À qui donner de l'XP")] IUser user,
            [Summary("amount", "Combien d'XP ajouter")]
            [MinValue(1)] [MaxValue(MaxAdjustment)] int amount) => AdjustAsync(user, amount);

        [SlashCommand("remove", "Retirer de l'XP à quelqu'un")]
        public Task RemoveXpAsync(
            [Summary("member", "À qui retirer de l'XP")] IUser user,
            [Summary("amount", "Combien d'XP retirer")]
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

    // Correcting someone's cailloux: a refund, a bug, an event prize. Same shape as /admin xp —
    // ephemeral, bots refused, a cap that catches a slipped digit — and, like it, the person is
    // not notified.
    [Group("pebble", "Ajuster les cailloux de quelqu'un")]
    public class PebbleAdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private const int MaxAdjustment = 1_000_000;

        private const string Denied =
            "Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";

        private readonly PebbleService _pebbles;

        public PebbleAdminModule(PebbleService pebbles)
        {
            _pebbles = pebbles;
        }

        [SlashCommand("add", "Ajouter des cailloux à quelqu'un")]
        public Task AddAsync(
            [Summary("member", "À qui donner des cailloux")] IUser user,
            [Summary("amount", "Combien de cailloux ajouter")]
            [MinValue(1)] [MaxValue(MaxAdjustment)] int amount) => AdjustAsync(user, amount);

        [SlashCommand("remove", "Retirer des cailloux à quelqu'un (jamais en dessous de 0)")]
        public Task RemoveAsync(
            [Summary("member", "À qui retirer des cailloux")] IUser user,
            [Summary("amount", "Combien de cailloux retirer")]
            [MinValue(1)] [MaxValue(MaxAdjustment)] int amount) => AdjustAsync(user, -amount);

        private async Task AdjustAsync(IUser target, long delta)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(Denied, ephemeral: true);
                return;
            }
            if (target.IsBot)
            {
                await RespondAsync("Les bots n'ont pas de portefeuille.", ephemeral: true);
                return;
            }

            var (old, now) = await _pebbles.AdjustAsync(Context.Guild.Id, target.Id, delta);
            var moved = Math.Abs(now - old);
            var text = delta >= 0
                ? $"{PebbleEconomy.Cailloux(moved)} ajoutés à {target.Mention}."
                : $"{PebbleEconomy.Cailloux(moved)} retirés à {target.Mention}" + (moved < -delta ? " (le solde ne descend pas sous 0)." : ".");
            await RespondAsync($"{text} Solde : **{PebbleEconomy.Cailloux(now)}**.", ephemeral: true, allowedMentions: AllowedMentions.None);
        }
    }

    [Group("plynling", "Gérer le Plynling de quelqu'un")]
    public class PlynlingAdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlynlingService _plynlings;
        private readonly ResponsePicker _picker;
        private readonly PlynlingAnnouncer _announcer;
        private readonly PlynlingRoleService _roles;

        public PlynlingAdminModule(PlynlingService plynlings, ResponsePicker picker, PlynlingAnnouncer announcer,
            PlynlingRoleService roles)
        {
            _roles = roles;
            _plynlings = plynlings;
            _picker = picker;
            _announcer = announcer;
        }

        // Staff only: the name is shown publicly (card, announcements, graveyard), so fixing
        // an offensive one is moderation. Reaches their latest grave too.
        [SlashCommand("rename", "Renommer le Plynling de quelqu'un")]
        public async Task RenameAsync(
            [Summary("user", "À qui est le Plynling")] IUser user,
            [Summary("name", "Son nouveau nom")] [MaxLength(InputCaps.PlynlingName)] string name)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
                return;
            }
            name = name.Trim();
            if (name.Length == 0)
            {
                await RespondAsync(PlynlingText.EmptyName, ephemeral: true);
                return;
            }

            var (plynling, oldName) = await _plynlings.RenameAsync(Context.Guild.Id, user.Id, name, DateTimeOffset.UtcNow);
            if (plynling is null)
            {
                await RespondAsync(PlynlingText.NoneFor(user.Id), ephemeral: true, allowedMentions: AllowedMentions.None);
                return;
            }

            await RespondAsync($"✏️ **{PlynlingCardUi.SafeName(oldName)}** s'appelle désormais **{PlynlingCardUi.SafeName(plynling.Name)}**.",
                ephemeral: true, allowedMentions: AllowedMentions.None);
            if (user.Id != Context.User.Id)   // after the reply — see PlynlingModule.FreezeAsync
                await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                    _picker.Pick(BotResponses.PlynlingStaffRenameDms.For(plynling.Gender)),
                    PlynlingCardUi.SafeName(oldName), PlynlingCardUi.SafeName(plynling.Name)));
        }

        // Staff only in v1 (a rare self-service item comes later, through the same
        // PlynlingService.ResurrectAsync). The comeback is announced publicly, like the death.
        [SlashCommand("resurrect", "Ressusciter le dernier Plynling de quelqu'un")]
        public async Task ResurrectAsync([Summary("user", "À qui est le Plynling")] IUser user)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
                return;
            }

            var now = DateTimeOffset.UtcNow;
            var (outcome, plynling) = await _plynlings.ResurrectAsync(Context.Guild.Id, user.Id, now);
            if (outcome != ResurrectOutcome.Resurrected || plynling is null)
            {
                await RespondAsync(outcome == ResurrectOutcome.NoGrave ? PlynlingText.NoGrave : PlynlingText.ResurrectBlocked,
                    ephemeral: true);
                return;
            }

            var where = await _announcer.ResolveGameChannelAsync(Context.Guild.Id) is { } game
                ? $"annoncé dans <#{game.Id}>"
                : "pas de salon de jeu ici, donc pas d'annonce";
            await RespondAsync($"{Emotes.Sparkle} **{PlynlingCardUi.SafeName(plynling.Name)}** est de retour ({where}).",
                ephemeral: true, allowedMentions: AllowedMentions.None);
            await _announcer.AnnounceResurrectionAsync(plynling, now);   // after the reply — see PlynlingModule.FreezeAsync
            await _roles.SyncMemberAsync(Context.Guild, user.Id);
        }

        // Staff only. Silent on purpose: no DM — the owner sees it on the card.
        [SlashCommand("cure", "Guérir le Plynling malade de quelqu'un")]
        public async Task CureAsync([Summary("user", "À qui est le Plynling")] IUser user)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
                return;
            }

            var (outcome, plynling) = await _plynlings.CureAsync(Context.Guild.Id, user.Id, DateTimeOffset.UtcNow);
            await RespondAsync(outcome switch
            {
                CureOutcome.Cured => $"💊 **{PlynlingCardUi.SafeName(plynling!.Name)}** est {plynling.Gender.Agree("guéri", "guérie")}.",
                CureOutcome.NotSick => $"**{PlynlingCardUi.SafeName(plynling!.Name)}** n'est pas malade.",
                CureOutcome.Dead => PlynlingText.Dead(plynling!.Gender),
                _ => PlynlingText.NoneFor(user.Id),
            }, ephemeral: true, allowedMentions: AllowedMentions.None);
        }

        // Staff only: the taught passion is free text shown publicly (card, visit stories), so
        // clearing an offensive one is moderation — the same reasoning as rename.
        [SlashCommand("passion-reset", "Effacer la passion apprise au Plynling de quelqu'un")]
        public async Task PassionResetAsync([Summary("user", "À qui est le Plynling")] IUser user)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(PlynlingText.StaffOnly, ephemeral: true);
                return;
            }

            var plynling = await _plynlings.ResetPassionAsync(Context.Guild.Id, user.Id, DateTimeOffset.UtcNow);
            if (plynling is null)
            {
                await RespondAsync(PlynlingText.NoneFor(user.Id), ephemeral: true, allowedMentions: AllowedMentions.None);
                return;
            }

            await RespondAsync(PlynlingText.PassionResetDone(PlynlingCardUi.SafeName(plynling.Name)),
                ephemeral: true, allowedMentions: AllowedMentions.None);
            if (user.Id != Context.User.Id)   // after the reply — see PlynlingModule.FreezeAsync
                await _announcer.DmOwnerAsync(plynling.OwnerId, string.Format(
                    _picker.Pick(BotResponses.PlynlingStaffPassionResetDms.For(plynling.Gender)),
                    PlynlingCardUi.SafeName(plynling.Name)));
        }
    }
}
