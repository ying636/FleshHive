using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_FillFleshSack : WorkGiver_Scanner
{
    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        foreach (Designation designation in pawn.Map.designationManager.AllDesignations)
        {
            if (designation.def == FleshHiveDefOf.FH_MarkPrey && designation.target.Thing is Pawn target)
            {
                yield return target;
            }
        }
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn target || !IsValidMarkedPrey(target))
        {
            return false;
        }
        if (!pawn.CanReserve(target))
        {
            return false;
        }
        if (!pawn.CanReach(target, PathEndMode.Touch, Danger.Deadly))
        {
            return false;
        }
        return FindAvailableSack(pawn, target) != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (t is not Pawn target || !IsValidMarkedPrey(target))
        {
            return null;
        }
        FleshSack sack = FindAvailableSack(pawn, target);
        if (sack == null)
        {
            return null;
        }
        Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_FillFleshSack, target, sack);
        job.count = 1;
        return job;
    }

    private bool IsValidMarkedPrey(Pawn pawn)
    {
        return pawn.MapHeld != null
            && pawn.MapHeld.designationManager.DesignationOn(pawn, FleshHiveDefOf.FH_MarkPrey) != null
            && pawn.Spawned
            && !pawn.Dead
            && pawn.Downed
            && pawn.RaceProps.IsFlesh
            && !pawn.RaceProps.IsMechanoid;
    }

    private FleshSack FindAvailableSack(Pawn pawn, Pawn target)
    {
        return pawn.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshSack)
            .OfType<FleshSack>()
            .Where(sack => sack.CanAcceptMore)
            .Where(sack => pawn.CanReserveAndReach(sack, PathEndMode.InteractionCell, Danger.Deadly))
            .Where(sack => target.CanReach(sack, PathEndMode.Touch, Danger.Deadly))
            .OrderBy(sack => pawn.Position.DistanceToSquared(sack.Position))
            .FirstOrDefault();
    }
}
