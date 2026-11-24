using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// Minecraft talk (McTalkReader, McChat.Game), and a call always getting words.
public static class McTalkChecks
{
    public static void Run(string[] args)
    {
        // Talk in game (McTalkReader): chat that isn't an order, read as Discord reads it, answered from the Discord pools made
        // fit for the game (McChat.Game). `--mc-review` lists the Discord lines left out of the game, and why (« - »), then every line as she says it in game (« + »).
        string[] TalkPool(string field) => (string[])Field(field);
        string[] IntentPool(string field, string intent) =>
            (string[])((IDictionary)Field(field))[Enum.Parse(intentType, intent)]!;
        var talkPicker = new ResponsePicker();
        var talkRandom = new Random(1);

        void SaysInGame(string text, McKin kin, string? kind, string[]? pool = null, bool call = false, bool listening = false,
            bool whispered = false, bool remark = false)
        {
            var got = McTalkReader.Read(text, kin, call, listening, whispered, McTalkReader.AskerId("Bob", kin), null, talkPicker, talkRandom);
            var said = got is { } t ? $"{t.Kind}{(t.Remark ? " (remark)" : "")}" : "nothing";
            if (kind is null ? got is not null
                : got is not { } g || g.Kind != kind || g.Remark != remark || (pool is not null && !ReferenceEquals(g.Pool, pool)))
                Fail($"talk « {text} » ({kin}{(call ? ", call" : "")}{(listening ? ", listening" : "")}{(whispered ? ", whispered" : "")}): got {said}, expected {kind ?? "nothing"}");
        }
        // A call stays a call: « oui ? » comes from the service.
        SaysInGame("syncs", McKin.Anyone, null, call: true);
        SaysInGame("hé syncs ?", McKin.Anyone, null, call: true);
        // Said to her: the intent, else the mood.
        SaysInGame("syncs ça va ?", McKin.Anyone, "HowAreYou", IntentPool("IntentLines", "HowAreYou"), call: true);
        SaysInGame("syncs je t'aime", McKin.Anyone, "Love", IntentPool("IntentLines", "Love"), call: true);
        SaysInGame("je t'aime syncs", McKin.Papa, "Love", IntentPool("OwnerIntentLines", "Love"), call: true);
        SaysInGame("syncs t'es nulle", McKin.Anyone, "comeback", TalkPool("Comebacks"), call: true);
        SaysInGame("syncs t'es nulle", McKin.Papa, "papa mean", TalkPool("OwnerMeanReplies"), call: true);
        SaysInGame("syncs raconte une blague", McKin.Anyone, "Joke", call: true);
        SaysInGame("syncs tu penses quoi de minecraft ?", McKin.Anyone, "Opinion", TalkPool("OpinionLines"), call: true);
        SaysInGame("syncs tu peux me construire une maison ?", McKin.Anyone, "Request", TalkPool("McCantDoLines"));
        SaysInGame("syncs t'es un bot ?", McKin.Anyone, "AreYouABot", call: true);
        // What wears her patience in game, as on Discord.
        foreach (var (said, costs) in new[]
        {
            ("syncs t'es nulle", true), ("syncs ok", true), ("syncs le boss a raté son attaque hier", true),
            ("syncs ça va ?", false), ("syncs merci", false), ("syncs tu viens quand ?", false),
        })
            if (McTalkReader.Read(said, McKin.Anyone, false, false, true, 1, null, talkPicker, talkRandom) is not { } read || read.Wears != costs)
                Fail($"talk « {said} » wears her patience: expected {costs}");
        SaysInGame("inabot viens voir, syncs", McKin.Anyone, "inabot");
        // About her, not to her: a remark, rationed by the service.
        SaysInGame("syncs a pris mon fer", McKin.Anyone, "talked about", TalkPool("TalkedAboutLines"), remark: true);
        SaysInGame("je joue avec syncs", McKin.Anyone, "talked about", remark: true);
        // Right after she answered: only what's plainly for her, without her name.
        SaysInGame("je t'aime", McKin.Anyone, "Love", listening: true);
        SaysInGame("mdr", McKin.Anyone, "Laughter", listening: true);
        SaysInGame("t'es nul", McKin.Anyone, null, listening: true);
        SaysInGame("on va miner ?", McKin.Anyone, null, listening: true);
        SaysInGame("bonjour", McKin.Anyone, null);
        // A whisper is to her, named or not.
        SaysInGame("ça va ?", McKin.Anyone, "HowAreYou", whispered: true);
        // A refused order is no talk.
        if (!McCommand.Parse("syncs viens pas", ["SYNCS", .. McCommand.HerNames]).Refused) Fail("« syncs viens pas » isn't a refusal");
        if (McCommand.Parse("syncs ça va", ["SYNCS", .. McCommand.HerNames]).Refused) Fail("« syncs ça va » read as a refusal");
        if (!McTalkReader.IsHerName("SYNCS") || McTalkReader.IsHerName("Bob")) Fail("McTalkReader.IsHerName");
        if (McTalkReader.AskerId("Bob", McKin.Anyone) != McTalkReader.AskerId("bob", McKin.Anyone)) Fail("AskerId depends on case");

        // What the game keeps of a Discord line.
        foreach (var (line, expected) in new (string, string?)[]
        {
            ("Je note." + Sticker.Jenote, "Je note."),
            ("Je sais pas gérer ça (>⩊<)" + Sticker.Flustered, "Je sais pas gérer ça"),
            ("Enfin, avec `/schedule create`." + Sticker.Smug, null),
            ("Pourquoi tu me tag ?", null),
            ("Va voir /level.", null),
            ("C'est **moi** la meilleure ✨", "C'est moi la meilleure"),
            ("Ok (enfin presque)", "Ok (enfin presque)"),
            // Her Discord systems, and having no body, which she has in game.
            ("Je compte vos XP. C'est passionnant.", null),
            ("Encore un et je commence à facturer. En cailloux.", null),
            ("Plus que tous les good bot du monde.", null),
            ("Ça va. Toujours pas de bras, mais à part ça, tout va bien.", null),
            ("À plus, {0}. Moi, je bouge pas. Je bouge jamais.", null),
            ("« Mon œil. » J' ai même pas d'œil.", null),
            ("Je veille sur Ping-Qilin. Elle dort.", "Je veille sur Ping-Qilin. Elle dort."),
            ("Inabot ?! Bouge pas ...", "Inabot ?! Bouge pas ..."),
        })
            if (McChat.Line(line) != expected) Fail($"McChat.Line « {line} »: got « {McChat.Line(line) ?? "null"} », expected « {expected ?? "null"} »");

        // Every pool she can answer with in game: something left, every line plain game chat, faces on a quarter at most.
        var talkPools = new[]
        {
            "IntentLines", "OwnerIntentLines", "TataIntentLines", "AboutHerLines", "HowAreYouTempLines", "OpinionLines", "PreferLines",
            "ChoiceLines", "YesLines", "NoLines", "MorningFunFacts", "Comebacks", "OwnerComebacks", "OwnerMeanReplies", "NiceReplies",
            "Greetings", "OwnerGreetings", "TataReplies", "SharingShrugLines", "TalkedAboutLines", "TalkedAboutOwnerLines",
            "BackbiteRoasts", "MistakenIdentityReplies", "ShutdownThreatOwner", "ShutdownThreatTata", "ShutdownThreatReplies",
            "OwnerRaspberryNameLines", "OwnerChildNameLines", "StreakEndLines", "StreakNudgeLines", "McCantDoLines",
        };
        var mcReview = args.Contains("--mc-review");
        var (talkTotal, talkKept) = (0, 0);
        foreach (var f in talkPools)
        foreach (var (name, lines) in Pools(f))
        {
            // Never said in game (McTalkReader leaves these intents to Discord): their lines are slash commands.
            if (name is "IntentLines[NextSession]" or "IntentLines[MyStats]") continue;
            var game = McChat.Game(lines);
            talkTotal += lines.Length;
            talkKept += game.Length;
            if (game.Length == 0) Fail($"{name}: nothing left in game");
            if (game.Count(McChat.HasFace) * 4 > Math.Max(game.Length, 4)) Fail($"{name}: too many faces in game");
            foreach (var l in game)
            {
                var s = string.Format(l, "Abcdefghijklmnop", "mercredi", "x", "y");
                if (s.Length > 256 || Sticker.Strip(s) != s || s.Contains('{') || s.Contains('}') || s.Contains('✨') || s.Contains('<')
                    && s.Contains(':') || !McChat.Drawable(s))
                    Fail($"{name}: not plain game chat: {l}");
            }
            if (mcReview)
            {
                foreach (var l in lines.Where(l => McChat.Line(l) is null))
                    Console.WriteLine($"- [{name}] {McChat.WhyNot(McChat.Clean(l))}: {Sticker.Strip(l)}");
                foreach (var l in game) Console.WriteLine($"+ [{name}] {l}");
            }
        }
        // The verdict an opinion quotes is cleaned at send time: nothing Discord-only may survive it.
        foreach (var f in new[] { "RateHerselfLines", "RateOwnerLines", "RateOwnerSelfLines", "RateCrushLines", "RateNemesisLines",
                     "RateDevilLines", "RateGodLines", "RateTataLines", "RateTataSelfLines", "RateRoutierLines", "RateMitrailletteLines" })
        foreach (var l in TalkPool(f))
            if (McChat.Clean(l) is var c && (c.Contains('`') || c.Contains("<:") || c.Contains("<@") || !McChat.Drawable(c)))
                Fail($"{f}: verdict not plain game chat: {c}");
        Console.WriteLine($"Minecraft talk: {talkKept} of {talkTotal} Discord lines usable in game.");

        // A call always gets words: an empty line there is a call she seems not to have heard (no « oui ? », no wave).
        foreach (var f in new[] { "McCalledLines", "McCalledLinesOwner", "McCalledLinesTata" })
            if (((string[])Field(f)).Any(l => l.Trim().Length == 0)) Fail($"{f}: an empty line, a call left unanswered");
    }
}
