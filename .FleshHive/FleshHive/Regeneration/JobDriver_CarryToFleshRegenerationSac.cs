using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_CarryToFleshRegenerationSac : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed)
            && pawn.Reserve(TargetB, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.B);
        this.FailOn(() => TargetThingA is not Pawn target || target.Destroyed || target.Dead
            || !target.Downed
            || !target.Spawned && pawn.carryTracker.CarriedThing != target
            || TargetThingB.TryGetComp<CompFleshRegenerationSac>()?.CanContain(target) != true);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.B, 60, useProgressBar: true);

        Toil enter = new Toil();
        enter.initAction = () =>
        {
            CompFleshRegenerationSac container = TargetThingB.TryGetComp<CompFleshRegenerationSac>();
            if (pawn.carryTracker.CarriedThing is not Pawn target
                || target != TargetThingA || !target.Downed || container?.CanContain(target) != true)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            container.AddUnit(target);
            if (!container.units.Contains(target))
            {
                EndJobWith(JobCondition.Incompletable);
            }
        };
        enter.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return enter;
    }
}
