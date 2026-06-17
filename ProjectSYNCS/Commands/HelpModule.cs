using Discord;
using Discord.Interactions;

namespace ProjectSYNCS.Commands;

public class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Comment utiliser le bot d'organisation de sessions")]
    public async Task HelpAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("Project S.Y.N.C.S. — Aide")
            .WithDescription(
                "Organise des sessions de jeu, des activités ou des soirées film, " +
                "et laisse les autres s'inscrire en un clic.")
            .WithColor(Color.Blue)
            .AddField("Commandes",
                "**`/schedule create`** — Planifie une nouvelle session (assistant en 4 étapes : type, jour, heure, détails).\n" +
                "**`/schedule list`** — Affiche les sessions actives du serveur. Tu peux republier une carte dans le salon courant.\n" +
                "**`/schedule edit <id>`** — Modifie une session que tu as organisée (titre, date, heure, participants max).\n" +
                "**`/schedule cancel <id>`** — Annule une session que tu as organisée.\n" +
                "**`/help`** — Affiche ce message.")
            .AddField("Créer une session — pas à pas",
                "Lance **`/schedule create`**. Un assistant privé te guide :\n" +
                "**1.** *Type* — choisis la catégorie : 🎮 Jeu, 🧑‍🤝‍🧑 Activité, 🎬 Film ou ✨ Autre.\n" +
                "**2.** *Jour* — sélectionne le jour (Aujourd'hui, Demain, puis les dates des 25 prochains jours).\n" +
                $"**3.** *Heure* — choisis l'heure, de 00h à 23h.\n" +
                "**4.** *Minutes* — choisis :00, :15, :30 ou :45 (pour plus de précision, tu peux modifier l'heure exacte directement sur la carte).\n" +
                "**5.** *Détails* — un formulaire s'ouvre : saisis le **nom** de la session et, en option, " +
                "le **nombre de participants max** ; laisse vide pour un nombre illimité).\n\n" +
                "Le bouton **Retour** permet de revenir à l'étape précédente. " +
                "Une fois le formulaire validé, la carte de session est publiée dans le salon.")
            .AddField("Boutons d'une session",
                "✅ **Rejoindre** — Tu participes.\n" +
                "🔄 **Peut-être** — Tu n'es pas sûr.\n" +
                "✖️ **Refuser** — Tu ne participes pas.\n" +
                "✏️ **Modifier** — Réservé à l'organisateur.\n" +
                "🗑️ **Annuler** — Réservé à l'organisateur.")
            .AddField("Modifier une session",
                "Le bouton **✏️ Modifier** (ou `/schedule edit <id>`) ouvre un formulaire, réservé à l'organisateur, " +
                "pour ajuster :\n" +
                "• le **nom** de la session ;\n" +
                "• la **date** (format `AAAA-MM-JJ`, ex. `2026-06-20`) ;\n" +
                "• l'**heure** (format `HH:mm`, ex. `20:30`) — c'est ici que tu fixes une heure précise ;\n" +
                "• le **nombre de participants max** (0 = illimité).\n" +
                "La carte se met à jour automatiquement après validation.")
            .AddField("Rappels",
                "Les participants inscrits reçoivent un rappel en message privé avant le début " +
                "de la session.")
            .AddField("Bon à savoir",
                $"• L'**ID** d'une session est affiché en pied de carte, à utiliser avec `/edit` et `/cancel`.\n")
            .WithFooter("Project S.Y.N.C.S.")
            .Build();

        await RespondAsync(embed: embed, ephemeral: true);
    }
}
