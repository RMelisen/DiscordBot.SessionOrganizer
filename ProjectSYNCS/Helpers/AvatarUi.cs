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
    /// A member who has since left the guild resolves to null, so fall back rather than
    /// dropping the thumbnail — a Section whose accessory is missing renders lopsided
    /// against its neighbours. Their <c>&lt;@id&gt;</c> still renders as a name, since
    /// the client resolves that itself.
    /// </remarks>
    public static ThumbnailBuilder Thumbnail(IUser? user) =>
        new ThumbnailBuilder()
            .WithMedia(new UnfurledMediaItemProperties(
                user?.GetDisplayAvatarUrl(size: 128) ?? CDN.GetDefaultUserAvatarUrl(0)))
            .WithDescription("Avatar");
}
