using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Runtime configuration, for staff. Two kinds of setting, with two rules:
//
// - **Lists** (excluded channels, idle channels, shame voters) are *additive* to what the
//   code hardcodes: the hardcoded entries are a floor that no command can remove, so a
//   config change can never take away an exclusion or a voting right by accident.
// - **Single channels** (game, main) *replace* their hardcoded default when set, and fall
//   back to it when cleared. A destination grants and revokes nothing.
//
// Either way, a server that never runs these commands behaves exactly as it did before
// they existed.
//
// One subgroup per setting rather than one flat command, unlike /shame: the settings have
// genuinely different shapes — sets needing add/remove, single values set or cleared — and
// cramming them into one command's optional parameters would leave people guessing which
// combinations are meaningful. /shame is flat only because it had to be invokable bare,
// which a parent with subcommands can never be; "show me the config" is naturally its own
// subcommand.
//
// The main and idle channels belong to MorningGreetingService and AmbientService, which
// live in HomeGuild only (one state each). Elsewhere those subcommands refuse rather than
// store a row nothing would ever read.
//
// Guild-only: every subcommand is scoped to Context.Guild.Id, which is null in a DM,
// and config.yaml ships register_globally: true (a global command is DM-enabled by
// default). Without this the command is reachable somewhere it can only throw.
[CommandContextType(InteractionContextType.Guild)]
// Deliberately NOT [DefaultMemberPermissions(GuildPermission.ManageGuild)] — see
// AdminModule for why. A permission bit cannot single out
// AvailabilityService.OwnerId, so on a server where the owner holds no ManageGuild
// role Discord would block him from a gate meant to admit him. IsStaff in every
// handler below is the only real check.
[Group("config", "Configurer le bot pour ce serveur (admins/modérateurs)")]
public class ConfigModule : InteractionModuleBase<SocketInteractionContext>
{
    private const string Denied =
        "Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";

    private const string HomeOnly =
        "Ce réglage ne sert que sur le serveur principal : ici, pas de bonjour du matin ni de vie nocturne (ᵕ • ᴗ •)";

    private readonly GuildConfigService _config;

    public ConfigModule(GuildConfigService config)
    {
        _config = config;
    }

    [SlashCommand("show", "Voir la configuration actuelle du serveur")]
    public async Task ShowAsync()
    {
        if (!await BeginAsync(Context)) return;

        var config = await _config.GetAsync(Context.Guild.Id);
        bool defaultGameHere = Context.Guild.GetChannel(PlynlingAnnouncer.DefaultGameChannelId) is not null;

        await FollowupAsync(
            components: ConfigCards.Build(config, Context.Guild.Id == HomeGuild.Id, defaultGameHere),
            flags: MessageFlags.ComponentsV2,
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    [Group("excluded-channels", "Les salons où rien ne compte (ni XP, ni mur de la honte)")]
    public class ExcludedChannelsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GuildConfigService _config;

        public ExcludedChannelsModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("add", "Exclure un salon : plus d'XP, et il ne compte plus pour le mur")]
        public async Task AddAsync(
            [Summary("channel", "Le salon à exclure")] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            // Refused rather than silently stored: writing it would create a second,
            // redundant source of truth for a channel the code already excludes, and
            // the row could then be "removed" without changing anything.
            if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est déjà exclu par défaut, dans le code. Rien à faire {Emotes.Sparkle}");
                return;
            }

            var added = await _config.AddExcludedChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, added
                ? $"<#{channel.Id}> est maintenant exclu. Plus d'XP, et il ne compte plus pour le mur ദ്ദി◝ ⩊ ◜.ᐟ"
                : $"<#{channel.Id}> était déjà dans la liste (ᵕ • ᴗ •)");
        }

        [SlashCommand("remove", "Réinclure un salon ajouté ici")]
        public async Task RemoveAsync(
            [Summary("channel", "Le salon à réinclure")] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            // The hardcoded list is a floor, not a default that can be edited away.
            // Said plainly here so it does not read as the command silently failing.
            if (XpTracker.HardcodedExcludedChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est exclu par défaut dans le code : je ne peux pas le réinclure d'ici {Emotes.Staring}");
                return;
            }

            var removed = await _config.RemoveExcludedChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, removed
                ? $"<#{channel.Id}> compte de nouveau. L'XP y est à nouveau gagnable {Emotes.Sparkle}"
                : $"<#{channel.Id}> n'était pas dans la liste (ᵕ • ᴗ •)");
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
            if (!await BeginAsync(Context)) return;

            await _config.SetModeratorRoleAsync(Context.Guild.Id, role.Id);

            await SayAsync(Context,
                $"<@&{role.Id}> peut maintenant voter avec `/shame`. "
                + "Le staff et la liste du code gardent leur accès, forcément ദ്ദി◝ ⩊ ◜.ᐟ");
        }

        [SlashCommand("clear", "Retirer le rôle modérateur configuré")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetModeratorRoleAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                "Plus de rôle modérateur configuré. Seuls le staff et les votants de la liste peuvent voter (ᵕ • ᴗ •)");
        }
    }

    [Group("shame-voters", "Les personnes autorisées à voter avec /shame")]
    public class ShameVotersModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GuildConfigService _config;

        public ShameVotersModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("add", "Autoriser quelqu'un à voter avec /shame")]
        public async Task AddAsync(
            [Summary("user", "La personne à qui confier le vote")] IUser user)
        {
            if (!await BeginAsync(Context)) return;

            if (user.IsBot)
            {
                await SayAsync(Context, "Un bot qui vote ? Le seul bot qui juge ici, c'est moi (ᵕ • ᴗ •)");
                return;
            }

            if (ShameModule.HardcodedExtraVoters.Contains(user.Id))
            {
                await SayAsync(Context,
                    $"<@{user.Id}> vote déjà, c'est écrit dans mon code. Rien à faire {Emotes.Sparkle}");
                return;
            }

            var added = await _config.AddShameVoterAsync(Context.Guild.Id, user.Id);

            await SayAsync(Context, added
                ? $"<@{user.Id}> peut maintenant voter avec `/shame`. Je garde un œil sur ses votes, évidemment ദ്ദി◝ ⩊ ◜.ᐟ"
                : $"<@{user.Id}> était déjà dans la liste (ᵕ • ᴗ •)");
        }

        [SlashCommand("remove", "Retirer le vote /shame à quelqu'un ajouté ici")]
        public async Task RemoveAsync(
            [Summary("user", "La personne à qui retirer le vote")] IUser user)
        {
            if (!await BeginAsync(Context)) return;

            // The hardcoded voters are a floor: a config change must never revoke a right
            // the code grants.
            if (ShameModule.HardcodedExtraVoters.Contains(user.Id))
            {
                await SayAsync(Context,
                    $"<@{user.Id}> vote par défaut, c'est dans mon code : je ne peux pas lui retirer ça d'ici {Emotes.Staring}");
                return;
            }

            var removed = await _config.RemoveShameVoterAsync(Context.Guild.Id, user.Id);

            await SayAsync(Context, removed
                ? $"<@{user.Id}> ne vote plus. Le staff et le rôle modérateur votent toujours, hein (ᵕ • ᴗ •)"
                : $"<@{user.Id}> n'était pas dans la liste (ᵕ • ᴗ •)");
        }
    }

    [Group("game-channel", "Le salon où les Plynlings sont annoncés")]
    public class GameChannelModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GuildConfigService _config;

        public GameChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir le salon des annonces Plynling (morts, retours, histoires)")]
        public async Task SetAsync(
            [Summary("channel", "Le salon de jeu")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            // The default stored as zero, so /config show says "par défaut" and a later
            // change to the default follows.
            await _config.SetGameChannelAsync(Context.Guild.Id,
                channel.Id == PlynlingAnnouncer.DefaultGameChannelId ? 0 : channel.Id);

            await SayAsync(Context,
                $"Les Plynlings seront annoncés dans <#{channel.Id}>. Morts, retours, histoires : tout passe par là maintenant {Emotes.Sparkle}");
        }

        [SlashCommand("clear", "Revenir au salon de jeu par défaut")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            await _config.SetGameChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                Context.Guild.GetChannel(PlynlingAnnouncer.DefaultGameChannelId) is not null
                    ? $"Retour au salon par défaut, <#{PlynlingAnnouncer.DefaultGameChannelId}> (ᵕ • ᴗ •)"
                    : "Plus de salon de jeu ici : les Plynlings de ce serveur ne seront plus annoncés (ᵕ • ᴗ •)");
        }
    }

    [Group("main-channel", "Le salon où elle dit bonjour et vit la nuit (serveur principal)")]
    public class MainChannelModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GuildConfigService _config;

        public MainChannelModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("set", "Choisir son salon principal : bonjour du matin, ligne de 3 h, réveil")]
        public async Task SetAsync(
            [Summary("channel", "Le salon principal")]
            [ChannelTypes(ChannelType.Text, ChannelType.News)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (Context.Guild.Id != HomeGuild.Id)
            {
                await SayAsync(Context, HomeOnly);
                return;
            }

            if (!CanSendIn(Context.Guild, channel))
            {
                await SayAsync(Context, CannotSend(channel));
                return;
            }

            await _config.SetMainChannelAsync(Context.Guild.Id,
                channel.Id == MorningGreetingService.DefaultChannelId ? 0 : channel.Id);

            await SayAsync(Context,
                $"J'emménage dans <#{channel.Id}>. Bonjour du matin, nuits blanches, tout se passera là-bas ദ്ദി◝ ⩊ ◜.ᐟ");
        }

        [SlashCommand("clear", "Revenir au salon principal par défaut")]
        public async Task ClearAsync()
        {
            if (!await BeginAsync(Context)) return;

            if (Context.Guild.Id != HomeGuild.Id)
            {
                await SayAsync(Context, HomeOnly);
                return;
            }

            await _config.SetMainChannelAsync(Context.Guild.Id, 0);

            await SayAsync(Context,
                $"Je rentre à la maison, dans <#{MorningGreetingService.DefaultChannelId}> {Emotes.Sparkle}");
        }
    }

    [Group("idle-channels", "Les salons qui doivent tous se taire avant qu'elle parle dans le vide")]
    public class IdleChannelsModule : InteractionModuleBase<SocketInteractionContext>
    {
        private readonly GuildConfigService _config;

        public IdleChannelsModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("add", "Ajouter un salon : s'il parle, elle ne parle pas dans le vide")]
        public async Task AddAsync(
            [Summary("channel", "Le salon à surveiller")]
            [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Forum)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (Context.Guild.Id != HomeGuild.Id)
            {
                await SayAsync(Context, HomeOnly);
                return;
            }

            if (AmbientService.HardcodedIdleChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est déjà surveillé par défaut, dans le code. Rien à faire {Emotes.Sparkle}");
                return;
            }

            var added = await _config.AddIdleChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, added
                ? $"<#{channel.Id}> est surveillé. Tant qu'on y parle, je me tais ദ്ദി◝ ⩊ ◜.ᐟ"
                : $"<#{channel.Id}> était déjà dans la liste (ᵕ • ᴗ •)");
        }

        [SlashCommand("remove", "Retirer un salon ajouté ici")]
        public async Task RemoveAsync(
            [Summary("channel", "Le salon à ne plus surveiller")]
            [ChannelTypes(ChannelType.Text, ChannelType.News, ChannelType.Forum)] IGuildChannel channel)
        {
            if (!await BeginAsync(Context)) return;

            if (Context.Guild.Id != HomeGuild.Id)
            {
                await SayAsync(Context, HomeOnly);
                return;
            }

            if (AmbientService.HardcodedIdleChannels.Contains(channel.Id))
            {
                await SayAsync(Context,
                    $"<#{channel.Id}> est surveillé par défaut dans le code : je ne peux pas l'oublier d'ici {Emotes.Staring}");
                return;
            }

            var removed = await _config.RemoveIdleChannelAsync(Context.Guild.Id, channel.Id);

            await SayAsync(Context, removed
                ? $"<#{channel.Id}> n'est plus surveillé. Ce qui s'y dit ne m'empêchera plus de parler {Emotes.Sparkle}"
                : $"<#{channel.Id}> n'était pas dans la liste (ᵕ • ᴗ •)");
        }
    }

    // Every handler starts here: refuse anyone who isn't staff, then defer — the writes
    // below can outrun Discord's 3 seconds on the Pi. Static so the nested group modules,
    // which are separate module instances, share it.
    private static async Task<bool> BeginAsync(SocketInteractionContext context)
    {
        if (!SessionPermissions.IsStaff(context.User))
        {
            await context.Interaction.RespondAsync(Denied, ephemeral: true);
            return false;
        }

        await context.Interaction.DeferAsync(ephemeral: true);
        return true;
    }

    // Channel, role and user mentions render as names without notifying anyone here, but
    // AllowedMentions.None costs nothing and keeps the rule uniform across every send.
    private static Task SayAsync(SocketInteractionContext context, string text) =>
        context.Interaction.FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);

    // A destination she can't post in would fail silently every time it is used; refusing
    // it here is the only moment anyone would hear about it.
    private static bool CanSendIn(SocketGuild guild, IGuildChannel channel)
    {
        if (guild.CurrentUser is not { } me || channel is not SocketGuildChannel socket) return false;
        var perms = me.GetPermissions(socket);
        return perms.ViewChannel && perms.SendMessages;
    }

    private static string CannotSend(IGuildChannel channel) =>
        $"Je ne peux pas écrire dans <#{channel.Id}>. Donne-moi le droit d'y voir et d'y parler d'abord {Emotes.Staring}";
}
