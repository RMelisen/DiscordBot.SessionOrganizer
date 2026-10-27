using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// The title alone, for `/vote create options:…` given without one: the options are already in the draft.
public class VoteTitleModal : IModal
{
    public string Title => "Nouveau vote";

    [InputLabel("Titre du vote")]
    [ModalTextInput("title", placeholder: "ex. Quel jeu ce soir ?", maxLength: InputCaps.Title)]
    public string PollTitle { get; set; } = string.Empty;
}
