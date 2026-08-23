using System.Collections.Generic;
using UnityEngine;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_RefillTwistedFlesh : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return this.pawn.Reserve(this.job.targetA, this.job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 30, true).FailOn(
            () => TargetThingA.Map != this.pawn.Map || this.pawn.Position.DistanceTo(TargetThingA.Position) > 2f);
        Toil fill = ToilMaker.MakeToil("RefillTwistedFlesh");
        fill.initAction = delegate
        {
            Thing target = TargetThingA;
            if (target == null || target.def != FleshHiveDefOf.Meat_Twisted)
            {
                this.EndJobWith(JobCondition.Incompletable);
                return;
            }

            int needed = TwistedFleshUtility.GetNeededAmount(this.pawn);
            if (needed <= 0)
            {
                this.EndJobWith(JobCondition.Succeeded);
                return;
            }

            int count = Mathf.Min(target.stackCount, needed, this.job.count);
            if (count <= 0)
            {
                this.EndJobWith(JobCondition.Incompletable);
                return;
            }

            Thing consumed = target.SplitOff(count);
            consumed.Destroy(DestroyMode.Vanish);

            TwistedFleshUtility.FillTwistedFlesh(this.pawn, count);
        };

        yield return fill;
    }
}
