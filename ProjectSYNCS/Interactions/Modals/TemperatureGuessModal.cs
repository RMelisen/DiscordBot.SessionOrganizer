using Discord.Interactions;

namespace ProjectSYNCS.Interactions.Modals;

// /arcade temperature's guess: one number, in °C. Parsed by TemperatureRules.ParseGuess, which
// accepts « 52 », « 52,5 » and « 52.5 °C ».
public class TemperatureGuessModal : IModal
{
    public string Title => "Ma température, en °C";

    [InputLabel("Ton pari")]
    [ModalTextInput("guess", placeholder: "ex. 52,5", maxLength: 8)]
    public string Guess { get; set; } = string.Empty;
}
