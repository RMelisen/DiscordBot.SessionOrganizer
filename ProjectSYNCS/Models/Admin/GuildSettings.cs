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

    /// <summary>
    /// Where her pop quiz goes. Zero means off: unlike the channels above there is no
    /// hardcoded default, so a server only gets quizzes once staff picks a channel.
    /// </summary>
    public ulong QuizChannelId { get; set; }

    /// <summary>
    /// The role SYNCS keeps on everyone with a living Plynling, and off everyone else.
    /// Zero means none: no role is managed. She only adds and removes it — name, colour
    /// and position stay staff's (see PlynlingRoleService).
    /// </summary>
    public ulong PlynlingRoleId { get; set; }

    /// <summary>
    /// The « Grosse tête de la semaine » role: held by last calendar week's best quiz answerer(s).
    /// Zero means none, and the whole crown is off (QuizCrownService).
    /// </summary>
    public ulong QuizRoleId { get; set; }

    /// <summary>
    /// The Monday (yyyymmdd, AppTime.DayKey) starting the last week whose crown was announced.
    /// Saved before the announcement, so a restart never announces twice. Zero means never.
    /// </summary>
    public int QuizCrownedWeek { get; set; }

    /// <summary>
    /// Where her unprompted, low-stakes posts go (Quokka's visits). Zero means the hardcoded
    /// <c>BotsChannel.DefaultId</c>, when it belongs to this guild — the same « default » contract
    /// as GameChannelId.
    /// </summary>
    public ulong BotsChannelId { get; set; }

    /// <summary>
    /// Where the level-up cards go. Zero means the bots channel (see <c>BotsChannel</c>), so a
    /// guild that never sets it keeps them wherever its bots channel is.
    /// </summary>
    public ulong LevelUpChannelId { get; set; }
}
