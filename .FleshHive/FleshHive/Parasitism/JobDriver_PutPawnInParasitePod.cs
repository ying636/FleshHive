using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_PutPawnInParasitePod : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        Pawn pawnToCarry = (Pawn)this.job.targetA.Thing;
        FleshParasitePod pod = (FleshParasitePod)this.job.targetB.Thing;
        bool ok = this.pawn.Reserve((Thing)pawnToCarry, this.job, 1, -1, null, errorOnFailed);
        ok &= this.pawn.Reserve((Thing)pod, this.job, 1, -1, null, errorOnFailed);
        return ok;
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => this.job.targetB.Thing is not FleshParasitePod pod || pod.curQuest == null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
        yield return Toils_Haul.StartCarryThing(TargetIndex.A);
        yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.InteractionCell);
        Toil putInPod = new Toil();
        putInPod.initAction = delegate
        {
            FleshParasitePod pod = (FleshParasitePod)this.job.targetB.Thing;
            Pawn carried = this.pawn.carryTracker.CarriedThing as Pawn;
            if (carried == null)
            {
                return;
            }
            if (pod.curQuest != null)
            { 
                if (pod.curQuest.target == carried)
                {
                    pod.target.TryAddOrTransfer(carried, true);
                }
                else if (pod.curQuest.flesh == carried)
                {
                    pod.flesh.TryAddOrTransfer(carried, true);
                }
                pod.curQuest.TryStart(pod);
            } 
        };
        putInPod.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return putInPod;
    }
}

public class JobDriver_EnterParasitePod : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        if (this.job.targetA.Thing is not FleshParasitePod pod)
        {
            return false;
        }
        return this.pawn.Reserve(pod, this.job, 1, -1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOn(() => this.job.targetA.Thing is not FleshParasitePod pod || pod.curQuest == null);
        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.InteractionCell);
        Toil enterPod = new Toil();
        enterPod.initAction = delegate
        {
            FleshParasitePod pod = (FleshParasitePod)this.job.targetA.Thing;
            if (pod.curQuest == null)
            {
                return;
            }
            this.pawn.DeSpawn();
            if (pod.curQuest.target == this.pawn)
            {
                pod.target.TryAdd(this.pawn, true);
            }
            else if (pod.curQuest.flesh == this.pawn)
            {
                pod.flesh.TryAdd(this.pawn, true);
            } 
            pod.curQuest.TryStart(pod);
        };
        enterPod.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return enterPod;
    }
}
