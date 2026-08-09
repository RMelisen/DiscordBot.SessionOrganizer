using Discord;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The avatar accessory a Components V2 <c>SectionBuilder</c> hangs off its side.
/// </summary>
/// <remarks>
/// A private member of <c>LevelModule</c> until <c>ShameModule</c> needed the same
/// thing — the same move <c>BotChat</c> and <c>EmoteMarkup</c> made when a second caller
/// appeared. Deliberately *not* in <see cref="LevelCardUi"/>, which holds the string
/// work and only the string work: this returns a Discord component.
/// </remarks>
public static class AvatarUi
{
    /// <summary>
    /// <paramref name="user"/>'s avatar, or Discord's default one when they cannot be
    /// resolved.
    /// </summary>
    /// <remarks>
    /// <para>Falls back rather than dropping the thumbnail — a Section whose accessory
    /// is missing renders lopsided against its neighbours. The person's
    /// <c>&lt;@id&gt;</c> still renders as a name either way, since the client resolves
    /// that itself.</para>
    /// <para><b>Reaching the fallback should now mean the member genuinely left the
    /// guild.</b> It used to also catch anyone simply missing from the gateway's member
    /// cache, which put Discord's generic blue logo on most of an all-time ranking:
    /// <c>Guild.GetUser</c> reads that cache, and it only held people seen in events
    /// since the last restart. <c>DiscordSocketConfig.AlwaysDownloadUsers</c> is now on
    /// for that reason — if these placeholders come back, check that flag before
    /// anything here.</para>
    /// <para>Note this is deliberately <b>not</b> <c>GetDefaultAvatarUrl()</c>: a user
    /// who merely has no custom picture never reaches this method, because
    /// <c>GetDisplayAvatarUrl</c> already returns their own correctly-coloured default.
    /// The literal <c>0</c> below is a placeholder for somebody we cannot resolve at
    /// all, which is why it is always the same blue.</para>
    /// </remarks>
    public static ThumbnailBuilder Thumbnail(IUser? user) =>
        new ThumbnailBuilder()
            .WithMedia(new UnfurledMediaItemProperties(
                user?.GetDisplayAvatarUrl(size: 128) ?? CDN.GetDefaultUserAvatarUrl(0)))
            .WithDescription("Avatar");
}
