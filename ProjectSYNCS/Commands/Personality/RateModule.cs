using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /rate: she scores anything out of 10. The score is hashed from the text (Helpers/Personality/
// Rating), so it never changes, whoever asks; a few names get fixed verdicts. One command, its own
// module, like /yesno — and like /yesno no [CommandContextType]: nothing here reads Context.Guild,
// so it works in a DM too.
public class RateModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ResponsePicker _picker;

    private readonly BadgeAwarder _badges;

    public RateModule(ResponsePicker picker, BadgeAwarder badges)
    {
        _picker = picker;
        _badges = badges;
    }

    [SlashCommand("rate", "Je note n'importe quoi sur 10. Et je ne change jamais d'avis")]
    public Task RateAsync(
        [Summary("thing", "Ce que je dois noter : un jeu, un plat, quelqu'un…")]
        [MaxLength(InputCaps.Rate)]
        string subject)
    {
        var result = Rating.Rate(subject, Context.User.Id, Context.Client.CurrentUser.Id, AvailabilityService.OwnerId, BotResponses.TataId);
        var verdict = Verdict(result, _picker);
        if (result.Kind == RatingKind.Herself)
            _badges.Award(Context.Guild?.Id ?? 0, Context.User.Id, MemberBadges.Keys.Critic);

        // Quoted so her verdict never reads as her own words (as /yesno echoes its question), and
        // sent with mentions off: a rated @someone or :emote: keeps its look, unpinged. Not
        // Format.Sanitize: it escapes '<' '>' ':' too, which would break exactly those.
        var content = $"{MessageFormat.Quote(subject.Trim(), InputCaps.Rate)}\n{verdict}";
        return RespondAsync(content, allowedMentions: AllowedMentions.None);
    }

    /// <summary>
    /// Her verdict line for a rating: the pool its kind calls for, picked, then formatted. Static
    /// and shared: ChatterService gives the same verdict when she's asked her opinion in chat.
    /// </summary>
    public static string Verdict(RatingResult result, ResponsePicker picker)
    {
        var (pool, score) = result.Kind switch
        {
            RatingKind.Herself => (BotResponses.RateHerselfLines, ""),
            RatingKind.Owner => (result.AboutAsker ? BotResponses.RateOwnerSelfLines : BotResponses.RateOwnerLines, ""),
            RatingKind.Crush => (BotResponses.RateCrushLines, ""),
            RatingKind.Nemesis => (BotResponses.RateNemesisLines, $"{result.Score}/10"),
            RatingKind.Devil => (BotResponses.RateDevilLines, ""),
            RatingKind.God => (BotResponses.RateGodLines, ""),
            RatingKind.Tata => (result.AboutAsker ? BotResponses.RateTataSelfLines : BotResponses.RateTataLines, ""),
            RatingKind.Routier => (BotResponses.RateRoutierLines, ""),
            RatingKind.Mitraillette => (BotResponses.RateMitrailletteLines, ""),
            _ => (BotResponses.RateBand(result.Score), $"{result.Score}/10"),
        };

        // The pick first, the format second (a template is never chosen after formatting). {0} is
        // the score; the fixed verdicts carry their own and ignore it.
        return string.Format(picker.Pick(pool), score);
    }
}
