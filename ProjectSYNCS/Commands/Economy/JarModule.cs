using System.Text;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /jar: the guild's jar (JarService, Jar). A gift is public; every goal it reaches unlocks the next
// reward, carried out right after the gift (ResolveNextRewardAsync, one per save) and announced in
// the bots channel, with the giveaway's card. A gift set the unlocks off, so they ignore silence mode
// like any notice a command set up. Deferred, since every answer reads the database first.
[Group("jar", "Le bocal à cailloux du serveur")]
[CommandContextType(InteractionContextType.Guild)]
public class JarModule : InteractionModuleBase<SocketInteractionContext>
{
    private const int BarWidth = 10;

    private readonly JarService _jar;
    private readonly IServiceProvider _services;
    private readonly GuildConfigService _config;
    private readonly PlynlingAnnouncer _announcer;
    private readonly ResponsePicker _picker;
    private readonly ILogger<JarModule> _logger;

    public JarModule(JarService jar, IServiceProvider services, GuildConfigService config, PlynlingAnnouncer announcer,
        ResponsePicker picker, ILogger<JarModule> logger)
    {
        _jar = jar;
        _services = services;
        _config = config;
        _announcer = announcer;
        _picker = picker;
        _logger = logger;
    }

    [SlashCommand("status", "Voir le bocal, son objectif et ses plus grands donateurs")]
    public async Task StatusAsync()
    {
        await DeferAsync(ephemeral: true);
        var now = DateTimeOffset.UtcNow;
        var status = await _jar.StatusAsync(Context.Guild.Id, Context.User.Id, now);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Embed = BuildCard(status, now);
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    [SlashCommand("give", "Verser des cailloux dans le bocal")]
    public async Task GiveAsync(
        [Summary("amount", "Combien de cailloux verser"), MinValue(Jar.MinGive), MaxValue(Jar.MaxGive)] long amount)
    {
        await DeferAsync();
        var now = DateTimeOffset.UtcNow;
        var result = await _jar.GiveAsync(Context.Guild.Id, Context.User.Id, amount, now);
        if (result.Outcome == JarGiveOutcome.TooPoor)
        {
            await DeleteOriginalResponseAsync();
            await FollowupAsync($"Tu as {PebbleEconomy.Cailloux(result.Balance)}, pas assez pour en verser {PebbleEconomy.Cailloux(amount)}.",
                ephemeral: true);
            return;
        }

        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = $"🫙 <@{Context.User.Id}> verse **{PebbleEconomy.Cailloux(amount)}** dans le bocal.";
            m.Embed = BuildCard(result.Jar, now);
            m.AllowedMentions = AllowedMentions.None;
        });

        await ResolveRewardsAsync(Context.User.Id, now);
    }

    // Every reward pending, oldest first, each in its own scope and save. A failure is logged and
    // stops the loop: what is left stays pending for the next gift.
    private async Task ResolveRewardsAsync(ulong giverId, DateTimeOffset now)
    {
        IMessageChannel? channel = null;
        while (true)
        {
            JarUnlock? unlock;
            try
            {
                channel ??= await AnnounceChannelAsync();
                await using var scope = _services.CreateAsyncScope();
                unlock = await scope.ServiceProvider.GetRequiredService<JarService>()
                    .ResolveNextRewardAsync(Context.Guild.Id, Context.Client.CurrentUser.Id, channel.Id, now);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to resolve a jar reward in guild {GuildId}.", Context.Guild.Id);
                return;
            }
            if (unlock is null) return;
            _logger.LogInformation("Jar reward {Reward} (goal {Level}) unlocked in guild {GuildId}.",
                unlock.Reward, unlock.Level, Context.Guild.Id);
            await AnnounceAsync(channel, unlock, giverId);
        }
    }

    private async Task AnnounceAsync(IMessageChannel channel, JarUnlock unlock, ulong giverId)
    {
        await BotChat.PostWithTypingAsync(channel, UnlockLine(unlock, giverId, _picker), _logger, "jar unlock",
            AllowedMentions.None);
        if (unlock.Giveaway is not { } giveaway) return;
        try
        {
            var card = await channel.SendMessageAsync(embed: GiveawayModule.BuildEmbed(giveaway),
                components: GiveawayModule.BuildComponents(giveaway), allowedMentions: AllowedMentions.None);
            await using var scope = _services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<GiveawayService>()
                .SetMessageLocationAsync(giveaway.Id, channel.Id, card.Id);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post the jar's giveaway card {GiveawayId}.", giveaway.Id);
        }
    }

    // The bots channel (BotsChannel, which falls back to the game channel), else where /jar give was used.
    private async Task<IMessageChannel> AnnounceChannelAsync() =>
        await BotsChannel.ResolveAsync(Context.Client, _config, _announcer, Context.Guild.Id) ?? Context.Channel;

    public static Embed BuildCard(JarStatus s, DateTimeOffset now)
    {
        var text = new StringBuilder()
            .AppendLine($"`{TextChart.Bar(s.Filled, s.Goal, BarWidth)}` **{LevelCardUi.Xp(s.Filled)}** / {PebbleEconomy.Cailloux(s.Goal)}")
            .AppendLine($"Prochaine récompense : **{RewardTitle(s.Next)}**");

        var today = AppTime.DayKey(now);
        AppendDays(text, "Double `/daily`", s.DoubleDailyFrom, s.DoubleDailyUntil, today);

        var embed = new EmbedBuilder()
            .WithTitle("🫙 Le bocal")
            .WithDescription(text.ToString().TrimEnd())
            .WithColor(new Color(0xC9A227));
        if (s.Top.Count > 0)
            embed.AddField("Plus grands donateurs",
                string.Join("\n", s.Top.Select((t, i) => $"{i + 1}. <@{t.UserId}> · {PebbleEconomy.Cailloux(t.Total)}")));
        return embed
            .WithFooter($"Ton total : {PebbleEconomy.Cailloux(s.Mine)} · objectifs atteints : {s.Level}")
            .Build();
    }

    // A bonus range that is today or still to come, as Discord dates.
    private static void AppendDays(StringBuilder text, string label, int from, int until, int today)
    {
        if (from == 0 || until < today) return;
        var first = Math.Max(from, today);
        var when = first == until ? Date(first) : $"du {Date(first)} au {Date(until)}";
        text.AppendLine(Jar.Covers(from, until, today) && first == today && first == until
            ? $"{label} : **aujourd'hui** ({when})"
            : $"{label} : {when}");
    }

    private static string Date(int dayKey) =>
        $"<t:{AppTime.AtWallClock(AppTime.FromDayKey(dayKey)).ToUnixTimeSeconds()}:D>";

    public static string RewardTitle(JarReward reward) => reward switch
    {
        JarReward.DoubleDaily => "Double `/daily` pour une journée",
        JarReward.Feast => "Festin pour tous les Plynlings",
        _ => "Un giveaway de cailloux",
    };

    public static string UnlockLine(JarUnlock unlock, ulong giverId, ResponsePicker picker)
    {
        var giver = $"<@{giverId}>";
        return unlock.Reward switch
        {
            JarReward.DoubleDaily => string.Format(picker.Pick(BotResponses.JarDoubleDailyLines), giver, Date(unlock.Day)),
            JarReward.Feast => string.Format(picker.Pick(BotResponses.JarFeastLines), giver, unlock.Fed),
            _ => string.Format(picker.Pick(BotResponses.JarGiveawayLines), giver),
        };
    }
}
