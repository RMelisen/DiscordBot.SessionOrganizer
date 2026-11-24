using Discord;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Commands;

// The two guides: /report help (what each report shows, read by staff and the owner —
// SessionPermissions.IsStaff) and /debug help (what each owner tool does, owner-only like the tools).
//
// Hand-maintained like /help: a new /report or /debug subcommand gets a line here. Static and
// Context-free so the checks can measure both against the embed caps (1024 a field, 6000 in all),
// which throw at send time.
public static class StaffHelpCards
{
    public const string Refusal = "Ce mode d'emploi est réservé aux administrateurs et aux modérateurs (˶ᵔ ᵕ ᵔ˶)";

    // Its page of the online guide (the /report section of staff.md).
    public static MessageComponent BuildReportHelpButtons() =>
        new ComponentBuilder().WithButton(HelpModule.GuideButton("staff/#report")).Build();

    public static Embed BuildReportHelp() =>
        new EmbedBuilder()
            .WithTitle("📊 Mes rapports — mode d'emploi")
            .WithColor(Color.Blue)
            .WithDescription(
                "Réservés aux administrateurs et aux modérateurs. Chaque rapport est **posté dans le salon, avec les noms** " +
                "(absents, retardataires, endormis compris) : choisis bien où tu le demandes.\n" +
                "Il s'ouvre sur **30 jours** ; ses boutons passent à **7 jours** ou **depuis toujours** " +
                "(depuis le début des relevés). Les flèches comparent à la période d'avant, au même rythme par jour compté.")
            .AddField("La communauté",
                "**`/report community`** — Membres actifs, les plus actifs (XP, écrit, vocal), arrivées et départs, émotes, quiz.\n" +
                "**`/report activity`** — Heures de pointe, jour le plus bavard, salons écrits et vocaux, " +
                "ceux qui restent silencieux ou vides.\n" +
                "**`/report sleeping`** — Les membres sans signe de vie depuis 30, 60 ou 90 jours (ses propres boutons). " +
                "Les nouveaux venus sont laissés de côté.\n" +
                "**`/report emotes`** — Les émotes préférées, celles qu'on oublie, les tendances, celles venues d'ailleurs.")
            .AddField("Les rendez-vous",
                "**`/report sessions`** — Inscriptions, présence, retards et absences, les créneaux qui marchent le mieux.\n" +
                "**`/report polls`** — Les sondages et ceux devenus des sessions, mon sondage du mercredi, les appels `/tonight`.")
            .AddField("Mes jeux",
                "**`/report quiz`** — Trouvé ou pas, par catégorie et par difficulté, les questions que personne n'a trouvées, le plus rapide.\n" +
                "**`/report plynlings`** — Les vivants, naissances et décès, leur santé, les événements, les liens, ceux à surveiller.\n" +
                "**`/report minecraft`** — Mon temps de jeu, mes ordres, mes aventures, mes récoltes.")
            .AddField("Moi et mes rouages",
                "**`/report syncs`** — Comment je vais : température, pannes et disponibilité, disque, mémoire et courant, " +
                "bavardage, réputation (good bot / bad bot), le temps en vocal avec moi.\n" +
                "**`/report commands`** — Les commandes les plus utilisées, par combien de personnes, les erreurs, " +
                "les raccourcis tapés, celles qu'on n'utilise pas. `/debug` est compté à part.\n" +
                "**`/report incidents`** — Ce qui a coincé : boutons et formulaires ratés, réponses trop lentes, " +
                "avertissements et erreurs du journal, base de données occupée, coupures et redémarrages.")
            .AddField("Et chaque mois",
                "Le 1er du mois, Papa et Tata reçoivent en DM un résumé du mois écoulé, avec ce qui mérite un coup d'œil, " +
                "suivi des rapports qui les concernent.")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();

    public static Embed BuildDebugHelp() =>
        new EmbedBuilder()
            .WithTitle("🔧 /debug — mode d'emploi")
            .WithColor(Color.DarkGrey)
            .WithDescription(
                "Tes outils de développement, Papa. **Toi seul peux t'en servir** : tout le monde voit `/debug` dans la liste, " +
                "mais chaque outil refuse les autres.")
            .AddField("Parler à ma place",
                "**`tell`** — Me fait dire un message dans un salon, éventuellement en réponse à un message. " +
                "Avec `announce`, je précise que c'est de ta part.\n" +
                "**`dm`** — M'envoie dire un message privé à quelqu'un.\n" +
                "**`delete`** — Supprime un de mes messages, par son lien. Jamais celui de quelqu'un d'autre.\n" +
                "**`react`** — Me fait réagir à un message avec l'émoji de ton choix, par son lien (ou retirer ma réaction).\n" +
                "**`sticker`** — Poste ici une de mes images, au choix de l'humeur.")
            .AddField("Mon comportement",
                "**`silence`** — Je ne parle plus que pour répondre aux commandes et aux boutons. Les rappels, " +
                "récaps et tirages programmés partent quand même. Tient après un redémarrage.\n" +
                "**`absent`** — Quand on te mentionne, je réponds que tu n'es pas disponible et je te transmets le message. " +
                "S'efface au redémarrage.\n" +
                "**`minecraft`** — Mon état sur le serveur Minecraft, mon inventaire ; me faire venir, partir, ou m'interdire de jouer.")
            .AddField("Consulter",
                "**`confession`** — Qui a écrit la confession n°N. L'anonymat de `/confess` s'arrête à toi.\n" +
                "**`answers`** — Ce qu'on me dit le plus, et les messages auxquels je n'ai pas su répondre précisément (posté dans le salon).\n" +
                "**`milestones`** — Le compte de messages de ce salon, ses paliers et qui les a atteints.\n" +
                "**`digest`** — Le résumé du mois dernier tout de suite en DM, le tien ou un aperçu de celui de Tata.\n" +
                "**`voyager`** — Où en est Voyager, ou poster une ligne de sa correspondance.\n" +
                "**`emotes`** — Génère le code des émotes du serveur que je ne connais pas encore.")
            .AddField("Coups de pouce et tests",
                "**`role`** — Donne un rôle à quelqu'un, avec mes permissions (jamais un rôle au-dessus du mien).\n" +
                "**`quiz`** — Pose une question tout de suite, ferme celle en cours, ou couronne la grosse tête de la semaine.\n" +
                "**`quokka`** — Fait venir Quokka tout de suite dans le salon des bots.\n" +
                "**`plynling`** · **`stress`** · **`modifier`** · **`event`** — Règlent ton propre Plynling pour tester " +
                "(hygiène, maladie, stress, modificateurs, événements).\n" +
                "**`heal`** — Remet un Plynling vivant en pleine forme : besoins au max, guéri, sans stress.\n" +
                "**`affinity`** — Les points d'affinité d'un Plynling avec chacun, et sa compatibilité cachée.\n" +
                "**`sweep`** — Lance tout de suite le passage horaire des Plynlings.")
            .WithFooter($"Project S.Y.N.C.S. v{AppInfo.Version}")
            .Build();
}
