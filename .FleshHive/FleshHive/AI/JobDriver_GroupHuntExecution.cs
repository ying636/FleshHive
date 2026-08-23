using System.Collections.Generic;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_GroupHuntExecution : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(TargetPawnA, job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedOrNull(TargetIndex.A);
        this.FailOn(() => TargetPawnA == null || TargetPawnA.Dead || !TargetPawnA.Downed);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch)
            .FailOnMobile(TargetIndex.A);
        yield return Toils_General.WaitWith(TargetIndex.A, ExecutionTicks, useProgressBar: true)
            .FailOnMobile(TargetIndex.A);
        yield return Toils_General.Do(ExecutePrey);
    }

    private void ExecutePrey()
    {
        Pawn prey = TargetPawnA;
        if (prey == null || prey.Dead || !prey.Downed)
        {
            return;
        }

        ExecutionUtility.DoHuntingExecution(pawn, prey);
        pawn.records?.Increment(RecordDefOf.AnimalsSlaughtered);
    }

    private const int ExecutionTicks = 180;
}
