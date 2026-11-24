using System.Collections;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;
using static Check;

// Replies under her cards (MessageCues.ReadCardReply).
public static class CardReplyChecks
{
    public static void Run(string[] args)
    {
        // Replies under her cards (MessageCues.ReadCardReply): what each is doing, and what must stay ordinary
        // talk. The bool is whether the reply tags someone else, which is what makes « vote » an order.
        var readCardReply = cues.GetMethod("ReadCardReply", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, tags, expected) in new (string, bool, string)[]
        {
            // Bringing it back.
            ("remonte le sondage stp", false, "Repost"), ("tu peux le remonter ?", false, "Repost"), ("up", false, "Repost"),
            ("up !", false, "Repost"), ("petit up", false, "Repost"), ("syncs, remets-le en bas", false, "Repost"),
            ("tu peux le mettre à la fin ?", false, "Repost"), ("republie-le", false, "Repost"), ("ramène la carte", false, "Repost"),
            ("tu peux pas le remonter ?", false, "Repost"), ("bump", false, "Repost"),
            ("le remonte pas", false, "None"), ("je suis en bas", false, "None"),
            ("remets-le", false, "Repost"), ("renvoie le sondage", false, "Repost"), ("tu peux le renvoyer ?", false, "Repost"),
            ("on le voit plus", false, "Repost"), ("le sondage est tout en haut", false, "Repost"), ("reposte stp", false, "Repost"),
            ("ramène des chips", false, "None"), ("tu ramènes quoi ?", false, "None"), ("renvoie-moi le lien", false, "None"),
            ("remets ton vote", false, "None"), ("remets la musique", false, "None"), ("on remonte la pente", false, "None"),
            ("je l'ai remonté", false, "None"), ("elle a déjà remonté le sondage", false, "None"),
            ("je suis noyé de boulot", false, "None"), ("Luca il est perdu", false, "None"), ("tu remontes à quelle heure ?", false, "None"),
            // Rallying the others.
            ("votez !", false, "Rally"), ("allez votez svp", false, "Rally"), ("<@123> vote", true, "Rally"),
            ("<@123> <@456> votez", true, "Rally"), ("vote stp <@123>", true, "Rally"), ("faut voter les gars", false, "Rally"),
            ("oubliez pas de voter", false, "Rally"), ("<@123> tu viens ?", true, "Rally"), ("venez", false, "Rally"),
            ("inscrivez-vous", false, "Rally"), ("<@123> clique", true, "Rally"),
            ("<@123> a voté", true, "None"), ("le vote est nul", false, "None"), ("vote", false, "Done"),
            ("<@123> j'ai voté", true, "Done"), ("votez pas", false, "None"),
            ("<@123>", true, "Rally"), ("<@123> <@456> 👀", true, "Rally"), ("<@123> t'as voté ?", true, "Rally"),
            ("<@123> et toi ?", true, "Rally"), ("<@123> chaud ?", true, "Rally"), ("et vous ?", false, "Rally"),
            ("personne a voté", false, "Rally"), ("bougez-vous", false, "Rally"), ("allez on vote", false, "Rally"),
            ("tout le monde clique", false, "Rally"), ("<@123> t'es chaud ce soir ?", true, "Rally"),
            ("t'as voté ?", false, "None"), ("<@123> je suis d'accord avec toi sur ce point", true, "None"),
            // Saying it's done.
            ("j'ai voté", false, "Done"), ("c'est fait", false, "Done"), ("voté !", false, "Done"), ("a voté", false, "Done"),
            ("j'ai voté pour samedi", false, "Done"), ("je me suis inscrite", false, "Done"), ("fait", false, "Done"),
            ("j'ai pas voté", false, "None"), ("c'est voté", false, "Done"), ("c'est bon", false, "Done"),
            ("voilà", false, "Done"), ("✅", false, "Done"), ("j'ai coché samedi", false, "Done"), ("on a tous voté", false, "Done"),
            ("jsuis inscrit", false, "Done"), ("j'ai mis du temps à venir", false, "None"), ("<@123> c'est bon", true, "None"),
            // Asking how it stands.
            ("c'est quand ?", false, "Status"), ("qui vient ?", false, "Status"), ("on en est où ?", false, "Status"),
            ("on est combien ?", false, "Status"), ("qui a gagné ?", false, "Status"), ("ça finit quand ?", false, "Status"),
            ("pourquoi samedi ?", false, "None"),
            ("c'est quand", false, "Status"), ("qui vient ce soir", false, "Status"), ("alors ?", false, "Status"),
            ("y a combien de votes ?", false, "Status"), ("ça commence à quelle heure ?", false, "Status"),
            ("qui est en tête ?", false, "Status"), ("c'est pour quand ?", false, "Status"),
            ("il est quelle heure ?", false, "None"), ("t'as fait combien de kills ?", false, "None"),
            ("le tournoi commence quand ?", false, "None"), ("résultat je suis mort", false, "None"),
            ("alors t'en penses quoi ?", false, "None"),
            // An answer typed instead of clicked.
            ("samedi pour moi", false, "TextAnswer"), ("je peux pas", false, "TextAnswer"), ("dispo", false, "TextAnswer"),
            ("je viens", false, "TextAnswer"), ("chaud", false, "TextAnswer"), ("moi", false, "TextAnswer"),
            ("plutôt dimanche", false, "TextAnswer"), ("go", false, "TextAnswer"), ("je vote Minecraft", false, "TextAnswer"),
            ("je peux voter ?", false, "None"),
            ("ok pour samedi", false, "TextAnswer"), ("samedi ou dimanche", false, "TextAnswer"), ("samedi ça me va", false, "TextAnswer"),
            ("ça va ce soir", false, "TextAnswer"), ("j'y serai", false, "TextAnswer"), ("compte sur moi", false, "TextAnswer"),
            ("je suis trop chaud", false, "TextAnswer"), ("pareil", false, "TextAnswer"), ("+1", false, "TextAnswer"),
            ("je peux pas venir samedi", false, "TextAnswer"), ("je peux voter pour deux", false, "None"),
            ("il fait chaud ici", false, "None"), ("c'est chaud", false, "None"), ("ça va être chaud ce soir", false, "None"),
            ("je dors chez Luca ce soir", false, "None"), ("à demain", false, "None"), ("bon weekend", false, "None"),
            ("c'est calme ce soir", false, "None"), ("le bot a planté hier soir", false, "None"),
            ("je te dis ça demain", false, "None"), ("ma connexion est nulle ce soir", false, "None"),
            // Ordinary replies: the card's context line, or the usual path.
            ("ok", false, "None"), ("merci", false, "None"), ("t'es nulle", false, "None"), ("", false, "None"),
            ("« votez » qu'il a dit", false, "None"),
        })
        {
            var got = readCardReply.Invoke(null, new object[] { text, tags })!.ToString();
            if (got != expected) Fail($"ReadCardReply(\"{text}\", tags: {tags}) = {got}, expected {expected}");
        }

        // The same ask with no card replied to (MessageCues.ReadNamedRepost): only with the card named.
        // « Poll all »: every open poll in the channel, not only the newest.
        var readNamedRepost = cues.GetMethod("ReadNamedRepost", BindingFlags.Public | BindingFlags.Static)!;
        foreach (var (text, expected) in new (string, string)[]
        {
            ("syncs remonte le sondage", "Poll"), ("syncs, tu peux remonter le sondage ?", "Poll"),
            ("<@999> remonte le sondage stp", "Poll"), ("remonte le vote syncs", "Poll"), ("syncs remets la session en bas", "Session"),
            ("syncs republie le tirage", "Giveaway"), ("syncs renvoie la carte", "Any"), ("syncs le sondage est tout en haut", "Poll"),
            ("syncs remonte-le", "None"), ("syncs up", "None"), ("syncs a remonté le sondage", "None"),
            ("on remonte le sondage", "None"), ("syncs remets la musique", "None"), ("syncs le sondage est nul", "None"),
            ("syncs t'as vu le sondage ?", "None"),
            // Every one of them.
            ("syncs remonte tous les sondages", "Poll all"), ("syncs remonte les sondages", "Poll all"),
            ("syncs tu peux remonter les deux sondages ?", "Poll all"), ("syncs remets toutes les cartes en bas", "Any all"),
            ("syncs remonte les sessions", "Session all"), ("syncs remonte tout", "Any all"), ("syncs remonte-les tous", "Any all"),
            ("syncs tu peux tout remonter ?", "Any all"), ("syncs remonte tout ça stp", "Any all"),
            ("syncs remonte le sondage pour tous", "Poll"), ("syncs remonte tout le monde", "None"),
            ("syncs remets-le tout en bas", "None"), ("syncs remonte le tout", "Any all"),
            // Refined: more verbs and nouns, and no past or person.
            ("syncs remets le vote", "Poll"), ("syncs rebalance le sondage", "Poll"), ("syncs re-poste le sondage", "Poll"),
            ("syncs redescends le sondage", "Poll"), ("syncs renvoi le sondage", "Poll"), ("syncs remonte le tonight", "Session"),
            ("syncs remonte le poll", "Poll"), ("syncs reup le sondage", "Poll"), ("syncs j'ai perdu le sondage", "Poll"),
            ("syncs le sondage s'est perdu tout en haut", "Poll"),
            ("syncs le sondage a été remonté", "None"), ("syncs le sondage est remonté", "None"),
            ("syncs remonte pas le sondage", "None"), ("syncs je suis perdu avec le sondage", "None"),
            ("syncs t'es perdue avec les sondages", "None"), ("syncs remets ton vote en bas", "None"), ("syncs on remonte tous les sondages", "None"),
        })
        {
            var result = readNamedRepost.Invoke(null, new object[] { text })!;
            var card = result.GetType().GetProperty("Card")!.GetValue(result)!.ToString();
            var all = (bool)result.GetType().GetProperty("All")!.GetValue(result)!;
            var got = card + (all ? " all" : "");
            if (got != expected) Fail($"ReadNamedRepost(\"{text}\") = {got}, expected {expected}");
        }

        // A card is recognised by its buttons, whatever else rides on the message.
        void CardIs(string[] ids, CardRef? expected)
        {
            var got = CardRefs.Read(ids);
            if (got != expected) Fail($"CardRefs.Read({string.Join(", ", ids)}) = {got?.ToString() ?? "null"}, expected {expected?.ToString() ?? "null"}");
        }
        CardIs(["poll:vote:12:40", "poll:close:12"], new CardRef(CardKind.Poll, 12));
        CardIs(["poll:tosession:7"], new CardRef(CardKind.Poll, 7));
        CardIs(["event:join:3", "event:sub:3"], new CardRef(CardKind.Session, 3));
        CardIs(["tonight:in:5"], new CardRef(CardKind.Tonight, 5));
        CardIs(["giveaway:enter:9"], new CardRef(CardKind.Giveaway, 9));
        CardIs(["poll:republish"], null);
        CardIs(["arcade:play:4"], null);
        CardIs([], null);

        // The everyday sweeps again, as if replied under a card: not failures (« je peux pas venir samedi »
        // under a poll is an answer), a count to watch. Run with --card-review to read them.
        var cardReads = ChatCueChecks.neutral.Concat(ChatCueChecks.slang)
            .Select(text => (Text: text, Got: readCardReply.Invoke(null, new object[] { text, false })!.ToString()))
            .Where(r => r.Got != "None")
            .ToList();
        Console.WriteLine($"card sweep: {cardReads.Count} of {ChatCueChecks.neutral.Length + ChatCueChecks.slang.Length} ordinary messages read as a card reply");
        if (args.Contains("--card-review"))
            foreach (var (text, got) in cardReads) Console.WriteLine($"  card: \"{text}\" -> {got}");
    }
}
