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
        foreach (Designation designation in pawn.Map.designationManager.SpawnedDesignationsOfDef(FleshHiveDefOf.FH_MarkPrey).ToList())
        {
            if (designation.target.Thing is Thing target && IsValidMarkedPrey(target))
            {
                yield return target;
            }
        }
    }

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!IsValidMarkedPrey(t))
        {
            return false;
        }
        if (!pawn.CanReserve(t))
        {
            return false;
        }
        if (!pawn.CanReach(t, PathEndMode.Touch, Danger.Deadly))
        {
            return false;
        }
        return FindAvailableSack(pawn, t) != null;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        if (!IsValidMarkedPrey(t))
        {
            return null;
        }
        FleshSack sack = FindAvailableSack(pawn, t);
        if (sack == null)
        {
            return null;
        }
        Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_FillFleshSack, t, sack);
        job.count = 1;
        return job;
    }

    private bool IsValidMarkedPrey(Thing target)
    {
        if (target?.Spawned != true)
        {
            return false;
        }
        Designation designation = target.Map.designationManager.DesignationOn(target, FleshHiveDefOf.FH_MarkPrey);
        if (designation == null)
        {
            return false;
        }
        if (target is Corpse corpse)
        {
            if (corpse.GetRotStage() != RotStage.Fresh)
            {
                target.Map.designationManager.RemoveDesignation(designation);
                return false;
            }
            return corpse.InnerPawn.RaceProps.IsFlesh && !corpse.InnerPawn.RaceProps.IsMechanoid;
        }
        return target is Pawn pawn && !pawn.Dead && pawn.Downed
            && pawn.RaceProps.IsFlesh && !pawn.RaceProps.IsMechanoid;
    }

    private FleshSack FindAvailableSack(Pawn pawn, Thing target)
    {
        return pawn.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshSack)
            .OfType<FleshSack>()
            .Where(sack => sack.CanAcceptMore)
            .Where(sack => pawn.CanReserveAndReach(sack, PathEndMode.InteractionCell, Danger.Deadly))
            .Where(sack => pawn.Map.reachability.CanReach(target.Position, sack, PathEndMode.Touch,
                TraverseParms.For(pawn, Danger.Deadly)))
            .OrderBy(sack => pawn.Position.DistanceToSquared(sack.Position))
            .FirstOrDefault();
    }
}
