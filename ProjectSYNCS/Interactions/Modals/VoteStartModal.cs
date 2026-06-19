using Discord.Interactions;

namespace ProjectSYNCS.Interactions.Modals;

public class VoteStartModal : IModal
{
    public string Title => "Nouveau vote";

    [InputLabel("Titre du vote")]
    [ModalTextInput("title", placeholder: "ex. Quel jeu ce soir ?")]
    public string PollTitle { get; set; } = string.Empty;
}
