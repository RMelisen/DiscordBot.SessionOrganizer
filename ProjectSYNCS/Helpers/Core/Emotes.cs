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
    // Frog Blushing With Big Eyes Emote
    public const string AdorableFrogId = "885135007822282762";
    public const string AdorableFrog = $"<:adorablefrog:{AdorableFrogId}>";

    // Super Happy Cat With a Small Heart Emote
    public const string CatHeartId = "982024469956669501";
    public const string CatHeart = $"<:cathearte_:{CatHeartId}>";

    // Crying Cat Emote
    public const string CryingCatId = "885135195915845653";
    public const string CryingCat = $"<:cryingcat:{CryingCatId}>";

    // Animated Dancing Blob Emote
    public const string DancingBlobId = "885209918892810330";
    public const string DancingBlob = $"<a:dancingblob:{DancingBlobId}>";

    // 10/10 Hand Emote
    public const string DixSurDixId = "885134866046419016";
    public const string DixSurDix = $"<:10sur10:{DixSurDixId}>";

    // Grey Curled Up Depressed Girl
    public const string DepressedId = "1531340935865303060";
    public const string Depressed = $"<:depressed:{DepressedId}>";

    // Happy Fumino Face Emote (Phase 1)
    public const string FuminoDepressionId = "1531341412514267146";
    public const string FuminoDepression = $"<:fuminodepression:{FuminoDepressionId}>";

    // Neutral-Happy Fumino Face Emote (Phase 2)
    public const string Fuminodepression2Id = "1531341441060573316";
    public const string Fuminodepression2 = $"<:fuminodepression2:{Fuminodepression2Id}>";

    // Sad Fumino Face Emote (Phase 3)
    public const string Fuminodepression3Id = "1531341472622842076";
    public const string Fuminodepression3 = $"<:fuminodepression3:{Fuminodepression3Id}>";

    // Goose Holding a Knife Emote
    public const string GooseKnifeId = "885214057756500019";
    public const string GooseKnife = $"<:gooseknife:{GooseKnifeId}>";

    // Animated Waving Hi Cat Emote
    public const string HiCatId = "1482305105276571774";
    public const string HiCat = $"<a:hi_cat:{HiCatId}>";

    // Hold The Pain Harold Emote
    public const string HtphId = "885137301259321405";
    public const string Htph = $"<:htph:{HtphId}>";

    // Minecraft Pixelated Heart Emote
    public const string McHeartId = "982024259918499870";
    public const string McHeart = $"<:mcheart:{McHeartId}>";

    // Cute Mushroom with Little Heart Emote
    public const string MushroomCuteId = "1525060374351839302";
    public const string MushroomCute = $"<:mushroomcute:{MushroomCuteId}>";

    // One Big Side Eye Staring Emote
    public const string NightmareOtherEyeId = "1536042805128994856";
    public const string NightmareOtherEye = $"<:nightmareothereye:{NightmareOtherEyeId}>";

    // Noice Emote
    public const string NoiceId = "982026504982655076";
    public const string Noice = $"<:noice:{NoiceId}>";

    // Paimon Holding a Knife Emote
    public const string OkPaimonId = "885213667052900352";
    public const string OkPaimon = $"<:okpaimon:{OkPaimonId}>";

    // Pepe Happy Hands Up Emote
    public const string PepeHappyId = "904759477599883284";
    public const string PepeHappy = $"<:PepeHappy:{PepeHappyId}>";

    // Staring Big Eyes Mouth Opened Face Emote
    public const string StaringId = "885135626444374126";
    public const string Staring = $"<:staring:{StaringId}>";

    // UwU Face Emote
    public const string UwuId = "885135876735246346";
    public const string Uwu = $"<:uwu:{UwuId}>";

    // Animated Very Angry Face Screaming Emote
    public const string VeryAngryId = "885135712578588703";
    public const string VeryAngry = $"<a:veryangry:{VeryAngryId}>";

    // Frowning Face Emote
    public const string ZulanaTerreurNocturneId = "1482006937863323783";
    public const string ZulanaTerreurNocturne = $"<:1_zulana_terreur_nocturne:{ZulanaTerreurNocturneId}>";

    // Worried Princess Face Emote
    public const string PrincessWorryId = "1534820933351641150";
    public const string PrincessWorry = $"<:princessWorry:{PrincessWorryId}>";

    // Jaded Face Emote
    public const string PrisonerFlatId = "1534820935872548884";
    public const string PrisonerFlat = $"<:prisoner_flat:{PrisonerFlatId}>";

    // Pleading Sad Big Eyes Face Emote
    public const string WitchSadId = "1536665672938160211";
    public const string WitchSad = $"<:witch_sad:{WitchSadId}>";

    // Melting Crying Face Emote
    public const string MeltCryId = "1508937311310839928";
    public const string MeltCry = $"<:melt_cry:{MeltCryId}>";

    // Eheh Malicious Face >:3 Emote
    public const string WitchEhehId = "1534820938112176282";
    public const string WitchEheh = $"<:witch_eheh:{WitchEhehId}>";

    // Animated Sparkle Emote
    public const string SparkleId = "1542157590606250120";
    public const string Sparkle = $"<a:sparkle:{SparkleId}>";

    // Giga Laugh Emote
    public const string GigaLaughId = "1482304998871138346";
    public const string GigaLaugh = $"<:giga_laugh:{GigaLaughId}>";

    // Animated Serious Face and Hand Agreeing Emote
    public const string HmmokId = "1524370766554992671";
    public const string Hmmok = $"<a:hmmok:{HmmokId}>";

    // DDLC Monika Holding Yes Sign Emote
    public const string MonikaYesId = "1524371041701331014";
    public const string MonikaYes = $"<:monika_yes:{MonikaYesId}>";

    // Monster Energy Drink Emote
    public const string MonsterId = "1534531762754027673";
    public const string Monster = $"<a:monster:{MonsterId}>";

    // Monster Energy Drink Emote
    public const string MonsterWhiteId = "1534531822254555189";
    public const string MonsterWhite = $"<:monster_white:{MonsterWhiteId}>";

    // Disgust-What? Face Emote
    public const string AinaniId = "1517126430004219974";
    public const string Ainani = $"<:ainani:{AinaniId}>";

    //Animated Pixelated Pink Heart Emote
    public const string Emote00heartpinkId = "1534825059967963258";
    public const string Emote00heartpink = $"<a:00heartpink:{Emote00heartpinkId}>";

    // ---- Her own faces: the mood emote pack (docs/syncs-appearance.md, "Emote pack") ----

    /// <summary>`:syncs_bored:`</summary>
    public const string SyncsBoredId = "1559544354459222076";
    public const string SyncsBored = $"<:syncs_bored:{SyncsBoredId}>";

    /// <summary>`:syncs_confused:`</summary>
    public const string SyncsConfusedId = "1559864472636751872";
    public const string SyncsConfused = $"<:syncs_confused:{SyncsConfusedId}>";

    /// <summary>`:syncs_cool:`</summary>
    public const string SyncsCoolId = "1559903984993566751";
    public const string SyncsCool = $"<:syncs_cool:{SyncsCoolId}>";

    /// <summary>`:syncs_devoted:`</summary>
    public const string SyncsDevotedId = "1559544240386605076";
    public const string SyncsDevoted = $"<:syncs_devoted:{SyncsDevotedId}>";

    /// <summary>`:syncs_disgust:`</summary>
    public const string SyncsDisgustId = "1559549516351541419";
    public const string SyncsDisgust = $"<:syncs_disgust:{SyncsDisgustId}>";

    /// <summary>`:syncs_flustered:`</summary>
    public const string SyncsFlusteredId = "1559544184422277201";
    public const string SyncsFlustered = $"<:syncs_flustered:{SyncsFlusteredId}>";

    /// <summary>`:syncs_formal:`</summary>
    public const string SyncsFormalId = "1559864551468699770";
    public const string SyncsFormal = $"<:syncs_formal:{SyncsFormalId}>";

    /// <summary>`:syncs_full_love:`</summary>
    public const string SyncsFullLoveId = "1559549795226493028";
    public const string SyncsFullLove = $"<:syncs_full_love:{SyncsFullLoveId}>";

    /// <summary>`:syncs_glitch:`</summary>
    public const string SyncsGlitchId = "1559544081112105041";
    public const string SyncsGlitch = $"<:syncs_glitch:{SyncsGlitchId}>";

    /// <summary>`:syncs_grumpy:`</summary>
    public const string SyncsGrumpyId = "1559864597215846460";
    public const string SyncsGrumpy = $"<:syncs_grumpy:{SyncsGrumpyId}>";

    /// <summary>`:syncs_happy:`</summary>
    public const string SyncsHappyId = "1559543923892953148";
    public const string SyncsHappy = $"<:syncs_happy:{SyncsHappyId}>";

    /// <summary>`:syncs_hurt:`</summary>
    public const string SyncsHurtId = "1559543844410757300";
    public const string SyncsHurt = $"<:syncs_hurt:{SyncsHurtId}>";

    /// <summary>`:syncs_horrified:`</summary>
    public const string SyncsHorrifiedId = "1559864702731817022";
    public const string SyncsHorrified = $"<:syncs_horrified:{SyncsHorrifiedId}>";

    /// <summary>`:syncs_laugh:`</summary>
    public const string SyncsLaughId = "1559543741604171857";
    public const string SyncsLaugh = $"<:syncs_laugh:{SyncsLaughId}>";

    /// <summary>`:syncs_love:`</summary>
    public const string SyncsLoveId = "1559549644617289728";
    public const string SyncsLove = $"<:syncs_love:{SyncsLoveId}>";

    /// <summary>`:syncs_outraged:`</summary>
    public const string SyncsOutragedId = "1559543627066384434";
    public const string SyncsOutraged = $"<:syncs_outraged:{SyncsOutragedId}>";

    /// <summary>`:syncs_overheat:`</summary>
    public const string SyncsOverheatId = "1559549910456602734";
    public const string SyncsOverheat = $"<:syncs_overheat:{SyncsOverheatId}>";

    /// <summary>`:syncs_reboot:`</summary>
    public const string SyncsRebootId = "1559549972410798260";
    public const string SyncsReboot = $"<:syncs_reboot:{SyncsRebootId}>";

    /// <summary>`:syncs_sad:`</summary>
    public const string SyncsSadId = "1559864780393545790";
    public const string SyncsSad = $"<:syncs_sad:{SyncsSadId}>";

    /// <summary>`:syncs_slay:`</summary>
    public const string SyncsSlayId = "1560172582467010580";
    public const string SyncsSlay = $"<:syncs_slay:{SyncsSlayId}>";

    /// <summary>`:syncs_smug:`</summary>
    public const string SyncsSmugId = "1559543422128488478";
    public const string SyncsSmug = $"<:syncs_smug:{SyncsSmugId}>";

    /// <summary>`:syncs_sulk:`</summary>
    public const string SyncsSulkId = "1559543350749691965";
    public const string SyncsSulk = $"<:syncs_sulk:{SyncsSulkId}>";

    /// <summary>`:syncs_terror:`</summary>
    public const string SyncsTerrorId = "1559543267136245861";
    public const string SyncsTerror = $"<:syncs_terror:{SyncsTerrorId}>";
}
