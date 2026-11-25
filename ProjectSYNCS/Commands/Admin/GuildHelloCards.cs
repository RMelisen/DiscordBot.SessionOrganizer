using Discord;

namespace ProjectSYNCS.Commands;

// The card she posts when she is added to a server (GuildHelloService): her line, what to set up,
// the setup command and the guide page about inviting her. Components V2, sent with
// AllowedMentions.None. Static and Context-free, so the checks measure it.
public static class GuildHelloCards
{
    public const string GuidePage = "inviter/";

    public static MessageComponent Build(string line) =>
        new ComponentBuilderV2()
            .AddComponent(new ContainerBuilder()
                .WithAccentColor(Color.Purple)
                .AddComponent(new TextDisplayBuilder(line))
                .AddComponent(new SeparatorBuilder())
                .AddComponent(new TextDisplayBuilder(
                    "**Pour commencer** (un admin, ou quelqu'un qui peut gérer le serveur) :\n"
                    + "1. `/config setup` : les salons où je poste et les rôles, en quelques clics.\n"
                    + "2. `/config show` : ce qui est réglé, et ce qui me manque ici (une permission, un salon).\n"
                    + "Ensuite, `/help` pour tout le reste."))
                .AddComponent(new ActionRowBuilder().WithButton(HelpModule.GuideButton(GuidePage))))
            .Build();
}
