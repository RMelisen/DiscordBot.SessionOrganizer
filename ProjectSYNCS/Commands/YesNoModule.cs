using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// A coin flip she delivers in her own voice. One command, its own module, the same
// shape as AbsenceModule and HelpModule.
//
// Deliberately no [CommandContextType]: nothing here reads Context.Guild, so it works
// in a DM as happily as in a channel — the same reason HelpModule carries no guard.
public class YesNoModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ResponsePicker _picker;

    public YesNoModule(ResponsePicker picker)
    {
        _picker = picker;
    }

    [SlashCommand("yesno", "Pose-moi une question fermée, je tranche")]
    public Task YesNoAsync(
        [Summary("question", "La question à trancher (facultatif)")]
        string? question = null)
    {
        // The flip first, the wording second. Picking a pool and then a line means a
        // "yes" phrasing can never come out of a "no" verdict — see BotResponses.
        var yes = Random.Shared.Next(2) == 0;
        var pool = yes ? BotResponses.YesLines : BotResponses.NoLines;

        var name = BotResponses.DisplayNameFor(Context.User.Id,
            (Context.User as SocketGuildUser)?.Nickname
            ?? Context.User.GlobalName
            ?? Context.User.Username);

        // Bucketed per channel like every other pool, so the same verdict phrasing
        // doesn't come back twice running in the same conversation.
        var verdict = string.Format(_picker.Pick(Context.Channel.Id, pool), name);

        // Public on purpose: the whole point is that the room sees the ruling. The
        // question is echoed only when one was given, so a bare /yesno stays a clean
        // one-liner — and it is quoted so her verdict never reads as her own words.
        var content = string.IsNullOrWhiteSpace(question)
            ? verdict
            : $"> {question.Trim()}\n{verdict}";

        // Same rule as every other relay here: the asker's text is echoed back, so it
        // must never become a way to ping a role or the whole server.
        return RespondAsync(content, allowedMentions: new AllowedMentions(AllowedMentionTypes.Users));
    }
}
