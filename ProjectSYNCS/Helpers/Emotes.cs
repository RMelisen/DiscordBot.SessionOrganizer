namespace ProjectSYNCS.Helpers;

// Every custom emote this server's bot uses, defined once.
//
// Centralised for three reasons, none of them cosmetic. **One:** hi_cat used to be
// written out thirteen times across three files in two different shapes — markup in
// BotResponses and ReminderService, a bare ulong in MessageCues — so a re-upload
// meant finding all of them. **Two:** emotes embedded in response lines were
// unreachable by any test; only the four reaction pools were checked, so a typo in a
// snowflake inside a line of chatter just rendered as literal text in Discord.
// **Three:** a custom emote written *without* its id (`<:name:>`) parses as an
// "emoji" whose name is the literal markup, which Discord rejects at send time —
// having one definition per emote makes that reviewable, and testable.
//
// Ids are `const string` rather than `ulong` on purpose: MessageCues only ever
// searches message text for them, and a string constant is what lets the markup
// below stay a compile-time constant. A ulong hole would not compile as one.
//
// These are this specific server's emotes, like the ids in AvailabilityService and
// BotResponses.PersonalComebacks. The bot can only react with an emote from a guild
// it shares.
public static class Emotes
{
    /// <summary>`:adorablefrog:`</summary>
    public const string AdorableFrogId = "885135007822282762";
    public const string AdorableFrog = $"<:adorablefrog:{AdorableFrogId}>";

    /// <summary>`:cathearte_:`</summary>
    public const string CatHeartId = "982024469956669501";
    public const string CatHeart = $"<:cathearte_:{CatHeartId}>";

    /// <summary>`:cryingcat:`</summary>
    public const string CryingCatId = "885135195915845653";
    public const string CryingCat = $"<:cryingcat:{CryingCatId}>";

    /// <summary>`:dancingblob:` (animated)</summary>
    public const string DancingBlobId = "885209918892810330";
    public const string DancingBlob = $"<a:dancingblob:{DancingBlobId}>";

    /// <summary>`:10sur10:`</summary>
    public const string DixSurDixId = "885134866046419016";
    public const string DixSurDix = $"<:10sur10:{DixSurDixId}>";

    /// <summary>`:fuminodepression:`</summary>
    public const string FuminoDepressionId = "1531341412514267146";
    public const string FuminoDepression = $"<:fuminodepression:{FuminoDepressionId}>";

    /// <summary>`:gooseknife:`</summary>
    public const string GooseKnifeId = "885214057756500019";
    public const string GooseKnife = $"<:gooseknife:{GooseKnifeId}>";

    /// <summary>`:hi_cat:` (animated)</summary>
    public const string HiCatId = "1482305105276571774";
    public const string HiCat = $"<a:hi_cat:{HiCatId}>";

    /// <summary>`:htph:`</summary>
    public const string HtphId = "885137301259321405";
    public const string Htph = $"<:htph:{HtphId}>";

    /// <summary>`:mcheart:`</summary>
    public const string McHeartId = "982024259918499870";
    public const string McHeart = $"<:mcheart:{McHeartId}>";

    /// <summary>`:mushroomcute:`</summary>
    public const string MushroomCuteId = "1525060374351839302";
    public const string MushroomCute = $"<:mushroomcute:{MushroomCuteId}>";

    /// <summary>`:nightmareothereye:`</summary>
    public const string NightmareOtherEyeId = "1536042805128994856";
    public const string NightmareOtherEye = $"<:nightmareothereye:{NightmareOtherEyeId}>";

    /// <summary>`:noice:`</summary>
    public const string NoiceId = "982026504982655076";
    public const string Noice = $"<:noice:{NoiceId}>";

    /// <summary>`:okpaimon:`</summary>
    public const string OkPaimonId = "885213667052900352";
    public const string OkPaimon = $"<:okpaimon:{OkPaimonId}>";

    /// <summary>`:PepeHappy:`</summary>
    public const string PepeHappyId = "904759477599883284";
    public const string PepeHappy = $"<:PepeHappy:{PepeHappyId}>";

    /// <summary>`:staring:`</summary>
    public const string StaringId = "885135626444374126";
    public const string Staring = $"<:staring:{StaringId}>";

    /// <summary>`:uwu:`</summary>
    public const string UwuId = "885135876735246346";
    public const string Uwu = $"<:uwu:{UwuId}>";

    /// <summary>`:veryangry:` (animated)</summary>
    public const string VeryAngryId = "885135712578588703";
    public const string VeryAngry = $"<a:veryangry:{VeryAngryId}>";

    /// <summary>`:1_zulana_terreur_nocturne:`</summary>
    public const string ZulanaTerreurNocturneId = "1482006937863323783";
    public const string ZulanaTerreurNocturne = $"<:1_zulana_terreur_nocturne:{ZulanaTerreurNocturneId}>";

    public const string PrincessWorryId = "1534820933351641150";
    public const string PrincessWorry = $"<:princessWorry:{PrincessWorryId}>";

    public const string PrisonerFlatId = "1534820935872548884";
    public const string PrisonerFlat = $"<:prisoner_flat:{PrisonerFlatId}>";

    public const string WitchSadId = "1536665672938160211";
    public const string WitchSad = $"<:witch_sad:{WitchSadId}>";

    public const string MeltCryId = "1508937311310839928";
    public const string MeltCry = $"<:melt_cry:{MeltCryId}>";

    public const string WitchEhehId = "1534820938112176282";
    public const string WitchEheh = $"<:witch_eheh:{WitchEhehId}>";


    
}
