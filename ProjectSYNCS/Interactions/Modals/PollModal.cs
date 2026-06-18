using Discord.Interactions;

namespace ProjectSYNCS.Interactions.Modals;

public class PollModal : IModal
{
    public string Title => "Nouveau sondage";

    [InputLabel("Titre du sondage")]
    [ModalTextInput("title", placeholder: "ex. Soirée jeux cette semaine ?")]
    public string PollTitle { get; set; } = string.Empty;
}
