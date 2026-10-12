using System.Reflection;

namespace ProjectSYNCS.Helpers;

public static class AppInfo
{
    // Set at build time from config.yaml (see ProjectSYNCS.csproj).
    public static string Version { get; } =
        Assembly.GetExecutingAssembly()
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? "?";
}
