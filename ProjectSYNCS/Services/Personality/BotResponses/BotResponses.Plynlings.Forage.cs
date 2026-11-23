namespace ProjectSYNCS.Services;

// Pools for one category; the index of every pool is in BotResponses.cs.
internal static partial class BotResponses
{
    // What /plynling forage answers, one pool per place (ForagePlaces keys). {0} = name, {1} = the find
    // as PlynlingText.ItemLabel prints it (emoji, bold name, rarity — no article), so it always comes
    // after a colon. A line must read for a collectible and for food: no « au fond de l'eau ».
    // Places and villagers from docs/village-lore.md § 5.
    public static readonly IReadOnlyDictionary<string, GenderedLines> PlynlingForaged = new Dictionary<string, GenderedLines>
    {
        ["sommeille"] = new(
            M: new[]
            {
                "**{0}** revient de la Sommeille, les pieds mouillés et très fier de lui. Il rapporte : {1} !",
                "Au Pont-Vieux, **{0}** a touché la pierre du pont, comme il se doit, puis fouillé les berges. Trouvé : {1} !",
                "La Sommeille coule si lentement que **{0}** a eu le temps de tout regarder. Sur la berge : {1} !",
                "**{0}** est rentré du bord de la rivière avec du sable plein le chapeau, et ceci : {1} !",
            },
            F: new[]
            {
                "**{0}** revient de la Sommeille, les pieds mouillés et très fière d'elle. Elle rapporte : {1} !",
                "Au Pont-Vieux, **{0}** a touché la pierre du pont, comme il se doit, puis fouillé les berges. Trouvé : {1} !",
                "La Sommeille coule si lentement que **{0}** a eu le temps de tout regarder. Sur la berge : {1} !",
                "**{0}** est rentrée du bord de la rivière avec du sable plein le chapeau, et ceci : {1} !",
            }),
        ["lisiere"] = new(
            M: new[]
            {
                "**{0}** a longé la Lisière sans creuser près de la Souche — c'est la coutume. Il revient quand même les bras pleins : {1} !",
                "La Mère Lichen n'a rien dit quand **{0}** est passé devant sa cabane. Elle a juste montré un coin de mousse, du menton. Dessous : {1} !",
                "**{0}** rentre de la Lisière en sentant l'humus et la feuille mouillée. Sa trouvaille : {1} !",
                "Sous les grands arbres, **{0}** s'est senti tout petit, et tout content. Il rapporte : {1} !",
            },
            F: new[]
            {
                "**{0}** a longé la Lisière sans creuser près de la Souche — c'est la coutume. Elle revient quand même les bras pleins : {1} !",
                "La Mère Lichen n'a rien dit quand **{0}** est passée devant sa cabane. Elle a juste montré un coin de mousse, du menton. Dessous : {1} !",
                "**{0}** rentre de la Lisière en sentant l'humus et la feuille mouillée. Sa trouvaille : {1} !",
                "Sous les grands arbres, **{0}** s'est sentie toute petite, et toute contente. Elle rapporte : {1} !",
            }),
        ["mouilles"] = new(
            M: new[]
            {
                "**{0}** revient des Mouilles avec de la boue jusqu'au chapeau. Il jure que ça valait le coup : {1} !",
                "Une grenouille a regardé **{0}** fouiller les roseaux pendant une heure sans rien dire. Résultat : {1} !",
                "Aux Mouilles, une libellule a suivi **{0}** tout du long, comme pour surveiller. Il rapporte quand même : {1} !",
                "**{0}** est rentré du marais par le chemin le plus sec, c'est-à-dire pas sec du tout. Dans ses bras : {1} !",
            },
            F: new[]
            {
                "**{0}** revient des Mouilles avec de la boue jusqu'au chapeau. Elle jure que ça valait le coup : {1} !",
                "Une grenouille a regardé **{0}** fouiller les roseaux pendant une heure sans rien dire. Résultat : {1} !",
                "Aux Mouilles, une libellule a suivi **{0}** tout du long, comme pour surveiller. Elle rapporte quand même : {1} !",
                "**{0}** est rentrée du marais par le chemin le plus sec, c'est-à-dire pas sec du tout. Dans ses bras : {1} !",
            }),
        ["communal"] = new(
            M: new[]
            {
                "**{0}** a traversé le Communal pendant qu'on se disputait encore à qui était la chèvre. Personne ne l'a vu ramasser ceci : {1} !",
                "Au Communal, une chèvre s'est beaucoup intéressée au chapeau de **{0}**. Il a tenu bon, et rapporte : {1} !",
                "**{0}** revient du Communal, un brin d'herbe accroché au pied. Sa récolte : {1} !",
                "Dans les hautes herbes du Communal, **{0}** a disparu un long moment. Il réapparaît avec ceci : {1} !",
            },
            F: new[]
            {
                "**{0}** a traversé le Communal pendant qu'on se disputait encore à qui était la chèvre. Personne ne l'a vue ramasser ceci : {1} !",
                "Au Communal, une chèvre s'est beaucoup intéressée au chapeau de **{0}**. Elle a tenu bon, et rapporte : {1} !",
                "**{0}** revient du Communal, un brin d'herbe accroché au pied. Sa récolte : {1} !",
                "Dans les hautes herbes du Communal, **{0}** a disparu un long moment. Elle réapparaît avec ceci : {1} !",
            }),
        ["puits"] = new(
            M: new[]
            {
                "**{0}** a fait le tour de la place du Puits, entre les étals et les enfants qui courent. Il a trouvé : {1} !",
                "Pendant que l'Intendant comptait quelque chose sur la place, **{0}** a regardé par terre. Bien vu : {1} !",
                "Près du Puits, **{0}** a repéré ce que quelqu'un avait laissé tomber un jour de marché. Trouvé, pas volé : {1} !",
                "**{0}** revient de la place du Puits avec l'air de quelqu'un qui a fait une affaire. Sans payer un caillou : {1} !",
            },
            F: new[]
            {
                "**{0}** a fait le tour de la place du Puits, entre les étals et les enfants qui courent. Elle a trouvé : {1} !",
                "Pendant que l'Intendant comptait quelque chose sur la place, **{0}** a regardé par terre. Bien vu : {1} !",
                "Près du Puits, **{0}** a repéré ce que quelqu'un avait laissé tomber un jour de marché. Trouvé, pas volé : {1} !",
                "**{0}** revient de la place du Puits avec l'air de quelqu'un qui a fait une affaire. Sans payer un caillou : {1} !",
            }),
    };
}
