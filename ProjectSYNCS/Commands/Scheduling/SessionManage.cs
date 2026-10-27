using Discord;
using ProjectSYNCS.Models;

namespace ProjectSYNCS.Commands;

// The ephemeral « ⚙️ Gérer » panel of a session card, and the cancel confirmation it and
// /schedule cancel share. Organiser and staff only: the panel is refused to anyone else, so the
// public card carries none of these buttons. Every verb is its own (root CLAUDE.md, custom-ids).
public static class SessionManage
{
    public static string Panel(SessionEvent session) =>
        $"**⚙️ Gérer « {Format.Sanitize(session.Title)} »** (#{session.Id})";

    public static MessageComponent PanelComponents(SessionEvent session)
    {
        int id = session.Id;
        var builder = new ComponentBuilder()
            .WithButton("Modifier", $"event:edit:{id}", ButtonStyle.Primary, new Emoji("✏️"), row: 0)
            .WithButton("Annuler la session", $"event:cancel:{id}", ButtonStyle.Danger, new Emoji("🗑️"), row: 0)
            .WithButton("Republier ici", $"event:repost:{id}", ButtonStyle.Secondary, new Emoji("📌"), row: 0);
        if (session.NativeEventId != 0)
            builder.WithButton("Retirer l'événement Discord", $"event:removenative:{id}", ButtonStyle.Secondary,
                new Emoji("📅"), row: 1);
        if (session.RecurringSessionId is not null)
            builder.WithButton("Arrêter la série", $"event:stopseries:{id}", ButtonStyle.Secondary, new Emoji("🔁"), row: 1);
        return builder.Build();
    }

    public static string CancelPrompt(SessionEvent session)
    {
        var line = $"Annuler **{Format.Sanitize(session.Title)}** (<t:{session.ScheduledAt.ToUnixTimeSeconds()}:F>) ? "
                   + "Les inscrits et les « peut-être » reçoivent un DM.";
        if (session.RecurringSessionId is not null) line += "\n-# Juste celle-ci : la série continue. « Arrêter la série » est dans « Gérer ».";
        return line;
    }

    public static MessageComponent CancelConfirm(int eventId) =>
        new ComponentBuilder()
            .WithButton("Oui, annuler", $"event:cancelyes:{eventId}", ButtonStyle.Danger)
            .WithButton("Non", $"event:cancelno:{eventId}", ButtonStyle.Secondary)
            .Build();
}
