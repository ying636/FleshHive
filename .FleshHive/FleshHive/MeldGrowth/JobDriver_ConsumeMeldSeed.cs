using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class JobDriver_ConsumeMeldSeed : JobDriver
{
    public override bool TryMakePreToilReservations(bool errorOnFailed)
    {
        return pawn.Reserve(job.targetA, job, 1, 1, null, errorOnFailed);
    }

    protected override IEnumerable<Toil> MakeNewToils()
    {
        this.FailOnDespawnedNullOrForbidden(TargetIndex.A);

        yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.ClosestTouch);

        Toil consume = Toils_General.Wait(ConsumeDurationTicks, TargetIndex.A);
        consume.PlaySustainerOrSound(SoundDefOf.RawMeat_Eat);
        consume.WithProgressBarToilDelay(TargetIndex.A);
        yield return consume;

        Toil applyGrowth = ToilMaker.MakeToil("ApplyMeldGrowth");
        applyGrowth.initAction = delegate
        {
            Thing seed = TargetThingA;
            ModExtension_MeldGrowth? seedExtension = seed?.def.GetModExtension<ModExtension_MeldGrowth>();
            ModExtension_MeldGrowth? meldExtension = pawn.def.GetModExtension<ModExtension_MeldGrowth>();
            if (seed == null || seed.stackCount <= 0 || seedExtension?.meld != pawn.def
                || meldExtension?.seed != seed.def)
            {
                Messages.Message("FH_MeldGrowth_ConsumeFailed".Translate(pawn.LabelShortCap), pawn,
                    MessageTypeDefOf.RejectInput, false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            Hediff_MeldGrowth? growth = pawn.health?.hediffSet
                ?.GetFirstHediffOfDef(FleshHiveDefOf.FH_MeldGrowth) as Hediff_MeldGrowth;
            if (growth == null)
            {
                growth = (Hediff_MeldGrowth)HediffMaker.MakeHediff(FleshHiveDefOf.FH_MeldGrowth, pawn);
                growth.Severity = 1f;
                pawn.health.AddHediff(growth);
            }
            else if (!growth.TryUpgrade())
            {
                Messages.Message("FH_MeldGrowth_CannotConsumeMaxLevel".Translate(
                    seed.LabelShort, Hediff_MeldGrowth.MaximumLevel), pawn, MessageTypeDefOf.RejectInput, false);
                EndJobWith(JobCondition.Incompletable);
                return;
            }

            if (pawn.health.hediffSet.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem)
                is ParasitismSystem parasitismSystem)
            {
                parasitismSystem.SetDirty();
            }

            seed.SplitOff(1).Destroy(DestroyMode.Vanish);
            Messages.Message("FH_MeldGrowth_Upgraded".Translate(
                pawn.LabelShortCap, growth.Level, Hediff_MeldGrowth.MaximumLevel), pawn,
                MessageTypeDefOf.PositiveEvent, false);
        };
        applyGrowth.defaultCompleteMode = ToilCompleteMode.Instant;
        yield return applyGrowth;
    }

    private const int ConsumeDurationTicks = 300;
}
