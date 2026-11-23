using ProjectSYNCS.Models;

namespace ProjectSYNCS.Helpers;

/// <summary>
/// The faces an event's story shows on its last page, the outcome: read from what the outcome did,
/// then overridden by a face tag opening a story outcome ([sad], [A:… B:…]). Every other page stays
/// calm. Pure, so the checks can pin it.
/// <para>
/// {A}: happy when only good things came of it (stress relief, a pleasant modifier, a need lifted,
/// cailloux, an item, a mark, affinity gained, a closer bond, a couple made), sad when only bad ones
/// (a failed roll, stress gained, an unpleasant modifier, affinity lost, a bond cooled, a couple that
/// came too late, a keepsake that hurts) — angry instead between rivals or enemies — and calm when
/// it was mixed or neither. A lesson alone is neither. {B}: the bond and the affinity it moved, a
/// heartbreak; calm otherwise.
/// </para>
/// </summary>
public static class PlynlingEventFaces
{
    public static (PlynlingMood Self, PlynlingMood Target) Outcome(PlynlingEventInstance inst, EventDef def, EventOption option)
    {
        var success = inst.ChallengeSucceeded ?? true;
        var effects = (success ? option.OnSuccess : option.OnFailure)
            .Where(e => PlynlingEventEngine.Applies(e, def, inst.DecidedAlone)).ToList();
        var closer = inst.BondBefore is { } before && inst.BondAfter is { } after
            ? Math.Sign(PlynlingBonds.Closeness(before) - PlynlingBonds.Closeness(after)) : 0;   // Closeness: 0 is closest
        var hostile = (inst.BondAfter ?? inst.BondBefore) is { } bond && PlynlingBonds.IsHostile(bond);
        var affinity = effects.OfType<AffinityShift>().Sum(a => a.Delta);
        var lateCouple = effects.OfType<Couple>().Any() && inst.BondAfter != PlynlingBond.Lovers;

        var good = inst.StressDelta < 0
                   || closer > 0
                   || affinity > 0
                   || effects.Any(e => e switch
                   {
                       ApplyModifier m => PlynlingModifiers.ByKey(m.Key) is { Negative: false },
                       LiftNeed or GiveCailloux or GiveItem => true,
                       GiveMark mark => !mark.Hurts,
                       _ => false,
                   });
        var bad = inst.ChallengeSucceeded == false
                  || inst.StressDelta > 0
                  || closer < 0
                  || affinity < 0
                  || lateCouple
                  || effects.Any(e => e switch
                  {
                      ApplyModifier m => PlynlingModifiers.ByKey(m.Key) is { Negative: true },
                      GiveMark mark => mark.Hurts,
                      _ => false,
                  });
        var down = hostile ? PlynlingMood.Angry : PlynlingMood.Sad;
        var self = good == bad ? PlynlingMood.Content : good ? PlynlingMood.Happy : down;

        var target = effects.OfType<Heartbreak>().Any() || lateCouple ? PlynlingMood.Sad
            : closer > 0 || (closer == 0 && affinity > 0) ? PlynlingMood.Happy
            : closer < 0 || affinity < 0 ? down
            : PlynlingMood.Content;

        // A face tag opening the outcome wins: [face] is {A}'s, [A:… B:…] names each.
        if (Tag(def, option, success) is { } tag)
        {
            self = tag.Both ?? tag.A ?? self;
            target = tag.B ?? target;
        }
        return (self, target);
    }

    // The face tag of the outcome that was told — the failure's when it failed, else the success's
    // (as PlynlingEventStory.OutcomeProse picks it). C# events have none.
    private static FaceTag? Tag(EventDef def, EventOption option, bool success)
    {
        if (def.Story is null || option.Source is not { } src) return null;
        var outcome = success ? src.Outcome ?? src.Success : src.Failure ?? src.Success ?? src.Outcome;
        return outcome?.Face;
    }
}
