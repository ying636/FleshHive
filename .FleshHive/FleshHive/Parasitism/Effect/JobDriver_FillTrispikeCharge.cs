using System.Collections.Generic;
using Verse;
using Verse.AI;

namespace FleshHive.Effect;

public class JobDriver_FillTrispikeCharge : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return this.pawn.Reserve(this.job.targetA, this.job, 1, this.job.count, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOnCannotTouch(TargetIndex.A, PathEndMode.Touch);

        if (!CompAbilityEffect_TrispikeRelease.TryGetCharge(this.pawn, out HediffComp_TrispikeCharge charge))
        {
            yield break;
        }

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);
        yield return Toils_General.WaitWith(TargetIndex.A, 30, true).FailOn(
            () => TargetThingA.Map != this.pawn.Map || this.pawn.Position.DistanceTo(TargetThingA.Position) > 2f);
        Toil fill = ToilMaker.MakeToil("FillTrispikeCharge");
        fill.initAction = delegate
        {
            Thing target = TargetThingA;
            if (target == null)
            {
                this.EndJobWith(JobCondition.Incompletable);
                return;
            }

            int count = charge.Props.fillCount;
            if (target.stackCount > count)
            {
                Thing consumed = target.SplitOff(count);
                consumed.Destroy(DestroyMode.Vanish);
            }
            else
            {
                target.Destroy();
            }

            charge.SetActive(true);
        };

        yield return fill;
    }
}

