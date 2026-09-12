using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_SuppressFleshHiveActivity : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.ClosestTouch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        MapComponent_FleshHive mapComp = pawn.Map?.GetComponent<MapComponent_FleshHive>();
        if (mapComp?.ShouldAutoSuppressActivity != true)
        {
            return Enumerable.Empty<Thing>();
        }

        return pawn.Map.listerThings.AllThings
            .OfType<ThingWithComps>()
            .Where(thing => thing.TryGetComp<CompSuppressible>() != null);
    }

    public override float GetPriority(Pawn pawn, TargetInfo t)
    {
        MapComponent_FleshHive mapComp = pawn.Map?.GetComponent<MapComponent_FleshHive>();
        if (mapComp?.ShouldAutoSuppressActivity != true)
        {
            return 0f;
        }

        return mapComp?.ActivityPercent ?? 0f;
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        MapComponent_FleshHive mapComp = pawn.Map?.GetComponent<MapComponent_FleshHive>();
        if (!forced && mapComp?.ShouldAutoSuppressActivity != true)
        {
            return false;
        }

        CompSuppressible suppressible = t.TryGetComp<CompSuppressible>();
        if (suppressible == null || !suppressible.CanSuppress(pawn, forced))
        {
            return false;
        }

        if (!FleshHiveActivitySuppressionUtility.TryGetSuppressionRate(pawn, out _))
        {
            JobFailReason.Is("ZeroSuppressionRate".Translate());
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
        Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_SuppressFleshHiveActivity, t);
        job.playerForced = forced;
        return job;
    }
}
