using Discord.Interactions;

namespace ProjectSYNCS.Interactions.Modals;

public class VoteOptionModal : IModal
{
    public string Title => "Ajouter une option";

    [InputLabel("Option")]
    [ModalTextInput("label", placeholder: "ex. Among Us")]
    public string OptionLabel { get; set; } = string.Empty;
}
