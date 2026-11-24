using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// /minecraft (MinecraftModule and Helpers/Minecraft), and the picker's narrow pick for game chat.
public static class MinecraftModuleChecks
{
    public static void Run()
    {
        // /minecraft (MinecraftModule and its pure pieces in Helpers/Minecraft).
        {
            string[] herNames = ["SYNCS", .. McCommand.HerNames];
            // help: every example parses as the order it illustrates; a talk example reads as talk; every page fits.
            foreach (var s in McHelp.Sections)
            foreach (var e in s.Examples)
            {
                var got = McCommand.Parse(e.Text, herNames, places: ["maison", "mine"]).Order;
                if (e.Order != McOrder.None && got != e.Order) Fail($"/minecraft help « {e.Text} »: parses as {got}, shown as {e.Order}");
                if (e.Order == McOrder.None && McTalkReader.Read(e.Text, McKin.Anyone, true, false, false, 1, null, new ResponsePicker(), new Random(1)) is null)
                    Fail($"/minecraft help « {e.Text} »: shown as talk, read as nothing");
            }
            for (var p = 0; p < ProjectSYNCS.Commands.MinecraftModule.HelpPages; p++)
            {
                var embed = ProjectSYNCS.Commands.MinecraftModule.BuildHelp(p);
                if (embed.Length > 6000 || embed.Fields.Any(f => f.Value.Length > 1024)) Fail($"/minecraft help page {p}: over the caps");
                Console.WriteLine($"/minecraft help page {p}: {embed.Length} chars, longest field {embed.Fields.Max(f => f.Value.Length)}");
            }

            // status: what she's doing, in a clause.
            foreach (var (order, who, place, expected) in new (McOrder?, string?, string?, string)[]
            {
                (McOrder.Fish, "Bob", null, "elle pêche pour Bob"),
                (McOrder.Follow, "Papa", null, "elle suit Papa"),
                (McOrder.Patrol, "Bob", "la mine", "elle garde la mine pour Bob"),
                (null, null, null, "elle ne fait rien de spécial"),
            })
                if (McDoing.Describe(order, who, place) != expected)
                    Fail($"McDoing.Describe({order}, {who}, {place}): « {McDoing.Describe(order, who, place)} », expected « {expected} »");
            foreach (var order in Enum.GetValues<McOrder>())
                if (string.IsNullOrWhiteSpace(McDoing.Describe(order, "Bob", null))) Fail($"McDoing.Describe({order}): empty");

            // « tu fais quoi ? » in game: her state in the first person, an order first, else her own life, else nothing.
            foreach (var order in Enum.GetValues<McOrder>())
                if (string.IsNullOrWhiteSpace(McDoing.FirstPerson(order, "Bob", "« mine »"))) Fail($"McDoing.FirstPerson({order}): empty");
            foreach (var (got, expected) in new (string?, string?)[]
            {
                (McDoing.FirstPerson(McOrder.Follow, "Papa", null), "je suis Papa"),
                (McDoing.FirstPerson(McOrder.Fish, "Bob", null), "je pêche pour Bob"),
                (McDoing.FirstPerson(McOrder.Follow, "Bob", null, toAsker: true), "je te suis"),
                (McDoing.FirstPerson(McOrder.Fish, "Bob", null, toAsker: true), "je pêche pour toi"),
                (McDoing.OwnLife(new("routine", "", "fish"), null, true), "je pêche un peu"),
                (McDoing.OwnLife(new("tagAlong", "papa", ""), "Papa", true), "je traîne avec Papa"),
                (McDoing.OwnLife(new("idle", "", ""), null, true), null),
                (McDoing.OwnLife(new("follow", "Bob", ""), "Bob", true), null),
                (McDoing.OwnLife(null, null, true), null),
                (McDoing.OwnLife(new("routine", "", "farm"), null, false), "elle s'occupe de la ferme (sa routine)"),
            })
                if (got != expected) Fail($"McDoing: « {got} », expected « {expected} »");
            // A whispered order asked about by someone else: its place stays out (the service passes no place).
            if (McDoing.FirstPerson(McOrder.GoToPlace, "Bob", null).Contains('«')) Fail("McDoing.FirstPerson: a place shown without one");
            foreach (var kind in new[] { "routine", "tagAlong", "forage", "retrieve", "danger", "sleep", "held", "seek" })
            foreach (var first in new[] { true, false })
                if (string.IsNullOrWhiteSpace(McDoing.OwnLife(new(kind, "Bob", kind == "danger" ? "fight" : kind == "routine" ? "home" : ""), "Bob", first)))
                    Fail($"McDoing.OwnLife({kind}, {first}): empty");

            // /debug minecraft « Voir son corps »: every field read and shown, a body that sends nothing of it still answered,
            // and the whole of it well inside Discord's 2000.
            {
                var full = System.Text.Json.JsonDocument.Parse("""
                    {"ev":"debug","pid":4242,"node":"v22.21.0","uptime":7384,"rss":143,"version":"26.1","pos":{"x":-1203,"y":64,"z":88},
                     "dimension":"the_nether","health":13.5,"food":17,"oxygen":20,
                     "task":{"kind":"fight","player":"AVeryLongPlayerName_","mode":"fallBack","own":false,"resume":"collect"},
                     "heldFor":540,"sitting":true,"eating":true,"sleeping":true,"swim":"dive","riding":true,"bridge":{"bridging":true,"standing":2},
                     "moving":true,"goal":"GoalFollow",
                     "loop":{"p50":11,"p99":148,"max":2210},
                     "compat":{"tickEnd":true,"renumberedActions":true,"corrections":1234567,"packetsPeak":482,"packetsPeakAt":1793016632919}}
                    """).RootElement;
                var text = McBodyReport.Format(full);
                foreach (var bit in new[] { "`-1203 64 88`", "`the_nether`", "13,5/20", "`fight` pour `AVeryLongPlayerName_`", "mode `fallBack`",
                             "reprend ensuite `collect`", "tenue sur place encore 9 min", "nage (`dive`)", "en bateau", "prête à poser un pont",
                             "2 blocs de pont à enlever", "en marche (`GoalFollow`)",
                             "max 2210 ms", "tick_end envoyés", "entity_action par nom", "1234567", "482/s", "2 h 03 min" })
                    if (!text.Contains(bit)) Fail($"McBodyReport: « {bit} » missing from the full report");
                if (text.Length > 1900) Fail($"McBodyReport: {text.Length} chars");
                var bare = McBodyReport.Format(System.Text.Json.JsonDocument.Parse("""{"ev":"debug"}""").RootElement);
                if (!bare.Contains("Tâche : aucune") || !bare.Contains("Position : inconnue")) Fail("McBodyReport: an empty report");
                Console.WriteLine($"/debug minecraft body view at worst: {text.Length} chars");
            }

            // link: a code works once, for the player it was issued for, within 10 minutes, and only the member's latest.
            var codes = new McLinkCodes();
            var lt0 = DateTimeOffset.UnixEpoch;
            var lc = codes.Issue(1, "Bob", lt0);
            if (lc.Length != 4 || !lc.All(char.IsDigit)) Fail("link code: not 4 digits");
            if (codes.Redeem("Alice", lc, lt0) is not null) Fail("link code: redeemed by another player");
            if (codes.Redeem("bob", lc, lt0.AddMinutes(11)) is not null) Fail("link code: redeemed after 10 minutes");
            var lc2 = codes.Issue(1, "Bob", lt0);
            var lc3 = codes.Issue(1, "Bob", lt0.AddSeconds(1));
            if (lc2 != lc3 && codes.Redeem("Bob", lc2, lt0.AddSeconds(2)) is not null) Fail("link code: an old code still works");
            if (codes.Redeem("BOB", lc3, lt0.AddSeconds(2)) != 1) Fail("link code: the right code, any case, refused");
            if (codes.Redeem("Bob", lc3, lt0.AddSeconds(3)) is not null) Fail("link code: used twice");
            if (McLinkCodes.ReadCode("lien 4821") != "4821" || McLinkCodes.ReadCode("Lien : 4821") != "4821"
                || McLinkCodes.ReadCode("syncs viens") is not null || McLinkCodes.ReadCode("lien 48211") is not null)
                Fail("link code: ReadCode");
            foreach (var (name, ok) in new[] { ("Bob_123", true), ("ab", false), ("Rodhengard", true), ("a b", false), ("§cBob", false),
                         ("abcdefghijklmnopq", false) })
                if (McLinkCodes.ValidName(name) != ok) Fail($"link: ValidName(« {name} ») should be {ok}");

            // invite: not allowed, then the owner's leave hold, then already in, then the server down, then already invited, else go.
            var inow = DateTimeOffset.UnixEpoch.AddDays(1);
            var never = DateTimeOffset.MinValue;
            foreach (var (allowed, up, inGame, hold, invited, expected) in new (bool, bool, bool, DateTimeOffset, DateTimeOffset, McInviteVerdict)[]
            {
                (false, true, false, never, never, McInviteVerdict.NotAllowed),
                (false, true, true, inow.AddMinutes(10), inow.AddMinutes(30), McInviteVerdict.NotAllowed),
                (true, true, false, inow.AddMinutes(10), never, McInviteVerdict.Held),
                (true, true, true, never, never, McInviteVerdict.AlreadyIn),
                (true, false, true, never, never, McInviteVerdict.AlreadyIn),
                (true, false, false, never, never, McInviteVerdict.ServerDown),
                (true, false, false, never, inow.AddMinutes(30), McInviteVerdict.ServerDown),
                (true, true, false, never, inow.AddMinutes(30), McInviteVerdict.AlreadyInvited),
                (true, true, false, never, inow.AddSeconds(-1), McInviteVerdict.Go),
            })
                if (McInvite.Decide(allowed, up, inGame, hold, invited, inow) != expected)
                    Fail($"McInvite.Decide({allowed}, up {up}, {inGame}, hold, invited): {McInvite.Decide(allowed, up, inGame, hold, invited, inow)}, expected {expected}");
            if (McInvite.Length != TimeSpan.FromHours(1)) Fail("McInvite.Length: an hour");
            // Her arrival credits the inviter only while the invite runs: an invite whose body never spawned is forgotten.
            if (McInvite.Inviter("Bob", inow.AddMinutes(5), inow) != "Bob") Fail("McInvite.Inviter: a running invite");
            if (McInvite.Inviter("Bob", inow.AddSeconds(-1), inow) is not null) Fail("McInvite.Inviter: an expired invite still credited");
            if (McInvite.Inviter(null, inow.AddMinutes(5), inow) is not null) Fail("McInvite.Inviter: nobody invited");

            // status: a whispered order's place or coordinates stay private.
            if (McDoing.ForStatus(McOrder.GoTo, "Bob", "120 64 -300", whispered: true).Contains("120"))
                Fail("McDoing.ForStatus: a whispered order's coordinates shown");
            if (!McDoing.ForStatus(McOrder.Patrol, "Bob", "la mine", whispered: false).Contains("la mine"))
                Fail("McDoing.ForStatus: a public order's place hidden");

            // stats: the tally adds, peeks without emptying, drains; the embed fits, empty or with many players.
            var tally = new McTally();
            tally.Add(McStats.Fish, 2);
            tally.Add(McStats.Fish, 3);
            tally.Add(McStats.Gathered("wood"), 20);
            if (tally.Peek().GetValueOrDefault(McStats.Fish) != 5) Fail("McTally: Add doesn't sum");
            if (tally.Peek().Count != 2) Fail("McTally: Peek emptied it");
            var drained = tally.Drain();
            if (drained.GetValueOrDefault(McStats.Gathered("wood")) != 20 || tally.Peek().Count != 0) Fail("McTally: Drain");
            if (McStats.OrdersBy("BoB") != "orders:bob") Fail("McStats.OrdersBy: lower case");
            var emptyStats = ProjectSYNCS.Commands.MinecraftModule.BuildStats(new Dictionary<string, long>(), 0, s => s);
            if (emptyStats.Length > 6000) Fail("/minecraft stats: empty embed over the cap");
            var crowd = Enumerable.Range(0, 50).ToDictionary(i => McStats.OrdersBy($"Player_{i:D2}_xxxxxx"), i => (long)i);
            foreach (var r in new[] { "wood", "stone", "iron", "coal", "gold", "diamond", "sand", "dirt" }) crowd[McStats.Gathered(r)] = 999_999;
            var crowded = ProjectSYNCS.Commands.MinecraftModule.BuildStats(crowd, 20, s => s);
            if (crowded.Length > 6000 || crowded.Fields.Any(f => f.Value.Length > 1024)) Fail("/minecraft stats: over the caps");
            if (crowded.Fields.Sum(f => f.Value.Split('\n').Count(l => l.Contains("player", StringComparison.OrdinalIgnoreCase))) != 5) Fail("/minecraft stats: not the top 5 players");
            if (!crowded.Fields.Any(f => f.Value.Contains(@"player\_49", StringComparison.OrdinalIgnoreCase))) Fail("/minecraft stats: the top player is missing");
        }

        // The picker's narrow pick (Minecraft's chat): a line comes back only once all but one of the others were said, and a
        // line repeated on purpose (a pool weighing its silence with empty lines) still leaves something to pick.
        {
            void Gaps(string[] pool, int minGap)
            {
                var talkPicker = new ResponsePicker();
                var last = new Dictionary<string, int>();
                for (var i = 0; i < 300; i++)
                {
                    var l = talkPicker.Pick(pool, 2);
                    if (last.TryGetValue(l, out var at) && i - at < minGap)
                    {
                        Fail($"ResponsePicker.Pick({pool.Length} lines, 2): « {l} » again after {i - at} picks");
                        return;
                    }
                    last[l] = i;
                }
                if (last.Count != pool.Distinct().Count()) Fail($"ResponsePicker.Pick({pool.Length} lines, 2): only {last.Count} lines ever said");
            }
            Gaps(["a", "b", "c", "d", "e", "f"], 5);
            Gaps(["a", "b", "c", "", "", ""], 4);
            Gaps(["a", "b"], 2);
        }
    }
}
