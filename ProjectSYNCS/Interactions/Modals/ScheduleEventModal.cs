using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// Field custom ids (title/description) must match ScheduleModule.DetailsModal, which builds this
// modal by hand to pre-fill it (the picker, « Modifier » on the preview, a poll turned into a session).
// Places are picked on the preview, not typed.
public class ScheduleEventModal : IModal
{
    public string Title => "Planifier une session";

    [InputLabel("Nom de la session")]
    [ModalTextInput("title", placeholder: "ex. Among Us, Gartic, Anime ?", maxLength: InputCaps.Title)]
    public string SessionTitle { get; set; } = string.Empty;

    [InputLabel("Description (facultatif)")]
    [ModalTextInput("description", TextInputStyle.Paragraph, maxLength: InputCaps.Description)]
    [RequiredInput(false)]
    public string Description { get; set; } = string.Empty;
}

// « Autre date… » in the picker's day select: the date typed, with the name, in one modal
// (a modal can't open another). Field ids must match ScheduleModule.TypedModal.
public class ScheduleTypedModal : IModal
{
    public string Title => "Planifier une session";

    [InputLabel("Nom de la session")]
    [ModalTextInput("title", placeholder: "ex. Among Us, Gartic, Anime ?", maxLength: InputCaps.Title)]
    public string SessionTitle { get; set; } = string.Empty;

    [InputLabel("Quand")]
    [ModalTextInput("when", placeholder: "ex. samedi 21h, 25/12 20:30, demain", maxLength: InputCaps.When)]
    public string When { get; set; } = string.Empty;

    [InputLabel("Description (facultatif)")]
    [ModalTextInput("description", TextInputStyle.Paragraph, maxLength: InputCaps.Description)]
    [RequiredInput(false)]
    public string Description { get; set; } = string.Empty;
}
