using HiveCreatureFramework;
using UnityEngine;
using Verse;

namespace FleshHive;

public class HediffComp_HelaNode : HediffComp_NodeUnit
{
    public new HediffCompProperties_HelaNode Props => (HediffCompProperties_HelaNode)props;

    public override void CompPostTick(ref float severityAdjustment)
    {
        base.CompPostTick(ref severityAdjustment);
        if (!Pawn.Spawned
            || Pawn.Dead
            || Props.maintenanceIntervalTicks <= 0
            || !Pawn.IsHashIntervalTick(Props.maintenanceIntervalTicks))
        {
            return;
        }

        MaintainControlledUnits();
    }

    public override void CompExposeData()
    {
        base.CompExposeData();
        Scribe_Values.Look(ref maintenanceCredit, "helaMaintenanceCredit");
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            maintenanceCredit = Mathf.Max(0f, maintenanceCredit);
        }
    }

    public override void Notify_Spawned()
    {
        base.Notify_Spawned();
        ParasitismSystem system = Pawn.health?.hediffSet?.GetFirstHediffOfDef(FleshHiveDefOf.FH_ParasitismSystem) as ParasitismSystem;
        if (system == null)
        {
            return;
        }

        system.SetDirty();
        Pawn.Map?.GetComponent<MapComponent_FleshHive>()?.RegisterTwistedFlesh(Pawn);
    }

    private void MaintainControlledUnits()
    {
        maintainedUnits.Clear();
        foreach (UnitGroup group in Groups)
        {
            if (group == null)
            {
                continue;
            }

            foreach (Pawn unit in group.units)
            {
                if (unit == null
                    || unit == Pawn
                    || !maintainedUnits.Add(unit)
                    || unit.Dead
                    || !unit.Spawned
                    || unit.Map != Pawn.Map)
                {
                    continue;
                }

                if (!TryMaintainUnit(unit) && TwistedFleshUtility.GetCurrentTwistedFlesh(Pawn) <= 0)
                {
                    return;
                }
            }
        }
    }

    private bool TryMaintainUnit(Pawn unit)
    {
        Need_Maintenance need = unit.needs == null ? null : unit.GetNeed<Need_Maintenance>();
        if (need == null || Props.maintenancePerInterval <= 0f || Props.maintenancePerTwistedFlesh <= 0f)
        {
            return false;
        }

        float amount = Mathf.Min(Props.maintenancePerInterval, need.MaxLevel - need.CurLevel);
        if (amount <= 0f)
        {
            return true;
        }

        if (maintenanceCredit + 0.0001f < amount)
        {
            if (!TwistedFleshUtility.ConsumeTwistedFlesh(Pawn, 1))
            {
                return false;
            }
            maintenanceCredit += Props.maintenancePerTwistedFlesh;
        }

        need.CurLevel += amount;
        maintenanceCredit = Mathf.Max(0f, maintenanceCredit - amount);
        return true;
    }

    private readonly HashSet<Pawn> maintainedUnits = new HashSet<Pawn>();
    private float maintenanceCredit;
}
