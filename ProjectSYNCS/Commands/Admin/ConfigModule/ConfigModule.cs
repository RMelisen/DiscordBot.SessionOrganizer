using Discord;
using Discord.Interactions;
using Discord.Net;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// Runtime configuration, for staff. Two kinds of setting, with two rules:
//
// - **Lists** (excluded channels, idle channels, shame voters) are *additive* to what the
//   code hardcodes: the hardcoded entries are a floor that no command can remove, so a
//   config change can never take away an exclusion or a voting right by accident.
// - **Single channels** (game, main) *replace* their hardcoded default when set, and fall
//   back to it when cleared. A destination grants and revokes nothing. The quiz channel is
//   the one without a default: clearing it turns the quiz off.
// - **The mirrored roles** (Plynling, Grosse tête) are neither: SYNCS *owns* who holds them (a
//   strict mirror — see RoleMirror). Setting one can therefore take the role away from
//   people, which is why `set` refuses @everyone, managed roles, roles above hers and any role
//   carrying permissions, and why `clear` only stops managing it and touches nobody.
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
// The main and idle channels belong to MorningGreetingService, AmbientService and the weekend
// poll, which run per server once their feature is on (/config feature, ServerFeatures). Only the
// home server has a default main channel and default idle channels.
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
[NoPremiumPrank]
[Group("config", "Configurer le bot pour ce serveur (admins/modérateurs)")]
public partial class ConfigModule : SyncsModuleBase
{
    internal const string Denied =
        "Cette commande est réservée aux administrateurs et aux modérateurs. Bien tenté (˶ᵔ ᵕ ᵔ˶)";

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
        bool defaultBotsHere = Context.Guild.GetChannel(BotsChannel.DefaultId) is not null;

        var roleProblem = config.PlynlingRoleId == 0
            ? null
            : RoleMirror.Problem(Context.Guild, Context.Guild.GetRole(config.PlynlingRoleId), adopting: false, RoleKind.Plynling);
        var quizRoleProblem = config.QuizRoleId == 0
            ? null
            : RoleMirror.Problem(Context.Guild, Context.Guild.GetRole(config.QuizRoleId), adopting: false, RoleKind.QuizCrown);

        var (health, unset) = ServerHealthProbe.Check(Context.Guild, config);

        await FollowupAsync(
            components: ConfigCards.Build(config, Context.Guild.Id == HomeGuild.Id, defaultGameHere, defaultBotsHere,
                roleProblem, quizRoleProblem, health, unset),
            flags: MessageFlags.ComponentsV2,
            ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    // ---- The mirrored roles (Plynling, Grosse tête) ----
    // One create / set for both, so they can never drift apart.

    /// <summary>What /config needs to drive one mirrored role.</summary>
    /// <param name="Label">How replies name it: « un rôle {Label} ».</param>
    /// <param name="Store">Saves the role id (zero clears it).</param>
    /// <param name="LoadHolders">Who should hold it now.</param>
    internal sealed record ManagedRole(RoleKind Kind, string Label, string DefaultName,
        Func<ulong, Task> Store, Func<Task<HashSet<ulong>>> LoadHolders);

    // A role created over REST reaches the gateway cache a moment later; a pass before that would
    // find "no such role" and log a problem that isn't one.
    private static readonly TimeSpan FirstPassDelay = TimeSpan.FromSeconds(3);

    // With its leading space, or nothing when the preview failed (the reply goes out regardless).
    private static string CountsLine((int ToAdd, int ToRemove)? preview) =>
        preview is { } p ? " " + RoleMirrorRules.Counts(p.ToAdd, p.ToRemove) : "";

    internal static async Task CreateManagedRoleAsync(SocketInteractionContext context, RoleMirror mirror,
        ILogger logger, ManagedRole setup, ulong currentId, string? name, string doneTail)
    {
        var guild = context.Guild;

        // A configured role that was deleted in Discord counts as none.
        if (currentId != 0 && guild.GetRole(currentId) is not null)
        {
            await SayAsync(context,
                $"Il y a déjà un rôle {setup.Label} : <@&{currentId}>. `/config {setup.Kind.Command} clear` d'abord si tu en veux un autre (ᵕ • ᴗ •)");
            return;
        }
        if (guild.CurrentUser?.GuildPermissions.ManageRoles != true)
        {
            await SayAsync(context, RoleMirrorRules.NoManageRoles);
            return;
        }

        name = string.IsNullOrWhiteSpace(name) ? setup.DefaultName : name.Trim();
        IRole role;
        try
        {
            // No permissions, mentionable by everyone (it is a ping group). A new role lands at the
            // bottom of the list, below hers, so she can manage it from the start.
            role = await guild.CreateRoleAsync(name,
                permissions: GuildPermissions.None,
                isMentionable: true,
                options: new RequestOptions { AuditLogReason = $"/config {setup.Kind.Command} create" });
        }
        catch (HttpException ex)
        {
            await SayAsync(context, $"Discord a refusé de créer le rôle ({ex.Reason ?? ex.HttpCode.ToString()}) {Emotes.Staring}");
            return;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Creating the {Kind} role failed in guild {GuildId}.", setup.Kind.Key, guild.Id);
            await SayAsync(context, "Discord ne m'a pas répondu à temps. Réessaie dans un instant (ᵕ • ᴗ •)");
            return;
        }

        try
        {
            await setup.Store(role.Id);
        }
        catch (Exception ex)
        {
            // Not stored, so nothing would ever manage it — and a second click would make another.
            // Take back the role just made rather than leave an orphan.
            logger.LogError(ex, "Saving the {Kind} role failed in guild {GuildId}; deleting role {RoleId}.",
                setup.Kind.Key, guild.Id, role.Id);
            try { await role.DeleteAsync(new RequestOptions { AuditLogReason = $"/config {setup.Kind.Command} create a échoué" }); }
            catch (Exception deleteEx) { logger.LogWarning(deleteEx, "Could not delete orphan role {RoleId}.", role.Id); }
            await SayAsync(context, "Je n'ai pas réussi à enregistrer le rôle, alors je l'ai retiré. Réessaie dans un instant (╥﹏╥)");
            return;
        }

        var preview = await mirror.PreviewAsync(guild, role.Id, setup.LoadHolders);
        await SayAsync(context, $"<@&{role.Id}> est créé.{CountsLine(preview)} {doneTail}");
        var roleId = role.Id;
        mirror.StartMirror(FirstPassDelay, () => mirror.MirrorGuildAsync(guild, setup.Kind, roleId, setup.LoadHolders));
    }

    internal static async Task SetManagedRoleAsync(SocketInteractionContext context, RoleMirror mirror,
        ManagedRole setup, IRole role, string doneLead)
    {
        var guild = context.Guild;
        if (RoleMirror.Problem(guild, guild.GetRole(role.Id), adopting: true, setup.Kind) is { } problem)
        {
            await SayAsync(context, problem);
            return;
        }

        await setup.Store(role.Id);
        var preview = await mirror.PreviewAsync(guild, role.Id, setup.LoadHolders);
        await SayAsync(context, $"<@&{role.Id}> {doneLead}{CountsLine(preview)}");
        var roleId = role.Id;
        mirror.StartMirror(TimeSpan.Zero, () => mirror.MirrorGuildAsync(guild, setup.Kind, roleId, setup.LoadHolders));
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
    internal static Task SayAsync(SocketInteractionContext context, string text) =>
        context.Interaction.FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);

    // A destination she can't post in would fail silently every time it is used; refusing
    // it here is the only moment anyone would hear about it.
    internal static bool CanSendIn(SocketGuild guild, IGuildChannel channel)
    {
        if (guild.CurrentUser is not { } me || channel is not SocketGuildChannel socket) return false;
        var perms = me.GetPermissions(socket);
        return perms.ViewChannel && perms.SendMessages;
    }

    internal static string CannotSend(IGuildChannel channel) =>
        $"Je ne peux pas écrire dans <#{channel.Id}>. Donne-moi le droit d'y voir et d'y parler d'abord {Emotes.Staring}";
}
