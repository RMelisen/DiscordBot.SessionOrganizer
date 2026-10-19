using Discord.Interactions;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Where in time a counted message, voice minute or command lands (ActivityCounter): the Paris day
/// and hour, both from one zoned instant so midnight can't split them.
/// </summary>
public static class ActivityKeys
{
    public static (int Day, int Hour) DayHour(DateTimeOffset instant)
    {
        var zoned = AppTime.ToZoned(instant);
        return (zoned.Year * 10000 + zoned.Month * 100 + zoned.Day, zoned.Hour);
    }
}

/// <summary>
/// The key a command is counted under (CommandDailyStat.Command, a stored string: append-only).
/// A slash command is its full path without the slash (« config excluded-channels add »); /pl is
/// folded into /plynling, its second registration. A typed shortcut starts with « ; ».
/// </summary>
public static class CommandPath
{
    public static string Join(IEnumerable<string> groups, string name)
    {
        var parts = groups.Append(name).ToList();
        if (parts.Count > 1 && parts[0] == "pl") parts[0] = "plynling";
        return string.Join(' ', parts);
    }

    public static string Of(SlashCommandInfo command)
    {
        var groups = new List<string>();
        for (var module = command.Module; module is not null; module = module.Parent)
            if (module.IsSlashGroup) groups.Add(module.SlashGroupName);
        groups.Reverse();
        return Join(groups, command.Name);
    }

    public static string Shortcut(ShortcutKind kind) => kind switch
    {
        ShortcutKind.View => ";pl",
        ShortcutKind.Work => ";work",
        ShortcutKind.Forage => ";forage",
        ShortcutKind.Visit => ";pl visit",
        _ => ";pl ?",
    };
}
