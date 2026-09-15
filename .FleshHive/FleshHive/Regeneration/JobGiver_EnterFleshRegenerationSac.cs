using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobGiver_EnterFleshRegenerationSac : ThinkNode_JobGiver
{
    protected override Job? TryGiveJob(Pawn pawn)
    {
        if (!pawn.Spawned || pawn.Dead || pawn.Downed || pawn.Drafted || pawn.InMentalState
            || pawn.Faction != Faction.OfPlayer || pawn.IsFighting()
            || HCFGameUtility.GetUnitComp(pawn)?.group?.hive is not Pawn node
            || node == pawn || !HCFGameUtility.IsNodeUnit(node))
        {
            return null;
        }

        Thing sac = GenClosest.ClosestThingReachable(
            pawn.Position, pawn.Map, ThingRequest.ForDef(FleshHiveDefOf.FH_FleshRegenerationSac),
            PathEndMode.Touch, TraverseParms.For(pawn, Danger.Some), 9999f,
            thing => !thing.IsForbidden(pawn) && pawn.CanReserve(thing)
                && thing.TryGetComp<CompFleshRegenerationSac>()?.CanContain(pawn) == true);
        return sac == null ? null : JobMaker.MakeJob(FleshHiveDefOf.FH_Job_EnterFleshRegenerationSac, sac);
    }
}
