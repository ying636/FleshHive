using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class FloatMenuOptionProvider_ConsumeMeldSeed : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        return pawn?.Faction == Faction.OfPlayer
               && pawn.def.GetModExtension<ModExtension_MeldGrowth>()?.seed != null;
    }

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        ModExtension_MeldGrowth? seedExtension = clickedThing?.def.GetModExtension<ModExtension_MeldGrowth>();
        if (seedExtension?.meld == null)
        {
            return null;
        }

        Pawn pawn = context.FirstSelectedPawn;
        ModExtension_MeldGrowth? meldExtension = pawn?.def.GetModExtension<ModExtension_MeldGrowth>();
        if (pawn == null || meldExtension?.seed == null)
        {
            return null;
        }

        string seedLabel = clickedThing.LabelShort;
        if (seedExtension.meld != pawn.def || meldExtension.seed != clickedThing.def)
        {
            return new FloatMenuOption("FH_MeldGrowth_CannotConsumeWrongSeed".Translate(seedLabel, pawn.LabelShortCap), null);
        }

        Hediff_MeldGrowth? growth = pawn.health?.hediffSet
            ?.GetFirstHediffOfDef(FleshHiveDefOf.FH_MeldGrowth) as Hediff_MeldGrowth;
        if (growth != null && !growth.CanUpgrade)
        {
            return new FloatMenuOption("FH_MeldGrowth_CannotConsumeMaxLevel".Translate(seedLabel, Hediff_MeldGrowth.MaximumLevel), null);
        }
        if (pawn.Downed)
        {
            return new FloatMenuOption("FH_MeldGrowth_CannotConsumeDowned".Translate(seedLabel), null);
        }
        if (!pawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return new FloatMenuOption("FH_MeldGrowth_CannotConsume".Translate(seedLabel) + ": "
                + "NoPath".Translate().CapitalizeFirst(), null);
        }
        if (!pawn.CanReserve(clickedThing))
        {
            return new FloatMenuOption("FH_MeldGrowth_CannotConsumeReserved".Translate(seedLabel), null);
        }
        if (clickedThing.IsBurning())
        {
            return new FloatMenuOption("FH_MeldGrowth_CannotConsume".Translate(seedLabel) + ": "
                + "BurningLower".Translate(), null);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
            "FH_MeldGrowth_ConsumeSeed".Translate(seedLabel), delegate
            {
                clickedThing.SetForbidden(false);
                Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_ConsumeMeldSeed, clickedThing);
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }, MenuOptionPriority.High), pawn, clickedThing, "ReservedBy");
    }
}
