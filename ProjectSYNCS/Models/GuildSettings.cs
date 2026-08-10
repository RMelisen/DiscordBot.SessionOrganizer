namespace ProjectSYNCS.Models;

// Per-guild configuration a server admin can change at runtime, without a redeploy.
// One row per guild, created lazily the first time something is configured.
//
// **Everything here is additive to what the code already hardcodes, never a
// replacement for it.** The hardcoded lists (XpTracker.ExcludedChannels,
// ShameModule.ExtraVoters) stay in force whether or not a row exists, so configuring
// something can never silently revoke access or un-exclude a channel. An unconfigured
// guild therefore behaves exactly as it did before this table existed, which is what
// makes shipping this a no-op for any server that ignores it.
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
}
