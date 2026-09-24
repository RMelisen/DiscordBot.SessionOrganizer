using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

public class VoteStartModal : IModal
{
    public string Title => "Nouveau vote";

    [InputLabel("Titre du vote")]
    [ModalTextInput("title", placeholder: "ex. Quel jeu ce soir ?", maxLength: InputCaps.Title)]
    public string PollTitle { get; set; } = string.Empty;
}
