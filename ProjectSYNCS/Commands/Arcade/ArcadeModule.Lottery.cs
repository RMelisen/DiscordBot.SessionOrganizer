using System.Globalization;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /arcade lottery: the weekly draw (LotteryService, Lottery; LotteryDrawService draws it). Unlike the
// other games it defers: buying writes the database before anything can be shown. Buying is public,
// the status and a refusal private.
public partial class ArcadeModule
{
    private static readonly CultureInfo LotteryFr = CultureInfo.GetCultureInfo("fr-FR");

    [SlashCommand("lottery", "La loterie de la semaine : 20 cailloux le ticket, tirage dimanche à 21 h")]
    public async Task LotteryAsync(
        [Summary("tickets", "Combien de tickets acheter (sans rien : voir la cagnotte)"), MinValue(1), MaxValue(Lottery.MaxPerBuy)]
        int? tickets = null)
    {
        var now = DateTimeOffset.UtcNow;
        await using var scope = _services.CreateAsyncScope();
        var lottery = scope.ServiceProvider.GetRequiredService<LotteryService>();

        if (tickets is not { } count)
        {
            await DeferAsync(ephemeral: true);
            var status = await lottery.StatusAsync(Context.Guild.Id, Context.User.Id, now);
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = LotteryStatusText(status);
                m.AllowedMentions = AllowedMentions.None;
            });
            return;
        }

        await DeferAsync();
        var result = await lottery.BuyAsync(Context.Guild.Id, Context.User.Id, count, now);
        var text = LotteryBuyReply(result, Context.User.Id);
        if (result.Outcome == LotteryBuyOutcome.TooPoor)
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

    public static string LotteryStatusText(LotteryStatus s)
    {
        var draw = $"Tirage <t:{s.DrawAt.ToUnixTimeSeconds()}:F> (<t:{s.DrawAt.ToUnixTimeSeconds()}:R>).";
        if (s.Tickets == 0)
            return $"🎟️ Personne n'a encore joué cette semaine. {Lottery.TicketPrice} cailloux le ticket, et mon ticket gratuit ajoute sa part à la cagnotte.\n-# {draw}";
        var odds = s.Mine == 0 ? "" : $" · tes chances : {((double)s.Mine / (s.Tickets + 1) * 100).ToString("0.0", LotteryFr)} %";
        return $"🎟️ Cagnotte : **{PebbleEconomy.Cailloux(s.Pot)}** · {s.Tickets} ticket(s), {s.Players} joueur(s) + le mien"
               + $"\nTes tickets : {s.Mine}{odds}\n-# {draw} Le gagnant prend 75 %, je brûle le reste.";
    }

    public static string LotteryBuyReply(LotteryBuy b, ulong userId) => b.Outcome switch
    {
        LotteryBuyOutcome.Bought =>
            $"🎟️ <@{userId}> prend {b.Tickets} ticket(s) · cagnotte : **{PebbleEconomy.Cailloux(b.Status.Pot)}** · tirage <t:{b.Status.DrawAt.ToUnixTimeSeconds()}:R>",
        _ => $"{b.Tickets} ticket(s), ça fait {PebbleEconomy.Cailloux(b.Cost)}, et tu as {PebbleEconomy.Cailloux(b.Balance)}.",
    };
}
