using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /bribe: paying her off. Two things for sale at one weekly price that doubles with each bribe
// (Helpers/Economy/Bribes): 24 h as her chouchou (warm answers and reactions, ChouchouBook), or one
// /shame hit she pretends she never saw. Deferred, since the save comes before anything shows; a
// purchase is public (it is the joke), a refusal private, like /pay's.
[Group("bribe", "Soudoyer SYNCS avec des cailloux")]
[CommandContextType(InteractionContextType.Guild)]
public class BribeModule : SyncsModuleBase
{
    private readonly BribeService _bribes;

    public BribeModule(BribeService bribes)
    {
        _bribes = bribes;
    }

    [SlashCommand("favourite", "Devenir son chouchou pendant 24 h")]
    public async Task FavouriteAsync()
    {
        await DeferAsync();
        var result = await _bribes.BuyFavouriteAsync(Context.Guild.Id, Context.User.Id, DateTimeOffset.UtcNow);
        await AnswerAsync(result, null);
    }

    [SlashCommand("shame", "Effacer un de tes points de honte")]
    public async Task ShameAsync([Summary("category", "Dans quel titre")] ShameCategory category)
    {
        await DeferAsync();
        var result = await _bribes.ForgiveShameAsync(Context.Guild.Id, Context.User.Id, category, DateTimeOffset.UtcNow);
        await AnswerAsync(result, category);
    }

    private async Task AnswerAsync(BribeResult result, ShameCategory? category)
    {
        var text = Reply(result, Context.User.Id, category);
        if (result.Outcome != BribeOutcome.Done)
        {
            await DeleteOriginalResponseAsync();
            await FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);
            return;
        }
        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = text;
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // What /bribe answers. A purchase names what was bought, its price and the next one this week;
    // a refusal names the number that settles it.
    public static string Reply(BribeResult result, ulong userId, ShameCategory? category) => result.Outcome switch
    {
        BribeOutcome.Done when category is { } c =>
            $"🙈 J'ai rien vu : un point de moins pour <@{userId}> dans **{ShameCategories.Title(c)}**, "
            + $"pour {PebbleEconomy.Cailloux(result.Price)}.\n-# Prochain pot-de-vin cette semaine : {PebbleEconomy.Cailloux(result.NextPrice)}.",
        BribeOutcome.Done =>
            $"💸 <@{userId}> est mon chouchou jusqu'à {Format(result.FavouriteUntil)} (+24 h), "
            + $"pour {PebbleEconomy.Cailloux(result.Price)}.\n-# Prochain pot-de-vin cette semaine : {PebbleEconomy.Cailloux(result.NextPrice)}.",
        BribeOutcome.TooPoor =>
            $"Un pot-de-vin coûte {PebbleEconomy.Cailloux(result.Price)} cette semaine, et tu en as {PebbleEconomy.Cailloux(result.Balance)}. Reviens plus riche.",
        BribeOutcome.Family =>
            "Tu es déjà de la famille. Ça, ça s'achète pas ♡",
        _ =>
            $"Tu n'as aucun point dans **{(category is { } n ? ShameCategories.Title(n) : "ce titre")}**. Je vais pas te faire payer pour rien. …Si ? Non.",
    };

    private static string Format(DateTimeOffset? until) =>
        until is { } u ? $"<t:{u.ToUnixTimeSeconds()}:f>" : "demain";
}
