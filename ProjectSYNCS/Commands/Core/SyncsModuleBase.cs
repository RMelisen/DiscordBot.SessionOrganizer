using System.Collections.Concurrent;
using Discord;
using Discord.Interactions;

namespace ProjectSYNCS.Commands;

/// <summary>
/// The base of every interaction module. It changes nothing, except for a command the premium
/// easter egg caught (PremiumPrankService): that interaction's first answer is already spent on the
/// paywall, now edited into « je rigole », so the command's own answers are rerouted —
/// <c>RespondAsync</c> becomes a follow-up, <c>DeferAsync</c> does nothing, and the « original
/// response » it edits, fetches or deletes is that follow-up, never the joke. Only the module's own
/// methods are rerouted: a command answering through <c>Context.Interaction</c>, or with a modal,
/// carries <see cref="NoPremiumPrankAttribute"/>.
/// </summary>
public abstract class SyncsModuleBase : InteractionModuleBase<SocketInteractionContext>
{
    // Interactions the prank answered, by id. An entry lives until the interaction's token expires
    // (PremiumPrankService removes it): a command's answers can outlast its handler.
    private static readonly ConcurrentDictionary<ulong, PrankedReply> Pranked = new();

    private sealed class PrankedReply
    {
        // The command deferred privately: an answer built from a later edit stays private.
        public bool DeferredEphemeral;

        // What stands for the command's original response: its first answer, sent as a follow-up.
        public IUserMessage? Original;
    }

    internal static void MarkPranked(ulong interactionId) => Pranked[interactionId] = new PrankedReply();

    internal static void ForgetPranked(ulong interactionId) => Pranked.TryRemove(interactionId, out _);

    private PrankedReply? Prank => Pranked.TryGetValue(Context.Interaction.Id, out var prank) ? prank : null;

    protected override Task DeferAsync(bool ephemeral = false, RequestOptions? options = null)
    {
        if (Prank is not { } prank) return base.DeferAsync(ephemeral, options);
        prank.DeferredEphemeral = ephemeral;
        return Task.CompletedTask;
    }

    protected override async Task RespondAsync(string? text = null, Embed[]? embeds = null, bool isTTS = false,
        bool ephemeral = false, AllowedMentions? allowedMentions = null, RequestOptions? options = null,
        MessageComponent? components = null, Embed? embed = null, PollProperties? poll = null,
        MessageFlags flags = MessageFlags.None)
    {
        if (Prank is not { } prank)
        {
            await base.RespondAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, components, embed, poll, flags);
            return;
        }
        prank.Original = await base.FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, components, embed, poll, flags);
    }

    // After a defer, the first follow-up is the answer: it becomes the original response.
    protected override async Task<IUserMessage> FollowupAsync(string? text = null, Embed[]? embeds = null,
        bool isTTS = false, bool ephemeral = false, AllowedMentions? allowedMentions = null,
        RequestOptions? options = null, MessageComponent? components = null, Embed? embed = null,
        PollProperties? poll = null, MessageFlags flags = MessageFlags.None)
    {
        var message = await base.FollowupAsync(text, embeds, isTTS, ephemeral, allowedMentions, options, components, embed, poll, flags);
        if (Prank is { Original: null } prank) prank.Original = message;
        return message;
    }

    protected override async Task<IUserMessage> ModifyOriginalResponseAsync(Action<MessageProperties> propBuilder,
        RequestOptions? options = null)
    {
        if (Prank is not { } prank) return await base.ModifyOriginalResponseAsync(propBuilder, options);
        if (prank.Original is { } original)
        {
            await original.ModifyAsync(propBuilder, options);
            return original;
        }

        // Deferred, nothing sent yet: the edit that would have replaced « réfléchit… » is sent instead.
        var props = new MessageProperties();
        propBuilder(props);
        prank.Original = await base.FollowupAsync(
            text: props.Content.GetValueOrDefault(),
            embeds: props.Embeds.GetValueOrDefault(),
            ephemeral: prank.DeferredEphemeral,
            allowedMentions: props.AllowedMentions.GetValueOrDefault(),
            options: options,
            components: props.Components.GetValueOrDefault(),
            embed: props.Embed.GetValueOrDefault(),
            flags: props.Flags.GetValueOrDefault() ?? MessageFlags.None);
        return prank.Original;
    }

    // A modal can only be a first answer. Commands that open one carry [NoPremiumPrank]; one that
    // forgot gets a private apology instead of an exception.
    protected override async Task RespondWithModalAsync(Modal modal, RequestOptions? options = null)
    {
        if (Prank is null)
        {
            await base.RespondWithModalAsync(modal, options);
            return;
        }
        await base.FollowupAsync(ModalLost, ephemeral: true);
    }

    protected override async Task RespondWithModalAsync<TModal>(string customId, RequestOptions? options = null,
        Action<ModalBuilder>? modifyModal = null)
    {
        if (Prank is null)
        {
            await base.RespondWithModalAsync<TModal>(customId, options, modifyModal);
            return;
        }
        await base.FollowupAsync(ModalLost, ephemeral: true);
    }

    private const string ModalLost = "Bon, la blague a mangé ton formulaire. Relance la commande, promis je recommence pas (˶ᵔ ᵕ ᵔ˶)";

    protected override async Task<IUserMessage> GetOriginalResponseAsync(RequestOptions? options = null) =>
        Prank is { Original: { } original } ? original : await base.GetOriginalResponseAsync(options);

    protected override async Task DeleteOriginalResponseAsync()
    {
        if (Prank is not { } prank)
        {
            await base.DeleteOriginalResponseAsync();
            return;
        }
        // Nothing sent yet (a refusal after a defer): there is nothing to take back, and the joke stays.
        if (prank.Original is { } original)
        {
            prank.Original = null;
            await original.DeleteAsync();
        }
    }
}
