namespace ProjectSYNCS.Models;

// A person an admin trusted with the /shame vote, on top of the staff, the moderator
// role and the hardcoded ShameModule.ExtraVoters. One row per (guild, user).
//
// Holds only the admin-added ids: the hardcoded voters are the floor, never written here,
// so no command can revoke a right the code grants.
public class GuildShameVoter
{
    public int Id { get; set; }

    public ulong GuildId { get; set; }
    public ulong UserId { get; set; }
}
