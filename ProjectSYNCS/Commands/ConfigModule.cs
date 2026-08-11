using System.Text;
using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Runtime configuration, for staff. Everything here is *additive* to what the code
// already hardcodes: a server that never runs these commands behaves exactly as it did
// before they existed, and nothing configured here can take away an exclusion or a
// voting right that the code grants.
//
// A group with subcommands rather than one flat command, unlike /shame: the two
// settings have genuinely different shapes — a channel *set* needing add/remove, a role
// that is simply set or cleared — and cramming both into one command's optional
// parameters would leave people guessing which combinations are meaningful. /shame is
// flat only because it had to be invokable bare, which a parent with subcommands can
// never be; nothing here has that constraint, since "show me the config" is naturally
// its own subcommand.
//
// Guild-only: every subcommand is scoped to Context.Guild.Id, which is null in a DM,
// and config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this the command is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
// Deliberately NOT [DefaultMemberPermissions(GuildPermission.ManageGuild)] — see
// XpAdminModule for why. A permission bit cannot single out
// AvailabilityService.OwnerId, so on a server where the owner holds no ManageGuild
// role Discord would block him from a gate meant to admit him. IsStaff in every
// handler below is the only real check.
[Group("config", "Configurer le bot pour ce serveur (admins/modérateurs)")]
public class ConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string Denied =
        "Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";

    private readonly GuildConfigService _config;

    public ConfigModule(GuildConfigService config)
    {
        _config = config;
    }

    [SlashCommand("show", "Voir la configuration actuelle du serveur")]
    public async Task ShowAsync()
    {
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(Denied, ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);

        var config = await _config.GetAsync(Context.Guild.Id);

        var sb = new StringBuilder();
        sb.AppendLine("## Configuration du serveur");
        sb.AppendLine();

        sb.AppendLine("**Rôle modérateur** — peut voter avec `/shame`");
        sb.AppendLine(config.ModeratorRoleId == 0
            ? "> *non configuré*"
            : $"> <@&{config.ModeratorRoleId}>");
        sb.AppendLine();

        // The two lists are shown apart because only one of them can be edited here.
        // Merging them would invite someone to try removing a hardcoded channel and be
        // told no for reasons the display never hinted at.
        sb.AppendLine("**Salons sans XP — par défaut** (non modifiables)");
        sb.AppendLine($"> {Describe(XpTracker.HardcodedExcludedChannels)}");
        sb.AppendLine();

        sb.AppendLine("**Salons sans XP — ajoutés ici**");
        sb.AppendLine($"> {Describe(config.ExcludedChannels)}");

        // Channel and role mentions render as names without notifying anyone, but
        // AllowedMentions.None costs nothing and keeps the rule uniform across every
        // send in this project.
        await FollowupAsync(sb.ToString(), ephemeral: true, allowedMentions: AllowedMentions.None);
    }

    [Group("channels", "Les salons où rien ne compte (ni XP, ni mur de la honte)")]
    public class ChannelsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GuildConfigService _config;

        public ChannelsModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("add", "Exclure un salon : plus d'XP, et il ne compte plus pour le mur")]
        public async Task AddAsync(
            [Summary("salon", "Le salon à exclure")] IGuildChannel channel)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(Denied, ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);

            // Refused rather than silently stored: writing it would create a second,
            // redundant source of truth for a channel the code already excludes, and
            // the row could then be "removed" without changing anything.
            if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id))
            {
                await FollowupAsync(
                    $"<#{channel.Id}> est déjà exclu par défaut, dans le code. Rien à faire ✨",
                    ephemeral: true, allowedMentions: AllowedMentions.None);
                return;
            }

            var added = await _config.AddExcludedChannelAsync(Context.Guild.Id, channel.Id);

            await FollowupAsync(
                added
                    ? $"<#{channel.Id}> est maintenant exclu. Plus d'XP, et il ne compte plus pour le mur ദ്ദി◝ ⩊ ◜.ᐟ"
                    : $"<#{channel.Id}> était déjà dans la liste (ᵕ • ᴗ •)",
                ephemeral: true, allowedMentions: AllowedMentions.None);
        }

        [SlashCommand("remove", "Réinclure un salon ajouté ici")]
        public async Task RemoveAsync(
            [Summary("salon", "Le salon à réinclure")] IGuildChannel channel)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(Denied, ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);

            // The hardcoded list is a floor, not a default that can be edited away.
            // Said plainly here so it does not read as the command silently failing.
            if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id))
            {
                await FollowupAsync(
                    $"<#{channel.Id}> est exclu par défaut dans le code : je ne peux pas le réinclure d'ici {Emotes.Staring}",
                    ephemeral: true, allowedMentions: AllowedMentions.None);
                return;
            }

            var removed = await _config.RemoveExcludedChannelAsync(Context.Guild.Id, channel.Id);

            await FollowupAsync(
                removed
                    ? $"<#{channel.Id}> compte de nouveau. L'XP y est à nouveau gagnable ✨"
                    : $"<#{channel.Id}> n'était pas dans la liste (ᵕ • ᴗ •)",
                ephemeral: true, allowedMentions: AllowedMentions.None);
        }
    }

    [Group("moderator-role", "Le rôle autorisé à voter avec /shame")]
    public class ModeratorRoleModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GuildConfigService _config;

        public ModeratorRoleModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Définir le rôle qui peut voter avec /shame")]
        public async Task SetAsync(
            [Summary("role", "Le rôle des modérateurs")] IRole role)
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(Denied, ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);
            await _config.SetModeratorRoleAsync(Context.Guild.Id, role.Id);

            await FollowupAsync(
                $"<@&{role.Id}> peut maintenant voter avec `/shame`. "
                + "Le staff et la liste du code gardent leur accès, forcément ദ്ദി◝ ⩊ ◜.ᐟ",
                ephemeral: true, allowedMentions: AllowedMentions.None);
        }

        [SlashCommand("clear", "Retirer le rôle modérateur configuré")]
        public async Task ClearAsync()
        {
            if (!SessionPermissions.IsStaff(Context.User))
            {
                await RespondAsync(Denied, ephemeral: true);
                return;
            }

            await DeferAsync(ephemeral: true);
            await _config.SetModeratorRoleAsync(Context.Guild.Id, 0);

            await FollowupAsync(
                "Plus de rôle modérateur configuré. Seuls le staff et la liste du code peuvent voter (ᵕ • ᴗ •)",
                ephemeral: true, allowedMentions: AllowedMentions.None);
        }
    }

    // Channel mentions rather than names: Discord resolves them client-side, so this
    // stays correct after a rename and needs no gateway lookup here.
    private static string Describe(IReadOnlyCollection<ulong> channelIds) =>
        channelIds.Count == 0
            ? "*aucun*"
            : string.Join(" ", channelIds.Select(id => $"<#{id}>"));
}
