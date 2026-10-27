using Discord;
using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// The vote's title and all its options in one go (PollRules.ParseOptions reads the lines).
// Sent by hand through VoteModule.StartModal (pre-filled title): keep ids, labels and caps in step.
public class VoteStartModal : IModal
{
    public string Title => "Nouveau vote";

    [InputLabel("Titre du vote")]
    [ModalTextInput("title", placeholder: "ex. Quel jeu ce soir ?", maxLength: InputCaps.Title)]
    public string PollTitle { get; set; } = string.Empty;

    [InputLabel("Options, une par ligne (2 à 10)")]
    [ModalTextInput("options", TextInputStyle.Paragraph, placeholder: "Among Us\nGartic Phone\nLethal Company",
        maxLength: InputCaps.VoteOptions)]
    public string Options { get; set; } = string.Empty;
}
