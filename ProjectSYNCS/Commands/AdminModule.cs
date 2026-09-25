using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /admin — every moderation action in one place: /admin xp add|remove and /admin plynling
// rename|resurrect. Settings are not actions and stay in /config. Staff freezing or thawing
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

    [Group("plynling", "Gérer le Plynling de quelqu'un")]
    public class PlynlingAdminModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly PlynlingService _plynlings;
        private readonly ResponsePicker _picker;
        private readonly PlynlingAnnouncer _announcer;

        public PlynlingAdminModule(PlynlingService plynlings, ResponsePicker picker, PlynlingAnnouncer announcer)
        {
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
                    _picker.Pick(plynling.OwnerId, BotResponses.PlynlingStaffRenameDms.For(plynling.Gender)),
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

            await RespondAsync($"✨ **{PlynlingCardUi.SafeName(plynling.Name)}** est de retour (annoncé dans <#{PlynlingAnnouncer.GameChannelId}>).",
                ephemeral: true, allowedMentions: AllowedMentions.None);
            await _announcer.AnnounceResurrectionAsync(plynling, now);   // after the reply — see PlynlingModule.FreezeAsync
        }
    }
}
