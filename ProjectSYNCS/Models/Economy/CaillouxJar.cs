namespace ProjectSYNCS.Models;

// The guild's jar (/jar): everyone pours cailloux in, and each goal reached unlocks the next reward
// in rotation for the whole server (Helpers/Economy/Jar). One row per guild.
//
// A gift only fills it. A goal reached leaves a reward pending (Level > RewardsDone), carried out
// afterwards by JarService.ResolveNextRewardAsync, one per save: a reward that fails stays pending
// for the next gift rather than being lost.
public class CaillouxJar
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }

    // How many goals have been reached; the current goal is Jar.GoalFor(Level).
    public int Level { get; set; }

    // Cailloux toward the current goal. A concurrency token: two gifts read the jar, the slower save
    // fails and re-applies on top, so a goal is never counted twice or missed.
    public long Filled { get; set; }

    // Every cailloux ever given, across all goals.
    public long Given { get; set; }

    // How many of the reached goals' rewards have been carried out. A concurrency token: two
    // resolvers racing for the same reward, the slower one retries and finds the next (or none).
    public int RewardsDone { get; set; }

    // The days (Paris yyyymmdd, inclusive, 0 = none) on which /daily pays double, and on which every
    // quiz window keeps its slot. A second unlock while one is pending adds the day after Until.
    public int DoubleDailyFrom { get; set; }
    public int DoubleDailyUntil { get; set; }
    public int QuizDayFrom { get; set; }
    public int QuizDayUntil { get; set; }
}

// One member's all-time total poured into their guild's jar: the card's top contributors.
public class JarGift
{
    public int Id { get; set; }
    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
    public long Total { get; set; }
}
