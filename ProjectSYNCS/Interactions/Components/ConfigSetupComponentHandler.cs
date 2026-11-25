using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// /config setup's controls (ConfigSetupCards). Every pick is saved at once through the same rules as
// the /config commands — a channel she can't post in is refused, a mirrored role goes through
// ConfigModule's create / set — then the page is redrawn in place. A refusal, or a role's result,
// comes as a private follow-up, like the command's own replies.
//
// IsStaff on every click: the setup message is ephemeral, but nothing about a component says who
// may press it. Each click defers first (the writes can outrun Discord's 3 seconds on the Pi).
public class ConfigSetupComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly GuildConfigService _config;
    private readonly RoleMirror _mirror;
    private readonly PlynlingRoleService _plynlingRoles;
    private readonly QuizCrownService _crown;
    private readonly ILogger<ConfigSetupComponentHandler> _logger;

    public ConfigSetupComponentHandler(GuildConfigService config, RoleMirror mirror, PlynlingRoleService plynlingRoles,
        QuizCrownService crown, ILogger<ConfigSetupComponentHandler> logger)
    {
        _config = config;
        _mirror = mirror;
        _plynlingRoles = plynlingRoles;
        _crown = crown;
        _logger = logger;
    }

    [ComponentInteraction(ConfigSetupCards.Prefix + ":next:*", ignoreGroupNames: true)]
    public Task OnNextAsync(string page) => ShowAsync(page);

    [ComponentInteraction(ConfigSetupCards.Prefix + ":prev:*", ignoreGroupNames: true)]
    public Task OnPrevAsync(string page) => ShowAsync(page);

    [ComponentInteraction(ConfigSetupCards.Prefix + ":done", ignoreGroupNames: true)]
    public async Task OnDoneAsync()
    {
        if (!await BeginAsync()) return;
        var view = ConfigModule.SetupViewOf(Context.Guild, await _config.GetAsync(Context.Guild.Id));
        await RedrawAsync(ConfigSetupCards.BuildDone(view));
    }

    // A channel picked (or the select emptied). Read from the raw values: an emptied select sends none.
    [ComponentInteraction(ConfigSetupCards.Prefix + ":ch:*:*", ignoreGroupNames: true)]
    public async Task OnChannelAsync(string slotStr, string page)
    {
        if (ConfigSetupCards.ParseSlot(slotStr) is not { } slot) return;
        if (!await BeginAsync()) return;

        var guild = Context.Guild;
        ulong picked = PickedId();
        if (picked != 0)
        {
            if (guild.GetChannel(picked) is not { } channel) return;
            if (!ConfigModule.CanSendIn(guild, channel))
            {
                await ConfigModule.SayAsync(Context, ConfigModule.CannotSend(channel));
                await ShowAfterAsync(page);
                return;
            }
        }

        // Same storage as each `set` / `clear`: the default itself is stored as zero.
        switch (slot)
        {
            case HealthSlot.Game:
                await _config.SetGameChannelAsync(guild.Id, picked == PlynlingAnnouncer.DefaultGameChannelId ? 0 : picked);
                break;
            case HealthSlot.Bots:
                await _config.SetBotsChannelAsync(guild.Id, picked == BotsChannel.DefaultId ? 0 : picked);
                break;
            case HealthSlot.LevelUp: await _config.SetLevelUpChannelAsync(guild.Id, picked); break;
            case HealthSlot.Quiz: await _config.SetQuizChannelAsync(guild.Id, picked); break;
            case HealthSlot.Confess: await _config.SetConfessChannelAsync(guild.Id, picked); break;
            case HealthSlot.FreeGames: await _config.SetFreeGamesChannelAsync(guild.Id, picked); break;
            case HealthSlot.Main:
                await _config.SetMainChannelAsync(guild.Id,
                    HomeGuild.Is(guild.Id) && picked == MorningGreetingService.DefaultChannelId ? 0 : picked);
                break;
            default: return;
        }
        _logger.LogInformation("/config setup: {Slot} set to {ChannelId} in guild {GuildId}.", slot, picked, guild.Id);
        await ShowAfterAsync(page);
    }

    // The features select: what is ticked is on, the rest off. Only the switches that change are
    // written, so an untouched feature keeps following its default.
    [ComponentInteraction(ConfigSetupCards.Prefix + ":feat:*", ignoreGroupNames: true)]
    public async Task OnFeaturesAsync(string page)
    {
        if (!await BeginAsync()) return;
        var guild = Context.Guild;
        var ticked = ((SocketMessageComponent)Context.Interaction).Data.Values?.ToHashSet() ?? new HashSet<string>();
        var config = await _config.GetAsync(guild.Id);

        foreach (var feature in ServerFeatures.All)
        {
            bool on = ticked.Contains(feature.Key);
            if (config.IsOn(feature.Key, guild.Id) == on) continue;
            await _config.SetFeatureAsync(guild.Id, feature.Key, on);
            _logger.LogInformation("/config setup: feature {Feature} {State} in guild {GuildId}.", feature.Key, on ? "on" : "off", guild.Id);
        }
        await ShowAfterAsync(page);
    }

    [ComponentInteraction(ConfigSetupCards.Prefix + ":role:*:*", ignoreGroupNames: true)]
    public async Task OnRoleAsync(string kind, string page)
    {
        if (!await BeginAsync()) return;
        var guild = Context.Guild;
        ulong picked = PickedId();

        if (kind == "moderator")
        {
            await _config.SetModeratorRoleAsync(guild.Id, picked);
        }
        else if (Setup(kind) is { } setup)
        {
            if (picked == 0)
                await setup.Store(0);   // like `clear`: stop managing it, touch nobody
            else if (guild.GetRole(picked) is { } role)
                await ConfigModule.SetManagedRoleAsync(Context, _mirror, setup, role, $"est maintenant le rôle {setup.Label}.");
        }
        else return;

        await ShowAfterAsync(page);
    }

    [ComponentInteraction(ConfigSetupCards.Prefix + ":create:*:*", ignoreGroupNames: true)]
    public async Task OnCreateAsync(string kind, string page)
    {
        if (Setup(kind) is not { } setup) return;
        if (!await BeginAsync()) return;

        var config = await _config.GetAsync(Context.Guild.Id);
        ulong current = kind == "plynling" ? config.PlynlingRoleId : config.QuizRoleId;
        // The same closing words as `/config plynling-role create` and `/config quiz-role create`.
        await ConfigModule.CreateManagedRoleAsync(Context, _mirror, _logger, setup, current, name: null,
            kind == "plynling"
                ? "Renomme-le ou change sa couleur comme tu veux : moi, je ne fais que le donner et le retirer ദ്ദി◝ ⩊ ◜.ᐟ"
                : "Chaque lundi à 10 h, je le donne à la meilleure tête du quiz de la semaine passée ദ്ദി◝ ⩊ ◜.ᐟ");
        await ShowAfterAsync(page);
    }

    // ---- Shared ------------------------------------------------------------------------------

    private ConfigModule.ManagedRole? Setup(string kind) => kind switch
    {
        "plynling" => new(RoleKind.Plynling, "Plynling", PlynlingRoleService.DefaultName,
            id => _config.SetPlynlingRoleAsync(Context.Guild.Id, id),
            () => _plynlingRoles.LoadHoldersAsync(Context.Guild.Id)),
        "quiz" => new(RoleKind.QuizCrown, "Grosse tête", QuizCrownService.DefaultName,
            id => _config.SetQuizRoleAsync(Context.Guild.Id, id),
            () => _crown.LoadHoldersAsync(Context.Guild.Id)),
        _ => null,
    };

    private async Task<bool> BeginAsync()
    {
        if (Context.Guild is null) return false;
        if (!SessionPermissions.IsStaff(Context.User))
        {
            await RespondAsync(ConfigModule.Denied, ephemeral: true);
            return false;
        }
        await DeferAsync();
        return true;
    }

    private async Task ShowAsync(string page)
    {
        if (!await BeginAsync()) return;
        await ShowAfterAsync(page);
    }

    // After a defer: the page redrawn from what is stored now.
    private async Task ShowAfterAsync(string page)
    {
        var view = ConfigModule.SetupViewOf(Context.Guild, await _config.GetAsync(Context.Guild.Id));
        await RedrawAsync(ConfigSetupCards.Build(ConfigSetupCards.Parse(page), view));
    }

    // V2 on every edit (root CLAUDE.md): the flag re-asserted, no content, no embeds, no pings.
    private async Task RedrawAsync(MessageComponent components)
    {
        try
        {
            await ModifyOriginalResponseAsync(msg =>
            {
                msg.Content = string.Empty;
                msg.Embeds = Array.Empty<Embed>();
                msg.Components = components;
                msg.Flags = MessageFlags.ComponentsV2;
                msg.AllowedMentions = AllowedMentions.None;
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not redraw /config setup in guild {GuildId}.", Context.Guild.Id);
        }
    }

    private ulong PickedId() =>
        ((SocketMessageComponent)Context.Interaction).Data.Values?.FirstOrDefault() is { } raw
        && ulong.TryParse(raw, out var id) ? id : 0;
}
