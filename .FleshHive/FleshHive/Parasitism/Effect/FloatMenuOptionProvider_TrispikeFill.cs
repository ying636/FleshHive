using System;
using System.Collections.Generic;
using RimWorld;
using RimWorld.Utility;
using Verse;
using Verse.AI;

namespace FleshHive.Effect;

public class FloatMenuOptionProvider_TrispikeFill : FloatMenuOptionProvider
{
    protected override bool Drafted => true;
    protected override bool Undrafted => true;
    protected override bool Multiselect => false;

    public override IEnumerable<FloatMenuOption> GetOptionsFor(Thing clickedThing, FloatMenuContext context)
    {
        Pawn pawn = context.FirstSelectedPawn;
        if (pawn == null || clickedThing?.def == null)
        {
            yield break;
        }

        ThingDef meat = ThingDef.Named("Meat_Twisted");
        if (clickedThing.def != meat)
        {
            yield break;
        }

        if (!CompAbilityEffect_TrispikeRelease.TryGetCharge(pawn, out HediffComp_TrispikeCharge charge))
        {
            yield break;
        }

        string label = "FH_TrispikeFill".Translate();
        if (charge.Active)
        {
            yield return new FloatMenuOption("FH_TrispikeFillCharged".Translate(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            yield break;
        }

        if (!pawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
        {
            yield return new FloatMenuOption(label + ": " + "NoPath".Translate().CapitalizeFirst(), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            yield break;
        }

        int need = charge.Props.fillCount;
        if (clickedThing.stackCount < need)
        {
            yield return new FloatMenuOption("FH_TrispikeFillNeed".Translate(need), null, MenuOptionPriority.Default, null, null, 0f, null, null, true, 0);
            yield break;
        }

        Action action = delegate
        {
            Job job = JobMaker.MakeJob(FleshHiveDefOf.FH_Job_FillTrispikeCharge, clickedThing);
            job.count = need;
            pawn.jobs.TryTakeOrderedJob(job);
        };
        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(label + $" x{need}", action), pawn, clickedThing, "ReservedBy");
    }
}
