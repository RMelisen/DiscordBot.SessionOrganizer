using Discord.Interactions;

namespace ProjectSYNCS.Interactions.Modals;

// A guess in /plynling play's « Plus ou moins ». Three characters: « 100 » is the longest
// valid answer, and anything else is refused in the handler.
public class GuessModal : IModal
{
    public string Title => "Plus ou moins";

    [InputLabel("Ton nombre (de 1 à 100)")]
    [ModalTextInput("guess", placeholder: "50", maxLength: 3)]
    public string Guess { get; set; } = string.Empty;
}
