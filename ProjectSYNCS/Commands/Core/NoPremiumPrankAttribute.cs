namespace ProjectSYNCS.Commands;

/// <summary>
/// Keeps a slash command (or a whole module) out of the premium easter egg (PremiumPrankService).
/// Required on every command that can answer with a modal (a modal must be the first answer, and
/// the paywall already is), that answers through <c>Context.Interaction</c> instead of the module's
/// own methods (SyncsModuleBase can't reroute those), or whose use must not show publicly (/confess).
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class NoPremiumPrankAttribute : Attribute;
