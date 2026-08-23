using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_InfectHarbingerTree : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.designationManager.SpawnedDesignationsOfDef(FleshHiveDefOf.FH_InfectHarbingerTree)
            .Select(designation => designation.target.Thing)
            .Where(thing => thing != null && thing.def == ThingDefOf.Plant_TreeHarbinger);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!IsValidTarget(pawn, t))
        {
            return false;
        }
        return FindTwistedMeat(pawn, t.Position) != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!IsValidTarget(pawn, t))
        {
            return null;
        }

        Thing meat = FindTwistedMeat(pawn, t.Position);
        if (meat == null)
        {
            return null;
        }

        Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_InfectHarbingerTree, t, meat);
        job.count = TwistedMeatCost;
        return job;
    }

    private bool IsValidTarget(Pawn pawn, Thing thing)
    {
        if (thing == null || thing.def != ThingDefOf.Plant_TreeHarbinger)
        {
            return false;
        }
        if (thing.Map.designationManager.DesignationOn(thing, FleshHiveDefOf.FH_InfectHarbingerTree) == null)
        {
            return false;
        }
        return pawn.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Deadly);
    }

    private Thing FindTwistedMeat(Pawn pawn, IntVec3 targetCell)
    {
        return GenClosest.ClosestThingReachable(
            targetCell,
            pawn.Map,
            ThingRequest.ForDef(FleshHiveDefOf.Meat_Twisted),
            PathEndMode.ClosestTouch,
            TraverseParms.For(pawn),
            9999f,
            thing => thing.stackCount >= TwistedMeatCost && pawn.CanReserve(thing));
    }

    private const int TwistedMeatCost = 25;
}
