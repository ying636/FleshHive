using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class FloatMenuOptionProvider_UseShardOnHela : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        return pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Hela) is Hediff_Hela;
    }

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing?.def != ThingDefOf.Shard)
        {
            return null;
        }

        Pawn pawn = context.FirstSelectedPawn;
        Hediff_Hela hela = pawn?.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_Hela) as Hediff_Hela;
        if (hela == null)
        {
            return null;
        }

        string shardLabel = clickedThing.LabelShort;
        if (!hela.CanIncreaseParasiteCapacity)
        {
            return new FloatMenuOption("FH_Hela_CannotUseShardMax".Translate(shardLabel, hela.MaximumParasiteCapacity), null);
        }
        if (pawn.Downed)
        {
            return new FloatMenuOption("FH_Hela_CannotUseShardDowned".Translate(shardLabel), null);
        }
        if (!pawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return new FloatMenuOption("FH_Hela_CannotUseShard".Translate(shardLabel) + ": " + "NoPath".Translate().CapitalizeFirst(), null);
        }
        if (!pawn.CanReserve(clickedThing))
        {
            return new FloatMenuOption("FH_Hela_CannotUseShardReserved".Translate(shardLabel), null);
        }
        if (clickedThing.IsBurning())
        {
            return new FloatMenuOption("FH_Hela_CannotUseShard".Translate(shardLabel) + ": " + "BurningLower".Translate(), null);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption("FH_Hela_UseShard".Translate(shardLabel), delegate
        {
            clickedThing.SetForbidden(false);
            Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_UseShardOnHela, clickedThing);
            job.count = 1;
            pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }, MenuOptionPriority.High), pawn, clickedThing, "ReservedBy");
    }
}
