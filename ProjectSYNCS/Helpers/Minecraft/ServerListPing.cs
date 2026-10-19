using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace ProjectSYNCS.Helpers;

/// <summary>What the server's status answer says: who is on, as far as it tells.</summary>
/// <param name="Online">The player count the server reports.</param>
/// <param name="Sample">
/// The names it lists. Vanilla lists at most 12, and a player who turned off « Allow Server
/// Listings » shows as « Anonymous Player », so this can be shorter than <paramref name="Online"/>.
/// </param>
public sealed record ServerStatus(int Online, int Max, IReadOnlyList<string> Sample, string Version);

/// <summary>
/// The Minecraft Java status ping (the one the server list does): a handshake and a status request
/// over TCP, answered with a JSON document. Needs nothing on the server, works on any host, and is
/// how SYNCS knows whether the server is up and who is playing before she joins.
/// </summary>
public static class ServerListPing
{
    // Any protocol number gets a status answer; -1 is the "just asking" convention.
    private const int ProtocolVersion = -1;
    private const int MaxResponseBytes = 1 << 20;

    /// <summary>The status, or null when the server doesn't answer properly within <paramref name="timeout"/>.</summary>
    public static async Task<ServerStatus?> QueryAsync(string host, int port, TimeSpan timeout, CancellationToken ct)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(timeout);
        try
        {
            using var tcp = new TcpClient();
            await tcp.ConnectAsync(host, port, cts.Token);
            var stream = tcp.GetStream();

            await stream.WriteAsync(Handshake(host, port), cts.Token);
            await stream.WriteAsync(new byte[] { 0x01, 0x00 }, cts.Token); // status request: length 1, id 0

            var length = await ReadVarIntAsync(stream, cts.Token);
            if (length <= 0 || length > MaxResponseBytes) return null;
            var packet = new byte[length];
            await stream.ReadExactlyAsync(packet, cts.Token);

            var offset = 0;
            if (ReadVarInt(packet, ref offset) != 0x00) return null;
            var jsonLength = ReadVarInt(packet, ref offset);
            if (jsonLength < 0 || offset + jsonLength > packet.Length) return null;
            return Parse(Encoding.UTF8.GetString(packet, offset, jsonLength));
        }
        catch (Exception ex) when (ex is SocketException or IOException or OperationCanceledException or EndOfStreamException
                                       or InvalidDataException)
        {
            if (ct.IsCancellationRequested) throw;
            return null;
        }
    }

    /// <summary>The status JSON's players and version; null if it isn't a status document.</summary>
    public static ServerStatus? Parse(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (!root.TryGetProperty("players", out var players)) return null;

            var online = players.TryGetProperty("online", out var o) && o.TryGetInt32(out var on) ? on : 0;
            var max = players.TryGetProperty("max", out var m) && m.TryGetInt32(out var mx) ? mx : 0;
            var sample = new List<string>();
            if (players.TryGetProperty("sample", out var s) && s.ValueKind == JsonValueKind.Array)
                foreach (var p in s.EnumerateArray())
                    if (p.TryGetProperty("name", out var n) && n.GetString() is { Length: > 0 } name)
                        sample.Add(name);
            var version = root.TryGetProperty("version", out var v) && v.TryGetProperty("name", out var vn) ? vn.GetString() ?? "" : "";
            return new ServerStatus(online, max, sample, version);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static byte[] Handshake(string host, int port)
    {
        var body = new List<byte>();
        WriteVarInt(body, 0x00); // packet id
        WriteVarInt(body, ProtocolVersion);
        var hostBytes = Encoding.UTF8.GetBytes(host);
        WriteVarInt(body, hostBytes.Length);
        body.AddRange(hostBytes);
        body.Add((byte)(port >> 8));
        body.Add((byte)port);
        WriteVarInt(body, 1); // next state: status

        var packet = new List<byte>();
        WriteVarInt(packet, body.Count);
        packet.AddRange(body);
        return packet.ToArray();
    }

    private static void WriteVarInt(List<byte> buffer, int value)
    {
        var v = (uint)value;
        do
        {
            var b = (byte)(v & 0x7F);
            v >>= 7;
            if (v != 0) b |= 0x80;
            buffer.Add(b);
        } while (v != 0);
    }

    private static int ReadVarInt(byte[] data, ref int offset)
    {
        int value = 0, shift = 0;
        while (true)
        {
            if (offset >= data.Length || shift >= 35) throw new InvalidDataException("bad varint");
            var b = data[offset++];
            value |= (b & 0x7F) << shift;
            if ((b & 0x80) == 0) return value;
            shift += 7;
        }
    }

    private static async Task<int> ReadVarIntAsync(Stream stream, CancellationToken ct)
    {
        int value = 0, shift = 0;
        var one = new byte[1];
        while (true)
        {
            if (shift >= 35) throw new InvalidDataException("bad varint");
            await stream.ReadExactlyAsync(one, ct);
            value |= (one[0] & 0x7F) << shift;
            if ((one[0] & 0x80) == 0) return value;
            shift += 7;
        }
    }
}
