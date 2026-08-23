using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_GroupHuntGather : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        Pawn? prey = pawn.mindState.duty?.focus.Pawn;
        if (prey == null || !prey.Spawned || prey.Dead || prey.Map != pawn.Map)
        {
            return JobMaker.MakeJob(JobDefOf.Wait_Wander);
        }

        if (pawn.TryGetComp<HiveCreatureFramework.UnitComp>()?.Props.overrideDuty_Attack
            == FleshHiveDefOf.FH_Attack_Ranged)
        {
            Ability? ability = JobGiver_GroupRangedAttackTarget.FindRangedAbility(pawn, prey);
            bool foundPosition = ability != null
                ? JobGiver_GroupRangedAttackTarget.TryFindCastPosition(pawn, prey, ability.verb,
                    out IntVec3 shootingPosition)
                : JobGiver_GroupRangedAttackTarget.TryFindSupportPosition(pawn, prey, out shootingPosition);
            if (!foundPosition)
            {
                return JobMaker.MakeJob(JobDefOf.Wait_Wander);
            }

            if (shootingPosition == pawn.Position)
            {
                Job waitCombat = JobMaker.MakeJob(JobDefOf.Wait_Combat);
                waitCombat.expiryInterval = WaitTicks;
                return waitCombat;
            }

            Job rangedJob = JobMaker.MakeJob(JobDefOf.Goto, shootingPosition);
            rangedJob.expiryInterval = GotoExpiryTicks;
            rangedJob.locomotionUrgency = LocomotionUrgency.Jog;
            rangedJob.checkOverrideOnExpire = true;
            return rangedJob;
        }

        if (pawn.Position.InHorDistOf(prey.Position, LordToil_GroupHunt.GatherRadius))
        {
            Job wait = JobMaker.MakeJob(JobDefOf.Wait_Wander);
            wait.expiryInterval = WaitTicks;
            return wait;
        }

        IntVec3 destination = CellFinder.RandomClosewalkCellNear(prey.Position, pawn.Map, DestinationRadius);
        if (!destination.IsValid || !pawn.CanReach(destination, PathEndMode.OnCell, Danger.Deadly))
        {
            return null;
        }

        Job job = JobMaker.MakeJob(JobDefOf.Goto, destination);
        job.expiryInterval = GotoExpiryTicks;
        job.locomotionUrgency = LocomotionUrgency.Jog;
        job.checkOverrideOnExpire = true;
        return job;
    }

    private const int DestinationRadius = 7;

    private const int GotoExpiryTicks = 180;

    private const int WaitTicks = 90;
}
