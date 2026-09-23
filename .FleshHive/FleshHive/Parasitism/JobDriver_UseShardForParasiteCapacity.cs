using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_UseShardForParasiteCapacity : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, 1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

        Toil wait = Toils_General.Wait(UseDurationTicks, TargetIndex.A);
        wait.WithProgressBarToilDelay(TargetIndex.A);
        yield return wait;

        Toil useShard = ToilMaker.MakeToil("UseShardForParasiteCapacity");
        useShard.initAction = delegate
        {
            IShardExpandableParasiteCapacity? expandableCapacity = pawn.health?.hediffSet?.hediffs?
                .OfType<IShardExpandableParasiteCapacity>().FirstOrDefault();
            Thing? shard = TargetThingA;
            if (expandableCapacity == null || shard == null || shard.def != ThingDefOf.Shard || shard.stackCount <= 0)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            if (!expandableCapacity.TryIncreaseParasiteCapacity())
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            shard.SplitOff(1).Destroy(DestroyMode.Vanish);
            pawn.health!.AddHediff(expandableCapacity.ShardComaDef);
            Messages.Message("FH_ParasiteCapacity_ShardUsed".Translate(pawn.LabelShortCap,
                expandableCapacity.ParasiteCapacity), pawn, MessageTypeDefOf.PositiveEvent, false);
        };
        useShard.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return useShard;
    }

    private const int UseDurationTicks = 180;
}
