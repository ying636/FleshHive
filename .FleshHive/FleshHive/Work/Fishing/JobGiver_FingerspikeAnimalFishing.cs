using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_FingerspikeAnimalFishing : ThinkNode_JobGiver
{
    protected override Job TryGiveJob(Pawn pawn)
    {
        return JobGiver_GetFood.TryFindFishJob(pawn);
    }
}
