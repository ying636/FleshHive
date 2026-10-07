using System;
using System.Reflection;
using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_FleshAbilityFight : JobGiver_UnitAbilityFight
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        Job? job = base.TryGiveJob(pawn);
        if (job?.ability == null
            || job.ability.verb.EffectiveRange <= 0f
            || job.ability.verb.TryFindShootLineFromTo(pawn.Position, job.targetA, out _))
        {
            return job;
        }

        if (!TryFindShootingPosition(pawn, out IntVec3 destination, job.verbToUse)
            || destination == pawn.Position)
        {
            return null;
        }

        Job moveJob = JobMaker.MakeJob(JobDefOf.Goto, destination);
        moveJob.expiryInterval = ExpiryInterval_Ability.RandomInRange;
        moveJob.checkOverrideOnExpire = true;
        return moveJob;
    }

    protected override bool TryFindShootingPosition(Pawn pawn, out IntVec3 dest, Verb? verbToUse = null)
    {
        dest = IntVec3.Invalid;
        Thing? target = pawn.mindState.enemyTarget;
        AbilityDef abilityDef = (AbilityDef)AbilityField.GetValue(this);
        Verb? verb = verbToUse ?? pawn.abilities?.GetAbility(abilityDef)?.verb;
        if (target == null || verb == null)
        {
            return false;
        }

        return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
        {
            caster = pawn,
            target = target,
            verb = verb,
            maxRangeFromTarget = verb.EffectiveRange,
            wantCoverFromTarget = false,
            preferredCastPosition = pawn.Position,
            validator = cell => verb.TryFindShootLineFromTo(cell, target, out _)
        }, out dest);
    }

    private static readonly FieldInfo AbilityField = typeof(JobGiver_AIAbilityFight)
        .GetField("ability", BindingFlags.Instance | BindingFlags.NonPublic)
        ?? throw new MissingFieldException(typeof(JobGiver_AIAbilityFight).FullName, "ability");
}
