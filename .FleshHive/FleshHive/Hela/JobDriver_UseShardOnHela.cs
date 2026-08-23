using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_UseShardOnHela : JobDriver
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

        Toil useShard = ToilMaker.MakeToil("UseShardOnHela");
        useShard.initAction = delegate
        {
            Hediff_Hela hela = pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Hela) as Hediff_Hela;
            Thing shard = TargetThingA;
            if (hela == null || shard == null || shard.def != ThingDefOf.Shard || shard.stackCount <= 0)
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }
            if (!hela.TryIncreaseParasiteCapacity())
            {
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            shard.SplitOff(1).Destroy(DestroyMode.Vanish);
            pawn.health.AddHediff(FleshHiveDefOf.FH_HelaShardComa);
            Messages.Message("FH_Hela_ShardUsed".Translate(pawn.LabelShortCap, hela.ParasiteCapacity), pawn,
                MessageTypeDefOf.PositiveEvent, false);
        };
        useShard.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return useShard;
    }

    private const int UseDurationTicks = 180;
}
