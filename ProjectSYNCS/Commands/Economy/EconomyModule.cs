using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// The cailloux commands. Guild-only: a wallet belongs to one server.
[CommandContextType(InteractionContextType.Guild)]
public class EconomyModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PebbleService _pebbles;
    private readonly ResponsePicker _picker;

    public EconomyModule(PebbleService pebbles, ResponsePicker picker)
    {
        _pebbles = pebbles;
        _picker = picker;
    }

    // Public, in her voice: routine actions are public because they happen in a
    // dedicated channel. Works without owning a Plynling — people save up before
    // adopting, and keep earning after a death.
    [SlashCommand("work", "Travailler pour gagner des cailloux (toutes les 4 h)")]
    public async Task WorkAsync()
    {
        var now = DateTimeOffset.UtcNow;
        var result = await _pebbles.WorkAsync(Context.Guild.Id, Context.User.Id, PebbleEconomy.RollWorkPay(), now);
        var (text, refused) = WorkReply(result, _picker);
        if (refused)
            await RespondAsync(text, ephemeral: true);
        else
            await RespondAsync(text, allowedMentions: AllowedMentions.None);
    }

    // What /work answers, shared with the ;work shortcut (PlynlingShortcutService). A refusal is
    // shown privately — or as a passing reply, from the shortcut.
    public static (string Text, bool Refused) WorkReply(WorkResult result, ResponsePicker picker)
    {
        if (!result.Paid) return (PlynlingText.WorkCooldown(result.NextWorkAt), true);

        var line = string.Format(picker.Pick(BotResponses.WorkLines),
            $"+{PebbleEconomy.Cailloux(result.Amount)}");
        var hint = result.WorkReminder ? "" : " · 🔔 rappel en MP : `/balance`";
        return ($"{line}\n-# Solde : {PebbleEconomy.Cailloux(result.Balance)} · prochain service <t:{result.NextWorkAt.ToUnixTimeSeconds()}:R>{hint}",
            false);
    }

    // Public, like /work: the streak is something to show. Deferred, since the claim writes before
    // anything shows; a refusal stays private (the deferred reply is deleted, then a private follow-up).
    [SlashCommand("daily", "Ta récompense du jour, qui grandit avec ta série")]
    public async Task DailyAsync()
    {
        await DeferAsync();
        var result = await _pebbles.ClaimDailyAsync(Context.Guild.Id, Context.User.Id, DateTimeOffset.UtcNow, Random.Shared);
        var (text, refused) = DailyReply(result, Context.User.Id, _picker);
        if (refused)
        {
            await DeleteOriginalResponseAsync();
            await FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);
        }
        else
        {
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = text;
                m.AllowedMentions = AllowedMentions.None;
            });
        }
    }

    // What /daily answers, shared with the ;daily shortcut. « trouvé : » rather than an agreed
    // participle, because items carry no grammatical gender. A claim opens on one of her lines; a
    // refusal stays bare.
    public static (string Text, bool Refused) DailyReply(DailyResult result, ulong userId, ResponsePicker picker)
    {
        if (!result.Claimed)
            return ($"Déjà réclamé aujourd'hui. Prochain `/daily` <t:{result.NextClaimAt.ToUnixTimeSeconds()}:R> · série : {DailyStreak.Days(result.Streak)}", true);

        var text = $"{picker.Pick(BotResponses.DailyLines)}\nSérie : {DailyStreak.Days(result.Streak)}\n+{PebbleEconomy.Cailloux(result.Amount)}";
        if (result.Find is not { } find) return (text, false);
        return (PlynlingText.FindLines($"{text}  ·  trouvé : {PlynlingText.ItemLabel(find.Item)}", find, userId), false);
    }

    // Private: balances are nobody else's business, and nothing in v1 needs a rich list.
    // The passive line makes the cap visible rather than mysterious. Ephemeral, so whoever clicks
    // its reminder button (EconomyComponentHandler) is its owner.
    [SlashCommand("balance", "Voir tes cailloux (visible par toi seul)")]
    public async Task BalanceAsync()
    {
        var balance = await _pebbles.GetBalanceAsync(Context.Guild.Id, Context.User.Id);
        var (text, components) = BalanceCard(balance, DateTimeOffset.UtcNow, null);
        await RespondAsync(text, components: components, ephemeral: true);
    }

    public const string RemindOnId = "bal:remind:on";
    public const string RemindOffId = "bal:remind:off";

    // /balance's card, shared with the reminder button's redraw. The button names the action, not
    // the state; the state line says where it stands.
    public static (string Text, MessageComponent Components) BalanceCard(PebbleBalance balance, DateTimeOffset now, string? notice)
    {
        var work = balance.NextWorkAt is { } next && next > now
            ? $"<t:{next.ToUnixTimeSeconds()}:R>"
            : "disponible maintenant";
        var reminder = balance.WorkReminder ? "🔔 activé" : "🔕 désactivé";

        var text =
            $"🪨 **{PebbleEconomy.Cailloux(balance.Balance)}**\n" +
            $"Prochain `/work` : {work}\n" +
            $"Rappel en MP quand ton service est prêt : {reminder}\n" +
            $"Cailloux passifs aujourd'hui : {balance.PassiveToday} / {PebbleEconomy.PassiveDailyCap}";
        if (notice is not null) text += $"\n\n{notice}";

        var button = balance.WorkReminder
            ? new ButtonBuilder("Ne plus me prévenir", RemindOffId, ButtonStyle.Secondary, emote: new Emoji("🔕"))
            : new ButtonBuilder("Me prévenir en MP", RemindOnId, ButtonStyle.Primary, emote: new Emoji("🔔"));
        return (text, new ComponentBuilder().WithButton(button).Build());
    }

    public const string RemindOnNotice =
        "🔔 C'est noté. Je t'écris en MP à la fin de chaque repos.\n-# Il faut que tes MP soient ouverts aux membres du serveur.";
    public const string RemindOffNotice = "🔕 Plus de rappel en MP.";

    // Public when it goes through, so the gift is seen; both people as pills, never pinged. The amount
    // and the tax are capped at the option too, so Discord refuses them before anything runs. Deferred,
    // since the save comes before anything shows; a refusal stays private, like /daily's.
    [SlashCommand("pay", "Donner des cailloux à quelqu'un (taxe de 10 %)")]
    public async Task PayAsync(
        [Summary("member", "Qui reçoit les cailloux")] IUser member,
        [Summary("amount", "Combien (la taxe de 10 % s'ajoute, à ta charge)"), MinValue(PebbleEconomy.PayMin), MaxValue(PebbleEconomy.PayDailyCap)] long amount)
    {
        if (member.Id == Context.User.Id)
        {
            await RespondAsync(PaySelfRefusal, ephemeral: true);
            return;
        }
        if (member.IsBot)
        {
            await RespondAsync(PayBotRefusal, ephemeral: true);
            return;
        }

        await DeferAsync();
        var result = await _pebbles.PayAsync(Context.Guild.Id, Context.User.Id, member.Id, amount, DateTimeOffset.UtcNow);
        var (text, refused) = PayReply(result, Context.User.Id, member.Id);
        if (refused)
        {
            await DeleteOriginalResponseAsync();
            await FollowupAsync(text, ephemeral: true, allowedMentions: AllowedMentions.None);
        }
        else
        {
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = text;
                m.AllowedMentions = AllowedMentions.None;
            });
        }
    }

    public const string PaySelfRefusal = "Te payer toi-même ? Ça s'appelle garder ses cailloux.";
    public const string PayBotRefusal = "Les bots n'ont pas de porte-monnaie. Moi non plus, d'ailleurs.";

    // What /pay answers. The refusals name the number that settles it: the total with the tax, or
    // what is left of today's cap.
    public static (string Text, bool Refused) PayReply(PayResult result, ulong fromId, ulong toId) => result.Outcome switch
    {
        PayOutcome.Paid =>
            ($"💸 <@{fromId}> a donné **{PebbleEconomy.Cailloux(result.Amount)}** à <@{toId}> · taxe : {PebbleEconomy.Cailloux(result.Tax)}", false),
        PayOutcome.Self => (PaySelfRefusal, true),
        PayOutcome.TooSmall =>
            ($"Le minimum, c'est {PebbleEconomy.Cailloux(PebbleEconomy.PayMin)}.", true),
        PayOutcome.TooPoor =>
            ($"Il te faut {PebbleEconomy.Cailloux(result.Amount + result.Tax)} (dont {PebbleEconomy.Cailloux(result.Tax)} de taxe), et tu en as {PebbleEconomy.Cailloux(result.Balance)}.", true),
        _ when result.RoomLeft == 0 =>
            ($"Tu as déjà donné {PebbleEconomy.Cailloux(PebbleEconomy.PayDailyCap)} aujourd'hui, le maximum. Reviens après minuit.", true),
        // Room left, but less than the minimum: suggesting it would only earn a second refusal.
        _ when result.RoomLeft < PebbleEconomy.PayMin =>
            ($"Il te reste {PebbleEconomy.Cailloux(result.RoomLeft)} à donner aujourd'hui, sous le minimum de {PebbleEconomy.Cailloux(PebbleEconomy.PayMin)}. Reviens après minuit.", true),
        _ =>
            ($"Tu ne peux plus donner que {PebbleEconomy.Cailloux(result.RoomLeft)} aujourd'hui (maximum {PebbleEconomy.Cailloux(PebbleEconomy.PayDailyCap)} par jour).", true),
    };
}
