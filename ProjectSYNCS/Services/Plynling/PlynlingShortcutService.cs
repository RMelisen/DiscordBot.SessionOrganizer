using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The typed shortcuts (Helpers/Plynling/PlynlingShortcut): ;pl, ;pl @x, ;pl visit @x, ;forage and
// ;work. Each runs the code its slash command runs (PlynlingModule.ViewLineAsync, BuildCardAsync,
// ForageReply and OpenVisitAsync, EconomyModule.WorkReply) and posts the same answer as a reply,
// so the two entry points cannot drift.
//
// A message cannot be answered privately, so a refusal is a reply that deletes itself after
// RefusalLife — long enough to read a « prochain service dans 2 heures », short enough not to litter.
//
// A shortcut is a command, not talk: BotService ends the fan-out when this handles a message, so it
// earns no XP and no passive cailloux (Plynling actions grant none — economy.md), draws no chatter and
// counts for no tracker, exactly like the slash command it stands for.
public sealed class PlynlingShortcutService
{
    private static readonly TimeSpan RefusalLife = TimeSpan.FromSeconds(15);

    private readonly IServiceProvider _services;
    private readonly ResponsePicker _picker;
    private readonly PlynlingCooldowns _cooldowns;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ILogger<PlynlingShortcutService> _logger;

    // Its own gate, per person and shortcut: a held-down Enter posts one card, not five, while ;work
    // straight after ;forage still runs. A repeat inside it is dropped without a word.
    private readonly CooldownGate<(ulong User, ShortcutKind Kind)> _repeat =
        new(TimeSpan.FromSeconds(3), forget: TimeSpan.FromMinutes(10));

    public PlynlingShortcutService(IServiceProvider services, ResponsePicker picker, PlynlingCooldowns cooldowns,
        PlynlingAnnouncer announcer, ILogger<PlynlingShortcutService> logger)
    {
        _services = services;
        _picker = picker;
        _cooldowns = cooldowns;
        _announcer = announcer;
        _logger = logger;
    }

    /// <summary>Runs the message if it is a shortcut, and says whether it was one.</summary>
    public async Task<bool> HandleMessageAsync(SocketMessage rawMessage)
    {
        if (rawMessage is not SocketUserMessage message) return false;
        if (message.Author.IsBot || message.Author.IsWebhook) return false;
        // Guild-only, like the commands: a wallet and a Plynling belong to one server.
        if (message.Channel is not SocketGuildChannel channel) return false;
        if (PlynlingShortcut.Parse(message.Content) is not { } shortcut) return false;
        if (!_repeat.TryClaim((message.Author.Id, shortcut.Kind))) return true;

        try
        {
            await RunAsync(message, channel.Guild, shortcut);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Shortcut {Kind} from {UserId} failed.", shortcut.Kind, message.Author.Id);
        }
        return true;
    }

    private async Task RunAsync(SocketUserMessage message, SocketGuild guild, Shortcut shortcut)
    {
        var now = DateTimeOffset.UtcNow;
        var authorId = message.Author.Id;
        await using var scope = _services.CreateAsyncScope();
        var plynlings = scope.ServiceProvider.GetRequiredService<PlynlingService>();

        switch (shortcut.Kind)
        {
            case ShortcutKind.Work:
            {
                var pebbles = scope.ServiceProvider.GetRequiredService<PebbleService>();
                var result = await pebbles.WorkAsync(guild.Id, authorId, PebbleEconomy.RollWorkPay(), now);
                await AnswerAsync(message, EconomyModule.WorkReply(result, _picker));
                return;
            }
            case ShortcutKind.Forage:
            {
                var result = await plynlings.ForageAsync(guild.Id, authorId, now, Random.Shared);
                await AnswerAsync(message, PlynlingModule.ForageReply(result, authorId));
                return;
            }
            case ShortcutKind.View:
                await ViewAsync(message, plynlings, guild.Id, shortcut.UserId ?? authorId, now);
                return;
            case ShortcutKind.Visit:
                await VisitAsync(message, scope.ServiceProvider, plynlings, guild, shortcut.UserId, now);
                return;
            default:
                await RefuseAsync(message, PlynlingShortcut.Usage);
                return;
        }
    }

    private async Task ViewAsync(SocketUserMessage message, PlynlingService plynlings, ulong guildId, ulong targetId,
        DateTimeOffset now)
    {
        var plynling = await plynlings.GetShownAsync(guildId, targetId, now);
        if (plynling is null)
        {
            await RefuseAsync(message, targetId == message.Author.Id ? PlynlingText.NoPlynling : PlynlingText.NoneFor(targetId));
            return;
        }
        var line = await PlynlingModule.ViewLineAsync(plynlings, _picker, plynling, message.Author.Id, now);
        var card = await PlynlingModule.BuildCardAsync(plynlings, plynling, now, line);
        await message.Channel.SendMessageAsync(components: card, flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None, messageReference: ReplyTo(message));
    }

    private async Task VisitAsync(SocketUserMessage message, IServiceProvider services, PlynlingService plynlings,
        SocketGuild guild, ulong? hostId, DateTimeOffset now)
    {
        if (hostId is not { } id)
        {
            await RefuseAsync(message, PlynlingShortcut.VisitWho);
            return;
        }
        // OpenVisitAsync needs to know whether the host is a bot; a mention always carries the user.
        IUser? host = message.MentionedUsers.FirstOrDefault(u => u.Id == id) ?? guild.GetUser(id);
        if (host is null)
        {
            await RefuseAsync(message, PlynlingText.NoneFor(id));
            return;
        }

        var opening = await PlynlingModule.OpenVisitAsync(plynlings, _cooldowns,
            services.GetRequiredService<PlynlingVisitRunner>(), _picker, guild.Id, message.Author.Id, host, now);
        if (opening.Refusal is not null || opening.Card is null)
        {
            await RefuseAsync(message, opening.Refusal ?? PlynlingText.Unknown);
            return;
        }

        // Mentions off even on the knock, unlike /plynling visit: the person's own « ;pl visit @x »
        // already pinged the host, and a second ping for one knock is one too many.
        await message.Channel.SendMessageAsync(components: opening.Card, flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None, messageReference: ReplyTo(message));
        if (opening.Story is not { } story) return;

        try
        {
            await message.Channel.SendMessageAsync(components: PlynlingPlayCards.BuildVisitStory(story, 0),
                flags: MessageFlags.ComponentsV2, allowedMentions: AllowedMentions.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Visit story {Story}: could not be posted", story.Id);
        }
        // What the visit left behind at her home, decided by her at once — after the visit's story.
        await _announcer.TellAsync(plynlings, opening.Untold ?? [], guild.Id, now, message.Channel);
    }

    private Task AnswerAsync(SocketUserMessage message, (string Text, bool Refused) reply) =>
        reply.Refused
            ? RefuseAsync(message, reply.Text)
            : message.Channel.SendMessageAsync(reply.Text, allowedMentions: AllowedMentions.None, messageReference: ReplyTo(message));

    // The private refusal of the slash command, as a passing reply.
    private async Task RefuseAsync(SocketUserMessage message, string text)
    {
        var sent = await message.Channel.SendMessageAsync(text, allowedMentions: AllowedMentions.None,
            messageReference: ReplyTo(message));
        _ = DeleteLaterAsync(sent);
    }

    // Fire-and-forget; a restart in between leaves the refusal up, which is harmless.
    private async Task DeleteLaterAsync(IMessage sent)
    {
        try
        {
            await Task.Delay(RefusalLife);
            await sent.DeleteAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not delete shortcut refusal {MessageId}.", sent.Id);
        }
    }

    // Still posted if the person deleted their message in the meantime.
    private static MessageReference ReplyTo(IMessage message) =>
        new(message.Id, failIfNotExists: false);
}
