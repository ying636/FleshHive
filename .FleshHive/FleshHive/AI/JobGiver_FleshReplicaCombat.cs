using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_FleshReplicaCombat : JobGiver_AIFightEnemies
{
    public override ThinkNode DeepCopy(bool resolve = true)
    {
        JobGiver_FleshReplicaCombat copy = (JobGiver_FleshReplicaCombat)base.DeepCopy(resolve);
        copy.respectDutyArea = respectDutyArea;
        return copy;
    }

    protected override Job? TryGiveJob(Pawn pawn)
    {
        Job? job = base.TryGiveJob(pawn);
        Thing? target = pawn.mindState.duty?.focus.Thing;
        if (job == null || !JobGiver_UnitCombat.IsValidTarget(pawn, target)
            || pawn.mindState.enemyTarget != target)
        {
            return job;
        }

        if (job.def == JobDefOf.AttackMelee)
        {
            job.expireRequiresEnemiesNearby = false;
        }
        else if (job.def == JobDefOf.Wait_Combat)
        {
            Verb? verb = pawn.TryGetAttackVerb(target, !pawn.IsColonist && !pawn.IsColonySubhuman);
            if (verb != null && !verb.verbProps.IsMeleeAttack && verb.CanHitTarget(target))
            {
                job = JobMaker.MakeJob(JobDefOf.AttackStatic, target);
                job.verbToUse = verb;
                job.maxNumStaticAttacks = 1;
                job.endIfCantShootTargetFromCurPos = true;
                job.expireRequiresEnemiesNearby = false;
            }
        }

        return job;
    }

    protected override void UpdateEnemyTarget(Pawn pawn)
    {
        Thing? target = pawn.mindState.duty?.focus.Thing;
        if (JobGiver_UnitCombat.IsValidTarget(pawn, target))
        {
            pawn.mindState.enemyTarget = target;
            return;
        }

        base.UpdateEnemyTarget(pawn);
    }

    protected override float GetFlagRadius(Pawn pawn)
    {
        return respectDutyArea
            ? pawn.mindState.duty.radius >= 0f ? pawn.mindState.duty.radius : DefaultDefendRadius
            : base.GetFlagRadius(pawn);
    }

    protected override IntVec3 GetFlagPosition(Pawn pawn)
    {
        return respectDutyArea ? pawn.mindState.duty.focus.Cell : base.GetFlagPosition(pawn);
    }

    protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb? verbToUse = null)
    {
        if (!respectDutyArea)
        {
            return base.TryFindShootingPosition(pawn, out dest, verbToUse);
        }

        Thing target = pawn.mindState.enemyTarget;
        Verb? verb = verbToUse ?? pawn.TryGetAttackVerb(target, !pawn.IsColonist && !pawn.IsColonySubhuman);
        if (verb == null)
        {
            dest = IntVec3.Invalid;
            return false;
        }

        return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
        {
            caster = pawn,
            target = target,
            verb = verb,
            maxRangeFromTarget = verb.EffectiveRange,
            locus = GetFlagPosition(pawn),
            maxRangeFromLocus = GetFlagRadius(pawn),
            wantCoverFromTarget = verb.EffectiveRange > 5f
        }, out dest);
    }

    public bool respectDutyArea;

    private const float DefaultDefendRadius = 28f;
}
