namespace ProjectSYNCS.Models;

// Per-guild configuration a server admin can change at runtime, without a redeploy.
// One row per guild, created lazily the first time something is configured.
//
// **Nothing here can take away what the code grants.** The hardcoded lists
// (XpTracker.ExcludedChannels, ShameModule.ExtraVoters) stay in force whether or not a
// row exists, so configuring something can never silently revoke access or un-exclude a
// channel. The channel ids below *replace* a hardcoded destination instead, which grants
// and revokes nothing. An unconfigured guild behaves exactly as it did before this table
// existed, which is what makes shipping this a no-op for any server that ignores it.
public class GuildSettings
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }

    /// <summary>
    /// A role whose holders may cast `/shame` votes, on top of the staff and the
    /// hardcoded voter list. Zero means unconfigured — the same "absent snowflake"
    /// convention <see cref="SessionEvent.NativeEventId"/> uses, rather than a nullable
    /// ulong, which would be the only one in this schema and needs a conversion nothing
    /// else here exercises.
    /// </summary>
    public ulong ModeratorRoleId { get; set; }

    /// <summary>
    /// Where Plynling announcements and event stories go. Zero means the hardcoded
    /// <c>PlynlingAnnouncer.DefaultGameChannelId</c>, when it belongs to this guild.
    /// Unlike the lists, a configured channel *replaces* the default: a destination grants
    /// or revokes nothing.
    /// </summary>
    public ulong GameChannelId { get; set; }

    /// <summary>
    /// Her main channel — morning hello, the 3 a.m. line, ghost typing, the wake-up line.
    /// Zero means the hardcoded <c>MorningGreetingService.DefaultChannelId</c>. Read for
    /// the home guild only.
    /// </summary>
    public ulong MainChannelId { get; set; }
}
