using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_AIFleshJumpToJobTarget : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        Ability? jumpAbility = pawn.abilities?.GetAbility(ability);
        if (jumpAbility == null || !jumpAbility.CanCast)
        {
            return null!;
        }

        Job? currentJob = pawn.CurJob;
        if (currentJob == null || currentJob.def == ability.jobDef)
        {
            return null!;
        }

        LocalTargetInfo target = currentJob.GetTarget(targetIndex);
        if (!target.IsValid)
        {
            return null!;
        }

        IntVec3 destination = target.Cell;
        float distance = pawn.Position.DistanceTo(destination);
        VerbProperties verbProperties = jumpAbility.verb.verbProps;
        if (distance < verbProperties.minRange
            || distance > jumpAbility.verb.EffectiveRange
            || !GenSight.LineOfSight(pawn.Position, destination, pawn.Map))
        {
            return null!;
        }

        if (target.HasThing && !RCellFinder.TryFindGoodAdjacentSpotToTouch(pawn, target.Thing, out destination))
        {
            return null!;
        }

        LocalTargetInfo jumpTarget = destination;
        return jumpAbility.verb.ValidateTarget(jumpTarget, false)
            ? jumpAbility.GetJob(jumpTarget, jumpTarget)
            : null!;
    }

    public AbilityDef ability = null!;

    public TargetIndex targetIndex = TargetIndex.A;
}
