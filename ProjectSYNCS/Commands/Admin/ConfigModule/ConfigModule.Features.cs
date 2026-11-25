using Discord.Interactions;
using ProjectSYNCS.Helpers;
using ProjectSYNCS.Services;

namespace ProjectSYNCS.Commands;

// /config feature: what she does on her own initiative here (ServerFeatures). On at home, off
// elsewhere until staff switch it on, except the member welcome. Part of /config: the class comment
// and the shared helpers are in ConfigModule.cs. /config setup has the same switches on a page.
public partial class ConfigModule
{
    [Group("feature", "Ce qu'elle fait d'elle-même ici : bonjour du matin, vie ambiante, Quokka…")]
    public class FeatureModule : SyncsModuleBase
    {
        private readonly GuildConfigService _config;

        public FeatureModule(GuildConfigService config)
        {
            _config = config;
        }

        [SlashCommand("enable", "Allumer une fonctionnalité sur ce serveur")]
        public Task EnableAsync([Summary("feature", "Laquelle")] FeatureChoice feature) => SetAsync(feature, true);

        [SlashCommand("disable", "Éteindre une fonctionnalité sur ce serveur")]
        public Task DisableAsync([Summary("feature", "Laquelle")] FeatureChoice feature) => SetAsync(feature, false);

        private async Task SetAsync(FeatureChoice choice, bool enabled)
        {
            if (!await BeginAsync(Context)) return;
            if (ServerFeatures.Find(KeyOf(choice)) is not { } feature) return;

            await _config.SetFeatureAsync(Context.Guild.Id, feature.Key, enabled);
            var config = await _config.GetAsync(Context.Guild.Id);

            var reply = enabled
                ? $"**{feature.Label}** : allumé ദ്ദി◝ ⩊ ◜.ᐟ"
                : $"**{feature.Label}** : éteint. Je me tiens tranquille là-dessus (ᵕ • ᴗ •)";
            if (enabled && feature.NeedsMainChannel && MorningGreetingService.MainChannelOf(config, Context.Guild.Id) == 0)
                reply += "\n⚠️ Il me faut un salon principal pour ça : `/config main-channel set`.";
            await SayAsync(Context, reply);
        }
    }

    /// <summary>The slash choices, one per ServerFeatures key (the checks hold them together).</summary>
    public enum FeatureChoice
    {
        [ChoiceDisplay("Bonjour du matin")] Morning,
        [ChoiceDisplay("Vie ambiante")] Ambient,
        [ChoiceDisplay("Sondage du week-end")] WeekendPoll,
        [ChoiceDisplay("Visites de Quokka")] Quokka,
        [ChoiceDisplay("Spectatrice en vocal")] VoiceSpectator,
        [ChoiceDisplay("Accueil des nouveaux")] Welcome,
    }

    public static string KeyOf(FeatureChoice choice) => choice switch
    {
        FeatureChoice.Morning => ServerFeatures.Morning,
        FeatureChoice.Ambient => ServerFeatures.Ambient,
        FeatureChoice.WeekendPoll => ServerFeatures.WeekendPoll,
        FeatureChoice.Quokka => ServerFeatures.Quokka,
        FeatureChoice.VoiceSpectator => ServerFeatures.VoiceSpectator,
        FeatureChoice.Welcome => ServerFeatures.Welcome,
        _ => "",
    };
}
