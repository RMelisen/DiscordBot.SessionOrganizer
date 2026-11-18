using System.Globalization;
using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /shoutout: the auction for a dedication in her morning hello (ShoutoutService, Shoutouts). The hello
// is only ever said in the home guild's main channel, so the auction exists there alone: elsewhere a
// bid would hold cailloux for a dedication nobody would ever read. Deferred, since every answer
// reads the database first; taking the lead is public, the rest private.
[Group("shoutout", "Les dédicaces dans son bonjour du matin")]
[CommandContextType(InteractionContextType.Guild)]
public class ShoutoutModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly CultureInfo French = CultureInfo.GetCultureInfo("fr-FR");

    public const string HomeOnly = "Les dédicaces, c'est dans mon bonjour du matin, et je ne le dis que sur mon serveur.";

    private readonly ShoutoutService _shoutouts;

    public ShoutoutModule(ShoutoutService shoutouts)
    {
        _shoutouts = shoutouts;
    }

    [SlashCommand("bid", "Enchérir pour une dédicace dans son bonjour de demain")]
    public async Task BidAsync(
        [Summary("amount", "Ta mise (rendue si quelqu'un enchérit plus haut)"), MinValue(Shoutouts.MinOpening), MaxValue(Shoutouts.MaxBid)] long amount)
    {
        if (Context.Guild.Id != HomeGuild.Id)
        {
            await RespondAsync(HomeOnly, ephemeral: true);
            return;
        }

        await DeferAsync();
        var result = await _shoutouts.BidAsync(Context.Guild.Id, Context.User.Id, amount, DateTimeOffset.UtcNow);
        var text = BidReply(result, Context.User.Id);
        if (result.Outcome != BidOutcome.Leading)
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

    [SlashCommand("status", "Où en sont les enchères pour la prochaine dédicace")]
    public async Task StatusAsync()
    {
        if (Context.Guild.Id != HomeGuild.Id)
        {
            await RespondAsync(HomeOnly, ephemeral: true);
            return;
        }

        await DeferAsync(ephemeral: true);
        var status = await _shoutouts.StatusAsync(Context.Guild.Id, DateTimeOffset.UtcNow);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = StatusText(status);
            m.AllowedMentions = AllowedMentions.None;
        });
    }

    // What a bid answers. The lead names the day, the amount, who got their bid back, the next
    // minimum and the close; a refusal names the number that settles it.
    public static string BidReply(BidResult result, ulong userId) => result.Outcome switch
    {
        BidOutcome.Leading =>
            $"📣 <@{userId}> mène les enchères pour la dédicace du {Day(result.ForDay)} : **{PebbleEconomy.Cailloux(result.Amount)}**."
            + (result.Outbid is { } prev ? $" <@{prev}> récupère sa mise." : "")
            + $"\n-# Prochaine enchère : au moins {PebbleEconomy.Cailloux(result.Minimum)}. Clôture {Close(result.ForDay)}.",
        BidOutcome.TooLow =>
            $"Il faut au moins {PebbleEconomy.Cailloux(result.Minimum)} pour mener les enchères du {Day(result.ForDay)}.",
        _ when result.Cost < result.Amount =>
            $"Monter ta mise à {PebbleEconomy.Cailloux(result.Amount)} te coûte {PebbleEconomy.Cailloux(result.Cost)} de plus, "
            + $"et tu as {PebbleEconomy.Cailloux(result.Balance)}.",
        _ =>
            $"Tu as {PebbleEconomy.Cailloux(result.Balance)}, pas assez pour miser {PebbleEconomy.Cailloux(result.Amount)}.",
    };

    public static string StatusText(ShoutoutStatus status)
    {
        var head = status.Leader is { } leader
            ? $"📣 Dédicace du {Day(status.ForDay)} : <@{leader}> mène avec **{PebbleEconomy.Cailloux(status.Amount)}**. "
              + $"Prochaine enchère : au moins {PebbleEconomy.Cailloux(status.Minimum)}."
            : $"📣 Dédicace du {Day(status.ForDay)} : personne n'a encore enchéri. Ouverture à {PebbleEconomy.Cailloux(status.Minimum)}.";
        var queued = status.Queued > 0
            ? $"\n-# {status.Queued} dédicace{(status.Queued > 1 ? "s gagnées attendent" : " gagnée attend")} un bonjour."
            : "";
        return $"{head}\n-# Clôture {Close(status.ForDay)}.{queued}";
    }

    private static string Day(int dayKey) => AppTime.FromDayKey(dayKey).ToString("dddd d MMMM", French);

    private static string Close(int forDay)
    {
        var unix = Shoutouts.ClosesAt(forDay).ToUnixTimeSeconds();
        return $"<t:{unix}:f> (<t:{unix}:R>)";
    }
}
