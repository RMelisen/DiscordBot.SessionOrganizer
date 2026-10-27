using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// Field custom ids (title/when/max_players/duration/description) must match the pre-filled
// ModalBuilder in ScheduleModule.BuildEditModal so values bind on submit.
public class EditSessionModal : IModal
{
    public string Title => "Modifier la session";

    [InputLabel("Nom de la session")]
    [ModalTextInput("title", maxLength: InputCaps.Title)]
    public string SessionTitle { get; set; } = string.Empty;

    [InputLabel("Quand")]
    [ModalTextInput("when", placeholder: "ex. samedi 21h, 25/12 20:30", maxLength: InputCaps.When)]
    public string When { get; set; } = string.Empty;

    [InputLabel("Places (vide ou 0 = illimité)")]
    [ModalTextInput("max_players", maxLength: 3)]
    [RequiredInput(false)]
    public string MaxPlayers { get; set; } = string.Empty;

    [InputLabel("Durée")]
    [ModalTextInput("duration", placeholder: "ex. 2h, 1h30, 45 min", maxLength: 12)]
    [RequiredInput(false)]
    public string Duration { get; set; } = string.Empty;

    [InputLabel("Description")]
    [ModalTextInput("description", TextInputStyle.Paragraph, maxLength: InputCaps.Description)]
    [RequiredInput(false)]
    public string Description { get; set; } = string.Empty;
}
