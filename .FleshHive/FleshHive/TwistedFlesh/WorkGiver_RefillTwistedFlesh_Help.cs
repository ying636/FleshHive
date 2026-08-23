using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_RefillTwistedFlesh_Help : WorkGiver_Scanner
{
    private static ThingDef MeatTwistedDef
    {
        get
        {
            if (meatTwistedDef == null)
            {
                meatTwistedDef = ThingDef.Named("Meat_Twisted");
            }
            return meatTwistedDef;
        }
    }

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForGroup(ThingRequestGroup.Pawn);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn == null || t is not Pawn target || target == pawn || !target.Spawned || target.Dead || target.Downed)
        {
            return false;
        }
        if (target.Faction != pawn.Faction || target.IsForbidden(pawn))
        {
            return false;
        }
        if (!TwistedFleshUtility.HasTwistedFleshStorage(target)
            || !TwistedFleshUtility.NeedsRefill(target, forced))
        {
            return false;
        }
        if (!pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly))
        {
            return false;
        }
        return FindTwistedMeat(pawn) != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!HasJobOnThing(pawn, t, forced))
        {
            return null;
        }

        Pawn targetPawn = t as Pawn;
        Thing twistedMeat = FindTwistedMeat(pawn);
        if (targetPawn == null || twistedMeat == null)
        {
            return null;
        }

        int needed = TwistedFleshUtility.GetNeededAmount(targetPawn);
        int refillAmount = Mathf.Max(1,
            Mathf.RoundToInt(TwistedFleshUtility.GetMaxTwistedFlesh(targetPawn) * 0.25f));
        int count = Mathf.Min(twistedMeat.stackCount, needed, refillAmount);
        if (count <= 0)
        {
            return null;
        }

        Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_RefillTwistedFlesh_Help, twistedMeat, targetPawn);
        job.count = count;
        return job;
    }

    private Thing FindTwistedMeat(Pawn pawn)
    {
        return GenClosest.ClosestThingReachable(
            pawn.Position,
            pawn.Map,
            ThingRequest.ForDef(MeatTwistedDef),
            PathEndMode.Touch,
            TraverseParms.For(pawn, Danger.Deadly),
            9999f,
            thing => thing.stackCount > 0 && !thing.IsForbidden(pawn) && pawn.CanReserve(thing));
    }

    private static ThingDef meatTwistedDef;
}
