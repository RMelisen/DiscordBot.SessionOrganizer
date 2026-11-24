using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 50–51. Requests made to her in chat, and the /debug answers misses of November 2026.
public static class ChatRequestChecks
{
    public static void Run()
    {
        // 50. Requests made to her in chat (ChatRequest): the date found inside the sentence, the title as
        // written, vote options, team counts; and the cues that make them requests in the first place.
        {
            var crNow = AppTime.AtWallClock(new DateTime(2026, 11, 17, 15, 0, 0)); // a Tuesday afternoon
            DateTimeOffset Paris(int month, int day, int hour, int minute = 0) => AppTime.AtWallClock(new DateTime(2026, month, day, hour, minute, 0));

            foreach (var (text, title, at) in new (string, string, DateTimeOffset?)[]
            {
                ("syncs organise un Valorant samedi 21h", "Valorant", Paris(11, 21, 21)),
                ("tu peux créer une session de Among Us demain soir stp ?", "Among Us", Paris(11, 18, 21)),
                ("on se fait un lol 5v5 vendredi à 20h30 ?", "Lol 5v5", Paris(11, 20, 20, 30)),
                ("organise une session pour samedi", "", Paris(11, 21, 21)),
                ("organise une session", "", null),
                ("organise un raid", "Raid", null),
                ("organise un jeu de société", "Jeu de société", null),
                ("Syncs, organise une session pour le défi créatif samedi 21h", "Défi créatif", Paris(11, 21, 21)),
                ("organise une session pour jouer à Valo samedi 21h", "Valo", Paris(11, 21, 21)),
                ("organise un escape game samedi 14h", "Escape game", Paris(11, 21, 14)),
                ("lance une partie de Minecraft le 25/12 à 20:30", "Minecraft", Paris(12, 25, 20, 30)),
                ("<@123> programme un Gartic Phone dimanche 18h", "Gartic Phone", Paris(11, 22, 18)),
                ("organise une session Les Sims samedi 21h", "Les Sims", Paris(11, 21, 21)),
                ("lance une partie de A Way Out demain soir", "A Way Out", Paris(11, 18, 21)),
                ("Organise un Valorant samedi", "Valorant", Paris(11, 21, 21)),
                ("Syncs. Le Valorant de samedi, tu l'organises ?", "Valorant", Paris(11, 21, 21)),
            })
            {
                var got = ChatRequest.ReadSession(text, crNow);
                if (got.Title != title || got.At != at) Fail($"ReadSession(« {text} ») = (« {got.Title} », {got.At}), expected (« {title} », {at})");
            }

            // What else a session request says: places, length, every week, kind; read out of the title and date.
            foreach (var (text, title, at, places, minutes, weekly, category) in new (string, string, DateTimeOffset?, int?, int?, bool, ProjectSYNCS.Models.SessionCategory?)[]
            {
                ("syncs organise un valo samedi 21h à 5 joueurs", "Valo", Paris(11, 21, 21), 5, null, false, null),
                ("organise une session Valorant samedi 21h, 5 places max", "Valorant", Paris(11, 21, 21), 5, null, false, null),
                ("organise un raid samedi 21h avec 6 personnes max", "Raid", Paris(11, 21, 21), 6, null, false, null),
                ("organise un among us demain soir jusqu'à 8 joueurs", "Among us", Paris(11, 18, 21), 8, null, false, null),
                ("lance une partie de Minecraft le 25/12 à 20:30 pour 4", "Minecraft", Paris(12, 25, 20, 30), 4, null, false, null),
                ("organise un lol samedi 21h pendant 3h", "Lol", Paris(11, 21, 21), null, 180, false, null),
                ("organise un lol samedi de 21h à 23h30", "Lol", Paris(11, 21, 21), null, 150, false, null),
                ("organise un valo samedi 21h pour 2h", "Valo", Paris(11, 21, 21), null, 120, false, null),
                ("organise une session de 90 minutes samedi 21h", "", Paris(11, 21, 21), null, 90, false, null),
                ("organise un valo samedi pour 21h", "Valo", Paris(11, 21, 21), null, null, false, null),
                ("organise un valo tous les samedis à 21h", "Valo", Paris(11, 21, 21), null, null, true, null),
                ("organise un valo chaque samedi 21h", "Valo", Paris(11, 21, 21), null, null, true, null),
                ("organise une session hebdo de valo le mercredi à 20h", "Valo", Paris(11, 18, 20), null, null, true, null),
                ("organise une soirée film vendredi 21h", "Film", Paris(11, 20, 21), null, null, false, ProjectSYNCS.Models.SessionCategory.Movie),
                ("organise un resto samedi 20h", "Resto", Paris(11, 21, 20), null, null, false, ProjectSYNCS.Models.SessionCategory.Activity),
                ("organise une session jeux de société vendredi 20h pour 6 personnes", "Jeux de société", Paris(11, 20, 20), 6, null, false, null),
                ("organise un valo samedi à 5", "Valo", Paris(11, 21, 5), null, null, false, null),
            })
            {
                var got = ChatRequest.ReadSession(text, crNow);
                if (got.Title != title || got.At != at || got.Places != places || got.Minutes != minutes || got.Weekly != weekly || got.Category != category)
                    Fail($"ReadSession(« {text} ») = {got}, expected (« {title} », {at}, {places}, {minutes}, {weekly}, {category})");
            }

            foreach (var (text, game, at) in new (string, string, DateTimeOffset?)[]
            {
                ("lance un tonight pour Minecraft à 22h", "Minecraft", Paris(11, 17, 22)),
                ("qui est chaud ce soir ?", "", Paris(11, 17, 21)),
                ("qui est chaud pour du Valorant ce soir", "Valorant", Paris(11, 17, 21)),
                ("lance un appel pour ce soir samedi", "", null),
            })
            {
                var got = ChatRequest.ReadTonight(text, crNow);
                if (got.Game != game || got.At != at) Fail($"ReadTonight(« {text} ») = (« {got.Game} », {got.At}), expected (« {game} », {at})");
            }

            foreach (var (text, what, due) in new (string, string, DateTimeOffset?)[]
            {
                ("rappelle-moi demain 9h de sortir les poubelles", "sortir les poubelles", Paris(11, 18, 9)),
                ("rappelle-moi de sortir les poubelles demain 9h", "sortir les poubelles", Paris(11, 18, 9)),
                ("tu peux me rappeler dans 2h d'appeler maman ?", "appeler maman", crNow.AddHours(2)),
                ("rappelle moi à 18h la lessive stp", "la lessive", Paris(11, 17, 18)),
                ("fais-moi penser à appeler le garage vendredi", "appeler le garage", Paris(11, 20, 9)),
                ("rappelle-moi de manger", "manger", null),
                ("Syncs, rappelle moi de pas écouter Ina dans 10 minutes", "pas écouter Ina", crNow.AddMinutes(10)),
                ("rappelle-moi de ne pas oublier le pain", "ne pas oublier le pain", null),
                ("tu peux pas me rappeler de manger ?", "manger", null),
                ("rappelle-moi de me coucher à minuit", "me coucher", Paris(11, 18, 0)),
                ("rappelle-moi de faire la vaisselle", "faire la vaisselle", null),
                ("rappelle moi de rappeler Paul demain 10h", "rappeler Paul", Paris(11, 18, 10)),
                ("rappelle-moi de dire à Léa que c'est ok", "dire à Léa que c'est ok", null),
                ("fais-moi penser à faire les courses", "faire les courses", null),
                ("rappelle moi de faire ce que tu dis", "faire ce que tu dis", null),
                ("rappelle-moi que j'ai un rdv à 17h", "j'ai un rdv", Paris(11, 17, 17)),
                ("rappelle-moi de te répondre s'il te plaît", "te répondre", null),
                ("préviens-moi de pas oublier le pain dans 1h stp", "pas oublier le pain", crNow.AddHours(1)),
            })
            {
                var got = ChatRequest.ReadReminder(text, crNow);
                if (got.Text != what || got.Due != due) Fail($"ReadReminder(« {text} ») = (« {got.Text} », {got.Due}), expected (« {what} », {due})");
            }

            foreach (var (text, title, options) in new (string, string, string[])[]
            {
                ("fais un sondage : pizza, sushi ou kebab", "pizza ou sushi ou kebab ?", ["pizza", "sushi", "kebab"]),
                ("lance un vote pizza ou sushi", "pizza ou sushi ?", ["pizza", "sushi"]),
                ("lance un sondage pour ce soir : Valorant, LoL", "Ce soir", ["Valorant", "LoL"]),
                ("tu peux faire un sondage entre Among Us et Gartic stp ?", "Among Us ou Gartic ?", ["Among Us", "Gartic"]),
                ("lance un sondage", "", []),
                ("lance un vote pizza", "", []),
                // A question: its title as asked, its options from what it asks.
                ("Syncs, crée un sondage pour savoir qui voudrait participer au défi créatif",
                    "Qui voudrait participer au défi créatif ?", ["Participe", "Ne participe pas"]),
                ("fais un sondage pour savoir qui vient samedi stp", "Qui vient samedi ?", ["Vient", "Ne vient pas"]),
                ("lance un sondage : qui est chaud pour un valo ce soir ?", "Qui est chaud pour un valo ce soir ?", ["Chaud", "Pas chaud"]),
                ("fais un sondage pour demander qui veut une pizza", "Qui veut une pizza ?", ["Moi", "Pas moi"]),
                ("fais un sondage pour savoir qui est le meilleur joueur", "Qui est le meilleur joueur ?", []),
                ("lance un sondage pour savoir si on joue samedi", "On joue samedi ?", ["Oui", "Non"]),
                ("fais un sondage pour savoir s'il faut changer de jeu", "Il faut changer de jeu ?", ["Oui", "Non"]),
                ("fais un sondage pour savoir si on joue à Valo ou LoL", "On joue à Valo ou LoL ?", ["Valo", "LoL"]),
                ("fais un sondage pour savoir si on joue à Among Us ou à Gartic", "On joue à Among Us ou à Gartic ?", ["Among Us", "Gartic"]),
                ("fais un sondage pour savoir si on mange des pizzas ou des sushis", "On mange des pizzas ou des sushis ?", ["pizzas", "sushis"]),
                ("fais un sondage pour savoir si on joue samedi ou dimanche", "On joue samedi ou dimanche ?", ["samedi", "dimanche"]),
                ("fais un sondage pour savoir si on reste ou on part", "On reste ou on part ?", ["On reste", "on part"]),
                ("fais un sondage pour savoir si on joue à Valo, LoL ou CS", "On joue à Valo, LoL ou CS ?", ["Valo", "LoL", "CS"]),
                ("fais un sondage pour savoir si on joue ce soir ou pas", "On joue ce soir ou pas ?", ["Oui", "Non"]),
                ("fais un sondage pour savoir quel jeu on fait vendredi", "Quel jeu on fait vendredi ?", []),
                ("fais un sondage pour savoir où on mange", "Où on mange ?", []),
                ("fais un sondage pour savoir ce qu'on mange ce soir", "Qu'est-ce qu'on mange ce soir ?", []),
                ("fais un sondage sur qui participe au tournoi", "Qui participe au tournoi ?", ["Participe", "Ne participe pas"]),
                ("fais un sondage pour savoir si on joue à Valo ou à LoL ce soir", "On joue à Valo ou à LoL ce soir ?", ["Valo", "LoL"]),
                ("fais un sondage pour savoir qui sera là vendredi", "Qui sera là vendredi ?", ["Présent", "Absent"]),
                ("fais un sondage pour savoir à quoi on joue ce soir", "À quoi on joue ce soir ?", []),
                ("fais un sondage: on joue à quoi ce soir ?", "On joue à quoi ce soir ?", []),
                ("fais un sondage : on garde le même jeu ?", "On garde le même jeu ?", ["Oui", "Non"]),
                ("fais un sondage pour savoir est-ce que vous venez samedi", "Vous venez samedi ?", ["Oui", "Non"]),
                ("fais un sondage pour savoir qui vient : Valo ou LoL", "Qui vient ?", ["Valo", "LoL"]),
                ("fais un sondage oui ou non pour le défi créatif", "Défi créatif", ["Oui", "Non"]),
                ("lance un sondage pour le défi créatif", "Défi créatif", []),
                ("lance un sondage : Valorant, LoL ?", "Valorant ou LoL ?", ["Valorant", "LoL"]),
                // Straight after the vote word, « qui » is a relative and « ou » joins options.
                ("lance un sondage qui dure deux jours", "", []),
                ("lance un vote pizza ou sushi stp", "pizza ou sushi ?", ["pizza", "sushi"]),
            })
            {
                var got = ChatRequest.ReadVote(text);
                if (got.Title != title || !got.Options.SequenceEqual(options))
                    Fail($"ReadVote(« {text} ») = (« {got.Title} », [{string.Join(", ", got.Options)}])");
            }

            foreach (var (text, count, balanced) in new (string, int, bool)[]
            {
                ("fais 3 équipes équilibrées", 3, true), ("fais des équipes", 2, false), ("fais les teams en quatre", 4, false),
                ("tu peux faire deux équipes équitables ?", 2, true), ("fais 12 équipes", 2, false),
            })
            {
                var got = ChatRequest.ReadTeams(text);
                if (got.Count != count || got.Balanced != balanced) Fail($"ReadTeams(« {text} ») = {got}");
            }

            foreach (var (text, expected) in new[]
            {
                ("organise un valo samedi 21h", "Request"), ("lance un among us demain", "Request"),
                ("on se fait un valo samedi 21h ?", "Request"), ("tu peux organiser un lol vendredi ?", "Request"),
                ("fais-moi penser à appeler maman", "Request"), ("ça vous dit un Gartic dimanche 18h ?", "Request"),
                ("on fait une session samedi ?", "NextSession"), ("organise un jeu", "None"),
                ("syncs organise un valo samedi 21h", "Request"), ("syncs lance un sondage pizza ou sushi", "Request"),
                ("Syncs, crée un sondage pour savoir qui voudrait participer au défi créatif", "Request"),
            })
            {
                var got = readIntent.Invoke(null, new object[] { text })!.ToString();
                if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
            }
            foreach (var (text, topic) in new[]
            {
                ("organise un valo ce soir", "Tonight"), ("organise un valo samedi 21h", "Session"),
                ("fais-moi penser à appeler maman", "Reminder"), ("on se fait un valo samedi 21h ?", "Session"),
                ("lance un among us demain", "Session"), ("fais gaffe demain", null), ("on se fait un valo ?", null),
                ("tu peux organiser un truc ?", "Other"), ("syncs, organise un valo samedi 21h", "Session"),
                ("syncs organise les sessions le samedi", null), ("syncs organise un valo samedi 21h", "Session"),
                ("syncs lance un sondage pizza ou sushi", "Poll"),
                ("Syncs, crée un sondage pour savoir qui voudrait participer au défi créatif", "Poll"),
                ("syncs, fais 3 équipes", "Teams"), ("rappelle-moi demain 9h de manger syncs", "Reminder"),
            })
            {
                var got = readTopic.Invoke(null, new object[] { text })?.ToString();
                if (got != topic) Fail($"ReadRequestTopic(\"{text}\") = {got}, expected {topic}");
            }

            // Her offers say what she read ({2}), and never open on it: « une session » or « ce soir 21:00 »
            // would start the sentence in lower case. Every topic she acts on with a button has its pool.
            foreach (var (name, lines) in Pools("RequestActionLines").Concat(Pools("OwnerRequestActionLines")))
                foreach (var l in lines)
                    if (!l.Contains("{2}") || l.StartsWith("{2}")) Fail($"{name}: {{2}} missing or first: {l}");
            var actionTopics = ((IDictionary)Field("RequestActionLines")).Keys.Cast<object>().Select(k => k.ToString()).ToHashSet();
            foreach (var t in new[] { "Session", "Tonight", "Poll" })
                if (!actionTopics.Contains(t)) Fail($"RequestActionLines: no pool for {t}");
            foreach (var l in (string[])Field("RequestNotYoursLines"))
                if (Sticker.Strip(l) != l || l.Contains('{')) Fail($"RequestNotYoursLines: tagged or formatted: {l}");
        }

        // 51. The /debug answers misses of November 2026: « où es-tu », praise for a line, invitations, messages
        // to pass on (Relay), and what is said about her when her name comes up untagged (ReadNameTalk).
        {
            foreach (var (text, expected) in new[]
            {
                // Where she is.
                ("Ou es-tu Syncs ?", "WhatAreYouDoing"), ("où es-tu ?", "WhatAreYouDoing"), ("t'es où ?", "WhatAreYouDoing"),
                ("tu es où syncs", "WhatAreYouDoing"),
                // Invitations, and their negation.
                ("je t'invite", "Invite"), ("syncs je t'invite à mon anniv", "Invite"), ("t'es invitée ce soir", "Invite"),
                ("Viens je t'attends, on mangera des petits gâteaux 😚👌🏼", "Invite"), ("tu veux venir jouer avec nous ?", "Invite"),
                ("tu peux venir jouer avec nous ?", "Invite"), ("viens jouer", "Invite"), ("rejoins-nous", "Invite"),
                ("ben voilà pourquoi on ne t'invite pas", "Uninvite"), ("on t'invite pas", "Uninvite"), ("t'es pas invitée", "Uninvite"),
                ("je te désinvite", "Uninvite"), ("je t'invite plus", "Uninvite"), ("personne t'invite", "Uninvite"),
                ("tu veux pas venir ?", "Request"), ("tu viens ce soir ?", "Question"), ("viens voir", "None"),
                // Messages to pass on: what is passed is someone else's words, so nothing in it is read.
                ("SYNCS dis à <@95119591247716352> que c'est un caca", "Relay"), ("dis à <@555> que je l'aime", "Relay"),
                ("tu peux dire à <@555> qu'il est en retard ?", "Relay"), ("dis bonjour à <@555>", "Relay"),
                ("<@999> dis à <@555> que t'es nulle", "Relay"), ("dis à <@555> : t'as gagné", "Relay"),
                ("j'ai dit à <@555> que c'est un caca", "Sharing"), ("dis-moi que tu m'aimes", "DoYouLoveMe"), ("dis à syncs de lancer un sondage", "None"),
                ("dis-lui que j'ai raison", "TakeSides"),
                // Refined after a sweep: more ways to pass something on, and invitations that aren't.
                ("syncs, dit à <@555> qu'il est nul", "Relay"), ("syncs dis à <@555> de venir", "Relay"),
                ("syncs préviens <@555> que la session commence", "Relay"), ("syncs rappelle à <@555> que c'est ce soir", "Relay"),
                ("syncs souhaite bonne nuit à <@555>", "Relay"), ("syncs dis merci à <@555>", "Relay"),
                ("syncs souhaite un bon anniversaire à <@555>", "Relay"), ("syncs dis lui <@555> que t'es là", "Relay"),
                ("je lui ai dit à <@555> que c'était nul", "Sharing"), ("il faut dire à <@555> que c'est ce soir", "None"),
                ("<@555> dis à syncs que c'est un caca", "None"),
                ("syncs viens en vocal", "Invite"), ("syncs viens nous voir", "Invite"), ("syncs on t'a pas invitée ?", "Uninvite"),
                ("syncs je t'invite plus tard", "Invite"), ("syncs je t'invite à voter", "None"), ("je t'invite à lire le règlement", "None"),
                ("je t'invite à te calmer", "None"), ("je t'invite à manger", "Invite"),
                ("syncs t'invites qui ce soir ?", "Question"), ("jouer avec nous c'est cool", "None"),
            })
            {
                var got = readIntent.Invoke(null, new object[] { text })!.ToString();
                if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
            }

            // Praise for a line or for her answer reads as kind; the quiz's « bonne réponse » doesn't.
            foreach (var (text, expected) in new[]
            {
                ("Good bit", "Nice"), ("nice one", "Nice"), ("Je prefere cette réponse", "Nice"), ("bien trouvé", "Nice"),
                ("bonne réponse", "None"), ("good bot", "None"), ("je préfère Valo", "None"),
            })
                if (Mood(text) != expected) Fail($"Analyze(\"{text}\") = {Mood(text)}, expected {expected}");

            foreach (var (text, kind, target, said, lead, named) in new (string, RelayKind?, ulong, string, ulong?, bool)[]
            {
                ("SYNCS dis à <@555> que c’est un caca", RelayKind.Message, 555, "que c’est un caca", null, true),
                ("dis à <@555> qu'il est beau syncs !", RelayKind.Message, 555, "qu'il est beau", null, true),
                ("<@999> tu peux dire à <@!555> que la session commence", RelayKind.Message, 555, "que la session commence", 999, false),
                ("syncs, répète à <@555> : t'as gagné", RelayKind.Message, 555, ": « t'as gagné »", null, true),
                ("syncs stp va dire à <@555> que je l'aime", RelayKind.Message, 555, "que je l'aime", null, true),
                ("dis bonjour à <@555>", RelayKind.Greeting, 555, "bonjour", null, false),
                ("syncs passe le bonjour à <@555> de ma part", RelayKind.Greeting, 555, "bonjour", null, true),
                ("fais un coucou à <@555>", RelayKind.Greeting, 555, "coucou", null, false),
                ("syncs, envoie des bisous à <@555> stp", RelayKind.Greeting, 555, "bisous", null, true),
                ("syncs dis à <@555> d'arrêter", RelayKind.Message, 555, "d'arrêter", null, true),
                ("syncs souhaite un joyeux anniversaire à <@555>", RelayKind.Greeting, 555, "joyeux anniversaire", null, true),
                ("rappelle <@555> demain", null, 0, "", null, false),
                ("j'ai dit à <@555> que c'est un caca", null, 0, "", null, false),
                ("il dit à <@555> que c'est un caca", null, 0, "", null, false),
                ("dis à <@555> que", null, 0, "", null, false),
                ("dis à Luca que c'est un caca", null, 0, "", null, false),
                ("dis à <@555> que " + new string('a', 400), null, 0, "", null, false),
                ("dis-moi que tu m'aimes", null, 0, "", null, false),
            })
            {
                var got = Relay.Read(text);
                var ok = kind is null ? got is null
                    : got is not null && got.Kind == kind && got.TargetId == target && got.Said == said && got.LeadId == lead && got.Named == named;
                if (!ok) Fail($"Relay.Read(\"{text}\") = {got}");
            }
            // Crafted whitespace stays fast (the patterns allow \s next to .+).
            var relayWatch = System.Diagnostics.Stopwatch.StartNew();
            Relay.Read("dis à <@555>" + new string(' ', 250) + "que" + new string(' ', 40));
            Relay.Read("syncs" + string.Concat(Enumerable.Repeat(" ,", 140)));
            if (relayWatch.ElapsedMilliseconds > 500) Fail($"Relay.Read is slow on hostile whitespace: {relayWatch.ElapsedMilliseconds} ms");

            var readNameTalk = cues.GetMethod("ReadNameTalk", BindingFlags.Public | BindingFlags.Static)!;
            foreach (var (text, expected) in new[]
            {
                ("Faites bien gaffe a ce que vous dites sur SYNCS", "Defended"), ("laissez syncs tranquille bande de nuls", "Defended"),
                ("touchez pas à Syncs", "Defended"), ("soyez gentils avec sync", "Defended"),
                ("j'aimerais faire deux trois tests sur SYNCS puis je suis dispo", "WorkOnHer"), ("je vais update syncs ce soir", "WorkOnHer"),
                ("je bosse sur syncs", "WorkOnHer"), ("une mise à jour de syncs arrive", "WorkOnHer"),
                ("Et syncs vous la shippez avec qui ? <:witch_eheh:1181617446252195990>", "Ship"),
                ("Syncs est juste jalouse elle veut pas me ship <:giga_laugh:1482304998871138346>", "Ship"),
                ("syncs a un crush ou quoi", "Ship"), ("S.Y.N.C.S est amoureuse", "Ship"),
                ("syncs a posté le sondage", "None"), ("Tu joues sans Syncs", "None"), ("<@345917214966415362> ajoute Syncs à MC", "None"),
                ("mon copain dit que syncs a planté", "None"), ("Marie et syncs", "None"), ("les ships de /ship sont nuls", "None"),
                // Digs that aren't insults; the ambiguous ones stay puzzled lines.
                ("A côté de la plaque comme syncs", "Dig"), ("je trouve syncs beaucoup trop présente dans les convos", "Dig"),
                ("tu as créé syncs, mais tu es devenu son esclave", "Dig"), ("Syncs qui s'en fiche de ce que tu vis mdr", "Dig"),
                ("syncs sert à rien", "Dig"), ("syncs déconne encore", "None"), ("j'en ai marre de syncs", "Dig"),
                ("syncs sert pas à rien", "None"), ("Plus de syncs", "None"), ("syncs sert à organiser les sessions", "None"),
                ("je comprends rien à syncs", "None"),
                // Refined after a sweep: a subject dig needs her name as its subject; ship talk is her love
                // life, not /ship's results; a version asked about announces nothing.
                ("syncs elle bug tout le temps", "Dig"), ("syncs m'agace", "Dig"), ("syncs comprend rien", "Dig"),
                ("syncs elle est pas drôle", "Dig"), ("on s'en fiche de syncs", "Dig"), ("syncs est vraiment à côté de la plaque", "Dig"),
                ("syncs, ce jeu me saoule", "None"), ("Luca parle trop, hein syncs", "None"), ("syncs est pas trop présente", "None"),
                ("syncs se moque de moi", "None"), ("ça déconne, syncs a planté ?", "None"), ("syncs, elle est où la session ?", "None"),
                ("syncs a ship Luca et Sandra", "None"), ("le ship de syncs était nul", "None"), ("le bot syncs fait des ships", "None"),
                ("syncs on la ship avec wall-e ?", "Ship"), ("shippez syncs avec quelqu'un", "Ship"), ("syncs est en couple avec quokka ?", "Ship"),
                ("c'est quoi la version de syncs ?", "None"), ("je vais ajouter un truc à syncs", "WorkOnHer"), ("je code syncs", "WorkOnHer"),
            })
            {
                var got = readNameTalk.Invoke(null, new object[] { text })!.ToString();
                if (got != expected) Fail($"ReadNameTalk(\"{text}\") = {got}, expected {expected}");
            }

            // The relay pools: who to tell ({2}) in every line; what to tell right after « dire » (it may be
            // « qu'il… » or « : « … » »); a greeting always inside « ».
            foreach (var l in (string[])Field("RelayLines"))
                if (!l.Contains("{2}") || !(l.Contains("dire {3}") || l.Contains("dise {3}")))
                    Fail($"RelayLines: {{2}} missing or {{3}} not after « dire »: {l}");
            foreach (var l in (string[])Field("RelayGreetingLines"))
                if (!l.Contains("{2}") || !l.Contains("« {3} »")) Fail($"RelayGreetingLines: {{2}} missing or {{3}} unquoted: {l}");
            foreach (var l in (string[])Field("RelayRefusedLines"))
                if (l.Contains("{3}")) Fail($"RelayRefusedLines: quotes the insult it refuses: {l}");
        }
    }
}
