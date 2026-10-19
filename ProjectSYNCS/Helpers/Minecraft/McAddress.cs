using System.Net;
using DnsClient;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// Where a Minecraft address really points. A server given as a bare name with no port (most hosts,
/// G-Portal included) publishes its port in an SRV record, <c>_minecraft._tcp.&lt;name&gt;</c>, which
/// the game's client looks up. The status ping needs the same lookup or it knocks on 25565 and finds
/// nothing.
/// </summary>
/// <remarks>
/// Mirrors what the client library does for the body (minecraft-protocol's <c>tcp_dns</c>): the lookup
/// only happens on the default port, for a name that isn't an IP or localhost, and anything that goes
/// wrong falls back to the address as given. The body gets the address as given and does its own
/// lookup, so its handshake carries the same host a player's client would.
/// </remarks>
public static class McAddress
{
    public const int DefaultPort = 25565;

    // Thread-safe, and caches answers for their TTL: one instance for the process.
    private static readonly LookupClient Dns = new(new LookupClientOptions { Timeout = TimeSpan.FromSeconds(3), Retries = 1 });

    /// <summary>The host and port to connect to for <paramref name="host"/>:<paramref name="port"/>.</summary>
    public static async Task<(string Host, int Port)> ResolveAsync(string host, int port, CancellationToken ct)
    {
        if (!NeedsLookup(host, port)) return (host, port);
        try
        {
            var answer = await Dns.QueryAsync($"_minecraft._tcp.{host}", QueryType.SRV, cancellationToken: ct);
            var record = answer.Answers.SrvRecords().OrderBy(r => r.Priority).ThenByDescending(r => r.Weight).FirstOrDefault();
            if (record is null) return (host, port);
            var target = record.Target.Value.TrimEnd('.');
            return (target.Length > 0 ? target : host, record.Port);
        }
        catch (Exception ex) when (ex is DnsResponseException or OperationCanceledException or System.Net.Sockets.SocketException
                                       or ArgumentException)
        {
            if (ct.IsCancellationRequested) throw;
            return (host, port);
        }
    }

    /// <summary>Whether the address is the kind a client resolves through SRV: a name, on the default port.</summary>
    public static bool NeedsLookup(string host, int port) =>
        port == DefaultPort && !IPAddress.TryParse(host, out _) && !host.Equals("localhost", StringComparison.OrdinalIgnoreCase);
}
