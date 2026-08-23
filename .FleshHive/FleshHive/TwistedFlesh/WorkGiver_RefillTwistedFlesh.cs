using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_RefillTwistedFlesh : WorkGiver_Scanner
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

    public override ThingRequest PotentialWorkThingRequest => ThingRequest.ForDef(MeatTwistedDef);

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (pawn == null || t == null || !t.Spawned || t.def != MeatTwistedDef || t.stackCount <= 0)
        {
            return false;
        }
        if (t.IsForbidden(pawn))
        {
            JobFailReason.Is("ForbiddenLower".Translate());
            return false;
        }
        if (!pawn.CanReserveAndReach(t, PathEndMode.Touch, Danger.Deadly))
        {
            JobFailReason.Is("Reserved".Translate());
            return false;
        }
        if (!TwistedFleshUtility.HasTwistedFleshStorage(pawn))
        {
            return false;
        }
        if (!TwistedFleshUtility.NeedsRefill(pawn, forced))
        {
            return false;
        }
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!HasJobOnThing(pawn, t, forced))
        {
            return null;
        }
        int needed = TwistedFleshUtility.GetNeededAmount(pawn);
        int refillAmount = Mathf.Max(1,
            Mathf.RoundToInt(TwistedFleshUtility.GetMaxTwistedFlesh(pawn) * 0.25f));
        int count = Mathf.Min(t.stackCount, needed, refillAmount);
        if (count <= 0)
        {
            return null;
        }
        Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_RefillTwistedFlesh, t);
        job.count = count;
        return job;
    }

    private static ThingDef meatTwistedDef;
}
