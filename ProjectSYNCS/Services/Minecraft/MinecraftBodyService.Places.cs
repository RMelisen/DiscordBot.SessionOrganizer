using System.Globalization;
using System.Text;
using System.Text.Json;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// The places players ask her to remember, by player, and her routine built on them. Part of MinecraftBodyService.
public sealed partial class MinecraftBodyService
{
    // ---- Remembered places ----------------------------------------------------------------------

    // Whose places are looked through, in order, for a place named without its player: the player's own first (each
    // has their own « maison »), then Papa's, Tata's and everyone else's by name, so the group's map still works for
    // someone who saved nothing. Without a player (her own trips, « rentre »): the family's first.
    private List<string> SearchOrderLocked(string? player)
    {
        var order = new List<string>();
        void Add(string who)
        {
            if (_places.ContainsKey(who) && !order.Contains(who, StringComparer.OrdinalIgnoreCase)) order.Add(who);
        }
        if (player is not null) Add(player);
        Add(_owner);
        Add(_tata);
        foreach (var who in _places.Keys.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)) Add(who);
        return order;
    }

    // Whose places may be named in chat (« la maison de Rodhengard »), by the name the parser looks for: everyone with a
    // place or on the server, and « Papa » and « Tata », as she calls them.
    private Dictionary<string, string> OwnerNamesLocked()
    {
        var owners = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var who in _places.Keys.Concat(_players).Where(w => w.Length > 0 && !Same(w, _username))) owners.TryAdd(who, who);
        if (_owner.Length > 0) owners.TryAdd("Papa", _owner);
        if (_tata.Length > 0) owners.TryAdd("Tata", _tata);
        return owners;
    }

    // A place by key, and whose it is; an empty key is home (« rentre »: hers, the family's), and a home word finds
    // whichever home that player saved. Of: the player named (« la maison de Rodhengard »), only theirs then. Key: what
    // was looked for, for the « I don't know it » line.
    private (McPlace Place, string Owner)? FindPlace(string player, string wanted, string? of, out string key)
    {
        lock (_gate)
        {
            key = wanted.Length > 0 ? wanted : McRoutine.HomeKeys[0];
            var home = wanted.Length == 0 || McRoutine.HomeKeys.Contains(wanted);
            foreach (var owner in of is not null ? [of] : SearchOrderLocked(wanted.Length > 0 ? player : null))
            {
                if (!_places.TryGetValue(owner, out var theirs)) continue;
                if (theirs.TryGetValue(key, out var exact)) return (exact, owner);
                if (home)
                    foreach (var homeKey in McRoutine.HomeKeys)
                        if (theirs.TryGetValue(homeKey, out var found)) return (found, owner);
            }
            return null;
        }
    }

    // The places her routine is built on (McRoutine), as spots: sent on spawn and whenever a place is saved, moved or
    // forgotten. The body decides when to go where.
    private void SendRoutine()
    {
        lock (_gate)
            if (_body is { } body && _spawned && !_leaving)
                SendRoutineLocked(body);
    }

    // Her own day is built on her family's map first: each kind of place is Papa's, else Tata's, else anyone's.
    private void SendRoutineLocked(MinecraftBodyProcess body)
    {
        McPlace? farm = null, fish = null, ranch = null, home = null;
        foreach (var owner in SearchOrderLocked(null))
        {
            var theirs = _places[owner];
            var kinds = McRoutine.Classify(theirs.Keys);
            farm ??= kinds.Farm is { } f ? theirs[f] : null;
            fish ??= kinds.Fish is { } fi ? theirs[fi] : null;
            ranch ??= kinds.Ranch is { } r ? theirs[r] : null;
            home ??= kinds.Home is { } h ? theirs[h] : null;
        }
        object? At(McPlace? p) => p is null ? null : new { x = p.X, y = p.Y, z = p.Z, dimension = p.Dimension };
        body.Send(new { op = "routine", farm = At(farm), fish = At(fish), ranch = At(ranch), home = At(home) });
    }

    // How her lines show a place's name.
    private static string Label(string name) => $"« {name} »";

    // A place in her lines to this player: with whose it is, unless it's theirs (« « maison » de Rodhengard »).
    private string Label(string name, string owner, string player) =>
        Same(owner, player) ? Label(name) : $"{Label(name)} {OfWhom(owner)}";

    // « de Papa », « d'Ethan ».
    private string OfWhom(string owner) =>
        Who(owner) is var who && who.Length > 0 && "aeiouyAEIOUY".Contains(who[0]) ? $"d'{who}" : $"de {who}";

    // Coordinates for her lines, with the dimension when it isn't the overworld (« 12 40 -8 dans le Nether »).
    private static string Spot(int x, int y, int z, string dimension) =>
        DimensionOf(dimension) is var d && d == "overworld" ? $"{x} {y} {z}" : $"{x} {y} {z} {BotResponses.McDimensionNames[d]}";

    // « overworld », « nether » or « end », from however the server names its dimension (« minecraft:the_nether »).
    private static string DimensionOf(string dimension) =>
        dimension.Contains("nether", StringComparison.OrdinalIgnoreCase) ? "nether"
        : dimension.Contains("end", StringComparison.OrdinalIgnoreCase) ? "end"
        : "overworld";

    // A place may be moved or forgotten by the player it belongs to, whoever saved it, or family: offline mode lets anyone
    // take any name, but a stranger shouldn't be able to wipe the group's map. Adding a place to someone else's map is
    // open, like any first save.
    private bool MayChange(McPlace place, string owner, string player) =>
        Same(owner, player) || Same(place.By, player) || KinOf(player) != McKin.Anyone;

    private void OnMarked(string player, int x, int y, int z, string dimension)
    {
        (string Key, string Name, string? Of, bool Private) mark;
        McPlace? existing = null;
        MinecraftState? state = null;
        string[] pool;
        string owner;
        lock (_gate)
        {
            if (!_marking.Remove(player, out mark)) return;
            owner = mark.Of ?? player;
            _places.TryGetValue(owner, out var theirs);
            theirs?.TryGetValue(mark.Key, out existing);
            if (existing is not null && !MayChange(existing, owner, player)) pool = BotResponses.McPlaceNotYoursLines;
            else if (existing is null && theirs?.Count >= McCommand.MaxPlaces) pool = BotResponses.McPlaceFullLines;
            else
            {
                if (theirs is null) _places[owner] = theirs = new Dictionary<string, McPlace>();
                theirs[mark.Key] = new McPlace(mark.Name, x, y, z, dimension, player);
                state = StateLocked();
                pool = existing is null ? BotResponses.McPlaceSavedLines : BotResponses.McPlaceMovedLines;
            }
        }
        if (state is not null)
        {
            SaveState(state);
            SendRoutine();
        }
        Reply(player, mark.Private, pool, Label(existing?.Name ?? mark.Name, owner, player), existing is null ? "" : Who(existing.By));
    }

    // Forgetting: the place by its exact name, the player's own first like any other (« oublie la mine » finds Papa's
    // when they have none, and only family may then forget it).
    private void ForgetPlace(string player, bool whispered, string text, string key, string? of)
    {
        McPlace? place = null;
        var owner = of ?? player;
        MinecraftState? state = null;
        lock (_gate)
        {
            foreach (var who in of is not null ? [of] : SearchOrderLocked(player))
            {
                if (!_places.TryGetValue(who, out var theirs) || !theirs.TryGetValue(key, out place)) continue;
                owner = who;
                if (MayChange(place, owner, player))
                {
                    theirs.Remove(key);
                    if (theirs.Count == 0) _places.Remove(who);
                    state = StateLocked();
                }
                break;
            }
        }
        if (state is not null)
        {
            SaveState(state);
            SendRoutine();
        }
        if (place is null) Reply(player, whispered, BotResponses.McPlaceUnknownLines, Label(McCommand.PlaceName(text, key), owner, player));
        else if (state is null) Reply(player, whispered, BotResponses.McPlaceNotYoursLines, Label(place.Name, owner, player), Who(place.By));
        else Reply(player, whispered, BotResponses.McPlaceForgottenLines, Label(place.Name, owner, player));
    }

    // The places she knows for this player, as many as fit a chat line: their own first, then the others they can send
    // her to, with whose they are. Of: one player's only (« liste les lieux de Rodhengard »).
    private void ListPlaces(string player, bool whispered, string? of)
    {
        List<string> names;
        lock (_gate)
            names = (of is not null ? [of] : SearchOrderLocked(player))
                .Where(_places.ContainsKey)
                .SelectMany(owner => _places[owner].Values.Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)
                    .Select(n => Same(owner, player) ? n : $"{n} {OfWhom(owner)}"))
                .ToList();
        if (names.Count == 0)
        {
            if (of is not null && !Same(of, player)) Reply(player, whispered, BotResponses.McPlaceListEmptyOfLines, Who(of));
            else Reply(player, whispered, BotResponses.McPlaceListEmptyLines);
            return;
        }
        var list = new StringBuilder();
        foreach (var name in names)
        {
            var next = (list.Length > 0 ? ", " : "") + name;
            if (list.Length + next.Length > ListMax)
            {
                list.Append('…');
                break;
            }
            list.Append(next);
        }
        Reply(player, whispered, BotResponses.McPlaceListLines, list.ToString());
    }

    // How long the list of places may be in a line, so the longest line still fits Minecraft's 256.
    public const int ListMax = 120;
}
