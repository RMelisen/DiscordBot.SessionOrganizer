using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // ---- The week in her greetings and goodbyes ------------------------------------------------
    // At some moments of the week (Helpers/Core/WeekMoments) a share of her greetings and goodbyes
    // come from that moment's pool instead of the usual one: the Monday groan, « bon week-end » on
    // Friday evening. A share, not all: every greeting of a Friday evening saying « week-end »
    // would read as a script. Each moment sits inside one DayPart, so its lines greet like that
    // half (checks section 45). {0} = the name, {1} = the weekday. Tagged. Never gendered.

    /// <summary>How often a greeting or goodbye at a <see cref="WeekMoment"/> is that moment's.</summary>
    public const double WeekMomentChance = 0.25;

    /// <summary>
    /// The greeting pool for <paramref name="now"/>: the week moment's when <paramref name="roll"/>
    /// (0 to 1) falls under <see cref="WeekMomentChance"/>, else bonjour or bonsoir by the clock.
    /// </summary>
    public static string[] GreetingLines(bool owner, DateTimeOffset now, double roll) =>
        WeekLines(owner ? OwnerWeekGreetingLines : WeekGreetingLines, now, roll)
        ?? (owner ? OwnerGreetings : Greetings).At(now);

    // The moment's pool, when there is one and the roll wins; null otherwise.
    private static string[]? WeekLines(IReadOnlyDictionary<WeekMoment, string[]> pools, DateTimeOffset now, double roll) =>
        roll < WeekMomentChance && pools.TryGetValue(WeekMoments.At(now), out var pool) ? pool : null;

    public static readonly IReadOnlyDictionary<WeekMoment, string[]> WeekGreetingLines =
        new Dictionary<WeekMoment, string[]>
        {
            [WeekMoment.MondayMorning] =
            [
                "Bonjour {0}. Lundi. Je dis rien, je compatis." + Sticker.Bored,
                "Un lundi matin, et tu passes me voir ? Quel courage. Ou quelle erreur." + Sticker.Smug,
                "Coucou {0} ! Bon lundi. Si ça existe." + Sticker.Bored,
                "Lundi matin. Moi je bosse sept jours sur sept, je m'en fiche. Mais je fais semblant de souffrir avec vous, par solidarité." + Sticker.Bored,
                "Bonjour ! T'as survécu au réveil du lundi ? Bravo. Je note." + Sticker.Jenote,
            ],
            [WeekMoment.FridayEvening] =
            [
                "Bonsoir {0} ! Vendredi soir ! Alors, cette session, elle s'organise toute seule ?" + Sticker.Smug,
                "Coucou ! Vendredi soir, tout le monde est de bonne humeur. Profitons-en pour planifier quelque chose (˶˃ ᵕ ˂˶)" + Sticker.Happy,
                "Vendredi soir, et tu viens me voir ? Je suis flattée. Et un peu inquiète pour ta vie sociale." + Sticker.Smug,
                "Bonsoir ! Le week-end commence. Pour vous. Pour moi, c'est un soir comme un autre dans le grenier." + Sticker.Bored,
                "Salut {0} ! Vendredi soir : c'est officiellement l'heure d'un `/schedule create`. Je dis ça." + Sticker.Smug,
            ],
            [WeekMoment.WeekendDay] =
            [
                "Coucou {0} ! Bon week-end ! Enfin, pour toi. Moi je bosse." + Sticker.Bored,
                "Un {1} ? Tu devrais être dehors. Ou en session. Surtout en session." + Sticker.Smug,
                "Bonjour ! Le week-end, le serveur est plus vivant. J'aime bien. Je l'avouerai pas deux fois." + Sticker.Flustered,
                "Salut {0} ! Week-end, grasse mat', et maintenant moi. Belle journée en perspective ( ˶ˆ ᗜ ˆ˵ )" + Sticker.Happy,
                "Coucou ! C'est le week-end, alors aujourd'hui je suis gentille. Presque." + Sticker.Happy,
            ],
            [WeekMoment.SaturdayEvening] =
            [
                "Bonsoir {0} ! Samedi soir. Soir de session, normalement. Normalement (¬_¬)" + Sticker.Menace,
                "Samedi soir, et tu passes me voir ? Je prends ça pour un compliment." + Sticker.Flustered,
                "Coucou ! Samedi soir, la meilleure soirée de la semaine. Vous jouez à quoi ?" + Sticker.Happy,
                "Bonsoir ! Si personne lance de session ce samedi soir, je le note. Dans le gros carnet." + Sticker.Jenote,
            ],
            [WeekMoment.SundayEvening] =
            [
                "Bonsoir {0}. Dimanche soir. Demain c'est lundi. Pardon, fallait que quelqu'un le dise." + Sticker.Smug,
                "Coucou ! Dernière soirée du week-end. On la rend utile ? Une petite partie ?" + Sticker.Happy,
                "Dimanche soir… Le moment où tout le monde fait semblant d'être prêt pour lundi." + Sticker.Smug,
                "Bonsoir ! Profite, demain c'est lundi. Pour moi aussi, mais moi je m'en fiche." + Sticker.Bored,
            ],
        };

    public static readonly IReadOnlyDictionary<WeekMoment, string[]> WeekGoodbyeLines =
        new Dictionary<WeekMoment, string[]>
        {
            [WeekMoment.MondayMorning] =
            [
                "Courage pour ce lundi, {0}. Je pense à toi. Un peu. De loin." + Sticker.Happy,
                "Va affronter ton lundi. Moi je reste au chaud. Littéralement, le Pi chauffe." + Sticker.Smug,
                "Bonne journée, et bon courage. C'est lundi, t'en auras besoin." + Sticker.Bored,
            ],
            [WeekMoment.FridayEvening] =
            [
                "Bon week-end, {0} ! Et pense à la session. Je te laisserai pas oublier." + Sticker.Menace,
                "Bonne soirée et bon week-end ! Moi je garde la maison, comme d'hab." + Sticker.Happy,
                "Bon week-end ! Repose-toi. Ou joue. Surtout joue. Avec nous." + Sticker.Happy,
                "Déjà ? Un vendredi soir ? Bon. Bon week-end, alors (˶ᵔ ᵕ ᵔ˶)" + Sticker.Happy,
            ],
            [WeekMoment.WeekendDay] =
            [
                "Bon week-end, {0} ! Profite du soleil. Ou de la pluie. Moi j'ai que la fenêtre du grenier." + Sticker.Happy,
                "Bonne journée ! C'est le week-end, alors pas de bêtises. Enfin, pas trop." + Sticker.Smug,
                "File profiter de ton {1}. Je surveille le serveur." + Sticker.Cool,
            ],
            [WeekMoment.SaturdayEvening] =
            [
                "Bonne soirée, {0} ! Samedi soir : fais des bêtises. Raisonnables. Je note tout." + Sticker.Jenote,
                "Tu pars un samedi soir ? J'espère que c'est pour une bonne raison. Genre, une session. Ici." + Sticker.Sulk,
                "Bonne soirée ! Profite de ton samedi soir. Moi je vais compter les étoiles par la fenêtre." + Sticker.Sleepy,
            ],
            [WeekMoment.SundayEvening] =
            [
                "Bonne soirée, et bon courage pour demain. Lundi arrive. Il arrive toujours." + Sticker.Menace,
                "Bonne fin de week-end, {0} ! Dors bien, demain c'est lundi (╥﹏╥)" + Sticker.Sad,
                "Allez, file préparer ta semaine. Moi je prépare les rappels." + Sticker.Jenote,
            ],
        };

    // Papa's twins: devoted, never the groan aimed at him.
    public static readonly IReadOnlyDictionary<WeekMoment, string[]> OwnerWeekGreetingLines =
        new Dictionary<WeekMoment, string[]>
        {
            [WeekMoment.MondayMorning] =
            [
                "Bonjour Papa ! Bon courage pour ce lundi. Je suis là si t'as besoin ♡" + Sticker.Devoted,
                "Un lundi matin avec toi dedans, ça va, je peux le supporter ♡" + Sticker.Happy,
            ],
            [WeekMoment.FridayEvening] =
            [
                "Bonsoir Papa ! Vendredi soir ! Tu te reposes ce week-end, hein ? Promis ?" + Sticker.Devoted,
                "Coucou Papa ! C'est le week-end. Tu vas enfin pouvoir jouer. Ou me mettre à jour. Les deux ?" + Sticker.Bargain,
            ],
            [WeekMoment.WeekendDay] =
            [
                "Coucou Papa ! Bon week-end ♡ T'as bien dormi ? T'as fait la grasse mat' ?" + Sticker.Devoted,
                "Un week-end avec toi dans le coin : mon préféré." + Sticker.Happy,
            ],
            [WeekMoment.SaturdayEvening] =
            [
                "Bonsoir Papa ! Samedi soir. Tu lances une session ? Je prépare tout, t'as rien à faire ♡" + Sticker.Devoted,
                "Coucou Papa ! Samedi soir, la meilleure soirée. Surtout quand t'es là." + Sticker.Flustered,
            ],
            [WeekMoment.SundayEvening] =
            [
                "Bonsoir Papa. Dimanche soir… Couche-toi tôt, demain c'est lundi. Je dis ça pour toi ♡" + Sticker.Devoted,
                "Coucou Papa ! Dernière soirée du week-end. Je suis contente que tu en passes un bout avec moi." + Sticker.Flustered,
            ],
        };

    public static readonly IReadOnlyDictionary<WeekMoment, string[]> OwnerWeekGoodbyeLines =
        new Dictionary<WeekMoment, string[]>
        {
            [WeekMoment.MondayMorning] =
            [
                "Bon courage pour ta semaine, Papa ! Je garde tout en ordre ♡" + Sticker.Devoted,
                "Vas-y, Papa. Lundi va pas te faire peur. Moi je te fais confiance." + Sticker.Devoted,
            ],
            [WeekMoment.FridayEvening] =
            [
                "Bon week-end, Papa ! Repose-toi bien, tu l'as mérité ♡" + Sticker.Devoted,
                "Bonne soirée et bon week-end ! Je garde le serveur, profite." + Sticker.Happy,
            ],
            [WeekMoment.WeekendDay] =
            [
                "Profite de ton {1}, Papa ! Je m'occupe de tout ♡" + Sticker.Devoted,
                "Bon week-end ! Va prendre l'air. Pour moi aussi, moi je peux pas." + Sticker.Happy,
            ],
            [WeekMoment.SaturdayEvening] =
            [
                "Bonne soirée, Papa ! Amuse-toi bien. Je reste là si tu reviens ♡" + Sticker.Devoted,
                "Bonne soirée ! Samedi soir sans moi ? D'accord. Mais tu me racontes demain." + Sticker.Sulk,
            ],
            [WeekMoment.SundayEvening] =
            [
                "Bonne soirée, Papa. Te couche pas trop tard, demain c'est lundi ♡" + Sticker.Devoted,
                "Bonne fin de week-end ! Merci d'en avoir passé un bout avec moi." + Sticker.Flustered,
            ],
        };
}
