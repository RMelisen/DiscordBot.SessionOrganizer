using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public partial class ScheduleModule : InteractionModuleBase<SocketInteractionContext>
{
    // ---- « Préparer la session », under her answer to a chat request -----------------------------
    // ChatterService offered it (ChatOffers): the title and time read from the message. The click opens
    // the same private flow /schedule create does, starting from the person's last choices. Her answer
    // is public, so nothing here may edit it: every step is a new private message, and a modal opens
    // only from one of those (a modal opened from her answer would update her answer on submit).

    [ComponentInteraction("chatreq:session:*", ignoreGroupNames: true)]
    public async Task OnChatSessionAsync(string idText)
    {
        if (!int.TryParse(idText, out var id) || !_offers.TryGet(id, out var offer))
        {
            await RespondAsync(ChatOfferText.Expired("`/schedule create`"), ephemeral: true);
            return;
        }
        if (offer.AskerId != Context.User.Id)
        {
            await RespondAsync(_picker.Pick(BotResponses.RequestNotYoursLines), ephemeral: true);
            return;
        }

        var draft = NewDraft();
        draft.Title = offer.Title;
        // A time read an hour ago may have gone by since.
        draft.At = offer.At is { } at && at > DateTimeOffset.UtcNow ? at : null;
        _drafts.Set(Context.User.Id, draft);

        if (draft.At is not null && draft.Title.Length > 0)
        {
            await ((SocketMessageComponent)Context.Interaction).DeferLoadingAsync(ephemeral: true);
            await DrawPreviewAsync(draft);
            return;
        }

        // The time known, the name not: one button to the name. Otherwise the time step, whose
        // « Taper une date » asks for the name too when it's missing.
        var step = draft.At is { } known
            ? Step($"**Nouvelle session** · <t:{known.ToUnixTimeSeconds()}:F> — comment elle s'appelle ?",
                new ActionRowBuilder().WithButton("Lui donner un nom", "schedule:chatname", ButtonStyle.Primary))
            : TimeStep(draft);
        await RespondAsync(components: step, flags: MessageFlags.ComponentsV2, ephemeral: true,
            allowedMentions: AllowedMentions.None);
    }

    [ComponentInteraction("schedule:chatname", ignoreGroupNames: true)]
    public async Task OnChatNameAsync()
    {
        if (await DraftAsync() is not { } draft) return;
        await RespondWithModalAsync(DetailsModal("schedule:details", draft));
    }
}
