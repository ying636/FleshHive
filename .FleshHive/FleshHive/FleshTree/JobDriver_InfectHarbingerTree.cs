using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_InfectHarbingerTree : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        bool ok = pawn.Reserve(job.targetA, job, 1, -1, null, errorOnFailed);
        ok &= pawn.Reserve(job.targetB, job, 1, job.count, null, errorOnFailed);
        return ok;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {  
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.ClosestTouch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.B, false, true, false, true);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);

        Toil infect = Toils_General.Wait(InfectionWorkTicks, TargetIndex.A);
        infect.WithProgressBarToilDelay(TargetIndex.A);
        infect.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);
        yield return infect;

        Toil finish = new Toil();
        finish.initAction = FinishInfection;
        finish.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return finish;
    }

    private void FinishInfection()
    {
        Thing tree = job.targetA.Thing;
        float growth = tree is Plant plant ? plant.Growth : 1f;
        Map map = tree.Map;
        IntVec3 position = tree.Position;
        Designation designation = map.designationManager.DesignationOn(tree, FleshHiveDefOf.FH_InfectHarbingerTree);
        if (designation != null)
        {
            map.designationManager.RemoveDesignation(designation);
        }

        pawn.carryTracker.CarriedThing?.Destroy(DestroyMode.Vanish);
        tree.Destroy(DestroyMode.Vanish);
        Thing fleshTree = ThingMaker.MakeThing(FleshHiveDefOf.FH_FleshTree);
        if (fleshTree is Plant fleshPlant)
        {
            fleshPlant.Growth = growth;
        }
        GenSpawn.Spawn(fleshTree, position, map);
    }

    private const int InfectionWorkTicks = 250;

    private const int TakeCount = 25;
}
