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
        return ($"{line}\n-# Solde : {PebbleEconomy.Cailloux(result.Balance)} · prochain service <t:{result.NextWorkAt.ToUnixTimeSeconds()}:R>",
            false);
    }

    // Private: balances are nobody else's business, and nothing in v1 needs a rich list.
    // The passive line makes the cap visible rather than mysterious.
    [SlashCommand("balance", "Voir tes cailloux (visible par toi seul)")]
    public async Task BalanceAsync()
    {
        var balance = await _pebbles.GetBalanceAsync(Context.Guild.Id, Context.User.Id);
        var now = DateTimeOffset.UtcNow;
        var work = balance.NextWorkAt is { } next && next > now
            ? $"<t:{next.ToUnixTimeSeconds()}:R>"
            : "disponible maintenant";

        await RespondAsync(
            $"🪨 **{PebbleEconomy.Cailloux(balance.Balance)}**\n" +
            $"Prochain `/work` : {work}\n" +
            $"Cailloux passifs aujourd'hui : {balance.PassiveToday} / {PebbleEconomy.PassiveDailyCap}",
            ephemeral: true);
    }
}
