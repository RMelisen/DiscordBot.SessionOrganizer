using System.Reflection;

namespace ProjectSYNCS.Helpers;

public static class AppInfo
{
    // Set at build time from config.yaml (see ProjectSYNCS.csproj).
    public static string Version { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "?";

    // "7" out of "7.40.1" — for lines that brag about the generation, not the patch.
    public static string Major { get; } = Version.Split('.')[0];
}
