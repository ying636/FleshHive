using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_EnterFleshRegenerationSac : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);
        this.FailOn(() => TargetThingA.TryGetComp<CompFleshRegenerationSac>()?.CanContain(pawn) != true
            || !job.playerForced && (pawn.Drafted || pawn.InMentalState));

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_General.WaitWith(TargetIndex.A, 60, useProgressBar: true);

        Toil enter = new Toil();
        enter.initAction = () =>
        {
            CompFleshRegenerationSac container = TargetThingA.TryGetComp<CompFleshRegenerationSac>();
            if (container == null || !container.CanContain(pawn))
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            container.AddUnit(pawn);
        };
        enter.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return enter;
    }
}
