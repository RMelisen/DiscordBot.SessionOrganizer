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
    /// A <c>/yesno</c> question, echoed back above the verdict in a 2000-char message.
    /// </summary>
    public const int Question = 400;

    /// <summary>
    /// A Plynling's name — Discord's own nickname limit. Shown on the card heading, in her
    /// lines and in public announcements, so it is capped at the option like every title.
    /// </summary>
    public const int PlynlingName = 32;
}
