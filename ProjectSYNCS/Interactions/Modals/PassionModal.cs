using Discord.Interactions;
using ProjectSYNCS.Helpers;

namespace ProjectSYNCS.Interactions.Modals;

// /plynling passion. Built by hand in PlynlingModule.PassionAsync to pre-fill the current
// passion — keep the custom id "passion", the label and the cap in sync with that builder.
public class PassionModal : IModal
{
    public string Title => "Sa passion";

    [InputLabel("Quelle passion veux-tu lui apprendre ?")]
    [RequiredInput(false)]
    [ModalTextInput("passion", placeholder: "Avec son article : la pêche, les trains…", maxLength: InputCaps.Passion)]
    public string Passion { get; set; } = string.Empty;
}
