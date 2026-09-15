using HiveCreatureFramework;
using RimWorld;
using Verse;
using Verse.AI;

namespace FleshHive;

public class CompFleshRegenerationSac : CompFleshHiveContainer, IThingHolderWithDrawnPawn
{
    public new CompProperties_FleshRegenerationSac Props => (CompProperties_FleshRegenerationSac)props;

    public float HeldPawnDrawPos_Y => parent.DrawPos.y + 0.03658537f;

    public float HeldPawnBodyAngle => 0f;

    public PawnPosture HeldPawnPosture => PawnPosture.LayingOnGroundFaceUp;

    public bool CanTreat(Pawn pawn)
    {
        return pawn != null && !pawn.Dead && pawn.Faction == Faction.OfPlayer
            && pawn.Faction == parent.Faction
            && pawn.RaceProps.FleshType == FleshTypeDefOf.Fleshbeast
            && HCFGameUtility.GetUnitComp(pawn)?.group is { } group
            && group.hive is Pawn node && node != pawn && node.Faction == parent.Faction
            && HCFGameUtility.IsNodeUnit(node);
    }

    public bool NeedsTreatment(Pawn pawn)
    {
        return pawn.health.hediffSet.hediffs.Any(hediff =>
            hediff is Hediff_Injury { Severity: > 0f }
            || hediff.def == HediffDefOf.BloodLoss
            || hediff is Hediff_MissingPart missing
                && missing.Part.parent != null
                && pawn.health.hediffSet.GetFirstHediffMatchingPart<Hediff_AddedPart>(missing.Part.parent) == null
                && pawn.health.hediffSet.GetFirstHediffMatchingPart<Hediff_MissingPart>(missing.Part.parent) == null);
    }

    public override bool CanContain(Pawn pawn)
    {
        return parent.Spawned && units.Count == 0 && CanTreat(pawn) && NeedsTreatment(pawn);
    }

    public override void AddUnit(Pawn unit)
    {
        if (!CanContain(unit))
        {
            Log.Error($"Cannot admit {unit} to flesh regeneration sac {parent}.");
            return;
        }

        base.AddUnit(unit);
        unit.Drawer.renderer.SetAllGraphicsDirty();
    }

    public override void CompTick()
    {
        if (!parent.Spawned || !parent.IsHashIntervalTick(Props.interval))
        {
            return;
        }

        for (int i = units.Count - 1; i >= 0; i--)
        {
            Pawn pawn = units[i];
            if (CanTreat(pawn))
            {
                Heal(pawn, GetHealPoint());
            }
            if (!CanTreat(pawn) || !NeedsTreatment(pawn))
            {
                if (units.TryDrop(pawn, parent.Position, parent.Map, ThingPlaceMode.Near, out _))
                {
                    pawn.Drawer.renderer.SetAllGraphicsDirty();
                }
            }
        }
    }

    public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
    {
        if (!CanContain(selPawn) || parent.IsForbidden(selPawn)
            || !selPawn.CanReserveAndReach(parent, PathEndMode.Touch, Danger.Some))
        {
            yield break;
        }

        yield return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(parent.LabelCap, () =>
        {
            selPawn.jobs.TryTakeOrderedJob(
                JobMaker.MakeJob(FleshHiveDefOf.FH_Job_EnterFleshRegenerationSac, parent), JobTag.Misc);
        }), selPawn, parent);
    }

    public override string CompInspectStringExtra()
    {
        return units.Count == 0 ? base.CompInspectStringExtra()
            : units[0].LabelShortCap + ": " + "UnitHealthPercent".Translate()
                + " " + units[0].health.summaryHealth.SummaryHealthPercent.ToStringPercent();
    }
}
