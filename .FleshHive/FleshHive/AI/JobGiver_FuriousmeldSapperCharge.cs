using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_FuriousmeldSapperCharge : JobGiver_AISapper
{
    public override ThinkNode DeepCopy(bool resolve = true)
    {
        JobGiver_FuriousmeldSapperCharge copy = (JobGiver_FuriousmeldSapperCharge)base.DeepCopy(resolve);
        copy.ability = ability;
        return copy;
    }

    protected override Job TryGiveJob(Pawn pawn)
    {
        Job sapperJob = base.TryGiveJob(pawn);
        if (!IsDestroyBuildingJob(sapperJob))
        {
            return sapperJob;
        }

        Ability charge = pawn.abilities?.GetAbility(ability);
        if (charge == null || charge.OnCooldown)
        {
            return sapperJob;
        }

        LocalTargetInfo target = new(sapperJob.targetA.Cell);
        if (!charge.CanApplyOn(target))
        {
            return sapperJob;
        }

        if (!charge.verb.CanHitTarget(target))
        {
            if (TryFindChargePosition(pawn, charge, target, out IntVec3 destination))
            {
                return JobMaker.MakeJob(JobDefOf.Goto, destination, 60, checkOverrideOnExpiry: true);
            }

            return sapperJob;
        }

        return charge.GetJob(target, target);
    }

    private static bool IsDestroyBuildingJob(Job job)
    {
        if (job?.targetA.Thing is not Building)
        {
            return false;
        }

        return job.def == JobDefOf.Mine
               || job.def == JobDefOf.AttackMelee
               || job.def == JobDefOf.UseVerbOnThing;
    }

    private static bool TryFindChargePosition(Pawn pawn, Ability charge, LocalTargetInfo target, out IntVec3 destination)
    {
        return CastPositionFinder.TryFindCastPosition(new CastPositionRequest
        {
            caster = pawn,
            target = target.Cell.GetEdifice(pawn.Map),
            verb = charge.verb,
            maxRangeFromTarget = charge.verb.EffectiveRange,
            wantCoverFromTarget = false,
            preferredCastPosition = pawn.Position
        }, out destination) && destination != pawn.Position;
    }

    public AbilityDef ability;
}
