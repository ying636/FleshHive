using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_RefillTwistedFlesh_Help : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Pawn targetPawn = this.job.targetB.Pawn;
        if (targetPawn == null)
        {
            return false;
        }
        if (!this.pawn.Reserve(targetPawn, this.job, 1, -1, null, errorOnFailed))
        {
            return false;
        }
        if (!this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed))
        {
            return false;
        }
        return true;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);

        yield return Toils_General.WaitWith(TargetIndex.B, 30, true).FailOn(
            () => TargetB.Thing.Map != this.pawn.Map || this.pawn.Position.DistanceTo(TargetB.Thing.Position) > 2f);

        Toil fill = ToilMaker.MakeToil("FillTargetTwistedFlesh");
        fill.initAction = delegate
        {
            Pawn targetPawn = this.job.targetB.Pawn;
            if (targetPawn == null)
            {
                this.EndJobWith(JobCondition.Incompletable);
                return;
            }

            Thing carried = this.pawn.carryTracker.CarriedThing;
            if (carried == null || carried.def != ThingDef.Named("Meat_Twisted"))
            {
                this.EndJobWith(JobCondition.Incompletable);
                return;
            }

            int needed = TwistedFleshUtility.GetNeededAmount(targetPawn);
            int count = Mathf.Min(carried.stackCount, needed, this.job.count);
            if (count <= 0)
            {
                this.EndJobWith(JobCondition.Succeeded);
                return;
            }

            Thing consumed = carried.SplitOff(count);
            consumed.Destroy(DestroyMode.Vanish);
            TwistedFleshUtility.FillTwistedFlesh(targetPawn, count);
        };
        yield return fill;
    }
}
