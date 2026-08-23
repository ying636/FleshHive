using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_GroupHuntExecute : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        Pawn? prey = pawn.mindState.duty?.focus.Pawn;
        if (prey == null || !prey.Spawned || prey.Dead || !prey.Downed || prey.Map != pawn.Map)
        {
            return MakeWaitJob();
        }

        if (!pawn.CanReserveAndReach(prey, PathEndMode.Touch, Danger.Deadly))
        {
            return MakeWaitJob();
        }

        return JobMaker.MakeJob(FleshHiveDefOf.FH_Job_HuntExecution, prey);
    }

    private static Job MakeWaitJob()
    {
        Job job = JobMaker.MakeJob(JobDefOf.Wait_Combat);
        job.expiryInterval = WaitTicks;
        job.checkOverrideOnExpire = true;
        return job;
    }

    private const int WaitTicks = 60;
}
