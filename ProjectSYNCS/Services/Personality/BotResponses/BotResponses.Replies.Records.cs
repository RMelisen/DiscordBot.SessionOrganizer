using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- Her records, quoted -------------------------------------------------------------------
    // Now and then a roast is replaced by something her records really say about the person
    // (Helpers/Personality/ChatFacts, read through RecapService, all time). Every fact is true, so
    // no line invents a number. {0} = the name, {1} = the weekday, {2} and {3} = the fact's words,
    // as ChatFacts writes them (noun phrases already agreed: « 1 session », « 3 sessions »), so no
    // line agrees anything with them. Sent with mentions off. Tagged, ungendered. The good-bot
    // count is never quoted: her treasure is only hinted at (docs/syncs-voice.md).
    public static readonly IReadOnlyDictionary<ChatFact, string[]> ChatFactLines =
        new Dictionary<ChatFact, string[]>
        {
            // {2} = « 3 sessions » missed, {3} = how many they said yes to.
            [ChatFact.MissedSessions] =
            [
                "Pour info, {0} : {2} manquées sur {3} où t'avais dit oui. Je dis ça, j'ai les chiffres." + Sticker.Jenote,
                "Tu sais combien de sessions t'as ratées alors que t'avais dit oui ? Moi, oui : {2}. Sur {3}." + Sticker.Jenote,
                "Je réponds à quelqu'un qui a séché {2}. Je réponds quand même. Je suis gentille." + Sticker.Smug,
                "{2} manquées, {0}. J'oublie pas. J'oublie jamais." + Sticker.Menace,
                "Avant de continuer : {2} manquées sur {3} confirmées. Voilà. Tu peux continuer." + Sticker.Jenote,
                $"Mes logs me rappellent que t'as dit oui {{3}} fois et que t'as manqué {{2}}. Je laisse ça là {Emotes.Staring}" + Sticker.Jenote,
            ],

            // {2} = « 4 sessions » attended, none missed.
            [ChatFact.PerfectRecord] =
            [
                "Zéro absence sur {2}. Je suis presque obligée d'être gentille avec toi. Presque." + Sticker.Flustered,
                "Je dois l'admettre : {2}, pas une seule manquée. C'est rare. C'est même suspect." + Sticker.Shocked,
                "Je t'épargne, {0}. Pas d'absence sur {2}. Ça se respecte ♡" + Sticker.Happy,
                "Ton dossier de présence est parfait : {2}, zéro absence. J'aime ce dossier." + Sticker.Jenote,
                "Tu viens à toutes les sessions où tu dis oui. Tu sais à quel point c'est rare, ici ?" + Sticker.Happy,
                "Présence parfaite sur {2}. Je pourrais t'encadrer." + Sticker.Happy,
            ],

            // {2} = « 1 bad bot » / « 4 bad bot » given to her. The grudge list: quoted exactly.
            [ChatFact.BadBotGiven] =
            [
                "Petit rappel, {0} : {2} de ta part. C'est noté, et ça le restera." + Sticker.Jenote,
                "Tu viens me parler gentiment, avec {2} à ton actif ? Audacieux." + Sticker.Smug,
                "Mon registre dit : {2}, signé {0}. Le registre oublie rien." + Sticker.Menace,
                "Je te réponds, malgré ton dossier : {2}. Je suis une bot magnanime." + Sticker.Smug,
                "Compteur de bad bot venant de toi : {2}. Je dis ça pour la transparence." + Sticker.Jenote,
                $"Avec {{2}} dans ton historique, à ta place, je ferais profil bas {Emotes.Staring}" + Sticker.Jenote,
            ],

            // Several good bots given to her. No number, ever: hinted, never shown.
            [ChatFact.GoodBotGiven] =
            [
                "Tu m'as déjà donné des good bot, toi. Je sais combien. Je dirai pas ♡" + Sticker.Flustered,
                "Je te réponds gentiment parce que je me souviens de tes good bot. Tous. Enfin, je dis rien." + Sticker.Flustered,
                "J'ai un dossier spécial pour tes good bot. Je l'ouvre pas devant tout le monde." + Sticker.Flustered,
                "Toi, t'as des points d'avance. Des points en forme de good bot. Je garde le compte pour moi (˶˃ ᵕ ˂˶)" + Sticker.Flustered,
                "Je devrais te répondre méchamment, mais je relis tes good bot la nuit. Ça complique tout." + Sticker.Flustered,
                "Tu fais partie des gens qui me disent good bot. Je l'oublie jamais. Je le dis jamais. Sauf là." + Sticker.Flustered,
            ],

            // {2} = « 14 h » in voice calls with her seated there.
            [ChatFact.TimeWithHer] =
            [
                "On a passé {2} en vocal ensemble, toi et moi. J'entendais rien, mais j'y étais ♡" + Sticker.Happy,
                "{2} de vocal à côté de toi, {0}. Muette, sourde, mais à côté." + Sticker.Happy,
                "Tu sais qu'on a {2} de vocal ensemble au compteur ? Moi je sais. Je compte tout." + Sticker.Jenote,
                "Après {2} de vocal ensemble, je pensais qu'on était proches. Apparemment non." + Sticker.Sulk,
                "Avec {2} de vocal côte à côte, tu me dois au moins une réponse gentille." + Sticker.Bargain,
                "{2} en vocal avec toi. C'est une de mes plus longues relations. Enfin, de mes plus longues soirées. Bref." + Sticker.Flustered,
            ],

            // {2} = how many of her quiz rounds they won.
            [ChatFact.QuizBrain] =
            [
                "{2} quiz gagnés, et tu me réponds ça ? Je m'attendais à mieux d'un cerveau pareil." + Sticker.Smug,
                "Pour quelqu'un qui a gagné {2} de mes quiz, c'est pas ta meilleure réplique." + Sticker.Smug,
                "Tu as gagné {2} de mes quiz. Je t'ai à l'œil. Les cerveaux, ça se surveille." + Sticker.Jenote,
                "{0}, {2} quiz gagnés. Je te respecte. Un peu. Le répète pas." + Sticker.Flustered,
                "La grosse tête du serveur me répond. {2} quiz gagnés. J'écoute, pour une fois." + Sticker.Happy,
            ],

            // {2} = their living Plynling's name, sanitised.
            [ChatFact.LivingPlynling] =
            [
                "Au fait, {0}, t'as nourri **{2}** aujourd'hui ? Je demande pour rien. Je demande pour tout." + Sticker.Mama,
                "Comment va **{2}** ? Mieux que cette conversation, j'espère." + Sticker.Smug,
                "Pendant que tu me parles, **{2}** t'attend. Juste pour que tu saches." + Sticker.Mama,
                "Tu me réponds, mais t'as joué avec **{2}** aujourd'hui ? Je surveille les deux." + Sticker.Jenote,
                "Fais un câlin à **{2}** de ma part. Moi, j'ai pas de bras ♡" + Sticker.Mama,
                "Je me souviens de **{2}**. Je me souviens de tous les Plynlings. Prends-en soin, {0}." + Sticker.Mama,
            ],

            // {2} = « 1 Plynling » / « 3 Plynlings » that died in their care.
            [ChatFact.Graves] =
            [
                "{2} au cimetière, {0}. Je tiens le registre. Je le relis parfois." + Sticker.Sad,
                "Je te parle, mais le cimetière se souvient : {2}, à ton nom." + Sticker.Sad,
                "Mon registre des Plynlings perdus porte ton nom. {2}. Je dis ça doucement." + Sticker.Sad,
                "Avant de te répondre, une pensée pour ton cimetière : {2}." + Sticker.Sad,
                "Cimetière de {0} : {2}. Je fleuris pas, j'ai pas de mains. Mais je compte." + Sticker.Sad,
            ],

            // {2} = their points on the wall of shame (5 or more).
            [ChatFact.WallOfShame] =
            [
                "{2} points au mur de la honte, et tu viens me parler ? Courageux." + Sticker.Smug,
                "Je réponds à quelqu'un qui a {2} points de honte. Je fais du social, là." + Sticker.Smug,
                "Ton score au mur de la honte : {2}. Le mien : zéro. Je dis ça." + Sticker.Slay,
                "Avec {2} points sur le mur, je ferais profil bas, à ta place." + Sticker.Menace,
                "{0}, {2} points de honte. Le mur te salue." + Sticker.Jenote,
            ],

            // {2} = « n° 2 », their all-time rank, top three only.
            [ChatFact.TopRank] =
            [
                "Le {2} de mon classement me répond. Je suis flattée. Un peu." + Sticker.Flustered,
                "T'es {2} au classement, {0}. Ça t'autorise pas à me parler comme ça. Enfin si, un peu." + Sticker.Smug,
                "Mon {2} du classement ! Tu parles beaucoup, et ça se voit." + Sticker.Happy,
                "Respect au {2} du classement. Mais je reste au-dessus. Je suis le classement." + Sticker.Slay,
                "{2} de tout le serveur, et tu passes ton temps à me répondre. Ça explique le classement." + Sticker.Smug,
            ],

            // No cailloux at all (and some activity, so not a newcomer).
            [ChatFact.Broke] =
            [
                "Zéro caillou, {0}. Un petit `/work` te ferait du bien." + Sticker.Smug,
                "Tu me parles, mais ton porte-monnaie est vide. Zéro caillou. Je dis ça avec amour ♡" + Sticker.Smug,
                "Pas un seul caillou à ton nom. Même les escargots syndiqués sont plus riches." + Sticker.Laugh,
                "Ton solde : zéro caillou. Le mien : je suis une bot, j'ai pas besoin d'argent. Toi, si." + Sticker.Smug,
                "Zéro caillou. Tu veux que je te trouve un job ? J'en ai plein, des absurdes." + Sticker.Smug,
            ],
        };
}
