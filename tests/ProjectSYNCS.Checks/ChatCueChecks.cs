using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 8–15. /debug answers, widened cues and the misfires they must not cause, the chat sweeps,
// Sharing and Ship.
public static class ChatCueChecks
{
    public static readonly HashSet<string> generic = new HashSet<string> { "None", "Question", "Sharing", "LowEffort" };

    public static readonly string[] neutral = new[]
    {
        "je suis en retard de 10 minutes", "le boss final est trop dur", "j'ai fini le donjon hier soir", "qui est chaud pour un raid ?",
        "il est où le lien du serveur ?", "j'arrive dans 5 min", "on se retrouve sur le vocal", "t'as vu le patch notes ?",
        "ils ont nerf mon perso", "ce jeu est trop bien", "j'ai acheté le DLC", "la mise à jour est sortie", "j'ai plus de batterie",
        "je dois manger avant", "Luca est pas encore là", "c'est quoi le code du lobby", "j'ai oublié mon mot de passe",
        "ma connexion est nulle ce soir", "on est combien ce soir ?", "je peux pas venir samedi", "il pleut des cordes ici",
        "mon chat a vomi sur le clavier", "demain je bosse tôt", "qui a pris mon spot ?", "j'ai eu un bug de collision",
        "on fait une partie de Mario Kart après ?", "t'as fait combien de kills ?", "le serveur a crash", "j'ai rage quit",
        "c'est la troisième fois que je meurs", "j'ai trouvé un œuf de Plynling", "mon plynling a faim", "j'ai gagné 200 cailloux",
        "il me manque une pièce pour le craft", "on se fait un morpion ?", "je suis devant le PC", "la soirée était cool hier",
        "vous avez vu le match hier ?", "il reste de la place ?", "y a plus de place", "t'inquiète je gère", "je gère pas ce boss",
        "c'est mort pour ce soir", "c'est calme ce soir", "rien à faire, il est trop fort", "t'as fini ?", "tu dors chez qui ?",
        "tu dors jamais toi", "y a quelqu'un pour un duo ?", "je comprends rien à ce jeu", "comment on fait pour rejoindre ?",
        "tu peux pas savoir", "il est gentil ce boss", "t'es pas croyable", "c'est quoi cette commande ?", "le bot a planté hier soir",
        "j'ai voté pour samedi", "je vote non", "qui organise la soirée ?", "tu sais si Luca vient ?", "t'as pas un chargeur ?",
        "je reviens dans 2 min", "je vais me chercher à boire", "j'ai sommeil mais je reste", "il dort déjà", "elle est là ce soir ?",
        "t'as vu la vidéo ?", "ça va être chaud ce soir", "ça va pas du tout ce patch", "pas du tout mon style de jeu",
        "je te laisse gagner", "possible que je sois en retard", "bonne idée", "tu es la seule à comprendre", "t'es la reine",
        "il est là le boss ?", "je dors chez Luca ce soir", "on lance une game ce soir ?", "il faut que je parte à 22h",
        "le dernier boss m'a tué trois fois", "j'ai mis à jour mes drivers", "on peut jouer à 4 ?", "je joue sur Switch",
        "mon micro marche pas", "tu m'entends pas bien ?", "je capte mal ici", "le jeu est en promo", "il a pris le dernier ticket",
        "j'ai eu la flemme", "je préfère le mode solo", "le niveau 3 est horrible", "je suis bloqué au chapitre 2",
        "il faut farmer encore", "j'ai drop une légendaire", "elle a carry la game", "on a perdu de peu", "c'était serré",
        "la team rouge a gagné", "je suis mort au premier round", "ce perso est cheaté", "faut nerf ce mec",
        "t'as combien de niveaux ?", "j'ai changé de pseudo", "c'est ma première fois sur ce jeu", "je suis nouveau ici",
        "le tournoi commence quand ?", "j'ai pris un abonnement", "le serveur est plein", "ça lag de ouf", "mon PC chauffe",
        "j'ai faim", "j'ai soif", "je suis crevé", "j'ai trop mangé", "il fait chaud ici", "je suis dehors", "je rentre du boulot",
        "je suis dans le train", "je finis mon film et j'arrive", "le film était nul", "la série est trop longue",
        "j'ai vu un écureuil", "mon frère joue aussi", "ma sœur arrive", "on mange quoi ce soir ?", "il est quelle heure ?",
        "ça commence à quelle heure ?", "je mets la musique", "la musique est trop forte", "je baisse le son",
        "j'ai perdu ma manette", "la manette et le PC c'est compatible ?", "samedi et dimanche, ça marche ?",
        "vendredi et samedi ensemble ?", "c'était mon pire match de la saison", "on est ensemble ce soir ?",
        "trouve-moi quelqu'un pour jouer", "je vais dormir chez Luca demain", "je m'endors devant le film",
        "y a rien à faire contre ce boss", "je m'ennuie de toi", "il fait genre il sait jouer", "oui bien sûr que je viens",
        "il a grillé un feu rouge", "je te crois", "c'est la vie", "c'est comme ça", "tant pis", "on verra bien demain soir avec les autres",
        "je l'ai vu hier", "il me l'a dit", "elle m'a envoyé un message", "on s'appelle plus tard", "je t'envoie le lien",
        "je te dis ça demain", "je te montre après", "je t'attends en vocal", "je te rejoins", "je te suis",
    };

    public static readonly string[] slang = new[]
    {
        "gg", "ez", "rip", "f", "wsh", "frr", "chui là", "j'arrive", "go", "go go go", "let's go", "on y va", "nice try",
        "noob", "gg wp", "osef de lui", "c'est chaud", "c'est ouf", "trop fort lui", "il est fou", "zebi", "wesh alors",
        "t'es sérieux ?", "sérieux ?", "vraiment ?", "ah bon ?", "hein ?", "quoi ?", "comment ça ?", "pourquoi ?",
        "la honte", "c'est la hess", "je suis dead", "chaud chaud", "jsuis grave en retard", "chui en route",
        "attends 2 sec", "deux secondes", "brb", "afk", "re", "back", "dispo ?", "qui est dispo ?", "vous êtes où ?",
        "on attend qui ?", "t'es en game ?", "lance", "invite moi", "accepte", "je t'add", "ajoute moi en ami",
        "c'est quoi ton pseudo ?", "t'as discord ?", "mdr non", "nan mais allô", "allô quoi",
    };

    public static void Run()
    {
        // 8. /debug answers: keys, the examples log, the embed caps.
        var kindsType = typeof(AnswerKinds);
        var consts = kindsType.GetFields(BindingFlags.Public | BindingFlags.Static).Where(f => f.IsLiteral).Select(f => (string)f.GetValue(null)!).ToList();
        var intentKey = kindsType.GetMethod("Intent", BindingFlags.NonPublic | BindingFlags.Static)!;
        var allKinds = consts
            .Concat(Enum.GetValues(intentType).Cast<object>().Where(v => v.ToString() != "None").Select(v => (string)intentKey.Invoke(null, new[] { v })!))
            .Concat(Enum.GetValues<ReplyContext>().Select(AnswerKinds.Context))
            .Concat(Enum.GetValues<ChatFact>().Select(AnswerKinds.Fact))
            .Concat(Enum.GetValues(asm.GetType("ProjectSYNCS.Services.CardReplyIntent")!).Cast<object>().Where(v => v.ToString() != "None")
                .Select(v => (string)kindsType.GetMethod("Card", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, new[] { v })!))
            .ToList();
        if (allKinds.Distinct().Count() != allKinds.Count) Fail("duplicate answer kind keys");
        if (!AnswerKinds.IsUnrecognised(AnswerKinds.Comeback) || AnswerKinds.IsUnrecognised(AnswerKinds.Nice)
            || !AnswerKinds.IsUnrecognised(AnswerKinds.Fact(ChatFact.Broke))) Fail("IsUnrecognised misreads");

        if (UnansweredLog.Clean("message 149\n  ligne\t") != "message 149 ligne") Fail($"examples text not collapsed: '{UnansweredLog.Clean("message 149\n  ligne\t")}'");
        if (UnansweredLog.Clean(new string('x', 500)).Length != UnansweredLog.MaxTextLength) Fail("long text not capped");
        if (UnansweredLog.Clean(" \n\t ").Length != 0) Fail("blank text kept");
        if (typeof(UnansweredMessage).GetProperties().Concat(typeof(ProjectSYNCS.Models.UnansweredExample).GetProperties())
            .Any(p => p.Name.Contains("User") || p.Name.Contains("Author"))) Fail("an example stores its author");
        var t0 = DateTimeOffset.UtcNow;
        var examples = Enumerable.Range(0, 150).Reverse()
            .Select(i => new UnansweredMessage(t0.AddSeconds(i), AnswerKinds.Key(ChatPath.Reply), AnswerKinds.Comeback, UnansweredLog.Clean($"message {i}\nligne")))
            .ToList();

        var cards = asm.GetType("ProjectSYNCS.Commands.AnswerStatsCards")!;
        var buildTypes = cards.GetMethod("BuildTypes")!;
        var buildExamples = cards.GetMethod("BuildExamples")!;
        var synthetic = allKinds.SelectMany(k => Enum.GetValues<ChatPath>().Select(p => new ChatAnswerCount(AnswerKinds.Key(p), k, 1234))).ToList();
        foreach (var period in Enum.GetValues<StatsPeriod>())
        {
            var embed = (Discord.Embed)buildTypes.Invoke(null, new object[] { synthetic, period })!;
            if (embed.Length > 6000) Fail($"answers embed is {embed.Length} characters");
            if (embed.Fields.Any(f => f.Value.Length > 1024)) Fail("an answers field exceeds 1024");
            if (period == StatsPeriod.Week) Console.WriteLine($"answers embed: {embed.Length} chars, {embed.Fields.Length} fields");
        }
        var empty = (Discord.Embed)buildTypes.Invoke(null, new object[] { new List<ChatAnswerCount>(), StatsPeriod.Week })!;
        if (empty.Fields.Length != 0) Fail("empty counts render fields");
        var file = (string)buildExamples.Invoke(null, new object[] { examples, UnansweredLog.KeptDays })!;
        if (!file.Contains("message 149 ligne") || !file.Contains("message 0 ligne")) Fail("examples file misses a line");

        // 9. Widened cues, and the misfires they must not cause.
        var cases3 = new (string Text, string Expected)[]
        {
            // Love
            ("tu veux sortir avec moi ?", "Love"), ("jtd", "Love"), ("je t'apprécie beaucoup", "Love"), ("t'es mon crush", "Love"),
            ("luv u", "Love"), ("on se marie ?", "Love"), ("je t'apprécie pas", "None"),
            // Apology
            ("déso", "Apology"), ("oups", "Apology"), ("oups pardon", "Apology"), ("c'est ma faute", "Apology"),
            ("c'est pas ma faute", "None"), ("j'ai pas fait exprès", "Apology"), ("je voulais pas te vexer", "Apology"),
            ("je voulais pas venir ce soir de toute façon", "Sharing"), ("oups j'ai raté le boss encore une fois", "None"),
            // HowAreYou
            ("salut, ça va ? tu viens ce soir ?", "HowAreYou"), ("tranquille ?", "HowAreYou"), ("tranquille et toi ?", "HowAreYou"),
            ("bien et toi ?", "HowAreYou"), ("ça baigne ?", "HowAreYou"), ("tu vas mieux ?", "HowAreYou"),
            ("comment va la vie ?", "HowAreYou"), ("comment était ta journée ?", "HowAreYou"), ("how are you ?", "HowAreYou"),
            ("ça va depuis ?", "HowAreYou"), ("on fait une partie tranquille ?", "Question"),
            ("c'est quand ta journée de repos ?", "Question"), ("ça va pour demain ?", "Question"), ("comment va ton plynling ?", "Question"),
            // WhoAreYou / AreYouABot / WhatAreYouDoing
            ("c'est qui ton créateur ?", "WhoAreYou"), ("t'es quoi comme bot ?", "WhoAreYou"),
            ("es-tu une IA ?", "AreYouABot"), ("t'es chat gpt ?", "AreYouABot"), ("t'as un cœur ?", "AreYouABot"),
            ("c'est toi qui écris ?", "AreYouABot"), ("are you a bot", "AreYouABot"),
            ("tfq", "WhatAreYouDoing"), ("qu'est-ce que tu fabriques ?", "WhatAreYouDoing"), ("tu t'ennuies ?", "WhatAreYouDoing"),
            ("wyd", "WhatAreYouDoing"), ("quoi de nouveau ?", "WhatAreYouDoing"),
            // Goodnight / Goodbye
            ("gn", "Goodnight"), ("dors bien syncs", "Goodnight"), ("bon je vais roupiller", "Goodnight"), ("go dodo", "Goodnight"),
            ("a+", "Goodbye"), ("A+ tout le monde", "Goodbye"), ("@+", "Goodbye"), ("a2m1", "Goodbye"), ("à tte", "Goodbye"),
            ("bon j'y vais", "Goodbye"), ("allez salut", "Goodbye"), ("je me sauve", "Goodbye"), ("tchuss", "Goodbye"),
            ("bonne continuation", "Goodbye"), ("j'y vais aussi samedi", "None"), ("salut", "None"),
            // Maybe
            ("j'en sais rien", "Maybe"), ("chais pas", "Maybe"), ("qui sait", "Maybe"), ("probablement", "Maybe"),
            ("pas sûr", "Maybe"), ("non je sais pas", "Maybe"), ("idk", "Maybe"), ("pas probable", "None"),
            // YouAreRight
            ("t'as trop raison", "YouAreRight"), ("tellement vrai", "YouAreRight"), ("je confirme", "YouAreRight"),
            ("elle a raison", "YouAreRight"), ("effectivement", "YouAreRight"), ("c'est clair", "YouAreRight"),
            ("c'est clair ?", "Question"), ("en effet", "YouAreRight"), ("fair enough", "YouAreRight"),
            ("j'ai bien vu le film hier avec mon frère et ma sœur", "Sharing"),
            // YouAreWrong
            ("t'es dans le faux", "YouAreWrong"), ("tu délires", "YouAreWrong"), ("nimp", "YouAreWrong"), ("tu mens", "Disbelief"),
            ("jamais de la vie", "YouAreWrong"), ("dans tes rêves", "YouAreWrong"), ("pas vraiment", "YouAreWrong"),
            ("wrong", "YouAreWrong"),
            // Request
            ("tu veux pas lancer un sondage ?", "Request"), ("j'aimerais que tu fasses les équipes", "Request"),
            ("annule la session", "Request"), ("tu peux timeout Luca ?", "Request"), ("réveille-toi", "AreYouThere"),
            ("mets un rappel pour demain", "Request"), ("donne-moi un café", "None"),
            // Laughter
            ("hohoho", "Laughter"), ("kekw", "Laughter"), ("je suis pliée", "Laughter"), ("je pleure de rire", "Laughter"),
            ("bonne blague", "Laughter"), ("niark niark", "Laughter"), ("😁", "Laughter"), ("je pleure", "None"),
            // LowEffort
            ("hein", "LowEffort"), ("pff", "LowEffort"), ("oh là là", "LowEffort"), ("tkt", "LowEffort"), ("yeah", "LowEffort"),
            ("ah ok genre", "LowEffort"),
            // Question
            ("qui vient ce soir", "NextSession"), ("où est le lien", "Question"), ("c'est quand la session", "NextSession"),
            ("quelle heure on commence", "Question"), ("tu sais si Luca vient", "Question"), ("why are you like this", "Question"),
            ("ou alors on joue demain", "None"), ("pkoi tu dis ça", "Question"),
        };
        foreach (var (text, expected) in cases3)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }

        // 10. The « discute avec Syncs quand tu peux » misfire.
        foreach (var (text, expected) in new[]
        {
            ("<@555> Discute avec Syncs quand tu peux. J'ai rework son \"intelligence conversationelle\", donc faut que je trouve les bugs", "None"),
            ("si tu peux", "None"), ("dès que tu peux", "None"), ("je sais que tu peux le faire", "None"),
            ("Syncs, tu penses quoi de <@555> ?", "Opinion"),
            ("est-ce que tu peux lancer un sondage ?", "Request"), ("tu peux lancer un sondage quand tu peux ?", "Request"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        foreach (var (text, expected) in new[]
        {
            ("syncs, ça va ?", true), ("ça va syncs ?", true), ("salut syncs ça va", true), ("S.Y.N.C.S, ça va ?", true),
            ("<@999> syncs ça va ?", true), ("syncs <@999> tu fais quoi", true),
            ("<@555> Discute avec Syncs quand tu peux", false), ("syncs ça va <@555> ?", false), ("<@&42> syncs ça va", false),
            ("dis à syncs de lancer un sondage", false), ("la sync a planté", false), ("je crois que syncs a bugué", false),
            // A tag that is what she is asked about doesn't take the message away from her.
            ("Syncs, tu penses quoi de <@555> ?", true), ("syncs tu préfères <@555> ou <@666> ?", true),
            ("syncs, <@555> et <@666> ça match ?", true), ("<@555> tu penses quoi de syncs ?", false),
            ("<@555> syncs, tu penses quoi de la pizza ?", false),
            // An order by name, no comma: « organise » and « lance » are an order when a request follows, a
            // fact about her when it's a habit, a question or a generic object.
            ("syncs organise un valo samedi 21h", true), ("syncs lance un sondage pizza ou sushi", true),
            ("hey syncs organise un among us demain", true), ("syncs lance un tonight pour Minecraft", true),
            ("syncs organise les sessions", false), ("syncs organise les sessions le samedi", false),
            ("syncs lance un sondage chaque mercredi", false), ("syncs organise un truc samedi ?", false),
            ("syncs lance encore un sondage", false), ("syncs organise tout ici", false),
        })
        {
            var got = (bool)addresses.Invoke(null, new object[] { text, 999UL })!;
            if (got != expected) Fail($"AddressesHer(\"{text}\") = {got}, expected {expected}");
        }

        // 11. The GigaLaugh emote, AreYouThere, AboutHer, Crush, Help, Bored, Thanks, Ship, and their collisions.
        var cases5 = new (string Text, string Expected)[]
        {
            // GigaLaugh
            ("<:giga_laugh:1482304998871138346>", "Laughter"), ("mdr <:giga_laugh:1482304998871138346>", "Laughter"),
            ("<:giga_laugh:1482304998871138346> <:giga_laugh:1482304998871138346>", "Laughter"),
            // AreYouThere
            ("t'es là ?", "AreYouThere"), ("syncs t'es là ?", "AreYouThere"), ("tu dors ?", "AreYouThere"), ("tu dors ou quoi", "AreYouThere"),
            ("y a quelqu'un ?", "AreYouThere"), ("allô ?", "AreYouThere"), ("réveille-toi", "AreYouThere"), ("syncs ?", "AreYouThere"),
            ("t'es toujours là ?", "AreYouThere"), ("t'es là ce soir ?", "Question"), ("t'es la meilleure", "None"),
            ("tu dors bien la nuit ?", "Question"), ("syncs", "None"), ("tu connais quelqu'un qui joue ?", "Question"),
            // AboutHer
            ("t'as quel âge ?", "AboutHer"), ("quel âge tu as", "AboutHer"), ("c'est quand ton anniv ?", "AboutHer"),
            ("t'es née quand ?", "AboutHer"), ("tu habites où ?", "AboutHer"), ("tu viens d'où ?", "AboutHer"),
            ("c'est quoi ton jeu préféré ?", "AboutHer"), ("ta couleur préférée ?", "AboutHer"), ("tu aimes quoi comme musique ?", "AboutHer"),
            ("tu aimes les chats ?", "Opinion"), ("je préfère Minecraft", "None"),
            // Crush
            ("t'es célibataire ?", "Crush"), ("t'as un crush ?", "Crush"), ("t'es amoureuse de qui ?", "Crush"),
            ("c'est qui ton crush ?", "Crush"), ("t'es mon crush", "Love"), ("je suis amoureux de toi", "Love"),
            ("il te plaît le jeu ?", "Question"), ("t'as quelqu'un pour jouer ce soir ?", "Question"),
            // Help
            ("comment ça marche ?", "Help"), ("tu sais faire quoi ?", "Help"), ("help", "Help"), ("aide-moi", "Help"),
            ("tu peux m'aider ?", "Help"), ("comment ça marche les sondages ?", "Help"), ("je suis perdu", "Help"),
            ("je comprends rien", "Help"), ("j'ai pas besoin d'aide", "Sharing"),
            ("tu peux m'aider à lancer un sondage ?", "Request"), ("aide-moi à organiser une session", "Request"),
            ("je suis perdu dans le donjon", "Sharing"), ("comment on fait pour battre ce boss", "Question"),
            ("tu peux danser ?", "Request"), ("tu sais faire un café ?", "Request"),
            // Bored
            ("je m'ennuie", "Bored"), ("on s'ennuie ici", "Bored"), ("on fait quoi ?", "Bored"), ("y a rien à faire", "Bored"),
            ("je sais pas quoi faire", "Bored"), ("je m'ennuie pas du tout", "Sharing"), ("j'en ai rien à faire", "None"),
            ("on fait quoi pour la session de samedi ?", "Question"), ("tu t'ennuies ?", "WhatAreYouDoing"), ("boring", "Bored"),
            // Thanks
            ("merci", "Thanks"), ("merci syncs", "Thanks"), ("merci beaucoup !", "Thanks"), ("thx", "Thanks"), ("ty", "Thanks"),
            ("c'est gentil", "Thanks"), ("non merci", "None"), ("merci, bonne nuit", "Goodnight"), ("merci à plus", "Goodbye"),
            ("tu peux lancer un sondage stp merci", "Request"), ("merci pour la session d'hier, c'était top", "Thanks"),
            ("ty for the carry yesterday guys", "None"),
            // Ship
            ("ship Luca et Sandra", "Ship"), ("tu peux ship moi et Luca ?", "Ship"), ("tu me ship avec <@123> ?", "Ship"),
            ("Luca et moi, ça match ?", "Ship"), ("toi et moi on irait bien ensemble ?", "Ship"),
            ("la pizza et l'ananas, compatibles ?", "Ship"), ("syncs, Luca et moi ça match ?", "Ship"),
            ("toi et moi on joue ensemble ?", "Question"), ("la relationship est compliquée", "None"),
            ("je pense que Luca et Sandra ça match", "None"),
        };
        foreach (var (text, expected) in cases5)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        ShipIs("ship Luca et Sandra", "Luca", "Sandra");
        ShipIs("tu me ship avec <@123> ?", "moi", "<@123>");
        ShipIs("<@999> Luca et moi, ça match ?", "Luca", "moi");
        ShipIs("syncs, la pizza et l'ananas compatibles ?", "pizza", "ananas");
        ShipIs("shippe-moi avec quelqu'un", "moi", "quelqu'un");
        // « pense » without its s, as typed in the wild.
        ShipIs("et tu pense quoi de <@111> x <@222> ?", "<@111>", "<@222>");
        ShipIs("syncs tu pense quoi de <@111> x <@222>", "<@111>", "<@222>");
        ShipIs("<@111> et <@222>, t'en pense quoi ?", "<@111>", "<@222>");
        var herTopic = cues.GetMethod("ReadHerTopic", BindingFlags.Public | BindingFlags.Static, new[] { typeof(string) })!;
        foreach (var (text, topic) in new[] { ("t'as quel âge ?", "Age"), ("ton anniv c'est quand", "Birthday"), ("tu vis où ?", "Home"), ("ton plat préféré ?", "Favourite"),
                     ("t'es en quelle version ?", "Version"), ("c'est quoi ta version syncs ?", "Version"), ("t'es à jour ?", "Version"),
                     ("<@123> tu tournes sur quelle version ?", "Version"), ("t'es en quelle version là ? tu viens ce soir ?", "Version"),
                     ("quelle version tu es", "Version"), ("what version are you on?", "Version") })
            if (herTopic.Invoke(null, new object[] { text })?.ToString() != topic) Fail($"ReadHerTopic(\"{text}\") != {topic}");
        // Her version, asked; the game's version, or someone's version of the facts, is something else.
        foreach (var (text, expected) in new[]
                 {
                     ("t'es en quelle version ?", "AboutHer"), ("syncs, c'est quoi ta version ?", "AboutHer"), ("t'es à jour ?", "AboutHer"),
                     ("t'es en quelle version de Minecraft ?", "Question"), ("t'as quelle version du jeu ?", "Question"),
                     ("c'est quoi ta version des faits ?", "Question"), ("t'es à jour sur le patch ?", "Question"),
                     ("je préfère ta version", "None"),
                 })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        if (readTopicAny.Invoke(null, new object[] { "comment ça marche les sondages ?" })!.ToString() != "Poll") Fail("help topic: sondages");
        if (MorningGreeting.DaysOld(new DateTimeOffset(2026, 10, 16, 12, 0, 0, TimeSpan.Zero)) != 122) Fail("DaysOld on 16 Oct 2026");

        // 12. The soup exchange, Disbelief, the wider ship, and a sweep of ordinary chat.
        var cases6 = new (string Text, string Expected)[]
        {
            // The exchange as it happened.
            ("<@999> pwet pwet", "None"), ("J'ai mangé de la soupe", "Sharing"), ("Respecte la soupe <:pepe:123>", "None"),
            ("Et Wall-E dans tout ça?", "Question"), ("Ouais ouais fais genre", "Disbelief"),
            // Disbelief, and what must stay something else.
            ("fais genre", "Disbelief"), ("mon œil", "Disbelief"), ("mais oui c'est ça", "Disbelief"), ("avoue", "Disbelief"),
            ("allez avoue", "Disbelief"), ("t'es grillée", "Disbelief"), ("tu rougis là", "Disbelief"), ("on te croit pas", "Disbelief"),
            ("je te crois pas", "Disbelief"), ("mais bien sûr", "Disbelief"), ("avoue que t'as triché", "Disbelief"),
            ("j'avoue", "YouAreRight"), ("j'avoue c'est fort", "YouAreRight"), ("il fait genre il sait jouer", "None"),
            ("oui bien sûr que je viens", "None"), ("je te crois", "None"), ("il a grillé un feu rouge", "None"),
            ("genre t'as gagné ?", "Question"), ("mais non fais genre", "Disbelief"),
            // Ship, wider.
            ("le ship entre Luca et Sandra", "Ship"), ("compatibilité entre Luca et moi ?", "Ship"), ("love calculator Luca et Sandra", "Ship"),
            ("est-ce que Luca et moi on est compatibles ?", "Ship"), ("Luca x Sandra ?", "Ship"), ("on irait bien ensemble ?", "Ship"),
            ("syncs on ferait un beau couple ?", "Ship"), ("tu nous ship ?", "Ship"), ("<@123> on irait bien ensemble ?", "Ship"),
            ("c'est qui mon âme sœur ?", "Ship"), ("avec qui tu me ship ?", "Ship"), ("trouve-moi l'âme sœur", "Ship"),
            ("c'est qui mon pire ship ?", "Ship"), ("avec qui je suis le moins compatible ?", "Ship"), ("shipper Luca avec Sandra", "Ship"),
            ("à ton avis, Luca et Sandra ça match ?", "Ship"), ("Luca et Sandra en couple ?", "Ship"),
            ("et tu pense quoi de <@111> x <@222> ?", "Ship"), ("syncs tu pense quoi de <@111> x <@222>", "Ship"),
            ("vendredi et samedi ensemble ?", "Question"), ("la manette et le PC c'est compatible ?", "Question"),
            ("c'était mon pire match de la saison", "None"), ("mon meilleur match c'était hier", "None"),
            ("on est ensemble ce soir ?", "Question"), ("trouve-moi quelqu'un pour jouer", "None"),
            ("samedi et dimanche, ça marche ?", "Question"), ("2 x 3 ?", "Question"), ("on va jouer ensemble ?", "Question"),
            // Sleep and boredom, tightened.
            ("je vais dormir chez Luca demain", "Sharing"), ("je m'endors devant le film", "Sharing"), ("bon je vais dormir", "Goodnight"),
            ("je vais me coucher, bonne nuit", "Goodnight"), ("je vais dormir là", "Goodnight"), ("dodo", "Goodnight"),
            ("y a rien à faire contre ce boss", "None"), ("y a rien à faire ce soir", "Bored"), ("y a rien à faire", "Bored"),
            ("je m'ennuie de toi", "None"), ("je m'ennuie trop", "Bored"), ("j'ai rien à faire aujourd'hui", "Bored"),
        };
        foreach (var (text, expected) in cases6)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        foreach (var (text, kind) in new[]
        {
            ("on irait bien ensemble ?", ShipAskKind.Us), ("c'est qui mon âme sœur ?", ShipAskKind.BestMatch),
            ("c'est qui mon pire ship ?", ShipAskKind.WorstMatch), ("ship Luca et moi", ShipAskKind.Pair),
        })
            if (ShipAsk.Read(text)?.Kind != kind) Fail($"ShipAsk kind of \"{text}\" = {ShipAsk.Read(text)?.Kind}, expected {kind}");
        ShipIs("est-ce que Luca et moi on est compatibles ?", "Luca", "moi");
        ShipIs("compatibilité entre la pizza et l'ananas", "pizza", "ananas");

        // The sweep: everyday chat aimed at her (a reply, a mention) that asks nothing she has a specific
        // answer for. Anything specific here is a misfire: it must read None, Question or LowEffort.
        var misfires = 0;
        foreach (var text in neutral)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
            if (!generic.Contains(got)) { misfires++; Fail($"sweep: \"{text}\" read as {got}"); }
        }
        Console.WriteLine($"sweep: {neutral.Length} ordinary messages, {misfires} misfire(s)");

        // 13. Second sweep: slang and gaming shorthand, and her name said in passing.
        var slangMisfires = 0;
        foreach (var text in slang)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
            if (!generic.Contains(got)) { slangMisfires++; Console.WriteLine($"  slang: \"{text}\" -> {got}"); }
        }
        Console.WriteLine($"slang sweep: {slang.Length} messages, {slangMisfires} specific");

        foreach (var text in new[]
        {
            "syncs a posté le sondage", "le sondage de syncs est nul", "demande à syncs", "j'ai parlé avec syncs",
            "c'est syncs qui gère ça", "la sync a planté", "pour syncs c'est facile", "<@555> regarde ce que syncs a dit",
            "sans syncs on serait perdus", "grâce à syncs j'ai pas oublié",
        })
            if ((bool)addresses.Invoke(null, new object[] { text, 999UL })!) Fail($"AddressesHer(\"{text}\") is true for her name said in passing");

        // 14. Sharing: someone telling her about their day.
        foreach (var (text, expected) in new[]
        {
            ("J'ai mangé de la soupe", "Sharing"), ("bon, je suis allé au ciné", "Sharing"), ("on a perdu la game", "Sharing"),
            ("hier j'ai fini Elden Ring", "Sharing"), ("du coup je me suis acheté un clavier", "Sharing"), ("chui trop crevé", "Sharing"),
            ("moi j'ai dormi 12 heures", "Sharing"), ("je viens de rentrer du boulot", "Sharing"), ("on est sortis au resto", "Sharing"),
            ("mdr j'ai raté mon bus", "Sharing"),
            // Not sharing: earlier intents, questions, opinions and wishes, things said to the listener.
            ("j'ai mangé de la soupe ?", "Question"), ("je m'ennuie", "Bored"), ("je suis d'accord", "YouAreRight"),
            ("je pense que c'est nul", "None"), ("je veux jouer", "None"), ("je t'envoie le lien", "None"),
            ("je te rejoins", "None"), ("je sais pas", "Maybe"), ("je vais dormir", "Goodnight"), ("j'ai faim", "Sharing"),
            ("je t'aime", "Love"), ("je comprends rien", "Help"), ("on y va", "None"), ("je", "None"),
            ("Luca a mangé de la soupe", "None"), ("on s'appelle plus tard", "None"), ("on s'est fait battre", "Sharing"),
            ("je préfère Minecraft", "None"), ("j'en ai rien à faire", "None"), ("on joue à quoi ce soir", "None"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        var sharedInSweep = neutral.Count(t => readIntent.Invoke(null, new object[] { t })!.ToString() == "Sharing");
        Console.WriteLine($"sweep: {sharedInSweep} of {neutral.Length} ordinary messages now read as Sharing");

        // 15. Wider Sharing and Ship cues.
        foreach (var (text, expected) in new[]
        {
            // Sharing
            ("devine quoi, j'ai gagné le tournoi", "Sharing"), ("tu sais quoi, j'ai eu mon permis", "Sharing"),
            ("au fait, je me suis coupé les cheveux", "Sharing"), ("jsors du ciné", "Sharing"), ("jrentre du taf", "Sharing"),
            ("mon chat a vomi sur le clavier", "Sharing"), ("ma sœur m'a offert un jeu", "Sharing"), ("nous avons gagné le raid", "Sharing"),
            ("en vrai j'ai bien dormi", "Sharing"), ("omg j'ai drop une légendaire", "Sharing"), ("on était trop forts hier", "Sharing"),
            ("la semaine dernière j'ai fini le jeu", "Sharing"), ("mes parents sont partis en vacances", "Sharing"),
            ("mon micro marche pas", "None"), ("ma sœur arrive", "None"), ("tu sais quoi ?", "Question"), ("devine quoi", "None"),
            ("mon avis c'est que c'est nul", "None"), ("ton chat a vomi", "None"), ("je préfère le mode solo", "None"),
            // Ship
            ("le couple Luca et Sandra", "Ship"), ("couple Luca / Sandra ?", "Ship"), ("Luca ❤️ Sandra ?", "Ship"), ("Luca <3 Sandra ?", "Ship"),
            ("ship <@1> <@2>", "Ship"), ("<@999> <@1> <@2> compatibles ?", "Ship"), ("on va finir ensemble ?", "Ship"),
            ("tu nous vois en couple ?", "Ship"), ("on est un beau couple ?", "Ship"), ("qui irait bien avec moi ?", "Ship"),
            ("je vais finir avec qui ?", "Ship"), ("avec qui je devrais sortir ?", "Ship"), ("qui va m'épouser ?", "Ship"),
            ("c'est qui mon futur mari ?", "Ship"), ("Luca et Sandra vont ensemble ?", "Ship"), ("la personne faite pour moi ?", "Ship"),
            ("gg ❤️ bien joué", "None"), ("merci ❤️", "Thanks"), ("<@1> <@2> on fait un match ?", "Question"),
            ("un couple de lapins et de chats", "None"), ("on va ensemble au ciné ?", "Question"),
            ("je finis avec qui en équipe ?", "Question"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        ShipIs("ship <@1> <@2>", "<@1>", "<@2>");
        ShipIs("<@999> <@1> <@2> compatibles ?", "<@1>", "<@2>");
        ShipIs("le couple Luca et Sandra", "Luca", "Sandra");
        ShipIs("Luca ❤️ Sandra ?", "Luca", "Sandra");
        if (ShipAsk.Read("c'est qui mon futur mari ?")?.Kind != ShipAskKind.BestMatch) Fail("futur mari is not a best-match search");
        if (ShipAsk.Read("on va finir ensemble ?")?.Kind != ShipAskKind.Us) Fail("on va finir ensemble is not Us");
        ShipIs("syncs   Luca    et   Sandra ,   ça   match ?", "Luca", "Sandra");
        ShipIs("Luca　et　Sandra ça match ?", "Luca", "Sandra");
        // Crafted whitespace once kept CompatRegex and OpinionBeforePairRegex backtracking for minutes, on
        // the gateway task. Every reader of chat text must stay fast on it.
        {
            string Rep(string s, int n) => string.Concat(Enumerable.Repeat(s, n));
            var hostile = new[]
            {
                "syncs a" + Rep(" ", 600) + "et" + Rep(" ", 600) + "b" + Rep(" ", 600) + "z",
                "syncs a" + Rep("　", 600) + "avec" + Rep("　", 600) + "b" + Rep("　", 600) + "z",
                "a" + Rep(" ", 600) + "x" + Rep(" ", 600) + "b" + Rep(" ", 600) + "z?",
                "syncs " + Rep("a  et  ", 280) + "b" + Rep(" ", 40) + "!",
                "syncs " + Rep("a et ", 40),
                "syncs " + Rep("a x ", 50) + "z ton avis ?",
            };
            foreach (var text in hostile)
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                ShipAsk.Read(text);
                Opinion.Read(text);
                readIntent.Invoke(null, new object[] { text });
                if (watch.ElapsedMilliseconds > 500)
                    Fail($"reading {text.Length} hostile chars took {watch.ElapsedMilliseconds} ms");
            }
        }
        foreach (var pool in new[] { "SharingShrugLines" })
            if (((string[])Field(pool)).Length != 32) Fail($"{pool} has {((string[])Field(pool)).Length} lines, expected 32");
        var sharingIntent = ((IDictionary)Field("IntentLines"))[Enum.Parse(intentType, "Sharing")];
        if (((string[])sharingIntent!).Length != 32) Fail($"IntentLines[Sharing] has {((string[])sharingIntent).Length} lines, expected 32");
    }
}
