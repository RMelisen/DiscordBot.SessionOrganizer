using Discord;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Sends or edits a Components V2 card together with the pictures it shows by <c>attachment://</c>
/// (a dressed Plynling, <see cref="PlynlingArt.SpriteOf"/>), so no send path forgets them: the files
/// come from <see cref="PlynlingPainter.FilesFor"/> and are disposed once sent. A new send uses the
/// files call only when there is a file; an edit always sets <c>Attachments</c>, even to nothing, so a
/// picture taken off never lingers. Every card goes out with the V2 flag and, unless told otherwise,
/// <see cref="AllowedMentions.None"/>.
/// </summary>
public static class CardSend
{
    public static Task RespondCardAsync(this IDiscordInteraction interaction, MessageComponent card, bool ephemeral = false,
        AllowedMentions? allowedMentions = null) =>
        WithFilesAsync(card, files => files.Count > 0
            ? interaction.RespondWithFilesAsync(files, components: card, ephemeral: ephemeral,
                allowedMentions: allowedMentions ?? AllowedMentions.None, flags: MessageFlags.ComponentsV2)
            : interaction.RespondAsync(components: card, ephemeral: ephemeral,
                allowedMentions: allowedMentions ?? AllowedMentions.None, flags: MessageFlags.ComponentsV2));

    public static Task FollowupCardAsync(this IDiscordInteraction interaction, MessageComponent card, bool ephemeral = false) =>
        WithFilesAsync(card, files => files.Count > 0
            ? interaction.FollowupWithFilesAsync(files, components: card, ephemeral: ephemeral,
                allowedMentions: AllowedMentions.None, flags: MessageFlags.ComponentsV2)
            : interaction.FollowupAsync(components: card, ephemeral: ephemeral,
                allowedMentions: AllowedMentions.None, flags: MessageFlags.ComponentsV2));

    /// <summary>The deferred or earlier response, rewritten in place.</summary>
    public static Task ModifyCardAsync(this IDiscordInteraction interaction, MessageComponent card) =>
        WithFilesAsync(card, files => interaction.ModifyOriginalResponseAsync(m => Fill(m, card, files)));

    /// <summary>A posted message, rewritten in place.</summary>
    public static Task ModifyCardAsync(this IMessageChannel channel, ulong messageId, MessageComponent card) =>
        WithFilesAsync(card, files => channel.ModifyMessageAsync(messageId, m => Fill(m, card, files)));

    public static Task SendCardAsync(this IMessageChannel channel, MessageComponent card, MessageReference? reply = null) =>
        WithFilesAsync(card, files => files.Count > 0
            ? channel.SendFilesAsync(files, components: card, allowedMentions: AllowedMentions.None, messageReference: reply,
                flags: MessageFlags.ComponentsV2)
            : channel.SendMessageAsync(components: card, allowedMentions: AllowedMentions.None, messageReference: reply,
                flags: MessageFlags.ComponentsV2));

    // Re-asserted on every edit: an update without the flag is rejected on a V2 message.
    private static void Fill(MessageProperties m, MessageComponent card, IReadOnlyList<FileAttachment> files)
    {
        m.Components = card;
        m.Flags = MessageFlags.ComponentsV2;
        m.AllowedMentions = AllowedMentions.None;
        m.Attachments = new Optional<IEnumerable<FileAttachment>>(files);
    }

    private static async Task WithFilesAsync(MessageComponent card, Func<IReadOnlyList<FileAttachment>, Task> send)
    {
        var files = PlynlingPainter.FilesFor(card);
        try
        {
            await send(files);
        }
        finally
        {
            foreach (var file in files) file.Dispose();
        }
    }
}
