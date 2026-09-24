using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

public class PollModal : IModal
{
    public string Title => "Nouveau sondage";

    [InputLabel("Titre du sondage")]
    [ModalTextInput("title", placeholder: "ex. Soirée jeux cette semaine ?", maxLength: InputCaps.Title)]
    public string PollTitle { get; set; } = string.Empty;
}
