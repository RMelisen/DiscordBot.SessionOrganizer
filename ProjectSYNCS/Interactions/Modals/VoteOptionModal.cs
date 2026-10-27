using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// One more option: the vote wizard's « Ajouter une option », and Gérer's on a live vote.
public class VoteOptionModal : IModal
{
    public string Title => "Ajouter une option";

    [InputLabel("Option")]
    [ModalTextInput("label", placeholder: "ex. Among Us", maxLength: PollRules.MaxLabelLength)]
    public string OptionLabel { get; set; } = string.Empty;
}

// One more slot, typed: the poll wizard's « Autre date… », and Gérer's « Ajouter » on a live date poll.
public class PollSlotModal : IModal
{
    public string Title => "Ajouter un créneau";

    [InputLabel("Quand")]
    [ModalTextInput("when", placeholder: "ex. samedi 21h, 25/12 20:30", maxLength: InputCaps.When)]
    public string When { get; set; } = string.Empty;
}

// Gérer → Renommer. Field id must match PollComponentHandler's pre-filled ModalBuilder.
public class PollRenameModal : IModal
{
    public string Title => "Renommer";

    [InputLabel("Nouveau titre")]
    [ModalTextInput("title", maxLength: InputCaps.Title)]
    public string PollTitle { get; set; } = string.Empty;
}
