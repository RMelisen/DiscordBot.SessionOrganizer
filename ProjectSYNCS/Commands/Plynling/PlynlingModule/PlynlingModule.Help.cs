using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Logging;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Interactions.Modals;
using ProjectSYNCS.Models;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

public abstract partial class PlynlingModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Comment fonctionnent les Plynlings")]
    public Task HelpAsync() => RespondAsync(embed: BuildHelpEmbed(0), components: BuildHelpButtons(0), ephemeral: true);

    /// <summary>
    /// The Plynling guide. Static and Context-free for the same reason as
    /// <see cref="HelpModule.BuildEmbed"/>: embed caps throw at *send* time, so the only way
    /// to know it fits is to build and measure it without a gateway.
    /// </summary>
    // /plynling help is two pages: the pet, and its personality and events. One embed could not hold
    // both under Discord's 6000-character cap. Each page is measured by the harness.
    public static Embed BuildHelpEmbed(int page = 0) => page == 1 ? BuildPersonalityHelp() : BuildCareHelp();

    public static MessageComponent BuildHelpButtons(int page) =>
        new ComponentBuilder()
            .WithButton("Le Plynling", "plyn:help:0", ButtonStyle.Secondary, new Emoji("🍄"), disabled: page == 0)
            .WithButton("Personnalité & événements", "plyn:help:1", ButtonStyle.Secondary, new Emoji("✨"), disabled: page == 1)
            .Build();

    private static Embed BuildPersonalityHelp() =>
        new EmbedBuilder()
            .WithTitle("🍄 Plynlings — personnalité et événements")
            .WithColor(new Color(0xCE323A))
            .AddField("Personnalité",
                "En grandissant, il gagne **4 traits** : un trait d'enfance bébé, deux de plus ado, un dernier adulte. " +
                "Ils façonnent ses **stats** (Diplomatie, Intendance, Sagesse, Ruse, Courage) et un petit titre. " +
                "**Personnalité** (bouton de sa carte) détaille tout, pour n'importe quel Plynling.")
            .AddField("Événements",
                "À peu près une fois par jour, il lui arrive quelque chose : **✨ Événement** apparaît sur sa carte " +
                "(3 en attente au plus). Son propriétaire choisit pour lui, en privé.\n" +
                "Certains choix demandent un trait ou une stat ; d'autres sont des **défis**, avec leurs chances affichées. " +
                "Ses choix l'entraînent : une stat monte pour quelques jours, et parfois, rarement, pour de bon. " +
                "Ce qu'il vit ado oriente son trait d'adulte.\n" +
                "Certains concernent un autre Plynling : une déclaration, un défi, un pacte, une réconciliation — " +
                "son propriétaire reçoit alors la question sur sa propre carte.\n" +
                "Sans réponse en 24 h, il décide **tout seul**, selon son caractère — sans jamais y perdre quoi que ce soit.\n" +
                "Chaque histoire est racontée dans le salon du jeu, page par page.")
            .AddField("Stress et humeurs",
                "Forcer un choix contre son caractère le **stresse** (le coût est affiché). Caresses, jeux, visites et bains " +
                "l'apaisent, et chaque matin aussi. À chaque palier (100, 200, 300), son bonheur file plus vite, puis ses stats " +
                "baissent, et il **craque** : un événement spécial, qui peut lui laisser une manie pour faire face.\n" +
                "Certains choix lui donnent une **humeur** pour quelques jours (💡, 🌧️…) : stats, appétit, bonheur. " +
                "Le stress ne vient **que** de tes choix : un Plynling qui décide seul n'en prend jamais.")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();

    private static Embed BuildCareHelp() =>
        new EmbedBuilder()
            .WithTitle("🍄 Plynlings — mode d'emploi")
            .WithDescription("Un Plynling est un petit champignon qui vit avec toi. Nourris-le, caresse-le, " +
                             "et surtout… ne l'oublie pas.\n-# Raccourcis : `/pl` au lieu de `/plynling` ; en texte, `;pl`, `;pl @…`, `;pl visit @…`, `;forage`, `;work`.")
            .WithColor(new Color(0xCE323A))
            .AddField("Adopter & regarder",
                "**`/plynling adopt name:`** — Gratuit, un seul à la fois. L'espèce est tirée au sort : " +
                "toutes ont les mêmes chances. Garçon ou fille ? Surprise.\n" +
                "**`/plynling view [user]`** — Sa carte, avec les boutons **Caresser** et **Nourrir**.\n" +
                "**`/plynling list`** — Tous les Plynlings vivants du serveur, du plus vieux au plus jeune.\n" +
                "**`/plynling journal [user]`** — Son journal : ses badges et ses souvenirs. Il gagne des **badges** " +
                "en vieillissant, en jouant, en rendant visite et en étant choyé — chacun rapporte quelques cailloux.\n" +
                "Il grandit : **bébé** sa première semaine, **ado** jusqu'à 14 jours, **adulte**, " +
                "puis **ancien** après 6 mois. Le temps passé gelé ne compte pas.")
            .AddField("S'en occuper",
                "La **faim** se vide en **2 jours** : à 0 %, il meurt. Le **bonheur** se vide en **36 heures** " +
                "(il est juste triste).\n" +
                "**Nourrir** (menu de sa carte) — Champignon (15), Shiitake (30), Morille (40, que du bonheur), Truffe (80, faim et bonheur). " +
                "Nourrir celui d'un autre coûte le double. Si tu as ce plat dans ton **garde-manger**, il est servi de là " +
                "(2 pour celui d'un autre) au lieu de tes cailloux.\n" +
                "**Caresser** (bouton de sa carte) — +25 % de bonheur, toutes les 4 h, sur n'importe quel Plynling.\n" +
                "Pour s'occuper de celui de quelqu'un : `/plynling view user:`, puis sa carte.\n" +
                "Il **dort de 1 h à 5 h** : on peut le nourrir, pas le caresser, et il ne meurt jamais dans son sommeil.\n" +
                "Son **humeur** compte : heureux, un repas le nourrit 15 % de plus et il te rapporte parfois un caillou ou un objet ; " +
                "triste, 25 % de moins. À 0 %, il **boude** et refuse de manger tant qu'on n'a pas joué avec lui ou qu'on " +
                "ne l'a pas caressé — sauf s'il meurt de faim.")
            .AddField("Propreté et santé",
                "L'**hygiène** se vide en 3 jours ; balades et jeux le salissent. Sous 33 %, il est **sale** et déprime plus vite. " +
                "**Laver** (sa carte, son propriétaire) : +60 %, toutes les 6 h, ou avec un savon.\n" +
                "Au réveil, il peut tomber **malade**, surtout sale : il mange moitié moins, ne joue ni ne visite. " +
                "**Soigner** (sa carte) : un médicament par jour, sinon il peut en mourir.")
            .AddField("Jouer & rendre visite",
                "**`/plynling play`** — Un mini-jeu au hasard avec ton Plynling : cache-cache, pierre-papier-ciseaux " +
                "ou plus ou moins. Une fois par heure : +15 % de bonheur, +25 % et quelques cailloux si tu gagnes.\n" +
                "**`/plynling visit user:`** — Ton Plynling toque chez quelqu'un. S'il l'accueille dans l'heure, une petite " +
                "histoire se joue, selon leur relation, et les deux gagnent du bonheur. Une visite par jour entre deux personnes.\n" +
                "À force de se voir, ils deviennent **amis**, **meilleurs amis**… ou **rivaux** et **ennemis**. " +
                "Un garçon et une fille très proches peuvent tomber **amoureux**. Plus ils s'aiment, plus leurs visites " +
                "les rendent heureux ; entre ennemis, elles les attristent.\n" +
                "**`/plynling relations [user]`** — Ses amis, ses amours et ses ennemis.")
            .AddField("Passions",
                "Chaque Plynling naît avec une **passion** : la cuisine, la musique, les étoiles, les siestes… " +
                "Pendant les visites, il en parle, et selon l'autre, ça passionne, ça ennuie ou ça tourne à la compétition.\n" +
                "**`/plynling passion`** — Apprends-lui une seconde passion, en toutes lettres (« la pêche », « les trains »). " +
                "Une fois par jour ; laisse vide pour l'effacer.")
            .AddField("Gagner des cailloux",
                "**`/work`** — 40 à 60 cailloux, toutes les 4 h.\n" +
                "Parler, réagir et le vocal rapportent aussi quelques cailloux (45 au plus par jour).\n" +
                "**`/balance`** — Ton solde, visible par toi seul.")
            .AddField("Inventaire & collection",
                "**`/plynling forage`** — Il part fouiller les environs (toutes les 4 h) et revient avec un objet — souvent un champignon — ou de quoi manger.\n" +
                "On trouve aussi des objets dans son cadeau du jour, en gagnant un jeu et pendant les bonnes visites. " +
                "Certains ne se trouvent qu'en une saison.\n" +
                "**`/inventory collection [user]`** — Le carnet, collection par collection : ce que tu as trouvé et ce qui manque. 5 collections de 8 objets (dont les insectes) et une grande de 30 champignons ; chacune complétée rapporte des cailloux.\n" +
                "**`/inventory view`** — Ton garde-manger et tes objets. **`/inventory shop`** — Nourriture, savon " +
                "et médicaments (−10 % dès 5).\n" +
                "**`/inventory give`** · **`/inventory trade`** · **`/inventory sell`** — Offrir, échanger (l'offre dure 1 h) ou vendre.")
            .AddField("Cosmétiques",
                "**`/plynling wardrobe`** — Habille ton Plynling : un thème pour sa carte, un titre, un accessoire, " +
                "et un cadre pour sa tombe.\n" +
                "**`/inventory cosmetics`** — La boutique : des basiques toujours là, une sélection qui change chaque lundi, " +
                "et des articles de saison.\n" +
                "**`/inventory craft`** — Fabrique des cosmétiques rares avec tes objets de collection.\n" +
                "Tes cosmétiques restent à toi et servent à tous tes Plynlings. On peut les offrir ou les échanger, pas les vendre.")
            .AddField("Partir en vacances",
                "**`/plynling freeze`** — Gèle ton Plynling (14 jours au plus) : plus rien ne bouge. " +
                "Seulement s'il a encore au moins 50 % de faim.\n" +
                "**`/plynling thaw`** — Le dégèle. Ensuite, 7 jours avant de pouvoir le regeler.")
            .AddField("La mort",
                "Tu reçois un **message privé** environ 3 h avant qu'il meure de faim (à 23 h la veille si ça tombe la nuit). " +
                "S'il meurt, tout le serveur l'apprend et il rejoint le cimetière.\n" +
                "**`/plynling graveyard [user]`** — Les tombes, triées par date ou par longueur de vie. Plus il a vécu, " +
                "plus sa tombe est belle.")
            .AddField("L'abandonner",
                "**`/plynling abandon`** — Il part pour toujours : pas de tombe, pas de retour. Tu devras taper son nom " +
                "pour confirmer. Tout le serveur l'apprendra, ça se verra sur `/shame`, et tu devras attendre 30 min " +
                "avant d'en adopter un autre.")
            .AddField("Staff",
                "**`/plynling freeze user:`** · **`/plynling thaw user:`** — Sur n'importe quel Plynling.\n" +
                "**`/admin plynling rename user: name:`** · **`/admin plynling resurrect user:`** · " +
                "**`/admin plynling passion-reset user:`** · **`/admin plynling cure user:`**")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();
}
