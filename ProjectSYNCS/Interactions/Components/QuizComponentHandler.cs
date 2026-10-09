using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The A/B/C/D buttons of a pop quiz card. The decision is QuizMasterService's (one try per
// person, first right answer wins); this only answers the click.
//
// Defers first: judging a click reads the round and a win writes the wallet and the board,
// which can outrun Discord's 3 seconds on the Pi. A deferred *update* (not a new message),
// so nothing is posted unless there is something to say, and the refusals are ephemeral
// followups only the clicker sees.
public class QuizComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private const string TooLate = "Trop tard, ce quiz est terminé (ᵕ • ᴗ •)";
    private const string Gone = "Ce quiz n'existe plus.";
    private const string Failed = "Oups, j'ai pas réussi à enregistrer ta réponse. Réessaie dans un instant.";

    private readonly QuizMasterService _quiz;
    private readonly ResponsePicker _picker;
    private readonly ILogger<QuizComponentHandler> _logger;

    public QuizComponentHandler(QuizMasterService quiz, ResponsePicker picker, ILogger<QuizComponentHandler> logger)
    {
        _quiz = quiz;
        _picker = picker;
        _logger = logger;
    }

    [ComponentInteraction("quiz:pick:*:*")]
    public async Task OnPickAsync(string roundIdStr, string positionStr)
    {
        if (!int.TryParse(roundIdStr, out var roundId) || !int.TryParse(positionStr, out var position))
        {
            await RespondAsync(Gone, ephemeral: true);
            return;
        }

        await DeferAsync();

        QuizPickResult result;
        try
        {
            result = await _quiz.PickAsync(roundId, Context.User.Id, position);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Quiz pick failed on round {RoundId}.", roundId);
            await FollowupAsync(Failed, ephemeral: true);
            return;
        }

        var reply = result switch
        {
            QuizPickResult.Wrong => _picker.Pick(BotResponses.QuizWrongLines),
            QuizPickResult.AlreadyTried => _picker.Pick(BotResponses.QuizAlreadyTriedLines),
            QuizPickResult.TooLate => TooLate,
            QuizPickResult.Gone => Gone,
            // The win is announced in the channel; the card already changed under them.
            _ => null,
        };
        if (reply is not null)
            await FollowupAsync(reply, ephemeral: true, allowedMentions: AllowedMentions.None);
    }
}
