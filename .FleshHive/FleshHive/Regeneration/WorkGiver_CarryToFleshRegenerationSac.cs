using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_CarryToFleshRegenerationSac : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        return pawn.Map.mapPawns.AllPawnsSpawned.Where(target => target.Downed
            && target.Faction == Faction.OfPlayer
            && HCFGameUtility.GetUnitComp(target)?.group?.hive is Pawn);
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        return t is Pawn target && target != pawn && target.Spawned && !target.Dead && target.Downed
            && pawn.Faction == Faction.OfPlayer && !target.IsForbidden(pawn)
            && pawn.CanReserveAndReach(target, PathEndMode.Touch, Danger.Deadly)
            && FindSac(pawn, target) != null;
    }

    public override Job? JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!HasJobOnThing(pawn, t, forced))
        {
            return null;
        }

        Thing? sac = FindSac(pawn, (Pawn)t);
        if (sac == null)
        {
            return null;
        }

        Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_CarryToFleshRegenerationSac, t, sac);
        job.count = 1;
        return job;
    }

    private Thing? FindSac(Pawn hauler, Pawn target)
    {
        return hauler.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshRegenerationSac)
            .Where(sac => !sac.IsForbidden(hauler)
                && sac.TryGetComp<CompFleshRegenerationSac>()?.CanContain(target) == true
                && hauler.CanReserveAndReach(sac, PathEndMode.Touch, Danger.Deadly)
                && hauler.Map.reachability.CanReach(target.Position, sac, PathEndMode.Touch,
                    TraverseParms.For(hauler, Danger.Deadly)))
            .OrderBy(sac => hauler.Position.DistanceToSquared(sac.Position))
            .FirstOrDefault();
    }
}
