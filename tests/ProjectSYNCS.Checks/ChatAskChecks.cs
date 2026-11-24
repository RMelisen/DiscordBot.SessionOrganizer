using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 21b–22. The schedule, numbers, dice and the other small asks, and Papa's pet names.
public static class ChatAskChecks
{
    public static void Run()
    {
        // 21b. The schedule, their numbers, dice, « chut », « qui a raison ? », « note-moi », « excuse-toi »,
        // with the chat each must leave alone.
        foreach (var (text, expected) in new[]
        {
            // DemandApology, read before Apology: « dis pardon » asks for one.
            ("excuse-toi", "DemandApology"), ("excuses-toi syncs", "DemandApology"), ("dis pardon", "DemandApology"),
            ("dis désolée", "DemandApology"), ("tu me dois des excuses", "DemandApology"), ("j'attends tes excuses", "DemandApology"),
            ("tu t'excuses ?", "DemandApology"), ("tu t'excuses pas ?", "DemandApology"),
            ("excuse-moi", "Apology"), ("pardon", "Apology"), ("je m'excuse", "Apology"), ("c'est une excuse bidon", "None"),
            ("pas besoin de t'excuser", "None"),
            // ShutUp: short, or the whole message.
            ("chut", "ShutUp"), ("chuuut", "ShutUp"), ("chut syncs", "ShutUp"), ("tais-toi", "ShutUp"), ("oh tais-toi un peu", "ShutUp"),
            ("silence", "ShutUp"), ("la ferme", "ShutUp"), ("ferme-la", "ShutUp"), ("arrête de parler", "ShutUp"),
            ("laisse-nous tranquille", "ShutUp"), ("on t'a rien demandé", "ShutUp"), ("shut up", "ShutUp"),
            ("chut il dort", "None"), ("ferme la porte", "None"), ("reste à la ferme", "None"), ("silence radio depuis hier", "None"),
            ("ta gueule", "None"),
            // A « non » opening it refuses what she said; it doesn't cancel the hush.
            ("Non tais toi", "ShutUp"), ("non, tais-toi", "ShutUp"), ("non mais tais-toi", "ShutUp"), ("non non tais-toi", "ShutUp"),
            // AnswerMe: the answer she didn't give.
            ("SYNCS pourquoi tu réponds pas à ma question?", "AnswerMe"), ("Répond à ma question pressente.", "AnswerMe"),
            ("réponds à ma question", "AnswerMe"), ("t'as pas répondu", "AnswerMe"), ("tu m'as toujours pas répondu", "AnswerMe"),
            ("j'attends ta réponse", "AnswerMe"), ("et ma réponse ?", "AnswerMe"), ("tu esquives", "AnswerMe"),
            ("tu esquives la question", "AnswerMe"), ("esquive pas", "AnswerMe"), ("réponds !", "AnswerMe"), ("réponds stp", "AnswerMe"),
            ("rep à ma question", "AnswerMe"), ("j'ai toujours pas de réponse à ma question", "AnswerMe"),
            ("tu réponds jamais", "AnswerMe"), ("pourquoi tu réponds jamais ?", "AnswerMe"),
            ("il a pas répondu à ma question", "None"), ("personne répond à ma question ?", "Question"), ("j'esquive pas", "Sharing"),
            ("tu esquives bien ses attaques", "None"), ("je réponds demain", "Sharing"), ("ma question c'est quand on joue", "None"),
            // MyStats: asked.
            ("je suis quel niveau ?", "MyStats"), ("je suis quel niveau", "MyStats"), ("j'ai combien de cailloux ?", "MyStats"),
            ("combien j'ai de cailloux", "MyStats"), ("comment va mon plynling ?", "MyStats"), ("mon niveau ?", "MyStats"),
            ("mes cailloux ?", "MyStats"), ("mes stats ?", "MyStats"), ("c'est quoi mon rang ?", "MyStats"), ("je suis riche ?", "MyStats"),
            ("mon plynling a faim", "Sharing"), ("j'ai gagné 200 cailloux", "Sharing"), ("le niveau 3 est horrible", "None"),
            ("t'as combien de niveaux ?", "Question"), ("je suis riche", "Sharing"), ("t'as vu mon niveau en ranked ?", "Question"),
            // TakeSides.
            ("qui a raison, moi ou Luca ?", "TakeSides"), ("qui a raison entre Luca et Sandra ?", "TakeSides"),
            ("moi ou <@1>, qui a raison ?", "TakeSides"), ("qui a raison ?", "TakeSides"), ("<@1> qui a raison ?", "TakeSides"),
            ("dis-lui que j'ai raison", "TakeSides"), ("dis-lui qu'il a tort", "TakeSides"), ("c'est qui qui a raison ?", "TakeSides"),
            ("t'as raison", "YouAreRight"), ("elle a raison", "YouAreRight"), ("qui a raison et qui a tort ?", "Question"),
            ("qui a raison dans ce débat ?", "Question"),
            // RateMe: asked, the adjective closing.
            ("note-moi", "RateMe"), ("tu me notes ?", "RateMe"), ("je vaux combien ?", "RateMe"), ("je suis beau ?", "RateMe"),
            ("je suis belle ?", "RateMe"), ("je suis vraiment mignonne ?", "RateMe"), ("tu me trouves comment ?", "RateMe"),
            ("tu me trouves drôle ?", "RateMe"), ("je suis comment ?", "RateMe"),
            ("je suis nul ?", "Down"), ("je suis beau", "Sharing"), ("je suis beau gosse en jeu ?", "Question"), ("tu me trouves où ?", "Question"),
            ("tu penses quoi de moi ?", "Opinion"),
            // NextSession: asked, before ShouldI and Request.
            ("c'est quand la prochaine session ?", "NextSession"), ("c'est quand la prochaine session", "NextSession"),
            ("y a une session ce soir ?", "NextSession"), ("on joue ce soir ?", "NextSession"), ("on joue quand ?", "NextSession"),
            ("qui vient ce soir ?", "NextSession"), ("y a quoi de prévu ?", "NextSession"), ("on joue ce soir ou pas ?", "NextSession"),
            ("tu peux me dire c'est quand la prochaine session ?", "NextSession"), ("y a pas de session ce soir ?", "NextSession"),
            ("on lance une game ce soir ?", "Question"), ("on joue à quoi ce soir", "None"), ("tu viens ce soir ?", "Question"),
            ("qui est chaud pour un raid ?", "Question"), ("la prochaine game tu joues support ?", "Question"), ("on joue ce soir", "None"),
            ("tu peux organiser une session ?", "Request"), ("la prochaine session je viens", "None"),
            // Roll.
            ("lance un dé", "Roll"), ("lance un dé stp", "Roll"), ("jette deux dés", "Roll"), ("lance les dés", "Roll"), ("1d20", "Roll"),
            ("roll 2d6", "Roll"), ("d20 ?", "Roll"), ("tu peux lancer un dé ?", "Roll"), ("choisis un nombre entre 1 et 10", "Roll"),
            ("un nombre entre 1 et 100 ?", "Roll"), ("donne-moi un chiffre au hasard", "Roll"), ("lance un dé à 20 faces", "Roll"),
            ("j'ai lancé un dé", "Sharing"), ("lance une partie de Valo", "Request"), ("on fait du d4 ce soir ?", "Question"),
            ("le nombre de joueurs entre 4 et 8", "None"), ("donne-moi le nombre de joueurs", "None"), ("un nombre de joueurs limité", "None"),
            ("lance un dé de tes sorts", "None"),

            // Refined: wider forms, and the chat each must leave alone.
            ("tu peux t'excuser ?", "DemandApology"), ("je veux que tu t'excuses", "DemandApology"), ("et mes excuses ?", "DemandApology"),
            ("des excuses ?", "DemandApology"), ("dis que t'es désolée", "DemandApology"), ("tu dois me demander pardon", "DemandApology"),
            // Not asked of her: the « pardon » in them reads as the plain Apology it always did.
            ("je lui dis pardon", "Apology"), ("je demande pardon", "Apology"), ("il m'a demandé pardon", "Apology"), ("toutes mes excuses", "Apology"),
            ("ferme ta bouche", "ShutUp"), ("arrête de jacasser", "ShutUp"), ("mute-toi", "ShutUp"), ("boucle-la", "ShutUp"),
            ("tu parles trop", "ShutUp"), ("tu parles trop syncs", "ShutUp"), ("laisse-nous", "ShutUp"), ("silence s'il te plaît", "ShutUp"),
            ("un peu de silence", "ShutUp"), ("tu parles trop vite", "None"), ("boucle la ceinture", "None"),
            ("je suis level combien ?", "MyStats"), ("mon level ?", "MyStats"), ("j'ai combien de sous ?", "MyStats"), ("mes sous ?", "MyStats"),
            ("mon plynling est vivant ?", "MyStats"), ("il devient quoi mon plynling ?", "MyStats"), ("donne-moi mes stats", "MyStats"),
            ("c'est quoi ma place au classement ?", "MyStats"), ("il me reste combien de temps ?", "Question"),
            ("tranche entre moi et Luca", "TakeSides"), ("départage-nous", "TakeSides"), ("à ton avis, qui a raison ?", "TakeSides"),
            ("qui a raison selon toi, moi ou Luca ?", "TakeSides"), ("moi ou Luca, qui a raison selon toi ?", "TakeSides"),
            ("qui a le plus raison entre Luca et Sandra ?", "TakeSides"), ("une tranche de pizza", "None"),
            ("évalue-moi", "RateMe"), ("tu me mettrais combien ?", "RateMe"), ("je suis jolie ?", "RateMe"), ("je suis chiant ?", "RateMe"),
            ("je suis beau en vrai ?", "RateMe"), ("je suis comment physiquement ?", "RateMe"), ("je suis fort en maths ?", "Question"),
            ("je suis fou de joie", "Sharing"),
            ("c'est quand la soirée ?", "NextSession"), ("session ce soir ?", "NextSession"), ("c'est prévu pour quand ?", "NextSession"),
            ("on rejoue quand ?", "NextSession"), ("qui sera là ce soir ?", "NextSession"), ("y a du monde ce soir ?", "NextSession"),
            ("un jet de d20", "Roll"), ("fais un jet de dés", "Roll"), ("random entre 1 et 100", "Roll"), ("lancez les dés", "Roll"),
            ("fais un jet de sauvegarde", "None"), ("le jet d'eau est cassé", "None"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }

        var readMyStats = cues.GetMethod("ReadMyStatsTopic", BindingFlags.Public | BindingFlags.Static, [typeof(string)])!;
        foreach (var (text, topic) in new[]
        {
            ("je suis quel niveau ?", MyStatsTopic.Level), ("c'est quoi mon rang ?", MyStatsTopic.Level), ("mon xp ?", MyStatsTopic.Level),
            ("j'ai combien de cailloux ?", MyStatsTopic.Balance), ("mes cailloux ?", MyStatsTopic.Balance),
            ("comment va mon plynling ?", MyStatsTopic.Plynling), ("mes stats ?", MyStatsTopic.All),
        })
            if (!Equals(readMyStats.Invoke(null, new object[] { text }), topic)) Fail($"ReadMyStatsTopic(\"{text}\") != {topic}");

        // Dice: what is read, what is rolled, and the caps.
        foreach (var (text, count, faces) in new[] { ("1d20", 1, 20), ("lance les dés", 2, 6), ("jette 3 dés à 20 faces", 3, 20), ("roll d8", 1, 8) })
            if (Dice.Read(text) is not { IsRange: false } d || d.Count != count || d.Faces != faces) Fail($"Dice.Read(\"{text}\") = {Dice.Read(text)}");
        if (Dice.Read("choisis un nombre entre 10 et 1") is not { IsRange: true, Min: 1, Max: 10 }) Fail("a reversed range is not 1..10");
        if (Dice.Read("un nombre au hasard") is not { IsRange: true, Min: 1, Max: 100 }) Fail("an open range is not 1..100");
        foreach (var text in new[] { "25d6", "1d1", "lance 0 dé", "1d5000" })
            if (Dice.Read(text) is not null) Fail($"Dice.Read(\"{text}\") rolls past the caps");
        {
            var diceRng = new Random(3);
            for (var i = 0; i < 200; i++)
            {
                var range = Dice.Roll(new DiceAsk(0, 0, 1, 6, true), diceRng);
                if (!int.TryParse(range.Result, out var n) || n is < 1 or > 6) Fail($"range roll out of bounds: {range.Result}");
            }
            var two = Dice.Roll(new DiceAsk(2, 6, 0, 0, false), diceRng);
            if (two.Spec != "2d6" || !System.Text.RegularExpressions.Regex.IsMatch(two.Result, @"^[1-6] \+ [1-6] = \d+$")) Fail($"2d6 rolled as {two}");
            if (Dice.Roll(new DiceAsk(0, 0, 1, 100, true), diceRng).Spec != "un nombre entre 1 et 100") Fail("range spec wording");
        }

        // TakeSides: the sides as written, or the asker alone.
        foreach (var (text, a, b) in new (string, string, string?)[]
        {
            ("qui a raison, moi ou Luca ?", "moi", "Luca"), ("qui a raison entre Luca et Sandra ?", "Luca", "Sandra"),
            ("moi ou <@1>, qui a raison ?", "moi", "<@1>"), ("syncs, qui a raison ? 🙂", "moi", null), ("dis-lui qu'il a tort", "moi", null),
        })
            if (TakeSides.Read(text) is not { } s || s.A != a || s.B != b) Fail($"TakeSides.Read(\"{text}\") = {TakeSides.Read(text)}");

        // SessionRoster: the count and the names in finished words.
        foreach (var (names, maybes, expected) in new (string[], int, string)[]
        {
            ([], 0, "aucune inscription"), (["Luca"], 0, "1 inscription (Luca)"), (["Luca"], 1, "1 inscription (Luca), plus un « peut-être »"),
            (["A", "B", "C"], 2, "3 inscriptions (A, B et C), plus 2 « peut-être »"),
            (["A", "B", "C", "D", "E", "F", "G"], 0, "7 inscriptions (A, B, C, D, E et 2 autres)"),
            (["A", "B", "C", "D", "E", "F"], 0, "6 inscriptions (A, B, C, D, E et 1 autre)"),
        })
            if (SessionRoster.Describe(names, maybes) != expected) Fail($"SessionRoster = {SessionRoster.Describe(names, maybes)}, expected {expected}");

        // MyStats: each topic's words, and nothing to say when there is nothing.
        {
            RecapStats Mine(long xp, int? level, int? rank, long balance, string? plyn, TimeSpan? age) =>
                new(StatsPeriod.AllTime, xp, level, rank, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, plyn, age, 0, balance);
            var full = Mine(900, 12, 4, 1250, "*Pix*", TimeSpan.FromDays(12.5));
            if (MyStats.Describe(full, MyStatsTopic.Level) is not ("niveau 12", "n° 4 du classement")) Fail($"level: {MyStats.Describe(full, MyStatsTopic.Level)}");
            if (MyStats.Describe(full, MyStatsTopic.Balance)?.A != 1250.ToString("N0", System.Globalization.CultureInfo.GetCultureInfo("fr-FR")) + " cailloux")
                Fail($"balance: {MyStats.Describe(full, MyStatsTopic.Balance)}");
            if (MyStats.Describe(full, MyStatsTopic.Plynling) is not { } p || p.A != Discord.Format.Sanitize("*Pix*") || p.B != "12 jours")
                Fail($"plynling: {MyStats.Describe(full, MyStatsTopic.Plynling)}");
            var blank = Mine(0, null, null, 0, null, null);
            if (MyStats.Describe(blank, MyStatsTopic.Level) is not null) Fail("no XP still has a level to show");
            if (MyStats.Describe(blank, MyStatsTopic.Plynling) is not null) Fail("no Plynling still has one to show");
            if (MyStats.Describe(blank, MyStatsTopic.Balance)?.A != "zéro caillou") Fail("an empty purse is not « zéro caillou »");
            if (MyStats.Describe(blank, MyStatsTopic.All)?.A != "zéro caillou") Fail($"all, empty: {MyStats.Describe(blank, MyStatsTopic.All)}");
            if (MyStats.Describe(Mine(10, 1, null, 1, null, TimeSpan.FromHours(3)), MyStatsTopic.Level)?.B != "hors classement") Fail("unranked wording");
        }
        // The pools that quote the numbers say them: every SessionNextLines line both {2} and {3}, every
        // TakeSidesLines line both sides, every Roll line the result.
        foreach (var l in (string[])Field("SessionNextLines"))
            if (!l.Contains("{2}") || !l.Contains("{3}")) Fail($"SessionNextLines drops a placeholder: {l}");
        foreach (var l in (string[])Field("TakeSidesLines"))
            if (!l.Contains("{2}") || !l.Contains("{3}")) Fail($"TakeSidesLines drops a side: {l}");
        foreach (var l in (string[])((IDictionary)Field("IntentLines"))[Enum.Parse(intentType, "Roll")]!)
            if (!l.Contains("{2}") || !l.Contains("{3}")) Fail($"IntentLines[Roll] drops the roll: {l}");

        // 22. Papa's pet names: which family, whether said to her, and what they must leave alone.
        var readPetName = cues.GetMethod("ReadPetName", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, expected) in new[]
        {
            ("ma framboise", "Raspberry"), ("Ma Framboise, ça va ?", "Raspberry"), ("salut petite framboise", "Raspberry"),
            ("bonne nuit ma petite framboise", "Raspberry"), ("ma framboiiise ♡", "Raspberry"),
            ("ma fille", "Child"), ("bravo ma fille !", "Child"), ("ma fiiille", "Child"), ("mon enfant, viens là", "Child"),
            ("ma petite fille est passée", "None"), ("j'ai mangé des framboises", "None"), ("une framboise", "None"),
            ("mes framboises", "None"), ("ma famille", "None"), ("mon enfance", "None"), ("syncs", "None"), ("", "None"),
            ("<:ma_framboise:123> lol", "None"), ("https://example.com/ma-framboise", "None"),
        })
        {
            var got = readPetName.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadPetName(\"{text}\") = {got}, expected {expected}");
        }

        var namesHerFrom = cues.GetMethod("NamesHerFrom", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, fromOwner, expected) in new[]
        {
            ("ma framboise", true, true), ("ma framboise", false, false), ("ma fille", false, false),
            ("syncs", false, true), ("syncs", true, true), ("rien à voir", true, false),
        })
        {
            var got = (bool)namesHerFrom.Invoke(null, new object[] { text, fromOwner })!;
            if (got != expected) Fail($"NamesHerFrom(\"{text}\", {fromOwner}) = {got}, expected {expected}");
        }

        var addressesByPet = cues.GetMethod("AddressesHerByPetName", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, expected) in new[]
        {
            ("ma framboise, ça va ?", true), ("ça va ma fille ?", true), ("salut ma petite framboise", true),
            ("<@999> mon enfant, t'en penses quoi ?", true), ("petite framboise tu fais quoi", true),
            ("ma fille a planté le bot", false), ("je joue avec ma fille", false), ("c'est ma fille", false),
            ("<@555> ma framboise ça va", false), ("dis à ma framboise de lancer un sondage", false),
        })
        {
            var got = (bool)addressesByPet.Invoke(null, new object[] { text, 999UL })!;
            if (got != expected) Fail($"AddressesHerByPetName(\"{text}\") = {got}, expected {expected}");
        }
    }
}
