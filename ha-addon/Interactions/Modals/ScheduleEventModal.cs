using Discord.Interactions;

namespace ProjectSYNCS.Interactions.Modals;

public class ScheduleEventModal : IModal
{
    public string Title => "Planifier une session";

    [InputLabel("Nom de la session")]
    [ModalTextInput("title", placeholder: "ex. Among Us, Gartic, Anime ?")]
    public string SessionTitle { get; set; } = string.Empty;

    [InputLabel("Nombre de participants max - Optionnel")]
    [ModalTextInput("max_players")]
    [RequiredInput(false)]
    public string MaxPlayers { get; set; } = string.Empty;
}
