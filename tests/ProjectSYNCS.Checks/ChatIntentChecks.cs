using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// 16–21. Every intent under six variants, mentions and quotes, Disbelief, the widened weak
// intents, Down/ShouldI/Joke/Blame/Love, yes and no under her questions.
public static class ChatIntentChecks
{
    public static void Run()
    {
        // 16. Every intent under six variants: the reading must not depend on caps, a tag, her name as an
        // address at either end, a trailing emote, or emphasis.
        var byIntent = new (string Intent, string[] Samples)[]
        {
            ("Love", new[] { "je t'aime", "jtm", "je t'adore", "je suis amoureux de toi", "tu veux sortir avec moi ?", "je t'apprécie beaucoup", "love you" }),
            ("Apology", new[] { "pardon", "désolé", "dsl j'étais pas là", "my bad", "c'est ma faute", "j'ai pas fait exprès", "toutes mes excuses" }),
            ("HowAreYou", new[] { "ça va ?", "comment tu vas ?", "tu vas bien ?", "ça roule ?", "bien dormi ?", "comment va la vie ?", "et toi, ça va ?" }),
            ("AreYouThere", new[] { "t'es là ?", "tu dors ?", "y a quelqu'un ?", "t'es toujours là ?", "réveille-toi" }),
            ("WhoAreYou", new[] { "t'es qui ?", "qui es-tu ?", "tu sers à quoi ?", "comment tu t'appelles ?", "c'est qui ton créateur ?", "présente-toi" }),
            ("AboutHer", new[] { "t'as quel âge ?", "c'est quand ton anniv ?", "tu habites où ?", "c'est quoi ton jeu préféré ?", "tu aimes quoi comme musique ?" }),
            ("AreYouABot", new[] { "t'es un bot ?", "t'es une IA ?", "tu as des sentiments ?", "t'es humaine ?", "es-tu une IA ?" }),
            ("Crush", new[] { "t'es célibataire ?", "t'as un crush ?", "t'es amoureuse ?", "c'est qui ton crush ?" }),
            ("WhatAreYouDoing", new[] { "tu fais quoi ?", "quoi de neuf ?", "qu'est-ce que tu fabriques ?", "tu t'ennuies ?", "t'es où ?" }),
            ("Opinion", new[] { "t'en penses quoi de la pizza ?", "ton avis sur Valorant ?", "tu aimes les chats ?", "tu préfères Minecraft ou Fortnite ?" }),
            ("Ship", new[] { "ship Luca et Sandra", "Luca et moi, ça match ?", "on irait bien ensemble ?", "c'est qui mon âme sœur ?", "tu penses quoi de <@111> et <@222> ?", "le couple Luca et Sandra" }),
            ("Help", new[] { "comment ça marche ?", "tu sais faire quoi ?", "aide-moi", "j'ai besoin d'aide", "je comprends rien" }),
            ("Request", new[] { "tu peux organiser une session ?", "organise une soirée", "lance un sondage", "tu peux me rappeler de manger ?", "fais des équipes" }),
            ("Bored", new[] { "je m'ennuie", "on s'ennuie", "on fait quoi ?", "y a rien à faire", "je sais pas quoi faire" }),
            ("Goodnight", new[] { "bonne nuit", "je vais dormir", "dodo", "je vais me coucher", "dors bien", "beaux rêves" }),
            ("Goodbye", new[] { "à plus", "à demain", "bye", "je dois y aller", "bon j'y vais", "allez salut", "a+" }),
            ("Thanks", new[] { "merci", "merci beaucoup", "c'est gentil", "thx", "je te remercie" }),
            ("Disbelief", new[] { "fais genre", "mon œil", "mais oui c'est ça", "avoue", "t'es grillée", "on te croit" }),
            ("Maybe", new[] { "peut-être", "on verra", "jsp", "je sais pas", "pourquoi pas", "probablement" }),
            ("YouAreRight", new[] { "t'as raison", "exactement", "bien vu", "je confirme", "j'avoue", "tellement vrai" }),
            ("YouAreWrong", new[] { "t'as tort", "c'est faux", "n'importe quoi", "tu délires", "pas du tout" }),
            ("Laughter", new[] { "mdr", "ptdr", "hahaha", "😂", "jpp", "je suis mort de rire", "t'es trop drôle" }),
            ("Question", new[] { "c'est quoi le jeu de ce soir ?", "pourquoi t'as dit ça", "c'est quoi le code du lobby ?", "il est où le lien ?" }),
            ("DemandApology", new[] { "excuse-toi", "dis pardon", "tu me dois des excuses", "demande pardon", "tu t'excuses ?", "je veux des excuses" }),
            ("ShutUp", new[] { "chut", "tais-toi", "silence", "la ferme", "arrête de parler", "laisse-nous tranquille", "chuuut" }),
            ("AnswerMe", new[] { "réponds à ma question", "t'as pas répondu", "tu esquives", "j'attends ta réponse", "pourquoi tu réponds pas à ma question ?" }),
            ("MyStats", new[] { "je suis quel niveau ?", "j'ai combien de cailloux ?", "comment va mon plynling ?", "mon niveau ?", "mes stats ?" }),
            ("TakeSides", new[] { "qui a raison, moi ou Luca ?", "qui a raison entre Luca et Sandra ?", "qui a raison ?", "dis-lui que j'ai raison" }),
            ("RateMe", new[] { "note-moi", "tu me notes ?", "je vaux combien ?", "je suis beau ?", "tu me trouves comment ?" }),
            ("NextSession", new[] { "c'est quand la prochaine session ?", "y a une session ce soir ?", "on joue ce soir ?", "qui vient ce soir ?", "y a quoi de prévu ?" }),
            ("Roll", new[] { "lance un dé", "1d20", "lance les dés", "choisis un nombre entre 1 et 10", "un nombre au hasard ?", "jette deux dés" }),
            ("Sharing", new[] { "j'ai mangé de la soupe", "on a perdu la game", "je viens de rentrer du boulot", "devine quoi, j'ai gagné le tournoi", "mon chat a vomi sur le clavier" }),
            ("LowEffort", new[] { "ok", "ah bon", "bah oui", "d'accord", "hein" }),
            ("Down", new[] { "je suis triste", "j'ai passé une sale journée", "ça va pas fort", "j'en peux plus", "j'ai pas le moral", "je me sens vraiment mal" }),
            ("ShouldI", new[] { "je devrais aller dormir ?", "je l'achète ?", "Valo ou LoL ?", "je joue ou je dors ?", "j'y vais ou pas ?" }),
            ("Joke", new[] { "raconte une blague", "fais-moi rire", "t'as une blague ?", "une blague ?", "tu peux me raconter une blague ?" }),
            ("FunFact", new[] { "un fun fact ?", "dis-moi un truc", "apprends-moi un truc", "raconte-moi quelque chose" }),
            ("Blame", new[] { "c'est la faute de qui ?", "c'est qui le coupable ?", "à qui la faute ?" }),
            ("DoYouLoveMe", new[] { "tu m'aimes ?", "tu m'aimes bien ?", "tu me détestes ?", "tu m'en veux ?", "je suis ton préféré ?" }),
            ("RememberMe", new[] { "tu te souviens de moi ?", "comment je m'appelle ?", "tu me connais ?", "tu sais qui je suis ?", "c'est quoi mon pseudo ?" }),
            ("AreYouUpset", new[] { "t'es fâchée ?", "tu boudes ?", "t'es triste ?", "t'es jalouse ?", "tu me fais la tête ?" }),
            ("WhatsYourProblem", new[] { "c'est quoi ton problème", "c quoi ton problème", "quel est ton problème", "t'as un problème avec moi" }),
            ("YouDontKnowMe", new[] { "c'est mal me connaître", "tu me connais pas", "tu sais rien de moi", "tu sais pas qui je suis" }),
            ("BackAtYou", new[] { "je te retourne le compliment", "toi-même", "pareil pour toi", "c'est celui qui dit qui est" }),
            ("Relay", new[] { "dis à <@555> que c'est un caca", "tu peux dire à <@555> qu'il est en retard ?", "dis bonjour à <@555>", "répète à <@555> : t'as gagné" }),
            ("Invite", new[] { "je t'invite", "viens jouer avec nous", "tu veux venir ?", "t'es invitée", "viens je t'attends" }),
            ("Uninvite", new[] { "on t'invite pas", "t'es pas invitée", "je te désinvite", "on ne t'invite jamais" }),
        };

        string WithTrailingName(string s) =>
            s.EndsWith('?') ? s[..^1].TrimEnd() + " syncs ?" : s + " syncs";

        var variants = new (string Name, Func<string, string> Make)[]
        {
            ("as is", s => s),
            ("caps", s => s.ToUpperInvariant()),
            ("tagged", s => "<@999> " + s),
            ("name first", s => "syncs, " + s),
            ("name last", WithTrailingName),
            ("emote", s => s + " 🙂"),
            ("emphasis", s => s.EndsWith('?') ? s : s + " !!"),
        };

        var variantFailures = new List<string>();
        var checkedCount = 0;
        foreach (var (intent, samples) in byIntent)
        foreach (var sample in samples)
        foreach (var (name, make) in variants)
        {
            var text = make(sample);
            var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
            checkedCount++;
            if (got != intent) variantFailures.Add($"{intent,-15} {name,-11} \"{text}\" -> {got}");
        }
        Console.WriteLine($"variants: {checkedCount} readings, {variantFailures.Count} off");
        foreach (var f in variantFailures) Console.WriteLine("  " + f);
        if (variantFailures.Count > 0) Fail($"{variantFailures.Count} variant readings off");

        // Every intent the enum has is covered by the table above, so a new one can't ship untested. Yes and
        // No are read by ReadAnswer instead (section 21).
        var covered = byIntent.Select(b => b.Intent).Append("Yes").Append("No").ToHashSet();
        foreach (var v in Enum.GetNames(intentType).Where(n => n != "None"))
            if (!covered.Contains(v)) Fail($"intent {v} has no samples in the variant table");

        // Two intents in one message: which one wins.
        foreach (var (text, expected) in new[]
        {
            ("merci, bonne nuit", "Goodnight"), ("merci et à demain", "Goodbye"), ("désolé, je t'aime", "Love"), ("pardon, ça va ?", "Apology"),
            ("mdr t'as raison", "YouAreRight"), ("ok merci", "Thanks"), ("merci t'es là ?", "AreYouThere"), ("bonne nuit, je t'aime", "Love"),
            ("t'es là ? je m'ennuie", "AreYouThere"), ("je m'ennuie, on fait quoi ?", "Bored"), ("peut-être, je sais pas", "Maybe"),
            ("fais genre, t'as tort", "Disbelief"), ("avoue, t'as un crush ?", "Crush"), ("ça va ? tu fais quoi ?", "HowAreYou"),
            ("tu fais quoi ? je m'ennuie", "WhatAreYouDoing"), ("t'es qui ? t'es un bot ?", "WhoAreYou"),
            ("tu peux ship Luca et Sandra ?", "Ship"), ("tu peux m'aider ?", "Help"), ("tu peux m'aider à lancer un sondage ?", "Request"),
            ("je suis d'accord, merci", "YouAreRight"), ("haha merci", "Thanks"), ("dsl j'ai mangé de la soupe", "Apology"),
            ("j'ai mangé de la soupe, et toi ça va ?", "HowAreYou"), ("on fait quoi ce soir ? je m'ennuie", "Bored"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"priority: \"{text}\" = {got}, expected {expected}");
        }

        // A third sweep: longer, multi-sentence messages about other things, quotes and third parties.
        var longNeutral = new[]
        {
            "hier on a joué jusqu'à 3h du matin avec Luca, c'était n'importe quoi mais on a fini le donjon",
            "Luca m'a dit qu'il viendrait mais il a toujours pas répondu, il est vraiment pas fiable",
            "le patch de ce soir change tout le méta, les tanks sont devenus inutiles et les soigneurs trop forts",
            "je sais pas si je pourrai venir samedi, ça dépend de mon boulot, je vous redis",
            "« t'es qui toi ? » qu'il m'a dit, j'ai rien compris",
            "il a écrit « je t'aime » à sa copine dans le mauvais salon, mdr",
            "elle a demandé « ça va ? » à tout le monde sauf à moi",
            "Sandra a dit bonne nuit à 20h, elle est malade je crois",
            "on devrait faire un tournoi le mois prochain avec des équipes de quatre",
            "quelqu'un a vu mon casque ? je le trouve plus nulle part depuis hier",
            "la musique du boss final est incroyable, je l'écoute en boucle",
            "j'ai demandé à Luca s'il était célibataire, il a rougi",
            "le bot de musique a encore planté pendant la soirée",
            "faut que je pense à acheter des piles pour la manette demain",
            "si vous voulez on peut décaler la session à dimanche, moi ça me va",
        };
        foreach (var text in longNeutral)
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString()!;
            if (!ChatCueChecks.generic.Contains(got)) Fail($"long sweep: \"{text}\" read as {got}");
        }

        // 17. « Tu penses quoi de @x et @Y ? », quotes, and trailing decoration.
        foreach (var (text, expected) in new[]
        {
            ("Tu penses quoi de <@111> et <@222> ?", "Ship"), ("<@999> tu penses quoi de <@111> et <@222> ?", "Ship"),
            ("<@111> et <@222>, t'en penses quoi ?", "Ship"), ("tu penses quoi de moi et Luca ?", "Ship"),
            ("t'en penses quoi de Luca et Sandra ensemble ?", "Ship"), ("<@111> et <@222> ça match ?", "Ship"),
            ("t'en penses quoi de la pizza et des pâtes ?", "Opinion"), ("t'en penses quoi de Minecraft et Fortnite ?", "Opinion"),
            ("tu penses quoi de moi et de mes cheveux ?", "Opinion"), ("t'en penses quoi de <@111> ?", "Opinion"),
            ("il a écrit « je t'aime » à sa copine, mdr", "None"), ("« je t'aime »", "Love"), ("\"je t'aime\"", "Love"),
            ("> ça va ?\nil a dit ça hier", "None"), ("elle a dit \"bonne nuit\" à 20h", "None"),
            ("syncs", "None"), ("syncs ?", "AreYouThere"), ("syncs, 😂", "Laughter"), ("syncs a raison", "YouAreRight"),
            ("syncs, organise une session", "Request"), ("merci syncs", "Thanks"), ("allez salut syncs", "Goodbye"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        ShipIs("Tu penses quoi de <@111> et <@222> ?", "<@111>", "<@222>");
        ShipIs("<@999> <@111> et <@222> ça match ?", "<@111>", "<@222>");
        ShipIs("Luca et moi, ça match syncs ? 🙂", "Luca", "moi");
        if (Opinion.Read("t'en penses quoi de <@111> et <@222> ?") is { } pairOpinion && pairOpinion.Subject == "et")
            Fail("an opinion of two tags still reads « et »");
        if (Opinion.Read("t'en penses quoi de la pizza ? 🙂")?.Subject != "pizza") Fail($"trailing emoji kept in the subject: {Opinion.Read("t'en penses quoi de la pizza ? 🙂")?.Subject}");
        if (Opinion.Read("t'en penses quoi de la pizza syncs ?")?.Subject != "pizza") Fail("trailing name kept in the subject");

        // 18. Called a liar: Disbelief, even though « menteuse » reads as mean on its own.
        foreach (var (text, expected) in new[]
        {
            ("t'es une menteuse", "Disbelief"), ("tu mens", "Disbelief"), ("pourquoi tu mens ?", "Disbelief"), ("menteuse !", "Disbelief"),
            ("sale menteuse", "Disbelief"), ("t'as menti", "Disbelief"), ("arrête de mentir", "Disbelief"), ("mytho", "Disbelief"),
            ("t'es une mytho", "Disbelief"), ("c'est du pipeau", "Disbelief"), ("tu racontes des bobards", "Disbelief"),
            ("syncs, t'es une menteuse", "Disbelief"), ("<@999> tu mens", "Disbelief"), ("non, t'es une menteuse", "Disbelief"),
            ("tu mens pas", "None"), ("t'es pas une menteuse", "None"), ("il ment tout le temps", "None"), ("il est un menteur", "None"),
            ("Luca est un vrai mytho", "None"), ("t'es une menteuse et une idiote en plus", "None"),
            ("le mensonge c'est mal", "None"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }

        // The mood still reads « menteuse » as mean (ReactionService, the backbite roast); ChatterService
        // lets Disbelief through only when the intent says so. Pin both halves.
        if (Mood("t'es une menteuse") != "Mean") Fail("« t'es une menteuse » no longer reads mean: ChatterService.IsMean relies on it");
        if (Mood("t'es une menteuse et une idiote en plus") != "Mean") Fail("a long insult with « menteuse » no longer reads mean");
        // A hush is not an insult: a mean mood reads no intent, so « tais-toi » would be roasted, not obeyed.
        foreach (var hush in new[] { "tais-toi", "TAIS-TOI !", "tais-toi syncs", "oh tais-toi un peu", "taisez-vous", "chut !", "la ferme", "ferme-la", "ferme ta bouche" })
            if (Mood(hush) == "Mean") Fail($"« {hush} » reads mean: it must reach the ShutUp intent");
        if (Mood("tais-toi, t'es nulle") != "Mean") Fail("« tais-toi, t'es nulle » no longer reads mean: the insult beside the hush must still count");
        if ((bool)addresses.Invoke(null, new object[] { "syncs est une menteuse", 999UL })!) Fail("« syncs est une menteuse » reads as said to her (it is a backbite)");

        // 19. The weakest intents widened: AreYouThere, AboutHer, Crush, Help, Disbelief, and the game talk
        // each new cue must leave alone.
        foreach (var (text, expected) in new[]
        {
            // AreYouThere: her silence noticed.
            ("tu m'ignores ?", "AreYouThere"), ("tu m'ignores maintenant ?", "AreYouThere"), ("tu réponds plus ?", "AreYouThere"),
            ("tu me réponds pas ?", "AreYouThere"), ("pourquoi tu me réponds pas ?", "AreYouThere"), ("réponds-moi", "AreYouThere"),
            ("t'es morte ?", "AreYouThere"), ("t'es morte ou quoi", "AreYouThere"), ("t'es plus là ?", "AreYouThere"),
            ("tu réponds pas à ma question ?", "AnswerMe"), ("t'es morte de rire ?", "Laughter"),
            // AboutHer: her taste asked with « quel », hobbies and passions.
            ("t'aimes quel jeu ?", "AboutHer"), ("tu aimes quelle musique ?", "AboutHer"), ("tu kiffes quel genre de film ?", "AboutHer"),
            ("tu préfères quoi comme musique ?", "AboutHer"), ("t'as des hobbies ?", "AboutHer"), ("tu as des passions ?", "AboutHer"),
            ("c'est quoi tes passions ?", "AboutHer"),
            ("tu préfères quel jeu, Minecraft ou Fortnite ?", "Opinion"), ("tu préfères Minecraft ou Fortnite ?", "Opinion"),
            // Crush: asked short, closing the message.
            ("tu kiffes qui ?", "Crush"), ("t'as quelqu'un ?", "Crush"), ("t'aimes bien qui sur le serveur ?", "Crush"),
            ("tu aimes qui syncs ?", "Crush"), ("t'aimes qui toi ?", "Crush"), ("t'as quelqu'un en vue ?", "Crush"),
            ("qui te fait craquer ?", "Crush"),
            ("tu aimes qui comme perso ?", "Question"), ("tu kiffes qui en jungle ?", "Question"),
            ("t'as quelqu'un qui peut me carry ?", "Question"), ("tu aimes qui dans ce jeu ?", "Question"),
            // Help: how to use one of her features, named.
            ("comment on fait un sondage ?", "Help"), ("comment je fais pour créer une session ?", "Help"),
            ("comment lancer un giveaway ?", "Help"), ("comment on met un rappel ?", "Help"), ("comment faire un quiz ?", "Help"),
            ("t'as des commandes ?", "Help"), ("je sais pas comment t'utiliser", "Help"),
            ("comment on fait pour finir le raid ?", "Question"), ("comment je fais une partie classée ?", "Question"),
            ("comment on fait des équipes sur Fortnite ?", "Question"), ("comment je fais pour changer d'équipe ?", "Question"),
            // Disbelief: doubt, asked.
            ("t'es sûre ?", "Disbelief"), ("mais t'es sûre ?", "Disbelief"), ("tu es sûre de toi ?", "Disbelief"), ("t'es sûr ?", "Disbelief"),
            ("sérieux ?", "Disbelief"), ("sérieusement ?", "Disbelief"), ("sérieux quoi ?", "Disbelief"), ("srx ?", "Disbelief"),
            ("ça m'étonnerait", "Disbelief"), ("j'en doute", "Disbelief"), ("j'en doute fort", "Disbelief"),
            ("t'es sûre que tu viens ?", "Question"), ("t'es sûre ou pas pour ce soir ?", "Question"), ("il est sérieux ?", "Question"),
            ("sérieux, le boss a 3 phases ?", "Question"), ("sérieux il a encore raté", "None"), ("t'es sur quoi ?", "Question"),
            ("t'es sur le serveur ?", "Question"), ("ça m'étonnerait pas", "None"), ("ça m'étonnerait qu'il vienne", "None"),
            ("j'en doute pas", "None"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        var preferQuel = Opinion.Read("tu préfères quel jeu, Minecraft ou Fortnite ?");
        if (preferQuel?.Subject != "Minecraft" || preferQuel?.Other != "Fortnite") Fail($"« tu préfères quel jeu, X ou Y » sides: ({preferQuel?.Subject}, {preferQuel?.Other})");
        if (Opinion.Read("tu aimes qui comme perso ?") is not null) Fail("« tu aimes qui comme perso ? » rates « qui comme perso »");
        foreach (var (text, topic) in new[] { ("comment on fait un sondage ?", "Poll"), ("comment je fais pour créer une session ?", "Session"), ("comment on met un rappel ?", "Reminder") })
            if (readTopicAny.Invoke(null, new object[] { text })!.ToString() != topic) Fail($"help topic: \"{text}\" != {topic}");

        // 20. Down, ShouldI, Joke, FunFact, Blame, and missing her (Love), with the chat each must leave alone.
        foreach (var (text, expected) in new[]
        {
            // Down: about themselves, said.
            ("je suis triste", "Down"), ("je suis trop triste", "Down"), ("je me sens vraiment mal", "Down"), ("jsuis deg", "Down"),
            ("je suis tellement déprimée", "Down"), ("j'ai passé une sale journée", "Down"), ("journée de merde", "Down"),
            ("ça va pas fort", "Down"), ("ça va pas du tout", "Down"), ("ça va pas fort en ce moment", "Down"),
            ("je pleure", "None"), ("ça va pas du tout ce patch", "None"), ("j'en peux plus", "Down"), ("j'en ai marre de ma vie", "Down"),
            ("j'ai pas le moral", "Down"), ("coup de mou", "Down"), ("je suis à bout", "Down"), ("je suis au bout du rouleau", "Down"),
            ("je me sens seule", "Down"), ("j'ai envie de pleurer", "Down"), ("je craque", "Down"), ("j'ai le seum", "Down"),
            ("journée de merde, je vais dormir", "Down"), ("syncs, je suis triste", "Down"), ("je suis nul", "Down"),
            ("je suis pas triste", "Sharing"), ("j'en peux plus mdr", "Laughter"), ("je pleure de rire", "Laughter"), ("j'en peux plus 😂", "Laughter"),
            ("ça va pas la tête ?", "Question"), ("ça va pas ce soir, désolé", "None"), ("je craque pour ce skin", "Sharing"),
            ("je suis seul en vocal", "Sharing"), ("je suis mal placé sur la map", "Sharing"), ("t'as passé une sale journée ?", "Question"),
            ("il y a un cafard dans la cuisine", "None"), ("il a le seum", "None"), ("je suis pas mal", "Sharing"),
            ("c'est triste ce film", "None"),
            // ShouldI: yes or no, or a choice.
            ("je devrais aller dormir ?", "ShouldI"), ("est-ce que je devrais acheter ce jeu ?", "ShouldI"), ("je l'achète ?", "ShouldI"),
            ("tu crois que je devrais lui écrire ?", "ShouldI"), ("oui ou non ?", "ShouldI"), ("j'y vais ou pas ?", "ShouldI"),
            ("on le fait ou pas ?", "ShouldI"), ("je joue ou je dors ?", "ShouldI"), ("Valo ou LoL ?", "ShouldI"),
            ("pizza ou sushi ?", "ShouldI"), ("pile ou face ?", "ShouldI"), ("pile ou face", "ShouldI"), ("syncs, samedi ou dimanche ?", "ShouldI"),
            ("je devrais dormir", "Sharing"), ("je lui dis quoi ?", "Question"), ("je le fais comment ?", "Question"),
            ("tu viens ou pas ?", "Question"), ("tu dors ou quoi", "AreYouThere"), ("c'est vrai ou faux ?", "Question"),
            ("tu préfères Valo ou LoL ?", "Opinion"), ("on se fait un Valo ou un LoL ce soir avec les autres ?", "Question"),
            ("ça va ou quoi ?", "HowAreYou"), ("je devrais faire quoi ?", "Question"),
            // Joke and FunFact.
            ("raconte une blague", "Joke"), ("raconte-moi une blague", "Joke"), ("fais-moi rire", "Joke"), ("une blague ?", "Joke"),
            ("t'as une blague ?", "Joke"), ("tu peux me raconter une blague ?", "Joke"), ("tu connais des blagues ?", "Joke"),
            ("c'est une blague ?", "Question"), ("sans blague", "None"), ("pas de blague ce soir", "None"),
            ("un fun fact ?", "FunFact"), ("dis-moi un truc", "FunFact"), ("raconte-moi quelque chose", "FunFact"),
            ("apprends-moi un truc", "FunFact"), ("dis moi un truc intéressant", "FunFact"),
            ("dis-moi un truc, tu viens ce soir ?", "Question"), ("dis quelque chose à Luca de ma part", "None"),
            // Blame.
            ("c'est la faute de qui ?", "Blame"), ("c'est qui le coupable ?", "Blame"), ("à qui la faute ?", "Blame"),
            ("c'est la faute de qui", "Blame"), ("c'est la faute à qui cette défaite ?", "Blame"),
            ("c'est la faute de Luca", "None"), ("c'est de ta faute", "None"),
            // Missing her is Love.
            ("tu m'as manqué", "Love"), ("tu me manques", "Love"), ("t'étais où, tu m'as manqué", "Love"), ("tu me manques pas", "None"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }
        var readChoice = cues.GetMethod("ReadChoice", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, a, b) in new[] { ("je joue ou je dors ?", "je joue", "je dors"), ("Valo ou LoL ? 🙂", "Valo", "LoL"), ("pile ou face", "pile", "face"), ("syncs, pizza ou sushi ?", "pizza", "sushi") })
        {
            var got = readChoice.Invoke(null, new object[] { text });
            var expectedPair = ((string, string)?)(a, b);
            if (!Equals(got, expectedPair)) Fail($"ReadChoice(\"{text}\") = {got}, expected ({a}, {b})");
        }
        var speaks = cues.GetMethod("SpeaksToSomeone", BindingFlags.Public | BindingFlags.Static)!;
        if ((bool)speaks.Invoke(null, new object[] { "journée de merde" })!) Fail("« journée de merde » speaks to someone");
        if (!(bool)speaks.Invoke(null, new object[] { "j'en ai marre de toi" })!) Fail("« j'en ai marre de toi » speaks to nobody");
        // The pools ChatterService formats the /blame lines into keep {0} out of them.
        foreach (var f in new[] { "BlameLines", "BlameSelfLines", "BlameQuokkaLines", "BlamePapaDodgeLines", "BlamePapaDodgeQuokkaLines", "BlameNobodyLines" })
            foreach (var l in (string[])Field(f))
                try { string.Format(l.Replace("{0}", "{2}"), "Nom", "lundi", "<@1>", ""); } catch (FormatException) { Fail($"{f}: {l}"); }
        if (!((string[])Field("BlamePapaDodgeLines")).Any(l => l.StartsWith("~~"))) Fail("no struck-through Papa dodge line left for chat");
        if (!((string[])Field("BlamePapaDodgeQuokkaLines")).Any(l => l.StartsWith("~~"))) Fail("no struck-through Papa-Quokka dodge line left for chat");

        // 21. Yes / No under her questions, « tu m'aimes ? », « tu te souviens de moi ? », « t'es fâchée ? »,
        // with the chat each must leave alone.
        foreach (var (text, expected) in new[]
        {
            // DoYouLoveMe.
            ("tu m'aimes ?", "DoYouLoveMe"), ("tu m'aimes", "DoYouLoveMe"), ("tu m'aimes pas ?", "DoYouLoveMe"),
            ("tu m'aimes un peu ?", "DoYouLoveMe"), ("est-ce que tu m'aimes ?", "DoYouLoveMe"), ("tu me détestes ou quoi ?", "DoYouLoveMe"),
            ("tu m'en veux encore ?", "DoYouLoveMe"), ("tu me kiffes ?", "DoYouLoveMe"), ("tu m'apprécies ?", "DoYouLoveMe"),
            ("je suis ta préférée ?", "DoYouLoveMe"), ("tu me supportes plus ?", "DoYouLoveMe"),
            ("je t'aime, tu m'aimes ?", "Love"), ("désolé, tu m'en veux ?", "Apology"), ("tu m'en veux si je pars ?", "Question"),
            ("tu me supportes en bot lane ?", "Question"), ("il m'aime pas", "None"), ("tu m'aimes plus que Luca ?", "DoYouLoveMe"),
            // RememberMe.
            ("tu te souviens de moi ?", "RememberMe"), ("tu te souviens pas de moi ?", "RememberMe"), ("tu te rappelles de moi", "RememberMe"),
            ("comment je m'appelle ?", "RememberMe"), ("comment je m'appelle déjà ?", "RememberMe"), ("c'est quoi mon prénom ?", "RememberMe"),
            ("tu me reconnais ?", "RememberMe"), ("tu sais qui je suis ?", "RememberMe"), ("je suis qui ?", "RememberMe"),
            ("tu m'as oublié ?", "RememberMe"), ("qui suis-je ?", "RememberMe"), ("tu connais mon nom ?", "RememberMe"),
            ("tu me connais, je suis toujours en retard", "None"), ("je suis qui pour juger", "Sharing"),
            ("tu te souviens de moi en vocal hier ?", "Question"), ("tu sais qui je suis allé voir ?", "Question"),
            ("c'est quoi mon pseudo sur Steam ?", "Question"), ("comment tu t'appelles ?", "WhoAreYou"), ("c'est quoi ton nom ?", "WhoAreYou"),
            ("tu m'as oublié pour la session ?", "Question"),
            // AreYouUpset.
            ("t'es fâchée ?", "AreYouUpset"), ("t'es fâché ?", "AreYouUpset"), ("t'es fâchée contre moi ?", "AreYouUpset"),
            ("tu boudes ?", "AreYouUpset"), ("tu boudes encore ?", "AreYouUpset"), ("t'es pas fâchée ?", "AreYouUpset"),
            ("t'es jalouse de lui ?", "AreYouUpset"), ("t'es triste là ?", "AreYouUpset"), ("t'es en colère ?", "AreYouUpset"),
            ("t'es vexée ?", "AreYouUpset"), ("ça va ? t'es fâchée ?", "HowAreYou"),
            ("t'es fâchée", "None"), ("t'es triste ce soir ou tu viens ?", "Question"),
        })
        {
            var got = readIntent.Invoke(null, new object[] { text })!.ToString();
            if (got != expected) Fail($"ReadIntent(\"{text}\") = {got}, expected {expected}");
        }

        var readAnswer = cues.GetMethod("ReadAnswer", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, expected) in new[]
        {
            ("oui", "Yes"), ("ouiii", "Yes"), ("ouais", "Yes"), ("bah oui", "Yes"), ("oui oui", "Yes"), ("carrément", "Yes"),
            ("grave", "Yes"), ("bien sûr", "Yes"), ("évidemment !", "Yes"), ("oui syncs", "Yes"), ("<@999> oui", "Yes"), ("oui 😂", "Yes"),
            ("tout à fait", "Yes"), ("si", "Yes"), ("oui merci", "Yes"), ("yes", "Yes"),
            ("non", "No"), ("nan", "No"), ("nooon", "No"), ("bah non", "No"), ("mais non", "No"), ("non non non", "No"),
            ("pas du tout", "No"), ("jamais", "No"), ("nope", "No"), ("non merci", "No"), ("absolument pas", "No"),
            ("oui ?", ""), ("non ?", ""), ("oui et non", ""), ("oui mais demain", ""), ("ok", ""), ("peut-être", ""),
            ("exactement", ""), ("merci", ""), ("oui je viens ce soir à la session", ""), ("", ""),
        })
        {
            var got = readAnswer.Invoke(null, new object[] { text })?.ToString() ?? "";
            if (got != expected) Fail($"ReadAnswer(\"{text}\") = {(got == "" ? "null" : got)}, expected {(expected == "" ? "null" : expected)}");
        }

        // A short answer to one of her open questions, quoted back as written (OpenAnswerLines).
        var readShortAnswer = cues.GetMethod("ReadShortAnswer", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, expected) in new[]
        {
            ("Javascript", "Javascript"), ("Python", "Python"), ("Grattoir", "Grattoir"), ("Ça s'appelle comme ça", "Ça s'appelle comme ça"),
            ("8 jours.", "8 jours"), ("  le   bleu !", "le bleu"), ("<@1> Minecraft", "Minecraft"), ("syncs, la pizza", "la pizza"),
            ("Valo <:emote:123>", "Valo"),
            ("Java ?", ""), ("t'es nulle", ""), ("toi", ""), ("<:emote:123>", ""), ("", ""),
            ("La réponse était: 8 jours. (Au bout de 7 jours, il a atteint 7 mètres)", ""),
        })
        {
            var got = readShortAnswer.Invoke(null, new object[] { text })?.ToString() ?? "";
            if (got != expected) Fail($"ReadShortAnswer(\"{text}\") = {(got == "" ? "null" : got)}, expected {(expected == "" ? "null" : expected)}");
        }
        foreach (var l in (string[])Field("OpenAnswerLines"))
            if (l.Contains("{2}") && !l.Contains("« {2} »")) Fail($"OpenAnswerLines quotes the answer outside « »: {l}");

        // The records pools: every plain RememberMe line and every opener says the name (the answer to
        // « comment je m'appelle ? »); the openers carry no tag, the fact line after them does; the grudge
        // quotes its count, the treasure never shows one.
        foreach (var l in (string[])((IDictionary)Field("IntentLines"))[Enum.Parse(intentType, "RememberMe")]!)
            if (!l.Contains("{0}")) Fail($"IntentLines[RememberMe] doesn't say the name: {l}");
        foreach (var l in (string[])Field("RememberMeOpeners"))
        {
            if (!l.Contains("{0}")) Fail($"RememberMeOpeners doesn't say the name: {l}");
            try
            {
                var s = string.Format(l, "Nom");
                if (Sticker.Strip(s) != s) Fail($"RememberMeOpeners is tagged: {l}");
            }
            catch (FormatException) { Fail($"RememberMeOpeners: format: {l}"); }
        }
        foreach (var l in (string[])Field("DoYouLoveMeGrudgeLines"))
            if (!l.Contains("{2}")) Fail($"DoYouLoveMeGrudgeLines doesn't quote the count: {l}");
        foreach (var l in (string[])Field("DoYouLoveMeTreasureLines"))
            if (l.Contains("{2}") || l.Replace("{0}", "").Replace("{1}", "").Any(char.IsDigit)) Fail($"DoYouLoveMeTreasureLines shows a number: {l}");
    }
}
