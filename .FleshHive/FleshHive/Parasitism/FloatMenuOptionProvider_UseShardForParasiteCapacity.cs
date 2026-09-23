using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class FloatMenuOptionProvider_UseShardForParasiteCapacity : FloatMenuOptionProvider
{
    protected override bool Drafted => true;

    protected override bool Undrafted => true;

    protected override bool Multiselect => false;

    protected override bool RequiresManipulation => true;

    protected override bool AppliesInt(FloatMenuContext context)
    {
        return GetExpandableCapacity(context.FirstSelectedPawn) != null;
    }

    protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
    {
        if (clickedThing?.def != ThingDefOf.Shard)
        {
            return null!;
        }

        Pawn pawn = context.FirstSelectedPawn;
        IShardExpandableParasiteCapacity? expandableCapacity = GetExpandableCapacity(pawn);
        if (expandableCapacity == null)
        {
            return null!;
        }

        string shardLabel = clickedThing.LabelShort;
        if (!expandableCapacity.CanIncreaseParasiteCapacity)
        {
            return new FloatMenuOption("FH_ParasiteCapacity_CannotUseShardMax".Translate(shardLabel,
                expandableCapacity.MaximumParasiteCapacity), null);
        }
        if (pawn.Downed)
        {
            return new FloatMenuOption("FH_ParasiteCapacity_CannotUseShardDowned".Translate(shardLabel, pawn.LabelShortCap), null);
        }
        if (!pawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            return new FloatMenuOption("FH_ParasiteCapacity_CannotUseShard".Translate(shardLabel) + ": " +
                "NoPath".Translate().CapitalizeFirst(), null);
        }
        if (!pawn.CanReserve(clickedThing))
        {
            return new FloatMenuOption("FH_ParasiteCapacity_CannotUseShardReserved".Translate(shardLabel), null);
        }
        if (clickedThing.IsBurning())
        {
            return new FloatMenuOption("FH_ParasiteCapacity_CannotUseShard".Translate(shardLabel) + ": " +
                "BurningLower".Translate(), null);
        }

        return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(
            "FH_ParasiteCapacity_UseShard".Translate(shardLabel), delegate
            {
                clickedThing.SetForbidden(false);
                Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_UseShardForParasiteCapacity, clickedThing);
                job.count = 1;
                pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
            }, MenuOptionPriority.High), pawn, clickedThing, "ReservedBy");
    }

    private static IShardExpandableParasiteCapacity? GetExpandableCapacity(Pawn? pawn)
    {
        return pawn?.health?.hediffSet?.hediffs?.OfType<IShardExpandableParasiteCapacity>().FirstOrDefault();
    }
}
