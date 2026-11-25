namespace ProjectSYNCS.Helpers;

/// <summary>One feature a server can switch on or off: its stored key, its French name, what it is.</summary>
/// <param name="Key">Stored in <c>GuildFeatureSetting.Feature</c>: append-only, never renamed.</param>
/// <param name="NeedsMainChannel">Whether it posts in the main channel (and so does nothing without one).</param>
public sealed record ServerFeature(string Key, string Label, string Description, bool NeedsMainChannel);

// The home features other servers can have (the owner's call: every one toggleable, on the home
// server too). A server stores only the choices staff made (GuildFeatureSetting); with no row, a
// feature takes its default: **on at home** (so an untouched home server behaves as before) and
// **off elsewhere**, except the member welcome, which already ran everywhere. Pure, so the checks
// hold the keys and the defaults.
//
// Not here, on purpose: her voice seat, Voyager's letters and the monthly digest — her
// single-instance life, home only, no switch.
public static class ServerFeatures
{
    // Keys are stored strings: append-only (root CLAUDE.md, Data).
    public const string Morning = "morning";
    public const string Ambient = "ambient";
    public const string Quokka = "quokka";
    public const string WeekendPoll = "weekend-poll";
    public const string VoiceSpectator = "voice-spectator";
    public const string Welcome = "welcome";

    public static readonly IReadOnlyList<ServerFeature> All = new ServerFeature[]
    {
        new(Morning, "Bonjour du matin", "un bonjour et une anecdote chaque matin, et `/shoutout`", true),
        new(Ambient, "Vie ambiante", "quelques mots quand le serveur dort, un peu de vie la nuit", true),
        new(WeekendPoll, "Sondage du week-end", "son sondage du mercredi pour le week-end", true),
        new(Quokka, "Visites de Quokka", "il passe parfois dans le salon des bots", false),
        new(VoiceSpectator, "Spectatrice en vocal", "un mot dans le salon vocal après une longue soirée", false),
        new(Welcome, "Accueil des nouveaux", "elle répond au message d'arrivée de Discord", false),
    };

    public static ServerFeature? Find(string key) => All.FirstOrDefault(f => f.Key == key);

    /// <summary>A feature's value on a server where staff never chose.</summary>
    public static bool DefaultFor(string key, bool isHome) => isHome || key == Welcome;

    /// <summary>Whether a feature is on: the stored choice, else the default.</summary>
    public static bool IsOn(IReadOnlyDictionary<string, bool> stored, string key, ulong guildId) =>
        IsOn(stored, key, HomeGuild.Is(guildId));

    public static bool IsOn(IReadOnlyDictionary<string, bool> stored, string key, bool isHome) =>
        stored.TryGetValue(key, out var on) ? on : DefaultFor(key, isHome);
}
