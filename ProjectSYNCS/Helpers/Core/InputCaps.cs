namespace ProjectSYNCS.Helpers;

/// <summary>
/// How long a piece of user-supplied text may be, per input, chosen from where that text
/// ends up rather than from taste.
/// </summary>
/// <remarks>
/// <para><b>These exist because the option limits are far larger than the render
/// limits.</b> A slash-command string option accepts up to 6000 characters unless
/// <c>[MaxLength]</c> says otherwise, and a <c>[ModalTextInput]</c> defaults to 4000 —
/// while an embed title holds 256, an embed description 4096, and a whole message 2000.
/// Exceeding one throws inside <c>Build()</c> at *send* time with nothing in the logs
/// naming the length, which is the failure that killed <c>/help</c> for six commits.</para>
/// <para><b>Capping at the option is the point.</b> Discord then refuses the input in the
/// client, so the bot never has to word a refusal and nothing over-long ever reaches the
/// database — where it would otherwise break every later re-render of the card, not just
/// the first send.</para>
/// <para>Constants rather than literals because <see cref="Title"/> is needed at five
/// sites that must agree: the three modal DTOs and the hand-built
/// <c>ModalBuilder</c>s that pre-fill the same modals.</para>
/// </remarks>
public static class InputCaps
{
    /// <summary>
    /// A session, poll or vote title. Lands in an embed title (256) behind a phase
    /// prefix — the longest is "🔴 EN COURS — Activité : " — so this leaves ample room.
    /// </summary>
    public const int Title = 150;

    /// <summary>A giveaway prize. Lands in an embed title (256) behind "🎉 ".</summary>
    public const int Prize = 200;

    /// <summary>
    /// A giveaway description. The embed description itself allows 4096, but the whole
    /// embed allows 6000 counting every field — and this card also carries the end time
    /// and up to 20 entrant mentions — so it is held well below its own limit.
    /// </summary>
    public const int Description = 1000;

    /// <summary>
    /// A vote's options typed one per line (/vote create): ten options of up to 80 characters,
    /// with their line breaks and list markers.
    /// </summary>
    public const int VoteOptions = 900;

    /// <summary>A date poll's slots typed in one line (« ven 21h, sam 21h, dim 18h »): ten of them, generously.</summary>
    public const int PollSlots = 300;

    /// <summary>A typed session date (« samedi 21h », « 25/12 20:30 »).</summary>
    public const int When = 60;

    /// <summary>
    /// A <c>/yesno</c> question, echoed back above the verdict in a 2000-char message.
    /// </summary>
    public const int Question = 400;

    /// <summary>
    /// A Plynling's name — Discord's own nickname limit. Shown on the card heading, in her
    /// lines and in public announcements, so it is capped at the option like every title.
    /// </summary>
    public const int PlynlingName = 32;

    /// <summary>
    /// A passion taught to a Plynling (/plynling passion). Shown on the card and inside visit
    /// story lines, so it stays short enough to read as a phrase, not a paragraph.
    /// </summary>
    public const int Passion = 40;

    /// <summary>
    /// A <c>/admin mute|unmute</c> reason. Goes to the audit log, which holds 512, behind
    /// "Par {username} : ".
    /// </summary>
    public const int MuteReason = 400;

    /// <summary>
    /// A <c>/admin warn</c> reason. Quoted in the DM under her line (2000 per message), and
    /// listed in full on <c>/admin warnings</c>, which shows up to
    /// <c>AdminCards.MaxWarningsShown</c> of them in one embed description (4096).
    /// </summary>
    public const int WarnReason = 400;

    /// <summary>
    /// A role name (<c>/config plynling-role create</c>) — Discord's own limit for role names.
    /// </summary>
    public const int RoleName = 100;

    /// <summary>
    /// What <c>/rate</c> rates. Quoted above her verdict, in a 2000-char message.
    /// </summary>
    public const int Rate = 200;

    /// <summary>
    /// A <c>/remindme</c> text. Quoted under her line in the DM (2000 per message), and shown cut
    /// short as a select option on <c>/reminders</c>.
    /// </summary>
    public const int Reminder = 400;

    /// <summary>
    /// <c>/remindme</c>'s « quand » (« demain 9h », « dans 2 heures »): anything longer isn't a
    /// time, and the parser's patterns stay cheap on short input.
    /// </summary>
    public const int ReminderWhen = 40;

    /// <summary>
    /// <c>/tonight</c>'s game. Lands in the card's embed title and, if the call becomes a session,
    /// in the session's title — so it shares <see cref="Title"/>'s budget.
    /// </summary>
    public const int TonightGame = Title;

    /// <summary>
    /// Each side of a <c>/ship</c>. Both are quoted on one line above her verdict.
    /// </summary>
    public const int Ship = 100;

    /// <summary>
    /// What <c>/blame</c> is about (« la défaite », « le ping »). Quoted above her verdict.
    /// </summary>
    public const int Blame = 100;

    /// <summary>
    /// Each option of a custom <c>/wouldyourather</c>. Shown in the card's embed description,
    /// both of them, and again in her own pick on the result.
    /// </summary>
    public const int WyrOption = 80;

    /// <summary>
    /// A <c>/confess</c> text. Lands in an embed description (4096), held well below it so a
    /// confession stays something people read.
    /// </summary>
    public const int Confession = 1000;
}
