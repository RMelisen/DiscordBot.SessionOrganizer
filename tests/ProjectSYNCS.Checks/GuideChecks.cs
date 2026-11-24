using System.Reflection;
using System.Text.RegularExpressions;
using Discord.Interactions;
using ProjectSYNCS.Commands;

// 69. The online user guide (guide/, published by .github/workflows/guide.yml) names every slash
// command a member or staff can use, written as its full path (`/admin xp add`). The guide is
// hand-written like /help and README.md, so this is what catches a new command shipped without
// its page. Owner-only commands and the /pl alias are deliberately absent.
public static class GuideChecks
{
    // Never in the guide: owner-only tools, and the /pl shortcut (same subcommands as /plynling,
    // mentioned by name on the Plynling page but not command by command).
    private static readonly string[] Excluded = { "/debug", "/pl" };

    public static void Run(Action<string> fail)
    {
        var folder = Path.Combine(StoryChecks.RepoRoot(), "guide");
        if (!Directory.Exists(folder)) { fail("guide: no guide/ folder"); return; }
        var text = string.Join("\n", Directory.EnumerateFiles(folder, "*.md", SearchOption.AllDirectories).Select(File.ReadAllText));

        var commands = AllCommandPaths().ToList();
        if (commands.Count < 50) fail($"guide: only {commands.Count} commands found by reflection, the scan is broken");

        foreach (var path in commands)
        {
            if (Excluded.Any(e => path == e || path.StartsWith(e + " "))) continue;
            if (!Regex.IsMatch(text, Regex.Escape(path) + @"(?![\w-])"))
                fail($"guide: {path} is never written out in guide/ (document it, full path in inline code)");
        }
    }

    // Every registered slash command as a full path: the [Group] names of its module and the
    // modules enclosing it, then the command. Inherited commands count (PlynlingLongModule gets
    // its subcommands from PlynlingModule).
    public static IEnumerable<string> AllCommandPaths()
    {
        var modules = typeof(HelpModule).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IInteractionModuleBase).IsAssignableFrom(t));
        foreach (var type in modules)
        {
            var groups = new List<string>();
            for (var t = type; t != null; t = t.DeclaringType)
                if (t.GetCustomAttribute<GroupAttribute>(inherit: false) is { } g) groups.Insert(0, g.Name);

            foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                if (m.GetCustomAttribute<SlashCommandAttribute>() is { } cmd)
                    yield return "/" + string.Join(" ", groups.Append(cmd.Name));
        }
    }
}
