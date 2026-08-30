using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_FingerspikeAnimalFishing : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        Job? job = JobGiver_GetFood.TryFindFishJob(pawn);
        if (job == null)
        {
            return null;
        }

        job.def = FleshHiveDefOf.FH_FishAnimal;
        return job;
    }
}
