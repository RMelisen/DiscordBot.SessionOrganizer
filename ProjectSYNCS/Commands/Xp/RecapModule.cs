using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /recap: one person's numbers over a window, from every table another subsystem already keeps
// (RecapService), and her one-line read of them. Components V2 like /level, so the avatar is the
// real one; sent with AllowedMentions.None, since a TextDisplay is real content. Guild-only:
// every query is scoped to Context.Guild.
[CommandContextType(InteractionContextType.Guild)]
public class RecapModule : InteractionModuleBase<SocketInteractionContext>
{
    // Month by default: a recap is about lately, and a week is often empty.
    public const StatsPeriod DefaultPeriod = StatsPeriod.Month;

    // A command refusal, not chatter: one fixed line, no pool.
    private const string BotRefusal = "Les bots n'ont pas de récap. Seulement des bugs. Surtout ceux-là (¬_¬)";

    private readonly RecapService _recap;
    private readonly ResponsePicker _picker;

    public RecapModule(RecapService recap, ResponsePicker picker)
    {
        _recap = recap;
        _picker = picker;
    }

    [SlashCommand("recap", "Ton récap : tes chiffres sur 7 jours, 30 jours ou depuis toujours")]
    public async Task RecapAsync(
        [Summary("user", "Le récap de quelqu'un d'autre (par défaut : toi)")] IUser? user = null)
    {
        var target = user ?? Context.User;
        if (target.Id == Context.Client.CurrentUser.Id)
        {
            await RespondAsync(_picker.Pick(BotResponses.RecapHerselfLines), allowedMentions: AllowedMentions.None);
            return;
        }
        if (target.IsBot)
        {
            await RespondAsync(BotRefusal, allowedMentions: AllowedMentions.None);
            return;
        }

        // A dozen small reads on the Pi: defer before the first one.
        await DeferAsync();
        var stats = await _recap.GetAsync(Context.Guild.Id, target.Id, DefaultPeriod);
        await FollowupAsync(
            components: BuildCard(target.Id, target, stats, Verdict(_picker, target.Id, stats)),
            flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);
    }

    /// <summary>Her last line: fond for Papa whatever his numbers, otherwise by profile.</summary>
    public static string Verdict(ResponsePicker picker, ulong userId, RecapStats stats) =>
        picker.Pick(userId == AvailabilityService.OwnerId
            ? BotResponses.RecapOwnerLines
            : BotResponses.RecapLines[RecapProfile.Classify(stats)]);

    /// <summary>
    /// The card. About a dozen components (container, section + text + thumbnail, up to five text
    /// blocks, two separators, one row of three buttons): far under the 40 cap. Shared by the
    /// command and RecapComponentHandler.
    /// </summary>
    /// <param name="user">Null when they left the server: the mention still renders, the avatar falls back.</param>
    public static MessageComponent BuildCard(ulong userId, IUser? user, RecapStats s, string verdict)
    {
        var container = new ContainerBuilder()
            .WithAccentColor(Color.Purple)
            .AddComponent(new SectionBuilder()
                .WithAccessory(AvatarUi.Thumbnail(user))
                .AddComponent(new TextDisplayBuilder(
                    $"### Récap de <@{userId}>\n-# {StatsPeriodUi.Label(s.Period)}")))
            .AddComponent(new SeparatorBuilder());

        foreach (var block in Blocks(s))
            container.AddComponent(new TextDisplayBuilder(block));

        container
            .AddComponent(new SeparatorBuilder())
            .AddComponent(new TextDisplayBuilder(verdict));

        return new ComponentBuilderV2()
            .AddComponent(container)
            // One row, one verb: the filter is the only button on the card.
            .AddComponent(new ActionRowBuilder().AddFilterRow($"recap:win:{userId}", s.Period))
            .Build();
    }

    // The card's sections, each skipped when it would be empty. Never gendered: « présence en
    // vocal », not « présent ».
    private static IEnumerable<string> Blocks(RecapStats s)
    {
        var activity = new List<string>
        {
            s.Level is { } level
                ? $"Niveau **{level}** · {LevelCardUi.Xp(s.Xp)} XP" + (s.Rank is { } rank ? $" · rang #{rank}" : "")
                : $"**{LevelCardUi.Xp(s.Xp)}** XP gagnés",
            $"{LevelCardUi.Xp(s.Reactions)} réaction(s) · {LevelCardUi.Duration(s.VoiceMinutes)} en vocal",
        };
        yield return "**Activité**\n" + string.Join("\n", activity);

        var withHer = new List<string>();
        if (s.MinutesWithHer > 0) withHer.Add($"{LevelCardUi.Duration(s.MinutesWithHer)} en vocal avec moi");
        if (s.GoodBot + s.BadBot > 0) withHer.Add($"{s.GoodBot} « good bot » · {s.BadBot} « bad bot »");
        if (withHer.Count > 0) yield return "**Avec moi**\n" + string.Join("\n", withHer);

        var nights = new List<string>();
        if (s.SessionsJoined > 0)
        {
            var line = $"{s.SessionsJoined} session(s) rejointe(s) · {s.SessionsAttended} avec présence en vocal";
            if (s.SessionsMissed > 0) line += $" · {s.SessionsMissed} absence(s)";
            nights.Add(line);
        }
        if (s.TonightIn > 0) nights.Add($"{s.TonightIn} « Chaud » sur /tonight");
        if (s.QuizWins > 0)
            nights.Add($"{s.QuizWins} quiz gagné(s)" + (s.QuizBestMs > 0 ? $" · record : {s.QuizBestMs / 1000.0:0.0} s" : ""));
        if (nights.Count > 0) yield return "**Soirées & jeux**\n" + string.Join("\n", nights);

        var life = new List<string>
        {
            s.PlynlingName is { } name
                ? $"Plynling : **{Format.Sanitize(name)}**, {(int)(s.PlynlingAge ?? TimeSpan.Zero).TotalDays} jour(s)"
                : "Pas de Plynling en ce moment",
        };
        if (s.Graves > 0) life.Add($"{s.Graves} tombe(s) au cimetière");
        life.Add($"{LevelCardUi.Xp(s.Balance)} cailloux");
        yield return "**Au quotidien**\n" + string.Join("\n", life);

        if (s.ShamePoints > 0)
        {
            var parts = new List<string>();
            if (s.MeanHits > 0) parts.Add($"{s.MeanHits} méchanceté(s)");
            if (s.ShoutHits > 0) parts.Add($"{s.ShoutHits} cri(s)");
            if (s.PerfidyHits > 0) parts.Add($"{s.PerfidyHits} trahison(s)");
            if (s.BanVotes > 0) parts.Add($"{s.BanVotes} vote(s) de bannissement");
            if (s.AbandonHits > 0) parts.Add($"{s.AbandonHits} abandon(s)");
            yield return "**Mur de la honte**\n" + string.Join(" · ", parts);
        }
    }
}
