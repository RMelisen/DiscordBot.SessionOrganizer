using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Commands;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Interactions.Components;

// The buttons under her answers to chat requests (ChatterService.Requests) that aren't a wizard's:
// « Lancer l'appel » posts the /tonight card, « Annuler » takes back a reminder she saved. Only the
// asker may press them. The session and vote buttons live in ScheduleModule and VoteModule, whose
// drafts are theirs.
[CommandContextType(InteractionContextType.Guild)]
public class ChatRequestComponentHandler : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ChatOffers _offers;
    private readonly TonightService _tonight;
    private readonly PersonalReminderStore _reminders;
    private readonly ResponsePicker _picker;
    private readonly ILogger<ChatRequestComponentHandler> _logger;

    public ChatRequestComponentHandler(ChatOffers offers, TonightService tonight, PersonalReminderStore reminders,
        ResponsePicker picker, ILogger<ChatRequestComponentHandler> logger)
    {
        _offers = offers;
        _tonight = tonight;
        _reminders = reminders;
        _picker = picker;
        _logger = logger;
    }

    [ComponentInteraction("chatreq:tonight:*")]
    public async Task OnTonightAsync(string idText)
    {
        if (!int.TryParse(idText, out var id) || !_offers.TryGet(id, out var offer))
        {
            await RespondAsync(ChatOfferText.Expired("`/tonight`"), ephemeral: true);
            return;
        }
        if (offer.AskerId != Context.User.Id)
        {
            await RespondAsync(_picker.Pick(BotResponses.RequestNotYoursLines), ephemeral: true);
            return;
        }
        // One call per offer: of two clicks, one takes it.
        if (!_offers.TryTake(id, out offer))
        {
            await RespondAsync("C'est déjà lancé.", ephemeral: true);
            return;
        }

        await DeferAsync();
        var startsAt = offer.At!.Value;
        if (startsAt <= DateTimeOffset.UtcNow)
        {
            await FollowupAsync("Cette heure-là est déjà passée. Lance `/tonight` avec une autre heure.", ephemeral: true);
            return;
        }
        if (await _tonight.GetOpenAsync(Context.Guild.Id) is { } open)
        {
            await FollowupAsync($"Quelqu'un a déjà demandé qui était chaud ce soir : {TonightModule.Link(open)}", ephemeral: true);
            return;
        }

        var call = await _tonight.CreateAsync(Context.Guild.Id, Context.Channel.Id, Context.User.Id, offer.Title, startsAt,
            TonightModule.DefaultMinimum, TonightModule.ExpiryFor(startsAt));
        try
        {
            var card = await Context.Channel.SendMessageAsync(embed: TonightModule.BuildEmbed(call),
                components: TonightModule.BuildComponents(call), allowedMentions: AllowedMentions.None);
            await _tonight.SetMessageAsync(call.Id, card.Id);
        }
        catch (Exception ex)
        {
            // A call with no card would block the next /tonight until midnight: close it.
            _logger.LogWarning(ex, "Could not post the /tonight card for call {CallId}; closing it.", call.Id);
            await _tonight.ClaimCloseAsync(call.Id);
            _offers.PutBack(id, offer);
            await FollowupAsync("Je n'ai pas pu poster l'appel ici. Il me manque peut-être une permission dans ce salon.",
                ephemeral: true);
            return;
        }
        await DisableAsync("Appel lancé");
    }

    [ComponentInteraction("chatreq:remundo:*:*")]
    public async Task OnReminderUndoAsync(string idText, string askerText)
    {
        if (!ulong.TryParse(askerText, out var askerId) || askerId != Context.User.Id)
        {
            await RespondAsync(_picker.Pick(BotResponses.RequestNotYoursLines), ephemeral: true);
            return;
        }

        await DeferAsync();
        if (!int.TryParse(idText, out var id) || !await _reminders.CancelAsync(id, Context.User.Id))
        {
            await FollowupAsync("Ce rappel n'est plus en attente : déjà envoyé, ou déjà annulé.", ephemeral: true);
            return;
        }
        await DisableAsync("Rappel annulé");
    }

    // Her answer keeps its line; its one button turns grey with what happened.
    private async Task DisableAsync(string label)
    {
        try
        {
            var done = new ComponentBuilder()
                .WithButton(label, $"chatreq:done:{Context.Interaction.Id}", ButtonStyle.Secondary, disabled: true)
                .Build();
            await ((SocketMessageComponent)Context.Interaction).ModifyOriginalResponseAsync(p => p.Components = done);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Could not grey out a chat request's button.");
        }
    }
}
