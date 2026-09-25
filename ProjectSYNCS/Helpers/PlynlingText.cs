using ProjectSYNCS.Models;

using ProjectSYNCS.Services;

namespace ProjectSYNCS.Helpers;

// Fixed French lines for refusals and plain notices. Deliberately not ResponsePicker
// pools: the pools exist so repeated *chatter* doesn't repeat, and a refusal is not
// chatter — varied, it would read as scripted.
//
// A line about one specific Plynling takes its gender ("ta Plynling", "gelée"); a line
// about the *person* — they have none, they already have one — stays in the generic
// masculine, since no particular Plynling is meant.
public static class PlynlingText
{
    public static string WorkCooldown(DateTimeOffset next) =>
        $"Tu as déjà travaillé. Prochain service <t:{next.ToUnixTimeSeconds()}:R>.";

    public const string NoPlynling = "Tu n'as pas de Plynling. `/plynling adopt` pour en adopter un !";
    public const string AlreadyHasOne = "Tu as déjà un Plynling. Un seul à la fois !";
    public const string EmptyName = "Il lui faut un vrai nom.";
    public const string Unknown = "Ce bouton ne correspond plus à rien.";
    public const string StaffOnly = "Cette action est réservée au staff.";
    public const string NoGrave = "Personne à ressusciter : cette personne n'a aucun Plynling au cimetière.";
    public const string ResurrectBlocked = "Cette personne a déjà un Plynling vivant — un seul à la fois.";
    // Refused before the Plynling is even loaded, so it cannot know the gender: worded to
    // need none.
    // Refused before the Plynling is loaded, so worded to need no gender. The timestamp is
    // Discord's own relative one (« dans 2 heures »), so it counts down in the client.
    public static string PetCooldown(DateTimeOffset readyAt) =>
        $"Une caresse toutes les 4 heures, pas plus. Prochaine caresse <t:{readyAt.ToUnixTimeSeconds()}:R>.";

    public static string NoneFor(ulong userId) => $"<@{userId}> n'a pas de Plynling.";

    // About the person, not a Plynling (the abandoned one is gone): generic masculine.
    public static string AdoptCooldown(DateTimeOffset ready) =>
        $"Tu viens d'abandonner un Plynling. Tu pourras en adopter un autre <t:{ready.ToUnixTimeSeconds()}:R>.";

    public static string AbandonMismatch(PlynlingGender g) =>
        $"Ce n'est pas son nom. Rien n'a été fait : {g.Agree("ton Plynling est toujours là", "ta Plynling est toujours là")}.";

    public static string AbandonDone(PlynlingGender g, string name) =>
        $"Tu as abandonné **{name}**. {g.Agree("Il", "Elle")} ne reviendra pas.";

    public static string TooPoor(long price, long balance) =>
        $"Il te faut {PebbleEconomy.Cailloux(price)}, tu n'en as que {balance}. `/work` pour en gagner.";

    public static string Dead(PlynlingGender g) => $"{g.Agree("Ce", "Cette")} Plynling n'est plus de ce monde… 🪦";

    public static string Frozen(PlynlingGender g) => g.Agree(
        "Ce Plynling est gelé : rien ne bouge tant qu'il n'est pas dégelé.",
        "Cette Plynling est gelée : rien ne bouge tant qu'elle n'est pas dégelée.");

    public static string Wasted(PlynlingGender g) =>
        $"{g.Agree("Il", "Elle")} n'a besoin de rien de tout ça pour l'instant — garde tes cailloux.";

    public static string PlayCooldown(PlynlingGender g) =>
        $"{g.Agree("Il", "Elle")} a déjà joué il y a moins d'une heure. Laisse-{g.Agree("le", "la")} souffler un peu !";

    public const string GameOver = "Cette partie est terminée.";

    // Under a meal served from the feeder's pantry. giverId: the friend who served it, if it
    // was not the owner.
    public static string FromPantry(string food, int used, int left, ulong? giverId) =>
        (giverId is { } g ? $"offert par <@{g}> · " : "") +
        $"depuis le garde-manger : −{used} {food} (il en reste {left})";

    public const string ShopTooPoor = "Pas assez de cailloux pour ça. `/work` pour en gagner.";
    public const string GiveSelf = "Tu ne peux pas te faire un cadeau à toi-même !";
    public const string GiveBot = "Les bots n'ont pas d'inventaire.";
    public const string NotEnoughItems = "Tu n'en as pas assez.";
    public const string UnknownItem = "Cet objet n'existe pas. Choisis-le dans la liste.";

    public static string Bought(int quantity, string food, long price, long balance, bool discounted) =>
        $"🛒 Tu as acheté **{quantity} × {food}** pour {PebbleEconomy.Cailloux(price)}" +
        (discounted ? " (−10 %)" : "") + $". Il te reste {PebbleEconomy.Cailloux(balance)}.";

    public static string Gave(ulong fromId, ulong toId, int quantity, string emoji, string name) =>
        $"🎁 <@{fromId}> offre **{quantity} × {emoji} {name}** à <@{toId}> !";

    public static string Sold(int quantity, ItemInfo item, long earned, long balance) =>
        $"💰 Tu as vendu **{quantity} × {item.Emoji} {item.Name}** pour {PebbleEconomy.Cailloux(earned)}. Il te reste {PebbleEconomy.Cailloux(balance)}.";

    // Trades. The two sides are « N × emoji nom ».
    public static string TradeSide(ItemInfo item, int quantity) => $"**{quantity} × {item.Emoji} {item.Name}**";

    public static string TradeOffered(ulong fromId, ulong toId, string give, string want, DateTimeOffset expires) =>
        $"🔁 <@{fromId}> propose un échange à <@{toId}> : {give} contre {want}.\n-# Expire <t:{expires.ToUnixTimeSeconds()}:R>.";

    public static string TradeDone(ulong fromId, ulong toId, string give, string want) =>
        $"🤝 Échange conclu ! <@{fromId}> a donné {give} à <@{toId}> contre {want}.";

    public static string TradeDeclined(ulong fromId, ulong toId, string give, string want) =>
        $"❌ <@{toId}> a refusé l'échange de <@{fromId}> ({give} contre {want}).";

    public static string TradeCancelled(ulong fromId, string give, string want) =>
        $"🚫 <@{fromId}> a retiré son offre ({give} contre {want}).";

    public static string TradeFailed(ulong fromId, string give, string want) =>
        $"⚠️ Échange impossible : <@{fromId}> n'a plus {give}. L'offre est retirée. (Il fallait {want} en retour.)";

    public const string TradeSelf = "Tu ne peux pas échanger avec toi-même !";
    public const string TradeSameItem = "Échanger un objet contre le même, ça ne change rien !";
    public const string TradeGone = "Cette offre n'existe plus (expirée, remplacée ou déjà traitée).";
    public const string TradeNotYours = "Cette offre ne t'est pas adressée.";
    public const string TradeYouLack = "Tu n'as pas ce qu'on te demande en échange. L'offre reste ouverte jusqu'à son expiration.";

    public static string TradeTheyLack(ulong toId) => $"<@{toId}> n'en a pas assez pour cet échange.";

    public static string SetCompleted(ulong userId, CollectionSet set) =>
        $"🏆 <@{userId}> a complété la collection **{set.Emoji} {set.Name}** ! +{PebbleEconomy.Cailloux(set.Reward)}";

    // A visit's confession, the visitor declaring itself. Names are already sanitised.
    public static string ConfessionAccepted(string a, string b) => $"💞 **{a}** a déclaré sa flamme à **{b}**… et c'est oui !";
    public static string ConfessionRefused(string a, string b) => $"💔 **{a}** a déclaré sa flamme à **{b}**… mais c'est non.";
    public static string BrokeUp(string a, string b) => $"💔 **{a}** et **{b}** se sont séparés.";
    public const string VisitSelf = "Ton Plynling ne peut pas se rendre visite à lui-même !";
    public const string NotYourInvite = "Cette invitation ne t'est pas adressée.";
    public const string VisitorGone = "Le visiteur n'est plus là…";
    public const string VisitFrozen = "L'un des deux Plynlings est gelé : pas de visite pour l'instant.";
    public const string InviteExpired = "🚪 Personne n'a ouvert : l'invitation a expiré.";

    public static string VisitedToday(ulong otherId) =>
        $"Vos Plynlings se sont déjà vus aujourd'hui, avec <@{otherId}>. Revenez demain !";
    public const string NotYourGame = "Ce n'est pas ta partie — lance la tienne avec `/plynling play`.";
    public const string GuessRange = "Un nombre entier entre 1 et 100, s'il te plaît.";

    // The name is already sanitised by the caller.
    public static string GiftFound(string name, long amount) =>
        $"🪨 **{name}** a trouvé un joli caillou pour toi ! +{PebbleEconomy.Cailloux(amount)}";

    // The item finds. Names are sanitised by the caller. A find is followed by any set it completed.
    public static string ItemLabel(ItemInfo item) =>
        item.Kind == ItemKind.Food ? $"{item.Emoji} **{item.Name}**" : $"{item.Emoji} **{item.Name}** ({ItemCatalog.RarityLabel(item.Rarity)})";

    public static string GiftItem(string name, ItemInfo item) =>
        $"🎁 **{name}** a trouvé quelque chose pour toi : {ItemLabel(item)} !";

    public static string PlayFind(string name, ItemInfo item) =>
        $"✨ En jouant, **{name}** a déniché {ItemLabel(item)} !";

    public static string VisitFind(string name, ulong ownerId, ItemInfo item) =>
        $"✨ **{name}** rapporte {ItemLabel(item)} pour <@{ownerId}> !";

    public static string Foraged(string name, PlynlingGender g, ItemInfo item) => item.Kind == ItemKind.Food
        ? $"🧺 **{name}** est {g.Agree("revenu", "revenue")} de sa balade avec de quoi manger : {ItemLabel(item)}, rangé dans ton garde-manger !"
        : $"🧺 **{name}** est {g.Agree("revenu", "revenue")} de sa balade avec {ItemLabel(item)} !";

    public static string ForageTooSoon(PlynlingGender g, DateTimeOffset ready) =>
        $"{g.Agree("Il", "Elle")} se remet de sa dernière balade. Prochaine sortie <t:{ready.ToUnixTimeSeconds()}:R>.";

    // The find's line, then one line per collection it completed.
    public static string FindLines(string line, ItemFind find, ulong ownerId) =>
        line + string.Concat(find.Completed.Select(set => "\n" + SetCompleted(ownerId, set)));

    public static string Sulking(PlynlingGender g) =>
        $"{g.Agree("Il", "Elle")} boude : {g.Agree("il", "elle")} veut qu'on joue avec {g.Agree("lui", "elle")} ou qu'on {g.Agree("le", "la")} caresse.";

    // Under a meal eaten happy or sad; null for an ordinary one.
    public static string? MealMood(PlynlingGender g, double factor) =>
        factor > 1 ? $"{g.Agree("Heureux", "Heureuse")}, {g.Agree("il", "elle")} mange de bon appétit (+{Pct(factor - 1)} %)"
        : factor < 1 ? $"Triste, {g.Agree("il", "elle")} chipote (−{Pct(1 - factor)} %)"
        : null;

    private static int Pct(double share) => (int)Math.Round(share * 100);

    public static string Asleep(PlynlingGender g) => $"Chut… {g.Agree("il", "elle")} dort. Reviens après 5 h.";

    public static string AlreadyFrozen(PlynlingGender g) => g.Agree("Il est déjà gelé.", "Elle est déjà gelée.");

    public static string NotFrozen(PlynlingGender g) => g.Agree("Il n'est pas gelé.", "Elle n'est pas gelée.");

    public static string TooHungryToFreeze(PlynlingGender g) => g.Agree(
        "Trop tard pour le geler : il a déjà trop faim (moins de 50 %). Nourris-le d'abord.",
        "Trop tard pour la geler : elle a déjà trop faim (moins de 50 %). Nourris-la d'abord.");

    public static string ThawStaffOnly(PlynlingGender g) => g.Agree(
        "C'est le staff qui l'a gelé : seul le staff peut le dégeler.",
        "C'est le staff qui l'a gelée : seul le staff peut la dégeler.");

    public static string FreezeCooldown(PlynlingGender g, DateTimeOffset next) =>
        $"Tu l'as {g.Agree("dégelé", "dégelée")} il y a moins de 7 jours. Prochain gel possible <t:{next.ToUnixTimeSeconds()}:R>.";

    // "…Notice", not "Frozen"/"Thawed": those names are the refusals above.
    public static string FrozenNotice(PlynlingGender g, string name, DateTimeOffset? until) => until is { } u
        ? $"❄️ **{name}** est {g.Agree("gelé", "gelée")} jusqu'au <t:{u.ToUnixTimeSeconds()}:f>. Rien ne bouge d'ici là."
        : $"❄️ **{name}** est {g.Agree("gelé", "gelée")} jusqu'à nouvel ordre du staff.";

    public static string ThawedNotice(PlynlingGender g, string name) =>
        $"🌱 **{name}** est {g.Agree("dégelé", "dégelée")}. La faim reprend son cours !";

    // Appended to the pet line on the card: "— caressée par @quelqu'un".
    public static string PettedBy(PlynlingGender g, ulong petterId) => $"{g.Agree("caressé", "caressée")} par <@{petterId}>";
}
