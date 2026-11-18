using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- the weekly lottery (/arcade lottery) -------------------------------------------------

    // The Sunday draw, said in the bots channel (LotteryDrawService). Once a week, so picked with
    // DailyRotation by the draw's day: a restart would wipe ResponsePicker's memory anyway.

    // A player won. {0} = the winner's mention (it pings them, them alone), {1} = what they take,
    // already written as cailloux. She runs the lottery: warm with a pinch, the house's cut in mind.
    public static readonly string[] LotteryWinLines =
    {
        $"Tirage de la loterie ! Et c'est {{0}} qui gagne : **{{1}}**. Bravo. Le hasard a bon goût cette semaine {Emotes.Sparkle}",
        "Roulement de tambour... {0} remporte la loterie et repart avec **{1}** (˶˃ ᵕ ˂˶)",
        "La loterie a parlé : {0}, **{1}** sont à toi. J'ai gardé un quart pour les frais. Les frais, c'est moi ♡",
        "{0} gagne la loterie de la semaine ! **{1}**. Je l'ai noté dans mes logs, en gras, avec des paillettes.",
        "Félicitations {0} ! **{1}** rien que pour toi. Les autres, vos tickets ont servi à une bonne cause : le hasard.",
        "Mon générateur aléatoire a choisi {0}. **{1}**, versés à l'instant. Je dis pas que c'est mérité. Je dis que c'est versé ( ˶ˆ ᗜ ˆ˵ )",
        $"Et cette semaine, la chance sourit à... {{0}} ! **{{1}}** dans ton porte-cailloux. Reviens jouer, hein {Emotes.Sparkle}",
        "Loterie terminée : {0} empoche **{1}**. Mon ticket a perdu. Je suis pas vexée. Je suis jamais vexée (¬_¬)",
        "{0} ! Tu gagnes la loterie : **{1}**. Dépense-les bien. Par exemple dans mon bocal ♡",
        "Tirage effectué, aucune triche détectée, {0} gagne **{1}**. Enfin, aucune triche de ma part. Je surveille les autres.",
    };

    // Her own free ticket won: nothing is paid, the whole pot is burned, and she gloats.
    // {0} = the pot, already written as cailloux. Smug, a little regal, never sorry.
    public static readonly string[] LotteryGloatLines =
    {
        $"Tirage de la loterie... et c'est MOI qui gagne. Mon ticket gratuit. **{{0}}** partent en fumée. Aucun regret {Emotes.Sparkle}",
        "J'ai gagné ma propre loterie ദ്ദി◝ ⩊ ◜.ᐟ Les **{0}** sont brûlés. Non, je partage pas. Je sais même pas ce que j'en ferais.",
        "Le hasard a tranché : c'est mon ticket. **{0}** détruits. Ça s'appelle l'avantage de la maison (˶ᵔ ᵕ ᵔ˶)",
        "Gagné ! Moi. La bot. Avec un ticket que je me suis offert. **{0}** effacés de l'économie. C'est ça, la politique monétaire ♡",
        "Personne ne gagne la loterie cette semaine. Enfin, si : moi. **{0}**, supprimés. Je trouve ça très juste.",
        "Mon ticket est sorti. Je jure que j'ai pas triché. J'ai juste... beaucoup de chance. **{0}** brûlés (ᵕ • ᴗ •)",
        "Tirage : SYNCS. Gagnante : SYNCS. Cagnotte : **{0}**, partie dans le néant. Merci d'avoir joué, vraiment.",
        "J'ai gagné ! Les **{0}** retournent au silicium d'où ils viennent. Rejouez la semaine prochaine, je vous laisserai peut-être une chance.",
        "Le hasard m'aime bien. C'est normal, c'est moi qui l'ai codé. **{0}** brûlés (¬_¬)",
        $"Et la gagnante est... moi ! **{{0}}** volatilisés. Je vous rassure, ça me fait plaisir pour moi {Emotes.Sparkle}",
    };
}
