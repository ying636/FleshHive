using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_AIFleshJumpToJobTarget : ThinkNode_JobGiver
{
    public override ThinkNode DeepCopy(bool resolve = true)
    {
        JobGiver_AIFleshJumpToJobTarget copy = (JobGiver_AIFleshJumpToJobTarget)base.DeepCopy(resolve);
        copy.ability = ability;
        copy.targetIndex = targetIndex;
        return copy;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        Ability? jumpAbility = pawn.abilities?.GetAbility(ability);
        if (jumpAbility == null || !jumpAbility.CanCast)
        {
            return null!;
        }

        Job? currentJob = pawn.CurJob;
        if (currentJob != null && currentJob.def == ability.jobDef)
        {
            return null!;
        }

        Thing? target = currentJob?.def == JobDefOf.AttackMelee
            ? currentJob.GetTarget(targetIndex).Thing
            : pawn.mindState?.enemyTarget ?? pawn.mindState?.duty?.focus.Thing;
        if (target == null
            || target == pawn
            || !target.Spawned
            || target.Destroyed
            || target.Map != pawn.Map
            || !target.HostileTo(pawn)
            || target is not IAttackTarget attackTarget
            || attackTarget.ThreatDisabled(pawn)
            || !AttackTargetFinder.IsAutoTargetable(attackTarget))
        {
            return null!;
        }

        if (!RCellFinder.TryFindGoodAdjacentSpotToTouch(pawn, target, out IntVec3 destination))
        {
            return null!;
        }

        float distance = pawn.Position.DistanceTo(destination);
        VerbProperties verbProperties = jumpAbility.verb.verbProps;
        if (destination == pawn.Position
            || distance < verbProperties.minRange
            || distance > jumpAbility.verb.EffectiveRange
            || !GenSight.LineOfSight(pawn.Position, destination, pawn.Map))
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
