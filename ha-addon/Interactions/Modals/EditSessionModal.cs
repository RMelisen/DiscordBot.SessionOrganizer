using Discord.Interactions;

namespace ProjectSYNCS.Interactions.Modals;

// Field custom ids (title/date/time/max_players) must match the pre-filled
// ModalBuilder in ScheduleModule.BuildEditModal so values bind on submit.
public class EditSessionModal : IModal
{
    public string Title => "Modifier la session";

    [InputLabel("Nom de la session")]
    [ModalTextInput("title")]
    public string SessionTitle { get; set; } = string.Empty;

    [InputLabel("Date (AAAA-MM-JJ)")]
    [ModalTextInput("date", placeholder: "ex. 2026-06-20")]
    public string Date { get; set; } = string.Empty;

    [InputLabel("Heure (HH:mm)")]
    [ModalTextInput("time", placeholder: "ex. 20:30")]
    public string Time { get; set; } = string.Empty;

    [InputLabel("Participants max (0 = illimité)")]
    [ModalTextInput("max_players")]
    [RequiredInput(false)]
    public string MaxPlayers { get; set; } = string.Empty;
}
