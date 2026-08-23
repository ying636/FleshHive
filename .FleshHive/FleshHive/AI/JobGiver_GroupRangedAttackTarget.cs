using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_GroupRangedAttackTarget : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        Pawn? target = pawn.mindState.duty?.focus.Pawn;
        if (!IsValidFocusedTarget(pawn, target))
        {
            target = FindHostileRangedTarget(pawn, CanFallbackToMelee(pawn));
        }

        if (target == null || !target.Spawned || target.Dead || target.Map != pawn.Map)
        {
            return MakeWaitJob();
        }

        Ability? ability = FindRangedAbility(pawn, target);
        if (ability == null)
        {
            if (CanFallbackToMelee(pawn))
            {
                return MakeMeleeAttackJob(target);
            }

            return TryFindSupportPosition(pawn, target, out IntVec3 supportPosition)
                && supportPosition != pawn.Position
                ? MakeGotoJob(supportPosition)
                : MakeWaitJob();
        }

        if (!TryFindCastPosition(pawn, target, ability.verb, out IntVec3 castPosition))
        {
            return TryFindApproachPosition(pawn, target, ability.verb, out IntVec3 approachPosition)
                && approachPosition != pawn.Position
                ? MakeGotoJob(approachPosition)
                : MakeWaitJob();
        }

        if (castPosition != pawn.Position)
        {
            return MakeGotoJob(castPosition);
        }

        return ability.AICanTargetNow(target) && ability.verb.CanHitTarget(target)
            ? ability.GetJob(target, target)
            : MakeWaitJob();
    }

    private static Pawn? FindHostileRangedTarget(Pawn pawn, bool allowMeleeFallback)
    {
        IAttackTarget? target = AttackTargetFinder.BestAttackTarget(
            pawn,
            TargetScanFlags.NeedReachable | TargetScanFlags.NeedThreat | TargetScanFlags.NeedAutoTargetable,
            candidate => candidate is Pawn targetPawn
                && IsValidFocusedTarget(pawn, targetPawn)
                && targetPawn.HostileTo(pawn)
                && (allowMeleeFallback || FindRangedAbility(pawn, targetPawn) != null),
            0f,
            RangedSearchRadius,
            pawn.Position,
            RangedSearchRadius);
        return target?.Thing as Pawn;
    }

    private static bool CanFallbackToMelee(Pawn pawn)
    {
        return pawn.kindDef == FleshHiveDefOf.FH_Puffspike
            || pawn.kindDef == FleshHiveDefOf.FH_Paraspike
            || pawn.kindDef == FleshHiveDefOf.FH_Fingerspike
            || pawn.kindDef == FleshHiveDefOf.FH_Toughspike
            || pawn.kindDef == FleshHiveDefOf.FH_Trispike
            || pawn.kindDef == FleshHiveDefOf.FH_Shieldspike;
    }

    private static bool TryFindApproachPosition(Pawn pawn, Pawn target, Verb verb, out IntVec3 approachPosition)
    {
        approachPosition = IntVec3.Invalid;
        float minimumRange = Mathf.Max(verb.verbProps.minRange,
            Mathf.Min(verb.EffectiveRange * PreferredRangeFactor, MaximumPreferredRange));
        int maximumRange = Mathf.CeilToInt(verb.EffectiveRange);
        int bestDistance = int.MaxValue;

        foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, maximumRange, true))
        {
            float distance = cell.DistanceTo(target.Position);
            if (!cell.InBounds(pawn.Map)
                || !cell.Standable(pawn.Map)
                || distance < minimumRange
                || distance > verb.EffectiveRange
                || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
            {
                continue;
            }

            int distanceToPawn = pawn.Position.DistanceToSquared(cell);
            if (distanceToPawn >= bestDistance)
            {
                continue;
            }

            bestDistance = distanceToPawn;
            approachPosition = cell;
        }

        return approachPosition.IsValid;
    }

    private static bool IsValidFocusedTarget(Pawn pawn, Pawn? target)
    {
        return target != null
            && target != pawn
            && target.Spawned
            && !target.Dead
            && !target.Downed
            && target.Map == pawn.Map;
    }

    public static Ability? FindRangedAbility(Pawn pawn, Pawn target)
    {
        return pawn.abilities?.AllAbilitiesForReading.FirstOrDefault(ability =>
            ability.def.aiCanUse
            && ability.def.ai_IsOffensive
            && ability.def.targetRequired
            && ability.verb != null
            && !ability.verb.verbProps.IsMeleeAttack
            && ability.def.verbProperties.targetParams.CanTarget(target)
            && ability.AICanTargetNow(target));
    }

    public static bool TryFindCastPosition(Pawn pawn, Pawn target, Verb verb, out IntVec3 castPosition)
    {
        float minimumRange = Mathf.Max(verb.verbProps.minRange,
            Mathf.Min(verb.EffectiveRange * PreferredRangeFactor, MaximumPreferredRange));
        return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
        {
            caster = pawn,
            target = target,
            verb = verb,
            maxRangeFromTarget = verb.EffectiveRange,
            wantCoverFromTarget = verb.EffectiveRange > 5f,
            preferredCastPosition = pawn.Position,
            validator = cell => cell.DistanceTo(target.Position) >= minimumRange
        }, out castPosition);
    }

    public static bool TryFindSupportPosition(Pawn pawn, Pawn target, out IntVec3 supportPosition)
    {
        supportPosition = IntVec3.Invalid;
        int bestDistance = int.MaxValue;
        foreach (IntVec3 cell in GenRadial.RadialCellsAround(target.Position, SupportMinRange, SupportMaxRange))
        {
            if (!cell.InBounds(pawn.Map) || !cell.Standable(pawn.Map)
                || !pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly))
            {
                continue;
            }

            int distance = pawn.Position.DistanceToSquared(cell);
            if (distance >= bestDistance)
            {
                continue;
            }

            bestDistance = distance;
            supportPosition = cell;
        }

        return supportPosition.IsValid;
    }

    private static Job MakeGotoJob(IntVec3 destination)
    {
        Job job = JobMaker.MakeJob(JobDefOf.Goto, destination);
        job.expiryInterval = GotoExpiryTicks;
        job.locomotionUrgency = LocomotionUrgency.Jog;
        job.checkOverrideOnExpire = true;
        return job;
    }

    private static Job MakeWaitJob()
    {
        Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat);
        job.expiryInterval = WaitTicks;
        job.checkOverrideOnExpire = true;
        return job;
    }

    private static Job MakeMeleeAttackJob(Pawn target)
    {
        Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
        job.expiryInterval = MeleeExpiryInterval.RandomInRange;
        job.checkOverrideOnExpire = true;
        job.expireRequiresEnemiesNearby = true;
        return job;
    }

    private const int GotoExpiryTicks = 180;

    private const int WaitTicks = 90;

    private static readonly IntRange MeleeExpiryInterval = new(360, 480);

    private const float PreferredRangeFactor = 0.65f;

    private const float MaximumPreferredRange = 18f;

    private const float SupportMinRange = 10f;

    private const float SupportMaxRange = 14f;

    private const float RangedSearchRadius = 9999f;
}
