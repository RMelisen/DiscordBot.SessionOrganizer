using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /quiz — her pop quiz's side of things people can open. The quiz itself has no command:
// QuizMasterService posts it on its own in the channel set by /config quiz-channel.
//
// A group with one subcommand for now, so the board has room for siblings without
// renaming what people type later.
//
// Guild-only: reads Context.Guild, null in a DM, and config.yaml ships
// register_globally: true (a global command is DM-enabled by default).
[CommandContextType(InteractionContextType.Guild)]
[Group("quiz", "Le quiz de SYNCS")]
public class QuizModule : InteractionModuleBase<SocketInteractionContext>
{
    // A month, like /shame: all-time would freeze the first winners at the top for good.
    private const StatsPeriod DefaultPeriod = StatsPeriod.Month;

    private readonly QuizService _quiz;

    public QuizModule(QuizService quiz)
    {
        _quiz = quiz;
    }

    [SlashCommand("leaderboard", "Le classement des meilleurs au quiz")]
    public async Task LeaderboardAsync()
    {
        await DeferAsync();
        var board = await _quiz.GetBoardAsync(Context.Guild.Id, DefaultPeriod);
        await FollowupAsync(
            components: QuizCards.BuildBoard(board, DefaultPeriod, Context.User.Id),
            flags: MessageFlags.ComponentsV2,
            allowedMentions: AllowedMentions.None);
    }

    // Its own verb (`:win:`), never the round buttons' `:pick:`. ignoreGroupNames because
    // this sits in a [Group]: without it Discord.Net prefixes the group name and the handler
    // silently never fires. StatsPeriodUi appends a page segment this board ignores.
    [ComponentInteraction("quiz:win:*:*", ignoreGroupNames: true)]
    public async Task OnPeriodAsync(string periodStr, string _)
    {
        if (!Enum.TryParse<StatsPeriod>(periodStr, out var period)) period = DefaultPeriod;

        await DeferAsync();
        var board = await _quiz.GetBoardAsync(Context.Guild.Id, period);
        var components = QuizCards.BuildBoard(board, period, Context.User.Id);

        await ModifyOriginalResponseAsync(m =>
        {
            m.Components = components;
            m.Flags = MessageFlags.ComponentsV2;
            m.AllowedMentions = AllowedMentions.None;
        });
    }
}
