using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// /plynling abandon's confirmation: the owner types their Plynling's name. Typing it cannot
// happen by misclick, and the pause to write it out is part of the point.
public class AbandonModal : IModal
{
    public string Title => "Abandonner ton Plynling ?";

    [InputLabel("Tape son nom pour confirmer")]
    [ModalTextInput("name", placeholder: "Son nom, exactement", maxLength: InputCaps.PlynlingName)]
    public string Name { get; set; } = string.Empty;
}
