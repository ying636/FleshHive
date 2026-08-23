using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class WorkGiver_FillParasitePod : WorkGiver_Scanner
{
    public override IEnumerable<Thing> PotentialWorkThingsGlobal(Pawn pawn)
    {
        var things = pawn.Map.listerThings.ThingsOfDef(FleshHiveDefOf.FH_FleshParasiteVat);
        for (int i = 0; i < things.Count; i++)
        {
            var b = things[i] as FleshParasitePod;
            if (b != null && b.curQuest != null)
            {
                yield return b;
            }
        }
    }

    public override PathEndMode PathEndMode => PathEndMode.Touch;

    public override bool HasJobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var pod = t as FleshParasitePod;
        if (pod == null || pod.curQuest == null)
        {
            return false;
        }
        if (!pawn.CanReserve(t))
        {
            return false;
        }
        var q = pod.curQuest;
        Pawn toCarry = null;
        if (q.target != null && !pod.target.Contains(q.target) && q.target.Spawned)
        {
            toCarry = q.target;
        }
        else if (q.flesh != null && !pod.flesh.Contains(q.flesh) && q.flesh.Spawned)
        {
            toCarry = q.flesh;
        }
        if (toCarry == null)
        {
            return false;
        }
        
        if (toCarry == pawn)
        {
            if (!pawn.Spawned || pawn.Downed)
            {
                return false;
            }
            if (!pawn.CanReach(t, PathEndMode.InteractionCell, Danger.Deadly))
            {
                return false;
            }
            return true;
        }
        
        if (!pawn.CanReserveAndReach(toCarry,PathEndMode.Touch,Danger.Deadly))
        {
            return false;
        }
        return true;
    }

    public override Job JobOnThing(Pawn pawn, Thing t, bool forced = false)
    {
        var pod = t as FleshParasitePod;
        if (pod == null || pod.curQuest == null)
        {
            return null;
        }
        var q = pod.curQuest;
        Pawn toCarry = null;
        if (q.target != null && !pod.target.Contains(q.target) && q.target.Spawned)
        {
            toCarry = q.target;
        }
        else if (q.flesh != null && !pod.flesh.Contains(q.flesh) && q.flesh.Spawned)
        {
            toCarry = q.flesh;
        }
        if (toCarry == null)
        {
            return null;
        }
        if (toCarry == pawn)
        {
            return JobMaker.MakeJob(FleshHiveDefOf.FH_Job_EnterParasitePod, pod);
        }
        var job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_PutPawnInParasitePod, toCarry, pod);
        job.count = 1;
        return job;
    }
}
